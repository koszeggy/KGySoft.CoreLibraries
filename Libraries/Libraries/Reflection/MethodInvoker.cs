using System;
using System.Reflection;

namespace KGySoft.Libraries.Reflection
{
    using KGySoft.Libraries.Resources;

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
        protected Delegate Invoker
        {
            get
            {
                if (invoker == null)
                {
                    invoker = CreateInvoker();
                }
                return invoker;
            }
        }

        /// <summary>
        /// When overridden, returns a delegate that invokes the associated method.
        /// </summary>
        protected abstract Delegate CreateInvoker();

        /// <summary>
        /// Creates a new instance of <see cref="MethodInvoker"/>
        /// </summary>
        protected MethodInvoker(MethodInfo method, Type declaringType, Type[] parameterTypes) :
            base(method, declaringType, parameterTypes)
        {
        }

        /// <summary>
        /// Retrieves an invoker for a method based on a <see cref="MethodInfo"/> instance.
        /// </summary>
        /// <param name="method">The <see cref="MethodInfo"/> that contains informations of the method to invoke.</param>
        /// <returns>Returns a <see cref="MethodInvoker"/> instance that can be used to invoke the method.</returns>
        public static MethodInvoker GetMethodInvoker(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException("method", Res.Get(Res.ArgumentNull));
            if (CachingEnabled)
                return (MethodInvoker)GetCreateAccessor(method);
            else
                return CreateMethodInvoker(method);
        }

        /// <summary>
        /// Non-caching version of invoker creation.
        /// </summary>
        internal static MethodInvoker CreateMethodInvoker(MethodInfo method)
        {
            ParameterInfo[] pi = method.GetParameters() ?? new ParameterInfo[0];
            Type[] parameterTypes = new Type[pi.Length];
            for (int i = 0; i < pi.Length; i++)
            {
                parameterTypes[i] = pi[i].ParameterType;
            }
            // late-initialization of MemberInfo to avoid caching
            if (method.ReturnType == typeof(void))
                return new ActionInvoker(method.DeclaringType, parameterTypes) { MemberInfo = method };
            else
                return new FunctionInvoker(method.DeclaringType, parameterTypes) { MemberInfo = method };
        }

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
