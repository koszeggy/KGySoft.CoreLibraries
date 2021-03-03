#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SerializationHelper.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

using KGySoft.Collections;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization
{
    internal static class SerializationHelper
    {
        #region Fields

        private static readonly IThreadSafeCacheAccessor<Type, FieldInfo[]> serializableFieldsCache = ThreadSafeCacheFactory.Create<Type, FieldInfo[]>(t =>
            t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(f => !f.IsNotSerialized)
                .OrderBy(f => f.MetadataToken).ToArray(), LockFreeCacheOptions.Profile1K);

        #endregion

        #region Methods

        internal static FieldInfo[] GetSerializableFields(Type t) => serializableFieldsCache[t];

        internal static Dictionary<string, FieldInfo> GetFieldsWithUniqueNames(Type type, bool considerNonSerialized)
        {
            var result = new Dictionary<string, (FieldInfo Field, int Count)>();

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
                    if (!result.ContainsKey(prefixedName))
                    {
                        result[prefixedName] = (field, 1);
                        continue;
                    }

                    // 1st try didn't work, using numeric postfix
                    entry.Count += 1;
                    result[name] = entry;
                    name += entry.Count.ToString(CultureInfo.InvariantCulture);
                    result[name] = (field, 1);
                }
            }

            return result.ToDictionary(e => e.Key, e => e.Value.Field);
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

        #endregion
    }
}
