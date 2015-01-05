using CodeCracker.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Usage
{
    public class InterfaceNameTests : CodeFixTest<InterfaceNameAnalyzer, InterfaceNameCodeFixProvider>
    {
        [Fact]
        public async Task InterfaceNameStartsWithLetterI()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public interface IFoo
        {
            void Test();
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task InterfaceNameNotStartsWithLetterI()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public interface Foo
        {
            void Test();
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = InterfaceNameAnalyzer.DiagnosticId,
                Message = InterfaceNameAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 9) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ChangeFieldAssignedOnDeclarationToReadonly()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public interface Foo
        {
            void Test();
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        public interface IFoo
        {
            void Test();
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }
    }
}