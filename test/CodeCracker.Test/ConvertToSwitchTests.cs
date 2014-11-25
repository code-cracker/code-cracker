using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class ConvertToSwitchTests : 
        CodeFixTest<ConvertToSwitchAnalyzer, ConvertToSwitchCodeFixProvider>
    {
        [Fact]
        public void CreateDiagnosticsWhenYouHaveThreeNestedIfsAndElse()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(string s)
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

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void CreateDiagnosticsWhenYouHaveThreeNestedIfs()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(string s)
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

            VerifyCSharpDiagnostic(test, expected);
        }


        [Fact]
        public void IgnoresIfWithNoElseIfNorElse()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(string s)
            {
                if (s == ""A"")
                { 
                    // .. 
                }
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(test);
        }

        [Fact]
        public void IgnoresIfWithNoElseIf()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(string s)
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
            VerifyCSharpHasNoDiagnostics(test);
        }

        [Fact]
        public void IgnoresLessThanThreeNestedIfs()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(string s)
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
            VerifyCSharpHasNoDiagnostics(test);
        }

        [Fact]
        public void IgnoresWhenNotAllConditionalsAreEqualsExpressions()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(string s)
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
            VerifyCSharpHasNoDiagnostics(test);
        }

        [Fact]
        public void IgnoresWhenNotAllConditionsUseSameIdentifier()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(string s, string f)
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
            VerifyCSharpHasNoDiagnostics(test);
        }

        [Fact]
        public void IgnoresWhenNotAllConditionsRightSidesAreConstants()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            string GetFee() { return null; }
            public void Foo(string s, string f)
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
            VerifyCSharpHasNoDiagnostics(test);
        }


        [Fact]
        public void FixReplacesNestedIfsWithSwitch()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public void Foo(string s)
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
        public void Foo(string s)
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
            VerifyCSharpFix(test, expected);
        }

        [Fact]
        public void FixReplacesNestedIfsWithSwitchWithDefaultCaseWhenElseIsUsed()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public void Foo(string s)
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
        public void Foo(string s)
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
            VerifyCSharpFix(test, expected);
        }

        [Fact]
        public void FixDoesNotUsesBreakWhenSwitchSectionReturns()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public void Foo(string s)
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
        public void Foo(string s)
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
            VerifyCSharpFix(test, expected);
        }
    }
}


