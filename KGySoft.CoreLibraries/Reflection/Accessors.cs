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
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Xml;
using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.CoreLibraries;

#if NETCOREAPP2_0
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

        #region ResourceManager

        private static FieldAccessor fieldResourceManager_neutralResourcesCulture;

#if NET40 || NET45 || NETCOREAPP2_0
        private static FieldAccessor fieldResourceManager_resourceSets;
#endif

        #endregion

        #region XmlException

        private static FieldAccessor fieldXmlException_lineNumber;
        private static FieldAccessor fieldXmlException_linePosition;

        #endregion

        #region ResXFileRef

#if NET35 || NET40 || NET45
        private static PropertyAccessor propertyResXFileRef_FileName;
        private static PropertyAccessor propertyResXFileRef_TypeName;
        private static PropertyAccessor propertyResXFileRef_TextFileEncoding;
#endif

        #endregion

        #region ResXDataNode

#if NET35 || NET40 || NET45
        private static FieldAccessor fieldResXDataNode_value;
        private static FieldAccessor fieldResXDataNode_comment;
        private static FieldAccessor fieldResXDataNode_fileRef;
        private static FieldAccessor fieldResXDataNode_nodeInfo;
#endif

        #endregion

        #region DataNodeInfo

#if NET35 || NET40 || NET45
        private static FieldAccessor fieldDataNodeInfo_Name;
        private static FieldAccessor fieldDataNodeInfo_Comment;
        private static FieldAccessor fieldDataNodeInfo_TypeName;
        private static FieldAccessor fieldDataNodeInfo_MimeType;
        private static FieldAccessor fieldDataNodeInfo_ValueData;
        private static FieldAccessor fieldDataNodeInfo_ReaderPosition;
#endif

        #endregion

        #region Point

#if NET35 || NET40 || NET45
        private static PropertyAccessor propertyPoint_X;
        private static PropertyAccessor propertyPoint_Y;
#endif

        #endregion

        #region RuntimeConstructorInfo

#if NET35 || NET40 || NET45
        private static ActionMethodAccessor methodRuntimeConstructorInfo_SerializationInvoke;
#endif

        #endregion

        #region MemoryStream

        private static FunctionMethodAccessor methodMemoryStream_InternalGetBuffer;

        #endregion

        #region UnmanagedMemoryStreamWrapper

        private static ParameterizedCreateInstanceAccessor ctorUnmanagedMemoryStreamWrapper;

        #endregion

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
        // Make sure every member in this region is in conditions. Provide an #else #error for open-ended versions where the member has to be checked for new target frameworks.

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

        #region ResourceManager

#if NET35 || NET40 || NET45 || NETCOREAPP2_0
        private static FieldAccessor ResourceManager_neutralResourcesCulture => fieldResourceManager_neutralResourcesCulture ?? (fieldResourceManager_neutralResourcesCulture = FieldAccessor.GetAccessor(typeof(ResourceManager).GetField("_neutralResourcesCulture", BindingFlags.Instance | BindingFlags.NonPublic)));
#else
#error .NET version is not set or not supported!
#endif

#if NET40 || NET45 || NETCOREAPP2_0
        private static FieldAccessor ResourceManager_resourceSets => fieldResourceManager_resourceSets ?? (fieldResourceManager_resourceSets = FieldAccessor.GetAccessor(typeof(ResourceManager).GetField("_resourceSets", BindingFlags.Instance | BindingFlags.NonPublic)));
#elif !NET35
#error .NET version is not set or not supported!
#endif

        #endregion

        #region XmlException

#if NET35 || NET40 || NET45 || NETCOREAPP2_0
        private static FieldAccessor XmlException_lineNumber => fieldXmlException_lineNumber ?? (fieldXmlException_lineNumber = FieldAccessor.CreateAccessor(typeof(XmlException).GetField("lineNumber", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor XmlException_linePosition => fieldXmlException_linePosition ?? (fieldXmlException_linePosition = FieldAccessor.CreateAccessor(typeof(XmlException).GetField("linePosition", BindingFlags.Instance | BindingFlags.NonPublic)));
#else
#error .NET version is not set or not supported!
#endif

        #endregion

        #region ResXFileRef
        // though we access only public ResXFileRef properties we treat it as it wasn't public because we need to check every added frameworks whether we can use this type

#if NET35 || NET40 || NET45
        private static PropertyAccessor ResXFileRef_FileName(object fileRef) => propertyResXFileRef_FileName ?? (propertyResXFileRef_FileName = PropertyAccessor.CreateAccessor(fileRef.GetType().GetProperty("FileName", BindingFlags.Instance | BindingFlags.Public)));
        private static PropertyAccessor ResXFileRef_TypeName(object fileRef) => propertyResXFileRef_TypeName ?? (propertyResXFileRef_TypeName = PropertyAccessor.CreateAccessor(fileRef.GetType().GetProperty("TypeName", BindingFlags.Instance | BindingFlags.Public)));
        private static PropertyAccessor ResXFileRef_TextFileEncoding(object fileRef) => propertyResXFileRef_TextFileEncoding ?? (propertyResXFileRef_TextFileEncoding = PropertyAccessor.CreateAccessor(fileRef.GetType().GetProperty("TextFileEncoding", BindingFlags.Instance | BindingFlags.Public)));
#elif !NETCOREAPP2_0 // No WinForms version has to be supported in .NET Core 2.0
#error .NET version is not set or not supported!
#endif

        #endregion

        #region ResXDataNode
        // Note: some of these are available as public properties but they must be accessed as fields because property getters alter the real values

#if NET35 || NET40 || NET45
        private static FieldAccessor ResXDataNode_value(object node) => fieldResXDataNode_value ?? (fieldResXDataNode_value = FieldAccessor.CreateAccessor(node.GetType().GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor ResXDataNode_comment(object node) => fieldResXDataNode_comment ?? (fieldResXDataNode_comment = FieldAccessor.CreateAccessor(node.GetType().GetField("comment", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor ResXDataNode_fileRef(object node) => fieldResXDataNode_fileRef ?? (fieldResXDataNode_fileRef = FieldAccessor.CreateAccessor(node.GetType().GetField("fileRef", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor ResXDataNode_nodeInfo(object node) => fieldResXDataNode_nodeInfo ?? (fieldResXDataNode_nodeInfo = FieldAccessor.CreateAccessor(node.GetType().GetField("nodeInfo", BindingFlags.Instance | BindingFlags.NonPublic)));
#elif !NETCOREAPP2_0 // No WinForms version has to be supported in .NET Core 2.0
#error .NET version is not set or not supported!
#endif

        #endregion

        #region DataNodeInfo

#if NET35 || NET40 || NET45
        private static FieldAccessor DataNodeInfo_Name(object nodeInfo) => fieldDataNodeInfo_Name ?? (fieldDataNodeInfo_Name = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("Name", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor DataNodeInfo_Comment(object nodeInfo) => fieldDataNodeInfo_Comment ?? (fieldDataNodeInfo_Comment = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("Comment", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor DataNodeInfo_TypeName(object nodeInfo) => fieldDataNodeInfo_TypeName ?? (fieldDataNodeInfo_TypeName = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("TypeName", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor DataNodeInfo_MimeType(object nodeInfo) => fieldDataNodeInfo_MimeType ?? (fieldDataNodeInfo_MimeType = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("MimeType", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor DataNodeInfo_ValueData(object nodeInfo) => fieldDataNodeInfo_ValueData ?? (fieldDataNodeInfo_ValueData = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("ValueData", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor DataNodeInfo_ReaderPosition(object nodeInfo) => fieldDataNodeInfo_ReaderPosition ?? (fieldDataNodeInfo_ReaderPosition = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("ReaderPosition", BindingFlags.Instance | BindingFlags.NonPublic)));
#elif !NETCOREAPP2_0 // No WinForms version has to be supported in .NET Core 2.0
#error .NET version is not set or not supported!
#endif

        #endregion

        #region Point
        // Since used only for DataNodeInfo.ReaderPosition the same applies for it

#if NET35 || NET40 || NET45
        private static PropertyAccessor Point_X(object point) => propertyPoint_X ?? (propertyPoint_X = PropertyAccessor.CreateAccessor(point.GetType().GetProperty("X")));
        private static PropertyAccessor Point_Y(object point) => propertyPoint_Y ?? (propertyPoint_Y = PropertyAccessor.CreateAccessor(point.GetType().GetProperty("Y")));
#elif !NETCOREAPP2_0 
#error .NET version is not set or not supported!
#endif

        #endregion

        #region RuntimeConstructorInfo

#if NET35 || NET40 || NET45
        private static ActionMethodAccessor RuntimeConstructorInfo_SerializationInvoke(ConstructorInfo ci) => methodRuntimeConstructorInfo_SerializationInvoke ?? (methodRuntimeConstructorInfo_SerializationInvoke = new ActionMethodAccessor(ci.GetType().GetMethod("SerializationInvoke", BindingFlags.Instance | BindingFlags.NonPublic)));
#elif !NETCOREAPP2_0 // There is no SerializationInvoke in .NET Core 2.0
#error .NET version is not set or not supported!
#endif

        #endregion

        #region MemoryStream

#if NET35 || NET40 || NET45 || NETCOREAPP2_0
        private static FunctionMethodAccessor MemoryStream_InternalGetBuffer => methodMemoryStream_InternalGetBuffer ?? (methodMemoryStream_InternalGetBuffer = new FunctionMethodAccessor(typeof(MemoryStream).GetMethod("InternalGetBuffer", BindingFlags.Instance | BindingFlags.NonPublic)));
#else
#error .NET version is not set or not supported!
#endif

        #endregion

        #region UnmanagedMemoryStreamWrapper

#if NET35 || NET40 || NET45 || NETCOREAPP2_0
        private static ParameterizedCreateInstanceAccessor UnmanagedMemoryStreamWrapper => ctorUnmanagedMemoryStreamWrapper ?? (ctorUnmanagedMemoryStreamWrapper = new ParameterizedCreateInstanceAccessor(Reflector.ResolveType("System.IO.UnmanagedMemoryStreamWrapper").GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(UnmanagedMemoryStream) }, null)));
#else
#error .NET version is not set or not supported!
#endif

        #endregion

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

        #region ResourceManager

        internal static CultureInfo GetNeutralResourcesCulture(this ResourceManager resourceManager) => (CultureInfo)ResourceManager_neutralResourcesCulture.Get(resourceManager);
        internal static void SetNeutralResourcesCulture(this ResourceManager resourceManager, CultureInfo ci) => ResourceManager_neutralResourcesCulture.Set(resourceManager, ci);

#if NET40 || NET45 || NETCOREAPP2_0
        internal static Dictionary<string, ResourceSet> GetResourceSets(this ResourceManager resourceManager) => (Dictionary<string, ResourceSet>)ResourceManager_resourceSets.Get(resourceManager);
        internal static void SetResourceSets(this ResourceManager resourceManager, Dictionary<string, ResourceSet> resourceSets) => ResourceManager_resourceSets.Set(resourceManager, resourceSets);
#endif

        #endregion

        #region XmlException

        internal static void SetLineNumber(this XmlException e, int lineNumber) => XmlException_lineNumber.Set(e, lineNumber);
        internal static void SetLinePosition(this XmlException e, int linePosition) => XmlException_linePosition.Set(e, linePosition);

        #endregion

        #region ResXFileRef

#if NET35 || NET40 || NET45
        internal static string ResXFileRef_GetFileName(object fileRef) => (string)ResXFileRef_FileName(fileRef).Get(fileRef);
        internal static string ResXFileRef_GetTypeName(object fileRef) => (string)ResXFileRef_TypeName(fileRef).Get(fileRef);
        internal static Encoding ResXFileRef_GetTextFileEncoding(object fileRef) => (Encoding)ResXFileRef_TextFileEncoding(fileRef).Get(fileRef);
#endif

        #endregion

        #region ResXDataNode

#if NET35 || NET40 || NET45
        internal static object ResXDataNode_GetValue(object node) => ResXDataNode_value(node).Get(node);
        internal static string ResXDataNode_GetComment(object node) => (string)ResXDataNode_comment(node).Get(node);
        internal static object ResXDataNode_GetFileRef(object node) => ResXDataNode_fileRef(node).Get(node);
        internal static object ResXDataNode_GetNodeInfo(object node) => ResXDataNode_nodeInfo(node).Get(node);
#endif

        #endregion

        #region DataNodeInfo

#if NET35 || NET40 || NET45
        internal static string DataNodeInfo_GetName(object nodeInfo) => (string)DataNodeInfo_Name(nodeInfo).Get(nodeInfo);
        internal static string DataNodeInfo_GetComment(object nodeInfo) => (string)DataNodeInfo_Comment(nodeInfo).Get(nodeInfo);
        internal static string DataNodeInfo_GetTypeName(object nodeInfo) => (string)DataNodeInfo_TypeName(nodeInfo).Get(nodeInfo);
        internal static string DataNodeInfo_GetMimeType(object nodeInfo) => (string)DataNodeInfo_MimeType(nodeInfo).Get(nodeInfo);
        internal static string DataNodeInfo_GetValueData(object nodeInfo) => (string)DataNodeInfo_ValueData(nodeInfo).Get(nodeInfo);
        internal static object DataNodeInfo_GetReaderPosition(object nodeInfo) => DataNodeInfo_ReaderPosition(nodeInfo).Get(nodeInfo);
#endif

        #endregion

        #region Point

#if NET35 || NET40 || NET45
        internal static int Point_GetX(object point) => (int)Point_X(point).Get(point);
        internal static int Point_GetY(object point) => (int)Point_Y(point).Get(point);
#endif

        #endregion

        #region RuntimeConstructorInfo

#if NET35 || NET40 || NET45
        internal static void SerializationInvoke(this ConstructorInfo ci, object target, SerializationInfo info, StreamingContext context) => RuntimeConstructorInfo_SerializationInvoke(ci).Invoke(ci, target, info, context);
#endif

        #endregion

        #region MemoryStream

        internal static byte[] InternalGetBuffer(this MemoryStream ms) => (byte[])MemoryStream_InternalGetBuffer.Invoke(ms);

        #endregion

        #region UnmanagedMemoryStreamWrapper

        internal static MemoryStream ToMemoryStream(this UnmanagedMemoryStream ums) => (MemoryStream)UnmanagedMemoryStreamWrapper.CreateInstance(ums);

        #endregion

        #endregion

        #endregion
    }
}
