#if NETFRAMEWORK || NETSTANDARD2_0
// ReSharper disable once CheckNamespace
namespace System
{
    internal static class MathF
    {
        #region Methods

        public static float Log(float x, float newBase) => (float)Math.Log(x, newBase);
        public static float Pow(float x, float y) => (float)Math.Pow(x, y);

        #endregion
    }
}
#endif