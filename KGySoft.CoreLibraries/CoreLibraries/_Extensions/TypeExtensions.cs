﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypeExtensions.cs
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
#if !NETSTANDARD2_0 && !NET9_0_OR_GREATER
using System.Reflection.Emit;
#endif
using System.Runtime.CompilerServices;
#if !NET9_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
using System.Security;
#if NETCOREAPP3_0_OR_GREATER
using System.Text;
#endif
using System.Threading;

using KGySoft.Collections;
using KGySoft.Reflection;

#endregion

#region Suppressions

#if !NETCOREAPP3_0_OR_GREATER
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="Type"/> type.
    /// </summary>
    public static class TypeExtensions
    {
        #region Nested Types

        #region SizeOfHelper struct
#if !NETSTANDARD2_0 && !NET9_0_OR_GREATER

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SizeOfHelper<T> // where T : struct // ISSUE: this prevents nullable structs
        {
            #region Fields

            public T Item1;
            public T Item2;

            #endregion
        }

#endif
        #endregion

        #endregion

        #region Fields

        private static readonly string collectionGenTypeName = Reflector.ICollectionGenType.Name;

        // When adding new types maintain the following places, too:
        // - ObjectExtensions.ToInvariantStringInternal
        //   - Test: ObjectExtensionsTest.ToInvariantStringRoundtripTest
        // - StringExtensions.Parser.knownTypes
        // - StringExtensions.Parser.TryParseKnownValueType<T>
        //   - Test: StringExtensionsTest.ParseTest
        // - SpanExtensions.Parser.knownTypes
        // - SpanExtensions.Parser.TryParseKnownValueType<T>
        //   - Test: SpanExtensionsTest.ParseTest
        // - ResXDataNode.nonCompatibleModeNativeTypes (for compatible format)
        //   - Test: ResXResourceWriterTest
        //   - Docs: ResXResourceWriter.CompatibleFormat
        // - XmlSerializerBase.GetStringValue (only special cases if needed, escapedNativelySupportedTypes)
        //   - Test: XmlSerializerTest.Tests
        //   - Docs: XmlSerializer Natively supported types
        private static readonly HashSet<Type> nativelyParsedTypes =
            new HashSet<Type>
            {
                Reflector.StringType, Reflector.CharType, Reflector.ByteType, Reflector.SByteType,
                Reflector.ShortType, Reflector.UShortType, Reflector.IntType, Reflector.UIntType, Reflector.LongType, Reflector.ULongType,
                Reflector.FloatType, Reflector.DoubleType, Reflector.DecimalType, Reflector.BoolType,
                Reflector.DateTimeType, Reflector.DateTimeOffsetType, Reflector.TimeSpanType,
                Reflector.IntPtrType, Reflector.UIntPtrType,
#if !NET35
                Reflector.BigIntegerType,
#endif
#if NETCOREAPP3_0_OR_GREATER
                Reflector.RuneType,
#endif
#if NET5_0_OR_GREATER
                Reflector.HalfType,
#endif
#if NET6_0_OR_GREATER
                Reflector.DateOnlyType,
                Reflector.TimeOnlyType,
#endif
#if NET7_0_OR_GREATER
                Reflector.Int128Type,
                Reflector.UInt128Type,
#endif
            };

        private static readonly Func<Type, ThreadSafeDictionary<Type, List<Delegate>>> conversionAddValueFactory = _ => new ThreadSafeDictionary<Type, List<Delegate>> { PreserveMergedKeys = true };

        /// <summary>
        /// The conversions used in <see cref="ObjectExtensions.Convert"/> and <see cref="StringExtensions.Parse"/> methods.
        /// Main key is the target type, the inner one is the source type. The inner value is practically a stack so always the last item is active.
        /// It is a list because any conversion can be removed by the UnregisterConversion method.
        /// It is always locked manually but race conditions are practically not expected for it, especially at that deep level.
        /// </summary>
        private static ThreadSafeDictionary<Type, ThreadSafeDictionary<Type, List<Delegate>>>? registeredConversions;

#if !NET9_0_OR_GREATER
        private static LockFreeCache<Type, int>? sizeOfCache;
#endif
        private static LockFreeCache<Type, bool>? hasReferenceCache;
        private static LockFreeCache<(Type GenTypeDef, TypesKey TypeArgs), Type>? genericTypeCache;
        private static LockFreeCache<(MethodInfo GenMethodDef, TypesKey TypeArgs), MethodInfo>? genericMethodsCache;
        private static LockFreeCache<Type, ConstructorInfo?>? defaultCtorCache;
        private static LockFreeCache<Type, bool>? isDefaultGetHashCodeCache;
        private static LockFreeCache<(Type, string), bool>? matchesNameCache;
        private static LockFreeCache<Type, (bool? IsDictionary, ConstructorInfo? DefaultCtor, ConstructorInfo? CollectionCtor, Type? ElementType)>? isSupportedCollectionForReflectionCache;

        // Using TypeConverterAttribute in key instead of TypeConverter because its Equals is by value
        private volatile static LockingDictionary<(Type Type, TypeConverterAttribute Converter), (TypeDescriptionProvider Provider, int Count)>? registeredTypeConverters;

        #endregion

        #region Properties

        private static ThreadSafeDictionary<Type, ThreadSafeDictionary<Type, List<Delegate>>> Conversions
        {
            get
            {
                if (registeredConversions == null)
                    Interlocked.CompareExchange(ref registeredConversions, new ThreadSafeDictionary<Type, ThreadSafeDictionary<Type, List<Delegate>>> { PreserveMergedKeys = true }, null);
                return registeredConversions;
            }
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Checks whether a <paramref name="value"/> can be an instance of <paramref name="type"/> when, for example,
        /// <paramref name="value"/> is passed to a method with <paramref name="type"/> parameter type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="value">The value, whose compatibility with the <paramref name="type"/> is checked.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> can be an instance of <paramref name="type"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para><paramref name="type"/> can be a <see cref="Nullable{T}"/> type.</para>
        /// <para>If <paramref name="type"/> is passed by reference, then the element type is checked.</para>
        /// <para>If either <paramref name="type"/> or <paramref name="value"/> is <see langword="enum"/>, then its underlying type is also accepted because both can be unboxed from an <see cref="object"/> without casting errors.</para>
        /// </remarks>
        public static bool CanAcceptValue(this Type type, object? value)
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);

            // checking null value: if not reference or nullable, null is wrong
            if (value == null)
                return (!type.IsValueType || type.IsNullable());

            if (type.IsInstanceOfType(value))
                return true;

            if (type.IsNullable())
                type = type.GetGenericArguments()[0];

            // if parameter is passed by reference (ref, out modifiers) the element type must be checked
            if (type.IsByRef)
                type = type.GetElementType()!;

            // getting the type of the real instance
            Type instanceType = value.GetType();

            // same types
            if (type == instanceType)
                return true;

            // ReSharper disable once PossibleNullReferenceException - false alarm due to the Nullable.GetUnderlyingType and type.GetElementType calls above
            if (type.IsAssignableFrom(instanceType))
                return true;

            // When unboxing, enums are compatible with their underlying type
            if (value is Enum && type == Enum.GetUnderlyingType(instanceType) // eg. (int)objValueContainingEnum
                || type.BaseType == Reflector.EnumType && instanceType == Enum.GetUnderlyingType(type)) // eg. (MyEnum)objValueContainingInt
                return true;

            if (type.IsPointer)
                return instanceType == Reflector.IntPtrType;

            return false;
        }

        /// <summary>
        /// Gets whether given <paramref name="type"/> is a <see cref="Nullable{T}"/> type.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns><see langword="true"/>, if <paramref name="type"/> is a <see cref="Nullable{T}"/> type; otherwise, <see langword="false"/>.</returns>
        public static bool IsNullable(this Type type)
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);
            return type.IsGenericTypeOf(Reflector.NullableType);
        }

        /// <summary>
        /// Determines whether the specified <paramref name="type"/> is an <see cref="Enum">enum</see> and <see cref="FlagsAttribute"/> is defined on it.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> is a flags <see cref="Enum">enum</see>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static bool IsFlagsEnum(this Type type)
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);
            return type.IsEnum && type.IsDefined(typeof(FlagsAttribute), false);
        }

        /// <summary>
        /// Gets whether the specified <paramref name="type"/> is a delegate.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the specified type is a delegate; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static bool IsDelegate(this Type type)
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);
            return type == Reflector.DelegateType || type.IsSubclassOf(Reflector.DelegateType);
        }

        /// <summary>
        /// Gets whether the given <paramref name="type"/> is a generic type of the specified <paramref name="genericTypeDefinition"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="genericTypeDefinition">The generic type definition.</param>
        /// <returns><see langword="true"/> if the given <paramref name="type"/> is a generic type of the specified <paramref name="genericTypeDefinition"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericTypeDefinition"/> is <see langword="null"/>.</exception>
        public static bool IsGenericTypeOf(this Type type, Type genericTypeDefinition)
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);
            if (genericTypeDefinition == null!)
                Throw.ArgumentNullException(Argument.genericTypeDefinition);
            return type.IsConstructedGenericType() && type.GetGenericTypeDefinition() == genericTypeDefinition;
        }

        /// <summary>
        /// Gets whether the given <paramref name="type"/>, its base classes or interfaces implement the specified <paramref name="genericTypeDefinition"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="genericTypeDefinition">The generic type definition.</param>
        /// <returns><see langword="true"/> if the given <paramref name="type"/> implements the specified <paramref name="genericTypeDefinition"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericTypeDefinition"/> is <see langword="null"/>.</exception>
        public static bool IsImplementationOfGenericType(this Type type, Type genericTypeDefinition) => IsImplementationOfGenericType(type, genericTypeDefinition, out var _);

        /// <summary>
        /// Gets whether the given <paramref name="type"/>, its base classes or interfaces implement the specified <paramref name="genericTypeDefinition"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="genericTypeDefinition">The generic type definition.</param>
        /// <param name="genericType">When this method returns <see langword="true"/>, then this parameter contains the found implementation of the specified <paramref name="genericTypeDefinition"/>.</param>
        /// <returns><see langword="true"/> if the given <paramref name="type"/> implements the specified <paramref name="genericTypeDefinition"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericTypeDefinition"/> is <see langword="null"/>.</exception>
        public static bool IsImplementationOfGenericType(this Type type, Type genericTypeDefinition, [MaybeNullWhen(false)]out Type genericType)
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);
            if (genericTypeDefinition == null!)
                Throw.ArgumentNullException(Argument.genericTypeDefinition);

            genericType = null;
            if (!genericTypeDefinition.IsGenericTypeDefinition)
                return false;

            string rootName = genericTypeDefinition.Name;
            if (genericTypeDefinition.IsInterface)
            {
                foreach (Type i in type.GetInterfaces())
                {
                    if (i.Name == rootName && i.IsGenericTypeOf(genericTypeDefinition))
                    {
                        genericType = i;
                        return true;
                    }
                }

                return false;
            }

            for (Type? t = type; t is not null; t = t.BaseType)
            {
                if (t.Name == rootName && t.IsGenericTypeOf(genericTypeDefinition))
                {
                    genericType = t;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Registers a type converter for a type.
        /// </summary>
        /// <typeparam name="TConverter">The <see cref="TypeConverter"/> to be registered.</typeparam>
        /// <param name="type">The <see cref="Type"/> to be associated with the new <typeparamref name="TConverter"/>.</param>
        /// <remarks>
        /// <para>After calling this method the <see cref="TypeDescriptor.GetConverter(Type)">TypeDescriptor.GetConverter</see>
        /// method will return the converter defined in <typeparamref name="TConverter"/>.</para>
        /// <para>To remove the registered converter and restore the previous one call
        /// the <see cref="UnregisterTypeConverter{TConverter}">UnregisterTypeConverter</see> method.</para>
        /// <note>If you want to permanently set a custom converter for a type, then it is recommended to register it at the start of your application
        /// or service. It is also possible to register a converter temporarily, and then restore the previous converter by calling
        /// the <see cref="UnregisterTypeConverter{TConverter}">UnregisterTypeConverter</see> method but then you must make sure there is no
        /// running concurrent operation on a different thread that expects to use an other converter for the same type.</note>
        /// </remarks>
        [SecuritySafeCritical]
        public static void RegisterTypeConverter<TConverter>(this Type type) where TConverter : TypeConverter
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);
            var attr = new TypeConverterAttribute(typeof(TConverter));

            if (registeredTypeConverters == null)
                Interlocked.CompareExchange(ref registeredTypeConverters, new(), null);

            try
            {
                registeredTypeConverters.Lock();
                var key = (type, attr);
                if (registeredTypeConverters.TryGetValue(key, out var value))
                {
                    registeredTypeConverters[key] = (value.Provider, value.Count + 1);
                    return;
                }

                registeredTypeConverters[key] = (TypeDescriptor.AddAttributes(type, attr), 1);
                TypeDescriptor.Refresh(type);
            }
            finally
            {
                registeredTypeConverters.Unlock();
            }
        }

        /// <summary>
        /// Unregisters a type converter for a type that was registered by the <see cref="RegisterTypeConverter{TConverter}">RegisterTypeConverter</see> method.
        /// </summary>
        /// <typeparam name="TConverter">The <see cref="TypeConverter"/> to be unregistered.</typeparam>
        /// <param name="type">The <see cref="Type"/> to be associated with the <typeparamref name="TConverter"/> to unregister.</param>
        /// <returns><see langword="true"/> if the type converter was successfully unregistered; <see langword="false"/> if <typeparamref name="TConverter"/>
        /// was not registered by the <see cref="RegisterTypeConverter{TConverter}">RegisterTypeConverter</see> method.</returns>
        /// <remarks>
        /// <para>After calling this method the original type converter will be restored for the specified <paramref name="type"/>.</para>
        /// <para>If <see cref="RegisterTypeConverter{TConverter}">RegisterTypeConverter</see> was called multiple times for the same <paramref name="type"/>
        /// and <typeparamref name="TConverter"/>, then this method should be also called the same number of times to restore the original converter.</para>
        /// </remarks>
        [SecuritySafeCritical]
        public static bool UnregisterTypeConverter<TConverter>(this Type type) where TConverter : TypeConverter
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);

            if (registeredTypeConverters == null)
                return false;

            var attr = new TypeConverterAttribute(typeof(TConverter));

            try
            {
                registeredTypeConverters.Lock();
                var key = (type, attr);
                if (!registeredTypeConverters.TryGetValue(key, out var value))
                    return false;

                if (value.Count > 1)
                {
                    registeredTypeConverters[key] = (value.Provider, value.Count - 1);
                    return true;
                }

                TypeDescriptor.RemoveProvider(value.Provider, type);
                registeredTypeConverters.Remove(key);
                TypeDescriptor.Refresh(type);
                return true;
            }
            finally
            {
                registeredTypeConverters.Unlock();
            }
        }

        /// <summary>
        /// Registers a <see cref="ConversionAttempt"/> from the specified <paramref name="sourceType"/> to <paramref name="targetType"/>.
        /// </summary>
        /// <param name="sourceType">The source <see cref="Type"/> for which the <paramref name="conversion"/> can be called.</param>
        /// <param name="targetType">The result <see cref="Type"/> that <paramref name="conversion"/> produces.</param>
        /// <param name="conversion">A <see cref="ConversionAttempt"/> delegate, which is able to perform the conversion.</param>
        /// <remarks>
        /// <para>After calling this method the <see cref="O:KGySoft.CoreLibraries.ObjectExtensions.Convert">Convert</see>/<see cref="O:KGySoft.CoreLibraries.ObjectExtensions.TryConvert">TryConvert</see>&#160;<see cref="object"/>
        /// extension methods and <see cref="O:KGySoft.CoreLibraries.StringExtensions.Parse">Parse</see>/<see cref="O:KGySoft.CoreLibraries.StringExtensions.TryParse">TryParse</see>&#160;<see cref="string"/> extension methods
        /// will be able to use the registered <paramref name="conversion"/> between <paramref name="sourceType"/> and <paramref name="targetType"/>.</para>
        /// <para>Calling the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see> methods for the same source and target types multiple times
        /// will override the old registered conversion with the new one.</para>
        /// <para><paramref name="sourceType"/> and <paramref name="targetType"/> can be interface, abstract or even a generic type definition.
        /// Preregistered conversions:
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// <para>The <see cref="O:KGySoft.CoreLibraries.TypeExtensions.UnregisterConversion">UnregisterConversion</see> methods can be used to remove a registered conversion.
        /// Registered conversions can be removed in any order but always the lastly set will be the active one.</para>
        /// <note>If you want to permanently set a custom conversion, then it is recommended to register it at the start of your application
        /// or service. It is also possible to register a conversion temporarily, and then restore the previous one by calling
        /// the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.UnregisterConversion">UnregisterConversion</see> methods but then you must make sure there is no
        /// running concurrent operation on a different thread that suppose to use different conversion for the same type.</note>
        /// <list type="bullet">
        /// <item><see cref="KeyValuePair{TKey,TValue}"/> to another <see cref="KeyValuePair{TKey,TValue}"/></item>
        /// <item><see cref="KeyValuePair{TKey,TValue}"/> to <see cref="DictionaryEntry"/></item>
        /// <item><see cref="DictionaryEntry"/> to <see cref="KeyValuePair{TKey,TValue}"/></item>
        /// <item><see cref="IEnumerable{T}"/> of <see cref="char">char</see> to <see cref="IEnumerable{T}"/> of <see cref="Rune"/> (.NET Core 3.0 and above)</item>
        /// <item><see cref="IEnumerable{T}"/> of <see cref="Rune"/> to <see cref="IEnumerable{T}"/> of <see cref="char">char</see> (.NET Core 3.0 and above)</item>
        /// </list>
        /// </para>
        /// </remarks>
        public static void RegisterConversion(this Type sourceType, Type targetType, ConversionAttempt conversion)
            => DoRegisterConversion(sourceType, targetType, conversion);

        /// <summary>
        /// Registers a <see cref="Conversion"/> from the specified <paramref name="sourceType"/> to <paramref name="targetType"/>.
        /// </summary>
        /// <param name="sourceType">The source <see cref="Type"/> for which the <paramref name="conversion"/> can be called.</param>
        /// <param name="targetType">The result <see cref="Type"/> that <paramref name="conversion"/> produces.</param>
        /// <param name="conversion">A <see cref="Conversion"/> delegate, which is able to perform the conversion.</param>
        /// <remarks>
        /// <para>After calling this method the <see cref="O:KGySoft.CoreLibraries.ObjectExtensions.Convert">Convert</see>/<see cref="O:KGySoft.CoreLibraries.ObjectExtensions.TryConvert">TryConvert</see>&#160;<see cref="object"/>
        /// extension methods and <see cref="O:KGySoft.CoreLibraries.StringExtensions.Parse">Parse</see>/<see cref="O:KGySoft.CoreLibraries.StringExtensions.TryParse">TryParse</see>&#160;<see cref="string"/> extension methods
        /// will be able to use the registered <paramref name="conversion"/> between <paramref name="sourceType"/> and <paramref name="targetType"/>.</para>
        /// <para>Calling the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see> methods for the same source and target types multiple times
        /// will override the old registered conversion with the new one.</para>
        /// <para><paramref name="sourceType"/> and <paramref name="targetType"/> can be interface, abstract or even a generic type definition.
        /// Preregistered conversions:
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// <para>The <see cref="O:KGySoft.CoreLibraries.TypeExtensions.UnregisterConversion">UnregisterConversion</see> methods can be used to remove a registered conversion.
        /// Registered conversions can be removed in any order but always the lastly set will be the active one.</para>
        /// <note>If you want to permanently set a custom conversion, then it is recommended to register it at the start of your application
        /// or service. It is also possible to register a conversion temporarily, and then restore the previous one by calling
        /// the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.UnregisterConversion">UnregisterConversion</see> methods but then you must make sure there is no
        /// running concurrent operation on a different thread that suppose to use different conversion for the same type.</note>
        /// <list type="bullet">
        /// <item><see cref="KeyValuePair{TKey,TValue}"/> to another <see cref="KeyValuePair{TKey,TValue}"/></item>
        /// <item><see cref="KeyValuePair{TKey,TValue}"/> to <see cref="DictionaryEntry"/></item>
        /// <item><see cref="DictionaryEntry"/> to <see cref="KeyValuePair{TKey,TValue}"/></item>
        /// <item><see cref="IEnumerable{T}"/> of <see cref="char">char</see> to <see cref="IEnumerable{T}"/> of <see cref="Rune"/> (.NET Core 3.0 and above)</item>
        /// <item><see cref="IEnumerable{T}"/> of <see cref="Rune"/> to <see cref="IEnumerable{T}"/> of <see cref="char">char</see> (.NET Core 3.0 and above)</item>
        /// </list>
        /// </para>
        /// </remarks>
        public static void RegisterConversion(this Type sourceType, Type targetType, Conversion conversion)
            => DoRegisterConversion(sourceType, targetType, conversion);

        /// <summary>
        /// Unregisters a conversion previously set by the <see cref="RegisterConversion(Type,Type,ConversionAttempt)"/> method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="RegisterConversion(Type,Type,ConversionAttempt)"/> method for details.
        /// </summary>
        /// <param name="sourceType">The source <see cref="Type"/> of the <paramref name="conversion"/> to remove.</param>
        /// <param name="targetType">The result <see cref="Type"/> of the <paramref name="conversion"/> to remove.</param>
        /// <param name="conversion">The <see cref="ConversionAttempt"/> delegate to remove.</param>
        /// <returns><see langword="true"/> if the conversion was successfully unregistered; <see langword="false"/> if <paramref name="conversion"/>
        /// was not registered by the <see cref="RegisterConversion(Type,Type,ConversionAttempt)"/> method.</returns>
        public static bool UnregisterConversion(this Type sourceType, Type targetType, ConversionAttempt conversion)
            => DoUnregisterConversion(sourceType, targetType, conversion);

        /// <summary>
        /// Unregisters a conversion previously set by the <see cref="RegisterConversion(Type,Type,Conversion)"/> method.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="RegisterConversion(Type,Type,Conversion)"/> method for details.
        /// </summary>
        /// <param name="sourceType">The source <see cref="Type"/> of the <paramref name="conversion"/> to remove.</param>
        /// <param name="targetType">The result <see cref="Type"/> of the <paramref name="conversion"/> to remove.</param>
        /// <param name="conversion">The <see cref="Conversion"/> delegate to remove.</param>
        /// <returns><see langword="true"/> if the conversion was successfully unregistered; <see langword="false"/> if <paramref name="conversion"/>
        /// was not registered by the <see cref="RegisterConversion(Type,Type,Conversion)"/> method.</returns>
        public static bool UnregisterConversion(this Type sourceType, Type targetType, Conversion conversion)
            => DoUnregisterConversion(sourceType, targetType, conversion);

        /// <summary>
        /// Gets the name of the <paramref name="type"/> by the specified <paramref name="kind"/>.
        /// </summary>
        /// <param name="type">The type whose name is to be obtained.</param>
        /// <param name="kind">The formatting kind for the name to be retrieved.</param>
        /// <returns>The name of the <paramref name="type"/> by the specified <paramref name="kind"/>.</returns>
        /// <remarks>
        /// <para>See the values of the <see cref="TypeNameKind"/>&#160;<see langword="enum"/> for the detailed differences
        /// from <see cref="Type"/> members such as <see cref="MemberInfo.Name"/>, <see cref="Type.FullName"/> and <see cref="Type.AssemblyQualifiedName"/>.</para>
        /// <para>Unlike the <see cref="Type"/> properties, the names produced by this method are never <see langword="null"/> for runtime types.</para>
        /// <para>This method always provides parseable type names by using the <see cref="TypeNameKind.AssemblyQualifiedName"/> kind.
        /// If the type contains generic arguments, then the result will be able to be parsed by
        /// the <see cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</see> method.</para>
        /// </remarks>
        /// <seealso cref="TypeNameKind"/>
        /// <seealso cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</seealso>
        public static string GetName(this Type type, TypeNameKind kind)
            => TypeResolver.GetName(type, kind, null, null);

        /// <summary>
        /// Gets the name of the <paramref name="type"/> by the specified <paramref name="kind"/> using custom callbacks
        /// for resolving the assembly and type names.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="GetName(Type,TypeNameKind)"/> overload for details.
        /// </summary>
        /// <param name="type">The type whose name is to be obtained.</param>
        /// <param name="kind">The formatting kind for the name to be retrieved. Determines the fallback names if the callbacks return <see langword="null"/>,
        /// and whether the <paramref name="assemblyNameResolver"/> will be called.</param>
        /// <param name="assemblyNameResolver">If not <see langword="null"/>, then will be called when the assembly identity of a type is requested.</param>
        /// <param name="typeNameResolver">If not <see langword="null"/>, then will be called for each ultimate element type and generic type definitions from
        /// which <paramref name="type"/> consists of.</param>
        /// <returns>The name of the <paramref name="type"/> by the specified <paramref name="kind"/> using the specified custom callbacks.</returns>
        /// <seealso cref="TypeNameKind"/>
        /// <seealso cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</seealso>
        public static string GetName(this Type type, TypeNameKind kind, Func<Type, AssemblyName?>? assemblyNameResolver, Func<Type, string?>? typeNameResolver)
            => TypeResolver.GetName(type, kind, assemblyNameResolver, typeNameResolver);

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets whether <paramref name="type"/> can be parsed by the Parse methods in the <see cref="StringExtensions"/> class.
        /// </summary>
        internal static bool CanBeParsedNatively(this Type type)
            => type.IsEnum || nativelyParsedTypes.Contains(type) || type == Reflector.RuntimeType;

        internal static Type? GetCollectionElementType(this Type type)
        {
            // Array
            if (type.IsArray)
                return type.GetElementType()!;

            // not IEnumerable
            if (!Reflector.IEnumerableType.IsAssignableFrom(type))
                return null;

            if (type.IsImplementationOfGenericType(Reflector.IEnumerableGenType, out Type? genericEnumerableType))
                return genericEnumerableType.GetGenericArguments()[0];
            return Reflector.IDictionaryType.IsAssignableFrom(type) ? Reflector.DictionaryEntryType
                : type == Reflector.BitArrayType ? Reflector.BoolType
                : type == Reflector.StringCollectionType ? Reflector.StringType
                : Reflector.ObjectType;
        }

        /// <summary>
        /// Gets whether <paramref name="type"/> is supported collection to populate by reflection.
        /// If <see langword="true"/> is returned one of the constructors are not <see langword="null"/> or <paramref name="type"/> is an array or a value type.
        /// If default constructor is used the collection still can be read-only or fixed size.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="defaultCtor">The default constructor or <see langword="null"/>. Non-null is returned only if the collection can be populated as an IList or generic Collection.</param>
        /// <param name="collectionCtor">The constructor to be initialized by collection or <see langword="null"/>. Can accept list, array or dictionary of element type.</param>
        /// <param name="elementType">The element type. For non-generic collections it is <see cref="object"/>.</param>
        /// <param name="isDictionary"><see langword="true"/>&#160;<paramref name="type"/> is a dictionary.</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> is a supported collection to populate by reflection; otherwise, <see langword="false"/>.</returns>
        internal static bool IsSupportedCollectionForReflection(this Type type, out ConstructorInfo? defaultCtor, out ConstructorInfo? collectionCtor, [MaybeNullWhen(false)]out Type elementType, out bool isDictionary)
        {
            if (isSupportedCollectionForReflectionCache == null)
            {
                Interlocked.CompareExchange(ref isSupportedCollectionForReflectionCache,
                    new LockFreeCache<Type, (bool?, ConstructorInfo?, ConstructorInfo?, Type?)>(GetIfSupportedCollectionForReflection, null, LockFreeCacheOptions.Profile128),
                    null);
            }

            var result = isSupportedCollectionForReflectionCache[type];
            defaultCtor = result.DefaultCtor;
            collectionCtor = result.CollectionCtor;
            elementType = result.ElementType;
            isDictionary = result.IsDictionary == true;
            return result.IsDictionary != null;
        }

        /// <summary>
        /// Creates an initializer collection for types for which <see cref="IsSupportedCollectionForReflection"/> returns <see langword="true"/> and returns a non-<see langword="null"/> collection initializer constructor.
        /// After the collection is populated call <see cref="EnumerableExtensions.AdjustInitializerCollection"/> before calling the constructor.
        /// </summary>
        internal static IEnumerable CreateInitializerCollection(this Type collectionElementType, bool isDictionary)
            => isDictionary
                ? (IEnumerable)(collectionElementType.IsGenericType
                    ? CreateInstanceAccessor.GetAccessor(Reflector.DictionaryGenType.GetGenericType(collectionElementType.GetGenericArguments())).CreateInstance()
                    : new Dictionary<object, object>())
                : (IEnumerable)CreateInstanceAccessor.GetAccessor(Reflector.ListGenType.GetGenericType(collectionElementType)).CreateInstance();

        /// <summary>
        /// Gets whether given type is a collection type and is capable to add/remove/clear items
        /// either by generic or non-generic way.
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns>True if <paramref name="type"/> is a collection type: implements <see cref="IList"/> or <see cref="ICollection{T}"/></returns>
        internal static bool IsCollection(this Type type)
        {
            return Reflector.IListType.IsAssignableFrom(type) || Reflector.IDictionaryType.IsAssignableFrom(type)
                || type.IsGenericTypeOf(Reflector.ICollectionGenType) || type.GetInterfaces().Any(i => i.Name == collectionGenTypeName && i.IsGenericTypeOf(Reflector.ICollectionGenType));
        }

        /// <summary>
        /// Gets whether given instance of type is a non read-only collection
        /// either by generic or non-generic way.
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <param name="instance">The object instance to test</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> is a collection type: implements <see cref="IList"/> or <see cref="ICollection{T}"/> and <c><paramref name="instance"/>.IsReadOnly</c> returns <see langword="false"/>.</returns>
        internal static bool IsReadWriteCollection(this Type type, object? instance)
        {
            if (instance == null)
                return false;

            if (!type.IsInstanceOfType(instance))
                Throw.ArgumentException(Argument.instance, Res.NotAnInstanceOfType(type));

            // not instance is IList test because type could be even object
            if (Reflector.IListType.IsAssignableFrom(type) && instance is IList list)
                return !list.IsReadOnly;
            if (Reflector.IDictionaryType.IsAssignableFrom(type))
                return !((IDictionary)instance).IsReadOnly;

            return IsGenericReadWriteCollection(type, instance);
        }

        /// <summary>
        /// Almost the same as <see cref="IsReadWriteCollection"/> but returns false for fixed size collections making sure that Add/Clear methods work.
        /// </summary>
        internal static bool IsPopulatableCollection(this Type type, object? instance)
        {
            if (instance == null)
                return false;

            if (!type.IsInstanceOfType(instance))
                Throw.ArgumentException(Argument.instance, Res.NotAnInstanceOfType(type));

            // not instance is IList test because type could be even object
            if (Reflector.IListType.IsAssignableFrom(type) && instance is IList list)
                return !list.IsReadOnly && !list.IsFixedSize;
            if (Reflector.IDictionaryType.IsAssignableFrom(type))
                return !((IDictionary)instance).IsReadOnly;

            return IsGenericReadWriteCollection(type, instance);
        }

        internal static ConstructorInfo? GetDefaultConstructor(this Type type)
        {
            if (defaultCtorCache == null)
            {
                Interlocked.CompareExchange(ref defaultCtorCache,
                    new LockFreeCache<Type, ConstructorInfo?>(t => t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null),
                        null, LockFreeCacheOptions.Profile128), null);
            }

            return defaultCtorCache[type];
        }

        internal static bool CanBeCreatedWithoutParameters(this Type type)
            => type.IsValueType || type.GetDefaultConstructor() != null;

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int SizeOf(this Type type)
        {
#if NET9_0_OR_GREATER
            return RuntimeHelpers.SizeOf(type.TypeHandle);
#else
            if (sizeOfCache == null)
                Interlocked.CompareExchange(ref sizeOfCache, new LockFreeCache<Type, int>(GetSize, null, LockFreeCacheOptions.Profile128), null);

            return sizeOfCache[type];
#endif
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static bool IsManaged(this Type type)
        {
            if (hasReferenceCache == null)
                Interlocked.CompareExchange(ref hasReferenceCache, new LockFreeCache<Type, bool>(HasReference, null, LockFreeCacheOptions.Profile128), null);
            return hasReferenceCache[type];
        }

        internal static IList<Delegate> GetConversions(this Type sourceType, Type targetType, bool? exactMatch)
        {
            // the exact match first
            ThreadSafeDictionary<Type, ThreadSafeDictionary<Type, List<Delegate>>>? conv = registeredConversions;
            if (conv == null)
                return Reflector.EmptyArray<Delegate>();

            Delegate? exactConversion = null;
            if (exactMatch != false)
            {
                if (conv.TryGetValue(targetType, out ThreadSafeDictionary<Type, List<Delegate>>? conversionsOfTarget)
                    && conversionsOfTarget.TryGetValue(sourceType, out List<Delegate>? conversionsOfSource))
                {
                    lock (conversionsOfSource)
                    {
                        if (conversionsOfSource.Count > 0)
                        {
                            exactConversion = conversionsOfSource[conversionsOfSource.Count - 1];
                            if (exactMatch == true)
                                return new[] { exactConversion };
                        }
                    }
                }
            }

            if (exactMatch == true)
                return Reflector.EmptyArray<Delegate>();

            var result = new List<Delegate>();
            if (exactConversion != null)
                result.Add(exactConversion);

            // non-exact matches: targets can match generic type, sources can match also interfaces and abstract types
            foreach (KeyValuePair<Type, ThreadSafeDictionary<Type, List<Delegate>>> conversionsForTarget in conv)
            {
                if (conversionsForTarget.Key.IsAssignableFrom(targetType) || targetType.IsImplementationOfGenericType(conversionsForTarget.Key))
                {
                    bool exactTarget = targetType == conversionsForTarget.Key;
                    foreach (KeyValuePair<Type, List<Delegate>> conversionsForSource in conversionsForTarget.Value)
                    {
                        if (exactTarget && conversionsForSource.Key == sourceType)
                            continue;
                        if (conversionsForSource.Key.IsAssignableFrom(sourceType) || sourceType.IsImplementationOfGenericType(conversionsForSource.Key))
                        {
                            lock (conversionsForSource.Value)
                            {
                                if (conversionsForSource.Value.Count > 0)
                                    result.Add(conversionsForSource.Value[conversionsForSource.Value.Count - 1]);
                            }
                        }
                    }
                }
            }

            return result;
        }

        internal static ICollection<Type> GetConversionSourceTypes(this Type targetType)
        {
            ThreadSafeDictionary<Type, ThreadSafeDictionary<Type, List<Delegate>>>? conv = registeredConversions;
            return conv == null ? Type.EmptyTypes
                : conv.TryGetValue(targetType, out var conversionsForTarget) ? conversionsForTarget.Keys
                : Type.EmptyTypes;
        }

        internal static ICollection<Type> GetNonExactConversionIntermediateTypes(this Type sourceType, Type targetType)
        {
            var result = new HashSet<Type>();

            // iterating all conversions and adding possible intermediate types
            foreach (KeyValuePair<Type, ThreadSafeDictionary<Type, List<Delegate>>> conversionsForTarget in Conversions)
            {
                // skipping if key is the exact targetType (those conversions are returned by GetConversionSourceTypes)
                if (targetType == conversionsForTarget.Key)
                    continue;

                // non-exact target is compatible with target type: adding all sources
                if (conversionsForTarget.Key.IsAssignableFrom(targetType) || targetType.IsImplementationOfGenericType(conversionsForTarget.Key))
                {
                    result.AddRange(conversionsForTarget.Value.Keys);
                    continue;
                }

                // iterating the sources of the current target type and adding the non-exact target type if there is at least one match
                // ReSharper disable once LoopCanBeConvertedToQuery - performance, sparing an enumerator and delegate allocation
                foreach (KeyValuePair<Type, List<Delegate>> conversionsForSource in conversionsForTarget.Value)
                {
                    if (conversionsForSource.Key.IsAssignableFrom(sourceType) || sourceType.IsImplementationOfGenericType(conversionsForSource.Key))
                    {
                        result.Add(conversionsForTarget.Key);
                        break;
                    }
                }
            }

            // At last, we add the string type as an ultimate fallback. We exploit here that HashSet preserves order if there was no deletion so
            // string will be tried lastly, unless it was already added by the registered conversions.
            result.Add(Reflector.StringType);
            return result;
        }

        internal static Type GetGenericType(this Type genTypeDef, params Type[] typeArgs)
        {
            Debug.Assert(!typeArgs.IsNullOrEmpty());
            if (genericTypeCache == null)
                Interlocked.CompareExchange(ref genericTypeCache, new LockFreeCache<(Type, TypesKey), Type>(CreateGenericType, null, LockFreeCacheOptions.Profile256), null);
            return genericTypeCache[(genTypeDef, new TypesKey(typeArgs))];
        }

        internal static MethodInfo GetGenericMethod(this MethodInfo genMethodDef, params Type[] typeArgs)
        {
            Debug.Assert(!typeArgs.IsNullOrEmpty());
            if (genericMethodsCache == null)
                Interlocked.CompareExchange(ref genericMethodsCache, new LockFreeCache<(MethodInfo, TypesKey), MethodInfo>(CreateGenericMethod, null, LockFreeCacheOptions.Profile128), null);
            return genericMethodsCache[(genMethodDef, new TypesKey(typeArgs))];
        }

        /// <summary>
        /// Similar to the public <see cref="IsImplementationOfGenericType(Type,Type,out Type)"/> but:
        /// - Only subclass check
        /// - type can be constructed generic or generic type definition
        /// - The returned genericType can be either constructed (if type was constructed) or genericTypeDefinition
        /// </summary>
        internal static bool IsSubclassOfGeneric(this Type type, Type genericTypeDefinition, [MaybeNullWhen(false)]out Type genericType)
        {
            Debug.Assert(genericTypeDefinition.IsGenericTypeDefinition);
            string rootName = genericTypeDefinition.Name;
            for (Type? t = type.BaseType; t is not null; t = t.BaseType)
            {
                if (t.Name == rootName && t.IsGenericType && t.GetGenericTypeDefinition() == genericTypeDefinition)
                {
                    genericType = t;
                    return true;
                }
            }

            genericType = null;
            return false;
        }

        internal static bool IsZeroBasedArray(this Type type)
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);

#if NETFRAMEWORK
            return type.IsSzArray();
#elif NETSTANDARD2_0
            return type.IsArray && type.Name.EndsWith("[]", StringComparison.Ordinal);
#else
            return type.IsSZArray;
#endif
        }

        internal static Type GetRootType(this Type type)
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);

            while (type.HasElementType)
                type = type.GetElementType()!;
            if (type.IsConstructedGenericType())
                type = type.GetGenericTypeDefinition();
            return type;
        }

        [SuppressMessage("ReSharper", "PossibleMistakenCallToGetType.2")]
        internal static bool IsRuntimeType(this Type type) => type.GetType() == Reflector.RuntimeType;

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static bool IsConstructedGenericType(this Type type) =>
#if NET35 || NET40
            type.IsGenericType && !type.IsGenericTypeDefinition;
#else
            type.IsConstructedGenericType;
#endif

        [SecuritySafeCritical]
        internal static object? GetDefaultValue(this Type type) => type.IsValueType
            // Trying to avoid executing possible existing parameterless struct constructor
            ? Reflector.TryCreateUninitializedObject(type, out object? result) ? result : Activator.CreateInstance(type)
            : null;

        internal static bool IsSignedIntegerType(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
                default:
                    return false;
            }
        }

        internal static ulong GetSizeMask(this Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.SByte:
                    return Byte.MaxValue;
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return UInt16.MaxValue;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    return UInt32.MaxValue;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return UInt64.MaxValue;
                default:
                    return Throw.InternalError<ulong>($"Unexpected enum base type code: {Type.GetTypeCode(type)}");
            }
        }

        internal static bool IsDefaultGetHashCode(this Type type)
        {
            #region Local Methods

            static bool LoadCacheItem(Type t)
            {
                if (!t.IsClass || !t.IsSealed)
                    return false;
                return t.GetMethod(nameof(GetHashCode), Type.EmptyTypes)?.DeclaringType == Reflector.ObjectType;
            }

            #endregion

            if (isDefaultGetHashCodeCache == null)
            {
                Interlocked.CompareExchange(ref isDefaultGetHashCodeCache,
                    new LockFreeCache<Type, bool>(LoadCacheItem, null, LockFreeCacheOptions.Profile128),
                    null);
            }

            return isDefaultGetHashCodeCache[type];
        }

        internal static IEnumerable<Type> GetNativelyParsedTypes() => nativelyParsedTypes;

        /// <summary>
        /// Gets whether name matches the type. Partial assembly match is not allowed because for now this is used in safeMode only.
        /// Otherwise, AssemblyResolver.IdentityMatches could be used as a fallback.
        /// Checks AssemblyQualifiedName, pure FullName for core types and considers possible forwarded identity.
        /// </summary>
        internal static bool MatchesName(this Type type, string name)
        {
            if (matchesNameCache == null)
            {
                Interlocked.CompareExchange(ref matchesNameCache,
                    new LockFreeCache<(Type, string), bool>(IsNameMatch, null, LockFreeCacheOptions.Profile128),
                    null);
            }

            return matchesNameCache[(type, name)];
        }

        #endregion

        #region Private Methods

        private static (bool? IsDictionary, ConstructorInfo? DefaultCtor, ConstructorInfo? CollectionCtor, Type? ElementType) GetIfSupportedCollectionForReflection(Type type)
        {
            // If IsDictionary is null, the result is not a supported collection, though element type and the constructors still can be retrieved.
            (bool? IsDictionary, ConstructorInfo? DefaultCtor, ConstructorInfo? CollectionCtor, Type? ElementType) result = default;

            // is IEnumerable
            if (!Reflector.IEnumerableType.IsAssignableFrom(type) || type.IsAbstract)
                return result;

            result.ElementType = type.GetCollectionElementType()!;

            // Array
            if (type.IsArray)
            {
                result.IsDictionary = false;
                return result;
            }

            result.IsDictionary = Reflector.IDictionaryType.IsAssignableFrom(type) || type.IsImplementationOfGenericType(Reflector.IDictionaryGenType);
            bool isPopulatableCollection = type.IsCollection();
            foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] args = ctor.GetParameters();

                // default constructor is ignored for non-populatable collections
                if (args.Length == 0 && isPopulatableCollection)
                {
                    result.DefaultCtor = ctor;
                    if (result.CollectionCtor is not null)
                        return result;
                }
                else if (args.Length == 1 && result.CollectionCtor is null)
                {
                    Type paramType = args[0].ParameterType;

                    // Excluding string as it is also IEnumerable<char>.
                    if (paramType == Reflector.StringType)
                        continue;

                    // collectionCtor is OK if can accept array or list of element type or dictionary of object or specified key-value element type
                    if (!result.IsDictionary.Value && (paramType.IsAssignableFrom(result.ElementType.MakeArrayType())
                            || paramType.IsAssignableFrom(Reflector.ListGenType.GetGenericType(result.ElementType)))
                        || result.IsDictionary.Value && paramType.IsAssignableFrom(Reflector.DictionaryGenType.GetGenericType(result.ElementType.IsGenericType
                            ? result.ElementType.GetGenericArguments()
                            : new[] { Reflector.ObjectType, Reflector.ObjectType })))
                    {
                        result.CollectionCtor = ctor;
                        if (result.DefaultCtor is not null)
                            return result;
                    }
                }
            }

            // We did not find both constructors. The type can be treated as a supported collection if it is populatable and has default ctor or when it has a collection ctor.
            if (!(isPopulatableCollection && (result.DefaultCtor is not null || type.IsValueType) || result.CollectionCtor is not null))
                result.IsDictionary = null;
            return result;
        }

        private static bool IsNameMatch((Type, string) key)
        {
            (Type type, string name) = key;
            string? aqn = type.AssemblyQualifiedName;

            // trivial match
            if (aqn == name)
                return true;

            // the type contains open generic parameters
            if (aqn == null)
                return false;

            // using TypeResolver just to parse the type name for possible generic types but we do the actual resolve by ourselves
            var expectedTypes = new HashSet<Type>();
            AddExpectedTypes(type, expectedTypes);
            return TypeResolver.ResolveType(name, DoResolveType, ResolveTypeOptions.None) == type;

            #region LocalMethods

            static void AddExpectedTypes(Type type, ICollection<Type> expectedTypes)
            {
                while (type.HasElementType)
                    type = type.GetElementType()!;

                if (!type.IsConstructedGenericType())
                {
                    expectedTypes.Add(type);
                    return;
                }

                expectedTypes.Add(type.GetGenericTypeDefinition());
                foreach (Type genericArgument in type.GetGenericArguments())
                    AddExpectedTypes(genericArgument, expectedTypes);
            }

            // Never returning null to prevent the fallback to take over.
            // Thrown exceptions are suppressed by TypeResolver because no ThrowError flag is used
            Type DoResolveType(AssemblyName? asmName, string typeName)
            {
                // throwing an exception for unexpected types
                Type expectedType = expectedTypes.First(t => t.FullName == typeName);

                string? actualAsmName = expectedType.Assembly.FullName;
                if (actualAsmName == null)
                    throw new InvalidOperationException();

                // Only full name is specified: accepting only for core libraries
                if (asmName == null)
                {
                    if (AssemblyResolver.IsCoreLibAssemblyName(actualAsmName) || AssemblyResolver.GetForwardedAssemblyName(expectedType).IsCoreIdentity)
                        return expectedType;
                    throw new InvalidOperationException();
                }

                if (AssemblyResolver.IdentityMatches(new AssemblyName(actualAsmName), asmName, true))
                    return expectedType;

                var forwardedName = AssemblyResolver.GetForwardedAssemblyName(expectedType);
                if (forwardedName.ForwardedAssemblyName == null
                    ? forwardedName.IsCoreIdentity && AssemblyResolver.IsCoreLibAssemblyName(asmName.FullName)
                    : AssemblyResolver.IdentityMatches(new AssemblyName(forwardedName.ForwardedAssemblyName), asmName, true))
                {
                    return expectedType;
                }

                throw new InvalidOperationException();
            }

            #endregion
        }

        private static bool IsGenericReadWriteCollection(Type type, object instance)
        {
            foreach (Type i in new[] { type }.Concat(type.GetInterfaces())) // including self type
            {
                if (i.Name != collectionGenTypeName)
                    continue;
                if (i.IsGenericTypeOf(Reflector.ICollectionGenType))
                {
                    PropertyInfo pi = i.GetProperty(nameof(ICollection<_>.IsReadOnly))!;
                    return !(bool)pi.Get(instance)!;
                }
            }

            return false;
        }

        private static void DoRegisterConversion(Type sourceType, Type targetType, Delegate conversion)
        {
            if (sourceType == null!)
                Throw.ArgumentNullException(Argument.sourceType);
            if (targetType == null!)
                Throw.ArgumentNullException(Argument.targetType);
            if (conversion == null!)
                Throw.ArgumentNullException(Argument.conversion);

            List<Delegate> conversionsForSource = Conversions
                .GetOrAdd(targetType, conversionAddValueFactory)
                .GetOrAdd(sourceType, _ => new List<Delegate>(1));

            lock (conversionsForSource)
                conversionsForSource.Add(conversion);
        }

        private static bool DoUnregisterConversion(this Type sourceType, Type targetType, Delegate conversion)
        {
            if (sourceType == null!)
                Throw.ArgumentNullException(Argument.sourceType);
            if (targetType == null!)
                Throw.ArgumentNullException(Argument.targetType);
            if (conversion == null!)
                Throw.ArgumentNullException(Argument.conversion);

            if (registeredConversions?.TryGetValue(targetType, out ThreadSafeDictionary<Type, List<Delegate>>? conversionsForTarget) != true
                || !conversionsForTarget!.TryGetValue(sourceType, out List<Delegate>? conversionsForSource))
            {
                return false;
            }

            lock (conversionsForSource)
            {
                var i = conversionsForSource.LastIndexOf(conversion);
                if (i < 0)
                    return false;

                // never removing the list itself
                conversionsForSource.RemoveAt(i);
                return true;
            }
        }


#if !NET9_0_OR_GREATER
        [SecuritySafeCritical]
        private static int GetSize(Type type)
        {
            if (!type.IsValueType)
                return IntPtr.Size;

            if (type.IsPrimitive)
                return Buffer.ByteLength(Array.CreateInstance(type, 1));

#if NETSTANDARD2_0
            return GetSizeFallback(type);
#else

#if NETFRAMEWORK
            if (EnvironmentHelper.IsPartiallyTrustedDomain)
                return GetSizeFallback(type);
#endif
            // If TypedReference layout is not recognized on current platform, then using a slow/unreliable fallback
            return !Reflector.CanUseTypedReference ? GetSizeFallback(type) : GetSizeComplex(type);
#endif
        }

#if !NETSTANDARD2_0
        [SecurityCritical]
#if NETFRAMEWORK
        [MethodImpl(MethodImplOptions.NoInlining)]
#endif
        private static int GetSizeComplex(Type type)
        {
            // Non-primitive struct: measuring the distance between two elements in a packed struct.
            // Unlike in Reflector<T> we cannot use an array here because we cannot obtain the address of the non strongly-typed items.
            Type helperType = typeof(SizeOfHelper<>).MakeGenericType(type); // not GetGenericType because GetSize result is also cached
            object instance = Activator.CreateInstance(helperType)!;

            // Pinning the created boxed object (not using GCHandle.Alloc because it is very slow and fails for non-blittable structs)
            // NOTE: would not be needed if we could access the ref byte of a field or non-generic array element so we could use Unsafe.ByteOffset
            unsafe
            {
                fixed (byte* _ = &Reflector.GetRawData(instance))
                {
                    // Now we can access the address of the fields safely. MakeTypedReference works here because primitive types are handled in the caller
                    TypedReference refItem1 = TypedReference.MakeTypedReference(instance, new[] { helperType.GetField(nameof(SizeOfHelper<_>.Item1))! });
                    TypedReference refItem2 = TypedReference.MakeTypedReference(instance, new[] { helperType.GetField(nameof(SizeOfHelper<_>.Item2))! });
                    Debug.Assert(__reftype(refItem1) == type && __reftype(refItem2) == type);

                    return (int)(Reflector.GetValueAddress(refItem2) - Reflector.GetValueAddress(refItem1));
                }
            }
        }
#endif

        [SecurityCritical]
        private static int GetSizeFallback(Type type)
        {
#if NETSTANDARD2_0 // DynamicMethod is not available. Fallback: calling the generic Reflector<T>.SizeOf by reflection
            if (!EnvironmentHelper.IsPartiallyTrustedDomain)
                return (int)typeof(Reflector<>).GetPropertyValue(type, nameof(Reflector<_>.SizeOf))!;

            // This can occur when the .NET Standard 2.0 build is used by .NET Framework in a partially trusted domain (not possible for NuGet references)
            try
            {
                // not SizeOf(Type) because that throws an exception for generics such as KeyValuePair<int, int>, whereas an instance of it works.
                return Marshal.SizeOf(Activator.CreateInstance(type));
            }
            catch (ArgumentException)
            {
                // contains a reference or whatever
                return default;
            }
#else
            // Emitting the SizeOf OpCode for the type, compiling a delegate and execute it, which is quite slow
            var dm = new DynamicMethod(nameof(GetSize), Reflector.UIntType, Type.EmptyTypes, typeof(TypeExtensions), true);
            ILGenerator gen = dm.GetILGenerator();
            gen.Emit(OpCodes.Sizeof, type);
            gen.Emit(OpCodes.Ret);
            var method = (Func<uint>)dm.CreateDelegate(typeof(Func<uint>));
            return (int)method.Invoke();
#endif
        }
#endif

        private static bool HasReference(Type type)
        {
            if (type.IsPrimitive || type.IsPointer || type.IsEnum)
                return false;
            if (!type.IsValueType)
                return true;

            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            // ReSharper disable once ForCanBeConvertedToForeach - performance
            for (var i = 0; i < fields.Length; i++)
            {
                if (HasReference(fields[i].FieldType))
                    return true;
            }

            return false;
        }

        private static Type CreateGenericType((Type GenTypeDef, TypesKey TypeArgs) key)
            => key.GenTypeDef.MakeGenericType(key.TypeArgs.Types);

        private static MethodInfo CreateGenericMethod((MethodInfo GenMethodDef, TypesKey TypeArgs) key)
            => key.GenMethodDef.MakeGenericMethod(key.TypeArgs.Types);

        #endregion

        #endregion
    }
}
