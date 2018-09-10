using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class PropertyChangedEventArgsUnnecessaryAllocationTests : CodeFixVerifier <PropertyChangedEventArgsUnnecessaryAllocationAnalyzer, PropertyChangedEventArgsUnnecessaryAllocationCodeFixProvider>
    {
        public static IEnumerable<object[]> SharedDataAnalyzer
        {
            get
            {
                yield return new[] { "\"Name\"" };
                yield return new[] { "nameof(Name)" };
                yield return new[] { "null" };
            }
        }

        [Fact]
        public async Task DoesNotTriggerDiagnosticWithEmptySourceCodeAsync()
        {
            const string source = @"";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Theory]
        [MemberData(nameof(SharedDataAnalyzer))]
        public async Task DoesTriggerDiagnosticAtPropertyChangedEventArgsInstanceCreation(string ctorArg)
        {
            var source = $"var args = new PropertyChangedEventArgs({ctorArg})";

            await VerifyCSharpDiagnosticAsync(source, PropertyChangedUnnecessaryAllocationDiagnostic(1, 12));
        }

        [Theory]
        [MemberData(nameof(SharedDataAnalyzer))]
        public async Task DoesTriggerDiagnosticAtPropertyChangedEventArgsInstanceCreationInMethodInvocation(string ctorArg)
        {
            var source = $@"
public class Test
{{
    public void TestMethod()
    {{
        PropertyChanged(new PropertyChangedEventArgs({ctorArg}))
    }}
}}";

            await VerifyCSharpDiagnosticAsync(source, PropertyChangedUnnecessaryAllocationDiagnostic(6, 25));
        }

        [Theory]
        [MemberData(nameof(SharedDataAnalyzer))]
        public async Task DoesTriggerDiagnosticAtPropertyChangedEventArgsInstanceCreationInObjectInitializer(string ctorArg)
        {
            var source = $"object args = new {{ Name = new PropertyChangedEventArgs({ctorArg}) }}";

            await VerifyCSharpDiagnosticAsync(source, PropertyChangedUnnecessaryAllocationDiagnostic(1, 28));
        }

        [Theory]
        [MemberData(nameof(SharedDataAnalyzer))]
        public async Task DoesNotTriggerDiagnosticAtPropertyChangedEventArgsInstanceCreationInFieldAssignmentWhenFieldIsStatic(string ctorArg)
        {
            var source = $@"
public class Test
{{
    private static PropertyChangedEventArgs field = new PropertyChangedEventArgs({ctorArg});
}}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DoesNotTriggerDiagnosticAtPropertyChangedEventArgsInstanceCreationInStaticConstructor()
        {
            const string source = @"
public class Test
{
    private static PropertyChangedEventArgs field;

    static Test()
    {
        field = new PropertyChangedEventArgs(""Name"");
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DoesNotTriggerDiagnosticAtObjectInstanceCreation()
        {
            const string source = @"
public class Test
{
    private object field = new object();
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DoesTriggerDiagnosticAtObjectInstanceCreationUsingQualifiedName()
        {
            const string source = @"
public class Test
{
    private object field = new System.ComponentModel.PropertyChangedEventArgs(null);
}";

            await VerifyCSharpDiagnosticAsync(source, PropertyChangedUnnecessaryAllocationDiagnostic(4, 28));
        }

        [Theory]
        [MemberData(nameof(SharedDataAnalyzer))]
        public async Task DoesTriggerDiagnosticInLambdaExpression(string ctorArg)
        {
            var source = $@"
using System;
using System.ComponentModel;
public class Test
{{
    private PropertyChangedEventArgs field;

    public Test()
    {{
        Action action = () => field = new PropertyChangedEventArgs({ctorArg});
    }}
}}";

            await VerifyCSharpDiagnosticAsync(source, PropertyChangedUnnecessaryAllocationDiagnostic(10, 39));
        }

        [Fact]
        public async Task DoesNotTriggerWhenArgumentIsNotLiteral()
        {
            var source = $@"
public class Test
{{
    public void TestMethod(string propertyName)
    {{
        PropertyChanged(new PropertyChangedEventArgs(propertyName))
    }}
}}";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        public static DiagnosticResult PropertyChangedUnnecessaryAllocationDiagnostic(int line, int column)
        {
            return new DiagnosticResult(DiagnosticId.PropertyChangedEventArgsUnnecessaryAllocation.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(line, column)
                .WithMessage("Create PropertyChangedEventArgs static instance and reuse it to avoid unecessary memory allocation.");
        }

        public static IEnumerable<object[]> SharedDataCodeFix
        {
            get
            {
                yield return new[] { "\"Name\"", "Name" };
                yield return new[] { "nameof(Name)", "Name" };
                yield return new[] { "null", "AllProperties" };
                yield return new[] { "\"*\"", "AllProperties" };
                yield return new[] { "\"Name-\"", "Name" };
            }
        }

        [Theory]
        [MemberData(nameof(SharedDataCodeFix))]
        public async Task ChangesPropertyChangedEventArgsInstanceToUseStaticField(string ctorArg, string fieldSuffix)
        {
            var source = $@"
using System.ComponentModel;
public class TestClass
{{
    public string Name {{ get;set; }}

    public void Foo()
    {{
        var args = new PropertyChangedEventArgs({ctorArg});
    }}
}}";

            var fixedCode = $@"
using System.ComponentModel;
public class TestClass
{{
    private static readonly PropertyChangedEventArgs PropertyChangedEventArgsFor{fieldSuffix} = new PropertyChangedEventArgs({ctorArg});

    public string Name {{ get;set; }}

    public void Foo()
    {{
        var args = PropertyChangedEventArgsFor{fieldSuffix};
    }}
}}";

            await VerifyCSharpFixAsync(source, fixedCode);
        }

        [Theory]
        [MemberData(nameof(SharedDataCodeFix))]
        public async Task DoesFixWhenEventArgsUsedInMethodInvocation(string ctorArg, string fieldSuffix)
        {
            var source = $@"
using System.ComponentModel;
public class TestClass
{{
    public string Name {{ get;set; }}

    public void Foo()
    {{
        On(new PropertyChangedEventArgs({ctorArg}));
    }}

    public void On(PropertyChangedEventArgs args) {{ }}
}}";

            var fixedCode = $@"
using System.ComponentModel;
public class TestClass
{{
    private static readonly PropertyChangedEventArgs PropertyChangedEventArgsFor{fieldSuffix} = new PropertyChangedEventArgs({ctorArg});

    public string Name {{ get;set; }}

    public void Foo()
    {{
        On(PropertyChangedEventArgsFor{fieldSuffix});
    }}

    public void On(PropertyChangedEventArgs args) {{ }}
}}";

            await VerifyCSharpFixAsync(source, fixedCode);
        }

        [Theory]
        [MemberData(nameof(SharedDataCodeFix))]
        public async Task HandlesMultipleClassDeclarations(string ctorArg, string fieldSuffix)
        {
            var source = $@"
using System.ComponentModel;
public class TestClass
{{
    public string Name {{ get;set; }}

    public void Foo()
    {{
        var args = new PropertyChangedEventArgs({ctorArg});
    }}
}}

public class TestClass2
{{
}}";

            var fixedCode = $@"
using System.ComponentModel;
public class TestClass
{{
    private static readonly PropertyChangedEventArgs PropertyChangedEventArgsFor{fieldSuffix} = new PropertyChangedEventArgs({ctorArg});

    public string Name {{ get;set; }}

    public void Foo()
    {{
        var args = PropertyChangedEventArgsFor{fieldSuffix};
    }}
}}

public class TestClass2
{{
}}";

            await VerifyCSharpFixAsync(source, fixedCode);
        }

        [Theory]
        [MemberData(nameof(SharedDataCodeFix))]
        public async Task DoesFixWhenQualifiedNameUsed(string ctorArg, string fieldSuffix)
        {
            var source = $@"
public class TestClass
{{
    public string Name {{ get;set; }}

    public void Foo()
    {{
        var args = new System.ComponentModel.PropertyChangedEventArgs({ctorArg});
    }}
}}";

            var fixedCode = $@"
public class TestClass
{{
    private static readonly System.ComponentModel.PropertyChangedEventArgs PropertyChangedEventArgsFor{fieldSuffix} = new System.ComponentModel.PropertyChangedEventArgs({ctorArg});

    public string Name {{ get;set; }}

    public void Foo()
    {{
        var args = PropertyChangedEventArgsFor{fieldSuffix};
    }}
}}";

            await VerifyCSharpFixAsync(source, fixedCode, allowNewCompilerDiagnostics: true);
        }

        [Theory]
        [MemberData(nameof(SharedDataCodeFix))]
        public async Task DoesFixWhenEventArgsCreatedInField(string ctorArg, string fieldSuffix)
        {
            var source = $@"
using System.ComponentModel;
public class TestClass
{{
    private PropertyChangedEventArgs field = new PropertyChangedEventArgs({ctorArg});
}}";

            var fixedCode = $@"
using System.ComponentModel;
public class TestClass
{{
    private static readonly PropertyChangedEventArgs PropertyChangedEventArgsFor{fieldSuffix} = new PropertyChangedEventArgs({ctorArg});
    private PropertyChangedEventArgs field = PropertyChangedEventArgsFor{fieldSuffix};
}}";

            await VerifyCSharpFixAsync(source, fixedCode);
        }

        [Theory]
        [MemberData(nameof(SharedDataCodeFix))]
        public async Task DoesFixWhenEventArgsCreatedInObjectInitializer(string ctorArg, string fieldSuffix)
        {
            var source = $@"
using System.ComponentModel;
public class TestClass
{{
    public string Name {{ get;set; }}

    public void Foo()
    {{
        object args = new {{ Name = new PropertyChangedEventArgs({ctorArg}) }};
    }}
}}";

            var fixedCode = $@"
using System.ComponentModel;
public class TestClass
{{
    private static readonly PropertyChangedEventArgs PropertyChangedEventArgsFor{fieldSuffix} = new PropertyChangedEventArgs({ctorArg});

    public string Name {{ get;set; }}

    public void Foo()
    {{
        object args = new {{ Name = PropertyChangedEventArgsFor{fieldSuffix} }};
    }}
}}";

            await VerifyCSharpFixAsync(source, fixedCode);
        }

        [Theory]
        [MemberData(nameof(SharedDataCodeFix))]
        public async Task HandlesNestedClass(string ctorArg, string fieldSuffix)
        {
            var source = $@"
using System.ComponentModel;
public class OuterClass
{{
    public class TestClass
    {{
        private PropertyChangedEventArgs field = new PropertyChangedEventArgs({ctorArg});
    }}
}}";

            var fixedCode = $@"
using System.ComponentModel;
public class OuterClass
{{
    public class TestClass
    {{
        private static readonly PropertyChangedEventArgs PropertyChangedEventArgsFor{fieldSuffix} = new PropertyChangedEventArgs({ctorArg});
        private PropertyChangedEventArgs field = PropertyChangedEventArgsFor{fieldSuffix};
    }}
}}";

            await VerifyCSharpFixAsync(source, fixedCode);
        }

        [Theory]
        [MemberData(nameof(SharedDataCodeFix))]
        public async Task DoesFixLambdaExpression(string ctorArg, string fieldSuffix)
        {
            var source = $@"
using System;
using System.ComponentModel;
public class Test
{{
    private PropertyChangedEventArgs field;

    public Test()
    {{
        Action action = () => field = new PropertyChangedEventArgs({ctorArg});
    }}
}}";

            var fixedCode = $@"
using System;
using System.ComponentModel;
public class Test
{{
    private static readonly PropertyChangedEventArgs PropertyChangedEventArgsFor{fieldSuffix} = new PropertyChangedEventArgs({ctorArg});
    private PropertyChangedEventArgs field;

    public Test()
    {{
        Action action = () => field = PropertyChangedEventArgsFor{fieldSuffix};
    }}
}}";

            await VerifyCSharpFixAsync(source, fixedCode);
        }

        [Theory]
        [MemberData(nameof(SharedDataCodeFix))]
        public async Task DoesFixWhenFieldNameIsAlreadyUsed(string ctorArg, string fieldSuffix)
        {
            var source = $@"
using System;
using System.ComponentModel;
public class Test
{{
    private PropertyChangedEventArgs PropertyChangedEventArgsFor{fieldSuffix};

    public Test()
    {{
        Action action = () => PropertyChangedEventArgsFor{fieldSuffix} = new PropertyChangedEventArgs({ctorArg});
    }}
}}";

            var fixedCode = $@"
using System;
using System.ComponentModel;
public class Test
{{
    private static readonly PropertyChangedEventArgs PropertyChangedEventArgsFor{fieldSuffix}1 = new PropertyChangedEventArgs({ctorArg});
    private PropertyChangedEventArgs PropertyChangedEventArgsFor{fieldSuffix};

    public Test()
    {{
        Action action = () => PropertyChangedEventArgsFor{fieldSuffix} = PropertyChangedEventArgsFor{fieldSuffix}1;
    }}
}}";

            await VerifyCSharpFixAsync(source, fixedCode);
        }
    }
}