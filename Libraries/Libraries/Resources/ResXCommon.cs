#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXCommon.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2017 - All Rights Reserved
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
using System.Runtime.Serialization;
using System.Xml;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Libraries.Resources
{
    internal static class ResXCommon
    {
        #region Constants

        #region Internal Constants

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

        internal const string XmlStr = "xml";
        internal const string SpaceStr = "space";
        internal const string PreserveStr = "preserve";

        internal const string ResXNullRefNameWinForms = "System.Resources.ResXNullRef, System.Windows.Forms";
        internal const string ResXFileRefNameWinForms = "System.Resources.ResXFileRef, System.Windows.Forms";
        internal const string ResXDataNodeNameWinForms = "System.Resources.ResXDataNode, System.Windows.Forms";
        internal const string ResXFileRefNameKGySoft = "KGySoft.Libraries.Resources.ResXFileRef, KGySoft.Libraries";
        internal const string ResXNullRefNameKGySoft = "KGySoft.Libraries.Resources.ResXNullRef, KGySoft.Libraries";
        internal const string ResXResourceReaderNameWinForms = "System.Resources.ResXResourceReader, System.Windows.Forms";
        internal const string ResXResourceWriterNameWinForms = "System.Resources.ResXResourceWriter, System.Windows.Forms";

        internal const string BinSerializedObjectMimeType = "application/x-microsoft.net.object.binary.base64";
        internal const string DefaultSerializedObjectMimeType = BinSerializedObjectMimeType;
        internal const string ByteArraySerializedObjectMimeType = "application/x-microsoft.net.object.bytearray.base64";
        internal const string KGySoftSerializedObjectMimeType = "text/kgysoft.net/object.binary.base64";
        internal const string ResMimeType = "text/microsoft-resx";

        internal const string Version = "2.0";

        #endregion

        #region Private Constants

        private const string beta2CompatSerializedObjectMimeType = "text/microsoft-urt/psuedoml-serialized/base64";
        private const string compatBinSerializedObjectMimeType = "text/microsoft-urt/binary-serialized/base64";
        private const string soapSerializedObjectMimeType = "application/x-microsoft.net.object.soap.base64";
        private const string compatSoapSerializedObjectMimeType = "text/microsoft-urt/soap-serialized/base64";

#if NET35
        private const string winformsPostfix = ", Version=2.0.3500.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
#elif NET40 || NET45

        private const string winformsPostfix = ", Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
#else
#error .NET version is not set or not supported!
#endif

        private const string soapFormatterTypeName = "System.Runtime.Serialization.Formatters.Soap.SoapFormatter, System.Runtime.Serialization.Formatters.Soap";

        #endregion

        #endregion

        #region Fields

        #region Internal Fields

        internal static readonly string[] BinSerializedMimeTypes = { BinSerializedObjectMimeType, beta2CompatSerializedObjectMimeType, compatBinSerializedObjectMimeType };
        internal static readonly string[] SoapSerializedMimeTypes = { soapSerializedObjectMimeType, compatSoapSerializedObjectMimeType };

        #endregion

        #region Private Fields

        private static IFormatter soapFormatter;

        #endregion

        #endregion

        #region Methods

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
        /// <param name="innerException">The original exception to wrap (if any).</param>
        /// <returns>A new <see cref="XmlException"/>.</returns>
        internal static XmlException CreateXmlException(string message, int line, int pos, Exception innerException = null)
        {
            var result = new XmlException(message, innerException);
            Accessors.XmlException_lineNumber.Set(result, line);
            Accessors.XmlException_linePosition.Set(result, pos);
            return result;
        }

        internal static IFormatter GetSoapFormatter()
        {
            if (soapFormatter == null)
            {
                try
                {
                    Type type = Reflector.ResolveType(soapFormatterTypeName, true, true);

                    // no Reflector or Accessor is needed because this is a static instance so will be invoked once. In this case Reflector would be slower for that single run.
                    if (type != null)
                        soapFormatter = (IFormatter)Activator.CreateInstance(type);
                }
                catch (ReflectionException)
                {
                    return null;
                }
            }

            return soapFormatter;
        }

        #endregion
    }
}
