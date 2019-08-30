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
    /// A class, which can generate an <see cref="EnumComparer{TEnum}"/> implementation.
    /// <br/>This class is a replacement of the old RecompILer logic and can be used also for .NET Core/Standard and partially trusted domains.
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
                throw new InvalidOperationException(Res.EnumTypeParameterInvalid);
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
        /// [Serializable] public class DynamicEnumComparer<TEnum> : EnumComparer<TEnum> where TEnum : struct, Enum
        /// ]]></summary>
        private static Type BuildGenericComparer(Type underlyingType)
        {
            TypeBuilder builder = ModuleBuilder.DefineType($"DynamicEnumComparer{underlyingType.Name}`1",
                TypeAttributes.Public,
                typeof(EnumComparer<>));
            builder.SetCustomAttribute(new CustomAttributeBuilder(typeof(SerializableAttribute).GetDefaultConstructor(), Reflector.EmptyObjects));
            GenericTypeParameterBuilder tEnum = builder.DefineGenericParameters("TEnum")[0];
            tEnum.SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);
            tEnum.SetBaseTypeConstraint(Reflector.EnumType);

            GenerateCtor(builder);
            GenerateEquals(builder, tEnum);
            GenerateGetHashCode(builder, underlyingType, tEnum);
            GenerateCompare(builder, underlyingType, tEnum);

            return builder.CreateType();
        }

        /// <summary><![CDATA[
        /// public DynamicEnumComparer() : base()
        /// ]]></summary>
        private static void GenerateCtor(TypeBuilder type)
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

        /// <summary><![CDATA[
        /// public override int GetHashCode(TEnum obj) => ((underlyingType)obj).GetHashCode();
        /// ]]></summary>
        private static void GenerateGetHashCode(TypeBuilder type, Type underlyingType, Type tEnum)
        {
            MethodBuilder methodGetHashCode = type.DefineMethod(nameof(EnumComparer<_>.GetHashCode), MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig);
            methodGetHashCode.SetReturnType(Reflector.IntType);
            methodGetHashCode.SetParameters(tEnum);
            methodGetHashCode.DefineParameter(1, ParameterAttributes.None, "obj");
            ILGenerator il = methodGetHashCode.GetILGenerator();
            MethodInfo underlyingGetHashCode = underlyingType.GetMethod(nameof(GetHashCode));
            il.DeclareLocal(underlyingType);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(GetConvOpCode(underlyingType));
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloca_S, 0);
            il.Emit(OpCodes.Call, underlyingGetHashCode);
            il.Emit(OpCodes.Ret);
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

        private static OpCode GetConvOpCode(Type underlyingType)
        {
            if (underlyingType == Reflector.ByteType)
                return OpCodes.Conv_U1;
            if (underlyingType == Reflector.SByteType)
                return OpCodes.Conv_I1;
            if (underlyingType == Reflector.ShortType)
                return OpCodes.Conv_I2;
            if (underlyingType == Reflector.UShortType)
                return OpCodes.Conv_U2;
            if (underlyingType == Reflector.IntType)
                return OpCodes.Conv_I4;
            if (underlyingType == Reflector.UIntType)
                return OpCodes.Conv_U4;
            if (underlyingType == Reflector.LongType)
                return OpCodes.Conv_I8;
            if (underlyingType == Reflector.ULongType)
                return OpCodes.Conv_U8;

            throw new InvalidOperationException(Res.InternalError($"Unexpected underlying type {underlyingType}"));
        }

        #endregion

        #endregion
    }
}