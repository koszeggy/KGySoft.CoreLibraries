#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnvironmentHelper.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;

#endregion

namespace KGySoft
{
    internal static class EnvironmentHelper
    {
        #region Fields
        
        private static bool? isMono;
        private static bool? isWindows;

#if NETFRAMEWORK || NETSTANDARD2_0
        private static bool? isPartiallyTrustedDomain;
#endif

        #endregion

        #region Properties

        internal static bool IsMono => isMono ??= Type.GetType("Mono.Runtime") != null;

#if NETFRAMEWORK || NETSTANDARD2_0
        internal static bool IsPartiallyTrustedDomain => isPartiallyTrustedDomain ??=
#if NET35
            !AppDomain.CurrentDomain.IsDefaultAppDomain();
#else
            !AppDomain.CurrentDomain.IsFullyTrusted;
#endif
#endif

        internal static bool IsWindows => isWindows ??= Environment.OSVersion.Platform == PlatformID.Win32NT;

        #endregion
    }
}
