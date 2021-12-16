#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Accessors.cs
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
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices; 
using System.Runtime.Serialization;
using System.Security; 
#if !NETCOREAPP2_0
using System.Text;
#endif
using System.Threading;

using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.CoreLibraries;

using CollectionExtensions = KGySoft.CoreLibraries.CollectionExtensions;

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

        private static MethodInfo? addRangeExtensionMethod;
        private static IThreadSafeCacheAccessor<Type, MethodAccessor>? methodsCollectionExtensions_AddRange;

        #endregion

        #region ListExtensions

        private static MethodInfo? insertRangeExtensionMethod;
        private static MethodInfo? removeRangeExtensionMethod;
        private static MethodInfo? replaceRangeExtensionMethod;
        private static IThreadSafeCacheAccessor<Type, MethodAccessor>? methodsListExtensions_InsertRange;
        private static IThreadSafeCacheAccessor<Type, MethodAccessor>? methodsListExtensions_RemoveRange;
        private static IThreadSafeCacheAccessor<Type, MethodAccessor>? methodsListExtensions_ReplaceRange;

        #endregion

        #region ICollection<T>

        private static IThreadSafeCacheAccessor<Type, PropertyAccessor>? propertiesICollection_IsReadOnly;
        private static IThreadSafeCacheAccessor<Type, MethodAccessor>? methodsICollection_Add;
        private static IThreadSafeCacheAccessor<Type, MethodAccessor>? methodsICollection_Clear;
        private static IThreadSafeCacheAccessor<Type, PropertyAccessor>? propertiesICollection_Count;
        private static IThreadSafeCacheAccessor<Type, MethodAccessor>? methodsICollection_Remove;

        #endregion

        #region IProducerConsumerCollection<T>

#if !NET35
        private static IThreadSafeCacheAccessor<Type, MethodAccessor>? methodsIProducerConsumerCollection_TryAdd;
#endif

        #endregion

        #region IList<T>

        private static IThreadSafeCacheAccessor<Type, MethodAccessor>? methodsIList_Insert;
        private static IThreadSafeCacheAccessor<Type, MethodAccessor>? methodsIList_RemoveAt;
        private static IThreadSafeCacheAccessor<Type, PropertyAccessor>? propertiesIList_Item;

        #endregion

        #endregion

        #region For Non-Public Members
        // In this region non-public accessors need conditions only if they are not applicable for every supported framework.
        // The #else-#error branches for open-ended versions are in the factories.

        #region Exception

#if NET35 || NET40
        private static FieldAccessor? fieldException_source;
        private static FieldAccessor? fieldException_remoteStackTraceString;
        private static ActionMethodAccessor? methodException_InternalPreserveStackTrace;
#endif

        #endregion

        #region MemoryStream

        private static FunctionMethodAccessor? methodMemoryStream_InternalGetBuffer;
        private static bool? hasMemoryStream_InternalGetBuffer;

        #endregion

        #region Object

        private static FunctionMethodAccessor? methodObject_MemberwiseClone;

        #endregion

        #region IIListProvider<T>

        private static IThreadSafeCacheAccessor<Type, MethodAccessor?>? methodsIIListProvider_GetCount;
        private static bool? hasIIListProvider;
        private static Type? typeIIListProvider;

        #endregion

        #region Pointer
#if NETFRAMEWORK && !NET35

        private static FunctionMethodAccessor? methodPointer_GetPointerValue;
        private static bool? hasPointer_GetPointerValue;

#endif
        #endregion

        #endregion

        #region Any Member

        private static IThreadSafeCacheAccessor<(Type DeclaringType, string PropertyName), PropertyAccessor?>? properties;
        private static IThreadSafeCacheAccessor<(Type DeclaringType, Type? FieldType, string? FieldNamePattern), FieldAccessor?>? fields;
        private static IThreadSafeCacheAccessor<(Type DeclaringType, Type? P1, Type? P2), ActionMethodAccessor?>? ctorMethods;

        #endregion

        #endregion

        #region Accessor Factories

        #region For Public Members

        #region CollectionExtensions

        private static MethodAccessor CollectionExtensions_AddRange(Type genericArgument)
        {
            if (methodsCollectionExtensions_AddRange == null)
            {
                if (addRangeExtensionMethod == null)
                    Interlocked.CompareExchange(ref addRangeExtensionMethod, typeof(CollectionExtensions).GetMethod(nameof(CollectionExtensions.AddRange)), null);

                Interlocked.CompareExchange(ref methodsCollectionExtensions_AddRange, 
                    ThreadSafeCacheFactory.Create<Type, MethodAccessor>(t => MethodAccessor.GetAccessor(addRangeExtensionMethod!.GetGenericMethod(t)), LockFreeCacheOptions.Profile4),
                    null);
            }

            return methodsCollectionExtensions_AddRange[genericArgument];
        }

        #endregion

        #region ListExtensions

        private static MethodAccessor ListExtensions_InsertRange(Type genericArgument)
        {
            if (methodsListExtensions_InsertRange == null)
            {
                if (insertRangeExtensionMethod == null)
                    Interlocked.CompareExchange(ref insertRangeExtensionMethod, typeof(ListExtensions).GetMethod(nameof(ListExtensions.InsertRange)), null);

                Interlocked.CompareExchange(ref methodsListExtensions_InsertRange,
                    ThreadSafeCacheFactory.Create<Type, MethodAccessor>(t => MethodAccessor.GetAccessor(insertRangeExtensionMethod!.GetGenericMethod(t)), LockFreeCacheOptions.Profile4),
                    null);
            }

            return methodsListExtensions_InsertRange[genericArgument];
        }

        private static MethodAccessor ListExtensions_RemoveRange(Type genericArgument)
        {
            if (methodsListExtensions_RemoveRange == null)
            {
                if (removeRangeExtensionMethod == null)
                    Interlocked.CompareExchange(ref removeRangeExtensionMethod, typeof(ListExtensions).GetMethod(nameof(ListExtensions.RemoveRange)), null);

                Interlocked.CompareExchange(ref methodsListExtensions_RemoveRange,
                    ThreadSafeCacheFactory.Create<Type, MethodAccessor>(t => MethodAccessor.GetAccessor(removeRangeExtensionMethod!.GetGenericMethod(t)), LockFreeCacheOptions.Profile4),
                    null);
            }

            return methodsListExtensions_RemoveRange[genericArgument];
        }

        private static MethodAccessor ListExtensions_ReplaceRange(Type genericArgument)
        {
            if (methodsListExtensions_ReplaceRange == null)
            {
                if (replaceRangeExtensionMethod == null)
                    Interlocked.CompareExchange(ref replaceRangeExtensionMethod, typeof(ListExtensions).GetMethod(nameof(ListExtensions.ReplaceRange)), null);

                Interlocked.CompareExchange(ref methodsListExtensions_ReplaceRange,
                    ThreadSafeCacheFactory.Create<Type, MethodAccessor>(t => MethodAccessor.GetAccessor(replaceRangeExtensionMethod!.GetGenericMethod(t)), LockFreeCacheOptions.Profile4),
                    null);
            }

            return methodsListExtensions_ReplaceRange[genericArgument];
        }

        #endregion

        #region ICollection<T>

        private static PropertyAccessor ICollection_IsReadOnly(Type collectionInterface)
        {
            if (propertiesICollection_IsReadOnly == null)
            {
                Interlocked.CompareExchange(ref propertiesICollection_IsReadOnly,
                    ThreadSafeCacheFactory.Create<Type, PropertyAccessor>(i => PropertyAccessor.GetAccessor(i.GetProperty(nameof(ICollection<_>.IsReadOnly))!), LockFreeCacheOptions.Profile16),
                    null);
            }

            return propertiesICollection_IsReadOnly[collectionInterface];
        }

        private static MethodAccessor ICollection_Add(Type collectionInterface)
        {
            if (methodsICollection_Add == null)
            {
                Interlocked.CompareExchange(ref methodsICollection_Add,
                    ThreadSafeCacheFactory.Create<Type, MethodAccessor>(i => MethodAccessor.GetAccessor(i.GetMethod(nameof(ICollection<_>.Add))!), LockFreeCacheOptions.Profile16),
                    null);
            }

            return methodsICollection_Add[collectionInterface];
        }

        private static MethodAccessor ICollection_Clear(Type collectionInterface)
        {
            if (methodsICollection_Clear == null)
            {
                Interlocked.CompareExchange(ref methodsICollection_Clear,
                    ThreadSafeCacheFactory.Create<Type, MethodAccessor>(i => MethodAccessor.GetAccessor(i.GetMethod(nameof(ICollection<_>.Clear))!), LockFreeCacheOptions.Profile16),
                    null);
            }

            return methodsICollection_Clear[collectionInterface];
        }

        private static PropertyAccessor ICollection_Count(Type collectionInterface)
        {
            if (propertiesICollection_Count == null)
            {
                Interlocked.CompareExchange(ref propertiesICollection_Count,
                    ThreadSafeCacheFactory.Create<Type, PropertyAccessor>(i => PropertyAccessor.GetAccessor(i.GetProperty(nameof(ICollection<_>.Count))!), LockFreeCacheOptions.Profile16),
                    null);
            }

            return propertiesICollection_Count[collectionInterface];
        }

        private static MethodAccessor ICollection_Remove(Type collectionInterface)
        {
            if (methodsICollection_Remove == null)
            {
                Interlocked.CompareExchange(ref methodsICollection_Remove,
                    ThreadSafeCacheFactory.Create<Type, MethodAccessor>(i => MethodAccessor.GetAccessor(i.GetMethod(nameof(ICollection<_>.Remove))!), LockFreeCacheOptions.Profile16),
                    null);
            }

            return methodsICollection_Remove[collectionInterface];
        }

        #endregion

        #region IProducerConsumerCollection<T>

#if !NET35
        private static MethodAccessor IProducerConsumerCollection_TryAdd(Type collectionInterface)
        {
            if (methodsIProducerConsumerCollection_TryAdd == null)
            {
                Interlocked.CompareExchange(ref methodsIProducerConsumerCollection_TryAdd,
                    ThreadSafeCacheFactory.Create<Type, MethodAccessor>(i => MethodAccessor.GetAccessor(i.GetMethod(nameof(IProducerConsumerCollection<_>.TryAdd))!), LockFreeCacheOptions.Profile16),
                    null);
            }

            return methodsIProducerConsumerCollection_TryAdd[collectionInterface];
        }
#endif

        #endregion

        #region IList<T>

        private static MethodAccessor IList_Insert(Type listInterface)
        {
            if (methodsIList_Insert == null)
            {
                Interlocked.CompareExchange(ref methodsIList_Insert,
                    ThreadSafeCacheFactory.Create<Type, MethodAccessor>(i => MethodAccessor.GetAccessor(i.GetMethod(nameof(IList<_>.Insert))!), LockFreeCacheOptions.Profile16),
                    null);
            }

            return methodsIList_Insert[listInterface];
        }

        private static MethodAccessor IList_RemoveAt(Type listInterface)
        {
            if (methodsIList_RemoveAt == null)
            {
                Interlocked.CompareExchange(ref methodsIList_RemoveAt,
                    ThreadSafeCacheFactory.Create<Type, MethodAccessor>(i => MethodAccessor.GetAccessor(i.GetMethod(nameof(IList<_>.RemoveAt))!), LockFreeCacheOptions.Profile16),
                    null);
            }

            return methodsIList_RemoveAt[listInterface];
        }

        private static PropertyAccessor IList_Item(Type listInterface)
        {
            if (propertiesIList_Item == null)
            {
                Interlocked.CompareExchange(ref propertiesIList_Item,
                    ThreadSafeCacheFactory.Create<Type, PropertyAccessor>(i => PropertyAccessor.GetAccessor(i.GetProperty("Item")!), LockFreeCacheOptions.Profile16),
                    null);
            }

            return propertiesIList_Item[listInterface];
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
        private static FieldAccessor Exception_source => fieldException_source ??= FieldAccessor.CreateAccessor(typeof(Exception).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic)!);
        private static FieldAccessor Exception_remoteStackTraceString => fieldException_remoteStackTraceString ??= FieldAccessor.CreateAccessor(typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic)!);
        private static MethodAccessor Exception_InternalPreserveStackTrace => methodException_InternalPreserveStackTrace ??= new ActionMethodAccessor(typeof(Exception).GetMethod(nameof(InternalPreserveStackTrace), BindingFlags.Instance | BindingFlags.NonPublic)!);
#endif

        #endregion

        #region MemoryStream

        private static FunctionMethodAccessor? MemoryStream_InternalGetBuffer
        {
            get
            {
                if (hasMemoryStream_InternalGetBuffer == null)
                {
                    MethodInfo? mi = typeof(MemoryStream).GetMethod("InternalGetBuffer", BindingFlags.Instance | BindingFlags.NonPublic);
                    hasMemoryStream_InternalGetBuffer = mi != null;
                    if (hasMemoryStream_InternalGetBuffer == true)
                        methodMemoryStream_InternalGetBuffer = new FunctionMethodAccessor(mi!);
                }

                return hasMemoryStream_InternalGetBuffer == true ? methodMemoryStream_InternalGetBuffer : null;
            }
        }

        #endregion

        #region Object

        private static FunctionMethodAccessor Object_MemberwiseClone => methodObject_MemberwiseClone ??= new FunctionMethodAccessor(typeof(object).GetMethod(nameof(MemberwiseClone), BindingFlags.Instance | BindingFlags.NonPublic)!);

        #endregion

        #region IIListProvider<T>

        private static Type? IIListProviderType
        {
            get
            {
                if (!hasIIListProvider.HasValue)
                {
                    typeIIListProvider = Reflector.ResolveType(typeof(Enumerable).Assembly, "System.Linq.IIListProvider`1");
                    hasIIListProvider = typeIIListProvider != null;
                }

                return typeIIListProvider;
            }
        }

        private static MethodAccessor? IIListProvider_GetCount(Type genericArgument)
        {
            static MethodAccessor? GetGetCountMethod(Type arg)
            {
                Type? listProviderType = IIListProviderType;
                if (listProviderType == null)
                    return null;
                Type genericType = listProviderType.GetGenericType(arg);
                MethodInfo? getCountMethod = genericType.GetMethod("GetCount");
                return getCountMethod == null ? null : MethodAccessor.GetAccessor(getCountMethod);
            }

            if (methodsIIListProvider_GetCount == null)
                Interlocked.CompareExchange(ref methodsIIListProvider_GetCount, ThreadSafeCacheFactory.Create<Type, MethodAccessor?>(GetGetCountMethod, LockFreeCacheOptions.Profile128), null);

            return methodsIIListProvider_GetCount[genericArgument];
        }

        #endregion

        #region Pointer
#if NETFRAMEWORK && !NET35

        private static FunctionMethodAccessor? Pointer_GetPointerValue
        {
            get
            {
                if (hasPointer_GetPointerValue == null)
                {
                    MethodInfo? mi = typeof(Pointer).GetMethod("GetPointerValue", BindingFlags.Instance | BindingFlags.NonPublic);
                    hasPointer_GetPointerValue = mi != null;
                    if (hasPointer_GetPointerValue == true)
                        methodPointer_GetPointerValue = new FunctionMethodAccessor(mi!);
                }

                return hasPointer_GetPointerValue == true ? methodPointer_GetPointerValue : null;
            }
        }

#endif
        #endregion

        #endregion

        #region Any Member

        private static PropertyAccessor? GetProperty(Type type, string propertyName)
        {
            static PropertyAccessor? GetPropertyAccessor((Type DeclaringType, string PropertyName) key)
            {
                // Ignoring case is allowed due to some incompatibilities between platforms (eg. internal IsSzArray vs. public IsSZArray).
                // This may prevent an InvalidOperationException when the Framework binaries are executed on .NET Core (may occur when using debugger visualizers, for example).
                PropertyInfo? property = key.DeclaringType.GetProperty(key.PropertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                return property == null ? null : PropertyAccessor.GetAccessor(property);
            }

            if (properties == null)
                Interlocked.CompareExchange(ref properties, ThreadSafeCacheFactory.Create<(Type, string), PropertyAccessor?>(GetPropertyAccessor, LockFreeCacheOptions.Profile128), null);
            return properties[(type, propertyName)];
        }

        private static FieldAccessor? GetField(Type type, Type? fieldType, string? fieldNamePattern)
        {
            // Fields are meant to be used for non-visible members either by type or name pattern (or both)
            FieldAccessor? GetFieldAccessor((Type DeclaringType, Type? FieldType, string? FieldNamePattern) key)
            {
                for (Type? t = key.DeclaringType; t != Reflector.ObjectType; t = t.BaseType)
                {
                    FieldInfo[] fieldArray = t!.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    FieldInfo? field = fieldArray.FirstOrDefault(f => (key.FieldType == null || f.FieldType == key.FieldType) && f.Name == key.FieldNamePattern) // exact name first
                        ?? fieldArray.FirstOrDefault(f => (key.FieldType == null || f.FieldType == key.FieldType)
                            && (key.FieldNamePattern == null || f.Name.Contains(key.FieldNamePattern, StringComparison.OrdinalIgnoreCase)));

                    if (field != null)
                        return FieldAccessor.GetAccessor(field);
                }

                return null;
            }

            if (fields == null)
                Interlocked.CompareExchange(ref fields, ThreadSafeCacheFactory.Create<(Type, Type?, string?), FieldAccessor?>(GetFieldAccessor, LockFreeCacheOptions.Profile128), null);
            return fields[(type, fieldType, fieldNamePattern)];
        }

        private static T? GetFieldValueOrDefault<T>(object obj, T? defaultValue = default, string? fieldNamePattern = null)
        {
            FieldAccessor? field = GetField(obj.GetType(), typeof(T), fieldNamePattern);
            return field == null ? defaultValue : (T)field.Get(obj)!;
        }

        private static T GetFieldValueOrDefault<T>(object obj, Func<T> defaultValueFactory)
        {
            FieldAccessor? field = GetField(obj.GetType(), typeof(T), null);
            return field == null ? defaultValueFactory.Invoke() : (T)field.Get(obj)!;
        }

        private static void SetFieldValue(object obj, string fieldNamePattern, object? value, bool throwIfMissing = true)
        {
            Type type = obj.GetType();
            FieldAccessor? field = GetField(type, null, fieldNamePattern);
            if (field == null)
            {
                if (throwIfMissing)
                    Throw.InvalidOperationException(Res.ReflectionInstanceFieldDoesNotExist(fieldNamePattern, type));
                return;
            }
#if NETSTANDARD2_0
            if (field.IsReadOnly || field.MemberInfo.DeclaringType?.IsValueType == true)
            {
                ((FieldInfo)field.MemberInfo).SetValue(obj, value);
                return;
            }
#endif

            field.Set(obj, value);
        }

        private static ActionMethodAccessor? GetCtorMethod(Type type, object?[] ctorArgs)
        {
            static ActionMethodAccessor? GetCtorMethodAccessor((Type Type, Type? P1, Type? P2) key)
            {
                ConstructorInfo? ci = key.P1 == null 
                    ? key.Type.GetDefaultConstructor()
                    : key.Type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, key.P2 == null ? new[] { key.P1 } : new[] { key.P1, key.P2 }, null);
                return ci == null ? null : new ActionMethodAccessor(ci);
            }

            if (ctorMethods == null)
                Interlocked.CompareExchange(ref ctorMethods, ThreadSafeCacheFactory.Create<(Type, Type?, Type?), ActionMethodAccessor?>(GetCtorMethodAccessor, LockFreeCacheOptions.Profile128), null);
            return ctorMethods[(type, ctorArgs.ElementAtOrDefault(0)?.GetType(), ctorArgs.ElementAtOrDefault(1)?.GetType())];
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

        internal static bool IsReadOnly([NoEnumeration]this IEnumerable collection, Type collectionInterface) => (bool)ICollection_IsReadOnly(collectionInterface).Get(collection)!;
        internal static void Add([NoEnumeration]this IEnumerable collection, Type collectionInterface, object? item) => ICollection_Add(collectionInterface).Invoke(collection, item);
        internal static void Clear([NoEnumeration]this IEnumerable collection, Type collectionInterface) => ICollection_Clear(collectionInterface).Invoke(collection);
        internal static int Count([NoEnumeration]this IEnumerable collection, Type collectionInterface) => (int)ICollection_Count(collectionInterface).Get(collection)!;
        internal static bool Remove([NoEnumeration]this IEnumerable collection, Type collectionInterface, object? item) => (bool)ICollection_Remove(collectionInterface).Invoke(collection, item)!;

        #endregion

        #region IProducerConsumerCollection<T>

#if !NET35
        internal static bool TryAddToProducerConsumerCollection([NoEnumeration]this IEnumerable collection, Type collectionInterface, object? item) => (bool)IProducerConsumerCollection_TryAdd(collectionInterface).Invoke(collection, item)!;
#endif

        #endregion

        #region IList<T>

        internal static void Insert([NoEnumeration]this IEnumerable list, Type listInterface, int index, object? item) => IList_Insert(listInterface).Invoke(list, index, item);
        internal static void RemoveAt([NoEnumeration]this IEnumerable list, Type listInterface, int index) => IList_RemoveAt(listInterface).Invoke(list, index);
        internal static void SetElementAt([NoEnumeration]this IEnumerable list, Type listInterface, int index, object? item) => IList_Item(listInterface).Set(list, item, index);

        #endregion

        #endregion

        #region For Non-Public Members
        // In this region non-public accessors need conditions only if they are not applicable for every supported framework.
        // The #else-#error branches for open-ended versions are in the factories.

        #region Exception

#if NET35 || NET40
        internal static string? GetSource(this Exception exception) => (string?)Exception_source.Get(exception);
        internal static void SetSource(this Exception exception, string? value) => Exception_source.Set(exception, value);
        internal static void SetRemoteStackTraceString(this Exception exception, string value) => Exception_remoteStackTraceString.Set(exception, value);
        internal static void InternalPreserveStackTrace(this Exception exception) => Exception_InternalPreserveStackTrace.Invoke(exception);
#endif

        #endregion

        #region Point

#if !NETCOREAPP2_0
        internal static int Point_GetX(object? point) => point == null ? 0 : (int)GetPropertyValue(point, "X")!;
        internal static int Point_GetY(object? point) => point == null ? 0 : (int)GetPropertyValue(point, "Y")!;
#endif

        #endregion

        #region MemoryStream

        // ReSharper disable once ConstantConditionalAccessQualifier - there are some targets where it can be null
        internal static byte[]? InternalGetBuffer(this MemoryStream ms) => (byte[]?)MemoryStream_InternalGetBuffer?.Invoke(ms);

        #endregion

        #region Object

        internal static object MemberwiseClone(this object obj) => Object_MemberwiseClone.Invoke(obj)!;

        #endregion

        #region IIListProvider<T>

#if !NET6_0_OR_GREATER
        internal static int? GetListProviderCount<T>([NoEnumeration]this IEnumerable<T> collection)
        {
            MethodAccessor? accessor = IIListProvider_GetCount(typeof(T));
            if (accessor == null || !accessor.MemberInfo.DeclaringType!.IsInstanceOfType(collection))
                return null;
            return accessor.Invoke(collection, true) as int?;
        }
#endif

        internal static int? GetListProviderCount([NoEnumeration]this IEnumerable collection)
        {
            Type? listProviderType = IIListProviderType;
            if (listProviderType == null || !collection.GetType().IsImplementationOfGenericType(listProviderType, out Type? genericType))
                return null;

            MethodAccessor? accessor = IIListProvider_GetCount(genericType.GetGenericArguments()[0]);
            if (accessor == null)
                return null;
            Debug.Assert(accessor.MemberInfo.DeclaringType!.IsInstanceOfType(collection));
            return accessor.Invoke(collection, true) as int?;
        }

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
        internal static string? ResXFileRef_GetFileName(object fileRef) => (string?)GetPropertyValue(fileRef, "FileName");
        internal static string? ResXFileRef_GetTypeName(object fileRef) => (string?)GetPropertyValue(fileRef, "TypeName");
        internal static Encoding? ResXFileRef_GetTextFileEncoding(object fileRef) => (Encoding?)GetPropertyValue(fileRef, "TextFileEncoding");
#endif

        #endregion

        #region ResXDataNode

#if !NETCOREAPP2_0
        internal static object? ResXDataNode_GetValue(object node) => GetFieldValueOrDefault<object?>(node, null, "value");
        internal static string? ResXDataNode_GetComment(object node) => GetFieldValueOrDefault<string?>(node, null, "comment");
        internal static object? ResXDataNode_GetFileRef(object node) => GetField(node.GetType(), null, "fileRef")?.Get(node);
        internal static object? ResXDataNode_GetNodeInfo(object node) => GetField(node.GetType(), null, "nodeInfo")?.Get(node);
#endif

        #endregion

        #region DataNodeInfo

#if !NETCOREAPP2_0
        internal static string? DataNodeInfo_GetName(object nodeInfo) => GetFieldValueOrDefault<string?>(nodeInfo, null, "Name");
        internal static string? DataNodeInfo_GetComment(object nodeInfo) => GetFieldValueOrDefault<string?>(nodeInfo, null, "Comment");
        internal static string? DataNodeInfo_GetTypeName(object nodeInfo) => GetFieldValueOrDefault<string?>(nodeInfo, null, "TypeName");
        internal static string? DataNodeInfo_GetMimeType(object nodeInfo) => GetFieldValueOrDefault<string?>(nodeInfo, null, "MimeType");
        internal static string? DataNodeInfo_GetValueData(object nodeInfo) => GetFieldValueOrDefault<string?>(nodeInfo, null, "ValueData");
        internal static object? DataNodeInfo_GetReaderPosition(object nodeInfo) => GetField(nodeInfo.GetType(), null, "ReaderPosition")?.Get(nodeInfo);
#endif

        #endregion

        #region IEnumerables

        internal static int Count([NoEnumeration]this IEnumerable collection)
        {
            if (collection is ICollection c)
                return c.Count;
            PropertyAccessor? property = GetProperty(collection.GetType(), "Count"); // StringDictionary
            if (property == null)
                Throw.InvalidOperationException(Res.ReflectionInstancePropertyDoesNotExist("Count", collection.GetType()));
            return (int)property.Get(collection)!;
        }

        internal static int Capacity([NoEnumeration]this IEnumerable collection)
        {
            PropertyAccessor? property = GetProperty(collection.GetType(), "Capacity"); // List<T>, CircularList<T>, SortedList<TKey, TValue>, SortedList, CircularSortedList<TKey, TValue>, ArrayList
            if (property == null)
                Throw.InvalidOperationException(Res.ReflectionInstancePropertyDoesNotExist("Capacity", collection.GetType()));
            return (int)property.Get(collection)!;
        }

        internal static bool IsCaseInsensitive([NoEnumeration]this IEnumerable collection)
            => GetFieldValueOrDefault(collection, false, "caseInsensitive"); // HybridDictionary

        internal static object? GetComparer([NoEnumeration]this IEnumerable collection)
        {
            // 1.) By Comparer/EqualityComparer property
            Type type = collection.GetType();
            PropertyAccessor? property =  GetProperty(type, type == typeof(Hashtable)
                ? "EqualityComparer" // Hashtable
                : "Comparer"); // Dictionary<TKey, TValue>, HashSet<T>, SortedSet<T>, SortedList<TKey, TValue>, SortedDictionary<TKey, TValue>, CircularSortedList<TKey, TValue>
            if (property != null)
                return property.Get(collection);

            // 2.) By *comparer* field
            return GetField(type, null, "comparer")?.Get(collection); // SortedList, ListDictionary, OrderedDictionary, ConcurrentDictionary
        }

        #endregion

        #region Comparer

        internal static CompareInfo? CompareInfo(this Comparer comparer)
            => GetFieldValueOrDefault<CompareInfo?>(comparer);

        #endregion

        #region BitArray

        internal static int[] GetUnderlyingArray(this BitArray bitArray)
        {
            int[]? result = GetFieldValueOrDefault<int[]?>(bitArray);
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

        internal static void SetKeyValue(object instance, object? key, object? value)
        {
            // Though DictionaryEntry.Key/Value have setters they must be set by reflection because of the boxed struct
            if (instance is DictionaryEntry)
            {
                Type type = instance.GetType();
#if NETSTANDARD2_0
                ((PropertyInfo)GetProperty(type, nameof(DictionaryEntry.Key))!.MemberInfo).SetValue(instance, key);
                ((PropertyInfo)GetProperty(type, nameof(DictionaryEntry.Value))!.MemberInfo).SetValue(instance, value);
#else
                GetProperty(type, nameof(DictionaryEntry.Key))!.Set(instance, key);
                GetProperty(type, nameof(DictionaryEntry.Value))!.Set(instance, value);
#endif
                return;
            }

            Debug.Assert(instance.GetType().IsGenericTypeOf(Reflector.KeyValuePairType));
            SetFieldValue(instance, "key", key);
            SetFieldValue(instance, "value", value);
        }

        #endregion

        #region Type
#if NETFRAMEWORK

        internal static bool IsSzArray(this Type type) => (bool)GetPropertyValue(type, nameof(IsSzArray))!;

#endif
        #endregion

        #region SerializationInfo

        internal static IFormatterConverter GetConverter(this SerializationInfo info)
            => GetFieldValueOrDefault<IFormatterConverter>(info, () => new FormatterConverter());

        #endregion

        #endregion

        #region Any Member
        // Note: These methods could be completely replaced by Reflector methods but these use a smaller and more direct cache

        internal static FieldInfo GetFieldInfo(this Type type, string fieldNamePattern)
        {
            FieldInfo? field = (FieldInfo?)GetField(type, null, fieldNamePattern)?.MemberInfo;
            if (field == null)
                Throw.InvalidOperationException(Res.ReflectionInstanceFieldDoesNotExist(fieldNamePattern, type));
            return field;
        }

        internal static object? GetPropertyValue(this Type genTypeDef, Type t, string propertyName)
        {
            Type type = genTypeDef.GetGenericType(t);
            PropertyAccessor? property = GetProperty(type, propertyName);
            if (property == null)
                Throw.InvalidOperationException(Res.ReflectionStaticPropertyDoesNotExist(propertyName, genTypeDef));
            return property.Get(null);
        }

        internal static object? GetPropertyValue(object instance, string propertyName)
        {
            PropertyAccessor? property = GetProperty(instance.GetType(), propertyName);
            if (property == null)
                Throw.InvalidOperationException(Res.ReflectionInstancePropertyDoesNotExist(propertyName, instance.GetType()));
            return property.Get(instance);
        }

        /// <summary>
        /// Invokes a constructor on an already created instance.
        /// </summary>
        internal static bool TryInvokeCtor(object instance, params object?[] ctorArgs)
        {
            Debug.Assert(ctorArgs.Length <= 2, "Constructors with more than 2 arguments are not cached. Use MethodBase.Invoke instead.");
            ActionMethodAccessor? accessor = GetCtorMethod(instance.GetType(), ctorArgs);
            if (accessor == null)
                return false;

#if NETSTANDARD2_0
            ((MethodBase)accessor.MemberInfo).Invoke(instance, ctorArgs);
            return true;
#else
            accessor.Invoke(instance, ctorArgs);
            return true;
#endif
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static object? Get(this FieldInfo field, object? instance)
        {
            if (field.FieldType.IsPointer)
            {
#if NETFRAMEWORK && !NET35
                if (EnvironmentHelper.IsPartiallyTrustedDomain)
                    return GetPointerPartiallyTrusted(field, instance);
#endif
                return GetPointer(field, instance);
            }

            return FieldAccessor.GetAccessor(field).Get(instance);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static void Set(this FieldInfo field, object? instance, object? value)
        {
            Debug.Assert(!field.IsLiteral);

            if (field.FieldType.IsPointer)
            {
                SetPointer(field, instance, value);
                return;
            }

#if NETSTANDARD2_0
            if (field.IsInitOnly || !field.IsStatic && field.DeclaringType?.IsValueType == true)
            {
                field.SetValue(instance, value);
                return;
            }
#endif

            FieldAccessor.GetAccessor(field).Set(instance, value);
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static unsafe object? Get(this PropertyInfo property, object? instance)
        {
            Debug.Assert(property.CanRead);

            if (property.PropertyType.IsPointer)
                return new IntPtr(Pointer.Unbox((Pointer)property.GetValue(instance, null)!));

#if NETSTANDARD2_0
            if (!property.GetGetMethod(true).IsStatic && property.DeclaringType?.IsValueType == true)
                return property.GetValue(instance);
#endif
            return PropertyAccessor.GetAccessor(property).Get(instance);
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static unsafe void Set(this PropertyInfo property, object? instance, object? value, params object?[] indexerParams)
        {
            Debug.Assert(property.CanWrite);

            if (property.PropertyType.IsPointer)
            {
                property.SetValue(instance, Pointer.Box(((IntPtr)value!).ToPointer(), property.PropertyType), null);
                return;
            }

#if NETSTANDARD2_0
            if (!property.GetSetMethod(true).IsStatic && property.DeclaringType?.IsValueType == true)
            {
                property.SetValue(instance, value, indexerParams);
                return;
            }
#endif

            PropertyAccessor.GetAccessor(property).Set(instance, value, indexerParams);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static object? Invoke(this MethodInfo method, object? instance, params object?[] parameters)
        {
#if NETSTANDARD2_0
            if (!method.IsStatic && method.DeclaringType?.IsValueType == true)
                return method.Invoke(instance, parameters);
#endif

            return MethodAccessor.GetAccessor(method).Invoke(instance, parameters);
        }

        [SecuritySafeCritical]
        private static unsafe object GetPointer(FieldInfo field, object? instance) => new IntPtr(Pointer.Unbox((Pointer)field.GetValue(instance)!));

#if NETFRAMEWORK && !NET35
        private static object? GetPointerPartiallyTrusted(FieldInfo field, object? instance) => Pointer_GetPointerValue?.Invoke((Pointer)field.GetValue(instance));
#endif

        [SecuritySafeCritical]
        private static unsafe void SetPointer(FieldInfo field, object? instance, object? value) => field.SetValue(instance, Pointer.Box(((IntPtr)value!).ToPointer(), field.FieldType));

        #endregion

        #endregion

        #endregion
    }
}
