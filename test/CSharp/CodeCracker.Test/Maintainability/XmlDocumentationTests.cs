using CodeCracker.CSharp.Maintainability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Maintainability
{
    public class XmlDocumentationAnalyzerTests : CodeFixVerifier<XmlDocumentationAnalyzer, XmlDocumentationMissingInCSharpCodeFixProvider>
    {
        [Fact]
        public async Task IgnoresClassDocs()
        {
            const string source = @"
namespace ConsoleApplication1
{
    /// <summary>
    ///
    /// </summary>
    class TypeName
    {
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task XmlDocumentationInsideMethodWithAttributeDoesNotCreateDiagnostic()
        {
            var source = @"
/// <summary>
/// </summary>
/// <param name=""value"" ></param>
/// <returns></returns>
[System.Runtime.CompilerServices.MethodImpl]
public int Foo(int value)
{
    ///
    var a = 1;
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task XmlDocumentationWithInheritDocNotCreateDiagnostic()
        {
            var source = @"
interface IBar
{
    /// <summary>
    /// Lololol
    /// </summary>
    /// <param name=""a""></param>
    /// <param name=""b""></param>
    void foo(int a, int b);
}

class Bar : IBar
{
    /// <inheritdoc/>
    public void foo(int a, int b) { }
}

class Bar2 : IBar
{
    /// <inheritdoc/>
    /// <remarks>
    /// You can still specify all the normal XML tags here, and they will
    /// overwrite inherited ones accordingly.
    /// </remarks>
    public void foo(int a, int b) { }
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task XmlDocumentationInsideMethodDoesNotCreateDiagnostic()
        {
            var source = @"
/// <summary>
/// </summary>
/// <param name=""value"" ></param>
/// <returns></returns>
public int Foo(int value)
{
    ///
    var a = 1;
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task XmlDocumentationWithNonexistentParameterOfMethodAnalyzerCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name=""analyzer"">The analyzer to run on the documents</param>
            /// <param name=""documents"">The Documents that the analyzer will be run on</param>
            /// <param name=""spans"">Optional TextSpan indicating where a Diagnostic will be found</param>
            /// <returns>An IEnumerable of Diagnostics that surfaced in teh source code, sorted by Location</returns>
            [System.Runtime.CompilerServices.MethodImpl]
            protected async static Task<Diagnostic[]> GetSortedDiagnosticsFromDocumentsAsync(DiagnosticAnalyzer analyzer, Document[] documents)
            {
            }
        }
    }";

            var expected = new DiagnosticResult(DiagnosticId.XmlDocumentation_MissingInCSharp.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 16)
                .WithMessage("You have missing/unexistent parameters in Xml Docs");

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task XmlDocumentationWithMissingParametersFromMethodAnalyzerCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name=""sources"">Classes in the form of strings</param>
            /// <param name=""language"">The language the source code is in</param>
            /// <returns>A Project created out of the Douments created from the source strings</returns>
            public static Project CreateProject(string[] sources, out AdhocWorkspace workspace, string language = LanguageNames.CSharp)
            {
            }
        }
    }";

            var expected = new DiagnosticResult(DiagnosticId.XmlDocumentation_MissingInXml.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(6, 16)
                .WithMessage("You have missing/unexistent parameters in Xml Docs");

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task XmlDocumentationWithMatchingParametersFromMethodAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name=""value"" ></param>
            /// <returns></returns>
            public int Foo(int value)
            {
            }
    }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task MethodWithoutXmlDocumentationAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo(int value)
            {
            }
    }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
    }

    public class XmlDocumentationRemoveUnexistentParametersCodeFixTests : CodeFixVerifier<XmlDocumentationAnalyzer, XmlDocumentationMissingInCSharpCodeFixProvider>
    {
        [Fact]
        public async Task FixRemoveParameterDoc()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name=""analyzer"">The analyzer to run on the documents</param>
            /// <param name=""documents"">The Documents that the analyzer will be run on</param>
            /// <param name=""spans"">Optional TextSpan indicating where a Diagnostic will be found</param>
            /// <returns>An IEnumerable of Diagnostics that surfaced in teh source code, sorted by Location</returns>
            protected async static Task<Diagnostic[]> GetSortedDiagnosticsFromDocumentsAsync(DiagnosticAnalyzer analyzer, Document[] documents)
            {
            }
        }
    }";

            const string expected = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name=""analyzer"">The analyzer to run on the documents</param>
            /// <param name=""documents"">The Documents that the analyzer will be run on</param>
            /// <returns>An IEnumerable of Diagnostics that surfaced in teh source code, sorted by Location</returns>
            protected async static Task<Diagnostic[]> GetSortedDiagnosticsFromDocumentsAsync(DiagnosticAnalyzer analyzer, Document[] documents)
            {
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task FixRemoveManyParameterDoc()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name=""analyzer"">The analyzer to run on the documents</param>
            /// <param name=""anotherToBeRemoved"">Optional TextSpan indicating where a Diagnostic will be found</param>
            /// <param name=""documents"">The Documents that the analyzer will be run on</param>
            /// <param name=""spansToBeRemoved"">Optional TextSpan indicating where a Diagnostic will be found</param>
            /// <returns>An IEnumerable of Diagnostics that surfaced in teh source code, sorted by Location</returns>
            protected async static Task<Diagnostic[]> GetSortedDiagnosticsFromDocumentsAsync(DiagnosticAnalyzer analyzer, Document[] documents)
            {
            }
        }
    }";

            const string expected = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name=""analyzer"">The analyzer to run on the documents</param>
            /// <param name=""documents"">The Documents that the analyzer will be run on</param>
            /// <returns>An IEnumerable of Diagnostics that surfaced in teh source code, sorted by Location</returns>
            protected async static Task<Diagnostic[]> GetSortedDiagnosticsFromDocumentsAsync(DiagnosticAnalyzer analyzer, Document[] documents)
            {
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected);
        }


    }

    public class XmlDocumentationCreateMissingParametersCodeFixTests : CodeFixVerifier<XmlDocumentationAnalyzer, XmlDocumentationMissingInXmlCodeFixProvider>
    {
        [Fact]
        public async Task FixCreateOneParameterDoc()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name=""sources"">Classes in the form of strings</param>
            /// <param name=""language"">The language the source code is in</param>
            /// <returns>A Project created out of the Douments created from the source strings</returns>
            public static Project CreateProject(string[] sources, out AdhocWorkspace workspace, string language = LanguageNames.CSharp)
            {
            }
        }
    }";

            const string expected = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name=""sources"">Classes in the form of strings</param>
            /// <param name=""language"">The language the source code is in</param>
            /// <param name=""workspace"">todo: describe workspace parameter on CreateProject</param>
            /// <returns>A Project created out of the Douments created from the source strings</returns>
            public static Project CreateProject(string[] sources, out AdhocWorkspace workspace, string language = LanguageNames.CSharp)
            {
            }
        }
    }";

            await VerifyCSharpFixAsync(source, expected);
        }


        [Fact]
        public async Task FixCreateManyParameterDoc()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            /// <returns>A Project created out of the Douments created from the source strings</returns>
            public static Project CreateProject(string[] sources, out AdhocWorkspace workspace, string language = LanguageNames.CSharp)
            {
            }
        }
    }";

            const string expected = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name=""sources"">todo: describe sources parameter on CreateProject</param>
            /// <param name=""workspace"">todo: describe workspace parameter on CreateProject</param>
            /// <param name=""language"">todo: describe language parameter on CreateProject</param>
            /// <returns>A Project created out of the Douments created from the source strings</returns>
            public static Project CreateProject(string[] sources, out AdhocWorkspace workspace, string language = LanguageNames.CSharp)
            {
            }
        }
    }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task FixCreateManyParameterDocWhenHaveFullDocSyntax()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            /// Some text
            /// </summary>
            /// <remarks>Remarks</remarks>
            /// <code>Code sample</code>
            /// <exception cref=""System.ArgumentException""></exception>
            /// <example>Some sample</example>
            /// <returns>A <see cref=""System.Threading.Tasks.Task""/> to you</returns>
            public static Project CreateProject(string[] sources, out AdhocWorkspace workspace, string language = LanguageNames.CSharp)
            {
            }
        }
    }";

            const string expected = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            /// Some text
            /// </summary>
            /// <param name=""sources"">todo: describe sources parameter on CreateProject</param>
            /// <param name=""workspace"">todo: describe workspace parameter on CreateProject</param>
            /// <param name=""language"">todo: describe language parameter on CreateProject</param>
            /// <remarks>Remarks</remarks>
            /// <code>Code sample</code>
            /// <exception cref=""System.ArgumentException""></exception>
            /// <example>Some sample</example>
            /// <returns>A <see cref=""System.Threading.Tasks.Task""/> to you</returns>
            public static Project CreateProject(string[] sources, out AdhocWorkspace workspace, string language = LanguageNames.CSharp)
            {
            }
        }
    }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task FixCreateManyParameterDocWhenHaveOnlySummary()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            public static Project CreateProject(string[] sources, out AdhocWorkspace workspace, string language = LanguageNames.CSharp)
            {
            }
        }
    }";

            const string expected = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name=""sources"">todo: describe sources parameter on CreateProject</param>
            /// <param name=""workspace"">todo: describe workspace parameter on CreateProject</param>
            /// <param name=""language"">todo: describe language parameter on CreateProject</param>
            public static Project CreateProject(string[] sources, out AdhocWorkspace workspace, string language = LanguageNames.CSharp)
            {
            }
        }
    }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task FixCreateManyParameterDocWhenHaveOnlyReturns()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <returns>A Project created out of the Douments created from the source strings</returns>
            public static Project CreateProject(string[] sources, out AdhocWorkspace workspace, string language = LanguageNames.CSharp)
            {
            }
        }
    }";

            const string expected = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <param name=""sources"">todo: describe sources parameter on CreateProject</param>
            /// <param name=""workspace"">todo: describe workspace parameter on CreateProject</param>
            /// <param name=""language"">todo: describe language parameter on CreateProject</param>
            /// <returns>A Project created out of the Douments created from the source strings</returns>
            public static Project CreateProject(string[] sources, out AdhocWorkspace workspace, string language = LanguageNames.CSharp)
            {
            }
        }
    }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task FixCreateManyParameterDocWhenHaveOnlyRemarks()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <remarks>
            /// Todo: Remove/Update when https://github.com/dotnet/roslyn/issues/2580 is completed and there is
            /// an api to check for analyzer exceptions
            /// </remarks>
            private static void CheckIfAnalyzerThrew(ImmutableArray<Diagnostic> diags)
            {
            }
        }
    }";

            const string expected = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            /// <param name=""diags"">todo: describe diags parameter on CheckIfAnalyzerThrew</param>
            /// <remarks>
            /// Todo: Remove/Update when https://github.com/dotnet/roslyn/issues/2580 is completed and there is
            /// an api to check for analyzer exceptions
            /// </remarks>
            private static void CheckIfAnalyzerThrew(ImmutableArray<Diagnostic> diags)
            {
            }
        }
    }";

            await VerifyCSharpFixAsync(source, expected);
        }

    }
}
