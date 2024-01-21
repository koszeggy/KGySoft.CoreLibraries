#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypesKey.cs
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
using System.Linq;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// A value-compared variable-length tuple of types
    /// </summary>
    internal readonly struct TypesKey : IEquatable<TypesKey>
    {
        #region Fields

        internal static readonly TypesKey Empty = new TypesKey(Type.EmptyTypes);

        #endregion

        #region Properties

        internal Type[] Types { get; }

        #endregion

        #region Constructors

        public TypesKey(Type[] types) => Types = types;

        #endregion

        #region Methods

        public override bool Equals(object? obj) => obj is TypesKey key && Equals(key);

        public bool Equals(TypesKey other)
        {
            if (Types.Length != other.Types.Length)
                return false;
            for (int i = 0; i < Types.Length; i++)
            {
                if (!ReferenceEquals(Types[i], other.Types[i]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var result = 13;

            // ReSharper disable once ForCanBeConvertedToForeach - performance
            for (int i = 0; i < Types.Length; i++)
                result = result * 397 + Types[i].GetHashCode();

            return result;
        }

        public override string ToString() => $"({Types.Select(t => t.GetName(TypeNameKind.ShortName)).Join(", ")})";

        #endregion
    }
}
