using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExtractClassToFileAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Extract Class to New File";
        internal const string MessageFormat = "Extract class '{0}' to new file.";
        internal const string Category = SupportedCategories.Refactoring;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ExtractClassToFile.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ExtractClassToFile));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var declaration = context.Node as ClassDeclarationSyntax;
            if (!declaration.Modifiers.Any(SyntaxKind.PublicKeyword)) return;

            var classSymbol = context.SemanticModel.GetDeclaredSymbol(declaration);
            if (classSymbol.ContainingNamespace.Name == "") return;

            var namespaceDeclaration = declaration.Parent as NamespaceDeclarationSyntax;
            var classCount = namespaceDeclaration.Members.Where(cl => cl.GetType().ToString().Contains(nameof(ClassDeclarationSyntax)));
            if (classCount.Count() == 1) return;

            var diagnostic = Diagnostic.Create(Rule, declaration.GetLocation(), declaration.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }
    }
}