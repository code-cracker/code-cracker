using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class DirectEventInvocationTests : CodeFixTest<DirectEventInvocationAnalyzer, DirectEventInvocationCodeFixProvider>
    {
        [Fact]
        public void WarningIfEventIsCalledDirectly()
        {
            var test = @"
                public class MyClass 
                { 
                    public event System.EventHandler MyEvent;

                    public void Execute() 
                    { 
                        MyEvent(this, System.EventArgs.Empty);
                    } 
                }";

            var expected = new DiagnosticResult
            {
                Id = DirectEventInvocationAnalyzer.DiagnosticId,
                Message = "Copy the 'MyEvent' event to a variable before invoke it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 25) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void WarningIfEventIsCalledDirectlyWithNullCheck()
        {
            var test = @"
                public class MyClass 
                { 
                    public event System.EventHandler MyEvent;

                    public void Execute() 
                    {
                        if (MyEvent != null)
                            MyEvent(this, System.EventArgs.Empty);
                    } 
                }";

            var expected = new DiagnosticResult
            {
                Id = DirectEventInvocationAnalyzer.DiagnosticId,
                Message = "Copy the 'MyEvent' event to a variable before invoke it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 29) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void NotWarningIfEventIsCopiedToLocalVariableBeforeInvocation()
        {
            var test = @"
                public class MyClass 
                { 
                    public event System.EventHandler MyEvent;

                    public void Execute() 
                    { 
                        var handler = MyEvent;
                        if (handler != null)
                            handler(this, System.EventArgs.Empty);
                    } 
                }";

            VerifyCSharpHasNoDiagnostics(test);
        }

        [Fact]
        public void WhenEventIsCalledDirectlyShouldCopyItToVariable()
        {
            var source = @"
                public class MyClass 
                { 
                    public event System.EventHandler MyEvent;

                    public void Execute() 
                    { 
                        MyEvent(this, System.EventArgs.Empty);
                    } 
                }";

            var fixtest = @"
                public class MyClass 
                { 
                    public event System.EventHandler MyEvent;

                    public void Execute() 
                    { 
                        var handler = MyEvent;
                        if (handler != null)
                            handler(this, System.EventArgs.Empty);
                    } 
                }";

            VerifyCSharpFix(source, fixtest, 0);
        }

        [Fact]
        public void KeepCommentsWhenReplacedWithCodeFix()
        {
            var source = @"
                public class MyClass 
                { 
                    public event System.EventHandler MyEvent;

                    public void Execute() 
                    { 
                        MyEvent(this, System.EventArgs.Empty); //Some Comment
                    } 
                }";

            var fixtest = @"
                public class MyClass 
                { 
                    public event System.EventHandler MyEvent;

                    public void Execute() 
                    { 
                        var handler = MyEvent;
                        if (handler != null)
                            handler(this, System.EventArgs.Empty); //Some Comment
                    } 
                }";

            VerifyCSharpFix(source, fixtest, 0);
        }

        [Fact]
        public void WhenEventIsCalledDirectlyWithNullCheckShouldReplacesTheExistentIfStatement()
        {
            var source = @"
                public class MyClass 
                { 
                    public event System.EventHandler MyEvent;

                    public void Execute() 
                    {
                        var v = 123;

                        if (MyEvent != null)
                            MyEvent(this, System.EventArgs.Empty);
                    } 
                }";

            var fixtest = @"
                public class MyClass 
                { 
                    public event System.EventHandler MyEvent;

                    public void Execute() 
                    { 
                        var v = 123;

                        var handler = MyEvent;
                        if (handler != null)
                            handler(this, System.EventArgs.Empty);
                    } 
                }";

            VerifyCSharpFix(source, fixtest, 0);
        }
    }
}