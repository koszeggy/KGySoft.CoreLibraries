#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DynamicMethodOptions.cs
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

#region Suppressions

#if NETSTANDARD2_0
#pragma warning disable CS1574 // the documentation contains members that are not available in every target
#endif

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Options for the <see cref="MemberAccessor.CreateMethodInvokerAsDynamicMethod">MemberAccessor.CreateMethodInvokerAsDynamicMethod</see> method.
    /// </summary>
    [Flags]
    internal enum DynamicMethodOptions
    {
        /// <summary>
        /// No special handling. The dynamic method will have object return value, object instance parameter (for non-constructors) and an object[] parameter for method parameters.
        /// Possible ref/out parameters are handled automatically, the results are assigned back to the object array.
        /// </summary>
        None = 0,

        /// <summary>
        /// Does not emit the object[] parameter but the exact number of object parameters (0..4) for method arguments. Also for simple property getters or indexers.
        /// Possible ref parameters are handled only as input parameters. Can be used together with <see cref="StronglyTyped"/>.
        /// </summary>
        ExactParameters = 1,

        /// <summary>
        /// Must be used with <see cref="ExactParameters"/>. It uses strongly typed parameters and return type.
        /// For value types the instance parameter is passed by reference.
        /// </summary>
        StronglyTyped = 1 << 1,

        /// <summary>
        /// By default, generates an object value parameter and also an object[] arguments parameter in case of indexers.
        /// If <see cref="ExactParameters"/> is set, then there will be one value parameter first and then the index (if any), which is a reversed order compared to indexer setter methods.
        /// If <see cref="StronglyTyped"/> is also set, then value and index (if any) will be strongly typed.
        /// </summary>
        TreatAsPropertySetter = 1 << 2,

        /// <summary>
        /// Treats a ConstructorInfo as a regular method, ie. instead of returning a new object adds the instance parameter and executes the constructor for it.
        /// </summary>
        TreatCtorAsMethod = 1 << 3,

        /// <summary>
        /// Generates object-returning methods with null return value for void methods. Cannot be used with <see cref="StronglyTyped"/>.
        /// </summary>
        ReturnNullForVoid = 1 << 4,
    }
}
