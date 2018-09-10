using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class ConvertToSwitchTests : CodeFixVerifier<ConvertToSwitchAnalyzer, ConvertToSwitchCodeFixProvider>
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
            var expected = new DiagnosticResult(DiagnosticId.ConvertToSwitch.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 17)
                .WithMessage("You could use 'switch' instead of 'if'.");

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
            var expected = new DiagnosticResult(DiagnosticId.ConvertToSwitch.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 17)
                .WithMessage("You could use 'switch' instead of 'if'.");

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

        [Fact]
        public async Task FixDoesNotUsesBreakWhenSwitchSectionThrows()
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
                throw new Exception();
            }
            else
            {
                throw new Exception();
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
                    throw new Exception();
                default:
                    throw new Exception();
            }
        }
    }
}";
            await VerifyCSharpFixAsync(test, expected, formatBeforeCompare: false);
        }

        [Fact]
        public async Task FixDoesNotRemoveComments()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public async Task Foo(string s)
        {

            //comment 0
            if (foo == ""foo1"")
            {
                //comment 1
                DoStuff();
                //comment extra
                DoStuff();
            }
            //comment 2
            else if (foo == ""foo2"")
            {
                //comment 3
                DoStuff();
            }
            else if (foo == ""foo3"")
            {
                //comment 3
                DoStuff();
            }
            //comment 4
            else
            {
                //comment 5
                DoStuff();
            }
            //comment 6
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

            //comment 0
            switch (foo)
            {
                case ""foo1"":
                    //comment 1
                    DoStuff();
                    //comment extra
                    DoStuff();
                    break;
                //comment 2
                case ""foo2"":
                    //comment 3
                    DoStuff();
                    break;
                case ""foo3"":
                    //comment 3
                    DoStuff();
                    break;
                //comment 4
                default:
                    //comment 5
                    DoStuff();
                    break;
            }
            //comment 6
        }
    }
}";
            await VerifyCSharpFixAsync(test, expected, formatBeforeCompare: false);
        }
    }
}