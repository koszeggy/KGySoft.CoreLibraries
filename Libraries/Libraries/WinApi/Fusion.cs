using System.Runtime.InteropServices;

namespace KGySoft.Libraries.WinApi
{

    /// <summary>
    /// The fusion API enables a runtime host to access the properties of an application's resources in order to locate
    /// the correct versions of those resources for the application.
    /// </summary>
    internal static class Fusion
    {
        /// <summary>
        /// Gets a pointer to a new <see cref="IAssemblyCache"/> instance that represents the global assembly cache.
        /// </summary>
        /// <param name="ppAsmCache">The returned <see cref="IAssemblyCache"/> pointer.</param>
        /// <param name="dwReserved">Reserved for future extensibility. dwReserved must be 0 (zero).</param>
        [DllImport("fusion.dll")]
        internal static extern int CreateAssemblyCache(out IAssemblyCache ppAsmCache, int dwReserved);
    }
}
