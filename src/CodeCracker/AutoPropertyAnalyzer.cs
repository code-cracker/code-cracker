using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AutoPropertyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CodeCracker.AutoPropertyAnalyzer";
        internal const string Title = "Use auto properties when possible";
        internal const string MessageFormat = "Use auto properties when possible.";
        internal const string Category = "Structure";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            throw new NotImplementedException();
        }
    }
}
