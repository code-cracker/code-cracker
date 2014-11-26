﻿using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class DirectEventInvocationTests : CodeFixTest<DirectEventInvocationAnalyzer, DirectEventInvocationCodeFixProvider>
    {
        [Fact]
        public async void WarningIfEventIsCalledDirectly()
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

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfCustomEventIsCalledDirectly()
        {
            var test = @"
                public class MyArgs : System.EventArgs
                {
                    public string Info { get; set; }
                }

                public class MyClass 
                { 
                    public event System.EventHandler<MyArgs> MyEvent;

                    public void Execute() 
                    { 
                        MyEvent(this, new MyArgs() { Info = ""ping"" });
                    } 
                }";

            var expected = new DiagnosticResult
            {
                Id = DirectEventInvocationAnalyzer.DiagnosticId,
                Message = "Copy the 'MyEvent' event to a variable before invoke it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 25) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfCustomEventWithCustomDelegateIsCalledDirectly()
        {
            var test = @"
                public class MyArgs : System.EventArgs
                {
                    public string Info { get; set; }
                }

                public delegate void Executed (object sender, MyArgs args);

                public class MyClass 
                { 
                    public event Executed MyEvent;

                    public void Execute() 
                    { 
                        MyEvent(this, new MyArgs() { Info = ""ping"" });
                    } 
                }";

            var expected = new DiagnosticResult
            {
                Id = DirectEventInvocationAnalyzer.DiagnosticId,
                Message = "Copy the 'MyEvent' event to a variable before invoke it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 25) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfEventIsCalledDirectlyWithNullCheck()
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

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void NotWarningIfEventIsCopiedToLocalVariableBeforeInvocation()
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

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void NotWarningIfInvocationIsNotAnEvent()
        {
            var test = @"
                public class MyClass 
                { 
                    public void Execute() 
                    { 
                        MyClass.Run(null);
                    } 

                    public static void Run(object obj) 
                    { 

                    }
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void WhenEventIsCalledDirectlyShouldCopyItToVariable()
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

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void KeepCommentsWhenReplacedWithCodeFix()
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

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void WhenEventIsCalledDirectlyWithNullCheckShouldReplacesTheExistentIfStatement()
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

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }
}