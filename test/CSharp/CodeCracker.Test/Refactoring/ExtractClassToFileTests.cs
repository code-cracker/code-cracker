using System.Threading.Tasks;
using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class ExtractClassToFileTests : CodeFixVerifier<ExtractClassToFileAnalyzer, ExtractClassToFileCodeFixProvider>
    {
        [Fact]
        public async Task SourceFileWithOnlyOneClass()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty { get; private set; }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInCSharpMethod());
        }

        public async Task SourceFileWithTwoClassesButNotPublic()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty { get; private set; }
        }

        class TypeName2
        {
            public int MyProperty2 { get; private set; }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInCSharpMethod());
        }

        public async Task IgnoreDiagnosticWhenClassOutsideNamespace()
        {
            const string test = @"
    using System;

    class TypeName
    {
        public int MyProperty { get; private set; }
    }

    class TypeName2
    {
        public int MyProperty2 { get; private set; }
    }
    ";
            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInCSharpMethod());
        }


        [Fact]
        public async Task CreateDiagnosticWhenHasTwoClassesAndAtLeastOnePublic()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty { get; private set; }
        }

        public class TypeName2
        {
            public int MyProperty2 { get; private set; }
        }
    }";

            var diagnostic = new DiagnosticResult
            {
                Id = DiagnosticId.ExtractClassToFile.ToDiagnosticId(),
                Message = "Extract class 'TypeName' to new file.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 9) }
            };
            var diagnostic1 = new DiagnosticResult
            {
                Id = DiagnosticId.ExtractClassToFile.ToDiagnosticId(),
                Message = "Extract class 'TypeName2' to new file.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 9) }
            };

            await VerifyCSharpDiagnosticAsync(test.WrapInCSharpMethod(), new [] { diagnostic, diagnostic1 });
        }

        [Fact]
        public async Task CreateDiagnosticWhenHasTwoClassesWithoutNameSpace()
        {
            const string test = @"
    using System;

        class TypeName
        {
            public int MyProperty { get; private set; }
        }

        public class TypeName2
        {
            public int MyProperty2 { get; private set; }
        }";

            var diagnostic = new DiagnosticResult
            {
                Id = DiagnosticId.ExtractClassToFile.ToDiagnosticId(),
                Message = "Extract class 'TypeName' to new file.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 9) }
            };

            var diagnostic1 = new DiagnosticResult
            {
                Id = DiagnosticId.ExtractClassToFile.ToDiagnosticId(),
                Message = "Extract class 'TypeName2' to new file.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 18, 9) }
            };
            await VerifyCSharpDiagnosticAsync(test.WrapInCSharpMethod(), new[] { diagnostic, diagnostic1 });
        }


        [Fact]
        public async Task SourceFileWithTwoClassesAndExtractOneClass()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty { get; private set; }
        }

        public class TypeName2
        {
            public int MyProperty2 { get; private set; }
        }
    }";
            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {

        public class TypeName2
        {
            public int MyProperty2 { get; private set; }
        }
    }";
            await VerifyCSharpFixAsync(test, expected, documentsCount: 2);
        }

        [Fact]
        public async Task SourceFileWithNestedNameSpaces()
        {
            const string test = @"
            using System;

            namespace Foo
            {
                using System.IO;
                namespace Bar
                {
                    class Bar2
                    {
                    }
        
                    /// <summary>
                    /// test
                    /// </summary>
                    namespace Baz
                    {
                        using System.Threading.Tasks;

                        class Baz2
                        {
                        }
                        class Bar21
                        {

                        }
                    }
                }
            }";

            const string expected = @"
            using System;

            namespace Foo
            {
                using System.IO;
                namespace Bar
                {
                    class Bar2
                    {
                    }
        
                    /// <summary>
                    /// test
                    /// </summary>
                    namespace Baz
                    {
                        using System.Threading.Tasks;
                        class Bar21
                        {

                        }
                    }
                }
            }";
            await VerifyCSharpFixAsync(test, expected, documentsCount: 2);
        }
    }
}
