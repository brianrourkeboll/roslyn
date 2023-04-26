﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class RemoteDebuggingSessionProxy : IActiveStatementSpanProvider, IDisposable
    {
        private readonly IDisposable? _connection;
        private readonly DebuggingSessionId _sessionId;
        private readonly Workspace _workspace;

        public RemoteDebuggingSessionProxy(Workspace workspace, IDisposable? connection, DebuggingSessionId sessionId)
        {
            _connection = connection;
            _sessionId = sessionId;
            _workspace = workspace;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        private IEditAndContinueService GetLocalService()
            => _workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>().Service;

        public async ValueTask BreakStateOrCapabilitiesChangedAsync(IDiagnosticAnalyzerService diagnosticService, EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource, bool? inBreakState, CancellationToken cancellationToken)
        {
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().BreakStateOrCapabilitiesChanged(_sessionId, inBreakState, out documentsToReanalyze);
            }
            else
            {
                var documentsToReanalyzeOpt = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DocumentId>>(
                    (service, cancallationToken) => service.BreakStateOrCapabilitiesChangedAsync(_sessionId, inBreakState, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                documentsToReanalyze = documentsToReanalyzeOpt.HasValue ? documentsToReanalyzeOpt.Value : ImmutableArray<DocumentId>.Empty;
            }

            // clear all reported rude edits:
            diagnosticService.Reanalyze(_workspace, projectIds: null, documentIds: documentsToReanalyze, highPriority: false);

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics(isSessionEnding: false);
        }

        public async ValueTask EndDebuggingSessionAsync(Solution compileTimeSolution, EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource, IDiagnosticAnalyzerService diagnosticService, CancellationToken cancellationToken)
        {
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().EndDebuggingSession(_sessionId, out documentsToReanalyze);
            }
            else
            {
                var documentsToReanalyzeOpt = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DocumentId>>(
                    (service, cancallationToken) => service.EndDebuggingSessionAsync(_sessionId, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                documentsToReanalyze = documentsToReanalyzeOpt.HasValue ? documentsToReanalyzeOpt.Value : ImmutableArray<DocumentId>.Empty;
            }

            var designTimeDocumentsToReanalyze = await CompileTimeSolutionProvider.GetDesignTimeDocumentsAsync(
                compileTimeSolution, documentsToReanalyze, designTimeSolution: _workspace.CurrentSolution, cancellationToken).ConfigureAwait(false);

            // clear all reported rude edits:
            diagnosticService.Reanalyze(_workspace, projectIds: null, documentIds: designTimeDocumentsToReanalyze, highPriority: false);

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics(isSessionEnding: true);

            Dispose();
        }

        public async ValueTask<(
                ModuleUpdates updates,
                ImmutableArray<DiagnosticData> diagnostics,
                ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> rudeEdits,
                DiagnosticData? syntaxError)> EmitSolutionUpdateAsync(
            Solution solution,
            ActiveStatementSpanProvider activeStatementSpanProvider,
            IDiagnosticAnalyzerService diagnosticService,
            EditAndContinueDiagnosticUpdateSource diagnosticUpdateSource,
            CancellationToken cancellationToken)
        {
            ModuleUpdates moduleUpdates;
            ImmutableArray<DiagnosticData> diagnosticData;
            ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)> rudeEdits;
            DiagnosticData? syntaxError;

            try
            {
                var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
                if (client == null)
                {
                    var results = await GetLocalService().EmitSolutionUpdateAsync(_sessionId, solution, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
                    moduleUpdates = results.ModuleUpdates;
                    diagnosticData = results.GetDiagnosticData(solution);
                    rudeEdits = results.RudeEdits;
                    syntaxError = results.GetSyntaxErrorData(solution);
                }
                else
                {
                    var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, EmitSolutionUpdateResults.Data>(
                        solution,
                        (service, solutionInfo, callbackId, cancellationToken) => service.EmitSolutionUpdateAsync(solutionInfo, callbackId, _sessionId, cancellationToken),
                        callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                        cancellationToken).ConfigureAwait(false);

                    if (result.HasValue)
                    {
                        moduleUpdates = result.Value.ModuleUpdates;
                        diagnosticData = result.Value.Diagnostics;
                        rudeEdits = result.Value.RudeEdits;
                        syntaxError = result.Value.SyntaxError;
                    }
                    else
                    {
                        moduleUpdates = new ModuleUpdates(ModuleUpdateStatus.RestartRequired, ImmutableArray<ModuleUpdate>.Empty);
                        diagnosticData = ImmutableArray<DiagnosticData>.Empty;
                        rudeEdits = ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)>.Empty;
                        syntaxError = null;
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
            {
                var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(RudeEditKind.InternalError);

                var diagnostic = Diagnostic.Create(
                    descriptor,
                    Location.None,
                    string.Format(descriptor.MessageFormat.ToString(), "", e.Message));

                diagnosticData = ImmutableArray.Create(DiagnosticData.Create(solution, diagnostic, project: null));
                rudeEdits = ImmutableArray<(DocumentId DocumentId, ImmutableArray<RudeEditDiagnostic> Diagnostics)>.Empty;
                moduleUpdates = new ModuleUpdates(ModuleUpdateStatus.RestartRequired, ImmutableArray<ModuleUpdate>.Empty);
                syntaxError = null;
            }

            // clear emit/apply diagnostics reported previously:
            diagnosticUpdateSource.ClearDiagnostics(isSessionEnding: false);

            // clear all reported rude edits:
            diagnosticService.Reanalyze(_workspace, projectIds: null, documentIds: rudeEdits.Select(d => d.DocumentId), highPriority: false);

            // report emit/apply diagnostics:
            diagnosticUpdateSource.ReportDiagnostics(_workspace, solution, diagnosticData, rudeEdits);

            return (moduleUpdates, diagnosticData, rudeEdits, syntaxError);
        }

        public async ValueTask CommitSolutionUpdateAsync(IDiagnosticAnalyzerService diagnosticService, CancellationToken cancellationToken)
        {
            ImmutableArray<DocumentId> documentsToReanalyze;

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().CommitSolutionUpdate(_sessionId, out documentsToReanalyze);
            }
            else
            {
                var documentsToReanalyzeOpt = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DocumentId>>(
                    (service, cancallationToken) => service.CommitSolutionUpdateAsync(_sessionId, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                documentsToReanalyze = documentsToReanalyzeOpt.HasValue ? documentsToReanalyzeOpt.Value : ImmutableArray<DocumentId>.Empty;
            }

            // clear all reported rude edits:
            diagnosticService.Reanalyze(_workspace, projectIds: null, documentIds: documentsToReanalyze, highPriority: false);
        }

        public async ValueTask DiscardSolutionUpdateAsync(CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().DiscardSolutionUpdate(_sessionId);
                return;
            }

            await client.TryInvokeAsync<IRemoteEditAndContinueService>(
                (service, cancellationToken) => service.DiscardSolutionUpdateAsync(_sessionId, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace.Services.SolutionServices, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetCurrentActiveStatementPositionAsync(_sessionId, solution, activeStatementSpanProvider, instructionId, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, LinePositionSpan?>(
                solution,
                (service, solutionInfo, callbackId, cancellationToken) => service.GetCurrentActiveStatementPositionAsync(solutionInfo, callbackId, _sessionId, instructionId, cancellationToken),
                callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : null;
        }

        public async ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(Solution solution, ManagedInstructionId instructionId, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace.Services.SolutionServices, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().IsActiveStatementInExceptionRegionAsync(_sessionId, solution, instructionId, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, bool?>(
                solution,
                (service, solutionInfo, cancellationToken) => service.IsActiveStatementInExceptionRegionAsync(solutionInfo, _sessionId, instructionId, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : null;
        }

        public async ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetBaseActiveStatementSpansAsync(_sessionId, solution, documentIds, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<ImmutableArray<ActiveStatementSpan>>>(
                solution,
                (service, solutionInfo, cancellationToken) => service.GetBaseActiveStatementSpansAsync(solutionInfo, _sessionId, documentIds, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : ImmutableArray<ImmutableArray<ActiveStatementSpan>>.Empty;
        }

        public async ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(TextDocument document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            // filter out documents that are not synchronized to remote process before we attempt remote invoke:
            if (!RemoteSupportedLanguages.IsSupported(document.Project.Language))
            {
                return ImmutableArray<ActiveStatementSpan>.Empty;
            }

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await GetLocalService().GetAdjustedActiveStatementSpansAsync(_sessionId, document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
            }

            var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<ActiveStatementSpan>>(
                document.Project.Solution,
                (service, solutionInfo, callbackId, cancellationToken) => service.GetAdjustedActiveStatementSpansAsync(solutionInfo, callbackId, _sessionId, document.Id, cancellationToken),
                callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : ImmutableArray<ActiveStatementSpan>.Empty;
        }
    }
}
