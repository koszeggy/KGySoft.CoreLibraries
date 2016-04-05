using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;

using KGySoft.Libraries.Reflection;

namespace KGySoft.Libraries.Resources
{
    /// <summary>
    /// Represents a link to an external resource.
    /// </summary>
    // TODO: doc: incompatibility: public ctor is strongly typed
    // new feature: (try)parse - todo: doc
    [TypeConverter(typeof(Converter))]
    public class ResXFileRef
    {
        private readonly string fileName;
        private readonly string typeName;
        private readonly string encoding;

        [NonSerialized]
        private Encoding textFileEncoding;

        ///// <summary>
        ///// Creates a new instance of the <see cref="T:System.Resources.ResXFileRef"/> class that references the specified file.
        ///// </summary>
        ///// <param name="fileName">The file to reference. </param><param name="typeName">The type of the resource that is referenced. </param><exception cref="T:System.ArgumentNullException"><paramref name="fileName"/> or <paramref name="typeName "/>is null.</exception>
        ///// <devdoc>
        /////     Creates a new ResXFileRef that points to the specified file.
        /////     The type refered to by typeName must support a constructor
        /////     that accepts a System.IO.Stream as a parameter.
        ///// </devdoc>
        //private ResXFileRef(string fileName, string typeName)
        //{
        //    //if (fileName == null)
        //    //{
        //    //    throw (new ArgumentNullException("fileName"));
        //    //}
        //    //if (typeName == null)
        //    //{
        //    //    throw (new ArgumentNullException("typeName"));
        //    //}
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Resources.ResXFileRef"/> class that references the specified file.
        /// </summary>
        /// <param name="fileName">The file to reference. </param>
        /// <param name="type">The type of the resource that is referenced. Should be either <see cref="string"/>, array of <see cref="byte"/>, <see cref="MemoryStream"/> or a type, which has a constructor with one <see cref="Stream"/> parameter.</param>
        /// <param name="textFileEncoding">The encoding used in the referenced file. Used if <paramref name="type"/> is <see cref="string"/>.</param>
        /// <devdoc>
        ///     Creates a new ResXFileRef that points to the specified file.
        ///     The type refered to by typeName must support a constructor
        ///     that accepts a System.IO.Stream as a parameter.
        /// </devdoc>
        public ResXFileRef(string fileName, Type type, Encoding textFileEncoding = null)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName), Res.Get(Res.ArgumentNull));
            if (type == null)
                throw (new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull)));

            this.fileName = fileName;
            typeName = type.Assembly == Reflector.mscorlibAssembly ? type.FullName : type.AssemblyQualifiedName;
            if (textFileEncoding != null)
            {
                this.textFileEncoding = textFileEncoding;
                encoding = textFileEncoding.WebName;
            }
        }

        internal ResXFileRef(string fileName, string typeName, string encoding)
        {
            this.fileName = fileName;
            this.typeName = typeName;
            this.encoding = encoding;
        }

        internal ResXFileRef Clone()
        {
            return new ResXFileRef(fileName, typeName, encoding);
        }

        public static ResXFileRef Parse(string s)
        {
            if (s == null)
                throw new ArgumentNullException(nameof(s), Res.ArgumentNull);

            ResXFileRef result;
            if (TryParse(s, out result))
                return result;

            throw new ArgumentException(Res.ArgumentInvalidString, nameof(s));
        }

        public static bool TryParse(string s, out ResXFileRef result)
        {
            string[] fileRefDetails = Converter.ParseResXFileRefString(s);
            if (fileRefDetails == null || fileRefDetails.Length < 2 || fileRefDetails.Length > 3)
            {
                result = null;
                return false;
            }

            result = new ResXFileRef(fileRefDetails[0], fileRefDetails[1], fileRefDetails.Length > 2 ? fileRefDetails[2] : null);
            return true;
        }

        /// <summary>
        /// Gets the file name specified in the current <see cref="ResXFileRef(string,string)"/> constructor.
        /// </summary>
        /// <returns>
        /// The name of the referenced file.
        /// </returns>
        public string FileName
        {
            get
            {
                return fileName;
            }
        }

        /// <summary>
        /// Gets the type name specified in the current <see cref="ResXFileRef(string,string)"/> constructor.
        /// </summary>
        /// <returns>
        /// The type name of the resource that is referenced.
        /// </returns>
        public string TypeName
        {
            get { return typeName; }
        }

        /// <summary>
        /// Gets the encoding specified in the current <see cref="ResXFileRef(string,string)"/> constructor.
        /// </summary>
        /// <returns>
        /// The encoding used in the referenced file.
        /// </returns>
        public Encoding TextFileEncoding
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

        internal string EncodingName
        {
            get { return encoding; }
        }

        // TODO: delete
        ///// <include file='doc\ResXFileRef.uex' path='docs/doc[@for="ResXFileRef.PathDifference"]/*' />
        ///// <devdoc>
        /////    path1+result = path2
        /////   A string which is the relative path difference between path1 and
        /////  path2 such that if path1 and the calculated difference are used
        /////  as arguments to Combine(), path2 is returned
        ///// </devdoc>
        //private static string PathDifference(string path1, string path2, bool compareCase)
        //{
        //    int i;
        //    int si = -1;

        //    for (i = 0; (i < path1.Length) && (i < path2.Length); ++i)
        //    {
        //        if ((path1[i] != path2[i]) && (compareCase || (Char.ToLower(path1[i], CultureInfo.InvariantCulture) != Char.ToLower(path2[i], CultureInfo.InvariantCulture))))
        //        {
        //            break;

        //        }
        //        else if (path1[i] == Path.DirectorySeparatorChar)
        //        {
        //            si = i;
        //        }
        //    }

        //    if (i == 0)
        //    {
        //        return path2;
        //    }
        //    if ((i == path1.Length) && (i == path2.Length))
        //    {
        //        return String.Empty;
        //    }

        //    StringBuilder relPath = new StringBuilder();

        //    for (; i < path1.Length; ++i)
        //    {
        //        if (path1[i] == Path.DirectorySeparatorChar)
        //        {
        //            relPath.Append(".." + Path.DirectorySeparatorChar);
        //        }
        //    }
        //    return relPath.ToString() + path2.Substring(si + 1);
        //}


        //internal void MakeFilePathRelative(string basePath)
        //{
        //    if (string.IsNullOrEmpty(basePath))
        //        return;

        //    fileName = PathDifference(basePath, fileName, false);
        //}

        /// <summary>
        /// Gets the text representation of the current <see cref="ResXFileRef"/> object.
        /// </summary>
        /// <returns>
        /// A string that consists of the concatenated text representations of the parameters specified in the current <see cref="ResXFileRef(string, string, string)"/> constructor.
        /// </returns>
        public override string ToString()
        {
            return ToString(fileName, typeName, encoding);
        }

        internal static string ToString(string fileName, string typeName, string encoding)
        {
            string result = "";

            if (fileName.IndexOf(';') != -1 || fileName.IndexOf('"') != -1)
            {
                result += ("\"" + fileName + "\";");
            }
            else
            {
                result += (fileName + ";");
            }
            result += typeName;
            if (encoding != null)
            {
                result += (";" + encoding);
            }
            return result;
        }

        internal object GetValue(Type objectType, string basePath)
        {
            return Converter.ConvertFrom(ToString(), objectType, basePath);
        }

        private class Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == Reflector.StringType;
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                return destinationType == Reflector.StringType;
            }

            public override Object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                return destinationType == Reflector.StringType ? value.ToString() : null;
            }

            internal static string[] ParseResXFileRefString(string stringValue)
            {
                string[] result = null;
                if (stringValue != null)
                {
                    stringValue = stringValue.Trim();
                    string fileName;
                    string remainingString;
                    if (stringValue.Length > 0 && stringValue[0] == '"')
                    {
                        int lastIndexOfQuote = stringValue.LastIndexOf('"');
                        if (lastIndexOfQuote - 1 < 0)
                            return null;
                        fileName = stringValue.Substring(1, lastIndexOfQuote - 1); // remove the quotes in" ..... " 
                        if (lastIndexOfQuote + 2 > stringValue.Length)
                            return null;
                        remainingString = stringValue.Substring(lastIndexOfQuote + 2);
                    }
                    else
                    {
                        int nextSemiColumn = stringValue.IndexOf(';');
                        if (nextSemiColumn == -1)
                            return null;
                        fileName = stringValue.Substring(0, nextSemiColumn);
                        if (nextSemiColumn + 1 > stringValue.Length)
                            return null;
                        remainingString = stringValue.Substring(nextSemiColumn + 1);
                    }
                    string[] parts = remainingString.Split(new char[] { ';' });
                    if (parts.Length > 1)
                    {
                        result = new string[] { fileName, parts[0], parts[1] };
                    }
                    else if (parts.Length > 0)
                    {
                        result = new string[] { fileName, parts[0] };
                    }
                    else
                    {
                        result = new string[] { fileName };
                    }
                }
                return result;
            }

            public override Object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                string stringValue = value as string;
                return stringValue != null ? ConvertFrom(stringValue, null, null) : null;
            }

            internal static object ConvertFrom(string stringValue, Type objectType, string basePath)
            {
                string[] parts = ParseResXFileRefString(stringValue);
                if (stringValue == null)
                    throw new ArgumentException(Res.ArgumentInvalidString, nameof(stringValue));
                string fileName = parts[0];
                if (!String.IsNullOrEmpty(basePath) && !Path.IsPathRooted(fileName))
                    fileName = Path.Combine(basePath, fileName);

                Type toCreate = objectType ?? Type.GetType(parts[1], true);

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
                    throw new FileNotFoundException(Res.Get(Res.ResXFileRefFileNotFound, fileName), fileName);
                using (FileStream s = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    //Debug.Assert(s != null, "Couldn't open " + fileName);
                    buffer = new byte[s.Length];
                    s.Read(buffer, 0, (int)s.Length);
                }

                if (toCreate == Reflector.ByteArrayType)
                    return buffer;

                MemoryStream memStream = new MemoryStream(buffer);
                if (toCreate == typeof(MemoryStream))
                    return memStream;

                return Reflector.Construct(toCreate, ReflectionWays.Auto, memStream);
            }
        }

        internal static ResXFileRef InitFromWinForms(object other)
        {
            return new ResXFileRef(
                Accessors.ResXFileRef_fileName_Get(other),
                Accessors.ResXFileRef_typeName_Get(other),
                Accessors.ResXFileRef_textFileEncoding_Get(other)?.WebName);
        }
    }
}
