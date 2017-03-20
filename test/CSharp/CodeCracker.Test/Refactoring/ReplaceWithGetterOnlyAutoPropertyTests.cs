using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var expected = new DiagnosticResult
            {
                Id = "CC0125",
                Message = GetDiagnosticMessage("Value"),
                Severity = DiagnosticSeverity.Hidden,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 16, 27)
                        }
            };

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
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(),
                Message = GetDiagnosticMessage("Value"),
                Severity = DiagnosticSeverity.Hidden,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 16, 27)
                        }
            };

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

            var expected1 = new DiagnosticResult
            {
                Id = DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(),
                Message = GetDiagnosticMessage("Value"),
                Severity = DiagnosticSeverity.Hidden,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 27)
                        }
            };
            var expected2 = new DiagnosticResult
            {
                Id = DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(),
                Message = GetDiagnosticMessage("Value2"),
                Severity = DiagnosticSeverity.Hidden,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 18, 27)
                        }
            };

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
            var expected1 = new DiagnosticResult
            {
                Id = DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(),
                Message = GetDiagnosticMessage("Value"),
                Severity = DiagnosticSeverity.Hidden,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 17, 27)
                        }
            };
            var expected2 = new DiagnosticResult
            {
                Id = DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(),
                Message = GetDiagnosticMessage("Value2"),
                Severity = DiagnosticSeverity.Hidden,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 18, 27)
                        }
            };

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
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(),
                Message = GetDiagnosticMessage("Value2"),
                Severity = DiagnosticSeverity.Hidden,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 18, 34)
                        }
            };

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
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(),
                Message = GetDiagnosticMessage("X"),
                Severity = DiagnosticSeverity.Hidden,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 11, 24)
                        }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);

            var fixtest = @"
            readonly int a = 0, y = 1, z = 2;

            public int X { get; } = 1;
            ".WrapInCSharpClass();
            await VerifyCSharpFixAsync(test, fixtest);
        }
    }
}
