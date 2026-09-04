#if !NET9_0_OR_GREATER

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, Inherited = false)]
    internal sealed class OverloadResolutionPriorityAttribute : Attribute
    {
        internal OverloadResolutionPriorityAttribute(int priority) => Priority = priority;

        public int Priority { get; }
    }
}

#endif