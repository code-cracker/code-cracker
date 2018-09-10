using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class PropertyPrivateSetTests : CodeFixVerifier<PropertyPrivateSetAnalyzer, PropertyPrivateSetCodeFixProvider>
    {
        [Fact]
        public async Task PropertyPrivateDeclaration()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty { get; private set; }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task NotWarningPrivatePropertyDeclaration()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int MyProperty { get; set; }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WarningOnPublicPropertyDeclaration()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty { get; set; }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.PropertyPrivateSet.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(8, 13)
                .WithMessage(PropertyPrivateSetAnalyzer.MessageFormat);

            await VerifyCSharpDiagnosticAsync(test, expected);
        }


        [Fact]
        public async Task PropertyPrivateBrackets()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty
            {
               get { return 0; }
               private set {  }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task ExpressionBodiedPropertyDoesNotCreateDiagnostic()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty => 0;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task PropertyPrivateFixAutoProperty()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty { get; set; }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty { get; private set; }
        }
    }";

            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task PropertyProtectedFixAutoProperty()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty { get; set; }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty { get; protected set; }
        }
    }";

            await VerifyCSharpFixAsync(test, expected, 1);
        }

        [Fact]
        public async Task PropertyPrivateFixAutoPropertyBrackets()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty
            {
               get { return 0; }
               set {  }
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty
            {
               get { return 0; }
               private set
               {  }
            }
        }
    }";

            await VerifyCSharpFixAsync(test, expected, 0);
        }


        [Fact]
        public async Task PropertyProtectedFixAutoPropertyBrackets()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty
            {
               get { return 0; }
               set {  }
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int MyProperty
            {
               get { return 0; }
               protected set
               {  }
            }
        }
    }";

            await VerifyCSharpFixAsync(test, expected, 1);
        }

    [Fact]
    public async Task PropertyWithAttributeProtectedFix()
    {
        const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            [Obsolete(""this property is obsolete"")]
            public int MyProperty
            {
               get { return 0; }
               set {  }
            }
        }
    }";

        const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            [Obsolete(""this property is obsolete"")]
            public int MyProperty
            {
               get { return 0; }
               protected set
               {  }
            }
        }
    }";

        await VerifyCSharpFixAsync(test, expected, 1);
    }
}
}