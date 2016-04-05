using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KGySoft.Libraries
{
    using KGySoft.Libraries.Resources;

    /// <summary>
    /// Stream extension methods
    /// </summary>
    public static class StreamTools
    {
        /// <summary>
        /// Copies the <paramref name="source"/> <see cref="Stream"/> into the <paramref name="destination"/> one.
        /// Copy begins on the current position of source stream. None of the streams are closed or sought after
        /// the end of the copy progress.
        /// </summary>
        /// <param name="source">Source stream.</param>
        /// <param name="destination">Destination stream.</param>
        /// <param name="bufferSize">Size of the buffer used for copying.</param>
        public static void CopyTo(this Stream source, Stream destination, int bufferSize)
        {
            if (source == null)
                throw new ArgumentNullException("source", Res.Get(Res.ArgumentNull));
            if (destination == null)
                throw new ArgumentNullException("destination", Res.Get(Res.ArgumentNull));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException("bufferSize", Res.Get(Res.ArgumentOutOfRange));
            if (!source.CanRead)
                throw new ArgumentException(Res.Get(Res.StreamCannotRead));
            if (!destination.CanWrite)
                throw new ArgumentException(Res.Get(Res.StreamCannotWrite));

            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, read);
            }
        }

        /// <summary>
        /// Copies the <paramref name="source"/> <see cref="Stream"/> into the <paramref name="destination"/> one.
        /// Copy begins on the current position of source stream. None of the streams are closed or sought after
        /// the end of the copy progress.
        /// </summary>
        /// <param name="source">Source stream.</param>
        /// <param name="destination">Destination stream.</param>
        public static void CopyTo(this Stream source, Stream destination)
        {
#if NET35
            int bufferSize = 4096;
#elif NET40 || NET45
            int bufferSize = Environment.SystemPageSize;
#else
#error .NET version is not set or not supported!
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
            if (s == null)
                throw new ArgumentNullException("s", Res.Get(Res.ArgumentNull));
            if (!s.CanRead)
                throw new ArgumentException(Res.Get(Res.StreamCannotRead));

            MemoryStream ms = s as MemoryStream;
            if (ms != null)
                return ms.ToArray();

            long pos = s.Position;
            if (pos != 0L)
            {
                if (!s.CanSeek)
                    throw new ArgumentException(Res.Get(Res.StreamCannotSeek));
                s.Seek(0, SeekOrigin.Begin);
            }

            byte[] result = new byte[s.Length];
            s.Read(result, 0, result.Length);
            if (s.CanSeek)
                s.Seek(pos, SeekOrigin.Begin);
            return result;
        }
    }
}
