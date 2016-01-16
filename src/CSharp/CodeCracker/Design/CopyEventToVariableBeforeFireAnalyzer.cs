using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Linq;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CopyEventToVariableBeforeFireAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Copy Event To Variable Before Fire";
        internal const string MessageFormat = "Copy the '{0}' event to a variable before firing it.";
        internal const string Category = SupportedCategories.Design;
        const string Description = "Events should always be checked for null before being invoked.\r\n"
            + "As in a multi-threading context it is possible for an event to be unsubscribed between "
            + "the moment where it is checked to be non-null and the moment it is raised the event must "
            + "be copied to a temporary variable before the check.";
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.CopyEventToVariableBeforeFire));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var invocation = (InvocationExpressionSyntax)context.Node;
            var identifier = invocation.Expression as IdentifierNameSyntax;
            if (identifier == null) return;
            if (context.Node.Parent.GetType().Name == nameof(ArrowExpressionClauseSyntax)) return;

            var typeInfo = context.SemanticModel.GetTypeInfo(identifier, context.CancellationToken);

            if (typeInfo.ConvertedType?.BaseType == null) return;

            var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;

            if (typeInfo.ConvertedType.BaseType.Name != typeof(MulticastDelegate).Name ||
                symbol is ILocalSymbol || symbol is IParameterSymbol || IsReadOnlyAndInitializedForCertain(context, symbol)) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), identifier.Identifier.Text));
        }

        /// <summary>
        /// Determines whether the specified symbol is a read only field and initialized in the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="symbol">The symbol.</param>
        /// <returns>
        /// True if the symbol is a read only field that is initialized either on declaration or in all constructors of the containing type; otherwise false.
        /// </returns>
        /// <remarks>
        /// If the symbol is initialized in a block of code of the constructor that might not always be called, the symbol is considered to
        /// not be initialized for certain. For more information <seealso cref="DoesBlockContainDefiniteInitializer(SyntaxNodeAnalysisContext, ISymbol, IEnumerable{StatementSyntax})"/>
        /// </remarks>
        private static bool IsReadOnlyAndInitializedForCertain(SyntaxNodeAnalysisContext context, ISymbol symbol)
        {
            if (!(symbol is IFieldSymbol)) return false;

            var field = symbol as IFieldSymbol;
            foreach (var declaringSyntaxReference in symbol.DeclaringSyntaxReferences)
            {
                var variableDeclarator = declaringSyntaxReference.GetSyntax(context.CancellationToken) as VariableDeclaratorSyntax;
                
                if (variableDeclarator != null && variableDeclarator.Initializer != null && field.IsReadOnly && 
                    !variableDeclarator.Initializer.Value.IsKind(SyntaxKind.NullLiteralExpression)) return true;
            }

            foreach (var constructor in symbol.ContainingType.Constructors)
            {
                foreach (var declaringSyntaxReference in constructor.DeclaringSyntaxReferences)
                {
                    var constructorSyntax = declaringSyntaxReference.GetSyntax() as ConstructorDeclarationSyntax;
                    if (constructorSyntax != null)
                    {
                        if (DoesBlockContainCertainInitializer(context, symbol, constructorSyntax.Body.Statements) == InitializerState.Initializer && field.IsReadOnly) return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Used to indicate what can be said about the initialization
        /// of a symbol in a given block of statements.
        /// </summary>
        private enum InitializerState
        {
            /// <summary>
            /// Indicates that the block of statements does NOT initialize the symbol for certain.
            /// </summary>
            None,
            /// <summary>
            /// Indicates that the block of statements DOES initialize the symbol for certain.
            /// </summary>
            Initializer,
            /// <summary>
            /// Indicates that the block of statements contains a way to skip any initializers
            /// following the given block of statements (for instance a return statement inside
            /// an if statement can skip any initializers after the if statement).
            /// </summary>
            WayToSkipInitializer,
        }

        /// <summary>
        /// This method can be used to determine if the specified block of
        /// statements contains an initializer for the specified symbol.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="symbol">The symbol.</param>
        /// <param name="statements">The statements.</param>
        /// <returns>
        /// True if the symbol is initialized for certain in the block of statements give; otherwise false.
        /// </returns>
        /// <remarks>
        /// Code blocks that might not always be called are:
        /// - An if or else statement.
        /// - The body of a for, while or for-each loop.
        /// - Switch statements
        /// 
        /// The following exceptions are taken into account:
        /// - If both if and else statements contain a certain initialization.
        /// - If all cases in a switch contain a certain initialization (this means a default case must exist as well).
        /// 
        /// Please note that this is a recursive function so we can check a block of code in an if statement for example.
        /// </remarks>
        private static InitializerState DoesBlockContainCertainInitializer(SyntaxNodeAnalysisContext context, ISymbol symbol, IEnumerable<StatementSyntax> statements)
        {
            // Keep track of the current initializer state. This can only be None
            // or Initializer, WayToSkipInitialezer will always be returned immediately.
            // Only way to go back from Initializer to None is if there is an assignment
            // to null after a previous assignment to a non-null value.
            var currentState = InitializerState.None;
            
            foreach (var statement in statements)
            {
                if (statement is ReturnStatementSyntax && currentState == InitializerState.None)
                {
                    return InitializerState.WayToSkipInitializer;
                }
                else if (statement is BlockSyntax)
                {
                    var blockResult = DoesBlockContainCertainInitializer(context, symbol, (statement as BlockSyntax).Statements);
                    if (blockResult == InitializerState.WayToSkipInitializer && currentState == InitializerState.None)
                        return blockResult;
                    if (blockResult == InitializerState.Initializer)
                        currentState = blockResult;
                }
                else if (statement is UsingStatementSyntax)
                {
                    var blockResult = DoesBlockContainCertainInitializer(context, symbol, new[] { (statement as UsingStatementSyntax).Statement });
                    if (blockResult == InitializerState.WayToSkipInitializer && currentState == InitializerState.None)
                        return blockResult;
                    if (blockResult == InitializerState.Initializer)
                        currentState = blockResult;
                }
                else if (statement is ExpressionStatementSyntax)
                {
                    var expression = (statement as ExpressionStatementSyntax).Expression;
                    if (expression is AssignmentExpressionSyntax)
                    {
                        var identifier = (expression as AssignmentExpressionSyntax).Left;
                        if (identifier != null)
                        {
                            var right = (expression as AssignmentExpressionSyntax).Right;
                            if (right != null && right.IsKind(SyntaxKind.NullLiteralExpression))
                                currentState = InitializerState.None;
                            else if (context.SemanticModel.GetSymbolInfo(identifier).Symbol == symbol)
                                currentState = InitializerState.Initializer;
                        }
                    }
                }
                else if(statement is SwitchStatementSyntax)
                {
                    var switchStatement = statement as SwitchStatementSyntax;
                    if(switchStatement.Sections.Any(s => s.Labels.Any(l => l is DefaultSwitchLabelSyntax)))
                    {
                        var results = switchStatement.Sections.Select(s => DoesBlockContainCertainInitializer(context, symbol, s.Statements));
                        if(results.All(r => r == InitializerState.Initializer))
                            currentState = InitializerState.Initializer;
                        else if(results.Any(r => r == InitializerState.WayToSkipInitializer && currentState == InitializerState.None))
                            return InitializerState.WayToSkipInitializer;
                    }                    
                }
                else if (statement is IfStatementSyntax)
                {
                    var ifStatement = statement as IfStatementSyntax;

                    var ifResult = DoesBlockContainCertainInitializer(context, symbol, new[] { ifStatement.Statement });
                    if (ifStatement.Else != null)
                    {                       
                        var elseResult = DoesBlockContainCertainInitializer(context, symbol, new[] { ifStatement.Else.Statement });

                        if (ifResult == InitializerState.Initializer && elseResult == InitializerState.Initializer)
                        {
                            currentState = InitializerState.Initializer;
                        }
                        if (elseResult == InitializerState.WayToSkipInitializer && currentState == InitializerState.None)
                        {
                            return InitializerState.WayToSkipInitializer;
                        }
                    }
                    if (ifResult == InitializerState.WayToSkipInitializer && currentState == InitializerState.None)
                    {
                        return InitializerState.WayToSkipInitializer;
                    }
                }
            }
            return currentState;
        }
    }
}