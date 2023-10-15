#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParameterizedCreateInstanceAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
#if NETSTANDARD2_0
using System.Linq.Expressions;
#endif
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif

using KGySoft.CoreLibraries;

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

        private protected override Func<object?[]?, object> CreateGeneralInitializer()
        {
            ConstructorInfo ctor = (ConstructorInfo)MemberInfo;
            // TODO
            //if (ctor.IsStatic)
            //    Throw.InvalidOperationException(Res.ReflectionInstanceConstructorExpected);

#if NETSTANDARD2_0
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
            var ctorParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                Type parameterType = ParameterTypes[i];

                // This just avoids error when ref parameters are used but does not assign results back
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;

                ctorParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), parameterType);
            }

            NewExpression construct = Expression.New(
                ctor, // constructor info
                ctorParameters); // arguments cast to target types

            var lambda = Expression.Lambda<Func<object?[]?, object>>(
                Expression.Convert(construct, Reflector.ObjectType), // return type converted to object
                argumentsParameter);
            return lambda.Compile();
#else
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(ctor, DynamicMethodOptions.None);
            return (Func<object?[]?, object>)dm.CreateDelegate(typeof(Func<object?[]?, object>));
#endif
        }

        private protected override Delegate CreateNonGenericInitializer()
        {
            ConstructorInfo ctor = (ConstructorInfo)MemberInfo;
            // TODO
            //if (ctor.IsStatic)
            //    Throw.InvalidOperationException(Res.ReflectionInstanceConstructorExpected);

            Type delegateType = ParameterTypes.Length switch
            {
                0 => typeof(Func<object?>),
                1 => typeof(Func<object?, object?>),
                2 => typeof(Func<object?, object?, object?>),
                3 => typeof(Func<object?, object?, object?, object?>),
                4 => typeof(Func<object?, object?, object?, object?, object?>),
                _ => Throw.InternalError<Type>("Unexpected number of parameters")
            };

#if NETSTANDARD2_0
    throw new NotImplementedException("CreateNonGenericSpecializedInvoker - expressions");
#else
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(ctor, DynamicMethodOptions.ExactParameters);
            return dm.CreateDelegate(delegateType);
#endif
        }

        [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Expression.New does not write the parameters")]
        private protected override Delegate CreateGenericInitializer()
        {
            ConstructorInfo ctor = (ConstructorInfo)MemberInfo;
            // TODO
            //if (ctor.IsStatic)
            //    Throw.InvalidOperationException(Res.ReflectionInstanceConstructorExpected);
            if (ParameterTypes.Length > 4)
                Throw.NotSupportedException(Res.ReflectionCtorGenericNotSupported);
            if (ParameterTypes.FirstOrDefault(p => p.IsPointer) is Type pointerParam)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(pointerParam));

            Type delegateType = (ParameterTypes.Length switch
            {
                0 => typeof(Func<>),
                1 => typeof(Func<,>),
                2 => typeof(Func<,,>),
                3 => typeof(Func<,,,>),
                4 => typeof(Func<,,,,>),
                _ => Throw.InternalError<Type>("Unexpected number of parameters")
            }).GetGenericType(StripByRefTypes(ParameterTypes).Append(ctor.DeclaringType!).ToArray());

#if NETSTANDARD2_0
            var parameters = new ParameterExpression[ParameterTypes.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                Type parameterType = ParameterTypes[i];

                // This just avoids error when ref parameters are used but does not assign results back
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;

                parameters[i] = Expression.Parameter(parameterType, $"param{i + 1}");
            }

            NewExpression construct = Expression.New(ctor, parameters);
            LambdaExpression lambda = Expression.Lambda(delegateType, construct, parameters);
            return lambda.Compile();
#else
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(ctor, DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return dm.CreateDelegate(delegateType);
#endif
        }

        #endregion
    }
}
