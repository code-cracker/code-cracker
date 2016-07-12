using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using System;

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddPropertyToConstructorFixProvider)), Shared]
    public class AddPropertyToConstructorFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.AddPropertyToConstructor.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => AddPropertyToConstructorCodeFixProviderAll.Instance;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.FirstOrDefault();
            context.RegisterCodeFix(CodeAction.Create("Create property to constructor:", c => AddPropertyToConstructorDocumentAsync(context.Document, diagnostic, c), nameof(AddPropertyToConstructorFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> AddPropertyToConstructorDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var properties = root.FindToken(diagnosticSpan.Start).Parent
                                                                 .AncestorsAndSelf()
                                                                 .OfType<PropertyDeclarationSyntax>();

            var currentClass = properties.FirstOrDefault()?.Parent as ClassDeclarationSyntax;
            var newRoot = AddPropertyToConstructor(root, currentClass, properties.FirstOrDefault());
            return document.WithSyntaxRoot(newRoot);
        }
        public static SyntaxNode AddPropertyToConstructor(SyntaxNode root, ClassDeclarationSyntax currentClass, PropertyDeclarationSyntax property)
        {
            return root.ReplaceNode(currentClass, CreateNewClass(currentClass, property));
        }


        private static ClassDeclarationSyntax CreateNewClass(ClassDeclarationSyntax currentClass, PropertyDeclarationSyntax property)
        {
            switch (DefinitionTypeCreationConstructor(currentClass))
            {
                case TypeConstructor.CreateDefaultConstructor:
                    return CreateNewClassWithDefaultConstructor(currentClass, property);
                case TypeConstructor.NewConstructor:
                    return CreateNewClassWithDefaultConstructorInitializer(currentClass, property);
                case TypeConstructor.UpdateCurrentConstructor:
                    return CreateNewClassUsingCurrentConstructor(currentClass, property);
            }
            return currentClass;
        }

        private static ClassDeclarationSyntax CreateNewClassWithDefaultConstructor(ClassDeclarationSyntax currentClass, PropertyDeclarationSyntax property)
        {
            return currentClass
                    .AddMembers(CreateNewConstructor(currentClass))
                    .AddMembers(CreateNewConstructor(currentClass, property));
        }

        private static ClassDeclarationSyntax CreateNewClassUsingCurrentConstructor(ClassDeclarationSyntax currentClass, PropertyDeclarationSyntax property)
        {
            var oldConstructor = GetCurrentConstructor(GetAllConstructors(currentClass));
            return currentClass.ReplaceNode(oldConstructor, CreateNewConstructor(currentClass, oldConstructor, property));
        }

        private static ClassDeclarationSyntax CreateNewClassWithDefaultConstructorInitializer(ClassDeclarationSyntax currentClass, PropertyDeclarationSyntax property)
        {
            return currentClass.AddMembers(CreateNewConstructorWithInitializer(currentClass, property));
        }

        private static TypeConstructor DefinitionTypeCreationConstructor(ClassDeclarationSyntax currentClass)
        {
            var allConstructor = GetAllConstructors(currentClass);

            if (!allConstructor.Any())
                return TypeConstructor.CreateDefaultConstructor;
            else if (IsNewConstructor(allConstructor))
                return TypeConstructor.NewConstructor;
            else if (IsUpdateConstructor(allConstructor))
                return TypeConstructor.UpdateCurrentConstructor;
            return TypeConstructor.CreateDefaultConstructor;
        }

        private static bool IsNewConstructor(IEnumerable<ConstructorDeclarationSyntax> constructors)
        {
            return constructors.Count() == 1 && GetParametersTotal(constructors) == 0;
        }

        private static bool IsUpdateConstructor(IEnumerable<ConstructorDeclarationSyntax> constructors)
        {
            return GetCurrentConstructor(constructors) != null;
        }

        private static ConstructorDeclarationSyntax GetCurrentConstructor(IEnumerable<ConstructorDeclarationSyntax> constructos)
        {
            return GetConstructorByParametersTotal(GetParametersTotal(constructos), constructos);
        }

        private static int GetParametersTotal(IEnumerable<ConstructorDeclarationSyntax> constructos)
        {
            return (from constructor in constructos
                    select new
                    {
                        total = constructor.ParameterList.Parameters.Count()
                    })
                    .OrderByDescending(o => o.total)
                    .Select(s => s.total)
                    .FirstOrDefault();
        }

        private static ConstructorDeclarationSyntax GetConstructorByParametersTotal(int parametersTotal, IEnumerable<ConstructorDeclarationSyntax> constructos)
        {
            return (from constructor in constructos
                    from param in constructor.DescendantNodes().OfType<ParameterListSyntax>()
                    where param.Parameters.Count() == parametersTotal
                    select constructor).LastOrDefault();
        }

        private static IEnumerable<ConstructorDeclarationSyntax> GetAllConstructors(ClassDeclarationSyntax currentClass)
        {
            return currentClass.Members.OfType<ConstructorDeclarationSyntax>();
        }

        private static ConstructorDeclarationSyntax CreateNewConstructor(ClassDeclarationSyntax currentClass)
        {
            return SyntaxFactory.ConstructorDeclaration(currentClass.Identifier.ValueText)
                                                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                                .WithBody(SyntaxFactory.Block())
                                                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static ConstructorDeclarationSyntax CreateNewConstructor(ClassDeclarationSyntax currentClass, PropertyDeclarationSyntax property)
        {
            return SyntaxFactory.ConstructorDeclaration(currentClass.Identifier.ValueText)
                                                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                                .WithParameterList(CreateNewParameter(property))
                                                .WithBody(CreateBodyConstructor(property))
                                                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static ConstructorDeclarationSyntax CreateNewConstructorWithInitializer(ClassDeclarationSyntax currentClass, PropertyDeclarationSyntax property)
        {
            return SyntaxFactory.ConstructorDeclaration(currentClass.Identifier.ValueText)
                                                .WithInitializer(SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer))
                                                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                                .WithParameterList(CreateNewParameter(property))
                                                .WithBody(CreateBodyConstructor(property))
                                                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static ConstructorDeclarationSyntax CreateNewConstructor(ClassDeclarationSyntax currentClass, ConstructorDeclarationSyntax defaultConstructor, PropertyDeclarationSyntax property)
        {
            return SyntaxFactory.ConstructorDeclaration(currentClass.Identifier.ValueText)
                                                .WithInitializer(defaultConstructor.Initializer)
                                                .WithModifiers(defaultConstructor.Modifiers)
                                                .WithParameterList(CreateNewParameter(property, defaultConstructor.ParameterList))
                                                .WithBody(defaultConstructor.Body.AddStatements(CreateBodyStatement(property).ToArray()));
        }

        private static BlockSyntax CreateBodyConstructor(PropertyDeclarationSyntax property)
        {
            return SyntaxFactory.Block(CreateBodyStatement(property));
        }

        private static List<StatementSyntax> CreateBodyStatement(PropertyDeclarationSyntax properties)
        {
            return new List<StatementSyntax> {
                      (SyntaxFactory.ExpressionStatement(CreateAssignmentExpression(properties)))
            };
        }

        private static AssignmentExpressionSyntax CreateAssignmentExpression(PropertyDeclarationSyntax property)
        {
            var propertyText = property.Identifier.Text;
            var propertyValue = FirstLetteToLower(propertyText);
            if (propertyText.Equals(propertyValue))
                propertyText = $"this.{propertyText}";

            return SyntaxFactory.AssignmentExpression
                                          (
                                             SyntaxKind.SimpleAssignmentExpression,
                                             SyntaxFactory.IdentifierName(propertyText),
                                             SyntaxFactory.IdentifierName(propertyValue)
                                           ).WithAdditionalAnnotations(Formatter.Annotation);

        }
        private static ParameterListSyntax CreateNewParameter(PropertyDeclarationSyntax property)
        {
            return SyntaxFactory.ParameterList(
                                SyntaxFactory.SeparatedList<ParameterSyntax>()
                                .Add(CreateParameter(property)));
        }

        private static ParameterListSyntax CreateNewParameter(PropertyDeclarationSyntax property, ParameterListSyntax parameterList)
        {
            return SyntaxFactory.ParameterList(
                                SyntaxFactory.SeparatedList<ParameterSyntax>()
                                .AddRange(parameterList.Parameters)
                                .Add(CreateParameter(property, setDefaultType: true)));
        }


        private static ParameterSyntax CreateParameter(PropertyDeclarationSyntax property, bool setDefaultType = false)
        {
            if (setDefaultType)
                return CreateParameterDefaultValue(property);

            return SyntaxFactory.Parameter(SyntaxFactory.Identifier($@"{FirstLetteToLower(property.Identifier.Text)}"))
                                               .WithType(property.Type);
        }

        private static ParameterSyntax CreateParameterDefaultValue(PropertyDeclarationSyntax property)
        {
            return SyntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>),
                                           SyntaxFactory.TokenList(),
                                           property.Type,
                                           SyntaxFactory.Identifier($@"{FirstLetteToLower(property.Identifier.Text)}"),
                                           SyntaxFactory.EqualsValueClause
                                           (
                                               SyntaxFactory.ParseExpression(PrepareDefaultTypes(property))
                                           ));
        }

        private static string FirstLetteToLower(string text) =>
            string.Concat(text.Replace(text[0].ToString(), text[0].ToString().ToLower()));

        private enum TypeConstructor
        {
            CreateDefaultConstructor,
            NewConstructor,
            UpdateCurrentConstructor,
        }

        private static SyntaxToken CreatePredefinedType(PropertyDeclarationSyntax property)
        {
            return SyntaxFactory.Identifier($"{FirstLetteToLower(property.Identifier.Text)}{PrepareDefaultTypes(property)}");
        }

        private static string PrepareDefaultTypes(PropertyDeclarationSyntax property)
        {
            var newPropertyValueDefault = string.Empty;
            var propertyTypeText = property.Type.GetText().ToString().Trim();
            var onlyTypeProperty = propertyTypeText.Replace("System.", "").ToLower();
            predefinedTypesDefaultValues.TryGetValue(onlyTypeProperty, out newPropertyValueDefault);

            if (string.IsNullOrWhiteSpace(newPropertyValueDefault))
                return $"default({propertyTypeText})";
            return $"{newPropertyValueDefault.Replace("#", propertyTypeText.Replace("?", ""))}";
        }

        private static ImmutableDictionary<string, string> predefinedTypesDefaultValues { get; } =
             new Dictionary<string, string>
             {
                 ["boolean"] = "false",
                 ["boolean?"] = "false",
                 ["bool"] = "false",
                 ["bool?"] = "false",
                 ["byte"] = "#.MinValue",
                 ["byte?"] = "#.MinValue",
                 ["sbyte"] = "#.MinValue",
                 ["sbyte?"] = "#.MinValue",
                 ["char"] = "#.MinValue",
                 ["char?"] = "#.MinValue",
                 ["decimal"] = "#.MinValue",
                 ["decimal?"] = "#.MinValue",
                 ["double"] = "#.MinValue",
                 ["double?"] = "#.MinValue",
                 ["float"] = "#.MinValue",
                 ["float?"] = "#.MinValue",
                 ["int"] = "#.MinValue",
                 ["int16"] = "#.MinValue",
                 ["int32"] = "#.MinValue",
                 ["int64"] = "#.MinValue",
                 ["int?"] = "#.MinValue",
                 ["int16?"] = "#.MinValue",
                 ["int32?"] = "#.MinValue",
                 ["int64?"] = "#.MinValue",
                 ["uint"] = "#.MinValue",
                 ["uint16"] = "#.MinValue",
                 ["uint32"] = "#.MinValue",
                 ["uint64"] = "#.MinValue",
                 ["uint?"] = "#.MinValue",
                 ["uint16?"] = "#.MinValue",
                 ["uint32?"] = "#.MinValue",
                 ["uint64?"] = "#.MinValue",
                 ["long"] = "#.MinValue",
                 ["long?"] = "#.MinValue",
                 ["ulong"] = "#.MinValue",
                 ["ulong?"] = "#.MinValue",
                 ["short"] = "#.MinValue",
                 ["short?"] = "#.MinValue",
                 ["ushort"] = "#.MinValue",
                 ["ushort?"] = "#.MinValue",
                 ["string"] = @"""""",
             }.ToImmutableDictionary();

        private static bool HasPredefinedType(ImmutableDictionary<string, string> types, string currentType) => types.ContainsKey(currentType);
    }
}
