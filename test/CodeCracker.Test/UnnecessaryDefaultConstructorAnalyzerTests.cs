using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class UnnecessaryDefaultConstructorAnalyzerTests : CodeFixTest<UnnecessaryDefaultConstructorAnalyzer, UnnecessaryDefaultConstructorCodeFixProvider>
    {
        [Fact]
        public void WhenHaveOnlyOneDefaultConstructorThenAnalyzerCreatesDiagnostic()
        {
            string sourceCode = @"using System;

            namespace ConsoleApplication1
            {
                public class MyType
                {
                    public MyType()
                    {
                    }
                }
            }";

            var expected = new DiagnosticResult
            {
                Id = UnnecessaryDefaultConstructorAnalyzer.DiagnosticId,
                Message = "Default constructor is unnecessary.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 21) }
            };
            VerifyCSharpDiagnostic(sourceCode, expected);
        }

        [Fact]
        public void WhenHaveAnotherConstructorNonDefaultThenAnalizerDoesNoCreatesDiagnostic()
        {
            string sourceCode = @"using System;

            namespace ConsoleApplication1
            {
                public class MyType
                {
                    public MyType()
                    {
                    }

                    public MyType(string parameter)
                    {
                    }
                }
            }";

            VerifyCSharpHasNoDiagnostics(sourceCode);
        }

        [Fact]
        public void WhenHaveAnotherConstructorNonDefaultAndDefaultConstructorIsLikePrivateThenAnalizerCreatesDiagnostic()
        {
            string sourceCode = @"using System;

            namespace ConsoleApplication1
            {
                public class MyType
                {
                    private MyType()
                    {
                    }

                    public MyType(string parameter)
                    {
                    }
                }
            }";

            var expected = new DiagnosticResult
            {
                Id = UnnecessaryDefaultConstructorAnalyzer.DiagnosticId,
                Message = "Default constructor is unnecessary.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 21) }
            };
            VerifyCSharpDiagnostic(sourceCode, expected);
        }

        [Fact]
        public void WhenHaveOnlyOneDefaultConstructorThenTheAnalyzerFixIt()
        {
            string sourceCode = @"using System;

            namespace ConsoleApplication1
            {
                public class MyType
                {
                    public MyType()
                    {
                    }
                }
            }";

            string fix = @"using System;

            namespace ConsoleApplication1
            {
                public class MyType
                {
                }
            }";

            VerifyCSharpFix(sourceCode, fix, 0, true);
        }

        [Fact]
        public void WhenHaveAnotherConstructorNonDefaultAndDefaultConstructorIsLikePrivateThenAnalizerFixIt()
        {
            string sourceCode = @"using System;

            namespace ConsoleApplication1
            {
                public class MyType
                {
                    private MyType()
                    {
                    }

                    public MyType(string parameter)
                    {
                    }
                }
            }";

            string fix = @"using System;

            namespace ConsoleApplication1
            {
                public class MyType
                {

                    public MyType(string parameter)
                    {
                    }
                }
            }";

            VerifyCSharpFix(sourceCode, fix, 0, true);
        }
    }
}
