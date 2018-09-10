using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class ReplaceWithGetterOnlyAutoPropertyTests : CodeFixVerifier<ReplaceWithGetterOnlyAutoPropertyAnalyzer, ReplaceWithGetterOnlyAutoPropertyCodeFixProvider>
    {
        private static string GetDiagnosticMessage(string propertyName) => $"Property {propertyName} can be converted to an getter-only auto-property.";

        [Fact]
        public async Task EmptyCodeBlockPassesWithoutErrors()
        {
            const string test = @"";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task SimplePropertyGetsTransformed()
        {
            var test = @"
            readonly string _value;

            TypeName(string value)
            {
                _value=value;
            }

            public string Value { get { return _value; } }
            ".WrapInCSharpClass();
            var expected = new DiagnosticResult(DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(16, 27)
                .WithMessage(GetDiagnosticMessage("Value"));

            await VerifyCSharpDiagnosticAsync(test, expected);

            var fixtest = @"
            TypeName(string value)
            {
            Value = value;
            }

            public string Value { get; }
            ".WrapInCSharpClass();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task SimplePropertyGetsNotTransformedIfLessThanCSharp6()
        {
            var test = @"
            readonly string _value;

            TypeName(string value)
            {
                _value=value;
            }

            public string Value { get { return _value; } }
            ".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(test, LanguageVersion.CSharp5);
        }

        [Fact]
        public async Task FieldInitializerIsPreserved()
        {
            var test = @"
            readonly string value, value2 = ""InitValue"";

            TypeName(string value)
            {
                this.value=value;
            }

            public string Value { get { return this.value; } }
            ".WrapInCSharpClass();
            var expected = new DiagnosticResult(DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(16, 27)
                .WithMessage(GetDiagnosticMessage("Value"));

            await VerifyCSharpDiagnosticAsync(test, expected);

            var fixtest = @"
            readonly string value2 = ""InitValue"";

            TypeName(string value)
            {
                this.Value = value;
            }

            public string Value { get; } = ""InitValue"";
            ".WrapInCSharpClass();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task MultiplePropertiesPerClassGetTranformed()
        {
            var test = @"
            readonly string value, value2=""InitValue"";

            TypeName(string value)
            {
                this.value=value;
                this.value2=value;
            }

            public string Value { get { return this.value; } }
            public string Value2 { get { return this.value2; } }
            ".WrapInCSharpClass();

            var expected1 = new DiagnosticResult(DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(17, 27)
                .WithMessage(GetDiagnosticMessage("Value"));
            var expected2 = new DiagnosticResult(DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(18, 27)
                .WithMessage(GetDiagnosticMessage("Value2"));

            await VerifyCSharpDiagnosticAsync(test, new DiagnosticResult[] { expected1, expected2 });

            var fixtest = @"
            TypeName(string value)
            {
                this.Value = value;
                this.Value2 = value;
            }

            public string Value { get; } = ""InitValue"";
            public string Value2 { get; } = ""InitValue"";
            ".WrapInCSharpClass();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task MultiplePropertiesPerClassWithFieldInitilizerAndUnusedFieldsGetTranformed()
        {
            var test = @"
            readonly string value, value2, value3=""InitValue"";

            TypeName(string value)
            {
                this.value=value;
                this.value2=value;
            }

            public string Value { get { return this.value; } }
            public string Value2 { get { return this.value2; } }
            ".WrapInCSharpClass();
            var expected1 = new DiagnosticResult(DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(17, 27)
                .WithMessage(GetDiagnosticMessage("Value"));
            var expected2 = new DiagnosticResult(DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(18, 27)
                .WithMessage(GetDiagnosticMessage("Value2"));

            await VerifyCSharpDiagnosticAsync(test, new DiagnosticResult[] { expected1, expected2 });

            var fixtest = @"
            readonly string value3=""InitValue"";

            TypeName(string value)
            {
                this.Value = value;
                this.Value2 = value;
            }

            public string Value { get; } = ""InitValue"";
            public string Value2 { get; } = ""InitValue"";
            ".WrapInCSharpClass();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task TypeOfPropertyMustFitTypeOfBackingField()
        {
            var test = @"
            readonly IList<string> value, value2;

            TypeName(IEnumerable<string> value)
            {
                this.value=value.ToList();
                this.value2=value.ToList();
            }

            public IEnumerable<string> Value { get { return this.value; } }
            public IList<string> Value2 { get { return this.value2; } }
            ".WrapInCSharpClass();
            var expected = new DiagnosticResult(DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(18, 34)
                .WithMessage(GetDiagnosticMessage("Value2"));

            await VerifyCSharpDiagnosticAsync(test, expected);

            var fixtest = @"
            readonly IList<string> value;

            TypeName(IEnumerable<string> value)
            {
                this.value=value.ToList();
                this.Value2 = value.ToList();
            }

            public IEnumerable<string> Value { get { return this.value; } }
            public IList<string> Value2 { get; }
            ".WrapInCSharpClass();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task ExplicitPropertyImplementationsAreIgnored()
        {
            const string test = @"
            namespace ConsoleApplication1
            {
                interface ITestInterface
                {
                    string Property { get; }
                }
                class TestClass2: ITestInterface
                {
                    readonly string _Property;

                    string ITestInterface.Property
                    {
                        get
                        {
                            return _Property;
                        }
                    }
                }
            }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task SeveralInitializerAreAssignedProperly()
        {
            var test = @"
            readonly int a = 0, x, y = 1, z = 2;

            public int X
            {
                get
                {
                    return x;
                }
            }
            ".WrapInCSharpClass();
            var fixtest = @"
            readonly int a = 0, y = 1, z = 2;

            public int X { get; } = 1;
            ".WrapInCSharpClass();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task FieldNameIsRenamedInClass()
        {
            var test = @"
            readonly int _X;

            public TypeName(int x)
            {
                _X=x;
                _X=_X*2;
                Console.Write(_X);
            }

            protected void M() => Console.Write(_X);

            public int X
            {
                get
                {
                    return _X;
                }
            }
            ".WrapInCSharpClass();
            var fixtest = @"
            public TypeName(int x)
            {
                X=x;
                X=X*2;
                Console.Write(X);
            }

            protected void M() => Console.Write(X);

            public int X { get; }
            ".WrapInCSharpClass();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task ShadowedFieldNameIsNotRenamedInClass()
        {
            var test = @"
            readonly int _X;

            public TypeName(int x)
            {
                _X=x;
            }

            protected void M()
            {
                string _X="";
                Console.Write(_X);
            }                

            public int X
            {
                get
                {
                    return _X;
                }
            }
            ".WrapInCSharpClass();
            var fixtest = @"
            public TypeName(int x)
            {
                X=x;
            }

            protected void M()
            {
                string _X="";
                Console.Write(_X);
            }                

            public int X { get; }
            ".WrapInCSharpClass();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task FieldAccessInInnerClassIsRenamed()
        {
            var test = @"
            readonly int _A;

            public TypeName(int a)
            {
                _A=a;
            }

            class InnerClass {
                InnerClass(TypeName outterObject)
                {
                    Console.Write(outterObject._A);
                }
            }
            public int A
            {
                get
                {
                    return _A;
                }
            }
            ".WrapInCSharpClass();
            var fixtest = @"
            public TypeName(int a)
            {
                A=a;
            }

            class InnerClass {
                InnerClass(TypeName outterObject)
                {
                    Console.Write(outterObject.A);
                }
            }
            public int A { get; }
            ".WrapInCSharpClass();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task FieldWithSameNameInOtherClassIsNotRenamed()
        {
            const string test = @"
            using System;
            namespace App {
                public class C1 
                {
                    readonly int _A;

                    public C1(int a)
                    {
                        _A=a;
                    }

                    public int A
                    {
                        get
                        {
                            return _A;
                        }
                    }
                }
                public class C2 
                {
                    readonly int _A;
                }
            }";
            const string fixtest = @"
            using System;
            namespace App {
                public class C1 
                {

                    public C1(int a)
                    {
                        A=a;
                    }

                    public int A { get; }
                }
                public class C2 
                {
                    readonly int _A;
                }
            }";
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task RenamingOfFieldAccessCanIntroduceNameClashesCaughtByCompilerWarningCS1717()
        {
            var test = @"
            readonly int _A;

            public TypeName(int A)
            {
                _A=A;
            }

            public int A
            {
                get
                {
                    return _A;
                }
            }
            ".WrapInCSharpClass();
            var fixtest = @"
            public TypeName(int A)
            {
                A=A;
            }

            public int A { get; }
            ".WrapInCSharpClass();
            // "A=A;" causes new compiler warning CS1717: Assignment made to same variable; did you mean to assign something else?
            // The fix would be to transform  the expression to this.A=A;
            // Maybe using Microsoft.CodeAnalysis.Rename.Renamer.RenameSymbolAsync() for the renaming is the able to fix this.
            await VerifyCSharpFixAsync(oldSource: test, newSource: fixtest, allowNewCompilerDiagnostics: true);
        }

        [Fact]
        public async Task FieldReferencesInPartialClassesGetRenamedIfInTheSameDocument()
        {
            const string test = @"
            namespace A
            {
                using System;
                public partial class A1
                {
                    readonly int _I;
                    public A1(int i)
                    {
                        _I = i;
                    }
                    public int I {  get { return _I; } }
                }
                public partial class A1
                {
                    public void Print() => Console.Write(_I);
                }
            }";
            const string fixtest = @"
            namespace A
            {
                using System;
                public partial class A1
                {
                    public A1(int i)
                    {
                        I = i;
                    }
                    public int I {  get; }
                }
                public partial class A1
                {
                    public void Print() => Console.Write(I);
                }
            }";
            await VerifyCSharpFixAsync(oldSource: test, newSource: fixtest, allowNewCompilerDiagnostics: false);
        }

        [Fact]
        public async Task FieldReferencesInPartialClassesInDifferentDocumentsGetNotRenamedAndCauseCompilerErrorCS0103()
        {
            const string testPart1 = @"
            namespace A
            {
                public partial class A1
                {
                    readonly int _I;
                    public A1(int i)
                    {
                        _I = i;
                    }
                    public int I {  get { return _I; } }
                }
            }";
            const string testPart2 = @"
            namespace A
            {
                using System;
                public partial class A1
                {
                    public void Print() => Console.Write(_I);
                }
            }";
            const string fixtestPart1 = @"
            namespace A
            {
                public partial class A1
                {
                    public A1(int i)
                    {
                        I = i;
                    }
                    public int I {  get; }
                }
            }";
            const string fixtestPart2 = @"
            namespace A
            {
                using System;
                public partial class A1
                {
                    public void Print() => Console.Write(_I);
                }
            }";
            //Console.Write(_I); causes CS0103 The name '_I' does not exist in the current context
            await VerifyCSharpFixAllAsync(oldSources: new string[] { testPart1, testPart2 }, newSources: new string[] { fixtestPart1, fixtestPart2 }, allowNewCompilerDiagnostics: true);
        }

        #region FixAll Tests

        [Fact]
        public async Task ReplaceMultiplePropertiesInOneClassFixAllTest()
        {
            var source1 = @"
            readonly int _A;
            readonly int _B;
            readonly string _C;

            public TypeName(int a, int b, string c)
            {
                _A=a;
                _B=b;
                _C=c;
            }
            public int A { get { return _A; } }
            public int B { get { return _B; } }
            public string C { get { return _C; } }
            ".WrapInCSharpClass();
            var fixtest1 = @"
            public TypeName(int a, int b, string c)
            {
                A=a;
                B=b;
                C=c;
            }
            public int A { get; }
            public int B { get; }
            public string C { get; }
            ".WrapInCSharpClass();
            await VerifyCSharpFixAllAsync(new[] { source1 }, new[] { fixtest1 });
        }

        [Fact]
        public async Task ReplaceMultiplePropertiesInOneClassInMultipleDocumentsFixAllTest()
        {
            const string source1 = @"
            namespace A 
            {
                public class A1
                {
                    readonly int _A;
                    readonly int _B;
                    readonly string _C;
                    public A1(int a, int b, string c)
                    {
                        _A=a;
                        _B=b;
                        _C=c;
                    }
                    public int A { get { return _A; } }
                    public int B { get { return _B; } }
                    public string C { get { return _C; } }
                }
            }";
            const string source2 = @"
            namespace A 
            {
                public class A2
                {
                    readonly int _A;
                    readonly int _B;
                    readonly string _C;
                    public A2(int a, int b, string c)
                    {
                        _A=a;
                        _B=b;
                        _C=c;
                    }
                    public int A { get { return _A; } }
                    public int B { get { return _B; } }
                    public string C { get { return _C; } }
                }
            }";
            const string source3 = @"
            namespace B 
            {
                public class B1
                {
                    readonly int _A;
                    readonly int _B;
                    readonly string _C;
                    public B1(int a, int b, string c)
                    {
                        _A=a;
                        _B=b;
                        _C=c;
                    }
                    public int A { get { return _A; } }
                    public int B { get { return _B; } }
                    public string C { get { return _C; } }
                }
            }";
            const string fixtest1 = @"
            namespace A 
            {
                public class A1
                {
                    public A1(int a, int b, string c)
                    {
                        A=a;
                        B=b;
                        C=c;
                    }
                    public int A { get; }
                    public int B { get; }
                    public string C { get; }
                }
            }";
            const string fixtest2 = @"
            namespace A 
            {
                public class A2
                {
                    public A2(int a, int b, string c)
                    {
                        A=a;
                        B=b;
                        C=c;
                    }
                    public int A { get; }
                    public int B { get; }
                    public string C { get; }
                }
            }";
            const string fixtest3 = @"
            namespace B 
            {
                public class B1
                {
                    public B1(int a, int b, string c)
                    {
                        A=a;
                        B=b;
                        C=c;
                    }
                    public int A { get; }
                    public int B { get; }
                    public string C { get; }
                }
            }";
            await VerifyCSharpFixAllAsync(new[] { source1, source2, source3 }, new[] { fixtest1, fixtest2, fixtest3 });
        }

        [Fact]
        public async Task ReplaceMultiplePropertiesInOneClassInMultipleDocumentsAndKeepExisitingDocumentsWithoutDiagnosticsFixAllTest()
        {
            const string source1 = @"
            namespace A 
            {
                public class A1
                {
                    readonly int _A;
                    readonly int _B;
                    readonly string _C;
                    public A1(int a, int b, string c)
                    {
                        _A=a;
                        _B=b;
                        _C=c;
                    }
                    public int A { get { return _A; } }
                    public int B { get { return _B; } }
                    public string C { get { return _C; } }
                }
            }";
            const string source2 = @"
            namespace A 
            {
                public class A2
                {
                    public A2()
                    {
                    }
                }
            }";
            const string fixtest1 = @"
            namespace A 
            {
                public class A1
                {
                    public A1(int a, int b, string c)
                    {
                        A=a;
                        B=b;
                        C=c;
                    }
                    public int A { get; }
                    public int B { get; }
                    public string C { get; }
                }
            }";
            await VerifyCSharpFixAllAsync(new[] { source1, source2 }, new[] { fixtest1, source2 });
        }
        #endregion
    }
}