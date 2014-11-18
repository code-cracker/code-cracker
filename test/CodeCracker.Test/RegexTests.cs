using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class RegexTests : CodeFixVerifier
    {
        [Fact]
        public void RegexAnalyzerCreateDiagnostic()
        {
            string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                 Regex.Match("", '[([0 - 9]{ 4})]');
            }
        }
    }";
            test = test.Replace("'", "\"");
            VerifyCSharpHasNoDiagnostics(test);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new RegexAnalyzer();
        }
    }
}