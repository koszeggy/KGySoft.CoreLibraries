#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FloatScale.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents the scaling strategy when generating random floating-point numbers.
    /// </summary>
    public enum FloatScale
    {
        /// <summary>
        /// The scaling will be chosen automatically based on the provided range.
        /// </summary>
        Auto,

        /// <summary>
        /// Forces to use linear scaling when generating random numbers.
        /// </summary>
        ForceLinear,

        /// <summary>
        /// Forces to use logarithmic scaling when generating random numbers.
        /// Please that generating random numbers on the logarithmic scale can be significantly slower than
        /// on the linear scale, especially when generating <see cref="decimal"/> values.
        /// </summary>
        ForceLogarithmic
    }
}
