#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RangeInfo.cs
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
using System.Threading;

using KGySoft.Collections;
using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    internal readonly struct RangeInfo
    {
        #region Fields

        #region Static Fields

        private static IThreadSafeCacheAccessor<Type, RangeInfo>? cache;

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

        public RangeInfo(Type type)
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
                case TypeCode.Object:
                    if (type == Reflector.IntPtrType)
                    {
                        if (IntPtr.Size == sizeof(int))
                            goto case TypeCode.Int32;
                        goto case TypeCode.Int64;
                    }

                    if (type == Reflector.UIntPtrType)
                    {
                        if (IntPtr.Size == sizeof(uint))
                            goto case TypeCode.UInt32;
                        goto case TypeCode.UInt64;
                    }

                    goto default;
                default:
                    this = default;
                    Throw.ArgumentOutOfRangeException(Argument.type);
                    break;
            }
        }

        #endregion

        #region Methods

        internal static RangeInfo GetRangeInfo(Type type)
        {
            if (cache == null)
                Interlocked.CompareExchange(ref cache, ThreadSafeCacheFactory.Create<Type, RangeInfo>(t => new RangeInfo(t), LockFreeCacheOptions.Profile16), null);
            return cache[type];
        }

        #endregion
    }
}
