using Xunit;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using CodeCracker.CSharp.Refactoring;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class SortUsingsTest : CodeFixVerifier<SortUsingsAnalyzer, SortUsingsCodeFixProvider>
    {
        [Fact]
        public async Task CreateDiagnosticSortingUsings()
        {
            const string usings = @"using System;
using System.Collections.Generic;
using System.Linq;
using static System.Console;
using System.Text;
using static System.DateTime;
using System.Threading.Tasks;";

            const string source = @"static void Main(string[] args)
{
}";

            var diagnostic = new DiagnosticResult
            {
                Id = DiagnosticId.SortUsings.ToDiagnosticId(),
                Message = "Sort Using directives by length",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 2, 5) }
            };
            await VerifyCSharpDiagnosticAsync(source.WrapInCSharpClass("Program", usings), diagnostic);
        }

        [Fact]
        public async Task FixSortingUsings()
        {
            const string actualUsings = @"using System;
using System.Collections.Generic;
using System.Linq;
using static System.Console;
using System.Text;
using static System.DateTime;
using System.Threading.Tasks;";

            const string expectedUsings = @"using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using static System.Console;
using static System.DateTime;";

            const string code = @"static void Main(string[] args)
{
}";
            const string typeName = "Program";

            var oldSource = code.WrapInCSharpClass(typeName, actualUsings);
            var newSource = code.WrapInCSharpClass(typeName, expectedUsings);
            await VerifyCSharpFixAsync(oldSource, newSource);
        }

    }
}
