using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class ObjectInitializerWithLocalDeclarationTests : CodeFixVerifier<ObjectInitializerAnalyzer, ObjectInitializerCodeFixProvider>
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
                var p = new Person
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

        [Fact]
        public async Task ObjectCreationWithoutConstructorDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            class Point
            {
                public int X { get; set; }
                public int Y { get; set; }
            }
            Point GetPoint() { return null; }
            void Foo()
            {
                var myPoint = GetPoint();
                myPoint.X = 5;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenInitializerWouldReferenceAnotherVariableCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class Point
        {
            public Point(int x) { X = x; }
            public int X { get; set; }
            public int Y { get; set; }
        }
        class Bar
        {
            void Foo()
            {
                var myPoint = new Point(5);
                var myPoint2 = new Point(5);
                myPoint2.Y = myPoint.X;
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ObjectInitializer_LocalDeclaration.ToDiagnosticId(),
                Message = "You can use initializers in here.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 17) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenInitializerWouldReferenceItselfDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class Point
        {
            public Point(int x) { X = x; }
            public int X { get; set; }
            public int Y { get; set; }
        }
        class Bar
        {
            void Foo()
            {
                var myPoint = new Point(5);
                myPoint.Y = myPoint.X;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenInitializerWouldReferenceItselfWithParamDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class Point
        {
            public Point(int x) { X = x; }
            public int X { get; set; }
            public int Y { get; set; }
        }
        class Bar
        {
            void Foo(int i)
            {
                var myPoint = new Point(5);
                myPoint.Y = myPoint.X + i;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenInitializerWouldReferenceItselfInAnExpressionDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class Point
        {
            public Point(int x) { X = x; }
            public int X { get; set; }
            public int Y { get; set; }
        }
        class Bar
        {
            void Foo()
            {
                var myPoint = new Point(5);
                myPoint.Y = 5 + myPoint.X;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenInitializerHasAConditionalExpressionDoesNotCreateDiagnostic()
        {
            const string source = @"
    class Person
    {
        public string Name { get; set; }
        public void Bar()
        {
            var p = System.DateTime.Now.Second > 2 ? null : new Person();
            p.Name = ""Giovanni"";
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenInitializerHasANullCoalesceDoesNotCreateDiagnostic()
        {
            const string source = @"
    class Person
    {
        public string Name { get; set; }
        public void Bar()
        {
            var p = null ?? new Person();
            p.Name = ""Giovanni"";
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
    }

    public class ObjectInitializerWithAssignmentTests : CodeFixVerifier<ObjectInitializerAnalyzer, ObjectInitializerCodeFixProvider>
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
                p = new Person
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

        [Fact]
        public async Task ObjectCreationWithoutConstructorDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            class Point
            {
                public int X { get; set; }
                public int Y { get; set; }
            }
            Point GetPoint() { return null; }

            Point myPoint;
            void Foo()
            {
                myPoint = GetPoint();
                myPoint.X = 5;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
    }
}