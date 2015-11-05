using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design.InconsistentAccessibilityTests
{
    public partial class InconsistentAccessibilityTests : CodeFixVerifier
    {
        [Fact]
        public async Task ShouldFixReturnTypeInconsistentAccessibilityInDelegateThatIsNamespaceMemberAsync()
        {
            const string testCode = @"public delegate InternalClass DelegateThatIsNamespaceMember();
class InternalClass { }";

            const string fixedCode = @"public delegate InternalClass DelegateThatIsNamespaceMember();
public class InternalClass { }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixReturnTypeInconsistentAccessibilityInDelegateThatIsTypeMemberAsync()
        {
            const string testCode = @"public class ClassWithDelegateDeclaration
{
    public delegate InternalClass Delegate();
}
internal class InternalClass { }";

            const string fixedCode = @"public class ClassWithDelegateDeclaration
{
    public delegate InternalClass Delegate();
}
public class InternalClass { }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixParameterTypeInconsinstentAccessibilityInDelegateThatIsNamespaceMemberAsync()
        {
            const string testCode = @"public delegate void DelegateThatIsNamespaceMember(PublicClass.NestedInternalClass param);

public class PublicClass
{
    internal class NestedInternalClass { }
}";

            const string fixedCode = @"public delegate void DelegateThatIsNamespaceMember(PublicClass.NestedInternalClass param);

public class PublicClass
{
    public class NestedInternalClass { }
}";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixParameterTypeInconsinstentAccessibilityInDelegateThatIsTypeMemberAsync()
        {
            const string testCode = @"public class ClassWithDelegateDeclaration
{
    public delegate void Delegate(PublicClass.NestedInternalClass param);
}
public class PublicClass
{
    internal class NestedInternalClass { }
}";

            const string fixedCode = @"public class ClassWithDelegateDeclaration
{
    public delegate void Delegate(PublicClass.NestedInternalClass param);
}
public class PublicClass
{
    public class NestedInternalClass { }
}";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixParameterTypeInconsinstentAccessibilityInDelegateWhenContainingTypeHasInsufficientAccessibility()
        {
            const string testCode = @"public delegate void DelegateThatIsNamespaceMember(InternalClass.NestedPublicClass param);

internal class InternalClass
{
    public class NestedPublicClass { }
}";

            const string fixedCode = @"public delegate void DelegateThatIsNamespaceMember(InternalClass.NestedPublicClass param);

public class InternalClass
{
    public class NestedPublicClass { }
}";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
