using CodeCracker.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Style
{
    public class ForInArrayTests : CodeFixTest<ForInArrayAnalyzer, ForInArrayCodeFixProvider>
    {

        [Fact]
        public async Task ForWithEmptyDeclarationAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithEmptyConditionAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithEmptyIncrementorsAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForAsWhileTrueAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithMultipleDeclarationsAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithMultipleIncrementorsAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithSingleBodyAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithBodyThatDoesNotAssignCurrentIndexToAVariableFromArrayAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithBodyThatDoesNotUseLessThanAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithBodyThatDoesAssignesAnotherVariableToAVariableFromArrayAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithBodyThatAssignesCurrentIndexToAVariableFromAnotherArrayAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithConditionalThatDoesNoIterateFullArrayAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithDeclarationNotStartingOnZeroAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWhereIndexIsUsedThroughtBodyAnalyzerDoesNotCreateDiagnostic()
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
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForInArrayAnalyzerCreatesDiagnostic()
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
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ForInArrayAnalyzerWhereArrayIsInitializedOutsideTheScopeCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Bar()
            {
                var array = new [] {1};
                Foo(array);
            }
            public int Foo(int[] array)
            {
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 17) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenUsingForWithAnArrayThenChangesToForeach()
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

            const string fixtest = @"
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
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingForWithAnArrayDeclaredOutsideTheScopeThenChangesToForeach()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Bar()
            {
                var array = new [] {1};
                Foo(array);
            }
            public int Foo(int[] array)
            {
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

            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Bar()
            {
                var array = new [] {1};
                Foo(array);
            }
            public int Foo(int[] array)
            {
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
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }
}