using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace KGySoft.Libraries.Reflection
{
    sealed class ValueTypeActionInvoker: MethodInvoker
    {
        /// <summary>
        /// Represents a non-generic action that can be used for any action methods.
        /// <paramref name="instance"/> is a ref parameter to preserve changed state of value types.
        /// </summary>
        private delegate void ValueTypeAction(ref object instance, object[] parameters);

        /// <summary>
        /// Non-caching internal constructor. Called from cache.
        /// </summary>
        internal ValueTypeActionInvoker(Type instanceType, Type[] parameterTypes)
            : base(null, instanceType, parameterTypes)
        {
        }

        /// <summary>
        /// Invokes the method with given parameters.
        /// </summary>
        /// <param name="instance">The instance on which the method is invoked</param>
        /// <param name="parameters">Method parameters</param>
        /// <returns>Returns alwas null because this class represents a method with void return type.</returns>
        public override object Invoke(object instance, params object[] parameters)
        {
            // Nem jó, mert az új instance elvész, hacsak nem ref az Invoke instance paramétere is!
            ((ValueTypeAction)Invoker)(ref instance, parameters);
            return null;
        }

        protected override Delegate CreateInvoker()
        {
            MethodInfo method = (MethodInfo)MemberInfo;
            DynamicMethod dm = new DynamicMethod(String.Format("<RunMethod>__{0}", method.Name), // method name
                MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard,
                typeof(void), // return type
                new Type[] { typeof(object).MakeByRefType(), // instance parameter
                    typeof(object[]) }, // parameters
                DeclaringType, true); // owner, skip visibility

            ILGenerator il = dm.GetILGenerator();

            // code to write:
            // public static void RunMethod(ref object instance, object[] parameters)
            // {
            //   DeclaringType unboxedInstance = (DeclaringType)instance;
            //   unboxedInstance.method(parameters);
            //   instance = unboxedInstance; // re-boxing
            // }

            LocalBuilder unboxedInstance = il.DeclareLocal(DeclaringType);
            // unboxing instance
            il.Emit(OpCodes.Ldarg_0); // loading 1st argument (instance)
            il.Emit(OpCodes.Ldind_Ref); // as a reference
            il.Emit(OpCodes.Unbox_Any, DeclaringType); // unboxing the instance
            il.Emit(OpCodes.Stloc_0); // saving value into 0. local // same as il.Emit(OpCodes.Stloc_S, unboxedInstance);
            il.Emit(OpCodes.Ldloca_S, unboxedInstance); 

            // loading parameters
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_1); // loading 2nd argument (parameters)
                il.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                il.Emit(OpCodes.Ldelem_Ref); // loading the pointed element in arguments
                if (ParameterTypes[i].IsValueType)
                    il.Emit(OpCodes.Unbox_Any, ParameterTypes[i]); // casting parameter as value type
                else
                    il.Emit(OpCodes.Castclass, ParameterTypes[i]); // casting parameter as reference
            }

            // calling the method
            il.Emit(OpCodes.Callvirt, method);

            // boxing instance back
            il.Emit(OpCodes.Ldarg_0); // loading instance parameter
            il.Emit(OpCodes.Ldloc_0); // loading unboxedInstance local variable
            il.Emit(OpCodes.Box, DeclaringType); // boxing

            il.Emit(OpCodes.Stind_Ref); // storing the boxed object value

            // returning without return value
            il.Emit(OpCodes.Ret);

            return dm.CreateDelegate(typeof(ValueTypeAction));
        }
    }
}
