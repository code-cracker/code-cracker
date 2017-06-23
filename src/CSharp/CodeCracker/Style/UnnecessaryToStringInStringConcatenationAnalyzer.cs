using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryToStringInStringConcatenationAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Unnecessary '.ToString()' call in string concatenation.";
        internal const string MessageFormat = Title;
        internal const string Category = SupportedCategories.Style;
        const string Description = "The runtime automatically calls '.ToString()' method for" +
            " string concatenation operations when there is no parameters. Remove them.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.UnnecessaryToStringInStringConcatenation.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            customTags: WellKnownDiagnosticTags.Unnecessary,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.UnnecessaryToStringInStringConcatenation));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.AddExpression);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var addExpression = (BinaryExpressionSyntax)context.Node;

            var hasInvocationExpression = addExpression.ChildNodesAndTokens().Any(x => x.IsKind(SyntaxKind.InvocationExpression));

            //string concatenation must have an InvocationExpression
            if (!hasInvocationExpression) return;
            var invocationExpressionsThatHaveToStringCall = GetInvocationExpressionsThatHaveToStringCall(addExpression);

            var redundantToStringCalls = FilterInvocationsThatAreRedundant(invocationExpressionsThatHaveToStringCall, addExpression, context.SemanticModel, context.CancellationToken);

            foreach (var expression in redundantToStringCalls)
            {
                var lastDot = expression.Expression.ChildNodesAndTokens().Last(x => x.IsKind(SyntaxKind.DotToken));
                var toStringTextSpan = new TextSpan(lastDot.Span.Start, expression.ArgumentList.Span.End - lastDot.Span.Start);
                var diagnostic = Diagnostic.Create(Rule, Location.Create(context.Node.SyntaxTree, toStringTextSpan));
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static IEnumerable<InvocationExpressionSyntax> GetInvocationExpressionsThatHaveToStringCall(BinaryExpressionSyntax addExpression)
        {
            return addExpression.ChildNodes().OfType<InvocationExpressionSyntax>()
                //Only default call to ToString method must be accepted
                .Where(x => x.Expression.ToString().EndsWith(@".ToString") && !x.ArgumentList.Arguments.Any());
        }

        private static IEnumerable<InvocationExpressionSyntax> FilterInvocationsThatAreRedundant(IEnumerable<InvocationExpressionSyntax> invocationExpressionsThatHaveToStringCall, BinaryExpressionSyntax addExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var node in invocationExpressionsThatHaveToStringCall)
            {
                var toStringReceiver = GetTypeInfoOfReceiverOfToStringCall(node, semanticModel, cancellationToken);
                //As long as the underlying type can not be resolved by the compiler (e.g. undefined type) 
                //removal is not save.
                if (toStringReceiver == null || toStringReceiver.TypeKind==TypeKind.Error)
                    continue;
                //If the underlying type is string, removal is save.
                if (IsTypeSymbolSystem_String(toStringReceiver))
                    yield return node;
                var otherType = GetTypeInfoOfOtherNode(node, addExpression, semanticModel, cancellationToken);
                if (otherType != null)
                {
                    if (CheckAddOperationOverloadsOfTypes(toStringReceiver, otherType))
                    {
                        yield return node;
                    }
                }
            }
        }

        private static ITypeSymbol GetTypeInfoOfReceiverOfToStringCall(InvocationExpressionSyntax toStringCall, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (toStringCall.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                return semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken).Type;
            }

            return null;
        }

        private static ITypeSymbol GetTypeInfoOfOtherNode(SyntaxNode toStringNode, BinaryExpressionSyntax addExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var otherNode = addExpression.Left == toStringNode
                ? addExpression.Right
                : addExpression.Right == toStringNode
                    ? addExpression.Left
                    : null;
            if (otherNode != null)
            {
                return semanticModel.GetTypeInfo(otherNode, cancellationToken).Type;
            }

            return null;
        }

        private static bool CheckAddOperationOverloadsOfTypes(ITypeSymbol toStringReceiver, ITypeSymbol otherType)
        {
            //If the underlying type has a custom AddOperator this operator will take precedence over everything else 
            if (HasTypeCustomAddOperator(toStringReceiver))
            {
                return false;
            }

            //If the other side is a string the string concatenation will be applied and "ToString" will be implicit called by the concatenation operator
            if (IsTypeSymbolSystem_String(otherType))
            {
                return true;
            }

            //If the underlying type is one of the build in types (numeric, datetime and so on) and the other side is not a string,
            //the result a removal is hard to predict and might be wrong. 
            if (HasAdditionOperator(toStringReceiver))
            {
                return false;
            }

            //If both sides are delegates, the plus operator combines the delegates:
            //https://msdn.microsoft.com/en-us/library/ms173175(v=vs.110).aspx
            if (IsTypeSmybolDelegateType(toStringReceiver) && IsTypeSmybolDelegateType(otherType))
            {
                return false;
            }

            //There might be more cases were removal is save but for now we opt out.
            return false;
        }

        private static bool IsTypeSmybolDelegateType(ITypeSymbol typeSymbol)
            => typeSymbol.TypeKind == TypeKind.Delegate;

        private static bool IsTypeSymbolSystem_String(ITypeSymbol typeSymbol)
            => typeSymbol.SpecialType == SpecialType.System_String;


        // see https://stackoverflow.com/a/41223159
        private static bool HasAdditionOperator(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Enum:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                //String has an addition operator but we are looking for types other than string with an addition overload.
                //case SpecialType.System_String:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_DateTime:
                    return true;
            }
            if (type.TypeKind == TypeKind.Enum)
            {
                return true;
            }
            return false;
        }

        private static bool HasTypeCustomAddOperator(ITypeSymbol type)
        {
            var customAdditionOperators = type.GetMembers("op_Addition").OfType<IMethodSymbol>();
            return customAdditionOperators.Any(ms => ms.MethodKind == MethodKind.UserDefinedOperator);
        }
    }
}