using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class MergeNestedIfTest : CodeFixVerifier<MergeNestedIfAnalyzer, MergeNestedIfCodeFixProvider>
    {
        [Fact]
        public async Task EmptyIfDoesNotCreateDiagnostic()
        {
            var test = @"
                if (true)
                {
                }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task SingleIfDoesNotCreateDiagnostic()
        {
            var test = @"
                if (true)
                {
                    var a = 1;
                }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task NestedIfCreatesDiagnostic()
        {
            var test = @"
                if (true)
                {
                    if (false)
                    {
                        var a = 1;
                    }
                }".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.MergeNestedIf.ToDiagnosticId(),
                Message = MergeNestedIfAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 17) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task NestedIfWithStatementsOnFirstIfDoesNotCreateDiagnostic()
        {
            var test = @"
                if (true)
                {
                    var a = 1;
                    if (false)
                    {
                        a = 2;
                    }
                }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfWithElseDoesNotCreateDiagnostic()
        {
            var test = @"
                if (true)
                {
                    if (false)
                    {
                        a = 2;
                    }
                }
                else
                {
                }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfWithNestedElseDoesNotCreateDiagnostic()
        {
            var test = @"
                if (true)
                {
                    if (false)
                    {
                        a = 2;
                    }
                    else
                    {
                    }
                }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task NestedIfIsFixed()
        {
            var test = @"
                //comment0
                if (true)//comment1
                {
                    //comment3
                    if (false)
                    {
                        var a = 1;//comment2
                    }
                }".WrapInCSharpMethod();
            var fixtest = @"
                //comment0
                //comment3
                if (true && false)//comment1
                {
                    var a = 1;//comment2
                }
".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task IfWithoutBlockWithNestedIfWithBlockFixed()
        {
            var test = @"
                var cond = System.DateTime.Now.Second > 1;
                //comment0
                if (true)//comment1
                    //comment3
                    if (cond)
                    {
                        var a = 1;//comment2
                    }".WrapInCSharpMethod();
            var fixtest = @"
                var cond = System.DateTime.Now.Second > 1;
                //comment0
                //comment3
                if (true && cond)//comment1
                {
                    var a = 1;//comment2
                }".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task IfWithBlockWithNestedIfWithoutBlockFixed()
        {
            var test = @"
                var cond = System.DateTime.Now.Second > 1;
                //comment0
                if (true)//comment1
                {
                    //comment3
                    if (cond)
                        var a = 1;//comment2
                }".WrapInCSharpMethod();
            var fixtest = @"
                var cond = System.DateTime.Now.Second > 1;
                //comment0
                //comment3
                if (true && cond)//comment1
                    var a = 1;
                //comment2".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task IfWithoutBlockWithNestedIfWithoutBlockFixed()
        {
            var test = @"
                var cond = System.DateTime.Now.Second > 1;
                //comment0
                if (true)//comment1
                    //comment3
                    if (cond)
                        var a = 1;//comment2".WrapInCSharpMethod();
            var fixtest = @"
                var cond = System.DateTime.Now.Second > 1;
                //comment0
                //comment3
                if (true && cond)//comment1
                    var a = 1;//comment2".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, fixtest);
        }
    }
}