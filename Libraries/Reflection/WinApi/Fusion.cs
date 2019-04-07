#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Fusion.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Runtime.InteropServices;

#endregion

namespace KGySoft.Reflection.WinApi
{
    /// <summary>
    /// The fusion API enables a runtime host to access the properties of an application's resources in order to locate
    /// the correct versions of those resources for the application.
    /// </summary>
    internal static class Fusion
    {
        #region Methods

        /// <summary>
        /// Gets a pointer to a new <see cref="IAssemblyCache"/> instance that represents the global assembly cache.
        /// </summary>
        /// <param name="ppAsmCache">The returned <see cref="IAssemblyCache"/> pointer.</param>
        /// <param name="dwReserved">Reserved for future extensibility. dwReserved must be 0 (zero).</param>
        [DllImport("fusion.dll")]
        internal static extern int CreateAssemblyCache(out IAssemblyCache ppAsmCache, int dwReserved);

        #endregion
    }
}
