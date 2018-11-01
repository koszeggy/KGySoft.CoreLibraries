using System;
using System.Linq;
using System.Reflection;

namespace KGySoft.Reflection
{
    /// <summary>
    /// Base class of method (action or function) invokation classes.
    /// Provides static <see cref="GetMethodInvoker"/> method to obtain invoker of any method.
    /// </summary>
    public abstract class MethodInvoker: MemberAccessor
    {
        private Delegate invoker;

        /// <summary>
        /// The method invoker delegate.
        /// </summary>
        protected Delegate Invoker => invoker ?? (invoker = CreateInvoker());

        /// <summary>
        /// When overridden, returns a delegate that invokes the associated method.
        /// </summary>
        protected abstract Delegate CreateInvoker();

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodInvoker"/> class.
        /// </summary>
        /// <param name="method">The method to associate with this <see cref="MethodInvoker"/>.</param>
        protected MethodInvoker(MethodBase method) :
            base(method, method.GetParameters().Select(p => p.ParameterType).ToArray())
        {
        }

        /// <summary>
        /// Retrieves an invoker for a method based on a <see cref="MethodInfo"/> instance.
        /// </summary>
        /// <param name="method">The <see cref="MethodInfo"/> that contains informations of the method to invoke.</param>
        /// <returns>Returns a <see cref="MethodInvoker"/> instance that can be used to invoke the method.</returns>
        public static MethodInvoker GetMethodInvoker(MethodInfo method) 
            => (MethodInvoker)GetCreateAccessor(method ?? throw new ArgumentNullException(nameof(method), Res.ArgumentNull));

        /// <summary>
        /// Non-caching version of invoker creation.
        /// </summary>
        internal static MethodInvoker CreateMethodInvoker(MethodInfo method) => method.ReturnType == typeof(void) 
            ? (MethodInvoker)new ActionInvoker(method) 
            : new FunctionInvoker(method);

        /// <summary>
        /// Invokes the method. Return value of <see cref="Void"/>-types methods are null.
        /// In case of a static method <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// </summary>
        /// <remarks>
        /// <note>
        /// First invocation of a method can be even slower than using <see cref="MethodBase.Invoke(object,object[])"/> of System.Reflection
        /// but further calls are much more fast.
        /// </note>
        /// </remarks>
        public abstract object Invoke(object instance, params object[] parameters);
    }
}
