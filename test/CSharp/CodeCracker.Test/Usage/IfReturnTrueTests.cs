using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class IfReturnTrueTests : CodeFixVerifier<IfReturnTrueAnalyzer, IfReturnTrueCodeFixProvider>
    {

        [Fact]
        public async Task WhenUsingIfWithoutElseAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButWithBlockWith2StatementsOnIfAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButWithBlockWith2StatementsOnElseAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButReturningTrueOnlyOnIfDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButReturningTrueOnlyOnElseDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingIfReturnTrueAndElseReturnFalseCreatesDiagnostic()
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
            var expected = new DiagnosticResult(DiagnosticId.IfReturnTrue.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(9, 17)
                .WithMessage("You should return the boolean directly.");

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenUsingIfReturnFalseAndElseReturnTrueCreatesDiagnostic()
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
            var expected = new DiagnosticResult(DiagnosticId.IfReturnTrue.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(9, 17)
                .WithMessage("You should return the boolean directly.");
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenUsingIfReturnTrueAndElseReturnFalseChangeToReturn()
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
            const string fixtest = @"
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
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingIfReturnFalseAndElseReturnTrueChangeToReturn()
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
            const string fixtest = @"
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
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }
}
