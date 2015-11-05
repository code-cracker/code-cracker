using System;
using System.Globalization;
using System.Threading;

namespace CodeCracker.Test
{
    public class ChangeCulture : IDisposable
    {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUICulture;
        private readonly CultureInfo _originalDefaultCulture;
        private readonly CultureInfo _originalDefaultUICulture;

        public ChangeCulture(string cultureName)
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _originalUICulture = Thread.CurrentThread.CurrentUICulture;
            _originalDefaultCulture = CultureInfo.DefaultThreadCurrentCulture;
            _originalDefaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;

            Thread.CurrentThread.CurrentCulture =
            Thread.CurrentThread.CurrentUICulture =
            CultureInfo.DefaultThreadCurrentCulture =
            CultureInfo.DefaultThreadCurrentUICulture =
                CultureInfo.GetCultureInfo(cultureName);

        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
            Thread.CurrentThread.CurrentUICulture = _originalUICulture;
            CultureInfo.DefaultThreadCurrentCulture = _originalDefaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _originalDefaultUICulture;
            GC.SuppressFinalize(this);
        }
    }
}
