using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReplaceWithGetterOnlyAutoPropertyAnalyzer : DiagnosticAnalyzer
    {
        internal const string Category = SupportedCategories.Refactoring;
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ReplaceWithGetterOnlyAutoPropertyAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ReplaceWithGetterOnlyAutoPropertyAnalyzer_Description), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ReplaceWithGetterOnlyAutoPropertyAnalyzer_MessageFormat), Resources.ResourceManager, typeof(Resources));

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ReplaceWithGetterOnlyAutoProperty));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(LanguageVersion.CSharp6, AnalyzeSymbol, SymbolKind.Property);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var namedTypeSymbol = (IPropertySymbol)context.Symbol;
            var properties = GetPropsWithOnlyGettersAndReadonlyBackingField(namedTypeSymbol, context);
            if (properties == null) return;
            var diagnostic = Diagnostic.Create(Rule, properties.Locations[0], properties.Name);
            context.ReportDiagnostic(diagnostic);
        }
        private static ISymbol GetPropsWithOnlyGettersAndReadonlyBackingField(IPropertySymbol propertySymbol, SymbolAnalysisContext context)
        {
            if (!propertySymbol.IsReadOnly || propertySymbol.IsStatic || !propertySymbol.CanBeReferencedByName) return null;
            var getMethod = propertySymbol.GetMethod;
            if (getMethod == null) return null;
            var reference = getMethod.DeclaringSyntaxReferences.FirstOrDefault();
            if (reference == null) return null;
            var declaration = reference.GetSyntax(context.CancellationToken) as AccessorDeclarationSyntax;
            if (declaration?.Body == null) return null;
            var returnNode = declaration.Body.ChildNodes().FirstOrDefault();
            if (returnNode?.Kind() != SyntaxKind.ReturnStatement) return null;
            var fieldNode = returnNode.ChildNodes().FirstOrDefault();
            if (fieldNode == null) return null;
            if (fieldNode.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                fieldNode = (fieldNode as MemberAccessExpressionSyntax).Name;
            if (fieldNode.Kind() != SyntaxKind.IdentifierName) return null;
            var model = context.Compilation.GetSemanticModel(fieldNode.SyntaxTree);
            var symbolInfo = model.GetSymbolInfo(fieldNode).Symbol as IFieldSymbol;
            if (symbolInfo != null &&
                symbolInfo.IsReadOnly &&
                (symbolInfo.DeclaredAccessibility == Accessibility.Private || symbolInfo.DeclaredAccessibility == Accessibility.NotApplicable) &&
                symbolInfo.ContainingType == propertySymbol.ContainingType &&
                symbolInfo.Type.Equals(propertySymbol.Type))

                return propertySymbol;
            return null;
        }
    }
}
