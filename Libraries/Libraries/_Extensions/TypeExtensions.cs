﻿#region Copyright

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
using System.Linq;
using System.Reflection;
using System.Text;

using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;

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
            => (type ?? throw new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull))).IsGenericType && type.GetGenericTypeDefinition() == nullableType;

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

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets whether <paramref name="type"/> is supported collection to populate by reflection.
        /// If <see langword="true"/> is returned one of the constructors are not <see langword="null"/> or <paramref name="type"/> is a value type.
        /// If default constructor is used the collection still can be read-only or fixed size.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <param name="defaultCtor">The default constructor or <see langword="null"/>.</param>
        /// <param name="collectionCtor">The constructor to be initialized by collection or <see langword="null"/>.</param>
        /// <param name="elementType">The element type. For non-generic collections it is <see cref="object"/>.</param>
        /// <param name="isDictionary"><see langword="true"/> <paramref name="type"/> is a dictionary.</param>
        /// <returns><see langword="true"/> if <paramref name="type"/> is a supported collection to populate by reflection; otherwise, <see langword="false"/>.</returns>
        internal static bool IsSupportedCollectionForReflection(this Type type, out ConstructorInfo defaultCtor, out ConstructorInfo collectionCtor, out Type elementType, out bool isDictionary)
        {
            #region Local Functions

            Type AsGenericIEnumerable(Type t)
                => t.IsGenericType && t.GetGenericTypeDefinition() == enumerableGenType
                    ? t
                    : t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == enumerableGenType);

            bool CanAcceptArrayOrList(Type t, Type element) 
                => t.IsAssignableFrom(element.MakeArrayType()) || t.IsAssignableFrom(typeof(List<>).MakeGenericType(element));

            #endregion

            defaultCtor = null;
            collectionCtor = null;
            elementType = null;
            isDictionary = false;

            // is IEnumeratble
            if (!enumerableType.IsAssignableFrom(type))
                return false;

            // determining elementType and isDictionry
            if (type.IsCollection())
            {
                Type genericEnumerableType = AsGenericIEnumerable(type);
                if (genericEnumerableType != null)
                {
                    elementType = genericEnumerableType.GetGenericArguments()[0];
                    isDictionary = elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                        && dictionaryGenType.MakeGenericType(elementType.GetGenericArguments()).IsAssignableFrom(type);
                }
                else
                {
                    isDictionary = dictionaryType.IsAssignableFrom(type);
                    elementType = isDictionary ? typeof(DictionaryEntry) : Reflector.ObjectType;
                }
            }
            // else : IEnumerable but cannot populate the collection. Maybe it has a proper constructor.

            foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                ParameterInfo[] args = ctor.GetParameters();
                if (args.Length == 0 && elementType != null)
                {
                    defaultCtor = ctor;
                    if (collectionCtor != null)
                        return true;
                }
                else if (args.Length == 1 && collectionCtor == null)
                {
                    Type paramType = args[0].ParameterType;

                    // Special case: Non-populable collection so only generic IEnumerable constructors can be accepted. Excluding string as it is also IEnumerable<char>.
                    if (elementType == null)
                    {
                        Type genericEnumerableType = AsGenericIEnumerable(paramType);
                        if (genericEnumerableType != null && paramType != Reflector.StringType)
                        {
                            Type paramElementType = genericEnumerableType.GetGenericArguments()[0];
                            if (CanAcceptArrayOrList(paramType, paramElementType))
                            {
                                elementType = paramElementType;
                                collectionCtor = ctor;
                                return true;
                            }
                        }
                        else if (paramType != Reflector.StringType && CanAcceptArrayOrList(paramType, Reflector.ObjectType))
                        {
                            elementType = Reflector.ObjectType;
                            collectionCtor = ctor;
                            return true;
                        }

                        continue;
                    }

                    if (!isDictionary && CanAcceptArrayOrList(paramType, elementType)
                        || isDictionary && paramType.IsAssignableFrom(typeof(Dictionary<,>).MakeGenericType(elementType.IsGenericType ? elementType.GenericTypeArguments : new[] { typeof(object), typeof(object) })))
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
            // type.GetInterface(collectionGenType.Name) != null - this would throw an exception if it has more implementations
            return typeof(IList).IsAssignableFrom(type) || dictionaryType.IsAssignableFrom(type)
                || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == collectionGenType);
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

            if (!instance.GetType().IsAssignableFrom(type))
                throw new ArgumentException(Res.Get(Res.NotAnInstanceOfType), nameof(instance));

            if (instance is IList list)
                return !list.IsReadOnly;
            if (instance is IDictionary dictionary)
                return !dictionary.IsReadOnly;

            foreach (Type i in type.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == collectionGenType)
                {
                    PropertyInfo pi = i.GetProperty(nameof(ICollection<_>.IsReadOnly));
                    return !(bool)PropertyAccessor.GetPropertyAccessor(pi).Get(instance);
                    //InterfaceMapping imap = type.GetInterfaceMap(i);
                    //MethodInfo getIsReadOnly = imap.TargetMethods.First(mi => mi.Name.EndsWith("get_IsReadOnly"));
                    //return !(bool)Reflector.RunMethod(instance, getIsReadOnly);
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

        #endregion

        #endregion
    }
}
