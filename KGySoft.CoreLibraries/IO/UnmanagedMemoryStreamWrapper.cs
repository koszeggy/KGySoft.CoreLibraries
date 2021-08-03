#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: UnmanagedMemoryStreamWrapper.cs
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

using System.IO;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.IO
{
    /// <summary>
    /// Similar to the System version but this is read-only and provides no overridden logic for async operations.
    /// </summary>
    internal sealed class UnmanagedMemoryStreamWrapper : MemoryStream
    {
        #region Fields

        private readonly UnmanagedMemoryStream unmanagedStream;

        #endregion

        #region Properties

        public override int Capacity
        {
            get => (int)unmanagedStream.Capacity;
            set => base.Capacity = value; // to throw the appropriate exception
        }

        public override bool CanRead => unmanagedStream.CanRead;
        public override bool CanSeek => unmanagedStream.CanSeek;
        public override bool CanWrite => false;
        public override long Length => unmanagedStream.Length;

        public override long Position
        {
            get => unmanagedStream.Position;
            set => unmanagedStream.Position = value;
        }

        #endregion

        #region Constructors

        internal UnmanagedMemoryStreamWrapper(UnmanagedMemoryStream stream) : base(Reflector.EmptyArray<byte>(), false)
            => unmanagedStream = stream;

        #endregion

        #region Methods

        #region Public Methods

        public override long Seek(long offset, SeekOrigin loc) => unmanagedStream.Seek(offset, loc);
        public override int Read(byte[] buffer, int offset, int count) => unmanagedStream.Read(buffer, offset, count);
        public override int ReadByte() => unmanagedStream.ReadByte();
        public override byte[] ToArray() => unmanagedStream.ToArray();

        public override void WriteTo(Stream stream)
        {
            if (stream == null!)
                Throw.ArgumentNullException(Argument.stream);
            byte[] buffer = ToArray();
            stream.Write(buffer, 0, buffer.Length);
        }

        public override void SetLength(long value) => Throw.NotSupportedException(Res.NotSupported);
        public override void Write(byte[] buffer, int offset, int count) => Throw.NotSupportedException(Res.NotSupported);
        public override void WriteByte(byte value) => Throw.NotSupportedException(Res.NotSupported);

        #endregion

        #region Protected Methods

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                    unmanagedStream.Dispose();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        #endregion

        #endregion
    }
}
