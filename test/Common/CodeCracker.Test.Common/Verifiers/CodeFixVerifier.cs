using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test
{
    /// <summary>
    /// Superclass of all Unit tests made for diagnostics with codefixes.
    /// Contains methods used to verify correctness of codefixes
    /// </summary>
    public abstract partial class CodeFixVerifier : DiagnosticVerifier
    {
        /// <summary>
        /// Returns the codefix being tested (C#) - to be implemented in non-abstract class
        /// </summary>
        /// <returns>The CodeFixProvider to be used for CSharp code</returns>
        protected virtual CodeFixProvider GetCodeFixProvider() => null;

        /// <summary>
        /// Called to test a C# codefix when applied on the inputted string as a source
        /// </summary>
        /// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        /// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
        protected async Task VerifyCSharpFixAsync(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, bool formatBeforeCompare = true, CodeFixProvider codeFixProvider = null)
        {
            if (formatBeforeCompare)
            {
                oldSource = await FormatSourceAsync(LanguageNames.CSharp, oldSource).ConfigureAwait(true);
                newSource = await FormatSourceAsync(LanguageNames.CSharp, newSource).ConfigureAwait(true);
            }
            codeFixProvider = codeFixProvider ?? GetCodeFixProvider();
            var diagnosticAnalyzer = GetDiagnosticAnalyzer();
            if (diagnosticAnalyzer != null)
                await VerifyFixAsync(LanguageNames.CSharp, diagnosticAnalyzer, codeFixProvider, oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics).ConfigureAwait(true);
            else
                await VerifyFixAsync(LanguageNames.CSharp, codeFixProvider.FixableDiagnosticIds, codeFixProvider, oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics).ConfigureAwait(true);
        }

        /// <summary>
        /// Called to test a VB codefix when applied on the inputted string as a source
        /// </summary>
        /// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        protected async Task VerifyBasicFixAsync(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false, bool formatBeforeCompare = false, CodeFixProvider codeFixProvider = null)
        {
            if (formatBeforeCompare)
            {
                oldSource = await FormatSourceAsync(LanguageNames.VisualBasic, oldSource).ConfigureAwait(true);
                newSource = await FormatSourceAsync(LanguageNames.VisualBasic, newSource).ConfigureAwait(true);
            }
            codeFixProvider = codeFixProvider ?? GetCodeFixProvider();
            var diagnosticAnalyzer = GetDiagnosticAnalyzer();
            if (diagnosticAnalyzer != null)
                await VerifyFixAsync(LanguageNames.VisualBasic, diagnosticAnalyzer, codeFixProvider, oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics).ConfigureAwait(true);
            else
                await VerifyFixAsync(LanguageNames.VisualBasic, codeFixProvider.FixableDiagnosticIds, codeFixProvider, oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics).ConfigureAwait(true);
        }

        /// <summary>
        /// General verifier for codefixes.
        /// Creates a Document from the source string, then gets diagnostics on it and applies the relevant codefixes.
        /// Then gets the string after the codefix is applied and compares it with the expected result.
        /// Note: If any codefix causes new diagnostics to show up, the test fails unless allowNewCompilerDiagnostics is set to true.
        /// </summary>
        /// <param name="language">The language the source code is in</param>
        /// <param name="analyzer">The analyzer to be applied to the source code</param>
        /// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
        /// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        private async Task VerifyFixAsync(string language, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string oldSource, string newSource, int? codeFixIndex, bool allowNewCompilerDiagnostics)
        {
            var supportedDiagnostics = analyzer.SupportedDiagnostics.Select(d => d.Id);
            var codeFixFixableDiagnostics = codeFixProvider.FixableDiagnosticIds;
            Assert.True(codeFixFixableDiagnostics.Any(d => supportedDiagnostics.Contains(d)), "Code fix provider does not fix the diagnostic provided by the analyzer.");
            var document = CreateDocument(oldSource, language);
            var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzer, new[] { document }).ConfigureAwait(true);
            var compilerDiagnostics = await GetCompilerDiagnosticsAsync(document).ConfigureAwait(true);
            var attempts = analyzerDiagnostics.Length;

            for (int i = 0; i < attempts; ++i)
            {
                var actions = new List<CodeAction>();
                var context = new CodeFixContext(document, analyzerDiagnostics[0], (a, d) => actions.Add(a), CancellationToken.None);
                await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(true);

                if (!actions.Any()) break;

                if (codeFixIndex != null)
                {
                    document = await ApplyFixAsync(document, actions.ElementAt((int)codeFixIndex)).ConfigureAwait(true);
                    break;
                }

                document = await ApplyFixAsync(document, actions.ElementAt(0)).ConfigureAwait(true);
                analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzer, new[] { document }).ConfigureAwait(true);

                var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(document).ConfigureAwait(true));


                //check if applying the code fix introduced any new compiler diagnostics
                if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
                {
                    // Format and get the compiler diagnostics again so that the locations make sense in the output
                    document = document.WithSyntaxRoot(Formatter.Format(await document.GetSyntaxRootAsync().ConfigureAwait(true), Formatter.Annotation, document.Project.Solution.Workspace));
                    newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(document).ConfigureAwait(true));

                    Assert.True(false, $"Fix introduced new compiler diagnostics:\r\n{string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString()))}\r\n\r\nNew document:\r\n{(await document.GetSyntaxRootAsync().ConfigureAwait(true)).ToFullString()}\r\n");
                }

                //check if there are analyzer diagnostics left after the code fix
                if (!analyzerDiagnostics.Any()) break;
            }

            //after applying all of the code fixes, compare the resulting string to the inputted one
            var actual = await GetStringFromDocumentAsync(document).ConfigureAwait(true);
            Assert.Equal(newSource, actual);
        }

        private async Task VerifyFixAsync(string language, ImmutableArray<string> diagnosticIds, CodeFixProvider codeFixProvider, string oldSource, string newSource, int? codeFixIndex, bool allowNewCompilerDiagnostics)
        {
            var document = CreateDocument(oldSource, language);
            var compilerDiagnostics = (await GetCompilerDiagnosticsAsync(document).ConfigureAwait(true)).ToList();
            var analyzerDiagnostics = compilerDiagnostics.Where(c => diagnosticIds.Contains(c.Id)).ToList();
            var attempts = analyzerDiagnostics.Count();

            for (int i = 0; i < attempts; ++i)
            {
                var actions = new List<CodeAction>();
                var context = new CodeFixContext(document, analyzerDiagnostics[0], (a, d) => actions.Add(a), CancellationToken.None);
                await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(true);

                if (!actions.Any()) break;

                if (codeFixIndex != null)
                {
                    document = await ApplyFixAsync(document, actions.ElementAt((int)codeFixIndex)).ConfigureAwait(true);
                    break;
                }

                document = await ApplyFixAsync(document, actions.ElementAt(0)).ConfigureAwait(true);

                var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(document).ConfigureAwait(true));
                compilerDiagnostics = (await GetCompilerDiagnosticsAsync(document).ConfigureAwait(true)).ToList();
                analyzerDiagnostics = compilerDiagnostics.Where(c => diagnosticIds.Contains(c.Id)).ToList();

                //check if applying the code fix introduced any new compiler diagnostics
                if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
                {
                    // Format and get the compiler diagnostics again so that the locations make sense in the output
                    document = document.WithSyntaxRoot(Formatter.Format(await document.GetSyntaxRootAsync().ConfigureAwait(true), Formatter.Annotation, document.Project.Solution.Workspace));
                    newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(document).ConfigureAwait(true));

                    Assert.True(false, $"Fix introduced new compiler diagnostics:\r\n{string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString()))}\r\n\r\nNew document:\r\n{(await document.GetSyntaxRootAsync().ConfigureAwait(true)).ToFullString()}\r\n");
                }

                //check if there are analyzer diagnostics left after the code fix
                if (!analyzerDiagnostics.Any()) break;
            }

            //after applying all of the code fixes, compare the resulting string to the inputted one
            var actual = await GetStringFromDocumentAsync(document).ConfigureAwait(true);
            Assert.Equal(newSource, actual);
        }

        protected async Task VerifyCSharpFixAllAsync(string[] oldSources, string[] newSources, bool allowNewCompilerDiagnostics = false, bool formatBeforeCompare = true, CodeFixProvider codeFixProvider = null)
        {
            if (formatBeforeCompare)
            {
                oldSources = await Task.WhenAll(oldSources.Select(s => FormatSourceAsync(LanguageNames.CSharp, s))).ConfigureAwait(true);
                newSources = await Task.WhenAll(newSources.Select(s => FormatSourceAsync(LanguageNames.CSharp, s))).ConfigureAwait(true);
            }
            codeFixProvider = codeFixProvider ?? GetCodeFixProvider();
            await VerifyFixAllAsync(LanguageNames.CSharp, GetDiagnosticAnalyzer(), codeFixProvider, oldSources, newSources, allowNewCompilerDiagnostics).ConfigureAwait(true);
        }

        protected async Task VerifyCSharpFixAllAsync(string oldSource, string newSource, bool allowNewCompilerDiagnostics = false, bool formatBeforeCompare = true, CodeFixProvider codeFixProvider = null)
        {
            if (formatBeforeCompare)
            {
                oldSource = await FormatSourceAsync(LanguageNames.CSharp, oldSource).ConfigureAwait(true);
                newSource = await FormatSourceAsync(LanguageNames.CSharp, newSource).ConfigureAwait(true);
            }
            codeFixProvider = codeFixProvider ?? GetCodeFixProvider();
            await VerifyFixAllAsync(LanguageNames.CSharp, GetDiagnosticAnalyzer(), codeFixProvider, oldSource, newSource, allowNewCompilerDiagnostics).ConfigureAwait(true);
        }

        protected async Task VerifyBasicFixAllAsync(string[] oldSources, string[] newSources, bool allowNewCompilerDiagnostics = false, bool formatBeforeCompare = true, CodeFixProvider codeFixProvider = null)
        {
            if (formatBeforeCompare)
            {
                oldSources = await Task.WhenAll(oldSources.Select(s => FormatSourceAsync(LanguageNames.VisualBasic, s))).ConfigureAwait(true);
                newSources = await Task.WhenAll(newSources.Select(s => FormatSourceAsync(LanguageNames.VisualBasic, s))).ConfigureAwait(true);
            }
            codeFixProvider = codeFixProvider ?? GetCodeFixProvider();
            await VerifyFixAllAsync(LanguageNames.VisualBasic, GetDiagnosticAnalyzer(), codeFixProvider, oldSources, newSources, allowNewCompilerDiagnostics).ConfigureAwait(true);
        }

        protected async Task VerifyBasicFixAllAsync(string oldSource, string newSource, bool allowNewCompilerDiagnostics = false, bool formatBeforeCompare = true, CodeFixProvider codeFixProvider = null)
        {
            if (formatBeforeCompare)
            {
                oldSource = await FormatSourceAsync(LanguageNames.VisualBasic, oldSource).ConfigureAwait(true);
                newSource = await FormatSourceAsync(LanguageNames.VisualBasic, newSource).ConfigureAwait(true);
            }
            codeFixProvider = codeFixProvider ?? GetCodeFixProvider();
            await VerifyFixAllAsync(LanguageNames.VisualBasic, GetDiagnosticAnalyzer(), codeFixProvider, oldSource, newSource, allowNewCompilerDiagnostics).ConfigureAwait(true);
        }

        private async Task VerifyFixAllAsync(string language, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string oldSource, string newSource, bool allowNewCompilerDiagnostics)
        {
            var document = CreateDocument(oldSource, language);
            var compilerDiagnostics = await GetCompilerDiagnosticsAsync(document).ConfigureAwait(true);
            var getDocumentDiagnosticsAsync = analyzer != null
                ? (Func<Document, Task<IEnumerable<Diagnostic>>>)(async doc =>
                     await GetSortedDiagnosticsFromDocumentsAsync(analyzer, new[] { doc }).ConfigureAwait(true))
                : (Func<Document, Task<IEnumerable<Diagnostic>>>)(async doc =>
                {
                    var compilerDiags = await GetCompilerDiagnosticsAsync(doc).ConfigureAwait(true);
                    return compilerDiags.Where(d => codeFixProvider.FixableDiagnosticIds.Contains(d.Id));
                });
            Func<Project, bool, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync = async (proj, b) =>
            {
                var theDocs = proj.Documents;
                var diags = await Task.WhenAll(theDocs.Select(d => getDocumentDiagnosticsAsync?.Invoke(d))).ConfigureAwait(true);
                return diags.SelectMany(d => d);
            };
            var fixAllProvider = codeFixProvider.GetFixAllProvider();
            var fixAllContext = NewFixAllContext(document, document.Project, codeFixProvider, FixAllScope.Document,
                null,//code action ids in codecracker are always null
                codeFixProvider.FixableDiagnosticIds,
                (doc, diagIds, cancelationToken) => getDocumentDiagnosticsAsync?.Invoke(doc),
                (theProject, b, diagIds, cancelationToken) => getProjectDiagnosticsAsync(theProject, b),
                CancellationToken.None);
            var action = await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(true);
            if (action == null) throw new Exception("No action supplied for the code fix.");
            document = await ApplyFixAsync(document, action).ConfigureAwait(true);
            //check if applying the code fix introduced any new compiler diagnostics
            var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(document).ConfigureAwait(true));
            if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
            {
                // Format and get the compiler diagnostics again so that the locations make sense in the output
                document = document.WithSyntaxRoot(Formatter.Format(await document.GetSyntaxRootAsync().ConfigureAwait(true), Formatter.Annotation, document.Project.Solution.Workspace));
                newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(document).ConfigureAwait(true));
                Assert.True(false, $"Fix introduced new compiler diagnostics:\r\n{string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString()))}\r\n\r\nNew document:\r\n{(await document.GetSyntaxRootAsync().ConfigureAwait(true)).ToFullString()}\r\n");
            }
            var actual = await GetStringFromDocumentAsync(document).ConfigureAwait(true);
            Assert.Equal(newSource, actual);
        }

        private async Task VerifyFixAllAsync(string language, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string[] oldSources, string[] newSources, bool allowNewCompilerDiagnostics)
        {
            var project = CreateProject(oldSources, language);
            var compilerDiagnostics = (await Task.WhenAll(project.Documents.Select(d => GetCompilerDiagnosticsAsync(d))).ConfigureAwait(true)).SelectMany(d => d);
            var fixAllProvider = codeFixProvider.GetFixAllProvider();
            FixAllContext fixAllContext;
            if (analyzer != null)
            {
                var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzer, project.Documents.ToArray()).ConfigureAwait(true);
                fixAllContext = NewFixAllContext(null, project, codeFixProvider, FixAllScope.Solution,
                    null,//code action ids in codecracker are always null
                    codeFixProvider.FixableDiagnosticIds,
                    (doc, diagnosticIds, cancelationToken) => Task.FromResult(analyzerDiagnostics.Where(d => d.Location.SourceTree.FilePath == doc.Name)),
                    (theProject, b, diagnosticIds, cancelationToken) => Task.FromResult((IEnumerable<Diagnostic>)analyzerDiagnostics), //todo: verify, probably wrong
                    CancellationToken.None);
            }
            else
            {
                Func<Document, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync = async doc =>
                {
                    var compilerDiags = await GetCompilerDiagnosticsAsync(doc).ConfigureAwait(true);
                    return compilerDiags.Where(d => codeFixProvider.FixableDiagnosticIds.Contains(d.Id));
                };
                Func<Project, bool, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync = async (proj, b) =>
                {
                    var theDocs = proj.Documents;
                    var diags = await Task.WhenAll(theDocs.Select(d => getDocumentDiagnosticsAsync(d))).ConfigureAwait(true);
                    return diags.SelectMany(d => d);
                };
                fixAllContext = NewFixAllContext(null, project, codeFixProvider, FixAllScope.Solution,
                    null,//code action ids in codecracker are always null
                    codeFixProvider.FixableDiagnosticIds,
                    (doc, diagIds, cancelationToken) => getDocumentDiagnosticsAsync(doc),
                    (theProject, b, diagIds, cancelationToken) => getProjectDiagnosticsAsync(theProject, b),
                    CancellationToken.None);
            }
            var action = await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(true);
            if (action == null) throw new Exception("No action supplied for the code fix.");
            project = await ApplyFixAsync(project, action).ConfigureAwait(true);
            //check if applying the code fix introduced any new compiler diagnostics
            var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, (await Task.WhenAll(project.Documents.Select(d => GetCompilerDiagnosticsAsync(d))).ConfigureAwait(true)).SelectMany(d => d));
            if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
                Assert.True(false, $"Fix introduced new compiler diagnostics:\r\n{string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString()))}\r\n");
            var docs = project.Documents.ToArray();
            for (int i = 0; i < docs.Length; i++)
            {
                var document = docs[i];
                var actual = await GetStringFromDocumentAsync(document).ConfigureAwait(true);
                Assert.Equal(newSources[i], actual);
            }
        }

        //todo: remove when FixAllContext get a public ctor, what should be soon
        public FixAllContext NewFixAllContext(Document document, Project project, CodeFixProvider codeFixProvider, FixAllScope scope, string codeActionEquivalenceKey, IEnumerable<string> diagnosticIds, Func<Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync, Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync, CancellationToken cancellationToken)
        {
            var parameters = new object[] { document, project, codeFixProvider, scope, codeActionEquivalenceKey, diagnosticIds, getDocumentDiagnosticsAsync, getProjectDiagnosticsAsync, cancellationToken };
            var fixallContext = (FixAllContext)Activator.CreateInstance(typeof(FixAllContext), BindingFlags.NonPublic | BindingFlags.Instance, null, parameters, null);
            return fixallContext;
        }

        /// <summary>
        /// Called to test a C# codefix when it should not had been registered
        /// </summary>
        /// <param name="source">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
        protected async Task VerifyCSharpHasNoFixAsync(string source, CodeFixProvider codeFixProvider = null) =>
            await VerifyHasNoFixAsync(LanguageNames.CSharp, GetDiagnosticAnalyzer(), codeFixProvider ?? GetCodeFixProvider(), source).ConfigureAwait(true);

        /// <summary>
        /// General verifier for a diagnostics that should not have fix registred.
        /// Creates a Document from the source string, then gets diagnostics on it and verify if it has no fix registred.
        /// It will fail the test if it has any fix registred to it
        /// </summary>
        /// <param name="language">The language the source code is in</param>
        /// <param name="analyzer">The analyzer to be applied to the source code</param>
        /// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
        /// <param name="source">A class in the form of a string before the CodeFix was applied to it</param>
        private async Task VerifyHasNoFixAsync(string language, DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string source)
        {
            var document = CreateDocument(source, language);
            var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzer, new[] { document }).ConfigureAwait(true);

            foreach (var analyzerDiagnostic in analyzerDiagnostics)
            {
                var actions = new List<CodeAction>();
                var context = new CodeFixContext(document, analyzerDiagnostic, (a, d) => actions.Add(a), CancellationToken.None);
                await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(true);
                Assert.False(actions.Any(), $"Should not have a code fix registered for diagnostic '{analyzerDiagnostic.Id}'");
            }
        }
    }

    public abstract class CodeFixVerifier<T, U> : CodeFixVerifier
        where T : DiagnosticAnalyzer, new()
        where U : CodeFixProvider, new()
    {
        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() => new T();

        protected override CodeFixProvider GetCodeFixProvider() => new U();
    }
}