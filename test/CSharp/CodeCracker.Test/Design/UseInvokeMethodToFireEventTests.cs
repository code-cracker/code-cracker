using CodeCracker.CSharp.Design;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public class UseInvokeMethodToFireEventTests : CodeFixVerifier<UseInvokeMethodToFireEventAnalyzer, UseInvokeMethodToFireEventCodeFixProvider>
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
                Message = string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat.ToString(), "MyEvent"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 25) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void AcceptExpressionBodiedMethods()
        {
            const string test = @"
                public class MyClass
                {
                    public event System.EventHandler MyEvent;
                    public void Execute() =>
                        MyEvent(this, System.EventArgs.Empty);
                }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(),
                Message = string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat.ToString(), "MyEvent"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 25) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void FixExpressionBodiedMethods()
        {
            const string source = @"
                public class MyClass
                {
                    public event System.EventHandler MyEvent;
                    public void Execute() =>
                        MyEvent(this, System.EventArgs.Empty);
                }";
            const string fixtest = @"
                public class MyClass
                {
                    public event System.EventHandler MyEvent;
                    public void Execute() =>
                        MyEvent?.Invoke(this, System.EventArgs.Empty);
                }";

            await VerifyCSharpFixAsync(source, fixtest);
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
                Message = string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat.ToString(), "MyEvent"),
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
                Message = string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat.ToString(), "MyEvent"),
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
        public async void RaiseDiagnosticEvenWhenVerifiedForNullAndNotReturnedOrThrown()
        {
            const string test = @"
                public class MyClass
                {
                    public static void Execute(System.Action action)
                    {
                        if (action == null)
                        {
                            var a = 1;
                        }
                        action();
                    }
                }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(),
                Message = string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat.ToString(), "action"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 25) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void RaiseDiagnosticEvenWhenVerifiedForNullAndNotReturnedOrThrownWithBlocklessIf()
        {
            const string test = @"
                public class MyClass
                {
                    public static void Execute(System.Action action)
                    {
                        if (action == null)
                            System.Console.WriteLine();
                        action();
                    }
                }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(),
                Message = string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat.ToString(), "action"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 25) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void RaiseDiagnosticIfNullCheckIsAfterInvocation()
        {
            const string test = @"
                public class MyClass
                {
                    public static void Execute(System.Action action)
                    {
                        action();
                        if (action == null) throw new Exception();
                    }
                }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(),
                Message = string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat.ToString(), "action"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 25) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void IgnoreIfAlreadyVerifiedForNullWithThrow()
        {
            const string test = @"
                public class MyClass
                {
                    public static void Execute(System.Action action)
                    {
                        if (action == null) throw new Exception();
                        action();
                    }
                }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void IgnoreIfAlreadyVerifiedForNullInverted()
        {
            const string test = @"
                public class MyClass
                {
                    public static void Execute(System.Action action)
                    {
                        if (null == action) throw new Exception();
                        action();
                    }
                }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void IgnoreIfAlreadyVerifiedForNullWithReturn()
        {
            const string test = @"
                public class MyClass
                {
                    public static void Execute(System.Action action)
                    {
                        if (action == null) return;
                        action();
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

            await VerifyCSharpFixAsync(source, fixtest);
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
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async void IgnoreMemberAccess()
        {
            var test = @"var tuple = new Tuple<int, Action>(1, null);
tuple.Item2();".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void ReportOnParametersWhenReturnTypeIsAReferenceType()
        {
            var test = @"
public static TReturn Method<T, TReturn>(System.Func<T, TReturn> getter) where T : System.Attribute where TReturn : class
{
    return getter(default(T));
}".WrapInCSharpClass();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(),
                Message = string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat.ToString(), "getter"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 12) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WhenMethodInvokedWithNonReferenceTypeHasNoDiagnostic()
        {
            var test = @"
                public static TReturn Method<T, TReturn>(System.Func<T, TReturn> getter) where T : System.Attribute where TReturn : struct
                {
                    return getter(default(T));
                }".WrapInCSharpClass();

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
    }
}