using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ChangeNamespaceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CodeCracker.ChangeNamespaceAnalyzer";
        internal const string Title = "Change Namespace";
        internal const string MessageFormat = "Consider change Namespace to Source File Directory Path";
        internal const string Category = "Naming";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNamespace, SyntaxKind.NamespaceDeclaration);
        }

        private static void AnalyzeNamespace(SyntaxNodeAnalysisContext context)
        {
            var newNameSpaceName = ReturnFileNameSpace(context.SemanticModel);
            if (newNameSpaceName == "") return;

            var namespaceStatement = (NamespaceDeclarationSyntax)context.Node;
            var nameSpaceIdentifier = namespaceStatement.Name.ToString();

            if (nameSpaceIdentifier != newNameSpaceName)
            {
                var diagnostic = Diagnostic.Create(Rule, namespaceStatement.Name.GetLocation(), Title);

                context.ReportDiagnostic(diagnostic);
            }
        }

        public static string ReturnFileNameSpace(SemanticModel semanticModel)
        {
            var sourceDirectory = Path.GetDirectoryName(semanticModel.SyntaxTree.FilePath.ToString()).Replace(" ", "");
            if (sourceDirectory == "") return "";
            var dirArray = sourceDirectory.Split('\\');

            var newNameSpace = "";
            foreach (var n in dirArray)
            {
                if (!n.Contains(":"))
                {
                    newNameSpace += n + ".";
                }
            }
            newNameSpace = newNameSpace.Substring(0, newNameSpace.Length - 1);
            return newNameSpace;
        }
    }
}
