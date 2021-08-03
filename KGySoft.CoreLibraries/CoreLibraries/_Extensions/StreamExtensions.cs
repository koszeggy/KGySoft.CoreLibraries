#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StreamExtensions.cs
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
using System.IO;

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

        #endregion
    }
}
