#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ReflectionWays.cs
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

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Represents the possible ways of reflection for the methods of the <see cref="Reflector"/> class.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1717:OnlyFlagsEnumsShouldHavePluralNames", Justification = "Would be a breaking change")]
    public enum ReflectionWays
    {
        /// <summary>
        /// Auto decision. In most cases it uses the <see cref="DynamicDelegate"/> way.
        /// </summary>
        Auto,

        /// <summary>
        /// Dynamic delegate way. This option uses cached <see cref="MemberAccessor"/> instances for reflection.
        /// In this case first access of a member is slower than accessing it via system reflection but further accesses are much more faster.
        /// </summary>
        DynamicDelegate,

        /// <summary>
        /// Uses the standard system reflection way.
        /// </summary>
        SystemReflection,

        /// <summary>
        /// Uses the type descriptor way. If there is no <see cref="ICustomTypeDescriptor"/> implementation for an instance,
        /// then this can be the slowest way as it internally falls back to use system reflection. Not applicable in all cases.
        /// </summary>
        TypeDescriptor
    }
}
