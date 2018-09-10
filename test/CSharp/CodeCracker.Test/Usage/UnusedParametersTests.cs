using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class UnusedParametersTests : CodeFixVerifier<UnusedParametersAnalyzer, UnusedParametersCodeFixProvider>
    {
        [Fact]
        public async Task MethodWithoutParametersDoesNotCreateDiagnostic()
        {
            const string source = @"
    class TypeName
    {
        public void Foo()
        {
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task UsedParameterDoesNotCreateDiagnostic2()
        {
            const string source = @"
using System.Globalization;
using System.Reflection;

namespace ClassLibrary1
{
    public class Class1
    {
        protected Class1() { }

        public static void SetDefaultThreadCulture(CultureInfo currentCulture, CultureInfo currentUICulture)
        {
            typeof(CultureInfo).InvokeMember(""s_userDefaultCulture"", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.SetField, null, null, new object[] { currentCulture }, CultureInfo.InvariantCulture);
            typeof(CultureInfo).InvokeMember(""s_userDefaultUICulture"", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.SetField, null, null, new object[] { currentUICulture }, CultureInfo.InvariantCulture);
        }
    }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task UsedParameterDoesNotCreateDiagnostic()
        {
            const string source = @"
    class TypeName
    {
        public int Foo(int a)
        {
            return a;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task UsedParameterWithVerbatimIdentifierDoesNotCreateDiagnostic()
        {
            var source = @"
public int Foo(int @a)
{
    return a;
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task UsedParameterUsedWithVerbatimIdentifierDoesNotCreateDiagnostic()
        {
            var source = @"
public int Foo(int a)
{
    return @a;
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task UsedParameterWithVerbatimIdentifierUsedWithVerbatimIdentifierDoesNotCreateDiagnostic()
        {
            var source = @"
public int Foo(int @a)
{
    return @a;
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task MethodWithoutStatementsCreatesDiagnostic()
        {
            const string source = @"
    class TypeName
    {
        public void Foo(int a)
        {
        }
    }";
            await VerifyCSharpDiagnosticAsync(source, CreateDiagnosticResult("a", 4, 25));
        }

        [Fact]
        public async Task IgnorePartialMethods()
        {
            const string source = @"
    partial class TypeName
    {
        public partial void Foo(int a)
        {
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FixUnusedParameter()
        {
            const string source = @"
    class TypeName
    {
        public void Foo(int a)
        {
        }
    }";
            const string fixtest = @"
    class TypeName
    {
        public void Foo()
        {
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task With2ParametersCreatesDiagnostic()
        {
            const string source = @"
    class TypeName
    {
        public int Foo(int a, int b)
        {
            return a;
        }
    }";
            await VerifyCSharpDiagnosticAsync(source, CreateDiagnosticResult("b", 4, 31));
        }

        [Fact]
        public async Task FixUnusedParameterWith2Parameters()
        {
            const string source = @"
    class TypeName
    {
        public int Foo(int a, int b)
        {
            return a;
        }
    }";
            const string fixtest = @"
    class TypeName
    {
        public int Foo(int a)
        {
            return a;
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task IgnoreOverrides()
        {
            const string source = @"
    class Base
    {
        public virtual int Foo(int a)
        {
            return a;
        }
    }
    class TypeName : Base
    {
        public override int Foo(int a)
        {
            throw new System.Exception();
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreMethodsThatImplementExplicitlyAnInterfaceMember()
        {
            const string source = @"
    interface IBase
    {
        int Foo(int a);
    }
    class TypeName : IBase
    {
        int IBase.Foo(int a)
        {
            throw new System.Exception();
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreMethodsThatImplementAnInterfaceMember()
        {
            const string source = @"
    interface IBase
    {
        int Foo(int a);
    }
    class TypeName : IBase
    {
        public int Foo(int a)
        {
            throw new System.Exception();
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreMethodsThatMatchEventHandlerPattern()
        {
            const string source = @"
    using System;
    class TypeName
    {
        public void Foo(object sender, EventArgs args)
        {
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreMethodsThatMatchEventHandlerPatternWithDerivedEventArgs()
        {
            const string source = @"
    using System;
    class MyArgs : EventArgs { }
    class TypeName
    {
        public void Foo(object sender, MyArgs args) { }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DoNotIgnoreMethodsThatMatchEventHandlerPatternButDoesNotReturnVoid()
        {
            const string source = @"
    using System;
    class TypeName
    {
        public int Foo(object sender, EventArgs args)
        {
            throw new Exception();
        }
    }";
            await VerifyCSharpDiagnosticAsync(source,
                CreateDiagnosticResult("sender", 5, 24), CreateDiagnosticResult("args", 5, 39));
        }

        [Fact]
        public async Task ConstructorWithoutStatementsCreatesDiagnostic()
        {
            const string source = @"
    class TypeName
    {
        public TypeName(int a) { }
    }";
            await VerifyCSharpDiagnosticAsync(source, CreateDiagnosticResult("a", 4, 25));
        }

        [Fact]
        public async Task IgnoreSerializableConstructor()
        {
            const string source = @"
    using System.Runtime.Serialization;
    using System;
    [Serializable]
    public class MyObject : ISerializable
    {
        protected MyObject(SerializationInfo info, StreamingContext context) { }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DoNotIgnoreSerializableConstructorIfTypeDoesNotImplementISerializable()
        {
            const string source = @"
    using System.Runtime.Serialization;
    using System;
    [Serializable]
    public class MyObject
    {
        protected MyObject(SerializationInfo info, StreamingContext context) { }
    }";
            await VerifyCSharpDiagnosticAsync(source,
                CreateDiagnosticResult("info", 7, 28), CreateDiagnosticResult("context", 7, 52));
        }

        [Fact]
        public async Task DoNotIgnoreSerializableConstructorIfTypeDoesNotHaveSerializableAttribute()
        {
            const string source = @"
    using System.Runtime.Serialization;
    public class MyObject : ISerializable
    {
        protected MyObject(SerializationInfo info, StreamingContext context) { }
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context) { }
    }";
            await VerifyCSharpDiagnosticAsync(source,
                CreateDiagnosticResult("info", 5, 28), CreateDiagnosticResult("context", 5, 52));
        }

        public static DiagnosticResult CreateDiagnosticResult(string parameterName, int line, int column)
        {
            return new DiagnosticResult(DiagnosticId.UnusedParameters.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(line, column)
                .WithMessage(string.Format(UnusedParametersAnalyzer.Message, parameterName));
        }

        [Fact]
        public async Task FixWhenTheParametersHasReferenceOnSameClass()
        {
            const string source = @"
class TypeName
{
    public void IsReferencing()
    {
        Foo(1, 2);
    }
    public int Foo(int a, int b)
    {
        return a;
    }
}";
            const string fixtest = @"
class TypeName
{
    public void IsReferencing()
    {
        Foo(1);
    }
    public int Foo(int a)
    {
        return a;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixParams()
        {
            const string source = @"
class TypeName
{
    public void IsReferencing()
    {
        Foo(1, 2, 3, 4);
    }
    public void Foo(int a, int b, params int[] c)
    {
        a = b;
    }
}";
            const string fixtest = @"
class TypeName
{
    public void IsReferencing()
    {
        Foo(1, 2);
    }
    public void Foo(int a, int b)
    {
        a = b;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixParamsWhenNotInUse()
        {
            const string source = @"
class TypeName
{
    public void IsReferencing()
    {
        Foo(1, 2);
    }
    public void Foo(int a, int b, params int[] c)
    {
        a = b;
    }
}";
            const string fixtest = @"
class TypeName
{
    public void IsReferencing()
    {
        Foo(1, 2);
    }
    public void Foo(int a, int b)
    {
        a = b;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAllInSameClass()
        {
            const string source = @"
class TypeName
{
    public void IsReferencing()
    {
        Foo(1, 2, 3, 4);
    }
    public void Foo(int a, int b, params int[] c)
    {
        a = 1;
    }
}";
            const string fixtest = @"
class TypeName
{
    public void IsReferencing()
    {
        Foo(1);
    }
    public void Foo(int a)
    {
        a = 1;
    }
}";
            await VerifyCSharpFixAllAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAllInDifferentClass()
        {
            const string source1 = @"
class TypeName
{
    public void IsReferencing()
    {
        Referenced.Foo(1, 2, 3, 4);
    }
}";
            const string source2 = @"
class Referenced
{
    public static void Foo(int a, int b, params int[] c)
    {
        a = 1;
    }
}";
            const string fix1 = @"
class TypeName
{
    public void IsReferencing()
    {
        Referenced.Foo(1);
    }
}";
            const string fix2 = @"
class Referenced
{
    public static void Foo(int a)
    {
        a = 1;
    }
}";
            await VerifyCSharpFixAllAsync(new[] { source1, source2 }, new[] { fix1, fix2 });
        }

        [Fact]
        public async Task FixWhenTheParametersHasReferenceOnDifferentClass()
        {
            const string source = @"
class HasRef
{
    public void IsReferencing()
    {
        new TypeName().Foo(1, 2);
    }
}
class TypeName
{
    public int Foo(int a, int b)
    {
        return a;
    }
}";
            const string fixtest = @"
class HasRef
{
    public void IsReferencing()
    {
        new TypeName().Foo(1);
    }
}
class TypeName
{
    public int Foo(int a)
    {
        return a;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenTheParametersHasReferenceOnDifferentClassOnStaticMethod()
        {
            const string source = @"
class HasRef
{
    public void IsReferencing()
    {
        TypeName.Foo(1, 2);
    }
}
class TypeName
{
    public static int Foo(int a, int b)
    {
        return a;
    }
}";
            const string fixtest = @"
class HasRef
{
    public void IsReferencing()
    {
        TypeName.Foo(1);
    }
}
class TypeName
{
    public static int Foo(int a)
    {
        return a;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenTheParametersHasReferenceOnConstructor()
        {
            const string source = @"
class HasRef
{
    public void IsReferencing()
    {
        new TypeName(1, 2);
    }
}
class TypeName
{
    public TypeName(int a, int b)
    {
        a = 1;
    }
}";
            const string fixtest = @"
class HasRef
{
    public void IsReferencing()
    {
        new TypeName(1);
    }
}
class TypeName
{
    public TypeName(int a)
    {
        a = 1;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task CallToBaseDoesNotCreateDiagnostic()
        {
            const string source = @"
class Base
{
  protected Base(int a) { a = 1; }
}
class Derived : Base
{
  Derived(int a) : base(a) { }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task CallToBaseWithExpressionDoesNotCreateDiagnostic()
        {
            const string source = @"
class Base
{
  protected Base(int a) { a = 1; }
}
class Derived : Base
{
  Derived(int a) : base(a + 1) { }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task CallWithRefPeremeterDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    bool TryParse(string input, ref int output)
    {
        try
        {
            output = int.Parse(input);
            return true;
        }
        catch
        {
            return false;
        }
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
        [Fact]
        public async Task CallWithUnusedRefPeremeterCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    bool TryParse(string input, ref int output, ref int out2)
    {
        try
        {
            output = int.Parse(input);
            return true;
        }
        catch
        {
            return false;
        }
    }
}";

            await VerifyCSharpDiagnosticAsync(source, CreateDiagnosticResult("out2", 4, 49));
        }

        [Fact]
        public async Task CallWithOutPeremeterDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    bool TryParse(string input, out int output)
    {
        try
        {
            output = int.Parse(input);
            return true;
        }
        catch
        {
            return false;
        }
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
        [Fact]
        public async Task CallWithUnusedOutPeremeterCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    bool TryParse(string input, out int output, out int out2)
    {
        try
        {
            output = int.Parse(input);
            return true;
        }
        catch
        {
            return false;
        }
    }
}";

            await VerifyCSharpDiagnosticAsync(source, CreateDiagnosticResult("out2", 4, 49));
        }

        [Fact]
        public async Task CallWithUnusedParameterExtensionMethodNoDiagnostic()
        {
            const string source = @"
static class C
{
    private static void Bar()
    {
        """".Foo();
    }
    private static void Foo(this string s)
    {
        s += """";
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task CallWithUnusedParameterExtensionMethodCreateDiagnostic()
        {
            const string source = @"
static class C
{
    private static void Bar()
    {
        """".Foo(1);
    }
    private static void Foo(this string s, int i)
    {
        s += """";
    }
}";
            await VerifyCSharpDiagnosticAsync(source,CreateDiagnosticResult("i", 8, 44));
        }

        [Fact]
        public async Task CallWithUnusedParameterExtensionMethodFix()
        {
            const string source = @"
static class C
{
    private static void Bar()
    {
        """".Foo(1);
    }
    private static void Foo(this string s, int i)
    {
        s += """";
    }
}";

            const string fixtest = @"
static class C
{
    private static void Bar()
    {
        """".Foo();
    }
    private static void Foo(this string s)
    {
        s += """";
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task CallWithUnusedTwoParameterExtensionMethodFix()
        {
            const string source = @"
static class C
{
    private static void Bar()
    {
        """".Foo(1,""a"");
    }
    private static void Foo(this string s, int i, string j)
    {
        s += i.ToString();
    }
}";

            const string fixtest = @"
static class C
{
    private static void Bar()
    {
        """".Foo(1);
    }
    private static void Foo(this string s, int i)
    {
        s += i.ToString();
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAllWithNamedParametersInSameClass()
        {
            const string source = @"
public static class C
{
    public static void IsReferencing()
    {
        Foo(b: 2, a: 1);
    }
    public static void Foo(int a, int b)
    {
        a = 1;
    }
}";
            const string fixtest = @"
public static class C
{
    public static void IsReferencing()
    {
        Foo(a: 1);
    }
    public static void Foo(int a)
    {
        a = 1;
    }
}";
            await VerifyCSharpFixAllAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenUsedAsMethodGroupDoesNotCreateDiagnostic()
        {
            const string source = @"
public class TypeName
{
    static void FireHandler(System.Func<int, int> getInt) => getInt?.Invoke(1);
    static void Init() => FireHandler(OnConfigFileChanged);
    static int OnConfigFileChanged(int i)
    {
        return 1;
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ExpressionBodiedMethodCreatesDiagnostic()
        {
            const string source = @"
public class TypeName
{
    static int Foo(int i) => 1;
}";
            await VerifyCSharpDiagnosticAsync(source, CreateDiagnosticResult("i", 4, 20));
        }

        [Fact]
        public async Task FixExpressionBodiedMethod()
        {
            const string source = @"
public class TypeName
{
    static int Foo(int i) => 1;
}";
            const string fixtest = @"
public class TypeName
{
    static int Foo() => 1;
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        /// <summary>
        ///        Virtual methods should be ignored by the analyzer, because variables don't need
        ///        to be actually used by the base class and still serve a legit purpose.
        /// </summary>
        [Fact]
        public async Task VirtualMethodsShouldBeIgnored()
        {

            const string source = @"
public class BaseClass
{
    protected virtual void PreProcess(string data)
    {
        // no real action in base class
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
    }
}