using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class ForInArrayTests : CodeFixVerifier
    {

        [Fact]
        public void ForWithEmptyDeclarationAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                var i = 0;
                for ( ; i < array.Length; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForWithEmptyConditionAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; ; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForWithEmptyIncrementorsAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length; )
                {
                    var item = array[i];
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForAsWhileTrueAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var i = 1;
                var array = new [] {1};
                for ( ; ; )
                {
                    var item = array[i];
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForWithMultipleDeclarationsAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0, j = 1; i < array.Length; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForWithMultipleIncrementorsAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                var j = 0;
                for (var i = 0; i < array.Length; i++, j++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForWithSingleBodyAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length; i++)
                    Console.WriteLine(1);
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForWithBodyThatDoesNotAssignCurrentIndexToAVariableFromArrayAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length; i++)
                {
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForWithBodyThatDoesNotUseLessThanAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i >= array.Length; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForWithBodyThatDoesAssignesAnotherVariableToAVariableFromArrayAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                var j = 1;
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array[j];
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForWithBodyThatAssignesCurrentIndexToAVariableFromAnotherArrayAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                var anotherArray = new [] {1};
                for (var i = 0; i < array.Length; i++)
                {
                    var item = anotherArray[i];
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForWithConditionalThatDoesNoIterateFullArrayAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length - 2; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForWithDeclarationNotStartingOnZeroAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 1; i < array.Length; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForWhereIndexIsUsedThroughtBodyAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array[i];
                    var j = i;
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ForInArrayAnalyzerCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array[i];
                    // whatever comes after
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = ForInArrayAnalyzer.DiagnosticId,
                Message = "You can use foreach instead of for.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 17) }
            };
            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void WhenUsingForWithAnArrayThenChangesToForeach()
        {

            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length; i++)
                {
                    string a;
                    // whatever comes before
                    var item = array[i];
                    // whatever comes after
                    string b;
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
                var array = new [] {1};
                foreach (var item in array)
                {
                    string a;
                    // whatever comes before
                    // whatever comes after
                    string b;
                }
            }
        }
    }";
            VerifyCSharpFix(source, fixtest, 0);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new ForInArrayCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ForInArrayAnalyzer();
        }
    }
}