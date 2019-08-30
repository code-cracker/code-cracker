using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker
{
    /// <summary>
    /// Contains all DiagnosticSeverities for all DiagnosticIds.
    /// </summary>
    public class SeverityConfigurations
    {
        /// <summary>
        /// Gets the DiagnosticSeverities that is configured by the user in VB language.
        /// </summary>
        public static SeverityConfigurations CurrentVB => _vbDefault.Value;

        /// <summary>
        /// Gets the DiagnosticSeverities that is configured by the user in C# language.
        /// </summary>
        public static SeverityConfigurations CurrentCS => _csDefault.Value;

        /// <summary>
        /// Gets the default DiagnosticSeverities of all DiagnosticIds.
        /// </summary>
        public static SeverityConfigurations Default => _default.Value;

        /// <summary>
        /// Initialize a new instance of <see cref="SeverityConfigurations"/>.
        /// </summary>
        /// <param name="additional">The differential to the default values.</param>
        private SeverityConfigurations(IDictionary<DiagnosticId, DiagnosticSeverity> additional = null)
        {
            if (additional != null)
            {
                foreach (var pair in additional)
                {
                    _diagnosticSeverities[pair.Key] = pair.Value;
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="DiagnosticSeverity"/> of the specified <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The <see cref="DiagnosticId"/> which you want to get the <see cref="DiagnosticSeverity"/>.</param>
        /// <returns>The <see cref="DiagnosticSeverity"/> of the specified <paramref name="id"/>.</returns>
        public DiagnosticSeverity this[DiagnosticId id]
            => _diagnosticSeverities.TryGetValue(id, out var value)
                ? value
                : DiagnosticSeverity.Warning;

        /// <summary>
        /// Stores all the DiagnosticSeverities.
        /// </summary>
        private readonly ConcurrentDictionary<DiagnosticId, DiagnosticSeverity> _diagnosticSeverities
            = new ConcurrentDictionary<DiagnosticId, DiagnosticSeverity>(new Dictionary<DiagnosticId, DiagnosticSeverity>
            {
                { DiagnosticId.None, DiagnosticSeverity.Hidden },
                { DiagnosticId.AlwaysUseVar, DiagnosticSeverity.Warning },
                { DiagnosticId.ArgumentException, DiagnosticSeverity.Warning },
                { DiagnosticId.CatchEmpty, DiagnosticSeverity.Warning },
                { DiagnosticId.EmptyCatchBlock, DiagnosticSeverity.Warning },
                { DiagnosticId.EmptyObjectInitializer, DiagnosticSeverity.Warning },
                { DiagnosticId.ForInArray, DiagnosticSeverity.Warning },
                { DiagnosticId.IfReturnTrue, DiagnosticSeverity.Warning },
                { DiagnosticId.ObjectInitializer_LocalDeclaration, DiagnosticSeverity.Warning },
                { DiagnosticId.ObjectInitializer_Assignment, DiagnosticSeverity.Warning },
                { DiagnosticId.Regex, DiagnosticSeverity.Error },
                { DiagnosticId.RemoveWhereWhenItIsPossible, DiagnosticSeverity.Warning },
                { DiagnosticId.RethrowException, DiagnosticSeverity.Warning },
                { DiagnosticId.TernaryOperator_Return, DiagnosticSeverity.Info },
                { DiagnosticId.TernaryOperator_Assignment, DiagnosticSeverity.Info },
                { DiagnosticId.UnnecessaryParenthesis, DiagnosticSeverity.Warning },
                { DiagnosticId.SwitchToAutoProp, DiagnosticSeverity.Info },
                { DiagnosticId.ExistenceOperator, DiagnosticSeverity.Info },
                { DiagnosticId.ConvertToSwitch, DiagnosticSeverity.Info },
                { DiagnosticId.ConvertLambdaExpressionToMethodGroup, DiagnosticSeverity.Hidden },
                { DiagnosticId.NameOf, DiagnosticSeverity.Warning },
                { DiagnosticId.DisposableVariableNotDisposed, DiagnosticSeverity.Warning },
                { DiagnosticId.SealedAttribute, DiagnosticSeverity.Warning },
                { DiagnosticId.StaticConstructorException, DiagnosticSeverity.Warning },
                { DiagnosticId.EmptyFinalizer, DiagnosticSeverity.Warning },
                { DiagnosticId.CallExtensionMethodAsExtension, DiagnosticSeverity.Info },
                { DiagnosticId.DisposablesShouldCallSuppressFinalize, DiagnosticSeverity.Warning },
                { DiagnosticId.MakeLocalVariableConstWhenItIsPossible, DiagnosticSeverity.Info },
                { DiagnosticId.UseInvokeMethodToFireEvent, DiagnosticSeverity.Warning },
                { DiagnosticId.DisposableFieldNotDisposed_Returned, DiagnosticSeverity.Info },
                { DiagnosticId.DisposableFieldNotDisposed_Created, DiagnosticSeverity.Warning },
                { DiagnosticId.AllowMembersOrdering, DiagnosticSeverity.Hidden },
                { DiagnosticId.RedundantFieldAssignment, DiagnosticSeverity.Info },
                { DiagnosticId.RemoveCommentedCode, DiagnosticSeverity.Info },
                { DiagnosticId.ConvertToExpressionBodiedMember, DiagnosticSeverity.Hidden },
                { DiagnosticId.StringBuilderInLoop, DiagnosticSeverity.Warning },
                { DiagnosticId.InvertFor, DiagnosticSeverity.Hidden },
                { DiagnosticId.ChangeAnyToAll, DiagnosticSeverity.Hidden },
                { DiagnosticId.ParameterRefactory, DiagnosticSeverity.Hidden },
                { DiagnosticId.StringRepresentation_RegularString, DiagnosticSeverity.Hidden },
                { DiagnosticId.StringRepresentation_VerbatimString, DiagnosticSeverity.Hidden },
                { DiagnosticId.PropertyPrivateSet, DiagnosticSeverity.Hidden },
                { DiagnosticId.StringFormat, DiagnosticSeverity.Info },
                { DiagnosticId.SimplifyRedundantBooleanComparisons, DiagnosticSeverity.Info },
                { DiagnosticId.ReadonlyField, DiagnosticSeverity.Info },
                { DiagnosticId.JsonNet, DiagnosticSeverity.Error },
                { DiagnosticId.StringFormatArgs_InvalidArgs, DiagnosticSeverity.Error },
                { DiagnosticId.UnusedParameters, DiagnosticSeverity.Warning },
                { DiagnosticId.AbstractClassShouldNotHavePublicCtors, DiagnosticSeverity.Warning },
                { DiagnosticId.TaskNameAsync, DiagnosticSeverity.Info },
                { DiagnosticId.InterfaceName, DiagnosticSeverity.Info },
                { DiagnosticId.Uri, DiagnosticSeverity.Error },
                { DiagnosticId.IPAddress, DiagnosticSeverity.Error },
                { DiagnosticId.RemoveTrailingWhitespace, DiagnosticSeverity.Info },
                { DiagnosticId.VirtualMethodOnConstructor, DiagnosticSeverity.Warning },
                { DiagnosticId.RemovePrivateMethodNeverUsed, DiagnosticSeverity.Info },
                { DiagnosticId.UseConfigureAwaitFalse, DiagnosticSeverity.Hidden },
                { DiagnosticId.IntroduceFieldFromConstructor, DiagnosticSeverity.Hidden },
                { DiagnosticId.RemoveAsyncFromMethod, DiagnosticSeverity.Info },
                { DiagnosticId.AddBracesToSwitchSections, DiagnosticSeverity.Hidden },
                { DiagnosticId.NoPrivateReadonlyField, DiagnosticSeverity.Info },
                { DiagnosticId.MergeNestedIf, DiagnosticSeverity.Hidden },
                { DiagnosticId.SplitIntoNestedIf, DiagnosticSeverity.Hidden },
                { DiagnosticId.NumericLiteral, DiagnosticSeverity.Hidden },
                { DiagnosticId.TernaryOperator_Iif, DiagnosticSeverity.Warning },
                { DiagnosticId.UseStaticRegexIsMatch, DiagnosticSeverity.Info },
                { DiagnosticId.ComputeExpression, DiagnosticSeverity.Hidden },
                { DiagnosticId.UseStringEmpty, DiagnosticSeverity.Hidden },
                { DiagnosticId.UseEmptyString, DiagnosticSeverity.Hidden },
                { DiagnosticId.RemoveRedundantElseClause, DiagnosticSeverity.Info },
                { DiagnosticId.XmlDocumentation_MissingInCSharp, DiagnosticSeverity.Info },
                { DiagnosticId.MakeMethodStatic, DiagnosticSeverity.Warning },
                { DiagnosticId.ChangeAllToAny, DiagnosticSeverity.Hidden },
                { DiagnosticId.ConsoleWriteLine, DiagnosticSeverity.Info },
                { DiagnosticId.XmlDocumentation_MissingInXml, DiagnosticSeverity.Warning },
                { DiagnosticId.NameOf_External, DiagnosticSeverity.Warning },
                { DiagnosticId.StringFormatArgs_ExtraArgs, DiagnosticSeverity.Warning },
                { DiagnosticId.AlwaysUseVarOnPrimitives, DiagnosticSeverity.Warning },
                { DiagnosticId.PropertyChangedEventArgsUnnecessaryAllocation, DiagnosticSeverity.Hidden },
                { DiagnosticId.UnnecessaryToStringInStringConcatenation, DiagnosticSeverity.Info },
                { DiagnosticId.SwitchCaseWithoutDefault, DiagnosticSeverity.Warning },
                { DiagnosticId.ReadOnlyComplexTypes, DiagnosticSeverity.Warning },
                { DiagnosticId.ReplaceWithGetterOnlyAutoProperty, DiagnosticSeverity.Hidden },
            });

        /// <summary>
        /// Lazy initialize value for <see cref="CurrentVB"/>.
        /// </summary>
        private static readonly Lazy<SeverityConfigurations> _vbDefault = new Lazy<SeverityConfigurations>(()
            => new SeverityConfigurations(new Dictionary<DiagnosticId, DiagnosticSeverity>
            {
                // These are the differantials to the C# version.
                { DiagnosticId.TernaryOperator_Assignment, DiagnosticSeverity.Warning },
                { DiagnosticId.TernaryOperator_Return, DiagnosticSeverity.Warning },
            }), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Lazy initialize value for <see cref="CurrentCS"/>.
        /// </summary>
        private static readonly Lazy<SeverityConfigurations> _csDefault = new Lazy<SeverityConfigurations>(()
            => new SeverityConfigurations(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Lazy initialize value for default.
        /// </summary>
        private static readonly Lazy<SeverityConfigurations> _default = new Lazy<SeverityConfigurations>(()
            => new SeverityConfigurations(), LazyThreadSafetyMode.ExecutionAndPublication);
    }
}
