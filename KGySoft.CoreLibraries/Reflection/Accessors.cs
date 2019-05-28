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
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Xml;
using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.CoreLibraries;

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

#if NET35 || NET40
        private static FieldAccessor fieldException_source;
        private static FieldAccessor fieldException_remoteStackTraceString;
#endif
        private static FieldAccessor fieldResourceManager_neutralResourcesCulture;
#if NET40 || NET45
        private static FieldAccessor fieldResourceManager_resourceSets;
#elif !NET35
#error .NET version is not set or not supported!
#endif
        private static FieldAccessor fieldResXDataNode_value;
        private static FieldAccessor fieldResXDataNode_comment;
        private static FieldAccessor fieldResXDataNode_fileRef;
        private static FieldAccessor fieldResXDataNode_nodeInfo;
        private static FieldAccessor fieldDataNodeInfo_Name;
        private static FieldAccessor fieldDataNodeInfo_Comment;
        private static FieldAccessor fieldDataNodeInfo_TypeName;
        private static FieldAccessor fieldDataNodeInfo_MimeType;
        private static FieldAccessor fieldDataNodeInfo_ValueData;
        private static FieldAccessor fieldDataNodeInfo_ReaderPosition;
        private static FieldAccessor fieldXmlException_lineNumber;
        private static FieldAccessor fieldXmlException_linePosition;

        private static PropertyAccessor propertyResXFileRef_FileName;
        private static PropertyAccessor propertyResXFileRef_TypeName;
        private static PropertyAccessor propertyResXFileRef_TextFileEncoding;
        private static PropertyAccessor propertyPoint_X;
        private static PropertyAccessor propertyPoint_Y;

#if NET35 || NET40
        private static ActionMethodAccessor methodException_InternalPreserveStackTrace;
#endif

#if NET35 || NET40 || NET45 // from .NET 4.72 capacity ctor is available
        private static IDictionary<Type, ActionMethodAccessor> methodsHashSet_Initialize;
#endif

        private static IDictionary<Type, ActionMethodAccessor> methodsCollectionExtensions_AddRange;
        private static IDictionary<Type, ActionMethodAccessor> methodsListExtensions_InsertRange;
        private static IDictionary<Type, ActionMethodAccessor> methodsListExtensions_RemoveRange;
        private static IDictionary<Type, ActionMethodAccessor> methodsListExtensions_ReplaceRange;

        private static MethodInfo addRangeExtensionMethod;
        private static MethodInfo insertRangeExtensionMethod;
        private static MethodInfo removeRangeExtensionMethod;
        private static MethodInfo replaceRangeExtensionMethod;

        #endregion

        #region Accessor Factory Properties and Methods

        #region For Non-Public Members
#if NET35 || NET40 || NET45 // Make sure this condition covers the whole region. Include all supported versions in the condition after checking the members.

        #region Exception

#if NET35 || NET40
        private static FieldAccessor Exception_source => fieldException_source ?? (fieldException_source = FieldAccessor.CreateAccessor(typeof(Exception).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor Exception_remoteStackTraceString => fieldException_remoteStackTraceString ?? (fieldException_remoteStackTraceString = FieldAccessor.CreateAccessor(typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static MethodAccessor Exception_InternalPreserveStackTrace => methodException_InternalPreserveStackTrace ?? (methodException_InternalPreserveStackTrace = new ActionMethodAccessor(typeof(Exception).GetMethod(nameof(InternalPreserveStackTrace), BindingFlags.Instance | BindingFlags.NonPublic)));
#endif

        #endregion

        #region HashSet<T>

#if NET35 || NET40 || NET45
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
#else
#error make sure not to use this from NET472, where capacity ctor is available
#endif

        #endregion

        #region ResourceManager

        private static FieldAccessor ResourceManager_neutralResourcesCulture => fieldResourceManager_neutralResourcesCulture ?? (fieldResourceManager_neutralResourcesCulture = FieldAccessor.GetAccessor(typeof(ResourceManager).GetField("_neutralResourcesCulture", BindingFlags.Instance | BindingFlags.NonPublic)));

#if NET40 || NET45
        private static FieldAccessor ResourceManager_resourceSets => fieldResourceManager_resourceSets ?? (fieldResourceManager_resourceSets = FieldAccessor.GetAccessor(typeof(ResourceManager).GetField("_resourceSets", BindingFlags.Instance | BindingFlags.NonPublic)));
#elif !NET35
#error .NET version is not set or not supported!
#endif

        #endregion

        #region XmlException

        private static FieldAccessor XmlException_lineNumber => fieldXmlException_lineNumber ?? (fieldXmlException_lineNumber = FieldAccessor.CreateAccessor(typeof(XmlException).GetField("lineNumber", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor XmlException_linePosition => fieldXmlException_linePosition ?? (fieldXmlException_linePosition = FieldAccessor.CreateAccessor(typeof(XmlException).GetField("linePosition", BindingFlags.Instance | BindingFlags.NonPublic)));

        #endregion

        #region ResXDataNode
        // Note: some of these are available as public properties but they must be accessed as fields because property getters alter the real values

        private static FieldAccessor ResXDataNode_value(object node) => fieldResXDataNode_value ?? (fieldResXDataNode_value = FieldAccessor.CreateAccessor(node.GetType().GetField("value", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor ResXDataNode_comment(object node) => fieldResXDataNode_comment ?? (fieldResXDataNode_comment = FieldAccessor.CreateAccessor(node.GetType().GetField("comment", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor ResXDataNode_fileRef(object node) => fieldResXDataNode_fileRef ?? (fieldResXDataNode_fileRef = FieldAccessor.CreateAccessor(node.GetType().GetField("fileRef", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor ResXDataNode_nodeInfo(object node) => fieldResXDataNode_nodeInfo ?? (fieldResXDataNode_nodeInfo = FieldAccessor.CreateAccessor(node.GetType().GetField("nodeInfo", BindingFlags.Instance | BindingFlags.NonPublic)));

        #endregion

        #region DataNodeInfo

        private static FieldAccessor DataNodeInfo_Name(object nodeInfo) => fieldDataNodeInfo_Name ?? (fieldDataNodeInfo_Name = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("Name", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor DataNodeInfo_Comment(object nodeInfo) => fieldDataNodeInfo_Comment ?? (fieldDataNodeInfo_Comment = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("Comment", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor DataNodeInfo_TypeName(object nodeInfo) => fieldDataNodeInfo_TypeName ?? (fieldDataNodeInfo_TypeName = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("TypeName", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor DataNodeInfo_MimeType(object nodeInfo) => fieldDataNodeInfo_MimeType ?? (fieldDataNodeInfo_MimeType = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("MimeType", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor DataNodeInfo_ValueData(object nodeInfo) => fieldDataNodeInfo_ValueData ?? (fieldDataNodeInfo_ValueData = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("ValueData", BindingFlags.Instance | BindingFlags.NonPublic)));
        private static FieldAccessor DataNodeInfo_ReaderPosition(object nodeInfo) => fieldDataNodeInfo_ReaderPosition ?? (fieldDataNodeInfo_ReaderPosition = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("ReaderPosition", BindingFlags.Instance | BindingFlags.NonPublic)));

        #endregion

#else
#error .NET version is not set or not supported! Check accessed non-public member names for the newly added .NET version.
#endif
        #endregion

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

        #region ResXFileRef

        private static PropertyAccessor ResXFileRef_FileName(object fileRef) => propertyResXFileRef_FileName ?? (propertyResXFileRef_FileName = PropertyAccessor.CreateAccessor(fileRef.GetType().GetProperty("FileName", BindingFlags.Instance | BindingFlags.Public)));
        private static PropertyAccessor ResXFileRef_TypeName(object fileRef) => propertyResXFileRef_TypeName ?? (propertyResXFileRef_TypeName = PropertyAccessor.CreateAccessor(fileRef.GetType().GetProperty("TypeName", BindingFlags.Instance | BindingFlags.Public)));
        private static PropertyAccessor ResXFileRef_TextFileEncoding(object fileRef) => propertyResXFileRef_TextFileEncoding ?? (propertyResXFileRef_TextFileEncoding = PropertyAccessor.CreateAccessor(fileRef.GetType().GetProperty("TextFileEncoding", BindingFlags.Instance | BindingFlags.Public)));

        #endregion

        #region Point

        private static PropertyAccessor Point_X(object point) => propertyPoint_X ?? (propertyPoint_X = PropertyAccessor.CreateAccessor(point.GetType().GetProperty("X")));
        private static PropertyAccessor Point_Y(object point) => propertyPoint_Y ?? (propertyPoint_Y = PropertyAccessor.CreateAccessor(point.GetType().GetProperty("Y")));

        #endregion

        #endregion

        #endregion

        #region Accessor Extension Methods

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
#else
#error make sure not to use this from NET472, where capacity ctor is available
#endif

        #endregion

        #region ResourceManager

        internal static CultureInfo GetNeutralResourcesCulture(this ResourceManager resourceManager) => (CultureInfo)ResourceManager_neutralResourcesCulture.Get(resourceManager);
        internal static void SetNeutralResourcesCulture(this ResourceManager resourceManager, CultureInfo ci) => ResourceManager_neutralResourcesCulture.Set(resourceManager, ci);
#if NET40 || NET45
        internal static Dictionary<string, ResourceSet> GetResourceSets(this ResourceManager resourceManager) => (Dictionary<string, ResourceSet>)ResourceManager_resourceSets.Get(resourceManager);
        internal static void SetResourceSets(this ResourceManager resourceManager, Dictionary<string, ResourceSet> resourceSets) => ResourceManager_resourceSets.Set(resourceManager, resourceSets);
#elif !NET35
#error .NET version is not set or not supported!
#endif

        #endregion

        #region XmlException

        internal static void SetLineNumber(this XmlException e, int lineNumber) => XmlException_lineNumber.Set(e, lineNumber);
        internal static void SetLinePosition(this XmlException e, int linePosition) => XmlException_linePosition.Set(e, linePosition);

        #endregion

        #region CollectionExtensions

        internal static void AddRange(this IEnumerable target, Type genericArgument, IEnumerable collection) => CollectionExtensions_AddRange(genericArgument).Invoke(null, target, collection);

        #endregion

        #region ListExtensions

        internal static void InsertRange(this IEnumerable target, Type genericArgument, int index, IEnumerable collection) => ListExtensions_InsertRange(genericArgument).Invoke(null, target, index, collection);
        internal static void RemoveRange(this IEnumerable collection, Type genericArgument, int index, int count) => ListExtensions_RemoveRange(genericArgument).Invoke(null, collection, index, count);
        internal static void ReplaceRange(this IEnumerable target, Type genericArgument, int index, int count, IEnumerable collection) => ListExtensions_ReplaceRange(genericArgument).Invoke(null, target, index, count, collection);

        #endregion

        #region ResXFileRef

        internal static string ResXFileRef_GetFileName(object fileRef) => (string)ResXFileRef_FileName(fileRef).Get(fileRef);
        internal static string ResXFileRef_GetTypeName(object fileRef) => (string)ResXFileRef_TypeName(fileRef).Get(fileRef);
        internal static Encoding ResXFileRef_GetTextFileEncoding(object fileRef) => (Encoding)ResXFileRef_TextFileEncoding(fileRef).Get(fileRef);

        #endregion

        #region ResXDataNode

        internal static object ResXDataNode_GetValue(object node) => ResXDataNode_value(node).Get(node);
        internal static string ResXDataNode_GetComment(object node) => (string)ResXDataNode_comment(node).Get(node);
        internal static object ResXDataNode_GetFileRef(object node) => ResXDataNode_fileRef(node).Get(node);
        internal static object ResXDataNode_GetNodeInfo(object node) => ResXDataNode_nodeInfo(node).Get(node);

        #endregion

        #region DataNodeInfo

        internal static string DataNodeInfo_GetName(object nodeInfo) => (string)DataNodeInfo_Name(nodeInfo).Get(nodeInfo);
        internal static string DataNodeInfo_GetComment(object nodeInfo) => (string)DataNodeInfo_Comment(nodeInfo).Get(nodeInfo);
        internal static string DataNodeInfo_GetTypeName(object nodeInfo) => (string)DataNodeInfo_TypeName(nodeInfo).Get(nodeInfo);
        internal static string DataNodeInfo_GetMimeType(object nodeInfo) => (string)DataNodeInfo_MimeType(nodeInfo).Get(nodeInfo);
        internal static string DataNodeInfo_GetValueData(object nodeInfo) => (string)DataNodeInfo_ValueData(nodeInfo).Get(nodeInfo);
        internal static object DataNodeInfo_GetReaderPosition(object nodeInfo) => DataNodeInfo_ReaderPosition(nodeInfo).Get(nodeInfo);

        #endregion

        #region Point

        internal static int Point_GetX(object point) => (int)Point_X(point).Get(point);
        internal static int Point_GetY(object point) => (int)Point_Y(point).Get(point);

        #endregion

        #endregion
    }
}
