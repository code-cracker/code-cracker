using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.Usage.PreRunMethodAnalyzers
{
    public class PreRunMethodChecker
    {
        private readonly SyntaxNodeAnalysisContext context;
        private readonly DiagnosticDescriptor diagnosticDescriptor;

        public PreRunMethodChecker(SyntaxNodeAnalysisContext context, DiagnosticDescriptor diagnosticDescriptor)
        {
            this.context = context;
            this.diagnosticDescriptor = diagnosticDescriptor;
        }

        public void AnalyzeConstrutor(PreRunMethodInfo preRunMethodInfo)
        {
            if (MethodFullNameIsNotFound(preRunMethodInfo.MethodFullDefinition))
            {
                return;
            }
            var argumentList = ((ObjectCreationExpressionSyntax)context.Node).ArgumentList;
            var arguments = GetArguments(argumentList);
            Execute(preRunMethodInfo, arguments, argumentList);
        }

        public void AnalyzeMethod(PreRunMethodInfo preRunMethodInfo)
        {
            if (MethodNameIsNotFound(preRunMethodInfo) ||
                MethodFullNameIsNotFound(preRunMethodInfo.MethodFullDefinition))
            {
                return;
            }
            var argumentList = ((InvocationExpressionSyntax)context.Node).ArgumentList;
            var arguments = GetArguments(argumentList);

            Execute(preRunMethodInfo, arguments, argumentList);
        }

        private void Execute(PreRunMethodInfo preRunMethodInfo, List<object> arguments, ArgumentListSyntax argumentList)
        {
            if (!argumentList.Arguments.Any())
            {
                return;
            }
            try
            {
                preRunMethodInfo.MethodToExecuteForChecking.Invoke(arguments);
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                }
                var diag = Diagnostic.Create(diagnosticDescriptor, argumentList.Arguments[preRunMethodInfo.ArgumentIndex].GetLocation(), ex.Message);
                context.ReportDiagnostic(diag);
            }
        }

        private bool MethodNameIsNotFound(PreRunMethodInfo preRunMethodInfo)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;
            var memberExpression = invocationExpression.Expression as MemberAccessExpressionSyntax;
            return memberExpression?.Name?.Identifier.ValueText != preRunMethodInfo.MethodName;
        }

        private bool MethodFullNameIsNotFound(string methodDefinition)
        {
            var memberSymbol = context.SemanticModel.GetSymbolInfo(context.Node).Symbol;
            return memberSymbol?.ToString() != methodDefinition;
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