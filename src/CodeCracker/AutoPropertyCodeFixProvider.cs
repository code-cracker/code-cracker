using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace CodeCracker
{
    [ExportCodeFixProvider("CodeCrackerAutoPropertyCodeFixProvider", LanguageNames.CSharp), Shared]
    public class AutoPropertyCodeFixProvider : CodeFixProvider
    {
        public override Task ComputeFixesAsync(CodeFixContext context)
        {
            throw new NotImplementedException();
        }

        public override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(AutoPropertyAnalyzer.DiagnosticId);
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
