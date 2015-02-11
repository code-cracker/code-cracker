using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.CSharp.Test.Style
{
    public class ObjectInitializerWithLocalDeclarationTests : CodeFixTest<ObjectInitializerAnalyzer, ObjectInitializerCodeFixProvider>
    {

        [Fact]
        public async Task WhenAssigningButNotCreatingAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var p = 1;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenDeclaringTwoVariablesAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                Person q = new Person(), r = new Person();
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenVariableIsDeclaredAndObjectIsCreatedButNoAssignmentsHappenLaterAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var p = new Person();
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenVariableIsDeclaredAndObjectIsCreatedButOnlyUnrelatedDeclarationsHappenLaterAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var p = new Person();
                string a;
                int i;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenVariableIsDeclaredAndObjectIsCreatedButOnlyUnrelatedAssignmentsHappenLaterAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private A a;
            public int Foo()
            {
                var p = new Person();
                a.Name = 1;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenDeclaringAndCreatingObjectAndAssigningPropertiesThenAnalyzerCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var p = new Person();
                p.Name = ""Giovanni"";
                p.Age = 25;
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ObjectInitializer_LocalDeclaration.ToDiagnosticId(),
                Message = "You can use initializers in here.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 17) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenUsingANewVariableDeclaredAndAssigningToPropertiesOfJustCreatedObjectChangeToObjectInitializersFix()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                string a;
                //some comment before
                var p = new Person();
                p.Name = ""Giovanni"";
                p.Age = 25;
                //some comment after
                string b;
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
                string a;
                //some comment before
                var p = new Person()
                {
                    Name = ""Giovanni"",
                    Age = 25
                };
                //some comment after
                string b;
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }

    public class ObjectInitializerWithAssingmentTests : CodeFixTest<ObjectInitializerAnalyzer, ObjectInitializerCodeFixProvider>
    {

        [Fact]
        public async Task WhenObjectIsCreatedButNoAssignmentsHappenLaterAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                Person p;
                p = new Person();
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenCreatingObjectAndAssigningPropertiesThenAnalyzerCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                Person p;
                p = new Person();
                p.Name = ""Giovanni"";
                p.Age = 25;
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ObjectInitializer_Assignment.ToDiagnosticId(),
                Message = "You can use initializers in here.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 17) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenObjectIsCreatedButOnlyUnrelatedDeclarationsHappenLaterAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                Person p;
                p = new Person();
                string a;
                int i;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenObjectIsCreatedButOnlyUnrelatedAssignmentsHappenLaterAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private A a;
            public int Foo()
            {
                Person p;
                p = new Person();
                a.Name = 1;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingAPreviouslyDeclaredVariableAndAssigningToPropertiesOfJustCreatedObjectChangeToObjectInitializersFix()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                string a;
                //some comment before
                Person p;
                p = new Person();
                p.Name = ""Giovanni"";
                p.Age = 25;
                //some comment after
                string b;
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
                string a;
                //some comment before
                Person p;
                p = new Person()
                {
                    Name = ""Giovanni"",
                    Age = 25
                };
                //some comment after
                string b;
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }
}