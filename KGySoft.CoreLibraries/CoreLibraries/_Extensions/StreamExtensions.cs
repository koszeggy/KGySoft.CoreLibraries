#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StreamExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
#if NETFRAMEWORK && !NET472_OR_GREATER || NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO;
using System.Security.Cryptography;

using KGySoft.Security.Cryptography;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="Stream"/> type.
    /// </summary>
    public static class StreamExtensions
    {
        #region Methods

        /// <summary>
        /// Copies the <paramref name="source"/>&#160;<see cref="Stream"/> into the <paramref name="destination"/> one.
        /// Copy begins on the current position of source stream. None of the streams are closed or sought after
        /// the end of the copy progress.
        /// </summary>
        /// <param name="source">Source stream.</param>
        /// <param name="destination">Destination stream.</param>
        /// <param name="bufferSize">Size of the buffer used for copying.</param>
        public static void CopyTo(this Stream source, Stream destination, int bufferSize)
        {
            if (source == null!)
                Throw.ArgumentNullException(Argument.source);
            if (destination == null!)
                Throw.ArgumentNullException(Argument.destination);
            if (bufferSize <= 0)
                Throw.ArgumentOutOfRangeException(Argument.bufferSize);
            if (!source.CanRead)
                Throw.ArgumentException(Argument.source, Res.StreamExtensionsStreamCannotRead);
            if (!destination.CanWrite)
                Throw.ArgumentException(Argument.destination, Res.StreamExtensionsStreamCannotWrite);

            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, read);
            }
        }

        /// <summary>
        /// Copies the <paramref name="source"/>&#160;<see cref="Stream"/> into the <paramref name="destination"/> one.
        /// Copy begins on the current position of source stream. None of the streams are closed or sought after
        /// the end of the copy progress.
        /// </summary>
        /// <param name="source">Source stream.</param>
        /// <param name="destination">Destination stream.</param>
        public static void CopyTo(this Stream source, Stream destination)
        {
#if NET35
            int bufferSize = 4096;
#else
            int bufferSize = Environment.SystemPageSize;
#endif

            CopyTo(source, destination, bufferSize);
        }

        /// <summary>
        /// Converts a stream to array of bytes. If the stream can be sought, its position will be the same as before calling this method.
        /// </summary>
        /// <param name="s">Source stream</param>
        /// <returns>A byte <see cref="Array"/> with the stream content.</returns>
        public static byte[] ToArray(this Stream s)
        {
            if (s == null!)
                Throw.ArgumentNullException(Argument.s);
            if (!s.CanRead)
                Throw.ArgumentException(Argument.s, Res.StreamExtensionsStreamCannotRead);

            if (s is MemoryStream ms)
                return ms.ToArray();

            if (!s.CanSeek)
            {
                using (ms = new MemoryStream())
                {
                    CopyTo(s, ms);
                    return ms.ToArray();
                }
            }

            long pos = s.Position;
            try
            {
                if (pos != 0L)
                    s.Seek(0, SeekOrigin.Begin);

                byte[] result = new byte[s.Length];
                int len = s.Read(result, 0, result.Length);

                // we could read the whole stream in one step
                if (len == s.Length)
                    return result;

                // we use the buffer with the first fragment and continue reading
                using (ms = new MemoryStream(result, 0, len, true, true) { Position = len })
                {
                    CopyTo(s, ms);

                    // if the stream still reports the same length we return its internal buffer to prevent duplicating the array in memory; otherwise, returning a new array
                    return ms.Length == s.Length ? ms.GetBuffer() : ms.ToArray();
                }
            }
            finally
            {
                s.Seek(pos, SeekOrigin.Begin);
            }
        }

        /// <summary>
        /// Encrypts a <paramref name="source"/> stream by the provided symmetric <paramref name="algorithm"/>, <paramref name="key"/> and <paramref name="iv"/>,
        /// and writes the encrypted result to the <paramref name="destination"/> stream. Both streams remain open after the encryption is done.
        /// </summary>
        /// <param name="source">The source stream to encrypt.</param>
        /// <param name="destination">The destination stream to write the encrypted data to.</param>
        /// <param name="algorithm">A <see cref="SymmetricAlgorithm"/> instance to be used for encryption.</param>
        /// <param name="key">Key to be used for encryption.</param>
        /// <param name="iv">Initialization vector to be used for encryption.</param>
        public static void Encrypt(this Stream source, Stream destination, SymmetricAlgorithm algorithm, byte[] key, byte[] iv)
        {
            if (source == null!)
                Throw.ArgumentNullException(Argument.source);
            if (destination == null!)
                Throw.ArgumentNullException(Argument.destination);
            if (algorithm == null!)
                Throw.ArgumentNullException(Argument.algorithm);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (iv == null!)
                Throw.ArgumentNullException(Argument.iv);

            algorithm.Key = key;
            algorithm.IV = iv;

            using ICryptoTransform encryptor = algorithm.CreateEncryptor();
#if NET472_OR_GREATER || NETCOREAPP
            using var encryptStream = new CryptoStream(destination, encryptor, CryptoStreamMode.Write, true);
            source.CopyTo(encryptStream);
#else
            var encryptStream = new CryptoStream(destination, encryptor, CryptoStreamMode.Write);
            try
            {
                source.CopyTo(encryptStream);
            }
            finally
            {
                encryptStream.FlushFinalBlock();
            }
#endif
        }

        /// <summary>
        /// Encrypts a <paramref name="source"/> stream by the provided symmetric <paramref name="algorithm"/> and <paramref name="password"/>, using a randomly generated <paramref name="salt"/>,
        /// and writes the encrypted result to the <paramref name="destination"/> stream. Both streams remain open after the encryption is done.
        /// </summary>
        /// <param name="source">The source stream to encrypt.</param>
        /// <param name="destination">The destination stream to write the encrypted data to.</param>
        /// <param name="algorithm">A <see cref="SymmetricAlgorithm"/> instance to be used for encryption.</param>
        /// <param name="password">Password of encryption.</param>
        /// <param name="salt">When this method returns, contains the randomly generated salt bytes used to derive the key and initialization vector bytes. This parameter is passed uninitialized.</param>
#if NETFRAMEWORK && !NET472_OR_GREATER || NETSTANDARD2_0
        [SuppressMessage("Security", "CA5379:Do Not Use Weak Key Derivation Function Algorithm", Justification = "The overload with a stronger algorithm requires at least .NET 4.7.2")]
#endif
        public static void Encrypt(this Stream source, Stream destination, SymmetricAlgorithm algorithm, string password, out byte[] salt)
        {
            if (password == null!)
                Throw.ArgumentNullException(Argument.password);
            if (algorithm == null!)
                Throw.ArgumentNullException(Argument.algorithm);

            salt = SecureRandom.Instance.NextBytes(8);
            int keyBytes = algorithm.KeySize >> 3;
            int blockBytes = algorithm.BlockSize >> 3;

#if NET6_0_OR_GREATER
            Span<byte> dest = stackalloc byte[keyBytes + blockBytes];
            Rfc2898DeriveBytes.Pbkdf2(password, salt, dest, 1000, HashAlgorithmName.SHA256);
            Encrypt(source, destination, algorithm, dest.Slice(0, keyBytes).ToArray(), dest.Slice(keyBytes).ToArray());
#else
#if NETFRAMEWORK && !NET472_OR_GREATER || NETSTANDARD2_0
            var passwordKey = new Rfc2898DeriveBytes(password, salt);
#else
            var passwordKey = new Rfc2898DeriveBytes(password, salt, 1000, HashAlgorithmName.SHA256);
#endif
#if !NET35
            using (passwordKey)
#endif
            {
                Encrypt(source, destination, algorithm, passwordKey.GetBytes(keyBytes), passwordKey.GetBytes(blockBytes));
            }
#endif
        }

        /// <summary>
        /// Encrypts a <paramref name="source"/> stream by the <see cref="Aes"/> algorithm using the provided <paramref name="password"/> and a randomly generated <paramref name="salt"/>,
        /// and writes the encrypted result to the <paramref name="destination"/> stream. Both streams remain open after the encryption is done.
        /// </summary>
        /// <param name="source">The source stream to encrypt.</param>
        /// <param name="destination">The destination stream to write the encrypted data to.</param>
        /// <param name="password">Password of encryption.</param>
        /// <param name="salt">When this method returns, contains the randomly generated salt bytes used to derive the key and initialization vector bytes. This parameter is passed uninitialized.</param>
        public static void Encrypt(this Stream source, Stream destination, string password, out byte[] salt)
        {
#if NETFRAMEWORK
            using SymmetricAlgorithm alg = new AesManaged();
#else
            using SymmetricAlgorithm alg = Aes.Create();
#endif
            Encrypt(source, destination, alg, password, out salt);
        }

        /// <summary>
        /// Encrypts a <paramref name="source"/> stream by the provided symmetric <paramref name="algorithm"/>, using a randomly generated key and initialization vector, which are
        /// returned in <paramref name="key"/> and <paramref name="iv"/> parameters, respectively. The encrypted result is written to the <paramref name="destination"/> stream.
        /// Both streams remain open after the encryption is done.
        /// </summary>
        /// <param name="source">The source stream to encrypt.</param>
        /// <param name="destination">The destination stream to write the encrypted data to.</param>
        /// <param name="algorithm">A <see cref="SymmetricAlgorithm"/> instance to be used for encryption.</param>
        /// <param name="key">Returns the automatically generated key used for encryption.</param>
        /// <param name="iv">Returns the automatically generated initialization vector used for encryption.</param>
        [CLSCompliant(false)]
        public static void Encrypt(this Stream source, Stream destination, SymmetricAlgorithm algorithm, out byte[] key, out byte[] iv)
        {
            if (algorithm == null!)
                Throw.ArgumentNullException(Argument.algorithm);

            algorithm.GenerateKey();
            algorithm.GenerateIV();
            key = algorithm.Key;
            iv = algorithm.IV;
            Encrypt(source, destination, algorithm, key, iv);
        }

        /// <summary>
        /// Encrypts a <paramref name="source"/> stream by the <see cref="Aes"/> algorithm using a randomly generated key and initialization vector, which are
        /// returned in <paramref name="key"/> and <paramref name="iv"/> parameters, respectively. The encrypted result is written to the <paramref name="destination"/> stream.
        /// Both streams remain open after the encryption is done.
        /// </summary>
        /// <param name="source">The source stream to encrypt.</param>
        /// <param name="destination">The destination stream to write the encrypted data to.</param>
        /// <param name="key">Returns the automatically generated key used for encryption.</param>
        /// <param name="iv">Returns the automatically generated initialization vector used for encryption.</param>
        public static void Encrypt(this Stream source, Stream destination, out byte[] key, out byte[] iv)
        {
#if NETFRAMEWORK
            using SymmetricAlgorithm alg = new AesManaged();
#else
            using SymmetricAlgorithm alg = Aes.Create();
#endif
            Encrypt(source, destination, alg, out key, out iv);
        }

        /// <summary>
        /// Decrypts a <paramref name="source"/> stream by the provided symmetric <paramref name="algorithm"/>, <paramref name="key"/> and initialization vector,
        /// and writes the decrypted result to the <paramref name="destination"/> stream. Both streams remain open after the decryption is done.
        /// </summary>
        /// <param name="source">The source stream to decrypt.</param>
        /// <param name="destination">The destination stream to write the decrypted data to.</param>
        /// <param name="algorithm">A <see cref="SymmetricAlgorithm"/> instance to use for decryption.</param>
        /// <param name="key">Key of decryption.</param>
        /// <param name="iv">The initialization vector to be used for decryption.</param>
        public static void Decrypt(this Stream source, Stream destination, SymmetricAlgorithm algorithm, byte[] key, byte[] iv)
        {
            if (source == null!)
                Throw.ArgumentNullException(Argument.source);
            if (destination == null!)
                Throw.ArgumentNullException(Argument.destination);
            if (algorithm == null!)
                Throw.ArgumentNullException(Argument.algorithm);
            if (key == null!)
                Throw.ArgumentNullException(Argument.key);
            if (iv == null!)
                Throw.ArgumentNullException(Argument.iv);

            algorithm.Key = key;
            algorithm.IV = iv;

            using ICryptoTransform decryptor = algorithm.CreateDecryptor();
#if NET472_OR_GREATER || NETCOREAPP
            using CryptoStream encryptStream = new CryptoStream(source, decryptor, CryptoStreamMode.Read, true);
            encryptStream.CopyTo(destination);
#else
            var encryptStream = new CryptoStream(source, decryptor, CryptoStreamMode.Read);
            try
            {
                encryptStream.CopyTo(destination);
            }
            finally
            {
                encryptStream.Flush(); // not calling FlushFinalBlock here, because it is called implicitly when decrypting a stream
            }
#endif
        }

        /// <summary>
        /// Decrypts a <paramref name="source"/> stream by the <see cref="Aes"/> algorithm using the provided <paramref name="key"/> and initialization vector,
        /// and writes the decrypted result to the <paramref name="destination"/> stream. Both streams remain open after the decryption is done.
        /// </summary>
        /// <param name="source">The source stream to decrypt.</param>
        /// <param name="destination">The destination stream to write the decrypted data to.</param>
        /// <param name="key">Key of decryption.</param>
        /// <param name="iv">The initialization vector to be used for decryption.</param>
        public static void Decrypt(this Stream source, Stream destination, byte[] key, byte[] iv)
        {
#if NETFRAMEWORK
            using SymmetricAlgorithm alg = new AesManaged();
#else
            using SymmetricAlgorithm alg = Aes.Create();
#endif
            Decrypt(source, destination, alg, key, iv);
        }

        /// <summary>
        /// Decrypts a <paramref name="source"/> stream by the provided symmetric <paramref name="algorithm"/>, <paramref name="password"/> and <paramref name="salt"/>,
        /// and writes the decrypted result to the <paramref name="destination"/> stream. Both streams remain open after the decryption is done.
        /// </summary>
        /// <param name="source">The source stream to decrypt.</param>
        /// <param name="destination">The destination stream to write the decrypted data to.</param>
        /// <param name="algorithm">A <see cref="SymmetricAlgorithm"/> instance to use for decryption.</param>
        /// <param name="password">Password of decryption.</param>
        /// <param name="salt">A salt value to be used to derive the key and initialization vector bytes.
        /// It should be the same as the one generated by the <see cref="Encrypt(Stream,Stream,SymmetricAlgorithm,string,out byte[])"/> method.</param>
#if NETFRAMEWORK && !NET472_OR_GREATER || NETSTANDARD2_0
        [SuppressMessage("Security", "CA5379:Do Not Use Weak Key Derivation Function Algorithm", Justification = "The overload with a stronger algorithm requires at least .NET 4.7.2")]
#endif
        public static void Decrypt(this Stream source, Stream destination, SymmetricAlgorithm algorithm, string password, byte[] salt)
        {
            if (algorithm == null!)
                Throw.ArgumentNullException(Argument.algorithm);
            if (password == null!)
                Throw.ArgumentNullException(Argument.password);
            if (salt == null!)
                Throw.ArgumentNullException(Argument.salt);

            int keyBytes = algorithm.KeySize >> 3;
            int blockBytes = algorithm.BlockSize >> 3;
#if NET6_0_OR_GREATER
            Span<byte> dest = stackalloc byte[keyBytes + blockBytes];
            Rfc2898DeriveBytes.Pbkdf2(password, salt, dest, 1000, HashAlgorithmName.SHA256);
            Decrypt(source, destination, algorithm, dest.Slice(0, keyBytes).ToArray(), dest.Slice(keyBytes).ToArray());
#else

#if NETFRAMEWORK && !NET472_OR_GREATER || NETSTANDARD2_0
            var passwordKey = new Rfc2898DeriveBytes(password, salt);
#else
            var passwordKey = new Rfc2898DeriveBytes(password, salt, 1000, HashAlgorithmName.SHA256);
#endif
#if !NET35
            using (passwordKey)
#endif
            {
                Decrypt(source, destination, algorithm, passwordKey.GetBytes(keyBytes), passwordKey.GetBytes(blockBytes));
            }
#endif
        }

        /// <summary>
        /// Decrypts a <paramref name="source"/> stream by the <see cref="Aes"/> algorithm using the provided <paramref name="password"/> and <paramref name="salt"/>,
        /// and writes the decrypted result to the <paramref name="destination"/> stream. Both streams remain open after the decryption is done.
        /// </summary>
        /// <param name="source">The source stream to decrypt.</param>
        /// <param name="destination">The destination stream to write the decrypted data to.</param>
        /// <param name="password">Password of decryption.</param>
        /// <param name="salt">A salt value to be used to derive the key and initialization vector bytes.
        /// It should be the same as the one generated by the <see cref="Encrypt(Stream,Stream,string,out byte[])"/> method.</param>
        public static void Decrypt(this Stream source, Stream destination, string password, byte[] salt)
        {
#if NETFRAMEWORK
            using SymmetricAlgorithm alg = new AesManaged();
#else
            using SymmetricAlgorithm alg = Aes.Create();
#endif
            Decrypt(source, destination, alg, password, salt);
        }

        #endregion
    }
}
