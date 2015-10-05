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
    public class NoPrivateReadonlyFieldAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Make field readonly";
        internal const string Message = "Make '{0}' readonly";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "A field that is only assigned on the constructor can be made readonly.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.NoPrivateReadonlyField.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.NoPrivateReadonlyField));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationStartAction(compilationStartContext =>
            {
                var candidateFields = new List<FieldCandidate>();
                var assignedFields = new List<ISymbol>();

                compilationStartContext.RegisterSyntaxNodeAction(
                    syntaxNodeAnalysisContext => CaptureCandidateFields(syntaxNodeAnalysisContext.Node as FieldDeclarationSyntax, syntaxNodeAnalysisContext.SemanticModel, candidateFields),
                    SyntaxKind.FieldDeclaration);

                compilationStartContext.RegisterSyntaxNodeAction(
                    syntaxNodeAnalysisContext => CaptureAssignedFields(syntaxNodeAnalysisContext.Node as TypeDeclarationSyntax, syntaxNodeAnalysisContext.SemanticModel, assignedFields),
                    SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);

                compilationStartContext.RegisterCompilationEndAction(compilationEndContext =>
                {
                    var fieldsWithoutAssignment = candidateFields.Distinct().Where(field => HasNoAssignment(field, assignedFields));
                    foreach (var candidateField in fieldsWithoutAssignment)
                    {
                        var props = new Dictionary<string, string> { { "identifier", candidateField.Variable.Identifier.Text } }.ToImmutableDictionary();
                        compilationEndContext.ReportDiagnostic(Diagnostic.Create(
                            Rule,
                            candidateField.Variable.Identifier.GetLocation(),
                            props,
                            candidateField.Variable.Identifier.Text));
                    }
                });
            });
        }

        private static void CaptureAssignedFields(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, List<ISymbol> assignedFields)
        {
            var t = new TypeDeclarationWithSymbol { TypeDeclaration = typeDeclaration, NamedTypeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration) };
            var fields = GetAssignedFieldsFromType(t, semanticModel);
            assignedFields.AddRange(fields);
        }
        private static void CaptureCandidateFields(FieldDeclarationSyntax field, SemanticModel semanticModel, List<FieldCandidate> candidateFields)
        {

            if (!CanBecameReadOnlyField(field)) return;
            var variables = field.Declaration.Variables;
            var currentAnalysisCandidateFields = variables.Select(s => new FieldCandidate { Variable = s, FieldSymbol = semanticModel.GetDeclaredSymbol(s) as IFieldSymbol })
                .Where(p => p.FieldSymbol != null && p.FieldSymbol.ContainingType != null);

            if (!currentAnalysisCandidateFields.Any()) return;
            candidateFields.AddRange(currentAnalysisCandidateFields);
        }

        private static bool CanBecameReadOnlyField(FieldDeclarationSyntax field)
        {
            var noPrivate = field.Modifiers.Any(p => p.IsKind(SyntaxKind.PublicKeyword) || p.IsKind(SyntaxKind.ProtectedKeyword) || p.IsKind(SyntaxKind.InternalKeyword));
            return noPrivate ? !field.Modifiers.Any(p => p.IsKind(SyntaxKind.ConstKeyword) || p.IsKind(SyntaxKind.ReadOnlyKeyword)) : false;
        }

        #region GetAssignedField

        private static IEnumerable<ISymbol> GetAssignedFieldsFromType(TypeDeclarationWithSymbol typeDeclarationWithSymbol, SemanticModel model)
        {
            var typeDeclaration = typeDeclarationWithSymbol.TypeDeclaration;
            var descendants = typeDeclaration.DescendantNodes(p => SkipNestedTypes(typeDeclaration, p));
            return descendants
                .OfType<AssignmentExpressionSyntax>()
                .Select(s => s.Left)
                .Union(
                    descendants
                        .OfType<PostfixUnaryExpressionSyntax>()
                        .Select(s => s.Operand))
                .Union(
                    descendants
                        .OfType<PrefixUnaryExpressionSyntax>()
                        .Select(s => s.Operand)
                )
                .Union(
                    descendants
                        .OfType<InvocationExpressionSyntax>()
                        .SelectMany(s => s.ArgumentList.Arguments.Where(p => !p.RefOrOutKeyword.IsKind(SyntaxKind.None)))
                        .Select(s => s.Expression)
                )
                .Select(s => new { Symbol = model.GetSymbolInfo(s).Symbol, Expression = s })
                .Where(p => p.Symbol != null)
                .Where(p => SkipFieldsFromItsOwnConstructor(typeDeclarationWithSymbol, p.Expression, p.Symbol))
                .Select(s => s.Symbol);
        }

        private static bool SkipNestedTypes(TypeDeclarationSyntax typeDeclaration, SyntaxNode node) =>
            node is TypeDeclarationSyntax ? node == typeDeclaration : true;

        private static bool SkipFieldsFromItsOwnConstructor(TypeDeclarationWithSymbol type, ExpressionSyntax assignmentExpression, ISymbol assignmentSymbol)
        {
            var parentConstructor = assignmentExpression.Ancestors().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();

            if (parentConstructor == null)
                return true;

            return
                assignmentSymbol.ContainingType != type.NamedTypeSymbol ||
                assignmentSymbol.IsStatic != parentConstructor.Modifiers.Any(p => p.IsKind(SyntaxKind.StaticKeyword));
        }

        #endregion

        private static bool HasNoAssignment(FieldCandidate field, List<ISymbol> assignedFields) =>
            !assignedFields.Any(assignedField => assignedField == field.FieldSymbol);

        private sealed class FieldCandidate
        {
            internal VariableDeclaratorSyntax Variable { get; set; }
            internal IFieldSymbol FieldSymbol { get; set; }

            public override bool Equals(object obj)
            {
                if (object.Equals(obj, null)) return false;
                if (GetType() != obj.GetType()) return false;
                return ((FieldCandidate)obj).FieldSymbol.Equals(FieldSymbol);
            }
            public override int GetHashCode()
            {
                var hash = 13;
                hash = (hash * 7) + FieldSymbol.GetHashCode();
                return hash;
            }

        }

        private class TypeDeclarationWithSymbol
        {
            internal TypeDeclarationSyntax TypeDeclaration { get; set; }
            internal INamedTypeSymbol NamedTypeSymbol { get; set; }
        }
    }
}