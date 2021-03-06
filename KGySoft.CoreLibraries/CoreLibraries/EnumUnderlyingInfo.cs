﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumUnderlyingInfo.cs
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

using System;
using System.Collections.Generic;
using System.Threading;

#endregion

namespace KGySoft.CoreLibraries
{
    internal readonly struct EnumUnderlyingInfo
    {
        #region Fields

        #region Static Fields

        private static IDictionary<Type, EnumUnderlyingInfo>? cache;

        #endregion

        #region Instance Fields

        internal readonly TypeCode TypeCode;
        internal readonly bool IsSigned;
        internal readonly long MinValue;
        internal readonly ulong MaxValue;
        internal readonly int BitSize;
        internal readonly ulong SizeMask;

        #endregion

        #endregion

        #region Constructors

        public EnumUnderlyingInfo(Type type)
        {
            TypeCode = Type.GetTypeCode(type);
            IsSigned = type.IsSignedIntegerType();

            switch (TypeCode)
            {
                case TypeCode.Boolean:
                    MinValue = 0L;
                    MaxValue = 1L;
                    SizeMask = 1L;
                    BitSize = 1;
                    break;
                case TypeCode.Byte:
                    MinValue = 0L;
                    MaxValue = Byte.MaxValue;
                    SizeMask = Byte.MaxValue;
                    BitSize = 8;
                    break;
                case TypeCode.SByte:
                    MinValue = SByte.MinValue;
                    MaxValue = (ulong)SByte.MaxValue;
                    SizeMask = Byte.MaxValue;
                    BitSize = 8;
                    break;
                case TypeCode.Int16:
                    MinValue = Int16.MinValue;
                    MaxValue = (ulong)Int16.MaxValue;
                    SizeMask = UInt16.MaxValue;
                    BitSize = 16;
                    break;
                case TypeCode.UInt16:
                case TypeCode.Char:
                    MinValue = 0L;
                    MaxValue = UInt16.MaxValue;
                    SizeMask = UInt16.MaxValue;
                    BitSize = 16;
                    break;
                case TypeCode.Int32:
                    MinValue = Int32.MinValue;
                    MaxValue = Int32.MaxValue;
                    SizeMask = UInt32.MaxValue;
                    BitSize = 32;
                    break;
                case TypeCode.UInt32:
                    MinValue = 0L;
                    MaxValue = UInt32.MaxValue;
                    SizeMask = UInt32.MaxValue;
                    BitSize = 32;
                    break;
                case TypeCode.Int64:
                    MinValue = Int64.MinValue;
                    MaxValue = Int64.MaxValue;
                    SizeMask = UInt64.MaxValue;
                    BitSize = 64;
                    break;
                case TypeCode.UInt64:
                    MinValue = 0L;
                    MaxValue = UInt64.MaxValue;
                    SizeMask = UInt64.MaxValue;
                    BitSize = 64;
                    break;
                default:
                    this = default;
                    Throw.ArgumentOutOfRangeException(Argument.type);
                    break;
            }
        }

        #endregion

        #region Methods

        internal static EnumUnderlyingInfo GetUnderlyingInfo(Type type)
        {
            if (cache == null)
                Interlocked.CompareExchange(ref cache, new Dictionary<Type, EnumUnderlyingInfo>(), null);
            if (cache.TryGetValue(type, out EnumUnderlyingInfo result))
                return result;
            result = new EnumUnderlyingInfo(type);
            cache[type] = result;
            return result;
        }

        #endregion
    }
}
