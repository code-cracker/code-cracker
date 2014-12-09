using CodeCracker.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Style
{
    public class PropertyPrivateSetTests : CodeFixTest<PropertyPrivateSetAnalyzer, PropertyPrivateSetCodeFixProvider>
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

               private set {  }
            }
        }
    }";

            await VerifyCSharpFixAsync(test, expected);
        }
    }
}