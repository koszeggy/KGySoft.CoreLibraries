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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
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

        private static readonly IThreadSafeCacheAccessor<Type, int> sizeOfCache = new Cache<Type, int>(GetSize).GetThreadSafeAccessor();
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
        public static bool CanAcceptValue(this Type type, object value)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            if (type == Reflector.ObjectType)
                return true;

            // checking null value: if not reference or nullable, null is wrong
            if (value == null)
                return (!type.IsValueType || type.IsNullable());

            if (type.IsNullable())
                type = Nullable.GetUnderlyingType(type);

            // getting the type of the real instance
            Type instanceType = value.GetType();

            // same types
            if (type == instanceType)
                return true;

            // if parameter is passed by reference (ref, out modifiers) the element type must be checked
            // ReSharper disable once PossibleNullReferenceException - false alarm due to the Nullable.GetUnderlyingType call above
            if (type.IsByRef)
            {
                type = type.GetElementType();
                if (type == Reflector.ObjectType || type == instanceType)
                    return true;
            }

            // instance is a concrete enum but type is not: when boxing or unboxing, enums are compatible with their underlying type
            // immediate return is ok because object and same types are checked above, other relationship is not possible
            if (value is Enum)
                return type == Reflector.EnumType || type == Enum.GetUnderlyingType(instanceType);

            // type is an enum but instance is not: when boxing or unboxing, enums are compatible with their underlying type
            // base type is checked because when type == Enum, the AssignableFrom will tell the truth
            // immediate return is ok because object and same types are checked above, other relationship is not possible
            // ReSharper disable once PossibleNullReferenceException - false alarm due to the Nullable.GetUnderlyingType and type.GetElementType calls above
            if (type.BaseType == Reflector.EnumType)
                return instanceType == Enum.GetUnderlyingType(type);

            return type.IsAssignableFrom(instanceType);
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
            return type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition;
        }

        /// <summary>
        /// Gets whether the given <paramref name="type"/>, its base classes or interfaces implement the specified <paramref name="genericTypeDefinition"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="genericTypeDefinition">The generic type definition.</param>
        /// <returns><see langword="true"/>&#160;if the given <paramref name="type"/> implements the specified <paramref name="genericTypeDefinition"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericTypeDefinition"/> is <see langword="null"/>.</exception>
        public static bool IsImplementationOfGenericType(this Type type, Type genericTypeDefinition)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            if (genericTypeDefinition == null)
                throw new ArgumentNullException(nameof(genericTypeDefinition), Res.ArgumentNull);
            if (!genericTypeDefinition.IsGenericTypeDefinition)
                return false;

            string rootName = genericTypeDefinition.Name;
            if (genericTypeDefinition.IsInterface)
                return type.GetInterfaces().Any(i => i.Name == rootName && i.IsGenericTypeOf(genericTypeDefinition));

            for (Type t = type; type != null; type = type.BaseType)
            {
                if (t.Name == rootName && t.IsGenericTypeOf(genericTypeDefinition))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Registers a type converter for a type.
        /// </summary>
        /// <typeparam name="TConverter">The <see cref="TypeConverter"/> to be registered.</typeparam>
        /// <param name="type">The <see cref="Type"/> to be associated with the new <typeparamref name="TConverter"/>.</param>
        /// <remarks>
        /// <para>After calling this method the <see cref="TypeDescriptor.GetConverter(System.Type)">TypeDescriptor.GetConverter</see>
        /// method will return the converter defined in <typeparamref name="TConverter"/>.</para>
        /// <note>Please note that if <see cref="TypeDescriptor.GetConverter(System.Type)">TypeDescriptor.GetConverter</see>
        /// has already been called for <paramref name="type"/> before registering the new converter, then the further calls
        /// after the registering may continue to return the original converter. So make sure you register your custom converters
        /// at the start of your application.</note></remarks>
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

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets whether <paramref name="type"/> can be parsed by the Parse methods in the <see cref="StringExtensions"/> class.
        /// </summary>
        internal static bool CanBeParsedNatively(this Type type)
        {
            return type.IsEnum || nativelyParsedTypes.Contains(type) || type == Reflector.RuntimeType
#if !NET35 && !NET40
                || type == Reflector.TypeInfo
#endif
                ;
        }

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
                    if (!isDictionary && (paramType.IsAssignableFrom(elementType.MakeArrayType()) || paramType.IsAssignableFrom(Reflector.ListGenType.MakeGenericType(elementType)))
                        || isDictionary && paramType.IsAssignableFrom(Reflector.DictionaryGenType.MakeGenericType(elementType.IsGenericType ? elementType.GetGenericArguments() : new[] { Reflector.ObjectType, Reflector.ObjectType })))
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
                ? (IEnumerable)(collectionElementType.IsGenericType ? Reflector.CreateInstance(Reflector.DictionaryGenType.MakeGenericType(collectionElementType.GetGenericArguments())) : new Dictionary<object, object>())
                : (IEnumerable)Reflector.CreateInstance(Reflector.ListGenType.MakeGenericType(collectionElementType));

        /// <summary>
        /// Gets whether given type is a collection type and is capable to add/remove/clear items
        /// either by generic or non-generic way.
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns>True if <paramref name="type"/> is a collection type: implements <see cref="IList"/> or <see cref="ICollection{T}"/></returns>
        internal static bool IsCollection(this Type type)
        {
            return Reflector.IListType.IsAssignableFrom(type) || Reflector.IDictionaryType.IsAssignableFrom(type)
                || type.GetInterfaces().Any(i => i.Name == collectionGenTypeName && i.IsGenericTypeOf(Reflector.ICollectionGenType));
        }

        /// <summary>
        /// Gets whether given instance of type type is a non read-only collection
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

            foreach (Type i in type.GetInterfaces())
            {
                if (i.Name != collectionGenTypeName)
                    continue;
                if (i.IsGenericTypeOf(Reflector.ICollectionGenType))
                {
                    PropertyInfo pi = i.GetProperty(nameof(ICollection<_>.IsReadOnly));
                    return !(bool)PropertyAccessor.GetAccessor(pi).Get(instance);
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the full name of the type with or without assembly name.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="useAqn">If <see langword="true"/>, gets AssemblyQualifiedName, except for mscorlib types.</param>
        /// <remarks>
        /// Differences to FullName/AssemblyQualifiedName/ToString:
        /// - If AQN is used, assembly name is appended only for non-mscorlib types
        /// - FullName contains AQN for generic parameters
        /// - ToString is OK for constructed generics without AQN but includes also type arguments for definitions, where rather FullName should be used.
        /// </remarks>
        internal static string GetTypeName(this Type type, bool useAqn)
        {
            StringBuilder sb;
            if (type.IsArray)
            {
                string result = GetTypeName(type.GetElementType(), useAqn);
                int rank = type.GetArrayRank();
                sb = new StringBuilder("[", 2 + rank);
                if (rank == 1 && !type.GetInterfaces().Any(i => i.IsGenericType))
                    sb.Append('*');
                else if (rank > 1)
                    sb.Append(new string(',', rank - 1));
                sb.Append(']');
                return result + sb;
            }

            if (type.IsByRef)
                return GetTypeName(type.GetElementType(), useAqn) + "&";

            if (type.IsPointer)
                return GetTypeName(type.GetElementType(), useAqn) + "*";

            // non-generic type or generic type definition
            if (!(type.IsGenericType && !type.IsGenericTypeDefinition)) // same as: !type.IsConstructedGenericType from .NET4
                return useAqn && type.Assembly != Reflector.mscorlibAssembly ? type.AssemblyQualifiedName : type.FullName;

            // generic type without aqn: ToString
            if (!useAqn)
                return type.ToString();

            // generic type with aqn: appending assembly only for non-mscorlib types
            sb = new StringBuilder(type.GetGenericTypeDefinition().FullName);
            sb.Append('[');
            int len = type.GetGenericArguments().Length;
            for (int i = 0; i < len; i++)
            {
                Type genericArgument = type.GetGenericArguments()[i];
                bool isMscorlibArg = genericArgument.Assembly == Reflector.mscorlibAssembly;
                if (!isMscorlibArg)
                    sb.Append('[');
                sb.Append(GetTypeName(genericArgument, true));
                if (!isMscorlibArg)
                    sb.Append(']');

                if (i < len - 1)
                    sb.Append(", ");
            }
            sb.Append(']');
            if (type.Assembly != Reflector.mscorlibAssembly)
            {
                sb.Append(", ");
                sb.Append(type.Assembly.FullName);
            }

            return sb.ToString();
        }

        internal static bool CanBeDerived(this Type type)
            => !(type.IsValueType || type.IsClass && type.IsSealed);

        internal static ConstructorInfo GetDefaultConstructor(this Type type)
            => type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);

        internal static bool CanBeCreatedWithoutParameters(this Type type)
            => type.IsValueType || type.GetDefaultConstructor() != null;

        internal static int SizeOf(this Type type) => sizeOfCache[type];

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

        #endregion

        #region Private Methods

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

            // Emitting the SizeOf OpCode for the type
            var dm = new DynamicMethod(nameof(GetSize), Reflector.UIntType, Type.EmptyTypes, typeof(TypeExtensions), true);
            ILGenerator gen = dm.GetILGenerator();
            gen.Emit(OpCodes.Sizeof, type);
            gen.Emit(OpCodes.Ret);
            var method = (Func<uint>)dm.CreateDelegate(typeof(Func<uint>));
            return (int)method.Invoke();
        }

        #endregion

        #endregion
    }
}
