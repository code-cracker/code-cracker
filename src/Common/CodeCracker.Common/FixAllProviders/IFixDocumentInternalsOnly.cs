using Microsoft.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.FixAllProviders
{
    /// <summary>
    /// This interface must be implemented by the associated CodeFixProvider. The CodeFixProvider must operate on a single document and
    /// should only change the document. This limits the possible operations of the CodeFixProvider to change only document internals without
    /// effecting other parts of the solution.
    /// </summary>
    public interface IFixDocumentInternalsOnly
    {
        Task<Document> FixDocumentAsync(SyntaxNode nodeWithDiagnostic, Document document, CancellationToken cancellationToken);
    }
}