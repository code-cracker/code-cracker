using CodeCracker.Performance;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Performance
{
    public class MakeLocalVariablesConstWhenItIsPossibleTests : CodeFixTest<MakeLocalVariableConstWhenItIsPossibleAnalyzer, MakeLocalVariableConstWhenItIsPossibleCodeFixProvider>
    {
        [Fact]
        public async Task IgnoresConstantDeclarations()
        {
            var test = _(@"const int a = 10;");
            await VerifyCSharpHasNoDiagnosticsAsync(test);

        }

        [Fact]
        public async Task IgnoresDeclarationsWithNoInitializers()
        {
            var test = _(@"int a;");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresDeclarationsWithNonConstants()
        {
            var test = _(@"int a = GetValue();");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresDeclarationsWithReferenceTypes()
        {
            var test = _(@"Foo a = new Foo();");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresStringInterpolations()
        {
            var test = _(@"
            int a = 10; 
            var s = ""a value is \{a}"";");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresVariablesThatChangesValueOutsideDeclaration()
        {
            var test = _(@"int a = 10;a = 20;");

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task CreateDiagnosticsWhenAssigningAPotentialConstant()
        {
            var test = _(@"int a = 10;");
            var expected = new DiagnosticResult
            {
                Id = MakeLocalVariableConstWhenItIsPossibleAnalyzer.DiagnosticId,
                Message = "This variables can be made const.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticsWhenAssigningAPotentialConstantInAVarDeclaration()
        {
            var test = _(@"var a = 10;");
            
            var expected = new DiagnosticResult
            {
                Id = MakeLocalVariableConstWhenItIsPossibleAnalyzer.DiagnosticId,
                Message = "This variables can be made const.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticsWhenAssigningNullToAReferenceType()
        {
            var test = _(@"Foo a = null;");
            
            var expected = new DiagnosticResult
            {
                Id = MakeLocalVariableConstWhenItIsPossibleAnalyzer.DiagnosticId,
                Message = "This variables can be made const.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task FixMakesAVariableConstWhenDeclarationSpecifiesTypeName()
        {
            var test = _(@"int a = 10;");
            var expected = _(@"const int a = 10;");
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task FixMakesAVariableConstWhenDeclarationUsesVar()
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
            const string expected = @"
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
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task FixMakesAVariableConstWhenDeclarationUsesVarWithString()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                var a = """";
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
                const string a = """";
            }
        }
    }";
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

        class Fee {}
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

        class Fee {}
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

        public string _(string code)
        {
            return @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                " + code + @"
            }
        }
    }";


        }
    }
}