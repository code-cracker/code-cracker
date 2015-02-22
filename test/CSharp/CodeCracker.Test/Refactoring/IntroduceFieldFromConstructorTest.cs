using CodeCracker.CSharp.Refactoring;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class IntroduceFieldFromConstructorTest : CodeFixVerifier<IntroduceFieldFromConstructorAnalyzer, IntroduceFieldFromConstructorCodeFixProvider>
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
        public async Task FieldAlreadyExistsAndMatchesType()
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
            private readonly int par;

            public TypeName(int par)
            {
               this.par1 = par;
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
            private string par;

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
            private string par;

            public TypeName(int par)
            {
               this.par1 = par;
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task ConstructorParameterWithPrivateFieldWhenFieldParameterNameAlreadyExistsInSecondePosition()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            private string bar, par;

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
            private string bar, par;

            public TypeName(int par)
            {
               this.par1 = par;
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task ConstructorParameterWithPrivateFieldWhenFieldParameterNameAlreadyExistsTwice()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            private string par;
            private string par1;

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
            private readonly int par2;
            private string par;
            private string par1;

            public TypeName(int par)
            {
               this.par2 = par;
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }



        [Fact]
        public async Task IntroduceFieldConstructorFixAllInProject()
        {
            const string source1 = @"
                using System;
                class foo1
                {
                    public foo1(int a)
                    {
                    }
                }
                class foo2
                {
                    public foo2(int a, int b)
                    {
                    }
                }
";
            const string source2 = @"
                using system;
                class foo3
                {
                    private string bar;

                    public foo3(int bar)
                    {
                    }
                }
";
            const string source3 = @"
               using system;
               class foo4
               {
                   public foo4(int a, string b)
                   {
                   }
               }
            ";
            const string fixtest1 = @"
                using System;
                class foo1
                {
                    private readonly int a;

                    public foo1(int a) 
                    {
                        this.a = a;
                    }
                }
                class foo2
                {
                    private readonly int b;
                    private readonly int a;

                    public foo2(int a, int b) 
                    {
                        this.a = a;
                        this.b = b;
                    }
                }
";
            const string fixtest2 = @"
                using system;
                class foo3
                {
                    private readonly int bar1;
                    private string bar;

                    public foo3(int bar)
                    {
                        this.bar1 = bar;
                    }
                }
";

            const string fixtest3 = @"
                using system;
                class foo4
                {
                    private readonly string b;
                    private readonly int a;

                    public foo4(int a, string b)
                    {
                        this.a = a;
                        this.b = b;
                    }
                }
    ";

            await VerifyFixAllAsync(new[] { source1, source2, source3}, new[] { fixtest1, fixtest2, fixtest3});
        }
    }
}