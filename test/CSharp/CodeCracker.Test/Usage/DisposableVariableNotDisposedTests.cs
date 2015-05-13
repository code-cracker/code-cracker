using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class DisposableVariableNotDisposedTests : CodeFixVerifier<DisposableVariableNotDisposedAnalyzer, DisposableVariableNotDisposedCodeFixProvider>
    {
        [Fact]
        public async Task VariableNotCreatedDoesNotCreateDiagnostic()
        {
            var source = "int i;".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task VariableNotDisposableDoesNotCreateDiagnostic()
        {
            var source = "new object();".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DisposableVariableCreatesDiagnostic()
        {
            var source = "new System.IO.MemoryStream();".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.DisposableVariableNotDisposed.ToDiagnosticId(),
                Message = string.Format(DisposableVariableNotDisposedAnalyzer.MessageFormat, "MemoryStream"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IgnoresDisposableObjectsCreatedWithUsingStatement()
        {
            var source = "using (new System.IO.MemoryStream()) { }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DisposableVariableDeclaredWithAnotherVariableCreatesOnlyOneDiagnostic()
        {

            var source = "System.IO.MemoryStream a, b = new System.IO.MemoryStream();".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.DisposableVariableNotDisposed.ToDiagnosticId(),
                Message = string.Format(DisposableVariableNotDisposedAnalyzer.MessageFormat, "MemoryStream"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 47) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task DisposableVariableWhenDisposedDoesNotCreateDiagnostic()
        {
            var source = @"System.IO.MemoryStream m;
m = new System.IO.MemoryStream();
m.Dispose();".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DisposableVariableWithDeclarationWhenDisposedDoesNotCreateDiagnostic()
        {
            var source = @"var m = new System.IO.MemoryStream();
m.Dispose();".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DisposableVariablePassedAsParamCreatesDiagnostic()
        {
            var source = "string.Format(\"\", new System.IO.MemoryStream());".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.DisposableVariableNotDisposed.ToDiagnosticId(),
                Message = string.Format(DisposableVariableNotDisposedAnalyzer.MessageFormat, "MemoryStream"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 35) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task DisposableVariableCallsIncorrectDisposeCreatesDiagnostic()
        {
            var source = @"var m = new System.IO.MemoryStream();
m.Dispose(true);".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.DisposableVariableNotDisposed.ToDiagnosticId(),
                Message = string.Format(DisposableVariableNotDisposedAnalyzer.MessageFormat, "MemoryStream"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 25) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task DisposableVariableCallsIncorrectDisposeSymbolCreatesDiagnostic()
        {
            const string source = @"
                class A
                {
                    void Foo()
                    {
                        var d = new Disposable();
                        d.Dispose();
                    }
                }
                class Disposable : System.IDisposable
                {
                    void System.IDisposable.Dispose() { }
                    public void Dispose() { }
                }
";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.DisposableVariableNotDisposed.ToDiagnosticId(),
                Message = string.Format(DisposableVariableNotDisposedAnalyzer.MessageFormat, "Disposable"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 33) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task DisposableVariableCallsIDisposableDisposeDirectlyDoesNotCreateDiagnostic()
        {
            const string source = @"
                class A
                {
                    void Foo()
                    {
                        var d = new Disposable();
                        ((System.IDisposable)d).Dispose();
                    }
                }
                class Disposable : System.IDisposable
                {
                    void System.IDisposable.Dispose() { }
                }
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DisposableVariableCallsOtherDisposableDisposeDirectlyCreatesDiagnostic()
        {
            const string source = @"
                class A
                {
                    void Foo()
                    {
                        var d = new Disposable();
                        ((System.IOtherDisposable)d).Dispose();
                    }
                }
                interface IOtherDiposable
                {
                    void Dispose();
                }
                class Disposable : System.IDisposable, IOtherDiposable
                {
                    void System.IDisposable.Dispose() { }
                    void IOtherDisposable.Dispose() { }
                }
";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.DisposableVariableNotDisposed.ToDiagnosticId(),
                Message = string.Format(DisposableVariableNotDisposedAnalyzer.MessageFormat, "Disposable"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 33) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task DisposableAssignedToFieldDoesNotCreateDiagnostic()
        {
            const string source = @"
                class A
                {
                    System.IO.MemoryStream field;
                    void Foo()
                    {
                        var m = new System.IO.MemoryStream();
                        field = m;
                    }
                }
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DisposableDeclaredOnFieldDoesNotCreateDiagnostic()
        {
            const string source = @"
                class A
                {
                    System.IO.MemoryStream field;
                    void Foo()
                    {
                        field = new System.IO.MemoryStream();
                    }
                }
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WithUsingDoesNotCreateDiagnostic()
        {
            var source = @"using (var m = new System.IO.MemoryStream()) { }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WithUsingAndAssignmentDoesNotCreateDiagnostic()
        {
            var source = @"System.IO.MemoryStream m;
using (m = new System.IO.MemoryStream()) { }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FixADisposableDeclarationWithoutDispose()
        {
            var source = @"var m = new System.IO.MemoryStream();".WrapInCSharpMethod();
            var fixtest = @"using (var m = new System.IO.MemoryStream())
{
}".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixADisposableDeclarationWithoutDisposeWithStatementsAfter()
        {
            var source = @"var m = new System.IO.MemoryStream();
Console.WriteLine(m);".WrapInCSharpMethod();
            var fixtest = @"using (var m = new System.IO.MemoryStream())
{
    Console.WriteLine(m);
}".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixADisposableDeclarationWithoutDisposeInsideBlock()
        {
            var source = @"if (DateTime.Now.Second % 2 == 0)
{
    var m = new System.IO.MemoryStream();
    Console.WriteLine(m);
}
Console.WriteLine(1);".WrapInCSharpMethod();
            var fixtest = @"if (DateTime.Now.Second % 2 == 0)
{
    using (var m = new System.IO.MemoryStream())
    {
        Console.WriteLine(m);
    }
}
Console.WriteLine(1);".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAnObjectCreationWithoutDispose()
        {
            var source = @"new System.IO.MemoryStream();".WrapInCSharpMethod();
            var fixtest = @"using (new System.IO.MemoryStream())
{
}".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixObjectCreationWithoutDisposeWithStatementsAfter()
        {
            var source = @"new System.IO.MemoryStream();
Console.WriteLine(1);".WrapInCSharpMethod();
            var fixtest = @"using (new System.IO.MemoryStream())
{
    Console.WriteLine(1);
}".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixObjectCreationWithoutDisposeInsideBlock()
        {
            var source = @"if (DateTime.Now.Second % 2 == 0)
{
    new System.IO.MemoryStream();
    Console.WriteLine(2);
}
Console.WriteLine(1);".WrapInCSharpMethod();
            var fixtest = @"if (DateTime.Now.Second % 2 == 0)
{
    using (new System.IO.MemoryStream())
    {
        Console.WriteLine(2);
    }
}
Console.WriteLine(1);".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAssignmentWithoutDispose()
        {
            var source = @"System.IO.MemoryStream m;
m = new System.IO.MemoryStream();".WrapInCSharpMethod();
            var fixtest = @"System.IO.MemoryStream m;
using (m = new System.IO.MemoryStream())
{
}".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAssignmentWithStatementsAfter()
        {
            var source = @"System.IO.MemoryStream m;
m = new System.IO.MemoryStream();
Console.WriteLine(m);".WrapInCSharpMethod();
            var fixtest = @"System.IO.MemoryStream m;
using (m = new System.IO.MemoryStream())
{
    Console.WriteLine(m);
}".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAssignmentInsideBlock()
        {
            var source = @"if (DateTime.Now.Second % 2 == 0)
{
    System.IO.MemoryStream m;
    m = new System.IO.MemoryStream();
    Console.WriteLine(m);
}
Console.WriteLine(1);".WrapInCSharpMethod();
            var fixtest = @"if (DateTime.Now.Second % 2 == 0)
{
    System.IO.MemoryStream m;
    using (m = new System.IO.MemoryStream())
    {
        Console.WriteLine(m);
    }
}
Console.WriteLine(1);".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAssignmentInsideBlockWithDifferentScopeInDeclarationAndAssignment()
        {
            var source = @"System.IO.MemoryStream m;
if (DateTime.Now.Second % 2 == 0)
{
    m = new System.IO.MemoryStream();
    Console.WriteLine(m);
}
Console.WriteLine(1);".WrapInCSharpMethod();
            var fixtest = @"System.IO.MemoryStream m;
if (DateTime.Now.Second % 2 == 0)
{
    using (m = new System.IO.MemoryStream())
    {
        Console.WriteLine(m);
    }
}
Console.WriteLine(1);".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAssignmentInsideBlockWithDifferentScopeInDeclarationAndAssignmentAndUseOnOutsideScope()
        {
            var source = @"System.IO.MemoryStream m;
if (DateTime.Now.Second % 2 == 0)
{
    m = new System.IO.MemoryStream();
    Console.WriteLine(m);
}
m.Flush();
Console.WriteLine(1);".WrapInCSharpMethod();
            var fixtest = @"System.IO.MemoryStream m;
if (DateTime.Now.Second % 2 == 0)
{
    m = new System.IO.MemoryStream();
    Console.WriteLine(m);
}
m.Flush();
Console.WriteLine(1);
m.Dispose();".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAssignmentInsideBlockWithDifferentScopeInDeclarationAndAssignmentAndUseOnOutsideScopeAndWithImplicitDispose()
        {
            const string source = @"
                using System;
                class A
                {
                    void Foo()
                    {
                        Disposable m;
                        if (DateTime.Now.Second % 2 == 0)
                        {
                            m = new Disposable();
                            Console.WriteLine(m);
                        }
                        m.Flush();
                        Console.WriteLine(1);
                    }
                }
                class Disposable : IDisposable
                {
                    void IDisposable.Dispose() { }
                    public void Flush() { }
                }
";
            const string fixtest = @"
                using System;
                class A
                {
                    void Foo()
                    {
                        Disposable m;
                        if (DateTime.Now.Second % 2 == 0)
                        {
                            m = new Disposable();
                            Console.WriteLine(m);
                        }
                        m.Flush();
                        Console.WriteLine(1);
                        ((IDisposable)m).Dispose();
                    }
                }
                class Disposable : IDisposable
                {
                    void IDisposable.Dispose() { }
                    public void Flush() { }
                }
";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAssignmentInsideBlockWithDifferentScopeInDeclarationWithImplicitDisposeAndNoExtraStatements()
        {
            const string source = @"
                using System;
                class A
                {
                    void Foo()
                    {
                        Disposable m;
                        if (DateTime.Now.Second % 2 == 0)
                        {
                            m = new Disposable();
                        }
                    }
                }
                class Disposable : IDisposable
                {
                    void IDisposable.Dispose() { }
                }
";
            const string fixtest = @"
                using System;
                class A
                {
                    void Foo()
                    {
                        Disposable m;
                        if (DateTime.Now.Second % 2 == 0)
                        {
                            using (m = new Disposable())
                            {
                            }
                        }
                    }
                }
                class Disposable : IDisposable
                {
                    void IDisposable.Dispose() { }
                }
";
            await VerifyCSharpFixAsync(source, fixtest);
        }


        [Fact]
        public async Task ExplicitlyDisposedObjectDoesNotCreateDiagnostic()
        {
            const string source = @"
                using System;
                class A
                {
                    void Foo()
                    {
                        Disposable m;
                        if (DateTime.Now.Second % 2 == 0)
                        {
                            m = new Disposable();
                        }
                        m.Flush();
                        ((IDisposable)m).Dispose();
                    }
                }
                class Disposable : IDisposable
                {
                    void IDisposable.Dispose() { }
                    void Flush() { }
                }
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FixAssignmentInsideBlockWithDifferentScopeInDeclarationAndAssignmentAndUseOnOutsideScopeAndWithImplicitDisposeAndDisjointFirstAndLastStatements()
        {
            const string source = @"
                using System;
                class A
                {
                    void Foo()
                    {
                        Disposable m = null;
                        if (DateTime.Now.Second % 2 == 0)
                        {
                            m = new Disposable();
                            Console.WriteLine(m);
                        }
                        m.Flush();
                        if (DateTime.Now.Second % 3 == 0)
                        {
                            Console.WriteLine(1);
                        }
                    }
                }
                class Disposable : IDisposable
                {
                    void IDisposable.Dispose() { }
                    public void Flush() { }
                }
";
            const string fixtest = @"
                using System;
                class A
                {
                    void Foo()
                    {
                        Disposable m = null;
                        if (DateTime.Now.Second % 2 == 0)
                        {
                            m = new Disposable();
                            Console.WriteLine(m);
                        }
                        m.Flush();
                        if (DateTime.Now.Second % 3 == 0)
                        {
                            Console.WriteLine(1);
                        } ((IDisposable)m).Dispose();
                    }
                }
                class Disposable : IDisposable
                {
                    void IDisposable.Dispose() { }
                    public void Flush() { }
                }
";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixConflictingScopesDescendingInTree()
        {
            const string source = @"
                using System;
                class A
                {
                    void Foo()
                    {
                        Disposable m = null;
                        if (DateTime.Now.Second % 2 == 0)
                        {
                            if (DateTime.Now.Second % 4 == 0)
                            {
                                m = new Disposable();
                                Console.WriteLine(m);
                            }
                            m.Flush();
                        }
                        if (DateTime.Now.Second % 3 == 0)
                        {
                            Console.WriteLine(1);
                        }
                    }
                }
                class Disposable : IDisposable
                {
                    void IDisposable.Dispose() { }
                    public void Flush() { }
                }
";
            const string fixtest = @"
                using System;
                class A
                {
                    void Foo()
                    {
                        Disposable m = null;
                        if (DateTime.Now.Second % 2 == 0)
                        {
                            if (DateTime.Now.Second % 4 == 0)
                            {
                                m = new Disposable();
                                Console.WriteLine(m);
                            }
                            m.Flush();
                        }
                        if (DateTime.Now.Second % 3 == 0)
                        {
                            Console.WriteLine(1);
                        } ((IDisposable)m).Dispose();
                    }
                }
                class Disposable : IDisposable
                {
                    void IDisposable.Dispose() { }
                    public void Flush() { }
                }
";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenVariableIsReturnedDoesNotCreateDiagnostic()
        {
            const string source = @"
                using System.IO;
                class A
                {
                    MemoryStream Foo()
                    {
                        var m = new MemoryStream();
                        return m;
                    }
                }
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FixAll()
        {
            const string source = @"
                using System;
                class A
                {
                    void Foo()
                    {
                        var d1 = new Disposable1();
                        var d2 = new Disposable2();
                    }
                }
                class Disposable1 : IDisposable
                {
                    public void Dispose() { }
                }
                class Disposable2 : IDisposable
                {
                    public void Dispose() { }
                }
";
            const string fixtest = @"
                using System;
                class A
                {
                    void Foo()
                    {
                        using (var d1 = new Disposable1())
                        {
                            using (var d2 = new Disposable2())
                            {
                            }
                        }
                    }
                }
                class Disposable1 : IDisposable
                {
                    public void Dispose() { }
                }
                class Disposable2 : IDisposable
                {
                    public void Dispose() { }
                }
";
            await VerifyCSharpFixAllAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAllInProject()
        {
            const string source1 = @"
                using System;
                class Disposable1 : IDisposable
                {
                    public void Dispose() { }
                }
                class Disposable2 : IDisposable
                {
                    public void Dispose() { }
                }
";
            const string source2 = @"
                class A
                {
                    void Foo()
                    {
                        var d1 = new Disposable1();
                        var d2 = new Disposable2();
                    }
                }
";
            const string source3 = @"
                class B
                {
                    void Foo()
                    {
                        var e1 = new Disposable1();
                        var e2 = new Disposable2();
                    }
                }
";
            const string fixtest1 = source1;
            const string fixtest2 = @"
                class A
                {
                    void Foo()
                    {
                        using (var d1 = new Disposable1())
                        {
                            using (var d2 = new Disposable2())
                            {
                            }
                        }
                    }
                }
";
            const string fixtest3 = @"
                class B
                {
                    void Foo()
                    {
                        using (var e1 = new Disposable1())
                        {
                            using (var e2 = new Disposable2())
                            {
                            }
                        }
                    }
                }
";
            await VerifyCSharpFixAllAsync(new[] { source1, source2, source3 }, new[] { fixtest1, fixtest2, fixtest3 });
        }
    }
}