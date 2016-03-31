using CodeCracker.CSharp.Refactoring;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class PropertyChangedEventArgsUnnecessaryAllocationCodeFixProviderTests :
            CodeFixVerifier
                <PropertyChangedEventArgsUnnecessaryAllocationAnalyzer,
                    PropertyChangedEventArgsUnnecessaryAllocationCodeFixProvider>
    {
        public static IEnumerable<object[]> SharedData
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
        [MemberData(nameof(SharedData))]
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
        [MemberData(nameof(SharedData))]
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
        [MemberData(nameof(SharedData))]
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
        [MemberData(nameof(SharedData))]
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
        [MemberData(nameof(SharedData))]
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
        [MemberData(nameof(SharedData))]
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
        [MemberData(nameof(SharedData))]
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
        [MemberData(nameof(SharedData))]
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
        [MemberData(nameof(SharedData))]
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
