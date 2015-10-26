using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class ReadonlyFieldTests : CodeFixVerifier<ReadonlyFieldAnalyzer, ReadonlyFieldCodeFixProvider>
    {
        [Fact]
        public async Task FieldWithoutAssignmentDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithoutAssignmentInAStructDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        struct TypeName
        {
            private int i;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task PublicFieldWithAssignmentOnDeclarationDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int i = 1;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithAssignmentOnDeclarationAlreadyReadonlyDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private readonly int i = 1;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ConstantFieldDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private const int i = 1;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithAssignmentOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "i"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 25) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FieldWithoutModifierWithAssignmentOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            int i = 1;
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "i"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 17) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FieldsWithAssignmentOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            private int j = 1;
        }
    }";
            var expected1 = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "i"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 25) }
            };
            var expected2 = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "j"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 25) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task TwoClassesWithFieldsWithAssignmentOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName1
        {
            private int i = 1;
            private int j = 1;
        }
        class TypeName2
        {
            private int k = 1;
            private int l = 1;
        }
    }";
            var expected1 = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "i"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 25) }
            };
            var expected2 = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "j"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 25) }
            };
            var expected3 = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "k"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 25) }
            };
            var expected4 = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "l"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 25) }
            };
            await VerifyCSharpDiagnosticAsync(source, new[] { expected1, expected2, expected3, expected4 });
        }

        [Fact]
        public async Task FieldWithAssignmentOnDeclarationAndNestedNamespaceCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        namespace SubNamespace
        {
            class TypeName
            {
                private int i = 1;
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "i"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 29) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FieldWithtAssignmentOnDeclarationInAStructCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        struct TypeName
        {
            private int i = 1;
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "i"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 25) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FieldWithAssignmentDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i;
            public void Foo()
            {
                i = 0;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithAssignmentInAStructDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i;
            public void Foo()
            {
                i = 0;
            }
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
        class TypeName
        {
            private int i;
            TypeName()
            {
                i = 0;
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "i"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 25) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FieldWithAssignmentOnConstructorAndOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            TypeName()
            {
                i = 0;
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "i"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 25) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ChangeFieldAssignedOnDeclarationToReadonly()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private readonly int i = 1;
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task ChangeFieldAssignedOnDeclarationWithoutModifierToReadonly()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            int i = 1;
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            readonly int i = 1;
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FieldsWithAssignmentOnDeclarationWithSingleDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i, j = 1;
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "j"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 28) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ChangeFieldWhenOtherFieldsAreDeclared()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i;
            private int j = 1;
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i;
            private readonly int j = 1;
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task ChangeFieldWithJointDeclarationToADifferentDeclaration()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            //leading
            private int i, j = 1;//trailling
            //leading trivia for token
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            //leading
            private int i;//trailling
            private readonly int j = 1;
            //leading trivia for token
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task ChangeFieldWithJointDeclarationToADifferentDeclarationWhenThereIsNoPrivateModifier()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            //leading
            int i, j = 1;//trailling
            //leading trivia for token
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            //leading
            int i;//trailling
            readonly int j = 1;
            //leading trivia for token
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FieldWithAssignmentOnConstructorThatIsAlreadyReadonlyDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private readonly int i;
            public TypeName()
            {
                i = 0;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldAssignedOnPropertyGetDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            public int Property
            {
                get
                {
                    i = 0;
                    return 1;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldAssignedOnPropertySetDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            public int Property
            {
                set
                {
                    i = 0;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldAssignedOnEventAddDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            public event System.EventHandler MyEvent
            {
                add { i = 0; }
                remove { }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldAssignedOnEventRemoveDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            public event System.EventHandler MyEvent
            {
                add { }
                remove { i = 0; }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldAssignedOnDelegateDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            void Foo()
            {
                System.Action a = () => { i = 0; };
            }
        }
   }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task StaticFieldUpdatedInInstanceConstructorDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private static int i;
            TypeName()
            {
                i = 1;
            }
        }
   }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task StaticFieldInitializedOnDeclarationUpdatedInInstanceConstructorDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private static int i = 0;
            TypeName()
            {
                i = 1;
            }
        }
   }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task StaticFieldInitializedOnStaticConstructorCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private static int i;
            static TypeName()
            {
                i = 1;
            }
        }
   }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "i"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 32) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task StaticFieldInitializedOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private static int i = 1;
        }
   }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReadonlyField.ToDiagnosticId(),
                Message = string.Format(ReadonlyFieldAnalyzer.Message, "i"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 32) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task UpdatesOnInnerClassesDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class OuterClass
        {
            private int i = 0;
            class InnerClass
            {
                OuterClass o;
                void Foo()
                {
                    o.i = 1;
                }
            }
        }
   }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldsWithoutAssignmentOnPartialClassOn2FilesDoesNotCreateDiagnostic()
        {
            const string source1 = @"
    namespace ConsoleApplication1
    {
        partial class TypeName
        {
            private int i;
            public void Foo2()
            {
                j = 0;
            }
        }
    }";
            const string source2 = @"
    namespace ConsoleApplication1
    {
        partial class TypeName
        {
            private int j;
            public void Foo()
            {
                i = 0;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source1, source2);
        }

    [Fact]
    public async Task FieldsAssignedOnLambdaDoesNotCreateDiagnostic()
    {
        const string source = @"
    namespace ConsoleApplication1
    {
        internal class Test
        {
            private string _value;

            public Test()
            {
                Func<string> factory = () => _value ?? (_value = ""Hello"");
            }
        }
    }";
        await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
    }
}
