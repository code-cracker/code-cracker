using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
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
            var expected = new DiagnosticResult(diagnosticId.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(13, column)
                .WithMessage(diagnosticId == DiagnosticId.ChangeAnyToAll ? ChangeAnyToAllAnalyzer.MessageAny : ChangeAnyToAllAnalyzer.MessageAll);
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

        [Theory]
        [InlineData("Any", DiagnosticId.ChangeAnyToAll)]
        [InlineData("All", DiagnosticId.ChangeAllToAny)]
        public async Task WithElvisOperatorCreatesDiagnostic(string methodName, DiagnosticId diagnosticId)
        {
            var source = $@"
using System;
using System.Linq;
class TypeSymbol
{{
    public System.Collections.Generic.IList<int> AllInterfaces;
}}
class TypeName
{{
    void Foo()
    {{
        var typeSymbol = new TypeSymbol();
        var y = typeSymbol?.AllInterfaces.{methodName}(i => i == 1);
    }}
}}";
            var expected = new DiagnosticResult(diagnosticId.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(13, 43)
                .WithMessage(diagnosticId == DiagnosticId.ChangeAnyToAll ? ChangeAnyToAllAnalyzer.MessageAny : ChangeAnyToAllAnalyzer.MessageAll);
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Theory]
        [InlineData("typeSymbol?.AllInterfaces.Any(i => i == 1)", "!typeSymbol?.AllInterfaces.All(i => i != 1)")]
        [InlineData("!typeSymbol?.AllInterfaces.All(i => i != 1)", "typeSymbol?.AllInterfaces.Any(i => i == 1)")]
        public async Task FixesWithElvisOperator(string code, string fixedCode)
        {
            const string source = @"
using System;
using System.Linq;
class TypeSymbol
{{
    public System.Collections.Generic.IList<int> AllInterfaces;
}}
class TypeName
{{
    void Foo()
    {{
        var typeSymbol = new TypeSymbol();
        var y = {0};
    }}
}}";
            await VerifyCSharpFixAsync(string.Format(source, code), string.Format(source, fixedCode));
        }

        [Theory]
        [InlineData("Any", DiagnosticId.ChangeAnyToAll)]
        [InlineData("All", DiagnosticId.ChangeAllToAny)]
        public async Task ExpressionBodiedMemberCreatesDiagnostic(string methodName, DiagnosticId diagnosticId)
        {
            var source = $@"
using System;
using System.Linq;
class TypeName
{{
    private System.Collections.Generic.IList<int> xs;
    bool Foo() => xs.{methodName}(i => i == 1);
}}";
            var expected = new DiagnosticResult(diagnosticId.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(7, 22)
                .WithMessage(diagnosticId == DiagnosticId.ChangeAnyToAll ? ChangeAnyToAllAnalyzer.MessageAny : ChangeAnyToAllAnalyzer.MessageAll);
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Theory]
        [InlineData("xs.Any(i => i == 1)", "!xs.All(i => i != 1)")]
        [InlineData("!xs.All(i => i != 1)", "xs.Any(i => i == 1)")]
        public async Task FixesExpressionBodiedMember(string code, string fixedCode)
        {
            const string source = @"
using System;
using System.Linq;
class TypeName
{{
    private System.Collections.Generic.IList<int> xs;
    bool Foo() => {0};
}}";
            await VerifyCSharpFixAsync(string.Format(source, code), string.Format(source, fixedCode));
        }

        [Theory]
        [InlineData("Any", DiagnosticId.ChangeAnyToAll)]
        [InlineData("All", DiagnosticId.ChangeAllToAny)]
        public async Task NegationWithCoalesceExpressionCreatesDiagnostic(string methodName, DiagnosticId diagnosticId)
        {
            var source = $@"
var ints = new [] {{ 1 }};
var query = !ints?.{methodName}(i => i == 1) ?? true;";
            var expected = new DiagnosticResult(diagnosticId.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(13, 20)
                .WithMessage(diagnosticId == DiagnosticId.ChangeAnyToAll ? ChangeAnyToAllAnalyzer.MessageAny : ChangeAnyToAllAnalyzer.MessageAll);
            await VerifyCSharpDiagnosticAsync(source.WrapInCSharpMethod(usings: "\nusing System.Linq;"), expected);
        }

        [Theory]
        [InlineData(@"
            var ints = new [] {1, 2};
            var query = !ints?.Any(i => i == 1) ?? true;", @"
            var ints = new [] {1, 2};
            var query = ints?.All(i => i != 1) ?? true;")]
        public async Task FixesNegationWithCoalesceExpression(string original, string fix) =>
            await VerifyCSharpFixAsync(original.WrapInCSharpMethod(usings: "\nusing System.Linq;"),
                fix.WrapInCSharpMethod(usings: "\nusing System.Linq;"));
    }
}