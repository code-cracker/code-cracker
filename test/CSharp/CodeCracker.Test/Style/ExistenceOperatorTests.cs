using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class ExistenceOperatorWithReturnTests : CodeFixVerifier<ExistenceOperatorAnalyzer, ExistenceOperatorCodeFixProvider>
    {
        [Fact]
        public async Task WhenUsingIfWithoutElseAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo()
            {
                A a = null;
                if (a != null)
                    return a.Name;
                return "";
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
            public string Foo()
            {
                A a = null;
                if (a != null)
                {
                    string a;
                    return a.Name;
                }
                else
                {
                    return null;
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
            public string Foo()
            {
                A a = null;
                if (a != null)
                {
                    return a.Name;
                }
                else
                {
                    string a;
                    return null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButReturningOnlyOnIfDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo()
            {
                A a = null;
                if (a != null)
                {
                    return a.Name;
                }
                else
                {
                    string a;
                }
                return null;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButReturningOnlyOnElseDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo()
            {
                A a = null;
                if (a != null)
                {
                    string a;
                }
                else
                {
                    return null;
                }
                return a.Name;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenIfWithElseButIfStatementDoesNotCheckForNullDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo()
            {
                A a = null;
                var condition = true;
                if (condition)
                {
                    return a.Name;
                }
                else
                {
                    return null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenIfStatementComparesWithSomethingThatIsNotNullDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo()
            {
                A a = null;
                var condition = true;
                if (1 != 2)
                {
                    return a.Name;
                }
                else
                {
                    return null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenIfStatementDoesNotCompareWithIdentifierDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo()
            {
                A a = null;
                if (Whatever() != null)
                {
                    return a.Name;
                }
                else
                {
                    return null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenIfConditionIdentifierDoesNotMatchIfStatementIdentifierDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo()
            {
                A a = null;
                A b = null;
                if (a != null)
                {
                    return b.Name;
                }
                else
                {
                    return null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingIfReturnExpressionAndElseReturnNullCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo()
            {
                A a = null;
                if (a != null)
                    return a.Name;
                else
                    return null;
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ExistenceOperator.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(9, 17)
                .WithMessage("You can use the existence operator.");
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenUsingIfReturnExpressionAndElseReturnNullChangesToReturnDirectlyWithExistenceOperator()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo()
            {
                A a = null;
                if (a != null)
                    return a.Name;
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
            public string Foo()
            {
                A a = null;
                return a?.Name;
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }
    public class ExistenceOperatorTestsWithAssignment : CodeFixVerifier<ExistenceOperatorAnalyzer, ExistenceOperatorCodeFixProvider>
    {

        [Fact]
        public async Task WhenUsingIfWithoutElseAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                A a = null;
                string name;
                if (a != null)
                    name = a.Name;
                name = "";
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
            public async Task Foo()
            {
                A a = null;
                string name;
                if (a != null)
                {
                    string a;
                    name = a.Name;
                }
                else
                {
                    name = null;
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
            public async Task Foo()
            {
                A a = null;
                string name;
                if (a != null)
                {
                    name = a.Name;
                }
                else
                {
                    string a;
                    name = null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButAssigningOnlyOnIfDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                A a = null;
                string name;
                if (a != null)
                {
                    name = a.Name;
                }
                else
                {
                    string a;
                }
                name = null;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButAssigningOnlyOnElseDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                A a = null;
                string name;
                if (a != null)
                {
                    string a;
                }
                else
                {
                    name = null;
                }
                name = a.Name;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenIfWithElseButIfStatementDoesNotCheckForNullDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo()
            {
                A a = null;
                var condition = true;
                string name;
                if (condition)
                {
                    name = a.Name;
                }
                else
                {
                    name = null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenIfStatementComparesWithSomethingThatIsNotNullDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                A a = null;
                string name;
                if (1 != 2)
                {
                    name = a.Name;
                }
                else
                {
                    name = null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenIfStatementDoesNotCompareWithIdentifierDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                A a = null;
                string name;
                if (Whatever() != null)
                {
                    name = a.Name;
                }
                else
                {
                    name = null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenIfConditionIdentifierDoesNotMatchIfStatementIdentifierDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                A a = null;
                A b = null;
                string name;
                if (a != null)
                {
                    name = b.Name;
                }
                else
                {
                    name = null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenIfConditionIsNotAnIdentifierDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private string TheName;
            public async Task Foo()
            {
                A a = null;
                A b = null;
                string name;
                if (a != null)
                {
                    A.TheName = b.Name;
                }
                else
                {
                    name = null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenElseConditionIsNotAnIdentifierDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private string TheName;
            public async Task Foo()
            {
                A a = null;
                A b = null;
                string name;
                if (a != null)
                {
                    name = b.Name;
                }
                else
                {
                    A.TheName = null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenIfIdentifierDoesNotMatchElseIdentifiderDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private string TheName;
            public async Task Foo()
            {
                A a = null;
                A b = null;
                string name;
                string name2;
                if (a != null)
                {
                    name = b.Name;
                }
                else
                {
                    name2 = null;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingIfAssignmentExpressionAndElseAssignNullCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                A a = null;
                string name;
                if (a != null)
                    name = a.Name;
                else
                    name = null;
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ExistenceOperator.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 17)
                .WithMessage("You can use the existence operator.");
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenUsingIfAssignmentExpressionAndElseAssignNullChangesToAssignDirectlyWithExistenceOperator()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo()
            {
                A a = null;
                string name;
                if (a != null)
                    name = a.Name;
                else
                    name = null;
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo()
            {
                A a = null;
                string name;
                name = a?.Name;
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }
    }
}