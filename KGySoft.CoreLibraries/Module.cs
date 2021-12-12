#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Module.cs
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

#if !NET5_0
using System.Diagnostics.CodeAnalysis;
#endif
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft
{
    internal static class Module
    {
        #region Methods

        [ModuleInitializer]
#if !NET5_0
        [SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries",
            Justification = "See the comment, it is intended and is important to work properly.")]
#endif
        internal static void ModuleInitializer()
        {
            // Just referencing Res in order to trigger its static constructor and initialize the project resources.
            // Thus configuring LanguageSettings in a consumer project will work for resources of KGySoft.CoreLibraries even if Res was not accessed yet.
            Res.Initialize();
        }

        #endregion
    }
}