#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXCommon.cs
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

using KGySoft.CoreLibraries;
using KGySoft.IO;
using KGySoft.Reflection;

#endregion

#region Suppressions

#if NETFRAMEWORK
// ReSharper disable ConstantNullCoalescingCondition - ToString CAN be null
#endif

#endregion

namespace KGySoft.Resources
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
        internal const string ResXFileRefNameKGySoft = "KGySoft.Resources.ResXFileRef, KGySoft.CoreLibraries";
        internal const string ResXNullRefNameKGySoft = "KGySoft.Resources.ResXNullRef, KGySoft.CoreLibraries";
        internal const string ResXResourceReaderNameWinForms = "System.Resources.ResXResourceReader, System.Windows.Forms";
        internal const string ResXResourceWriterNameWinForms = "System.Resources.ResXResourceWriter, System.Windows.Forms";

        internal const string BinSerializedObjectMimeType = "application/x-microsoft.net.object.binary.base64";
        internal const string DefaultSerializedObjectMimeType = BinSerializedObjectMimeType;
        internal const string ByteArraySerializedObjectMimeType = "application/x-microsoft.net.object.bytearray.base64";
        internal const string KGySoftSerializedObjectMimeType = "text/kgysoft.net/object.binary.base64";
        internal const string ResMimeType = "text/microsoft-resx";

        internal const string Version = "2.0";

#if NET35
        internal const string WinFormsPostfix = ", Version=2.0.3500.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
#else
        internal const string WinFormsPostfix = ", Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
#endif

        #endregion

        #region Private Constants

        private const string beta2CompatSerializedObjectMimeType = "text/microsoft-urt/psuedoml-serialized/base64";
        private const string compatBinSerializedObjectMimeType = "text/microsoft-urt/binary-serialized/base64";
        private const string soapSerializedObjectMimeType = "application/x-microsoft.net.object.soap.base64";
        private const string compatSoapSerializedObjectMimeType = "text/microsoft-urt/soap-serialized/base64";

        private const string soapFormatterTypeName = "System.Runtime.Serialization.Formatters.Soap.SoapFormatter, System.Runtime.Serialization.Formatters.Soap";

        #endregion

        #endregion

        #region Fields

        #region Internal Fields

        internal static readonly string[] BinSerializedMimeTypes = { BinSerializedObjectMimeType, beta2CompatSerializedObjectMimeType, compatBinSerializedObjectMimeType };
        internal static readonly string[] SoapSerializedMimeTypes = { soapSerializedObjectMimeType, compatSoapSerializedObjectMimeType };

        #endregion

        #region Private Fields

        private static IFormatter? soapFormatter;

        #endregion

        #endregion

        #region Methods

        #region Internal Methods

        /// <summary>
        /// Gets assembly info for the corresponding type. If the delegate is provided it is used to get this information.
        /// </summary>
        [return:NotNullIfNotNull("type")]
        internal static string GetAssemblyQualifiedName(Type? type, Func<Type, string?>? typeNameConverter, bool compatibleFormat)
        {
            #region Local Methods

            static AssemblyName GetAssemblyName(Type t)
            {
                string? legacyName = AssemblyResolver.GetForwardedAssemblyName(t, false);
                return legacyName != null ? new AssemblyName(legacyName) : t.Assembly.GetName();
            }

            #endregion

            if (type == null)
                return null!;

            string? result = null;

            if (typeNameConverter != null)
                result = typeNameConverter.Invoke(type);

            if (String.IsNullOrEmpty(result))
            {
                if (compatibleFormat)
                {
                    if (type == typeof(ResXFileRef))
                        result = ResXFileRefNameWinForms + WinFormsPostfix;
                    else if (type == Reflector.ByteArrayType)
                        return Reflector.ByteArrayType.GetName(TypeNameKind.ForcedAssemblyQualifiedName, GetAssemblyName, null); // byte[]: The System.ResXReader recognizes it only with AssemblyQualifiedName
                    else if (type == typeof(ResXNullRef))
                        result = ResXNullRefNameWinForms + WinFormsPostfix;
                    else if (type == typeof(ResXResourceReader))
                        result = ResXResourceReaderNameWinForms + WinFormsPostfix;
                    else if (type == typeof(ResXResourceWriter))
                        result = ResXResourceWriterNameWinForms + WinFormsPostfix;
                }

                result ??= type.GetName(TypeNameKind.AssemblyQualifiedName, GetAssemblyName, null);
            }

            return result!;
        }

        internal static string ToBase64(byte[] value)
        {
            const int lineLength = 100;
            string valueData = value.ToBase64String(lineLength, 6);
            if (valueData.Length > lineLength)
                valueData = Environment.NewLine + valueData + Environment.NewLine + "    ";
            return valueData;
        }

        internal static XmlException CreateXmlException(string message, int line, int pos, Exception? innerException = null)
            => new XmlException(message, innerException, line, pos);

        internal static IFormatter? GetSoapFormatter()
        {
            if (soapFormatter == null)
            {
                try
                {
                    Type? type = Reflector.ResolveType(soapFormatterTypeName);

                    // no Reflector or Accessor is needed because this is a static instance so will be invoked once. In this case Reflector would be slower for that single run.
                    if (type != null)
                        soapFormatter = (IFormatter)Activator.CreateInstance(type)!;
                }
                catch (ReflectionException)
                {
                    return null;
                }
            }

            return soapFormatter;
        }

        internal static MemoryStream? ToMemoryStream(string name, object? value, bool safeMode)
        {
            if (value == null)
                return null;

            if (value is ResXDataNode node)
                return ToStreamSafe(node);

            if (CanGetAsMemoryStream(value, safeMode, out MemoryStream? result))
                return result;

            Throw.InvalidOperationException(Res.ResourcesNonStreamResourceWithType(name, value.GetType()));
            return default;
        }

        #endregion

        #region Private Methods

        private static bool CanGetAsMemoryStream(object value, bool safeMode, [MaybeNullWhen(false)]out MemoryStream result)
        {
            // .NET issue: is operator would capture sbyte[], too (https://stackoverflow.com/q/33896316/5114784)
            if (value.GetType() == Reflector.ByteArrayType)
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                result = new MemoryStream((byte[])value, false);
                return true;
            }

            if (value is MemoryStream ms)
            {
                result = ms.GetType() == typeof(MemoryStream)
                    ? new MemoryStream(ms.InternalGetBuffer() ?? ms.ToArray(), false)
                    : ms;
                return true;
            }

            if (!safeMode)
            {
                result = null;
                return false;
            }

            if (value is string s)
            {
                result = new StringStream(s);
                return true;
            }

            result = new StringStream(value.ToString() ?? String.Empty);
            return true;
        }

        private static MemoryStream? ToStreamSafe(ResXDataNode node)
        {
            object? value = node.ValueInternal;

            // not deserialized yet
            if (value == null)
            {
                string? typeName = node.FileRef?.TypeName ?? node.AssemblyQualifiedName;
                if (typeName != null)
                {
                    string fullName = TypeResolver.StripName(typeName, false);
                    if (fullName == TypeResolver.StringTypeFullName
                        || fullName == Reflector.ByteArrayType.GetName(TypeNameKind.FullName)
                        || fullName == typeof(MemoryStream).GetName(TypeNameKind.FullName))
                    {
                        value = node.GetValueSafe();
                    }
                }
            }

            if (value is ResXNullRef)
                return null;

            if (value != null && CanGetAsMemoryStream(value, false, out MemoryStream? result))
                return result;

            // not a supported type or type cannot be determined: by raw value
            return new StringStream(node.ValueData ?? node.GetDataNodeInfo(null, null).ValueData!);
        }

        #endregion

        #endregion
    }
}
