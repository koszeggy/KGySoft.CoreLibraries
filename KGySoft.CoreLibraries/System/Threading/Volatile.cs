#if NET35 || NET40

// ReSharper disable once CheckNamespace
namespace System.Threading
{
    internal static class Volatile
    {
        #region Methods
        
        internal static int Read(ref int location) => Thread.VolatileRead(ref location);
        internal static long Read(ref long location) => Thread.VolatileRead(ref location);

        internal static void Write(ref long location, long value) => Thread.VolatileWrite(ref location, value);

        #endregion
    }
}

#endif