#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypeExtensions.cs
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SizeOfHelper<T> where T : struct
        {
            #region Fields
            
            public T Item1;
            public T Item2;

            #endregion
        }

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
        // - XmlSerializerBase.GetStringValue (only special cases if needed)
        //   - Test: XmlSerializerTest.Tests
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
            };

        private static readonly Func<Type, ThreadSafeDictionary<Type, Delegate>> conversionAddValueFactory = _ => new ThreadSafeDictionary<Type, Delegate> { PreserveMergedKeys = true };

        /// <summary>
        /// The conversions used in <see cref="ObjectExtensions.Convert"/> and <see cref="StringExtensions.Parse(string,Type,bool)"/> methods.
        /// Main key is the target type, the inner one is the source type.
        /// </summary>
        private static ThreadSafeDictionary<Type, ThreadSafeDictionary<Type, Delegate>>? conversions;

        private static IThreadSafeCacheAccessor<Type, int>? sizeOfCache;
        private static IThreadSafeCacheAccessor<Type, bool>? hasReferenceCache;
        private static IThreadSafeCacheAccessor<(Type GenTypeDef, Type T1, Type? T2), Type>? genericTypeCache;
        private static IThreadSafeCacheAccessor<(MethodInfo GenMethodDef, Type T1, Type? T2), MethodInfo>? genericMethodsCache;
        private static IThreadSafeCacheAccessor<Type, ConstructorInfo?>? defaultCtorCache;
        private static IThreadSafeCacheAccessor<Type, bool>? isDefaultGetHashCodeCache;

        #endregion

        #region Properties

        private static ThreadSafeDictionary<Type, ThreadSafeDictionary<Type, Delegate>> Conversions
        {
            get
            {
                if (conversions == null)
                    Interlocked.CompareExchange(ref conversions, new ThreadSafeDictionary<Type, ThreadSafeDictionary<Type, Delegate>> { PreserveMergedKeys = true }, null);
                return conversions;
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
        /// <returns><see langword="true"/>&#160;if <paramref name="value"/> can be an instance of <paramref name="type"/>; otherwise, <see langword="false"/>.</returns>
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
            // ReSharper disable once PossibleNullReferenceException - false alarm due to the Nullable.GetUnderlyingType call above
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
        /// <returns><see langword="true"/>&#160;if <paramref name="type"/> is a flags <see cref="Enum">enum</see>; otherwise, <see langword="false"/>.</returns>
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
        /// <returns><see langword="true"/>&#160;if the specified type is a delegate; otherwise, <see langword="false"/>.</returns>
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
        /// <returns><see langword="true"/>&#160;if the given <paramref name="type"/> is a generic type of the specified <paramref name="genericTypeDefinition"/>; otherwise, <see langword="false"/>.</returns>
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
        /// <returns><see langword="true"/>&#160;if the given <paramref name="type"/> implements the specified <paramref name="genericTypeDefinition"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericTypeDefinition"/> is <see langword="null"/>.</exception>
        public static bool IsImplementationOfGenericType(this Type type, Type genericTypeDefinition) => IsImplementationOfGenericType(type, genericTypeDefinition, out var _);

        /// <summary>
        /// Gets whether the given <paramref name="type"/>, its base classes or interfaces implement the specified <paramref name="genericTypeDefinition"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="genericTypeDefinition">The generic type definition.</param>
        /// <param name="genericType">When this method returns <see langword="true"/>, then this parameter contains the found implementation of the specified <paramref name="genericTypeDefinition"/>.</param>
        /// <returns><see langword="true"/>&#160;if the given <paramref name="type"/> implements the specified <paramref name="genericTypeDefinition"/>; otherwise, <see langword="false"/>.</returns>
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

            for (Type? t = type; t != null; t = t.BaseType)
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
        /// <note>Please note that if <see cref="TypeDescriptor.GetConverter(Type)">TypeDescriptor.GetConverter</see>
        /// has already been called for <paramref name="type"/> before registering the new converter, then the further calls
        /// after the registering may continue to return the original converter. So make sure you register your custom converters
        /// at the start of your application.</note></remarks>
        [SecuritySafeCritical]
        public static void RegisterTypeConverter<TConverter>(this Type type) where TConverter : TypeConverter
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);
            TypeConverterAttribute attr = new TypeConverterAttribute(typeof(TConverter));

            if (!TypeDescriptor.GetAttributes(type).Contains(attr))
                TypeDescriptor.AddAttributes(type, attr);
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
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// <para><paramref name="sourceType"/> and <paramref name="targetType"/> can be interface, abstract or even a generic type definition.
        /// Preregistered conversions:
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
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// <para><paramref name="sourceType"/> and <paramref name="targetType"/> can be interface, abstract or even a generic type definition.
        /// Preregistered conversions:
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
        /// Gets the name of the <paramref name="type"/> by the specified <paramref name="kind"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="type">The type whose name is to be obtained.</param>
        /// <param name="kind">The formatting kind for the name to be retrieved.</param>
        /// <returns>The name of the <paramref name="type"/> by the specified <paramref name="kind"/>.</returns>
        /// <remarks>
        /// <para>See the values of the <see cref="TypeNameKind"/>&#160;<see langword="enum"/>&#160;for the detailed differences
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
        /// If <see langword="true"/>&#160;is returned one of the constructors are not <see langword="null"/>&#160;or <paramref name="type"/> is an array or a value type.
        /// If default constructor is used the collection still can be read-only or fixed size.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="defaultCtor">The default constructor or <see langword="null"/>. Non-null is returned only if the collection can be populated as an IList or generic Collection.</param>
        /// <param name="collectionCtor">The constructor to be initialized by collection or <see langword="null"/>. Can accept list, array or dictionary of element type.</param>
        /// <param name="elementType">The element type. For non-generic collections it is <see cref="object"/>.</param>
        /// <param name="isDictionary"><see langword="true"/>&#160;<paramref name="type"/> is a dictionary.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="type"/> is a supported collection to populate by reflection; otherwise, <see langword="false"/>.</returns>
        internal static bool IsSupportedCollectionForReflection(this Type type, out ConstructorInfo? defaultCtor, out ConstructorInfo? collectionCtor, [MaybeNullWhen(false)]out Type elementType, out bool isDictionary)
        {
            defaultCtor = null;
            collectionCtor = null;
            elementType = null;
            isDictionary = false;

            // is IEnumerable
            if (!Reflector.IEnumerableType.IsAssignableFrom(type) || type.IsAbstract)
                return false;

            elementType = type.GetCollectionElementType()!;
            isDictionary = Reflector.IDictionaryType.IsAssignableFrom(type) || type.IsImplementationOfGenericType(Reflector.IDictionaryGenType);

            // Array
            if (type.IsArray)
                return true;

            bool isPopulatableCollection = type.IsCollection();
            foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] args = ctor.GetParameters();

                // default constructor is ignored for non-populatable collections
                if (args.Length == 0 && isPopulatableCollection)
                {
                    defaultCtor = ctor;
                    if (collectionCtor != null)
                        return true;
                }
                else if (args.Length == 1 && collectionCtor == null)
                {
                    Type paramType = args[0].ParameterType;

                    // Excluding string as it is also IEnumerable<char>.
                    if (paramType == Reflector.StringType)
                        continue;

                    // collectionCtor is OK if can accept array or list of element type or dictionary of object or specified key-value element type
                    if (!isDictionary && (paramType.IsAssignableFrom(elementType.MakeArrayType()) || paramType.IsAssignableFrom(Reflector.ListGenType.GetGenericType(elementType)))
                        || isDictionary && paramType.IsAssignableFrom(Reflector.DictionaryGenType.GetGenericType(elementType.IsGenericType ? elementType.GetGenericArguments() : new[] { Reflector.ObjectType, Reflector.ObjectType })))
                    {
                        collectionCtor = ctor;
                        if (defaultCtor != null)
                            return true;
                    }
                }
            }

            return isPopulatableCollection && (defaultCtor != null || type.IsValueType) || collectionCtor != null;
        }

        /// <summary>
        /// Creates an initializer collection for types for which <see cref="IsSupportedCollectionForReflection"/> returns <see langword="true"/>&#160;and returns a non-<see langword="null"/>&#160;collection initializer constructor.
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
        /// <returns><see langword="true"/>&#160;if <paramref name="type"/> is a collection type: implements <see cref="IList"/> or <see cref="ICollection{T}"/> and <c><paramref name="instance"/>.IsReadOnly</c> returns <see langword="false"/>.</returns>
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
                    ThreadSafeCacheFactory.Create<Type, ConstructorInfo?>(t => t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null), LockFreeCacheOptions.Profile128),
                    null);
            }

            return defaultCtorCache[type];
        }

        internal static bool CanBeCreatedWithoutParameters(this Type type)
            => type.IsValueType || type.GetDefaultConstructor() != null;

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static int SizeOf(this Type type)
        {
            if (sizeOfCache == null)
                Interlocked.CompareExchange(ref sizeOfCache, ThreadSafeCacheFactory.Create<Type, int>(GetSize, LockFreeCacheOptions.Profile128), null);
            return sizeOfCache[type];
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static bool IsManaged(this Type type)
        {
            if (hasReferenceCache == null)
                Interlocked.CompareExchange(ref hasReferenceCache, ThreadSafeCacheFactory.Create<Type, bool>(HasReference, LockFreeCacheOptions.Profile128), null);
            return hasReferenceCache[type];
        }

        internal static IList<Delegate> GetConversions(this Type sourceType, Type targetType, bool? exactMatch)
        {
            // the exact match first
            ThreadSafeDictionary<Type, ThreadSafeDictionary<Type, Delegate>>? conv = conversions;
            if (conv == null)
                return Reflector.EmptyArray<Delegate>();

            Delegate? exactConversion = null;
            if (exactMatch != false)
            {
                if (conv.TryGetValue(targetType, out ThreadSafeDictionary<Type, Delegate>? conversionsOfTarget)
                    && conversionsOfTarget.TryGetValue(sourceType, out exactConversion)
                    && exactMatch == true)
                {
                    return new[] { exactConversion };
                }
            }

            if (exactMatch == true)
                return Reflector.EmptyArray<Delegate>();

            var result = new List<Delegate>();
            if (exactConversion != null)
                result.Add(exactConversion);

            // non-exact matches: targets can match generic type, sources can match also interfaces and abstract types
            foreach (KeyValuePair<Type, ThreadSafeDictionary<Type, Delegate>> conversionsForTarget in conv)
            {
                if (conversionsForTarget.Key.IsAssignableFrom(targetType) || targetType.IsImplementationOfGenericType(conversionsForTarget.Key))
                {
                    bool exactTarget = targetType == conversionsForTarget.Key;
                    foreach (KeyValuePair<Type, Delegate> conversionForSource in conversionsForTarget.Value)
                    {
                        if (exactTarget && conversionForSource.Key == sourceType)
                            continue;
                        if (conversionForSource.Key.IsAssignableFrom(sourceType) || sourceType.IsImplementationOfGenericType(conversionForSource.Key))
                            result.Add(conversionForSource.Value);
                    }
                }
            }

            return result;
        }

        internal static ICollection<Type> GetConversionSourceTypes(this Type targetType)
        {
            ThreadSafeDictionary<Type, ThreadSafeDictionary<Type, Delegate>>? conv = conversions;
            return conv == null ? Type.EmptyTypes
                : conv.TryGetValue(targetType, out var conversionsForTarget) ? conversionsForTarget.Keys
                : Type.EmptyTypes;
        }

        internal static ICollection<Type> GetNonExactConversionIntermediateTypes(this Type sourceType, Type targetType)
        {
            var result = new HashSet<Type>();

            // iterating all conversions and adding possible intermediate types
            foreach (KeyValuePair<Type, ThreadSafeDictionary<Type, Delegate>> conversionsForTarget in Conversions)
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
                foreach (KeyValuePair<Type, Delegate> conversionForSource in conversionsForTarget.Value)
                {
                    if (conversionForSource.Key.IsAssignableFrom(sourceType) || sourceType.IsImplementationOfGenericType(conversionForSource.Key))
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

        internal static Type GetGenericType(this Type genTypeDef, Type t1, Type? t2 = null)
        {
            if (genericTypeCache == null)
                Interlocked.CompareExchange(ref genericTypeCache, ThreadSafeCacheFactory.Create<(Type, Type, Type?), Type>(CreateGenericType, LockFreeCacheOptions.Profile256), null);
            return genericTypeCache[(genTypeDef, t1, t2)];
        }

        internal static Type GetGenericType(this Type genTypeDef, Type[] args)
        {
            if (args == null!)
                Throw.ArgumentNullException(Argument.args);
            switch (args.Length)
            {
                case 1:
                    return GetGenericType(genTypeDef, args[0]);
                case 2:
                    return GetGenericType(genTypeDef, args[0], args[1]);
                default:
                    return genTypeDef.MakeGenericType(args);
            }
        }

        internal static MethodInfo GetGenericMethod(this MethodInfo genMethodDef, Type t1, Type? t2 = null)
        {
            if (genericMethodsCache == null)
                Interlocked.CompareExchange(ref genericMethodsCache, ThreadSafeCacheFactory.Create<(MethodInfo, Type, Type?), MethodInfo>(CreateGenericMethod, LockFreeCacheOptions.Profile128), null);
            return genericMethodsCache[(genMethodDef, t1, t2)];
        }

        internal static MethodInfo GetGenericMethod(this MethodInfo genMethodDef, Type[] args)
        {
            if (args == null!)
                Throw.ArgumentNullException(Argument.args);
            switch (args.Length)
            {
                case 1:
                    return GetGenericMethod(genMethodDef, args[0]);
                case 2:
                    return GetGenericMethod(genMethodDef, args[0], args[1]);
                default:
                    return genMethodDef.MakeGenericMethod(args);
            }
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

        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
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
                    ThreadSafeCacheFactory.Create<Type, bool>(LoadCacheItem, LockFreeCacheOptions.Profile128),
                    null);
            }

            return isDefaultGetHashCodeCache[type];
        }

        #endregion

        #region Private Methods

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

            Conversions.GetOrAdd(targetType, conversionAddValueFactory)[sourceType] = conversion;
        }

        [SecuritySafeCritical]
        private static unsafe int GetSize(Type type)
        {
            if (!type.IsValueType)
                return IntPtr.Size;

            if (type.IsPrimitive)
                return Buffer.ByteLength(Array.CreateInstance(type, 1));

            // If TypedReference layout is not recognized on current platform, then using a slow/unreliable fallback
            if (!Reflector.CanUseTypedReference)
                return GetSizeFallback(type);

            // non-primitive struct: measuring the distance between two elements in a packed struct
            Type helperType = typeof(SizeOfHelper<>).MakeGenericType(type); // not GetGenericType because GetSize result is also cached
            object instance = Activator.CreateInstance(helperType)!;

            // pinning the created boxed object (not using GCHandle.Alloc because it is very slow and fails for non-blittable structs)
            TypedReference boxReference = __makeref(instance);
            while (true)
            {
                byte* unpinnedAddress = Reflector.GetReferencedDataAddress(boxReference);
                ref byte asRef = ref *unpinnedAddress;
                fixed (byte* pinnedAddress = &asRef)
                {
                    // the instance has been relocated before pinning: trying again
                    if (pinnedAddress != Reflector.GetReferencedDataAddress(boxReference))
                        continue;

                    // Now we can assess the address of the fields safely. MakeTypedReference works here because primitive types are handled above
                    TypedReference refItem1 = TypedReference.MakeTypedReference(instance, new[] { helperType.GetField(nameof(SizeOfHelper<_>.Item1))! });
                    TypedReference refItem2 = TypedReference.MakeTypedReference(instance, new[] { helperType.GetField(nameof(SizeOfHelper<_>.Item2))! });
                    Debug.Assert(__reftype(refItem1) == type && __reftype(refItem2) == type);

                    return (int)(Reflector.GetValueAddress(refItem2) - Reflector.GetValueAddress(refItem1));
                }
            }
        }

        [SecurityCritical]
        private static int GetSizeFallback(Type type)
        {
#if NETSTANDARD2_0 // DynamicMethod is not available. Fallback: using Marshal.SizeOf (which has a different result sometimes)
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
            // Emitting the SizeOf OpCode for the type
            var dm = new DynamicMethod(nameof(GetSize), Reflector.UIntType, Type.EmptyTypes, typeof(TypeExtensions), true);
            ILGenerator gen = dm.GetILGenerator();
            gen.Emit(OpCodes.Sizeof, type);
            gen.Emit(OpCodes.Ret);
            var method = (Func<uint>)dm.CreateDelegate(typeof(Func<uint>));
            return (int)method.Invoke();
#endif
        }

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

        private static Type CreateGenericType((Type GenTypeDef, Type T1, Type? T2) key)
            => key.GenTypeDef.MakeGenericType(key.T2 == null ? new[] { key.T1 } : new[] { key.T1, key.T2 });

        private static MethodInfo CreateGenericMethod((MethodInfo GenMethodDef, Type T1, Type? T2) key)
            => key.GenMethodDef.MakeGenericMethod(key.T2 == null ? new[] { key.T1 } : new[] { key.T1, key.T2 });

        #endregion

        #endregion
    }
}
