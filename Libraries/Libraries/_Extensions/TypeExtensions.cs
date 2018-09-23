#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypeExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2018 - All Rights Reserved
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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using KGySoft.Collections;
using KGySoft.Libraries.Resources;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Libraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="Type"/> type.
    /// </summary>
    public static class TypeExtensions
    {
        #region Fields

        private static readonly Type nullableType = typeof(Nullable<>);
        private static readonly Type enumerableType = typeof(IEnumerable);
        private static readonly Type enumerableGenType = typeof(IEnumerable<>);
        private static readonly Type dictionaryType = typeof(IDictionary);
        private static readonly Type dictionaryGenType = typeof(IDictionary<,>);
        private static readonly Type collectionGenType = typeof(ICollection<>);
        private static readonly string collectionGenTypeName = collectionGenType.Name;

        private static readonly Cache<Type, int> sizeOfCache = new Cache<Type, int>(DoGetSizeOf, 1024) { EnsureCapacity = false };

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
                throw new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull));

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
            => (type ?? throw new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull))).IsGenericTypeOf(nullableType);

        /// <summary>
        /// Determines whether the specified <paramref name="type"/> is an <see cref="Enum">enum</see> and <see cref="FlagsAttribute"/> is defined on it.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> is a flags <see cref="Enum">enum</see>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        public static bool IsFlagsEnum(this Type type)
            => (type ?? throw new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull))).IsEnum && type.IsDefined(typeof(FlagsAttribute), false);

        /// <summary>
        /// Gets whether the specified <paramref name="type"/> is a delegate.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the specified type is a delegate; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> is <see langword="null"/>.</exception>
        public static bool IsDelegate(this Type type)
            => typeof(Delegate).IsAssignableFrom(type ?? throw new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull)));

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
                throw new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull));
            if (genericTypeDefinition == null)
                throw new ArgumentNullException(nameof(genericTypeDefinition), Res.Get(Res.ArgumentNull));
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
                throw new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull));
            if (genericTypeDefinition == null)
                throw new ArgumentNullException(nameof(genericTypeDefinition), Res.Get(Res.ArgumentNull));

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

        #endregion

        #region Internal Methods

        internal static Type GetCollectionElementType(this Type type)
        {
            // Array
            if (type.IsArray)
                return type.GetElementType();

            // not IEnumeratble
            if (!enumerableType.IsAssignableFrom(type))
                return null;

            Type genericEnumerableType = type.IsGenericTypeOf(enumerableGenType) ? type : type.GetInterfaces().FirstOrDefault(i => i.IsGenericTypeOf(enumerableGenType));
            return genericEnumerableType != null
                ? genericEnumerableType.GetGenericArguments()[0]
                : (dictionaryType.IsAssignableFrom(type)
                    ? typeof(DictionaryEntry)
                    : type == typeof(BitArray)
                        ? typeof(bool)
                        : type == typeof(StringCollection)
                            ? Reflector.StringType
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
            if (!enumerableType.IsAssignableFrom(type))
                return false;

            elementType = type.GetCollectionElementType();
            isDictionary = dictionaryType.IsAssignableFrom(type) || type.IsImplementationOfGenericType(dictionaryGenType);

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
                    if (!isDictionary && (paramType.IsAssignableFrom(elementType.MakeArrayType()) || paramType.IsAssignableFrom(typeof(List<>).MakeGenericType(elementType)))
                        || isDictionary && paramType.IsAssignableFrom(typeof(Dictionary<,>).MakeGenericType(elementType.IsGenericType ? elementType.GetGenericArguments() : new[] { typeof(object), typeof(object) })))
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
            return typeof(IList).IsAssignableFrom(type) || dictionaryType.IsAssignableFrom(type)
                || type.GetInterfaces().Any(i => i.Name == collectionGenTypeName && i.IsGenericTypeOf(collectionGenType));
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
            if (typeof(IList).IsAssignableFrom(type))
                return !((IList)instance).IsReadOnly;
            if (dictionaryType.IsAssignableFrom(type))
                return !((IDictionary)instance).IsReadOnly;

            foreach (Type i in type.GetInterfaces())
            {
                if (i.Name != collectionGenTypeName)
                    continue;
                if (i.IsGenericTypeOf(collectionGenType))
                {
                    PropertyInfo pi = i.GetProperty(nameof(ICollection<_>.IsReadOnly));
                    return !(bool)PropertyAccessor.GetPropertyAccessor(pi).Get(instance);
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

        internal static int SizeOf(this Type type)
        {
            lock (sizeOfCache)
            {
                return sizeOfCache[type];
            }
        }

        #endregion

        #region

        private static int DoGetSizeOf(Type type)
        {
            var dm = new DynamicMethod(nameof(DoGetSizeOf), typeof(uint), Type.EmptyTypes, typeof(TypeExtensions), true);
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
