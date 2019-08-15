#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StringStream.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
using System.IO;
#if NET35
using KGySoft.CoreLibraries;
#endif
using KGySoft.Reflection;

#endregion

namespace KGySoft.IO
{
    [Serializable]
    internal sealed class StringStream : MemoryStream
    {
        #region Fields

        private readonly string str;

        private int position;

        #endregion

        #region Properties and Indexers

        #region Properties

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                if (position < 0)
                    throw new ObjectDisposedException(null, Res.ObjectDisposed);
                return str.Length << 1;
            }
        }

        public override long Position
        {
            get
            {
                if (position < 0)
                    throw new ObjectDisposedException(null, Res.ObjectDisposed);
                return position;
            }
            set
            {
                if (position < 0)
                    throw new ObjectDisposedException(null, Res.ObjectDisposed);
                if (value < 0L || value > Length)
                    throw new ArgumentOutOfRangeException(nameof(value), Res.ArgumentOutOfRange);
                position = (int)value;
            }
        }

        public override int Capacity
        {
            get => position >= 0 ? str.Length << 1 : throw new ObjectDisposedException(null, Res.ObjectDisposed);
            set => base.Capacity = value; // to throw the appropriate exception
        }

        #endregion

        #region Indexers

        private byte this[int i] => (i & 1) == 0 ? (byte)(str[i >> 1] & 0xFF) : (byte)(str[i >> 1] >> 8);

        #endregion

        #endregion

        #region Constructors

        public StringStream(string s) : base(Reflector.EmptyArray<byte>(), false)
            => str = s ?? throw new ArgumentNullException(nameof(s), Res.ArgumentNull);

        #endregion

        #region Methods

        #region Public Methods

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (position < 0)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length - offset;
                    break;
            }

            return position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (position < 0)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), Res.ArgumentNull);
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if (buffer.Length - offset < count)
                throw new ArgumentException(Res.ArrayInvalidOffsLen);
            int len = Math.Min(count, (str.Length << 1) - position);
            for (int i = 0; i < len; i++)
                buffer[offset++] = this[position++];
            return len;
        }

        public override int ReadByte()
        {
            if (position < 0)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            return position == str.Length << 1 ? -1 : this[position++];
        }

        public override byte[] ToArray()
        {
            var result = new byte[str.Length << 1];
            for (int i = 0; i < result.Length; i++)
                result[i] = this[i];
            return result;
        }

        public override void WriteTo(Stream stream)
        {
            if (position < 0)
                throw new ObjectDisposedException(null, Res.ObjectDisposed);
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), Res.ArgumentNull);
            using (var ss = new StringStream(str))
                ss.CopyTo(stream);
        }

        public override void SetLength(long value) => throw new NotSupportedException(Res.NotSupported);
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException(Res.NotSupported);
        public override void WriteByte(byte value) => throw new NotSupportedException(Res.NotSupported);

        public override string ToString() => str;

        #endregion

        #region Protected Methods

        [SuppressMessage("Microsoft.Usage", "CA2215:Dispose methods should call base class dispose",
            Justification = "Just avoiding calling Dispose on a disposed object")]
        protected override void Dispose(bool disposing)
        {
            if (position < 0)
                return;
            position = -1;
            base.Dispose(disposing);
        }

        #endregion

        #endregion
    }
}
