using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class ChangeAsToCastTests : CodeFixVerifier<ChangeAsToCastAnalyzer, ChangeAsToCastCodeFixProvider>
    {
        [Fact]
        public async Task As_Should_Create_Diagnostic()
        {
            const string test = @"object o = ""hello"";
string s = o as string;";
            var diagnostic = new DiagnosticResult
            {
                Id = DiagnosticId.ChangeAsToCast.ToDiagnosticId(),
                Message = ChangeAsToCastAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 12) }
            };
            await VerifyCSharpDiagnosticAsync(test.WrapInCSharpMethod(), diagnostic);
        }

        [Fact]
        public async Task Cast_To_Reference_Type_Should_Create_Diagnostic()
        {
            const string test = @"object o = ""hello"";
string s = (string)o;";
            var diagnostic = new DiagnosticResult
            {
                Id = DiagnosticId.ChangeAsToCast.ToDiagnosticId(),
                Message = ChangeAsToCastAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 12) }
            };
            await VerifyCSharpDiagnosticAsync(test.WrapInCSharpMethod(), diagnostic);
        }

        [Fact]
        public async Task Cast_To_Value_Type_Should_Not_Create_Diagnostic()
        {
            const string test = @"object o = 42;
int s = (int)o;";

            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInCSharpMethod());
        }

        [Fact]
        public async Task CodeFix_Should_Replace_As_With_Cast()
        {
            const string oldCode = @"object o = ""hello"";
string s = o as string;";
            const string newCode = @"object o = ""hello"";
string s = (string)o;";
            await VerifyCSharpFixAsync(oldCode.WrapInCSharpMethod(), newCode.WrapInCSharpMethod());
        }

        [Fact]
        public async Task CodeFix_Should_Replace_Cast_With_As()
        {
            const string oldCode = @"object o = ""hello"";
string s = (string)o;";
            const string newCode = @"object o = ""hello"";
string s = o as string;";
            await VerifyCSharpFixAsync(oldCode.WrapInCSharpMethod(), newCode.WrapInCSharpMethod());
        }
    }
}
