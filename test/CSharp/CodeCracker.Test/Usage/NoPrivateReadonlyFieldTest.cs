using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace CodeCracker.Test.CSharp.Usage
{
    public class NoPrivateReadonlyFieldTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() => new NoPrivateReadonlyFieldAnalyzer();

        static DiagnosticResult CreateExpectedDiagnosticResult(int line, int column, string fieldName = "i")
        {
            return new DiagnosticResult(DiagnosticId.NoPrivateReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(line, column)
                .WithMessage(string.Format(NoPrivateReadonlyFieldAnalyzer.Message, fieldName));
        }

        [Fact]
        public async Task PrivateFieldWithAssignmentOnDeclarationCreatesNoDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				private int i = 1;
			}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ReadonlyFieldCreatesNoDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				internal readonly int i = 1;
			}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ConstFieldCreatesNoDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				protected const int i = 1;
			}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task NoPrivateFieldWithAssignmentOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				protected int i = 1;
			}";

            await VerifyCSharpDiagnosticAsync(source, CreateExpectedDiagnosticResult(line: 4, column: 19));
        }

        [Fact]
        public async Task FieldWithoutAcessModifierCreatesNoDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				int i = 1;
			}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task NoPrivateFieldsInAStructCreatesDiagnostic()
        {
            const string source = @"
			public struct TypeName
			{
				public int i = 1;
				internal int j = 1;
			}";
            await VerifyCSharpDiagnosticAsync(
                source,
                CreateExpectedDiagnosticResult(line: 4, column: 16),
                CreateExpectedDiagnosticResult(line: 5, column: 18, fieldName: "j"));
        }

        [Fact]
        public async Task StaticFieldCreatesDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				protected static int i = 1;
			}";
            await VerifyCSharpDiagnosticAsync(source, CreateExpectedDiagnosticResult(line: 4, column: 26));
        }

        [Fact]
        public async Task StaticFieldAssignmentOnStaticConstructorCreatesDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				protected static int i;
				static TypeName()
				{
					i = 0;
				}
			}";
            await VerifyCSharpDiagnosticAsync(source, CreateExpectedDiagnosticResult(line: 4, column: 26));
        }

        [Fact]
        public async Task StaticFieldAssignmentOnAInstanceConstructorCreatesNoDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				protected static int i;
				private TypeName()
				{
					i = 0;
				}
			}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithAssignmentCreatesNoDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				public int i;
				public void Foo()
				{
					i = 0;
				}
			}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithAssignmentOnConstructorCreatesDiagnostic()
        {
            const string source = @"
			namespace ConsoleApplication1
			{
				public class TypeName
				{
					protected int i;
					public TypeName()
					{
						i = 0;
					}
				}
			}";
            await VerifyCSharpDiagnosticAsync(source, CreateExpectedDiagnosticResult(line: 6, column: 20));
        }

        [Fact]
        public async Task FieldWithPostfixUnaryAssignmentOnMethodCreatesNoDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				protected int i = 0;
				public void Foo()
				{
					i++;
				}
			}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithPrefixUnaryAssignmentOnMethodCreatesNoDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				protected int i = 0;
				public void Foo()
				{
					--i;
				}
			}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithPostfixUnaryAssignmentOnConstructorCreatesDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				protected int i = 0;
				public TypeName()
				{
					i++;
				}
			}";
            await VerifyCSharpDiagnosticAsync(source, CreateExpectedDiagnosticResult(line: 4, column: 19));
        }

        [Fact]
        public async Task FieldWithPrefixUnaryAssignmentOnConstructorCreatesDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				protected int i = 0;
				public TypeName()
				{
					--i;
				}
			}";
            await VerifyCSharpDiagnosticAsync(source, CreateExpectedDiagnosticResult(line: 4, column: 19));
        }

        [Fact]
        public async Task TwoClassesWithNoPrivateFieldsCreatesDiagnostic()
        {
            const string source = @"
			namespace ConsoleApplication1
			{
				public class TypeName1
				{
					public int i = 1;
					public int j = 1;
				}
				internal class TypeName2
				{
					internal int k = 1;
					internal int l = 1;
				}
			}";
            await VerifyCSharpDiagnosticAsync(source, new[] {
                CreateExpectedDiagnosticResult(line: 6, column: 17, fieldName: "i"),
                CreateExpectedDiagnosticResult(line: 7, column: 17, fieldName: "j"),
                CreateExpectedDiagnosticResult(line: 11, column: 19, fieldName: "k"),
                CreateExpectedDiagnosticResult(line: 12, column: 19, fieldName: "l")
            });
        }

        [Fact]
        public async Task ReadFieldFieldFromOtherTypeCreatesDiagnostic()
        {
            const string source1 = @"
			namespace ConsoleApplication1
			{
				public class TypeName1
				{
					public int i = 1;
				}
			}";

            const string source2 = @"
			namespace ConsoleApplication1
			{
				public class TypeName2
				{
					TypeName2()
					{
						var x = new TypeName1().i;
					}
				}
			}";
            await VerifyCSharpDiagnosticAsync(new[] { source1, source2 }, new[] { CreateExpectedDiagnosticResult(line: 6, column: 17) });
        }

        [Fact]
        public async Task SetFieldFieldFromOtherTypeCreatesNoDiagnostic()
        {
            const string source1 = @"
			namespace ConsoleApplication1
			{
				public class TypeName1
				{
					public int i = 1;
				}
			}";

            const string source2 = @"
			namespace ConsoleApplication1
			{
				public class TypeName2
				{
					TypeName2()
					{
						new TypeName1() { i = 10 };
					}
				}
			}";
            await VerifyCSharpHasNoDiagnosticsAsync(source1, source2);
        }

        [Fact]
        public async Task ReadFieldFieldFromNestedTypeCreatesDiagnostic()
        {
            const string source = @"
			public class TypeName1
			{
				protected int j = 0;
				TypeName1()
				{
					new TypeName2(0).i.ToString();
				}

				struct TypeName2
				{
					internal int i;
					internal TypeName2(int i)
					{
						this.i = i;
						new TypeName1().j.ToString();
					}
				}
			}";
            await VerifyCSharpDiagnosticAsync(source,
                CreateExpectedDiagnosticResult(line: 04, column: 19, fieldName: "j"),
                CreateExpectedDiagnosticResult(line: 12, column: 19, fieldName: "i"));
        }

        [Fact]
        public async Task SetFieldFieldFromNestedTypeCreatesNoDiagnostic()
        {
            const string source = @"
			public class TypeName1
			{
				TypeName1()
				{
					new TypeName2() { i = 1 };
				}

				struct TypeName2
				{
					internal int i;
				}
			}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithAssignmentOnMethodByRefOrOutParameterCreatesNoDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				public int i;
				public int k;
				void Foo()
				{
					Foo1(ref i);
					Foo2(out k);
				}
				void Foo1(ref int j)
				{
				}
				void Foo2(out int m)
				{
					m = 0;
				}
			}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithAssignmentOnConstructorByRefOrOutParameterCreatesDiagnostic()
        {
            const string source = @"
			public class TypeName
			{
				public int i;
				public int k;
				TypeName()
				{
					Foo1(ref i);
					Foo2(out k);
				}
				void Foo1(ref int j)
				{
				}
				void Foo2(out int m)
				{
					m = 0;
				}
			}";
            await VerifyCSharpDiagnosticAsync(source,
                CreateExpectedDiagnosticResult(line: 4, column: 16, fieldName: "i"),
                CreateExpectedDiagnosticResult(line: 5, column: 16, fieldName: "k"));
        }

        [Fact]
        public async Task FieldInPartialClassWithAssignmentOnConstructorInOtherTreeCreatesDiagnostic()
        {
            const string source1 = @"
			public partial class TypeName
			{
				public int i;
			}";

            const string source2 = @"
			public partial class TypeName
			{
				TypeName()
				{
					i = 0;
				};
			}";

            await VerifyCSharpDiagnosticAsync(
                sources: new[] { source1, source2 },
                expected: new[] { CreateExpectedDiagnosticResult(line: 4, column: 16) });
        }

        [Fact]
        public async Task FieldInPartialClassWithAssignmentOnMethodInOtherTreeCreatesNoDiagnostic()
        {
            const string source1 = @"
			public partial class TypeName
			{
				public int i;
			}";

            const string source2 = @"
			public partial class TypeName
			{
				void Foo()
				{
					i = 0;
				};
			}";
            await VerifyCSharpHasNoDiagnosticsAsync(source1, source2);
        }
    }
}