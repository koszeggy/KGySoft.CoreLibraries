#if NETFRAMEWORK
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IAssemblyCache.cs
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

namespace KGySoft.Reflection.WinApi
{
    /// <summary>
    /// Represents the global assembly cache for use by the fusion technology.
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae")]
    internal interface IAssemblyCache
    {
#region Methods

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
        int QueryAssemblyInfo(int flags, [MarshalAs(UnmanagedType.LPWStr)]string assemblyName, ref ASSEMBLY_INFO assemblyInfo);

#endregion
    }
}
#endif