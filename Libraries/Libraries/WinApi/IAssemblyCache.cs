using System.Runtime.InteropServices;
namespace KGySoft.Libraries.WinApi
{
    /// <summary>
    /// Represents the global assembly cache for use by the fusion technology.
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
    internal interface IAssemblyCache
    {
        /// <summary>
        /// Placeholder for the UninstallAssembly method, which is the first one in this COM interface
        /// </summary>
        void Reserved0();

        /// <summary>
        /// Gets the requested data about the specified assembly.
        /// </summary>
        /// <param name="flags"> Flags defined in Fusion.idl. The following values are supported:
        /// QUERYASMINFO_FLAG_VALIDATE (0x00000001)
        /// QUERYASMINFO_FLAG_GETSIZE (0x00000002)</param>
        /// <param name="assemblyName">The name of the assembly for which data will be retrieved.</param>
        /// <param name="assemblyInfo">An <see cref="ASSEMBLY_INFO"/> structure that contains data about the assembly.</param>
        [PreserveSig]
        int QueryAssemblyInfo(int flags, [MarshalAs(UnmanagedType.LPWStr)] string assemblyName, ref ASSEMBLY_INFO assemblyInfo);
    }
}
