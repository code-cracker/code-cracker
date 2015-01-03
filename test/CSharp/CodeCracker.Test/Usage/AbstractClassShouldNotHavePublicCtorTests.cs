using System.Threading.Tasks;
using CodeCracker.Usage;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Usage
{
    public class AbstractClassShouldNotHavePublicCtorTests :
        CodeFixTest<AbstractClassShouldNotHavePublicCtorsAnalyzer, AbstractClassShouldNotHavePublicCtorsCodeFixProvider>

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
                Id = AbstractClassShouldNotHavePublicCtorsAnalyzer.DiagnosticId,
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
