using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace KGySoft.Reflection
{
    /// <summary>
    /// Field accessor class for getting and setting fields via dynamic delegates.
    /// </summary>
    public sealed class FieldAccessor: MemberAccessor
    {
        /// <summary>
        /// Represents a non-generic setter that can be used for any fields.
        /// </summary>
        private delegate void FieldSetter(object instance, object value);

        /// <summary>
        /// Represents a non-generic getter that can be used for any fields.
        /// </summary>
        private delegate object FieldGetter(object instance);

        private FieldGetter getter;
        private FieldSetter setter;

        /// <summary>
        /// The field getter delegate.
        /// </summary>
        private FieldGetter Getter
        {
            get
            {
                if (getter == null)
                {
                    getter = CreateGetter();
                }
                return getter;
            }
        }

        private FieldGetter CreateGetter()
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");

            FieldInfo field = (FieldInfo)MemberInfo;
            MemberExpression member = Expression.Field(
                field.IsStatic ? null : Expression.Convert(instanceParameter, DeclaringType), // (TInstance)instance
                field);

            LambdaExpression lambda = Expression.Lambda<FieldGetter>(
                Expression.Convert(member, typeof(object)), // object return type
                instanceParameter); // instance (object)
            return (FieldGetter)lambda.Compile();
        }

        /// <summary>
        /// The field setter delegate.
        /// </summary>
        private FieldSetter Setter
        {
            get
            {
                if (setter == null)
                {
                    if (IsConst)
                        throw new InvalidOperationException(Res.Get(Res.SetConstantField, DeclaringType, MemberInfo));
                    setter = CreateSetter();
                }
                return setter;
            }
        }

        private FieldSetter CreateSetter()
        {
            FieldInfo field = (FieldInfo)MemberInfo;

            // .NET 4.0: Using Expression.Assign (does not work for struct instance fields)
            // http://scmccart.wordpress.com/2009/10/08/c-4-0-exposer-an-evil-dynamicobject/
            //# var param = Expression.Parameter(this.Type, “obj”);
            //#             var value = Expression.Parameter(typeof(object), “val”);
            //#  
            //#             var lambda = Expression.Lambda<Action<T, object>>(
            //#                 Expression.Assign(
            //#                     Expression.Field(param, fieldInfo),
            //#                     Expression.Convert(value, fieldInfo.FieldType)),
            //#                 param, value);
            //#  
            //#             return lambda.Compile();

            // The always working solution: dynamic method

            DynamicMethod dm = new DynamicMethod(String.Format("<SetField>__{0}", field.Name), // setter method name
                typeof(void), // return type
                new Type[] { typeof(object), typeof(object) }, DeclaringType, true); // instance and value parameters

            ILGenerator il = dm.GetILGenerator();

            // if instance field, then processing instance parameter
            if (!field.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)
                // casting object instance to target type
                if (DeclaringType.IsValueType)
                {
                    // Note: this is a tricky solution that cannot be made in C#:
                    // We are just unboxing the value type without storing it in a typed local variable
                    // This makes possible to preserve the modified content of a value type without using ref parameter
                    il.Emit(OpCodes.Unbox, DeclaringType); // unboxing the instance

                    // If instance parameter was a ref parameter, then it should be unboxed into a local variable:
                    //LocalBuilder typedInstance = il.DeclareLocal(DeclaringType);
                    //il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)
                    //il.Emit(OpCodes.Ldind_Ref); // as a reference - in dm instance parameter must be defined as: typeof(object).MakeByRefType()
                    //il.Emit(OpCodes.Unbox_Any, DeclaringType); // unboxing the instance
                    //il.Emit(OpCodes.Stloc_0); // saving value into 0. local
                    //il.Emit(OpCodes.Ldloca_S, typedInstance); 
                }
                else
                {
                    il.Emit(OpCodes.Castclass, DeclaringType);

                    // If instance parameter was a ref parameter, then it should be unboxed into a local variable:
                    //LocalBuilder typedInstance = il.DeclareLocal(DeclaringType);
                    //il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)
                    //il.Emit(OpCodes.Ldind_Ref); // as a reference - in dm instance parameter must be defined as: typeof(object).MakeByRefType()
                    //il.Emit(OpCodes.Castclass, DeclaringType); // casting the instance
                    //il.Emit(OpCodes.Stloc_0); // saving value into 0. local
                    //il.Emit(OpCodes.Ldloca_S, typedInstance); 
                }
            }

            // processing 1st argument: value parameter
            il.Emit(OpCodes.Ldarg_1);
            // casting object value to field type
            if (field.FieldType.IsValueType)
                il.Emit(OpCodes.Unbox_Any, field.FieldType);
            else
                il.Emit(OpCodes.Castclass, field.FieldType);

            // processing assignment
            if (field.IsStatic)
                il.Emit(OpCodes.Stsfld, field);
            else
                il.Emit(OpCodes.Stfld, field);

            // returning without return value
            il.Emit(OpCodes.Ret);

            return (FieldSetter)dm.CreateDelegate(typeof(FieldSetter));
        }

        /// <summary>
        /// Gets whether the field is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return ((FieldInfo)MemberInfo).IsInitOnly; }
        }

        /// <summary>
        /// Gets whether the field is a constant.
        /// </summary>
        public bool IsConst
        {
            get { return ((FieldInfo)MemberInfo).IsLiteral; }
        }

        /// <summary>
        /// Creates a new FieldAccessor.
        /// </summary>
        private FieldAccessor(FieldInfo field, Type declaringType) :
            base(field, declaringType, null)
        {
        }

        /// <summary>
        /// Creates an accessor for a field.
        /// </summary>
        public static FieldAccessor GetFieldAccessor(FieldInfo field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field), Res.Get(Res.ArgumentNull));
            if (CachingEnabled)
                return (FieldAccessor)GetCreateAccessor(field);
            else
                return CreateFieldAccessor(field);
        }

        /// <summary>
        /// Non-caching version of field accessor creation.
        /// </summary>
        internal static FieldAccessor CreateFieldAccessor(FieldInfo field)
        {
            // late-initialization of MemberInfo to avoid caching
            return new FieldAccessor(null, field.DeclaringType) { MemberInfo = field };
        }

        /// <summary>
        /// Sets the field.
        /// In case of a static static field <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// </summary>
        /// <remarks>
        /// <note>
        /// First setting of a field can be even slower than using <see cref="FieldInfo.SetValue(object,object)"/> of System.Reflection
        /// but further calls are much more fast.
        /// </note>
        /// </remarks>
        public void Set(object instance, object value)
        {
            Setter.Invoke(instance, value);
        }

        /// <summary>
        /// Gets and returns the value of a field.
        /// In case of a static static field <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// </summary>
        /// <remarks>
        /// <note>
        /// Getting a field value at first time can be even slower than using <see cref="FieldInfo.GetValue(object)"/> of System.Reflection
        /// but further calls are much more fast.
        /// </note>
        /// </remarks>
        public object Get(object instance)
        {
            return Getter.Invoke(instance);
        }
    }
}
