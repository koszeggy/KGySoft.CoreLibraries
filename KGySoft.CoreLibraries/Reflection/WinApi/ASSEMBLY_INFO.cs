#if NETFRAMEWORK
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ASSEMBLY_INFO.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Runtime.InteropServices;

#endregion

#region Suppressions

#pragma warning disable 649 // Field is never assigned - Windows API structure
// ReSharper disable InconsistentNaming
// ReSharper disable NotAccessedField.Global

#endregion

namespace KGySoft.Reflection.WinApi
{
    /// <summary>
    /// Contains information about an assembly that is registered in the global assembly cache.
    /// </summary>
    internal struct ASSEMBLY_INFO
    {
#region Fields

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

#endregion
    }
}
#endif