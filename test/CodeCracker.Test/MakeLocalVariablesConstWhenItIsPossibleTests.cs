using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class MakeLocalVariablesConstWhenItIsPossibleTests : CodeFixTest<MakeLocalVariableConstWhenItIsPossibleAnalyzer, MakeLocalVariableConstWhenItIsPossibleCodeFixProvider>
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
                int a;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresDeclarationsWithNonConstants()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                int a = GetValue();
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresDeclarationsWithReferenceTypes()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                Foo a = new Foo();
            }
        }
        class Foo {}
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresVariablesThatChangesValueOutsideDeclaration()
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
                a = 20;
            }
        }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task CreateDiagnosticsWhenAssigningAPotentialConstant()
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
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                Foo a = null;
            }
        }
        class Foo {}
    }";
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
                const int a = 10;
            }
        }
    }";
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
    }
}