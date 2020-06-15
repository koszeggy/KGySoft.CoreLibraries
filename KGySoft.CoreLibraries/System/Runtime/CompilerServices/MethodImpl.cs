// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    internal static class MethodImpl
    {
        internal const MethodImplOptions AggressiveInlining =
#if NET35 || NET40
            (MethodImplOptions)256;
#else
            MethodImplOptions.AggressiveInlining;
#endif
    }
}
