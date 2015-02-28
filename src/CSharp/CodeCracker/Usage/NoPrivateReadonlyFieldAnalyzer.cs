using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System;

namespace CodeCracker.Usage
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class NoPrivateReadonlyFieldAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "CC0074";
		internal const string Title = "Make field readonly";
		internal const string Message = "Make '{0}' readonly";
		internal const string Category = SupportedCategories.Usage;
		const string Description = "A field that is only assigned on the constructor can be made readonly.";

		internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
			DiagnosticId,
			Title,
			Message,
			Category,
			DiagnosticSeverity.Info,
			isEnabledByDefault: true,
			description: Description,
			helpLink: HelpLink.ForDiagnostic(DiagnosticId));

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

		public override void Initialize(AnalysisContext analysisContext)
		{
			var candidateFields = new List<FieldCandidate>();
			var assignedFields = new List<ISymbol>();

			analysisContext.RegisterCompilationStartAction(compilationStartContext =>
			{
				foreach (var tree in compilationStartContext.Compilation.SyntaxTrees)
					CaptureCandidateAssignedFieldsFromTree(tree, compilationStartContext, candidateFields, assignedFields);
			});

			analysisContext.RegisterCompilationEndAction(compilationEndContext =>
			{
				foreach (var candidateField in candidateFields.Where(field => HasNoAssignment(field, assignedFields)))
					compilationEndContext.ReportDiagnostic(Diagnostic.Create(
						Rule,
						candidateField.Variable.Identifier.GetLocation(),
						candidateField.Variable.Identifier.Text));
			});
		}

		private void CaptureCandidateAssignedFieldsFromTree(SyntaxTree tree, CompilationStartAnalysisContext compilationStartContext, List<FieldCandidate> candidateFields, List<ISymbol> assignedFields)
		{
			if (!compilationStartContext.Compilation.SyntaxTrees.Contains(tree))
				return;

			SyntaxNode root;
			if (!tree.TryGetRoot(out root))
				return;

			var semanticModel = compilationStartContext.Compilation.GetSemanticModel(tree);

			candidateFields.AddRange(GetCandidateFields(root, semanticModel));

			if (candidateFields.Count() == 0)
				return;

			assignedFields.AddRange(GetAssignedField(root, semanticModel));
		}

		#region GetCandidateFields

		private IEnumerable<FieldCandidate> GetCandidateFields(SyntaxNode root, SemanticModel semanticModel)
		{
			return root
					.DescendantNodesAndSelf()
					.OfType<FieldDeclarationSyntax>()
					.Where(CanBecameReadOnlyField)
					.SelectMany(s => s.Declaration.Variables)
					.Select(s => new FieldCandidate { Variable = s, FieldSymbol = semanticModel.GetDeclaredSymbol(s) as IFieldSymbol })
					.Where(p => p.FieldSymbol != null && p.FieldSymbol.ContainingType != null);
		}

		private bool CanBecameReadOnlyField(FieldDeclarationSyntax field)
		{
			var noPrivate = field.Modifiers.Any(p => p.IsKind(SyntaxKind.PublicKeyword) || p.IsKind(SyntaxKind.ProtectedKeyword) || p.IsKind(SyntaxKind.InternalKeyword));
			return noPrivate ? !field.Modifiers.Any(p => p.IsKind(SyntaxKind.ConstKeyword) || p.IsKind(SyntaxKind.ReadOnlyKeyword)) : false;
		}

		#endregion

		#region GetAssignedField

		private IEnumerable<ISymbol> GetAssignedField(SyntaxNode root, SemanticModel semanticModel) =>
			GetClassAndStructTypeDeclaration(root)
				.Select(s => new TypeDeclarationWithSymbol { TypeDeclaration = s, NamedTypeSymbol = semanticModel.GetDeclaredSymbol(s) })
				.SelectMany(type => GetAssignedFieldFromType(type, semanticModel));

		private IEnumerable<TypeDeclarationSyntax> GetClassAndStructTypeDeclaration(SyntaxNode root) =>
			root.DescendantNodesAndSelf()
				.OfType<TypeDeclarationSyntax>()
				.Where(p => p.GetType() != typeof(InterfaceDeclarationSyntax));

		private IEnumerable<ISymbol> GetAssignedFieldFromType(TypeDeclarationWithSymbol typeDeclarationWithSymbol, SemanticModel model)
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

		private bool SkipNestedTypes(TypeDeclarationSyntax typeDeclaration, SyntaxNode node) =>
			node is TypeDeclarationSyntax ? node == typeDeclaration : true;

		private bool SkipFieldsFromItsOwnConstructor(TypeDeclarationWithSymbol typeDeclarationWithSymbol, ExpressionSyntax p)
		{
			throw new NotImplementedException();
		}

		private bool SkipFieldsFromItsOwnConstructor(TypeDeclarationWithSymbol type, ExpressionSyntax assignmentExpression, ISymbol assignmentSymbol)
		{
			var parentConstructor = assignmentExpression.Ancestors().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();

			if (parentConstructor == null)
				return true;

			return
				assignmentSymbol.ContainingType != type.NamedTypeSymbol ||
				assignmentSymbol.IsStatic != parentConstructor.Modifiers.Any(p => p.IsKind(SyntaxKind.StaticKeyword));
		}

		#endregion

		private bool HasNoAssignment(FieldCandidate field, List<ISymbol> assignedFields) =>
			!assignedFields.Any(assignedField => assignedField == field.FieldSymbol);

		private class FieldCandidate
		{
			internal VariableDeclaratorSyntax Variable { get; set; }
			internal IFieldSymbol FieldSymbol { get; set; }
		}

		private class TypeDeclarationWithSymbol
		{
			internal TypeDeclarationSyntax TypeDeclaration { get; set; }
			internal INamedTypeSymbol NamedTypeSymbol { get; set; }
		}
	}
}