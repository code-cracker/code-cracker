using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace TestHelper
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
        protected virtual CodeFixProvider GetCSharpCodeFixProvider()
        {
            return null;
        }

        /// <summary>
        /// Returns the codefix being tested (VB) - to be implemented in non-abstract class
        /// </summary>
        /// <returns>The CodeFixProvider to be used for VisualBasic code</returns>
        protected virtual CodeFixProvider GetBasicCodeFixProvider()
        {
            return null;
        }

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
                oldSource = await TestHelpers.FormatSourceAsync(LanguageNames.CSharp, oldSource);
                newSource = await TestHelpers.FormatSourceAsync(LanguageNames.CSharp, newSource);
            }
            await VerifyFixAsync(LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), codeFixProvider ?? GetCSharpCodeFixProvider(), oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics);
        }

        /// <summary>
        /// Called to test a VB codefix when applied on the inputted string as a source
        /// </summary>
        /// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        protected async Task VerifyBasicFixAsync(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false)
        {
            await VerifyFixAsync(LanguageNames.VisualBasic, GetBasicDiagnosticAnalyzer(), GetBasicCodeFixProvider(), oldSource, newSource, codeFixIndex, allowNewCompilerDiagnostics);
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
            var document = TestHelpers.CreateDocument(oldSource, language);
            var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzer, new[] { document });
            var compilerDiagnostics = await GetCompilerDiagnosticsAsync(document);
            var attempts = analyzerDiagnostics.Length;

            for (int i = 0; i < attempts; ++i)
            {
                var actions = new List<CodeAction>();
                var context = new CodeFixContext(document, analyzerDiagnostics[0], (a, d) => actions.Add(a), CancellationToken.None);
                await codeFixProvider.ComputeFixesAsync(context);

                if (!actions.Any())
                {
                    break;
                }

                if (codeFixIndex != null)
                {
                    document = await ApplyFixAsync(document, actions.ElementAt((int)codeFixIndex));
                    break;
                }

                document = await ApplyFixAsync(document, actions.ElementAt(0));
                analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzer, new[] { document });

                var newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(document));

                //check if applying the code fix introduced any new compiler diagnostics
                if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
                {
                    // Format and get the compiler diagnostics again so that the locations make sense in the output
                    document = document.WithSyntaxRoot(Formatter.Format(await document.GetSyntaxRootAsync(), Formatter.Annotation, document.Project.Solution.Workspace));
                    newCompilerDiagnostics = GetNewDiagnostics(compilerDiagnostics, await GetCompilerDiagnosticsAsync(document));

                    Assert.True(false, $"Fix introduced new compiler diagnostics:\r\n{string.Join("\r\n", newCompilerDiagnostics.Select(d => d.ToString()))}\r\n\r\nNew document:\r\n{(await document.GetSyntaxRootAsync()).ToFullString()}\r\n");
                }

                //check if there are analyzer diagnostics left after the code fix
                if (!analyzerDiagnostics.Any())
                {
                    break;
                }
            }

            //after applying all of the code fixes, compare the resulting string to the inputted one
            var actual = await TestHelpers.GetStringFromDocumentAsync(document);
            Assert.Equal(newSource, actual);
        }

        /// <summary>
        /// Called to test a C# codefix when it should not had been registered
        /// </summary>
        /// <param name="source">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
        protected async Task VerifyCSharpHasNoFixAsync(string source, CodeFixProvider codeFixProvider = null)
        {   
            await VerifyHasNoFixAsync(LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer(), codeFixProvider ?? GetCSharpCodeFixProvider(), source);
        }

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
            var document = TestHelpers.CreateDocument(source, language);
            var analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzer, new[] { document });

            foreach (var analyzerDiagnostic in analyzerDiagnostics)
            {
                var actions = new List<CodeAction>();
                var context = new CodeFixContext(document, analyzerDiagnostic, (a, d) => actions.Add(a), CancellationToken.None);
                await codeFixProvider.ComputeFixesAsync(context);

                Assert.False(actions.Any(),
                    string.Format(
                        "Should not have a code fix registered for diagnostic '{0}'",
                        analyzerDiagnostic.Id));
            }
        }

    }
}