#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXResourceWriter.cs
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
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Serialization.Binary;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Writes resources in an XML resource (.resx) file or an output stream.
    /// <br/>See the <strong>Remarks</strong> section for an example and for the differences compared to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcewriter" target="_blank">System.Resources.ResXResourceWriter</a> class.
    /// </summary>
    /// <remarks>
    /// <note>This class is similar to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcewriter" target="_blank">System.Resources.ResXResourceWriter</a>
    /// in <c>System.Windows.Forms.dll</c>. See the <a href="#comparison">Comparison with System.Resources.ResXResourceWriter</a> section for the differences.</note>
    /// <note type="tip">To see when to use the <see cref="ResXResourceReader"/>, <see cref="ResXResourceWriter"/>, <see cref="ResXResourceSet"/>, <see cref="ResXResourceManager"/>, <see cref="HybridResourceManager"/> and <see cref="DynamicResourceManager"/>
    /// classes see the documentation of the <see cref="N:KGySoft.Resources">KGySoft.Resources</see> namespace.</note>
    /// <para>Resources are specified as name/value pairs using the <see cref="AddResource(string,object)">AddResource</see> method.</para>
    /// <para>If <see cref="CompatibleFormat"/> property is <see langword="true"/>, <see cref="ResXResourceWriter"/> emits .resx files, which can be then read not just by <see cref="ResXResourceReader"/>
    /// but by the original <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> class, too.</para>
    /// <example>
    /// The following example shows how to create a resource file by <see cref="ResXResourceWriter"/> and add different kind of resource objects to it. At the end it displays the resulting .resx file content.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Drawing;
    /// using System.IO;
    /// using KGySoft.Resources;
    ///
    /// public class Example
    /// {
    ///     [Serializable]
    ///     private class MyCustomClass
    ///     {
    ///         public string StringProp { get; set; }
    ///         public int IntProp { get; set; }
    ///     }
    ///
    ///     public static void Main()
    ///     {
    ///         // Check the result with CompatibleFormat = true as well.
    ///         // You will get a much longer result, which will be able to read by System.Resources.ResXResourceReader, too.
    ///         var result = new StringWriter();
    ///         using (var writer = new ResXResourceWriter(result) { CompatibleFormat = false })
    ///         {
    ///             writer.AddResource("string", "string value");
    ///             writer.AddResource("int", 42);
    ///             writer.AddResource("null", (object)null);
    ///             writer.AddResource("file", new ResXFileRef(@"images\Image.jpg", typeof(Bitmap)));
    ///             writer.AddResource("custom", new MyCustomClass { IntProp = 42, StringProp = "blah" });
    ///         }
    ///
    ///         Console.WriteLine(result.GetStringBuilder());
    ///     }
    /// }
    /// 
    /// // The example displays the following output:
    /// // <?xml version="1.0" encoding="utf-8"?>
    /// // <root>
    /// //   <data name="string">
    /// //     <value>string value</value>
    /// //   </data>
    /// //   <data name="int" type="System.Int32">
    /// //     <value>42</value>
    /// //   </data>
    /// //   <assembly alias="KGySoft.CoreLibraries" name="KGySoft.CoreLibraries, Version=3.6.3.1, Culture=neutral, PublicKeyToken=b45eba277439ddfe" />
    /// //   <data name="null" type="KGySoft.Resources.ResXNullRef, KGySoft.CoreLibraries">
    /// //     <value />
    /// //   </data>
    /// //   <data name="file" type="KGySoft.Resources.ResXFileRef, KGySoft.CoreLibraries">
    /// //     <value>images\Image.jpg;System.Drawing.Bitmap, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a</value>
    /// //   </data>
    /// //   <data name="custom" mimetype="text/kgysoft.net/object.binary.base64">
    /// //     <value>
    /// //       PgAChAQFQkNvbnNvbGVBcHAxLCBWZXJzaW9uPTEuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49bnVsbBVF
    /// //       eGFtcGxlK015Q3VzdG9tQ2xhc3MBAhs8U3RyaW5nUHJvcD5rX19CYWNraW5nRmllbGQDEgAEYmxhaBg8SW50UHJvcD5rX19CYWNr
    /// //       aW5nRmllbGQECEAqAA==
    /// //     </value>
    /// //   </data>
    /// // </root>]]></code>
    /// </example>
    /// <h1 class="heading">Comparison with System.Resources.ResXResourceWriter<a name="comparison">&#160;</a></h1>
    /// <note>When writing a .resx file in <see cref="CompatibleFormat"/>, the <c>System.Windows.Forms.dll</c> is not loaded when referencing
    /// <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a> and <strong>System.Resources.ResXNullRef</strong> types.</note>
    /// <para><strong>Incompatibility</strong> with <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcewriter" target="_blank">System.Resources.ResXResourceWriter</a>:
    /// <list type="bullet">
    /// <item>The System version has several public string fields, which are not intended to be accessed by a consumer code. Therefore the following fields are missing (they are not public) in this version:
    /// <list type="bullet">
    /// <item><c>BinSerializedObjectMimeType</c></item>
    /// <item><c>ByteArraySerializedObjectMimeType</c></item>
    /// <item><c>DefaultSerializedObjectMimeType</c></item>
    /// <item><c>ResMimeType</c></item>
    /// <item><c>ResourceSchema</c></item>
    /// <item><c>SoapSerializedObjectMimeType</c></item>
    /// <item><c>Version</c></item>
    /// </list></item>
    /// <item>In this version there are no one parameter constructors. Instead, the second parameter (<c>typeNameConverter</c>) is optional. If used purely from C# (no reflection or whatsoever), this change is a
    /// compatible one.</item>
    /// <item>This <see cref="ResXResourceWriter"/> is a sealed class.</item>
    /// <item>After disposing the <see cref="ResXResourceWriter"/> instance or calling the <see cref="Close">Close</see> method, calling the <see cref="Generate">Generate</see> method will throw an <see cref="ObjectDisposedException"/>.</item>
    /// <item>The <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcewriter.addalias" target="_blank">System.Resources.ResXResourceWriter.AddAlias</a> method just
    /// populates an inner alias list causing that the alias will be recognized on further processing but will never be dumped into the output stream. In this <see cref="ResXResourceWriter"/> implementation
    /// the <see cref="AddAlias(string,AssemblyName,bool)">AddAlias</see> method is somewhat different: not just registers the alias as a known one but also dumps that into the output stream.
    /// It can be specified though, whether the dump should be deferred until the alias is actually referenced for the first time.</item>
    /// </list>
    /// </para>
    /// <para><strong>New features and improvements</strong> compared to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcewriter" target="_blank">System.Resources.ResXResourceWriter</a>:
    /// <list type="bullet">
    /// <item><term>Compatibility</term>
    /// <description>If <see cref="CompatibleFormat"/> is <see langword="true"/>, the resulting .resx file can be read by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a>.</description></item>
    /// <item><term>Compactness</term>
    /// <description>The more compact output is achieved in multiple ways:
    /// <list type="bullet">
    /// <item>If <see cref="OmitHeader"/> is <see langword="true"/>, the header and the schema is not dumped into the resulting .resx file. If <see cref="CompatibleFormat"/> is <see langword="true"/>, then only
    /// the header comment can be omitted.</item>
    /// <item>Whitespace preserving to string values is applied only if it is really necessary (even if <see cref="CompatibleFormat"/> is <see langword="true"/>).</item>
    /// <item>If an object can only be binary serialized, then instead of using <see cref="BinaryFormatter"/> it is serialized by <see cref="BinarySerializationFormatter"/>, which produces a much more compact result (only if <see cref="CompatibleFormat"/> is <see langword="false"/>).
    /// A new MIME type has been introduced to identify binary data serialized this new way.</item>
    /// </list></description></item>
    /// <item><strong>New overloads:</strong>
    /// <list type="bullet">
    /// <item><see cref="AddAlias(string,string,bool)">AddAlias</see> method now can be called with a <see cref="string"/> assembly name and not just by an <see cref="AssemblyName"/> instance.
    /// Both overloads have now an optional <see cref="bool"/> argument for specifying whether the alias must be dumped immediately.</item>
    /// <item>New <see cref="AddMetadata(ResXDataNode)">AddMetadata(ResXDataNode)</see> overload, working similarly to the existing <see cref="AddResource(ResXDataNode)">AddResource(ResXDataNode)</see> method.</item>
    /// </list></item>
    /// <item><term><see cref="AutoGenerateAlias"/> property</term>
    /// <description>If <see langword="true"/>, alias names for assemblies will be automatically generated without calling <see cref="AddAlias(string,string,bool)">AddAlias</see> method.
    /// If <see langword="false"/>, the assembly qualified names will be used for non-mscorlib types, unless an alias name was explicitly added by the <see cref="AddAlias(string,string,bool)">AddAlias</see> method or the <see cref="ResXDataNode"/> to dump already contains an alias name.</description></item>
    /// <item><strong>Better support of several types:</strong>
    /// <list type="table">
    /// <listheader><term>Type</term><term>Improvement</term><term>How it is handled by the System version</term></listheader>
    /// <item>
    /// <term><see langword="null"/>&#160;value</term>
    /// <term>Invalid .resx representations of <see langword="null"/>&#160;value is fixed when re-written by <see cref="ResXResourceWriter"/>.
    /// </term><term>When the System version rewrites such an invalid <see langword="null"/>&#160;node, it turns into empty string.</term></item>
    /// <item><term><see cref="Array"/> of <see cref="sbyte"/></term>
    /// <term>Serializing <see cref="sbyte">sbyte[]</see> type works properly.</term>
    /// <term>The <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcewriter" target="_blank">System.Resources.ResXResourceWriter</a> changes the <see cref="sbyte">sbyte[]</see> types to <see cref="byte">byte[]</see>.</term></item>
    /// <item><term><see cref="char"/></term>
    /// <term>Support of unpaired surrogate characters.</term>
    /// <term><a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcewriter" target="_blank">System.Resources.ResXResourceWriter</a> cannot serialize unpaired surrogates, though
    /// <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> can read them successfully if they are serialized by <see cref="ResXResourceWriter"/> and <see cref="CompatibleFormat"/> is <see langword="true"/>.</term></item>
    /// <item><term><see cref="string"/> and any type serialized by a <see cref="TypeConverter"/>.</term>
    /// <term>Strings containing unpaired surrogates and invalid Unicode characters can be written without any error.</term>
    /// <term><a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcewriter" target="_blank">System.Resources.ResXResourceWriter</a> cannot serialize such strings, though
    /// <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> can read them successfully if they are serialized by <see cref="ResXResourceWriter"/> and <see cref="CompatibleFormat"/> is <see langword="true"/>.</term></item>
    /// <item><term><see cref="DateTime"/> and <see cref="DateTimeOffset"/></term>
    /// <term>Serialized in a different way so even the milliseconds part is preserved.</term>
    /// <term>The fixed form can be deserialized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a>, too;
    /// however, the <see cref="DateTime.Kind">DateTime.Kind</see> will be always <see cref="DateTimeKind.Local"/>.</term></item>
    /// <item><term><see cref="float"/>, <see cref="double"/> and <see cref="decimal"/></term>
    /// <term>-0 (negative zero) value is handled correctly.</term>
    /// <term>The fixed form can be deserialized by <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a>, too;
    /// however, in case of <see cref="float"/> and <see cref="double"/> -0 will always turn to +0.</term></item>
    /// <item><term><see cref="IntPtr"/>, <see cref="UIntPtr"/>, <see cref="DBNull"/> and <see cref="Type"/> instances containing a runtime type.</term>
    /// <term>These types are supported natively (without a <c>mimetype</c> attribute). Only if <see cref="CompatibleFormat"/> is <see langword="false"/>.</term>
    /// <term><a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcewriter" target="_blank">System.Resources.ResXResourceWriter</a> can serialize these type only by <see cref="BinaryFormatter"/>.
    /// Though in .NET Core and .NET Standard <see cref="Type"/> is not serializable even by <see cref="BinaryFormatter"/>.</term></item>
    /// <item><term>Generic types</term>
    /// <term>Generic types with a <see cref="TypeConverter"/> are handled correctly.</term>
    /// <term>Parsing generic type names may fail with <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a>.
    /// The problem does not occur on binary serialization because in that case the type name is not dumped into the .resx file but is encoded in the binary stream.</term></item>
    /// <item><term>Any non-serializable type</term>
    /// <term>As long as the <see cref="BinarySerializationFormatter"/> can serialize the non-serializable type, this implementation supports non-serializable types as well. This works even if <see cref="CompatibleFormat"/> is <see langword="true"/>.</term>
    /// <term>If <see cref="CompatibleFormat"/> is <see langword="true"/>&#160;during serialization, deserialization works even with <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a>
    /// as long as <c>KGySoft.CoreLibraries</c> assembly can be loaded and <see cref="BinaryFormatter"/> can find the <see cref="AnyObjectSerializerWrapper"/> class.</term></item>
    /// </list></item>
    /// </list>
    /// </para>
    /// </remarks>
    public sealed class ResXResourceWriter : IResourceWriter
    {
        #region ResXWriter class

        /// <summary>
        /// Required because a writer created by XmlWriter.Create preserves \r chars only if they are entitized;
        /// however, to keep things readable (and compatible), entitization should be omitted on new lines and base64 wrapping,
        /// but entitization cannot be changed once the writing has been started. So this writer:
        /// - Entitizes \r \n chars only if they are not used together
        /// - Can write strings containing unpaired surrogates and invalid Unicode characters. This result can be read correctly even by system ResXResourceReader.
        /// </summary>
        private sealed class ResXWriter : XmlTextWriter
        {
            #region Fields

            private Stream? stream;
            private TextWriter? textWriter;
            private bool isInsideAttribute;

            #endregion

            #region Properties

            internal bool FromTextWriter => textWriter != null;

            #endregion

            #region Constructors

            internal ResXWriter(string fileName)
                : this(new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
            }

            internal ResXWriter(Stream stream) : base(stream, null)
            {
                this.stream = stream;
                Formatting = Formatting.Indented;
            }

            internal ResXWriter(TextWriter textWriter) : base(textWriter)
            {
                this.textWriter = textWriter;
                Formatting = Formatting.Indented;
            }

            #endregion

            #region Methods

            #region Public Methods

            [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
            [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute", Justification = "False alarm, prefix and ns can be null")]
            public override void WriteStartAttribute(string? prefix, string localName, string? ns)
            {
                isInsideAttribute = true;
                base.WriteStartAttribute(prefix, localName, ns);
            }

            public override void WriteEndAttribute()
            {
                isInsideAttribute = false;
                base.WriteEndAttribute();
            }

            public override void WriteString(string? text)
            {
                if (text == null)
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
                            if (c == '\t' // TAB is OK
                                || (c >= 0x20 && c.IsValidCharacter()))
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
                                        ++i;
                                    continue;
                                }
                            }

                            // valid surrogate pair
                            if (i < text.Length - 1 && Char.IsSurrogatePair(c, text[i + 1]))
                            {
                                sb.Append(new[] { c, text[i + 1] });
                                ++i;
                                continue;
                            }

                            // none of above: entitizing (including invalid Unicode characters)
                            sb.Append("&#x");
                            sb.Append(((int)c).ToString("X", CultureInfo.InvariantCulture));
                            sb.Append(';');
                            break;
                    }
                }

                WriteRaw(sb.ToString());
            }

            #endregion

            #region Protected Methods

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    stream?.Close();
                    textWriter?.Close();
                }

                stream = null;
                textWriter = null;
            }

            #endregion

            #endregion
        }

        #endregion

        #region Fields

        #region Static Fields

        private static readonly string resourceHeader = $@"
    <!-- 
    Microsoft ResX Schema 
    
    Version {ResXCommon.Version}
    
    The primary goals of this format is to allow a simple XML format 
    that is mostly human readable. The generation and parsing of the 
    various data types are done through the TypeConverter classes 
    associated with the data types.
    
    Example:
    
    ... ado.net/XML headers & schema ...
    <resheader name=""resmimetype"">text/microsoft-resx</resheader>
    <resheader name=""version"">{ResXCommon.Version}</resheader>
    <resheader name=""reader"">System.Resources.ResXResourceReader, System.Windows.Forms, ...</resheader>
    <resheader name=""writer"">System.Resources.ResXResourceWriter, System.Windows.Forms, ...</resheader>
    <data name=""Name1""><value>this is my long string</value><comment>this is a comment</comment></data>
    <data name=""Color1"" type=""System.Drawing.Color, System.Drawing"">Blue</data>
    <data name=""Bitmap1"" mimetype=""{ResXCommon.BinSerializedObjectMimeType}"">
        <value>[base64 mime encoded serialized .NET Framework object]</value>
    </data>
    <data name=""Icon1"" type=""System.Drawing.Icon, System.Drawing"" mimetype=""{ResXCommon.ByteArraySerializedObjectMimeType}"">
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
    
    Note - {ResXCommon.BinSerializedObjectMimeType} is the format 
    that the ResXResourceWriter will generate, however the reader can 
    read any of the formats listed below.
    
    mimetype: {ResXCommon.BinSerializedObjectMimeType}
    value   : The object must be serialized with 
            : System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
            : and then encoded with base64 encoding.
    
    mimetype: {ResXCommon.ByteArraySerializedObjectMimeType}
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

        #endregion

        #region Instance Fields

        private readonly Func<Type, string?>? typeNameConverter; // not a public property to be consistent with ResXDataNode class.

        /// <summary>
        /// Stores the alias mapping that should be used when writing assemblies.
        /// </summary>
        private StringKeyedDictionary<string>? aliases;

        /// <summary>
        /// Stores the already written and active alias mapping.
        /// </summary>
        private StringKeyedDictionary<string>? activeAliases;

        private bool autoGenerateAlias = true;
        private ResXWriter? writer;
        private string? basePath;
        private bool hasBeenSaved;
        private bool initialized;
        private bool compatibleFormat = true;
        private bool omitHeader = true;
        private bool safeMode;

        #endregion

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets whether the <see cref="ResXResourceWriter"/> instance should create a System compatible .resx file, which can be used
        /// by the built-in resource editor of the Visual Studio.
        /// <br/>Default value: <see langword="true"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <remarks>
        /// The value of the property affects the following differences:
        /// <list type="bullet">
        /// <item><description>The <c>reader</c> and <c>writer</c> attributes in <c>resheader</c> elements.</description></item>
        /// <item><description>Type of <see cref="ResXFileRef"/> file references.</description></item>
        /// <item><description>The placeholder type of <see langword="null"/>&#160;references.</description></item>
        /// <item><description>If <c>CompatibleFormat</c> is <see langword="false"/>, some additional types are supported natively (without a <c>mimetype</c> attribute): <see cref="IntPtr"/>, <see cref="UIntPtr"/>, <see cref="DBNull"/> and <see cref="Type"/>.</description></item>
        /// <item><description>If <c>CompatibleFormat</c> is <see langword="false"/>, unpaired surrogate <see cref="char"/> values are supported.</description></item>
        /// <item><description>The <c>mimetype</c> and content of binary serialized elements. If <c>CompatibleFormat</c> is <see langword="false"/>, these objects are
        /// serialized by <see cref="BinarySerializationFormatter"/>, which provides a much more compact result than the default <see cref="BinaryFormatter"/>.</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">In a set operation, a value cannot be specified because the creation of the .resx file content has already been started.</exception>
        public bool CompatibleFormat
        {
            get => compatibleFormat;
            set
            {
                if (initialized)
                    Throw.InvalidOperationException(Res.ResourcesInvalidResXWriterPropertyChange);
                compatibleFormat = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the header should be omitted. If both <see cref="CompatibleFormat"/> and <see cref="OmitHeader"/> are <see langword="true"/>, then
        /// only the XML comment will be omitted. If <see cref="CompatibleFormat"/> is <see langword="false"/>&#160;and <see cref="OmitHeader"/> is <see langword="true"/>, then
        /// the comment, the .resx schema and the <c>&lt;resheader&gt;</c> elements will be omitted, too.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">In a set operation, a value cannot be specified because the creation of the .resx file content has already been started.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceWriter"/> instance is already disposed.</exception>
        public bool OmitHeader
        {
            get => omitHeader;
            set
            {
                if (initialized)
                    Throw.InvalidOperationException(Res.ResourcesInvalidResXWriterPropertyChange);

                if (writer == null)
                    Throw.ObjectDisposedException();

                omitHeader = value;
            }
        }

        /// <summary>
        /// Gets or sets the base path for the relative file path specified in a <see cref="ResXFileRef"/> object.
        /// <br/>Default value: <see langword="null"/>.
        /// </summary>
        /// <returns>
        /// A path that, if prepended to the relative file path specified in a <see cref="ResXFileRef"/> object, yields an absolute path to an XML resource file.
        /// </returns>
        /// <exception cref="InvalidOperationException">In a set operation, a value cannot be specified because the creation of the .resx file content has already been started.</exception>
        public string? BasePath
        {
            get => basePath;
            set
            {
                if (initialized)
                    Throw.InvalidOperationException(Res.ResourcesInvalidResXWriterPropertyChange);
                basePath = value;
            }
        }

        /// <summary>
        /// Gets or sets whether an alias should be auto-generated for referenced assemblies.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="AutoGenerateAlias"/> is <see langword="false"/>, then the assembly names will be referenced by fully qualified names
        /// unless the alias names are explicitly added by the <see cref="O:KGySoft.Resources.ResXResourceWriter.AddAlias">AddAlias</see> methods, or when a <see cref="ResXDataNode"/>
        /// added by <see cref="AddResource(ResXDataNode)"/> method already contains an alias.</para>
        /// <para>If <see cref="AutoGenerateAlias"/> is <see langword="true"/>, then the assembly aliases are re-generated, even if a <see cref="ResXDataNode"/>
        /// already contains an alias. To use explicitly defined names instead of auto generated names use the <see cref="O:KGySoft.Resources.ResXResourceWriter.AddAlias">AddAlias</see> methods.</para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceWriter"/> instance is already disposed.</exception>
        public bool AutoGenerateAlias
        {
            get => autoGenerateAlias;
            set
            {
                if (writer == null)
                    Throw.ObjectDisposedException();
                autoGenerateAlias = value;
            }
        }

        /// <summary>
        /// Gets or sets whether it is prohibited to load assemblies when writing <see cref="ResXDataNode"/> instances whose raw .resx content
        /// needs to be regenerated and whose value has not been deserialized yet. This can occur only if <see cref="CompatibleFormat"/> is <see langword="true"/>,
        /// and when the <see cref="ResXDataNode"/> instances to write have been read from another .resx source.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>This property affects only <see cref="AddResource(ResXDataNode)"/> and <see cref="AddMetadata(ResXDataNode)"/> methods when <see cref="CompatibleFormat"/>
        /// is <see langword="true"/>, and the <see cref="ResXDataNode"/> to write contains no deserialized value but only raw .resx data that is not compatible with
        /// the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> class.</para>
        /// </remarks>
        public bool SafeMode
        {
            get => safeMode;
            set
            {
                if (writer == null)
                    Throw.ObjectDisposedException();
                safeMode = value;
            }
        }

        #endregion

        #region Private Properties

        private XmlWriter Writer
        {
            get
            {
                if (writer == null)
                    Throw.ObjectDisposedException();

                if (!initialized)
                    InitializeWriter();

                return writer;
            }
        }

        #endregion

        #endregion

        #region Construction and Destruction

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceWriter"/> class that writes the resources to a specified file.
        /// </summary>
        /// <param name="fileName">The file to send output to.</param>
        /// <param name="typeNameConverter">A delegate that can be used to specify type names explicitly (eg. to target earlier versions of assemblies or the .NET Framework). This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>If <paramref name="typeNameConverter"/> is specified it can be used to dump custom type names for any type. If it returns <see langword="null"/>&#160;for a <see cref="Type"/>, then the default
        /// name will be used. To deserialize a .resx content with custom type names the <see cref="ResXResourceReader"/> constructors should be called with a non-<see langword="null"/>&#160;<see cref="ITypeResolutionService"/> instance.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langowrd="null"/>.</exception>
        public ResXResourceWriter(string fileName, Func<Type, string?>? typeNameConverter = null)
        {
            if (fileName == null!)
                Throw.ArgumentNullException(Argument.fileName);

            writer = new ResXWriter(fileName);
            this.typeNameConverter = typeNameConverter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceWriter"/> class that writes the resources to a specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to send the output to.</param>
        /// <param name="typeNameConverter">A delegate that can be used to specify type names explicitly (eg. to target earlier versions of assemblies or the .NET Framework). This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>If <paramref name="typeNameConverter"/> is specified it can be used to dump custom type names for any type. If it returns <see langword="null"/>&#160;for a <see cref="Type"/>, then the default
        /// name will be used. To deserialize a .resx content with custom type names the <see cref="ResXResourceReader"/> constructors should be called with a non-<see langword="null"/>&#160;<see cref="ITypeResolutionService"/> instance.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langowrd="null"/>.</exception>
        public ResXResourceWriter(Stream stream, Func<Type, string?>? typeNameConverter = null)
        {
            if (stream == null!)
                Throw.ArgumentNullException(Argument.stream);

            writer = new ResXWriter(stream);
            this.typeNameConverter = typeNameConverter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceWriter"/> class that writes the resources by a specified <paramref name="textWriter"/>.
        /// </summary>
        /// <param name="textWriter">The <see cref="TextWriter"/> object to send output to.</param>
        /// <param name="typeNameConverter">A delegate that can be used to specify type names explicitly (eg. to target earlier versions of assemblies or the .NET Framework). This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>If <paramref name="typeNameConverter"/> is specified it can be used to dump custom type names for any type. If it returns <see langword="null"/>&#160;for a <see cref="Type"/>, then the default
        /// name will be used. To deserialize a .resx content with custom type names the <see cref="ResXResourceReader"/> constructors should be called with a non-<see langword="null"/>&#160;<see cref="ITypeResolutionService"/> instance.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="textWriter"/> is <see langowrd="null"/>.</exception>
        public ResXResourceWriter(TextWriter textWriter, Func<Type, string?>? typeNameConverter = null)
        {
            if (textWriter == null!)
                Throw.ArgumentNullException(Argument.textWriter);

            writer = new ResXWriter(textWriter);
            this.typeNameConverter = typeNameConverter;
        }

        #endregion

        #region Destructor

        /// <summary>
        /// This member overrides the <see cref="Object.Finalize"/> method.
        /// </summary>
        ~ResXResourceWriter() => Dispose(false);

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        private static bool PreserveSpaces(string value)
        {
            char c;
            return value.Length > 0 && ((c = value[0]) == ' ' || c == '\t' || c == '\r' || c == '\n');
        }

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Adds the specified alias to the mapping of aliases.
        /// </summary>
        /// <param name="aliasName">The name of the alias.</param>
        /// <param name="assemblyName">The name of the assembly represented by <paramref name="aliasName"/>.</param>
        /// <param name="forceWriteImmediately"><see langword="true"/>&#160;to write the alias immediately to the .resx file; <see langword="false"/>&#160;just to
        /// add it to the inner mapping and write it only when the assembly is referenced for the first time. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="aliasName"/> or <paramref name="assemblyName"/> is <see langword="null"/>.</exception>
        public void AddAlias(string aliasName, AssemblyName assemblyName, bool forceWriteImmediately = false)
        {
            if (assemblyName == null!)
                Throw.ArgumentNullException(Argument.assemblyName);

            AddAlias(aliasName, assemblyName.FullName, forceWriteImmediately);
        }

        /// <summary>
        /// Adds the specified alias to the mapping of aliases.
        /// </summary>
        /// <param name="aliasName">The name of the alias.</param>
        /// <param name="assemblyName">The name of the assembly represented by <paramref name="aliasName"/>.</param>
        /// <param name="forceWriteImmediately"><see langword="true"/>&#160;to write the alias immediately to the .resx file; <see langword="false"/>&#160;just to
        /// add it to the inner mapping and write it only when the assembly is referenced for the first time. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="aliasName"/> or <paramref name="assemblyName"/> is <see langword="null"/>.</exception>
        public void AddAlias(string aliasName, string assemblyName, bool forceWriteImmediately = false)
        {
            if (aliasName == null!)
                Throw.ArgumentNullException(Argument.aliasName);
            if (assemblyName == null!)
                Throw.ArgumentNullException(Argument.assemblyName);

            aliases ??= new StringKeyedDictionary<string>();
            aliases[assemblyName] = aliasName;

            if (forceWriteImmediately)
                AddAssemblyRow(aliasName, assemblyName);
        }

        /// <summary>
        /// Adds a metadata node whose value is specified as a byte array to the list of resources to write.
        /// </summary>
        /// <param name="name">The name of a property.</param>
        /// <param name="value">A byte array containing the value of the property to add.</param>
        public void AddMetadata(string name, byte[]? value) => AddDataRow(ResXCommon.MetadataStr, name, value);

        /// <summary>
        /// Adds a metadata node whose value is specified as a string to the list of resources to write.
        /// </summary>
        /// <param name="name">The name of a property.</param>
        /// <param name="value">A string that is the value of the property to add.</param>
        public void AddMetadata(string name, string? value) => AddDataRow(ResXCommon.MetadataStr, name, value);

        /// <summary>
        /// Adds a metadata node whose value is specified as an object to the list of resources to write.
        /// </summary>
        /// <param name="name">The name of a property.</param>
        /// <param name="value">An object that is the value of the property to add.</param>
        public void AddMetadata(string name, object? value)
        {
            if (value is ResXDataNode node)
            {
                if (name != node.Name)
                    node = new ResXDataNode(name, value);
                AddMetadata(node);
            }
            else
                AddDataRow(ResXCommon.MetadataStr, name, value);
        }

        /// <summary>
        /// Adds a metadata node specified in a <see cref="ResXDataNode"/> object to the list of resources to write.
        /// </summary>
        /// <param name="node">A <see cref="ResXDataNode"/> object that contains a metadata name/value pair.</param>
        public void AddMetadata(ResXDataNode node)
        {
            if (node == null!)
                Throw.ArgumentNullException(Argument.node);
            AddDataRow(ResXCommon.MetadataStr, node.Name, node);
        }

        /// <summary>
        /// Adds a named resource specified as a byte array to the list of resources to write.
        /// </summary>
        /// <param name="name">The name of the resource. </param>
        /// <param name="value">The value of the resource to add as an 8-bit unsigned integer array.</param>
        public void AddResource(string name, byte[]? value) => AddDataRow(ResXCommon.DataStr, name, value);

        /// <summary>
        /// Adds a named resource specified as an object to the list of resources to write.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        /// <param name="value">The value of the resource.</param>
        public void AddResource(string name, object? value)
        {
            if (value is ResXDataNode node)
            {
                if (name != node.Name)
                    node = new ResXDataNode(name, value);
                AddResource(node);
            }
            else
                AddDataRow(ResXCommon.DataStr, name, value);
        }

        /// <summary>
        /// Adds a string resource to the resources.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        /// <param name="value">The value of the resource.</param>
        public void AddResource(string name, string? value) => AddDataRow(ResXCommon.DataStr, name, value);

        /// <summary>
        /// Adds a named resource specified in a <see cref="ResXDataNode"/> object to the list of resources to write.
        /// </summary>
        /// <param name="node">A <see cref="ResXDataNode"/> object that contains a resource name/value pair.</param>
        public void AddResource(ResXDataNode node)
        {
            if (node == null!)
                Throw.ArgumentNullException(Argument.node);
            AddDataRow(ResXCommon.DataStr, node.Name, node);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ResXResourceWriter"/>.
        /// If content has not been saved yet (see <see cref="Generate">Generate</see> method), then firstly flushes any remaining content.
        /// </summary>
        /// <remarks>Calling this method is the equivalent of calling <see cref="Dispose()">Dispose</see>.</remarks>
        public void Close() => Dispose();

        /// <summary>
        /// Releases all resources used by the <see cref="ResXResourceWriter"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Flushes all pending content into the output file, <see cref="TextWriter"/> or <see cref="Stream"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The resource has already been saved.</exception>
        /// <exception cref="ObjectDisposedException">The writer has already been disposed.</exception>
        /// <remarks>If used in a <c>using</c> construct, it is not needed to call this method explicitly (see the example at <see cref="ResXResourceWriter"/>).
        /// <see cref="Close">Close</see> and <see cref="Dispose()">Dispose</see> methods call it internally if necessary.</remarks>
        public void Generate()
        {
            if (hasBeenSaved)
                Throw.InvalidOperationException(Res.ResourcesWriterSaved);

            hasBeenSaved = true;
            XmlWriter w = Writer;
            w.WriteEndElement();
            w.Flush();
        }

        #endregion

        #region Private Methods

        private void InitializeWriter()
        {
            Debug.Assert(writer != null);

            // otherwise, UTF-16 would have been dumped
            if (writer!.FromTextWriter)
                writer.WriteRaw("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            else
                writer.WriteStartDocument();

            initialized = true;
            writer.WriteStartElement("root");
            if (compatibleFormat || !omitHeader)
            {
                using (XmlReader reader = XmlReader.Create(new StringReader(compatibleFormat
                        ? (omitHeader ? resourceSchema : resourceHeader + resourceSchema)
                        : resourceSchema),
                    new XmlReaderSettings { CloseInput = true, IgnoreWhitespace = true }))
                {
                    writer.WriteNode(reader, true);
                }
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

        private void AddDataRow(string elementName, string name, ResXDataNode node)
        {
            DataNodeInfo info = node.GetDataNodeInfo(typeNameConverter, compatibleFormat, safeMode);
            string? value = info.ValueData;
            ResXFileRef? fileRef;
            if (basePath != null && (fileRef = node.FileRef) != null && Path.IsPathRooted(fileRef.FileName))
                value = ResXFileRef.ToString(Files.GetRelativePath(fileRef.FileName, basePath), fileRef.TypeName, fileRef.EncodingName);

            AddDataRow(elementName, name, value, GetTypeNameWithAlias(info), info.MimeType, info.Comment);
        }

        private void AddDataRow(string elementName, string name, byte[]? value)
        {
            // if it's null, set it here as a ResXNullRef
            if (value == null)
                AddDataRow(elementName, name, null, GetTypeNameWithAlias(typeof(ResXNullRef)), null, null);
            else
                AddDataRow(elementName, name, ResXCommon.ToBase64(value), GetTypeNameWithAlias(Reflector.ByteArrayType), null, null);
        }

        private void AddDataRow(string elementName, string name, object? value)
        {
            // 1.) String
            if (value is string valueData)
            {
                AddDataRow(elementName, name, valueData);
                return;
            }

            // 2.) byte[] - double check must be performed because of sbyte[] (https://stackoverflow.com/q/33896316/5114784)
            if (value is byte[] byteArray && value.GetType() == Reflector.ByteArrayType)
            {
                AddDataRow(elementName, name, byteArray);
                return;
            }

            // 3.) Any other (including null and ResXFileRef (and WinForms.ResXDataNode/ResXFileRef, too))
            ResXDataNode node = new ResXDataNode(name, value);
            AddDataRow(elementName, name, node);
        }

        private void AddDataRow(string elementName, string name, string? value)
        {
            // if it's a null string, set it here as a ResXNullRef
            if (value == null)
                AddDataRow(elementName, name, null, GetTypeNameWithAlias(typeof(ResXNullRef)), null, null);
            else
                AddDataRow(elementName, name, value, null, null, null);
        }

        private void AddDataRow(string elementName, string name, string? value, string? typeWithAlias, string? mimeType, string? comment)
        {
            if (hasBeenSaved)
                Throw.InvalidOperationException(Res.ResourcesWriterSaved);

            XmlWriter w = Writer;
            w.WriteStartElement(elementName);
            w.WriteAttributeString(ResXCommon.NameStr, name);

            if (typeWithAlias != null)
                w.WriteAttributeString(ResXCommon.TypeStr, typeWithAlias);

            if (mimeType != null)
                w.WriteAttributeString(ResXCommon.MimeTypeStr, mimeType);

            if (value != null && mimeType == null && (typeWithAlias == null || !typeWithAlias.StartsWith("System.Byte[]", StringComparison.Ordinal)) && PreserveSpaces(value))
                w.WriteAttributeString(ResXCommon.XmlStr, ResXCommon.SpaceStr, null, ResXCommon.PreserveStr);

            // for empty strings writing <value/> for compatibility reasons; otherwise, null and empty string representation is differentiated
            w.WriteStartElement(ResXCommon.ValueStr);
            if (value != null && (value.Length > 0 || typeWithAlias != null))
                w.WriteString(value);

            w.WriteEndElement();
            if (!String.IsNullOrEmpty(comment))
            {
                w.WriteStartElement(ResXCommon.CommentStr);
                w.WriteString(comment);
                w.WriteEndElement();
            }

            w.WriteEndElement();
        }

        private void AddAssemblyRow(string alias, string assembly)
        {
            XmlWriter w = Writer;
            w.WriteStartElement(ResXCommon.AssemblyStr);
            w.WriteAttributeString(ResXCommon.AliasStr, alias);
            w.WriteAttributeString(ResXCommon.NameStr, assembly);
            w.WriteEndElement();
            activeAliases ??= new StringKeyedDictionary<string>();
            activeAliases[assembly] = alias;
        }

        private string? GetTypeNameWithAlias(DataNodeInfo info)
            => info.TypeName == null ? null : GetTypeNameWithAlias(info.TypeName, info.AssemblyAliasValue);

        private string GetTypeNameWithAlias(Type type)
            => GetTypeNameWithAlias(ResXCommon.GetAssemblyQualifiedName(type, typeNameConverter, compatibleFormat), null);

        /// <summary>
        /// Gets the type name with alias.
        /// </summary>
        /// <param name="origTypeName">Input type name with assembly or alias name.</param>
        /// <param name="asmName">If not null, origTypeName contains an alias name, and this is the assembly name.</param>
        /// <returns>The type name with a (possibly generated) alias or with the full name.</returns>
        private string GetTypeNameWithAlias(string origTypeName, string? asmName)
        {
            int genericEnd = origTypeName.LastIndexOf(']');
            int asmNamePos = origTypeName.IndexOf(',', genericEnd + 1);
            string typeName = asmNamePos >= 0 ? origTypeName.Substring(0, asmNamePos).Trim() : origTypeName;
            string? asmOrAliasName = asmNamePos >= 0 ? origTypeName.Substring(asmNamePos + 1).Trim() : null;
            string? alias;

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
                string generatedAlias = new AssemblyName(asmName).Name!;
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
            alias = new AssemblyName(asmName).Name!;
            AddAssemblyRow(alias, asmName);
            return typeName + ", " + alias;
        }

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

        #endregion

        #endregion

        #endregion
    }
}
