using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class ChangeAnyToAllTests : CodeFixVerifier<ChangeAnyToAllAnalyzer, ChangeAnyToAllCodeFixProvider>
    {
        [Theory]
        [InlineData(@"
            var ints = new [] {1, 2}.AsQueryable();
            var query = ints.Any(i => i > 0);", 30, DiagnosticId.ChangeAnyToAll)]
        [InlineData(@"
            var ints = new [] {1, 2}.AsQueryable();
            var query = !ints.Any(i => i > 0);", 31, DiagnosticId.ChangeAnyToAll)]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => i > 0);", 30, DiagnosticId.ChangeAnyToAll)]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = !ints.Any(i => i > 0);", 31, DiagnosticId.ChangeAnyToAll)]
        [InlineData(@"
            var ints = new [] {1, 2}.AsQueryable();
            var query = !ints.All(i => i > 0);", 31, DiagnosticId.ChangeAllToAny)]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = !ints.All(i => i > 0);", 31, DiagnosticId.ChangeAllToAny)]
        public async Task AnyAndAllWithLinqCreatesDiagnostic(string code, int column, DiagnosticId diagnosticId)
        {
            var source = code.WrapInCSharpMethod(usings: "\nusing System.Linq;");
            var expected = new DiagnosticResult
            {
                Id = diagnosticId.ToDiagnosticId(),
                Message = diagnosticId == DiagnosticId.ChangeAnyToAll ? ChangeAnyToAllAnalyzer.MessageAny : ChangeAnyToAllAnalyzer.MessageAll,
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, column) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ComplexLambdaDoesNotCreateDiagnostic()
        {
            var source = @"
            var ints = new [] {1, 2};
            var notAll = !ints.All(i => { return i > 0; } );".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task InvokingMethodThatIsNotAnyDoesNotCreateDiagnostic()
        {
            var source = "Console.WriteLine(1);".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ConvertsConditionalExpression()
        {
            var original = @"
            var ints = new [] {1, 2};
            var query = ints.Any(i => true ? true : false);".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            var fix = @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => (true ? true : false) == false);".WrapInCSharpMethod(usings: "\nusing System.Linq;");
            await VerifyCSharpFixAsync(original, fix);
        }

        [Theory]
        [InlineData(@"
            var ints = new [] {1, 2};
            ints.Any(i => true);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            ints.All(i => true);")]
        public async Task ExpressionStatementsDoNotCreateDiagnostic(string code)
        {
            var original = code.WrapInCSharpMethod(usings: "\nusing System.Linq;");
            await VerifyCSharpHasNoDiagnosticsAsync(original);
        }

        [Theory]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => i > 1);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => i <= 1);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => i >= 1);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => i < 1);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => i < 1);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => i >= 1);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => i <= 1);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => i > 1);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => i == 1);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => i != 1);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => i != 1);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => i == 1);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => !true);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => true);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => !(i == 1));", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => i == 1);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => true);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => false);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => false);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => true);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = !ints.Any(i => false);", @"
            var ints = new [] {1, 2};
            var query = ints.All(i => true);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => (i == 1) == true);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => (i == 1) == false);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => true == (i == 1));", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => false == (i == 1));")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => (i == 1) == false);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => i == 1);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => false == (i == 1));", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => i == 1);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => (i == 1) != true);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => i == 1);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => true != (i == 1));", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => i == 1);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => (i == 1) != false);", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => (i == 1) == false);")]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = ints.Any(i => false != (i == 1));", @"
            var ints = new [] {1, 2};
            var query = !ints.All(i => false == (i == 1));")]
        public async Task ConvertsSpecialCases(string original, string fix) =>
            await VerifyCSharpFixAsync(original.WrapInCSharpMethod(usings: "\nusing System.Linq;"),
                fix.WrapInCSharpMethod(usings: "\nusing System.Linq;"));
    }
}