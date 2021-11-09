#if !NET5_0_OR_GREATER
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class ModuleInitializerAttribute : Attribute { }
} 
#endif