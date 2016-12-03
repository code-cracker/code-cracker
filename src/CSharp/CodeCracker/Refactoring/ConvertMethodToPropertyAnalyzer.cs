using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConvertMethodToPropertyAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ConvertMethodToProperty_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ConvertMethodToProperty_MessageFormat), Resources.ResourceManager, typeof(Resources));
        internal const string Category = SupportedCategories.Refactoring;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ConvertMethodToProperty.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ConvertMethodToProperty));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated())
            {
                return;
            }

            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            var returnType = methodDeclaration.ReturnType;
            if (IsVoid(returnType))
            {
                return;
            }

            if (methodDeclaration.ParameterList.Parameters.Count != 0)
            {
                return;
            }

            if (methodDeclaration.Arity != 0)
            {
                return;
            }
            if (methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword, SyntaxKind.VirtualKeyword))
            {
                return;
            }

            if ((methodDeclaration.Body ?? (object )methodDeclaration.ExpressionBody) == null)
            {
                return;
            }

            var semanticModel = context.SemanticModel;
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
            var theClass = methodSymbol.ContainingType;

            var allOverloads = theClass.GetMembers(methodSymbol.Name);
            if (allOverloads.Length != 1)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, methodDeclaration.GetLocation());


            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsVoid(TypeSyntax typeSyntax)
        {
            var predefinedTypeSyntax = typeSyntax as PredefinedTypeSyntax;
            return predefinedTypeSyntax?.Keyword.Kind() == SyntaxKind.VoidKeyword;
        }

    }
}
