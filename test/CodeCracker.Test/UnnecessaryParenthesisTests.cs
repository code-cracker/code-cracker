using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class UnnecessaryParenthesisTests : CodeFixVerifier
    {
        [Fact]
        public void ConstructorWithEmptyParenthesisWithInitializerTriggersFix()
        {
            const string source = @"var a = new B() { X = 1 };";
            var expected = new DiagnosticResult
            {
                Id = UnnecessaryParenthesisAnalyzer.DiagnosticId,
                Message = "Remove unnecessary parenthesis.",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 1, 14)
                        }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void ConstructorWithoutParenthesisWithInitializerIsIgnored()
        {
            const string source = @"new B { X = 1 };";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ConstructorWithEmptyParenthesisWithoutInitializerIsIgnored()
        {
            const string source = @"new B();";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ConstructorWithArgumentsWithInitializerIsIgnored()
        {
            const string source = @"new Sample(1) { A = 2 };";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ConstructorWithArgumentsWithoutInitializerIsIgnored()
        {
            const string source = @"new Sample(1);";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void ConstructorWithoutArgumentsWithEmptyInitializerIsIgnored()
        {
            const string source = @"new Sample { };";
            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void RemoveUnnecessaryParenthesisInConstructorCall()
        {
            const string oldSource = @"var a = new B() { X = 1 };";
            const string newSource = @"var a = new B { X = 1 };";

            VerifyCSharpFix(oldSource, newSource);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UnnecessaryParenthesisAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new UnnecessaryParenthesisCodeFixProvider();
        }
    }
}
