using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using TestHelper;
using CodeCracker;
using Xunit;

namespace CodeCracker.Test
{
    public class TernaryOperatorTests : CodeFixVerifier
    {
        private const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                    return 1;
                else
                    return 2;
            }
        }
    }";

        [Fact]
        public void WhenUsingIfWithoutElseAnalyzerDoesNotCreateDiagnostic()
        {
            const string sourceWithoutElse = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                    return 1;
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(sourceWithoutElse);
        }

        [Fact]
        public void WhenUsingIfWithElseButWithBlockWith2StatementsOnIfAnalyzerDoesNotCreateDiagnostic()
        {
            const string sourceWithoutElse = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                {
                    string a = null;
                    return 1;
                }
                else
                {
                    return 2;
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(sourceWithoutElse);
        }

        [Fact]
        public void WhenUsingIfWithElseButWithBlockWith2StatementsOnElseAnalyzerDoesNotCreateDiagnostic()
        {
            const string sourceWithoutElse = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                {
                    return 1;
                }
                else
                {
                    string a = null;
                    return 2;
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(sourceWithoutElse);
        }

        [Fact]
        public void WhenUsingIfWithElseButWithoutReturnOnElseAnalyzerDoesNotCreateDiagnostic()
        {
            const string sourceWithoutElse = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                {
                    return 2;
                }
                else
                {
                    string a = null;
                }
                return 1;
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(sourceWithoutElse);
        }

        [Fact]
        public void WhenUsingIfWithElseButWithoutReturnOnIfAnalyzerDoesNotCreateDiagnostic()
        {
            const string sourceWithoutElse = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                {
                    string a = null;
                }
                else
                {
                    return 2;
                }
                return 1;
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(sourceWithoutElse);
        }

        [Fact]
        public void WhenUsingIfAndElseWithDirectReturnAnalyzerCreatesDiagnostic()
        {
            var expected = new DiagnosticResult
            {
                Id = TernaryOperatorAnalyzer.DiagnosticId,
                Message = "You can use a ternary operator.",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 9, 17)
                        }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void WhenUsingIfAndElseWithDirectReturnChangeToTernaryFix()
        {

            var fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                return something ? 1 : 2;
            }
        }
    }";
            VerifyCSharpFix(source, fixtest, 0);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new TernaryOperatorCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new TernaryOperatorAnalyzer();
        }
    }
}