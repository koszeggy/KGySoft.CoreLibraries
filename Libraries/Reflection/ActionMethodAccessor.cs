using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace KGySoft.Reflection
{
    /// <summary>
    /// Action method accessor invoker for any parameters. Internal, cannot be instantiated from outside.
    /// </summary>
    internal sealed class ActionMethodAccessor : MethodAccessor
    {
        /// <summary>
        /// Represents a non-generic action that can be used for any action methods.
        /// </summary>
        private delegate void AnyAction(object target, object[] arguments);

        internal ActionMethodAccessor(MethodBase mi)
            : base(mi)
        {
        }

        /// <summary>
        /// Invokes the method with given parameters.
        /// </summary>
        /// <param name="instance">The instance on which the method is invoked</param>
        /// <param name="parameters">Method parameters</param>
        /// <returns>Returns always <see langword="null"/> because this class represents a method with <see cref="Void"/> return type.</returns>
        public override object Invoke(object instance, params object[] parameters)
        {
            ((AnyAction)Invoker)(instance, parameters);
            return null;
        }

        protected override Delegate CreateInvoker()
        {
            var methodBase = (MethodBase)MemberInfo;
            bool hasRefParameters = ParameterTypes.Any(p => p.IsByRef);
            Type declaringType = methodBase.DeclaringType;
            if (!methodBase.IsStatic && declaringType == null)
                throw new InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

            // for classes and static methods that have no ref parameters: Lambda expression
            // ReSharper disable once PossibleNullReferenceException - declaring type was already checked above
            if (!hasRefParameters && (methodBase.IsStatic || !declaringType.IsValueType) && methodBase is MethodInfo method)
            {
                ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
                ParameterExpression parametersParameter = Expression.Parameter(typeof(object[]), "parameters");
                UnaryExpression[] methodParameters = new UnaryExpression[ParameterTypes.Length];
                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    //// This just avoids error when ref parameters are used but does not assign results back
                    //Type parameterType = ParameterTypes[i];
                    //if (parameterType.IsByRef)
                    //    parameterType = parameterType.GetElementType();
                    methodParameters[i] = Expression.Convert(Expression.ArrayIndex(parametersParameter, Expression.Constant(i)), ParameterTypes[i]);
                }

                // ReSharper disable once AssignNullToNotNullAttribute - declaring type was already checked above
                MethodCallExpression methodToCall = Expression.Call(
                    method.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                    method, // method info
                    methodParameters); // parameters casted to target types

                LambdaExpression lambda = Expression.Lambda<AnyAction>(
                    methodToCall, // no return type
                    instanceParameter, // instance (object)
                    parametersParameter);
                return lambda.Compile();
            }
            // for struct instance methods or methods with ref/out parameters: Dynamic method
            else
            {
                var options = methodBase is ConstructorInfo ? DynamicMethodOptions.TreatCtorAsMethod : DynamicMethodOptions.None;
                if (hasRefParameters)
                    options |= DynamicMethodOptions.HandleByRefParameters;
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(methodBase, options);
                return dm.CreateDelegate(typeof(AnyAction));
            }
        }
    }

    ///// <summary>
    ///// Represents a parameterless method with <see cref="Void"/> return type.
    ///// </summary>
    ///// <typeparam name="T">The type that contains the method to invoke.</typeparam>
    //public sealed class ActionInvoker<T>: MethodInvoker
    //{
    //    // TODO: delete private constructors if they are never called (activator from base)
    //    /// <summary>
    //    /// Private constructor to make possible fast dynamic creation.
    //    /// </summary>
    //    private ActionInvoker()
    //        : base(null, typeof(T))
    //    {
    //    }

    //    /// <summary>
    //    /// Creates a new instance of <see cref="ActionInvoker&lt;T&gt;"/> class.
    //    /// </summary>
    //    public ActionInvoker(MethodInfo method)
    //        : base(method, typeof(T))
    //    {
    //        if (method == null)
    //            throw new ArgumentNullException("method");
    //    }

    //    /// <summary>
    //    /// Invokes the method in a non-generic way.
    //    /// If possible use the <see cref="Invoke(T)"/> overload for better performance.
    //    /// </summary>
    //    /// <param name="instance">The object instance that contains the method to invoke.</param>
    //    /// <param name="parameters">Ignored because this class represents a parameterless method.</param>
    //    /// <returns>Returns null because this class represents a method with <see cref="Void"/> return type.</returns>
    //    public override object Invoke(object instance, object[] parameters)
    //    {
    //        ((Action<T>)Invoker)((T)instance);
    //        return null;
    //    }

    //    /// <summary>
    //    /// Invokes the method on the given instance.
    //    /// </summary>
    //    /// <param name="instance">The object instance on that the method should be invoked.</param>
    //    public void Invoke(T instance)
    //    {
    //        ((Action<T>)Invoker)(instance);
    //    }
    //}

    //public sealed class ActionInvoker<T, TP1>: MethodInvoker
    //{
    //    /// <summary>
    //    /// Private constructor to make fast creation possible.
    //    /// </summary>
    //    private ActionInvoker()
    //        : base(null, typeof(T), typeof(TP1))
    //    {
    //    }

    //    public ActionInvoker(MethodInfo method)
    //        : base(method, typeof(T), typeof(TP1))
    //    {
    //        if (method == null)
    //            throw new ArgumentNullException("method");
    //    }

    //    public override object Invoke(object instance, object[] parameters)
    //    {
    //        ((Action<T, TP1>)Invoker)((T)instance, (TP1)parameters[0]);
    //        return null;
    //    }

    //    public void Invoke(T instance, TP1 p1)
    //    {
    //        ((Action<T, TP1>)Invoker)(instance, p1);
    //    }

    //}

    //public sealed class ActionInvoker<T, TP1, TP2>: MethodInvoker
    //{
    //    /// <summary>
    //    /// Private constructor to make fast creation possible.
    //    /// </summary>
    //    private ActionInvoker()
    //        : base(null, typeof(T), typeof(TP1), typeof(TP2))
    //    {
    //    }

    //    public ActionInvoker(MethodInfo method)
    //        : base(method, typeof(T), typeof(TP1), typeof(TP2))
    //    {
    //        if (method == null)
    //            throw new ArgumentNullException("method");
    //    }

    //    public override object Invoke(object instance, object[] parameters)
    //    {
    //        ((Action<T, TP1, TP2>)Invoker)((T)instance, (TP1)parameters[0], (TP2)parameters[1]);
    //        return null;
    //    }

    //    public void Invoke(T instance, TP1 p1, TP2 p2)
    //    {
    //        ((Action<T, TP1, TP2>)Invoker)(instance, p1, p2);
    //    }

    //}

    //public sealed class ActionInvoker<T, TP1, TP2, TP3>: MethodInvoker
    //{
    //    /// <summary>
    //    /// Private constructor to make fast creation possible.
    //    /// </summary>
    //    private ActionInvoker()
    //        : base(null, typeof(T), typeof(TP1), typeof(TP2), typeof(TP3))
    //    {
    //    }

    //    public ActionInvoker(MethodInfo method)
    //        : base(method, typeof(T), typeof(TP1), typeof(TP2), typeof(TP3))
    //    {
    //        if (method == null)
    //            throw new ArgumentNullException("method");
    //    }

    //    public override object Invoke(object instance, object[] parameters)
    //    {
    //        ((Action<T, TP1, TP2, TP3>)Invoker)((T)instance, (TP1)parameters[0], (TP2)parameters[1], (TP3)parameters[2]);
    //        return null;
    //    }

    //    public void Invoke(T instance, TP1 p1, TP2 p2, TP3 p3)
    //    {
    //        ((Action<T, TP1, TP2, TP3>)Invoker)(instance, p1, p2, p3);
    //    }
    //}

}
