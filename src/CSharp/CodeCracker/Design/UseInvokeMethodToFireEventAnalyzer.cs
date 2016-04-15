using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseInvokeMethodToFireEventAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Check for null before calling a delegate";
        internal const string MessageFormat = "Verify if delegate '{0}' is null before invoking it.";
        internal const string Category = SupportedCategories.Design;
        const string Description = "In C#6 a delegate can be invoked using the null-propagating operator (?.) and it's"
            + " invoke method to avoid throwing a NullReference exception when there is no method attached to the delegate. "
            + "Or you can check for null before calling the delegate.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.UseInvokeMethodToFireEvent));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var invocation = (InvocationExpressionSyntax)context.Node;
            var identifier = invocation.Expression as IdentifierNameSyntax;
            if (identifier == null) return;
            var typeInfo = context.SemanticModel.GetTypeInfo(identifier, context.CancellationToken);

            if (typeInfo.ConvertedType?.BaseType == null) return;
            if (typeInfo.ConvertedType.BaseType.Name != typeof(MulticastDelegate).Name) return;

            var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
            if (symbol is ILocalSymbol) return;

            var invokedMethodSymbol = (typeInfo.ConvertedType as INamedTypeSymbol)?.DelegateInvokeMethod;
            if (invokedMethodSymbol == null) return;

            if (HasCheckForNullThatReturns(invocation, context.SemanticModel, symbol)) return;
            if (IsInsideANullCheck(invocation, context.SemanticModel, symbol)) return;
            if (symbol.IsReadOnlyAndInitializedForCertain(context)) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), identifier.Identifier.Text));
        }

        private static bool HasCheckForNullThatReturns(InvocationExpressionSyntax invocation, SemanticModel semanticModel, ISymbol symbol)
        {
            var method = invocation.FirstAncestorOfKind(SyntaxKind.MethodDeclaration) as MethodDeclarationSyntax;
            if (method != null && method.Body != null)
            {
                var ifs = method.Body.Statements.OfKind(SyntaxKind.IfStatement);
                foreach (IfStatementSyntax @if in ifs)
                {
                    if (!@if.Condition?.IsKind(SyntaxKind.EqualsExpression) ?? true) continue;
                    var equals = (BinaryExpressionSyntax)@if.Condition;
                    if (equals.Left == null || equals.Right == null) continue;
                    if (@if.GetLocation().SourceSpan.Start > invocation.GetLocation().SourceSpan.Start) return false;
                    ISymbol identifierSymbol;
                    if (equals.Right.IsKind(SyntaxKind.NullLiteralExpression) && equals.Left.IsKind(SyntaxKind.IdentifierName))
                        identifierSymbol = semanticModel.GetSymbolInfo(equals.Left).Symbol;
                    else if (equals.Left.IsKind(SyntaxKind.NullLiteralExpression) && equals.Right.IsKind(SyntaxKind.IdentifierName))
                        identifierSymbol = semanticModel.GetSymbolInfo(equals.Right).Symbol;
                    else continue;
                    if (!symbol.Equals(identifierSymbol)) continue;
                    if (@if.Statement == null) continue;
                    if (@if.Statement.IsKind(SyntaxKind.Block))
                    {
                        var ifBlock = (BlockSyntax)@if.Statement;
                        if (ifBlock.Statements.OfKind(SyntaxKind.ThrowStatement, SyntaxKind.ReturnStatement).Any()) return true;
                    }
                    else
                    {
                        if (@if.Statement.IsAnyKind(SyntaxKind.ThrowStatement, SyntaxKind.ReturnStatement)) return true;
                    }
                }
            }
            return false;
        }

        private static bool IsInsideANullCheck(InvocationExpressionSyntax invocation, SemanticModel semanticModel, ISymbol symbol)
        {
            var ifs = invocation.Ancestors().OfType<IfStatementSyntax>();
            foreach (IfStatementSyntax @if in ifs)
            {
                if (!@if.Condition?.IsKind(SyntaxKind.NotEqualsExpression) ?? true) continue;
                var equals = (BinaryExpressionSyntax)@if.Condition;
                if (equals.Left == null || equals.Right == null) continue;
                ISymbol identifierSymbol;
                if (equals.Right.IsKind(SyntaxKind.NullLiteralExpression) && equals.Left.IsKind(SyntaxKind.IdentifierName))
                    identifierSymbol = semanticModel.GetSymbolInfo(equals.Left).Symbol;
                else if (equals.Left.IsKind(SyntaxKind.NullLiteralExpression) && equals.Right.IsKind(SyntaxKind.IdentifierName))
                    identifierSymbol = semanticModel.GetSymbolInfo(equals.Right).Symbol;
                else continue;
                if (symbol.Equals(identifierSymbol)) return true;
            }
            return false;
        }
    }
}