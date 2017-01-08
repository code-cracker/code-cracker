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

        private struct MethodKindComparer : IComparer<MethodKind>
        {
            public int Compare(MethodKind x, MethodKind y) =>
                x - y == 0
                ? 0
                : (x == MethodKind.Constructor
                    ? 1
                    : (y == MethodKind.Constructor
                        ? -1
                        : x - y));
        }
        private static readonly MethodKindComparer methodKindComparer = new MethodKindComparer();

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
                methods = methods.OrderByDescending(m => m.MethodKind, methodKindComparer).ToList();
                foreach (var method in methods)
                {
                    foreach (var syntaxReference in method.DeclaringSyntaxReferences)
                    {
                        var syntaxRefSemanticModel = syntaxReference.SyntaxTree.Equals(context.Tree)
                                ? semanticModel
                                : compilation.GetSemanticModel(syntaxReference.SyntaxTree);
                        var descendants = syntaxReference.GetSyntax().DescendantNodes().ToList();
                        var argsWithRefOrOut = descendants.OfType<ArgumentSyntax>().Where(a => a.RefOrOutKeyword != null);
                        foreach (var argWithRefOrOut in argsWithRefOrOut)
                        {
                            var fieldSymbol = syntaxRefSemanticModel.GetSymbolInfo(argWithRefOrOut.Expression).Symbol as IFieldSymbol;
                            if (fieldSymbol == null) continue;
                            variablesToMakeReadonly.Remove(fieldSymbol);
                        }
                        var assignments = descendants.OfKind(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxKind.AddAssignmentExpression, SyntaxKind.AndAssignmentExpression, SyntaxKind.DivideAssignmentExpression,
                            SyntaxKind.ExclusiveOrAssignmentExpression, SyntaxKind.LeftShiftAssignmentExpression, SyntaxKind.ModuloAssignmentExpression,
                            SyntaxKind.MultiplyAssignmentExpression, SyntaxKind.OrAssignmentExpression, SyntaxKind.RightShiftAssignmentExpression,
                            SyntaxKind.SubtractAssignmentExpression);
                        foreach (AssignmentExpressionSyntax assignment in assignments)
                        {
                            var fieldSymbol = syntaxRefSemanticModel.GetSymbolInfo(assignment.Left).Symbol as IFieldSymbol;
                            VerifyVariable(variablesToMakeReadonly, method, syntaxRefSemanticModel, assignment, fieldSymbol);
                        }
                        var postFixUnaries = descendants.OfKind(SyntaxKind.PostIncrementExpression, SyntaxKind.PostDecrementExpression);
                        foreach (PostfixUnaryExpressionSyntax postFixUnary in postFixUnaries)
                        {
                            var fieldSymbol = syntaxRefSemanticModel.GetSymbolInfo(postFixUnary.Operand).Symbol as IFieldSymbol;
                            VerifyVariable(variablesToMakeReadonly, method, syntaxRefSemanticModel, postFixUnary, fieldSymbol);
                        }
                        var preFixUnaries = descendants.OfKind(SyntaxKind.PreDecrementExpression, SyntaxKind.PreIncrementExpression);
                        foreach (PrefixUnaryExpressionSyntax preFixUnary in preFixUnaries)
                        {
                            var fieldSymbol = syntaxRefSemanticModel.GetSymbolInfo(preFixUnary.Operand).Symbol as IFieldSymbol;
                            VerifyVariable(variablesToMakeReadonly, method, syntaxRefSemanticModel, preFixUnary, fieldSymbol);
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

        private static void VerifyVariable(Dictionary<IFieldSymbol, VariableDeclaratorSyntax> variablesToMakeReadonly, IMethodSymbol method,
            SemanticModel syntaxRefSemanticModel, SyntaxNode node, IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol == null) return;
            if (!CanBeMadeReadonly(fieldSymbol)) return;
            if (!HasAssignmentInLambda(node)
            && ((method.MethodKind == MethodKind.StaticConstructor && fieldSymbol.IsStatic)
            || (method.MethodKind == MethodKind.Constructor && !fieldSymbol.IsStatic)))
                AddVariableThatWasSkippedBeforeBecauseItLackedAInitializer(variablesToMakeReadonly, fieldSymbol, node, syntaxRefSemanticModel);
            else
                RemoveVariableThatHasAssignment(variablesToMakeReadonly, fieldSymbol);
        }

        private static bool HasAssignmentInLambda(SyntaxNode assignment)
        {
            var parent = assignment.Parent;
            while (parent != null)
            {
                if (parent is AnonymousFunctionExpressionSyntax)
                    return true;
                parent = parent.Parent;
            }
            return false;
        }

        private static void AddVariableThatWasSkippedBeforeBecauseItLackedAInitializer(Dictionary<IFieldSymbol, VariableDeclaratorSyntax> variablesToMakeReadonly, IFieldSymbol fieldSymbol, SyntaxNode assignment, SemanticModel semanticModel)
        {
            if (!fieldSymbol.IsReadOnly && !variablesToMakeReadonly.Keys.Contains(fieldSymbol))
            {
                var containingType = assignment.FirstAncestorOfKind(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
                if (containingType == null) return;
                var containingTypeSymbol = semanticModel.GetDeclaredSymbol(containingType) as INamedTypeSymbol;
                if (containingTypeSymbol == null) return;
                if (!fieldSymbol.ContainingType.Equals(containingTypeSymbol)) return;
                foreach (var variable in fieldSymbol.DeclaringSyntaxReferences)
                    variablesToMakeReadonly.Add(fieldSymbol, (VariableDeclaratorSyntax)variable.GetSyntax());
            }
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
            if (fieldDeclaration == null ||
                IsComplexValueType(semanticModel, fieldDeclaration) ||
                !CanBeMadeReadonly(fieldDeclaration))
            {
                return variablesToMakeReadonly;
            }
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

        private static bool IsComplexValueType(SemanticModel semanticModel, FieldDeclarationSyntax fieldDeclaration)
        {
            var fieldTypeName = fieldDeclaration.Declaration.Type;
            var fieldType = semanticModel.GetTypeInfo(fieldTypeName).ConvertedType;
            return fieldType.IsValueType && !(fieldType.TypeKind == TypeKind.Enum || fieldType.IsPrimitive());
        }

        private static bool CanBeMadeReadonly(IFieldSymbol fieldSymbol)
        {
            return (fieldSymbol.DeclaredAccessibility == Accessibility.NotApplicable
                || fieldSymbol.DeclaredAccessibility == Accessibility.Private)
                && !fieldSymbol.IsReadOnly
                && !fieldSymbol.IsConst;
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
