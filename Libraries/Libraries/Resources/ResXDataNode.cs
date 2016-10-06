using System.Reflection.Emit;

namespace KGySoft.Libraries.Resources
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Text;
    using System.Xml;

    using KGySoft.Libraries.Reflection;
    using KGySoft.Libraries.Serialization;

    /// <summary>
    /// Represents an element in an XML resource (.resx) file.
    /// </summary>
    // TODO:
    // + GetValue optional cleanup/preserveRawData paraméterrel (a type feloldó is lehet optional): ha deserializálta az objektumot, törölje-e a nodeinfo-t és filerefet
    //   - A ResXResourceReader enumerátorai hívhatnák cleanup nélkül, a reader amúgy is tipikusan rövid életű, meg ez az alacsony szint, ahol nem önállóskodunk. Ha visszaváltanak resx módra, lássák továbbra is az eredeti állapotot.
    //   - A ResXResourceSet-ben lehetne egy property, hogy nem SafeMode esetben hogy működjön, default lehetne cleanuppal (AutoFreeXmlData). Így ha elkérünk egy resource-ot, majd mentünk, mindig beágyazva íródik vissza, de memóriát spórolunk.
    //     - ha kiírás történik és auto cleanup van (GetDataNodeInfo a ResXResourceSet mentéséből), és már van cache-elt value, a nodeInfo field ne is legyen beállítva visszatéréskor
    //     - kérdés, hogy ha cleanup van, mi legyen a filereffel. Ez csak akkor törölhető, ha van cached object, ilyenkor viszont a következő kiírás beágyazott objektumot generál.
    // + A nodeinfo tartalmát get-only propertykben kivezetni. Cleanup után, vagy ha nincs node info, nullt adjanak vissza
    //     vagy: ez újragenerálja az esetleg cleanupolt infot: az osztályba felvett TypeName, ValueData, MimeType, (esetleg alias is) mind return GetDataNodeInfo(null, null).xxx alakban legyenek.
    // remarks TODO-k:
    // Inkompatibiltás:
    // - Name is read-only property.
    // - nincs GetValueTypeName metódus. Helyette elkérhető a ténylegesen letárolt type string és az azonosító. A typeNameConverter-t ellátó Func a Writer-nek adható meg.
    // - nincs GetValue(AssemblyName[])
    // - Nincsenek typeNameConverter-es publikus ctor-ok. Ezt kizárólag az internal GetDataNodeInfo használja, amit a writer hív, ő viszont átadhatja a saját converterét
    // New feature:
    // - DataNodeInfo elemei kivezetve
    [Serializable]
    public sealed class ResXDataNode : ISerializable
    {
        private static readonly char[] specialChars = new char[] { ' ', '\r', '\n' };
        private static string compatibleFileRefTypeName;

        private string name;
        private string comment;
        private object value;

        private DataNodeInfo nodeInfo;
        private ResXFileRef fileRef;

        /// <summary>
        /// The cached assembly qualified name of the value. For FileRef it is initialized as FileRef and once the value
        /// is retrieved it returns the real type of the value.
        /// </summary>
        private string assemblyQualifiedName;

        /// <summary>
        /// Gets whether the <see cref="assemblyQualifiedName"/> is from a real type. It is false if the <see cref="assemblyQualifiedName"/>
        /// is created from a string or is FileRef.
        /// </summary>
        private bool aqnValid;

        //// todo: törölni, ha nem szükséges feltétlenül. Akár lokális változó is lehet(?)
        //private IFormatter binaryFormatter = null;

        //#if UNUSED
        //        private string mimeType;
        //        private string valueData;
        //#endif

        //private string typeName; // is only used when we create a resxdatanode manually with an object and contains the FQN

        //private string fileRefFullPath;
        //private string fileRefType;
        //private string fileRefTextEncoding;


        // this is going to be used to check if a ResXDataNode is of type ResXFileRef
        //private static ITypeResolutionService internalTypeResolver = new AssemblyNamesTypeResolutionService(null/*new AssemblyName[] { new AssemblyName("System.Windows.Forms") }*/);

        // call back function to get type name for multitargeting.
        // No public property to force using constructors for the following reasons:
        // 1. one of the constructors needs this field (if used) to initialize the object, make it consistent with the other ctrs to avoid errors.
        // 2. once the object is constructed the delegate should not be changed to avoid getting inconsistent results.
        //private Func<Type, string> typeNameConverter;

        // constructors

        //private ResXDataNode()
        //{
        //}

//        internal ResXDataNode Clone()
//        {
//            ResXDataNode result = new ResXDataNode();
//            result.nodeInfo = (this.nodeInfo != null) ? this.nodeInfo.Clone() : null; // nodeinfo is just made up of immutable objects, we don't need to clone it
//            result.name = this.name;
//            result.comment = this.comment;
//#if UNUSED            
//            result.mimeType = this.mimeType;
//            result.valueData = this.valueData;
//#endif
//            //result.typeName = this.typeName;
//            //result.fileRefFullPath = this.fileRefFullPath;
//            //result.fileRefType = this.fileRefType;
//            //result.fileRefTextEncoding = this.fileRefTextEncoding;
//            result.value = this.value; // we don't clone the value, because we don't know how
//            result.fileRef = (this.fileRef != null) ? this.fileRef.Clone() : null;
//            //result.typeNameConverter = this.typeNameConverter;
//            return result;
//        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="T:System.Resources.ResXDataNode"/> class.
        ///// </summary>
        ///// <param name="name">The name of the resource.</param><param name="value">The resource to store. </param><exception cref="T:System.InvalidOperationException">The resource named in <paramref name="value"/> does not support serialization. </exception><exception cref="T:System.ArgumentNullException"><paramref name="name"/> is null.</exception><exception cref="T:System.ArgumentException"><paramref name="name"/> is a string of zero length.</exception>
        //public ResXDataNode(string name, object value)
        //    : this(name, value, null)
        //{
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Resources.ResXDataNode"/> class.
        /// </summary>
        public ResXDataNode(string name, object value/*, Func<Type, string> typeNameConverter*/)
        {
            if (name == null)
                throw new ArgumentNullException("name", Res.Get(Res.ArgumentNull));

            if (name.Length == 0)
                throw new ArgumentException(Res.Get(Res.ArgumentEmpty), "name");

            //this.typeNameConverter = typeNameConverter;

            // TODO: support non-serializable
            //Type valueType = (value == null) ? typeof(object) : value.GetType();
            //if (value != null && !valueType.IsSerializable)
            //{
            //    throw new InvalidOperationException(/*TODO SR.GetString(SR.NotSerializableType, name, valueType.FullName)*/);
            //}
            //else if (value != null)
            //{
            //    this.typeName = MultitargetUtil.GetAssemblyQualifiedName(valueType, this.typeNameConverter);
            //}

            this.name = name;

            // 1.) null
            if (value == null)
            {
                // unlike the WinForms version, we use ResXNullRef to indicate a null value; otherwise, in GetValue we would always try to deserialize the null value
                this.value = ResXNullRef.Value;
                return;
            }

            // 2.) ResXDataNode
            ResXDataNode other = value as ResXDataNode;
            if (other != null)
            {
                InitFrom(other);
                return;
            }

            // 3.) FileRef
            ResXFileRef fr = value as ResXFileRef;
            if (fr != null)
            {
                fileRef = fr;
                return;
            }

            string typeName = value.GetType().AssemblyQualifiedName;
            if (typeName != null)
            {
                // 4.) System ResXDataNode
                if (typeName.StartsWith(ResXCommon.ResXDataNodeNameWinForms, StringComparison.Ordinal))
                {
                    InitFromWinForms(value);
                    return;
                }

                // 5.) System ResXFileRef
                if (typeName.StartsWith(ResXCommon.ResXFileRefNameWinForms, StringComparison.Ordinal))
                {
                    fileRef = ResXFileRef.InitFromWinForms(value);
                    return;
                }
            }

            // 6.) other value
            this.value = value;
        }

        private void InitFromWinForms(object other)
        {
            value = Accessors.ResXDataNode_value_Get(other);
            comment = Accessors.ResXDataNode_comment_Get(other);
            object fileRefWinForms = Accessors.ResXDataNode_fileRef_Get(other);
            if (fileRefWinForms != null)
                fileRef = ResXFileRef.InitFromWinForms(fileRefWinForms);
            object nodeInfoWinForms = Accessors.ResXDataNode_nodeInfo_Get(other);
            if (nodeInfoWinForms != null)
                nodeInfo = DataNodeInfo.InitFromWinForms(nodeInfoWinForms);

            // the WinForms version uses simply null instead of ResXNullRef
            if (value == null && fileRef == null && nodeInfo == null)
                value = ResXNullRef.Value;
        }

        private void InitFrom(ResXDataNode other)
        {
            value = other.value;
            assemblyQualifiedName = other.assemblyQualifiedName;
            aqnValid = other.aqnValid;
            comment = other.comment;
            if (other.fileRef != null)
                fileRef = other.fileRef.Clone();
            if (other.nodeInfo != null)
                nodeInfo = other.nodeInfo.Clone();
        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="T:System.Resources.ResXDataNode"/> class with a reference to a resource file.
        ///// </summary>
        ///// <param name="name">The name of the resource.</param><param name="fileRef">The file reference to use as the resource.</param><exception cref="T:System.ArgumentNullException"><paramref name="name"/> is null or <paramref name="fileRef"/> is null.</exception><exception cref="T:System.ArgumentException"><paramref name="name"/> is a string of zero length.</exception>
        //public ResXDataNode(string name, ResXFileRef fileRef)
        //    : this(name, fileRef, null)
        //{
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXDataNode"/> class with a reference to a resource file.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        /// <param name="fileRef">The file reference.</param>
        /// <param name="basePath">A base path for the relative path defined in <paramref name="fileRef"/>. Default value: <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException">
        /// name
        /// or
        /// fileRef
        /// </exception>
        /// <exception cref="System.ArgumentException">name</exception>
        public ResXDataNode(string name, ResXFileRef fileRef, string basePath = null/*, Func<Type, string> typeNameConverter*/)
        {
            if (name == null)
                throw new ArgumentNullException("name", Res.Get(Res.ArgumentNull));

            if (fileRef == null)
                throw new ArgumentNullException("fileRef", Res.Get(Res.ArgumentNull));

            if (name.Length == 0)
                throw new ArgumentException(Res.Get(Res.ArgumentEmpty), "name");

            this.name = name;
            this.fileRef = fileRef;
            //this.typeNameConverter = typeNameConverter;

            if (basePath != null)
                GetDataNodeInfo(null, null).BasePath = basePath;
        }

        internal ResXDataNode(DataNodeInfo nodeInfo)
        {
            this.nodeInfo = nodeInfo;
            InitFileRef();
        }

        internal object ValueInternal
        {
            get { return value; }
        }

        private static string CompatibleFileRefTypeName
        {
            get
            {
                if (compatibleFileRefTypeName == null)
                {
                    AssemblyName asmName = Reflector.mscorlibAssembly.GetName();
                    asmName.Name = "System.Windows.Forms";
                    compatibleFileRefTypeName = "System.Resources.ResXFileRef, " + asmName.FullName;
                }

                return compatibleFileRefTypeName;
            }
        }

        /// <summary>
        /// Gets the assembly qualified name of the node, or null, if type cannot be determined until deserializing it.
        /// </summary>
        private string AssemblyQualifiedName
        {
            get
            {
                if (assemblyQualifiedName != null)
                    return assemblyQualifiedName;

                if (value != null)
                {
                    assemblyQualifiedName = value.GetType().AssemblyQualifiedName;
                    aqnValid = true;
                }
                else if (nodeInfo != null)
                {
                    // if FileRef is not null and there is a nodeinfo, this property returns the AQN of the FileRef type,
                    // which is alright and is required in InitFileRef
                    aqnValid = false;
                    if (nodeInfo.AssemblyAliasValue == null)
                        assemblyQualifiedName = GetTypeName(nodeInfo);
                    else
                    {
                        string fullName = GetTypeName(nodeInfo);
                        if (fullName == null)
                            return null;
                        int genericEnd = fullName.LastIndexOf(']');
                        int aliasPos = fullName.IndexOf(',', genericEnd + 1);

                        if (aliasPos >= 0)
                            fullName = fullName.Substring(0, aliasPos);

                        assemblyQualifiedName = fullName + ", " + nodeInfo.AssemblyAliasValue;
                    }
                }
                else // if (fileRef != null)
                {
                    aqnValid = false;
                    assemblyQualifiedName = fileRef.TypeName;
                }

                return assemblyQualifiedName;
            }
        }

        private string GetTypeName(DataNodeInfo nodeInfo)
        {
            if (nodeInfo.TypeName != null)
                return nodeInfo.TypeName;

            // occurs when there is no <value> element in the resx
            if (nodeInfo.ValueData == null)
                return typeof(ResXNullRef).AssemblyQualifiedName;

            if (nodeInfo.MimeType == null)
                return Reflector.StringType.AssemblyQualifiedName;

            return null;
        }

        /// <summary>
        /// Called from <see cref="ResXResourceSet"/>.
        /// </summary>
        internal object GetValueInternal(bool safeMode, bool isString, bool cleanup, string basePath)
        {
            object result;
            if (safeMode)
            {
                if (!isString)
                    return this;

                // returning a string for any type
                result = value;
                if (result is string)
                    return result;

                if (nodeInfo != null)
                    return nodeInfo.ValueData;

                if (fileRef != null)
                    return fileRef.ToString();

                // here there is no available string meta so generating nodeInfo
                Debug.Assert(result != null);
                nodeInfo = GetDataNodeInfo(null, null);
                return nodeInfo.ValueData;
            }

            // non-safe mode
            if (!isString)
                return GetValue(null, basePath, cleanup);

            result = value;
            if (result is string)
                return result;

            if (result == ResXNullRef.Value)
                return null;

            if (result != null)
                throw new InvalidOperationException(Res.Get(Res.NonStringResourceWithType, Name, result.GetType().ToString()));

            // result is not deserialized here yet

            // Omitting assembly because of the version. If we know before the serialization that result is not a string,
            // we can already throw an exception. But type is checked once again at the end, after deserialization.
            string stringName = Reflector.StringType.FullName;
            string aqn = AssemblyQualifiedName;
            if (aqn != null && !IsNullRef(aqn) && !aqn.StartsWith(stringName, StringComparison.Ordinal) && (fileRef == null || !fileRef.TypeName.StartsWith(stringName, StringComparison.Ordinal)))
                throw new InvalidOperationException(Res.Get(Res.NonStringResourceWithType, Name, fileRef == null ? aqn : fileRef.TypeName));

            result = GetValue(null, basePath, cleanup);
            if (result == null || result is string)
                return result;

            throw new InvalidOperationException(Res.Get(Res.NonStringResourceWithType, Name, result.GetType().ToString()));
        }


        private void InitFileRef()
        {
            if (!IsFileRef(AssemblyQualifiedName))
                return;

            ResXFileRef.TryParse(nodeInfo.ValueData, out fileRef);
            //string[] fileRefDetails = ParseResxFileRefString(nodeInfo.ValueData);
            //if (fileRefDetails != null && fileRefDetails.Length > 1)
            //{
            //    //if (!Path.IsPathRooted(fileRefDetails[0]) && basePath != null)
            //    //{
            //    //    fileRefDetails[0] = Path.Combine(basePath, fileRefDetails[0]);
            //    //}
            //    //else
            //    //{
            //    //    fileRefFullPath = fileRefDetails[0];
            //    //}
            //    //fileRefType = fileRefDetails[1];
            //    //if (fileRefDetails.Length > 2)
            //    //{
            //    //    fileRefTextEncoding = fileRefDetails[2];
            //    //}

            //    fileRef = new ResXFileRef(fileRefDetails[0], fileRefDetails[1], fileRefDetails.Length > 2 ? fileRefDetails[2] : null);
            //}
        }

        private static bool IsFileRef(string assemblyQualifiedName)
        {
            if (assemblyQualifiedName == null)
                return false;

            // the common scenario as it is saved by system resx
            return assemblyQualifiedName.StartsWith(ResXCommon.ResXFileRefNameWinForms, StringComparison.Ordinal)
                || assemblyQualifiedName.StartsWith(ResXCommon.ResXFileRefNameKGySoft, StringComparison.Ordinal);
        }

        private static bool IsNullRef(string assemblyQualifiedName)
        {
            // the common scenario as it is saved by system resx
            return assemblyQualifiedName.StartsWith(ResXCommon.ResXNullRefNameWinForms, StringComparison.Ordinal)
                || assemblyQualifiedName.StartsWith(ResXCommon.ResXNullRefNameKGySoft, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets or sets an arbitrary comment regarding this resource.
        /// </summary>
        /// 
        /// <returns>
        /// A string that represents the comment.
        /// </returns>
        public string Comment
        {
            get
            {
                string result = comment;
                DataNodeInfo ni = nodeInfo;
                return result == null && ni != null ? ni.Comment : (result ?? String.Empty);
            }
            set
            {
                comment = value;
            }
        }

        /// <summary>
        /// Gets the name of this resource.
        /// </summary>
        /// <returns>
        /// A string that corresponds to the resource name. If no name is assigned, this property returns a zero-length string.
        /// </returns>
        public string Name
        {
            get
            {
                string result = name;
                if (result == null && nodeInfo != null)
                {
                    result = nodeInfo.Name;
                }
                return result;
            }
            //[
            //    SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters") // "Name" is the name of the property.
            //    // So we don't have to localize it.
            //]
            //set
            //{
            //    if (value == null)
            //    {
            //        throw (new ArgumentNullException("Name"));
            //    }
            //    if (value.Length == 0)
            //    {
            //        throw (new ArgumentException("Name"));
            //    }
            //    name = value;
            //}
        }

        /// <summary>
        /// Gets the file reference for this resource, or <see langword="null"/> if this resource does not have a file reference.
        /// </summary>
        /// <returns>
        /// The file reference, if this resource uses one. If this resource stores its value as an <see cref="object"/>,
        /// this property will return <see langword="null"/>.
        /// </returns>
        public ResXFileRef FileRef
        {
            get
            {
                // TODO: ez csak akkor kéne, ha a előfordulhat, hogy a FileRef-et töröljük, és NodeInfo-ból újra elő kell állítani.
                //if (fileRef == null && IsFileRef(AssemblyQualifiedName))
                //    InitFileRef(null);

                // TODO: delete - nem kell copy, nem tud változni
                //if (FileRefFullPath == null)
                //{
                //    return null;
                //}
                //if (fileRef == null)
                //{
                //    if (String.IsNullOrEmpty(fileRefTextEncoding))
                //    {
                //        fileRef = new ResXFileRef(FileRefFullPath, FileRefType);
                //    }
                //    else
                //    {
                //        fileRef = new ResXFileRef(FileRefFullPath, FileRefType, Encoding.GetEncoding(FileRefTextEncoding));
                //    }
                //}
                return fileRef;
            }
        }

        /// <summary>
        /// Gets the assembly name defined in the source .resx file if <see cref="TypeName"/> contains an assembly alias name,
        /// or <see langword="null"/>, if <see cref="TypeName"/> contains the assembly qualified name or if the resource does not contain the .resx information.
        /// </summary>
        public string AssemblyAliasValue
        {
            get { return nodeInfo?.AssemblyAliasValue; }
        }

        /// <summary>
        /// Gets the type information as <see cref="string"/> as it is stored in the source .resx file. It can be either an assembly qualified name,
        /// or a type name with or without an assembly alias name. If <see cref="AssemblyAliasValue"/> is not <see langword="null"/>, this property value
        /// contains an assembly alias name. The property returns <see langword="null"/>, if the <c>type</c> attribute is not defined or if the resource does not contain the .resx information.
        /// </summary>
        public string TypeName
        {
            get { return nodeInfo?.TypeName; }
        }

        /// <summary>
        /// Gets the MIME type as it is stored in the .resx file for this resource, or <see langword="null"/>,
        /// if the <c>mimetype</c> attribute is not defined or if the resource does not contain the .resx information.
        /// </summary>
        public string MimeType
        {
            get { return nodeInfo?.MimeType; }
        }

        /// <summary>
        /// Gets the raw value data as <see cref="string"/> as it is stored in the source .resx file, or
        /// <see langword="null"/>, if the resource does not contain the .resx information.
        /// </summary>
        public string ValueData
        {
            get { return nodeInfo?.ValueData; }
        }

        //private string FileRefFullPath
        //{
        //    get
        //    {
        //        string result = (fileRef == null ? null : fileRef.FileName);
        //        if (result == null)
        //        {
        //            result = fileRefFullPath;
        //        }
        //        return result;
        //    }
        //}

        //private string FileRefType
        //{
        //    get
        //    {
        //        string result = (fileRef == null ? null : fileRef.TypeName);
        //        if (result == null)
        //        {
        //            result = fileRefType;
        //        }
        //        return result;
        //    }
        //}

        //private string FileRefTextEncoding
        //{
        //    get
        //    {
        //        string result = (fileRef == null ? null : (fileRef.TextFileEncoding == null ? null : fileRef.TextFileEncoding.BodyName));
        //        if (result == null)
        //        {
        //            result = fileRefTextEncoding;
        //        }

        //        return result;
        //    }
        //}


#if !SYSTEM_WEB     // System.Web does not link with the Soap assembly
        ///// <devdoc>
        /////     As a performance optimization, we isolate the soap class here in a separate
        /////     function.  We don't care about the binary formatter because it lives in 
        /////     mscorlib, which is already loaded.  The soap formatter lives in a separate
        /////     assembly, however, so there is value in preventing it from needlessly
        /////     being loaded.
        ///// </devdoc>
        //[MethodImplAttribute(MethodImplOptions.NoInlining)]
        //private IFormatter CreateSoapFormatter()
        //{
        //    return new System.Runtime.Serialization.Formatters.Soap.SoapFormatter();
        //}
#endif

        private void InitNodeInfo(Func<Type, string> typeNameConverter, bool compatibleFormat)
        {
            Debug.Assert(value != null, "value is null in FillDataNodeInfoFromObject");

            // 1.) natively supported type
            if (CanConvertNatively(compatibleFormat))
            {
                InitNodeInfoNative(typeNameConverter);
                return;
            }

            // 2.) byte[] - as cast would allow sbyte[] as well
            if (value.GetType() == Reflector.ByteArrayType)
            {
                byte[] bytes = (byte[])value;
                nodeInfo.ValueData = ResXCommon.ToBase64(bytes);
                nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(
                    Reflector.ByteArrayType,
                    typeNameConverter,
                    compatibleFormat);
                nodeInfo.AssemblyAliasValue = null;
                nodeInfo.CompatibleFormat = true;
                return;
            }

            // 3.) CultureInfo - because CultureInfoConverter sets the CurrentUICulture in a finally block
            CultureInfo ci = value as CultureInfo;
            if (ci != null)
            {
                nodeInfo.ValueData = ci.Name;
                nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(
                    typeof(CultureInfo),
                    typeNameConverter,
                    compatibleFormat);
                nodeInfo.AssemblyAliasValue = null;
                nodeInfo.CompatibleFormat = true;
                return;
            }

            // 4.) null
            if (value == ResXNullRef.Value)
            {
                nodeInfo.ValueData = String.Empty;
                nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(
                    typeof(ResXNullRef),
                    typeNameConverter,
                    compatibleFormat);
                nodeInfo.AssemblyAliasValue = null;
                nodeInfo.CompatibleFormat = compatibleFormat;
                return;
            }

            //if (compatibleFormat && !valueType.IsSerializable)
            //{
            //    throw new InvalidOperationException(Res.Get(Res.ResXNotSerializableType, name, valueType.FullName));
            //}

            // 5.) to string by TypeConverter
            Type type = value.GetType();
            TypeConverter tc = TypeDescriptor.GetConverter(type);
            bool toString = tc.CanConvertTo(typeof(string));
            bool fromString = tc.CanConvertFrom(typeof(string));
            try
            {
                if (toString && fromString)
                {
                    nodeInfo.ValueData = tc.ConvertToInvariantString(value);
                    nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(type, typeNameConverter, compatibleFormat);
                    nodeInfo.AssemblyAliasValue = null;
                    nodeInfo.CompatibleFormat = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                // Some custom type converters will throw in ConvertTo(string)
                // to indicate that this object should be serialized through ISeriazable
                // instead of as a string. This is semi-wrong, but something we will have to
                // live with to allow user created Cursors to be serializable.
                if (ClientUtils.IsSecurityOrCriticalException(ex))
                {
                    throw;
                }
            }

            // 6.) to byte[] by TypeConverter
            bool toByteArray = tc.CanConvertTo(typeof(byte[]));
            bool fromByteArray = tc.CanConvertFrom(typeof(byte[]));
            if (toByteArray && fromByteArray)
            {
                byte[] data = (byte[])tc.ConvertTo(value, typeof(byte[]));
                nodeInfo.ValueData = ResXCommon.ToBase64(data);
                nodeInfo.MimeType = ResXCommon.ByteArraySerializedObjectMimeType;
                nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(type, typeNameConverter, compatibleFormat);
                nodeInfo.AssemblyAliasValue = null;
                nodeInfo.CompatibleFormat = true;
                return;
            }

            // 7.) to byte[] by BinaryFormatter
            nodeInfo.TypeName = null;
            nodeInfo.AssemblyAliasValue = null;
            if (compatibleFormat)
            {
                var binaryFormatter = new BinaryFormatter();
                if (typeNameConverter != null)
                    binaryFormatter.Binder = new ResXSerializationBinder(typeNameConverter, compatibleFormat);

                MemoryStream ms = new MemoryStream();
                bool wrap = !type.IsSerializable
                    || type.IsArray && type.GetArrayRank() == 1 && !type.GetElementType().IsPrimitive && !type.GetInterfaces().Any(i => i.IsGenericType);
                binaryFormatter.Serialize(ms, wrap ? new AnyObjectSerializerWrapper(value, true) : value);
                nodeInfo.ValueData = ResXCommon.ToBase64(ms.ToArray());
                nodeInfo.MimeType = ResXCommon.DefaultSerializedObjectMimeType;
                nodeInfo.CompatibleFormat = true;
                return;
            }

            // 8.) to byte[] by KGySoft BinarySerializationFormatter
            var serializer = new BinarySerializationFormatter();
            if (typeNameConverter != null)
                serializer.Binder = new ResXSerializationBinder(typeNameConverter, false);
            nodeInfo.ValueData = ResXCommon.ToBase64(serializer.Serialize(value));
            nodeInfo.MimeType = ResXCommon.KGySoftSerializedObjectMimeType;
            nodeInfo.CompatibleFormat = false;
        }

        private void InitNodeInfoNative(Func<Type, string> typeNameConverter)
        {
            nodeInfo.AssemblyAliasValue = null;
            nodeInfo.CompatibleFormat = true;
            string stringValue = value as string;
            nodeInfo.TypeName = stringValue != null
                ? null
                : ResXCommon.GetAssemblyQualifiedName(value.GetType(), typeNameConverter, false);

            if (stringValue != null)
            {
                nodeInfo.ValueData = stringValue;
                return;
            }
            if (value is DateTime)
            {
                nodeInfo.ValueData = XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
                return;
            }
            if (value is DateTimeOffset)
            {
                nodeInfo.ValueData = XmlConvert.ToString((DateTimeOffset)value);
                return;
            }
            if (value is double)
            {
                nodeInfo.ValueData = ((double)value).ToRoundtripString();
                return;
            }
            if (value is float)
            {
                nodeInfo.ValueData = ((float)value).ToRoundtripString();
                return;
            }
            if (value is decimal)
            {
                nodeInfo.ValueData = ((decimal)value).ToRoundtripString();
                return;
            }

            // char/byte/sbyte/short/ushort/int/uint/long/ulong/bool/DBNull
            IConvertible convertibleValue = value as IConvertible;
            if (convertibleValue != null)
            {
                nodeInfo.ValueData = Convert.ToString(value, NumberFormatInfo.InvariantInfo);
                if (value is DBNull)
                    nodeInfo.CompatibleFormat = false;
                return;                
            }

            nodeInfo.CompatibleFormat = false;

            // Type
            Type type = value as Type;
            if (type != null)
            {
                nodeInfo.ValueData = type.GetTypeName(true);
                return;
            }

            // IntPtr/UIntPtr
            nodeInfo.ValueData = value.ToString();
        }

        private bool CanConvertNatively(bool compatibleFormat)
        {
            Type type = value.GetType();
            return Reflector.CanParseNatively(type)
                && ((!compatibleFormat
                    || !(type.In(typeof(DBNull), typeof(IntPtr), typeof(UIntPtr), Reflector.RuntimeType)))
                    && (type != Reflector.RuntimeType || !(((Type)value).IsGenericParameter)));
        }

        private object NodeInfoToObject(DataNodeInfo dataNodeInfo, ITypeResolutionService typeResolver)
        {
            // handling that <value> can be missing in resx.
            if (dataNodeInfo.ValueData == null)
                return ResXNullRef.Value;

            string mimeTypeName = dataNodeInfo.MimeType;
            string typeName = AssemblyQualifiedName;
            Type type;

            // from MIME type
            if (!String.IsNullOrEmpty(mimeTypeName))
            {
                // 1.) BinaryFormatter
                if (mimeTypeName.In(ResXCommon.BinSerializedMimeTypes))
                {
                    byte[] serializedData = FromBase64WrappedString(dataNodeInfo.ValueData);

                    var binaryFormatter = new BinaryFormatter();
                    binaryFormatter.Binder = typeResolver != null
                        ? (SerializationBinder)new ResXSerializationBinder(typeResolver)
                        : new WeakAssemblySerializationBinder();

                    object result = null;
                    if (serializedData != null && serializedData.Length > 0)
                    {
                        result = binaryFormatter.Deserialize(new MemoryStream(serializedData));
                        if (result != ResXNullRef.Value && IsNullRef(result.GetType().AssemblyQualifiedName))
                            result = ResXNullRef.Value;
                    }

                    return result;
                }
#if !SYSTEM_WEB // System.Web does not link with the Soap assembly
                //else if (String.Equals(mimeTypeName, ResXResourceWriter.SoapSerializedObjectMimeType)
                //         || String.Equals(mimeTypeName, ResXResourceWriter.CompatSoapSerializedObjectMimeType))
                //{
                //    string text = dataNodeInfo.ValueData;
                //    byte[] serializedData;
                //    serializedData = FromBase64WrappedString(text);

                //    if (serializedData != null && serializedData.Length > 0)
                //    {

                //        // Performance : don't inline a new SoapFormatter here.  That will always bring in
                //        //               the soap assembly, which we don't want.  Throw this in another
                //        //               function so the class doesn't have to get loaded.
                //        //
                //        IFormatter formatter = CreateSoapFormatter();
                //        result = formatter.Deserialize(new MemoryStream(serializedData));
                //        if (result is ResXNullRef)
                //        {
                //            result = null;
                //        }
                //    }
                //}
#endif

                // 2.) By TypeConverter from byte[]
                if (String.Equals(mimeTypeName, ResXCommon.ByteArraySerializedObjectMimeType))
                {
                    if (String.IsNullOrEmpty(typeName))
                        throw new XmlException(Res.Get(Res.XmlMissingAttribute, ResXCommon.TypeStr, dataNodeInfo.Line, dataNodeInfo.Column));

                    type = ResolveType(typeName, typeResolver);
                    if (type == null)
                    {
                        string newMessage = Res.Get(Res.TypeLoadException, typeName, dataNodeInfo.Line, dataNodeInfo.Column);
                        XmlException xml = new XmlException(newMessage, null, dataNodeInfo.Line, dataNodeInfo.Column);
                        TypeLoadException newTle = new TypeLoadException(newMessage, xml);
                        throw newTle;
                    }

                    TypeConverter byteArrayConverter = TypeDescriptor.GetConverter(type);
                    if (!byteArrayConverter.CanConvertFrom(typeof(byte[])))
                    {
                        string message = Res.Get(Res.ConvertFromByteArrayNotSupportedAt, typeName, dataNodeInfo.Line, dataNodeInfo.Column, Res.Get(Res.ConvertFromByteArrayNotSupported, byteArrayConverter.GetType().FullName));
                        XmlException xml = new XmlException(message, null, dataNodeInfo.Line, dataNodeInfo.Column);
                        NotSupportedException newNse = new NotSupportedException(message, xml);
                        throw newNse;                        
                    }

                    byte[] serializedData = FromBase64WrappedString(dataNodeInfo.ValueData);
                    if (serializedData == null)
                        return null;

                    try
                    {
                        return byteArrayConverter.ConvertFrom(serializedData);
                    }
                    catch (NotSupportedException e)
                    {
                        string message = Res.Get(Res.ConvertFromByteArrayNotSupportedAt, typeName, dataNodeInfo.Line, dataNodeInfo.Column, e.Message);
                        XmlException xml = new XmlException(message, e, dataNodeInfo.Line, dataNodeInfo.Column);
                        NotSupportedException newNse = new NotSupportedException(message, xml);
                        throw newNse;
                    }
                }

                // 3.) BinarySerializationFormatter
                if (mimeTypeName == ResXCommon.KGySoftSerializedObjectMimeType)
                {
                    string text = dataNodeInfo.ValueData;
                    byte[] serializedData = FromBase64WrappedString(text);

                    var serializer = new BinarySerializationFormatter();
                    serializer.Binder = typeResolver != null
                        ? (SerializationBinder)new ResXSerializationBinder(typeResolver)
                        : new WeakAssemblySerializationBinder();

                    object result = null;
                    if (serializedData != null && serializedData.Length > 0)
                    {
                        result = serializer.Deserialize(serializedData);
                        if (result != ResXNullRef.Value && IsNullRef(result.GetType().AssemblyQualifiedName))
                            result = ResXNullRef.Value;
                    }

                    return result;
                }

                throw new NotSupportedException(Res.Get(Res.ResXMimeTypeNotSupported, mimeTypeName, dataNodeInfo.Line, dataNodeInfo.Column));
            }

            // No MIME type
            Debug.Assert(typeName != null, "If there is no MIME type, typeName is expected to be string");
            if (typeName == null)
                return dataNodeInfo.ValueData;

            type = ResolveType(typeName, typeResolver);
            if (type == null)
            {
                string newMessage = Res.Get(Res.TypeLoadException, typeName, dataNodeInfo.Line, dataNodeInfo.Column);
                XmlException xml = new XmlException(newMessage, null, dataNodeInfo.Line, dataNodeInfo.Column);
                TypeLoadException newTle = new TypeLoadException(newMessage, xml);
                throw newTle;
            }

            // 4.) Native type - type converter is slower and will cont convert negative zeros, for example.
            if (Reflector.CanParseNatively(type))
                return Reflector.Parse(type, dataNodeInfo.ValueData);

            // 5.) null
            if (type == typeof(ResXNullRef))
                return ResXNullRef.Value;

            // 6.) byte[]
            if (type == Reflector.ByteArrayType)
                return FromBase64WrappedString(dataNodeInfo.ValueData);

            // 7.) By TypeConverter from string
            TypeConverter tc = TypeDescriptor.GetConverter(type);
            if (!tc.CanConvertFrom(Reflector.StringType))
            {
                string message = Res.Get(Res.ConvertFromStringNotSupportedAt, typeName, dataNodeInfo.Line, dataNodeInfo.Column, Res.Get(Res.ConvertFromStringNotSupported, tc.GetType().FullName));
                XmlException xml = new XmlException(message, null, dataNodeInfo.Line, dataNodeInfo.Column);
                NotSupportedException newNse = new NotSupportedException(message, xml);
                throw newNse;
            }

            try
            {
                return tc.ConvertFromInvariantString(dataNodeInfo.ValueData);
            }
            catch (NotSupportedException e)
            {
                string message = Res.Get(Res.ConvertFromStringNotSupportedAt, typeName, dataNodeInfo.Line, dataNodeInfo.Column, e.Message);
                XmlException xml = new XmlException(message, e, dataNodeInfo.Line, dataNodeInfo.Column);
                NotSupportedException newNse = new NotSupportedException(message, xml);
                throw newNse;
            }
        }

        internal DataNodeInfo GetDataNodeInfo(Func<Type, string> typeNameConverter, bool? compatibleFormat/* TODO: del, string basePath*/)
        {
            bool toInit = nodeInfo == null;
            bool toReInit = toInit || compatibleFormat.HasValue && nodeInfo.CompatibleFormat != compatibleFormat.Value;

            if (nodeInfo == null)
                nodeInfo = new DataNodeInfo();
            else if (nodeInfo.ValueData == null && !toReInit)
            {
                // handling that <value> element can be missing in .resx:
                // configuring a regular null; otherwise, an empty string would be written next time
                nodeInfo.ValueData = String.Empty;
                nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(typeof(ResXNullRef), typeNameConverter, compatibleFormat.GetValueOrDefault());
                nodeInfo.AssemblyAliasValue = null;
                nodeInfo.CompatibleFormat = compatibleFormat.GetValueOrDefault();
            }

            // name and comment are mutable properties so setting them in all cases
            nodeInfo.Name = Name;
            nodeInfo.Comment = Comment;

            // Though FileRef is a public property, it is immutable so there is no need to always refresh NodeInfo from fileRef
            if (!toReInit)
                return nodeInfo;

            // if we dont have a datanodeinfo it could be either a direct object OR a fileref
            if (fileRef != null)
            {
                //TODO: del 
                //if (isPathRooted)
                //    fileRef.MakeFilePathRelative(basePath);
                nodeInfo.ValueData = fileRef.ToString();
                nodeInfo.MimeType = null;
                nodeInfo.CompatibleFormat = compatibleFormat.GetValueOrDefault();
                if (compatibleFormat.GetValueOrDefault())
                {
                    if (String.IsNullOrEmpty(nodeInfo.TypeName) || !nodeInfo.TypeName.StartsWith(ResXCommon.ResXFileRefNameWinForms, StringComparison.Ordinal))
                    {
                        nodeInfo.TypeName = CompatibleFileRefTypeName;
                        nodeInfo.AssemblyAliasValue = null;
                        aqnValid = false;
                        assemblyQualifiedName = null;
                    }
                }
                else if (String.IsNullOrEmpty(nodeInfo.TypeName) || !nodeInfo.TypeName.StartsWith(ResXCommon.ResXFileRefNameKGySoft, StringComparison.Ordinal))
                {
                    nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(typeof(ResXFileRef), typeNameConverter, compatibleFormat.GetValueOrDefault());
                    nodeInfo.AssemblyAliasValue = null;
                    aqnValid = false;
                    assemblyQualifiedName = null;
                }
            }
            else if (toInit || nodeInfo.ValueData == null || compatibleFormat.GetValueOrDefault() && !nodeInfo.CompatibleFormat)
            {
                // first init, invalid null, or switching to compatible format
                if (value == null)
                {
                    Debug.Assert(!toInit, "Value should not be null when DataNodeInfo is generated from scratch.");
                    value = NodeInfoToObject(nodeInfo, null);
                }

                InitNodeInfo(typeNameConverter, compatibleFormat.GetValueOrDefault());
            }

            return nodeInfo;
        }

        //TODO
        ///// <include file='doc\ResXDataNode.uex' path='docs/doc[@for="ResXDataNode.GetNodePosition"]/*' />
        ///// <devdoc>
        /////    Might return the position in the resx file of the current node, if known
        /////    otherwise, will return Point(0,0) since point is a struct 
        ///// </devdoc>
        //public Point GetNodePosition()
        //{
        //    if (nodeInfo == null)
        //    {
        //        return new Point();
        //    }
        //    else
        //    {
        //        return nodeInfo.ReaderPosition;
        //    }
        //}

        ///// <summary>
        ///// Retrieves the type name for the value by using the specified type resolution service.
        ///// </summary>
        ///// 
        ///// <returns>
        ///// A string that represents the fully qualified name of the type.
        ///// </returns>
        ///// <param name="typeResolver">The type resolution service to use to locate a converter for this type. </param><exception cref="T:System.TypeLoadException">The corresponding type could not be found. </exception>
        ///// <devdoc>
        /////    Get the FQ type name for this datanode.
        /////    We return typeof(object) for ResXNullRef
        ///// </devdoc>
        //public string GetValueTypeName(ITypeResolutionService typeResolver)
        //{
        //    // the type name here is always a FQN
        //    if (typeName != null && typeName.Length > 0)
        //    {
        //        if (typeName.Equals(MultitargetUtil.GetAssemblyQualifiedName(typeof(ResXNullRef), this.typeNameConverter)))
        //        {
        //            return MultitargetUtil.GetAssemblyQualifiedName(typeof(object), this.typeNameConverter);
        //        }
        //        else
        //        {
        //            return typeName;
        //        }
        //    }
        //    string result = FileRefType;
        //    Type objectType = null;
        //    // do we have a fileref?
        //    if (result != null)
        //    {
        //        // try to resolve this type
        //        objectType = ResolveType(FileRefType, typeResolver);
        //    }
        //    else if (nodeInfo != null)
        //    {

        //        // we dont have a fileref, try to resolve the type of the datanode
        //        result = nodeInfo.TypeName;
        //        // if typename is null, the default is just a string
        //        if (result == null || result.Length == 0)
        //        {
        //            // we still dont know... do we have a mimetype? if yes, our only option is to 
        //            // deserialize to know what we're dealing with... very inefficient...
        //            if (nodeInfo.MimeType != null && nodeInfo.MimeType.Length > 0)
        //            {
        //                object insideObject = null;

        //                try
        //                {
        //                    insideObject = GenerateObjectFromDataNodeInfo(nodeInfo, typeResolver);
        //                }
        //                catch (Exception ex)
        //                { // it'd be better to catch SerializationException but the underlying type resolver
        //                    // can throw things like FileNotFoundException which is kinda confusing, so I am catching all here..
        //                    if (ClientUtils.IsCriticalException(ex))
        //                    {
        //                        throw;
        //                    }
        //                    // something went wrong, type is not specified at all or stream is corrupted
        //                    // return system.object
        //                    result = MultitargetUtil.GetAssemblyQualifiedName(typeof(object), this.typeNameConverter);
        //                }

        //                if (insideObject != null)
        //                {
        //                    result = MultitargetUtil.GetAssemblyQualifiedName(insideObject.GetType(), this.typeNameConverter);
        //                }
        //            }
        //            else
        //            {
        //                // no typename, no mimetype, we have a string...
        //                result = MultitargetUtil.GetAssemblyQualifiedName(typeof(string), this.typeNameConverter);
        //            }
        //        }
        //        else
        //        {
        //            objectType = ResolveType(nodeInfo.TypeName, typeResolver);
        //        }
        //    }
        //    if (objectType != null)
        //    {
        //        if (objectType == typeof(ResXNullRef))
        //        {
        //            result = MultitargetUtil.GetAssemblyQualifiedName(typeof(object), this.typeNameConverter);
        //        }
        //        else
        //        {
        //            result = MultitargetUtil.GetAssemblyQualifiedName(objectType, this.typeNameConverter);
        //        }
        //    }
        //    return result;
        //}

        ///// <summary>
        ///// Retrieves the type name for the value by examining the specified assemblies.
        ///// </summary>
        ///// 
        ///// <returns>
        ///// A string that represents the fully qualified name of the type.
        ///// </returns>
        ///// <param name="names">The assemblies to examine for the type. </param><exception cref="T:System.TypeLoadException">The corresponding type could not be found. </exception>
        ///// <devdoc>
        /////    Get the FQ type name for this datanode
        ///// </devdoc>
        //public string GetValueTypeName(AssemblyName[] names)
        //{
        //    return GetValueTypeName(new AssemblyNamesTypeResolutionService(names));
        //}

        /// <summary>
        /// Retrieves the object that is stored by this node by using the specified type resolution service.
        /// </summary>
        /// <returns>
        /// The object that corresponds to the stored value.
        /// </returns>
        /// <param name="basePath">Defines a base path for file reference values. Used when <see cref="FileRef"/> is not <see langword="null"/>.
        /// If this parameter is <see langword="null"/>, tries to use the original base path, if any. Default value is <see langword="null"/>.</param>
        /// <param name="typeResolver">The type resolution service to use when looking for a type converter. Default value is <see langword="null"/>.</param>
        /// <param name="cleanupNodeInfo"><c>true</c> to free the underlying XML data once the value is deserialized; otherwise, <c>false</c>. Default value is <c>false</c>.</param>
        /// <exception cref="TypeLoadException">The corresponding type could not be found, or an appropriate type converter is not available.</exception>
        public object GetValue(ITypeResolutionService typeResolver = null, string basePath = null, bool cleanupNodeInfo = false)
        {
            object result;
            if (value != null)
                result = value;
            else if (fileRef != null)
            {
                // fileRef.TypeName is always an AQN, so there is no need to play with the alias name.
                Type objectType = ResolveType(fileRef.TypeName, typeResolver);
                if (objectType != null)
                {
                    if (basePath == null && nodeInfo != null)
                        basePath = nodeInfo.BasePath;
                    value = result = fileRef.GetValue(objectType, basePath);
                }
                else
                    throw new TypeLoadException(
                        nodeInfo == null
                            ? Res.Get(Res.TypeLoadExceptionShort, fileRef.TypeName)
                            : Res.Get(Res.TypeLoadException, fileRef.TypeName, nodeInfo.Line, nodeInfo.Column));
            }
            else
            {
                // it's embedded, we deserialize it
                value = result = NodeInfoToObject(nodeInfo, typeResolver);
            }

            if (cleanupNodeInfo && nodeInfo != null)
            {
                if (name == null)
                    name = nodeInfo.Name;
                if (comment == null)
                    comment = nodeInfo.Comment;

                nodeInfo = null;
            }

            // if AQN is already set, but not from the resulting type, resetting it with the real type of the value
            if (!aqnValid && assemblyQualifiedName != null)
            {
                assemblyQualifiedName = result.GetType().AssemblyQualifiedName;
                aqnValid = true;
            }
            return result == ResXNullRef.Value ? null : result;
        }

        ///// <summary>
        ///// Retrieves the object that is stored by this node by searching the specified assemblies.
        ///// </summary>
        ///// 
        ///// <returns>
        ///// The object that corresponds to the stored value.
        ///// </returns>
        ///// <param name="names">The list of assemblies to search for the type of the object.</param><exception cref="T:System.TypeLoadException">The corresponding type could not be found, or an appropriate type converter is not available.</exception>
        ///// <devdoc>
        /////    Get the value contained in this datanode
        ///// </devdoc>
        //public object GetValue(AssemblyName[] names)
        //{
        //    return GetValue(new AssemblyNamesTypeResolutionService(names));
        //}

        private static byte[] FromBase64WrappedString(string text)
        {
            if (text.IndexOfAny(specialChars) == -1)
                return Convert.FromBase64String(text);

            StringBuilder sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case ' ':
                    case '\r':
                    case '\n':
                        break;
                    default:
                        sb.Append(text[i]);
                        break;
                }
            }
            return Convert.FromBase64String(sb.ToString());
        }

        private Type ResolveType(string assemblyQualifiedName, ITypeResolutionService typeResolver)
        {
            // Mapping winforms refs to KGySoft ones.
            if (IsFileRef(assemblyQualifiedName))
                return typeof(ResXFileRef);

            if (IsNullRef(assemblyQualifiedName))
                return typeof(ResXNullRef);

            Type result = null;
            if (typeResolver != null)
                result = typeResolver.GetType(assemblyQualifiedName, false);

            if (result == null)
                result = Reflector.ResolveType(assemblyQualifiedName, true, true);

            // Mapping winforms refs to KGySoft ones (can happen in case of non-usual aliases)
            if (IsFileRef(result.AssemblyQualifiedName))
                return typeof(ResXFileRef);

            if (IsNullRef(result.AssemblyQualifiedName))
                return typeof(ResXNullRef);

            return result;
        }

        //private Type ResolveType(string typeName, ITypeResolutionService typeResolver)
        //{
        //    Type t = null;
        //    if (typeResolver != null)
        //    {

        //        // If we cannot find the strong-named type, then try to see
        //        // if the TypeResolver can bind to partial names. For this, 
        //        // we will strip out the partial names and keep the rest of the
        //        // strong-name information to try again.
        //        //

        //        t = typeResolver.GetType(typeName, false);
        //        if (t == null)
        //        {

        //            string[] typeParts = typeName.Split(new char[] { ',' });

        //            // Break up the type name from the rest of the assembly strong name.
        //            if (typeParts.Length >= 2)
        //            {
        //                string partialName = typeParts[0].Trim();
        //                string assemblyName = typeParts[1].Trim();
        //                partialName = partialName + ", " + assemblyName;
        //                t = typeResolver.GetType(partialName, false);
        //            }
        //        }
        //    }

        //    if (t == null)
        //    {
        //        t = Type.GetType(typeName);
        //    }

        //    return t;
        //}

        void ISerializable.GetObjectData(SerializationInfo si, StreamingContext context)
        {
            // ReSharper disable once LocalVariableHidesMember
            DataNodeInfo nodeInfo = GetDataNodeInfo(null, null);
            si.AddValue(ResXCommon.NameStr, nodeInfo.Name);
            si.AddValue(ResXCommon.CommentStr, nodeInfo.Comment);
            si.AddValue(ResXCommon.TypeStr, nodeInfo.TypeName);
            si.AddValue(ResXCommon.MimeTypeStr, nodeInfo.MimeType);
            si.AddValue(ResXCommon.ValueStr, nodeInfo.ValueData);
            si.AddValue(ResXCommon.AliasStr, nodeInfo.AssemblyAliasValue);
            si.AddValue("BasePath", nodeInfo.BasePath);
            si.AddValue("CompatibleFormat", nodeInfo.CompatibleFormat);
            // no fileRef is needed, it is retrieved from nodeInfo
        }

        private ResXDataNode(SerializationInfo info, StreamingContext context)
        {
            nodeInfo = new DataNodeInfo
                {
                    Name = info.GetString(ResXCommon.NameStr),
                    Comment = info.GetString(ResXCommon.CommentStr),
                    TypeName = info.GetString(ResXCommon.TypeStr),
                    MimeType = info.GetString(ResXCommon.MimeTypeStr),
                    ValueData = info.GetString(ResXCommon.ValueStr),
                    AssemblyAliasValue = info.GetString(ResXCommon.AliasStr),
                    BasePath = info.GetString("BasePath"),
                    CompatibleFormat = info.GetBoolean("CompatibleFormat")
                };
            InitFileRef();
        }

        #region Overrides of Object

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            if (fileRef != null)
                return fileRef.ToString();
            object valueCopy = value;
            if (valueCopy != null)
                return valueCopy.ToString();
            DataNodeInfo nodeInfoCopy = nodeInfo;
            if (nodeInfoCopy != null)
                return nodeInfoCopy.ValueData ?? String.Empty;
            return base.ToString();
        }

        #endregion

        private sealed class ResXSerializationBinder : SerializationBinder
        {
            private ITypeResolutionService typeResolver; // deserialization
            private Func<Type, string> typeNameConverter; // serialization
            private bool compatibleFormat; // serialization

            internal ResXSerializationBinder(ITypeResolutionService typeResolver)
            {
                this.typeResolver = typeResolver;
            }

            internal ResXSerializationBinder(Func<Type, string> typeNameConverter, bool compatibleFormat)
            {
                this.typeNameConverter = typeNameConverter;
                this.compatibleFormat = compatibleFormat;
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                if (typeResolver == null)
                {
                    return null;
                }

                string aqn = typeName + ", " + assemblyName;

                Type t = typeResolver.GetType(aqn);
                if (t != null)
                    return t;

                // the original WinForms version fails for generic types. We do the same: we strip either the version
                // or full assembly part from the type
                string strippedName = StripTypeName(aqn, true);
                if (strippedName != aqn)
                    t = typeResolver.GetType(strippedName) ?? typeResolver.GetType(StripTypeName(typeName, true));

                //string[] typeParts = origTypeName.Split(new char[] { ',' });

                //// Break up the assembly name from the rest of the assembly strong name.
                //// we try 1) FQN 2) FQN without a version 3) just the short name
                //if (typeParts.Length > 2)
                //{
                //    string partialName = typeParts[0].Trim();

                //    for (int i = 1; i < typeParts.Length; ++i)
                //    {
                //        string s = typeParts[i].Trim();
                //        if (!s.StartsWith("Version=") && !s.StartsWith("version="))
                //        {
                //            partialName = partialName + ", " + s;
                //        }
                //    }

                //    t = typeResolver.GetType(partialName) ?? typeResolver.GetType(typeParts[0].Trim());
                //}

                // Binder couldn't handle it, let the default loader take over.
                return t;
            }

            private static string StripTypeName(string origTypeName, bool removeVersionOnly)
            {
                int genericEnd = origTypeName.LastIndexOf(']');
                int asmNamePos = origTypeName.IndexOf(',', genericEnd + 1);

                if (asmNamePos < 0 && genericEnd < 0)
                    return origTypeName;

                string typeName = asmNamePos < 0 ? origTypeName : origTypeName.Substring(0, asmNamePos);
                string rebuiltTypeName = typeName;

                // generic or array type
                if (genericEnd >= 0)
                {
                    string genericTypeName;
                    string[] genericTypeParams;
                    int[] arrayRanks;
                    Reflector.GetNameAndIndices(typeName, out genericTypeName, out genericTypeParams, out arrayRanks);
                    if (genericTypeParams != null)
                        rebuiltTypeName = RebuildTypeName(genericTypeName, genericTypeParams, arrayRanks, removeVersionOnly);
                }

                // assembly part is included: removing it entirely or just the version part
                if (asmNamePos >= 0)
                {
                    var typeParts = origTypeName.Substring(asmNamePos + 1).Split(',').Select(s => s.Trim());

                    // returning the type name only
                    if (!removeVersionOnly)
                        return rebuiltTypeName;

                    // removing the assembly part only
#if NET35
                    rebuiltTypeName += ", " + String.Join(", ", typeParts.Where(s => !s.StartsWith("Version=", StringComparison.OrdinalIgnoreCase)).ToArray());
#elif NET40 || NET45
                    rebuiltTypeName += ", " + String.Join(", ", typeParts.Where(s => !s.StartsWith("Version=", StringComparison.OrdinalIgnoreCase)));
#else
#error Unsupported .NET version
#endif
                }

                return rebuiltTypeName;
            }

            private static string RebuildTypeName(string genericTypeName, string[] genericTypeParams, int[] arrayRanks, bool removeVersionOnly)
            {
                StringBuilder result = new StringBuilder(genericTypeName + "[");
                for (int i = 0; i < genericTypeParams.Length; i++)
                {
                    if (i != 0)
                        result.Append(", ");

                    result.Append('[');
                    result.Append(StripTypeName(genericTypeParams[i], removeVersionOnly));
                    result.Append(']');
                }
                result.Append(']');

                if (arrayRanks != null)
                {
                    foreach (int rank in arrayRanks)
                    {
                        result.Append('[');
                        switch (rank)
                        {
                            case -1:
                                result.Append('*');
                                break;
                            case 1:
                                break;
                            default:
                                result.Append(",".Repeat(rank - 1));
                                break;
                        }
                        result.Append(']');
                    }
                }

                return result.ToString();
            }

#if NET40 || NET45
            //
            // Get the multitarget-aware string representation for the give type.
            public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                // Normally we don't change origTypeName when changing the target framework, 
                // only assembly version or assembly name might change, thus we are setting 
                // origTypeName only if it changed with the framework version.
                // If binder passes in a null, BinaryFormatter will use the original value or 
                // for un-serializable types will redirect to another type. 
                // For example:
                //
                // Encoding = Encoding.GetEncoding("shift_jis");
                // public Encoding Encoding { get; set; }
                // property type (Encoding) is abstract, but the value is instantiated to a specific class,
                // and should be serialized as a specific class in order to be able to instantiate the result.
                //
                // another example are singleton objects like DBNull.Value which are serialized by System.UnitySerializationHolder
                typeName = null;
                if (typeNameConverter != null)
                {
                    string assemblyQualifiedTypeName = ResXCommon.GetAssemblyQualifiedName(serializedType, typeNameConverter, compatibleFormat);
                    if (!String.IsNullOrEmpty(assemblyQualifiedTypeName))
                    {
                        int genericEnd = assemblyQualifiedTypeName.LastIndexOf(']');
                        int asmNamePos = assemblyQualifiedTypeName.IndexOf(',', genericEnd + 1);
                        if (asmNamePos > 0 && asmNamePos < assemblyQualifiedTypeName.Length - 1)
                        {
                            assemblyName = assemblyQualifiedTypeName.Substring(asmNamePos + 1).TrimStart();
                            string newTypeName = assemblyQualifiedTypeName.Substring(0, asmNamePos);
                            if (!string.Equals(newTypeName, serializedType.FullName, StringComparison.InvariantCulture))
                            {
                                typeName = newTypeName;
                            }
                            return;
                        }
                    }
                }

                base.BindToName(serializedType, out assemblyName, out typeName);
            }
#elif !NET35
#error .NET version is not set or not supported!
#endif

        }
    }

    internal class DataNodeInfo
    {
        internal bool CompatibleFormat;
        internal string Name;
        internal string Comment;
        internal string TypeName;
        internal string AssemblyAliasValue;
        internal string MimeType;
        internal string ValueData;
        internal string BasePath;
        internal int Line; //only used to track position in the reader
        internal int Column; //only used to track position in the reader

        internal DataNodeInfo Clone()
        {
            return (DataNodeInfo)MemberwiseClone();
        }

        internal static DataNodeInfo InitFromWinForms(object nodeInfoWinForms)
        {
            object pos = Accessors.DataNodeInfo_ReaderPosition_Get(nodeInfoWinForms);
            return new DataNodeInfo
                {
                    CompatibleFormat = true,
                    Name = Accessors.DataNodeInfo_Name_Get(nodeInfoWinForms),
                    Comment = Accessors.DataNodeInfo_Comment_Get(nodeInfoWinForms),
                    TypeName = Accessors.DataNodeInfo_TypeName_Get(nodeInfoWinForms),
                    MimeType = Accessors.DataNodeInfo_MimeType_Get(nodeInfoWinForms),
                    ValueData = Accessors.DataNodeInfo_ValueData_Get(nodeInfoWinForms),
                    Line = Accessors.Point_Y_Get(pos),
                    Column = Accessors.Point_X_Get(pos)
                };
        }

        internal void DetectCompatibleFormat()
        {
            CompatibleFormat = MimeType != ResXCommon.KGySoftSerializedObjectMimeType 
                && (TypeName == null || (!TypeName.StartsWith(ResXCommon.ResXFileRefNameKGySoft, StringComparison.Ordinal) && !TypeName.StartsWith(ResXCommon.ResXNullRefNameKGySoft, StringComparison.Ordinal)));
        }
    }

    // This class implements a partial type resolver for the BinaryFormatter.
    // This is needed to be able to read binary serialized content from older
    // NDP types and map them to newer versions.
    //

    //    internal class AssemblyNamesTypeResolutionService : ITypeResolutionService
    //    {
    //        private string resxFileRefName = "System.Resources.ResXFileRef, System.Windows.Forms";

    //        private AssemblyName[] names;
    //        private Hashtable cachedAssemblies;
    //        private Hashtable cachedTypes;

    //        private static string NetFrameworkPath = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), "Microsoft.Net\\Framework");

    //        internal AssemblyNamesTypeResolutionService(AssemblyName[] names)
    //        {
    //            this.names = names;
    //        }

    //        public Assembly GetAssembly(AssemblyName name)
    //        {
    //            return GetAssembly(name, true);
    //        }

    //        public Assembly GetAssembly(AssemblyName name, bool throwOnError)
    //        {

    //            Assembly result = null;

    //            if (cachedAssemblies == null)
    //            {
    //                cachedAssemblies = Hashtable.Synchronized(new Hashtable());
    //            }

    //            if (cachedAssemblies.Contains(name))
    //            {
    //                result = cachedAssemblies[name] as Assembly;
    //            }

    //            if (result == null)
    //            {
    //                // try to load it first from the gac
    //#pragma warning disable 0618
    //                //Although LoadWithPartialName is obsolete, we still have to call it: changing 
    //                //this would be breaking in cases where people edited their resource files by
    //                //hand.
    //                result = Assembly.LoadWithPartialName(name.FullName);
    //#pragma warning restore 0618
    //                if (result != null)
    //                {
    //                    cachedAssemblies[name] = result;
    //                }
    //                else if (names != null)
    //                {
    //                    for (int i = 0; i < names.Length; i++)
    //                    {
    //                        if (name.Equals(names[i]))
    //                        {
    //                            try
    //                            {
    //                                result = Assembly.LoadFrom(GetPathOfAssembly(name));
    //                                if (result != null)
    //                                {
    //                                    cachedAssemblies[name] = result;
    //                                }
    //                            }
    //                            catch
    //                            {
    //                                if (throwOnError)
    //                                {
    //                                    throw;
    //                                }
    //                            }
    //                        }
    //                    }
    //                }
    //            }

    //            return result;
    //        }

    //        public string GetPathOfAssembly(AssemblyName name)
    //        {
    //            return name.CodeBase;
    //        }


    //        public Type GetType(string name)
    //        {
    //            return GetType(name, true);
    //        }

    //        public Type GetType(string name, bool throwOnError)
    //        {
    //            return GetType(name, throwOnError, false);
    //        }

    //        public Type GetType(string name, bool throwOnError, bool ignoreCase)
    //        {
    //            // mapping winforms.ResxFileRef to KGySoft one
    //            if (name.StartsWith(resxFileRefName))
    //                return typeof(ResXFileRef);

    //            Type result = null;

    //            // Check type cache first
    //            if (cachedTypes == null)
    //            {
    //                cachedTypes = Hashtable.Synchronized(new Hashtable(StringComparer.Ordinal));
    //            }

    //            if (cachedTypes.Contains(name))
    //            {
    //                result = cachedTypes[name] as Type;
    //                return result;
    //            }

    //            // Missed in cache, try to resolve the type. First try to resolve in the GAC
    //            if (name.IndexOf(',') != -1)
    //            {
    //                result = Type.GetType(name, false, ignoreCase);
    //            }

    //            //
    //            // Did not find it in the GAC, check the reference assemblies
    //            if (result == null && names != null)
    //            {
    //                //
    //                // If the type is assembly qualified name, we sort the assembly names
    //                // to put assemblies with same name in the front so that they can
    //                // be searched first.
    //                int pos = name.IndexOf(',');
    //                if (pos > 0 && pos < name.Length - 1)
    //                {
    //                    string fullName = name.Substring(pos + 1).Trim();
    //                    AssemblyName assemblyName = null;
    //                    try
    //                    {
    //                        assemblyName = new AssemblyName(fullName);
    //                    }
    //                    catch
    //                    {
    //                    }

    //                    if (assemblyName != null)
    //                    {
    //                        List<AssemblyName> assemblyList = new List<AssemblyName>(names.Length);
    //                        for (int i = 0; i < names.Length; i++)
    //                        {
    //                            if (string.Compare(assemblyName.Name, names[i].Name, StringComparison.OrdinalIgnoreCase) == 0)
    //                            {
    //                                assemblyList.Insert(0, names[i]);
    //                            }
    //                            else
    //                            {
    //                                assemblyList.Add(names[i]);
    //                            }
    //                        }
    //                        names = assemblyList.ToArray();
    //                    }
    //                }

    //                // Search each reference assembly
    //                for (int i = 0; i < names.Length; i++)
    //                {
    //                    Assembly a = GetAssembly(names[i], false);
    //                    if (a != null)
    //                    {
    //                        result = a.GetType(name, false, ignoreCase);
    //                        if (result == null)
    //                        {
    //                            int indexOfComma = name.IndexOf(",");
    //                            if (indexOfComma != -1)
    //                            {
    //                                string shortName = name.Substring(0, indexOfComma);
    //                                result = a.GetType(shortName, false, ignoreCase);
    //                            }
    //                        }
    //                    }

    //                    if (result != null)
    //                        break;
    //                }
    //            }

    //            if (result == null && throwOnError)
    //            {
    //                throw new ArgumentException(/*TODO: SR.GetString(SR.InvalidResXNoType, name)*/);
    //            }

    //            if (result != null)
    //            {
    //                // Only cache types from .Net framework or GAC because they don't need to update.
    //                // For simplicity, don't cache custom types
    //                if (result.Assembly.GlobalAssemblyCache || IsNetFrameworkAssembly(result.Assembly.Location))
    //                {
    //                    cachedTypes[name] = result;
    //                }
    //            }

    //            return result;
    //        }

    //        /// <devdoc>
    //        /// This is matching %windir%\Microsoft.NET\Framework*, so both 32bit and 64bit framework will be covered.
    //        /// </devdoc>
    //        private bool IsNetFrameworkAssembly(string assemblyPath)
    //        {
    //            return assemblyPath != null && assemblyPath.StartsWith(NetFrameworkPath, StringComparison.OrdinalIgnoreCase);
    //        }

    //        public void ReferenceAssembly(AssemblyName name)
    //        {
    //            throw new NotSupportedException();
    //        }

    //    }
}
