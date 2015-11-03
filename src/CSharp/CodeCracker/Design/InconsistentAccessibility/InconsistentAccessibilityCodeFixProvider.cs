using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InconsistentAccessibilityCodeFixProvider)), Shared]
    public sealed class InconsistentAccessibilityCodeFixProvider : CodeFixProvider
    {
        internal const string InconsistentAccessibilityInMethodReturnTypeCompilerErrorNumber = "CS0050";
        internal const string InconsistentAccessibilityInMethodParameterCompilerErrorNumber = "CS0051";
        internal const string InconsistentAccessibilityInFieldTypeCompilerErrorNumber = "CS0052";
        internal const string InconsistentAccessibilityInPropertyTypeCompilerErrorNumber = "CS0053";
        internal const string InconsistentAccessibilityInIndexerReturnTypeCompilerErrorNumber = "CS0054";
        internal const string InconsistentAccessibilityInIndexerParameterCompilerErrorNumber = "CS0055";
        internal const string InconsistentAccessibilityInOperatorReturnTypeCompilerErrorNumber = "CS0056";
        internal const string InconsistentAccessibilityInOperatorParameterCompilerErrorNumber = "CS0057";
        internal const string InconsistentAccessibilityInDelegateReturnTypeCompilerErrorNumber = "CS0058";
        internal const string InconsistentAccessibilityInDelegateParameterTypeCompilerErrorNumber = "CS0059";
        internal const string InconsistentAccessibilityInBaseClassCompilerErrorNumber = "CS0060";
        internal const string InconsistentAccessibilityInBaseInterfaceCompilerErrorNumber = "CS0061";

        private static readonly ImmutableDictionary<string, InconsistentAccessibilitySourceProvider> Providers =
            ConfigureInconsistentAccessibilityInfoProviders();

        private readonly IInconsistentAccessibilityCodeFix inconsistentAccessibilityCodeFix =
            new InconsistentAccessibilityCodeFix();

        private readonly IInconsistentAccessibilityFixInfoProvider inconsistentAccessibilityFixInfoProvider =
            ComposeFixInfoProvider();

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override ImmutableArray<string> FixableDiagnosticIds => Providers.Keys.ToImmutableArray();

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var inconsistentAccessibilitySource =
                    await
                        Providers[diagnostic.Id].ExtractInconsistentAccessibilitySourceAsync(context.Document, diagnostic,
                            context.CancellationToken).ConfigureAwait(false);

                if (inconsistentAccessibilitySource != InconsistentAccessibilitySource.Invalid)
                {
                    var fixInfo =
                        await
                            inconsistentAccessibilityFixInfoProvider.CreateFixInfoAsync(context,
                                inconsistentAccessibilitySource);

                    await
                        inconsistentAccessibilityCodeFix.FixAsync(context, diagnostic, inconsistentAccessibilitySource,
                            fixInfo);
                }
            }
        }

        private static IInconsistentAccessibilityFixInfoProvider ComposeFixInfoProvider()
        {
            return new AccessibilityModifiersEvaluator(new AccessibilityDomainChecker());
        }

        private static ImmutableDictionary<string, InconsistentAccessibilitySourceProvider>
            ConfigureInconsistentAccessibilityInfoProviders()
        {
            return new Dictionary<string, InconsistentAccessibilitySourceProvider>
            {
                {
                    InconsistentAccessibilityInBaseInterfaceCompilerErrorNumber,
                    new InconsistentAccessibilityInBaseInterface()
                },
                {InconsistentAccessibilityInBaseClassCompilerErrorNumber, new InconsistentAccessibilityInBaseClass()},
                {
                    InconsistentAccessibilityInDelegateParameterTypeCompilerErrorNumber,
                    new InconsistentAccessibilityInDelegateParameterType()
                },
                {
                    InconsistentAccessibilityInDelegateReturnTypeCompilerErrorNumber,
                    new InconsistentAccessibilityInDelegateReturnType()
                },
                {InconsistentAccessibilityInFieldTypeCompilerErrorNumber, new InconsistentAccessibilityInFieldType()},
                {
                    InconsistentAccessibilityInIndexerParameterCompilerErrorNumber,
                    new InconsistentAccessibilityInIndexerParameter()
                },
                {
                    InconsistentAccessibilityInIndexerReturnTypeCompilerErrorNumber,
                    new InconsistentAccessibilityInIndexerReturnType()
                },
                {
                    InconsistentAccessibilityInMethodParameterCompilerErrorNumber,
                    new InconsistentAccessibilityInMethodParameter()
                },
                {
                    InconsistentAccessibilityInMethodReturnTypeCompilerErrorNumber,
                    new InconsistentAccessibilityInMethodReturnType()
                },
                {
                    InconsistentAccessibilityInOperatorParameterCompilerErrorNumber,
                    new InconsistentAccessibilityInOperatorParameter()
                },
                {
                    InconsistentAccessibilityInOperatorReturnTypeCompilerErrorNumber,
                    new InconsistentAccessibilityInOperatorReturnType()
                },
                {
                    InconsistentAccessibilityInPropertyTypeCompilerErrorNumber,
                    new InconsistentAccessibilityInPropertyType()
                }
            }.ToImmutableDictionary();
        }
    }
}
