#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ByteArrayExtensions.cs
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
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;

using KGySoft.Collections;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="Array">byte[]</see> type.
    /// </summary>
    public static class ByteArrayExtensions
    {
        #region Public Methods

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
        [SecuritySafeCritical]
        public static unsafe string ToHexValuesString(this byte[] bytes, string? separator = null)
        {
            if (bytes == null!)
                Throw.ArgumentNullException(Argument.bytes);
            bool useSeparator = !String.IsNullOrEmpty(separator);
            if (useSeparator)
            {
                // ReSharper disable once ForCanBeConvertedToForeach - it used to be an Any call but has been refactored due to performance
                for (int i = 0; i < separator!.Length; i++)
                {
                    char c = separator[i];
                    if (c >= '0' && c <= '9' || c >= 'A' && c <= 'F' || c >= 'a' && c <= 'f')
                        Throw.ArgumentException(Argument.separator, Res.ByteArrayExtensionsSeparatorInvalidHex);
                }
            }

            int bytesLength = bytes.Length;
            if (bytesLength == 0)
                return String.Empty;

            int len = (bytesLength << 1) + (useSeparator ? (bytesLength - 1) * separator!.Length : 0);
            string result = new String('\0', len);
            fixed (char* pResult = result)
            {
                var sb = new MutableStringBuilder(pResult, len);

                // ReSharper disable once ForCanBeConvertedToForeach - performance
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (useSeparator && sb.Length != 0)
                        sb.Append(separator!);
                    sb.AppendHex(bytes[i]);
                }
            }

            return result;
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
            string? separator,
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
                    lineLength -= 1;
                    if (lineLength == 0)
                        lineLength = 2;
                }

                return Split(raw, lineLength, indentSize, indentChar, indentSingleLine);
            }

            return Wrap(raw, separator!, lineLength, indentSize, indentChar, indentSingleLine);
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
        [SecuritySafeCritical]
        public static unsafe string ToDecimalValuesString(this byte[] bytes, string separator = ", ")
        {
            if (bytes == null!)
                Throw.ArgumentNullException(Argument.bytes);
            if (separator == null!)
                Throw.ArgumentNullException(Argument.separator);

            if (separator.Length == 0 || separator.Any(c => c >= '0' && c <= '9'))
                Throw.ArgumentException(Argument.separator, Res.ByteArrayExtensionsSeparatorInvalidDec);

            var buf = new ArraySection<char>(bytes.Length * (3 + separator.Length), false);
            try
            {
                fixed (char* pBuf = buf)
                {
                    var result = new MutableStringBuilder(pBuf, buf.Length);
                    
                    // ReSharper disable once ForCanBeConvertedToForeach - intended, performance
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        if (result.Length != 0)
                            result.Append(separator);
                        result.Append(bytes[i]);
                    }

                    return result.ToString();
                }
            }
            finally
            {
                buf.Release();
            }
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
            if (bytes == null!)
                Throw.ArgumentNullException(Argument.bytes);

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
        public static byte[] Compress(this byte[] bytes)
        {
            if (bytes == null!)
                Throw.ArgumentNullException(Argument.bytes);

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
        public static byte[] Decompress(this byte[] bytes)
        {
            if (bytes == null!)
                Throw.ArgumentNullException(Argument.bytes);

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
        public static byte[] Encrypt(this byte[] bytes, SymmetricAlgorithm algorithm, byte[] key, byte[] iv)
        {
            if (bytes == null!)
                Throw.ArgumentNullException(Argument.bytes);
            if (algorithm == null!)
                Throw.ArgumentNullException(Argument.algorithm);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (iv == null!)
                Throw.ArgumentNullException(Argument.iv);

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
#if NET35 || NET40 || NET45 || NETSTANDARD2_0
        [SuppressMessage("Security", "CA5379:Do Not Use Weak Key Derivation Function Algorithm", Justification = "The overload with a stronger algorithm requires at least .NET 4.7.2")] 
#endif
        public static byte[] Encrypt(this byte[] bytes, SymmetricAlgorithm algorithm, string password, string? salt)
        {
            if (password == null!)
                Throw.ArgumentNullException(Argument.password);
            if (algorithm == null!)
                Throw.ArgumentNullException(Argument.algorithm);

            CheckSalt(ref salt);
#if NET35 || NET40 || NET45 || NETSTANDARD2_0
            var passwordKey = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt));
#else
            var passwordKey = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 1000, HashAlgorithmName.SHA256);
#endif
#if !NET35
            using (passwordKey)
#endif
            {
                return Encrypt(bytes, algorithm, passwordKey.GetBytes(algorithm.KeySize >> 3), passwordKey.GetBytes(algorithm.BlockSize >> 3));
            }
        }

        /// <summary>
        /// Encrypts a byte array by the <see cref="Aes"/> algorithm using the provided <paramref name="password"/> and <paramref name="salt"/>.
        /// </summary>
        /// <param name="bytes">Source bytes to encrypt.</param>
        /// <param name="password">Password of encryption.</param>
        /// <param name="salt">A salt value to be used for encryption. If <see langword="null"/>&#160;or is empty, a default salt will be used.</param>
        /// <returns>The encrypted result of <paramref name="bytes"/>.</returns>
        public static byte[] Encrypt(this byte[] bytes, string password, string? salt)
        {
#if NETFRAMEWORK
            using SymmetricAlgorithm alg = new AesManaged();
#else
            using SymmetricAlgorithm alg = Aes.Create();
#endif
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
            if (algorithm == null!)
                Throw.ArgumentNullException(Argument.algorithm);

            algorithm.GenerateKey();
            algorithm.GenerateIV();
            key = algorithm.Key;
            iv = algorithm.IV;
            return Encrypt(bytes, algorithm, key, iv);
        }

        /// <summary>
        /// Encrypts a byte array by the <see cref="Aes"/> algorithm using a randomly generated key and initialization vector, which are
        /// returned in <paramref name="key"/> and <paramref name="iv"/> parameters, respectively.
        /// </summary>
        /// <param name="bytes">Source bytes to encrypt.</param>
        /// <param name="key">Returns the automatically generated key used for encryption.</param>
        /// <param name="iv">Returns the automatically generated initialization vector used for encryption.</param>
        /// <returns>The encrypted result of <paramref name="bytes"/>.</returns>
        public static byte[] Encrypt(this byte[] bytes, out byte[] key, out byte[] iv)
        {
#if NETFRAMEWORK
            using SymmetricAlgorithm alg = new AesManaged();
#else
            using SymmetricAlgorithm alg = Aes.Create();
#endif
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
        public static byte[] Decrypt(this byte[] bytes, SymmetricAlgorithm algorithm, byte[] key, byte[] iv)
        {
            if (bytes == null!)
                Throw.ArgumentNullException(Argument.bytes);
            if (algorithm == null!)
                Throw.ArgumentNullException(Argument.algorithm);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (iv == null!)
                Throw.ArgumentNullException(Argument.iv);
            
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
        /// Decrypts a byte array by the <see cref="Aes"/> algorithm using the provided <paramref name="key"/> and initialization vector.
        /// </summary>
        /// <param name="bytes">Source bytes to decrypt.</param>
        /// <param name="key">Key of decryption.</param>
        /// <param name="iv">The initialization vector to be used for decryption.</param>
        /// <returns>The decrypted result of <paramref name="bytes"/>.</returns>
        public static byte[] Decrypt(this byte[] bytes, byte[] key, byte[] iv)
        {
#if NETFRAMEWORK
            using SymmetricAlgorithm alg = new AesManaged();
#else
            using SymmetricAlgorithm alg = Aes.Create();
#endif
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
#if NET35 || NET40 || NET45 || NETSTANDARD2_0
        [SuppressMessage("Security", "CA5379:Do Not Use Weak Key Derivation Function Algorithm", Justification = "The overload with a stronger algorithm requires at least .NET 4.7.2")]
#endif
        public static byte[] Decrypt(this byte[] bytes, SymmetricAlgorithm algorithm, string password, string? salt)
        {
            if (algorithm == null!)
                Throw.ArgumentNullException(Argument.algorithm);
            if (password == null!)
                Throw.ArgumentNullException(Argument.password);

            CheckSalt(ref salt);
#if NET35 || NET40 || NET45 || NETSTANDARD2_0
            var passwordKey = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt));
#else
            var passwordKey = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt), 1000, HashAlgorithmName.SHA256);
#endif
#if !NET35
            using (passwordKey)
#endif
            {
                return Decrypt(bytes, algorithm, passwordKey.GetBytes(algorithm.KeySize >> 3), passwordKey.GetBytes(algorithm.BlockSize >> 3));
            }
        }

        /// <summary>
        /// Decrypts a byte array by the <see cref="Aes"/> algorithm using the provided <paramref name="password"/> and <paramref name="salt"/>.
        /// </summary>
        /// <param name="bytes">Source bytes to decrypt.</param>
        /// <param name="password">Password of decryption.</param>
        /// <param name="salt">A salt value to be used for decryption. If <see langword="null"/>&#160;or is empty, a default salt will be used.</param>
        /// <returns>The decrypted result of <paramref name="bytes"/>.</returns>
        public static byte[] Decrypt(this byte[] bytes, string password, string? salt)
        {
#if NETFRAMEWORK
            using SymmetricAlgorithm alg = new AesManaged();
#else
            using SymmetricAlgorithm alg = Aes.Create();
#endif
            return Decrypt(bytes, alg, password, salt);
        }

        #endregion

        #endregion

        #region Private methods

        private static void CheckSalt([NotNull]ref string? salt)
        {
            if (String.IsNullOrEmpty(salt))
            {
                salt = "ABCDEFGH";
                return;
            }

            if (salt!.Length < 8)
                salt = salt.Repeat((int)Math.Ceiling(8d / salt.Length));
        }

        [SecuritySafeCritical]
        private static unsafe string Split(string text, int lineLength, int indentSize, char indentChar, bool indentSingleLine)
        {
            // single line
            if (lineLength <= 0 || text.Length <= lineLength)
            {
                if (!indentSingleLine || indentSize <= 0)
                    return text;
                return text.PadLeft(text.Length + indentSize, indentChar);
            }

            int lineCount = (int)Math.Ceiling((double)text.Length / lineLength);
            int len = text.Length + lineCount * indentSize + (lineCount - 1) * Environment.NewLine.Length;
            var result = new String('\0', len);
            fixed (char* pResult = result)
            {
                var sb = new MutableStringBuilder(pResult, len);

                int pos;
                for (pos = 0; pos < text.Length - lineLength; pos += lineLength)
                {
                    sb.Append(indentChar, indentSize);
                    sb.Append(text, pos, lineLength);
                    sb.AppendLine();
                }

                sb.Append(indentChar, indentSize);
                sb.Append(text, pos, text.Length - pos);
                Debug.Assert(sb.Length == sb.Capacity, "Wrong length initialization");
            }

            return result;
        }

        private static string Wrap(string text, string separator, int lineLength, int indentSize, char indentChar, bool indentSingleLine)
        {
            // single line
            if (lineLength <= 0 || text.Length <= lineLength)
            {
                if (!indentSingleLine || indentSize <= 0)
                    return text;
                return text.PadLeft(text.Length + indentSize, indentChar);
            }

            if (indentSize < 0)
                indentSize = 0;

            // Not using MutableStringBuilder because the final length can be longer than this if lines cannot be completely filled
            StringBuilder result = new StringBuilder(text.Length + (indentSize + Environment.NewLine.Length) * (text.Length / lineLength + 1));
            int pos;
            int nextSep;
            int currLineLen = 0;
            bool firstLine = true;

            string indent = indentSize > 0 ? new String(indentChar, indentSize) : String.Empty;
            StringSegment fragment;
            for (pos = 0; (nextSep = text.IndexOf(separator, pos, StringComparison.Ordinal)) > 0; pos = nextSep + separator.Length)
            {
                fragment = new StringSegment(text, pos, nextSep - pos + separator.Length);

                // wrapping is needed
                if (currLineLen + fragment.Length > lineLength)
                {
                    // the first fragment of the line exceeds the line length: dumping, and then wrapping.
                    if (currLineLen == 0)
                    {
                        result.Append(indentChar, indentSize);
                        result.Append(fragment.UnderlyingString, fragment.Offset, fragment.Length);
                        result.AppendLine();
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
                    result.Append(fragment.UnderlyingString, fragment.Offset, fragment.Length);
                    currLineLen = fragment.Length;
                    continue;
                }

                // no wrapping is needed
                result.Append(fragment.UnderlyingString, fragment.Offset, fragment.Length);
                currLineLen += fragment.Length;
            }

            // processing the last fragment
            fragment = text.AsSegment(pos);

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
                    result.Append(fragment.UnderlyingString, fragment.Offset, fragment.Length);
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

            result.Append(fragment.UnderlyingString, fragment.Offset, fragment.Length);
            return result.ToString();
        }

        #endregion
    }
}
