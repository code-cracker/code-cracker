using CodeCracker.CSharp.Design;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CodeCracker.CSharp.Test.Design
{
    public class UseInvokeMethodToFireEventTests : CodeFixTest<UseInvokeMethodToFireEventAnalyzer, UseInvokeMethodToFireEventCodeFixProvider>
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
                Id = DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(),
                Message = "Use ?.Invoke operator and method to fire 'MyEvent' event.",
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
                Id = DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(),
                Message = "Use ?.Invoke operator and method to fire 'MyEvent' event.",
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
                Id = DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(),
                Message = "Use ?.Invoke operator and method to fire 'MyEvent' event.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 25) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void NotWarningIfEventIsFiredWithInvokeMethod()
        {
            const string test = @"
                public class MyClass 
                { 
                    public event System.EventHandler MyEvent;

                    public void Execute() 
                    { 
                        MyEvent?.Invoke(this, System.EventArgs.Empty);
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
        public async void WhenEventIsFiredDirectlyShouldUseInvokeMethod()
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
                        MyEvent?.Invoke(this, System.EventArgs.Empty);
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
                        MyEvent(this, System.EventArgs.Empty); //Some Comment
                    } 
                }";

            const string fixtest = @"
                public class MyClass 
                { 
                    public event System.EventHandler MyEvent;

                    public void Execute() 
                    { 
                        MyEvent?.Invoke(this, System.EventArgs.Empty); //Some Comment
                    } 
                }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void IgnoreMemberAccess()
        {
            var test = @"var tuple = new Tuple<int, Action>(1, null);
tuple.Item2();".WrapInMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
    }
}