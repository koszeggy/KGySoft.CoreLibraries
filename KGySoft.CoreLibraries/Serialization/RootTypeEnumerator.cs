#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RootTypeEnumerator.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections.Generic;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization
{
    /// <summary>
    /// An enumerator that deconstructs types to root types without an element type.
    /// </summary>
    internal struct RootTypeEnumerator
    {
        #region Fields

        private readonly Queue<Type> types;

        #endregion

        #region Properties

        public Type Current { get; private set; } = default!;

        #endregion

        #region Constructors

        internal RootTypeEnumerator(Type? expectedType, IEnumerable<Type>? expectedCustomTypes = null)
        {
            Debug.Assert(expectedType != null || expectedCustomTypes is not ICollection<Type> { Count: 0 });
            types = new Queue<Type>(expectedCustomTypes ?? Reflector.EmptyArray<Type>());
            if (expectedType is not null)
                types.Enqueue(expectedType);
        }

        #endregion

        #region Methods

        public RootTypeEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
#if NETCOREAPP || NETSTANDARD2_1_OR_GREATER
            while (types.TryDequeue(out Type? type))
#else
            while (types.Count > 0)
#endif
            {
#if !(NETCOREAPP || NETSTANDARD2_1_OR_GREATER)
                var type = types.Dequeue();
#endif

                if (type == null!)
                    Throw.ArgumentException(Argument.expectedCustomTypes, Res.ArgumentContainsNull);

                while (type!.HasElementType)
                    type = type.GetElementType();

                if (type.IsConstructedGenericType())
                {
                    foreach (Type arg in type.GetGenericArguments())
                        types.Enqueue(arg);
                    type = type.GetGenericTypeDefinition();
                }

                Current = type;
                return true;
            }

            return false;
        }

        #endregion
    }
}
