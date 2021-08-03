#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SerializationInfoExtensions.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
#if NET35
using System.Reflection; 
#endif
using System.Runtime.Serialization;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Provides extension methods for the <see cref="SerializationInfo"/> class.
    /// </summary>
    public static class SerializationInfoExtensions
    {
        #region Methods

        #region Public Methods

        /// <summary>
        /// Gets a lazy-evaluating <see cref="IEnumerable{T}"/> wrapper for the specified <see cref="SerializationInfo"/>.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to be converted.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> instance that contains the <see cref="SerializationEntry"/> items of the provided <see cref="SerializationInfo"/>.</returns>
        /// <remarks>
        /// <para>The returned instance has a lazy enumerator. Adding and removing elements during enumerating the <see cref="SerializationInfo"/> can lead to
        /// an inconsistent state (it tolerates calling the <see cref="ReplaceValue">ReplaceValue</see> though).</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public static IEnumerable<SerializationEntry> ToEnumerable(this SerializationInfo info)
        {
            if (info == null!)
                Throw.ArgumentNullException(Argument.info);

            if (info.MemberCount == 0)
                yield break;

            foreach (SerializationEntry entry in info)
                yield return entry;
        }

        /// <summary>
        /// Tries the get value from the <see cref="SerializationInfo"/> associated with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to retrieve the value from.</param>
        /// <param name="name">The name of the value to be retrieved.</param>
        /// <param name="value">When this method returns, the value associated with the specified name, if the <paramref name="name"/> is found;
        /// otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>
        /// <returns>
        /// <see langword="true"/>, if <see cref="SerializationInfo"/> contains an element with the specified name; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryGetValue(this SerializationInfo info, string name, out object? value)
        {
            if (info == null!)
                Throw.ArgumentNullException(Argument.info);

            foreach (SerializationEntry entry in info)
            {
                if (entry.Name == name)
                {
                    value = entry.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Tries the get value from the <see cref="SerializationInfo"/> associated with the specified <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="T">The type of the element to get.</typeparam>
        /// <param name="info">The <see cref="SerializationInfo"/> to retrieve the value from.</param>
        /// <param name="name">The name of the value to be retrieved.</param>
        /// <param name="value">When this method returns, the value associated with the specified name, if the <paramref name="name"/> is found
        /// and has the type of <typeparamref name="T"/>; otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>
        /// <returns>
        /// <see langword="true"/>, if <see cref="SerializationInfo"/> contains an element with the specified name
        /// and type of <typeparamref name="T"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryGetValue<T>(this SerializationInfo info, string name, [MaybeNullWhen(false)]out T value)
        {
            if (info.TryGetValue(name, out object? result) && typeof(T).CanAcceptValue(result))
            {
                value = (T)result!;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to get a value from a <see cref="SerializationInfo"/> for the given <paramref name="name"/>.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to retrieve the value from.</param>
        /// <param name="name">The name of the value to be retrieved.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="name"/> was not found. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>The found value or <see langword="null"/> if <paramref name="name"/> was not found in the <see cref="SerializationInfo"/>.</returns>
        public static object? GetValueOrDefault(this SerializationInfo info, string name, object? defaultValue = null)
            => info.TryGetValue(name, out object? result) ? result : defaultValue;

        /// <summary>
        /// Tries to get a value from a <see cref="SerializationInfo"/> for the given <paramref name="name"/>.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to retrieve the value from.</param>
        /// <param name="name">The name of the value to be retrieved.</param>
        /// <param name="defaultValue">The default value to return if <paramref name="name"/> was not found. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>&#160;if <typeparamref name="T"/> is a reference type; otherwise, the bitwise zero value of <typeparamref name="T"/>.</param>
        /// <typeparam name="T">The type of the value with the corresponding <paramref name="name"/> to get.</typeparam>
        /// <returns>The found value or <paramref name="defaultValue"/> if <paramref name="name"/> was not found or its value cannot be cast to <typeparamref name="T"/>.</returns>
        public static T GetValueOrDefault<T>(this SerializationInfo info, string name, T defaultValue = default!)
            => info.TryGetValue(name, out T? result) ? result! : defaultValue;

        /// <summary>
        /// Gets whether an entry with the specified <paramref name="name"/> exists in the specified <see cref="SerializationInfo"/>.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to check.</param>
        /// <param name="name">The name of the value to be searched for.</param>
        /// <returns><see langword="true"/>&#160;if the <see cref="SerializationInfo"/> contains an entry with the specified <paramref name="name"/>;
        /// otherwise, <see langword="false"/>.</returns>
        public static bool ContainsName(this SerializationInfo info, string name)
        {
            if (name == null!)
                Throw.ArgumentNullException(Argument.info);

            return info.ToEnumerable().Any(e => e.Name == name);
        }

        /// <summary>
        /// Removes a value of the specified <paramref name="name"/> from the <see cref="SerializationInfo"/>.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to remove the value from.</param>
        /// <param name="name">The name of the entry to remove.</param>
        /// <returns><see langword="true"/>&#160;if an entry with the specified name existed in the <see cref="SerializationInfo"/>
        /// and has been removed; otherwise, <see langword="false"/>.</returns>
        public static bool RemoveValue(this SerializationInfo info, string name)
        {
            if (info == null!)
                Throw.ArgumentNullException(Argument.info);
            if (name == null!)
                Throw.ArgumentNullException(Argument.name);

            if (info.MemberCount == 0)
                return false;
            SerializationInfo clone = InitEmptyInstanceFrom(info);
            bool removed = false;
            foreach (SerializationEntry entry in info)
            {
                if (entry.Name == name)
                    removed = true;
                else
                    clone.AddValue(entry.Name, entry.Value, entry.ObjectType);
            }

            if (!removed)
                return false;

            SerializationHelper.CopyFields(clone, info);
            return true;
        }

        /// <summary>
        /// Updates or adds a value with the specified <paramref name="name"/> in the <see cref="SerializationInfo"/>.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to be updated.</param>
        /// <param name="name">The name of the entry to be updated or added.</param>
        /// <param name="value">The new value to be set.</param>
        /// <param name="type">The type of the value to be added. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns><see langword="true"/>&#160;if an update occurred (the <see cref="SerializationInfo"/> already contained an entry with the specified <paramref name="name"/>);
        /// <see langword="false"/>&#160;if the value has just been added as a new value.</returns>
        public static bool UpdateValue(this SerializationInfo info, string name, object? value, Type? type = null)
        {
            if (info == null!)
                Throw.ArgumentNullException(Argument.info);
            if (name == null!)
                Throw.ArgumentNullException(Argument.name);

            if (info.MemberCount == 0 || !info.ContainsName(name))
            {
                info.AddValue(name, value, type ?? value?.GetType() ?? Reflector.ObjectType);
                return false;
            }

            SerializationInfo clone = InitEmptyInstanceFrom(info);
            foreach (SerializationEntry entry in info)
            {
                if (entry.Name == name)
                    info.AddValue(name, value, type ?? value?.GetType() ?? Reflector.ObjectType);
                else
                    clone.AddValue(entry.Name, entry.Value, entry.ObjectType);
            }

            SerializationHelper.CopyFields(clone, info);
            return true;
        }

        /// <summary>
        /// Replaces a value with the specified old and new names in the <see cref="SerializationInfo"/>.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to be updated.</param>
        /// <param name="oldName">The name of the entry to be removed.</param>
        /// <param name="newName">The name of the entry to be added.</param>
        /// <param name="value">The new value to be set.</param>
        /// <param name="type">The type of the value to be added. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns><see langword="true"/>&#160;if an entry with the specified old name existed in the <see cref="SerializationInfo"/>
        /// and the replace has been performed; otherwise, <see langword="false"/>.</returns>
        public static bool ReplaceValue(this SerializationInfo info, string oldName, string newName, object? value, Type? type = null)
        {
            if (info == null!)
                Throw.ArgumentNullException(Argument.info);
            if (oldName == null!)
                Throw.ArgumentNullException(Argument.oldName);
            if (newName == null!)
                Throw.ArgumentNullException(Argument.newName);

            if (info.MemberCount == 0 || !info.ContainsName(oldName))
                return false;

            SerializationInfo clone = InitEmptyInstanceFrom(info);
            foreach (SerializationEntry entry in info)
            {
                if (entry.Name == oldName)
                    clone.AddValue(newName, value, type ?? value?.GetType() ?? Reflector.ObjectType);
                else
                    clone.AddValue(entry.Name, entry.Value, entry.ObjectType);
            }

            SerializationHelper.CopyFields(clone, info);
            return true;
        }

        #endregion

        #region Private Methods

        private static SerializationInfo InitEmptyInstanceFrom(SerializationInfo info)
        {
#if NET35
            Assembly asm = Reflector.ResolveAssembly(info.AssemblyName, ResolveAssemblyOptions.AllowPartialMatch | ResolveAssemblyOptions.TryToLoadAssembly | ResolveAssemblyOptions.ThrowError)!;
            Type type = Reflector.ResolveType(asm, info.FullTypeName, ResolveTypeOptions.AllowPartialAssemblyMatch | ResolveTypeOptions.TryToLoadAssemblies | ResolveTypeOptions.ThrowError)!;
            return new SerializationInfo(type, info.GetConverter())
            {
                AssemblyName = info.AssemblyName,
                FullTypeName = info.FullTypeName
            };
#else
            var result = new SerializationInfo(info.ObjectType, info.GetConverter());
            if (info.IsAssemblyNameSetExplicit)
                result.AssemblyName = info.AssemblyName;
            if (info.IsFullTypeNameSetExplicit)
                result.FullTypeName = info.FullTypeName;
            return result;
#endif
        }

        #endregion

        #endregion
    }
}
