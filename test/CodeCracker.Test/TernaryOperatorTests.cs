using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class TernaryOperatorWithAssignmentTests : CodeFixTest<TernaryOperatorAnalyzer, TernaryOperatorWithAssignmentCodeFixProvider>
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
        public async Task WhenUsingIfWithoutElseAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(sourceWithoutElse);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButWithBlockWith2StatementsOnIfAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(sourceWithoutElse);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButWithBlockWith2StatementsOnElseAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(sourceWithoutElse);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButWithoutReturnOnElseAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(sourceWithoutElse);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButWithoutReturnOnIfAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(sourceWithoutElse);
        }

        [Fact]
        public async Task TwoIfsInARowAnalyzerDoesNotCreateDiagnostic()
        {
            const string sourceWithoutElse = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                string s;
                if (s == ""A"")
                {
                    DoSomething();
                }
                else if (s == ""A"")
                {
                    DoSomething();
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(sourceWithoutElse);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithDirectReturnAnalyzerCreatesDiagnostic()
        {
            var expected = new DiagnosticResult
            {
                Id = TernaryOperatorAnalyzer.DiagnosticIdForIfWithReturn,
                Message = "You can use a ternary operator.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 17) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithAssignmentChangeToTernaryFix()
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
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }


        [Fact]
        public async Task WhenUsingIfAndElseWithComplexAssignmentChangeToTernaryFix()
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
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }

    public class TernaryOperatorWithReturnTests : CodeFixTest<TernaryOperatorAnalyzer, TernaryOperatorWithReturnCodeFixProvider>
    {
        [Fact]
        public async Task WhenUsingIfAndElseWithDirectReturnChangeToTernaryFix()
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
                else
                    return 2;
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
                return something ? 1 : 2;
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithAssignmentAnalyzerCreatesDiagnostic()
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }
    }
}