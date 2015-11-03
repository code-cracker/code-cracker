using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design.InconsistentAccessibilityTests
{
    public partial class InconsistentAccessibilityTests : CodeFixVerifier
    {
        [Fact]
        public async Task ShouldFixInconsistentAccessibilityInBinaryOperatorParameterAsync()
        {
            var testCode = @"public class Money
    {
        public static string operator +(Money m, SomeClass s)
        {
            return string.Empty;
        }
    }

    internal class SomeClass
    {

    }";

            var fixedCode = @"public class Money
    {
        public static string operator +(Money m, SomeClass s)
        {
            return string.Empty;
        }
    }

    public class SomeClass
    {

    }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityInConversionOperatorParameterAsync()
        {
            var testCode = @"public class Money
    {
        public static explicit operator Money(SomeClass s)
        {
            return new Money();
        }
    }

    internal class SomeClass
    {

    }";

            var fixedCode = @"public class Money
    {
        public static explicit operator Money(SomeClass s)
        {
            return new Money();
        }
    }

    public class SomeClass
    {

    }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
