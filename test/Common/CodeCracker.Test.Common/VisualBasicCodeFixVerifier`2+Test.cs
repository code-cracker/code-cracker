using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.VisualBasic;

namespace CodeCracker.Test
{
    public static partial class VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix>
    {
        public class Test : CodeFixTest<XUnitVerifier>
        {
            public override string Language => LanguageNames.VisualBasic;

            protected override string DefaultFileExt => "vb";

            protected override CompilationOptions CreateCompilationOptions()
                => new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
            {
                yield return new TAnalyzer();
            }

            protected override IEnumerable<CodeFixProvider> GetCodeFixProviders()
            {
                yield return new TCodeFix();
            }
        }
    }
}
