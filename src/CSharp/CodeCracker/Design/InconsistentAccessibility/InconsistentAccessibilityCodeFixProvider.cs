using System.Collections.Immutable;
using System.Composition;
using System.Threading;
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

        private readonly IInconsistentAccessibilityCodeFix inconsistentAccessibilityCodeFix =
            new AccessibilityDomainChecker(new InconsistentAccessibilityCodeFix());

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override ImmutableArray<string> FixableDiagnosticIds
            =>
                ImmutableArray.Create(
                    InconsistentAccessibilityInMethodReturnTypeCompilerErrorNumber,
                    InconsistentAccessibilityInMethodParameterCompilerErrorNumber,
                    InconsistentAccessibilityInFieldTypeCompilerErrorNumber,
                    InconsistentAccessibilityInPropertyTypeCompilerErrorNumber,
                    InconsistentAccessibilityInIndexerReturnTypeCompilerErrorNumber,
                    InconsistentAccessibilityInIndexerParameterCompilerErrorNumber,
                    InconsistentAccessibilityInOperatorReturnTypeCompilerErrorNumber,
                    InconsistentAccessibilityInOperatorParameterCompilerErrorNumber,
                    InconsistentAccessibilityInDelegateReturnTypeCompilerErrorNumber,
                    InconsistentAccessibilityInDelegateParameterTypeCompilerErrorNumber);

        private static async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            InconsistentAccessibilityInfoProvider inconsistentAccessibilityProvider = null;

            switch (diagnostic.Id)
            {
                case InconsistentAccessibilityInMethodReturnTypeCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInMethodReturnType();
                    break;
                case InconsistentAccessibilityInMethodParameterCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInMethodParameter();
                    break;
                case InconsistentAccessibilityInFieldTypeCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInFieldType();
                    break;
                case InconsistentAccessibilityInPropertyTypeCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInPropertyType();
                    break;
                case InconsistentAccessibilityInIndexerReturnTypeCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInIndexerReturnType();
                    break;
                case InconsistentAccessibilityInIndexerParameterCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInIndexerParameter();
                    break;
                case InconsistentAccessibilityInOperatorReturnTypeCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInOperatorReturnType();
                    break;
                case InconsistentAccessibilityInOperatorParameterCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInOperatorParameter();
                    break;
                case InconsistentAccessibilityInDelegateReturnTypeCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInDelegateReturnType();
                    break;
                case InconsistentAccessibilityInDelegateParameterTypeCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInDelegateParameterType();
                    break;
            }

            return await inconsistentAccessibilityProvider.GetInconsistentAccessibilityInfoAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var inconsistentAccessibilityInfo = await GetInconsistentAccessibilityInfoAsync(context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false);

                await inconsistentAccessibilityCodeFix.FixAsync(context, diagnostic, inconsistentAccessibilityInfo);
            }
        }
    }
}
