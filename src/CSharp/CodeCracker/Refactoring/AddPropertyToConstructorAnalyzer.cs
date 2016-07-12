using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AddPropertyToConstructorAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Create property to constructor";
        internal const string MessageFormat = "Add property {0} to constructor.";
        internal const string Category = SupportedCategories.Refactoring;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.AddPropertyToConstructor.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.AddPropertyToConstructor));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var currentClass = context.Node as ClassDeclarationSyntax;
            var properties = GetAllPublicProperties(currentClass);
            if (!properties.Any()) return;
            var constructors = GetAllConstructors(currentClass);
            GenerateDiagnostic(context, properties, constructors);
        }

        private static IEnumerable<PropertyDeclarationSyntax> GetAllPublicProperties(ClassDeclarationSyntax classDeclaration)
        {
            return from properties in classDeclaration.Members.OfType<PropertyDeclarationSyntax>()
                   where !properties.Modifiers.Any(a =>
                         (a.IsKind(SyntaxKind.PrivateKeyword) ||
                         a.IsKind(SyntaxKind.ConstKeyword) ||
                         a.IsKind(SyntaxKind.ReadOnlyKeyword) ||
                         a.IsKind(SyntaxKind.StaticKeyword))) &&
                         (properties.Modifiers.Any(a => a.IsKind(SyntaxKind.PublicKeyword) && properties.Initializer == null))
                   select properties;

        }

        private static IEnumerable<ConstructorDeclarationSyntax> GetAllConstructors(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Members.OfType<ConstructorDeclarationSyntax>()
                                            .Where(w => w.IsAnyKind(SyntaxKind.ConstructorDeclaration));
        }

        private static void GenerateDiagnostic(SyntaxNodeAnalysisContext context, IEnumerable<PropertyDeclarationSyntax> properties, IEnumerable<ConstructorDeclarationSyntax> constructors)
        {
            foreach (var property in properties)
            {
                if (!constructors.Any())
                {
                    AddDiagnostic(context, property);
                    continue;
                }

                if (!HasPropertyInConstructors(constructors, property))
                    AddDiagnostic(context, property);
            }
        }

        private static bool HasPropertyInConstructors(IEnumerable<ConstructorDeclarationSyntax> constructors, PropertyDeclarationSyntax currentProperty)
        {
            var allConstructor = constructors.Where(a => a.Body != null);
            if (allConstructor == null) return false;

            var hasIdentifierAssigned = (from constructor in allConstructor
                                         from identifierSyntax in constructor.Body?.DescendantNodes()?.OfType<ExpressionSyntax>()?.OfType<IdentifierNameSyntax>()
                                         where identifierSyntax?.Identifier.Value == currentProperty?.Identifier.Value
                                         select identifierSyntax).Any();
            return hasIdentifierAssigned;
        }

        private static void AddDiagnostic(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax currentProperty)
        {
            var diagnostic = Diagnostic.Create(Rule, currentProperty.GetLocation(), currentProperty.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }

    }
}
