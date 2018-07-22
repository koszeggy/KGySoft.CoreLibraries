using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace KGySoft.Libraries.Reflection
{
    /// <summary>
    /// Contains reflection extension methods on <see cref="Type"/> class.
    /// </summary>
    public static class TypeReflectionTools
    {
        #region Parsing

        /// <summary>
        /// Parses an object from a string value. In first place tries to parse the type natively.
        /// If native conversion fails but the type has a type converter that can convert from string,
        /// then type converter is used.
        /// </summary>
        /// <param name="type">Type of the instance to create.</param>
        /// <param name="value">Value in string format to parse. If value is <see langword="null"/> and <paramref name="type"/> is a reference type, returns <see langword="null"/>.</param>
        /// <param name="context">An optional context for assigning the value via type converter.</param>
        /// <param name="culture">Appropriate culture needed for number types.</param>
        /// <returns>The parsed value.</returns>
        /// <remarks>
        /// Natively parsed types:
        /// <list type="bullet">
        /// <item><description><see cref="System.Enum"/> based types</description></item>
        /// <item><description><see cref="string"/></description></item>
        /// <item><description><see cref="char"/></description></item>
        /// <item><description><see cref="byte"/></description></item>
        /// <item><description><see cref="sbyte"/></description></item>
        /// <item><description><see cref="short"/></description></item>
        /// <item><description><see cref="ushort"/></description></item>
        /// <item><description><see cref="int"/></description></item>
        /// <item><description><see cref="uint"/></description></item>
        /// <item><description><see cref="long"/></description></item>
        /// <item><description><see cref="ulong"/></description></item>
        /// <item><description><see cref="float"/></description></item>
        /// <item><description><see cref="double"/></description></item>
        /// <item><description><see cref="decimal"/></description></item>
        /// <item><description><see cref="bool"/></description></item>
        /// <item><description><see cref="Type"/></description></item>
        /// <item><description><see cref="DateTime"/></description></item>
        /// <item><description><see cref="DBNull"/></description></item>
        /// <item><description><see cref="Void"/> (only "null" or empty value is accepted)</description></item>
        /// <item><description><see cref="object"/> (value treated as string but value "null" returns null and empty value returns a new <see cref="object"/> instance)</description></item>
        /// <item><description>Any types that can convert their value from System.String via type converters</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">If <paramref name="type"/> is value type, then <paramref name="value"/> must not be <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Parameter <paramref name="value"/> cannot be parsed as <paramref name="type"/></exception>
        /// <exception cref="FormatException">Parameter <paramref name="value"/> cannot be parsed as <paramref name="type"/></exception>
        /// <exception cref="ReflectionException">Parameter <paramref name="value"/> cannot be parsed as <see cref="Type"/> -or- no appropriate <see cref="TypeConverter"/> found to convert <paramref name="value"/> from <see cref="string"/> value.</exception>
        /// <seealso cref="Reflector"/>
        /// <seealso cref="Reflector.Parse(System.Type,string,System.ComponentModel.ITypeDescriptorContext,System.Globalization.CultureInfo)"/>
        public static object Parse(this Type type, string value, ITypeDescriptorContext context, CultureInfo culture)
        {
            return Reflector.Parse(type, value, context, culture);
        }

        /// <summary>
        /// Parses an object from a string value without context using invariant culture. In first place tries to parse the type natively.
        /// If native conversion fails but the type has a type converter that can convert from string,
        /// then type converter is used.
        /// </summary>
        /// <param name="type">Type of the instance to create.</param>
        /// <param name="value">Value in string to parse.</param>
        /// <returns>The parsed value.</returns>
        /// <remarks>
        /// Natively parsed types:
        /// <list type="bullet">
        /// <item><description><see cref="Enum"/> based types</description></item>
        /// <item><description><see cref="string"/></description></item>
        /// <item><description><see cref="char"/></description></item>
        /// <item><description><see cref="byte"/></description></item>
        /// <item><description><see cref="sbyte"/></description></item>
        /// <item><description><see cref="short"/></description></item>
        /// <item><description><see cref="ushort"/></description></item>
        /// <item><description><see cref="int"/></description></item>
        /// <item><description><see cref="uint"/></description></item>
        /// <item><description><see cref="long"/></description></item>
        /// <item><description><see cref="ulong"/></description></item>
        /// <item><description><see cref="float"/></description></item>
        /// <item><description><see cref="double"/></description></item>
        /// <item><description><see cref="decimal"/></description></item>
        /// <item><description><see cref="bool"/></description></item>
        /// <item><description><see cref="DateTime"/></description></item>
        /// <item><description><see cref="Type"/></description></item>
        /// <item><description><see cref="Void"/> (only "null" or empty value is accepted)</description></item>
        /// <item><description><see cref="object"/> (value treated as string but value "null" returns <see langword="null"/> and empty string returns a new <see cref="object"/> instance)</description></item>
        /// <item><description><see cref="DBNull"/> (only "null", "DBNull" or empty value is accepted)</description></item>
        /// <item><description>Any types that can convert their value from System.String via type converters</description></item>
        /// </list>
        /// </remarks>
        /// <seealso cref="Reflector"/>
        /// <seealso cref="Reflector.Parse(System.Type,string)"/>
        public static object Parse(this Type type, string value)
        {
            return Reflector.Parse(type, value, null, CultureInfo.InvariantCulture);
        }

        #endregion

        #region SetProperty

        /// <summary>
        /// Sets a static property based on a property name given in <paramref name="propertyName"/> parameter.
        /// Property can refer to either public or non-public properties.
        /// </summary>
        /// <param name="type">The type that contains the static the property to set.</param>
        /// <param name="propertyName">The property name to set.</param>
        /// <param name="value">The new desired value of the property to set.</param>
        /// <param name="way">Preferred access mode.
        /// Auto option uses dynamic delegate mode. In case of <see cref="ReflectionWays.DynamicDelegate"/> first access of a property is slow but
        /// further calls are faster than the <see cref="ReflectionWays.SystemReflection"/> or <see cref="ReflectionWays.TypeDescriptor"/> way.
        /// On components you may want to use the <see cref="ReflectionWays.TypeDescriptor"/> way to trigger property change events and to make possible to roll back
        /// changed values on error.
        /// </param>
        /// <seealso cref="Reflector"/>
        /// <seealso cref="Reflector.SetStaticPropertyByName(System.Type,string,object,KGySoft.Libraries.Reflection.ReflectionWays)"/>
        public static void SetStaticPropertyByName(this Type type, string propertyName, object value, ReflectionWays way)
        {
            Reflector.SetStaticPropertyByName(type, propertyName, value, way);
        }

        /// <summary>
        /// Sets a static property based on a property name given in <paramref name="propertyName"/> parameter.
        /// Property can refer to either public or non-public properties.
        /// </summary>
        /// <param name="type">The type that contains the static the property to set.</param>
        /// <param name="propertyName">The property name to set.</param>
        /// <param name="value">The new desired value of the property to set.</param>
        /// <seealso cref="Reflector"/>
        /// <seealso cref="Reflector.SetStaticPropertyByName(System.Type,string,object)"/>
        public static void SetStaticPropertyByName(this Type type, string propertyName, object value)
        {
            Reflector.SetStaticPropertyByName(type, propertyName, value);
        }

        #endregion
    }
}
