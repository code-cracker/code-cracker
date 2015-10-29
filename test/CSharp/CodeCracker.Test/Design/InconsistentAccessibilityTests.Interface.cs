using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public partial class InconsistentAccessibilityTests : CodeFixVerifier
    {
        [Fact]
        public async Task ShouldFixInconsistentAccessibilityInBaseInterfaceAsync()
        {
            var testCode = @"interface InternalInterface { }
    public interface PublicInterface : InternalInterface {}";

            var fixedCode = @"public interface InternalInterface { }
    public interface PublicInterface : InternalInterface {}";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityInBaseInterfaceForNestedInterfaceAsync()
        {
            var testCode = @"public class ClassWithProtectedInterface
    {
        protected interface ProtectedInterface { }
    }

    public class PublicClass : ClassWithProtectedInterface
    {
        public interface InterfaceDerivingFromProtectedInterface : ProtectedInterface { }
    }";

            var fixedCode = @"public class ClassWithProtectedInterface
    {
        public interface ProtectedInterface { }
    }

    public class PublicClass : ClassWithProtectedInterface
    {
        public interface InterfaceDerivingFromProtectedInterface : ProtectedInterface { }
    }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
