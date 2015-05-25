using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class RemoveRedundantElseClauseTests : CodeFixVerifier<RemoveRedundantElseClauseAnalyzer, RemoveRedundantElseClauseCodeFixProvider>
    {
        [Fact]
        public async Task FixEmptyElse()
        {
            var test = WrapInCSharpMethod(@"
            if (1 == 2)
            {
            return 1;
            }
            else
            {
            }");
            var fixtest = WrapInCSharpMethod(@"
            if (1 == 2)
            {
            return 1;
            }");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixEmptyElseWhenThereIsNoSignificantCodeInsideItsBlock()
        {
            var test = WrapInCSharpMethod(@"
            if (1 == 2)
            {
            return 1;
            }
            else
            {
            //var a = 2;
            //var b = 3;
            //return b;
            }");
            var fixtest = WrapInCSharpMethod(@"
            if (1 == 2)
            {
            return 1;
            }");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task IgnoreWhenThereIsNoSignificantCodeInsideIfBlock()
        {
            var test = WrapInCSharpMethod(@"
            if (1 == 2)
            {
                //var a = 2;
                //return 1;                
            }
            else
            {
            }");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task CreateDiagnosticsWhenEmptyElse()
        {
            var test = WrapInCSharpMethod(@"if(1 == 2){ return 1; } else { }");

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.RemoveRedundantElseClause.ToDiagnosticId(),
                Message = "Remove redundant else",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 57) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        private string WrapInCSharpMethod(string code)
        {
            return @"
                    using System;

                    namespace ConsoleApplication1
                    {
                        class TypeName
                        {
                            public int Foo()
                            {
                                " + code + @"
                            }
                        }
                    }";
        }
    }
}
