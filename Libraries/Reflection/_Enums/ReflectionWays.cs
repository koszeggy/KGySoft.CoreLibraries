#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ReflectionWays.cs
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

using System.ComponentModel;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Possible reflection ways for the methods of the <see cref="Reflector"/> class.
    /// </summary>
    public enum ReflectionWays
    {
        /// <summary>
        /// Auto decision. In most cases it means the <see cref="DynamicDelegate"/> way.
        /// </summary>
        Auto,

        /// <summary>
        /// Dynamic delegate way. In this case first access of a member is slower than accessing it via
        /// system reflection but further accesses are much more faster.
        /// </summary>
        DynamicDelegate,

        /// <summary>
        /// Uses the standard system reflection way.
        /// </summary>
        SystemReflection,

        /// <summary>
        /// Uses type descriptor way. If there is no <see cref="ICustomTypeDescriptor"/> implementation for an instance,
        /// then this can be the slowest way but this is the preferred way for <see cref="Component"/>s. Not applicable in all cases.
        /// </summary>
        TypeDescriptor
    }
}
