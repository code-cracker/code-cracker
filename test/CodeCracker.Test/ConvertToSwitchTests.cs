using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class ConvertToSwitchTests :
        CodeFixTest<ConvertToSwitchAnalyzer, ConvertToSwitchCodeFixProvider>
    {
        [Fact]
        public async Task CreateDiagnosticsWhenYouHaveThreeNestedIfsAndElse()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo(string s)
            {
                if (s == ""A"")
                {
                    // ..
                }
                else if (s == ""B"")
                {
                    // ..
                }
                else if (s == ""C"")
                {
                    // ..
                }
                else
                {
                    // ..
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = ConvertToSwitchAnalyzer.DiagnosticId,
                Message = "You could use 'switch' instead of 'if'.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticsWhenYouHaveThreeNestedIfs()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo(string s)
            {
                if (s == ""A"")
                {
                    // ..
                }
                else if (s == ""B"")
                {
                    // ..
                }
                else if (s == ""C"")
                {
                    // ..
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = ConvertToSwitchAnalyzer.DiagnosticId,
                Message = "You could use 'switch' instead of 'if'.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }


        [Fact]
        public async Task IgnoresIfWithNoElseIfNorElse()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo(string s)
            {
                if (s == ""A"")
                {
                    // ..
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresIfWithNoElseIf()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo(string s)
            {
                if (s == ""A"")
                {
                    // ..
                }
                else
                {
                    // ..
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresLessThanThreeNestedIfs()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo(string s)
            {
                if (s == ""A"")
                {
                    // ..
                }
                else if (s == ""B"")
                {
                    // ..
                }
                else
                {
                    // ..
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenNotAllConditionalsAreEqualsExpressions()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo(string s)
            {
                if (s == ""A"")
                {
                    // ..
                }
                else if (s == ""B"")
                {
                    // ..
                }
                else if (s != ""C"")
                {
                    // ..
                }
                else
                {
                    // ..
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenNotAllConditionsUseSameIdentifier()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo(string s, string f)
            {
                if (s == ""A"")
                {
                    // ..
                }
                else if (s == ""B"")
                {
                    // ..
                }
                else if (f == ""C"")
                {
                    // ..
                }
                else
                {
                    // ..
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenNotAllConditionsRightSidesAreConstants()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            string GetFee() { return null; }
            public async Task Foo(string s, string f)
            {
                if (s == GetFee())
                {
                    // ..
                }
                else if (s == ""B"")
                {
                    // ..
                }
                else if (s == ""C"")
                {
                    // ..
                }
                else
                {
                    // ..
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        [Fact]
        public async Task FixReplacesNestedIfsWithSwitch()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public async Task Foo(string s)
        {
            if (s == ""A"")
            {
                DoStuff();
            }
            else if (s == ""B"")
            {
                DoStuff();
            }
            else if (s == ""C"")
            {
                DoStuff();
            }
        }
    }
}";

            const string expected = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public async Task Foo(string s)
        {
            switch (s)
            {
                case ""A"":
                    DoStuff();
                    break;
                case ""B"":
                    DoStuff();
                    break;
                case ""C"":
                    DoStuff();
                    break;
            }
        }
    }
}";
            await VerifyCSharpFixAsync(test, expected, formatBeforeCompare: false);
        }

        [Fact]
        public async Task FixReplacesNestedIfsWithSwitchWithDefaultCaseWhenElseIsUsed()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public async Task Foo(string s)
        {
            if (s == ""A"")
            {
                DoStuff();
            }
            else if (s == ""B"")
            {
                DoStuff();
            }
            else if (s == ""C"")
            {
                DoStuff();
                DoExtraStuff();
            }
            else
            {
                DoStuff();
            }
        }
    }
}";

            const string expected = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public async Task Foo(string s)
        {
            switch (s)
            {
                case ""A"":
                    DoStuff();
                    break;
                case ""B"":
                    DoStuff();
                    break;
                case ""C"":
                    DoStuff();
                    DoExtraStuff();
                    break;
                default:
                    DoStuff();
                    break;
            }
        }
    }
}";
            await VerifyCSharpFixAsync(test, expected, formatBeforeCompare: false);
        }

        [Fact]
        public async Task FixDoesNotUsesBreakWhenSwitchSectionReturns()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public async Task Foo(string s)
        {
            if (s == ""A"")
            {
                DoStuff();
            }
            else if (s == ""B"")
            {
                DoStuff();
            }
            else if (s == ""C"")
            {
                DoStuff();
                DoExtraStuff();
                return;
            }
            else
            {
                DoStuff();
                return;
            }
        }
    }
}";

            const string expected = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public async Task Foo(string s)
        {
            switch (s)
            {
                case ""A"":
                    DoStuff();
                    break;
                case ""B"":
                    DoStuff();
                    break;
                case ""C"":
                    DoStuff();
                    DoExtraStuff();
                    return;
                default:
                    DoStuff();
                    return;
            }
        }
    }
}";
            await VerifyCSharpFixAsync(test, expected, formatBeforeCompare: false);
        }
    }
}