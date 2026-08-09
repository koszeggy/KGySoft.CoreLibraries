#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IExpandoResourceSetInternal.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

using System.Diagnostics.CodeAnalysis;

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents a class that can hold replaceable resources
    /// </summary>
    internal interface IExpandoResourceSetInternal
    {
        #region Methods

        [RequiresDynamicCode(ResXCommon.UnsafeReadRequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(ResXCommon.UnsafeReadRequiresUnreferencedCodeMessage)]
        object? GetResource(string name, bool ignoreCase, bool isString, bool asSafe, bool cloneValue);

        [RequiresDynamicCode(ResXCommon.UnsafeReadRequiresDynamicCodeMessage)]
        [RequiresUnreferencedCode(ResXCommon.UnsafeReadRequiresUnreferencedCodeMessage)]
        object? GetMeta(string name, bool ignoreCase, bool isString, bool asSafe, bool cloneValue);

        #endregion
    }
}
