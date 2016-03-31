using CodeCracker.CSharp.Design;
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
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
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
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
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
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
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
        public async void WarningIfEventIsReadOnlyButNotAssignedInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly int SomeOtherField;
                    readonly System.EventHandler MyEvent;

                    public MyClass()
                    {
                        SomeOtherField = 42;
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 25) }
            };

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

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 25) }
            };

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

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 25) }
            };

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

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 25) }
            };

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

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 25) }
            };

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

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 25) }
            };

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

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 25) }
            };

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

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 25) }
            };

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

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 25) }
            };

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

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 25) }
            };

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

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
                Message = "Copy the 'MyEvent' event to a variable before firing it.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 17, 25) }
            };

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
        public async void NotWarningIfEventIsReadOnlyAndAssignedInBlockInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent;

                    public MyClass(bool shouldAssign)
                    {
                        {
                            MyEvent = (sender, args) => { };
                        }
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void NotWarningIfEventIsReadOnlyAndAssignedInIfAndElseInConstructor()
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
                        else
                        {
                            MyEvent = (sender, args) => { };
                        }
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void NotWarningIfEventIsReadOnlyAndAssignedInAllSwitchCasesInConstructor()
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
                            default: MyEvent = (sender, args) => { }; break;
                        }
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void WarningIfEventIsReadOnlyAndReturnAfterAssignmentInConstructor()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent;

                    public MyClass(bool returnEarly)
                    {
                        MyEvent = (sender, args) => { };
                        if(returnEarly)
                        {
                            return;
                        }
                    }

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
                    }
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void NotWarningIfEventIsReadOnlyAndAssignedOnFieldDeclaration()
        {
            const string test = @"
                public class MyClass
                {
                    readonly System.EventHandler MyEvent = (sender, args) => { };

                    public void Execute()
                    {
                        MyEvent(this, System.EventArgs.Empty);
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

        [Fact]
        public async void FixWhenInsideExpression()
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
            await VerifyCSharpFixAsync(code, fix);
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
            await VerifyCSharpFixAsync(code, fix);
        }
    }
}