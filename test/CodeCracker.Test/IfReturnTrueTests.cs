using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class IfReturnTrueTests : CodeFixVerifier
    {

        [Fact]
        public void WhenUsingIfWithoutElseAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
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
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenUsingIfWithElseButWithBlockWith2StatementsOnIfAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
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
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenUsingIfWithElseButWithBlockWith2StatementsOnElseAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
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
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenUsingIfWithElseButReturningTrueOnlyOnIfDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                {
                    return true;
                }
                else
                {
                    string a = null;
                }
                return 1;
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenUsingIfWithElseButReturningTrueOnlyOnElseDoesNotCreateDiagnostic()
        {
            const string source = @"
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
                    return true;
                }
                return 1;
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenUsingIfReturnTrueAndElseReturnFalseCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = IfReturnTrueAnalyzer.DiagnosticId,
                Message = "You should return directly.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 17) }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void WhenUsingIfReturnFalseAndElseReturnTrueCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = IfReturnTrueAnalyzer.DiagnosticId,
                Message = "You should return directly.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 17) }
            };
            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void WhenUsingIfReturnTrueAndElseReturnFalseChangeToReturn()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public bool Foo()
            {
                var something = true;
                //some comment before
                if (something)
                {
                    return true;
                }
                else
                {
                    return false;
                }
                //some comment after
                string a;
            }
        }
    }";
            var fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public bool Foo()
            {
                var something = true;
                //some comment before
                return something;
                //some comment after
                string a;
            }
        }
    }";
            VerifyCSharpFix(source, fixtest, 0);
        }

        [Fact]
        public void WhenUsingIfReturnFalseAndElseReturnTrueChangeToReturn()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public bool Foo()
            {
                var something = true;
                //some comment before
                if (something)
                {
                    return false;
                }
                else
                {
                    return true;
                }
                //some comment after
                string a;
            }
        }
    }";
            var fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public bool Foo()
            {
                var something = true;
                //some comment before
                return something == false;
                //some comment after
                string a;
            }
        }
    }";
            VerifyCSharpFix(source, fixtest, 0);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new IfReturnTrueCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new IfReturnTrueAnalyzer();
        }
    }
}