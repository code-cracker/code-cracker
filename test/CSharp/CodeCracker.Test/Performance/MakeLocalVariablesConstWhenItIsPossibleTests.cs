using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Performance
{
    public class MakeLocalVariablesConstWhenItIsPossibleTests : CodeFixVerifier<MakeLocalVariableConstWhenItIsPossibleAnalyzer, MakeLocalVariableConstWhenItIsPossibleCodeFixProvider>
    {
        [Fact]
        public async Task IgnoresConstantDeclarations()
        {
            var test = @"const int a = 10;".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);

        }

        [Fact]
        public async Task IgnoresDeclarationsWithNoInitializers()
        {
            var test = @"int a;".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresDeclarationsWithNonConstants()
        {
            var test = @"int a = GetValue();".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresDeclarationsWithReferenceTypes()
        {
            var test = @"Foo a = new Foo();".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresStringInterpolations()
        {
            var test = @"
            var s = $""a value is {""a""}"";".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresVariablesThatChangesValueOutsideDeclaration()
        {
            var test = @"int a = 10;a = 20;".WrapInCSharpMethod();

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresPointerDeclarations()
        {
            var test = @"void* value = null;".WrapInCSharpMethod();

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task CreateDiagnosticsWhenAssigningAPotentialConstant()
        {
            var test = @"int a = 10;".WrapInCSharpMethod();
            var expected = new DiagnosticResult(DiagnosticId.MakeLocalVariableConstWhenItIsPossible.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 13)
                .WithMessage("This variable can be made const.");
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticsWhenAssigningAPotentialConstantInAVarDeclaration()
        {
            var test = @"var a = 10;".WrapInCSharpMethod();

            var expected = new DiagnosticResult(DiagnosticId.MakeLocalVariableConstWhenItIsPossible.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 13)
                .WithMessage("This variable can be made const.");
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticsWhenAssigningNullToAReferenceType()
        {
            var test = @"Foo a = null;".WrapInCSharpMethod();

            var expected = new DiagnosticResult(DiagnosticId.MakeLocalVariableConstWhenItIsPossible.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 13)
                .WithMessage("This variable can be made const.");
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task IgnoresNullableVariables()
        {
            var test = "int? a = 1;".WrapInCSharpMethod();

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task FixMakesAVariableConstWhenDeclarationSpecifiesTypeName()
        {
            var test = @"int a = 10;".WrapInCSharpMethod();
            var expected = @"const int a = 10;".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task FixMakesAVariableConstWhenDeclarationUsesVar()
        {
            var test = @"var a = 10;".WrapInCSharpMethod();
            var expected = @"const int a = 10;".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task FixMakesAVariableConstWhenDeclarationUsesVarWithString()
        {
            var test = @"var a = """"".WrapInCSharpMethod();
            var expected = @"const string a = """"".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task FixMakesAVariableConstWhenSettingNullToAReferenceType()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                Fee a = null;
            }
        }

        class Fee {}
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                const Fee a = null;
            }
        }

        class Fee {}
    }";
            await VerifyCSharpFixAsync(test, expected);
        }


        [Fact]
        public async Task FixMakesAVariableConstWhenUsingVarAsAlias()
        {
            const string test = @"
    using System;
    using var = System.Int32;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                var a = 0;
            }
        }
    }";

            const string expected = @"
    using System;
    using var = System.Int32;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                const var a = 0;
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task FixMakesAVariableConstWhenUsingVarAsClass()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                //comment a
                var a = null;
                //comment b
            }
        }

        class var {}
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                //comment a
                const var a = null;
                //comment b
            }
        }

        class var {}
    }";
            await VerifyCSharpFixAsync(test, expected);
        }
    }
}