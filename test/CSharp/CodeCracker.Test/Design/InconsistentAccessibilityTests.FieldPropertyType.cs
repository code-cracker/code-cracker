using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public partial class InconsistentAccessibilityTests : CodeFixVerifier
    {
		[Fact]
		public async Task ShouldFixInconsistentAccessibilityErrorInClassFieldTypeAsync()
        {
            const string testCode = @"public class Dependent
{
    public DependedUpon field;
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

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

		[Fact]
		public async Task ShouldFixInconsistentAccessibilityErrorInClassFieldTypeWhenQualifiedNameAsync()
        {
            const string testCode = @"public class Dependent
{
    internal Dependent.DependedUpon field;

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

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInStructFieldTypeAsync()
        {
            const string testCode = @"public struct Dependent
{
    public DependedUpon field, field1;
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

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInPropertyTypeAsync()
        {
            const string testCode = @"public class Dependent
{
    public DependedUpon Property
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

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInPropertyTypeWhenUsingAutoPropertyAsync()
        {
            const string testCode = @"public class Dependent
{
    public DependedUpon Property
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

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
