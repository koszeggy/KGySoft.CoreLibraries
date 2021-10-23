#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
#if !NET35
using System.Numerics;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Security;
using System.Text;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Serialization.Xml;

#endregion

#region Suppressions

#if !NET6_0_OR_GREATER
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif
#if NET5_0_OR_GREATER
#pragma warning disable CS8768 // Nullability of return type does not match implemented member - BinarySerializationFormatter supports de/serializing null
#endif

#endregion

/* How to add a new type
 * =====================
 *
 * I. Adding a simple type
 * ~~~~~~~~~~~~~~~~~~~~~~~
 * 1. Add type to DataTypes 0-5 bits (adjust free places in comments)
 * 2. If type is pure (unambiguous by DataType) add it to supportedNonPrimitiveElementTypes.
 *    Otherwise, handle it in SerializationManager.GetDataType/GetImpureDataType
 * 3. If type is pure handle it type in SerializationManager.WritePureObject. If serialization is more than one line create a static WriteXXX.
 *    Otherwise, handle it in SerializationManager.WriteImpureObject. Create a WriteXXX that can be called separately from writing type.
 * 4. Handle type in DeserializationManager.ReadObject: for reference types call TryGetFromCache. Always set createdResult.
 * 5. Add type to DataTypeDescriptor.GetElementType.
 *    If type is non-pure and WriteXXX starts with WriteType, then you can put it into the group with ReadType.
 * 6. Add type to unit test:
 *    - SerializeSimpleTypes
 *    - SerializeSimpleArrays
 *    - SerializeNullableArrays (value types)
 *    - SerializationSurrogateTest
 * 7. Add type to description - Natively supported simple types
 *
 * II. Adding a collection type
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * 1. Add type to DataTypes 8-13 bits (adjust free places in comments)
 *    - 0..15 << 8: Generic collections
 *    - 16..31 << 8: Non-generic collections
 *    - 32..47 << 8: Generic dictionaries
 *    - 48..63 << 8: Non-generic dictionaries
 * 2. Update serializationInfo initializer - mind the groups of 1.
 *    - If new CollectionInfo flag has to be defined, a property in CollectionSerializationInfo might be also needed
 * 3. Add type to supportedCollections
 * 4. Handle type in SerializationManager.GetDictionaryValueTypes - mind non-dictionary/dictionary types
 * 5. Add type to DataTypeDescriptor.GetCollectionType - mind groups
 * 6. If needed, update CollectionSerializationInfo.WriteSpecificProperties and InitializeCollection (e.g. new flag in 2.)
 * 7. If collection type is an ordered non-IList collection, or an unordered non-ICollection<T> collection,
 *    then handle it in AddCollectionElement
 * 8. Add type to unit test:
 *    - SerializeSimpleGenericCollections or SerializeSimpleNonGenericCollections
 *    - SerializeSupportedDictionaries - twice when generic dictionary type; otherwise, only once
 *   [- SerializeComplexGenericCollections - when generic]
 *   [- SerializationSurrogateTest]
 * 9. Add type to description - Collections
 *
 * To debug the serialized stream of the test cases set BinarySerializerTest.dumpDetails and see the console output.
 */
namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Serializes and deserializes objects in binary format.
    /// <br/>See the <strong>Remarks</strong> section for details and for the differences to <see cref="BinaryFormatter"/>.
    /// </summary>
    /// <seealso cref="BinarySerializer"/>
    /// <seealso cref="BinarySerializationOptions"/>
    /// <seealso cref="IBinarySerializable"/>
    /// <remarks>
    /// <note type="warning">The fundamental goal of binary serialization is to store the bitwise content of an object, hence in general case it relies on
    /// field values (including private ones), which can change from version to version. Therefore, binary serialization is recommended only for in-process purposes,
    /// such as deep cloning or undo/redo, etc. If it is known that a type will be deserialized in another environment and it can be completely restored by its public members,
    /// then a text-based serialization (see also <see cref="XmlSerializer"/>) can be a better choice.</note>
    /// <note type="security"><para>Do not use binary serialization if the serialization stream may come from an untrusted source (eg. remote service, file or database).
    /// If you still need to do so (eg. due to compatibility), then it is highly recommended to enable the <see cref="BinarySerializationOptions.SafeMode"/> option, which prevents
    /// loading assemblies during the deserialization as well as instantiating non-serializable types, and guards against some attacks that may cause <see cref="OutOfMemoryException"/>.
    /// When using <see cref="BinarySerializationOptions.SafeMode"/> you must preload every assembly referred by the serialization stream.</para>
    /// <para>Please note though that even some system types can be dangerous. In the .NET Framework there are some serializable types in the fundamental core assemblies that
    /// can be exploited for several attacks (causing unresponsiveness, <see cref="StackOverflowException"/> or even files to be deleted). Starting with .NET Core these types are not
    /// serializable anymore and some of them have been moved to separate NuGet packages anyway, but the <see cref="BinaryFormatter"/> in the .NET Framework is still vulnerable against such attacks.
    /// When using the <see cref="BinarySerializationOptions.SafeMode"/> flag, the <see cref="BinarySerializationFormatter"/> is protected against the known security issues
    /// on all platforms but of course it cannot guard you against the already loaded potentially harmful types.</para>
    /// <para>Please also note that <see cref="BinarySerializationOptions.SafeMode"/> cannot prevent deserializing invalid content if a serializable type does not implement <see cref="ISerializable"/>
    /// and does it not validate the incoming <see cref="SerializationInfo"/> in its serialization constructor. All serializable types that can have an invalid state regarding the field values
    /// should implement <see cref="ISerializable"/> and should throw a <see cref="SerializationException"/> from their serialization constructor if validation fails.
    /// Other exceptions thrown by the constructor will be wrapped into a <see cref="SerializationException"/>.</para>
    /// <para>To be completely secured use binary serialization in-process only, or (especially when targeting the .NET Framework), set the <see cref="Binder"/> property to a <see cref="SerializationBinder"/>
    /// instance that uses strict mapping. For example, you can use the <see cref="CustomSerializationBinder"/> class with handlers that throw exceptions for unexpected assemblies and types.</para>
    /// <para>Please also note that if the <see cref="Binder"/> property is set, then using <see cref="BinarySerializationOptions.SafeMode"/> cannot prevent loading assemblies by the binder itself.
    /// It can just assure that if the binder returns <see langword="null"/>, then the default resolve logic will not allow loading assemblies. The binders in this library that can perform automatic
    /// type resolving, such the <see cref="WeakAssemblySerializationBinder"/> and <see cref="ForwardedTypesSerializationBinder"/> have their own <c>SafeMode</c> property.
    /// If you use them, make sure to set their <c>SafeMode</c> property to <see langword="true"/>&#160;to prevent loading assemblies by the binders themselves.</para>
    /// <para>Similarly, if the <see cref="SurrogateSelector"/> property is set, then they provide a custom serialization even for types that are not serializable. The surrogate selectors in this library,
    /// such as the <see cref="CustomSerializerSurrogateSelector"/> and <see cref="NameInvariantSurrogateSelector"/> types have their own <c>SafeMode</c> property.
    /// If you use them, make sure to set their <c>SafeMode</c> property to <see langword="true"/>&#160;to prevent deserializing non-serializable types.</para></note>
    /// <para><see cref="BinarySerializationFormatter"/> aims to serialize objects effectively where the serialized data is almost always more compact than the results produced by the <see cref="BinaryFormatter"/> class.</para>
    /// <para><see cref="BinarySerializationFormatter"/> natively supports all of the primitive types and a sort of other simple types, arrays, generic and non-generic collections.
    /// <note>Serialization of natively supported types produce an especially compact result because these types are not serialized by traversing and storing the fields of the object graph recursively.
    /// This means not just better performance for these types but also prevents compatibility issues between different platforms because these types are not encoded by assembly identity and type name.
    /// Serialization of complex types can be somewhat slower for the first time than by <see cref="BinaryFormatter"/> but the serialized result is almost always shorter than the one by <see cref="BinaryFormatter"/>,
    /// especially when generic types are involved.</note></para>
    /// <para>Even if a type is not marked to be serializable by the <see cref="SerializableAttribute"/>, then you can use the <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option to force their serialization.
    /// Alternatively, you can implement the <see cref="IBinarySerializable"/> interface, which can be used to produce a more compact custom serialization than the one provided by implementing the <see cref="ISerializable"/> interface.
    /// A custom serialization logic can be applied also by setting the <see cref="SurrogateSelector"/> property.<para>
    /// </para>Similarly to <see cref="BinaryFormatter"/>, <see cref="ISerializable"/> implementations are also supported, and they are considered only for types marked by the <see cref="SerializableAttribute"/>, unless
    /// the <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option is enabled for the serialization.</para>
    /// <para>As <see cref="BinarySerializationFormatter"/> implements <see cref="IFormatter"/> it fully supports <see cref="SerializationBinder"/> and <see cref="ISurrogateSelector"/> implementations.
    /// <note type="tip">A <see cref="SerializationBinder"/> can be used to deserialize types of unmatching assembly identity and to specify custom type-name mappings in both directions.
    /// Though <see cref="BinarySerializationFormatter"/> automatically handles <see cref="TypeForwardedToAttribute"/> and <see cref="TypeForwardedFromAttribute"/> (see also
    /// the <see cref="BinarySerializationOptions.IgnoreTypeForwardedFromAttribute"/> option), you can use also the <see cref="ForwardedTypesSerializationBinder"/>, especially for types without a defined forwarding.
    /// The <see cref="WeakAssemblySerializationBinder"/> can also be general solution if you need to ignore the assembly version or the complete assembly identity on resolving a type.
    /// If the name of the type has also been changed, then the <see cref="CustomSerializationBinder"/> can be used.
    /// See also the <strong>Remarks</strong> section of the <see cref="Binder"/> property for more details.</note>
    /// <note type="tip">An <see cref="ISurrogateSelector"/> can be used to customize serialization and deserialization. It can be used for types that cannot be handled anyway for some reason.
    /// For example, if you need to deserialize types, whose field names have been renamed you can use the <see cref="CustomSerializerSurrogateSelector"/>.
    /// Or, if the produced raw data has to be compatible with the obfuscated version of a type, then it can be achieved by the <see cref="NameInvariantSurrogateSelector"/>.</note>
    /// </para>
    /// <para>There are three ways to serialize/deserialize an object. To serialize into a byte array use the <see cref="Serialize">Serialize</see> method. Its result can be deserialized by the <see cref="Deserialize">Deserialize</see> method.
    /// Additionally, you can use the <see cref="SerializeToStream">SerializeToStream</see>/<see cref="DeserializeFromStream">DeserializeFromStream</see> methods to dump/read the result to and from a <see cref="Stream"/>, and the
    /// the <see cref="SerializeByWriter">SerializeByWriter</see>/<see cref="DeserializeByReader">DeserializeByReader</see> methods to use specific <see cref="BinaryWriter"/> and <see cref="BinaryReader"/> instances for
    /// serialization and deserialization, respectively.</para>
    /// <note type="warning">In .NET Framework almost every type was serializable by <see cref="BinaryFormatter"/>. In .NET Core this principle has been
    /// radically changed. Many types are just simply not marked by the <see cref="SerializableAttribute"/> anymore (eg. <see cref="MemoryStream"/>,
    /// <see cref="CultureInfo"/>, <see cref="Encoding"/>), and also there are some others, which still implement <see cref="ISerializable"/> but their <see cref="ISerializable.GetObjectData">GetObjectData</see>
    /// throw a <see cref="PlatformNotSupportedException"/> now. Binary serialization of these types is not recommended anymore. If you still must serialize or deserialize such types
    /// see the <strong>Remarks</strong> section of the <see cref="CustomSerializerSurrogateSelector"/> for more details.</note>
    /// <h1 class="heading">Natively supported simple types</h1>
    /// <para>Following types are natively supported. When these types are serialized, no recursive traversal of the fields occurs:
    /// <list type="bullet">
    /// <item><see langword="null"/>&#160;reference</item>
    /// <item>Non-derived <see cref="object"/> instances.</item>
    /// <item><see cref="DBNull"/></item>
    /// <item><see cref="bool"/></item>
    /// <item><see cref="sbyte"/></item>
    /// <item><see cref="byte"/></item>
    /// <item><see cref="short"/></item>
    /// <item><see cref="ushort"/></item>
    /// <item><see cref="int"/></item>
    /// <item><see cref="uint"/></item>
    /// <item><see cref="long"/></item>
    /// <item><see cref="ulong"/></item>
    /// <item><see cref="char"/></item>
    /// <item><see cref="string"/></item>
    /// <item><see cref="float"/></item>
    /// <item><see cref="double"/></item>
    /// <item><see cref="decimal"/></item>
    /// <item><see cref="DateTime"/></item>
    /// <item><see cref="TimeSpan"/></item>
    /// <item><see cref="DateTimeOffset"/></item>
    /// <item><see cref="IntPtr"/></item>
    /// <item><see cref="UIntPtr"/></item>
    /// <item><see cref="Version"/></item>
    /// <item><see cref="Guid"/></item>
    /// <item><see cref="Uri"/></item>
    /// <item><see cref="StringBuilder"/></item>
    /// <item><see cref="BigInteger"/> (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="Rune"/> (in .NET Core 3.0 and above)</item>
    /// <item><see cref="Index"/> (in .NET Standard 2.1 and above)</item>
    /// <item><see cref="Range"/> (in .NET Standard 2.1 and above)</item>
    /// <item><see cref="Half"/> (in .NET 5.0 and above)</item>
    /// <item><see cref="DateOnly"/> (in .NET 6.0 and above)</item>
    /// <item><see cref="TimeOnly"/> (in .NET 6.0 and above)</item>
    /// <item><see cref="Enum"/> types</item>
    /// <item><see cref="Type"/> instances if they are runtime types.</item>
    /// <item><see cref="Nullable{T}"/> types if type parameter is any of the supported types.</item>
    /// <item>Any object that implements the <see cref="IBinarySerializable"/> interface.</item>
    /// <item><see cref="KeyValuePair{TKey,TValue}"/> if <see cref="KeyValuePair{TKey,TValue}.Key"/> and <see cref="KeyValuePair{TKey,TValue}.Value"/> are any of the supported types.</item>
    /// <item><see cref="DictionaryEntry"/> if <see cref="DictionaryEntry.Key"/> and <see cref="DictionaryEntry.Value"/> are any of the supported types.</item>
    /// </list>
    /// <note>
    /// <list type="bullet">
    /// <item>Serializing <see cref="Enum"/> types will result a longer raw data than serializing their numeric value, though the result will be still shorter than the one produced by <see cref="BinaryFormatter"/>.</item>
    /// <item>If <see cref="KeyValuePair{TKey,TValue}"/> contains non-natively supported type arguments or <see cref="DictionaryEntry"/> has non-natively supported keys an values, then for them recursive serialization may occur.
    /// If they contain non-serializable types, then the <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option should be enabled.</item>
    /// </list>
    /// </note>
    /// </para>
    /// <h1 class="heading">Natively supported generic collections</h1>
    /// <para>Following generic collections are natively supported. When their generic arguments are one of the simple types or other supported collections, then no recursive traversal of the fields occurs:
    /// <list type="bullet">
    /// <item><see cref="Array"/> of element types above or compound of other supported collections</item>
    /// <item><see cref="List{T}"/></item>
    /// <item><see cref="CircularList{T}"/></item>
    /// <item><see cref="LinkedList{T}"/></item>
    /// <item><see cref="HashSet{T}"/></item>
    /// <item><see cref="Queue{T}"/></item>
    /// <item><see cref="Stack{T}"/></item>
    /// <item><see cref="SortedSet{T}"/> (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="ConcurrentBag{T}"/> (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="ConcurrentQueue{T}"/> (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="ConcurrentStack{T}"/> (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="Dictionary{TKey,TValue}"/></item>
    /// <item><see cref="SortedList{TKey,TValue}"/></item>
    /// <item><see cref="SortedDictionary{TKey,TValue}"/></item>
    /// <item><see cref="CircularSortedList{TKey,TValue}"/></item>
    /// <item><see cref="ConcurrentDictionary{TKey,TValue}"/> (in .NET Framework 4.0 and above)</item>
    /// </list>
    /// <note>
    /// <list type="bullet">
    /// <item><see cref="Array"/>s can be single- and multidimensional, jagged (array of arrays) and don't have to be zero index-based. Arrays and other generic collections can be nested.</item>
    /// <item>If a collection uses a non-default <see cref="IEqualityComparer{T}"/> or <see cref="IComparer{T}"/> implementation, then it is possible that the type cannot be serialized without enabling
    /// <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option, unless the comparer is decorated by <see cref="SerializableAttribute"/> or implements the <see cref="IBinarySerializable"/> interface.</item>
    /// <item>If an <see cref="Array"/> has <see cref="object"/> element type or <see cref="object"/> is used in generic arguments of the collections above and an element is not a natively supported type, then recursive serialization of fields
    /// may occur. For non-serializable types the <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option might be enabled.</item>
    /// <item>Even if a generic collection of <see cref="object"/> contains natively supported types only, the result will be somewhat longer than in case of a more specific element type.</item>
    /// </list>
    /// </note>
    /// </para>
    /// <note type="tip">The shortest result can be achieved by using <see langword="sealed"/>&#160;classes or value types as array base types and generic parameters.</note>
    /// <h1 class="heading">Natively supported non-generic collections</h1>
    /// <para>Following non-generic collections are natively supported. When they contain only other natively supported elements, then no recursive traversal of the fields occurs:
    /// <list type="table">
    /// <listheader><term>Collection type</term><description>Used element type</description></listheader>
    /// <item><term><see cref="ArrayList"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="Queue"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="Stack"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="StringCollection"/></term><description><see cref="string"/></description></item>
    /// <item><term><see cref="Hashtable"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="SortedList"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="ListDictionary"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="HybridDictionary"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="OrderedDictionary"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="StringDictionary"/></term><description><see cref="string"/></description></item>
    /// <item><term><see cref="BitArray"/></term><description><see cref="bool"/> (actually the type is stored in a compact way)</description></item>
    /// <item><term><see cref="BitVector32"/></term><description><see cref="bool"/> (actually the type is stored in a compact way)</description></item>
    /// <item><term><see cref="BitVector32.Section"/></term><description>n.a.</description></item>
    /// </list>
    /// <note>
    /// <list type="bullet">
    /// <item>If a collection uses a non-default <see cref="IEqualityComparer"/> or <see cref="IComparer"/> implementation, then it is possible that the type cannot be serialized without enabling
    /// <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option, unless the comparer is decorated by <see cref="SerializableAttribute"/> or implements the <see cref="IBinarySerializable"/> interface.</item>
    /// <item>If an element in these collections is not a natively supported type, then recursive serialization of fields may occur. For non-serializable types the <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/>
    /// option might be enabled.</item>
    /// </list>
    /// </note>
    /// </para>
    /// <h1 class="heading">Serialization events</h1>
    /// <para><see cref="BinarySerializationFormatter"/> supports calling methods decorated by <see cref="OnSerializingAttribute"/>, <see cref="OnSerializedAttribute"/>,
    /// <see cref="OnDeserializingAttribute"/> and <see cref="OnDeserializedAttribute"/> as well as calling <see cref="IDeserializationCallback.OnDeserialization">IDeserializationCallback.OnDeserialization</see> method.
    /// Attributes should be used on methods that have a single <see cref="StreamingContext"/> parameter.
    /// <note>Please note that if a value type was serialized by the <see cref="BinarySerializationOptions.CompactSerializationOfStructures"/> option, then the method of <see cref="OnDeserializingAttribute"/> can be invoked
    /// only after restoring the whole content so fields will be already restored.</note>
    /// </para>
    /// </remarks>
    /// <example>
    /// <note type="tip">Try also <a href="https://dotnetfiddle.net/T7BUyB" target="_blank">online</a>.</note>
    /// The following example demonstrates the length difference produced by the <see cref="BinarySerializationFormatter"/> and <see cref="BinaryFormatter"/> classes. Feel free to change the generated type.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Collections;
    /// using System.Collections.Generic;
    /// using System.Globalization;
    /// using System.IO;
    /// using System.Linq;
    /// using System.Reflection;
    /// using System.Runtime.Serialization;
    /// using System.Runtime.Serialization.Formatters.Binary;
    /// 
    /// using KGySoft.CoreLibraries;
    /// using KGySoft.Serialization.Binary;
    ///
    /// public static class Example
    /// {
    ///     public static void Main()
    ///     {
    ///         IFormatter formatter;
    ///
    ///         // feel free to change the type in NextObject<>
    ///         var instance = ThreadSafeRandom.Instance.NextObject<Dictionary<int, List<string>>>();
    ///         Console.WriteLine("Generated object:   " + Dump(instance));
    ///
    ///         using (var ms = new MemoryStream())
    ///         {
    ///             // serializing by KGy SOFT version:
    ///             formatter = new BinarySerializationFormatter();
    ///             formatter.Serialize(ms, instance);
    ///
    ///             // deserialization:
    ///             ms.Position = 0L;
    ///             object deserialized = formatter.Deserialize(ms);
    ///
    ///             Console.WriteLine("Deserialized object " + Dump(deserialized));
    ///             Console.WriteLine("Length by BinarySerializationFormatter: " + ms.Length);
    ///         }
    ///
    ///         using (var ms = new MemoryStream())
    ///         {
    ///             // serializing by System version:
    ///             formatter = new BinaryFormatter();
    ///             formatter.Serialize(ms, instance);
    ///             Console.WriteLine("Length by BinaryFormatter: " + ms.Length);
    ///         }
    ///     }
    ///
    ///     private static string Dump(object o)
    ///     {
    ///         if (o == null)
    ///             return "<null>";
    ///
    ///         if (o is IConvertible convertible)
    ///             return convertible.ToString(CultureInfo.InvariantCulture);
    ///
    ///         if (o is IEnumerable enumerable)
    ///             return $"[{enumerable.Cast<object>().Select(Dump).Join(", ")}]";
    ///
    ///         return $"{{{o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => $"{p.Name} = {Dump(p.GetValue(o))}").Join(", ")}}}";
    ///     }
    /// }
    ///
    /// // This code example produces a similar output to this one:
    /// // Generated object:   [{Key = 1418272504, Value = [aqez]}, {Key = 552276491, Value = [addejibude, yifefa]}]
    /// // Deserialized object [{Key = 1418272504, Value = [aqez]}, {Key = 552276491, Value = [addejibude, yifefa]}]
    /// // Length by BinarySerializationFormatter: 50
    /// // Length by BinaryFormatter: 2217]]></code>
    /// </example>
    /// <seealso cref="BinarySerializer"/>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Supports many types natively, which is intended. See also DataTypes enum.")]
    public sealed partial class BinarySerializationFormatter : IFormatter
    {
        #region Nested Types

        #region Enumerations

        /// <summary>
        /// Represents possible types. One of the simple types can be combined with one of the collection types and the flags.
        /// Nested generic collections can be encoded by multiple consecutive <see cref="DataTypes"/> values.
        /// </summary>
        [Flags]
        //[DebuggerDisplay("{BinarySerializationFormatter.DataTypeToString(this)}")] // If debugger cannot display it: Tools/Options/Debugging/General: Use Managed Compatibility Mode
        private enum DataTypes : ushort
        {
            // ===== LOW BYTE =====

            // ----- simple/element types: -----
            SimpleTypes = 0x3F, // bits 0-5 (6 bits - up to 64 types)

            // ..... pure types (they are unambiguous without a type name): .....
            //PureTypes = 0x3F, // bits 0-5 but never 11xxx, see also ImpureType ('5.5 bits' - up to 48 types)

            // . . . Primitive types (they are never custom serialized) . . .
            //PrimitiveTypes = 0x0F, // bits 0-3 (4 bits - up to 16 types)

            Null = 0, // Not a type but represents null/none values. As a collection element represents no simple element type (nested collection).
            Void = 1, // used rather as a type than an instance

            Bool = 2,
            Int8 = 3,
            UInt8 = 4,

            // Compressible types: 5-15
            Int16 = 5,
            UInt16 = 6,
            Int32 = 7,
            UInt32 = 8,
            Int64 = 9,
            UInt64 = 10,

            Single = 11,
            Double = 12,

            Char = 13,

            IntPtr = 14,
            UIntPtr = 15,
            // Compressible types end

            // . . . Non-primitive, platform independent pure types (16-31 - up to 16 types) . . .
            String = 16, // though not a primitive type, it cannot be custom serialized either
            StringBuilder = 17,
            Uri = 18,

            // ReSharper disable once InconsistentNaming
            DBNull = 19, // Non-serializable in .NET Core 2
            Object = 20, // Non sealed type. Can be any type as collection element.

            Decimal = 21,

            DateTime = 22,
            TimeSpan = 23,
            DateTimeOffset = 24,

            Version = 25,

            Guid = 26,

            BitArray = 27, // Too complex special handling would be needed as a collection so treated as simple type
            BitVector32 = 28, // Non-serializable
            BitVector32Section = 29, // Non-serializable

            RuntimeType = 30, // Non-serializable in .NET Core. Not meant to be combined but it can happen if collection element type is RuntimeType.

            // 31: reserved

            // . . . Non-primitive, platform-dependent pure types (32-48 - up to 16 types) . . .

            BigInteger = 32, // Only in .NET Framework 4.0 and above
            Rune = 33, // Only in .NET Core 3.0 and above
            Index = 34, // Only in .NET Standard 2.1 and above
            Range = 35, // Only in .NET Standard 2.1 and above
            Half = 36, // Only in .NET 5 and above
            DateOnly = 37, // Only in .NET 6 and above
            TimeOnly = 38, // Only in .NET 6 and above

            // 39-47: reserved

            // ..... impure types (their type cannot be determined purely by a DataType) .....
            ImpureType = 3 << 4, // caution: 2-bits flag (11000)

            // 48: Reserved (though it would have the same value as the ImpureType flag)

            GenericTypeDefinition = 49, // Must be combined with a supported generic collection type.
            Pointer = 50, // Followed by DataTypes. Cannot be combined.
            ByRef = 51, // Followed by DataTypes. Cannot be combined.

            // 52-59: 8 reserved values

            //SerializationEnd = 59, // Planned technical type for IAdvancedBinarySerializable (refers to a static object)
            BinarySerializable = 60, // IBinarySerializable implementation. Can be combined.
            RawStruct = 61, // Any ValueType. Can be combined only with Nullable but not with collections.
            RecursiveObjectGraph = 62, // Represents a recursively serialized object graph. As a type, represents any unspecified type. Can be combined.
            // 63: Reserved (though it would have has the same value as the SimpleTypes mask)

            // ----- flags: -----
            Store7BitEncoded = 1 << 6, // Applicable for every >1 byte fix-length data type
            Extended = 1 << 7, // On serialization indicates that high byte also is used.

            // ===== HIGH BYTE =====

            // ----- collection types: -----
            CollectionTypes = 0x3F00, // 8-13 bits (6 bits - up to 64 types)

            // ..... generic collections: .....
            Array = 1 << 8, // actually not a generic type but can be encoded the same way
            List = 2 << 8,
            LinkedList = 3 << 8,
            HashSet = 4 << 8,
            Queue = 5 << 8,
            Stack = 6 << 8,
            CircularList = 7 << 8,
            SortedSet = 8 << 8,
            ConcurrentBag = 9 << 8,
            ConcurrentQueue = 10 << 8,
            ConcurrentStack = 11 << 8,
            // 12-15 << 8: 4 reserved generic collections

            // ...... non-generic collections:
            ArrayList = 16 << 8,
            QueueNonGeneric = 17 << 8,
            StackNonGeneric = 18 << 8,
            StringCollection = 19 << 8,
            // 20-31 << 8: 12 reserved non-generic collection

            // ...... generic dictionaries:
            Dictionary = 32 << 8, // Represents both the generic Dictionary type and a flag (1 << 13) for all dictionaries
            SortedList = 33 << 8,
            SortedDictionary = 34 << 8,
            CircularSortedList = 35 << 8,
            ConcurrentDictionary = 36 << 8,
            // 37-45 << 8 : 9 reserved generic dictionaries

            KeyValuePair = 46 << 8, // Defined as a collection type so can be encoded the same way as dictionaries
            KeyValuePairNullable = 47 << 8, // The Nullable flag would be used for the key so this is the nullable version of the previous one.

            // ...... non-generic dictionaries:
            Hashtable = 48 << 8,
            SortedListNonGeneric = 49 << 8,
            ListDictionary = 50 << 8,
            HybridDictionary = 51 << 8,
            OrderedDictionary = 52 << 8,
            StringDictionary = 53 << 8,
            // 54-60 << 8 : 7 reserved non-generic dictionaries

            DictionaryEntry = 61 << 8, // Could be a simple type but keeping consistency with KeyValuePair
            DictionaryEntryNullable = 62 << 8, // The Nullable flag would be used for the key (which is invalid for this type) so this is the nullable version of the previous one.
            // 63 << 8: Reserved (though it would have has the same value as the CollectionTypes mask)

            // ------ flags
            Enum = 1 << 14,
            Nullable = 1 << 15 // Can be combined with simple types. Nullable collections are separate items.
        }

        /// <summary>
        /// Special serialization info for collections
        /// </summary>
        [Flags]
        private enum CollectionInfo
        {
            None = 0,

            /// <summary>
            /// Identifies that the collection has a Capacity property that has to be (re)stored.
            /// If this flag is disabled but there is a constructor with capacity parameter, then the size (Count) will be used at constructor
            /// </summary>
            HasCapacity = 1,

            /// <summary>
            /// Indicates that the collection has an EqualityComparer that can be passed to a constructor
            /// </summary>
            HasEqualityComparer = 1 << 1,

            /// <summary>
            /// Indicates that the collection has a Comparer that can be passed to a constructor
            /// </summary>
            HasComparer = 1 << 2,

            /// <summary>
            /// Indicates that the collection is a dictionary.
            /// </summary>
            IsDictionary = 1 << 3,

            /// <summary>
            /// Should be enabled for generic types to process embedded non-simple elements.
            /// </summary>
            IsGeneric = 1 << 4,

            /// <summary>
            /// Should be set for stack-like collections
            /// </summary>
            ReverseElements = 1 << 5,

            /// <summary>
            /// Only in HybridDictionary
            /// </summary>
            HasCaseInsensitivity = 1 << 6,

            /// <summary>
            /// For types that can be both read-only and read-write (now in OrderedDictionary)
            /// </summary>
            HasReadOnly = 1 << 7,

            /// <summary>
            /// Indicates that the "collection" is a single element
            /// (now for DictionaryEntry, KeyValuePair: special "collections" with exactly two elements, which are easy to encode along with collection types)
            /// </summary>
            IsSingleElement = 1 << 8,

            /// <summary>
            /// Indicates that <see cref="EnumComparer{TEnum}"/> is the default for enum element types.
            /// </summary>
            DefaultEnumComparer = 1 << 9,

            /// <summary>
            /// Indicates that even default comparer cannot be null.
            /// </summary>
            NonNullDefaultComparer = 1 << 10,
        }

        /// <summary>
        /// Possible arguments of a collection constructor
        /// </summary>
        private enum CollectionCtorArguments
        {
            Capacity,
            Comparer,
            CaseInsensitivity
        }

        /// <summary>
        /// Contains some serialization-time attributes for non-primitive types.
        /// This ensures that the deserializer can process a type (or at least throw a reasonable exception)
        /// if it changed since serialization (eg. sealed vs non-sealed, serialization way, etc.)
        /// </summary>
        [Flags]
        private enum TypeAttributes // : byte
        {
            None,

            ValueType = 1,
            Sealed = 1 << 1,
            Enum = 1 << 2,
            RecursiveObjectGraph = 1 << 3,
            CustomSerialized = 1 << 4,
            BinarySerializable = 1 << 5,
            RawStruct = 1 << 6,

        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// A mocked <see cref="Type"/> by name. Not derived from <see cref="Type"/> because that has tons of abstract methods.
        /// </summary>
        private sealed class TypeByString : MemberInfo
        {
            #region Properties

            public override MemberTypes MemberType => MemberTypes.TypeInfo;
            public override string Name { get; }
            public override Type? DeclaringType => null;
            public override Type? ReflectedType => null;

            #endregion

            #region Constructors

            public TypeByString(string? assemblyName, string typeName) => Name = typeName + ", " + assemblyName;

            #endregion

            #region Methods

            public override object[] GetCustomAttributes(bool inherit) => Reflector.EmptyObjects;
            public override bool IsDefined(Type attributeType, bool inherit) => false;
            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => Reflector.EmptyObjects;
            public override string ToString() => Name;
            public override bool Equals(object? obj) => obj is TypeByString other && Name == other.Name;
            public override int GetHashCode() => Name.GetHashCode();

            #endregion
        }

        #endregion

        #region Nested Structs

        #region Compressible<T> struct

        /// <summary>
        /// A wrapper type for 7-bit encoded types if they are encoded by index rather than DataTypes.
        /// </summary>
        // ReSharper disable once UnusedTypeParameter - used for encoding compressed type
        private struct Compressible<T> where T : struct
        {
        }

        #endregion

        #region GenericMethodDefinitionPlaceholder struct

        /// <summary>
        /// An indicator type for generic method parameters.
        /// </summary>
        private struct GenericMethodDefinitionPlaceholder
        {
        }

        #endregion

        #endregion 
        
        #endregion

        #region Fields

        #region Static Fields

        private static readonly Type onSerializingAttribute = typeof(OnSerializingAttribute);
        private static readonly Type onSerializedAttribute = typeof(OnSerializedAttribute);
        private static readonly Type onDeserializingAttribute = typeof(OnDeserializingAttribute);
        private static readonly Type onDeserializedAttribute = typeof(OnDeserializedAttribute);

        private static readonly Type compressibleType = typeof(Compressible<>);
        private static readonly Type serializableType = typeof(ISerializable);
        private static readonly Type binarySerializableType = typeof(IBinarySerializable);
        private static readonly Type genericMethodDefinitionPlaceholderType = typeof(GenericMethodDefinitionPlaceholder);

        private static readonly Dictionary<DataTypes, CollectionSerializationInfo> serializationInfo = new Dictionary<DataTypes, CollectionSerializationInfo>(EnumComparer<DataTypes>.Comparer)
        {
            // generic collections
            { DataTypes.Array, CollectionSerializationInfo.Default }, // Could be IsGeneric, but does not matter as arrays are handled separately
            {
                DataTypes.List, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasCapacity,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity }
                }
            },
            {
                DataTypes.LinkedList, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric,
                    SpecificAddMethod = nameof(LinkedList<_>.AddLast)
                }
            },
            {
                DataTypes.HashSet, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasEqualityComparer,
#if NET35 || NET40 || NET45 || NETSTANDARD2_0
                    CtorArguments = new[] { CollectionCtorArguments.Comparer },
#else
                    CtorArguments = new[] { CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer },
#endif
                    SpecificAddMethod = nameof(HashSet<_>.Add) // because faster than via ICollection<T>.Add
                }
            },
            {
                DataTypes.Queue, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity },
                    SpecificAddMethod = nameof(Queue<_>.Enqueue)
                }
            },
            {
                DataTypes.Stack, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.ReverseElements,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity },
                    SpecificAddMethod = nameof(Stack<_>.Push)
                }
            },
            {
                DataTypes.CircularList, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasCapacity,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity }
                }
            },
#if !NET35
            {
                DataTypes.SortedSet, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer },
                    SpecificAddMethod = nameof(SortedSet<_>.Add)
                }
            },
            {
                DataTypes.ConcurrentBag, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric,
                    SpecificAddMethod = nameof(ConcurrentBag<_>.Add)
                }
            },
            {
                DataTypes.ConcurrentQueue, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric,
                    SpecificAddMethod = nameof(ConcurrentQueue<_>.Enqueue)
                }
            },
            {
                DataTypes.ConcurrentStack, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.ReverseElements,
                    SpecificAddMethod = nameof(ConcurrentStack<_>.Push)
                }
            },
#endif

            // non-generic collections
            {
                DataTypes.ArrayList, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.HasCapacity,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity }
                }
            },
            {
                DataTypes.QueueNonGeneric, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.None,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity },
                    SpecificAddMethod = nameof(Queue.Enqueue)
                }
            },
            {
                DataTypes.StackNonGeneric, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.ReverseElements,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity },
                    SpecificAddMethod = nameof(Stack.Push)
                }
            },
            { DataTypes.StringCollection, CollectionSerializationInfo.Default },

            // generic dictionaries
            {
                DataTypes.Dictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer }
                }
            },
            {
                DataTypes.SortedList, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasCapacity | CollectionInfo.IsDictionary | CollectionInfo.HasComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer }
                }
            },
            {
                DataTypes.SortedDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer }
                }
            },
            { DataTypes.KeyValuePair, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.IsSingleElement } },
            { DataTypes.KeyValuePairNullable, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.IsSingleElement } },
            {
                DataTypes.CircularSortedList, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasCapacity | CollectionInfo.HasComparer | CollectionInfo.DefaultEnumComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer }
                }
            },
#if !NET35
            {
                DataTypes.ConcurrentDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer
#if NETFRAMEWORK || NETSTANDARD
                        | CollectionInfo.NonNullDefaultComparer
#endif
                    ,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer }
                }
            },
#endif

            // non-generic dictionaries
            {
                DataTypes.Hashtable, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer }
                }
            },
            {
                DataTypes.SortedListNonGeneric, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.HasCapacity | CollectionInfo.IsDictionary | CollectionInfo.HasComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer, CollectionCtorArguments.Capacity }
                }
            },
            {
                DataTypes.ListDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsDictionary | CollectionInfo.HasComparer, // yes, it uses Comparer and not EqualityComparer
                    CtorArguments = new[] { CollectionCtorArguments.Comparer }
                }
            },
            {
                DataTypes.HybridDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsDictionary | CollectionInfo.HasCaseInsensitivity,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity, CollectionCtorArguments.CaseInsensitivity }
                }
            },
            {
                DataTypes.OrderedDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer | CollectionInfo.HasReadOnly,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer }
                }
            },
            {
                DataTypes.StringDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsDictionary,
                    SpecificAddMethod = nameof(StringDictionary.Add)
                }
            },
            { DataTypes.DictionaryEntry, new CollectionSerializationInfo { Info = CollectionInfo.IsDictionary | CollectionInfo.IsSingleElement } },
            { DataTypes.DictionaryEntryNullable, new CollectionSerializationInfo { Info = CollectionInfo.IsDictionary | CollectionInfo.IsSingleElement } }
        };

        private static readonly IThreadSafeCacheAccessor<Type, Dictionary<Type, IEnumerable<MethodInfo>?>> methodsByAttributeCache
            = ThreadSafeCacheFactory.Create<Type, Dictionary<Type, IEnumerable<MethodInfo>?>>(_ => new Dictionary<Type, IEnumerable<MethodInfo>?>(4), LockFreeCacheOptions.Profile256);

        // including string and the abstract enum and array types
        private static readonly Dictionary<Type, DataTypes> primitiveTypes = new Dictionary<Type, DataTypes>
        {
            { Reflector.BoolType, DataTypes.Bool },
            { Reflector.ByteType, DataTypes.UInt8 },
            { Reflector.SByteType, DataTypes.Int8 },
            { Reflector.ShortType, DataTypes.Int16 },
            { Reflector.UShortType, DataTypes.UInt16 },
            { Reflector.IntType, DataTypes.Int32 },
            { Reflector.UIntType, DataTypes.UInt32 },
            { Reflector.LongType, DataTypes.Int64 },
            { Reflector.ULongType, DataTypes.UInt64 },
            { Reflector.CharType, DataTypes.Char },
            { Reflector.StringType, DataTypes.String },
            { Reflector.FloatType, DataTypes.Single },
            { Reflector.DoubleType, DataTypes.Double },
            { Reflector.IntPtrType, DataTypes.IntPtr },
            { Reflector.UIntPtrType, DataTypes.UIntPtr },
            { Reflector.VoidType, DataTypes.Void },
        };

        private static readonly Dictionary<Type, DataTypes> supportedNonPrimitiveElementTypes = new Dictionary<Type, DataTypes>
        {
            { Reflector.DecimalType, DataTypes.Decimal },
            { Reflector.DateTimeType, DataTypes.DateTime },
            { Reflector.DateTimeOffsetType, DataTypes.DateTimeOffset },
            { Reflector.TimeSpanType, DataTypes.TimeSpan },
            { Reflector.ObjectType, DataTypes.Object },
            { Reflector.RuntimeType, DataTypes.RuntimeType },
            { Reflector.DBNullType, DataTypes.DBNull },
            { typeof(Version), DataTypes.Version },
            { Reflector.GuidType, DataTypes.Guid },
            { typeof(Uri), DataTypes.Uri },
            { typeof(StringBuilder), DataTypes.StringBuilder },
            { Reflector.BitArrayType, DataTypes.BitArray },
            { typeof(BitVector32), DataTypes.BitVector32 },
            { typeof(BitVector32.Section), DataTypes.BitVector32Section },
#if !NET35
		    { Reflector.BigIntegerType, DataTypes.BigInteger},
#endif
#if NETCOREAPP3_0_OR_GREATER
            { Reflector.RuneType, DataTypes.Rune },
#endif
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            { typeof(Index), DataTypes.Index },
            { typeof(Range), DataTypes.Range },
#endif
#if NET5_0_OR_GREATER
            { Reflector.HalfType, DataTypes.Half },
#endif
#if NET6_0_OR_GREATER
            { Reflector.DateOnlyType, DataTypes.DateOnly },
            { Reflector.TimeOnlyType, DataTypes.TimeOnly },
#endif
        };

        private static readonly Dictionary<Type, DataTypes> supportedCollections = new Dictionary<Type, DataTypes>
        {
            // Array is not here because that is an abstract type. Arrays are handled separately.
            { Reflector.ListGenType, DataTypes.List },
            { typeof(Queue<>), DataTypes.Queue },
            { typeof(Stack<>), DataTypes.Stack },
            { typeof(LinkedList<>), DataTypes.LinkedList },
            { typeof(HashSet<>), DataTypes.HashSet },
#if !NET35
            { typeof(SortedSet<>), DataTypes.SortedSet },
            { typeof(ConcurrentBag<>), DataTypes.ConcurrentBag },
            { typeof(ConcurrentQueue<>), DataTypes.ConcurrentQueue },
            { typeof(ConcurrentStack<>), DataTypes.ConcurrentStack },
#endif

            { typeof(ArrayList), DataTypes.ArrayList },
            { typeof(Queue), DataTypes.QueueNonGeneric },
            { typeof(Stack), DataTypes.StackNonGeneric },
            { Reflector.StringCollectionType, DataTypes.StringCollection },

            { Reflector.DictionaryGenType, DataTypes.Dictionary },
            { typeof(SortedList<,>), DataTypes.SortedList },
            { typeof(SortedDictionary<,>), DataTypes.SortedDictionary },
            { typeof(CircularSortedList<,>), DataTypes.CircularSortedList },
#if !NET35
            { typeof(ConcurrentDictionary<,>), DataTypes.ConcurrentDictionary },
#endif

            { typeof(Hashtable), DataTypes.Hashtable },
            { typeof(SortedList), DataTypes.SortedListNonGeneric },
            { typeof(ListDictionary), DataTypes.ListDictionary },
            { typeof(HybridDictionary), DataTypes.HybridDictionary },
            { typeof(OrderedDictionary), DataTypes.OrderedDictionary },
            { typeof(StringDictionary), DataTypes.StringDictionary },

            { Reflector.KeyValuePairType, DataTypes.KeyValuePair },
            { Reflector.DictionaryEntryType, DataTypes.DictionaryEntry },
        };

        #endregion

        #region Instance Fields

        private BinarySerializationOptions serializationOptions;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Options used for serialization and deserialization.
        /// <br/>See the <see cref="BinarySerializationOptions"/> enumeration for details.
        /// </summary>
        public BinarySerializationOptions Options
        {
            get => serializationOptions;
            set
            {
                if (!value.AllFlagsDefined())
                    Throw.FlagsEnumArgumentOutOfRange(Argument.value, value);

                serializationOptions = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="SerializationBinder"/> that performs type conversions to and from <see cref="string">string</see>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <remarks>
        /// <para>By default, the binder is not called for natively supported types.</para>
        /// <para>If the <see cref="BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes"/> flag is set in <see cref="Options"/>,
        /// then the binder is called for the non-primitive natively supported types.</para>
        /// <para>This formatter does not call the binder types that have element types, for constructed generic types and generic parameter types.
        /// Instead, the binder is called only for the element types, the generic type definition and the generic arguments separately.</para>
        /// <note>In .NET Framework 3.5 setting this property has no effect during serialization unless the binder implements
        /// the <see cref="ISerializationBinder"/> interface.</note>
        /// <note type="tip">If you serialize forwarded types that have no defined forwarding by the <see cref="TypeForwardedToAttribute"/> and <see cref="TypeForwardedFromAttribute"/>
        /// attributes, then to ensure emitting compatible assembly identities on different .NET platforms use the <see cref="ForwardedTypesSerializationBinder"/>,
        /// define the missing mappings by the <see cref="ForwardedTypesSerializationBinder.AddType">AddType</see> method and set its <see cref="ForwardedTypesSerializationBinder.WriteLegacyIdentity"/> property to <see langword="true"/>.
        /// Alternatively, you can use the <see cref="WeakAssemblySerializationBinder"/> or you can just serialize the object without
        /// assembly information by setting the <see cref="BinarySerializationOptions.OmitAssemblyQualifiedNames"/> flag in the <see cref="Options"/>.</note>
        /// <note type="security"><para>If you use binders for deserialization, then setting the <see cref="BinarySerializationOptions.SafeMode"/> flag in the <see cref="Options"/>
        /// cannot prevent loading assemblies by the binder itself. The binders in this library that can perform automatic type resolving,
        /// such the <see cref="WeakAssemblySerializationBinder"/> and <see cref="ForwardedTypesSerializationBinder"/> have their own <c>SafeMode</c> property.
        /// Make sure to set them to <see langword="true"/>&#160;to prevent loading assemblies by the binders themselves.</para>
        /// <para>See the security notes at the <strong>Remarks</strong> section of the <see cref="BinarySerializationFormatter"/> class for more details.</para></note>
        /// </remarks>
        public SerializationBinder? Binder { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="StreamingContext"/> used for serialization and deserialization.
        /// </summary>
        public StreamingContext Context { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="ISurrogateSelector"/> can be used to customize serialization and deserialization.
        /// </summary>
        public ISurrogateSelector? SurrogateSelector { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="BinarySerializationFormatter"/> class.
        /// </summary>
        /// <param name="options">Options used for serialization. This parameter is optional.
        /// <br/>Default value: <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/>, <see cref="BinarySerializationOptions.CompactSerializationOfStructures"/>.</param>
        public BinarySerializationFormatter(BinarySerializationOptions options = BinarySerializer.DefaultOptions)
        {
            Context = new StreamingContext(StreamingContextStates.All);
            Options = options;
        }

        #endregion

        #region Methods

        #region Static Methods

        private static DataTypes GetCollectionDataType(DataTypes dt) => dt & DataTypes.CollectionTypes;
        private static DataTypes GetElementDataType(DataTypes dt) => dt & ~DataTypes.CollectionTypes;
        private static DataTypes GetUnderlyingSimpleType(DataTypes dt) => dt & DataTypes.SimpleTypes;
        private static DataTypes GetCollectionOrElementType(DataTypes dt) => (dt & DataTypes.CollectionTypes) != DataTypes.Null ? dt & DataTypes.CollectionTypes : dt & ~DataTypes.CollectionTypes;
        private static bool IsElementType(DataTypes dt) => (dt & ~DataTypes.CollectionTypes) != DataTypes.Null;
        private static bool IsCollectionType(DataTypes dt) => (dt & DataTypes.CollectionTypes) != DataTypes.Null;
        private static bool IsNullable(DataTypes dt) => (dt & DataTypes.Nullable) != DataTypes.Null || dt is DataTypes.DictionaryEntryNullable or DataTypes.KeyValuePairNullable;
        private static bool IsCompressible(DataTypes dt) => (uint)((dt & DataTypes.SimpleTypes) - DataTypes.Int16) <= DataTypes.UIntPtr - DataTypes.Int16;
        private static bool IsCompressed(DataTypes dt) => (dt & DataTypes.Store7BitEncoded) != DataTypes.Null;
        private static bool IsEnum(DataTypes dt) => (dt & DataTypes.Enum) != DataTypes.Null;
        private static bool IsPureType(DataTypes dt) => !IsEnum(dt) && (dt & DataTypes.ImpureType) != DataTypes.ImpureType;
        private static bool IsPureSimpleType(DataTypes dt) => (dt & (DataTypes.SimpleTypes | DataTypes.Nullable)) == dt && (dt & DataTypes.SimpleTypes) < DataTypes.ImpureType;
        private static bool IsDictionary(DataTypes dt) => (dt & DataTypes.Dictionary) != DataTypes.Null;
        private static bool CanHaveRecursion(DataTypes dt) => (dt & DataTypes.SimpleTypes) is DataTypes.RecursiveObjectGraph or DataTypes.BinarySerializable or DataTypes.Object;
        private static bool CanBeEncoded(DataTypes dt) => IsCollectionType(dt) || dt is DataTypes.Pointer or DataTypes.ByRef;
        private static bool IsImpureTypeButEnum(DataTypes dt) => (dt & DataTypes.ImpureType) == DataTypes.ImpureType;
        private static bool IsImpureType(DataTypes dt) => IsEnum(dt) || IsImpureTypeButEnum(dt);
        private static bool IsExtended(DataTypes dt) => (dt & DataTypes.Extended) != DataTypes.Null;

        private static void Write7BitInt(BinaryWriter bw, int value)
        {
            uint v = (uint)value;
            while (v >= 0x80UL)
            {
                bw.Write((byte)(v | 0x80UL));
                v >>= 7;
            }

            bw.Write((byte)v);
        }

        /// <summary>
        /// Must be separated from Write7BitInt because -1 would result 10 bytes here and 5 there
        /// </summary>
        private static void Write7BitLong(BinaryWriter bw, ulong value)
        {
            while (value >= 0x80UL)
            {
                bw.Write((byte)(value | 0x80UL));
                value >>= 7;
            }

            bw.Write((byte)value);
        }

        private static int Read7BitInt(BinaryReader br)
        {
            int result = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream. Max 4 * 7 bits are valid
                if (shift == 35)
                    Throw.SerializationException(Res.BinarySerializationInvalidStreamData);

                b = br.ReadByte();

                result |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);

            return result;
        }

        private static long Read7BitLong(BinaryReader br)
        {
            long result = 0L;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream. Max 9 * 7 bits are valid
                if (shift == 70)
                    Throw.SerializationException(Res.BinarySerializationInvalidStreamData);

                b = br.ReadByte();

                result |= (b & 0x7FL) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);

            return result;
        }

        private static void WriteDataType(BinaryWriter bw, DataTypes dataType)
        {
            // using the low byte only
            if ((dataType & (DataTypes)0xFF) == dataType)
            {
                Debug.Assert(!IsExtended(dataType));
                bw.Write((byte)dataType);
                return;
            }

            // writing the whole word
            bw.Write((ushort)(dataType | DataTypes.Extended));
        }

        private static DataTypes ReadDataType(BinaryReader br)
        {
            var result = (DataTypes)br.ReadByte();
            if (IsExtended(result))
            {
                result |= (DataTypes)(br.ReadByte() << 8);
                result &= ~DataTypes.Extended;
            }

            return result;
        }

        /// <summary>
        /// Converts a <see cref="DataTypes"/> enumeration into the corresponding string representation.
        /// This method is needed because <see cref="Enum.ToString()"/> and <see cref="Enum{TEnum}.ToString(TEnum,EnumFormattingOptions,string)"/>
        /// cannot always handle the fields and flags structure of <see cref="DataTypes"/> enum.
        /// </summary>
        private static string DataTypeToString(DataTypes dataType)
        {
            if (dataType.In(DataTypes.Null, DataTypes.SimpleTypes, DataTypes.CollectionTypes))
                return dataType.ToString<DataTypes>();

            StringBuilder result = new StringBuilder();
            if (IsCollectionType(dataType))
                result.Append(Enum<DataTypes>.ToString(GetCollectionDataType(dataType), EnumFormattingOptions.CompoundFlagsAndNumber, " | "));
            if (IsElementType(dataType))
            {
                if (result.Length > 0)
                    result.Insert(0, " | ");
                result.Insert(0, Enum<DataTypes>.ToString(GetElementDataType(dataType), EnumFormattingOptions.CompoundFlagsAndNumber, " | "));
            }

            return result.ToString();
        }

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Serializes an object into a byte array.
        /// </summary>
        /// <param name="data">The object to serialize</param>
        /// <returns>Serialized raw data of the object</returns>
        [SecuritySafeCritical]
        public byte[] Serialize(object? data)
        {
            MemoryStream result;
            using (BinaryWriter bw = new BinaryWriter(result = new MemoryStream()))
            {
                var manager = new SerializationManager(Context, Options, Binder, SurrogateSelector);
                manager.WriteRoot(bw, data);
                return result.ToArray();
            }
        }

        /// <summary>
        /// Deserializes the specified part of a byte array into an object.
        /// </summary>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="offset">Points to the starting position of the object data in <paramref name="rawData"/>. This parameter is optional.
        /// <br/>Default value: <c>0</c>.</param>
        /// <returns>The deserialized data.</returns>
        /// <overloads>In the two-parameter overload the start offset of the data to deserialize can be specified.</overloads>
        public object? Deserialize(byte[] rawData, int offset = 0)
        {
            using (BinaryReader br = new BinaryReader(offset == 0 ? new MemoryStream(rawData) : new MemoryStream(rawData, offset, rawData.Length - offset)))
                return DeserializeByReader(br);
        }

        /// <summary>
        /// Serializes the given <paramref name="data"/> into a <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream, into which the data is written. The stream must support writing and will remain open after serialization.</param>
        /// <param name="data">The data that will be written into the stream.</param>
        [SecuritySafeCritical]
        public void SerializeToStream(Stream stream, object? data) => SerializeByWriter(new BinaryWriter(stream), data);

        /// <summary>
        /// Deserializes data beginning at current position of given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream, from which the data is read. The stream must support reading and will remain open after deserialization.</param>
        /// <returns>The deserialized data.</returns>
        public object? DeserializeFromStream(Stream stream) => DeserializeByReader(new BinaryReader(stream));

        /// <summary>
        /// Serializes the given <paramref name="data"/> by using the provided <paramref name="writer"/>.
        /// </summary>
        /// <remarks>
        /// <note>This method produces compatible serialized data with <see cref="Serialize">Serialize</see>
        /// and <see cref="SerializeToStream">SerializeToStream</see> methods only when encoding of the writer is UTF-8. Otherwise, you must use <see cref="DeserializeByReader">DeserializeByReader</see> with the same encoding as here.</note>
        /// </remarks>
        /// <param name="writer">The writer that will used to serialize data. The writer will remain opened after serialization.</param>
        /// <param name="data">The data that will be written by the writer.</param>
        [SecuritySafeCritical]
        public void SerializeByWriter(BinaryWriter writer, object? data)
        {
            if (writer == null!)
                Throw.ArgumentNullException(Argument.writer);
            var manager = new SerializationManager(Context, Options, Binder, SurrogateSelector);
            manager.WriteRoot(writer, data);
        }

        /// <summary>
        /// Deserializes data beginning at current position of given <paramref name="reader"/>.
        /// </summary>
        /// <remarks>
        /// <note>If data was serialized by <see cref="Serialize">Serialize</see> or <see cref="SerializeToStream">SerializeToStream</see> methods, then
        /// <paramref name="reader"/> must use UTF-8 encoding to get correct result. If data was serialized by the <see cref="SerializeByWriter">SerializeByWriter</see> method, then you must use the same encoding as there.</note>
        /// </remarks>
        /// <param name="reader">The reader that will be used to deserialize data. The reader will remain opened after deserialization.</param>
        /// <returns>The deserialized data.</returns>
        [SecuritySafeCritical]
        public object? DeserializeByReader(BinaryReader reader)
        {
            if (reader == null!)
                Throw.ArgumentNullException(Argument.reader);
            var manager = new DeserializationManager(Context, Options, Binder, SurrogateSelector);
            return manager.Deserialize(reader);
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        object? IFormatter.Deserialize(Stream serializationStream) => DeserializeFromStream(serializationStream);
        void IFormatter.Serialize(Stream serializationStream, object? graph) => SerializeToStream(serializationStream, graph);

        #endregion

        #endregion

        #endregion
    }
}
