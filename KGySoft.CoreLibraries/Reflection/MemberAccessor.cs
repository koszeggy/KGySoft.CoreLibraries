#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MemberAccessor.cs
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
using System.Collections.Generic;
using System.Diagnostics;
#if !NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis; 
#endif
using System.Linq;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif
using System.Runtime.CompilerServices;
using System.Security;

using KGySoft.Collections;
using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Base class of accessor classes that may access members without system reflection.
    /// See the derived classes for more details.
    /// </summary>
    /// <seealso cref="FieldAccessor"/>
    /// <seealso cref="PropertyAccessor"/>
    /// <seealso cref="MethodAccessor"/>
    /// <seealso cref="CreateInstanceAccessor"/>
    public abstract class MemberAccessor
    {
        #region Delegates

        // NOTE: actually these should be private protected but then help builder emits warnings for the missing documentation
        internal delegate void ReferenceTypeAction<in TInstance>(TInstance instance) where TInstance : class;
        internal delegate void ReferenceTypeAction<in TInstance, in T>(TInstance instance, T arg) where TInstance : class;
        internal delegate void ReferenceTypeAction<in TInstance, in T1, in T2>(TInstance instance, T1 arg1, T2 arg2) where TInstance : class;
        internal delegate void ReferenceTypeAction<in TInstance, in T1, in T2, in T3>(TInstance instance, T1 arg1, T2 arg2, T3 arg3) where TInstance : class;
        internal delegate void ReferenceTypeAction<in TInstance, in T1, in T2, in T3, in T4>(TInstance instance, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where TInstance : class;

        internal delegate void ValueTypeAction<TInstance>(in TInstance instance) where TInstance : struct;
        internal delegate void ValueTypeAction<TInstance, in T>(in TInstance instance, T arg) where TInstance : struct;
        internal delegate void ValueTypeAction<TInstance, in T1, in T2>(in TInstance instance, T1 arg1, T2 arg2) where TInstance : struct;
        internal delegate void ValueTypeAction<TInstance, in T1, in T2, in T3>(in TInstance instance, T1 arg1, T2 arg2, T3 arg3) where TInstance : struct;
        internal delegate void ValueTypeAction<TInstance, in T1, in T2, in T3, in T4>(in TInstance instance, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where TInstance : struct;

        internal delegate TResult ReferenceTypeFunction<in TInstance, out TResult>(TInstance instance) where TInstance : class;
        internal delegate TResult ReferenceTypeFunction<in TInstance, in T, out TResult>(TInstance instance, T arg) where TInstance : class;
        internal delegate TResult ReferenceTypeFunction<in TInstance, in T1, in T2, out TResult>(TInstance instance, T1 arg1, T2 arg2) where TInstance : class;
        internal delegate TResult ReferenceTypeFunction<in TInstance, in T1, in T2, in T3, out TResult>(TInstance instance, T1 arg1, T2 arg2, T3 arg3) where TInstance : class;
        internal delegate TResult ReferenceTypeFunction<in TInstance, in T1, in T2, in T3, in T4, out TResult>(TInstance instance, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where TInstance : class;

        internal delegate TResult ValueTypeFunction<TInstance, out TResult>(in TInstance instance) where TInstance : struct;
        internal delegate TResult ValueTypeFunction<TInstance, in T, out TResult>(in TInstance instance, T arg) where TInstance : struct;
        internal delegate TResult ValueTypeFunction<TInstance, in T1, in T2, out TResult>(in TInstance instance, T1 arg1, T2 arg2) where TInstance : struct;
        internal delegate TResult ValueTypeFunction<TInstance, in T1, in T2, in T3, out TResult>(in TInstance instance, T1 arg1, T2 arg2, T3 arg3) where TInstance : struct;
        internal delegate TResult ValueTypeFunction<TInstance, in T1, in T2, in T3, in T4, out TResult>(in TInstance instance, T1 arg1, T2 arg2, T3 arg3, T4 arg4) where TInstance : struct;

#if NET35
        internal delegate TResult Func<in T1, in T2, in T3, in T4, in T5, out TResult>(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
#endif

        #endregion

        #region Constants

        private const string methodInvokerPrefix = "<InvokeMethod>__";
        private const string ctorInvokerPrefix = "<InvokeCtor>__";

        #endregion

        #region Fields

        private static readonly LockFreeCache<MemberInfo, MemberAccessor> accessorCache = new(CreateAccessor, null, LockFreeCacheOptions.Profile8K);

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the reflection member info of the accessed member.
        /// </summary>
        public MemberInfo MemberInfo { get; }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Gets the type of parameters of the accessed member in the reflected type.
        /// </summary>
        internal Type[] ParameterTypes { get; }

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Protected constructor for the abstract <see cref="MemberAccessor"/>.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> for which the accessor is created.</param>
        /// <param name="parameterTypes">A <see cref="Type"/> array of member parameters (method/constructor/indexer)</param>
        private protected MemberAccessor(MemberInfo member, Type[]? parameterTypes)
        {
            if (member == null!)
                Throw.ArgumentNullException(Argument.member);
            MemberInfo = member;
            ParameterTypes = parameterTypes ?? Type.EmptyTypes;
            Type? pointerType = ParameterTypes.FirstOrDefault(p => p.IsPointer);
            if (pointerType != null)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(pointerType));
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Protected Methods

        /// <summary>
        /// Gets an existing or creates a new <see cref="MemberAccessor"/> for the specified <paramref name="memberInfo"/>.
        /// </summary>
        /// <param name="memberInfo">The <see cref="MemberInfo"/> for which the accessor is to be obtained.</param>
        /// <returns>A <see cref="MemberAccessor"/> instance for the specified <paramref name="memberInfo"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        protected static MemberAccessor GetCreateAccessor(MemberInfo memberInfo) => accessorCache[memberInfo];

        #endregion

        #region Private Protected Methods

        private protected static void ThrowIfSecurityConflict(Exception exception, string? accessorPrefix = null)
        {
            if (exception is not VerificationException ve)
                return;

            try
            {
                var stackTrace = new StackTrace(ve);
                string? methodName = stackTrace.FrameCount > 0 ? stackTrace.GetFrame(0)?.GetMethod()?.Name : null;
                if (methodName == null || !(accessorPrefix == null ? methodName.ContainsAny(methodInvokerPrefix, ctorInvokerPrefix) : methodName.StartsWith(accessorPrefix, StringComparison.Ordinal)))
                    return;

                Throw.NotSupportedException(Res.ReflectionSecuritySettingsConflict, exception);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is NotSupportedException))
            {
                // if we cannot obtain the stack trace we assume the VerificationException is due to the used security settings
                Throw.NotSupportedException(Res.ReflectionSecuritySettingsConflict, exception);
            }
        }

        private protected static IEnumerable<Type> StripByRefTypes(IEnumerable<Type> types) => types.Select(t => t.IsByRef ? t.GetElementType()! : t);

        #endregion

        #region Private Methods

        /// <summary>
        /// This method is associated with the itemLoader of the cache.
        /// </summary>
        /// <remarks>
        /// Note: Make sure that created MemberAccessor is not cached until returning from this method
        /// </remarks>
        private static MemberAccessor CreateAccessor(MemberInfo member)
        {
            // method
            if (member is MethodInfo method)
                return MethodAccessor.CreateAccessor(method);

            // property
            if (member is PropertyInfo property)
                return PropertyAccessor.CreateAccessor(property);

            // constructor (parameterless/parameterized)
            if (member is Type || member is ConstructorInfo)
                return CreateInstanceAccessor.CreateAccessor(member);

            // field
            if (member is FieldInfo field)
                return FieldAccessor.CreateAccessor(field);

            return Throw.NotSupportedException<MemberAccessor>(Res.ReflectionNotSupportedMemberType(member.MemberType));
        }

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="MemberAccessor"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current <see cref="MemberAccessor"/>.</param>
        /// <returns><see langword="true"/> if the specified object is equal to the current <see cref="MemberAccessor"/>; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj) => obj is MemberAccessor other && Equals(other.MemberInfo, MemberInfo);

        /// <summary>
        /// Gets a hash code for the current <see cref="MemberAccessor"/> instance.
        /// </summary>
        /// <returns>A hash code for the current <see cref="MemberAccessor"/>.</returns>
        public override int GetHashCode() => MemberInfo.GetHashCode();

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="MemberAccessor"/>.
        /// </summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString() => MemberInfo.MemberType + ": " + MemberInfo;

        #endregion

        #region Private Protected Methods

#if !NETSTANDARD2_0
        /// <summary>
        /// Gets a <see cref="DynamicMethod"/> that invokes the referred <paramref name="methodBase"/> (method or constructor).
        /// An overridden class may use this to create a delegate optionally.
        /// Return type of the created method is <see cref="object"/> if the method has any kind of return value, otherwise, <see cref="Void"/>.
        /// </summary>
        /// <param name="methodBase">The method or constructor, which invocation should be generated</param>
        /// <param name="options">Options for generation. Affects parameters of generated method and ref/out parameters handling</param>
        /// <returns>
        /// Returns a <see cref="DynamicMethod"/> with given options. Return type of the method
        /// is <see cref="Void"/> if method has no return type, otherwise, <see cref="object"/>.
        /// By default, method parameters are <c>(<see cref="object"/> instance, <see cref="object"/>[] parameters)</c>,
        /// but when <see cref="DynamicMethodOptions.TreatAsPropertySetter"/> is set, then
        /// parameters are either <c>(<see cref="object"/> instance, <see cref="object"/> value)</c>
        /// or <c>(<see cref="object"/> instance, <see cref="object"/> value, <see cref="object"/>[] indexerParameters)</c>.
        /// For constructors, generated parameter is always <c><see cref="object"/>[] parameters</c>.
        /// </returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
        private protected DynamicMethod CreateMethodInvokerAsDynamicMethod(MethodBase methodBase, DynamicMethodOptions options)
        {
            if (methodBase == null!)
                Throw.ArgumentNullException(Argument.methodBase);
            Type? declaringType = methodBase.DeclaringType;
            MethodInfo? method = methodBase as MethodInfo;
            ConstructorInfo? ctor = methodBase as ConstructorInfo;
            bool isStatic = methodBase.IsStatic;
            if (method == null && ctor == null)
                Throw.ArgumentException(Argument.methodBase, Res.ReflectionInvalidMethodBase);
            if (declaringType == null && !isStatic)
                Throw.ArgumentException(Argument.methodBase, Res.ReflectionDeclaringTypeExpected);

            bool isValueType = declaringType?.IsValueType == true;
            bool treatCtorAsMethod = options.HasFlag<DynamicMethodOptions>(DynamicMethodOptions.TreatCtorAsMethod);
            bool returnNullForVoid = options.HasFlag<DynamicMethodOptions>(DynamicMethodOptions.ReturnNullForVoid);
            bool stronglyTyped = options.HasFlag<DynamicMethodOptions>(DynamicMethodOptions.StronglyTyped);
            bool treatAsPropertySetter = options.HasFlag<DynamicMethodOptions>(DynamicMethodOptions.TreatAsPropertySetter);
            bool exactParameters = options.HasFlag<DynamicMethodOptions>(DynamicMethodOptions.ExactParameters);
            Type returnType = method != null ? method.ReturnType : treatCtorAsMethod ? Reflector.VoidType : declaringType!;
            bool isRefReturn = returnType.IsByRef;
            if (isRefReturn)
                returnType = returnType.GetElementType()!;

            Type dmReturnType = returnType == Reflector.VoidType ? returnNullForVoid ? Reflector.ObjectType : Reflector.VoidType
                : stronglyTyped ? returnType
                : Reflector.ObjectType;

            (string methodName, List<Type> methodParameters) = GetNameAndParams();

            DynamicMethod dm = new DynamicMethod(methodName, // method name
                dmReturnType, // return type
                methodParameters.ToArray(), // parameters
                GetOwner(), true); // owner

            ILGenerator il = dm.GetILGenerator();

            // generating local variables for ref/out parameters and initializing ref parameters
            GenerateLocalsForRefParams();

            // if instance method:
            if ((method != null && !isStatic) || treatCtorAsMethod)
            {
                il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)

                if (!stronglyTyped)
                    il.Emit(isValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType!); // unboxing the instance
            }

            // loading parameters for the method call (property setter: indexer parameters)
            LoadParameters();

            // property value is the last parameter in the actual setter method
            if (treatAsPropertySetter)
            {
                PropertyInfo? pi = MemberInfo as PropertyInfo;
                if (pi == null)
                    Throw.InvalidOperationException(Res.ReflectionCannotTreatPropertySetter);

                // loading value parameter
                il.Emit(stronglyTyped && isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);

                if (!stronglyTyped)
                    il.Emit(pi.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, pi.PropertyType);
            }

            if (ctor != null)
            {
                // calling the constructor as method
                if (treatCtorAsMethod)
                    il.Emit(ctor.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, ctor);
                else
                    // invoking the constructor
                    il.Emit(OpCodes.Newobj, ctor);
            }
            else
                // calling the method
                il.Emit(methodBase.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method!);

            // handling ref return
            if (isRefReturn)
            {
                if (returnType.IsValueType)
                    il.Emit(OpCodes.Ldobj, returnType);
                else
                    il.Emit(OpCodes.Ldind_Ref);
            }

            // assigning back ref/out parameters
            AssignRefParams();

            // boxing return value if value type
            if (!stronglyTyped && returnType.IsValueType && returnType != Reflector.VoidType)
                il.Emit(OpCodes.Box, returnType);

            // returning
            if (returnNullForVoid && returnType == Reflector.VoidType)
                il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
            return dm;

            #region Local Methods

            (string Name, List<Type> Parameters) GetNameAndParams()
            {
                var parameters = new List<Type>();
                string name = methodBase is ConstructorInfo && !treatCtorAsMethod
                    ? ctorInvokerPrefix + declaringType!.Name
                    : methodInvokerPrefix + methodBase.Name;

                // instance parameter
                if (treatCtorAsMethod || methodBase is MethodInfo && (!stronglyTyped || !isStatic))
                    parameters.Add(stronglyTyped ? (isValueType ? declaringType!.MakeByRefType() : declaringType!) : Reflector.ObjectType);

                // property setter: value
                if (treatAsPropertySetter)
                    parameters.Add(stronglyTyped ? ((PropertyInfo)MemberInfo).PropertyType : Reflector.ObjectType);

                // parameters
                if (!exactParameters)
                    parameters.Add(typeof(object[]));
                else
                {
                    Debug.Assert(ParameterTypes.Length <= 4, "More than 4 parameters are not expected for separate parameters");
                    if (stronglyTyped)
                        parameters.AddRange(StripByRefTypes(ParameterTypes));
                    else
                        for (int i = 0; i < ParameterTypes.Length; i++)
                            parameters.Add(Reflector.ObjectType);
                }

                return (name, parameters);
            }

            void GenerateLocalsForRefParams()
            {
                // No locals are needed for strongly typed parameters as we can pass the reference of the parameters directly
                if (stronglyTyped)
                    return;

                ParameterInfo[] parameters = methodBase.GetParameters();
                int paramsOffset = methodBase is MethodInfo || treatCtorAsMethod ? 1 : 0;

                for (int i = 0, localsIndex = 0; i < ParameterTypes.Length; i++)
                {
                    if (!ParameterTypes[i].IsByRef)
                        continue;

                    Type paramType = ParameterTypes[i].GetElementType()!;
                    il.DeclareLocal(paramType);

                    // initializing locals of ref (non-out) parameters
                    if (!parameters[i].IsOut)
                    {
                        // from the object[] parameters
                        if (!exactParameters)
                        {
                            EmitLdarg(il, paramsOffset); // loading parameters argument
                            il.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                            il.Emit(OpCodes.Ldelem_Ref); // loading the pointed element in arguments
                        }
                        // from separate parameters
                        else
                            EmitLdarg(il, paramsOffset + i); // loading parameter

                        if (!stronglyTyped)
                            il.Emit(paramType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramType);
                        il.Emit(OpCodes.Stloc, localsIndex); // storing value in local variable
                    }

                    localsIndex++;
                }
            }

            void LoadParameters()
            {
                int paramsOffset = stronglyTyped ? isStatic || methodBase is ConstructorInfo ? 0 : 1
                    : methodBase is ConstructorInfo && !treatCtorAsMethod ? 0
                    : !treatAsPropertySetter ? 1
                    : 2;

                for (int i = 0, localsIndex = 0; i < ParameterTypes.Length; i++)
                {
                    // ref/out parameters: by the address of the local variables or parameters
                    if (ParameterTypes[i].IsByRef)
                    {
                        if (stronglyTyped)
                            il.Emit(OpCodes.Ldarga, i + paramsOffset);
                        else
                            il.Emit(OpCodes.Ldloca, localsIndex++);

                        continue;
                    }

                    // normal parameters from object[] parameters argument
                    if (!exactParameters)
                    {
                        EmitLdarg(il, paramsOffset); // loading parameters argument
                        il.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                        il.Emit(OpCodes.Ldelem_Ref); // loading the pointed element in arguments
                    }
                    // normal parameters as separate parameters
                    else
                        EmitLdarg(il, paramsOffset + i); // loading parameter

                    if (!stronglyTyped)
                        il.Emit(ParameterTypes[i].IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, ParameterTypes[i]);
                }
            }

            void AssignRefParams()
            {
                // Assigning back ref/out parameters if object[] parameters are used
                if (exactParameters)
                    return;

                Debug.Assert(!stronglyTyped, "Strongly typed generation is expected with exact parameters");
                for (int i = 0, localsIndex = 0; i < ParameterTypes.Length; i++)
                {
                    if (!ParameterTypes[i].IsByRef)
                        continue;

                    Type paramType = ParameterTypes[i].GetElementType()!;
                    il.Emit(methodBase is MethodInfo || treatCtorAsMethod ? OpCodes.Ldarg_1 : OpCodes.Ldarg_0); // loading parameters argument
                    il.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                    il.Emit(OpCodes.Ldloc, (short)localsIndex); // loading local variable
                    ++localsIndex;

                    // ReSharper disable once PossibleNullReferenceException - not null because of the if above
                    if (paramType.IsValueType)
                        il.Emit(OpCodes.Box, paramType); // boxing value type into object
                    il.Emit(OpCodes.Stelem_Ref); // storing the variable into the pointed array index
                }
            }

            static void EmitLdarg(ILGenerator il, int index)
            {
                switch (index)
                {
                    case 0:
                        il.Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        il.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        il.Emit(OpCodes.Ldarg, (short)index);
                        break;
                }
            }

            #endregion
        }

        private protected Type GetOwner() => MemberInfo.DeclaringType?.IsInterface != false ? Reflector.ObjectType : MemberInfo.DeclaringType;
#endif

        #endregion

        #endregion

        #endregion
    }
}
