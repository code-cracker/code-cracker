﻿using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class AbstractClassShouldNotHavePublicCtorTests :
        CodeFixVerifier<AbstractClassShouldNotHavePublicCtorsAnalyzer, AbstractClassShouldNotHavePublicCtorsCodeFixProvider>

    {
        [Fact]
        public async Task CreateDiagnosticWhenAnAbstractClassHavePublicConstructor()
        {
            const string test = @"
            abstract class Foo
            {
                public Foo() { /* .. */ }
            }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.AbstractClassShouldNotHavePublicCtors.ToDiagnosticId(),
                Message = "Constructor should not be public.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }


        [Fact]
        public async Task IgnoresPublicCtorInNonAbstractClasses()
        {
            const string test = @"
            class Foo
            {
                public Foo() { /* .. */ }
            }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresProtectedCtorInNonAbstractClasses()
        {
            const string test = @"
            abstract class Foo
            {
                protected Foo() { /* .. */ }
            }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresPrivateCtorInNonAbstractClasses()
        {
            const string test = @"
            abstract class Foo
            {
                private Foo() { /* .. */ }
            }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresCtorOfStructNestedInAbstractClasses()
        {
            const string test = @"
            public abstract class C
            {
                public struct S
                {
                    private int x;

                    public S(int x)
                    {
                        this.x = x;
                    }
                }
            }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task FixReplacesPublicWithProtectedModifierInAbstractClasses()
        {
            const string test = @"
            abstract class Foo
            {
                public Foo() { /* .. */ }
            }";

            const string fixtest = @"
            abstract class Foo
            {
                protected Foo() { /* .. */ }
            }";

            await VerifyCSharpFixAsync(test, fixtest);
        }
    }
}