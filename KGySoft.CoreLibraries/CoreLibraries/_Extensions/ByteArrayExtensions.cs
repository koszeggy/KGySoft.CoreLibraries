#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ByteArrayExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="Array">byte[]</see> type.
    /// </summary>
    public static class ByteArrayExtensions
    {
        #region Extension Methods

        #region Hex

        /// <summary>
        /// Converts the byte array to string of hexadecimal values.
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <param name="separator">The separator to use between the hex numbers. If <see langword="null"/>&#160;or empty, the hex stream will be continuous. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>The string representation, in hex, of the contents of <paramref name="bytes"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentException"><paramref name="separator"/> contains hex digits</exception>
        public static string ToHexValuesString(this byte[] bytes, string separator = null)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), Res.ArgumentNull);
            bool useSeparator = !String.IsNullOrEmpty(separator);
            if (useSeparator && separator.Any(c => c >= '0' && c <= '9' || c >= 'A' && c <= 'F' || c >= 'a' && c <= 'f'))
                throw new ArgumentException(Res.ByteArrayExtensionsSeparatorInvalidHex, nameof(separator));

            StringBuilder result = new StringBuilder(bytes.Length * (2 + (separator ?? String.Empty).Length));
            for (int i = 0; i < bytes.Length; i++)
            {
                result.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));
                if (useSeparator && i < bytes.Length - 1)
                    result.Append(separator);
            }

            return result.ToString();
        }

        /// <summary>
        /// Converts the byte array to string of hexadecimal values.
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <param name="separator">The separator to use between the hex numbers. If <see langword="null"/>&#160;or empty, the hex stream will be continuous.</param>
        /// <param name="lineLength">Specifies the length of a line in the result not counting the indentation. When 0 or less, the result will not be wrapped to lines.</param>
        /// <param name="indentSize">Size of the indentation. If greater than zero, the new lines will be prefixed with as many <paramref name="indentChar"/> characters as this parameter specifies. This parameter is optional.
        /// <br/>Default value: <c>0</c></param>
        /// <param name="indentChar">The character to be used for the indentation. This parameter is optional.
        /// <br/>Default value: <c>' '</c> (space)</param>
        /// <param name="indentSingleLine">If set to <see langword="true"/>, then a single line result will be indented, too. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>The string representation, in hex, of the contents of <paramref name="bytes"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentException"><paramref name="separator"/> contains hex digits</exception>
        public static string ToHexValuesString(
            this byte[] bytes,
            string separator,
            int lineLength,
            int indentSize = 0,
            char indentChar = ' ',
            bool indentSingleLine = false)
        {
            string raw = ToHexValuesString(bytes, separator);

            // no separator: simple splitting at even points
            if (String.IsNullOrEmpty(separator))
            {
                if (lineLength > 0 && (lineLength & 1) == 1)
                {
                    lineLength--;
                    if (lineLength == 0)
                        lineLength = 2;
                }

                return Split(raw, lineLength, indentSize, indentChar, indentSingleLine);
            }

            return Wrap(raw, separator, lineLength, indentSize, indentChar, indentSingleLine);
        }

        #endregion

        #region Decimal

        /// <summary>
        /// Converts the byte array to string of decimal values.
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <param name="separator">The separator to use between the decimal numbers. This parameter is optional.
        /// <br/>Default value: <c>", "</c> (comma and space)</param>
        /// <returns>The string representation, in decimal, of the contents of <paramref name="bytes"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> or <paramref name="separator"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentException"><paramref name="separator"/> is empty or contains decimal digits</exception>
        public static string ToDecimalValuesString(this byte[] bytes, string separator = ", ")
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), Res.ArgumentNull);
            if (separator == null)
                throw new ArgumentNullException(nameof(separator), Res.ArgumentNull);

            if (separator.Length == 0 || separator.Any(c => c >= '0' && c <= '9'))
                throw new ArgumentException(Res.ByteArrayExtensionsSeparatorInvalidDec, nameof(separator));

            StringBuilder result = new StringBuilder(bytes.Length * (3 + separator.Length));
            for (int i = 0; i < bytes.Length; i++)
            {
                result.Append(bytes[i].ToString(CultureInfo.InvariantCulture));
                if (i < bytes.Length - 1)
                    result.Append(separator);
            }

            return result.ToString();
        }

        /// <summary>
        /// Converts the byte array to string of decimal values.
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <param name="separator">The separator to use between the decimal numbers.</param>
        /// <param name="lineLength">Specifies the length of a line in the result not counting the indentation. When 0 or less, the result will not be wrapped to lines.</param>
        /// <param name="indentSize">Size of the indentation. If greater than zero, the new lines will be prefixed with as many <paramref name="indentChar"/> characters as this parameter specifies. This parameter is optional.
        /// <br/>Default value: <c>0</c></param>
        /// <param name="indentChar">The character to be used for the indentation. This parameter is optional.
        /// <br/>Default value: <c>' '</c> (space)</param>
        /// <param name="indentSingleLine">If set to <see langword="true"/>, then a single line result will be indented, too. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>The string representation, in decimal, of the contents of <paramref name="bytes"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> or <paramref name="separator"/> is <see langword="null"/></exception>
        /// <exception cref="ArgumentException"><paramref name="separator"/> is empty or contains decimal digits</exception>
        public static string ToDecimalValuesString(
            this byte[] bytes,
            string separator,
            int lineLength,
            int indentSize = 0,
            char indentChar = ' ',
            bool indentSingleLine = false)
        {
            string raw = ToDecimalValuesString(bytes, separator);
            return Wrap(raw, separator, lineLength, indentSize, indentChar, indentSingleLine);
        }

        #endregion

        #region Base64

        /// <summary>
        /// Converts the given <paramref name="bytes"/> into a Base64 encoded string.
        /// </summary>
        /// <param name="bytes">The bytes to convert.</param>
        /// <param name="lineLength">Specifies the length of a line in the result not counting the indentation. When 0 or less, the result will not be wrapped to lines. This parameter is optional.
        /// <br/>Default value: <c>0</c>.</param>
        /// <param name="indentSize">Size of the indentation. If greater than zero, the new lines will be prefixed with as many <paramref name="indentChar"/> characters as this parameter specifies. This parameter is optional.
        /// <br/>Default value: <c>0</c>.</param>
        /// <param name="indentChar">The character to be used for the indentation. This parameter is optional.
        /// <br/>Default value: <c>' '</c> (space)</param>
        /// <param name="indentSingleLine">If set to <see langword="true"/>, then a single line result will be indented, too. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>The string representation, in base 64, of the contents of <paramref name="bytes"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is <see langword="null"/></exception>
        public static string ToBase64String(this byte[] bytes, int lineLength = 0, int indentSize = 0, char indentChar = ' ', bool indentSingleLine = false)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), Res.ArgumentNull);

            string raw = Convert.ToBase64String(bytes);
            return Split(raw, lineLength, indentSize, indentChar, indentSingleLine);
        }

        #endregion

        #region Compression

        /// <summary>
        /// Compresses the provided <paramref name="bytes"/> and returns the compressed data.
        /// Compressed data can be decompressed by <see cref="Decompress">Decompress</see> method.
        /// </summary>
        /// <param name="bytes">The bytes to compress.</param>
        /// <returns>Compressed data. It is not guaranteed that compressed data is shorter than original one.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "DeflateStream is created with leaveOpen = true")]
        public static byte[] Compress(this byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), Res.ArgumentNull);

            using (MemoryStream encStream = new MemoryStream())
            {
                using (DeflateStream compStream = new DeflateStream(encStream, CompressionMode.Compress, true))
                {
                    compStream.Write(bytes, 0, bytes.Length);
                    // stream must be closed here, otherwise, data would loss in encStream (simple Flush does not help!)
                }

                return encStream.ToArray();
            }
        }

        /// <summary>
        /// Decompresses the provided <paramref name="bytes"/> that was compressed by <see cref="Compress">Compress</see> method.
        /// </summary>
        /// <param name="bytes">The bytes to decompress.</param>
        /// <returns>Decompressed data. It is not guaranteed that compressed data is shorter than original one.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "DeflateStream is created with leaveOpen = true")]
        public static byte[] Decompress(this byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), Res.ArgumentNull);

            using (MemoryStream result = new MemoryStream(), encStream = new MemoryStream(bytes))
            {
                using (DeflateStream compStream = new DeflateStream(encStream, CompressionMode.Decompress, true))
                {
                    int b;
                    while ((b = compStream.ReadByte()) != -1)
                        result.WriteByte((byte)b);
                }

                return result.ToArray();
            }
        }

        #endregion

        #region Encryption

        /// <summary>
        /// Encrypts a byte array by the provided symmetric <paramref name="algorithm"/>, <paramref name="key"/> and initialization vector.
        /// </summary>
        /// <param name="bytes">Source bytes to encrypt.</param>
        /// <param name="algorithm">A <see cref="SymmetricAlgorithm"/> instance to be used for encryption.</param>
        /// <param name="key">Key to be used for encryption.</param>
        /// <param name="iv">Initialization vector to be used for encryption.</param>
        /// <returns>The encrypted result of <paramref name="bytes"/>.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "MemoryStream can be disposed multiple times safely (CryptoStream constructor with leaveOpen available from .NET 4.7.2)")]
        public static byte[] Encrypt(this byte[] bytes, SymmetricAlgorithm algorithm, byte[] key, byte[] iv)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), Res.ArgumentNull);
            if (algorithm == null)
                throw new ArgumentNullException(nameof(algorithm), Res.ArgumentNull);
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.ArgumentNull);
            if (iv == null)
                throw new ArgumentNullException(nameof(iv), Res.ArgumentNull);

            algorithm.Key = key;
            algorithm.IV = iv;

            ICryptoTransform encryptor = algorithm.CreateEncryptor();
            using (MemoryStream encryptedResult = new MemoryStream())
            {
                using (CryptoStream encryptStream = new CryptoStream(encryptedResult, encryptor, CryptoStreamMode.Write))
                    encryptStream.Write(bytes, 0, bytes.Length);

                return encryptedResult.ToArray();
            }
        }

        /// <summary>
        /// Encrypts a byte array by the provided symmetric <paramref name="algorithm"/>, <paramref name="password"/> and <paramref name="salt"/>.
        /// </summary>
        /// <param name="bytes">Source bytes to encrypt.</param>
        /// <param name="algorithm">A <see cref="SymmetricAlgorithm"/> instance to be used for encryption.</param>
        /// <param name="password">Password of encryption.</param>
        /// <param name="salt">A salt value to be used for encryption. If <see langword="null"/>&#160;or is empty, a default salt will be used.</param>
        /// <returns>The encrypted result of <paramref name="bytes"/>.</returns>
        public static byte[] Encrypt(this byte[] bytes, SymmetricAlgorithm algorithm, string password, string salt)
        {
            if (password == null)
                throw new ArgumentNullException(nameof(password), Res.ArgumentNull);
            if (algorithm == null)
                throw new ArgumentNullException(nameof(algorithm), Res.ArgumentNull);

            CheckSalt(ref salt);
            using (var passwordKey = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt)))
                return Encrypt(bytes, algorithm, passwordKey.GetBytes(algorithm.KeySize >> 3), passwordKey.GetBytes(algorithm.BlockSize >> 3));
        }

        /// <summary>
        /// Encrypts a byte array by the <see cref="RijndaelManaged"/> algorithm using the provided <paramref name="password"/> and <paramref name="salt"/>.
        /// </summary>
        /// <param name="bytes">Source bytes to encrypt.</param>
        /// <param name="password">Password of encryption.</param>
        /// <param name="salt">A salt value to be used for encryption. If <see langword="null"/>&#160;or is empty, a default salt will be used.</param>
        /// <returns>The encrypted result of <paramref name="bytes"/>.</returns>
        public static byte[] Encrypt(this byte[] bytes, string password, string salt)
        {
            using (SymmetricAlgorithm alg = new RijndaelManaged())
                return Encrypt(bytes, alg, password, salt);
        }

        /// <summary>
        /// Encrypts a byte array by the provided symmetric <paramref name="algorithm"/>, using a randomly generated key and initialization vector, which are
        /// returned in <paramref name="key"/> and <paramref name="iv"/> parameters, respectively.
        /// </summary>
        /// <param name="bytes">Source bytes to encrypt.</param>
        /// <param name="algorithm">A <see cref="SymmetricAlgorithm"/> instance to be used for encryption.</param>
        /// <param name="key">Returns the automatically generated key used for encryption.</param>
        /// <param name="iv">Returns the automatically generated initialization vector used for encryption.</param>
        /// <returns>The encrypted result of <paramref name="bytes"/>.</returns>
        [CLSCompliant(false)]
        public static byte[] Encrypt(this byte[] bytes, SymmetricAlgorithm algorithm, out byte[] key, out byte[] iv)
        {
            if (algorithm == null)
                throw new ArgumentNullException(nameof(algorithm), Res.ArgumentNull);

            algorithm.GenerateKey();
            algorithm.GenerateIV();
            key = algorithm.Key;
            iv = algorithm.IV;
            return Encrypt(bytes, algorithm, key, iv);
        }

        /// <summary>
        /// Encrypts a byte array by the <see cref="RijndaelManaged"/> algorithm using a randomly generated key and initialization vector, which are
        /// returned in <paramref name="key"/> and <paramref name="iv"/> parameters, respectively.
        /// </summary>
        /// <param name="bytes">Source bytes to encrypt.</param>
        /// <param name="key">Returns the automatically generated key used for encryption.</param>
        /// <param name="iv">Returns the automatically generated initialization vector used for encryption.</param>
        /// <returns>The encrypted result of <paramref name="bytes"/>.</returns>
        public static byte[] Encrypt(this byte[] bytes, out byte[] key, out byte[] iv)
        {
            using (SymmetricAlgorithm alg = new RijndaelManaged())
                return Encrypt(bytes, alg, out key, out iv);
        }

        /// <summary>
        /// Decrypts a byte array by the provided symmetric <paramref name="algorithm"/>, <paramref name="key"/> and initialization vector.
        /// </summary>
        /// <param name="bytes">Source bytes to decrypt.</param>
        /// <param name="algorithm">A <see cref="SymmetricAlgorithm"/> instance to use for decryption.</param>
        /// <param name="key">Key of decryption.</param>
        /// <param name="iv">The initialization vector to be used for decryption.</param>
        /// <returns>The decrypted result of <paramref name="bytes"/>.</returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "By this constructor CryptoStream does not leave the inner stream open")]
        public static byte[] Decrypt(this byte[] bytes, SymmetricAlgorithm algorithm, byte[] key, byte[] iv)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes), Res.ArgumentNull);
            if (algorithm == null)
                throw new ArgumentNullException(nameof(algorithm), Res.ArgumentNull);
            if (key == null)
                throw new ArgumentNullException(nameof(key), Res.ArgumentNull);
            if (iv == null)
                throw new ArgumentNullException(nameof(iv), Res.ArgumentNull);

            algorithm.Key = key;
            algorithm.IV = iv;

            ICryptoTransform decryptor = algorithm.CreateDecryptor();
            using (CryptoStream encryptStream = new CryptoStream(new MemoryStream(bytes), decryptor, CryptoStreamMode.Read))
            {
                // result is never longer than source
                byte[] decryptedResult = new byte[bytes.Length];
                int readBytes = encryptStream.Read(decryptedResult, 0, decryptedResult.Length);

                // if result is shorter, trimming the array
                if (readBytes != decryptedResult.Length)
                    Array.Resize(ref decryptedResult, readBytes);

                return decryptedResult;
            }
        }

        /// <summary>
        /// Decrypts a byte array by the <see cref="RijndaelManaged"/> algorithm using the provided <paramref name="key"/> and initialization vector.
        /// </summary>
        /// <param name="bytes">Source bytes to decrypt.</param>
        /// <param name="key">Key of decryption.</param>
        /// <param name="iv">The initialization vector to be used for decryption.</param>
        /// <returns>The decrypted result of <paramref name="bytes"/>.</returns>
        public static byte[] Decrypt(this byte[] bytes, byte[] key, byte[] iv)
        {
            using (SymmetricAlgorithm alg = new RijndaelManaged())
                return Decrypt(bytes, alg, key, iv);
        }

        /// <summary>
        /// Decrypts a byte array by the provided symmetric <paramref name="algorithm"/>, <paramref name="password"/> and <paramref name="salt"/>.
        /// </summary>
        /// <param name="bytes">Source bytes to decrypt.</param>
        /// <param name="algorithm">A <see cref="SymmetricAlgorithm"/> instance to use for decryption.</param>
        /// <param name="password">Password of decryption.</param>
        /// <param name="salt">A salt value to be used for decryption. If <see langword="null"/>&#160;or is empty, a default salt will be used.</param>
        /// <returns>The decrypted result of <paramref name="bytes"/>.</returns>
        public static byte[] Decrypt(this byte[] bytes, SymmetricAlgorithm algorithm, string password, string salt)
        {
            if (algorithm == null)
                throw new ArgumentNullException(nameof(algorithm), Res.ArgumentNull);
            if (password == null)
                throw new ArgumentNullException(nameof(password), Res.ArgumentNull);

            CheckSalt(ref salt);
            using (var passwordKey = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt)))
                return Decrypt(bytes, algorithm, passwordKey.GetBytes(algorithm.KeySize >> 3), passwordKey.GetBytes(algorithm.BlockSize >> 3));
        }

        /// <summary>
        /// Decrypts a byte array by the <see cref="RijndaelManaged"/> algorithm using the provided <paramref name="password"/> and <paramref name="salt"/>.
        /// </summary>
        /// <param name="bytes">Source bytes to decrypt.</param>
        /// <param name="password">Password of decryption.</param>
        /// <param name="salt">A salt value to be used for decryption. If <see langword="null"/>&#160;or is empty, a default salt will be used.</param>
        /// <returns>The decrypted result of <paramref name="bytes"/>.</returns>
        public static byte[] Decrypt(this byte[] bytes, string password, string salt)
        {
            using (SymmetricAlgorithm alg = new RijndaelManaged())
                return Decrypt(bytes, alg, password, salt);
        }

        #endregion

        #endregion

        #region Private methods

        private static void CheckSalt(ref string salt)
        {
            if (String.IsNullOrEmpty(salt))
            {
                salt = "ABCDEFGH";
                return;
            }
            if (salt.Length < 8)
            {
                salt = salt.Repeat(Convert.ToInt32(Math.Ceiling(8d / salt.Length)));
            }
        }

        private static string Split(string text, int lineLength, int indentSize, char indentChar, bool indentSingleLine)
        {
            if (lineLength <= 0 || text.Length <= lineLength)
                return indentSingleLine && indentSize > 0 ? new String(indentChar, indentSize) + text : text;

            string indent = indentSize > 0 ? new String(indentChar, indentSize) : String.Empty;
            StringBuilder result =
                new StringBuilder(text.Length + (indentSize + Environment.NewLine.Length) * (text.Length / lineLength + 1));
            int i;
            for (i = 0; i < text.Length - lineLength; i += lineLength)
            {
                result.Append(indent);
                result.Append(text, i, lineLength);
                result.AppendLine();
            }

            result.Append(indent);
            result.Append(text, i, text.Length - i);
            return result.ToString();
        }

        private static string Wrap(string text, string separator, int lineLength, int indentSize, char indentChar, bool indentSingleLine)
        {
            if (lineLength <= 0 || text.Length <= lineLength)
                return indentSingleLine && indentSize > 0 ? new String(indentChar, indentSize) + text : text;

            string indent = indentSize > 0 ? new String(indentChar, indentSize) : String.Empty;
            if (lineLength < 0)
                lineLength = 0;
            if (indentSize < 0)
                indentSize = 0;

            StringBuilder result = new StringBuilder(text.Length + (indentSize + Environment.NewLine.Length) * (text.Length / lineLength + 1));
            int pos;
            int nextSep;
            int currLineLen = 0;
            bool firstLine = true;
            string fragment;
            for (pos = 0; (nextSep = text.IndexOf(separator, pos, StringComparison.Ordinal)) > 0; pos = nextSep + separator.Length)
            {
                fragment = text.Substring(pos, nextSep - pos + separator.Length);

                // wrapping is needed
                if (currLineLen + fragment.Length > lineLength)
                {
                    // the first fragment of the line exceeds the line length: dumping, and then wrapping.
                    if (currLineLen == 0)
                    {
                        result.Append(indent);
                        result.AppendLine(fragment);
                        firstLine = false;
                        continue;
                    }

                    // with the current fragment the line would be too long: wrapping, then dumping the fragment into next line
                    if (firstLine)
                    {
                        result.Insert(0, indent);
                        firstLine = false;
                    }

                    result.AppendLine();
                    result.Append(indent);
                    result.Append(fragment);
                    currLineLen = fragment.Length;
                    continue;
                }

                // no wrapping is needed
                result.Append(fragment);
                currLineLen += fragment.Length;
            }

            // processing the last fragment
            fragment = text.Substring(pos);

            // wrapping is needed
            if (currLineLen + fragment.Length > lineLength)
            {
                // the last fragment exceeds the line length alone: dumping but no need to add a last new line
                if (currLineLen == 0)
                {
                    // this is the only fragment
                    if (firstLine)
                        return indentSingleLine && indentSize > 0 ? indent + text : text;

                    result.Append(indent);
                    result.Append(fragment);
                    return result.ToString();
                }

                // with the last fragment the line would be too long: wrapping, then dumping the fragment into next line
                if (firstLine)
                    result.Insert(0, indent);

                result.AppendLine();
                result.Append(indent);
            }

            // adding last fragment and returning result
            if (currLineLen == 0)
                result.Append(indent);

            result.Append(fragment);
            return result.ToString();
        }

        #endregion
    }
}
