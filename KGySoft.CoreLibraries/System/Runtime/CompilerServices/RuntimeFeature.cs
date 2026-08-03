#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER)
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
#if NETSTANDARD2_0 || (NETFRAMEWORK && !NET471_OR_GREATER)
    internal static class RuntimeFeature
    {
        #region Properties

        internal static bool IsDynamicCodeSupported => true;

        #endregion
    }
#else
    internal static class RuntimeFeatureExtensions
    {
        extension(RuntimeFeature)
        {
            internal static bool IsDynamicCodeSupported => true;
        }
    }
#endif
}
#endif