using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class InterfaceNameTests : CodeFixVerifier<InterfaceNameAnalyzer, InterfaceNameCodeFixProvider>
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
                Id = DiagnosticId.InterfaceName.ToDiagnosticId(),
                Message = InterfaceNameAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 9) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ChangeInterfaceNameWithoutI()
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


        [Fact]
        public async Task ChangeInterfaceNameWithoutIAndClassImplementation()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public interface Foo
        {
            void Test();
        }

        public class Test : Foo
        {
            public void Test()
            {

            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        public interface IFoo
        {
            void Test();
        }

        public class Test : IFoo
        {
            public void Test()
            {

            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }
    }
}