#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SerializationHelper.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2025 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Collections.Generic;
using System.Data;

#region Used Namespaces

using System;
using System.Diagnostics.CodeAnalysis;
#if NETFRAMEWORK
using System.CodeDom.Compiler;
#endif
using System.IO;
#if !NET35
using System.Collections;
#endif
using System.Globalization;
using System.Linq;
using System.Reflection;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

#region Used Aliases

using TypeExtensions = KGySoft.CoreLibraries.TypeExtensions;

#endregion

#endregion

#region Suppressions

#if NET8_0_OR_GREATER
#pragma warning disable SYSLIB0050 // Type.IsSerializable/FieldInfo.IsNotSerialized is obsolete - required by BinarySerializationFormatter, which still supports the original infrastructure
#endif

#endregion

namespace KGySoft.Serialization
{
    internal static class SerializationHelper
    {
        #region Fields

        private static readonly LockFreeCache<Type, FieldInfo[]> serializableFieldsCache = new(t =>
            t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(f => !f.IsNotSerialized)
                .OrderBy(f => f.MetadataToken).ToArray(), null, LockFreeCacheOptions.Profile1K);

        private static readonly HashSet<Type> unsafeTypes =
        [
#if NETFRAMEWORK
            typeof(TempFileCollection), // can be used to delete files
#endif
#if !NET35
            StructuralComparisons.StructuralComparer.GetType(), // can be used for DoS attacks
            StructuralComparisons.StructuralEqualityComparer.GetType(), // can be used for DoS attacks
#endif
            // these are non-sealed types with overridable constructors, so their derived types should be handled explicitly as well
            typeof(Delegate), // even when an actual delegate is specified as an expected type, it can be mapped to any unverifiable code
            typeof(FileSystemInfo), // can be used to generate SMB requests with prepared short network paths
            typeof(DataSet), // if RemotingFormat is Binary, it uses a nested BinaryFormatter internally, and we cannot check its content
            typeof(DataTable), // DataSet/DataTable: even if RemotingFormat is Xml, the payload can be prepared to produce a DoS attack - https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/dataset-datatable-dataview/security-guidance
        ];

        private static StringKeyedDictionary<Type>? knownSimpleTypes;

        #endregion

        #region Properties

        private static StringKeyedDictionary<Type> KnownSimpleTypes
        {
            get
            {
                if (knownSimpleTypes == null)
                {
                    var result = new StringKeyedDictionary<Type>();

                    // populating with full names only because assembly name is checked by the caller
                    foreach (Type type in TypeExtensions.GetNativelyParsedTypes().Concat([Reflector.ObjectType, Reflector.NullableType, Reflector.GuidType, Reflector.DictionaryEntryType, Reflector.KeyValuePairType]))
                        result[type.FullName!] = type;

                    knownSimpleTypes = result;
                }

                return knownSimpleTypes;
            }
        }

        #endregion

        #region Methods

        internal static FieldInfo[] GetSerializableFields(Type t) => serializableFieldsCache[t];

        internal static StringKeyedDictionary<FieldInfo> GetFieldsWithUniqueNames(Type type, bool considerNonSerialized)
        {
            var result = new StringKeyedDictionary<(FieldInfo Field, int Count)>();

            // ReSharper disable once PossibleNullReferenceException
            for (Type t = type; t != Reflector.ObjectType; t = t.BaseType!)
            {
                // ReSharper disable once PossibleNullReferenceException
                FieldInfo[] fields = considerNonSerialized
                        ? GetSerializableFields(t)
                        : t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                foreach (FieldInfo field in fields)
                {
                    string name = field.Name;
                    if (!result.TryGetValue(name, out var entry))
                    {
                        result[name] = (field, 1);
                        continue;
                    }

                    // conflicting name 1st try: prefixing by type name
                    string prefixedName = field.DeclaringType!.Name + '+' + field.Name;
                    if (result.TryAdd(prefixedName, (field, 1)))
                        continue;

                    // 1st try didn't work, using numeric postfix
                    entry.Count += 1;
                    result[name] = entry;
                    name += entry.Count.ToString(CultureInfo.InvariantCulture);
                    result[name] = (field, 1);
                }
            }

            return result.ToStringKeyedDictionary(e => e.Key, e => e.Value.Field);
        }

        /// <summary>
        /// Restores target from source. Can be used for read-only properties when source object is already fully serialized.
        /// </summary>
        internal static void CopyFields(object source, object target)
        {
            Debug.Assert(target != null! && source != null! && target.GetType() == source.GetType(), $"Same types are expected in {nameof(CopyFields)}.");
            Debug.Assert(!target!.GetType().IsArray, $"Arrays are not expected in {nameof(CopyFields)}.");

            for (Type? t = target.GetType(); t != null; t = t.BaseType)
            {
                foreach (FieldInfo field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    field.Set(target, field.Get(source));
            }
        }

        internal static bool IsSafeType(Type type)
        {
            if (type.CanBeParsedNatively())
                return true;

            if (IsUnsafeType(type))
                return false;

            return type.IsSerializable;
        }

        internal static bool IsUnsafeType(Type type)
        {
            // unsafeTypes are serializable in the .NET Framework (some even in .NET Core) but still we must deny their deserialization
            // in SafeMode because they can be used for known attacks.
            return unsafeTypes.Contains(type)
                || type.IsSubclassOf(typeof(FileSystemInfo))
                || type.IsSubclassOf(typeof(DataSet))
                || type.IsSubclassOf(typeof(DataTable))
                || type.IsDelegate();
        }

        internal static bool TryGetKnownSimpleType(string typeName, [MaybeNullWhen(false)]out Type result)
        {
            Debug.Assert(!typeName.Contains(']') || typeName.IndexOf(',', typeName.LastIndexOf(']')) < 0, "Non-assembly qualified name expected");
            return KnownSimpleTypes.TryGetValue(typeName, out result);
        }

        #endregion
    }
}
