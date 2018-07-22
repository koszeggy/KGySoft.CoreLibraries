using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KGySoft.Libraries.Reflection
{
    /// <summary>
    /// Contains reflection extension methods on <see cref="object"/> class.
    /// </summary>
    public static class ObjectReflectionTools
    {
        #region SetProperty

        /// <summary>
        /// Sets an instance property based on a property name given in <paramref name="propertyName"/> parameter.
        /// Property can refer to either public or non-public properties. To avoid ambiguity (in case of indexers), this method gets
        /// all of the properties of the same name and chooses the first one to which provided <paramref name="indexerParameters"/> fit.
        /// </summary>
        /// <param name="instance">The object instance on which the property should be set.</param>
        /// <param name="propertyName">The property name to set.</param>
        /// <param name="value">The new desired value of the property to set.</param>
        /// <param name="way">Preferred access mode.
        /// <see cref="ReflectionWays.Auto"/> option uses dynamic delegate mode in case of reference types, while uses system reflection mode in case of value types.
        /// In case of <see cref="ReflectionWays.DynamicDelegate"/> first access of a property is slow but
        /// further calls are faster than the <see cref="ReflectionWays.SystemReflection"/> or <see cref="ReflectionWays.TypeDescriptor"/> way.
        /// On components you may want to use the <see cref="ReflectionWays.TypeDescriptor"/> way to trigger property change events and to make possible to roll back
        /// changed values on error.
        /// </param>
        /// <param name="indexerParameters">Index parameters if the property to set is an indexer.</param>
        /// <remarks>
        /// <note>
        /// To preserve the changed inner state of a value type
        /// you must pass your struct in <paramref name="instance"/> parameter as an <see cref="object"/>.
        /// </note>
        /// </remarks>
        /// <seealso cref="Reflector"/>
        /// <seealso cref="Reflector.SetInstancePropertyByName(object,string,object,KGySoft.Libraries.Reflection.ReflectionWays,object[])"/>
        public static void SetInstancePropertyByName(this object instance, string propertyName, object value, ReflectionWays way, params object[] indexerParameters)
        {
            Reflector.SetInstancePropertyByName(instance, propertyName, value, way, indexerParameters);
        }

        /// <summary>
        /// Sets an instance property based on a property name given in <paramref name="propertyName"/> parameter.
        /// Property can refer to either public or non-public properties. To avoid ambiguity (in case of indexers), this method gets
        /// all of the properties of the same name and chooses the first one to which provided <paramref name="indexerParameters"/> fit.
        /// </summary>
        /// <param name="instance">The object instance on which the property should be set.</param>
        /// <param name="propertyName">The property name to set.</param>
        /// <param name="value">The new desired value of the property to set.</param>
        /// <param name="indexerParameters">Index parameters if the property to set is an indexer.</param>
        /// <seealso cref="Reflector"/>
        /// <seealso cref="Reflector.SetInstancePropertyByName(object,string,object,object[])"/>
        public static void SetInstancePropertyByName(this object instance, string propertyName, object value, params object[] indexerParameters)
        {
            Reflector.SetInstancePropertyByName(instance, propertyName, value, indexerParameters);
        }

        /// <summary>
        /// Sets the appropriate indexed member of an object <paramref name="instance"/>. The matching indexer is
        /// selected by the provided <paramref name="indexerParameters"/>.
        /// </summary>
        /// <remarks>
        /// This method does not access indexers of explicit interface implementations. Though <see cref="Array"/>s have
        /// only explicitly implemented interface indexers, this method can be used also for arrays if <paramref name="indexerParameters"/>
        /// can be converted to integer (respectively <see cref="long"/>) values. In case of arrays <paramref name="way"/> is irrelevant.
        /// </remarks>
        /// <param name="instance">The object instance on which the indexer should be set.</param>
        /// <param name="value">The new desired value of the indexer to set.</param>
        /// <param name="way">Preferred access mode.
        /// <see cref="ReflectionWays.Auto"/> option uses dynamic delegate mode.
        /// In case of <see cref="ReflectionWays.DynamicDelegate"/> first access of an indexer is slow but
        /// further calls are faster than the <see cref="ReflectionWays.SystemReflection"/> way.
        /// <see cref="ReflectionWays.TypeDescriptor"/> way cannot be used on indexers.
        /// </param>
        /// <param name="indexerParameters">Index parameters.</param>
        /// <remarks>
        /// <note>
        /// To preserve the changed inner state of a value type
        /// you must pass your struct in <paramref name="instance"/> parameter as an <see cref="object"/>.
        /// </note>
        /// </remarks>
        /// <seealso cref="Reflector"/>
        /// <seealso cref="Reflector.SetIndexedMember(object,object,KGySoft.Libraries.Reflection.ReflectionWays,object[])"/>
        public static void SetIndexedMember(this object instance, object value, ReflectionWays way, params object[] indexerParameters)
        {
            Reflector.SetIndexedMember(instance, value, way, indexerParameters);
        }

        /// <summary>
        /// Sets the appropriate indexed member of an object <paramref name="instance"/>. The matching indexer is
        /// selected by the provided <paramref name="indexerParameters"/>.
        /// </summary>
        /// <remarks>
        /// This method does not access indexers of explicit interface implementations. Though <see cref="Array"/>s have
        /// only explicitly implemented interface indexers, this method can be used also for arrays if <paramref name="indexerParameters"/>
        /// can be converted to integer (respectively <see cref="long"/>) values.
        /// </remarks>
        /// <param name="instance">The object instance on which the indexer should be set.</param>
        /// <param name="value">The new desired value of the indexer to set.</param>
        /// <param name="indexerParameters">Index parameters.</param>
        /// <seealso cref="Reflector"/>
        /// <seealso cref="Reflector.SetIndexedMember(object,object,object[])"/>
        public static void SetIndexedMember(this object instance, object value, params object[] indexerParameters)
        {
            Reflector.SetIndexedMember(instance, value, ReflectionWays.Auto, indexerParameters);
        }

        #endregion
    }
}
