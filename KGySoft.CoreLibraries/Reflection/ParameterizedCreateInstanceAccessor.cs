#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParameterizedCreateInstanceAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2022 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Runtime.ExceptionServices;

using KGySoft.Annotations;
using KGySoft.CoreLibraries;

#region Used Namespaces

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

#endregion

#region Used Aliases

using AnyCtor = System.Func<object?[]?, object>;

#endregion

#endregion

#region Suppressions

#if NETFRAMEWORK
#pragma warning disable CS8763 // A method marked [DoesNotReturn] should not return - false alarm, ExceptionDispatchInfo.Throw() does not return either.
#endif

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Object factory for creating new instance of an object via a specified constructor.
    /// </summary>
    internal sealed class ParameterizedCreateInstanceAccessor : CreateInstanceAccessor
    {
        #region Constructors

        internal ParameterizedCreateInstanceAccessor(ConstructorInfo ctor)
            : base(ctor)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public override object CreateInstance(params object?[]? parameters)
        {
            try
            {
                return ((AnyCtor)Initializer)(parameters);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception
                PostValidate(parameters, e);
                return null!; // actually never reached, just to satisfy the compiler
            }
        }

        #endregion

        #region Private Protected Methods

        private protected override Delegate CreateInitializer()
        {
            ConstructorInfo ctor = (ConstructorInfo)MemberInfo;
            // TODO
            //if (ctor.IsStatic)
            //    Throw.InvalidOperationException(Res.ReflectionInstanceConstructorExpected);

#if !NETSTANDARD2_0
            // for constructors with ref/out parameters: Dynamic method
            if (ParameterTypes.Any(p => p.IsByRef))
            {
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(ctor, DynamicMethodOptions.HandleByRefParameters);
                return dm.CreateDelegate(typeof(AnyCtor));
            }
#endif

            // for constructors that have no ref parameters: Lambda expression
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
            var ctorParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                Type parameterType = ParameterTypes[i];
#if NETSTANDARD2_0
                // This just avoids error when ref parameters are used but does not assign results back
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;
#endif
                ctorParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), parameterType);
            }

            NewExpression construct = Expression.New(
                ctor, // constructor info
                ctorParameters); // arguments cast to target types

            LambdaExpression lambda = Expression.Lambda<AnyCtor>(
                Expression.Convert(construct, Reflector.ObjectType), // return type converted to object
                argumentsParameter);
            return lambda.Compile();
        }

        [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Expression.New does not write the parameters")]
        private protected override Delegate CreateGenericInitializer()
        {
            ConstructorInfo ctor = (ConstructorInfo)MemberInfo;
            // TODO
            //if (ctor.IsStatic)
            //    Throw.InvalidOperationException(Res.ReflectionInstanceConstructorExpected);
            if (ParameterTypes.Length > 4 || ParameterTypes.Any(p => p.IsByRef))
                Throw.NotSupportedException(Res.ReflectionCtorGenericNotSupported);
            if (ParameterTypes.FirstOrDefault(p => p.IsPointer) is Type pointerParam)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(pointerParam));

            var parameters = new ParameterExpression[ParameterTypes.Length];
            for (int i = 0; i < parameters.Length; i++)
                parameters[i] = Expression.Parameter(ParameterTypes[i], $"param{i + 1}");
            NewExpression construct = Expression.New(ctor, parameters);
            Type delegateType = (ParameterTypes.Length switch
            {
                0 => typeof(Func<>),
                1 => typeof(Func<,>),
                2 => typeof(Func<,,>),
                3 => typeof(Func<,,,>),
                4 => typeof(Func<,,,,>),
                _ => Throw.InternalError<Type>("Unexpected number of parameters")
            }).GetGenericType(ParameterTypes.Concat(new[] { ctor.DeclaringType }).ToArray());
            LambdaExpression lambda = Expression.Lambda(delegateType, construct, parameters);
            return lambda.Compile();
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ContractAnnotation("=> halt"), DoesNotReturn]
        private void PostValidate(object?[]? parameters, Exception exception)
        {
            if (ParameterTypes.Length > 0)
            {
                if (parameters == null)
                    Throw.ArgumentNullException(Argument.parameters, Res.ArgumentNull);
                if (parameters.Length != ParameterTypes.Length)
                    Throw.ArgumentException(Argument.parameters, Res.ReflectionParametersInvalid);
                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    if (!ParameterTypes[i].CanAcceptValue(parameters[i]))
                        Throw.ArgumentException(Argument.parameters, Res.ReflectionParametersInvalid);
                }
            }

            ThrowIfSecurityConflict(exception);

            // exceptions from the method itself: re-throwing the original exception
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        #endregion

        #endregion
    }
}
