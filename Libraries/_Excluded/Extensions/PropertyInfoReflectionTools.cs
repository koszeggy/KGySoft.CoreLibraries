using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace KGySoft.Libraries.Reflection
{
    /// <summary>
    /// Contains reflection extension methods on <see cref="PropertyInfo"/> class.
    /// </summary>
    public static class PropertyInfoReflectionTools
    {
        #region SetProperty

        /// <summary>
        /// Sets a property based on <see cref="PropertyInfo"/> medatada given in <paramref name="property"/> parameter.
        /// </summary>
        /// <param name="instance">If <paramref name="property"/> to set is an instance property, then this parameter should
        /// contain the object instance on which the property setting should be performed.</param>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The desired new value of the property</param>
        /// <param name="indexerParameters">Indexer parameters if <paramref name="property"/> is an indexer. Otherwise, this parameter is omitted.</param>
        /// <param name="way">Preferred access mode. Usable ways: <see cref="ReflectionWays.Auto"/>, <see cref="ReflectionWays.SystemReflection"/>, <see cref="ReflectionWays.DynamicDelegate"/>
        /// <see cref="ReflectionWays.Auto"/> option uses dynamic delegate mode.
        /// In case of <see cref="ReflectionWays.DynamicDelegate"/> first access of a property is slow but
        /// further calls are faster than the <see cref="ReflectionWays.SystemReflection"/> way.
        /// </param>
        /// <remarks>
        /// <note>
        /// To preserve the changed inner state of a value type
        /// you must pass your struct in <paramref name="instance"/> parameter as an <see cref="object"/>.
        /// </note>
        /// </remarks>
        /// <seealso cref="Reflector"/>
        /// <seealso cref="Reflector.SetProperty(object,System.Reflection.PropertyInfo,object,KGySoft.Libraries.Reflection.ReflectionWays,object[])"/>
        public static void SetProperty(this PropertyInfo property, object instance, object value, ReflectionWays way, params object[] indexerParameters)
        {
            Reflector.SetProperty(instance, property, value, way, indexerParameters);
        }

        /// <summary>
        /// Sets a property based on <see cref="PropertyInfo"/> medatada given in <paramref name="property"/> parameter.
        /// </summary>
        /// <param name="instance">If <paramref name="property"/> to set is an instance property, then this parameter should
        /// contain the object instance on which the property setting should be performed.</param>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The desired new value of the property</param>
        /// <seealso cref="Reflector"/>
        /// <seealso cref="Reflector.SetProperty(object,System.Reflection.PropertyInfo,object)"/>
        public static void SetProperty(this PropertyInfo property, object instance, object value)
        {
            Reflector.SetProperty(instance, property, value, ReflectionWays.Auto);
        }

        #endregion

        #region GetProperty

        /// <summary>
        /// Gets a property based on <see cref="PropertyInfo"/> medatada given in <paramref name="property"/> parameter.
        /// </summary>
        /// <param name="instance">If <paramref name="property"/> to get is an instance property, then this parameter should
        /// contain the object instance on which the property getting should be performed.</param>
        /// <param name="property">The property to get.</param>
        /// <param name="indexerParameters">Indexer parameters if <paramref name="property"/> is an indexer. Otherwise, this parameter is omitted.</param>
        /// <param name="way">Preferred reflection mode. Usable ways: <see cref="ReflectionWays.Auto"/>, <see cref="ReflectionWays.SystemReflection"/>, <see cref="ReflectionWays.DynamicDelegate"/>.
        /// Auto option uses dynamic delegate mode. In case of dynamic delegate mode first access of a property is slow but
        /// further accesses are faster than the system reflection way.
        /// </param>
        /// <returns>The value of the property.</returns>
        /// <remarks>
        /// <note>
        /// If the property getter changes the inner state of the instance, then to preserve the changed inner state of a value type
        /// you must pass your struct in <paramref name="instance"/> parameter as an <see cref="object"/>.
        /// </note>
        /// </remarks>
        /// <seealso cref="Reflector"/>
        /// <seealso cref="Reflector.GetProperty(object,System.Reflection.PropertyInfo,KGySoft.Libraries.Reflection.ReflectionWays,object[])"/>
        public static object GetProperty(this PropertyInfo property, object instance, ReflectionWays way, params object[] indexerParameters)
        {
            return Reflector.GetProperty(instance, property, way, indexerParameters);
        }

        /// <summary>
        /// Gets a property based on <see cref="PropertyInfo"/> medatada given in <paramref name="property"/> parameter.
        /// </summary>
        /// <param name="instance">If <paramref name="property"/> to get is an instance property, then this parameter should
        /// contain the object instance on which the property getting should be performed.</param>
        /// <param name="property">The property to get.</param>
        /// <returns>The value of the property.</returns>
        /// <seealso cref="Reflector"/>
        /// <seealso cref="Reflector.GetProperty(object,System.Reflection.PropertyInfo)"/>
        public static object GetProperty(this PropertyInfo property, object instance)
        {
            return Reflector.GetProperty(instance, property, ReflectionWays.Auto, null);
        }

        #endregion

        #region Accessor

        /// <summary>
        /// Gets an accessor for the <paramref name="property"/> that provides faster
        /// property access than <see cref="PropertyInfo"/>.
        /// </summary>
        public static PropertyAccessor GetPropertyAccessor(this PropertyInfo property)
        {
            return PropertyAccessor.GetPropertyAccessor(property);
        }

        #endregion
    }
}
