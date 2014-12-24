using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AbstractClassShouldNotHavePublicCtorsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0060";
        internal const string Title = "Abastract class should not have public constructors.";
        internal const string MessageFormat = "Constructor should not be public.";
        internal const string Category = SupportedCategories.Usage;
        
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ConstructorDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var ctor = (ConstructorDeclarationSyntax)context.Node;
            if (!ctor.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))) return;
            
            var @class = ctor.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (@class == null) return;
            if (!@class.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword))) return;
            
            var diagnostic = Diagnostic.Create(Rule, ctor.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
