using CodeCracker.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Usage
{
    public class UnusedParametersTests : CodeFixTest<UnusedParametersAnalyzer, UnusedParametersCodeFixProvider>
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
            return new DiagnosticResult
            {
                Id = DiagnosticId.UnusedParameters.ToDiagnosticId(),
                Message = string.Format(UnusedParametersAnalyzer.Message, parameterName),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, column) }
            };
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
    }
}