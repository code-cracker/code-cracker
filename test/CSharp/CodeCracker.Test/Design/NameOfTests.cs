using CodeCracker.CSharp.Design;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public class NameOfTests : CodeFixVerifier<NameOfAnalyzer, NameOfCodeFixProvider>
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("", "null")]
        [InlineData("", "b")]
        [InlineData("string a", "b")]
        public async Task WhenStringLiteralInMethodShouldNotReportDiagnostic(string parameters, string stringLiteral)
        {
            var source = @"
public class TypeName
{
    void Foo(" + parameters + @")
    {
        var whatever = """ + stringLiteral + @""";
    }
}";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Theory]
        [InlineData("string b", "b", "b")]
        [InlineData("string @for", "for", "@for")]
        [InlineData("string @xyz", "xyz", "@xyz")]
        public async Task WhenStringLiteralInMethodShouldReportDiagnostic(string parameters, string stringLiteral, string nameofArgument)
        {
            var source = @"
public class TypeName
{
    void Foo(" + parameters + @")
    {
        var whatever = """ + stringLiteral + @""";
    }
}";

            var expected = CreateNameofDiagnosticResult(nameofArgument, 6, 24);

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IgnoreIfConstructorHasNoParameters()
        {
            const string test = @"
public class TypeName()
{
    public TypeName()
    {
        string whatever = ""b"";
    }
}";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreInVariableDeclaration()
        {
            const string test = @"
public class TypeName()
{
    public TypeName()
    {
        var @var = string.IsNullOrEmpty(""var"");
    }
}";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreOnPropertyDeclaration()
        {
            const string test = @"
public class TypeName
{
    public string MyProperty { get; set; } = ""MyProperty"";
}";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreNamesNotDeclaredYet()
        {
            const string test = @"
public class TypeName
{
    void Foo()
    {
        var str = ""x"";
        var x = 1;
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenUsingSomeStringInAttributeShouldNotReportDiagnostic()
        {
            const string test = @"
public class TypeName
{
    [Whatever(""a"")]
    [Whatever(""xyz""]
    void Foo(string a)
    {
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenReferencingExternalSymbolShouldReportDiagnostic()
        {
            const string test = @"
using System;
public class TypeName
{
    public static void Foo()
    {
        var a = ""DateTime"";
    }
}";
            var expected = CreateNameofDiagnosticResult("DateTime", 7, 17, DiagnosticId.NameOf_External);
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Theory]
        [InlineData("xyz", false)]
        [InlineData("OtherProperty", true)]
        [InlineData("SomeStruct", true)]
        [InlineData("readonlyField", true)]
        [InlineData("Property", true)]
        public async Task WhenUsingProgramElementNameStringAsIndexerParameter(string stringLiteral, bool shouldReportDiagnostic)
        {
            var source = @"
public class TypeName
{
    private readonly int readonlyField;
    public int OtherProperty { get; set; }
    public event EventHandler ParticularEvent;
    public delegate int SomeDelegate(int c, double d);

    public interface IInterface {}
    public struct SomeStruct {}
    public enum SomeEnum {}
    public class NestedClass {}

    public int Property
    {
        set
        {
            this[""" + stringLiteral + @"""] = value;
        }
    }

    public int this[string s]
    {
        get { return 0;}
        set { }
    }
}";
            if (!shouldReportDiagnostic)
            {
                await VerifyCSharpHasNoDiagnosticsAsync(source);
            }
            else
            {
                var expected = CreateNameofDiagnosticResult(stringLiteral, 18, 18);

                await VerifyCSharpDiagnosticAsync(source, expected);
            }
        }

        [Theory]
        [InlineData("xyz", false)]
        [InlineData("NestedClass", true)]
        [InlineData("SomeStruct", true)]
        [InlineData("SomeEnum", true)]
        [InlineData("IInterface", true)]
        [InlineData("N2", true)]
        [InlineData("SomeDelegate", true)]
        [InlineData("readonlyField", true)]
        [InlineData("ParticularEvent", true)]
        [InlineData("Property", true)]
        [InlineData("TypeName", true)]
        [InlineData("Invoke", true)]
        [InlineData("N1", true)]
        [InlineData("N3", true)]
        public async Task WhenUsingProgramElementNameStringInMethodInvocation(string stringLiteral, bool shouldReportDiagnostic)
        {
            var source = @"
namespace N1.N2
{
    namespace N3
    {
        public class TypeName
        {
            private readonly int readonlyField;
            public int Property { get; set; }
            public event EventHandler ParticularEvent;
            public delegate int SomeDelegate(int c, double d);

            public interface IInterface {}
            public struct SomeStruct {}
            public enum SomeEnum {}
            public class NestedClass {}

            public int Property
            {
                set
                {
                    Invoke(""abc"", """ + stringLiteral + @""");
                }
            }

            private void Invoke(string arg1, string arg2)
            {
            }
        }
    }
}";
            if (!shouldReportDiagnostic)
            {
                await VerifyCSharpHasNoDiagnosticsAsync(source);
            }
            else
            {
                var expected = CreateNameofDiagnosticResult(stringLiteral, 22, 35);

                await VerifyCSharpDiagnosticAsync(source, expected);
            }
        }

        [Fact]
        public async Task WhenUsingProgramElementStringInVariableAssignment()
        {
            const string source = @"
public class TypeName
{
    private readonly int readonlyField;
    public class NestedClass {}

    public int Property
    {
        set
        {
            string variable = ""NestedClass"";
            variable = ""xyz"";
            variable = ""readonlyField"";
        }
    }
}";

            var expectedForFirstAssignment = CreateNameofDiagnosticResult("NestedClass", 11, 31);
            var expectedForSecondAssignment = CreateNameofDiagnosticResult("readonlyField", 13, 24);

            await VerifyCSharpDiagnosticAsync(source, expectedForFirstAssignment, expectedForSecondAssignment);
        }

        [Fact]
        public async Task WhenUsingProgramElementNameStringInAttributeShouldReportDiagnostic()
        {
            const string source = @"
namespace N1.N2
{
    public class TypeName
    {
        private readonly int readonlyField;
        public int Property { get; set; }
        public event EventHandler ParticularEvent;
        public delegate int SomeDelegate(int c, double d);

        [Whatever(""N2"")]
        [Whatever(""SomeDelegate"")]
        [Whatever(""readonlyField"")]
        [Whatever(""ParticularEvent"")]
        [Whatever(""Property"")]
        [Whatever(""TypeName"")]
        [Whatever(""Foo"")]
        [Whatever(""N1"")]
        void Foo(string a)
        {
        }
    }
}";
            var expected = new[]
            {
                CreateNameofDiagnosticResult("N2", 11, 19),
                CreateNameofDiagnosticResult("SomeDelegate", 12, 19),
                CreateNameofDiagnosticResult("readonlyField", 13, 19),
                CreateNameofDiagnosticResult("ParticularEvent", 14, 19),
                CreateNameofDiagnosticResult("Property", 15, 19),
                CreateNameofDiagnosticResult("TypeName", 16, 19),
                CreateNameofDiagnosticResult("Foo", 17, 19),
                CreateNameofDiagnosticResult("N1", 18, 19)
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenUsingStringLiteralInObjectInitializer()
        {
            const string source = @"
namespace N1.N2
{
    public class OtherTypeName
    {
        public string Property { get; set; }
        private string Property2 { get; set; }
        public string Property3 { get; set; }
    }

    public class TypeName
    {

        void Foo(string a)
        {
            var instance = new OtherTypeName
            {
                Property = ""xyz"",
                Property2 = ""OtherTypeName"",
                Property3 = ""Property2""
            }
        }
    }
}";

            await VerifyCSharpDiagnosticAsync(source, CreateNameofDiagnosticResult("OtherTypeName", 19, 29));
        }

        [Fact]
        public async Task WhenUsingProgramElementNameInArrayInitializer()
        {
            const string source = @"
public class TypeName
{
    private readonly int readonlyField;
    public interface IInterface {}

    void Foo(string a)
    {
        var tab = new[] { ""readonlyField"", ""xyz"", ""IInterface"" };
    }
}";
            var expected = new[]
            {
                CreateNameofDiagnosticResult("readonlyField", 9, 27),
                CreateNameofDiagnosticResult("IInterface", 9, 51)
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenCreatingNewObjectWithStringLiterals()
        {
            const string source = @"
public struct TypeName
{

    void Foo(string a)
    {
        var instance = new OtherTypeName(""b"", ""xyz"");
        instance2 = new OtherTypeName(""TypeName"", ""a"");
    }
}";
            var expected = new[]
            {
                CreateNameofDiagnosticResult("TypeName", 8, 39),
                CreateNameofDiagnosticResult("a", 8, 51)
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenUsingStringLiteralInVariousPlaces()
        {
            const string source = @"
namespace N1.N2
{
    namespace @using
    {
        public delegate int SomeDelegate(int a, int b);

        public class BaseTypeName
        {
            public BaseTypeName(string a)
            {
            }
        }

        public class @class : BaseTypeName
        {
            private readonly int readonlyField;
            public event EventHandler ParticularEvent;

            public string className = ""class"";
            public string fieldName = ""field"";
            public string someName = ""variable"";
            public string namespaceName = ""using"";
            public string namespaceName2 = ""N2"";

            void Foo() => string.Format(""{0}"", ""xyz"");
            void Foo2() => string.Format(""{0}"", ""readonlyField"");

            public @class() : base(""SomeDelegate"") { }

            void Foo3(string a, BaseTypeName d)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>
                {
                    { ""b"", ""readonlyField"" },
                    { ""xyz"", ""ParticularEvent"" },
                    { ""c"", ""Foo3""},
                };
            }

            public int Property
            {
                get
                {
                    var variable = 5;
                    return variable;
                }
            }

            public string namespaceName3 = ""N1.N2"";

            public string verbatimString = @""
verbatim
string
lines"";
        }
    }
}";
            var expected = new[]
            {
                CreateNameofDiagnosticResult("@class", 20, 39),
                CreateNameofDiagnosticResult("@using", 23, 43),
                CreateNameofDiagnosticResult("N2", 24, 44),
                CreateNameofDiagnosticResult("readonlyField", 27, 49),
                CreateNameofDiagnosticResult("SomeDelegate", 29, 36),
                CreateNameofDiagnosticResult("readonlyField", 35, 28),
                CreateNameofDiagnosticResult("ParticularEvent", 36, 30),
                CreateNameofDiagnosticResult("Foo3", 37, 28)
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FixWithVerbatimIdentifiers()
        {
            const string source = @"
public class TypeName
{
    public TypeName(int @object)
    {
        var name = ""object"";
    }

    void Foo(string a, int @for, string b, object @int)
    {
        var whatever = ""for"";
        var whatever1 = ""b"";
        var whatever2 = ""a"";
        var whatever3 = ""int"";
    }
}";

            const string fixtest = @"
public class TypeName
{
    public TypeName(int @object)
    {
        var name = nameof(@object);
    }

    void Foo(string a, int @for, string b, object @int)
    {
        var whatever = nameof(@for);
        var whatever1 = nameof(b);
        var whatever2 = nameof(a);
        var whatever3 = nameof(@int);
    }
}";

            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenUsingStringLiteralEqualsParameterNameInConstructorFixItToNameof()
        {
            const string source = @"
public class TypeName
{
    public TypeName(string b)
    {
        string whatever = ""b"";
    }
}";

            const string fixtest = @"
public class TypeName
{
    public TypeName(string b)
    {
        string whatever = nameof(b);
    }
}";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingStringLiteralEqualsParameterNameInConstructorFixItToNameofMustKeepComments()
        {
            const string source = @"
public class TypeName
{
    public TypeName(string b)
    {
        //a
        string whatever = ""b"";//d
        //b
    }
}";

            const string fixtest = @"
public class TypeName
{
    public TypeName(string b)
    {
        //a
        string whatever = nameof(b);//d
        //b
    }
}";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingStringLiteralEqualsParameterNameInMethodFixItToNameof()
        {
            const string source = @"
public class TypeName
{
    void Foo(string b)
    {
        string whatever = ""b"";
    }
}";

            const string fixtest = @"
public class TypeName
{
    void Foo(string b)
    {
        string whatever = nameof(b);
    }
}";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingStringLiteralEqualsSecondParameterNameInMethodFixItToNameof()
        {
            const string source = @"
public class TypeName
{
    void Foo(string a, string b)
    {
        string whatever = ""b"";
    }
}";

            const string fixtest = @"
public class TypeName
{
    void Foo(string a, string b)
    {
        string whatever = nameof(b);
    }
}";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingStringLiteralEqualsParameterNameInMethodMustKeepComments()
        {
            const string source = @"
public class TypeName
{
    void Foo(string b)
    {
        //a
        string whatever = ""b""//d;
        //b
    }
}";

            const string fixtest = @"
public class TypeName
{
    void Foo(string b)
    {
        //a
        string whatever = nameof(b)//d;
        //b
    }
}";

            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAll()
        {
            const string source = @"
public class TypeName
{
    void Foo(string a, string b)
    {
        var whatever = ""a"";
        var whatever2 = ""b"";
    }
}";

            const string fixtest = @"
public class TypeName
{
    void Foo(string a, string b)
    {
        var whatever = nameof(a);
        var whatever2 = nameof(b);
    }
}";

            await VerifyCSharpFixAllAsync(source, fixtest);
        }

        [Theory]
        [InlineData("NestedClass")]
        [InlineData("SomeStruct")]
        [InlineData("SomeEnum")]
        [InlineData("IInterface")]
        [InlineData("N2")]
        [InlineData("SomeDelegate")]
        [InlineData("readonlyField")]
        [InlineData("ParticularEvent")]
        [InlineData("Property")]
        [InlineData("TypeName")]
        [InlineData("Invoke")]
        [InlineData("N1")]
        [InlineData("N3")]
        public async Task WhenUsingProgramElementNameStringInMethodInvocationThenFixUpdatesAsExpected(string stringLiteral)
        {
            const string source = @"
namespace N1.N2
{
    namespace N3
    {
        public class TypeName
        {
            private readonly int readonlyField;
            public int Property { get; set; }
            public event EventHandler ParticularEvent;
            public delegate int SomeDelegate(int c, double d);

            public interface IInterface {}
            public struct SomeStruct {}
            public enum SomeEnum {}
            public class NestedClass {}

            public int Property2
            {
                set
                {
                    Invoke(""abc"", <REPLACE>);
                }
            }

            private void Invoke(string arg1, string arg2)
            {
            }
        }
    }
}";
            await VerifyCSharpFixAllAsync(source.Replace("<REPLACE>", $@"""{stringLiteral}"""), source.Replace("<REPLACE>", $@"nameof({stringLiteral})"));
        }
        [Fact]
        public async Task ReplacesUsingMethodName()
        {
            const string source = @"
public class TypeName
{
    void Foo(TypeName d)
    {
        var bar = new [] { ""Foo"" };
    }
}";

            const string fixtest = @"
public class TypeName
{
    void Foo(TypeName d)
    {
        var bar = new [] { nameof(Foo) };
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        private static DiagnosticResult CreateNameofDiagnosticResult(string nameofArgument, int diagnosticLine, int diagnosticColumn, DiagnosticId id = DiagnosticId.NameOf)
        {
            return new DiagnosticResult(id.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(diagnosticLine, diagnosticColumn)
                .WithMessage($"Use 'nameof({nameofArgument})' instead of specifying the program element name.");
        }
    }
}