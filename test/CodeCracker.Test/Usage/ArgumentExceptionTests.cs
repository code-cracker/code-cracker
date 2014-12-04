using CodeCracker.Usage;
using Microsoft.CodeAnalysis;
using System;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Usage
{
    public class ArgumentExceptionTests : CodeFixTest<ArgumentExceptionAnalyzer, ArgumentExceptionCodeFixProvider>
    {
        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentAnalyzerCreatesDiagnostic()
        {
            var test = _(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var expected = new DiagnosticResult
            {
                Id = ArgumentExceptionAnalyzer.DiagnosticId,
                Message = "Type argument 'c' is not in the argument list.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 56) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionInCtorWithInvalidArgumentAnalyzerCreatesDiagnostic()
        {
            var test = _(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var expected = new DiagnosticResult
            {
                Id = ArgumentExceptionAnalyzer.DiagnosticId,
                Message = "Type argument 'c' is not in the argument list.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 56) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentAndApplyingFirstFixUsesFirstParameter()
        {
            var test = _(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var fixtest = _(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""a"");
            }");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentAndApplyingSecondFixUsesSecondParameter()
        {
            var test = _(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var fixtest = _(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""b"");
            }");
            await VerifyCSharpFixAsync(test, fixtest, 1);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInCtorAndApplyingFirstFixUsesFirstParameter()
        {
            var test = _(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var fixtest = _(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""a"");
            }");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInCtorAndApplyingSecondFixUsesSecondParameter()
        {
            var test = _(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var fixtest = _(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""b"");
            }");
            await VerifyCSharpFixAsync(test, fixtest, 1);
        }

        [Fact]
        public async Task IgnoresArgumentExceptionObjectsInFields()
        {
            var test = _(@"
            ArgumentException ex = new ArgumentException(""message"", ""paramName"");
            ");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresArgumentExceptionObjectsInGetAccessors()
        {
            var test = _(@"
            public string StrangePropertyThatThrowsArgumentExceptionInsideGet
            { get { throw ArgumentException(""message"", ""paramName""); } }
            ");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionInSetPropertyArgumentNameShouldBeValue()
        {
            var test = _(@"
            public string RejectsEverythingProperty
            {
                get { return null; } 
                set { throw new ArgumentException(""message"", ""c""); } 
            }
            ");

            var expected = new DiagnosticResult
            {
                Id = ArgumentExceptionAnalyzer.DiagnosticId,
                Message = "Type argument 'c' is not in the argument list.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 62) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInSetAccessorAndApplyingFixUsesParameter()
        {
            var test = _(@"
            public string RejectsEverythingProperty
            {
                get { return null; } 
                set { throw new ArgumentException(""message"", ""c""); } 
            }
            ");

            var fixtest = _(@"
            public string RejectsEverythingProperty
            {
                get { return null; } 
                set { throw new ArgumentException(""message"", ""value""); } 
            }
            ");
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionInGetPropertyWithIndexersArgumentNameShouldBeInParameterList()
        {
            var test = _(@"
            public string this[int a, int b]
            {
                get { throw new ArgumentException(""message"", ""c""); } 
            }
            ");

            var expected = new DiagnosticResult
            {
                Id = ArgumentExceptionAnalyzer.DiagnosticId,
                Message = "Type argument 'c' is not in the argument list.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 62) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInIndexerGetAccessorAndApplyingFirstFixUsesFirstParameter()
        {
            var test = _(@"
            public string this[int a, int b]
            {
                get { throw new ArgumentException(""message"", ""c""); } 
            }
            ");

            var fixtest = _(@"
            public string this[int a, int b]
            {
                get { throw new ArgumentException(""message"", ""a""); } 
            }
            ");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }
        

        [Fact]
        public async Task WhenThrowingArgumentExceptionInLambdaArgumentNameShouldBeInParameterList()
        {
            var test = _(@"
            Action<int> action = (p) => { throw new ArgumentException(""message"", ""paramName""); };
            ");

            var expected = new DiagnosticResult
            {
                Id = ArgumentExceptionAnalyzer.DiagnosticId,
                Message = "Type argument 'paramName' is not in the argument list.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 82) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInLambdasAndApplyingFixUsesLambdaParameter()
        {
            var test = _(@"
            Action<int> action = (p) => { throw new ArgumentException(""message"", ""paramName""); };
            ");

            var fixtest = _(@"
            Action<int> action = (p) => { throw new ArgumentException(""message"", ""p""); };
            ");
            await VerifyCSharpFixAsync(test, fixtest);
        }


        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInSimpleLambdasAndApplyingFixUsesLambdaParameter()
        {
            var test = _(@"
            Action<int> action = p => { throw new ArgumentException(""message"", ""paramName""); };

            void Foo(string a)
            {
                Action<int> action = param => { throw new ArgumentException(""message"", ""paramName""); };
            } 
            ");

            var fixtest = _(@"
            Action<int> action = p => { throw new ArgumentException(""message"", ""p""); };

            void Foo(string a)
            {
                Action<int> action = param => { throw new ArgumentException(""message"", ""param""); };
            } 
            ");
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInIndexerGetAccessorAndApplyingSecondFixUsesSecondParameter()
        {
            var test = _(@"
            public string this[int a, int b]
            {
                get { throw new ArgumentException(""message"", ""c""); } 
            }
            ");

            var fixtest = _(@"
            public string this[int a, int b]
            {
                get { throw new ArgumentException(""message"", ""b""); } 
            }
            ");
            await VerifyCSharpFixAsync(test, fixtest, 1);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInIndexerSetAccessorAndApplyingThirdFixUsesValue()
        {
            var test = _(@"
            public string this[int a, int b]
            {
                get { return null; }
                set { throw new ArgumentException(""message"", ""c""); } 
            }
            ");

            var fixtest = _(@"
            public string this[int a, int b]
            {
                get { return null; }
                set { throw new ArgumentException(""message"", ""value""); } 
            }
            ");
            await VerifyCSharpFixAsync(test, fixtest, 2);
        }

        [Fact]
        public async Task IgnoresArgumentExceptionObjectsInSetAccessorsOfIndexersThatUsesValue()
        {
            var test = _(@"
            public string this[int a, int b]
            {
                get { return null; }
                set { throw new ArgumentException(""message"", ""value""); } 
            }
            ");

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresArgumentExceptionObjectsInInitializerOfAutoProperties()
        {
            var test = _(@"
            ArgumentException Exception { get; } = new ArgumentException(""message"", ""paramName"");
            ");

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        
        static string _(string code)
        {
            return @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            " + code + @"
        }
    }";

        }
    }
}