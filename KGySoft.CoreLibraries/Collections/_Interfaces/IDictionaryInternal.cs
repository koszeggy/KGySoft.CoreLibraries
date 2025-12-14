#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IDictionaryInternal.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2025 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Collections.Generic;

#endregion

#region Suppressions

#if NETCOREAPP3_0 // Only in .NET Core 3 the IDictionary<TKey, TValue> has the TKey : notnull constraint. In .NET 5 this has already been removed
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
#endif

#endregion

namespace KGySoft.Collections
{
    internal interface IDictionaryInternal<TKey, TValue> : IDictionary<TKey, TValue>
    {
        #region Methods

        bool TryAdd(TKey key, TValue value);

        #endregion
    }
}