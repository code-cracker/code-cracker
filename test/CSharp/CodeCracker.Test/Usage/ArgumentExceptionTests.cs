using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class ArgumentExceptionTests : CodeFixVerifier<ArgumentExceptionAnalyzer, ArgumentExceptionCodeFixProvider>
    {
        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentAnalyzerCreatesDiagnostic()
        {
            var test = Wrap(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var expected = new DiagnosticResult(DiagnosticId.ArgumentException.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(11, 56)
                .WithMessage("Type argument 'c' is not in the argument list.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionInCtorWithInvalidArgumentAnalyzerCreatesDiagnostic()
        {
            var test = Wrap(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var expected = new DiagnosticResult(DiagnosticId.ArgumentException.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(11, 56)
                .WithMessage("Type argument 'c' is not in the argument list.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentAndApplyingFirstFixUsesFirstParameter()
        {
            var test = Wrap(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var fixtest = Wrap(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""a"");
            }");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentAndApplyingSecondFixUsesSecondParameter()
        {
            var test = Wrap(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var fixtest = Wrap(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""b"");
            }");
            await VerifyCSharpFixAsync(test, fixtest, 1);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInCtorAndApplyingFirstFixUsesFirstParameter()
        {
            var test = Wrap(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var fixtest = Wrap(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""a"");
            }");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInCtorAndApplyingSecondFixUsesSecondParameter()
        {
            var test = Wrap(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var fixtest = Wrap(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""b"");
            }");
            await VerifyCSharpFixAsync(test, fixtest, 1);
        }

        [Fact]
        public async Task IgnoresArgumentExceptionObjectsInFields()
        {
            var test = Wrap(@"
            ArgumentException ex = new ArgumentException(""message"", ""paramName"");
            ");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresArgumentExceptionObjectsInGetAccessors()
        {
            var test = Wrap(@"
            public string StrangePropertyThatThrowsArgumentExceptionInsideGet
            { get { throw ArgumentException(""message"", ""paramName""); } }
            ");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionInSetPropertyArgumentNameShouldBeValue()
        {
            var test = Wrap(@"
            public string RejectsEverythingProperty
            {
                get { return null; } 
                set { throw new ArgumentException(""message"", ""c""); } 
            }
            ");

            var expected = new DiagnosticResult(DiagnosticId.ArgumentException.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(12, 62)
                .WithMessage("Type argument 'c' is not in the argument list.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInSetAccessorAndApplyingFixUsesParameter()
        {
            var test = Wrap(@"
            public string RejectsEverythingProperty
            {
                get { return null; } 
                set { throw new ArgumentException(""message"", ""c""); } 
            }
            ");

            var fixtest = Wrap(@"
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
            var test = Wrap(@"
            public string this[int a, int b]
            {
                get { throw new ArgumentException(""message"", ""c""); } 
            }
            ");

            var expected = new DiagnosticResult(DiagnosticId.ArgumentException.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(11, 62)
                .WithMessage("Type argument 'c' is not in the argument list.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInIndexerGetAccessorAndApplyingFirstFixUsesFirstParameter()
        {
            var test = Wrap(@"
            public string this[int a, int b]
            {
                get { throw new ArgumentException(""message"", ""c""); } 
            }
            ");

            var fixtest = Wrap(@"
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
            var test = Wrap(@"
            Action<int> action = (p) => { throw new ArgumentException(""message"", ""paramName""); };
            ");

            var expected = new DiagnosticResult(DiagnosticId.ArgumentException.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(9, 82)
                .WithMessage("Type argument 'paramName' is not in the argument list.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInLambdasAndApplyingFixUsesLambdaParameter()
        {
            var test = Wrap(@"
            Action<int> action = (p) => { throw new ArgumentException(""message"", ""paramName""); };
            ");

            var fixtest = Wrap(@"
            Action<int> action = (p) => { throw new ArgumentException(""message"", ""p""); };
            ");
            await VerifyCSharpFixAsync(test, fixtest);
        }


        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInSimpleLambdasAndApplyingFixUsesLambdaParameter()
        {
            var test = Wrap(@"
            Action<int> action = p => { throw new ArgumentException(""message"", ""paramName""); };

            void Foo(string a)
            {
                Action<int> action = param => { throw new ArgumentException(""message"", ""paramName""); };
            } 
            ");

            var fixtest = Wrap(@"
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
            var test = Wrap(@"
            public string this[int a, int b]
            {
                get { throw new ArgumentException(""message"", ""c""); } 
            }
            ");

            var fixtest = Wrap(@"
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
            var test = Wrap(@"
            public string this[int a, int b]
            {
                get { return null; }
                set { throw new ArgumentException(""message"", ""c""); } 
            }
            ");

            var fixtest = Wrap(@"
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
            var test = Wrap(@"
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
            var test = Wrap(@"
            ArgumentException Exception { get; } = new ArgumentException(""message"", ""paramName"");
            ");

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        static string Wrap(string code)
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