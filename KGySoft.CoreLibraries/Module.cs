using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace KGySoft
{
    internal static class Module
    {
        [ModuleInitializer]
        internal static void ModuleInitializer()
        {
            // Just referencing Res in order to trigger its static constructor and initialize the project resources.
            // Thus configuring LanguageSettings in a consumer project will work for resources of KGySoft.CoreLibraries even if Res was not accessed yet.
            Res.Initialize();
        }
    }
}
