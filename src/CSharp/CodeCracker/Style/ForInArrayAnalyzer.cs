using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ForInArrayAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Use foreach";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Style;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ForInArray.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ForInArray));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.ForStatement);

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var forStatement = context.Node as ForStatementSyntax;
            if (forStatement == null) return;
            if (forStatement.Declaration == null || forStatement.Condition == null || !forStatement.Incrementors.Any()
                || forStatement.Declaration.Variables.Count != 1 || forStatement.Incrementors.Count != 1) return;
            var forBlock = forStatement.Statement as BlockSyntax;
            if (forBlock == null) return;
            var condition = forStatement.Condition as BinaryExpressionSyntax;
            if (condition == null || !condition.IsKind(SyntaxKind.LessThanExpression)) return;
            var arrayAccessor = condition.Right as MemberAccessExpressionSyntax;
            if (arrayAccessor == null) return;
            if (!arrayAccessor.IsKind(SyntaxKind.SimpleMemberAccessExpression)) return;
            var arrayId = context.SemanticModel.GetSymbolInfo(arrayAccessor.Expression).Symbol as ILocalSymbol;
            var literalExpression = forStatement.Declaration.Variables.Single().Initializer.Value as LiteralExpressionSyntax;
            if (literalExpression == null || !literalExpression.IsKind(SyntaxKind.NumericLiteralExpression)) return;
            if (literalExpression.Token.ValueText != "0") return;
            var controlVarId = context.SemanticModel.GetDeclaredSymbol(forStatement.Declaration.Variables.Single());
            var otherUsesOfIndexToken = forBlock.DescendantTokens().Count(t => t.Text == forStatement.Declaration.Variables.Single().Identifier.Text);
            if (otherUsesOfIndexToken != 1) return;

            var arrayAccessorSymbols = (from s in forBlock.Statements.OfType<LocalDeclarationStatementSyntax>()
                                        where s.Declaration.Variables.Count == 1
                                        let declaration = s.Declaration.Variables.First()
                                        where declaration?.Initializer?.Value is ElementAccessExpressionSyntax
                                        let init = (ElementAccessExpressionSyntax)declaration.Initializer.Value
                                        let initSymbol = context.SemanticModel.GetSymbolInfo(init.ArgumentList.Arguments.First().Expression).Symbol
                                        where controlVarId.Equals(initSymbol)
                                        let someArrayInit = context.SemanticModel.GetSymbolInfo(init.Expression).Symbol as ILocalSymbol
                                        where someArrayInit == null || someArrayInit.Equals(arrayId)
                                        select arrayId).ToList();
            if (!arrayAccessorSymbols.Any()) return;

            var diagnostic = Diagnostic.Create(Rule, forStatement.ForKeyword.GetLocation(), "You can use foreach instead of for.");
            context.ReportDiagnostic(diagnostic);
        }
    }
}