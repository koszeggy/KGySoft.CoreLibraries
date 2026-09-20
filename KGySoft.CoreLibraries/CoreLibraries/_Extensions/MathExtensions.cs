#if !NETCOREAPP3_0_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MathExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
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

namespace KGySoft.CoreLibraries
{
    internal static class MathExtensions
    {
        extension(Math)
        {
            #region Methods

            internal static double Log2(double a) => Math.Log(a, 2d);
            
            #endregion
        }

        extension(MathF)
        {
            #region Methods

#if !(NETFRAMEWORK || NETSTANDARD2_0)
            internal static float Log2(float a) => MathF.Log(a, 2f);
#endif

            #endregion
        }
    }
}
#endif