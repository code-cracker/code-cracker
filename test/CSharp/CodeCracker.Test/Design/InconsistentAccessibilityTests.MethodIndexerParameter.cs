using System.Threading.Tasks;
using CodeCracker.CSharp.Design.InconsistentAccessibility;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    using Verify = CSharpCodeFixVerifier<EmptyAnalyzer, InconsistentAccessibilityCodeFixProvider>;

    public partial class InconsistentAccessibilityTests
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
    public {|CS0051:Dependent|}(int a, DependendedUpon d, string b)
    {
    }
}
" + dependedUponModfifier + (dependedUponModfifier.Length > 0 ? " " : "") + @"class DependendedUpon
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

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
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
        [InlineData("protected internal", "  protected  ", "protected internal")]
        [InlineData("protected internal", "internal", "protected internal")]
        [InlineData("internal protected", "private", "protected internal")]
        [InlineData("internal", "protected", "internal")]
        [InlineData("internal", "private", "internal")]
        public async Task ShouldChangeAccessibilityWhenErrrorInMethod(string methodModifier, string dependedUponModifier, string fixedDependedUponModifier)
        {
            var sourceCode = @"
public class Dependent
{
    " + methodModifier + @" void {|CS0051:SomeMethod|}(DependendedUpon d)
    {
    }

    " + dependedUponModifier + (dependedUponModifier.Length > 0 ? " " : "") + @"class DependendedUpon
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

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Theory]
        [InlineData("public", "internal", "public")]
        [InlineData("public", "", "public")]
        public async Task ShouldChangeAccessibilityWhenErrorInInterface(string interfaceAccessibilityModifier, string dependedUponModifier, string fixedDependedUponModifier)
        {
            var sourceCode = @"
" + interfaceAccessibilityModifier + @" interface Dependent
{
    void {|CS0051:SomeMethod|}(DependendedUpon d);
}
" + dependedUponModifier + (dependedUponModifier.Length > 0 ? " " : "") + @"class DependendedUpon
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

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenQualifiedNameIsUsedForParameterType()
        {
            const string sourceCode = @"
public class Dependent
{
    public {|CS0051:Dependent|}(Dependent.DependendedUpon d)
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

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenAliasQualifiedNameIsUsedForParameterType()
        {
            const string sourceCode = @"
public class Dependent
{
    public {|CS0051:Dependent|}(global::DependendedUpon d)
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

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenUsingDelegateAsParameter()
        {
            const string sourceCode = @"
public class Dependent
{
    public {|CS0051:Dependent|}(DependedUpon d)
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

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenUsingEnumAsParameter()
        {
            const string sourceCode = @"
public class Dependent
{
    public {|CS0051:Dependent|}(DependedUpon d)
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

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenUsingInterfaceAsParameter()
        {
            const string sourceCode = @"
public class Dependent
{
    public {|CS0051:Dependent|}(DependedUpon d)
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

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenUsingGenericClassAsParameter()
        {
            const string sourceCode = @"
public class Dependent
{
    public {|CS0051:Dependent|}(DependedUpon<int> d)
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

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityToAllPartialDeclarationsAsync()
        {
            const string sourceCode = @"
public class Dependent
{
    public {|CS0051:Dependent|}(DependedUpon d)
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

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenErrorInIndexerParameterAsync()
        {
            const string sourceCode = @"public class Dependent
{
    public int {|CS0055:this|}[int idx, DependedUpon dependedUpon]
    {
        get { return 0; }
        set { }
    }
}

class DependedUpon
{
}";

            const string fixedCode = @"public class Dependent
{
    public int this[int idx, DependedUpon dependedUpon]
    {
        get { return 0; }
        set { }
    }
}

public class DependedUpon
{
}";

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenErrorInIndexerParameterUsingQualifiedNameAsync()
        {
            const string sourceCode = @"public class Dependent
{
    public int {|CS0055:this|}[int idx, Dependent.DependedUpon dependedUpon]
    {
        get { return 0; }
        set { }
    }

    class DependedUpon
    {
    }
}";

            const string fixedCode = @"public class Dependent
{
    public int this[int idx, Dependent.DependedUpon dependedUpon]
    {
        get { return 0; }
        set { }
    }

    public class DependedUpon
    {
    }
}";

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldChangeAccessibilityWhenErrorInMoreThanOneIndexerParameterAsync()
        {
            const string sourceCode = @"public class Dependent
{
    public int {|CS0055:{|CS0055:this|}|}[int idx, Dependent.DependedUpon dependedUpon, DependedUpon2 dependedUpon2]
    {
        get { return 0; }
        set { }
    }

    class DependedUpon
    {
    }
}

class DependedUpon2
{
}";

            const string fixedCode = @"public class Dependent
{
    public int this[int idx, Dependent.DependedUpon dependedUpon, DependedUpon2 dependedUpon2]
    {
        get { return 0; }
        set { }
    }

    public class DependedUpon
    {
    }
}

public class DependedUpon2
{
}";

            await Verify.VerifyCodeFixAsync(sourceCode, fixedCode).ConfigureAwait(false);
        }
    }
}
