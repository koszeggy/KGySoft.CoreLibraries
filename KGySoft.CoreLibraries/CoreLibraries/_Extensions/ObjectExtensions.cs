#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObjectExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
#if NETFRAMEWORK
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#endif
using System.Runtime.Serialization;
using System.Security; 

using KGySoft.Serialization.Binary;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="Object"/> type.
    /// </summary>
    public static partial class ObjectExtensions
    {
        #region Methods

        /// <summary>
        /// Gets whether <paramref name="item"/> is among the elements of <paramref name="set"/>.
        /// <br/>See the <strong>Examples</strong> section of the generic <see cref="In{T}(T,T[])"/> overload for an example.
        /// </summary>
        /// <param name="item">The item to search for in <paramref name="set"/>.</param>
        /// <param name="set">The set of items in which to search the specified <paramref name="item"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> is among the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method works similarly to the <c>in</c> operator in SQL and Pascal.</para>
        /// <para>This overload uses <see cref="object.Equals(object,object)">Object.Equals</see> method to compare the items.
        /// <note>For better performance use the generic <see cref="In{T}(T,T[])"/> or <see cref="In{T}(T,Func{T}[])"/> methods whenever possible.</note></para>
        /// </remarks>
        public static bool In(this object item, params object[] set)
        {
            int length;
            if (set == null || (length = set.Length) == 0)
                return false;

            for (int i = 0; i < length; i++)
            {
                if (Equals(item, set[i]))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets whether <paramref name="item"/> is among the elements of <paramref name="set"/>.
        /// <br/>See the <strong>Examples</strong> section for an example.
        /// </summary>
        /// <param name="item">The item to search for in <paramref name="set"/>.</param>
        /// <param name="set">The set of items in which to search the specified <paramref name="item"/>.</param>
        /// <typeparam name="T">The type of <paramref name="item"/> and the <paramref name="set"/> elements.</typeparam>
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> is among the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method works similarly to the <c>in</c> operator in SQL and Pascal.</para>
        /// <para>This overload uses generic <see cref="IEqualityComparer{T}"/> implementations to compare the items for the best performance.
        /// <note>If elements of <paramref name="set"/> are complex expressions consider to use the <see cref="In{T}(T,Func{T}[])"/> overload instead to prevent evaluating all elements until they are actually compared.</note></para>
        /// </remarks>
        /// <example>
        /// <code lang="C#"><![CDATA[
        /// using System;
        /// using KGySoft.CoreLibraries;
        /// 
        /// public class Example
        /// {
        ///     public static void Main()
        ///     {
        ///         string stringValue = "blah";
        /// 
        ///         // standard way:
        ///         if (stringValue == "something" || stringValue == "something else" || stringValue == "maybe some other value" || stringValue == "or...")
        ///             DoSomething();
        /// 
        ///         // In method:
        ///         if (stringValue.In("something", "something else", "maybe some other value", "or..."))
        ///             DoSomething();
        ///     }
        /// }]]></code>
        /// </example>
        public static bool In<T>(this T item, params T[] set)
        {
            int length;
            if (set == null || (length = set.Length) == 0)
                return false;

            var comparer = ComparerHelper<T>.EqualityComparer;
            for (int i = 0; i < length; i++)
            {
                if (comparer.Equals(item, set[i]))
                    return true;
            }

            return false;
        }

#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
        /// <summary>
        /// Gets whether <paramref name="item"/> is among the elements of <paramref name="set"/>.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="In{T}(T,T[])"/> overload for an example.
        /// </summary>
        /// <param name="item">The item to search for in <paramref name="set"/>.</param>
        /// <param name="set">The set of items in which to search the specified <paramref name="item"/>.</param>
        /// <typeparam name="T">The type of <paramref name="item"/> and the <paramref name="set"/> elements.</typeparam>
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> is among the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method works similarly to the <c>in</c> operator in SQL and Pascal.</para>
        /// <para>This overload uses generic <see cref="IEqualityComparer{T}"/> implementations to compare the items for the best performance.
        /// <note>If elements of <paramref name="set"/> are complex expressions consider to use the <see cref="In{T}(T,Func{T}[])"/> overload instead to prevent evaluating all elements until they are actually compared.</note></para>
        /// </remarks>
        public static bool In<T>(this T item, ReadOnlySpan<T> set)
        {
            int length;
            if (set == null || (length = set.Length) == 0)
                return false;

            var comparer = ComparerHelper<T>.EqualityComparer;
            for (int i = 0; i < length; i++)
            {
                if (comparer.Equals(item, set[i]))
                    return true;
            }

            return false;
        } 
#endif

        /// <summary>
        /// Gets whether <paramref name="item"/> is among the results of <paramref name="set"/>.
        /// </summary>
        /// <param name="item">The item to search for in the results of <paramref name="set"/>.</param>
        /// <param name="set">The set of delegates, whose results are checked whether they are equal to the specified <paramref name="item"/>.</param>
        /// <typeparam name="T">The type of <paramref name="item"/> and the <paramref name="set"/> elements.</typeparam>
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> is among the results of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method works similarly to the <c>in</c> operator in SQL and Pascal.</para>
        /// <para>This overload uses generic <see cref="IEqualityComparer{T}"/> implementations to compare the items for the best performance.
        /// The elements of <paramref name="set"/> are evaluated only when they are actually compared so if a result is found the rest of the elements will not be evaluated.
        /// <note>If elements of <paramref name="set"/> are constants or simple expressions consider to use the <see cref="In{T}(T,T[])"/> overload to eliminate the overhead of delegate invokes.</note></para>
        /// </remarks>
        public static bool In<T>(this T item, params Func<T>[] set)
        {
            int length;
            if (set == null || (length = set.Length) == 0)
                return false;

            var comparer = ComparerHelper<T>.EqualityComparer;
            for (int i = 0; i < length; i++)
            {
                Func<T> func = set[i];
                if (func == null)
                    Throw.ArgumentException(Argument.set, Res.ArgumentContainsNull);
                if (comparer.Equals(item, func.Invoke()))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets whether <paramref name="item"/> is among the results of <paramref name="set"/>.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="In{T}(T,T[])"/> overload for an example.
        /// </summary>
        /// <param name="item">The item to search for in the results of <paramref name="set"/>.</param>
        /// <param name="set">The set of items in which to search the specified <paramref name="item"/>.</param>
        /// <typeparam name="T">The type of <paramref name="item"/> and the <paramref name="set"/> elements.</typeparam>
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> is among the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        public static bool In<T>(this T item, IEnumerable<T> set)
        {
            if (set == null)
                return false;

            IEqualityComparer<T> comparer = ComparerHelper<T>.EqualityComparer;
            foreach (T element in set)
            {
                if (comparer.Equals(item, element))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Clones an object by deep cloning.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="obj">The object to clone.</param>
        /// <param name="ignoreCustomSerialization"><see langword="true"/>&#160;to ignore <see cref="ISerializable"/> and <see cref="IObjectReference"/> implementations
        /// as well as serialization constructors and serializing methods; <see langword="false"/>&#160;to consider all of these techniques instead of performing a forced
        /// field-based serialization. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>The functionally equivalent clone of the object.</returns>
        /// <remarks>
        /// <para>This method makes possible to clone objects even if their type is not marked by the <see cref="SerializableAttribute"/>; however,
        /// in such case it is not guaranteed that the result is functionally equivalent to the input object.</para>
        /// <note type="warning">In .NET Core there are some types that implement the <see cref="ISerializable"/> interface, though they are not serializable.
        /// In such cases the cloning attempt typically throws a <see cref="PlatformNotSupportedException"/>. To clone such objects the <paramref name="ignoreCustomSerialization"/>
        /// parameter should be <see langword="true"/>.</note>
        /// <para>If <paramref name="ignoreCustomSerialization"/> is <see langword="false"/>, then it is not guaranteed that the object can be cloned in all circumstances (see the note above).</para>
        /// <para>On the other hand, if <paramref name="ignoreCustomSerialization"/> is <see langword="true"/>, then it can happen that even singleton types will be deep cloned.
        /// The cloning is performed by the <see cref="BinarySerializationFormatter"/> class, which supports some singleton types natively (such as <see cref="Type"/> and <see cref="DBNull"/>),
        /// which will be always cloned correctly.</para>
        /// <para>In .NET Framework remote objects are cloned in a special way and the result is always a local object.
        /// The <paramref name="ignoreCustomSerialization"/> parameter is ignored for remote objects.</para>
        /// </remarks>
        [SecuritySafeCritical]
        public static T DeepClone<T>(this T obj, bool ignoreCustomSerialization = false)
        {
            ISurrogateSelector surrogate = null;
            var formatter = new BinarySerializationFormatter(BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.IgnoreTypeForwardedFromAttribute);
#if NETFRAMEWORK
            if (RemotingServices.IsTransparentProxy(obj))
                surrogate = new RemotingSurrogateSelector();
            else
#endif
            if (ignoreCustomSerialization)
            {
                formatter.Options |= BinarySerializationOptions.IgnoreSerializationMethods | BinarySerializationOptions.IgnoreIObjectReference | BinarySerializationOptions.IgnoreIBinarySerializable;
                surrogate = new CustomSerializerSurrogateSelector { IgnoreISerializable = true, IgnoreNonSerializedAttribute = true };
            }

            formatter.SurrogateSelector = surrogate;
            using (var stream = new MemoryStream())
            {
                formatter.SerializeToStream(stream, obj);
                stream.Position = 0L;
#if NETFRAMEWORK
                if (surrogate is RemotingSurrogateSelector)
                    formatter.SurrogateSelector = null;
#endif
                return (T)formatter.DeserializeFromStream(stream);
            }
        }

        /// <summary>
        /// Converts an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <typeparamref name="TTarget"/>.
        /// </summary>
        /// <typeparam name="TTarget">The desired type of the return value.</typeparam>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The culture to use for the conversion. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An object of <typeparamref name="TTarget"/>, which is the result of the conversion.</returns>
        /// <exception cref="ArgumentException"><paramref name="obj"/> cannot be converted to <typeparamref name="TTarget"/>.</exception>
        /// <remarks>
        /// <para>The method firstly tries to use registered direct conversions between source and target types, then attempts to perform the conversion via <see cref="IConvertible"/> types and registered <see cref="TypeConverter"/>s.
        /// If these attempts fail, then the registered conversions tried to be used for intermediate steps, if possible.</para>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
        /// <para>A <see cref="TypeConverter"/> can be registered by the <see cref="TypeExtensions.RegisterTypeConverter{TConverter}">RegisterTypeConverter</see>&#160;<see cref="Type"/> extension method.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// <para><typeparamref name="TTarget"/> can be even a collection type if <paramref name="obj"/> is also an <see cref="IEnumerable"/> implementation.
        /// The target collection type must have either a default constructor or a constructor that can accept a list, array or dictionary as an initializer collection.</para>
        /// </remarks>
        /// <example>
        /// <note type="tip">Try also <a href="https://dotnetfiddle.net/rzg8If" target="_blank">online</a>.</note>
        /// <code lang="C#"><![CDATA[
        /// using System;
        /// using System.Collections;
        /// using System.Collections.Generic;
        /// using System.Collections.ObjectModel;
        /// using System.Linq;
        /// using KGySoft.CoreLibraries;
        /// 
        /// public class Example
        /// {
        ///     public static void Main()
        ///     {
        ///         // between convertible types: like the Convert class but supports also enums in both ways
        ///         ConvertTo<int>("123"); // culture can be specified, default is InvariantCulture
        ///         ConvertTo<float>(ConsoleColor.Blue);
        ///         ConvertTo<ConsoleColor>(13); // this would fail by Convert.ChangeType
        /// 
        ///         // TypeConverters are used if possible:
        ///         ConvertTo<Guid>("AADC78003DAB4906826EFD8B2D5CF33D");
        /// 
        ///         // New conversions can be registered:
        ///         ConvertTo<IntPtr>(42L); // fail
        ///         typeof(long).RegisterConversion(typeof(IntPtr), (obj, type, culture) => new IntPtr((long)obj));
        ///         ConvertTo<IntPtr>(42L); // success
        /// 
        ///         // Registered conversions can be used as intermediate steps:
        ///         ConvertTo<IntPtr>('x'); // char => long => IntPtr
        /// 
        ///         // Collection conversion is also supported:
        ///         ConvertTo<bool[]>(new List<int> { 1, 0, 0, 1 });
        ///         ConvertTo<List<int>>("Blah"); // works because string is an IEnumerable<char>
        ///         ConvertTo<string>(new[] { 'h', 'e', 'l', 'l', 'o' }); // because string has a char[] constructor
        ///         ConvertTo<ReadOnlyCollection<string>>(new[] { 1.0m, 2, -1 }); // via the IList<T> constructor
        /// 
        ///         // even between non-generic collections:
        ///         ConvertTo<ArrayList>(new HashSet<int> { 1, 2, 3 });
        ///         ConvertTo<Dictionary<ConsoleColor, string>>(new Hashtable { { 1, "One" }, { "Black", 'x' } });
        ///     }
        /// 
        ///     private static void ConvertTo<T>(object source)
        ///     {
        ///         Console.Write($"{source.GetType().GetName(TypeNameKind.ShortName)} => {typeof(T).GetName(TypeNameKind.ShortName)}: {AsString(source)} => ");
        ///         try
        ///         {
        ///             T result = source.Convert<T>(); // a culture can be specified here for string conversions
        ///             Console.WriteLine(AsString(result));
        ///         }
        ///         catch (Exception e)
        ///         {
        ///             Console.WriteLine(e.Message.Replace(Environment.NewLine, " "));
        ///         }
        ///     }
        /// 
        ///     private static string AsString(object obj)
        ///     {
        ///         if (obj == null)
        ///             return "<null>";
        /// 
        ///         // KeyValuePair has a similar ToString to this one
        ///         if (obj is DictionaryEntry de)
        ///             return $"[{de.Key}, {de.Value}]";
        /// 
        ///         if (!(obj is IEnumerable) || obj is string)
        ///             return obj.ToString();
        /// 
        ///         return String.Join(", ", ((IEnumerable)obj).Cast<object>().Select(AsString));
        ///     }
        /// }
        /// 
        /// // This example produces the following output:
        /// // String => Int32: 123 => 123
        /// // ConsoleColor => Single: Blue => 9
        /// // Int32 => ConsoleColor: 13 => Magenta
        /// // String => Guid: AADC78003DAB4906826EFD8B2D5CF33D => aadc7800-3dab-4906-826e-fd8b2d5cf33d
        /// // Int64 => IntPtr: 42 => The specified argument cannot be converted to type System.IntPtr. Parameter name: obj
        /// // Int64 => IntPtr: 42 => 42
        /// // Char => IntPtr: x => 120
        /// // List`1 => Boolean[]: 1, 0, 0, 1 => True, False, False, True
        /// // String => List`1: Blah => 66, 108, 97, 104
        /// // Char[] => String: h, e, l, l, o => hello
        /// // Decimal[] => ReadOnlyCollection`1: 1.0, 2, -1 => 1.0, 2, -1
        /// // HashSet`1 => ArrayList: 1, 2, 3 => 1, 2, 3
        /// // Hashtable => Dictionary`2: [1, One], [Black, x] => [DarkBlue, One], [Black, x]]]></code>
        /// </example>
        public static TTarget Convert<TTarget>(this object obj, CultureInfo culture = null)
        {
            if (!ObjectConverter.TryConvert(obj, typeof(TTarget), culture, out object result, out Exception error) || (!(result is TTarget) && !typeof(TTarget).CanAcceptValue(result)))
                Throw.ArgumentException(Argument.obj, Res.ObjectExtensionsCannotConvertToType(typeof(TTarget)), error);
            return (TTarget)result;
        }

        /// <summary>
        /// Converts an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <paramref name="targetType"/>.
        /// <br/>See the <strong>Examples</strong> section of the generic <see cref="Convert{TTarget}"/> overload for an example.
        /// </summary>
        /// <param name="targetType">The desired type of the return value.</param>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The culture to use for the conversion. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An object of <paramref name="targetType"/>, which is the result of the conversion.</returns>
        /// <exception cref="ArgumentException"><paramref name="obj"/> cannot be converted to <paramref name="targetType"/>.</exception>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// <para><paramref name="targetType"/> can be even a collection type if <paramref name="obj"/> is also an <see cref="IEnumerable"/> implementation.
        /// The target collection type must have either a default constructor or a constructor that can accept a list, array or dictionary as an initializer collection.</para>
        /// </remarks>
        public static object Convert(this object obj, Type targetType, CultureInfo culture = null)
        {
            if (!ObjectConverter.TryConvert(obj, targetType, culture, out object result, out Exception error) || !targetType.CanAcceptValue(result))
                Throw.ArgumentException(Argument.obj, Res.ObjectExtensionsCannotConvertToType(targetType), error);
            return result;
        }

        /// <summary>
        /// Tries to convert an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <typeparamref name="TTarget"/>.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="Convert{TTarget}"/> method for a related example.
        /// </summary>
        /// <typeparam name="TTarget">The desired type of the returned <paramref name="value"/>.</typeparam>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The culture to use for the conversion. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the conversion.</param>
        /// <returns><see langword="true"/>, if <paramref name="obj"/> could be converted to <typeparamref name="TTarget"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// </remarks>
        public static bool TryConvert<TTarget>(this object obj, CultureInfo culture, out TTarget value)
        {
            if (TryConvert(obj, typeof(TTarget), culture, out object result) && (result is TTarget || typeof(TTarget).CanAcceptValue(result)))
            {
                value = (TTarget)result;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Tries to convert an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <typeparamref name="TTarget"/>.
        /// <br/>See the <strong>Examples</strong> section of the <see cref="Convert{TTarget}"/> method for a related example.
        /// </summary>
        /// <typeparam name="TTarget">The desired type of the returned <paramref name="value"/>.</typeparam>
        /// <param name="obj">The object to convert.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the conversion.</param>
        /// <returns><see langword="true"/>, if <paramref name="obj"/> could be converted to <typeparamref name="TTarget"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// </remarks>
        public static bool TryConvert<TTarget>(this object obj, out TTarget value) => TryConvert(obj, null, out value);

        /// <summary>
        /// Tries to convert an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <paramref name="targetType"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="targetType">The desired type of the returned <paramref name="value"/>.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the conversion.</param>
        /// <returns><see langword="true"/>, if <paramref name="obj"/> could be converted to <paramref name="targetType"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// </remarks>
        public static bool TryConvert(this object obj, Type targetType, out object value) => TryConvert(obj, targetType, null, out value);

        /// <summary>
        /// Tries to convert an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <paramref name="targetType"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="targetType">The desired type of the returned <paramref name="value"/>.</param>
        /// <param name="culture">The culture to use for the conversion. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the conversion.</param>
        /// <returns><see langword="true"/>, if <paramref name="obj"/> could be converted to <paramref name="targetType"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// </remarks>
        public static bool TryConvert(this object obj, Type targetType, CultureInfo culture, out object value) => ObjectConverter.TryConvert(obj, targetType, culture, out value, out var _);

        #endregion
    }
}
