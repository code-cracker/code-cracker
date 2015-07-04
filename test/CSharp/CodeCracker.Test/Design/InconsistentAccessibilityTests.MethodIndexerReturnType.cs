using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public partial class InconsistentAccessibilityTests : CodeFixVerifier
    {
        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInMethodReturnTypeAsync()
        {
            const string testCode = @"public class Dependent
{
    public DependedUpon Method()
    {
        return null;
    }
}

    internal class DependedUpon
    {
    }";
            const string fixedCode = @"public class Dependent
{
    public DependedUpon Method()
    {
        return null;
    }
}

    public class DependedUpon
    {
    }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInMethodReturnTypeWhenQualifiedNameAsync()
        {
            const string testCode = @"public class Dependent
{
    public Dependent.DependedUpon Method()
    {
        return null;
    }

    internal class DependedUpon
    {
    }
}";
            const string fixedCode = @"public class Dependent
{
    public Dependent.DependedUpon Method()
    {
        return null;
    }

    public class DependedUpon
    {
    }
}";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInIndexerReturnTypeAsync()
        {
            const string testCode = @"public class Dependent
{
    public DependedUpon this[int a]
    {
        get { return null; }
        set { }
    }
}

    internal class DependedUpon
    {
    }";
            const string fixedCode = @"public class Dependent
{
    public DependedUpon this[int a]
    {
        get { return null; }
        set { }
    }
}

    public class DependedUpon
    {
    }";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInIndexerReturnTypeWhenQualifiedNameAsync()
        {
            const string testCode = @"public class Dependent
{
    public Dependent.DependedUpon this[int a]
    {
        get { return null; }
        set { }
    }

    internal class DependedUpon
    {
    }
}";
            const string fixedCode = @"public class Dependent
{
    public Dependent.DependedUpon this[int a]
    {
        get { return null; }
        set { }
    }

    public class DependedUpon
    {
    }
}";

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
