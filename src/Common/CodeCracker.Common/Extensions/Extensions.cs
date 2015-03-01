using System.Collections.Generic;

namespace CodeCracker
{
    public static class Extensions
    {
        public static string ToDiagnosticId(this DiagnosticId diagnosticId) => $"CC{(int)diagnosticId :D4}";

        public static IDictionary<K, V> AddRange<K, V>(this IDictionary<K, V> dictionary, IDictionary<K, V> newValues)
        {
            if (dictionary == null || newValues == null) return dictionary;
            foreach (var kv in newValues) dictionary.Add(kv);
            return dictionary;
        }

    }
}
