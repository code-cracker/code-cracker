using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class ObjectInitializerWithLocalDeclarationTests : CodeFixVerifier
    {

        [Fact]
        public void WhenAssigningButNotCreatingAnalyzerDoesNotCreateDiagnostic()
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
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenDeclaringTwoVariablesAnalyzerDoesNotCreateDiagnostic()
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
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenVariableIsDeclaredAndObjectIsCreatedButNoAssignmentsHappenLaterAnalyzerDoesNotCreateDiagnostic()
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
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenVariableIsDeclaredAndObjectIsCreatedButOnlyUnrelatedDeclarationsHappenLaterAnalyzerDoesNotCreateDiagnostic()
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
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenVariableIsDeclaredAndObjectIsCreatedButOnlyUnrelatedAssignmentsHappenLaterAnalyzerDoesNotCreateDiagnostic()
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
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenDeclaringAndCreatingObjectAndAssigningPropertiesThenAnalyzerCreatesDiagnostic()
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
                Id = ObjectInitializerAnalyzer.DiagnosticIdLocalDeclaration,
                Message = "You can use initializers in here.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 17) }
            };
            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void WhenUsingANewVariableDeclaredAndAssigningToPropertiesOfJustCreatedObjectChangeToObjectInitializersFix()
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

            var fixtest = @"
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
            VerifyCSharpFix(source, fixtest, 0);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ObjectInitializerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ObjectInitializerAnalyzer();
        }
    }

    public class ObjectInitializerWithAssingmentTests : CodeFixVerifier
    {

        [Fact]
        public void WhenObjectIsCreatedButNoAssignmentsHappenLaterAnalyzerDoesNotCreateDiagnostic()
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
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenCreatingObjectAndAssigningPropertiesThenAnalyzerCreatesDiagnostic()
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
                Id = ObjectInitializerAnalyzer.DiagnosticIdLocalDeclaration,
                Message = "You can use initializers in here.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 17) }
            };
            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void WhenObjectIsCreatedButOnlyUnrelatedDeclarationsHappenLaterAnalyzerDoesNotCreateDiagnostic()
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
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenObjectIsCreatedButOnlyUnrelatedAssignmentsHappenLaterAnalyzerDoesNotCreateDiagnostic()
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
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenUsingAPreviouslyDeclaredVariableAndAssigningToPropertiesOfJustCreatedObjectChangeToObjectInitializersFix()
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

            var fixtest = @"
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
            VerifyCSharpFix(source, fixtest, 0);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ObjectInitializerCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ObjectInitializerAnalyzer();
        }
    }
}