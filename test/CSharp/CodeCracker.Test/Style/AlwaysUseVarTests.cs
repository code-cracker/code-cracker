using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
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
        public async Task CreateDiagnosticsWhenAssigningValueWithSameDeclaringType()
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
                Id = DiagnosticId.AlwaysUseVar.ToDiagnosticId(),
                Message = "Use 'var' instead of specifying the type name.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

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
    }
}