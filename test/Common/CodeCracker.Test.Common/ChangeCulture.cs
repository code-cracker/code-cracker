using System;
using System.Globalization;
using System.Threading;

namespace CodeCracker.Test
{
    public class ChangeCulture : IDisposable
    {
        private readonly CultureInfo originalCulture;
        private readonly CultureInfo originalUICulture;
        private readonly CultureInfo originalDefaultCulture;
        private readonly CultureInfo originalDefaultUICulture;

        public ChangeCulture(string cultureName)
        {
            originalCulture = Thread.CurrentThread.CurrentCulture;
            originalUICulture = Thread.CurrentThread.CurrentUICulture;
            originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
            originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture =
            CultureInfo.DefaultThreadCurrentCulture =
            CultureInfo.DefaultThreadCurrentUICulture =
                CultureInfo.GetCultureInfo(cultureName);

        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUICulture;
            CultureInfo.DefaultThreadCurrentCulture = originalDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalDefaultUICulture;
            GC.SuppressFinalize(this);
        }
    }
}