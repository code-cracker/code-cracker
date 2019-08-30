using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCracker
{
    public class SeverityConfigurations
    {
        public static SeverityConfigurations Current { get; } = new SeverityConfigurations();

        public static SeverityConfigurations Default { get; } = new SeverityConfigurations();

        private readonly ConcurrentDictionary<DiagnosticId, DiagnosticSeverity> _diagnosticServerities;

        private SeverityConfigurations()
        {
            var values = new Dictionary<DiagnosticId, DiagnosticSeverity>
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
            };
            _diagnosticServerities = new ConcurrentDictionary<DiagnosticId, DiagnosticSeverity>(values);
        }

        public DiagnosticSeverity this[DiagnosticId id]
            => _diagnosticServerities.TryGetValue(id, out var value)
                ? value
                : DiagnosticSeverity.Warning;
    }
}
