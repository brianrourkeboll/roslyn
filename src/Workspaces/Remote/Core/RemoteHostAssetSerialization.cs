﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Nerdbank.Streams;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostAssetSerialization
    {
        private static readonly ObjectPool<SerializableBytes.ReadWriteStream> s_streamPool = new(SerializableBytes.CreateWritableStream);

        public static async ValueTask WriteDataAsync(
            PipeWriter pipeWriter,
            AssetPath assetPath,
            ReadOnlyMemory<Checksum> checksums,
            SolutionAssetStorage.Scope scope,
            ISerializerService serializer,
            CancellationToken cancellationToken)
        {
            var pipeWriterStream = pipeWriter.AsStream();

            var foundChecksumCount = 0;

            await scope.AddAssetsAsync(
                assetPath,
                checksums,
                WriteAssetToPipeAsync,
                cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfTrue(foundChecksumCount != checksums.Length);

            return;

            async ValueTask WriteAssetToPipeAsync(Checksum checksum, object asset, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(asset);
                foundChecksumCount++;

                using var pooledObject = s_streamPool.GetPooledObject();
                var tempStream = pooledObject.Object;
                tempStream.Position = 0;

                // Don't truncate the stream as we're going to be writing to it multiple times.  This will allow us to
                // reuse the internal chunks of the buffer, without having to reallocate them over and over again.
                tempStream.SetLength(0, truncate: false);

                // Write the asset to a temporary buffer so we can calculate its length.  Note: as this is an in-memory
                // temporary buffer, we don't have to worry about synchronous writes on it blocking on the pipe-writer.
                // Instead, we'll handle the pipe-writing ourselves afterwards in a completely async fasion.
                WriteAssetToTempStream(tempStream, checksum, asset);

                // Write the length of the asset to the pipe writer so the reader knows how much data to read.
                WriteLengthToPipeWriter(tempStream.Length);

                // Ensure we flush out the length so the reading side knows how much data to read.
                await pipeWriterStream.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Now, asynchronously copy the temp buffer over to the writer stream.
                tempStream.Position = 0;
                await tempStream.CopyToAsync(pipeWriter, cancellationToken).ConfigureAwait(false);

                // We flush after each item as that forms a reasonably sized chunk of data to want to then send over
                // the pipe for the reader on the other side to read.  This allows the item-writing to remain
                // entirely synchronous without any blocking on async flushing, while also ensuring that we're not
                // buffering the entire stream of data into the pipe before it gets sent to the other side.
                await pipeWriterStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            void WriteAssetToTempStream(Stream tempStream, Checksum checksum, object asset)
            {
                using var objectWriter = new ObjectWriter(tempStream, leaveOpen: true, cancellationToken);
                {
                    // Write the checksum for the asset we're writing out, so the other side knows what asset this is.
                    checksum.WriteTo(objectWriter);

                    // Write out the kind so the receiving end knows how to deserialize this asset.
                    var kind = asset.GetWellKnownSynchronizationKind();
                    objectWriter.WriteInt32((int)kind);

                    // Now serialize out the asset itself.
                    serializer.Serialize(asset, objectWriter, scope.ReplicationContext, cancellationToken);
                }
            }

            void WriteLengthToPipeWriter(long length)
            {
                Contract.ThrowIfTrue(length > int.MaxValue);

                var span = pipeWriter.GetSpan(sizeof(int));
                BinaryPrimitives.WriteInt32LittleEndian(span, (int)length);
                pipeWriter.Advance(span.Length);
            }
        }

        public static ValueTask ReadDataAsync<T, TArg>(
            PipeReader pipeReader, int objectCount, ISerializerService serializerService, Action<Checksum, T, TArg> callback, TArg arg, CancellationToken cancellationToken)
        {
            // Suppress ExecutionContext flow for asynchronous operations operate on the pipe. In addition to avoiding
            // ExecutionContext allocations, this clears the LogicalCallContext and avoids the need to clone data set by
            // CallContext.LogicalSetData at each yielding await in the task tree.
            //
            // ⚠ DO NOT AWAIT INSIDE THE USING. The Dispose method that restores ExecutionContext flow must run on the
            // same thread where SuppressFlow was originally run.
            using var _ = FlowControlHelper.TrySuppressFlow();
            return ReadDataSuppressedFlowAsync(pipeReader, objectCount, serializerService, callback, arg, cancellationToken);

            static async ValueTask ReadDataSuppressedFlowAsync(
                PipeReader pipeReader, int objectCount, ISerializerService serializerService, Action<Checksum, T, TArg> callback, TArg arg, CancellationToken cancellationToken)
            {
                using var pipeReaderStream = pipeReader.AsStream(leaveOpen: true);

                for (var i = 0; i < objectCount; i++)
                {
                    // First, read the length of the data chunk we'll be reading.
                    var readResult = await pipeReader.ReadAtLeastAsync(sizeof(int), cancellationToken).ConfigureAwait(false);
                    var length = ReadLength(readResult);

                    // Advance past the length.
                    pipeReader.AdvanceTo(readResult.Buffer.GetPosition(sizeof(int)));

                    // Now buffer in the rest of the data we need to read.  Because we're reading as much data in as
                    // we'll need to consume, all further reading (for this single item) can handle synchronously
                    // without worrying about this blocking the reading thread on cross-process pipe io.
                    await pipeReader.ReadAtLeastAsync(length, cancellationToken).ConfigureAwait(false);

                    using var reader = ObjectReader.GetReader(pipeReaderStream, leaveOpen: true, cancellationToken);
                    {
                        var checksum = Checksum.ReadFrom(reader);
                        var kind = (WellKnownSynchronizationKind)reader.ReadInt32();

                        // in service hub, cancellation means simply closed stream
                        var result = serializerService.Deserialize(kind, reader, cancellationToken);
                        Contract.ThrowIfNull(result);
                        callback(checksum, (T)result, arg);
                    }
                }
            }

            static int ReadLength(ReadResult readResult)
            {
                var sequenceReader = new SequenceReader<byte>(readResult.Buffer);
                Contract.ThrowIfFalse(sequenceReader.TryReadLittleEndian(out int length));
                return length;
            }
        }
    }
}
