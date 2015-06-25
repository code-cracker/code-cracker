using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public partial class InconsistentAccessibilityTests : CodeFixVerifier
    {
		[Fact]
		public async Task ShouldFixInconsistentAccessibilityErrorInClassFieldTypeAsync()
        {
            var testCode = @"public class Dependent
{
    public DependedUpon field;
}

class DependedUpon
{
}";
			var fixedCode = @"public class Dependent
{
    public DependedUpon field;
}

public class DependedUpon
{
}";

            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

		[Fact]
		public async Task ShouldFixInconsistentAccessibilityErrorInClassFieldTypeWhenQualifiedNameAsync()
        {
            var testCode = @"public class Dependent
{
    internal Dependent.DependedUpon field;

	class DependedUpon
	{
	}
}";

			var fixedCode = @"public class Dependent
{
    internal Dependent.DependedUpon field;

	internal class DependedUpon
	{
	}
}";

            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInStructFieldTypeAsync()
        {
            var testCode = @"public struct Dependent
{
    public DependedUpon field, field1;
}

class DependedUpon
{
}";
            var fixedCode = @"public struct Dependent
{
    public DependedUpon field, field1;
}

public class DependedUpon
{
}";

            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInPropertyTypeAsync()
        {
            var testCode = @"public class Dependent
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

            var fixedCode = @"public class Dependent
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

            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldFixInconsistentAccessibilityErrorInPropertyTypeWhenUsingAutoPropertyAsync()
        {
            var testCode = @"public class Dependent
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

            var fixedCode = @"public class Dependent
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

            await this.VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }
    }
}
