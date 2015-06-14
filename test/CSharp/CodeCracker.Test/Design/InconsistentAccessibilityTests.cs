using CodeCracker.CSharp.Design.InconsistentAccessibility;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public class InconsistentAccessibilityTests : CodeFixVerifier
    {
        [Theory]
        [InlineData("class","internal")]
        [InlineData("class","")]
        [InlineData("struct", "internal")]
        [InlineData("struct", "")]
        public async Task ShouldChangeAccessibilityWhenErrorInConstructor(string type, string dependedUponModfifier)
        {
            var sourceCode = @"
public " + type + @" Dependent
{
    public Dependent(int a, DependendedUpon d, string b)
    {
    }
}
" + dependedUponModfifier + @" class DependendedUpon
{
}";

            var fixedCode = @"
public " + type + @" Dependent
{
    public Dependent(int a, DependendedUpon d, string b)
    {
    }
}
public class DependendedUpon
{
}";

            await VerifyCSharpFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("public", "sealed", "public sealed")]
        [InlineData("public static", "sealed internal", "sealed public")]
        [InlineData("public", "private", "public")]
        [InlineData("public", "protected", "public")]
        [InlineData("public", "/* a */ protected /* b */ internal /* c */", "/* a */ public /* b */  /* c */")]
        [InlineData("protected virtual", "", "protected")]
        [InlineData("protected", "internal /* comment */", "protected /* comment */")]
        [InlineData("protected", "private", "protected")]
        [InlineData("protected internal", "", "protected internal")]
        [InlineData("protected internal", "  protected  ", "  protected internal  ")]
        [InlineData("protected internal", "internal", "protected internal")]
        [InlineData("internal protected", "private", "protected internal")]
        [InlineData("internal", "protected", "internal")]
        [InlineData("internal", "private", "internal")]
        public async Task ShouldChangeAccessibilityWhenErrrorInMethod(string methodModifier, string dependedUponModifier, string fixedDependedUponModifier)
        {
            var sourceCode = @"
public class Dependent
{
    " + methodModifier + @" void SomeMethod(DependendedUpon d)
    {
    }

    " + dependedUponModifier + @" class DependendedUpon
    {
    }
}";

             var fixedCode = @"
public class Dependent
{
    " + methodModifier + @" void SomeMethod(DependendedUpon d)
    {
    }

    " + fixedDependedUponModifier + @" class DependendedUpon
    {
    }
}";

            await VerifyCSharpFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("public", "internal", "public")]
        [InlineData("public", "", "public")]
        public async Task ShouldChangeAccessibilityWhenErrorInInterface(string interfaceAccessibilityModifier, string dependedUponModifier, string fixedDependedUponModifier)
        {
            var sourceCode = @"
" + interfaceAccessibilityModifier + @" interface Dependent
{
    void SomeMethod(DependendedUpon d);
}
" + dependedUponModifier + @" class DependendedUpon
{
}";

            var fixedCode = @"
" + interfaceAccessibilityModifier + @" interface Dependent
{
    void SomeMethod(DependendedUpon d);
}
" + fixedDependedUponModifier + @" class DependendedUpon
{
}";

            await VerifyCSharpFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenQualifiedNameIsUsedForParameterType()
        {
            const string sourceCode = @"
public class Dependent
{
    public Dependent(Dependent.DependendedUpon d)
    {
    }

    internal class DependendedUpon
    {
    }
}";

            const string fixedCode = @"
public class Dependent
{
    public Dependent(Dependent.DependendedUpon d)
    {
    }

    public class DependendedUpon
    {
    }
}";

            await VerifyCSharpFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenAliasQualifiedNameIsUsedForParameterType()
        {
            const string sourceCode = @"
public class Dependent
{
    public Dependent(global::DependendedUpon d)
    {
    }
}
internal class DependendedUpon
{
}";

            const string fixedCode = @"
public class Dependent
{
    public Dependent(global::DependendedUpon d)
    {
    }
}
public class DependendedUpon
{
}";

            await VerifyCSharpFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenUsingDelegateAsParameter()
        {
            const string sourceCode = @"
public class Dependent
{
    public Dependent(DependedUpon d)
    {
    }
}
delegate void DependedUpon(int a);";

            const string fixedCode = @"
public class Dependent
{
    public Dependent(DependedUpon d)
    {
    }
}
public delegate void DependedUpon(int a);";

            await VerifyCSharpFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenUsingEnumAsParameter()
        {
            const string sourceCode = @"
public class Dependent
{
    public Dependent(DependedUpon d)
    {
    }
}
enum DependedUpon {}";

            const string fixedCode = @"
public class Dependent
{
    public Dependent(DependedUpon d)
    {
    }
}
public enum DependedUpon {}";

            await VerifyCSharpFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenUsingInterfaceAsParameter()
        {
            const string sourceCode = @"
public class Dependent
{
    public Dependent(DependedUpon d)
    {
    }
}
interface DependedUpon {}";

            const string fixedCode = @"
public class Dependent
{
    public Dependent(DependedUpon d)
    {
    }
}
public interface DependedUpon {}";

            await VerifyCSharpFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenUsingGenericClassAsParameter()
        {
            const string sourceCode = @"
public class Dependent
{
    public Dependent(DependedUpon<int> d)
    {
    }
}
class DependedUpon<T> {}";

            const string fixedCode = @"
public class Dependent
{
    public Dependent(DependedUpon<int> d)
    {
    }
}
public class DependedUpon<T> {}";

            await VerifyCSharpFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityToAllPartialDeclarationsAsync()
        {
            const string sourceCode = @"
public class Dependent
{
    public Dependent(DependedUpon d)
    {
    }
}
partial class DependedUpon {}
class SomeClass {}
partial class DependedUpon {}";

            const string fixedCode = @"
public class Dependent
{
    public Dependent(DependedUpon d)
    {
    }
}
public partial class DependedUpon {}
class SomeClass {}
public partial class DependedUpon {}";

            await VerifyCSharpFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        protected override CodeFixProvider GetCodeFixProvider() => new InconsistentAccessibilityCodeFixProvider();
    }
}
