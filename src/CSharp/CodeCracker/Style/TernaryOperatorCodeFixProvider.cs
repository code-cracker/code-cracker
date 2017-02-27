using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TernaryOperatorWithReturnCodeFixProvider)), Shared]
    public class TernaryOperatorWithReturnCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.TernaryOperator_Return.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Change to ternary operator", c => MakeTernaryAsync(context.Document, diagnostic, c), nameof(TernaryOperatorWithReturnCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> MakeTernaryAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var ifStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();
            var returnStatementInsideIf = (ReturnStatementSyntax)(ifStatement.Statement is BlockSyntax ? ((BlockSyntax)ifStatement.Statement).Statements.Single() : ifStatement.Statement);
            var elseStatement = ifStatement.Else;
            var returnStatementInsideElse = (ReturnStatementSyntax)(elseStatement.Statement is BlockSyntax ? ((BlockSyntax)elseStatement.Statement).Statements.Single() : elseStatement.Statement);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var conditionalExpression = TernaryOperatorCodeFixHelper.CreateExpressions(returnStatementInsideIf.Expression, returnStatementInsideElse.Expression, ifStatement.Condition, semanticModel);
            var ternary =
                SyntaxFactory.ReturnStatement(
                    conditionalExpression)
                    .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                    .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(ifStatement, ternary);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TernaryOperatorWithAssignmentCodeFixProvider)), Shared]
    public class TernaryOperatorWithAssignmentCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.TernaryOperator_Assignment.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Change to ternary operator", c => MakeTernaryAsync(context.Document, diagnostic, c), nameof(TernaryOperatorWithAssignmentCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> MakeTernaryAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var ifStatement = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();
            var expressionInsideIf = (ExpressionStatementSyntax)(ifStatement.Statement is BlockSyntax ? ((BlockSyntax)ifStatement.Statement).Statements.Single() : ifStatement.Statement);
            var elseStatement = ifStatement.Else;
            var expressionInsideElse = (ExpressionStatementSyntax)(elseStatement.Statement is BlockSyntax ? ((BlockSyntax)elseStatement.Statement).Statements.Single() : elseStatement.Statement);
            var assignmentExpressionInsideIf = (AssignmentExpressionSyntax)expressionInsideIf.Expression;
            var assignmentExpressionInsideElse = (AssignmentExpressionSyntax)expressionInsideElse.Expression;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var conditionalExpression = TernaryOperatorCodeFixHelper.CreateExpressions(assignmentExpressionInsideIf.Right, assignmentExpressionInsideElse.Right, ifStatement.Condition, semanticModel);
            var ternary =
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        assignmentExpressionInsideIf.Kind(),
                        assignmentExpressionInsideIf.Left,
                        conditionalExpression))
                    .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                    .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(ifStatement, ternary);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }

    internal static class TernaryOperatorCodeFixHelper
    {
        public static ExpressionSyntax CreateExpressions(ExpressionSyntax ifExpression, ExpressionSyntax elseExpression, ExpressionSyntax ifStatementCondition, SemanticModel semanticModel)
        {

            var methodCallSimplification = TrySimplifyMethodCalls(ifStatementCondition, ifExpression, elseExpression, semanticModel);
            if (methodCallSimplification != null) return methodCallSimplification;
            var ifTypeInfo = semanticModel.GetTypeInfo(ifExpression);
            var elseTypeInfo = semanticModel.GetTypeInfo(elseExpression);
            var typeSyntax = SyntaxFactory.IdentifierName(ifTypeInfo.ConvertedType.ToMinimalDisplayString(semanticModel, ifExpression.SpanStart));
            ExpressionSyntax trueExpression; ExpressionSyntax falseExpression;
            CreateExpressions(ifExpression, elseExpression, ifTypeInfo.Type, elseTypeInfo.Type,
                ifTypeInfo.ConvertedType, elseTypeInfo.ConvertedType, typeSyntax, semanticModel, out trueExpression, out falseExpression);
            return SyntaxFactory.ConditionalExpression(ifStatementCondition, trueExpression, falseExpression);
        }

        private static ExpressionSyntax TrySimplifyMethodCalls(ExpressionSyntax ifStatementCondition, ExpressionSyntax ifExpression, ExpressionSyntax elseExpression, SemanticModel semanticModel)
        {
            if (ifExpression is InvocationExpressionSyntax && elseExpression is InvocationExpressionSyntax)
            {
                var ifInvocation = ifExpression as InvocationExpressionSyntax;
                var elseInvocation = elseExpression as InvocationExpressionSyntax;
                var ifMethodinfo = semanticModel.GetSymbolInfo(ifInvocation.Expression);
                var elseMethodinfo = semanticModel.GetSymbolInfo(elseInvocation.Expression);
                if (object.Equals(ifMethodinfo, elseMethodinfo)) //same method and overload
                {
                    var findSingleArgumentIndexThatDiffers = FindSingleArgumentIndexThatDiffers(ifInvocation.ArgumentList, elseInvocation.ArgumentList, semanticModel);
                    if (findSingleArgumentIndexThatDiffers >= 0)
                        return SyntaxFactory.InvocationExpression(ifInvocation.Expression, CreateMethodArgumentList(ifStatementCondition, ifInvocation.ArgumentList, elseInvocation.ArgumentList, findSingleArgumentIndexThatDiffers, semanticModel));
                }
            }
            return null;
        }

        private static ArgumentListSyntax CreateMethodArgumentList(ExpressionSyntax ifStatementCondition, ArgumentListSyntax argList1, ArgumentListSyntax argList2, int argumentIndexThatDiffers, SemanticModel semanticModel)
        {
            var zipped = argList1.Arguments.Zip(argList2.Arguments, (a1, a2) => new { a1, a2 }).Select((a, i) => new { a.a1, a.a2, i });
            var argSelector = zipped.Select((args, i) =>
                    (i == argumentIndexThatDiffers) ?
                    SyntaxFactory.Argument(GetConditionalExpressionForArgument(ifStatementCondition, args.a1.Expression, args.a2.Expression, semanticModel))
                    : args.a1);
            return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argSelector));
        }

        private static ConditionalExpressionSyntax GetConditionalExpressionForArgument(ExpressionSyntax ifStatementCondition, ExpressionSyntax ifExpression, ExpressionSyntax elseExpression, SemanticModel semanticModel)
        {
            var ifTypeInfo = semanticModel.GetTypeInfo(ifExpression);
            var elseTypeInfo = semanticModel.GetTypeInfo(elseExpression);
            var typeSyntax = SyntaxFactory.IdentifierName(ifTypeInfo.ConvertedType.ToMinimalDisplayString(semanticModel, ifExpression.SpanStart));
            ExpressionSyntax trueExpression; ExpressionSyntax falseExpression;
            CreateExpressions(ifExpression, elseExpression, ifTypeInfo.Type, elseTypeInfo.Type,
                ifTypeInfo.ConvertedType, elseTypeInfo.ConvertedType, typeSyntax, semanticModel, out trueExpression, out falseExpression);
            return SyntaxFactory.ConditionalExpression(ifStatementCondition, trueExpression, falseExpression);
        }

        private static int FindSingleArgumentIndexThatDiffers(ArgumentListSyntax argList1, ArgumentListSyntax argList2, SemanticModel semanticModel)
        {
            var zipped = argList1.Arguments.Zip(argList2.Arguments, (a1, a2) => new { a1, a2 }).Select((a, i) => new { a.a1, a.a2, i });
            var singleMissmatch = zipped.Where(args =>
            {
                var a1Text = args.a1.GetText();
                var a2Text = args.a2.GetText();
                return !a1Text.ContentEquals(a2Text);
            }).Take(2).ToList();
            return (singleMissmatch.Count == 0 || singleMissmatch.Count > 1) ? -1 : singleMissmatch[0].i;
        }

        private static void CreateExpressions(ExpressionSyntax ifExpression, ExpressionSyntax elseExpression,
            ITypeSymbol ifType, ITypeSymbol elseType,
            ITypeSymbol ifConvertedType, ITypeSymbol elseConvertedType,
            TypeSyntax typeSyntax, SemanticModel semanticModel,
            out ExpressionSyntax trueExpression, out ExpressionSyntax falseExpression)
        {
            var isNullable = false;
            if (ifConvertedType?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                var constValue = semanticModel.GetConstantValue(ifExpression);
                trueExpression = constValue.HasValue && constValue.Value == null
                    ? SyntaxFactory.CastExpression(typeSyntax, ifExpression)
                    : ifExpression;
                isNullable = true;
            }
            else
            {
                trueExpression = ifExpression;
            }
            if (elseConvertedType?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                var constValue = semanticModel.GetConstantValue(elseExpression);
                falseExpression = constValue.HasValue && constValue.Value == null
                    ? SyntaxFactory.CastExpression(typeSyntax, elseExpression)
                    : elseExpression;
                isNullable = true;
            }
            else
            {
                falseExpression = elseExpression;
            }
            if (!elseType.HasImplicitNumericConversion(ifType)
                && !IsEnumAndZero(ifType, elseExpression)
                && !IsEnumAndZero(elseType, ifExpression)
                && (!isNullable && !ifType.CanBeAssignedTo(elseType) || !elseType.CanBeAssignedTo(ifType)))
                trueExpression = CastToBaseType(ifExpression, ifType, elseType, trueExpression);
        }

        private static bool IsEnumAndZero(ITypeSymbol type, ExpressionSyntax expression) =>
            type?.BaseType?.SpecialType == SpecialType.System_Enum && expression?.ToString() == "0";

        private static ExpressionSyntax CastToBaseType(ExpressionSyntax ifExpression, ITypeSymbol ifType, ITypeSymbol elseType, ExpressionSyntax trueExpression)
        {
            var commonBaseType = ifType.GetCommonBaseType(elseType);
            if (commonBaseType.Equals(ifType)) return trueExpression;
            if (commonBaseType != null)
                trueExpression = SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(commonBaseType.Name).WithAdditionalAnnotations(Simplifier.Annotation), ifExpression);
            return trueExpression;
        }

        private static bool CanBeAssignedTo(this ITypeSymbol type, ITypeSymbol possibleBaseType)
        {
            if (type == null || possibleBaseType == null) return true;
            if (type.Kind == SymbolKind.ErrorType || possibleBaseType.Kind == SymbolKind.ErrorType) return true;
            if (type == null || possibleBaseType == null) return true;
            if (type.SpecialType == SpecialType.System_Object) return true;
            var baseType = type;
            while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
            {
                if (baseType.Equals(possibleBaseType)) return true;
                baseType = baseType.BaseType;
            }
            return false;
        }

        private static ITypeSymbol GetCommonBaseType(this ITypeSymbol type, ITypeSymbol otherType)
        {
            var baseType = type;
            while (baseType != null)
            {
                var otherBaseType = otherType;
                while (otherBaseType != null)
                {
                    if (baseType.Equals(otherBaseType)) return baseType;
                    otherBaseType = otherBaseType.BaseType;
                }
                baseType = baseType.BaseType;
            }
            return null;
        }
    }
}