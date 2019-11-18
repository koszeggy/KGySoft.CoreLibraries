#if !NETSTANDARD2_0
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumComparerBuilder.cs
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
using System.Reflection;
using System.Reflection.Emit;

using KGySoft.Collections;
using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// A class, which can generate an <see cref="EnumComparer{TEnum}"/> and <see cref="EnumConverter{TEnum}"/> implementations.
    /// <br/>This class is a replacement of the old RecompILer logic and can be used also for .NET Core/Standard platforms.
    /// </summary>
    internal static class EnumComparerBuilder
    {
        #region Fields

        /// <summary>
        /// Key: Enum underlying type.
        /// Value: A <![CDATA[DynamicEnumComparer<TEnum>]]> generic type definition using the matching size and sign.
        /// </summary>
        private static readonly IDictionary<Type, Type> comparers = new LockingDictionary<Type, Type>();

        private static ModuleBuilder moduleBuilder;

        #endregion

        #region Properties

        private static ModuleBuilder ModuleBuilder
        {
            get
            {
                if (moduleBuilder == null)
                {
                    AssemblyName asmName = new AssemblyName("DynamicEnumComparer");
#if NET35 || NET40
                    AssemblyBuilder asm = AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
#else
                    AssemblyBuilder asm = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
#endif

                    moduleBuilder = asm.DefineDynamicModule(asmName.Name);
                }

                return moduleBuilder;
            }
        }

        #endregion

        #region Methods

        #region Internal Methods

        /// <summary>
        /// Gets an <see cref="EnumComparer{TEnum}"/> implementation.
        /// </summary>
        internal static EnumComparer<TEnum> GetComparer<TEnum>()
        {
            if (!typeof(TEnum).IsEnum)
                Throw.InvalidOperationException(Res.EnumTypeParameterInvalid);
            Type underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
            if (!comparers.TryGetValue(underlyingType, out Type comparerDefinition))
            {
                comparerDefinition = BuildGenericComparer(underlyingType);
                comparers[underlyingType] = comparerDefinition;
            }

            Type type = comparerDefinition.GetGenericType(typeof(TEnum));
            return (EnumComparer<TEnum>)Activator.CreateInstance(type);
        }

        #endregion

        #region Private Methods

        /// <summary><![CDATA[
        /// [Serializable] public sealed class DynamicEnumComparer<TEnum> : EnumComparer<TEnum> where TEnum : struct, Enum
        /// ]]></summary>
        private static Type BuildGenericComparer(Type underlyingType)
        {
            TypeBuilder builder = ModuleBuilder.DefineType($"DynamicEnumComparer{underlyingType.Name}`1",
                TypeAttributes.Public | TypeAttributes.Sealed,
                typeof(EnumComparer<>));
            builder.SetCustomAttribute(new CustomAttributeBuilder(typeof(SerializableAttribute).GetDefaultConstructor(), Reflector.EmptyObjects));
            GenericTypeParameterBuilder tEnum = builder.DefineGenericParameters("TEnum")[0];
            tEnum.SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);
            tEnum.SetBaseTypeConstraint(Reflector.EnumType);

            GenerateDynamicEnumComparerCtor(builder);
            GenerateEquals(builder, tEnum);
            GenerateGetHashCode(builder, underlyingType, tEnum);
            GenerateCompare(builder, underlyingType, tEnum);
            GenerateToEnum(builder, underlyingType, tEnum);
            GenerateToUInt64(builder, underlyingType, tEnum);
            GenerateToInt64(builder, underlyingType, tEnum);

            return builder.CreateType();
        }

        /// <summary><![CDATA[
        /// public DynamicEnumComparer() : base()
        /// ]]></summary>
        private static void GenerateDynamicEnumComparerCtor(TypeBuilder type)
        {
            MethodBuilder ctor = type.DefineMethod(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig);
            ConstructorInfo baseCtor = typeof(EnumComparer<>).GetDefaultConstructor();
            ctor.SetReturnType(Reflector.VoidType);
            ILGenerator il = ctor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, baseCtor);
            il.Emit(OpCodes.Ret);
        }

        /// <summary><![CDATA[
        /// public override bool Equals(TEnum x, TEnum y) => (x == y);
        /// ]]></summary>
        private static void GenerateEquals(TypeBuilder type, Type tEnum)
        {
            MethodBuilder methodEquals = type.DefineMethod(nameof(EnumComparer<_>.Equals), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            methodEquals.SetReturnType(Reflector.BoolType);
            methodEquals.SetParameters(tEnum, tEnum);
            methodEquals.DefineParameter(1, ParameterAttributes.None, "x");
            methodEquals.DefineParameter(2, ParameterAttributes.None, "y");
            ILGenerator il = methodEquals.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ceq);
            il.Emit(OpCodes.Ret);
        }

        private enum ByteEnum : byte { }
        private enum SByteEnum : sbyte { }
        private enum Int16Enum : short { }
        private enum UInt16Enum : ushort { }
        private enum Int32Enum : int { }
        private enum UInt32Enum : uint { }
        private enum Int64Enum : long { }
        private enum UInt64Enum : ulong { }

        private static int GetHashCode(ByteEnum obj) => (int)obj;
        private static int GetHashCode(SByteEnum obj) => (int)obj;
        private static int GetHashCode(Int16Enum obj) => (int)obj;
        private static int GetHashCode(UInt16Enum obj) => (int)obj;
        private static int GetHashCode(Int32Enum obj) => (int)obj;
        private static int GetHashCode(UInt32Enum obj) => (int)obj;
        private static int GetHashCode(Int64Enum obj) => (int)((long)obj ^ ((long)obj >> 32));
        private static int GetHashCode(UInt64Enum obj) => (int)((ulong)obj ^ ((ulong)obj >> 32));

        /// <summary><![CDATA[
        /// public override int GetHashCode(TEnum obj);
        /// ]]></summary>
        private static void GenerateGetHashCode(TypeBuilder type, Type underlyingType, Type tEnum)
        {
            MethodBuilder methodGetHashCode = type.DefineMethod(nameof(EnumComparer<_>.GetHashCode), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            methodGetHashCode.SetReturnType(Reflector.IntType);
            methodGetHashCode.SetParameters(tEnum);
            methodGetHashCode.DefineParameter(1, ParameterAttributes.None, "obj");
            ILGenerator il = methodGetHashCode.GetILGenerator();

            var typeCode = Type.GetTypeCode(underlyingType);
            switch (typeCode)
            {
                // return (int)obj:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Boolean:
                case TypeCode.Char:
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ret);
                    return;

                // return (int)((long)obj ^ ((long)obj >> 32)):
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4_S, (byte)32);
                    il.Emit(typeCode == TypeCode.Int64 ? OpCodes.Shr : OpCodes.Shr_Un);
                    il.Emit(OpCodes.Xor);
                    il.Emit(OpCodes.Conv_I4);
                    il.Emit(OpCodes.Ret);
                    return;
            }

            Throw.InternalError($"Not an enum type: {tEnum}");
        }

        /// <summary><![CDATA[
        /// public override int Compare(TEnum x, TEnum y) => ((underlyingType)x).CompareTo((underlyingType)y);
        /// ]]></summary>
        private static void GenerateCompare(TypeBuilder type, Type underlyingType, Type tEnum)
        {
            MethodBuilder methodCompare = type.DefineMethod(nameof(EnumComparer<_>.Compare), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            MethodInfo underlyingCompareTo = underlyingType.GetMethod(nameof(IComparable<_>.CompareTo), new[] { underlyingType });
            methodCompare.SetReturnType(Reflector.IntType);
            methodCompare.SetParameters(tEnum, tEnum);
            methodCompare.DefineParameter(1, ParameterAttributes.None, "x");
            methodCompare.DefineParameter(2, ParameterAttributes.None, "y");
            ILGenerator il = methodCompare.GetILGenerator();
            il.DeclareLocal(underlyingType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(GetConvOpCode(underlyingType));
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloca_S, 0);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(GetConvOpCode(underlyingType));
            il.Emit(OpCodes.Call, underlyingCompareTo);
            il.Emit(OpCodes.Ret);
        }

        /// <summary><![CDATA[
        /// protected override TEnum ToEnum(ulong value) => (TEnum)value;
        /// ]]></summary>
        private static void GenerateToEnum(TypeBuilder type, Type underlyingType, Type tEnum)
        {
            MethodBuilder methodToEnum = type.DefineMethod(nameof(EnumComparer<_>.ToEnum), MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            methodToEnum.SetReturnType(tEnum);
            methodToEnum.SetParameters(Reflector.ULongType);
            methodToEnum.DefineParameter(1, ParameterAttributes.None, "value");
            ILGenerator il = methodToEnum.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            if (underlyingType.GetSizeMask() != UInt64.MaxValue)
                il.Emit(GetConvOpCode(underlyingType));
            il.Emit(OpCodes.Ret);
        }

        /// <summary><![CDATA[
        /// protected override ulong ToUInt64(TEnum value) => (ulong)value & sizeMask;
        /// ]]></summary>
        private static void GenerateToUInt64(TypeBuilder type, Type underlyingType, Type tEnum)
        {
            MethodBuilder methodToUInt64 = type.DefineMethod(nameof(EnumComparer<_>.ToUInt64), MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            methodToUInt64.SetReturnType(Reflector.ULongType);
            methodToUInt64.SetParameters(tEnum);
            methodToUInt64.DefineParameter(1, ParameterAttributes.None, "value");
            ILGenerator il = methodToUInt64.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            ulong sizeMask = underlyingType.GetSizeMask();
            if (sizeMask != UInt64.MaxValue)
            {
                il.Emit(GetConvOpCode(underlyingType));
                if (underlyingType.IsSignedIntegerType())
                {
                    il.Emit(OpCodes.Ldc_I8, (long)sizeMask);
                    il.Emit(OpCodes.And);
                }
            }

            il.Emit(OpCodes.Ret);
        }

        /// <summary><![CDATA[
        /// protected override long ToInt64(TEnum value) => (long)value;
        /// ]]></summary>
        private static void GenerateToInt64(TypeBuilder type, Type underlyingType, Type tEnum)
        {
            MethodBuilder methodToUInt64 = type.DefineMethod(nameof(EnumComparer<_>.ToInt64), MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            methodToUInt64.SetReturnType(Reflector.LongType);
            methodToUInt64.SetParameters(tEnum);
            methodToUInt64.DefineParameter(1, ParameterAttributes.None, "value");
            ILGenerator il = methodToUInt64.GetILGenerator();
            il.Emit(OpCodes.Ldarg_1);
            if (underlyingType.GetSizeMask() != UInt64.MaxValue)
                il.Emit(GetConvOpCode(underlyingType));

            il.Emit(OpCodes.Ret);
        }

        private static OpCode GetConvOpCode(Type underlyingType)
        {
            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.Byte:
                    return OpCodes.Conv_U1;
                case TypeCode.SByte:
                    return OpCodes.Conv_I1;
                case TypeCode.Int16:
                    return OpCodes.Conv_I2;
                case TypeCode.UInt16:
                    return OpCodes.Conv_U2;
                case TypeCode.Int32:
                    return OpCodes.Conv_I4;
                case TypeCode.UInt32:
                    return OpCodes.Conv_U4;
                case TypeCode.Int64:
                    return OpCodes.Conv_I8;
                case TypeCode.UInt64:
                    return OpCodes.Conv_U8;
                default:
                    return Throw.InternalError<OpCode>($"Unexpected underlying type {underlyingType}");
            }
        }

        #endregion

        #endregion
    }
}
#endif