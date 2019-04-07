﻿#region Copyright

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
using System.Reflection;
using System.Resources;
using System.Text;
using System.Xml;

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

        #region Field accessors

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
        private static FieldAccessor fieldResXFileRef_fileName;
        private static FieldAccessor fieldResXFileRef_typeName;
        private static FieldAccessor fieldResXFileRef_textFileEncoding;
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

        #endregion

        #region Property accessors

        private static PropertyAccessor propertyPoint_X;
        private static PropertyAccessor propertyPoint_Y;

        #endregion

        #region Method accessors

#if NET35 || NET40
        private static ActionMethodAccessor methodException_InternalPreserveStackTrace;
#endif

#if NET35 || NET40 || NET45
        private static IDictionary<Type, ActionMethodAccessor> methodsHashSet_Initialize;
#else
#error make sure not to use this from NET472, where capacity ctor is available
#endif

        private static IDictionary<Type, ActionMethodAccessor> methodsCollectionExtensions_AddRange;
        private static IDictionary<Type, ActionMethodAccessor> methodsListExtensions_InsertRange;
        private static IDictionary<Type, ActionMethodAccessor> methodsListExtensions_RemoveRange;
        private static IDictionary<Type, ActionMethodAccessor> methodsListExtensions_ReplaceRange;

        #endregion

        #region MethodInfos

        private static MethodInfo addRangeExtensionMethod;
        private static MethodInfo insertRangeExtensionMethod;
        private static MethodInfo removeRangeExtensionMethod;
        private static MethodInfo replaceRangeExtensionMethod;

        #endregion

        #endregion

        #region Properties - for accessors of types of referenced assemblies

        #region Field accessors

#if NET35 || NET40
        internal static FieldAccessor Exception_source
        {
            get
            {
                if (fieldException_source != null)
                    return fieldException_source;

                return fieldException_source = FieldAccessor.CreateAccessor(typeof(Exception).GetField("_source", BindingFlags.Instance | BindingFlags.NonPublic));
            }
        }

        internal static FieldAccessor Exception_remoteStackTraceString
        {
            get
            {
                if (fieldException_remoteStackTraceString != null)
                    return fieldException_remoteStackTraceString;

                return fieldException_remoteStackTraceString = FieldAccessor.CreateAccessor(typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic));
            }
        }
#endif

        internal static FieldAccessor ResourceManager_neutralResourcesCulture
        {
            get
            {
                if (fieldResourceManager_neutralResourcesCulture != null)
                    return fieldResourceManager_neutralResourcesCulture;

                fieldResourceManager_neutralResourcesCulture = FieldAccessor.GetAccessor(typeof(ResourceManager).GetField("_neutralResourcesCulture", BindingFlags.Instance | BindingFlags.NonPublic));
                return fieldResourceManager_neutralResourcesCulture;
            }
        }

#if NET40 || NET45
        internal static FieldAccessor ResourceManager_resourceSets
        {
            get
            {
                if (fieldResourceManager_resourceSets != null)
                    return fieldResourceManager_resourceSets;

                fieldResourceManager_resourceSets = FieldAccessor.GetAccessor(typeof(ResourceManager).GetField("_resourceSets", BindingFlags.Instance | BindingFlags.NonPublic));
                return fieldResourceManager_resourceSets;
            }
        }

#elif !NET35
#error .NET version is not set or not supported!
#endif

        internal static FieldAccessor XmlException_lineNumber
        {
            get
            {
                if (fieldXmlException_lineNumber != null)
                    return fieldXmlException_lineNumber;

                return fieldXmlException_lineNumber = FieldAccessor.CreateAccessor(typeof(XmlException).GetField("lineNumber", BindingFlags.Instance | BindingFlags.NonPublic));
            }
        }

        internal static FieldAccessor XmlException_linePosition
        {
            get
            {
                if (fieldXmlException_linePosition != null)
                    return fieldXmlException_linePosition;

                return fieldXmlException_linePosition = FieldAccessor.CreateAccessor(typeof(XmlException).GetField("linePosition", BindingFlags.Instance | BindingFlags.NonPublic));
            }
        }

        #endregion

        #endregion

        #region Methods - for accessors of types of referenced assemblies

#if NET35 || NET40
        internal static void InternalPreserveStackTrace(this Exception exception)
        {
            if (methodException_InternalPreserveStackTrace == null)
                methodException_InternalPreserveStackTrace = new ActionMethodAccessor(typeof(Exception).GetMethod(nameof(InternalPreserveStackTrace), BindingFlags.Instance | BindingFlags.NonPublic));
            methodException_InternalPreserveStackTrace.Invoke(exception);
        }
#endif

#if NET35 || NET40 || NET45

        internal static void Initialize<T>(this HashSet<T> hashSet, int capacity)
        {
            if (methodsHashSet_Initialize == null)
                methodsHashSet_Initialize = new Dictionary<Type, ActionMethodAccessor>().AsThreadSafe();
            if (!methodsHashSet_Initialize.TryGetValue(typeof(T), out ActionMethodAccessor invoker))
            {
                invoker = new ActionMethodAccessor(hashSet.GetType().GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic));
                methodsHashSet_Initialize[typeof(T)] = invoker;
            }

            invoker.Invoke(hashSet, capacity);
        }

#else
#error make sure not to use this from NET472, where capacity ctor is available
#endif

        internal static MethodAccessor CollectionExtensions_AddRange(Type genericArgument)
        {
            // Could be an IEnumerable extension but caller needs to check the method before executing
            if (methodsCollectionExtensions_AddRange == null)
                methodsCollectionExtensions_AddRange = new LockingDictionary<Type, ActionMethodAccessor>();
            if (!methodsCollectionExtensions_AddRange.TryGetValue(genericArgument, out ActionMethodAccessor accessor))
            {
                // ReSharper disable once PossibleNullReferenceException - will not be null, it exists (ensured by nameof)
                accessor = new ActionMethodAccessor(addRangeExtensionMethod ?? (addRangeExtensionMethod = typeof(CollectionExtensions).GetMethod(nameof(CollectionExtensions.AddRange))).MakeGenericMethod(genericArgument));
                methodsCollectionExtensions_AddRange[genericArgument] = accessor;
            }

            return accessor;
        }

        internal static MethodAccessor ListExtensions_InsertRange(Type genericArgument)
        {
            // Could be an IEnumerable extension but caller needs to check the method before executing
            if (methodsListExtensions_InsertRange == null)
                methodsListExtensions_InsertRange = new LockingDictionary<Type, ActionMethodAccessor>();
            if (!methodsListExtensions_InsertRange.TryGetValue(genericArgument, out ActionMethodAccessor accessor))
            {
                // ReSharper disable once PossibleNullReferenceException - will not be null, it exists (ensured by nameof)
                accessor = new ActionMethodAccessor(insertRangeExtensionMethod ?? (insertRangeExtensionMethod = typeof(ListExtensions).GetMethod(nameof(ListExtensions.InsertRange))).MakeGenericMethod(genericArgument));
                methodsListExtensions_InsertRange[genericArgument] = accessor;
            }

            return accessor;
        }

        internal static void RemoveRange(this IEnumerable collection, Type genericArgument, int index, int count)
        {
            if (methodsListExtensions_RemoveRange == null)
                methodsListExtensions_RemoveRange = new LockingDictionary<Type, ActionMethodAccessor>();
            if (!methodsListExtensions_RemoveRange.TryGetValue(genericArgument, out ActionMethodAccessor accessor))
            {
                // ReSharper disable once PossibleNullReferenceException - will not be null, it exists (ensured by nameof)
                accessor = new ActionMethodAccessor(removeRangeExtensionMethod ?? (removeRangeExtensionMethod = typeof(ListExtensions).GetMethod(nameof(ListExtensions.RemoveRange))).MakeGenericMethod(genericArgument));
                methodsListExtensions_RemoveRange[genericArgument] = accessor;
            }

            accessor.Invoke(null, collection, index, count);
        }

        internal static MethodAccessor ListExtensions_ReplaceRange(Type genericArgument)
        {
            // Could be an IEnumerable extension but caller needs to check the method before executing
            if (methodsListExtensions_ReplaceRange == null)
                methodsListExtensions_ReplaceRange = new LockingDictionary<Type, ActionMethodAccessor>();
            if (!methodsListExtensions_ReplaceRange.TryGetValue(genericArgument, out ActionMethodAccessor accessor))
            {
                // ReSharper disable once PossibleNullReferenceException - will not be null, it exists (ensured by nameof)
                accessor = new ActionMethodAccessor(replaceRangeExtensionMethod ?? (replaceRangeExtensionMethod = typeof(ListExtensions).GetMethod(nameof(ListExtensions.ReplaceRange))).MakeGenericMethod(genericArgument));
                methodsListExtensions_ReplaceRange[genericArgument] = accessor;
            }

            return accessor;
        }

        #endregion

        #region Methods - for accessors of types of non-referenced assemblies

        #region Field accessors

        internal static string ResXFileRef_fileName_Get(object fileRef)
        {
            if (fieldResXFileRef_fileName == null)
                fieldResXFileRef_fileName = FieldAccessor.CreateAccessor(fileRef.GetType().GetField("fileName", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldResXFileRef_fileName.Get(fileRef);
        }

        internal static string ResXFileRef_typeName_Get(object fileRef)
        {
            if (fieldResXFileRef_typeName == null)
                fieldResXFileRef_typeName = FieldAccessor.CreateAccessor(fileRef.GetType().GetField("typeName", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldResXFileRef_typeName.Get(fileRef);
        }

        internal static Encoding ResXFileRef_textFileEncoding_Get(object fileRef)
        {
            if (fieldResXFileRef_textFileEncoding == null)
                fieldResXFileRef_textFileEncoding = FieldAccessor.CreateAccessor(fileRef.GetType().GetField("textFileEncoding", BindingFlags.Instance | BindingFlags.NonPublic));

            return (Encoding)fieldResXFileRef_textFileEncoding.Get(fileRef);
        }

        internal static object ResXDataNode_value_Get(object node)
        {
            if (fieldResXDataNode_value == null)
                fieldResXDataNode_value = FieldAccessor.CreateAccessor(node.GetType().GetField("value", BindingFlags.Instance | BindingFlags.NonPublic));

            return fieldResXDataNode_value.Get(node);
        }

        internal static string ResXDataNode_comment_Get(object node)
        {
            if (fieldResXDataNode_comment == null)
                fieldResXDataNode_comment = FieldAccessor.CreateAccessor(node.GetType().GetField("comment", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldResXDataNode_comment.Get(node);
        }

        internal static object ResXDataNode_fileRef_Get(object node)
        {
            if (fieldResXDataNode_fileRef == null)
                fieldResXDataNode_fileRef = FieldAccessor.CreateAccessor(node.GetType().GetField("fileRef", BindingFlags.Instance | BindingFlags.NonPublic));

            return fieldResXDataNode_fileRef.Get(node);
        }

        internal static object ResXDataNode_nodeInfo_Get(object node)
        {
            if (fieldResXDataNode_nodeInfo == null)
                fieldResXDataNode_nodeInfo = FieldAccessor.CreateAccessor(node.GetType().GetField("nodeInfo", BindingFlags.Instance | BindingFlags.NonPublic));

            return fieldResXDataNode_nodeInfo.Get(node);
        }

        internal static string DataNodeInfo_Name_Get(object nodeInfo)
        {
            if (fieldDataNodeInfo_Name == null)
                fieldDataNodeInfo_Name = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("Name", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldDataNodeInfo_Name.Get(nodeInfo);
        }

        internal static string DataNodeInfo_Comment_Get(object nodeInfo)
        {
            if (fieldDataNodeInfo_Comment == null)
                fieldDataNodeInfo_Comment = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("Comment", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldDataNodeInfo_Comment.Get(nodeInfo);
        }

        internal static string DataNodeInfo_TypeName_Get(object nodeInfo)
        {
            if (fieldDataNodeInfo_TypeName == null)
                fieldDataNodeInfo_TypeName = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("TypeName", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldDataNodeInfo_TypeName.Get(nodeInfo);
        }

        internal static string DataNodeInfo_MimeType_Get(object nodeInfo)
        {
            if (fieldDataNodeInfo_MimeType == null)
                fieldDataNodeInfo_MimeType = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("MimeType", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldDataNodeInfo_MimeType.Get(nodeInfo);
        }

        internal static string DataNodeInfo_ValueData_Get(object nodeInfo)
        {
            if (fieldDataNodeInfo_ValueData == null)
                fieldDataNodeInfo_ValueData = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("ValueData", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldDataNodeInfo_ValueData.Get(nodeInfo);
        }

        internal static object DataNodeInfo_ReaderPosition_Get(object nodeInfo)
        {
            if (fieldDataNodeInfo_ReaderPosition == null)
                fieldDataNodeInfo_ReaderPosition = FieldAccessor.CreateAccessor(nodeInfo.GetType().GetField("ReaderPosition", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldDataNodeInfo_ReaderPosition.Get(nodeInfo);
        }

        #endregion

        #region Property accessors

        internal static int Point_X_Get(object point)
        {
            if (propertyPoint_X == null)
                propertyPoint_X = PropertyAccessor.CreateAccessor(point.GetType().GetProperty("X"));

            return (int)propertyPoint_X.Get(point);
        }

        internal static int Point_Y_Get(object point)
        {
            if (propertyPoint_Y == null)
                propertyPoint_Y = PropertyAccessor.CreateAccessor(point.GetType().GetProperty("Y"));

            return (int)propertyPoint_Y.Get(point);
        }

        #endregion

        #endregion
    }
}
