using System.ComponentModel;

namespace KGySoft.Reflection
{
    /// <summary>
    /// Possible reflection ways in methods of <see cref="Reflector"/> class.
    /// </summary>
    public enum ReflectionWays
    {
        /// <summary>
        /// Auto decision. In most cases it means the <see cref="DynamicDelegate"/> way.
        /// </summary>
        Auto,

        /// <summary>
        /// Dynamic delegate way. In this case first access of a member is slower than accessing it via
        /// system reflection but further accesses are much more faster.
        /// </summary>
        DynamicDelegate,

        /// <summary>
        /// Uses the standard system reflection way.
        /// </summary>
        SystemReflection,

        /// <summary>
        /// Uses type descriptor way. This is the slowest way but is preferable in case of
        /// <see cref="Component"/>s. Not applicable in all cases.
        /// </summary>
        TypeDescriptor
    }
}