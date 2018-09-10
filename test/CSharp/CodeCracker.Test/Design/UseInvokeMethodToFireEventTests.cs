using CodeCracker.CSharp.Design;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
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

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(8, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfEventIsReadOnlyFiredDirectlyAndNotInitialized()
        {
            const string test = @"
                public class MyClass
                {
                    public readonly event System.EventHandler MyEvent;

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(8, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfEventIsReadOnlyAndAssignedInIfInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent;

                    public MyClass(bool shouldAssign)
                    {
                        if(shouldAssign)
                        {
                            MyEvent = (sender, args) => { };
                        }
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(16, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfEventIsReadOnlyAndAssignedInForeachInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent;

                    public MyClass(int[] values)
                    {
                        foreach(var value in values)
                        {
                            MyEvent = (sender, args) => { };
                        }
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(16, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfEventIsReadOnlyAndAssignedInForInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent;

                    public MyClass(int number)
                    {
                        for(int i = 0; i < number; i++)
                        {
                            MyEvent = (sender, args) => { };
                        }
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(16, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfEventIsReadOnlyAndAssignedInWhileInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent;

                    public MyClass(int number)
                    {
                        while(number > 0)
                        {
                            MyEvent = (sender, args) => { };
                            number--;
                        }
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(17, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfEventIsReadOnlyAndAssignedAfterReturnInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent;

                    public MyClass(bool returnEarly)
                    {
                        if(returnEarly)
                        {
                            return;
                        }
                        MyEvent = (sender, args) => { };
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(17, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfEventIsReadOnlyAndAssignedToNullRegularAssignmentOnFieldDeclaration()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent = null;

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(8, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfEventIsReadOnlyAndAssignedToNullInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent;

                    public MyClass(bool returnEarly)
                    {
                        MyEvent = null;
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(13, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfEventIsReadOnlyAndAssignedToNullAfterRegularAssignmentInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent;

                    public MyClass(bool returnEarly)
                    {
                        MyEvent = (sender, args) => { };
                        MyEvent = null;
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(14, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfEventIsReadOnlyAndAssignedInAllSwitchCasesButNoDefaultInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent;

                    public MyClass(int value)
                    {
                        switch(value)
                        {
                            case 1: MyEvent = (sender, args) => { }; break;
                            case 2: MyEvent = (sender, args) => { }; break;
                        }
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(17, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfEventIsReadOnlyAndAssignedInASwitchCaseButNotAllInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent;

                    public MyClass(int value)
                    {
                        switch(value)
                        {
                            case 1: MyEvent = (sender, args) => { }; break;
                            default: break;
                        }
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(17, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void NotWarningIfEventIsReadOnlyAndAssignedInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent;

                    public MyClass()
                    {
                        MyEvent = (sender, args) => { };
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
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
            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(6, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));
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

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(13, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

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

            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(15, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "MyEvent"));

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
        public async void NotWarningIfEventIsReadOnlyWithInitializer()
        {
            const string test = @"
                public class MyClass
                {
                    public readonly System.Action MyAction = () => { };
                    public void Execute()
                    {
                        MyAction();
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
            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(10, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "action"));
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
            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(8, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "action"));
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
            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(6, 25)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "action"));
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
        public async void IgnoreIfAlreadyVerifiedForNotNullOnGrandparentIf()
        {
            var test = @"
public static void Execute(System.Action action)
{
    if (action != null)
    {
        if (1 > 0)
        {
            action();
        }
    }
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void IgnoreIfAlreadyVerifiedForNotNullWithNullOnRight()
        {
            var test = @"
public static void Execute(System.Action action)
{
    if (action != null)
        action();
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void IgnoreIfAlreadyVerifiedForNotNullWithNullOnLeft()
        {
            var test = @"
public static void Execute(System.Action action)
{
    if (null != action)
        action();
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void IgnoreIfAlreadyVerifiedForNullCreatesDiagnostic()
        {
            var test = @"
public static void Execute(System.Action action)
{
    if (null == action)
        action();
}".WrapInCSharpClass();
            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(12, 9)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "action"));
            await VerifyCSharpDiagnosticAsync(test, expected);
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
            var expected = new DiagnosticResult(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(11, 12)
                .WithMessage(string.Format(UseInvokeMethodToFireEventAnalyzer.MessageFormat, "getter"));
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WhenMethodInvokedWithNonReferenceTypeHasOnlyIfNullDiagnostic()
        {
            var test = @"
                public static TReturn Method<T, TReturn>(System.Func<T, TReturn> getter) where TReturn : struct
                {
                    return getter?.Invoke(default(T)) ?? default(TReturn);
                }".WrapInCSharpClass();
            await VerifyCSharpHasNumberOfCodeActionsAsync(test, 1);
        }

        public static TReturn Method<T, TReturn>(System.Func<T, TReturn> getter) where TReturn : struct
        {
            getter?.Invoke(default(T));
            return getter?.Invoke(default(T)) ?? default(TReturn);
        }

        [Fact]
        public async void FixWithInvokeWithNonReferenceType()
        {
            var source = @"
                public static TReturn Method<T, TReturn>(System.Func<T, TReturn> getter) where T : System.Attribute where TReturn : struct
                {
                    return getter(default(T));
                }".WrapInCSharpClass();
            var fix = @"
                public static TReturn Method<T, TReturn>(System.Func<T, TReturn> getter) where T : System.Attribute where TReturn : struct
                {
                    return getter?.Invoke(default(T)) ?? default(TReturn);
                }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fix, codeFixIndex: 0);
        }

        [Fact]
        public async void FixWithCheckForNullWithNonReferenceType()
        {
            var source = @"
                public static TReturn Method<T, TReturn>(System.Func<T, TReturn> getter) where T : System.Attribute where TReturn : struct
                {
                    return getter(default(T));
                }".WrapInCSharpClass();
            var fix = @"
                public static TReturn Method<T, TReturn>(System.Func<T, TReturn> getter) where T : System.Attribute where TReturn : struct
                {
                    var handler = getter;
                    if (handler != null)
                        return handler(default(T));
                }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fix, codeFixIndex: 1, allowNewCompilerDiagnostics: true);
        }

        [Fact]
        public async void FixWithCheckForNullAndKeepCommentsWhenReplacedWithCodeFix()
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
            await VerifyCSharpFixAsync(source, fixtest, 1);
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

            await VerifyCSharpFixAsync(source, fixtest, 1);
        }

        [Fact]
        public async void OnlyOneFixIfExpressionBodied()
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
            await VerifyCSharpHasNumberOfCodeActionsAsync(test, 1);
        }

        [Fact]
        public async void FixWhenInsideExpressionWithInvoke()
        {
            var code = @"
public System.Func<string, bool> AllowInteraction { get; protected set; }
protected bool AllowedInteraction()
{
    if (!AllowInteraction("""") || 1 == System.DateTime.Now.Second)
    {
        return false;
    }
    return true;
}".WrapInCSharpClass();
            var fix = @"
public System.Func<string, bool> AllowInteraction { get; protected set; }
protected bool AllowedInteraction()
{
    if (!AllowInteraction?.Invoke("""") ?? default(bool) || 1 == System.DateTime.Now.Second)
    {
        return false;
    }
    return true;
}".WrapInCSharpClass();
            await VerifyCSharpFixAsync(code, fix);
        }

        [Fact]
        public async void FixWhenInsideABinaryExpressionWithPrecedenceWithInvoke()
        {
            var code = @"
void Foo(Func<bool> f)
{
    var b = true;
    if (b || f())
    {
    }
}".WrapInCSharpClass();
            var fix = @"
void Foo(Func<bool> f)
{
    var b = true;
    if (b || (f?.Invoke() ?? default(bool)))
    {
    }
}".WrapInCSharpClass();
            await VerifyCSharpFixAsync(code, fix);
        }

        [Fact]
        public async void FixWhenInsideExpressionWithCheckForNull()
        {
            var code = @"
public System.Func<string, bool> AllowInteraction { get; protected set; }
protected bool AllowedInteraction()
{
    if (!AllowInteraction("""") || 1 == System.DateTime.Now.Second)
    {
        return false;
    }
    return true;
}".WrapInCSharpClass();
            var fix = @"
public System.Func<string, bool> AllowInteraction { get; protected set; }
protected bool AllowedInteraction()
{
    var handler = AllowInteraction;
    if (handler != null)
        if (!handler("""") || 1 == System.DateTime.Now.Second)
        {
            return false;
        }
    return true;
}".WrapInCSharpClass();
            await VerifyCSharpFixAsync(code, fix, 1);
        }

        [Fact]
        public async void FixWhenInsideExpressionAndNameAlreadyExists()
        {
            var code = @"
public System.Func<string, bool> AllowInteraction { get; protected set; }
protected bool AllowedInteraction()
{
    var handler = 1;
    if (!AllowInteraction("""") || 1 == System.DateTime.Now.Second)
    {
        return false;
    }
    return true;
}".WrapInCSharpClass();
            var fix = @"
public System.Func<string, bool> AllowInteraction { get; protected set; }
protected bool AllowedInteraction()
{
    var handler = 1;
    var handler1 = AllowInteraction;
    if (handler1 != null)
        if (!handler1("""") || 1 == System.DateTime.Now.Second)
        {
            return false;
        }
    return true;
}".WrapInCSharpClass();
            await VerifyCSharpFixAsync(code, fix, 1);
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

            await VerifyCSharpFixAsync(source, fixtest, 1);
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
        public async void IgnoreIfAlreadyCheckedForNull()
        {
            const string test = @"
public static int Get(Func<int,int> method) {
    return method != null ? method(12345) : 0;
}";

            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInCSharpClass());
        }

        [Fact]
        public async void IgnoreIfInLogicalOrThatCheckedForNull()
        {
            const string test = @"
public class Foo
{
    private System.Func<bool> _filter;
    public void Bar()
    {
        var b = _filter == null || _filter();
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInCSharpClass());
        }

        [Fact]
        public async void IgnoreIfInLogicalAndThatCheckedForNotNull()
        {
            const string test = @"
public class Foo
{
    private System.Func<bool> _filter;
    public void Bar()
    {
        var b = _filter != null && _filter();
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInCSharpClass());
        }

        [Fact]
        public async void IgnoreIfInConstructorAndThatCheckedForNotNull()
        {
            //https://github.com/code-cracker/code-cracker/issues/926
            const string test = @"
public class Foo
{
    public Foo(System.Action action)
    {
        if (action == null)
            throw new System.ArgumentNullException();
        action();
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInCSharpClass());
        }
    }
}