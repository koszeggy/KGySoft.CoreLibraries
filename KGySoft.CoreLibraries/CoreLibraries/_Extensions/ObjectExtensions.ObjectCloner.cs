#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObjectExtensions.ObjectCloner.cs
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security;

using KGySoft.Reflection;
using KGySoft.Serialization;

#endregion

namespace KGySoft.CoreLibraries
{
    public partial class ObjectExtensions
    {
        private class ObjectCloner
        {
            #region Fields

            private readonly Func<object, object?>? customClone;
            private readonly Dictionary<object, object> clonesCache;

            #endregion

            #region Constructors

            private ObjectCloner(Func<object, object?>? customClone)
            {
                this.customClone = customClone;
                clonesCache = new Dictionary<object, object>(ReferenceEqualityComparer.Comparer);
            }

            #endregion

            #region Methods

            #region Static Methods

            #region Internal Methods

            [return:NotNullIfNotNull("obj")]internal static object? Clone(object? obj, Func<object, object?>? customClone)
            {
                if (obj == null)
                    return null;

                object? result = customClone?.Invoke(obj);
                if (result != null)
                    return result;

                var type = obj.GetType();

                // non-cloned types
                if (!IsDeepCloned(type))
                    return obj;

                // actual cloning
                return new ObjectCloner(customClone).CreateClone(obj, true, true);
            }

            #endregion

            #region Private Methods

            private static bool IsDeepCloned(Type type)
                => !type.IsPrimitive
                && type != Reflector.StringType
                && !type.IsEnum
                && !type.IsDelegate()
                && type != Reflector.RuntimeType
                && type.IsManaged();

            #endregion

            #endregion

            #region Instance Methods

            [SecuritySafeCritical]
            private object CreateClone(object obj, bool handleReference, bool isRoot = false)
            {
                var type = obj.GetType();
                Debug.Assert(!isRoot || IsDeepCloned(type));

                object? clone = null;

                // trying from reference cache
                if (handleReference && clonesCache.TryGetValue(obj, out clone))
                    return clone;

                // just to avoid double check for root
                if (!isRoot)
                {
                    // cloning by the specified delegate
                    clone = customClone?.Invoke(obj);

                    // cloning is not needed (using self instance)
                    if (clone == null && !IsDeepCloned(type))
                        clone = obj;
                }

                Array? array = obj as Array;
                bool deepCloning = clone == null;

                // initializing the clone instance
                if (deepCloning)
                {
                    if (array != null)
                        clone = array.Clone();
                    else if (!Reflector.TryCreateUninitializedObject(type, out clone))
                        clone = obj.MemberwiseClone();
                }

                // Adding the (possibly empty) clone to the cache. As reference equality is used hash code cannot change while initializing fields.
                // It is important to add the clone to the cache before starting recursion so possible circular references can be handled correctly.
                if (handleReference)
                    clonesCache[obj] = clone!;

                if (deepCloning)
                {
                    if (array != null)
                        CloneArray(array, (Array)clone!);
                    else
                        CloneFields(obj, clone!);
                }

                return clone!;
            }

            private void CloneArray(Array source, Array target)
            {
                var type = source.GetType();
                Debug.Assert(type == target.GetType());

                Type elementType = type.GetElementType()!;

                // not even a simple copy is necessary because target is a shallow clone
                if (customClone == null && !IsDeepCloned(elementType))
                    return;

                bool handleReferences = !elementType.IsValueType;
                if (type.IsZeroBasedArray())
                {
                    int length = source.Length;
                    for (int i = 0; i < length; i++)
                    {
                        object? obj = source.GetValue(i);
                        if (obj != null)
                            target.SetValue(CreateClone(obj, handleReferences), i);
                    }

                    return;
                }

                var indexer = new ArrayIndexer(source);
                while (indexer.MoveNext())
                {
                    object? obj = source.GetValue(indexer.Current);
                    if (obj != null)
                        target.SetValue(CreateClone(obj, handleReferences), indexer.Current);
                }
            }

            private void CloneFields(object source, object target)
            {
                var type = source.GetType();
                Debug.Assert(type == target.GetType());

                for (Type? t = type; t != Reflector.ObjectType; t = t.BaseType)
                {
                    FieldInfo[] fields = t!.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    foreach (FieldInfo field in fields)
                    {
                        object? obj = field.Get(source);
                        if (obj != null)
                            field.Set(target, CreateClone(obj, !field.FieldType.IsValueType));
                    }
                }
            }

            #endregion

            #endregion
        }
    }
}
