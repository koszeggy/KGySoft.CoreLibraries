using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;

using KGySoft.Libraries.Serialization;

namespace KGySoft.Libraries.Resources
{
    using System.Diagnostics;
    using System.Linq;

    using KGySoft.Libraries.Reflection;

    /// <summary>
    /// Writes resources in an XML resource (.resx) file or an output stream.
    /// </summary>
    // Ha Set-ből íródik:
    // - az alias dictionary az újradefiniálások miatt nem biztos, hogy elég infó
    // - ezért végigmenni a data/metadata elemeken, és az aliasokat első szükséges előforduláskor dumpolni
    //   - ha volt újradefiniált alias, és nem változott a beolvasás óta az elem, ez látszódni fog a DataNodeInfo-ban, így ki lehet írni
    //   - ha egy datahoz nincs alias (mert nem is volt, vagy új elem), de a dictionary-ben van hozzá, első előforduláskor kiírni
    //   - ha marad kiíratlan elem a dictionaryben, akkor azok utólag lettek hozzáadva, a legvégén ezeket egyben kiírjuk különösebb cél nélkül
    // - a fentiek alapján az alias dictionary-ből vagy ki kell venni a feldolgozott elemeket, vagy trackelni egy másik dictionaryben, hogy mik az aktuálisan élő aliasok
    // Ha nem setből
    // - baromi egyszerű, minden szekvenciálisan jön. Így lehet duplikált elemeket is hozzáadni
    // - lehetne egy AutoGenerateAlias, ez esetben AddAlias nélkül generáljuk őket (mint most), ha meg ki van kapcsolva, mindig AssemblyQualifiedName használata a nem mscorlib típusokhoz, FullName az mscorlibhez, semmi a stringhez (már ha objectként írjuk, és nem ResXDataNode-ként)
    // Inkompatibilitás/javítások/jobbítások
    // - public field-ek hiányoznak
    // - Az AddAlias hívása után a System verzióban már nem lesz assembly node az xml-hez adva, ott tehát ez egy belső mapping, ami sosincs kiírva. Itt ki lesz, de kérhető, hogy csak az első hivatkozás esetén, ha még nem szerepel.
    // - Better whitespace preserve logic even if compatibleformat is true
    // - DateTime(Offset) másképp íródik, így a millisec infó sem vész el. A System verzió is tudja deserializálni, de úgy a Kind mindig Local lesz.
    // - float/double/decimal: -0 támogatása (kompat módban is). A System verzió is tudja deserializálni, de a float/double -0-ból mindig +0 lesz.
    // - float/double/decimal: -0 támogatása (kompat módban is). A System verzió is tudja deserializálni, de a float/double -0-ból mindig +0 lesz.
    // - char: unpaired surrogate támogatása (kompat módban is). A System verzió ilyet nem tud serializálni, de ezt kompat módban is jól tudja deserializálni.
    // - sbyte[] - a system verzió ezt byte[]-ként serializálja. Ha ezzel csináljunk, kompat módban is jól tudja deserializálni.
    // - bármilyen típus, amiben invalid char/string van: a system és a kompat verzió nem garantált, hogy jó lesz. Nem kompat módban jó lesz.
    // - generikus típus serializálása TypConverterrel: a system verzió elhasalhat a generikus típusok parse-olásánál, nem compatban működik
    // Új funkciók
    // - AutoGenerateAlias
    // - CompatibleFormat. Ha ki van kapcsolva, lásd a leírását meg a fenti felsorolást is
    public sealed class ResXResourceWriter : IResourceWriter
    {
        /// <summary>
        /// Required because a writer created by XmlWriter.Create preserves \r chars only if they are entitized;
        /// however, to keep things readable (and compatible), entitization should be omitted on new lines and base64 wrapping,
        /// but entitization cannot be changed once the writing has started. So this writer:
        /// - Entitizes \r \n if they are not used together
        /// - Can write strings containing unpaired surrogates and invalid Unicode characters. This result can be read correctly even by system ResXResourceReader.
        /// </summary>
        private sealed class ResXWriter : XmlTextWriter
        {
            private Stream stream;
            private TextWriter textWriter;
            private bool isInsideAttribute;

            internal ResXWriter(string fileName)
                : this(new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
            }

            internal ResXWriter(Stream stream) : base(stream, null)
            {
                this.stream = stream;
                Formatting = Formatting.Indented;
            }

            internal ResXWriter(TextWriter textWriter): base(textWriter)
            {
                this.textWriter = textWriter;
                Formatting = Formatting.Indented;
            }

            internal bool FromTextWriter
            {
                get { return textWriter != null; }
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    if (stream != null)
                        stream.Close();
                    if (textWriter != null)
                        textWriter.Close();
                }

                stream = null;
                textWriter = null;
            }

            public override void WriteStartAttribute(string prefix, string localName, string ns)
            {
                isInsideAttribute = true;
                base.WriteStartAttribute(prefix, localName, ns);
            }

            public override void WriteEndAttribute()
            {
                isInsideAttribute = false;
                base.WriteEndAttribute();
            }

            public override void WriteString(string text)
            {
                if (String.IsNullOrEmpty(text))
                    return;

                if (isInsideAttribute)
                {
                    base.WriteString(text);
                    return;
                }

                string newLine = Environment.NewLine;
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    switch (c)
                    {
                        case '<':
                            sb.Append("&lt;");
                            continue;
                        case '>':
                            sb.Append("&gt;");
                            continue;
                        case '&':
                            sb.Append("&amp;");
                            continue;
                        default:
                            if (c == '\t' // TAB is ok
                                || (c >= 0x20 && c <= 0xD7FF) // space..HighSurrogateStart-1 are ok
                                || (c >= 0xE000 && c <= 0xFFFD)) // LowSurrogateEnd+1..ReplacementCharacter are ok
                            {
                                sb.Append(c);
                                continue;
                            }

                            // new line is not escaped unless its parts are not paired alone
                            if (c == newLine[0])
                            {
                                if (newLine.Length == 1 || (i < text.Length - 1 && text[i + 1] == newLine[1]))
                                {
                                    sb.Append(newLine);
                                    if (newLine.Length == 2)
                                        i++;
                                    continue;
                                }
                            }

                            // valid surrogate pair
                            if (i < text.Length - 1 && Char.IsSurrogatePair(c, text[i + 1]))
                            {
                                sb.Append(new[] { c, text[i + 1] });
                                i++;
                                continue;
                            }

                            // none of above: entitizing (including invalid Unicode characters)
                            sb.Append("&#x");
                            sb.Append(((int)c).ToString("X"));
                            sb.Append(';');
                            break;
                    }                    
                }

                WriteRaw(sb.ToString());
            }

            //        return new XmlWriterSettings
                //{
                //    Indent = true,
                //    CloseOutput = true,
                //    CheckCharacters = false, // for chars between 0..31
                //    NewLineHandling = NewLineHandling.Entitize // otherwise, char \r is replaced in reader to \n even if space is preserved
                //};

        }

        //private Hashtable cachedAliases;

        /// <summary>
        /// Stores the alias mapping that should be used when writing assemblies.
        /// </summary>
        private Dictionary<string, string> aliases;

        /// <summary>
        /// Stores the already written and active alias mapping.
        /// </summary>
        private Dictionary<string, string> activeAliases;

        private bool autoGenerateAlias = true;

        //private static TraceSwitch ResValueProviderSwitch = new TraceSwitch("ResX", "Debug the resource value provider");

        private static readonly string resourceHeader = @"
    <!-- 
    Microsoft ResX Schema 
    
    Version " + ResXCommon.Version + @"
    
    The primary goals of this format is to allow a simple XML format 
    that is mostly human readable. The generation and parsing of the 
    various data types are done through the TypeConverter classes 
    associated with the data types.
    
    Example:
    
    ... ado.net/XML headers & schema ...
    <resheader name=""resmimetype"">text/microsoft-resx</resheader>
    <resheader name=""version"">" + ResXCommon.Version + @"</resheader>
    <resheader name=""reader"">System.Resources.ResXResourceReader, System.Windows.Forms, ...</resheader>
    <resheader name=""writer"">System.Resources.ResXResourceWriter, System.Windows.Forms, ...</resheader>
    <data name=""Name1""><value>this is my long string</value><comment>this is a comment</comment></data>
    <data name=""Color1"" type=""System.Drawing.Color, System.Drawing"">Blue</data>
    <data name=""Bitmap1"" mimetype=""" + ResXCommon.BinSerializedObjectMimeType + @""">
        <value>[base64 mime encoded serialized .NET Framework object]</value>
    </data>
    <data name=""Icon1"" type=""System.Drawing.Icon, System.Drawing"" mimetype="""
            + ResXCommon.ByteArraySerializedObjectMimeType + @""">
        <value>[base64 mime encoded string representing a byte array form of the .NET Framework object]</value>
        <comment>This is a comment</comment>
    </data>
                
    There are any number of ""resheader"" rows that contain simple 
    name/value pairs.
    
    Each data row contains a name, and value. The row also contains a 
    type or mimetype. Type corresponds to a .NET class that support 
    text/value conversion through the TypeConverter architecture. 
    Classes that don't support this are serialized and stored with the 
    mimetype set.
    
    The mimetype is used for serialized objects, and tells the 
    ResXResourceReader how to depersist the object. This is currently not 
    extensible. For a given mimetype the value must be set accordingly:
    
    Note - " + ResXCommon.BinSerializedObjectMimeType + @" is the format 
    that the ResXResourceWriter will generate, however the reader can 
    read any of the formats listed below.
    
    mimetype: " + ResXCommon.BinSerializedObjectMimeType + @"
    value   : The object must be serialized with 
            : System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
            : and then encoded with base64 encoding.
    
    mimetype: " + ResXCommon.ByteArraySerializedObjectMimeType + @"
    value   : The object must be serialized into a byte array 
            : using a System.ComponentModel.TypeConverter
            : and then encoded with base64 encoding.
    -->";

        private static readonly string resourceSchema = @"
    <xsd:schema id=""root"" xmlns="""" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
        <xsd:import namespace=""http://www.w3.org/XML/1998/namespace""/>
        <xsd:element name=""root"" msdata:IsDataSet=""true"">
            <xsd:complexType>
                <xsd:choice maxOccurs=""unbounded"">
                    <xsd:element name=""metadata"">
                        <xsd:complexType>
                            <xsd:sequence>
                            <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0""/>
                            </xsd:sequence>
                            <xsd:attribute name=""name"" use=""required"" type=""xsd:string""/>
                            <xsd:attribute name=""type"" type=""xsd:string""/>
                            <xsd:attribute name=""mimetype"" type=""xsd:string""/>
                            <xsd:attribute ref=""xml:space""/>                            
                        </xsd:complexType>
                    </xsd:element>
                    <xsd:element name=""assembly"">
                      <xsd:complexType>
                        <xsd:attribute name=""alias"" type=""xsd:string""/>
                        <xsd:attribute name=""name"" type=""xsd:string""/>
                      </xsd:complexType>
                    </xsd:element>
                    <xsd:element name=""data"">
                        <xsd:complexType>
                            <xsd:sequence>
                                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />
                                <xsd:element name=""comment"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""2"" />
                            </xsd:sequence>
                            <xsd:attribute name=""name"" type=""xsd:string"" use=""required"" msdata:Ordinal=""1"" />
                            <xsd:attribute name=""type"" type=""xsd:string"" msdata:Ordinal=""3"" />
                            <xsd:attribute name=""mimetype"" type=""xsd:string"" msdata:Ordinal=""4"" />
                            <xsd:attribute ref=""xml:space""/>
                        </xsd:complexType>
                    </xsd:element>
                    <xsd:element name=""resheader"">
                        <xsd:complexType>
                            <xsd:sequence>
                                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />
                            </xsd:sequence>
                            <xsd:attribute name=""name"" type=""xsd:string"" use=""required"" />
                        </xsd:complexType>
                    </xsd:element>
                </xsd:choice>
            </xsd:complexType>
        </xsd:element>
        </xsd:schema>
        ";
        
        //IFormatter binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter ();
        //string fileName;
        //Stream stream;
        //TextWriter textWriter;
        //XmlTextWriter xmlTextWriter;
        private ResXWriter writer;
        private string basePath;

        private bool hasBeenSaved;
        private bool initialized;

        private Func<Type, string> typeNameConverter; // no public property to be consistent with ResXDataNode class.

        private bool compatibleFormat = true;
        private bool omitHeader = true;

        /// <summary>
        /// Gets or sets whether the <see cref="ResXResourceWriter"/> instance should create a System compatible .resx file, which can be used
        /// built-in resource editor of the Visual Studio. Default value is <c>true</c>. See the Remarks section for more info.
        /// </summary>
        /// <remarks>
        /// The value of the property affects the following differences:
        /// <list type="bullet">
        /// <item><description>The reader and writer resheader elements.</description></item>
        /// <item><description>Type of <see cref="ResXFileRef"/> file references.</description></item>
        /// <item><description>The placeholder type of <see langword="null"/> references.</description></item>
        /// <item><description>If <c>CompatibleFormat</c> is <c>false</c>, some additional types are supported natively (without a mimetype): <see cref="IntPtr"/>, <see cref="UIntPtr"/>, <see cref="DBNull"/> and <see cref="Type"/> (unless it is a generic type argument).</description></item>
        /// <item><description>If <c>CompatibleFormat</c> is <c>false</c>, unpaired surrogate <see cref="char"/> values are supported.</description></item>
        /// <item><description>The mimetype and content of binary serialized elements. If <c>CompatibleFormat</c> is <c>false</c>, these objects are
        /// serialized by <see cref="BinarySerializationFormatter"/>, which provides a much more compact result than the default <see cref="BinaryFormatter"/>.</description></item>
        /// <item><description>If <c>CompatibleFormat</c> is <c>false</c>, the even non-serializable objects can be added to the .resx file.
        /// Though there is no guarantee that the deserialized object will work properly.</description></item>
        /// </list>
        /// </remarks>
        public bool CompatibleFormat
        {
            get { return compatibleFormat; }
            set
            {
                if (initialized)
                    throw new InvalidOperationException(Res.Get(Res.InvalidResXWriterPropertyChange));
                compatibleFormat = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the header should be written. If both <see cref="CompatibleFormat"/> and <see cref="OmitHeader"/> are <c>true</c>, then
        /// only the XML comment will be omitted. If <see cref="CompatibleFormat"/> is <c>false</c> and <see cref="OmitHeader"/> is <c>true</c>, then
        /// the comment, the .ResX schema and the &lt;resheader&gt; elements will be omitted, too.
        /// <br/>Default value: <c>true</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">In a set operation, a value cannot be specified because the XML resource file has already been accessed and is in use.</exception>
        public bool OmitHeader
        {
            get { return omitHeader; }
            set
            {
                if (initialized)
                    throw new InvalidOperationException(Res.Get(Res.InvalidResXWriterPropertyChange));

                if (writer == null)
                    throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

                omitHeader = value;
            }
        }

        /// <summary>
        /// Gets or sets the base path for the relative file path specified in a <see cref="ResXFileRef"/> object.
        /// </summary>
        /// <returns>
        /// A path that, if prepended to the relative file path specified in a <see cref="ResXFileRef"/> object, yields an absolute path to an XML resource file.
        /// </returns>
        public string BasePath
        {
            get { return basePath; }
            set
            {
                if (initialized)
                    throw new InvalidOperationException(Res.Get(Res.InvalidResXWriterPropertyChange));
                basePath = value;
            }
        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="ResXResourceWriter"/> class that writes the resources to the specified file.
        ///// </summary>
        ///// <param name="fileName">The output file name.</param>
        //public ResXResourceWriter(string fileName) {
        //    this.fileName = fileName;
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceWriter"/> class that writes the resources to a specified file.
        /// </summary>
        /// <param name="fileName">The file to send output to.</param>
        /// <param name="typeNameConverter">The delegate that can be used to target earlier versions of assemblies or the .NET Framework.</param>
        public ResXResourceWriter(string fileName, Func<Type, string> typeNameConverter = null)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName", Res.Get(Res.ArgumentNull));

            //writer = XmlWriter.Create(fileName, GetWriterSettings());
            writer = new ResXWriter(fileName);
            this.typeNameConverter = typeNameConverter;
        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="T:System.Resources.ResXResourceWriter"/> class that writes the resources to the specified stream object.
        ///// </summary>
        ///// <param name="stream">The output stream. </param>
        ///// <devdoc>
        /////     Creates a new ResXResourceWriter that will write to the specified stream.
        ///// </devdoc>
        //public ResXResourceWriter(Stream stream) {
        //    this.stream = stream;
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceWriter"/> class that writes the resources to a specified <paramref name="stream"/>..
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to send the output to.</param>
        /// <param name="typeNameConverter">The delegate that can be used to target earlier versions of assemblies or the .NET Framework.</param>
        public ResXResourceWriter(Stream stream, Func<Type, string> typeNameConverter = null)
        {
            //writer = XmlWriter.Create(stream, GetWriterSettings());
            writer = new ResXWriter(stream);
            this.typeNameConverter = typeNameConverter;
        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="T:System.Resources.ResXResourceWriter"/> class that writes to the specified <see cref="T:System.IO.TextWriter"/> object.
        ///// </summary>
        ///// <param name="textWriter">The <see cref="T:System.IO.TextWriter"/> object to send the output to. </param>
        ///// <devdoc>
        /////     Creates a new ResXResourceWriter that will write to the specified TextWriter.
        ///// </devdoc>
        //public ResXResourceWriter(TextWriter textWriter) {
        //    this.textWriter = textWriter;
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceWriter"/> class that writes the resources to a specified <paramref name="textWriter"/>.
        /// </summary>
        /// <param name="textWriter">The <see cref="TextWriter"/> object to send output to.</param>
        /// <param name="typeNameConverter">The delegate that can be used to target earlier versions of assemblies or the .NET Framework.</param>
        public ResXResourceWriter(TextWriter textWriter, Func<Type, string> typeNameConverter = null)
        {
            //writer = XmlWriter.Create(textWriter, GetWriterSettings());
            writer = new ResXWriter(textWriter);
            this.typeNameConverter = typeNameConverter;
        }

        //private XmlWriterSettings GetWriterSettings()
        //{
        //    return new XmlWriterSettings
        //        {
        //            Indent = true,
        //            CloseOutput = true,
        //            CheckCharacters = false, // for chars between 0..31
        //            NewLineHandling = NewLineHandling.Entitize // otherwise, char \r is replaced in reader to \n even if space is preserved
        //        };
        //}

        /// <summary>
        /// This member overrides the <see cref="M:System.Object.Finalize"/> method.
        /// </summary>
        ~ResXResourceWriter()
        {
            Dispose(false);
        }

        private void InitializeWriter()
        {
            // otherwise, UTF-16 would have been dumped
            if (writer.FromTextWriter)
                writer.WriteRaw("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            else
                writer.WriteStartDocument();

            initialized = true;
            writer.WriteStartElement("root");
            if (compatibleFormat || !omitHeader)
            {
                XmlReader reader =
                    XmlReader.Create(
                        new StringReader(compatibleFormat 
                            ? (omitHeader ? resourceSchema : resourceHeader + resourceSchema)
                            : resourceSchema),
                        new XmlReaderSettings { CloseInput = true, IgnoreWhitespace = true });
                writer.WriteNode(reader, true);
            }

            if (!compatibleFormat && omitHeader)
                return;

            writer.WriteStartElement(ResXCommon.ResHeaderStr);
            writer.WriteAttributeString(ResXCommon.NameStr, ResXCommon.ResMimeTypeStr);
            writer.WriteStartElement(ResXCommon.ValueStr);
            writer.WriteString(ResXCommon.ResMimeType);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteStartElement(ResXCommon.ResHeaderStr);
            writer.WriteAttributeString(ResXCommon.NameStr, ResXCommon.VersionStr);
            writer.WriteStartElement(ResXCommon.ValueStr);
            writer.WriteString(ResXCommon.Version);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteStartElement(ResXCommon.ResHeaderStr);
            writer.WriteAttributeString(ResXCommon.NameStr, ResXCommon.ReaderStr);
            writer.WriteStartElement(ResXCommon.ValueStr);
            writer.WriteString(ResXCommon.GetAssemblyQualifiedName(typeof(ResXResourceReader), null, compatibleFormat));
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteStartElement(ResXCommon.ResHeaderStr);
            writer.WriteAttributeString(ResXCommon.NameStr, ResXCommon.WriterStr);
            writer.WriteStartElement(ResXCommon.ValueStr);
            writer.WriteString(ResXCommon.GetAssemblyQualifiedName(typeof(ResXResourceWriter), null, compatibleFormat));
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private XmlWriter Writer
        {
            get
            {
                if (writer == null)
                    throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

                if (!initialized)
                    InitializeWriter();

                return writer;
            }
        }

        /// <summary>
        /// Gets or sets whether an alias should be auto-generated for referenced assemblies.
        /// <br/>
        /// Default value: <c>true</c>.
        /// </summary>
        /// <remarks>
        /// <para>If <c>AutoGenerateAlias</c> is <c>false</c>, the assembly names will be referenced by fully qualified names,
        /// unless the alias names are explicitly added by <see cref="AddAlias(string,string,bool)"/> method, or when a <see cref="ResXDataNode"/>
        /// added by <see cref="AddResource(ResXDataNode)"/> method already contains an alias.</para>
        /// <para>If <c>AutoGenerateAlias</c> is <c>true</c>, the assembly aliases are re-generated, even if a <see cref="ResXDataNode"/>
        /// already contains an alias. To use explicitly defined names instead of auto generated names use the <see cref="AddAlias(string,string,bool)"/> method.</para>
        /// </remarks>
        public bool AutoGenerateAlias
        {
            get { return autoGenerateAlias; }
            set
            {
                if (writer == null)
                    throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                autoGenerateAlias = value;
            }
        }

        /// <summary>
        /// Adds the specified alias to the mapping of aliases.
        /// </summary>
        /// <param name="aliasName">The name of the alias.</param>
        /// <param name="assemblyName">The name of the assembly represented by <paramref name="aliasName"/>.</param>
        /// <param name="forceWriteImmediately"><c>true</c> to write the alias immediately to the .resx file; <c>false</c> just to
        /// add it to the inner mapping and write it only when it is referenced for the first time.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assemblyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="aliasName"/> is <see langword="null"/>.</exception>
        public void AddAlias(string aliasName, AssemblyName assemblyName, bool forceWriteImmediately = false)
        {
            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName", Res.Get(Res.ArgumentNull));

            AddAlias(aliasName, assemblyName.FullName, forceWriteImmediately);
        }

        /// <summary>
        /// Adds the specified alias to the mapping of aliases.
        /// </summary>
        /// <param name="aliasName">The name of the alias.</param>
        /// <param name="assemblyName">The name of the assembly represented by <paramref name="aliasName"/>.</param>
        /// <param name="forceWriteImmediately"><c>true</c> to write the alias immediately to the .resx file; <c>false</c> just to
        /// add it to the inner mapping and write it only when it is referenced for the first time.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assemblyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="aliasName"/> is <see langword="null"/>.</exception>
        public void AddAlias(string aliasName, string assemblyName, bool forceWriteImmediately = false)
        {
            if (aliasName == null)
                throw new ArgumentNullException("aliasName", Res.Get(Res.ArgumentNull));
            if (assemblyName == null)
                throw new ArgumentNullException("assemblyName", Res.Get(Res.ArgumentNull));

            if (aliases == null)
                aliases = new Dictionary<string, string>();
            aliases[assemblyName] = aliasName;

            if (forceWriteImmediately)
                AddAssemblyRow(aliasName, assemblyName);
        }

        /// <summary>
        /// Adds a design-time property whose value is specifed as a byte array to the list of resources to write.
        /// </summary>
        /// <param name="name">The name of a property.</param><param name="value">A byte array containing the value of the property to add.</param><exception cref="T:System.InvalidOperationException">The resource specified by the <paramref name="name"/> parameter has already been added.</exception>
        public void AddMetadata(string name, byte[] value) {
            AddDataRow(ResXCommon.MetadataStr, name, value);
        }

        /// <summary>
        /// Adds a design-time property whose value is specified as a string to the list of resources to write.
        /// </summary>
        /// <param name="name">The name of a property.</param><param name="value">A string that is the value of the property to add.</param><exception cref="T:System.InvalidOperationException">The resource specified by the <paramref name="name"/> property has already been added.</exception>
        public void AddMetadata(string name, string value) {
            AddDataRow(ResXCommon.MetadataStr, name, value);
        }

        /// <summary>
        /// Adds a design-time property whose value is specified as an object to the list of resources to write.
        /// </summary>
        /// <param name="name">The name of a property.</param><param name="value">An object that is the value of the property to add.</param><exception cref="T:System.InvalidOperationException">The resource specified by the <paramref name="name"/> parameter has already been added.</exception>
        public void AddMetadata(string name, object value)
        {
            var node = value as ResXDataNode;
            if (node != null)
            {
                if (name != node.Name)
                    node = new ResXDataNode(name, value);
                AddMetadata(node);
            }
            else
            {
                AddDataRow(ResXCommon.MetadataStr, name, value);
            }
        }

        /// <summary>
        /// Adds a design-time property specified in a <see cref="ResXDataNode"/> object to the list of resources to write.
        /// </summary>
        /// <param name="node">A <see cref="ResXDataNode"/> object that contains a metadata name/value pair.</param>
        public void AddMetadata(ResXDataNode node)
        {
            AddDataRow(ResXCommon.MetadataStr, node.Name, node);
        }

        /// <summary>
        /// Adds a named resource specified as a byte array to the list of resources to write.
        /// </summary>
        /// <param name="name">The name of the resource. </param><param name="value">The value of the resource to add as an 8-bit unsigned integer array. </param>
        /// <devdoc>
        ///     Adds a blob resource to the resources.
        /// </devdoc>
        // NOTE: Part of IResourceWriter - not protected by class level LinkDemand.
        public void AddResource(string name, byte[] value) {
            AddDataRow(ResXCommon.DataStr, name, value);
        }

        /// <summary>
        /// Adds a named resource specified as an object to the list of resources to write.
        /// </summary>
        /// <param name="name">The name of the resource. </param><param name="value">The value of the resource. </param>
        /// <devdoc>
        ///     Adds a resource to the resources. If the resource is a string,
        ///     it will be saved that way, otherwise it will be serialized
        ///     and stored as in binary.
        /// </devdoc>
        // NOTE: Part of IResourceWriter - not protected by class level LinkDemand.
        public void AddResource(string name, object value)
        {
            var node = value as ResXDataNode;
            if (node != null)
            {
                if (name != node.Name)
                    node = new ResXDataNode(name, value);
                AddResource(node);
            }
            else 
            {
                AddDataRow(ResXCommon.DataStr, name, value);
            }
        }

        /// <summary>
        /// Adds a string resource to the resources.
        /// </summary>
        /// <param name="name">The name of the resource. </param><param name="value">The value of the resource. </param>
        /// <devdoc>
        ///     Adds a string resource to the resources.
        /// </devdoc>
        // NOTE: Part of IResourceWriter - not protected by class level LinkDemand.
        public void AddResource(string name, string value) {
            AddDataRow(ResXCommon.DataStr, name, value);
        }

        // TODO: delete
        ///// <summary>
        ///// Adds a named resource specified in a <see cref="T:System.Resources.ResXDataNode"/> object to the list of resources to write.
        ///// </summary>
        ///// <param name="node">A <see cref="T:System.Resources.ResXDataNode"/> object that contains a resource name/value pair.</param>
        ///// <devdoc>
        /////     Adds a string resource to the resources.
        ///// </devdoc>
        //public void AddResource(ResXDataNode node) {
        //    // we're modifying the node as we're adding it to the resxwriter
        //    // this is BAD, so we clone it. adding it to a writer doesnt change it
        //    // we're messing with a copy
        //    ResXDataNode nodeClone = node.Clone();
            
        //    ResXFileRef fileRef = nodeClone.FileRef;
        //    string modifiedBasePath = BasePath;
            
        //    if (!String.IsNullOrEmpty(modifiedBasePath)) {
        //        if (!(modifiedBasePath.EndsWith("\\")))
        //        {
        //            modifiedBasePath += "\\";
        //        }
        //        if (fileRef != null) {
        //            fileRef.MakeFilePathRelative(modifiedBasePath);
        //        }
        //    }
        //    DataNodeInfo info = nodeClone.GetDataNodeInfo(typeNameConverter, compatibleFormat, basePath);
        //    AddDataRow(ResXCommon.DataStr, info.Name, info.ValueData, info.TypeName, info.AssemblyAliasValue, info.MimeType, info.Comment);
        //}

        /// <summary>
        /// Adds a named resource specified in a <see cref="ResXDataNode"/> object to the list of resources to write.
        /// </summary>
        /// <param name="node">A <see cref="ResXDataNode"/> object that contains a resource name/value pair.</param>
        public void AddResource(ResXDataNode node)
        {
            AddDataRow(ResXCommon.DataStr, node.Name, node);
        }

        private void AddDataRow(string elementName, string name, ResXDataNode node)
        {
            DataNodeInfo info = node.GetDataNodeInfo(typeNameConverter, compatibleFormat);
            string value = info.ValueData;
            ResXFileRef fileRef;
            if (basePath != null && (fileRef = node.FileRef) != null && Path.IsPathRooted(fileRef.FileName))
            {
                value = ResXFileRef.ToString(Files.GetRelativePath(fileRef.FileName, basePath), fileRef.TypeName, fileRef.EncodingName);
            }

            AddDataRow(elementName, name, value, GetTypeNameWithAlias(info), info.MimeType, info.Comment);
        }

        //private void AddDataRow(string elementName, string name, ResXFileRef fileRef)
        //{
        //    // cloning so the wrapper ResXDataNode will not be referenced longer than it is needed. basePath is not required here because GetValue will be never called.
        //    AddDataRow(elementName, name, new ResXDataNode(name, fileRef.Clone(), null));
        //}

        /// <devdoc>
        ///     Adds a blob resource to the resources.
        /// </devdoc>
        private void AddDataRow(string elementName, string name, byte[] value)
        {
            AddDataRow(elementName, name, ResXCommon.ToBase64(value), GetTypeNameWithAlias(Reflector.ByteArrayType), null, null);
        }

        ///// <devdoc>
        /////     Adds a resource to the resources. If the resource is a string,
        /////     it will be saved that way, otherwise it will be serialized
        /////     and stored as in binary.
        ///// </devdoc>
        //private void AddDataRow(string elementName, string name, object value)
        //{
        //    //Debug.WriteLineIf(ResValueProviderSwitch.TraceVerbose, "  resx: adding resource " + name);
        //    if (value is string)
        //    {
        //        AddDataRow(elementName, name, (string)value);
        //    }
        //    else if (value is byte[])
        //    {
        //        AddDataRow(elementName, name, (byte[])value);
        //    }
        //    else if (value is ResXFileRef)
        //    {
        //        ResXFileRef fileRef = (ResXFileRef)value;
        //        ResXDataNode node = new ResXDataNode(name, fileRef, basePath); // TODO: elvileg már nem kell
        //        fileRef.MakeFilePathRelative(BasePath);
        //        DataNodeInfo info = node.GetDataNodeInfo(typeNameConverter, compatibleFormat, null);
        //        AddDataRow(elementName, info.Name, info.ValueData, info.TypeName, info.AssemblyAliasValue, info.MimeType, info.Comment);
        //    }
        //    else
        //    {
        //        ResXDataNode node = new ResXDataNode(name, value);
        //        DataNodeInfo info = node.GetDataNodeInfo(typeNameConverter, compatibleFormat, null);
        //        AddDataRow(elementName, info.Name, info.ValueData, info.TypeName, info.AssemblyAliasValue, info.MimeType, info.Comment);
        //    }
        //}        

        /// <devdoc>
        ///     Adds a resource to the resources. If the resource is a string,
        ///     it will be saved that way, otherwise it will be serialized
        ///     and stored as in binary.
        /// </devdoc>
        private void AddDataRow(string elementName, string name, object value)
        {
            // 1.) String
            string valueData = value as string;
            if (valueData != null)
            {
                AddDataRow(elementName, name, valueData);
                return;
            }

            // 2.) byte[] - double check must be performed because of sbyte[]
            byte[] byteArray = value as byte[];
            if (byteArray != null && value.GetType() == Reflector.ByteArrayType)
            {
                AddDataRow(elementName, name, byteArray);
                return;
            }

            // 3.) Any other (including null and ResXFileRef (and WinForms.ResXDataNode/ResXFileRef, too))
            ResXDataNode node = new ResXDataNode(name, value);
            AddDataRow(elementName, name, node);
        }

        /// <devdoc>
        ///     Adds a string resource to the resources.
        /// </devdoc>
        private void AddDataRow(string elementName, string name, string value) {
            if (value == null)
            {
                // if it's a null string, set it here as a resxnullref
                AddDataRow(elementName, name, null, GetTypeNameWithAlias(typeof(ResXNullRef)), null, null);
            }
            else
            {
                AddDataRow(elementName, name, value, null, null, null);
            }
        }

        /// <devdoc>
        ///     Adds a new row to the Resources table. This helper is used because
        ///     we want to always late bind to the columns for greater flexibility.
        /// </devdoc>
        //[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void AddDataRow(string elementName, string name, string value, string typeWithAlias, string mimeType, string comment)
        {
            if (hasBeenSaved)
                throw new InvalidOperationException(Res.Get(Res.ResXResourceWriterSaved));

            // TODO: move to caller
            //string alias = null;
            //if (!string.IsNullOrEmpty(type) && elementName == ResXCommon.DataStr)
            //{
            //    string assemblyName = GetFullName(type);
            //    if(string.IsNullOrEmpty(assemblyName)) {
            //        try {
            //            Type typeObject = Type.GetType(type);
            //            if(typeObject == typeof(string)) {
            //                type = null;
            //            } else if(typeObject != null) {
            //                assemblyName = GetFullName(ResXCommon.GetAssemblyQualifiedName(typeObject, typeNameConverter, compatibleFormat));
            //                alias = GetAliasFromName(new AssemblyName(assemblyName));
            //            }
            //        } catch {
            //        }
            //    } else {
            //        alias = GetAliasFromName(new AssemblyName(GetFullName(type)));
            //    }
            //    //AddAssemblyRow(AssemblyStr, alias, GetFullName(type));
            //}

            Writer.WriteStartElement(elementName);
            writer.WriteAttributeString(ResXCommon.NameStr, name);

            // TODO: del
            //if (!string.IsNullOrEmpty(alias) && !string.IsNullOrEmpty(type) && elementName == ResXCommon.DataStr) {
            //     // CHANGE: we still output version information. This might have
            //    // to change in 3.2
            //    string typeName = GetTypeName(type);
            //    string typeValue = typeName + ", " + alias;
            //    Writer.WriteAttributeString(ResXCommon.TypeStr, typeValue);
            //}
            //else {
            if (typeWithAlias != null)
                writer.WriteAttributeString(ResXCommon.TypeStr, typeWithAlias);
            //}

            if (mimeType != null)
                writer.WriteAttributeString(ResXCommon.MimeTypeStr, mimeType);

            // TODO: del
            //if((type == null && mimeType == null) || (type != null && type.StartsWith("System.Char", StringComparison.Ordinal))) {
            //    writer.WriteAttributeString("xml", "space", null, "preserve");
            //}

            if (value != null && mimeType == null && (typeWithAlias == null || !typeWithAlias.StartsWith("System.Byte[]", StringComparison.Ordinal)) && PreserveSpaces(value))
                writer.WriteAttributeString("xml", "space", null, "preserve");

            writer.WriteStartElement(ResXCommon.ValueStr);
            if (!string.IsNullOrEmpty(value))
                writer.WriteString(value);

            writer.WriteEndElement();
            if (!string.IsNullOrEmpty(comment))
            {
                writer.WriteStartElement(ResXCommon.CommentStr);
                writer.WriteString(comment);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static bool PreserveSpaces(string value)
        {
            char c;
            return value.Length > 0 && ((c = value[0]) == ' ' || c == '\t' || c == '\r' || c == '\n');
        }

        private void AddAssemblyRow(string alias, string assembly)
        {
            Writer.WriteStartElement(ResXCommon.AssemblyStr);
            writer.WriteAttributeString(ResXCommon.AliasStr, alias);
            writer.WriteAttributeString(ResXCommon.NameStr, assembly);
            writer.WriteEndElement();
            if (activeAliases == null)
                activeAliases = new Dictionary<string, string>();

            activeAliases[assembly] = alias;
        }

        //private string GetAliasFromName(AssemblyName assemblyName)
        //{
        //    if (cachedAliases == null)
        //    {
        //        cachedAliases = new Hashtable();
        //    }
        //    string alias = (string)cachedAliases[assemblyName.FullName];
        //    if (string.IsNullOrEmpty(alias))
        //    {
        //        alias = assemblyName.Name;
        //        AddAlias(alias, assemblyName);
        //        AddAssemblyRow(alias, assemblyName.FullName);
        //    }
        //    return alias;
        //}

        private string GetTypeNameWithAlias(DataNodeInfo info)
        {
            return info.TypeName == null ? null : GetTypeNameWithAlias(info.TypeName, info.AssemblyAliasValue);
        }

        private string GetTypeNameWithAlias(Type type)
        {
            return GetTypeNameWithAlias(ResXCommon.GetAssemblyQualifiedName(type, typeNameConverter, compatibleFormat), null);
        }

        /// <summary>
        /// Gets the type name with alias.
        /// </summary>
        /// <param name="origTypeName">Input type name with assembly or alias name.</param>
        /// <param name="asmName">If not null, origTypeName contains an alias name, and this is the assembly name.</param>
        /// <returns>The type name with a (possibly generated) alias or with the full name.</returns>
        private string GetTypeNameWithAlias(string origTypeName, string asmName)
        {
            int genericEnd = origTypeName.LastIndexOf(']');
            int asmNamePos = origTypeName.IndexOf(',', genericEnd + 1);
            string typeName = asmNamePos >= 0 ? origTypeName.Substring(0, asmNamePos).Trim() : origTypeName;
            string asmOrAliasName = asmNamePos >= 0 ? origTypeName.Substring(asmNamePos + 1).Trim() : null;
            string alias;

            if (asmOrAliasName == null)
            {
                Debug.Assert(asmName == null, "There is a resolved alias value without an alias name");
                return origTypeName;
            }

            // asmOrAliasName is now an alias, which has to be resolved to asmName
            if (asmName != null && asmOrAliasName != asmName)
            {
                // 1. if this is an active alias, just return
                if (activeAliases != null && activeAliases.TryGetValue(asmName, out alias) && alias == asmOrAliasName)
                    return typeName + ", " + alias;

                // 2. if not, check among the mandatory alias names to use
                if (aliases != null && aliases.TryGetValue(asmName, out alias))
                {
                    // if it is there, making it active
                    AddAssemblyRow(alias, asmName);
                    return typeName + ", " + alias;
                }

                // 3. Alias name is now coming from the original .resx. Setting it as the active alias there is no auto-generate.
                if (!autoGenerateAlias)
                {
                    AddAssemblyRow(asmOrAliasName, asmName);
                    return origTypeName;
                }

                // 4. Otherwise, generating a proper name. If it is not the active name, making it active
                string generatedAlias = new AssemblyName(asmName).Name;
                if (activeAliases == null || !activeAliases.TryGetValue(asmName, out alias) || alias != generatedAlias)
                    AddAssemblyRow(generatedAlias, asmName);
                return typeName + ", " + generatedAlias;
            }

            // origTypeName is an AQN here, asmOrAliasName is an assembly name
            asmName = asmOrAliasName;

            // 1. if asmName has an active alias, build the result and return
            if (activeAliases != null && activeAliases.TryGetValue(asmName, out alias))
                return typeName + ", " + alias;

            // 2. if it is among the mandatory aliases to use, write the assembly, set as active, build the result and return
            if (aliases != null && aliases.TryGetValue(asmName, out alias))
            {
                AddAssemblyRow(alias, asmName);
                return typeName + ", " + alias;
            }

            // 3. if no auto generate, returning original AQN
            if (!autoGenerateAlias)
                return origTypeName;

            // 4. generate alias, making it active, write assembly, return the generated alias with type name
            alias = new AssemblyName(asmName).Name;
            AddAssemblyRow(alias, asmName);
            return typeName + ", " + alias;
        }

        /// <summary>
        /// Releases all resources used by the <see cref="T:System.Resources.ResXResourceWriter"/>.
        /// </summary>
        /// <devdoc>
        ///     Closes any files or streams locked by the writer.
        /// </devdoc>
        // NOTE: Part of IResourceWriter - not protected by class level LinkDemand.
        public void Close() {
            Dispose();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ResXResourceWriter"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Resources.ResXResourceWriter"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
        private void Dispose(bool disposing)
        {
            if (writer == null)
                return;

            if (disposing)
            {
                if (!hasBeenSaved)
                    Generate();

                writer.Close();
            }

            writer = null;
            aliases = null;
            activeAliases = null;
        }

        private string GetTypeName(string typeName) {
             int indexStart = typeName.IndexOf(',');
             return ((indexStart == -1) ? typeName : typeName.Substring(0, indexStart));
        }


        private string GetFullName(string typeName) {
             int indexStart = typeName.IndexOf(',');
             if(indexStart == -1)
                return null;
             return typeName.Substring(indexStart + 2);
        }

#if UNUSED
        private string GetSimpleName(string typeName) {
             int indexStart = typeName.IndexOf(",");
             int indexEnd =  typeName.IndexOf(",", indexStart + 1);
             return typeName.Substring(indexStart + 2, indexEnd - indexStart  - 3); 
        }

        static string StripVersionInformation(string typeName) {
            int indexStart = typeName.IndexOf(" Version=");
            if(indexStart ==-1)
                indexStart = typeName.IndexOf("Version=");
            if(indexStart ==-1)
                indexStart = typeName.IndexOf("version=");
            int indexEnd = -1;
            string result = typeName;
            if(indexStart != -1) {
                // foudn version
                indexEnd = typeName.IndexOf(",", indexStart);
                if(indexEnd != -1) {
                    result = typeName.Remove(indexStart, indexEnd-indexStart+1);
                }
            }
            return result;
            
        }
#endif

        /// <summary>
        /// Writes all resources added by the <see cref="M:System.Resources.ResXResourceWriter.AddResource(System.String,System.Byte[])"/> method to the output file or stream.
        /// </summary>
        /// <exception cref="InvalidOperationException">The resource has already been saved.</exception>
        /// <exception cref="ObjectDisposedException">The writer has already been disposed.</exception>
        public void Generate()
        {
            if (writer == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (hasBeenSaved)
                throw new InvalidOperationException(Res.Get(Res.ResXResourceWriterSaved));

            hasBeenSaved = true;
            if (initialized)
                writer.WriteEndElement();
            writer.Flush();
        }
    }
}
