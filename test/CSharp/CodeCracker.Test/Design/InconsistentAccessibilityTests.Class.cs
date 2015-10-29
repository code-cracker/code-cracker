using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public partial class InconsistentAccessibilityTests
    {
        [Fact]
        public async Task ShouldFixInconsistentAccessibilityInBaseClassAsync()
        {
            var testCode = @"class InternalClass { }
    public class PublicClass : InternalClass { }";

            var fixedCode = @"public class InternalClass { }
    public class PublicClass : InternalClass { }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityInBaseClassAsyncForNestedTypesAsync()
        {
            var testCode = @"public class PublicClass
    {
        private class NestedClass { }

        internal class ClassDerivingFromNestedClass : NestedClass { }
    }";
            var fixedCode = @"public class PublicClass
    {
        internal class NestedClass { }

        internal class ClassDerivingFromNestedClass : NestedClass { }
    }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
