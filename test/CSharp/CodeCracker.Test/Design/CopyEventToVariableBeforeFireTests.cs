﻿using CodeCracker.CSharp.Design;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public class CopyEventToVariableBeforeFireTests : CodeFixVerifier<CopyEventToVariableBeforeFireAnalyzer, CopyEventToVariableBeforeFireCodeFixProvider>
    {
        [Fact]
        public async void WarningIfEventIsFiredDirectly()
        {
            const string test = @"
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
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before fire it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 25) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfCustomEventIsFiredDirectly()
        {
            const string test = @"
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
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before fire it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 25) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfCustomEventWithCustomDelegateIsFiredDirectly()
        {
            const string test = @"
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
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before fire it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 25) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void NotWarningIfEventIsCopiedToLocalVariableBeforeFire()
        {
            const string test = @"
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
        public async void NotWarningIfIsNotAnEvent()
        {
            const string test = @"
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
        public async void NotWarningIfIsAParameter()
        {
            const string test = @"
                public class MyClass
                {
                    public void Execute(Action action)
                    {
                        action();
                    }
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void WhenEventIsFiredDirectlyShouldCopyItToVariable()
        {
            const string source = @"
                public class MyClass
                {
                    public event System.EventHandler MyEvent;

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            const string fixtest = @"
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
            const string source = @"
                public class MyClass
                {
                    public event System.EventHandler MyEvent;

                    public void Execute()
                    {
                        //comment
                        MyEvent(this, System.EventArgs.Empty); //Some Comment
                    }
                }";

            const string fixtest = @"
                public class MyClass
                {
                    public event System.EventHandler MyEvent;

                    public void Execute()
                    {
                        //comment
                        var handler = MyEvent;
                        if (handler != null)
                            handler(this, System.EventArgs.Empty); //Some Comment
                    }
                }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void FixWhenInvocationIsInsideABlockWithoutBraces()
        {
            const string source = @"
                public class MyClass
                {
                    public event System.EventHandler MyEvent;
                    bool raiseEvents = true;

                    public void Execute()
                    {
                        if (raiseEvents) MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            const string fixtest = @"
                public class MyClass
                {
                    public event System.EventHandler MyEvent;
                    bool raiseEvents = true;

                    public void Execute()
                    {
                        if (raiseEvents)
                        {
                            var handler = MyEvent;
                            if (handler != null)
                                handler(this, System.EventArgs.Empty);
                        }
                    }
                }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void IgnoreMemberAccess()
        {
            var test = @"var tuple = new Tuple<int, Action>(1, null);
tuple.Item2();".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void NotWarningIfExpressionBodied()
        {
            const string test = @"
                public class MyClass
                {
                    public int Foo(int par1, int par2) => FuncCalc(par1,par2);

                    public int FuncCalc(int p1, int p2)
                    {
                         return p1*p2;
                    }

                }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
    }
}