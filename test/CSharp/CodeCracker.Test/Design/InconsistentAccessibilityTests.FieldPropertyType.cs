using System.Threading.Tasks;
using CodeCracker.CSharp.Design.InconsistentAccessibility;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    using Verify = CSharpCodeFixVerifier<EmptyAnalyzer, InconsistentAccessibilityCodeFixProvider>;

    public partial class InconsistentAccessibilityTests
    {
		[Fact]
		public async Task ShouldFixInconsistentAccessibilityErrorInClassFieldTypeAsync()
        {
            const string testCode = @"public class Dependent
{
    public DependedUpon {|CS0052:field|};
}

class DependedUpon
{
}";
            const string fixedCode = @"public class Dependent
{
    public DependedUpon field;
}

public class DependedUpon
{
}";

            await Verify.VerifyCodeFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

		[Fact]
		public async Task ShouldFixInconsistentAccessibilityErrorInClassFieldTypeWhenQualifiedNameAsync()
        {
            const string testCode = @"public class Dependent
{
    internal Dependent.DependedUpon {|CS0052:field|};

	class DependedUpon
	{
	}
}";

            const string fixedCode = @"public class Dependent
{
    internal Dependent.DependedUpon field;

	internal class DependedUpon
	{
	}
}";

            await Verify.VerifyCodeFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInStructFieldTypeAsync()
        {
            const string testCode = @"public struct Dependent
{
    public DependedUpon {|CS0052:field|}, {|CS0052:field1|};
}

class DependedUpon
{
}";
            const string fixedCode = @"public struct Dependent
{
    public DependedUpon field, field1;
}

public class DependedUpon
{
}";

            await Verify.VerifyCodeFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInPropertyTypeAsync()
        {
            const string testCode = @"public class Dependent
{
    public DependedUpon {|CS0053:Property|}
    {
        get { return null; }
        set { }
    }
}

class DependedUpon
{
}";

            const string fixedCode = @"public class Dependent
{
    public DependedUpon Property
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
        public async Task ShouldFixInconsistentAccessibilityErrorInPropertyTypeWhenUsingAutoPropertyAsync()
        {
            const string testCode = @"public class Dependent
{
    public DependedUpon {|CS0053:Property|}
    {
        get;
        set;
    }
}

class DependedUpon
{
}";

            const string fixedCode = @"public class Dependent
{
    public DependedUpon Property
    {
        get;
        set;
    }
}

public class DependedUpon
{
}";

            await Verify.VerifyCodeFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
