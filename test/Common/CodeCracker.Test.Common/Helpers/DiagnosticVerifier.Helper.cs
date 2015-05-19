using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.Test
{
    /// <summary>
    /// Class for turning strings into documents and getting the diagnostics on them
    /// All methods are static
    /// </summary>
    public abstract partial class DiagnosticVerifier
    {
        private static readonly MetadataReference CorlibReference = MetadataReference.CreateFromAssembly(typeof(object).Assembly);
        private static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromAssembly(typeof(Enumerable).Assembly);
        private static readonly MetadataReference RegexReference = MetadataReference.CreateFromAssembly(typeof(System.Text.RegularExpressions.Regex).Assembly);
        private static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromAssembly(typeof(CSharpCompilation).Assembly);
        private static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromAssembly(typeof(Compilation).Assembly);
        private static readonly MetadataReference JsonNetReference = MetadataReference.CreateFromAssembly(typeof(JsonConvert).Assembly);

        internal static string DefaultFilePathPrefix = nameof(Test);
        internal static string CSharpDefaultFileExt = "cs";
        internal static string VisualBasicDefaultExt = "vb";
        internal static string CSharpDefaultFilePath = DefaultFilePathPrefix + 0 + "." + CSharpDefaultFileExt;
        internal static string VisualBasicDefaultFilePath = DefaultFilePathPrefix + 0 + "." + VisualBasicDefaultExt;
        internal static string TestProjectName = "TestProject";

        /// <summary>
        /// Given classes in the form of strings, their language, and an IDiagnosticAnlayzer to apply to it, return the diagnostics found in the string after converting it to a document.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the soruce classes are in</param>
        /// <param name="analyzer">The analyzer to be run on the sources</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in teh source code, sorted by Location</returns>
        private static async Task<Diagnostic[]> GetSortedDiagnosticsAsync(string[] sources, string language, DiagnosticAnalyzer analyzer, LanguageVersion languageVersionCSharp, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersionVB) =>
            await GetSortedDiagnosticsFromDocumentsAsync(analyzer, GetDocuments(sources, language, languageVersionCSharp, languageVersionVB)).ConfigureAwait(true);

        /// <summary>
        /// Given an analyzer and a document to apply it to, run the analyzer and gather an array of diagnostics found in it.
        /// The returned diagnostics are then ordered by location in the source document.
        /// </summary>
        /// <param name="analyzer">The analyzer to run on the documents</param>
        /// <param name="documents">The Documents that the analyzer will be run on</param>
        /// <returns>An IEnumerable of Diagnostics that surfaced in teh source code, sorted by Location</returns>
        protected async static Task<Diagnostic[]> GetSortedDiagnosticsFromDocumentsAsync(DiagnosticAnalyzer analyzer, Document[] documents)
        {
            var projects = new HashSet<Project>();
            foreach (var document in documents)
                projects.Add(document.Project);

            var diagnostics = new List<Diagnostic>();
            foreach (var project in projects)
            {
                var compilation = await project.GetCompilationAsync().ConfigureAwait(true);
                var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
                var diags = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(true);
                foreach (var diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        foreach (var document in project.Documents)
                        {
                            var tree = await document.GetSyntaxTreeAsync().ConfigureAwait(true);
                            if (tree == diag.Location.SourceTree) diagnostics.Add(diag);
                        }
                    }
                }
            }
            var results = SortDiagnostics(diagnostics);
            return results;
        }

        private static Diagnostic[] SortDiagnostics(List<Diagnostic> diagnostics) =>
            diagnostics.OrderBy(d => d.Location.SourceTree.FilePath).ThenBy(d => d.Location.SourceSpan.Start).ToArray();

        #region Set up compilation and documents
        /// <summary>
        /// Given an array of strings as soruces and a language, turn them into a project and return the documents and spans of it.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Tuple containing the Documents produced from the sources and thier TextSpans if relevant</returns>
        public static Document[] GetDocuments(string[] sources, string language, LanguageVersion languageVersionCSharp, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersionVB)
        {
            if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
                throw new ArgumentException("Unsupported Language");

            for (int i = 0; i < sources.Length; i++)
            {
                var fileName = language == LanguageNames.CSharp ? nameof(Test) + i + ".cs" : nameof(Test) + i + ".vb";
            }

            var project = CreateProject(sources, language, languageVersionCSharp, languageVersionVB);
            var documents = project.Documents.ToArray();

            if (sources.Length != documents.Length)
            {
                throw new SystemException("Amount of sources did not match amount of Documents created");
            }

            return documents;
        }

        /// <summary>
        /// Create a Document from a string through creating a project that contains it.
        /// </summary>
        /// <param name="source">Classes in the form of a string</param>
        /// <param name="language">The language the source code is in</param>
        /// <returns>A Document created from the source string</returns>
        public static Document CreateDocument(string source,
            string language,
            LanguageVersion languageVersionCSharp,
            Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersionVB) =>
            CreateProject(new[] { source }, language, languageVersionCSharp, languageVersionVB).Documents.First();

        /// <summary>
        /// Create a project using the inputted strings as sources.
        /// </summary>
        /// <param name="sources">Classes in the form of strings</param>
        /// <param name="language">The language the source code is in</param>
        /// <param name="workspace">The created workspace containing the project</param>
        /// <returns>A Project created out of the Douments created from the source strings</returns>
        public static Project CreateProject(string[] sources,
            string language,
            LanguageVersion languageVersionCSharp,
            Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersionVB)
        {
            var fileNamePrefix = DefaultFilePathPrefix;
            string fileExt;
            ParseOptions parseOptions;
            if (language == LanguageNames.CSharp)
            {
                fileExt = CSharpDefaultFileExt;
                parseOptions = new CSharpParseOptions(languageVersionCSharp);
            }
            else
            {
                fileExt = VisualBasicDefaultExt;
                parseOptions = new Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions(languageVersionVB);
            }

            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);
#pragma warning disable CC0022
            var workspace = new AdhocWorkspace();
#pragma warning restore CC0022

            var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), TestProjectName,
                TestProjectName, language,
                parseOptions: parseOptions,
                metadataReferences: ImmutableList.Create(
                    CorlibReference, SystemCoreReference, RegexReference,
                    CSharpSymbolsReference, CodeAnalysisReference, JsonNetReference));

            workspace.AddProject(projectInfo);

            var count = 0;
            foreach (var source in sources)
            {
                var newFileName = fileNamePrefix + count + "." + fileExt;
                workspace.AddDocument(projectId, newFileName, SourceText.From(source));
                count++;
            }

            var project = workspace.CurrentSolution.GetProject(projectId);
            var newCompilationOptions = project.CompilationOptions.WithSpecificDiagnosticOptions(diagOptions);
            var solution = workspace.CurrentSolution.WithProjectCompilationOptions(projectId, newCompilationOptions);
            var newProject = solution.GetProject(projectId);
            return newProject;
        }

        private static readonly Dictionary<string, ReportDiagnostic> diagOptions = Enumerable.Range(1, 1000).Select(i => $"CC{i:D4}").ToDictionary(id => id, id => ReportDiagnostic.Default);

        #endregion

        /// <summary>
        /// Given a document, turn it into a string based on the syntax root
        /// </summary>
        /// <param name="document">The Document to be converted to a string</param>
        /// <returns>A string contianing the syntax of the Document after formatting</returns>
        public static async Task<string> GetStringFromDocumentAsync(Document document)
        {
            var simplifiedDoc = await Simplifier.ReduceAsync(document, Simplifier.Annotation).ConfigureAwait(true);
            var root = await simplifiedDoc.GetSyntaxRootAsync().ConfigureAwait(true);
            root = Formatter.Format(root, Formatter.Annotation, simplifiedDoc.Project.Solution.Workspace);
            return root.GetText().ToString();
        }

        public static async Task<string> FormatSourceAsync(string language, string source, LanguageVersion languageVersionCSharp = LanguageVersion.CSharp6, Microsoft.CodeAnalysis.VisualBasic.LanguageVersion languageVersionVB = Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic14)
        {
            var document = CreateDocument(source, language, languageVersionCSharp, languageVersionVB);
            var newDoc = await Formatter.FormatAsync(document).ConfigureAwait(true);
            return (await newDoc.GetSyntaxRootAsync().ConfigureAwait(true)).ToFullString();
        }
    }
}