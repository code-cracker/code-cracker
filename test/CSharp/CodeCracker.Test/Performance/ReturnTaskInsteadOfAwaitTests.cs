using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Performance
{
    public class ReturnTaskInsteadOfAwaitTests : DiagnosticVerifier<ReturnTaskInsteadOfAwaitAnalyzer>
    {
        [Fact]
        public async Task ReturnTaskDirectlyWithSimpleFunction()
        {
            const string test = @"
                async void FooAsync()
                {
                    Console.WriteLine(42);
                    await SomethingElseAsync();
                }
                
                async Task SomethingElseAsync()
                {
                    await Task.Delay(200);
                    Console.WriteLine(""Done Waiting"");
                }
                ";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReturnTaskInsteadOfAwait.ToDiagnosticId(),
                Message = "This method can directly return a task.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 2, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }
    }
}