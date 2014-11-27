using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;


namespace CodeCracker.Test
{
    public class NameOfTests : CodeFixTest<NameOfAnalyzer, AlwaysUseVarCodeFixProvider>
    {
        [Fact]
        public async Task IgnoreIfStringLiteralIsWhiteSpace()
        {
            var test = @"
        void Foo()
        {
            var whatever = """";
        }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreIfStringLiteralIsNull()
        {
            var test = @"
        void Foo()
        {
            var whatever = null;
        }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreIfClassHasNoParameters()
        {
            var test = @"
        public class Foo()
            public Foo()
            {
                string whatever = ""b"";
            }
        }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreIfMethodHasNoParameters()
        {
            var test = @"
        void Foo()
        {
            string whatever = ""b"";
        }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreIfMethodHasParametersUnlikeOfStringLiteral()
        {
            var test = @"
        void Foo(string a)
        {
            string whatever = ""b"";
        }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task EvaluateIfMethodHasParametersEqualOfStringLiteral()
        {
            var test = @"
    public class TypeName
    {
        void Foo(string b)
        {
            string whatever = ""b"";
        }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
    }
}
