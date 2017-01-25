using System;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Xml;

// ReSharper disable InconsistentNaming - Properties are named here: Type_Member. Fields: accessorType_Member
namespace KGySoft.Libraries.Reflection
{
    /// <summary>
    /// Contains lazy initialized well-known accessors used in the project.
    /// </summary>
    internal static class Accessors
    {
#if NET40
        // needed only when compiled against .NET 4.0 because when 4.5 is installed, it replaces original 4.0 dlls
        private static bool isNet45;

        internal static bool IsNet45
        {
            get
            {
                if (RuntimeMethodHandle_InvokeMethod == null)
                    return false;
                return isNet45;
            }
        }

#elif !(NET35 || NET45)
#error .NET version is not set or not supported!
#endif

        #region Fields

        #region Field accessors

        private static FieldAccessor fieldException_remoteStackTraceString;
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

        private static PropertyAccessor propertyRuntimeConstructorInfo_Signature;
        private static PropertyAccessor propertyPoint_X;
        private static PropertyAccessor propertyPoint_Y;

        #endregion
        
        #region Method accessors

        private static MethodInvoker methodRuntimeMethodHandle_InvokeMethod;

        #endregion
        
        #endregion

        #region Properties - for accessors of types of referenced assemblies

        #region Field accessors

        internal static FieldAccessor Exception_remoteStackTraceString
        {
            get
            {
                if (fieldException_remoteStackTraceString != null)
                    return fieldException_remoteStackTraceString;

                return fieldException_remoteStackTraceString = FieldAccessor.CreateFieldAccessor(typeof(Exception).GetField("_remoteStackTraceString", BindingFlags.Instance | BindingFlags.NonPublic));
            }
        }

        internal static FieldAccessor ResourceManager_neutralResourcesCulture
        {
            get
            {
                if (fieldResourceManager_neutralResourcesCulture != null)
                    return fieldResourceManager_neutralResourcesCulture;

                fieldResourceManager_neutralResourcesCulture = FieldAccessor.GetFieldAccessor(typeof(ResourceManager).GetField("_neutralResourcesCulture", BindingFlags.Instance | BindingFlags.NonPublic));
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

                fieldResourceManager_resourceSets = FieldAccessor.GetFieldAccessor(typeof(ResourceManager).GetField("_resourceSets", BindingFlags.Instance | BindingFlags.NonPublic));
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

                return fieldXmlException_lineNumber = FieldAccessor.CreateFieldAccessor(typeof(XmlException).GetField("lineNumber", BindingFlags.Instance | BindingFlags.NonPublic));
            }
        }

        internal static FieldAccessor XmlException_linePosition
        {
            get
            {
                if (fieldXmlException_linePosition != null)
                    return fieldXmlException_linePosition;

                return fieldXmlException_linePosition = FieldAccessor.CreateFieldAccessor(typeof(XmlException).GetField("linePosition", BindingFlags.Instance | BindingFlags.NonPublic));
            }
        }

        #endregion

        #region Property accessors

        internal static PropertyAccessor RuntimeConstructorInfo_Signature
        {
            get
            {
                if (propertyRuntimeConstructorInfo_Signature == null)
                {
                    // retrieving a RuntimeConstructorInfo instance: Object..ctor()
                    ConstructorInfo ctor = Reflector.ObjectType.GetConstructor(Type.EmptyTypes);

                    // ReSharper disable once PossibleNullReferenceException
                    PropertyInfo pi = ctor.GetType().GetProperty("Signature", BindingFlags.Instance | BindingFlags.NonPublic);
                    propertyRuntimeConstructorInfo_Signature = PropertyAccessor.CreatePropertyAccessor(pi);
                }

                return propertyRuntimeConstructorInfo_Signature;
            }
        }

        #endregion

        #region Method accessors

        internal static MethodInvoker RuntimeMethodHandle_InvokeMethod
        {
            get
            {
                if (methodRuntimeMethodHandle_InvokeMethod == null)
                {
#if NET35
                    MethodInfo mi = typeof(RuntimeMethodHandle).GetMethod("InvokeMethodFast", BindingFlags.NonPublic | BindingFlags.Instance);
#elif NET40
                    // .NET 4.0 and 4.5 cannot be differentiated (both have 4.0.0.0 mscorlib version) so probing
                    BindingFlags bf = BindingFlags.NonPublic | BindingFlags.Static;
                    MethodInfo mi = typeof(RuntimeMethodHandle).GetMethod("InvokeMethod", bf);
                    isNet45 = mi != null;
                    if (!isNet45)
                        mi = typeof(RuntimeMethodHandle).GetMethod("InvokeMethodFast", bf);
#elif NET45
                    MethodInfo mi = typeof(RuntimeMethodHandle).GetMethod("InvokeMethod", BindingFlags.NonPublic | BindingFlags.Static);
#else
#error .NET version is not set or not supported!
#endif
                    // in .NET 3.5 and 4.0 it will be ActionInvoker, otherwise FunctionInvoker
                    methodRuntimeMethodHandle_InvokeMethod = MethodInvoker.CreateMethodInvoker(mi);
                }

                return methodRuntimeMethodHandle_InvokeMethod;
            }
        }

        #endregion
        
        #endregion

        #region Methods - for accessors of types of non-referenced assemblies

        #region Field accessors

        internal static string ResXFileRef_fileName_Get(object fileRef)
        {
            if (fieldResXFileRef_fileName == null)
                fieldResXFileRef_fileName = FieldAccessor.CreateFieldAccessor(fileRef.GetType().GetField("fileName", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldResXFileRef_fileName.Get(fileRef);
        }

        internal static string ResXFileRef_typeName_Get(object fileRef)
        {
            if (fieldResXFileRef_typeName == null)
                fieldResXFileRef_typeName = FieldAccessor.CreateFieldAccessor(fileRef.GetType().GetField("typeName", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldResXFileRef_typeName.Get(fileRef);
        }

        internal static Encoding ResXFileRef_textFileEncoding_Get(object fileRef)
        {
            if (fieldResXFileRef_textFileEncoding == null)
                fieldResXFileRef_textFileEncoding = FieldAccessor.CreateFieldAccessor(fileRef.GetType().GetField("textFileEncoding", BindingFlags.Instance | BindingFlags.NonPublic));

            return (Encoding)fieldResXFileRef_textFileEncoding.Get(fileRef);
        }

        internal static object ResXDataNode_value_Get(object node)
        {
            if (fieldResXDataNode_value == null)
                fieldResXDataNode_value = FieldAccessor.CreateFieldAccessor(node.GetType().GetField("value", BindingFlags.Instance | BindingFlags.NonPublic));

            return fieldResXDataNode_value.Get(node);
        }

        internal static string ResXDataNode_comment_Get(object node)
        {
            if (fieldResXDataNode_comment == null)
                fieldResXDataNode_comment = FieldAccessor.CreateFieldAccessor(node.GetType().GetField("comment", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldResXDataNode_comment.Get(node);
        }

        internal static object ResXDataNode_fileRef_Get(object node)
        {
            if (fieldResXDataNode_fileRef == null)
                fieldResXDataNode_fileRef = FieldAccessor.CreateFieldAccessor(node.GetType().GetField("fileRef", BindingFlags.Instance | BindingFlags.NonPublic));

            return fieldResXDataNode_fileRef.Get(node);
        }

        internal static object ResXDataNode_nodeInfo_Get(object node)
        {
            if (fieldResXDataNode_nodeInfo == null)
                fieldResXDataNode_nodeInfo = FieldAccessor.CreateFieldAccessor(node.GetType().GetField("nodeInfo", BindingFlags.Instance | BindingFlags.NonPublic));

            return fieldResXDataNode_nodeInfo.Get(node);
        }

        internal static string DataNodeInfo_Name_Get(object nodeInfo)
        {
            if (fieldDataNodeInfo_Name == null)
                fieldDataNodeInfo_Name = FieldAccessor.CreateFieldAccessor(nodeInfo.GetType().GetField("Name", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldDataNodeInfo_Name.Get(nodeInfo);
        }

        internal static string DataNodeInfo_Comment_Get(object nodeInfo)
        {
            if (fieldDataNodeInfo_Comment == null)
                fieldDataNodeInfo_Comment = FieldAccessor.CreateFieldAccessor(nodeInfo.GetType().GetField("Comment", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldDataNodeInfo_Comment.Get(nodeInfo);
        }

        internal static string DataNodeInfo_TypeName_Get(object nodeInfo)
        {
            if (fieldDataNodeInfo_TypeName == null)
                fieldDataNodeInfo_TypeName = FieldAccessor.CreateFieldAccessor(nodeInfo.GetType().GetField("TypeName", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldDataNodeInfo_TypeName.Get(nodeInfo);
        }

        internal static string DataNodeInfo_MimeType_Get(object nodeInfo)
        {
            if (fieldDataNodeInfo_MimeType == null)
                fieldDataNodeInfo_MimeType = FieldAccessor.CreateFieldAccessor(nodeInfo.GetType().GetField("MimeType", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldDataNodeInfo_MimeType.Get(nodeInfo);
        }

        internal static string DataNodeInfo_ValueData_Get(object nodeInfo)
        {
            if (fieldDataNodeInfo_ValueData == null)
                fieldDataNodeInfo_ValueData = FieldAccessor.CreateFieldAccessor(nodeInfo.GetType().GetField("ValueData", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldDataNodeInfo_ValueData.Get(nodeInfo);
        }

        internal static object DataNodeInfo_ReaderPosition_Get(object nodeInfo)
        {
            if (fieldDataNodeInfo_ReaderPosition == null)
                fieldDataNodeInfo_ReaderPosition = FieldAccessor.CreateFieldAccessor(nodeInfo.GetType().GetField("ReaderPosition", BindingFlags.Instance | BindingFlags.NonPublic));

            return (string)fieldDataNodeInfo_ReaderPosition.Get(nodeInfo);
        }

        #endregion

        #region Property accessors

        internal static int Point_X_Get(object point)
        {
            if (propertyPoint_X == null)
                propertyPoint_X = PropertyAccessor.CreatePropertyAccessor(point.GetType().GetProperty("X"));

            return (int)propertyPoint_X.Get(point);
        }

        internal static int Point_Y_Get(object point)
        {
            if (propertyPoint_Y == null)
                propertyPoint_Y = PropertyAccessor.CreatePropertyAccessor(point.GetType().GetProperty("Y"));

            return (int)propertyPoint_Y.Get(point);
        }

        #endregion

        #endregion
    }
}
