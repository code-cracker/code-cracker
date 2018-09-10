using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test
{
    /// <summary>
    /// Generic superclass of all Unit Tests for DiagnosticAnalyzers
    /// </summary>
    public abstract class DiagnosticVerifier<T> : DiagnosticVerifier where T : DiagnosticAnalyzer, new()
    {
        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
        {
            return new T();
        }
    }

    /// <summary>
    /// Superclass of all Unit Tests for DiagnosticAnalyzers
    /// </summary>
    public abstract partial class DiagnosticVerifier
    {
        /// <summary>
        /// Get the analyzer being tested - to be implemented in non-abstract class
        /// </summary>
        protected virtual DiagnosticAnalyzer GetDiagnosticAnalyzer() => null;

        protected async Task VerifyBasicHasNoDiagnosticsAsync(string source, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersionVB = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14) =>
            await VerifyBasicDiagnosticAsync(source, new DiagnosticResult[] { }, languageVersionVB).ConfigureAwait(true);

        protected async Task VerifyBasicHasNoDiagnosticsAsync(string source1, string source2, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersionVB = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14) =>
            await VerifyBasicDiagnosticAsync(new[] { source1, source2 }, new DiagnosticResult[] { }, languageVersionVB).ConfigureAwait(true);

        protected async Task VerifyBasicHasNoDiagnosticsAsync(string[] sources, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersionVB = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14) =>
            await VerifyBasicDiagnosticAsync(sources, new DiagnosticResult[] { }, languageVersionVB).ConfigureAwait(true);

        protected async Task VerifyCSharpHasNoDiagnosticsAsync(string source, LanguageVersion languageVersion = LanguageVersion.CSharp6) =>
            await VerifyCSharpDiagnosticAsync(source, new DiagnosticResult[] { }, languageVersion).ConfigureAwait(true);

        protected async Task VerifyCSharpHasNoDiagnosticsAsync(string source1, string source2, LanguageVersion languageVersion = LanguageVersion.CSharp6) =>
            await VerifyCSharpDiagnosticAsync(new[] { source1, source2 }, new DiagnosticResult[] { }, languageVersion).ConfigureAwait(true);

        protected async Task VerifyCSharpHasNoDiagnosticsAsync(string[] sources, LanguageVersion languageVersion = LanguageVersion.CSharp6) =>
            await VerifyCSharpDiagnosticAsync(sources, new DiagnosticResult[] { }, languageVersion).ConfigureAwait(true);

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        /// <param name="expected"> DiagnosticResults that should appear after the analyzer is run on the source</param>
        /// <param name="languageVersion">The C# language version, defaults to the latest stable version.</param>
        protected async Task VerifyCSharpDiagnosticAsync(string source, DiagnosticResult[] expected, LanguageVersion languageVersion = LanguageVersion.CSharp6) =>
            await VerifyDiagnosticsAsync(new[] { source }, LanguageNames.CSharp, GetDiagnosticAnalyzer(), expected, languageVersion, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14).ConfigureAwait(true);

        protected async Task VerifyCSharpDiagnosticAsync(string source, DiagnosticResult expected, LanguageVersion languageVersion = LanguageVersion.CSharp6) =>
            await VerifyDiagnosticsAsync(new[] { source }, LanguageNames.CSharp, GetDiagnosticAnalyzer(), new[] { expected }, languageVersion, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14).ConfigureAwait(true);

        protected async Task VerifyCSharpDiagnosticAsync(string source, DiagnosticResult expected1, DiagnosticResult expected2, LanguageVersion languageVersion = LanguageVersion.CSharp6) =>
            await VerifyDiagnosticsAsync(new[] { source }, LanguageNames.CSharp, GetDiagnosticAnalyzer(), new[] { expected1, expected2 }, languageVersion, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14).ConfigureAwait(true);

        /// <summary>
        /// Called to test a VB DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the source</param>
        /// <param name="languageVersion">The VB language version, defaults to the latest stable version.</param>
        protected async Task VerifyBasicDiagnosticAsync(string source, DiagnosticResult[] expected, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersion = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14) =>
            await VerifyDiagnosticsAsync(new[] { source }, LanguageNames.VisualBasic, GetDiagnosticAnalyzer(), expected, LanguageVersion.CSharp6, languageVersion).ConfigureAwait(true);

        protected async Task VerifyBasicDiagnosticAsync(string source, DiagnosticResult expected, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersionVB = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14) =>
            await VerifyDiagnosticsAsync(new[] { source }, LanguageNames.VisualBasic, GetDiagnosticAnalyzer(), new[] { expected }, LanguageVersion.CSharp6, languageVersionVB).ConfigureAwait(true);

        protected async Task VerifyBasicDiagnosticAsync(string source, DiagnosticResult expected1, DiagnosticResult expected2, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersionVB = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14) =>
            await VerifyDiagnosticsAsync(new[] { source }, LanguageNames.VisualBasic, GetDiagnosticAnalyzer(), new[] { expected1, expected2 }, LanguageVersion.CSharp6, languageVersionVB).ConfigureAwait(true);

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the inputted strings as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
        /// <param name="languageVersion">The C# language version, defaults to the latest stable version.</param>
        protected async Task VerifyCSharpDiagnosticAsync(string[] sources, DiagnosticResult[] expected, LanguageVersion languageVersion = LanguageVersion.CSharp6) =>
            await VerifyDiagnosticsAsync(sources, LanguageNames.CSharp, GetDiagnosticAnalyzer(), expected, languageVersion, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14).ConfigureAwait(true);

        /// <summary>
        /// Called to test a VB DiagnosticAnalyzer when applied on the inputted strings as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
        /// <param name="languageVersion">The VB language version, defaults to the latest stable version.</param>
        protected async Task VerifyBasicDiagnosticAsync(string[] sources, DiagnosticResult[] expected, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersion = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14) =>
            await VerifyDiagnosticsAsync(sources, LanguageNames.VisualBasic, GetDiagnosticAnalyzer(), expected, LanguageVersion.CSharp6, languageVersion).ConfigureAwait(true);

        /// <summary>
        /// General method that gets a collection of actual diagnostics found in the source after the analyzer is run,
        /// then verifies each of them.
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run teh analyzers on</param>
        /// <param name="language">The language of the classes represented by the source strings</param>
        /// <param name="analyzer">The analyzer to be run on the source code</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
        /// <param name="languageVersionCSharp">The C# language version, default to latest.</param>
        /// <param name="languageVersionVB">The VB language version, default to latest.</param>
        private async static Task VerifyDiagnosticsAsync(string[] sources, string language, DiagnosticAnalyzer analyzer, DiagnosticResult[] expected, LanguageVersion languageVersionCSharp, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersionVB)
        {
            var diagnostics = await GetSortedDiagnosticsAsync(sources, language, analyzer, languageVersionCSharp, languageVersionVB).ConfigureAwait(true);
            var defaultFilePath = language == LanguageNames.CSharp ? CSharpDefaultFilePath : VisualBasicDefaultFilePath;
            VerifyDiagnosticResults(diagnostics, analyzer, defaultFilePath, expected);
        }


        /// <summary>
        /// Checks each of the actual Diagnostics found and compares them with the corresponding DiagnosticResult in the array of expected results.
        /// Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, and Message of the DiagnosticResult match the actual diagnostic.
        /// </summary>
        /// <param name="actualResults">The Diagnostics found by the compiler after running the analyzer on the source code</param>
        /// <param name="analyzer">The analyzer that was being run on the sources</param>
        /// <param name="expectedResults">Diagnsotic Results that should have appeared in the code</param>
        private static void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, string defaultFilePath, params DiagnosticResult[] expectedResults)
        {
            var expectedCount = expectedResults.Length;
            var actualCount = actualResults.Count();

            if (expectedCount != actualCount)
            {
                var diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzer, actualResults.ToArray()) : "    NONE.";

                Assert.True(false, $"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n");
            }

            for (int i = 0; i < expectedResults.Length; i++)
            {
                var actual = actualResults.ElementAt(i);
                var expected = expectedResults[i].WithDefaultPath(defaultFilePath);

                if (!expected.HasLocation)
                {
                    if (actual.Location != Location.None)
                    {
                        Assert.True(false, $"Expected:\nA project diagnostic with No location\nActual:\n{FormatDiagnostics(analyzer, actual)}");
                    }
                }
                else
                {
                    VerifyDiagnosticLocation(analyzer, actual, actual.Location, expected.Spans.First());
                    var additionalLocations = actual.AdditionalLocations.ToArray();

                    if (additionalLocations.Length != expected.Spans.Length - 1)
                    {
                        Assert.True(false, $"Expected {expected.Spans.Length - 1} additional locations but got {additionalLocations.Length} for Diagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");
                    }

                    for (int j = 0; j < additionalLocations.Length; ++j)
                    {
                        VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expected.Spans[j + 1]);
                    }
                }

                if (actual.Id != expected.Id)
                    Assert.True(false, $"Expected diagnostic id to be \"{expected.Id}\" was \"{actual.Id}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");

                if (actual.Severity != expected.Severity)
                    Assert.True(false, $"Expected diagnostic severity to be \"{expected.Severity}\" was \"{actual.Severity}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");

                if (actual.GetMessage() != expected.Message)
                    Assert.True(false, $"Expected diagnostic message to be \"{expected.Message}\" was \"{actual.GetMessage()}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");
            }
        }

        /// <summary>
        /// Helper method to VerifyDiagnosticResult that checks the location of a diagnostic and compares it with the location in the expected DiagnosticResult.
        /// </summary>
        /// <param name="analyzer">The analyzer that was being run on the sources</param>
        /// <param name="diagnostic">The diagnostic that was found in the code</param>
        /// <param name="actual">The Location of the Diagnostic found in the code</param>
        /// <param name="expected">The DiagnosticResultLocation that should have been found</param>
        private static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, FileLinePositionSpan expected)
        {
            var actualSpan = actual.GetLineSpan();

            Assert.True(actualSpan.Path == expected.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
                $"Expected diagnostic to be in file \"{expected.Path}\" was actually in file \"{actualSpan.Path}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");

            var actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0)
                if (actualLinePosition.Line != expected.StartLinePosition.Line)
                    Assert.True(false, $"Expected diagnostic to be on line \"{expected.StartLinePosition.Line + 1}\" was actually on line \"{actualLinePosition.Line + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualLinePosition.Character > 0)
                if (actualLinePosition.Character != expected.StartLinePosition.Character)
                    Assert.True(false, $"Expected diagnostic to start at column \"{expected.StartLinePosition.Character + 1}\" was actually at column \"{actualLinePosition.Character + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");
        }

        /// <summary>
        /// Helper method to format a Diagnostic into an easily reasible string
        /// </summary>
        /// <param name="analyzer">The analyzer that this Verifer tests</param>
        /// <param name="diagnostics">The Diagnostics to be formatted</param>
        /// <returns>The Diagnostics formatted as a string</returns>
        private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < diagnostics.Length; ++i)
            {
                builder.AppendLine("// " + diagnostics[i].ToString());

                var analyzerType = analyzer.GetType();
                var rules = analyzer.SupportedDiagnostics;

                foreach (var rule in rules)
                {
                    if (rule != null && rule.Id == diagnostics[i].Id)
                    {
                        var location = diagnostics[i].Location;
                        if (location == Location.None)
                        {
                            builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, rule.Id);
                        }
                        else
                        {
                            Assert.True(location.IsInSource, $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata:\r\n{diagnostics[i]}");

                            var resultMethodName = diagnostics[i].Location.SourceTree.FilePath.EndsWith(".cs") ? "GetCSharpResultAt" : "GetBasicResultAt";
                            var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

                            builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
                                resultMethodName,
                                linePosition.Line + 1,
                                linePosition.Character + 1,
                                analyzerType.Name,
                                rule.Id);
                        }

                        if (i != diagnostics.Length - 1) builder.Append(',');

                        builder.AppendLine();
                        break;
                    }
                }
            }
            return builder.ToString();
        }
    }
}