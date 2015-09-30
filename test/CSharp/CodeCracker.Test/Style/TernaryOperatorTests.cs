using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class TernaryOperatorWithAssignmentTests : CodeFixVerifier<TernaryOperatorAnalyzer, TernaryOperatorWithAssignmentCodeFixProvider>
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
                Id = DiagnosticId.TernaryOperator_Return.ToDiagnosticId(),
                Message = "You can use a ternary operator.",
                Severity = DiagnosticSeverity.Info,
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
            const string fixtest = @"
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
        public async Task WhenUsingIfAndElseWithNullableValueTypeAssignmentChangeToTernaryFix()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var something = true;
                int? a;
                if (something)
                {
                    a = 1;
                }
                else
                {
                    a = null;
                }
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var something = true;
                int? a;
                a = something ? 1 : (int?)null;
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithAssignmentChangeToTernaryFixAll()
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
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                a = something ? ""a"" : ""b"";
                a = something ? ""a"" : ""b"";
            }
        }
    }";
            await VerifyCSharpFixAllAsync(new string[] { source, source.Replace("TypeName", "TypeName1") }, new string[] { fixtest, fixtest.Replace("TypeName", "TypeName1") });
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
            const string fixtest = @"
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

        [Fact]
        public async Task WhenUsingIfAndElseWithComplexAssignmentChangeToTernaryFixAll()
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
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                a = something ? ""a"" + ""b"" : ""c"" + GetInfo(1);
                a = something ? ""a"" + ""b"" : ""c"" + GetInfo(1);
            }
        }
    }";
            await VerifyCSharpFixAllAsync(new string[] { source, source.Replace("TypeName", "TypeName1") }, new string[] { fixtest, fixtest.Replace("TypeName", "TypeName1") });
        }
    }

    public class TernaryOperatorWithReturnTests : CodeFixVerifier<TernaryOperatorAnalyzer, TernaryOperatorWithReturnCodeFixProvider>
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

            const string fixtest = @"
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
        public async Task WhenUsingIfAndElseWithNullableValueTypeDirectReturnChangeToTernaryFix()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int? Foo()
            {
                var something = true;
                if (something)
                    return 1;
                else
                    return null;
            }
        }
    }";

            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int? Foo()
            {
                var something = true;
                return something ? 1 : (int?)null;
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithDirectReturnChangeToTernaryFixAll()
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
                if (something)
                    return 1;
                else
                    return 2;
            }
        }
    }";

            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                return something ? 1 : 2;
                return something ? 1 : 2;
            }
        }
    }";
            await VerifyCSharpFixAllAsync(new string[] { source, source.Replace("TypeName", "TypeName1") }, new string[] { fixtest, fixtest.Replace("TypeName", "TypeName1") });
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
                Id = DiagnosticId.TernaryOperator_Assignment.ToDiagnosticId(),
                Message = "You can use a ternary operator.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }
    }
}