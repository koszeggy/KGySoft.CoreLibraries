#if NETFRAMEWORK || NETSTANDARD2_0
// ReSharper disable once CheckNamespace
namespace System
{
    internal static class MathF
    {
        #region Methods

        internal static float Log(float x, float newBase) => (float)Math.Log(x, newBase);
        internal static float Pow(float x, float y) => (float)Math.Pow(x, y);
        internal static float Log2(float a) => (float)Math.Log(a, 2d);

        #endregion
    }
}
#endif