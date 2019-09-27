#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Accessors.cs
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
using System.Collections;
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
#if !NETCOREAPP2_0
using System.Text;
#endif
using System.Threading;

using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.CoreLibraries;

#if NETCOREAPP2_0 || NETSTANDARD2_1
using CollectionExtensions = KGySoft.CoreLibraries.CollectionExtensions;
#endif

#endregion

// ReSharper disable InconsistentNaming - Properties are named here: Type_Member. Fields: accessorType_Member
namespace KGySoft.Reflection
{
    /// <summary>
    /// Contains lazy initialized well-known accessors used in the project.
    /// </summary>
    internal static class Accessors
    {
        #region Fields

        #region For Public Members

        #region CollectionExtensions

        private static MethodInfo addRangeExtensionMethod;
        private static IDictionary<Type, ActionMethodAccessor> methodsCollectionExtensions_AddRange;

        #endregion

        #region ListExtensions

        private static MethodInfo insertRangeExtensionMethod;
        private static MethodInfo removeRangeExtensionMethod;
        private static MethodInfo replaceRangeExtensionMethod;
        private static IDictionary<Type, ActionMethodAccessor> methodsListExtensions_InsertRange;
        private static IDictionary<Type, ActionMethodAccessor> methodsListExtensions_RemoveRange;
        private static IDictionary<Type, ActionMethodAccessor> methodsListExtensions_ReplaceRange;

        #endregion

        #region ICollection<T>

        private static IDictionary<Type, SimplePropertyAccessor> propertiesICollection_IsReadOnly;
        private static IDictionary<Type, ActionMethodAccessor> methodsICollection_Add;
        private static IDictionary<Type, ActionMethodAccessor> methodsICollection_Clear;
        private static IDictionary<Type, SimplePropertyAccessor> propertiesICollection_Count;
        private static IDictionary<Type, FunctionMethodAccessor> methodsICollection_Remove;

        #endregion

        #region IProducerConsumerCollection<T>

#if !NET35
        private static IDictionary<Type, FunctionMethodAccessor> methodsIProducerConsumerCollection_TryAdd;
#endif

        #endregion

        #region IList<T>

        private static IDictionary<Type, ActionMethodAccessor> methodsIList_Insert;
        private static IDictionary<Type, ActionMethodAccessor> methodsIList_RemoveAt;
        private static IDictionary<Type, IndexerAccessor> propertiesIList_Item;

        #endregion

        #endregion

        #region For Non-Public Members
        // In this region non-public accessors need conditions only if they are not applicable for every supported framework.
        // The #else-#error branches for open-ended versions are in the factories.

        #region Exception

#if NET35 || NET40
        private static FieldAccessor fieldException_source;
        private static FieldAccessor fieldException_remoteStackTraceString;
        private static ActionMethodAccessor methodException_InternalPreserveStackTrace;
#endif

        #endregion

        #region HashSet<T>

#if NET35 || NET40 || NET45 // from .NET 4.72 capacity ctor is available
        private static IDictionary<Type, ActionMethodAccessor> methodsHashSet_Initialize;
#endif

        #endregion

        #region MemoryStream

        private static FunctionMethodAccessor methodMemoryStream_InternalGetBuffer;

#if !NETFRAMEWORK
        private static bool? hasMemoryStream_InternalGetBuffer;
#endif

        #endregion

        #endregion

        #region Any Member

        private static IThreadSafeCacheAccessor<(Type DeclaringType, string PropertyName), PropertyAccessor> properties;
        private static IThreadSafeCacheAccessor<(Type DeclaringType, Type FieldType, string FieldNamePattern), FieldAccessor> fields;
        private static IThreadSafeCacheAccessor<(Type DeclaringType, Type P1, Type P2), ActionMethodAccessor> ctorMethods;

        #endregion

        #endregion

        #region Accessor Factories

        #region For Public Members

        #region CollectionExtensions

        private static MethodAccessor CollectionExtensions_AddRange(Type genericArgument)
        {
            if (methodsCollectionExtensions_AddRange == null)
                Interlocked.CompareExchange(ref methodsCollectionExtensions_AddRange, new LockingDictionary<Type, ActionMethodAccessor>(), null);
            if (!methodsCollectionExtensions_AddRange.TryGetValue(genericArgument, out ActionMethodAccessor accessor))
            {
                // ReSharper disable once PossibleNullReferenceException - will not be null, it exists (ensured by nameof)
                accessor = new ActionMethodAccessor(addRangeExtensionMethod ?? (addRangeExtensionMethod = typeof(CollectionExtensions).GetMethod(nameof(CollectionExtensions.AddRange))).MakeGenericMethod(genericArgument));
                methodsCollectionExtensions_AddRange[genericArgument] = accessor;
            }

            return accessor;
        }

        #endregion

        #region ListExtensions

        private static MethodAccessor ListExtensions_InsertRange(Type genericArgument)
        {
            // Could be an IEnumerable extension but caller needs to check the method before executing
            if (methodsListExtensions_InsertRange == null)
                Interlocked.CompareExchange(ref methodsListExtensions_InsertRange, new LockingDictionary<Type, ActionMethodAccessor>(), null);
            if (!methodsListExtensions_InsertRange.TryGetValue(genericArgument, out ActionMethodAccessor accessor))
            {
                // ReSharper disable once PossibleNullReferenceException - will not be null, it exists (ensured by nameof)
                accessor = new ActionMethodAccessor(insertRangeExtensionMethod ?? (insertRangeExtensionMethod = typeof(ListExtensions).GetMethod(nameof(ListExtensions.InsertRange))).MakeGenericMethod(genericArgument));
                methodsListExtensions_InsertRange[genericArgument] = accessor;
            }

            return accessor;
        }

        private static MethodAccessor ListExtensions_RemoveRange(Type genericArgument)
        {
            // Could be an IEnumerable extension but caller needs to check the method before executing
            if (methodsListExtensions_RemoveRange == null)
                Interlocked.CompareExchange(ref methodsListExtensions_RemoveRange, new LockingDictionary<Type, ActionMethodAccessor>(), null);
            if (!methodsListExtensions_RemoveRange.TryGetValue(genericArgument, out ActionMethodAccessor accessor))
            {
                // ReSharper disable once PossibleNullReferenceException - will not be null, it exists (ensured by nameof)
                accessor = new ActionMethodAccessor(removeRangeExtensionMethod ?? (removeRangeExtensionMethod = typeof(ListExtensions).GetMethod(nameof(ListExtensions.RemoveRange))).MakeGenericMethod(genericArgument));
                methodsListExtensions_RemoveRange[genericArgument] = accessor;
            }

            return accessor;
        }

        private static MethodAccessor ListExtensions_ReplaceRange(Type genericArgument)
        {
            // Could be an IEnumerable extension but caller needs to check the method before executing
            if (methodsListExtensions_ReplaceRange == null)
                Interlocked.CompareExchange(ref methodsListExtensions_ReplaceRange, new LockingDictionary<Type, ActionMethodAccessor>(), null);
            if (!methodsListExtensions_ReplaceRange.TryGetValue(genericArgument, out ActionMethodAccessor accessor))
            {
                // ReSharper disable once PossibleNullReferenceException - will not be null, it exists (ensured by nameof)
                accessor = new ActionMethodAccessor(replaceRangeExtensionMethod ?? (replaceRangeExtensionMethod = typeof(ListExtensions).GetMethod(nameof(ListExtensions.ReplaceRange))).MakeGenericMethod(genericArgument));
                methodsListExtensions_ReplaceRange[genericArgument] = accessor;
            }

            return accessor;
        }


        #endregion

        #region ICollection<T>

        private static SimplePropertyAccessor ICollection_IsReadOnly(Type collectionInterface)
        {
            if (propertiesICollection_IsReadOnly == null)
                Interlocked.CompareExchange(ref propertiesICollection_IsReadOnly, new LockingDictionary<Type, SimplePropertyAccessor>(), null);
            if (!propertiesICollection_IsReadOnly.TryGetValue(collectionInterface, out SimplePropertyAccessor accessor))
            {
                accessor = new SimplePropertyAccessor(collectionInterface.GetProperty(nameof(ICollection<_>.IsReadOnly)));
                propertiesICollection_IsReadOnly[collectionInterface] = accessor;
            }

            return accessor;
        }

        private static ActionMethodAccessor ICollection_Add(Type collectionInterface)
        {
            if (methodsICollection_Add == null)
                Interlocked.CompareExchange(ref methodsICollection_Add, new LockingDictionary<Type, ActionMethodAccessor>(), null);
            if (!methodsICollection_Add.TryGetValue(collectionInterface, out ActionMethodAccessor accessor))
            {
                accessor = new ActionMethodAccessor(collectionInterface.GetMethod(nameof(ICollection<_>.Add)));
                methodsICollection_Add[collectionInterface] = accessor;
            }

            return accessor;
        }

        private static ActionMethodAccessor ICollection_Clear(Type collectionInterface)
        {
            if (methodsICollection_Clear == null)
                Interlocked.CompareExchange(ref methodsICollection_Clear, new LockingDictionary<Type, ActionMethodAccessor>(), null);
            if (!methodsICollection_Clear.TryGetValue(collectionInterface, out ActionMethodAccessor accessor))
            {
                accessor = new ActionMethodAccessor(collectionInterface.GetMethod(nameof(ICollection<_>.Clear)));
                methodsICollection_Clear[collectionInterface] = accessor;
            }

            return accessor;
        }

        private static SimplePropertyAccessor ICollection_Count(Type collectionInterface)
        {
            if (propertiesICollection_Count == null)
                Interlocked.CompareExchange(ref propertiesICollection_Count, new LockingDictionary<Type, SimplePropertyAccessor>(), null);
            if (!propertiesICollection_Count.TryGetValue(collectionInterface, out SimplePropertyAccessor accessor))
            {
                accessor = new SimplePropertyAccessor(collectionInterface.GetProperty(nameof(ICollection<_>.Count)));
                propertiesICollection_Count[collectionInterface] = accessor;
            }

            return accessor;
        }

        private static FunctionMethodAccessor ICollection_Remove(Type collectionInterface)
        {
            if (methodsICollection_Remove == null)
                Interlocked.CompareExchange(ref methodsICollection_Remove, new LockingDictionary<Type, FunctionMethodAccessor>(), null);
            if (!methodsICollection_Remove.TryGetValue(collectionInterface, out FunctionMethodAccessor accessor))
            {
                accessor = new FunctionMethodAccessor(collectionInterface.GetMethod(nameof(ICollection<_>.Remove)));
                methodsICollection_Remove[collectionInterface] = accessor;
            }

            return accessor;
        }

        #endregion

        #region IProducerConsumerCollection<T>

#if !NET35
        private static FunctionMethodAccessor IProducerConsumerCollection_TryAdd(Type collectionInterface)
        {
            if (methodsIProducerConsumerCollection_TryAdd == null)
                Interlocked.CompareExchange(ref methodsIProducerConsumerCollection_TryAdd, new LockingDictionary<Type, FunctionMethodAccessor>(), null);
            if (!methodsIProducerConsumerCollection_TryAdd.TryGetValue(collectionInterface, out FunctionMethodAccessor accessor))
            {
                accessor = new FunctionMethodAccessor(collectionInterface.GetMethod(nameof(IProducerConsumerCollection<_>.TryAdd)));
                methodsIProducerConsumerCollection_TryAdd[collectionInterface] = accessor;
            }

            return accessor;
        }
#endif

        #endregion

        #region IList<T>

        private static ActionMethodAccessor IList_Insert(Type listInterface)
        {
            if (methodsIList_Insert == null)
                Interlocked.CompareExchange(ref methodsIList_Insert, new LockingDictionary<Type, ActionMethodAccessor>(), null);
            if (!methodsIList_Insert.TryGetValue(listInterface, out ActionMethodAccessor accessor))
            {
                accessor = new ActionMethodAccessor(listInterface.GetMethod(nameof(IList<_>.Insert)));
                methodsIList_Insert[listInterface] = accessor;
            }

            return accessor;
        }

        private static ActionMethodAccessor IList_RemoveAt(Type listInterface)
        {
            if (methodsIList_RemoveAt == null)
                Interlocked.CompareExchange(ref methodsIList_RemoveAt, new LockingDictionary<Type, ActionMethodAccessor>(), null);
            if (!methodsIList_RemoveAt.TryGetValue(listInterface, out ActionMethodAccessor accessor))
            {
                accessor = new ActionMethodAccessor(listInterface.GetMethod(nameof(IList<_>.RemoveAt)));
                methodsIList_RemoveAt[listInterface] = accessor;
            }

            return accessor;
        }

        private static IndexerAccessor IList_Item(Type listInterface)
        {
            if (propertiesIList_Item == null)
                Interlocked.CompareExchange(ref propertiesIList_Item, new LockingDictionary<Type, IndexerAccessor>(), null);
            if (!propertiesIList_Item.TryGetValue(listInterface, out IndexerAccessor accessor))
            {
                accessor = new IndexerAccessor(listInterface.GetProperty("Item"));
                propertiesIList_Item[listInterface] = accessor;
            }

            return accessor;
        }

        #endregion

        #endregion

        #region For Non-Public Members
        // Make sure every member in this region is in conditions.
        // Use as narrow conditions as possible and provide an #else #error for open-ended versions so new target frameworks have to always be reviewed.
        // Non-Framework versions can be executed on any runtime (even .NET Core picks a semi-random installation) so for .NET Core/Standard the internal methods must be prepared for null MemberInfos.
        // Whenever possible, use some workaround for non-public .NET Core/Standard libraries.

        #region Exception

#if NET35 || NET40
        private static FieldAccessor Exception_source => fieldException_source ?? (fieldException_source = FieldAccessor.CreateAccessor(typeof(Exception).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor Exception_remoteStackTraceString => fieldException_remoteStackTraceString ?? (fieldException_remoteStackTraceString = FieldAccessor.CreateAccessor(typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static MethodAccessor Exception_InternalPreserveStackTrace => methodException_InternalPreserveStackTrace ?? (methodException_InternalPreserveStackTrace = new ActionMethodAccessor(typeof(Exception).GetMethod(nameof(InternalPreserveStackTrace), BindingFlags.Instance | BindingFlags.NonPublic)));
#endif

        #endregion

        #region HashSet<T>

#if NET35 || NET40 || NET45 // for other frameworks we expect that ctor with capacity is available. If not, the usages will provide the compile error
        private static MethodAccessor HashSet_Initialize<T>()
        {
            if (methodsHashSet_Initialize == null)
                Interlocked.CompareExchange(ref methodsHashSet_Initialize, new Dictionary<Type, ActionMethodAccessor>().AsThreadSafe(), null);
            if (!methodsHashSet_Initialize.TryGetValue(typeof(T), out ActionMethodAccessor accessor))
            {
                accessor = new ActionMethodAccessor(typeof(HashSet<T>).GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic));
                methodsHashSet_Initialize[typeof(T)] = accessor;
            }

            return accessor;
        }
#endif

        #endregion

        #region MemoryStream

#if NET35 || NET40 || NET45
        private static FunctionMethodAccessor MemoryStream_InternalGetBuffer => methodMemoryStream_InternalGetBuffer ?? (methodMemoryStream_InternalGetBuffer = new FunctionMethodAccessor(typeof(MemoryStream).GetMethod("InternalGetBuffer", BindingFlags.Instance | BindingFlags.NonPublic)));
#elif NETCOREAPP2_0 || NETSTANDARD2_1
        private static FunctionMethodAccessor MemoryStream_InternalGetBuffer
        {
            get
            {
                if (hasMemoryStream_InternalGetBuffer == null)
                {
                    MethodInfo mi = typeof(MemoryStream).GetMethod("InternalGetBuffer", BindingFlags.Instance | BindingFlags.NonPublic);
                    hasMemoryStream_InternalGetBuffer = mi != null;
                    if (hasMemoryStream_InternalGetBuffer == true)
                        methodMemoryStream_InternalGetBuffer = new FunctionMethodAccessor(mi);
                }

                return hasMemoryStream_InternalGetBuffer == true ? methodMemoryStream_InternalGetBuffer : null;
            }
        }
#else
#error .NET version is not set or not supported!
#endif

        #endregion

        #endregion

        #region Any Member

        private static PropertyAccessor GetProperty(Type type, string propertyName)
        {
            static PropertyAccessor GetPropertyAccessor((Type DeclaringType, string PropertyName) key)
            {
                // Properties are meant to be used for visible members so always exact names are searched
                PropertyInfo property = key.DeclaringType.GetProperty(key.PropertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return property == null ? null : PropertyAccessor.GetAccessor(property);
            }

            if (properties == null)
                Interlocked.CompareExchange(ref properties, new Cache<(Type, string), PropertyAccessor>(GetPropertyAccessor).GetThreadSafeAccessor(), null);
            return properties[(type, propertyName)];
        }

        private static FieldAccessor GetField(Type type, Type fieldType, string fieldNamePattern)
        {
            FieldAccessor GetFieldAccessor((Type DeclaringType, Type FieldType, string FieldNamePattern) key)
            {
                // Fields are meant to be used for non-visible members either by type or name pattern (or both)
                var fields = key.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                FieldInfo field =
                    fields.FirstOrDefault(f => (key.FieldType == null || f.FieldType == key.FieldType) && f.Name == key.FieldNamePattern) // exact name first
                    ?? fields.FirstOrDefault(f => (key.FieldType == null || f.FieldType == key.FieldType)
                        && (key.FieldNamePattern == null || f.Name.Contains(key.FieldNamePattern, StringComparison.OrdinalIgnoreCase)));
                return field == null ? null : FieldAccessor.GetAccessor(field);
            }

            if (fields == null)
                Interlocked.CompareExchange(ref fields, new Cache<(Type, Type, string), FieldAccessor>(GetFieldAccessor).GetThreadSafeAccessor(), null);
            return fields[(type, fieldType, fieldNamePattern)];
        }

        private static T GetFieldValueOrDefault<T>(object obj, T defaultValue = default, string fieldNamePattern = null)
        {
            var field = GetField(obj.GetType(), typeof(T), fieldNamePattern);
            return field == null ? defaultValue : (T)field.Get(obj);
        }

        private static void SetFieldValue(object obj, string fieldNamePattern, object value, bool throwIfMissing = true)
        {
            Type type = obj.GetType();
            FieldAccessor field = GetField(type, null, fieldNamePattern);
            if (field == null)
            {
                if (throwIfMissing)
                    throw new InvalidOperationException(Res.ReflectionInstanceFieldDoesNotExist(fieldNamePattern, type));
                return;
            }

            field.Set(obj, value);
        }

        private static ActionMethodAccessor GetCtorMethod(Type type, object[] ctorArgs)
        {
            ActionMethodAccessor GetCtorMethodAccessor((Type Type, Type P1, Type P2) key)
            {
                ConstructorInfo ci = key.P1 == null 
                    ? key.Type.GetDefaultConstructor()
                    : key.Type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, key.P2 == null ? new[] { key.P1 } : new[] { key.P1, key.P2 }, null);
                return ci == null ? null : new ActionMethodAccessor(ci);
            }

            if (ctorMethods == null)
                Interlocked.CompareExchange(ref ctorMethods, new Cache<(Type, Type, Type), ActionMethodAccessor>(GetCtorMethodAccessor).GetThreadSafeAccessor(), null);
            return ctorMethods[(type, ctorArgs?.ElementAtOrDefault(0)?.GetType(), ctorArgs?.ElementAtOrDefault(1)?.GetType())];
        }

        #endregion

        #endregion

        #region Internal Accessor Methods

        #region For Public Members

        #region CollectionExtensions

        internal static void AddRange(this IEnumerable target, Type genericArgument, IEnumerable collection) => CollectionExtensions_AddRange(genericArgument).Invoke(null, target, collection);

        #endregion

        #region ListExtensions

        internal static void InsertRange(this IEnumerable target, Type genericArgument, int index, IEnumerable collection) => ListExtensions_InsertRange(genericArgument).Invoke(null, target, index, collection);
        internal static void RemoveRange(this IEnumerable collection, Type genericArgument, int index, int count) => ListExtensions_RemoveRange(genericArgument).Invoke(null, collection, index, count);
        internal static void ReplaceRange(this IEnumerable target, Type genericArgument, int index, int count, IEnumerable collection) => ListExtensions_ReplaceRange(genericArgument).Invoke(null, target, index, count, collection);

        #endregion

        #region ICollection<T>

        internal static bool IsReadOnly([NoEnumeration] this IEnumerable collection, Type collectionInterface) => (bool)ICollection_IsReadOnly(collectionInterface).Get(collection);
        internal static void Add([NoEnumeration] this IEnumerable collection, Type collectionInterface, object item) => ICollection_Add(collectionInterface).Invoke(collection, item);
        internal static void Clear([NoEnumeration] this IEnumerable collection, Type collectionInterface) => ICollection_Clear(collectionInterface).Invoke(collection);
        internal static int Count([NoEnumeration] this IEnumerable collection, Type collectionInterface) => (int)ICollection_Count(collectionInterface).Get(collection);
        internal static bool Remove([NoEnumeration] this IEnumerable collection, Type collectionInterface, object item) => (bool)ICollection_Remove(collectionInterface).Invoke(collection, item);

        #endregion

        #region IProducerConsumerCollection<T>

#if !NET35
        internal static bool TryAddToProducerConsumerCollection([NoEnumeration] this IEnumerable collection, Type collectionInterface, object item) => (bool)IProducerConsumerCollection_TryAdd(collectionInterface).Invoke(collection, item);
#endif

        #endregion

        #region IList<T>

        internal static void Insert([NoEnumeration] this IEnumerable list, Type listInterface, int index, object item) => IList_Insert(listInterface).Invoke(list, index, item);
        internal static void RemoveAt([NoEnumeration] this IEnumerable list, Type listInterface, int index) => IList_RemoveAt(listInterface).Invoke(list, index);
        internal static void SetElementAt([NoEnumeration] this IEnumerable list, Type listInterface, int index, object item) => IList_Item(listInterface).Set(list, item, index);

        #endregion

        #endregion

        #region For Non-Public Members
        // In this region non-public accessors need conditions only if they are not applicable for every supported framework.
        // The #else-#error branches for open-ended versions are in the factories.

        #region Exception

#if NET35 || NET40
        internal static string GetSource(this Exception exception) => (string)Exception_source.Get(exception);
        internal static void SetSource(this Exception exception, string value) => Exception_source.Set(exception, value);
        internal static void SetRemoteStackTraceString(this Exception exception, string value) => Exception_remoteStackTraceString.Set(exception, value);
        internal static void InternalPreserveStackTrace(this Exception exception) => Exception_InternalPreserveStackTrace.Invoke(exception);
#endif

        #endregion

        #region HashSet<T>

#if NET35 || NET40 || NET45
        internal static void Initialize<T>(this HashSet<T> hashSet, int capacity) => HashSet_Initialize<T>().Invoke(hashSet, capacity);
#endif

        #endregion

        #region Point

#if !NETCOREAPP2_0
        internal static int Point_GetX(object point) => point == null ? 0 : (int)GetPropertyValue(point, "X");
        internal static int Point_GetY(object point) => point == null ? 0 : (int)GetPropertyValue(point, "Y");
#endif

        #endregion

        #region MemoryStream

        internal static byte[] InternalGetBuffer(this MemoryStream ms) => (byte[])MemoryStream_InternalGetBuffer?.Invoke(ms);

        #endregion

        #endregion

        #region Members of Any Type
        // Note: The methods also here should be as specific as possible. "Any Type" means that the caller must know whether these methods can be used for a specific type
        //       and that these members use a common cache for any type.
        // Important: Visible members are allowed to be called on types only where we know these properties exist. Otherwise, an InvalidOperationException can be thrown.
        //            For non-visible members we always have to provide some default value.

        #region Specific Members

        #region ResXFileRef

#if !NETCOREAPP2_0
        internal static string ResXFileRef_GetFileName(object fileRef) => (string)GetPropertyValue(fileRef, "FileName");
        internal static string ResXFileRef_GetTypeName(object fileRef) => (string)GetPropertyValue(fileRef, "TypeName");
        internal static Encoding ResXFileRef_GetTextFileEncoding(object fileRef) => (Encoding)GetPropertyValue(fileRef, "TextFileEncoding");
#endif

        #endregion

        #region ResXDataNode

#if !NETCOREAPP2_0
        internal static object ResXDataNode_GetValue(object node) => GetFieldValueOrDefault<object>(node, null, "value");
        internal static string ResXDataNode_GetComment(object node) => GetFieldValueOrDefault<string>(node, null, "comment");
        internal static object ResXDataNode_GetFileRef(object node) => GetField(node.GetType(), null, "fileRef")?.Get(node);
        internal static object ResXDataNode_GetNodeInfo(object node) => GetField(node.GetType(), null, "nodeInfo")?.Get(node);
#endif

        #endregion

        #region DataNodeInfo

#if !NETCOREAPP2_0
        internal static string DataNodeInfo_GetName(object nodeInfo) => GetFieldValueOrDefault<string>(nodeInfo, null, "Name");
        internal static string DataNodeInfo_GetComment(object nodeInfo) => GetFieldValueOrDefault<string>(nodeInfo, null, "Comment");
        internal static string DataNodeInfo_GetTypeName(object nodeInfo) => GetFieldValueOrDefault<string>(nodeInfo, null, "TypeName");
        internal static string DataNodeInfo_GetMimeType(object nodeInfo) => GetFieldValueOrDefault<string>(nodeInfo, null, "MimeType");
        internal static string DataNodeInfo_GetValueData(object nodeInfo) => GetFieldValueOrDefault<string>(nodeInfo, null, "ValueData");
        internal static object DataNodeInfo_GetReaderPosition(object nodeInfo) => GetField(nodeInfo.GetType(), null, "ReaderPosition")?.Get(nodeInfo);
#endif

        #endregion

        #region IEnumerables

        internal static int Count([NoEnumeration] this IEnumerable collection)
        {
            if (collection is ICollection c)
                return c.Count;
            PropertyAccessor property = GetProperty(collection.GetType(), "Count") // StringDictionary
                ?? throw new InvalidOperationException(Res.ReflectionInstancePropertyDoesNotExist("Count", collection.GetType()));
            return (int)property.Get(collection);
        }

        internal static int Capacity([NoEnumeration] this IEnumerable collection)
        {
            PropertyAccessor property = GetProperty(collection.GetType(), "Capacity") // List<T>, CircularList<T>, SortedList<TKey, TValue>, SortedList, CircularSortedList<TKey, TValue>, ArrayList
                ?? throw new InvalidOperationException(Res.ReflectionInstancePropertyDoesNotExist("Capacity", collection.GetType()));
            return (int)property.Get(collection);
        }

        internal static bool IsCaseInsensitive([NoEnumeration] this IEnumerable collection)
            => GetFieldValueOrDefault(collection, false, "caseInsensitive"); // HybridDictionary

        internal static object GetComparer([NoEnumeration] this IEnumerable collection)
        {
            // 1.) By Comparer/EqualityComparer property
            Type type = collection.GetType();
            PropertyAccessor property = GetProperty(type, "Comparer") // Dictionary<TKey, TValue>, HashSet<T>, SortedSet<T>, SortedList<TKey, TValue>, SortedDictionary<TKey, TValue>, CircularSortedList<TKey, TValue>
                ?? GetProperty(type, "EqualityComparer"); // Hashtable
            if (property != null)
                return property.Get(collection);

            // 2.) By *comparer* field
            return GetField(type, null, "comparer")?.Get(collection); // SortedList, ListDictionary, OrderedDictionary
        }

        #endregion

        #region Comparer

        internal static CompareInfo CompareInfo(this Comparer comparer)
            => GetFieldValueOrDefault<CompareInfo>(comparer);

        #endregion

        #region BitArray

        internal static int[] GetUnderlyingArray(this BitArray bitArray)
        {
            var result = GetFieldValueOrDefault<int[]>(bitArray);
            if (result != null)
                return result;

            // we need to restore the array from the bits (should never occur but we must provide a fallback due to private field handling)
            int len = bitArray.Length;
            result = new int[len > 0 ? ((len - 1) >> 5) + 1 : 0];
            for (int i = 0; i < len; i++)
            {
                if (bitArray[i])
                    result[i >> 5] |= 1 << (i % 32);
            }

            return result;
        }

        #endregion

        #region DictionaryEntry/KeyValuePair

        internal static void SetKeyValue(object instance, object key, object value)
        {
            // Though DictionaryEntry.Key/Value have setters they must be set by reflection because of the boxed struct
            if (instance is DictionaryEntry)
            {
                Type type = instance.GetType();
                GetProperty(type, nameof(DictionaryEntry.Key)).Set(instance, key);
                GetProperty(type, nameof(DictionaryEntry.Value)).Set(instance, value);
                return;
            }

            Debug.Assert(instance.GetType().IsGenericTypeOf(Reflector.KeyValuePairType));
            SetFieldValue(instance, "key", key);
            SetFieldValue(instance, "value", value);
        }

        #endregion

        #endregion

        #region Any Member
        // Note: These methods could be completely replaced by Reflector methods but these use a smaller and more direct cache

        internal static object GetPropertyValue(this Type genTypeDef, Type t, string propertyName)
        {
            Type type = genTypeDef.GetGenericType(t);
            PropertyAccessor property = GetProperty(type, propertyName)
                ?? throw new InvalidOperationException(Res.ReflectionStaticPropertyDoesNotExist(propertyName, genTypeDef));
            return property.Get(null);
        }

        internal static object GetPropertyValue(object instance, string propertyName)
        {
            PropertyAccessor property = GetProperty(instance.GetType(), propertyName)
                ?? throw new InvalidOperationException(Res.ReflectionInstancePropertyDoesNotExist(propertyName, instance.GetType()));
            return property.Get(instance);
        }

        /// <summary>
        /// Invokes a constructor on an already created instance.
        /// </summary>
        internal static bool TryInvokeCtor(object instance, params object[] ctorArgs)
        {
            Debug.Assert(ctorArgs == null || ctorArgs.Length <= 2, "Constructors with more than 2 arguments are not cached. Use MethodBase.Invoke instead.");
            ActionMethodAccessor accessor = GetCtorMethod(instance.GetType(), ctorArgs);
            if (accessor == null)
                return false;

            accessor.Invoke(instance, ctorArgs);
            return true;
        }

        #endregion

        #endregion

        #endregion
    }
}
