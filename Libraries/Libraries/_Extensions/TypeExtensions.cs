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

        private static Type nullableType = typeof(Nullable<>);

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
        /// <returns><c>true</c>, if <paramref name="type"/> is a <see cref="Nullable{T}"/> type; otherwise, <c>false</c>.</returns>
        public static bool IsNullable(this Type type)
            => (type ?? throw new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull))).IsGenericType && type.GetGenericTypeDefinition() == nullableType;

        /// <summary>
        /// Determines whether the specified <paramref name="type"/> is an <see cref="Enum">enum</see> and <see cref="FlagsAttribute"/> is defined on it.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if <paramref name="type"/> is a flags <see cref="Enum">enum</see>; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        public static bool IsFlagsEnum(this Type type)
            => (type ?? throw new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull))).IsEnum && type.IsDefined(typeof(FlagsAttribute), false);

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets whether given type is a collection type and is capable to add/remove/clear items
        /// either by generic or non-generic way.
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <returns>True if <paramref name="type"/> is a collection type: implements <see cref="IList"/> or <see cref="ICollection{T}"/></returns>
        internal static bool IsCollection(this Type type)
        {
            if (typeof(IList).IsAssignableFrom(type) || typeof(IDictionary).IsAssignableFrom(type))
                return true;
            foreach (Type i in type.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets whether given instance of type type is a non read-only collection
        /// either by generic or non-generic way.
        /// </summary>
        /// <param name="type">The type to test</param>
        /// <param name="instance">The object instance to test</param>
        /// <returns><c>true</c> if <paramref name="type"/> is a collection type: implements <see cref="IList"/> or <see cref="ICollection{T}"/> and <c><paramref name="instance"/>.IsReadOnly</c> returns <c>false</c>.</returns>
        internal static bool IsReadWriteCollection(this Type type, object instance)
        {
            if (instance == null)
                return false;

            if (!instance.GetType().IsAssignableFrom(type))
                throw new ArgumentException(Res.Get(Res.NotAnInstanceOfType), nameof(instance));

            IList list = instance as IList;
            if (list != null)
                return !list.IsReadOnly;
            IDictionary dictionary = instance as IDictionary;
            if (dictionary != null)
                return !dictionary.IsReadOnly;

            foreach (Type i in type.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>))
                {
                    PropertyInfo pi = i.GetProperty("IsReadOnly");
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
        /// <param name="useAqn">If <c>true</c>, gets AssemblyQualifiedName, except for mscorlib types.</param>
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

        #endregion

        #endregion
    }
}