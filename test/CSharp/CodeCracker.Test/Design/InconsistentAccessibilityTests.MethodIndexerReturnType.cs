using System.Threading.Tasks;
using CodeCracker.CSharp.Design.InconsistentAccessibility;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    using Verify = CSharpCodeFixVerifier<EmptyAnalyzer, InconsistentAccessibilityCodeFixProvider>;

    public partial class InconsistentAccessibilityTests
    {
        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInMethodReturnTypeAsync()
        {
            const string testCode = @"public class Dependent
{
    public DependedUpon {|CS0050:Method|}()
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

            await Verify.VerifyCodeFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInMethodReturnTypeWhenQualifiedNameAsync()
        {
            const string testCode = @"public class Dependent
{
    public Dependent.DependedUpon {|CS0050:Method|}()
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

            await Verify.VerifyCodeFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInIndexerReturnTypeAsync()
        {
            const string testCode = @"public class Dependent
{
    public DependedUpon {|CS0054:this|}[int a]
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

            await Verify.VerifyCodeFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInIndexerReturnTypeWhenQualifiedNameAsync()
        {
            const string testCode = @"public class Dependent
{
    public Dependent.DependedUpon {|CS0054:this|}[int a]
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

            await Verify.VerifyCodeFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
