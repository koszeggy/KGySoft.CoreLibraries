using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using KGySoft.Collections;
using KGySoft.CoreLibraries;

namespace KGySoft.Reflection
{
    /// <summary>
    /// Base class of accessor classes that may access members without system reflection.
    /// </summary>
    public abstract class MemberAccessor
    {
        /// <summary>
        /// Options for <see cref="MemberAccessor.CreateMethodInvokerAsDynamicMethod"/> method.
        /// </summary>
        [Flags]
        protected enum DynamicMethodOptions
        {
            /// <summary>
            /// No special handling.
            /// </summary>
            None = 0,

            /// <summary>
            /// Generates local variables for ref/out parameters and assigns them back in the object[] parameters array
            /// </summary>
            HandleByRefParameters = 1,

            /// <summary>
            /// Generates an object value parameter and also an object[] arguments parameter in case of indexers
            /// </summary>
            TreatAsPropertySetter = 1 << 1,

            /// <summary>
            /// Does not emit the object[] parameter for method arguments (for simple property getters)
            /// </summary>
            OmitParameters = 1 << 2,

            /// <summary>
            /// Treats a ConstructorInfo as a regular method
            /// </summary>
            TreatCtorAsMethod = 1 << 3,
        }

        private readonly Type[] parameterTypes;

        /// <summary>
        /// This locks also the loader method but this is OK because a new accessor creation is fast.
        /// </summary>
        private static readonly LockingDictionary<MemberInfo, MemberAccessor> accessorCache = new Cache<MemberInfo, MemberAccessor>(CreateAccessor, 8192).AsThreadSafe();

        ///// <summary>
        ///// Gets or sets the cache size used for caching compiled accessors. Setting size to 0 disables caching.
        ///// </summary>
        //public static int CacheSize
        //{
        //    get
        //    {
        //        return CachingEnabled ? accessorCache.Capacity : 0;
        //    }
        //    set
        //    {
        //        if (value < 0)
        //            throw new ArgumentOutOfRangeException(nameof(value), Res.ArgumentOutOfRange);
        //        else if (value == 0)
        //        {
        //            CachingEnabled = false;
        //            accessorCache.Clear();
        //        }
        //        else
        //        {
        //            CachingEnabled = true;
        //            accessorCache.Capacity = value;
        //        }
        //    }
        //}

        ///// <summary>
        ///// Gets or sets whether caching of compiled <see cref="MemberAccessor"/> instances is enabled.
        ///// Disabling caching is not recommended.
        ///// </summary>
        //public static bool CachingEnabled { get; set; }

        /// <summary>
        /// Gets the reflection member info of the accessed member.
        /// </summary>
        public MemberInfo MemberInfo { get; }

        /// <summary>
        /// This method is associated with itemLoader of cache.
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

        /// <summary>
        /// Gets the type of parameters of the accessed member in the reflected type.
        /// </summary>
        protected Type[] ParameterTypes
        {
            get { return parameterTypes; }
        }

        /// <summary>
        /// Protected constructor for the abstract <see cref="MemberAccessor"/>.
        /// </summary>
        /// <param name="member">Pass null when invoked from non-public constructors to avoid double caching.
        /// You may set member from initializer, too. Public constructors must not allow null member to avoid violating encapsulation</param>
        /// <param name="parameterTypes">A <see cref="Type"/> array of member parameters (method/constructor/indexer)</param>
        protected MemberAccessor(MemberInfo member, Type[] parameterTypes)
        {
            MemberInfo = member ?? throw new ArgumentNullException(nameof(member), Res.ArgumentNull);
            this.parameterTypes = parameterTypes ?? Type.EmptyTypes;
        }

        //static MemberAccessor()
        //{
        //    CachingEnabled = true;
        //}

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="MemberAccessor"/>.
        /// </summary>
        public override bool Equals(object obj)
        {
            MemberAccessor other = obj as MemberAccessor;
            return other != null && Equals(other.MemberInfo, this.MemberInfo);
        }

        /// <summary>
        /// Gets a hash code fot the current <see cref="MemberAccessor"/> instance.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="MemberAccessor"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return MemberInfo != null ? MemberInfo.GetHashCode() : base.GetHashCode();
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="MemberAccessor"/>.
        /// </summary>
        public override string ToString()
        {
            return MemberInfo != null ? MemberInfo.MemberType + ": " + MemberInfo : base.ToString();
        }

        /// <summary>
        /// Gets accessor from cache in thread-safe way.
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        protected static MemberAccessor GetCreateAccessor(MemberInfo memberInfo)
        {
            return accessorCache[memberInfo];
        }

        /// <summary>
        /// Gets a <see cref="DynamicMethod"/> that invokes the referred <paramref name="methodBase"/> (method or constructor).
        /// An overridden class may use this to create a delegate optionally.
        /// Return type of the created method is <see cref="object"/> if the method has any kind of return value, otherwise, <see cref="Void"/>.
        /// </summary>
        /// <param name="methodBase">The method or constructor, which invocation should be generated</param>
        /// <param name="options">Options for generation. Affects parameters of generated method and ref/out parameters handling</param>
        /// <returns>
        /// Returns a <see cref="DynamicMethod"/> with given options. Return value of the method
        /// is <see cref="Void"/> if method has no return type, otherwise, <see cref="object"/>.
        /// By default, method parameters are <c>(<see cref="object"/> instance, <see cref="object"/>[] parameters)</c>,
        /// but when <see cref="DynamicMethodOptions.TreatAsPropertySetter"/> is set, then
        /// parameters are either <c>(<see cref="object"/> instance, <see cref="object"/> value)</c>
        /// or <c>(<see cref="object"/> instance, <see cref="object"/> value, <see cref="object"/>[] indexerParameters)</c>.
        /// For constructors, generated parameter is always <c><see cref="object"/>[] parameters</c>.
        /// </returns>
        protected DynamicMethod CreateMethodInvokerAsDynamicMethod(MethodBase methodBase, DynamicMethodOptions options)
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
            Type returnType = method != null ? method.ReturnType : treatCtorAsMethod ? typeof(void) : declaringType;
            Type dmReturnType = returnType == typeof(void) ? typeof(void) : typeof(object);

            List<Type> methodParameters = new List<Type>();
            string methodName;

            if (ctor != null && !treatCtorAsMethod)
            {
                methodName = String.Format("<Construct>__{0}", declaringType.Name);
                methodParameters.Add(typeof(object[])); // ctor parameters
            }
            else
            {
                methodName = String.Format("<RunMethod>__{0}", methodBase.Name);
                methodParameters.Add(typeof(object)); // instance parameter

                // not a property setter
                if ((options & DynamicMethodOptions.TreatAsPropertySetter) == DynamicMethodOptions.None)
                {
                    if ((options & DynamicMethodOptions.OmitParameters) == DynamicMethodOptions.None)
                        methodParameters.Add(typeof(object[])); // method parameters
                }
                // property setter
                else
                {
                    methodParameters.Add(typeof(object)); // value
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
                    if (ParameterTypes[i].IsByRef)
                    {
                        Type paramType = ParameterTypes[i].GetElementType();
                        il.DeclareLocal(paramType);
                        // initializing locals of ref (non-out) parameters
                        if (!parameters[i].IsOut)
                        {
                            il.Emit(method != null || treatCtorAsMethod ? OpCodes.Ldarg_1 : OpCodes.Ldarg_0); // loading parameters argument
                            il.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                            il.Emit(OpCodes.Ldelem_Ref); // loading the pointed element in arguments
                            if (paramType.IsValueType)
                                il.Emit(OpCodes.Unbox_Any, paramType); // casting parameter as value type
                            else
                                il.Emit(OpCodes.Castclass, paramType); // casting parameter as reference
                            il.Emit(OpCodes.Stloc, localsIndex); // storing value in local variable
                        }
                        localsIndex++;
                    }
                }
            }

            LocalBuilder returnValue = null;
            // return value is the last local variable
            if (returnType != typeof(void))
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
                    //LocalBuilder unboxedInstance = il.DeclareLocal(DeclaringType);
                    //il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)
                    //il.Emit(OpCodes.Ldind_Ref); // as a reference - in dm instance parameter must be defined as: typeof(object).MakeByRefType()
                    //il.Emit(OpCodes.Unbox_Any, DeclaringType); // unboxing the instance
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
                    if (ParameterTypes[i].IsValueType)
                        il.Emit(OpCodes.Unbox_Any, ParameterTypes[i]); // casting parameter as value type
                    else
                        il.Emit(OpCodes.Castclass, ParameterTypes[i]); // casting parameter as reference
                }
            }

            // property value is the last parameter in a setter method
            if ((options & DynamicMethodOptions.TreatAsPropertySetter) != DynamicMethodOptions.None)
            {
                PropertyInfo pi = MemberInfo as PropertyInfo;
                if (pi == null)
                    throw new InvalidOperationException(Res.ReflectionCannotTreatPropertySetter);
                il.Emit(OpCodes.Ldarg_1); // loading value parameter (always the 1st param in setter delegate because static properties are set by expressions)
                if (pi.PropertyType.IsValueType)
                    il.Emit(OpCodes.Unbox_Any, pi.PropertyType); // casting value as value type
                else
                    il.Emit(OpCodes.Castclass, pi.PropertyType); // casting value as reference               
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
            //il.Emit(OpCodes.Box, DeclaringType); // boxing
            //il.Emit(OpCodes.Stind_Ref); // storing the boxed object value

            // assigning back ref/out parameters
            if ((options & DynamicMethodOptions.HandleByRefParameters) != DynamicMethodOptions.None)
            {
                for (int i = 0, localsIndex = 0; i < ParameterTypes.Length; i++)
                {
                    if (ParameterTypes[i].IsByRef)
                    {
                        Type paramType = ParameterTypes[i].GetElementType();
                        il.Emit(ctor != null ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1); // loading parameters argument
                        il.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                        il.Emit(OpCodes.Ldloc, localsIndex++); // loading local variable
                        if (paramType.IsValueType)
                            il.Emit(OpCodes.Box, paramType); // boxing value type into object
                        il.Emit(OpCodes.Stelem_Ref); // storing the variable into the pointed array index
                    }
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
    }
}
