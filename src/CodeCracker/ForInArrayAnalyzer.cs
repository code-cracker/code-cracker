using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ForInArrayAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CodeCracker.ForInArrayAnalyzer ";
        internal const string Title = "Use foreach";
        internal const string MessageFormat = "{0}";
        internal const string Category = "Syntax";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.ForStatement);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var forStatement = context.Node as ForStatementSyntax;
            if (forStatement == null) return;
            if (forStatement.Declaration == null || forStatement.Condition == null || !forStatement.Incrementors.Any()
                || forStatement.Declaration.Variables.Count != 1 || forStatement.Incrementors.Count != 1) return;
            var forBlock = forStatement.Statement as BlockSyntax;
            if (forBlock == null) return;
            var condition = forStatement.Condition as BinaryExpressionSyntax;
            if (!condition?.IsKind(SyntaxKind.LessThanExpression) ?? false) return;
            var arrayAccessor = condition.Right as MemberAccessExpressionSyntax;
            if (arrayAccessor == null) return;
            if (!arrayAccessor.IsKind(SyntaxKind.SimpleMemberAccessExpression)) return;
            var arrayId = context.SemanticModel.GetSymbolInfo(arrayAccessor.Expression).Symbol as ILocalSymbol;
            var literalExpression = forStatement.Declaration.Variables.Single().Initializer.Value as LiteralExpressionSyntax;
            if (!literalExpression?.IsKind(SyntaxKind.NumericLiteralExpression) ?? false) return;
            if (literalExpression.Token.ValueText != "0") return;
            var controlVarId = context.SemanticModel.GetDeclaredSymbol(forStatement.Declaration.Variables.Single());

            var arrayAccessorSymbols = (from s in forBlock.Statements.OfType<LocalDeclarationStatementSyntax>()
                                        where s.Declaration.Variables.Count == 1
                                        let declaration = s.Declaration.Variables.First()
                                        where declaration?.Initializer?.Value is ElementAccessExpressionSyntax
                                        let init = (ElementAccessExpressionSyntax)declaration.Initializer.Value
                                        let initSymbol = context.SemanticModel.GetSymbolInfo(init.ArgumentList.Arguments.First().Expression).Symbol
                                        where controlVarId.Equals(initSymbol)
                                        let someArrayInit = context.SemanticModel.GetSymbolInfo(init.Expression).Symbol as ILocalSymbol
                                        where someArrayInit.Equals(arrayId)
                                        select someArrayInit).ToList();
            if (!arrayAccessorSymbols.Any()) return;

            var diagnostic = Diagnostic.Create(Rule, forStatement.ForKeyword.GetLocation(), "You can use foreach instead of for.");
            context.ReportDiagnostic(diagnostic);
        }
    }
}