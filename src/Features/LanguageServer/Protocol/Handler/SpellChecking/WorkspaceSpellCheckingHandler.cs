﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SpellChecking
{
    [Method(VSInternalMethods.WorkspacePullDiagnosticName)]
    internal class WorkspaceSpellCheckingHandler : AbstractSpellCheckingHandler<VSInternalWorkspaceSpellCheckableParams, VSInternalWorkspaceSpellCheckableReport>
    {
        public override TextDocumentIdentifier? GetTextDocumentIdentifier(VSInternalWorkspaceSpellCheckableParams request)
            => null;

        protected override VSInternalWorkspaceSpellCheckableReport CreateReport(TextDocumentIdentifier identifier, VSInternalSpellCheckableRange[]? ranges, string? resultId)
            => new()
            {
                TextDocument = identifier,
                Ranges = ranges!,
                ResultId = resultId,
            };

        protected override ImmutableArray<PreviousResult>? GetPreviousResults(VSInternalWorkspaceSpellCheckableParams requestParams)
            => requestParams.PreviousResults?.Where(d => d.PreviousResultId != null).Select(d => new PreviousResult(d.PreviousResultId!, d.TextDocument!)).ToImmutableArray();

        protected override ImmutableArray<Document> GetOrderedDocuments(RequestContext context, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context.Solution);

            // If we're being called from razor, we do not support WorkspaceDiagnostics at all.  For razor, workspace
            // diagnostics will be handled by razor itself, which will operate by calling into Roslyn and asking for
            // document-diagnostics instead.
            if (context.ClientName != null)
                return ImmutableArray<Document>.Empty;

            using var _ = ArrayBuilder<Document>.GetInstance(out var result);

            var solution = context.Solution;

            var documentTrackingService = solution.Workspace.Services.GetRequiredService<IDocumentTrackingService>();

            // Collect all the documents from the solution in the order we'd like to get diagnostics for.  This will
            // prioritize the files from currently active projects, but then also include all other docs in all projects
            // (depending on current FSA settings).

            var activeDocument = documentTrackingService.GetActiveDocument(solution);
            var visibleDocuments = documentTrackingService.GetVisibleDocuments(solution);

            // Now, prioritize the projects related to the active/visible files.
            AddDocumentsFromProject(activeDocument?.Project, context.SupportedLanguages);
            foreach (var doc in visibleDocuments)
                AddDocumentsFromProject(doc.Project, context.SupportedLanguages);

            // finally, add the remainder of all documents.
            foreach (var project in solution.Projects)
                AddDocumentsFromProject(project, context.SupportedLanguages);

            // Ensure that we only process documents once.
            result.RemoveDuplicates();
            return result.ToImmutable();

            void AddDocumentsFromProject(Project? project, ImmutableArray<string> supportedLanguages)
            {
                if (project == null)
                    return;

                if (!supportedLanguages.Contains(project.Language))
                {
                    // This project is for a language not supported by the LSP server making the request.
                    // Do not report diagnostics for these projects.
                    return;
                }

                // Otherwise, if the user has an open file from this project, or FSA is on, then include all the
                // documents from it. If all features are enabled for source generated documents, make sure they are
                // included as well.
                var documents = project.Documents;
                foreach (var document in documents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Only consider closed documents here (and only open ones in the DocumentPullDiagnosticHandler).
                    // Each handler treats those as separate worlds that they are responsible for.
                    if (context.IsTracking(document.GetURI()))
                    {
                        context.TraceInformation($"Skipping tracked document: {document.GetURI()}");
                        continue;
                    }

                    result.Add(document);
                }
            }
        }
    }
}
