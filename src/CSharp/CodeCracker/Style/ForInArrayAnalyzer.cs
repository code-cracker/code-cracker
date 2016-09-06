using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System;
using System.Collections.Generic;

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
            var iterableSymbols = (from s in forBlock.Statements.OfType<LocalDeclarationStatementSyntax>()
                                   where s.Declaration.Variables.Count == 1
                                   let declaration = s.Declaration.Variables.First()
                                   where declaration?.Initializer?.Value is ElementAccessExpressionSyntax
                                   let iterableSymbol = (ILocalSymbol)semanticModel.GetDeclaredSymbol(declaration)
                                   let iterableType = iterableSymbol.Type
                                   where !(iterableType.IsPrimitive() ^ iterableType.IsValueType)
                                   let init = (ElementAccessExpressionSyntax)declaration.Initializer.Value
                                   let initSymbol = semanticModel.GetSymbolInfo(init.ArgumentList.Arguments.First().Expression).Symbol
                                   where controlVarId.Equals(initSymbol)
                                   let someArrayInit = semanticModel.GetSymbolInfo(init.Expression).Symbol
                                   where arrayId.Equals(someArrayInit)
                                   select iterableSymbol).ToList();
            if (!iterableSymbols.Any()) return;
            if (IsIterationVariableWritten(semanticModel, forBlock, iterableSymbols)) return;
            var diagnostic = Diagnostic.Create(Rule, forStatement.ForKeyword.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsIterationVariableWritten(SemanticModel semanticModel, BlockSyntax forBlock, List<ILocalSymbol> iterableSymbols)
        {
            var forDescendants = forBlock.DescendantNodes();
            var assignments = (from assignmentExpression in forDescendants.OfType<AssignmentExpressionSyntax>()
                               let assignmentLeftSymbol = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol
                               where iterableSymbols.Any(i => i.Equals(assignmentLeftSymbol))
                               select assignmentExpression).ToList();
            if (assignments.Any()) return true;
            var refs = (from argument in forDescendants.OfType<ArgumentSyntax>()
                        where argument.RefOrOutKeyword != null
                        let argumentExpressionSymbol = semanticModel.GetSymbolInfo(argument.Expression).Symbol
                        where iterableSymbols.Any(i => i.Equals(argumentExpressionSymbol))
                        select argument).ToList();
            if (refs.Any()) return true;
            var postfixUnaries = (from postfixUnaryExpression in forDescendants.OfType<PostfixUnaryExpressionSyntax>()
                                  let operandSymbol = semanticModel.GetSymbolInfo(postfixUnaryExpression.Operand).Symbol
                                  where iterableSymbols.Any(i => i.Equals(operandSymbol))
                                  select postfixUnaryExpression).ToList();
            if (postfixUnaries.Any()) return true;
            var prefixUnaries = (from postfixUnaryExpression in forDescendants.OfType<PrefixUnaryExpressionSyntax>()
                                  let operandSymbol = semanticModel.GetSymbolInfo(postfixUnaryExpression.Operand).Symbol
                                  where iterableSymbols.Any(i => i.Equals(operandSymbol))
                                  select postfixUnaryExpression).ToList();
            if (prefixUnaries.Any()) return true;
            return false;
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