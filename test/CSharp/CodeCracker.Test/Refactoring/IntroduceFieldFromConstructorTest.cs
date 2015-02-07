using CodeCracker.Refactoring;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Refactoring
{
    public class IntroduceFieldFromConstructorTest : CodeFixTest<IntroduceFieldFromConstructorAnalyzer, IntroduceFieldFromConstructorCodeFixProvider>
    {
        [Fact]
        public async Task WhenConstructorParameterHasPrivateField()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int par;

            public TypeName(int par)
            {
               this.par = par;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenConstructorParameterHasPrivateReadOnlyField()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            private readonly int par;

            public TypeName(int par)
            {
               this.par = par;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenConstructorParameterHasAnyFieldAssign()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int myField;

            public TypeName(int par)
            {
               this.myField = par;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task ConstructorParameterWithPrivateField()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public TypeName(int par)
            {
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            private readonly int par;

            public TypeName(int par)
            {
               this.par = par;
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }


        [Fact]
        public async Task ConstructorParameterWithPrivateFieldWhenFieldParameterNameAlreadyExists()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int par;

            public TypeName(int par)
            {
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            private readonly int par1;
            private int par;

            public TypeName(int par)
            {
               this.par1 = par;
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }

    }



}
