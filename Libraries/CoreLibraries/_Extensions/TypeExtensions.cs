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
using System.Collections.Specialized;
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
        private static IDictionary<Type, IDictionary<Type, Conversion>> conversions = new LockingDictionary<Type, IDictionary<Type, Conversion>>();

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Checks whether a <paramref name="value"/> can be an instance of <paramref name="type"/> when, for example,
        /// <paramref name="value"/> is passed to a method with <paramref name="type"/> parameter type.
        /// </summary>
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
        /// <returns><see langword="true"/> if <paramref name="type"/> is a flags <see cref="Enum">enum</see>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        public static bool IsFlagsEnum(this Type type)
            => (type ?? throw new ArgumentNullException(nameof(type), Res.ArgumentNull)).IsEnum && type.IsDefined(typeof(FlagsAttribute), false);

        /// <summary>
        /// Gets whether the specified <paramref name="type"/> is a delegate.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the specified type is a delegate; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static bool IsDelegate(this Type type)
            => Reflector.DelegateType.IsAssignableFrom(type ?? throw new ArgumentNullException(nameof(type), Res.ArgumentNull));

        /// <summary>
        /// Gets whether the given <paramref name="type"/> is a generic type of the specified <paramref name="genericTypeDefinition"/>.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="genericTypeDefinition">The generic type definition.</param>
        /// <returns><see langword="true"/> if the given <paramref name="type"/> is a generic type of the specified <paramref name="genericTypeDefinition"/>; otherwise, <see langword="false"/>.</returns>
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
        /// <returns><see langword="true"/> if the given <paramref name="type"/> implements the specified <paramref name="genericTypeDefinition"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="genericTypeDefinition"/> is <see langword="null"/>.</exception>
        public static bool IsImplementationOfGenericType(this Type type, Type genericTypeDefinition)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            if (genericTypeDefinition == null)
                throw new ArgumentNullException(nameof(genericTypeDefinition), Res.ArgumentNull);

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
        /// Registers a <see cref="Conversion"/> from the specified <paramref name="sourceType"/> to the <paramref name="targetType"/>.
        /// </summary>
        /// <param name="sourceType">The source <see cref="Type"/> for which the <paramref name="conversion"/> can be called.</param>
        /// <param name="targetType">The result <see cref="Type"/> that <paramref name="conversion"/> produces.</param>
        /// <param name="conversion">A <see cref="Conversion"/> delegate, which is able to perform the conversion.</param>
        /// <remarks>
        #error itt
        /// <para>After calling this method the <see cref="TypeDescriptor.GetConverter(System.Type)">TypeDescriptor.GetConverter</see>
        /// method will return the converter defined in <typeparamref name="TConverter"/>.</para>
        /// <note>Please note that if <see cref="TypeDescriptor.GetConverter(System.Type)">TypeDescriptor.GetConverter</see>
        /// has already been called for <paramref name="type"/> before registering the new converter, then the further calls
        /// after the registering may continue to return the original converter. So make sure you register your custom converters
        /// at the start of your application.</note></remarks>
        public static void RegisterConversion(this Type sourceType, Type targetType, Conversion conversion)
        {
            if (sourceType == null)
                throw new ArgumentNullException(nameof(sourceType), Res.ArgumentNull);
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType), Res.ArgumentNull);
            if (conversion == null)
                throw new ArgumentNullException(nameof(conversion), Res.ArgumentNull);

            if (!conversions.TryGetValue(targetType, out var conversionsOfTarget))
            {
                conversionsOfTarget = new LockingDictionary<Type, Conversion>();
                conversions[targetType] = conversionsOfTarget;
            }

            conversionsOfTarget[sourceType] = conversion;
        }

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
        /// If <see langword="true"/> is returned one of the constructors are not <see langword="null"/> or <paramref name="type"/> is an array or a value type.
        /// If default constructor is used the collection still can be read-only or fixed size.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="defaultCtor">The default constructor or <see langword="null"/>. Non-null is returned only the collection can be populated as an IList or generic Collection.</param>
        /// <param name="collectionCtor">The constructor to be initialized by collection or <see langword="null"/>.</param>
        /// <param name="elementType">The element type. For non-generic collections it is <see cref="object"/>.</param>
        /// <param name="isDictionary"><see langword="true"/> <paramref name="type"/> is a dictionary.</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> is a supported collection to populate by reflection; otherwise, <see langword="false"/>.</returns>
        internal static bool IsSupportedCollectionForReflection(this Type type, out ConstructorInfo defaultCtor, out ConstructorInfo collectionCtor, out Type elementType, out bool isDictionary)
        {
            defaultCtor = null;
            collectionCtor = null;
            elementType = null;
            isDictionary = false;

            // is IEnumerable
            if (!Reflector.IEnumerableType.IsAssignableFrom(type))
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
        /// <returns><see langword="true"/> if <paramref name="type"/> is a collection type: implements <see cref="IList"/> or <see cref="ICollection{T}"/> and <c><paramref name="instance"/>.IsReadOnly</c> returns <see langword="false"/>.</returns>
        internal static bool IsReadWriteCollection(this Type type, object instance)
        {
            if (instance == null)
                return false;

            if (!type.IsInstanceOfType(instance))
                throw new ArgumentException(Res.Get(Res.NotAnInstanceOfType, type), nameof(instance));

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

        #endregion

        #region Private Methods

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
            return checked((int)method.Invoke());
        }

        #endregion

        #endregion
    }
}
