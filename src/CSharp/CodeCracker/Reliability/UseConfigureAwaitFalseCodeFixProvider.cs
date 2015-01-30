using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeCracker.Reliability
{
    public class UseConfigureAwaitFalseCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            throw new NotImplementedException();
        }

        public override Task ComputeFixesAsync(CodeFixContext context)
        {
            throw new NotImplementedException();
        }
    }
}
