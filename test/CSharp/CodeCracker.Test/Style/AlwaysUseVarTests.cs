using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class AlwaysUseVarTests : CodeFixVerifier<AlwaysUseVarAnalyzer, AlwaysUseVarCodeFixProvider>
    {

        [Fact]
        public async Task IgnoresConstantDeclarations()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                const int a = 10;
            }
        }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);

        }

        [Fact]
        public async Task IgnoresVarDeclarations()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                var a = 10;
            }
        }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);

        }

        [Fact]
        public async Task IgnoresDeclarationsWithNoInitializers()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                int a = 10, b;
            }
        }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);

        }

        [Fact]
        public async Task IgnoresDeclarationsWithNoIdentityConversions()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                IFee fee = new Fee();
            }
        }
        interface IFee{}
        class Fee: IFee {}
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);

        }

        [Fact]
        public async Task IgnoresDeclarationsWithDynamicVaribleName()
        {
            const string test = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                dynamic fee = Fee();
            }
            object Fee() { return 42; }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        [Fact]
        public async Task DeclarationsWithDynamicVaribleNameWithInitializerAlsoDynamicCreatesDiagnosic()
        {
            const string test = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                dynamic fee = Fee();
            }
            dynamic Fee() { return 42; }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.AlwaysUseVar.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(9, 17)
                .WithMessage("Use 'var' instead of specifying the type name.");
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Theory]
        [InlineData("int")]
        [InlineData("System.Int32")]
        public async Task CreateDiagnosticsWhenAssigningValueWithSameDeclaringTypePrimitive(string value)
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                " + value + @" a = 10;
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.AlwaysUseVarOnPrimitives.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(10, 17)
                .WithMessage("Use 'var' instead of specifying the type name.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Theory]
        [InlineData("string a = \"10\";")]
        [InlineData("DateTime date1 = new DateTime(2013, 6, 1, 12, 32, 30);")]
        public async Task CreateDiagnosticsWhenAssigningValueWithSameDeclaringTypeNonPrimitive(string value)
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                " + value + @"
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.AlwaysUseVar.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(10, 17)
                .WithMessage("Use 'var' instead of specifying the type name.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task FixReplacesDeclaringTypeWithVarIdentifier()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                int a = 10;
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                var a = 10;
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task FixReplacesDeclaringTypeWithVarIdentifierFixAll()
        {
            const string source1 = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                int a = 10;
                int b = 10;
            }
        }
    }";
            const string source2 = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                string a = ""10"";
                string b = ""10"";
            }
        }
    }";

            const string fixtest1 = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = 10;
                var b = 10;
            }
        }
    }";
            const string fixtest2 = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = ""10"";
                var b = ""10"";
            }
        }
    }";
            await VerifyCSharpFixAllAsync(new[] { source1, source2 }, new[] { fixtest1, fixtest2 });
        }


        [Fact]
        public async Task FixReplacesMultipleDeclarationWithMultipleVars()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                int a = 10, b = 12;
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                var a = 10;
                var b = 12;
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }



        [Fact]
        public async Task FixPreservesTriviaSensibly()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                int a = 10; //Blue

                /*variables for use*/ string /*desc of b*/b = /* why not*/ ""12"", /*Formatter does this My next variable*/ c /* is quite nice and it */ = ""23"";
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                var a = 10; //Blue

                /*variables for use*/ var /*desc of b*/b = /* why not*/ ""12"";
                var
/*Formatter does this My next variable*/ c /* is quite nice and it */ = ""23"";
            }
        }
    }";
            await VerifyCSharpFixAllAsync(test, expected);
        }
    }
}