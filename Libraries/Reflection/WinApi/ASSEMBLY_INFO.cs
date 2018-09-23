using System.Runtime.InteropServices;

namespace KGySoft.Reflection.WinApi
{
    /// <summary>
    /// Contains information about an assembly that is registered in the global assembly cache.
    /// </summary>
    internal struct ASSEMBLY_INFO
    {
        /// <summary>
        /// The size, in bytes, of the structure. This field is reserved for future extensibility.
        /// </summary>
        public int cbAssemblyInfo;

        /// <summary>
        /// Flags that indicate installation details about the assembly. The following values are supported:
        /// The ASSEMBLYINFO_FLAG_INSTALLED value, which indicates that the assembly is installed. The current version of the .NET Framework always sets dwAssemblyFlags to this value.
        /// The ASSEMBLYINFO_FLAG_PAYLOADRESIDENT value, which indicates that the assembly is a payload resident. The current version of the .NET Framework never sets dwAssemblyFlags to this value.
        /// </summary>
        public int assemblyFlags;

        /// <summary>
        /// The total size, in kilobytes, of the files that the assembly contains.
        /// </summary>
        public long assemblySizeInKB;

        /// <summary>
        /// A pointer to a string buffer that holds the current path to the manifest file. The path must end with a null character.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string currentAssemblyPath;

        /// <summary>
        /// The number of wide characters, including the null terminator, that pszCurrentAssemblyPathBuf contains.
        /// </summary>
        public int cchBuf;
    }
}
