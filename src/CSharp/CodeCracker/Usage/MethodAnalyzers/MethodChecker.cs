using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.Usage.MethodAnalyzers
{
    public class MethodChecker
    {
        private readonly SyntaxNodeAnalysisContext context;
        private readonly DiagnosticDescriptor diagnosticDescriptor;

        public MethodChecker(SyntaxNodeAnalysisContext context, DiagnosticDescriptor diagnosticDescriptor)
        {
            this.context = context;
            this.diagnosticDescriptor = diagnosticDescriptor;
        }

        public void AnalyzeConstrutor(MethodInformation methodInformation)
        {
            if (ConstructorNameNotFound(methodInformation) || MethodFullNameNotFound(methodInformation.MethodFullDefinition))
            {
                return;
            }
            var argumentList = ((ObjectCreationExpressionSyntax)context.Node).ArgumentList;
            var arguments = GetArguments(argumentList);
            Execute(methodInformation, arguments, argumentList);
        }

        private bool ConstructorNameNotFound(MethodInformation methodInformation)
        {
            return AbreviatedConstructorNameNotFound(methodInformation) && QualifiedConstructorNameNotFound(methodInformation);
        }

        private bool AbreviatedConstructorNameNotFound(MethodInformation methodInformation)
        {
            var objectCreationExpressionSyntax = (ObjectCreationExpressionSyntax) context.Node;
            var identifier = objectCreationExpressionSyntax.Type as IdentifierNameSyntax;
            return identifier?.Identifier.ValueText != methodInformation.MethodName;
        }

        private bool QualifiedConstructorNameNotFound(MethodInformation methodInformation)
        {
            var objectCreationExpressionSyntax = (ObjectCreationExpressionSyntax)context.Node;
            var identifier = objectCreationExpressionSyntax.Type as QualifiedNameSyntax;
            return identifier?.Right.ToString() != methodInformation.MethodName;
        }

        public void AnalyzeMethod(MethodInformation methodInformation)
        {
            if (MethodNameNotFound(methodInformation) ||
                MethodFullNameNotFound(methodInformation.MethodFullDefinition))
            {
                return;
            }
            var argumentList = ((InvocationExpressionSyntax)context.Node).ArgumentList;
            var arguments = GetArguments(argumentList);

            Execute(methodInformation, arguments, argumentList);
        }

        private bool MethodNameNotFound(MethodInformation methodInformation)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;
            var memberExpression = invocationExpression.Expression as MemberAccessExpressionSyntax;
            return memberExpression?.Name?.Identifier.ValueText != methodInformation.MethodName;
        }

        private bool MethodFullNameNotFound(string methodDefinition)
        {
            var memberSymbol = context.SemanticModel.GetSymbolInfo(context.Node).Symbol;
            return memberSymbol?.ToString() != methodDefinition;
        }

        private void Execute(MethodInformation methodInformation, List<object> arguments, ArgumentListSyntax argumentList)
        {
            if (!argumentList.Arguments.Any())
            {
                return;
            }
            try
            {
                methodInformation.MethodAction.Invoke(arguments);
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }
                var diag = Diagnostic.Create(diagnosticDescriptor, argumentList.Arguments[methodInformation.ArgumentIndex].GetLocation(), ex.Message);
                context.ReportDiagnostic(diag);
            }
        }

        private List<object> GetArguments(ArgumentListSyntax argumentList)
        {
            return argumentList.Arguments
                .Select(a => a.Expression)
                .Select(l => l == null ? null : context.SemanticModel.GetConstantValue(l).Value)
                .ToList();
        }
    }
}