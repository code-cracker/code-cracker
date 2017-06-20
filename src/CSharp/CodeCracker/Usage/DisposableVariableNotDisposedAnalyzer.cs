using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisposableVariableNotDisposedAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Should dispose object";
        internal const string MessageFormat = "{0} should be disposed.";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "When a disposable object is created it should be disposed as soon as possible.\n" +
            "This warning will appear if you create a disposable object and don't store, return or dispose it.";
        public const string cantFix = "cantFix";
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.DisposableVariableNotDisposed.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.DisposableVariableNotDisposed));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var objectCreation = context.Node as ObjectCreationExpressionSyntax;
            if (objectCreation == null) return;
            if (objectCreation.Parent == null) return;

            var originalNode = objectCreation;
            SyntaxNode topSyntaxNode = originalNode;
            while (topSyntaxNode.Parent.IsAnyKind(SyntaxKind.ParenthesizedExpression, SyntaxKind.ConditionalExpression, SyntaxKind.CastExpression, SyntaxKind.CoalesceExpression))
                topSyntaxNode = topSyntaxNode.Parent;

            if (topSyntaxNode.Parent.IsAnyKind(SyntaxKind.ReturnStatement, SyntaxKind.UsingStatement, SyntaxKind.YieldReturnStatement))
                return;

            if (topSyntaxNode.Ancestors().Any(i => i.IsAnyKind(
                SyntaxKind.ArrowExpressionClause,
                SyntaxKind.ThisConstructorInitializer,
                SyntaxKind.BaseConstructorInitializer,
                SyntaxKind.ObjectCreationExpression)))
                return;

            var semanticModel = context.SemanticModel;
            var type = semanticModel.GetSymbolInfo(originalNode.Type).Symbol as INamedTypeSymbol;
            if (type == null) return;
            if (!type.AllInterfaces.Any(i => i.ToString() == "System.IDisposable")) return;
            ISymbol identitySymbol = null;
            StatementSyntax statement = null;
            if (topSyntaxNode.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                var assignmentExpression = (AssignmentExpressionSyntax)topSyntaxNode.Parent;
                identitySymbol = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol;
                if (identitySymbol?.Kind != SymbolKind.Local) return;
                if (assignmentExpression.FirstAncestorOrSelf<MethodDeclarationSyntax>() == null) return;
                var usingStatement = assignmentExpression.Parent as UsingStatementSyntax;
                if (usingStatement != null) return;
                statement = assignmentExpression.Parent as ExpressionStatementSyntax;
            }
            else if (topSyntaxNode.Parent.IsKind(SyntaxKind.EqualsValueClause) && topSyntaxNode.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
            {
                var variableDeclarator = (VariableDeclaratorSyntax)topSyntaxNode.Parent.Parent;
                var variableDeclaration = variableDeclarator?.Parent as VariableDeclarationSyntax;
                identitySymbol = semanticModel.GetDeclaredSymbol(variableDeclarator);
                if (identitySymbol == null) return;
                var usingStatement = variableDeclaration?.Parent as UsingStatementSyntax;
                if (usingStatement != null) return;
                statement = variableDeclaration.Parent as LocalDeclarationStatementSyntax;
                if ((statement?.FirstAncestorOrSelf<MethodDeclarationSyntax>()) == null) return;
            }
            else if (topSyntaxNode.Parent.IsAnyKind(SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression))
            {
                var anonymousFunction = topSyntaxNode.Parent as AnonymousFunctionExpressionSyntax;
                var methodSymbol = semanticModel.GetSymbolInfo(anonymousFunction).Symbol as IMethodSymbol;
                if (!methodSymbol.ReturnsVoid) return;
                var props = new Dictionary<string, string> { { "typeName", type.Name }, { cantFix, "" } }.ToImmutableDictionary();
                context.ReportDiagnostic(Diagnostic.Create(Rule, originalNode.GetLocation(), props, type.Name.ToString()));
            }
            else
            {
                var props = new Dictionary<string, string> { { "typeName", type.Name } }.ToImmutableDictionary();
                context.ReportDiagnostic(Diagnostic.Create(Rule, originalNode.GetLocation(), props, type.Name.ToString()));
                return;
            }
            if (statement != null && identitySymbol != null)
            {
                var isDisposeOrAssigned = IsDisposedOrAssigned(semanticModel, statement, (ILocalSymbol)identitySymbol);
                if (isDisposeOrAssigned) return;
                var props = new Dictionary<string, string> { { "typeName", type.Name } }.ToImmutableDictionary();
                context.ReportDiagnostic(Diagnostic.Create(Rule, originalNode.GetLocation(), props, type.Name.ToString()));
            }
        }

        private static bool IsDisposedOrAssigned(SemanticModel semanticModel, StatementSyntax statement, ILocalSymbol identitySymbol)
        {
            var method = statement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method == null) return false;
            if (IsReturned(method, statement, semanticModel, identitySymbol)) return true;
            foreach (var childStatement in method.Body.DescendantNodes().OfType<StatementSyntax>())
            {
                if (childStatement.SpanStart > statement.SpanStart
                && (IsCorrectDispose(childStatement as ExpressionStatementSyntax, semanticModel, identitySymbol)
                || IsPassedAsArgument(childStatement, semanticModel, identitySymbol)
                || IsAssignedToFieldOrProperty(childStatement as ExpressionStatementSyntax, semanticModel, identitySymbol)))
                    return true;
            }
            return false;
        }

        private static bool IsPassedAsArgument(StatementSyntax statement, SemanticModel semanticModel, ILocalSymbol identitySymbol)
        {
            if (statement == null) return false;
            var args = statement.DescendantNodes().OfKind<ArgumentSyntax>(SyntaxKind.Argument);
            foreach (var arg in args)
            {
                var argSymbol = semanticModel.GetSymbolInfo(arg.Expression).Symbol;
                if (identitySymbol.Equals(argSymbol)) return true;
            }
            return false;
        }


        private static bool IsReturned(MethodDeclarationSyntax method, StatementSyntax statement, SemanticModel semanticModel, ILocalSymbol identitySymbol)
        {
            var anonymousFunction = statement.FirstAncestorOfKind(SyntaxKind.ParenthesizedLambdaExpression,
                SyntaxKind.SimpleLambdaExpression, SyntaxKind.AnonymousMethodExpression) as AnonymousFunctionExpressionSyntax;
            IMethodSymbol methodSymbol;
            BlockSyntax body;
            if (anonymousFunction != null)
            {
                methodSymbol = semanticModel.GetSymbolInfo(anonymousFunction).Symbol as IMethodSymbol;
                body = anonymousFunction.Body as BlockSyntax;
            }
            else
            {
                methodSymbol = semanticModel.GetDeclaredSymbol(method);
                body = method.Body;
            }
            if (body == null) return true;
            var returnTypeSymbol = methodSymbol?.ReturnType;
            if (returnTypeSymbol == null) return false;
            if (returnTypeSymbol.SpecialType == SpecialType.System_Void) return false;
            var bodyDescendantNodes = body.DescendantNodes().ToList();
            var returnExpressions = bodyDescendantNodes.OfType<ReturnStatementSyntax>().Select(r => r.Expression).Union(
                bodyDescendantNodes.OfKind<YieldStatementSyntax>(SyntaxKind.YieldReturnStatement).Select(yr => yr.Expression));
            var isReturning = returnExpressions.Any(returnExpression =>
            {
                var returnSymbol = semanticModel.GetSymbolInfo(returnExpression).Symbol;
                if (returnSymbol == null) return false;
                return returnSymbol.Equals(identitySymbol);
            });
            return isReturning;
        }

        private static bool IsAssignedToFieldOrProperty(ExpressionStatementSyntax expressionStatement, SemanticModel semanticModel, ILocalSymbol identitySymbol)
        {
            if (expressionStatement == null) return false;
            if (!expressionStatement.Expression.IsKind(SyntaxKind.SimpleAssignmentExpression)) return false;
            var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;
            var assignmentTarget = semanticModel.GetSymbolInfo(assignment.Left).Symbol;
            if (assignmentTarget?.Kind != SymbolKind.Field && assignmentTarget?.Kind != SymbolKind.Property) return false;
            var assignmentSource = semanticModel.GetSymbolInfo(assignment.Right).Symbol;
            return (identitySymbol.Equals(assignmentSource));
        }

        private static bool IsCorrectDispose(ExpressionStatementSyntax expressionStatement, SemanticModel semanticModel, ILocalSymbol identitySymbol)
        {
            if (expressionStatement == null) return false;
            var invocation = expressionStatement.Expression as InvocationExpressionSyntax;
            ExpressionSyntax expressionAccessed;
            IdentifierNameSyntax memberAccessed;
            if (invocation == null)
            {
                var conditionalAccessExpression = expressionStatement.Expression as ConditionalAccessExpressionSyntax;
                if (conditionalAccessExpression == null) return false;
                invocation = conditionalAccessExpression.WhenNotNull as InvocationExpressionSyntax;
                var memberBinding = invocation?.Expression as MemberBindingExpressionSyntax;
                if (memberBinding == null) return false;
                expressionAccessed = conditionalAccessExpression.Expression;
                memberAccessed = memberBinding.Name as IdentifierNameSyntax;
            }
            else
            {
                var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
                if (memberAccess == null) return false;
                expressionAccessed = memberAccess.Expression;
                memberAccessed = memberAccess.Name as IdentifierNameSyntax;
            }
            if (memberAccessed == null) return false;
            if (invocation.ArgumentList.Arguments.Any()) return false;
            ISymbol memberSymbol;
            if (expressionAccessed.IsKind(SyntaxKind.IdentifierName))
            {
                memberSymbol = semanticModel.GetSymbolInfo(expressionAccessed).Symbol;
            }
            else if (expressionAccessed is ParenthesizedExpressionSyntax)
            {
                var parenthesizedExpression = (ParenthesizedExpressionSyntax)expressionAccessed;
                var cast = parenthesizedExpression.Expression as CastExpressionSyntax;
                if (cast == null) return false;
                var catTypeSymbol = semanticModel.GetTypeInfo(cast.Type).Type;
                if (catTypeSymbol.SpecialType != SpecialType.System_IDisposable) return false;
                memberSymbol = semanticModel.GetSymbolInfo(cast.Expression).Symbol;
            }
            else return false;
            if (memberSymbol == null || !memberSymbol.Equals(identitySymbol)) return false;
            if (memberAccessed.Identifier.Text != "Dispose" || memberAccessed.Arity != 0) return false;
            var methodSymbol = semanticModel.GetSymbolInfo(memberAccessed).Symbol as IMethodSymbol;
            if (methodSymbol == null) return false;
            if (methodSymbol.ToString() == "System.IDisposable.Dispose()") return true;
            var disposeMethod = (IMethodSymbol)semanticModel.Compilation.GetSpecialType(SpecialType.System_IDisposable).GetMembers("Dispose").Single();
            var isDispose = methodSymbol.Equals(methodSymbol.ContainingType.FindImplementationForInterfaceMember(disposeMethod));
            return isDispose;
        }
    }
}