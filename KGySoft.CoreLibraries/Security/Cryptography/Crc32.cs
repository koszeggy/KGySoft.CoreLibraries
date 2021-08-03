#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Crc32.cs
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
using System.Security.Cryptography;
using System.Text;

using KGySoft.Collections;

#endregion

namespace KGySoft.Security.Cryptography
{
    /// <summary>
    /// Implementation of the CRC-32 hash algorithm.
    /// </summary>
    [CLSCompliant(false)]
    public class Crc32 : HashAlgorithm
    {
        #region Constants

        /// <summary>
        /// The standard polynomial for the <see cref="Crc32"/> hash algorithm. This field is constant.
        /// </summary>
        public const uint StandardPolynomial = 0xEDB88320;

        /// <summary>
        /// The Castagnoli polynomial for the <see cref="Crc32"/> hash algorithm (also known as CRC-32C). This field is constant.
        /// </summary>
        public const uint CastagnoliPolynomial = 0x82F63B78;

        /// <summary>
        /// The Koopman polynomial for the <see cref="Crc32"/> hash algorithm (also known as CRC-32K). This field is constant.
        /// </summary>
        public const uint KoopmanPolynomial = 0xEB31D82E;

        #endregion

        #region Fields

        #region Static Fields

        private static readonly IThreadSafeCacheAccessor<uint, uint[]> tablesCache = ThreadSafeCacheFactory.Create<uint, uint[]>(CreateTable, LockFreeCacheOptions.Profile4);

        #endregion

        #region Instance Fields

        private readonly uint initialCrc;
        private readonly bool isBigEndian;
        private readonly uint[] table;

        private uint hash;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Gets the size, in bits, of the computed hash code.
        /// </summary>
        public override int HashSize => 32;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Crc32"/> class with default settings.
        /// </summary>
        public Crc32()
            : this(StandardPolynomial)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Crc32"/> class.
        /// </summary>
        /// <param name="polynomial">The polynomial to use to calculate the CRC value.</param>
        /// <param name="initialCrc">The initial CRC value to use. If the final CRC is calculated in more sessions the result of the last calculation can be specified here. This parameter is optional.
        /// <br/>Default value: <c>0</c>.</param>
        /// <param name="isBigEndian">If <see langword="true"/>, the byte order of the <see cref="O:System.Security.Cryptography.HashAlgorithm.ComputeHash">ComputeHash</see> methods and the <see cref="HashAlgorithm.Hash"/> property
        /// will be big endian, which is the standard CRC32 representation; otherwise, little endian. Does not affect the <see cref="uint"/> return values though. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        [CLSCompliant(false)]
        public Crc32(uint polynomial, uint initialCrc = 0U, bool isBigEndian = true)
        {
            HashSizeValue = 32;
            table = tablesCache[polynomial];
            this.initialCrc = initialCrc;
            this.isBigEndian = isBigEndian;
            Reset();
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        /// <summary>
        /// Computes the CRC-32 hash value for the specified byte array.
        /// </summary>
        /// <param name="buffer">The input to compute the hash code for.</param>
        /// <param name="offset">The offset into the byte array from which to begin using data.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        /// <param name="initialCrc">The initial CRC value to use. If the final CRC is calculated in more sessions the result of the last calculation can be specified here. This parameter is optional.
        /// <br/>Default value: <c>0</c>.</param>
        /// <param name="polynomial">The polynomial to use to calculate the CRC value. This parameter is optional.
        /// <br/>Default value: <see cref="StandardPolynomial"/>.</param>
        /// <returns>The CRC-32 hash value of the specified <paramref name="buffer"/>; or, if <paramref name="initialCrc"/> was specified, an
        /// accumulated hash value appended by the current <paramref name="buffer"/>.</returns>
        public static uint CalculateHash(byte[] buffer, int offset, int count, uint initialCrc = 0U, uint polynomial = StandardPolynomial)
        {
            if (buffer == null!)
                Throw.ArgumentNullException(Argument.buffer);
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                Throw.ArgumentOutOfRangeException(Argument.count);
            return CalculateHash(tablesCache[polynomial], initialCrc, buffer, offset, count);
        }

        /// <summary>
        /// Computes the CRC-32 hash value for the specified byte array.
        /// </summary>
        /// <param name="buffer">The input to compute the hash code for.</param>
        /// <param name="initialCrc">The initial CRC value to use. If the final CRC is calculated in more sessions the result of the last calculation can be specified here. This parameter is optional.
        /// <br/>Default value: <c>0</c>.</param>
        /// <param name="polynomial">The polynomial to use to calculate the CRC value. This parameter is optional.
        /// <br/>Default value: <see cref="StandardPolynomial"/>.</param>
        /// <returns>The CRC-32 hash value of the specified <paramref name="buffer"/>; or, if <paramref name="initialCrc"/> was specified, an
        /// accumulated hash value appended by the current <paramref name="buffer"/>.</returns>
        public static uint CalculateHash(byte[] buffer, uint initialCrc = 0U, uint polynomial = StandardPolynomial)
        {
            if (buffer == null!)
                Throw.ArgumentNullException(Argument.buffer);
            return CalculateHash(tablesCache[polynomial], initialCrc, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Computes the CRC-32 hash value for the specified <see cref="string"/>.
        /// </summary>
        /// <param name="s">The input <see cref="string"/> to compute the hash code for.</param>
        /// <param name="encoding">An <see cref="Encoding"/> instance to specify the desired byte representation of the specified string, or <see langword="null"/>&#160;to use <see cref="Encoding.UTF8"/> encoding. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="polynomial">The polynomial to use to calculate the CRC value. This parameter is optional.
        /// <br/>Default value: <see cref="StandardPolynomial"/>.</param>
        /// <returns>The CRC-32 hash value of the specified <see cref="string"/>.</returns>
        public static uint CalculateHash(string s, Encoding? encoding = null, uint polynomial = StandardPolynomial)
        {
            if (s == null!)
                Throw.ArgumentNullException(Argument.buffer);
            byte[] buffer = (encoding ?? Encoding.UTF8).GetBytes(s);
            return CalculateHash(tablesCache[polynomial], 0U, buffer, 0, buffer.Length);
        }

        #endregion

        #region Private Methods

        private static uint[] CreateTable(uint polynomial)
        {
            //uint[] table = new uint[256];
            //for (int i = 0; i < 256; i++)
            //{
            //    uint entry = (uint)i;
            //    for (int j = 0; j < 8; j++)
            //        if ((entry & 1) == 1)
            //            entry = (entry >> 1) ^ polynomial;
            //        else
            //            entry = entry >> 1;
            //    table[i] = entry;
            //}

            var table = new uint[16 * 256];
            for (uint i = 0; i < 256; i++)
            {
                uint entry = i;
                for (int j = 0; j < 16; j++)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        entry = (entry & 1) == 1
                            ? (entry >> 1) ^ polynomial
                            : entry >> 1;
                    }

                    table[(j * 256) + i] = entry;
                }
            }

            return table;
        }

        private static uint CalculateHash(uint[] table, uint initialCrc, byte[] buffer, int offset, int count)
        {
            if (count <= 0)
                return initialCrc;

            uint crc = ~initialCrc;
            int end = offset + count;
            //while (offset < end)
            //    crc = table[buffer[offset++] ^ crc & 0xff] ^ (crc >> 8);

            while (offset < end - 16)
            {
                uint a = table[(3 * 256) + buffer[offset + 12]]
                        ^ table[(2 * 256) + buffer[offset + 13]]
                        ^ table[(1 * 256) + buffer[offset + 14]]
                        ^ table[(0 * 256) + buffer[offset + 15]];

                uint b = table[(7 * 256) + buffer[offset + 8]]
                        ^ table[(6 * 256) + buffer[offset + 9]]
                        ^ table[(5 * 256) + buffer[offset + 10]]
                        ^ table[(4 * 256) + buffer[offset + 11]];

                uint c = table[(11 * 256) + buffer[offset + 4]]
                        ^ table[(10 * 256) + buffer[offset + 5]]
                        ^ table[(9 * 256) + buffer[offset + 6]]
                        ^ table[(8 * 256) + buffer[offset + 7]];

                uint d = table[(15 * 256) + ((byte)crc ^ buffer[offset])]
                        ^ table[(14 * 256) + ((byte)(crc >> 8) ^ buffer[offset + 1])]
                        ^ table[(13 * 256) + ((byte)(crc >> 16) ^ buffer[offset + 2])]
                        ^ table[(12 * 256) + ((crc >> 24) ^ buffer[offset + 3])];

                crc = a ^ b ^ c ^ d;
                offset += 16;
            }

            while (offset < end)
            {
                crc = table[(byte)(crc ^ buffer[offset])] ^ (crc >> 8);
                offset += 1;
            }

            return ~crc;
        }

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Resets internal state of the algorithm. Used internally.
        /// </summary>
        public override void Initialize() => Reset();

        #endregion

        #region Protected Methods

        /// <summary>
        /// Computes the CRC-32 hash for the specified <paramref name="array"/>.
        /// </summary>
        /// <param name="array">The input to compute the hash code for.</param>
        /// <param name="offset">The offset into the byte array from which to begin using data.</param>
        /// <param name="count">The number of bytes in the array to use as data.</param>
        protected override void HashCore(byte[] array, int offset, int count) 
            => hash = CalculateHash(table, hash, array, offset, count);

        /// <summary>
        /// Gets the final result of the computed CRC-32 hash as a byte array.
        /// </summary>
        /// <returns>
        /// The computed hash code.
        /// </returns>
        protected override byte[] HashFinal()
        {
            return isBigEndian 
                ? new[] { (byte)(hash >> 24), (byte)(hash >> 16), (byte)(hash >> 8), (byte)hash } 
                : new[] { (byte)hash, (byte)(hash >> 8), (byte)(hash >> 16), (byte)(hash >> 24) };
        }

        #endregion

        #region Private Methods

        private void Reset() => hash = initialCrc;

        #endregion

        #endregion

        #endregion
    }
}
