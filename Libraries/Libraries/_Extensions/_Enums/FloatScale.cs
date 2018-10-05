#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomScale.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.Libraries
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
        /// </summary>
        ForceLogarithmic
    }
}
