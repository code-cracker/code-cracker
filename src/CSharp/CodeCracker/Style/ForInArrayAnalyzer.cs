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
        internal const string MessageFormat = "You can use foreach instead of for.";
        internal const string Category = SupportedCategories.Style;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
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

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
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
            var semanticModel = context.SemanticModel;
            var arrayId = semanticModel.GetSymbolInfo(arrayAccessor.Expression).Symbol;
            if (arrayId == null) return;
            if (!IsEnumerable(arrayId)) return;
            var forVariable = forStatement.Declaration.Variables.First();
            var literalExpression = forVariable.Initializer.Value as LiteralExpressionSyntax;
            if (literalExpression == null || !literalExpression.IsKind(SyntaxKind.NumericLiteralExpression)) return;
            if (literalExpression.Token.ValueText != "0") return;
            var controlVarId = semanticModel.GetDeclaredSymbol(forVariable);
            var otherUsesOfIndexToken = forBlock.DescendantTokens().Count(t =>
                {
                    if (t.Text != forVariable.Identifier.Text) return false;
                    var elementAccess = t.GetAncestor<ElementAccessExpressionSyntax>();
                    if (elementAccess == null) return true;
                    var assignment = elementAccess.Parent as AssignmentExpressionSyntax;
                    if (assignment != null && assignment.Left == elementAccess) return true;
                    var accessIdentifier = elementAccess.Expression as IdentifierNameSyntax;
                    if (accessIdentifier == null) return true;
                    var identifierSymbol = semanticModel.GetSymbolInfo(accessIdentifier).Symbol;
                    if (identifierSymbol == null) return true;
                    return !identifierSymbol.Equals(arrayId);
                });
            if (otherUsesOfIndexToken != 0) return;
            var arrayAccessorSymbols = (from s in forBlock.Statements.OfType<LocalDeclarationStatementSyntax>()
                                        where s.Declaration.Variables.Count == 1
                                        let declaration = s.Declaration.Variables.First()
                                        where declaration?.Initializer?.Value is ElementAccessExpressionSyntax
                                        let init = (ElementAccessExpressionSyntax)declaration.Initializer.Value
                                        let initSymbol = semanticModel.GetSymbolInfo(init.ArgumentList.Arguments.First().Expression).Symbol
                                        where controlVarId.Equals(initSymbol)
                                        let someArrayInit = semanticModel.GetSymbolInfo(init.Expression).Symbol
                                        where arrayId.Equals(someArrayInit)
                                        select arrayId).ToList();
            if (!arrayAccessorSymbols.Any()) return;
            var valueTypeVariable = (from s in forBlock.Statements.OfType<LocalDeclarationStatementSyntax>()
                                     where s.Declaration.Variables.Count == 1
                                     let symbol = semanticModel.GetDeclaredSymbol(s.Declaration.Variables.First()) as ISymbol
                                     where ((ILocalSymbol)symbol).Type.IsValueType
                                     select s).ToList();
            
            foreach (var statement in forBlock.Statements.OfType<ExpressionStatementSyntax>())
            {
                var left = (statement.Expression as AssignmentExpressionSyntax)?.Left;
                var memberAccess = (left as MemberAccessExpressionSyntax)?.Expression;
                var identifier = "";
                if (memberAccess?.GetType()?.Name == "IdentifierNameSyntax")
                {
                    identifier = (memberAccess as IdentifierNameSyntax)?.Identifier.Text ?? "";
                }
                else
                {
                    var element = (memberAccess as ElementAccessExpressionSyntax)?.Expression;
                    identifier = (element as IdentifierNameSyntax)?.Identifier.Text ?? "";
                }
                if (identifier == arrayId.Name && valueTypeVariable.Any()) return;
            }
            var diagnostic = Diagnostic.Create(Rule, forStatement.ForKeyword.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsEnumerable(ISymbol arrayId)
        {
            var type = (arrayId as ILocalSymbol)?.Type
                ?? (arrayId as IParameterSymbol)?.Type
                ?? (arrayId as IPropertySymbol)?.Type
                ?? (arrayId as IFieldSymbol)?.Type;
            if (type == null) return false;
            if (type.AllInterfaces.Any(i => i.ToString() == "System.Collections.IEnumerable")) return true;
            var allReturnTypes = type.GetMembers("GetEnumerator")
                .Select(m => m as IMethodSymbol)
                .Where(m => m != null)
                .Select(m => m.ReturnType).ToList();
            if (allReturnTypes.Any(t => t.ToString() == "System.Collections.IEnumerator")) return true;
            var hasGetEnumerator = allReturnTypes.SelectMany(t => t.AllInterfaces)
                .Distinct()
                .Any(i => i.ToString() == "System.Collections.IEnumerator");
            return hasGetEnumerator;
        }
    }
}