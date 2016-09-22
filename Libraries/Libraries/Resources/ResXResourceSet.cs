namespace KGySoft.Libraries.Resources
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.IO;
    using System.Resources;

    /// <summary>
    /// Represents all resources in an XML resource (.resx) file.
    /// </summary>
    // TODO:
    // - ctor(TextReader) és amit még a reader fogadni tud
    // - static LoadFrom(string)
    // + override GetString/etc an use locks, dictionaries
    // + disposed, ha a resources null
    // - ha streamből lett létrehozva, a Save() csak akkor használható, ha a FileName-t besetteljük
    // - Save(fileName), Save(Stream), Save(StringBuilder/TextWriter) és amit még a Writer fogadni tud
    //   - forceEmbedded paraméter
    //   - compatibleFormat paraméter
    // + SafeMode: default=false
    // + AutoCleanup - AutoFreeXmlData: default=true
    // ? Serialization: ISerializable, forget base class members (Note: RuntimeResourceSet is not serializable)
    // - SetObject/Meta/Alias: ha az ignoreCase dictionary nem null, ahhoz is hozzáadni
    // Remarks todo:
    // - Compatible with winforms version but see constructors
    // - Performance is much better because only the actually accessed objects are deserialized.
    // - Safer, even if SafeMode is off, because objects are deserialized only when they are accessed.
    // - load/Save to stream and textwriter, too. The manager classes work with multiple sets and files.
    // - GetxxxEnumerator metódusokhoz leírni, hogy az enumerátor DictionaryEntry.Value-ja SafeMMode-tól függ
    // Incompatibility!!!: (ha lehet, változtatni, hogy az egy stringes verzió itt is filename legyen, talán string,string, mindkettő optional, első filenév második basepath)
    // - ctor(string) itt basePath, file-ból betöltéshez ctor(string, string) kell
    // [Serializable]
    // legyen sealed vagy override-oljuk a ReadResources-t, ami dobjon NotSupportedException-t vagy ne csináljon semmit
    public class ResXResourceSet : ResourceSet, IExpandoResourceSet, IResXResourceContainer, IExpandoResourceSetInternal, IEnumerable
    {
        private Dictionary<string, ResXDataNode> resources;
        private Dictionary<string, ResXDataNode> resourcesIgnoreCase;
        private Dictionary<string, ResXDataNode> metadata;
        private Dictionary<string, ResXDataNode> metadataIgnoreCase;
        private Dictionary<string, string> aliases;
        private readonly string fileName;
        private bool safeMode;
        private bool autoFreeXmlData = true;
        private string basePath;
        private bool isModified;
        private int version;

        /// <summary>
        /// If this <see cref="ResXResourceSet"/> has been created from a file, returns the name of the original file.
        /// This property will not change if the <see cref="ResXResourceSet"/> is saved into another file.
        /// </summary>
        public string FileName
        {
            get { return fileName; }
        }

        /// <summary>
        /// Gets or sets whether the <see cref="ResXResourceSet"/> works in safe mode. In safe mode the retrieved
        /// objects are not deserialized automatically. See Remarks section for details.
        /// <br/>Default value: <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>When <c>SafeMode</c> is <c>true</c>, the <see cref="GetObject(string,bool)"/> and <see cref="GetMetaObject"/> methods
        /// return <see cref="ResXDataNode"/> instances instead of deserialized objects. You can retrieve the deserialized
        /// objects on demand by calling the <see cref="ResXDataNode.GetValue"/> method on the <see cref="ResXDataNode"/> instance.</para>
        /// <para>When <c>SafeMode</c> is <c>true</c>, the <see cref="GetString(string,bool)"/> and <see cref="GetMetaString"/> methods
        /// work for every defined item in the resource set. For non-string elements the raw XML string value will be returned.</para>
        /// <para>If <c>SafeMode</c> is <c>true</c>, the <see cref="AutoFreeXmlData"/> property is ignored. The raw XML data of a node
        /// can be freed by calling the <see cref="ResXDataNode.GetValue"/>.</para>
        /// </remarks>
        /// <seealso cref="ResXResourceReader.UseResXDataNodes"/>
        /// <seealso cref="ResXResourceManager.SafeMode"/>
        /// <seealso cref="HybridResourceManager.SafeMode"/>
        /// <seealso cref="AutoFreeXmlData"/>
        public bool SafeMode
        {
            get { return safeMode; }
            set
            {
                if (resources == null)
                    throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                safeMode = value;
            }
        }

        /// <summary>
        /// Gets the base path for the relative file paths specified in a <see cref="ResXFileRef"/> object.
        /// </summary>
        /// <returns>
        /// A path that, if prepended to the relative file path specified in a <see cref="ResXFileRef"/> object, yields an absolute path to a resource file.
        /// </returns>
        public string BasePath
        {
            get { return basePath; }

            //// TODO: delete
            //set
            //{
            //    if (resources == null)
            //        throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
            //    basePath = value;
            //}
        }

        // TODO: ez majd csak a Writerben és DRM-ben lesz property, minden másik osztályban a save-ben megmondható
        //public bool CompatibilityMode
        //{
        //    get { return compatibilityMode; }
        //    set
        //    {
        //        if (resources == null)
        //            throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
        //        compatibilityMode = value;
        //    }
        //}

        /// <summary>
        /// Gets or sets whether the raw XML data of the stored elements should be freed once their value has been deserialized.
        /// <br/>Default value: <c>true</c>.
        /// </summary>
        /// <value>
        /// <c>true</c> to free the stored raw XML data automatically; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// <para>If the value of the property is <c>true</c>, then the stored raw XML data will be automatically freed when
        /// a resource or metadata item is obtained by <see cref="GetObject(string)"/>, <see cref="GetMetaObject"/>, <see cref="GetString(string)"/> or <see cref="GetMetaString"/> and their overloads.
        /// The raw XML data is re-generated on demand if needed, it is transparent to the user.</para>
        /// <para>If <see cref="SafeMode"/> is <c>true</c>, this property has no effect and the clean-up can be controlled by the <see cref="ResXDataNode.GetValue"/> method.</para>
        /// </remarks>
        public bool AutoFreeXmlData
        {
            get { return autoFreeXmlData; }
            set { autoFreeXmlData = value; }
        }

        /// <summary>
        /// Gets whether this <see cref="ResXResourceSet" /> instance is modified.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is modified; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        public bool IsModified
        {
            get
            {
                if (resources == null)
                    throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                return isModified;
            }
        }

        private ResXResourceSet(string basePath, ResXResourceReader reader): this(basePath)
        {
            using (reader)
            {
                // this will not deserialize anything just quickly parses the .resx and stores the raw nodes
                reader.ReadAllInternal(resources, metadata, aliases);
            }
        }

        /// <summary>
        /// Initializes a new, empty instance of the <see cref="ResXResourceSet"/> class.
        /// </summary>
        /// <param name="basePath">The base path for the relative file paths specified in the <see cref="ResXFileRef"/> objects,
        /// which will be added to this empty <see cref="ResXResourceSet"/> instance.</param>
        public ResXResourceSet(string basePath = null)
        {
            Table = null; // base ctor initializes that; however, we don't need it.
            this.basePath = basePath;
            resources = new Dictionary<string, ResXDataNode>();
            metadata = new Dictionary<string, ResXDataNode>(0);
            aliases = new Dictionary<string, string>(0);
        }

        /// <summary>
        /// Initializes a new instance of a <see cref="ResXResourceSet"/> class using the <see cref="ResXResourceReader"/> that opens and reads resources from the specified file.
        /// </summary>
        /// <param name="fileName">The name of the file to read resources from. </param>
        /// <param name="basePath">The base path for the relative file paths specified in a <see cref="ResXFileRef"/> object. If <see langword="null"/>, the directory part of <paramref name="fileName"/> will be used.</param>
        public ResXResourceSet(string fileName, string basePath)
            : this(basePath, new ResXResourceReader(fileName) { BasePath = basePath ?? Path.GetDirectoryName(fileName) })
        {
            this.fileName = fileName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceSet"/> class using the <see cref="ResXResourceReader"/> to read resources from the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> of resources to be read. The stream should refer to a valid resource file content.</param>
        /// <param name="basePath">The base path for the relative file paths specified in a <see cref="ResXFileRef"/> object. If <see langword="null"/>, the current directory will be used.</param>
        public ResXResourceSet(Stream stream, string basePath = null)
            : this(basePath, new ResXResourceReader(stream) { BasePath = basePath })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceSet"/> class using the <see cref="ResXResourceReader"/> to read resources from the specified <paramref name="textReader"/>.
        /// </summary>
        /// <param name="textReader">The <see cref="TextReader"/> of resources to be read. The reader should refer to a valid resource file content.</param>
        /// <param name="basePath">The base path for the relative file paths specified in a <see cref="ResXFileRef"/> object. If <see langword="null"/>, the current directory will be used.</param>
        public ResXResourceSet(TextReader textReader, string basePath = null)
            : this(basePath, new ResXResourceReader(textReader) { BasePath = basePath })
        {
        }

        /// <summary>
        /// Returns the preferred resource reader class for this kind of <see cref="ResXResourceSet"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of the preferred resource reader for this kind of <see cref="ResXResourceSet"/>.
        /// </returns>
        public override Type GetDefaultReader()
        {
            return typeof(ResXResourceReader);
        }

        /// <summary>
        /// Returns the preferred resource writer class for this kind of <see cref="ResXResourceSet"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of the preferred resource writer for this kind of <see cref="ResXResourceSet"/>.
        /// </returns>
        public override Type GetDefaultWriter()
        {
            return typeof(ResXResourceWriter);
        }

        /// <summary>
        /// Releases resources (other than memory) associated with the current instance, closing internal managed objects if requested.
        /// </summary>
        /// <param name="disposing">Indicates whether the objects contained in the current instance should be explicitly closed.</param>
        protected override void Dispose(bool disposing)
        {
            resources = null;
            metadata = null;
            aliases = null;
            resourcesIgnoreCase = null;
            metadataIgnoreCase = null;
            basePath = null;
        }

        /// <summary>
        /// Returns an <see cref="IDictionaryEnumerator" /> that can iterate through the resources of the <see cref="ResXResourceSet" />.
        /// </summary>
        /// <returns>
        /// An <see cref="IDictionaryEnumerator" /> for this <see cref="ResXResourceSet" />.
        /// </returns>
        public override IDictionaryEnumerator GetEnumerator()
        {
            return GetEnumeratorInternal(ResXEnumeratorModes.Resources);
        }

        /// <summary>
        /// Returns an <see cref="IDictionaryEnumerator" /> that can iterate through the metadata of the <see cref="ResXResourceSet" />.
        /// </summary>
        /// <returns>
        /// An <see cref="IDictionaryEnumerator" /> for this <see cref="ResXResourceSet" />.
        /// </returns>
        public IDictionaryEnumerator GetMetadataEnumerator()
        {
            return GetEnumeratorInternal(ResXEnumeratorModes.Metadata);
        }

        /// <summary>
        /// Returns an <see cref="IDictionaryEnumerator" /> that can iterate through the aliases of the <see cref="ResXResourceSet" />.
        /// </summary>
        /// <returns>
        /// An <see cref="IDictionaryEnumerator" /> for this <see cref="ResXResourceSet" />.
        /// </returns>
        public IDictionaryEnumerator GetAliasEnumerator()
        {
            return GetEnumeratorInternal(ResXEnumeratorModes.Aliases);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorInternal(ResXEnumeratorModes.Resources);
        }

        private IDictionaryEnumerator GetEnumeratorInternal(ResXEnumeratorModes mode)
        {
            var syncObj = resources;
            if (syncObj == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            lock (syncObj)
            {
                return new ResXResourceEnumerator(this, mode, version);
            }
        }

        /// <summary>
        /// Searches for a resource object with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Case-sensitive name of the resource to search for.</param>
        /// <returns>
        /// The requested resource, or when <see cref="SafeMode"/> is <c>true</c>, a <see cref="ResXDataNode"/> instance
        /// from which the resource can be obtained. If the requested <paramref name="name"/> cannot be found, <see langword="null"/> is returned.
        /// </returns>
        /// <remarks>
        /// When <see cref="SafeMode"/> is <c>true</c>, the returned object is a <see cref="ResXDataNode"/> instance
        /// from which the resource can be obtained.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        public override object GetObject(string name)
        {
            return GetValueInternal(name, false, false, safeMode, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Searches for a resource object with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the resource to search for.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored.</param>
        /// <returns>
        /// The requested resource, or when <see cref="SafeMode"/> is <c>true</c>, a <see cref="ResXDataNode"/> instance
        /// from which the resource can be obtained. If the requested <paramref name="name"/> cannot be found, <see langword="null"/> is returned.
        /// </returns>
        /// <remarks>
        /// When <see cref="SafeMode"/> is <c>true</c>, the returned object is a <see cref="ResXDataNode"/> instance
        /// from which the resource can be obtained.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        public override object GetObject(string name, bool ignoreCase)
        {
            return GetValueInternal(name, ignoreCase, false, safeMode, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Searches for a <see cref="string" /> resource with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the resource to search for.</param>
        /// <returns>
        /// The <see cref="string"/> value of a resource.
        /// If <see cref="SafeMode"/> is <c>false</c>, an <see cref="InvalidOperationException"/> will be thrown for
        /// non-string resources. If <see cref="SafeMode"/> is <c>true</c>, the raw XML value will be returned for non-string resources.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <c>false</c> and the type of the resource is not <see cref="string"/>.</exception>
        public override string GetString(string name)
        {
            return (string)GetValueInternal(name, false, true, safeMode, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Searches for a <see cref="string" /> resource with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the resource to search for.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored.</param>
        /// <returns>
        /// The <see cref="string"/> value of a resource.
        /// If <see cref="SafeMode"/> is <c>false</c>, an <see cref="InvalidOperationException"/> will be thrown for
        /// non-string resources. If <see cref="SafeMode"/> is <c>true</c>, the raw XML value will be returned for non-string resources.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <c>false</c> and the type of the resource is not <see cref="string"/>.</exception>
        public override string GetString(string name, bool ignoreCase)
        {
            return (string)GetValueInternal(name, ignoreCase, true, safeMode, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Searches for a metadata object with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the metadata to search for.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored.</param>
        /// <returns>
        /// The requested metadata, or when <see cref="SafeMode"/> is <c>true</c>, a <see cref="ResXDataNode"/> instance
        /// from which the metadata can be obtained. If the requested <paramref name="name"/> cannot be found, <see langword="null"/> is returned.
        /// </returns>
        /// <remarks>
        /// When <see cref="SafeMode"/> is <c>true</c>, the returned object is a <see cref="ResXDataNode"/> instance
        /// from which the metadata can be obtained.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        public object GetMetaObject(string name, bool ignoreCase = false)
        {
            return GetValueInternal(name, ignoreCase, false, safeMode, metadata, ref metadataIgnoreCase);
        }

        /// <summary>
        /// Searches for a <see cref="string" /> metadata with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the metadata to search for.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored.</param>
        /// <returns>
        /// The <see cref="string"/> value of a metadata.
        /// If <see cref="SafeMode"/> is <c>false</c>, an <see cref="InvalidOperationException"/> will be thrown for
        /// non-string metadata. If <see cref="SafeMode"/> is <c>true</c>, the raw XML value will be returned for non-string metadata.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <c>false</c> and the type of the metadata is not <see cref="string"/>.</exception>
        public string GetMetaString(string name, bool ignoreCase = false)
        {
            return (string)GetValueInternal(name, ignoreCase, true, safeMode, metadata, ref metadataIgnoreCase);
        }

        /// <summary>
        /// Gets the assembly name for the specified <paramref name="alias"/>.
        /// </summary>
        /// <param name="alias">The alias of the the assembly name, which should be retrieved.</param>
        /// <returns>The assembly name of the <paramref name="alias"/>, or <see langword="null"/> if there is no such alias defined.</returns>
        /// <remarks>If an alias is redefined in the .resx file, then this method returns the last occurrence of the alias value.</remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="alias"/> is <see langword="null"/>.</exception>
        public string GetAliasValue(string alias)
        {
            var dict = aliases;
            if (dict == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            string result;
            lock (dict)
            {
                if (!dict.TryGetValue(alias, out result))
                    return null;                
            }

            return result;
        }

        /// <summary>
        /// Adds or replaces a resource object in the current <see cref="ResXResourceSet" /> with the specified <paramref name="name" />.
        /// </summary>
        /// <param name="name">Name of the resource to set. Casing is not ignored.</param>
        /// <param name="value">The resource value to set.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet" /> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>If <paramref name="value"/> is <see langword="null"/>, a null reference will be explicitly stored.
        /// Its effect is similar to the <see cref="RemoveObject"/> method (<see cref="GetObject(string)"/> will return <see langword="null"/> in both cases),
        /// but if <see langword="null"/> has been set, it will returned among the results of the <see cref="GetEnumerator"/> method.</para>
        /// </remarks>
        public void SetObject(string name, object value)
        {
            SetValueInternal(name, value, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Adds or replaces a metadata object in the current <see cref="ResXResourceSet" /> with the specified <paramref name="name" />.
        /// </summary>
        /// <param name="name">Name of the metadata value to set.</param>
        /// <param name="value">The metadata value to set. If <see langword="null" />, the value will be removed.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>If <paramref name="value"/> is <see langword="null"/>, a null reference will be explicitly stored.
        /// Its effect is similar to the <see cref="RemoveMetaObject"/> method (<see cref="GetMetaObject"/> will return <see langword="null"/> in both cases),
        /// but if <see langword="null"/> has been set, it will returned among the results of the <see cref="GetMetadataEnumerator"/> method.</para>
        /// </remarks>
        public void SetMetaObject(string name, object value)
        {
            SetValueInternal(name, value, metadata, ref metadataIgnoreCase);
        }

        /// <summary>
        /// Adds or replaces an assembly alias value in the current <see cref="ResXResourceSet"/>.
        /// </summary>
        /// <param name="alias">The alias name to use instead of <paramref name="assemblyName"/> in the saved .resx file.</param>
        /// <param name="assemblyName">The fully or partially qualified name of the assembly.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="assemblyName"/> or <paramref name="alias"/> is <see langword="null"/>.</exception>
        public void SetAliasValue(string alias, string assemblyName)
        {
            var dict = aliases;
            if (dict == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (alias == null)
                throw new ArgumentNullException("alias", Res.Get(Res.ArgumentNull));

            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName", Res.Get(Res.ArgumentNull));

            lock (dict)
            {
                string asmName;
                if (dict.TryGetValue(alias, out asmName) && asmName == assemblyName)
                    return;

                dict[alias] = assemblyName;
                isModified = true;
                version++;
            }
        }

        /// <summary>
        /// Removes a resource object from the current <see cref="ResXResourceSet"/> with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the resource value to remove.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        public void RemoveObject(string name)
        {
            RemoveValueInternal(name, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Removes a metadata object in the current <see cref="ResXResourceSet"/> with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the metadata value to remove.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        public void RemoveMetaObject(string name)
        {
            RemoveValueInternal(name, metadata, ref metadataIgnoreCase);            
        }

        /// <summary>
        /// Removes an assembly alias value in the current <see cref="ResXResourceSet"/>.
        /// </summary>
        /// <param name="alias">The alias, which should be removed.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="alias"/> is <see langword="null"/>.</exception>
        public void RemoveAliasValue(string alias)
        {
            var dict = aliases;
            if (dict == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (alias == null)
                throw new ArgumentNullException("alias", Res.Get(Res.ArgumentNull));

            lock (dict)
            {
                if (!dict.ContainsKey(alias))
                    return;

                dict.Remove(alias);
                isModified = true;
            }
        }

        /// <summary>
        /// Saves the <see cref="ResXResourceSet" /> to the specified file.</summary>
        /// <param name="fileName">The location of the file where you want to save the resources.</param>
        /// <param name="basePath">A new base path for the file paths specified in the <see cref="ResXFileRef"/> objects. If <see langword="null"/>,
        /// the original <see cref="BasePath"/> will be used. The file paths in the saved .resx file will be relative to the <paramref name="basePath"/>.
        /// Applicable if <paramref name="forceEmbeddedResources"/> is <c>false</c>.</param>
        /// <param name="compatibleFormat">If set to <c>true</c>, the result .resx file can be read by the system <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx">ResXResourceReader</a> class
        /// and the Visual Studio Resource Editor. If set to <c>false</c>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />), but the result can be read only by <see cref="ResXResourceReader" /><br />Default value: <c>false</c>.</param>
        /// <param name="forceEmbeddedResources">If set to <c>true</c> the resources using a file reference (<see cref="ResXFileRef" />) will be replaced into embedded resources.
        /// <br/>Default value: <c>false</c></param>
        /// <seealso cref="ResXResourceWriter"/>
        /// <seealso cref="ResXResourceWriter.CompatibleFormat"/>
        public void Save(string fileName, bool compatibleFormat = false, bool forceEmbeddedResources = false, string basePath = null)
        {
            using (var writer = new ResXResourceWriter(fileName) { BasePath = basePath ?? this.basePath, CompatibleFormat = compatibleFormat })
            {
                Save(writer, forceEmbeddedResources);
            }
        }

        /// <summary>
        /// Saves the <see cref="ResXResourceSet" /> to the specified file.</summary>
        /// <param name="stream">The stream to which you want to save.</param>
        /// <param name="basePath">A new base path for the file paths specified in the <see cref="ResXFileRef"/> objects. If <see langword="null"/>,
        /// the original <see cref="BasePath"/> will be used. The file paths in the saved .resx file will be relative to the <paramref name="basePath"/>.
        /// Applicable if <paramref name="forceEmbeddedResources"/> is <c>false</c>.</param>
        /// <param name="compatibleFormat">If set to <c>true</c>, the result .resx file can be read by the system <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx">ResXResourceReader</a> class
        /// and the Visual Studio Resource Editor. If set to <c>false</c>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />), but the result can be read only by <see cref="ResXResourceReader" /><br />Default value: <c>false</c>.</param>
        /// <param name="forceEmbeddedResources">If set to <c>true</c> the resources using a file reference (<see cref="ResXFileRef" />) will be replaced into embedded resources.
        /// <br/>Default value: <c>false</c></param>
        /// <seealso cref="ResXResourceWriter"/>
        /// <seealso cref="ResXResourceWriter.CompatibleFormat"/>
        public void Save(Stream stream, bool compatibleFormat = false, bool forceEmbeddedResources = false, string basePath = null)
        {
            using (var writer = new ResXResourceWriter(stream) { BasePath = basePath ?? this.basePath, CompatibleFormat = compatibleFormat })
            {
                Save(writer, forceEmbeddedResources);
            }
        }

        /// <summary>
        /// Saves the <see cref="ResXResourceSet" /> to the specified file.</summary>
        /// <param name="textWriter">The text writer to which you want to save.</param>
        /// <param name="basePath">A new base path for the file paths specified in the <see cref="ResXFileRef"/> objects. If <see langword="null"/>,
        /// the original <see cref="BasePath"/> will be used. The file paths in the saved .resx file will be relative to the <paramref name="basePath"/>.
        /// Applicable if <paramref name="forceEmbeddedResources"/> is <c>false</c>.</param>
        /// <param name="compatibleFormat">If set to <c>true</c>, the result .resx file can be read by the system <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx">ResXResourceReader</a> class
        /// and the Visual Studio Resource Editor. If set to <c>false</c>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />), but the result can be read only by <see cref="ResXResourceReader" /><br />Default value: <c>false</c>.</param>
        /// <param name="forceEmbeddedResources">If set to <c>true</c> the resources using a file reference (<see cref="ResXFileRef" />) will be replaced into embedded resources.
        /// <br/>Default value: <c>false</c></param>
        /// <seealso cref="ResXResourceWriter"/>
        /// <seealso cref="ResXResourceWriter.CompatibleFormat"/>
        public void Save(TextWriter textWriter, bool compatibleFormat = false, bool forceEmbeddedResources = false, string basePath = null)
        {
            using (var writer = new ResXResourceWriter(textWriter) { BasePath = basePath ?? this.basePath, CompatibleFormat = compatibleFormat })
            {
                Save(writer, forceEmbeddedResources);
            }
        }

        private void Save(ResXResourceWriter writer, bool forceEmbeddedResources)
        {
            var resources = this.resources;
            var metadata = this.metadata;
            var aliases = this.aliases;

            if ((resources ?? metadata ?? (object)aliases) == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            // 1. Adding existing aliases (writing them on-demand) - non existing ones will be auto-generated
            lock (aliases)
            {
                foreach (var alias in aliases)
                {
                    writer.AddAlias(alias.Key, alias.Value);
                }
            }

            // ReSharper disable PossibleNullReferenceException - false alarm, ReSharper does not recognise the effect of ?? operator
            // 2. Adding resources (not freeing xml data during saving)
            bool adjustPath = basePath != null && basePath != writer.BasePath;
            lock (resources)
            {
                foreach (var resource in resources)
                {
                    writer.AddResource(GetNodeToSave(resource.Value, forceEmbeddedResources, adjustPath));
                }
            }

            // 3. Adding metadata
            lock (metadata)
            {
                foreach (var meta in metadata)
                {
                    writer.AddMetadata(GetNodeToSave(meta.Value, forceEmbeddedResources, adjustPath));
                }
            }
            // ReSharper restore PossibleNullReferenceException

            writer.Generate();
            isModified = false;
        }

        private ResXDataNode GetNodeToSave(ResXDataNode node, bool forceEmbeddedResources, bool adjustPath)
        {
            ResXFileRef fileRef = node.FileRef;
            if (fileRef == null)
                return node;

            if (forceEmbeddedResources)
                node = new ResXDataNode(node.Name, node.GetValue(null, basePath, !safeMode && autoFreeXmlData));
            else if (adjustPath && !Path.IsPathRooted(fileRef.FileName))
            {
                // Restoring the original full path so the ResXResourceWriter can create a new relative path to the new basePath
                string origPath = Path.GetFullPath(Path.Combine(basePath, fileRef.FileName));
                node = new ResXDataNode(node.Name, new ResXFileRef(origPath, fileRef.TypeName, fileRef.EncodingName));
            }

            return node;
        }

        internal object GetResourceInternal(string name, bool ignoreCase, bool isString, bool asSafe)
        {
            return GetValueInternal(name, ignoreCase, isString, asSafe, resources, ref resourcesIgnoreCase);
        }

        internal object GetMetaInternal(string name, bool ignoreCase, bool isString, bool asSafe)
        {
            return GetValueInternal(name, ignoreCase, isString, asSafe, metadata, ref metadataIgnoreCase);
        }

        /// <summary>
        /// Gets whether the current <see cref="ResXResourceSet"/> contains a resource with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the resource to check.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored.</param>
        /// <returns><c>true</c>, if the current <see cref="ResXResourceSet"/> contains a resource with name <paramref name="name"/>; otherwise, <c>false</c>.</returns>
        public bool ContainsResource(string name, bool ignoreCase)
        {
            return ContainsInternal(name, ignoreCase, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Gets whether the current <see cref="ResXResourceSet"/> contains a metadata with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the metadata to check.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored.</param>
        /// <returns><c>true</c>, if the current <see cref="ResXResourceSet"/> contains a metadata with name <paramref name="name"/>; otherwise, <c>false</c>.</returns>
        public bool ContainsMeta(string name, bool ignoreCase)
        {
            return ContainsInternal(name, ignoreCase, metadata, ref metadataIgnoreCase);
        }

        private bool ContainsInternal(string name, bool ignoreCase, Dictionary<string, ResXDataNode> data, ref Dictionary<string, ResXDataNode> dataCaseInsensitive)
        {
            if (data == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            lock (data)
            {
                if (data.ContainsKey(name))
                    return true;

                if (!ignoreCase)
                    return false;

                if (dataCaseInsensitive == null)
                {
                    dataCaseInsensitive = InitCaseInsensitive(data);
                }

                return dataCaseInsensitive.ContainsKey(name);
            }
        }

        private object GetValueInternal(string name, bool ignoreCase, bool isString, bool asSafe, Dictionary<string, ResXDataNode> data, ref Dictionary<string, ResXDataNode> dataCaseInsensitive)
        {
            if (data == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (name == null)
                throw new ArgumentNullException("name", Res.Get(Res.ArgumentNull));

            lock (data)
            {
                ResXDataNode result;
                if (data.TryGetValue(name, out result))
                    return result.GetValueInternal(asSafe, isString, autoFreeXmlData, basePath);

                if (!ignoreCase)
                    return null;

                if (dataCaseInsensitive == null)
                {
                    dataCaseInsensitive = InitCaseInsensitive(data);
                }

                if (dataCaseInsensitive.TryGetValue(name, out result))
                    return result.GetValueInternal(asSafe, isString, autoFreeXmlData, basePath);
            }

            return null;
        }

        private Dictionary<string, ResXDataNode> InitCaseInsensitive(Dictionary<string, ResXDataNode> data)
        {
            var result = new Dictionary<string, ResXDataNode>(data.Count, StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, ResXDataNode> item in data)
            {
                result[item.Key] = item.Value;
            }

            return result;
        }

        private void SetValueInternal(string name, object value, Dictionary<string, ResXDataNode> data, ref Dictionary<string, ResXDataNode> dataIgnoreCase)
        {
            if (data == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (name == null)
                throw new ArgumentNullException("name", Res.Get(Res.ArgumentNull));

            lock (data)
            {
                ResXDataNode valueNode;

                // optimization: if the deserialized value is the same reference, which is about to be added, returning
                if (data.TryGetValue(name, out valueNode) && valueNode.ValueInternal == (value ?? ResXNullRef.Value))
                    return;

                valueNode = new ResXDataNode(name, value);
                data[name] = valueNode;
                if (dataIgnoreCase != null)
                    dataIgnoreCase[name] = valueNode;

                isModified = true;
                version++;
            }
        }

        private void RemoveValueInternal(string name, Dictionary<string, ResXDataNode> data, ref Dictionary<string, ResXDataNode> dataIgnoreCase)
        {
            if (data == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (name == null)
                throw new ArgumentNullException("name", Res.Get(Res.ArgumentNull));

            lock (data)
            {
                if (!data.ContainsKey(name))
                    return;

                // clearing the whole ignoreCase dictionary, because cannot tell whether the element should be removed.
                data.Remove(name);
                dataIgnoreCase = null;
                isModified = true;
                version++;
            }
        }

        #region IResXResourceContainer Members

        Dictionary<string, ResXDataNode> IResXResourceContainer.Resources
        {
            get { return resources; }
        }

        Dictionary<string, ResXDataNode> IResXResourceContainer.Metadata
        {
            get { return metadata; }
        }

        Dictionary<string, string> IResXResourceContainer.Aliases
        {
            get { return aliases; }
        }

        bool IResXResourceContainer.UseResXDataNodes
        {
            get { return safeMode; }
        }

        ITypeResolutionService IResXResourceContainer.TypeResolver
        {
            get { return null; }
        }

        int IResXResourceContainer.Version => version;

        #endregion

        #region IExpandoResourceSetInternal Members

        object IExpandoResourceSetInternal.GetResource(string name, bool ignoreCase, bool isString, bool asSafe)
        {
            return GetResourceInternal(name, ignoreCase, isString, asSafe);
        }

        object IExpandoResourceSetInternal.GetMeta(string name, bool ignoreCase, bool isString, bool asSafe)
        {
            return GetMetaInternal(name, ignoreCase, isString, asSafe);
        }

        #endregion
    }
}