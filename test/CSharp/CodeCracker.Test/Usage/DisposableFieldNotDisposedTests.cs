using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class DisposableFieldNotDisposedTests : CodeFixVerifier<DisposableFieldNotDisposedAnalyzer, DisposableFieldNotDisposedCodeFixProvider>
    {
        [Fact]
        public async Task FieldNotDisposableDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingTheDisposablePatternItDoesNotCreateDiagnostic()
        {
            const string source = @"
using System;
using System.IO;
public class A : IDisposable
{
    private MemoryStream disposableField = new MemoryStream();
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    private void Dispose(bool disposing)
    {
        if (disposing)
            if (disposableField != null)
                disposableField.Dispose();
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenUsingTheDisposablePatternWithNullPropagationItDoesNotCreateDiagnostic()
        {
            const string source = @"
using System;
using System.IO;
public class A : IDisposable
{
    private MemoryStream disposableField = new MemoryStream();
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    private void Dispose(bool disposing)
    {
        if (disposing)
            disposableField?.Dispose();
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenAFieldThatImplementsIDisposableIsAssignedThroughAMethodCallCreatesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private D field = D.Create();
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(7, 23)
                .WithMessage(string.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenAFieldDeclarationIsNotAssignedDoesNotCreateDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private D field;
        }
        class D : IDisposable
        {
            public void Dispose() { }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenAFieldDeclarationHas2VariableItCreates2Diagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private D field1 = new D(), field2 = D.Create();
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            var expected1 = new DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(7, 23)
                .WithMessage(string.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field1"));
            var expected2 = new DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(7, 41)
                .WithMessage(string.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field2"));
            await VerifyCSharpDiagnosticAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task WhenAFieldThatImplementsIDisposableIsDispoedOnATypeThatIsNotDisposableCreatesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private D field = D.Create();
            public void Dispose()
            {
                field.Dispose();
            }
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(7, 23)
                .WithMessage(string.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenAFieldThatImplementsIDisposableIsNotDisposedCreatesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName : IDisposable
        {
            private D field = D.Create();
            public void Dispose()
            {
            }
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(7, 23)
                .WithMessage(string.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenAFieldThatImplementsIDisposableIsDisposedDoesNotCreateDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName : IDisposable
        {
            private D field = D.Create();
            public void Dispose()
            {
                field.Dispose();
            }
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenAFieldThatImplementsIDisposableIsDisposedWithThisDoesNotCreateDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName : IDisposable
        {
            private D field = D.Create();
            public void Dispose()
            {
                this.field.Dispose();
            }
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenAFieldThatImplementsIDisposableIsDisposedThroughImplicitImplementationDoesNotCreateDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName : IDisposable
        {
            private D field = D.Create();
            void IDisposable.Dispose()
            {
                field.Dispose();
            }
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WithStructCreatesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        struct TypeName
        {
            private D field = new D();
        }
        struct D : IDisposable
        {
            public void Dispose() { }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(7, 23)
                .WithMessage(string.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WithPartialClassCreatesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class partial TypeName : IDisposable { }
        class partial TypeName
        {
            private D field = new D();
            public void Dispose()
            {
            }
            public void Dispose() { }
        }
        class D : IDisposable
        {
            public void Dispose() { }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(8, 23)
                .WithMessage(string.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenAFieldThatImplementsIDisposableIsCallingIncorrectDisposeCreatesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName : IDisposable
        {
            private D field = D.Create();
            public void Dispose(bool arg)
            {
                field.Dispose(true);
            }
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
            public void Dispose(bool arg) { }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(7, 23)
                .WithMessage(string.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenAFieldThatImplementsIDisposableIsBeingDisposedNotOnCorrectDisposeCreatesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName : IDisposable
        {
            private D field = D.Create();
            public void Dispose(bool value)
            {
                field.Dispose();
            }
            public void Dispose() { }
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(7, 23)
                .WithMessage(string.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenAFieldThatImplementsIDisposableIsAssignedThroughAnObjectConstructionCreatesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private D field = new D();
        }
        class D : IDisposable
        {
            public void Dispose() { }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(7, 23)
                .WithMessage(string.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenAnIDisposableFieldIsAssignedThroughAMethodCallCreatesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private IDisposable field = D.Create();
        }
        class D : IDisposable
        {
            public static IDisposable Create() => new D();
            public void Dispose() { }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(7, 33)
                .WithMessage(string.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FixAFieldThatImplementsIDisposableAndIsAssignedThroughAMethodCallWithouthSimplifiedTypes()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private D field = D.Create();
        }
        class D : System.IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName : System.IDisposable
        {
            private D field = D.Create();

            public void Dispose()
            {
                field.Dispose();
            }
        }
        class D : System.IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAFieldThatImplementsIDisposableAndIsAssignedThroughAMethodCall()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private D field = D.Create();
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            const string fixtest = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName : IDisposable
        {
            private D field = D.Create();

            public void Dispose()
            {
                field.Dispose();
            }
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAFieldThatImplementsIDisposableAndIsAssignedThroughAMethodCallAndEnclosingClassHasBaseListOfInheritance()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class OtherClass { }
        class TypeName : OtherClass
        {
            private D field = D.Create();
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            const string fixtest = @"
    using System;
    namespace ConsoleApplication1
    {
        class OtherClass { }
        class TypeName : OtherClass, IDisposable
        {
            private D field = D.Create();

            public void Dispose()
            {
                field.Dispose();
            }
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAFieldThatImplementsIDisposableAndIsAssignedThroughAMethodCallAndDisposeMethodAlreadyExists()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private D field = D.Create();
            private D field2 = D.Create();

            public void Dispose()
            {
                field2.Dispose();//comment1
            }
        }
        class D : System.IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName : System.IDisposable
        {
            private D field = D.Create();
            private D field2 = D.Create();

            public void Dispose()
            {
                field2.Dispose();//comment1
                field.Dispose();
            }
        }
        class D : System.IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }


        [Fact]
        public async Task FixAFieldThatImplementsIDisposableAndIsAssignedThroughAMethodCallAndDisposeMethodAlreadyExistsAndEnclosingTypeImplementsIDipososable()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName : IDisposable
        {
            private D field = D.Create();
            private D field2 = D.Create();

            public void Dispose()
            {
                field2.Dispose();
            }
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            const string fixtest = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName : IDisposable
        {
            private D field = D.Create();
            private D field2 = D.Create();

            public void Dispose()
            {
                field2.Dispose();
                field.Dispose();
            }
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAFieldThatImplementsIDisposableAndIsAssignedThroughAMethodCallAndEnclosingTypeImplementsIDipososableButIsMissingDisposeMethod()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName : IDisposable
        {
            private D field = D.Create();
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            const string fixtest = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName : IDisposable
        {
            private D field = D.Create();

            public void Dispose()
            {
                field.Dispose();
            }
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAFieldThatImplementsIDisposableAndIsAssignedThroughObjectCreation()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private D field = new D();
        }
        class D : System.IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName : System.IDisposable
        {
            private D field = new D();

            public void Dispose()
            {
                field.Dispose();
            }
        }
        class D : System.IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }


        [Fact]
        public async Task FixWithDisposeMethodOnPartialClass()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        partial class TypeName
        {
            private D field = new D();
        }
        partial class TypeName : IDisposable
        {
            public void Dispose()
            {
            }
        }
        class D : System.IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            const string fixtest = @"
    using System;
    namespace ConsoleApplication1
    {
        partial class TypeName
        {
            private D field = new D();//add field.Dispose(); to the Dispose method on another file.
        }
        partial class TypeName : IDisposable
        {
            public void Dispose()
            {
            }
        }
        class D : System.IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task DisposableFieldOnAbstractClassWithAbstractDisposableDoesNotCreateDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        abstract class TypeName : IDisposable
        {
            private D field = D.Create();
            public abstract void Dispose();
        }
        class D : IDisposable
        {
            public static D Create() => new D();
            public void Dispose() { }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task AlreadyDisposedDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName : System.IDisposable
{
    private D field = new D();
    public void Dispose() => field.Dispose();
}
class D : System.IDisposable
{
    public void Dispose() { }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task StaticFieldDoesNotCreateDiagnostic()
        {
            const string source = @"
static class TypeName
{
    private static D d = new D();
}
class D : System.IDisposable
{
    public void Dispose() { }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
    }
}