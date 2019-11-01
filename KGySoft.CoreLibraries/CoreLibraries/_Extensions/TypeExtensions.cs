#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypeExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
#if !(NET35 || NET40)
using System.Runtime.CompilerServices; 
#endif
#if !NETSTANDARD2_0
using System.Reflection.Emit; 
#else
using System.Runtime.InteropServices; 
#endif
using System.Security;
using System.Threading;

using KGySoft.Collections;
using KGySoft.Reflection;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="Type"/> type.
    /// </summary>
    public static class TypeExtensions
    {
        #region Fields

        private static readonly string collectionGenTypeName = Reflector.ICollectionGenType.Name;

        private static readonly HashSet<Type> nativelyParsedTypes =
            new HashSet<Type>
            {
                Reflector.StringType, Reflector.CharType, Reflector.ByteType, Reflector.SByteType,
                Reflector.ShortType, Reflector.UShortType, Reflector.IntType, Reflector.UIntType, Reflector.LongType, Reflector.ULongType,
                Reflector.FloatType, Reflector.DoubleType, Reflector.DecimalType, Reflector.BoolType,
                Reflector.DateTimeType, Reflector.DateTimeOffsetType, Reflector.TimeSpanType,
                Reflector.IntPtrType, Reflector.UIntPtrType
            };

        /// <summary>
        /// The conversions used in <see cref="ObjectExtensions.Convert"/> and <see cref="StringExtensions.Parse"/> methods.
        /// Main key is the target type, the inner one is the source type.
        /// </summary>
        private static readonly IDictionary<Type, IDictionary<Type, Delegate>> conversions = new LockingDictionary<Type, IDictionary<Type, Delegate>>();

        private static IThreadSafeCacheAccessor<Type, int> sizeOfCache;
        private static IThreadSafeCacheAccessor<(Type GenTypeDef, Type T1, Type T2), Type> genericTypeCache;
        private static IThreadSafeCacheAccessor<(MethodInfo GenMethodDef, Type T1, Type T2), MethodInfo> genericMethodsCache;
        private static IThreadSafeCacheAccessor<Type, ConstructorInfo> defaultCtorCache;

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
        public static bool CanAcceptValue(this Type type, object value)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

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
                type = type.GetElementType();

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
            => (type ?? throw new ArgumentNullException(nameof(type), Res.ArgumentNull)).IsGenericTypeOf(Reflector.NullableType);

        /// <summary>
        /// Determines whether the specified <paramref name="type"/> is an <see cref="Enum">enum</see> and <see cref="FlagsAttribute"/> is defined on it.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="type"/> is a flags <see cref="Enum">enum</see>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static bool IsFlagsEnum(this Type type)
            => (type ?? throw new ArgumentNullException(nameof(type), Res.ArgumentNull)).IsEnum && type.IsDefined(typeof(FlagsAttribute), false);

        /// <summary>
        /// Gets whether the specified <paramref name="type"/> is a delegate.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/>&#160;if the specified type is a delegate; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static bool IsDelegate(this Type type)
            => Reflector.DelegateType.IsAssignableFrom(type ?? throw new ArgumentNullException(nameof(type), Res.ArgumentNull));

        /// <summary>
        /// Gets whether the given <paramref name="type"/> is a generic type of the specified <paramref name="genericTypeDefinition"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="genericTypeDefinition">The generic type definition.</param>
        /// <returns><see langword="true"/>&#160;if the given <paramref name="type"/> is a generic type of the specified <paramref name="genericTypeDefinition"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericTypeDefinition"/> is <see langword="null"/>.</exception>
        public static bool IsGenericTypeOf(this Type type, Type genericTypeDefinition)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            if (genericTypeDefinition == null)
                throw new ArgumentNullException(nameof(genericTypeDefinition), Res.ArgumentNull);
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
        public static bool IsImplementationOfGenericType(this Type type, Type genericTypeDefinition, out Type genericType)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            if (genericTypeDefinition == null)
                throw new ArgumentNullException(nameof(genericTypeDefinition), Res.ArgumentNull);

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

            for (Type t = type; type != null; type = type.BaseType)
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
#if !NET35
        [SecuritySafeCritical]
#endif
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter",
            Justification = "Intended. Method<T>() is more simple than Method(Type)")]
        public static void RegisterTypeConverter<TConverter>(this Type type) where TConverter : TypeConverter
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
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
        /// </list>
        /// </para>
        /// </remarks>
        public static void RegisterConversion(this Type sourceType, Type targetType, Conversion conversion)
            => DoRegisterConversion(sourceType, targetType, conversion);

        /// <summary>
        /// Gets the name of the <paramref name="type"/> of the specified <paramref name="kind"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="type">The type whose name is to be obtained.</param>
        /// <param name="kind">The formatting kind for the name to be retrieved.</param>
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
        /// Gets the name of the <paramref name="type"/> of the specified <paramref name="kind"/> using custom callbacks
        /// for resolving the assembly and type names.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="GetName(Type,TypeNameKind)"/> overload for details.
        /// </summary>
        /// <param name="type">The type whose name is to be obtained.</param>
        /// <param name="kind">The formatting kind for the name to be retrieved. Determines the fallback names if the callbacks return <see langword="null"/>,
        /// and whether the <paramref name="assemblyNameResolver"/> will be called.</param>
        /// <param name="assemblyNameResolver">If not <see langword="null"/>, then will be called when the assembly identity of a type is requested.</param>
        /// <param name="typeNameResolver">If not <see langword="null"/>, then will be called for each ultimate element type and generic type definitions from
        /// which <paramref name="type"/> consists of.</param>
        /// <seealso cref="TypeNameKind"/>
        /// <seealso cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</seealso>
        public static string GetName(this Type type, TypeNameKind kind, Func<Type, AssemblyName> assemblyNameResolver, Func<Type, string> typeNameResolver)
            => TypeResolver.GetName(type, kind, assemblyNameResolver, typeNameResolver);

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets whether <paramref name="type"/> can be parsed by the Parse methods in the <see cref="StringExtensions"/> class.
        /// </summary>
        internal static bool CanBeParsedNatively(this Type type)
            => type.IsEnum || nativelyParsedTypes.Contains(type) || type == Reflector.RuntimeType;

        internal static Type GetCollectionElementType(this Type type)
        {
            // Array
            if (type.IsArray)
                return type.GetElementType();

            // not IEnumerable
            if (!Reflector.IEnumerableType.IsAssignableFrom(type))
                return null;

            Type genericEnumerableType = type.IsGenericTypeOf(Reflector.IEnumerableGenType) ? type : type.GetInterfaces().FirstOrDefault(i => i.IsGenericTypeOf(Reflector.IEnumerableGenType));
            return genericEnumerableType != null ? genericEnumerableType.GetGenericArguments()[0]
                : (Reflector.IDictionaryType.IsAssignableFrom(type) ? Reflector.DictionaryEntryType
                    : type == Reflector.BitArrayType ? Reflector.BoolType
                    : type == Reflector.StringCollectionType ? Reflector.StringType
                    : Reflector.ObjectType);
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
        internal static bool IsSupportedCollectionForReflection(this Type type, out ConstructorInfo defaultCtor, out ConstructorInfo collectionCtor, out Type elementType, out bool isDictionary)
        {
            defaultCtor = null;
            collectionCtor = null;
            elementType = null;
            isDictionary = false;

            // is IEnumerable
            if (!Reflector.IEnumerableType.IsAssignableFrom(type) || type.IsAbstract)
                return false;

            elementType = type.GetCollectionElementType();
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

            return (defaultCtor != null || type.IsValueType) || collectionCtor != null;
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
        internal static bool IsReadWriteCollection(this Type type, object instance)
        {
            if (instance == null)
                return false;

            if (!type.IsInstanceOfType(instance))
                throw new ArgumentException(Res.NotAnInstanceOfType(type), nameof(instance));

            // not instance is IList test because then type could be even object
            if (Reflector.IListType.IsAssignableFrom(type))
                return !((IList)instance).IsReadOnly;
            if (Reflector.IDictionaryType.IsAssignableFrom(type))
                return !((IDictionary)instance).IsReadOnly;

            foreach (Type i in new[] { type }.Concat(type.GetInterfaces())) // including self type
            {
                if (i.Name != collectionGenTypeName)
                    continue;
                if (i.IsGenericTypeOf(Reflector.ICollectionGenType))
                {
                    if (instance is Array) // checked just here because type can be a non-collection
                        return true;
                    PropertyInfo pi = i.GetProperty(nameof(ICollection<_>.IsReadOnly));
                    return !(bool)pi.Get(instance);
                }
            }

            return false;
        }

        internal static bool CanBeDerived(this Type type)
            => !(type.IsValueType || type.IsClass && type.IsSealed);

        internal static ConstructorInfo GetDefaultConstructor(this Type type)
        {
            if (defaultCtorCache == null)
            {
                Interlocked.CompareExchange(ref defaultCtorCache,
                    new Cache<Type, ConstructorInfo>(t => t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null)).GetThreadSafeAccessor(),
                    null);
            }

            return defaultCtorCache[type];
        }

        internal static bool CanBeCreatedWithoutParameters(this Type type)
            => type.IsValueType || type.GetDefaultConstructor() != null;

        internal static int SizeOf(this Type type)
        {
            if (sizeOfCache == null)
                Interlocked.CompareExchange(ref sizeOfCache, new Cache<Type, int>(GetSize).GetThreadSafeAccessor(), null);
            return sizeOfCache[type];
        }

        internal static IList<Delegate> GetConversions(this Type sourceType, Type targetType, bool? exactMatch)
        {
            var result = new List<Delegate>();

            // the exact match first
            if (exactMatch != false && conversions.TryGetValue(targetType, out IDictionary<Type, Delegate> conversionsOfTarget) && conversionsOfTarget.TryGetValue(sourceType, out Delegate conversion))
                result.Add(conversion);

            if (exactMatch == true)
                return result;

            // non-exact matches: targets can match generic type, sources can match also interfaces and abstract types
            foreach (KeyValuePair<Type, IDictionary<Type, Delegate>> conversionsForTarget in conversions)
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

        internal static IList<Type> GetConversionSourceTypes(this Type targetType)
        {
            var result = new List<Type>();

            // adding sources for exact target match
            if (conversions.TryGetValue(targetType, out var conversionsForTarget))
                result.AddRange(conversionsForTarget.Keys);

            // adding sources for generic target matches
            foreach (KeyValuePair<Type, IDictionary<Type, Delegate>> conversionsForGenericTarget in conversions.Where(c => c.Key.IsAssignableFrom(targetType) || targetType.IsImplementationOfGenericType(c.Key)))
                result.AddRange(conversionsForGenericTarget.Value.Keys);

            return result;
        }

        internal static Type GetGenericType(this Type genTypeDef, Type t1, Type t2 = null)
        {
            if (genericTypeCache == null)
                Interlocked.CompareExchange(ref genericTypeCache, genericTypeCache = new Cache<(Type, Type, Type), Type>(CreateGenericType, 256).GetThreadSafeAccessor(), null);
            return genericTypeCache[(genTypeDef, t1, t2)];
        }

        internal static Type GetGenericType(this Type genTypeDef, Type[] args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args), Res.ArgumentNull);
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

        internal static MethodInfo GetGenericMethod(this MethodInfo genMethodDef, Type t1, Type t2 = null)
        {
            if (genericMethodsCache == null)
                Interlocked.CompareExchange(ref genericMethodsCache, genericMethodsCache = new Cache<(MethodInfo, Type, Type), MethodInfo>(CreateGenericMethod).GetThreadSafeAccessor(), null);
            return genericMethodsCache[(genMethodDef, t1, t2)];
        }

        internal static MethodInfo GetGenericMethod(this MethodInfo genMethodDef, Type[] args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args), Res.ArgumentNull);
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
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

#if NET35 || NET40 || NET45 || NET472
            return type.IsSzArray();
#elif NETSTANDARD2_0
            return type.IsArray && type.Name.EndsWith("[]", StringComparison.Ordinal);
#else
            return type.IsSZArray;
#endif
        }

        internal static Type GetRootType(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            
            // ReSharper disable once PossibleNullReferenceException - false alarm, see condition
            while (type.HasElementType)
                type = type.GetElementType();
            if (type.IsConstructedGenericType())
                type = type.GetGenericTypeDefinition();
            return type;
        }

        internal static bool IsRuntimeType(this Type type) => type?.GetType() == Reflector.RuntimeType;

#if !(NET35 || NET40)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static bool IsConstructedGenericType(this Type type) =>
#if NET35 || NET40
            type.IsGenericType && !type.IsGenericTypeDefinition;
#else
            type.IsConstructedGenericType;
#endif

        #endregion

        #region Private Methods

        [SuppressMessage("Style", "IDE0016:Use 'throw' expression", Justification = "Throwing at the beginning may spare a lot of calculations")]
        private static void DoRegisterConversion(Type sourceType, Type targetType, Delegate conversion)
        {
            if (sourceType == null)
                throw new ArgumentNullException(nameof(sourceType), Res.ArgumentNull);
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType), Res.ArgumentNull);
            if (conversion == null)
                throw new ArgumentNullException(nameof(conversion), Res.ArgumentNull);

            if (!conversions.TryGetValue(targetType, out IDictionary<Type, Delegate> conversionsOfTarget))
            {
                conversionsOfTarget = new LockingDictionary<Type, Delegate>();
                conversions[targetType] = conversionsOfTarget;
            }

            conversionsOfTarget[sourceType] = conversion;
        }

        private static int GetSize(Type type)
        {
            if (type.IsPrimitive)
            {
                Array array = Array.CreateInstance(type, 1);
                return Buffer.ByteLength(array);
            }

            if (!type.IsValueType)
                return IntPtr.Size;

#if NETSTANDARD2_0 // DynamicMethod is not available. Fallback: using Marshal.SizeOf (which has a different result sometimes)
            try
            {
                // not SizeOf(Type) because that throws an exception for generics such as KeyValuePair<int, int>, whereas an instance of it works.
                return Marshal.SizeOf(Activator.CreateInstance(type));
            }
            catch (ArgumentException)
            {
                // contains a reference or whatever
                return 0;
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

        private static Type CreateGenericType((Type GenTypeDef, Type T1, Type T2) key)
            => key.GenTypeDef.MakeGenericType(key.T2 == null ? new[] { key.T1 } : new[] { key.T1, key.T2 });

        private static MethodInfo CreateGenericMethod((MethodInfo GenMethodDef, Type T1, Type T2) key)
            => key.GenMethodDef.MakeGenericMethod(key.T2 == null ? new[] { key.T1 } : new[] { key.T1, key.T2 });

        #endregion

        #endregion
    }
}
