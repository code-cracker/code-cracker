using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.Test
{
    public class FixAllDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        private readonly ImmutableHashSet<string> diagnosticIds;

        /// <summary>
        /// Delegate to fetch diagnostics for any given document within the given fix all scope.
        /// This delegate is invoked by <see cref="GetDocumentDiagnosticsAsync(Document, CancellationToken)"/> with the given <see cref="diagnosticIds"/> as arguments.
        /// </summary>
        private readonly Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync;

        /// <summary>
        /// Delegate to fetch diagnostics for any given project within the given fix all scope.
        /// This delegate is invoked by <see cref="GetProjectDiagnosticsAsync(Project, CancellationToken)"/> and <see cref="GetAllDiagnosticsAsync(Project, CancellationToken)"/>
        /// with the given <see cref="diagnosticIds"/> as arguments.
        /// The boolean argument to the delegate indicates whether or not to return location-based diagnostics, i.e.
        /// (a) False => Return only diagnostics with <see cref="Location.None"/>.
        /// (b) True => Return all project diagnostics, regardless of whether or not they have a location.
        /// </summary>
        private readonly Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync;

        public FixAllDiagnosticProvider(
            ImmutableHashSet<string> diagnosticIds,
            Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
            Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync)
        {
            this.diagnosticIds = diagnosticIds;
            this.getDocumentDiagnosticsAsync = getDocumentDiagnosticsAsync;
            this.getProjectDiagnosticsAsync = getProjectDiagnosticsAsync;
        }

#pragma warning disable CC0031
#pragma warning disable CC0016
        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken) =>
            getDocumentDiagnosticsAsync(document, diagnosticIds, cancellationToken);

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            getProjectDiagnosticsAsync(project, true, diagnosticIds, cancellationToken);

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            getProjectDiagnosticsAsync(project, false, diagnosticIds, cancellationToken);
    }
#pragma warning restore CC0016
#pragma warning restore CC0031
}