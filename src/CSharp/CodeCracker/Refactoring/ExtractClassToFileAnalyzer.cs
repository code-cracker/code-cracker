using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System;

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

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxTreeAction(AnalyzeTree);

        private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetRoot();
            var classes = root.DescendantNodes(c => !c.IsKind(SyntaxKind.ClassDeclaration)).Where(cl => cl.IsKind(SyntaxKind.ClassDeclaration)).ToList();
            if (classes.Count <= 1) return;
            foreach (ClassDeclarationSyntax c in classes)
            {
                var diagnostic = Diagnostic.Create(Rule, c.GetLocation(), c.Identifier.Text);
                context.ReportDiagnostic(diagnostic);
            }


        }

        //private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        //{
        //    if (context.IsGenerated()) return;
        //    var declaration = context.Node as ClassDeclarationSyntax;
        //    var classSymbol = context.SemanticModel.GetDeclaredSymbol(declaration);

        //    var namespaceDeclaration = declaration.Parent;
        //    var classCount = namespaceDeclaration.ChildNodes().Where(cl => cl.IsKind(SyntaxKind.ClassDeclaration));
        //    if (classCount.Count() == 1) return;

        //    var diagnostic = Diagnostic.Create(Rule, declaration.GetLocation(), declaration.Identifier.Text);
        //    context.ReportDiagnostic(diagnostic);
        //}
    }
}