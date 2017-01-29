using System;
using System.Xml;
using KGySoft.Libraries.Reflection;

namespace KGySoft.Libraries.Resources
{
    internal static class ResXCommon
    {
        internal const string TypeStr = "type";
        internal const string NameStr = "name";
        internal const string DataStr = "data";
        internal const string MetadataStr = "metadata";
        internal const string MimeTypeStr = "mimetype";
        internal const string ValueStr = "value";
        internal const string ResHeaderStr = "resheader";
        internal const string VersionStr = "version";
        internal const string ResMimeTypeStr = "resmimetype";
        internal const string ReaderStr = "reader";
        internal const string WriterStr = "writer";
        internal const string CommentStr = "comment";
        internal const string AssemblyStr = "assembly";
        internal const string AliasStr = "alias";

        private const string beta2CompatSerializedObjectMimeType = "text/microsoft-urt/psuedoml-serialized/base64";
        private const string compatBinSerializedObjectMimeType = "text/microsoft-urt/binary-serialized/base64";
        //internal const string CompatSoapSerializedObjectMimeType = "text/microsoft-urt/soap-serialized/base64";

        internal const string ResXNullRefNameWinForms = "System.Resources.ResXNullRef, System.Windows.Forms";
        internal const string ResXFileRefNameWinForms = "System.Resources.ResXFileRef, System.Windows.Forms";
        internal const string ResXDataNodeNameWinForms = "System.Resources.ResXDataNode, System.Windows.Forms";
        internal const string ResXFileRefNameKGySoft = "KGySoft.Libraries.Resources.ResXFileRef, KGySoft.Libraries";
        internal const string ResXNullRefNameKGySoft = "KGySoft.Libraries.Resources.ResXNullRef, KGySoft.Libraries";
        internal const string ResXResourceReaderNameWinForms = "System.Resources.ResXResourceReader, System.Windows.Forms";
        internal const string ResXResourceWriterNameWinForms = "System.Resources.ResXResourceWriter, System.Windows.Forms";

#if NET35
        private const string winformsPostfix = ", Version=2.0.3500.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
#elif NET40 || NET45

        private const string winformsPostfix = ", Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
#else
#error .NET version is not set or not supported!
#endif

        internal const string BinSerializedObjectMimeType = "application/x-microsoft.net.object.binary.base64";
        //internal const string SoapSerializedObjectMimeType = "application/x-microsoft.net.object.soap.base64";
        internal const string DefaultSerializedObjectMimeType = BinSerializedObjectMimeType;
        internal const string ByteArraySerializedObjectMimeType = "application/x-microsoft.net.object.bytearray.base64";
        internal const string KGySoftSerializedObjectMimeType = "text/kgysoft.net/object.binary.base64";
        internal const string ResMimeType = "text/microsoft-resx";
        internal static readonly string Version = "2.0";

        internal static readonly string[] BinSerializedMimeTypes = { BinSerializedObjectMimeType, beta2CompatSerializedObjectMimeType, compatBinSerializedObjectMimeType };

        /// <summary>
        /// Gets assembly info for the corresponding type. If the delegate is provided it is used to get this information.
        /// </summary>
        internal static string GetAssemblyQualifiedName(Type type, Func<Type, string> typeNameConverter, bool compatibleFormat)
        {
            if (type == null)
                return null;

            string result = null;

            if (typeNameConverter != null)
                result = typeNameConverter.Invoke(type);

            if (String.IsNullOrEmpty(result))
            {
                if (compatibleFormat)
                {
                    if (type == typeof(ResXFileRef))
                        result = ResXFileRefNameWinForms + winformsPostfix;
                    else if (type == Reflector.ByteArrayType)
                        return Reflector.ByteArrayType.AssemblyQualifiedName; // byte[]: The System.ResXReader recognizes it only with aqn
                    else if (type == typeof(ResXNullRef))
                        result = ResXNullRefNameWinForms + winformsPostfix;
                    else if (type == typeof(ResXResourceReader))
                        result = ResXResourceReaderNameWinForms + winformsPostfix;
                    else if (type == typeof(ResXResourceWriter))
                        result = ResXResourceWriterNameWinForms + winformsPostfix;
                }

                if (result == null)
                    result = type.GetTypeName(true);
            }

            return result;
        }

        internal static string ToBase64(byte[] value)
        {
            const int lineLength = 100;
            string valueData = value.ToBase64String(lineLength, 6);
            if (valueData.Length > lineLength)
                valueData = Environment.NewLine + valueData + Environment.NewLine + "    ";
            return valueData;
        }

        /// <summary>
        /// Creates an XmlException with already localized position message and still set position.
        /// </summary>
        /// <param name="message">The already formatted and localized message.</param>
        /// <param name="line">The line to set.</param>
        /// <param name="pos">The position to set.</param>
        /// <returns></returns>
        internal static XmlException CreateXmlException(string message, int line, int pos, Exception innerException = null)
        {
            var result = new XmlException(message, innerException);
            Accessors.XmlException_lineNumber.Set(result, line);
            Accessors.XmlException_linePosition.Set(result, pos);
            return result;
        }
    }
}
