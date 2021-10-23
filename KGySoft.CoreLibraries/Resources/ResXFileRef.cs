#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXFileRef.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents a link to an external resource.
    /// <br/>See the <strong>Remarks</strong> section for the differences compared to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a> class.
    /// </summary>
    /// <remarks>
    /// <note>This class is similar to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a>
    /// in <c>System.Windows.Forms.dll</c>. See the <a href="#comparison">Comparison with System.Resources.ResXFileRef</a> section for the differences.</note>
    /// <para>The <see cref="ResXFileRef"/> class is used to include references to files in an XML resource (.resx) file.
    /// A <see cref="ResXFileRef"/> object represents a link to an external resource in an XML resource (.resx) file.
    /// You can add a <see cref="ResXFileRef"/> object to a .resx file programmatically by one of the following options:
    /// <list type="bullet">
    /// <item>Call the <see cref="ResXResourceWriter.AddResource(string,object)">ResXResourceWriter.AddResource(string, object)</see> method where the second parameter is a <see cref="ResXFileRef"/> instance.</item>
    /// <item>Call the <see cref="ResXResourceSet.SetObject">ResXResourceSet.SetObject(string, object)</see> method where the second parameter is a <see cref="ResXFileRef"/> instance and then save the <see cref="ResXResourceSet"/> instance.</item>
    /// <item>Call the <see cref="ResXResourceManager.SetObject">ResXResourceManager.SetObject(string, object, CultureInfo)</see> method where the second parameter is a <see cref="ResXFileRef"/> instance and then save the <see cref="ResXResourceManager"/> instance.</item>
    /// <item>Call the <see cref="HybridResourceManager.SetObject">HybridResourceManager.SetObject(string, object, CultureInfo)</see> method where the second parameter is a <see cref="ResXFileRef"/> instance and then save the <see cref="HybridResourceManager"/> instance.</item>
    /// <item>Call the <see cref="DynamicResourceManager.SetObject">HybridResourceManager.SetObject(string, object, CultureInfo)</see> method where the second parameter is a <see cref="ResXFileRef"/> instance and then save the <see cref="DynamicResourceManager"/> instance.</item>
    /// </list>
    /// </para>
    /// <h1 class="heading">Comparison with System.Resources.ResXFileRef<a name="comparison">&#160;</a></h1>
    /// <note>The compatibility with <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a> is provided without any reference to <c>System.Windows.Forms.dll</c>, where that type is located.</note>
    /// <note>When serialized in compatibility mode (see <see cref="ResXResourceWriter.CompatibleFormat">ResXResourceWriter.CompatibleFormat</see>, <see cref="O:KGySoft.Resources.ResXResourceSet.Save">ResXResourceSet.Save</see>,
    /// <see cref="ResXResourceManager.SaveResourceSet">ResXResourceManager.SaveResourceSet</see> and <see cref="ResXResourceManager.SaveAllResources">ResXResourceManager.SaveAllResources</see>),
    /// the result will be able to be parsed by the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a> type, too.</note>
    /// <para><strong>Incompatibility</strong> with <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a>:
    /// <list type="bullet">
    /// <item>The <see cref="ResXFileRef(string,Type,Encoding)">constructor</see> is incompatible with <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a>
    /// implementation. Unlike in system version you must specify the type by a <see cref="Type"/> instance instead of a string.</item>
    /// </list></para>
    /// <para><strong>New features and improvements</strong> compared to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a>:
    /// <list type="bullet">
    /// <item><term>Parsing</term><description>A string can parsed to a <see cref="ResXFileRef"/> instance by <see cref="Parse"/> and <see cref="TryParse"/> methods.</description></item>
    /// </list></para>
    /// <note type="security">The <see cref="TypeConverter"/> that is assigned to the <see cref="ResXFileRef"/> type may load assemblies when its <see cref="TypeConverter.ConvertFrom(ITypeDescriptorContext,CultureInfo,object)">ConvertFrom</see> method is called.
    /// The recommended way to retrieve a file resource is via the <see cref="ResXDataNode"/> class. Its <see cref="ResXDataNode.GetValueSafe">GetValueSafe</see> method guarantees that no assembly is loaded
    /// during the deserialization, including retrieving resources from file references.</note>
    /// </remarks>
    /// <seealso cref="ResXDataNode"/>
    /// <seealso cref="ResXResourceWriter"/>
    /// <seealso cref="ResXResourceSet"/>
    /// <seealso cref="ResXResourceManager"/>
    /// <seealso cref="HybridResourceManager"/>
    [TypeConverter(typeof(Converter))]
    [Serializable]
    public sealed class ResXFileRef
    {
        #region Nested classes

        #region Converter class

        private class Converter : TypeConverter
        {
            #region Methods

            #region Static Methods

            internal static string[]? ParseResXFileRefString(string? stringValue)
            {
                if (stringValue == null)
                    return null;

                stringValue = stringValue.Trim();
                string fileName;
                string remainingString;
                if (stringValue.Length > 0 && stringValue[0] == '"')
                {
                    int lastIndexOfQuote = stringValue.LastIndexOf('"');
                    if (lastIndexOfQuote - 1 < 0)
                        return null;
                    fileName = stringValue.Substring(1, lastIndexOfQuote - 1);
                    if (lastIndexOfQuote + 2 > stringValue.Length)
                        return null;
                    remainingString = stringValue.Substring(lastIndexOfQuote + 2);
                }
                else
                {
                    int nextSemicolon = stringValue.IndexOf(';');
                    if (nextSemicolon == -1)
                        return null;
                    fileName = stringValue.Substring(0, nextSemicolon);
                    if (nextSemicolon + 1 > stringValue.Length)
                        return null;
                    remainingString = stringValue.Substring(nextSemicolon + 1);
                }

                string[] parts = remainingString.Split(';');
                string[] result;
                if (parts.Length > 1)
                    result = new[] { fileName, parts[0], parts[1] };
                else if (parts.Length > 0)
                    result = new[] { fileName, parts[0] };
                else
                    result = new[] { fileName };

                return result;
            }

            internal static object ConvertFrom(string stringValue, Type? objectType, string? basePath)
            {
                if (stringValue == null!)
                    Throw.ArgumentNullException(Argument.stringValue);
                string[]? parts = ParseResXFileRefString(stringValue);
                if (parts == null)
                    Throw.ArgumentException(Argument.stringValue, Res.ArgumentInvalidString);
                string fileName = parts[0];
                if (!String.IsNullOrEmpty(basePath) && !Path.IsPathRooted(fileName))
                    fileName = Path.Combine(basePath!, fileName);

                // Security note: the TryToLoadAssemblies flag makes possible to load any (potentially harmful) assemblies,
                // but it does not affect any public access via the ResXDataNode, which resolves the type in a safe or unsafe way,
                // and then passes non-null objectType here.
                Type? toCreate = objectType ?? TypeResolver.ResolveType(parts[1], null,
                    ResolveTypeOptions.AllowPartialAssemblyMatch | ResolveTypeOptions.TryToLoadAssemblies | ResolveTypeOptions.ThrowError);

                // string: consider encoding
                if (toCreate == Reflector.StringType)
                {
                    Encoding textFileEncoding = Encoding.Default;
                    if (parts.Length > 2)
                        textFileEncoding = Encoding.GetEncoding(parts[2]);

                    using (StreamReader sr = new StreamReader(fileName, textFileEncoding))
                    {
                        return sr.ReadToEnd();
                    }
                }

                // binary: unless a byte array or memory stream is requested, creating the result from stream
                byte[] buffer;

                if (!File.Exists(fileName))
                    Throw.FileNotFoundException(Res.ResourcesFileRefFileNotFound(fileName), fileName);
                using (FileStream s = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    buffer = s.ToArray();

                if (toCreate == Reflector.ByteArrayType)
                    return buffer;

                var memStream = new MemoryStream(buffer);
                return toCreate == typeof(MemoryStream)
                    ? memStream
                    : Reflector.CreateInstance(toCreate!, ReflectionWays.Auto, memStream);
            }

            #endregion

            #region Instance Methods

            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == Reflector.StringType;

            public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == Reflector.StringType;

            public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) => destinationType == Reflector.StringType ? value?.ToString() : null;

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value is string stringValue ? ConvertFrom(stringValue, null, null) : null;

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        private readonly string fileName;
        private readonly string typeName;
        private readonly string? encoding;

        [NonSerialized]
        private Encoding? textFileEncoding;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the file name specified in the <see cref="ResXFileRef(string,Type,Encoding)">constructor</see>.
        /// </summary>
        /// <returns>
        /// The name of the referenced file.
        /// </returns>
        public string FileName => fileName;

        /// <summary>
        /// Gets the type name specified in the <see cref="ResXFileRef(string,Type,Encoding)">constructor</see>.
        /// </summary>
        /// <returns>
        /// The type name of the resource that is referenced.
        /// </returns>
        public string TypeName => typeName;

        /// <summary>
        /// Gets the encoding specified in the <see cref="ResXFileRef(string,Type,Encoding)">constructor</see>.
        /// </summary>
        /// <returns>
        /// The encoding used in the referenced file.
        /// </returns>
        public Encoding? TextFileEncoding
        {
            get
            {
                if (textFileEncoding != null)
                    return textFileEncoding;

                if (encoding == null)
                    return null;

                return textFileEncoding = Encoding.GetEncoding(encoding);
            }
        }

        #endregion

        #region Internal Properties

        internal string? EncodingName => encoding;

        #endregion

        #endregion

        #region Constructors

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXFileRef"/> class that references the specified file.
        /// </summary>
        /// <param name="fileName">The file to reference. </param>
        /// <param name="type">The type of the resource that is referenced. Should be either <see cref="string"/>, array of <see cref="byte"/>, <see cref="MemoryStream"/> or a type, which has a constructor with one <see cref="Stream"/> parameter.</param>
        /// <param name="textFileEncoding">The encoding used in the referenced file. Used if <paramref name="type"/> is <see cref="string"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        public ResXFileRef(string fileName, Type type, Encoding? textFileEncoding = null)
        {
            if (fileName == null!)
                Throw.ArgumentNullException(Argument.fileName);
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);

            this.fileName = fileName;
            typeName = type.GetName(TypeNameKind.AssemblyQualifiedName);
            if (textFileEncoding != null)
            {
                this.textFileEncoding = textFileEncoding;
                encoding = textFileEncoding.WebName;
            }
        }

        #endregion

        #region Internal Constructors

        internal ResXFileRef(string fileName, string typeName, string? encoding)
        {
            this.fileName = fileName;
            this.typeName = typeName;
            this.encoding = encoding;
        }

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        /// <summary>
        /// Converts the string representation of a file reference to a <see cref="ResXFileRef"/> instance.
        /// </summary>
        /// <param name="s">The string representation of the file reference to convert.</param>
        /// <returns>A <see cref="ResXFileRef"/> instance that represents the file reference specified in <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="s"/> is contains invalid value.</exception>
        public static ResXFileRef Parse(string s)
        {
            if (s == null!)
                Throw.ArgumentNullException(Argument.s);

            if (TryParse(s, out ResXFileRef? result))
                return result;

            Throw.ArgumentException(Argument.s, Res.ArgumentInvalidString);
            return default;
        }

        /// <summary>
        /// Converts the string representation of a file reference to a <see cref="ResXFileRef"/> instance. A return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">The string representation of the file reference to convert.</param>
        /// <param name="result">When this method returns, contains a <see cref="ResXFileRef"/> instance that represents the file reference specified in <paramref name="s"/>,
        /// if the conversion succeeded, or <see langword="null"/>&#160;if the conversion failed.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="s"/> was converted successfully; otherwise, <see langword="false"/>.</returns>
        public static bool TryParse(string? s, [MaybeNullWhen(false)]out ResXFileRef result)
        {
            string[]? fileRefDetails = Converter.ParseResXFileRefString(s);
            if (fileRefDetails == null || fileRefDetails.Length < 2 || fileRefDetails.Length > 3)
            {
                result = null;
                return false;
            }

            result = new ResXFileRef(fileRefDetails[0], fileRefDetails[1], fileRefDetails.Length > 2 ? fileRefDetails[2] : null);
            return true;
        }

        #endregion

        #region Internal Methods

        internal static string ToString(string fileName, string typeName, string? encoding)
        {
            string result = String.Empty;

            if (fileName.IndexOf(';') != -1 || fileName.IndexOf('"') != -1)
                result += "\"" + fileName + "\";";
            else
                result += fileName + ";";

            result += typeName;
            if (encoding != null)
                result += ";" + encoding;

            return result;
        }

#if !NETCOREAPP2_0
        internal static ResXFileRef InitFromWinForms(object other) => new ResXFileRef(
            Accessors.ResXFileRef_GetFileName(other)!,
            Accessors.ResXFileRef_GetTypeName(other)!,
            Accessors.ResXFileRef_GetTextFileEncoding(other)?.WebName);
#endif

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Gets the text representation of the current <see cref="ResXFileRef"/> object.
        /// </summary>
        /// <returns>
        /// A string that consists of the concatenated text representations of the parameters specified in the <see cref="ResXFileRef(string,Type,Encoding)">constructor</see>.
        /// </returns>
        public override string ToString() => ToString(fileName, typeName, encoding);

        #endregion

        #region Internal Methods

        internal object GetValue(Type objectType, string? basePath) => Converter.ConvertFrom(ToString(), objectType, basePath);

        #endregion

        #endregion

        #endregion
    }
}
