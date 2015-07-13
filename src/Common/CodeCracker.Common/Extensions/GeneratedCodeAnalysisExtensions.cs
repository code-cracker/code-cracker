using System.Text.RegularExpressions;

namespace CodeCracker
{
    public static class GeneratedCodeAnalysisExtensions
    {
        public static bool IsOnGeneratedFile(this string filePath) =>
            Regex.IsMatch(filePath, @"(\\service|\\TemporaryGeneratedFile_.*|\\assemblyinfo|\\assemblyattributes|\.(g\.i|g|designer|generated|assemblyattributes))\.(cs|vb)$",
                RegexOptions.IgnoreCase);
    }
}