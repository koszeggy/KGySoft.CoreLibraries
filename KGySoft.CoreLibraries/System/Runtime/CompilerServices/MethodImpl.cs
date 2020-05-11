using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    internal static class MethodImpl
    {
        internal const MethodImplOptions AggressiveInlining =
#if !(NET35 || NET40)
            MethodImplOptions.AggressiveInlining;
#else
            (MethodImplOptions)256;
#endif
    }
}
