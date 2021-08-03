#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DynamicMethodOptions.cs
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
        /// No special handling.
        /// </summary>
        None = 0,

        /// <summary>
        /// Generates local variables for ref/out parameters and assigns them back in the object[] parameters array
        /// </summary>
        HandleByRefParameters = 1,

        /// <summary>
        /// Generates an object value parameter and also an object[] arguments parameter in case of indexers
        /// </summary>
        TreatAsPropertySetter = 1 << 1,

        /// <summary>
        /// Does not emit the object[] parameter for method arguments (for simple property getters)
        /// </summary>
        OmitParameters = 1 << 2,

        /// <summary>
        /// Treats a ConstructorInfo as a regular method
        /// </summary>
        TreatCtorAsMethod = 1 << 3,
    }
}
