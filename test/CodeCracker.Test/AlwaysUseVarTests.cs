using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class AlwaysUseVarTests
         : CodeFixTest<AlwaysUseVarAnalyzer, AlwaysUseVarCodeFixProvider>
    {
        [Fact]
        public void IgnoresConstantDeclarations()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                const int a = 10;
            }
        }
    }";

            VerifyCSharpHasNoDiagnostics(test);

        }

        [Fact]
        public void IgnoresVarDeclarations()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = 10;
            }
        }
    }";

            VerifyCSharpHasNoDiagnostics(test);

        }

        [Fact]
        public void IgnoresDeclarationsWithNoInitializers()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                int a = 10, b;
            }
        }
    }";

            VerifyCSharpHasNoDiagnostics(test);

        }

        [Fact]
        public void IgnoresDeclarationsWithNoIdentityConversions()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                IFee fee = new Fee();
            }
        }
        interface IFee{}
        class Fee: IFee {}
    }";

            VerifyCSharpHasNoDiagnostics(test);

        }

        [Fact]
        public void CreateDiagnosticsWhenAssigningValueWithSameDeclaringType()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                int a = 10;
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = AlwaysUseVarAnalyzer.DiagnosticId,
                Message = "Use 'var' instead specifying the type name.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void FixReplacesDeclaringTypeWithVarIdentifier()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
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
            public void Foo()
            {
                var a = 10;
            }
        }
    }";
            VerifyCSharpFix(test, expected);
        }
    }
}
