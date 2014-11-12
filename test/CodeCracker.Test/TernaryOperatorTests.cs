using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class TernaryOperatorWithAssignmentTests : CodeFixVerifier
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
                Id = TernaryOperatorAnalyzer.DiagnosticIdForIfWithReturn,
                Message = "You can use a ternary operator.",
                Severity = DiagnosticSeverity.Warning,
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
            return new TernaryOperatorWithReturnCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new TernaryOperatorAnalyzer();
        }
    }

    public class TernaryOperatorWithReturnTests : CodeFixVerifier
    {

        [Fact]
        public void WhenUsingIfAndElseWithAssignmentChangeToTernaryFix()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                if (something)
                {
                    a = ""a"";
                }
                else
                {
                    a = ""b"";
                }
            }
        }
    }";
            var fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                a = something ? ""a"" : ""b"";
            }
        }
    }";
            VerifyCSharpFix(source, fixtest, 0);
        }


        [Fact]
        public void WhenUsingIfAndElseWithComplexAssignmentChangeToTernaryFix()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                if (something)
                {
                    a = ""a"" + ""b"";
                }
                else
                {
                    a = ""c"" + GetInfo(1);
                }
            }
        }
    }";
            var fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                a = something ? ""a"" + ""b"" : ""c"" + GetInfo(1);
            }
        }
    }";
            VerifyCSharpFix(source, fixtest, 0);
        }

        [Fact]
        public void WhenUsingIfAndElseWithAssignmentAnalyzerCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                if (something)
                {
                    a = ""a"";
                }
                else
                {
                    a = ""b"";
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = TernaryOperatorAnalyzer.DiagnosticIdForIfWithAssignment,
                Message = "You can use a ternary operator.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 10, 17)
                        }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new TernaryOperatorWithAssignmentCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new TernaryOperatorAnalyzer();
        }
    }
}