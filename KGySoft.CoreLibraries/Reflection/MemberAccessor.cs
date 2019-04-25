#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MemberAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using KGySoft.Collections;

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
        #region Fields

        /// <summary>
        /// This locks also the loader method but this is OK because a new accessor creation is fast.
        /// </summary>
        private static readonly IThreadSafeCacheAccessor<MemberInfo, MemberAccessor> accessorCache = new Cache<MemberInfo, MemberAccessor>(CreateAccessor, 8192).GetThreadSafeAccessor(true);

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the reflection member info of the accessed member.
        /// </summary>
        public MemberInfo MemberInfo { get; }

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets the type of parameters of the accessed member in the reflected type.
        /// </summary>
        protected Type[] ParameterTypes { get; }

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Protected constructor for the abstract <see cref="MemberAccessor"/>.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> for which the accessor is created.</param>
        /// <param name="parameterTypes">A <see cref="Type"/> array of member parameters (method/constructor/indexer)</param>
        protected MemberAccessor(MemberInfo member, Type[] parameterTypes)
        {
            MemberInfo = member ?? throw new ArgumentNullException(nameof(member), Res.ArgumentNull);
            ParameterTypes = parameterTypes ?? Type.EmptyTypes;
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
        protected static MemberAccessor GetCreateAccessor(MemberInfo memberInfo) => accessorCache[memberInfo];

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
            MethodInfo method = member as MethodInfo;
            if (method != null)
                return MethodAccessor.CreateAccessor(method);

            // property
            PropertyInfo property = member as PropertyInfo;
            if (property != null)
                return PropertyAccessor.CreateAccessor(property);

            // constructor (parameterless/parameterized)
            if (member is Type || member is ConstructorInfo)
                return CreateInstanceAccessor.CreateAccessor(member);

            // field
            FieldInfo field = member as FieldInfo;
            if (field != null)
                return FieldAccessor.CreateAccessor(field);

            throw new NotSupportedException(Res.ReflectionNotSupportedMemberType(member.MemberType));
        }

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="MemberAccessor"/>.
        /// </summary>
        /// <param name="obj">The object to compare with the current <see cref="MemberAccessor"/>.</param>
        /// <returns><see langword="true"/>&#160;if the specified object is equal to the current <see cref="MemberAccessor"/>; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj) => obj is MemberAccessor other && Equals(other.MemberInfo, MemberInfo);

        /// <summary>
        /// Gets a hash code for the current <see cref="MemberAccessor"/> instance.
        /// </summary>
        /// <returns>A hash code for the current <see cref="MemberAccessor"/>.</returns>
        public override int GetHashCode() => MemberInfo != null ? MemberInfo.GetHashCode() : 0;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="MemberAccessor"/>.
        /// </summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString() => MemberInfo != null ? MemberInfo.MemberType + ": " + MemberInfo : base.ToString();

        #endregion

        #region Internal Methods

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
        internal /*private protected*/ DynamicMethod CreateMethodInvokerAsDynamicMethod(MethodBase methodBase, DynamicMethodOptions options)
        {
            if (methodBase == null)
                throw new ArgumentNullException(nameof(methodBase), Res.ArgumentNull);
            Type declaringType = methodBase.DeclaringType;
            if (declaringType == null)
                throw new ArgumentException(Res.ReflectionDeclaringTypeExpected, nameof(methodBase));
            MethodInfo method = methodBase as MethodInfo;
            ConstructorInfo ctor = methodBase as ConstructorInfo;
            if (method == null && ctor == null)
                throw new ArgumentException(Res.ReflectionInvalidMethodBase, nameof(methodBase));

            bool treatCtorAsMethod = (options & DynamicMethodOptions.TreatCtorAsMethod) != DynamicMethodOptions.None;
            Type returnType = method != null ? method.ReturnType : treatCtorAsMethod ? Reflector.VoidType : declaringType;
            Type dmReturnType = returnType == Reflector.VoidType ? Reflector.VoidType : Reflector.ObjectType;

            List<Type> methodParameters = new List<Type>();
            string methodName;

            if (ctor != null && !treatCtorAsMethod)
            {
                methodName = $"<Create>__{declaringType.Name}";
                methodParameters.Add(typeof(object[])); // ctor parameters
            }
            else
            {
                methodName = $"<RunMethod>__{methodBase.Name}";
                methodParameters.Add(Reflector.ObjectType); // instance parameter

                // not a property setter
                if ((options & DynamicMethodOptions.TreatAsPropertySetter) == DynamicMethodOptions.None)
                {
                    if ((options & DynamicMethodOptions.OmitParameters) == DynamicMethodOptions.None)
                        methodParameters.Add(typeof(object[])); // method parameters
                }
                // property setter
                else
                {
                    methodParameters.Add(Reflector.ObjectType); // value
                    if (ParameterTypes.Length > 0)
                        methodParameters.Add(typeof(object[])); // indexer parameters
                }
            }

            DynamicMethod dm = new DynamicMethod(methodName, // method name
                    dmReturnType, // return type
                    methodParameters.ToArray(), // parameters
                    declaringType, true); // owner

            ILGenerator il = dm.GetILGenerator();

            // generating local variables for ref/out parameters and initializing ref parameters
            if ((options & DynamicMethodOptions.HandleByRefParameters) != DynamicMethodOptions.None)
            {
                ParameterInfo[] parameters = methodBase.GetParameters();
                for (int i = 0, localsIndex = 0; i < ParameterTypes.Length; i++)
                {
                    if (!ParameterTypes[i].IsByRef)
                        continue;

                    Type paramType = ParameterTypes[i].GetElementType();

                    // ReSharper disable once AssignNullToNotNullAttribute - not null because of the if above
                    il.DeclareLocal(paramType);
                        
                    // initializing locals of ref (non-out) parameters
                    if (!parameters[i].IsOut)
                    {
                        il.Emit(method != null || treatCtorAsMethod ? OpCodes.Ldarg_1 : OpCodes.Ldarg_0); // loading parameters argument
                        il.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                        il.Emit(OpCodes.Ldelem_Ref); // loading the pointed element in arguments
                        il.Emit(paramType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramType);
                        il.Emit(OpCodes.Stloc, localsIndex); // storing value in local variable
                    }

                    localsIndex++;
                }
            }

            LocalBuilder returnValue = null;
            // return value is the last local variable
            if (returnType != Reflector.VoidType)
                returnValue = il.DeclareLocal(returnType);

            // if instance method:
            if ((method != null && !method.IsStatic) || treatCtorAsMethod)
            {
                il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)
                if (declaringType.IsValueType)
                {
                    // Note: this is a tricky solution that could not be made in C#:
                    // We are just unboxing the value type without storing it in a typed local variable
                    // This makes possible to preserve the modified content of a value type without using ref parameter
                    il.Emit(OpCodes.Unbox, declaringType); // unboxing the instance

                    // If instance parameter was a ref parameter, then it should be unboxed into a local variable:
                    //LocalBuilder unboxedInstance = il.DeclareLocal(declaringType);
                    //il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)
                    //il.Emit(OpCodes.Ldind_Ref); // as a reference - in dm instance parameter must be defined as: Reflector.ObjectType.MakeByRefType()
                    //il.Emit(OpCodes.Unbox_Any, declaringType); // unboxing the instance
                    //il.Emit(OpCodes.Stloc_0); // saving value into 0. local
                    //il.Emit(OpCodes.Ldloca_S, unboxedInstance);
                }
            }

            // loading parameters (property setter: indexer parameters)
            for (int i = 0, localsIndex = 0; i < ParameterTypes.Length; i++)
            {
                // ref/out parameters: from local variables
                if (ParameterTypes[i].IsByRef)
                {
                    il.Emit(OpCodes.Ldloca, localsIndex++); // loading address of local variable
                }
                // normal parameters: from object[] parameters argument
                else
                {
                    il.Emit(ctor != null ? OpCodes.Ldarg_0 : (options & DynamicMethodOptions.TreatAsPropertySetter) == DynamicMethodOptions.None ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2); // loading parameters argument
                    il.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                    il.Emit(OpCodes.Ldelem_Ref); // loading the pointed element in arguments
                    il.Emit(ParameterTypes[i].IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, ParameterTypes[i]);
                }
            }

            // property value is the last parameter in a setter method
            if ((options & DynamicMethodOptions.TreatAsPropertySetter) != DynamicMethodOptions.None)
            {
                PropertyInfo pi = MemberInfo as PropertyInfo;
                if (pi == null)
                    throw new InvalidOperationException(Res.ReflectionCannotTreatPropertySetter);
                il.Emit(OpCodes.Ldarg_1); // loading value parameter (always the 1st param in setter delegate because static properties are set by expressions)
                il.Emit(pi.PropertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, pi.PropertyType);
            }

            if (ctor != null)
            {
                if (treatCtorAsMethod)
                    // calling the constructor as method
                    il.Emit(ctor.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, ctor);
                else
                    // invoking the constructor
                    il.Emit(OpCodes.Newobj, ctor);
            }
            else
                // calling the method
                il.Emit(methodBase.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);

            // If instance parameter was a ref parameter, then local variable should be boxed back:
            //il.Emit(OpCodes.Ldarg_0); // loading instance parameter
            //il.Emit(OpCodes.Ldloc_0); // loading unboxedInstance local variable
            //il.Emit(OpCodes.Box, declaringType); // boxing
            //il.Emit(OpCodes.Stind_Ref); // storing the boxed object value

            // assigning back ref/out parameters
            if ((options & DynamicMethodOptions.HandleByRefParameters) != DynamicMethodOptions.None)
            {
                for (int i = 0, localsIndex = 0; i < ParameterTypes.Length; i++)
                {
                    if (!ParameterTypes[i].IsByRef)
                        continue;
                    Type paramType = ParameterTypes[i].GetElementType();
                    il.Emit(ctor != null ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1); // loading parameters argument
                    il.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                    il.Emit(OpCodes.Ldloc, localsIndex++); // loading local variable
                    
                    // ReSharper disable once PossibleNullReferenceException - not null because of the if above
                    if (paramType.IsValueType)
                        il.Emit(OpCodes.Box, paramType); // boxing value type into object
                    il.Emit(OpCodes.Stelem_Ref); // storing the variable into the pointed array index
                }
            }

            // setting return value
            if (returnValue != null)
            {
                il.Emit(OpCodes.Stloc, returnValue); // storing return value to local variable

                il.Emit(OpCodes.Ldloc, returnValue); // loading return value from its local variable
                if (returnType.IsValueType)
                    il.Emit(OpCodes.Box, returnType); // boxing if value type
            }

            // returning
            il.Emit(OpCodes.Ret);
            return dm;
        }

        #endregion

        #endregion

        #endregion
    }
}
