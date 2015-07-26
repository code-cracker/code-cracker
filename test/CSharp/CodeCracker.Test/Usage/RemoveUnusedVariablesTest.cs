using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class RemoveUnusedVariablesTest : CodeFixVerifier
    {
        protected override CodeFixProvider GetCodeFixProvider() => new RemoveUnusedVariablesCodeFixProvider();

        [Fact]
        public async Task WhenVariableIsAssignedButItsValueIsNeverUsedShouldCreateDiagnostics()
        {
            const string source = @"
        class TypeName
        {
            public void Foo()
            {
                int a = 10;
                return;
            }
        }";
            const string fixtest = @"
        class TypeName
        {
            public void Foo()
            {
                return;
            }
        }";

            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenVariableIsCreatedButNeverUsedShouldCreateDiagnostics()
        {

            const string source = @"
        class TypeName
        {
            public void Foo()
            {
                int a;
                return;
            }
        }";
            const string fixtest = @"
        class TypeName
        {
            public void Foo()
            {
                return;
            }
        }";

            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenVariableWithinOfMethodDoesNotUsedShouldCreateDiagnostics()
        {
            const string source = @"
        class Name
        {
            public void NewFoo()
            {
                int number = 2;
            }
        }";
            const string fixtest = @"
        class Name
        {
            public void NewFoo()
            {
            }
        }";

            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenVariableInCatchDeclarationShouldCreateDiagnostics()
        {
            const string source = @"
        class Name
        {
            public void NewFoo()
            {
                try
                {
                }
                catch(Exception ex)
                {
                    throw;
                }
            }
        }";
            const string fixtest = @"
        class Name
        {
            public void NewFoo()
            {
                try
                {
                }
                catch(Exception)
                {
                    throw;
                }
            }
        }";

            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenVariableWithinOfConstructorDoesNotUsedShouldCreateDiagnostics()
        {
            const string source = @"
        class Name
        {
            public Name()
            {
                var foo = 2;
            }
        }";
            const string fixtest = @"
        class Name
        {
            public Name()
            {
            }
        }";


            await VerifyCSharpFixAsync(source, fixtest);
        }
    }
}
