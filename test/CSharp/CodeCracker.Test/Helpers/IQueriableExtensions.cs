using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.Test.CSharp.Helpers
{
    public static class IQueriableExtensions
    {
#pragma warning disable CC0057 // Unused parameters
        public static Task<TSource> FirstAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default(CancellationToken)) => null;

        public static Task<TSource> FirstOrDefaultAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default(CancellationToken)) => null;

        public static Task<TSource> LastAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default(CancellationToken)) => null;

        public static Task<TSource> LastOrDefaultAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default(CancellationToken)) => null;

        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source,
           CancellationToken cancellationToken = default(CancellationToken)) => Task.FromResult(false);

        public static Task<TSource> SingleAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default(CancellationToken)) => null;

        public static Task<TSource> SingleOrDefaultAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default(CancellationToken)) => null;

        public static Task<int> CountAsync<TSource>(
            this IQueryable<TSource> source,
            CancellationToken cancellationToken = default(CancellationToken)) => Task.FromResult(0);

#pragma warning restore CC0057 // Unused parameters
    }
}