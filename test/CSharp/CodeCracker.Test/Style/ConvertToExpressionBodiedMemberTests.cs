using CodeCracker.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Style
{
    public class ConvertToExpressionBodiedMemberTests : CodeFixTest<ConvertToExpressionBodiedMemberAnalyzer, ConvertToExpressionBodiedMemberCodeFixProvider>
    {
        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task FixReplacesMethodConventionalBodyWithArrowExpression()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public override string ToString()
            {
                return ""Teste"";
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public override string ToString() =>  ""Teste"";
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }


        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task FixReplacesOperatorConventionalBodyWithArrowExpression()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static TypeName operator +(TypeName a, TypeName b) 
            {
                return a.Add(b);
            }
        }
    }";

            const string expected = @"
using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static TypeName operator +(TypeName a, TypeName b) => a.Add(b);
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }


        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task FixReplacesConversionOperatorConventionalBodyWithArrowExpression()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static implicit operator string(TypeName n) 
            { 
                return n.Prop1 + "" "" + n.Prop2;
            }
        }
    }";

            const string expected = @"
using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static implicit operator string(TypeName n) => n.Prop1 + "" "" + n.Prop2;
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }


        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task FixReplacesIndexerBodyWithArrowExpression()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public Foo this[int id]
            {
                get { return _internalList[id]; }
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public Foo this[int id] => _internalList[id];
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task FixReplacesPropertyBodyWithArrowExpression()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo
            {
                get { return n.Prop1 + "" "" + n.Prop2; }
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo => n.Prop1 + "" "" + n.Prop2;
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }


        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task CreateDiagnosticsWhenMethodCouldBeAnExpressionBodiedMember()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                return ""Teste"";
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = ConvertToExpressionBodiedMemberAnalyzer.DiagnosticId,
                Message = ConvertToExpressionBodiedMemberAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 13) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task CreateDiagnosticsWhenOperatorCouldBeAnExpressionBodiedMember()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static TypeName operator +(TypeName a, TypeName b) 
            {
                return a.Add(b);
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = ConvertToExpressionBodiedMemberAnalyzer.DiagnosticId,
                Message = ConvertToExpressionBodiedMemberAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 13) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task CreateDiagnosticsWhenUserDefinedConversionCouldBeAnExpressionBodiedMember()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public static implicit operator string(TypeName n) 
            { 
                return n.Prop1 + "" "" + n.Prop2;
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = ConvertToExpressionBodiedMemberAnalyzer.DiagnosticId,
                Message = ConvertToExpressionBodiedMemberAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 13) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task CreateDiagnosticsWhenIndexerCouldBeAnExpressionBodiedMember()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public Foo this[int id]
            {
                get { return _internalList[id]; }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = ConvertToExpressionBodiedMemberAnalyzer.DiagnosticId,
                Message = ConvertToExpressionBodiedMemberAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 13) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task CreateDiagnosticsWhenPropertyCouldBeAnExpressionBodiedMember()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo
            {
                get { return n.Prop1 + "" "" + n.Prop2; }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = ConvertToExpressionBodiedMemberAnalyzer.DiagnosticId,
                Message = ConvertToExpressionBodiedMemberAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 13) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task IgnoresExpressionBodiedMembers()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo() => ""Teste"";
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task IgnoresMethodsWithTwoLinesOrMore()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public override string ToString()
            {
                string s = ""s"";
                return s;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task IgnoresIndexersWithGetAndSet()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public Foo this[int id]
            {
                get { return _internalList[id]; }
                set { _internalList[id] = value; }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        public async Task IgnoresIndexersWithArrows()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public Foo this[int id] => _internalList[id];
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task IgnoresIndexersWithTwoOrMoreStatements()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public Foo this[int id]
            {
                get
                {
                    var internalId = Process(id)
                    return _internalList[internalId];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task IgnoresPropertiesWithGetAndSet()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo
            {
                get { return _foo; }
                set { _foo = value; }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        public async Task IgnoresPropertiesWithArrows()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo => _foo;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact(Skip = "Diagnostic disabled because of a bug")]
        public async Task IgnoresPropertiesWithTwoOrMoreStatements()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string Foo
            {
                get
                {
                    var foo = Process(bar);
                    return foo;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

    }
}