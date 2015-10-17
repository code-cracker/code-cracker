using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Collections.Generic;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReadonlyFieldAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Make field readonly";
        internal const string Message = "Make '{0}' readonly";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "A field that is only assigned on the constructor can be made readonly.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ReadonlyField.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ReadonlyField));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterCompilationStartAction(AnalyzeCompilation);

        private static void AnalyzeCompilation(CompilationStartAnalysisContext compilationStartAnalysisContext)
        {
            var compilation = compilationStartAnalysisContext.Compilation;
            compilationStartAnalysisContext.RegisterSyntaxTreeAction(context => AnalyzeTree(context, compilation));
        }

        private static void AnalyzeTree(SyntaxTreeAnalysisContext context, Compilation compilation)
        {
            if (context.IsGenerated()) return;
            if (!compilation.SyntaxTrees.Contains(context.Tree)) return;
            var semanticModel = compilation.GetSemanticModel(context.Tree);
            SyntaxNode root;
            if (!context.Tree.TryGetRoot(out root)) return;
            var types = GetTypesInRoot(root);
            foreach (var type in types)
            {
                var fieldDeclarations = type.ChildNodes().OfType<FieldDeclarationSyntax>();
                var variablesToMakeReadonly = GetCandidateVariables(semanticModel, fieldDeclarations);
                var typeSymbol = semanticModel.GetDeclaredSymbol(type);
                if (typeSymbol == null) continue;
                var methods = typeSymbol.GetAllMethodsIncludingFromInnerTypes();
                foreach (var method in methods)
                {
                    foreach (var syntaxReference in method.DeclaringSyntaxReferences)
                    {
                        var syntaxRefSemanticModel = syntaxReference.SyntaxTree.Equals(context.Tree)
                                ? semanticModel
                                : compilation.GetSemanticModel(syntaxReference.SyntaxTree);
                        var assignments = syntaxReference.GetSyntax().DescendantNodes().OfType<AssignmentExpressionSyntax>();
                        foreach (var assignment in assignments)
                        {
                            var fieldSymbol = syntaxRefSemanticModel.GetSymbolInfo(assignment.Left).Symbol as IFieldSymbol;
                            if (fieldSymbol == null) continue;
                            if (method.MethodKind == MethodKind.StaticConstructor && fieldSymbol.IsStatic)
                                AddVariableThatWasSkippedBeforeBecauseItLackedAInitializer(variablesToMakeReadonly, fieldSymbol, assignment);
                            else if (method.MethodKind == MethodKind.Constructor && !fieldSymbol.IsStatic)
                                AddVariableThatWasSkippedBeforeBecauseItLackedAInitializer(variablesToMakeReadonly, fieldSymbol, assignment);
                            else
                                RemoveVariableThatHasAssignment(variablesToMakeReadonly, fieldSymbol);
                        }
                    }
                }
                foreach (var readonlyVariable in variablesToMakeReadonly.Values)
                {
                    var props = new Dictionary<string, string> { { "identifier", readonlyVariable.Identifier.Text } }.ToImmutableDictionary();
                    var diagnostic = Diagnostic.Create(Rule, readonlyVariable.GetLocation(), props, readonlyVariable.Identifier.Text);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static void AddVariableThatWasSkippedBeforeBecauseItLackedAInitializer(Dictionary<IFieldSymbol, VariableDeclaratorSyntax> variablesToMakeReadonly, IFieldSymbol fieldSymbol, AssignmentExpressionSyntax assignment)
        {
            var parent = assignment.Parent;
            while (parent != null)
            {
                if (parent is AnonymousFunctionExpressionSyntax)
                    return;
                if (parent is ConstructorDeclarationSyntax)
                    break;
                parent = parent.Parent;
            }

            if (!fieldSymbol.IsReadOnly && !variablesToMakeReadonly.Keys.Contains(fieldSymbol))
                foreach (var variable in fieldSymbol.DeclaringSyntaxReferences)
                    variablesToMakeReadonly.Add(fieldSymbol, (VariableDeclaratorSyntax)variable.GetSyntax());
        }

        private static void RemoveVariableThatHasAssignment(Dictionary<IFieldSymbol, VariableDeclaratorSyntax> variablesToMakeReadonly, IFieldSymbol fieldSymbol)
        {
            if (variablesToMakeReadonly.Keys.Contains(fieldSymbol))
                variablesToMakeReadonly.Remove(fieldSymbol);
        }

        private static Dictionary<IFieldSymbol, VariableDeclaratorSyntax> GetCandidateVariables(SemanticModel semanticModel, IEnumerable<FieldDeclarationSyntax> fieldDeclarations)
        {
            var variablesToMakeReadonly = new Dictionary<IFieldSymbol, VariableDeclaratorSyntax>();
            foreach (var fieldDeclaration in fieldDeclarations)
                variablesToMakeReadonly.AddRange(GetCandidateVariables(semanticModel, fieldDeclaration));
            return variablesToMakeReadonly;
        }

        private static Dictionary<IFieldSymbol, VariableDeclaratorSyntax> GetCandidateVariables(SemanticModel semanticModel, FieldDeclarationSyntax fieldDeclaration)
        {
            var variablesToMakeReadonly = new Dictionary<IFieldSymbol, VariableDeclaratorSyntax>();
            if (fieldDeclaration == null) return variablesToMakeReadonly;
            if (!CanBeMadeReadonly(fieldDeclaration)) return variablesToMakeReadonly;
            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                if (variable.Initializer == null) continue;
                var variableSymbol = semanticModel.GetDeclaredSymbol(variable);
                if (variableSymbol == null) continue;
                variablesToMakeReadonly.Add((IFieldSymbol)variableSymbol, variable);
            }
            return variablesToMakeReadonly;
        }

        private static bool CanBeMadeReadonly(FieldDeclarationSyntax fieldDeclaration)
        {
            return !fieldDeclaration.Modifiers.Any()
                || !fieldDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)
                    || m.IsKind(SyntaxKind.ProtectedKeyword)
                    || m.IsKind(SyntaxKind.InternalKeyword)
                    || m.IsKind(SyntaxKind.ReadOnlyKeyword)
                    || m.IsKind(SyntaxKind.ConstKeyword));
        }

        private static List<TypeDeclarationSyntax> GetTypesInRoot(SyntaxNode root)
        {
            var types = new List<TypeDeclarationSyntax>();
            if (root.IsKind(SyntaxKind.ClassDeclaration) || root.IsKind(SyntaxKind.StructDeclaration))
                types.Add((TypeDeclarationSyntax)root);
            else
                types.AddRange(root.DescendantTypes());
            return types;
        }
    }
}
