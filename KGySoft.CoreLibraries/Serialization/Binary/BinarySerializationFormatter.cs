﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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
#if !NET35
using System.Collections.Concurrent;
#endif
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif
using System.Collections.Generic;
#if NETCOREAPP
using System.Collections.Immutable;
#endif
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
#if NETCOREAPP
using System.Linq;
#endif
#if !NET35
using System.Numerics;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Serialization.Xml;

#endregion

#region Suppressions

#if !NET9_0_OR_GREATER
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif
#if NET5_0_OR_GREATER
#pragma warning disable CS8768 // Nullability of return type does not match implemented member - BinarySerializationFormatter supports de/serializing null
#endif
#if NET8_0_OR_GREATER
#pragma warning disable SYSLIB0011 // BinaryFormatter serialization is obsolete and should not be used - false alarm, not using BinaryFormatter but implementing IFormatter in BinarySerializationFormatter, which is a safer replacement of BinaryFormatter
#pragma warning disable SYSLIB0050 // ISurrogateSelector/StreamingContext/StreamingContextStates is obsolete - needed by IFormatter implementation, which is maintained for compatibility reasons
#endif

#endregion

/* How to add a new type
 * =====================
 *
 * I. Adding a simple type
 * ~~~~~~~~~~~~~~~~~~~~~~~
 * When NOT to add:
 * - If contains a delegate
 * - If may contain a pointer or may require unmanaged preallocated memory
 * When to add with special care and only if really justified
 * - If type is abstract, non-sealed or internal (e.g. object, RuntimeType)
 * - If type has known values but the type in general cannot be supported (e.g. comparer singleton instances vs. general instances)
 * - If type is impure (ambiguous without extra info) or may contain elements that make safe deserialization impossible
 * 1. Add type to DataTypes bits 0..5 (adjust free places in comments) or to bits 16..23 (Extended)
 * 2. If type is pure (unambiguous by DataTypes) add it to supportedNonPrimitiveElementTypes.
 *    If the added data type is abstract and there is a special logic to determine if the type is supported, add the logic to DetermineSpecialSupport.
 *    Otherwise, handle it in SerializationManager.GetDataType/GetImpureDataType
 * 3. If type is pure handle it type in SerializationManager.WritePureObject. If serialization is more than one line create a static WriteXXX.
 *    Otherwise, handle it in SerializationManager.WriteImpureObject. Create a WriteXXX that can be called separately from writing type.
 * 4. Handle type in DeserializationManager.ReadObject:
 *    - For reference types call TryGetFromCache (or TryGetFromCacheOrAddPlaceholder if ReadXXX also accesses the cache).
 *    - Always set createdResult if addToCache is not handled in ReadXXX (e.g. impure types handle it by themselves).
 *    - If type is value type and it can read nested cached objects the caching condition is different (see StringSegment)
 * 5. Add type to DataTypeDescriptor.GetElementType.
 *    If type is non-pure and WriteXXX starts with WriteType, then you can put it into the group with ReadType.
 * 6. Add type to unit test:
 *    - SerializeSimpleTypes
 *    - SerializeSimpleArrays
 *    - SerializeNullableArrays (value types)
 *    - SerializationSurrogateTest
 * 7. Add type to description - Natively supported simple types
 *    - Adjust the #if for pragma warning for CS1574 if it is introduced in a new .NET version
 *
 * II. Adding a collection type
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * When NOT to add
 * - If may contain delegate or event subscriptions (e.g. Cache<TKey, TValue>, ObservableCollection<T>)
 * - If may wrap another exposed collection of any type (e.g. Collection<T>, exposed by Items)
 * - If may wrap another collection that is not exposed, though the wrapped collection type may affect the behavior (locking collections, BlockingCollection<T>)
 * When to add with special care and only if really justified
 * - If type is abstract, non-sealed or internal (e.g. frozen collections)
 * - If may contain a dependency that can be extracted legally (e.g. underlying array/string/manager of Memory<T> can be extracted by MemoryMarshal)
 * 1. Add type to DataTypes 8..13 bits (adjust free places in comments) or to bits 24..30 (Extended)
 *    - 1..15 << 8: Generic collections
 *    - 16..31 << 8: Non-generic collections or special collections
 *    - 32..47 << 8: Generic/specialized dictionaries
 *    - 48..63 << 8: Non-generic dictionaries
 *    - 1..63 << 24: Extended collections. If value type, must be here to support NullableExtendedCollection.
 *    - 64..127 << 24: Extended dictionaries. If value type, must be here to support NullableExtendedCollection.
 * 2. Update serializationInfo initializer - mind the groups of 1.
 *    - If new CollectionInfo flag has to be defined, a property in CollectionSerializationInfo might be also needed
 * 3. If type is not abstract add it to supportedCollections
 *    If the added data type is abstract add the needed logic to DetermineSpecialSupport.
 * 4. Add type to DataTypeDescriptor.GetCollectionType (even abstract types) - mind groups
 *    - If collection has a known fixed size and is deserialized by a backing array then DataTypeDescriptor.FixedItemsSize might be needed to adjusted, too.
 * 5. If needed, update CollectionSerializationInfo.WriteSpecificProperties and InitializeCollection (e.g. new flag in 2.)
 *    - If new CtorArguments were added to 2., then check also GetInitializer and CreateCollection
 * 6. If collection type is an ordered non-IList collection, or an unordered non-ICollection<T> collection,
 *    then handle it in AddCollectionElement/AddDictionaryElement. Add new usage reference if needed.
 * 7. Add type to unit test:
 *    - SerializeSimpleGenericCollections or SerializeSimpleNonGenericCollections
 *    - SerializeNullableArrays (value types)
 *    - SerializeSupportedDictionaries - twice when generic dictionary type; otherwise, only once
 *   [- SerializeComplexGenericCollections - when generic]
 *   [- SerializationSurrogateTest]
 *   [- SerializeCircularReferences - if new usage reference is added in 6. add it to TestData.Box<T>, too]
 * 8. Add type to description - Collections
 *    - Adjust the #if for pragma warning for CS1574 if it is introduced in a new .NET version
 *
 * To debug the serialized stream of the test cases set BinarySerializerTest.dumpDetails and see the console output.
 */
namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Serializes and deserializes objects in binary format.
    /// <div style="display: none;"><br/>See the <a href="https://koszeggy.github.io/docs/corelibraries/html/T_KGySoft_Serialization_Binary_BinarySerializationFormatter.htm">online help</a> for a more detailed description with examples.</div>
    /// </summary>
    /// <seealso cref="BinarySerializer"/>
    /// <seealso cref="BinarySerializationOptions"/>
    /// <seealso cref="IBinarySerializable"/>
    /// <remarks>
    /// <note type="warning">The fundamental goal of binary serialization is to store the bitwise content of an object, hence in general case (when custom types
    /// are involved) it relies on field values, including private ones that can change from version to version. Therefore, binary serialization is recommended
    /// only if your serialized objects purely consist of natively supported types (see them below). If you need to serialize custom types, then it is recommended
    /// to do it for in-process purposes only, such as deep cloning or undo/redo, etc. If it is known that a type will be deserialized in another environment and
    /// it can be completely restored by its public members, then a text-based serialization (see also <see cref="XmlSerializer"/>) can be a better choice.</note>
    /// <note type="security"><para>If the serialization stream may come from an untrusted source (e.g. remote service, file or database) make sure you enable
    /// the <see cref="BinarySerializationOptions.SafeMode"/> option. It prevents loading assemblies during the deserialization, denies resolving unexpected natively not supported types by name,
    /// does not allow instantiating natively not supported types that are not serializable, and guards against some attacks that may cause <see cref="OutOfMemoryException"/>.
    /// When using <see cref="BinarySerializationOptions.SafeMode"/> all of the natively not supported types, whose assembly qualified names are
    /// stored in the serialization stream must be explicitly declared as expected types in the deserialization methods, including apparently innocent types such as <see langword="enum"/>s.</para>
    /// <para>Please also note that in safe mode some system types are forbidden to use even if they are serializable and are specified as expected types.
    /// In the .NET Framework there are some serializable types in the fundamental core assemblies that can be exploited for several attacks (causing unresponsiveness,
    /// <see cref="StackOverflowException"/> or even files to be deleted). Starting with .NET Core these types are not serializable anymore and some of them have been moved to separate NuGet packages anyway,
    /// but the <see cref="BinaryFormatter"/> class in the .NET Framework is still vulnerable against such attacks. When using the <see cref="BinarySerializationOptions.SafeMode"/> flag,
    /// the <see cref="BinarySerializationFormatter"/> is protected against the known security issues on all platforms but of course it cannot guard you against every potentially harmful type if
    /// you explicitly specify them as expected types in the deserialization methods.</para>
    /// <para>Please also note that the <see cref="IFormatter"/> infrastructure has other security flaws as well but some of these can be reduced by the serializable types themselves.
    /// Most serializable types do not validate the incoming data. All serializable types that can have an invalid state regarding the field values
    /// should implement <see cref="ISerializable"/> and should throw a <see cref="SerializationException"/> from their serialization constructor if validation fails.
    /// The <see cref="BinarySerializationFormatter"/> wraps every other exception thrown by the constructor into a <see cref="SerializationException"/>.
    /// Not even the core .NET types have such validation, which is one reason why this library supports so many types natively. And for custom types
    /// see the example at the <a href="#example">Example: How to implement a custom serializable type</a> section.</para>
    /// <para>Starting with version 8.0.0 in safe mode the <see cref="Binder"/> property can only be <see langword="null"/> or a <see cref="ForwardedTypesSerializationBinder"/> instance if you set
    /// its <see cref="ForwardedTypesSerializationBinder.SafeMode"/> to <see langword="true"/>. Furthermore, in safe mode it is not allowed to set any surrogate selectors in the <see cref="SurrogateSelector"/> property.
    /// Please note that this library also contains a sort of serialization binders and surrogates, most of them have their own <c>SafeMode</c> property
    /// (e.g. <see cref="WeakAssemblySerializationBinder"/>, <see cref="CustomSerializerSurrogateSelector"/> or <see cref="NameInvariantSurrogateSelector"/>), still,
    /// not even they are allowed to be used when the <see cref="BinarySerializationOptions.SafeMode"/> option is enabled. It's because their safe mode just provide some not too strict general protection,
    /// instead of being able to filter a specific set of predefined types.</para>
    /// <para>If you must disable <see cref="BinarySerializationOptions.SafeMode"/> for some reason, then use binary serialization in-process only, or apply some cryptographically secure encryption
    /// to the serialization stream.</para></note>
    /// <para><see cref="BinarySerializationFormatter"/> aims to serialize objects effectively where the serialized data is almost always more compact than the results produced by the <see cref="BinaryFormatter"/> class.</para>
    /// <para><see cref="BinarySerializationFormatter"/> natively supports all the primitive types and a sort of other simple types, arrays, generic and non-generic collections.</para>
    /// </remarks>
    /// <example>
    /// <h4>Example 1: Size comparison to BinaryFormatter</h4>
    /// <note type="tip">Try also <a href="https://dotnetfiddle.net/nQfFrQ" target="_blank">online</a>.</note>
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
    /// <note>Serialization of natively supported types produce an especially compact result because these types are not serialized by traversing and storing the fields of the object graph recursively.
    /// This means not just better performance and improved security for these types but also prevents compatibility issues between different platforms because these types are not encoded by assembly identity and type name.
    /// Serialization of natively not supported types can be somewhat slower for the first time than by <see cref="BinaryFormatter"/> but the serialized result is almost always shorter than the one by <see cref="BinaryFormatter"/>,
    /// especially when generic types are involved.</note>
    /// <h4>Example 2: How to implement a custom serializable type<a name="example">&#160;</a></h4>
    /// <note type="tip">For the most compact result and to avoid using the obsoleted serialization infrastructure in .NET 8.0 and above it is recommended to implement the <see cref="IBinarySerializable"/> interface.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="IBinarySerializable"/> interface for details and examples.</note>
    /// <para>The following example shows how to apply validation for a serializable class that does not implement <see cref="ISerializable"/> so it will be serialized by its fields.</para>
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Runtime.Serialization;
    ///  
    /// [Serializable]
    /// public class Example1
    /// {
    ///     public int IntProp { get; set; }
    ///     public string StringProp { get; set; }
    ///
    ///     // Regular validation when constructing the class normally
    ///     public Example1(int intValue, string stringValue)
    ///     {
    ///         if (intValue <= 0)
    ///             throw new ArgumentOutOfRangeException(nameof(intValue));
    ///         if (stringValue == null)
    ///             throw new ArgumentNullException(nameof(stringValue));
    ///         if (stringValue.Length == 0)
    ///             throw new ArgumentException("Value is empty", nameof(stringValue));
    ///
    ///         IntProp = intValue;
    ///         StringProp = stringValue;
    ///     }
    ///
    ///     // The validation for deserialization. This is executed once the instance is deserialized.
    ///     // Another way for post-validation if you implement the IDeserializationCallback interface.
    ///     [OnDeserialized]
    ///     private void OnDeserialized(StreamingContext context)
    ///     {
    ///         if (IntProp <= 0 || String.IsNullOrEmpty(StringProp))
    ///             throw new SerializationException("Invalid serialization stream");
    ///     }
    /// }]]></code>
    /// <para>The example above may not be applicable if you need to validate the values in advance just like in the constructor.
    /// In that case you can implement the <see cref="ISerializable"/> interface and do the validation in the special serialization constructor:</para>
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Runtime.Serialization;
    ///  
    /// [Serializable]
    /// public class Example2 : ISerializable
    /// {
    ///     public int IntProp { get; set; }
    ///     public string StringProp { get; set; }
    ///
    ///     // Regular validation when constructing the class normally
    ///     public Example2(int intValue, string stringValue)
    ///     {
    ///         if (intValue <= 0)
    ///             throw new ArgumentOutOfRangeException(nameof(intValue));
    ///         if (stringValue == null)
    ///             throw new ArgumentNullException(nameof(stringValue));
    ///         if (stringValue.Length == 0)
    ///             throw new ArgumentException("Value is empty", nameof(stringValue));
    ///
    ///         IntProp = intValue;
    ///         StringProp = stringValue;
    ///     }
    ///
    ///     // Deserialization constructor with validation
    ///     private Example2(SerializationInfo info, StreamingContext context)
    ///     {
    ///         // GetValue throws SerializationException internally if the specified name is not found
    ///         if (info.GetValue(nameof(IntProp), typeof(int)) is not int i || i <= 0)
    ///             throw new SerializationException("IntProp is invalid");
    ///         if (info.GetValue(nameof(StringProp), typeof(string)) is not string s || s.Length == 0)
    ///             throw new SerializationException("StringProp is invalid");
    ///
    ///         IntProp = i;
    ///         StringProp = s;
    ///     }
    ///
    ///     // Serialization
    ///     void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    ///     {
    ///         info.AddValue(nameof(IntProp), IntProp);
    ///         info.AddValue(nameof(StringProp), StringProp);
    ///     }
    /// }]]></code>
    /// <note type="caution">The examples above are compatible also with <see cref="BinaryFormatter"/>.
    /// Still, it is not recommended to use <see cref="BinaryFormatter"/> because it is vulnerable at multiple levels.
    /// See the security notes at the top of the page for more details.</note>
    /// <h2>Further Remarks</h2>
    /// <para>Even if a type is not marked to be serializable by the <see cref="SerializableAttribute"/>, then you can use the <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/>
    /// option to force its serialization. Please note though that such types might not be able to be deserialized when <see cref="BinarySerializationOptions.SafeMode"/> is enabled. Alternatively, you can implement
    /// the <see cref="IBinarySerializable"/> interface, which can be used to produce a more compact custom serialization than the one provided by implementing the <see cref="ISerializable"/> interface.</para>
    /// <para>As <see cref="BinarySerializationFormatter"/> implements <see cref="IFormatter"/> it fully supports <see cref="SerializationBinder"/> and <see cref="ISurrogateSelector"/> implementations,
    /// though for security they are mainly disabled in safe mode. See security notes above for more details.</para>
    /// <para>A <see cref="SerializationBinder"/> can be used to deserialize types of unmatching assembly identity and to specify custom type-name mappings in both directions.
    /// Though <see cref="BinarySerializationFormatter"/> automatically handles <see cref="TypeForwardedToAttribute"/> and <see cref="TypeForwardedFromAttribute"/> (see also
    /// the <see cref="BinarySerializationOptions.IgnoreTypeForwardedFromAttribute"/> option), you can use also the <see cref="ForwardedTypesSerializationBinder"/>, especially for types without a defined forwarding.
    /// The <see cref="WeakAssemblySerializationBinder"/> can also be general solution if you need to ignore the assembly version or the complete assembly identity on resolving a type.
    /// If the name of the type has also been changed, then the <see cref="CustomSerializationBinder"/> can be used.
    /// See also the <strong>Remarks</strong> section of the <see cref="Binder"/> property for more details.</para>
    /// <para>An <see cref="ISurrogateSelector"/> can be used to customize serialization and deserialization. It can be used for types that cannot be handled anyway for some reason.
    /// For example, if you need to deserialize types, whose field names have been renamed you can use the <see cref="CustomSerializerSurrogateSelector"/>.
    /// Or, if the produced raw data has to be compatible with the obfuscated version of a type, then it can be achieved by the <see cref="NameInvariantSurrogateSelector"/>.</para>
    /// <para>There are three ways to serialize/deserialize an object. To serialize into a byte array use the <see cref="Serialize">Serialize</see> method.
    /// Its result can be deserialized by the <see cref="O:KGySoft.Serialization.Binary.BinarySerializationFormatter.Deserialize">Deserialize</see> methods.
    /// Additionally, you can use the <see cref="SerializeToStream">SerializeToStream</see>/<see cref="O:KGySoft.Serialization.Binary.BinarySerializationFormatter.DeserializeFromStream">DeserializeFromStream</see> methods to dump/read the result
    /// to and from a <see cref="Stream"/>, and the the <see cref="SerializeByWriter">SerializeByWriter</see>/<see cref="O:KGySoft.Serialization.Binary.BinarySerializationFormatter.DeserializeByReader">DeserializeByReader</see>
    /// methods to use specific <see cref="BinaryWriter"/> and <see cref="BinaryReader"/> instances for serialization and deserialization, respectively.</para>
    /// <note type="warning">In .NET Framework almost every type was serializable by <see cref="BinaryFormatter"/>. In .NET Core this principle has been
    /// radically changed. Many types are just simply not marked by the <see cref="SerializableAttribute"/> anymore (e.g. <see cref="MemoryStream"/>,
    /// <see cref="CultureInfo"/>, <see cref="Encoding"/>), whereas some types, which still implement <see cref="ISerializable"/> now
    /// throw a <see cref="PlatformNotSupportedException"/> from their <see cref="ISerializable.GetObjectData">GetObjectData</see>.
    /// Binary serialization of these types is not recommended anymore. If you still must serialize or deserialize such types
    /// see the <strong>Remarks</strong> section of the <see cref="CustomSerializerSurrogateSelector"/> for more details.</note>
    /// <h2>Natively supported simple types</h2>
    /// <para>Following types are natively supported. When these types are serialized, no type name is stored and there is no recursive traversal of the fields:
    /// <list type="bullet">
    /// <item><see langword="null"/> reference</item>
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
    /// <item><see cref="StringSegment"/></item>
    /// <item><see cref="CultureInfo"/></item>
    /// <item><see cref="CompareInfo"/></item>
    /// <item><see cref="Comparer"/></item>
    /// <item><see cref="CaseInsensitiveComparer"/></item>
    /// <item><see cref="Enum"/> types, though their names are saved in the serialization stream so they must be specified as expected types when deserializing in safe mode.</item>
    /// <item><see cref="Type"/> instances if they are runtime types. For natively not supported types their names are stored in the stream so in safe mode deserialization they must be specified as expected types.</item>
    /// <item>Known <see cref="StringComparer"/> implementations.</item>
    /// <item><see cref="StringSegmentComparer"/> implementations.</item>
    /// <item><see cref="Nullable{T}"/> types if type parameter is any of the supported types.</item>
    /// <item><see cref="KeyValuePair{TKey,TValue}"/> if <see cref="KeyValuePair{TKey,TValue}.Key"/> and <see cref="KeyValuePair{TKey,TValue}.Value"/> are any of the supported types.</item>
    /// <item><see cref="DictionaryEntry"/> if <see cref="DictionaryEntry.Key"/> and <see cref="DictionaryEntry.Value"/> are any of the supported types.</item>
    /// <item><see cref="StrongBox{T}"/> if <see cref="StrongBox{T}.Value"/> is any of the supported types.</item>
    /// <item>Default <see cref="EqualityComparer{T}"/> implementations.</item>
    /// <item>Default <see cref="Comparer{T}"/> implementations.</item>
    /// <item>Default <see cref="EnumComparer{T}"/> implementations.</item>
    /// <item><see cref="BigInteger"/> (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="Complex"/> (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="Vector2"/> (in .NET Framework 4.6 and above)</item>
    /// <item><see cref="Vector3"/> (in .NET Framework 4.6 and above)</item>
    /// <item><see cref="Vector4"/> (in .NET Framework 4.6 and above)</item>
    /// <item><see cref="Quaternion"/> (in .NET Framework 4.6 and above)</item>
    /// <item><see cref="Plane"/> (in .NET Framework 4.6 and above)</item>
    /// <item><see cref="Matrix3x2"/> (in .NET Framework 4.6 and above)</item>
    /// <item><see cref="Matrix4x4"/> (in .NET Framework 4.6 and above)</item>
    /// <item><see cref="Rune"/> (in .NET Core 3.0 and above)</item>
    /// <item><see cref="Index"/> (in .NET Standard 2.1 and above)</item>
    /// <item><see cref="Range"/> (in .NET Standard 2.1 and above)</item>
    /// <item><see cref="Half"/> (in .NET 5.0 and above)</item>
    /// <item><see cref="DateOnly"/> (in .NET 6.0 and above)</item>
    /// <item><see cref="TimeOnly"/> (in .NET 6.0 and above)</item>
    /// <item><see cref="Int128"/> (in .NET 7.0 and above)</item>
    /// <item><see cref="UInt128"/> (in .NET 7.0 and above)</item>
    /// <item><see cref="Tuple"/> types if generic arguments are any of the supported types (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="ValueTuple"/> types if generic arguments are any of the supported types (in .NET Standard 2.0 and above)</item>
    /// <item>Any object that implements the <see cref="IBinarySerializable"/> interface.</item>
    /// </list>
    /// <note>
    /// <list type="bullet">
    /// <item>Serializing <see cref="Enum"/> types will end up in a longer result raw data than serializing their numeric value, though the result will be still shorter than the one produced by <see cref="BinaryFormatter"/>.
    /// Please note that when deserializing in safe mode enums must be specified among the expected custom types. It's because though enums themselves are harmless, their type must be resolved just like any other type
    /// so a manipulated serialization stream may contain some altered type identity that could be resolved to a harmful type.</item>
    /// <item>If a <see cref="KeyValuePair{TKey,TValue}"/> contains natively not supported type arguments or <see cref="DictionaryEntry"/> has natively not supported keys an values,
    /// then for them recursive serialization may occur. If they contain non-serializable types, then the <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option should be enabled.
    /// The same applies also for <see cref="Tuple"/> and <see cref="ValueTuple"/> types.</item>
    /// </list>
    /// </note>
    /// </para>
    /// <h2>Natively supported generic collections</h2>
    /// <para>Following generic collections are natively supported. When their generic arguments are one of the simple types or other supported collections, then no recursive traversal of the fields occurs:
    /// <list type="bullet">
    /// <item><see cref="Array"/> of element types above or compound of other supported collections</item>
    /// <item><see cref="List{T}"/></item>
    /// <item><see cref="CircularList{T}"/></item>
    /// <item><see cref="LinkedList{T}"/></item>
    /// <item><see cref="HashSet{T}"/></item>
    /// <item><see cref="Queue{T}"/></item>
    /// <item><see cref="Stack{T}"/></item>
    /// <item><see cref="ThreadSafeHashSet{T}"/></item>
    /// <item><see cref="ArraySegment{T}"/></item>
    /// <item><see cref="ArraySection{T}"/></item>
    /// <item><see cref="Array2D{T}"/></item>
    /// <item><see cref="Array3D{T}"/></item>
    /// <item><see cref="CastArray{TFrom,TTo}"/></item>
    /// <item><see cref="CastArray2D{TFrom,TTo}"/></item>
    /// <item><see cref="CastArray3D{TFrom,TTo}"/></item>
    /// <item><see cref="SortedSet{T}"/> (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="ConcurrentBag{T}"/> (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="ConcurrentQueue{T}"/> (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="ConcurrentStack{T}"/> (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="Dictionary{TKey,TValue}"/></item>
    /// <item><see cref="SortedList{TKey,TValue}"/></item>
    /// <item><see cref="SortedDictionary{TKey,TValue}"/></item>
    /// <item><see cref="CircularSortedList{TKey,TValue}"/></item>
    /// <item><see cref="AllowNullDictionary{TKey,TValue}"/></item>
    /// <item><see cref="ConcurrentDictionary{TKey,TValue}"/> (in .NET Framework 4.0 and above)</item>
    /// <item><see cref="ImmutableArray{T}"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableArray{T}.Builder"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableList{T}"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableList{T}.Builder"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableHashSet{T}"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableHashSet{T}.Builder"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableSortedSet{T}"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableSortedSet{T}.Builder"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableQueue{T}"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableStack{T}"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableDictionary{TKey,TValue}"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableDictionary{TKey,TValue}.Builder"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableSortedDictionary{TKey,TValue}"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="ImmutableSortedDictionary{TKey,TValue}.Builder"/> (in .NET Core 2.0 and above)</item>
    /// <item><see cref="Memory{T}"/> (in .NET Core 2.1 and above)</item>
    /// <item><see cref="ReadOnlyMemory{T}"/> (in .NET Core 2.1 and above)</item>
    /// <item><see cref="Vector64{T}"/> (in .NET Core 3.0 and above)</item>
    /// <item><see cref="Vector128{T}"/> (in .NET Core 3.0 and above)</item>
    /// <item><see cref="Vector256{T}"/> (in .NET Core 3.0 and above)</item>
    /// <item><see cref="Vector512{T}"/> (in .NET 8.0 and above)</item>
    /// <item><see cref="FrozenSet{T}"/> (in .NET 8.0 and above)</item>
    /// <item><see cref="FrozenDictionary{TKey,TValue}"/> (in .NET 8.0 and above)</item>
    /// <item><see cref="OrderedDictionary{TKey,TValue}"/> (in .NET 9.0 and above)</item>
    /// </list>
    /// <note>
    /// <list type="bullet">
    /// <item><see cref="Array"/>s can be single- and multidimensional, jagged (array of arrays) and don't have to be zero index-based. Arrays and other generic collections can be nested.</item>
    /// <item>If a collection uses an unsupported <see cref="IEqualityComparer{T}"/> or <see cref="IComparer{T}"/> implementation, then it is possible that the type cannot be serialized without enabling
    /// <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option, unless the comparer is decorated by <see cref="SerializableAttribute"/> or implements the <see cref="IBinarySerializable"/> interface.</item>
    /// <item>If an <see cref="Array"/> has <see cref="object"/> element type or <see cref="object"/> is used in generic arguments of the collections above and an element is not a natively supported type, then recursive serialization of fields
    /// may occur. For non-serializable types the <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option might be needed to be enabled.</item>
    /// <item>Even if a generic collection of <see cref="object"/> contains natively supported types only, the result will be somewhat longer than in case of a more specific element type.</item>
    /// </list>
    /// </note>
    /// </para>
    /// <note type="tip">The shortest result can be achieved by using <see langword="sealed"/> classes or value types as array base types and generic parameters.</note>
    /// <h2>Natively supported non-generic collections</h2>
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
    /// <item>If a collection uses an unsupported <see cref="IEqualityComparer"/> or <see cref="IComparer"/> implementation, then it is possible that the type cannot be serialized without enabling
    /// <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option, unless the comparer is decorated by <see cref="SerializableAttribute"/> or implements the <see cref="IBinarySerializable"/> interface.</item>
    /// <item>If an element in these collections is not a natively supported type, then recursive serialization of fields may occur. For non-serializable types the <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/>
    /// option might be enabled.</item>
    /// </list>
    /// </note>
    /// </para>
    /// <h2>Serialization events</h2>
    /// <para><see cref="BinarySerializationFormatter"/> supports calling methods decorated by <see cref="OnSerializingAttribute"/>, <see cref="OnSerializedAttribute"/>,
    /// <see cref="OnDeserializingAttribute"/> and <see cref="OnDeserializedAttribute"/> as well as calling <see cref="IDeserializationCallback.OnDeserialization">IDeserializationCallback.OnDeserialization</see> method.
    /// Attributes should be used on methods that have a single <see cref="StreamingContext"/> parameter.
    /// <note>Please note that if a value type was serialized by the <see cref="BinarySerializationOptions.CompactSerializationOfStructures"/> option, then the method of <see cref="OnDeserializingAttribute"/> can be invoked
    /// only after restoring the whole content so fields will be already restored.</note>
    /// </para>
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
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Names match the corresponding type names (e.g. DBNull, Matrix3x2)")]
        //[DebuggerDisplay("{BinarySerializationFormatter.DataTypeToString(this)}")] // If debugger cannot display it: Tools/Options/Debugging/General: Use Managed Compatibility Mode
        private enum DataTypes : uint
        {
            // ===== BYTE 0. =====

            // ----- simple/element types: -----
            SimpleTypesLow = 0b00111111, // bits 0-5 (6 bits - up to 64 types) - see also SimpleTypesExtended

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

            RuntimeType = 30, // Non-serializable in .NET Core. Not meant to be combined, but it can happen if collection element type is RuntimeType.

            StringSegment = 31,

            // . . . Non-primitive, platform-dependent pure types (32-48 - up to 16 types) . . .

            BigInteger = 32, // .NET Framework 4.0 and above
            Rune = 33, // .NET Core 3.0 and above
            Index = 34, // .NET Standard 2.1 and above
            Range = 35, // .NET Standard 2.1 and above
            Half = 36, // .NET 5 and above
            DateOnly = 37, // .NET 6 and above
            TimeOnly = 38, // .NET 6 and above
            Int128 = 39, // .NET 7 and above
            UInt128 = 40, // .NET 7 and above
            ValueTuple0 = 41, // .NET Standard 2.0 and above - note: generic value tuples are on byte 3. as special collections

            // 42-47: 6 reserved values
            // TODO: Some candidates:
            //Int256 = 41, UInt256 = 42, // https://github.com/dotnet/runtime/issues/80663
            //Decimal32 = 43, Decimal64 = 44, Decimal128 = 45, // https://github.com/dotnet/runtime/issues/81376
            //Float8 = 46, // if it will be added, better to add to non-extended types due to its small size - https://github.com/dotnet/runtime/issues/25004

            ExtendedSimpleType = 47, // Indicates that byte 2. is also included. It allows additional 255 simple types (excluding zero).

            // ..... impure types (their type cannot be determined purely by a DataType) .....
            ImpureType = 0b00110000, // caution: 2-bits flag

            // 48: Reserved (though it would have the same value as the ImpureType flag)

            GenericTypeDefinition = 49, // Must be combined with a supported generic collection type.
            Pointer = 50, // Followed by DataTypes. Cannot be combined.
            ByRef = 51, // Followed by DataTypes. Cannot be combined.

            // 54-59: 6 reserved values

            //SerializationEnd = 59, // Planned technical type for IAdvancedBinarySerializable (refers to a static object)
            BinarySerializable = 60, // IBinarySerializable implementation. Can be combined.
            RawStruct = 61, // Any ValueType. Can be combined only with Nullable but not with collections.
            RecursiveObjectGraph = 62, // Represents a recursively serialized object graph. As a type, represents any unspecified type. Can be combined.
            // 63: Reserved (though it would have has the same value as the SimpleTypesLow mask or ExtendedSimpleType | ImpureType that can indicate extended impure types if running out of reserved values)

            // ----- flags: -----
            Store7BitEncoded = 1 << 6, // Applicable for every >1 byte fix-length data type
            Extended = 1 << 7, // On serialization, it indicates that byte 1. also is used.

            // ===== BYTE 1. =====

            // ----- collection types: -----
            CollectionTypesLow = 0b00111111_00000000, // 8-13 bits (6 bits - up to 64 types)

            // ..... generic collections: .....
            Array = 1 << 8, // actually not a generic type but can be encoded the same way
            List = 2 << 8,
            LinkedList = 3 << 8,
            HashSet = 4 << 8,
            Queue = 5 << 8,
            Stack = 6 << 8,
            CircularList = 7 << 8,
            SortedSet = 8 << 8, // .NET Framework 4.0 and above
            ConcurrentBag = 9 << 8, // .NET Framework 4.0 and above
            ConcurrentQueue = 10 << 8, // .NET Framework 4.0 and above
            ConcurrentStack = 11 << 8, // .NET Framework 4.0 and above
            ThreadSafeHashSet = 12 << 8,

            // ...... generic comparers (element type is encoded similarly to collections): ......
            GenericEqualityComparerDefault = 13 << 8,
            GenericComparerDefault = 14 << 8,
            EnumComparer = 15 << 8,

            // ...... non-generic or special collections: ......
            ArrayList = 16 << 8,
            QueueNonGeneric = 17 << 8,
            StackNonGeneric = 18 << 8,
            StringCollection = 19 << 8,

            StrongBox = 20 << 8, // Defined as a collection type so can be encoded the same way as other collections

            // tuples: - note: ValueTuples are on byte 3, so they can be combined with NullableExtendedCollection
            Tuple1 = 21 << 8,
            Tuple2 = 22 << 8,
            Tuple3 = 23 << 8,
            Tuple4 = 24 << 8,
            Tuple5 = 25 << 8,
            Tuple6 = 26 << 8,
            Tuple7 = 27 << 8,
            Tuple8 = 28 << 8,

            // 29-30 << 2: reserved types

            ExtendedCollectionType = 31 << 8, // Indicates that byte 3. is also included

            // ...... generic dictionaries:
            Dictionary = 32 << 8, // Represents both the generic Dictionary type and a flag (1 << 13) for generic dictionaries
            SortedList = 33 << 8,
            SortedDictionary = 34 << 8,
            CircularSortedList = 35 << 8,
            ConcurrentDictionary = 36 << 8, // .NET Framework 4.0 and above
            ThreadSafeDictionary = 37 << 8,
            AllowNullDictionary = 38 << 8,
            OrderedDictionaryGeneric = 39 << 8, // .NET 9 and above
            
            // 40-45 << 8 : 6 reserved generic dictionaries
            // TODO: candidate: TwoWayDictionary if it will be implemented

            KeyValuePair = 46 << 8, // Defined as a collection type so can be encoded the same way as dictionaries
            KeyValuePairNullable = 47 << 8, // The Nullable flag would be used for the key so this is the nullable version of KeyValuePair.

            // ...... non-generic or specialized dictionaries:
            Hashtable = 48 << 8,
            SortedListNonGeneric = 49 << 8,
            ListDictionary = 50 << 8,
            HybridDictionary = 51 << 8,
            OrderedDictionary = 52 << 8,
            StringDictionary = 53 << 8,
            StringKeyedDictionary = 54 << 8, // actually generic but has only a TValue argument

            // 55-60 << 8 : 6 reserved non-generic dictionaries

            DictionaryEntry = 61 << 8, // Could be a simple type but keeping consistency with KeyValuePair
            DictionaryEntryNullable = 62 << 8, // The Nullable flag would be used for the key (which is invalid for this type) so this is the nullable version of DictionaryEntry.
            // 63 << 8: Reserved (though it would have has the same value as the CollectionTypes mask)

            // ------ flags
            Enum = 1 << 14,
            Nullable = 1 << 15, // Can be combined with simple types. Nullable collections are separate items or have their extended flag on byte 3.

            // ===== BYTE 2. =====

            SimpleTypesExtended = 0b11111111_00000000_00000000U, // bits 16-23 - up to 255 types without zero
            SimpleTypesAll = SimpleTypesLow | SimpleTypesExtended,

            // ..... pure types (they are unambiguous without a type name): .....
            //PureTypesExtended = (0b01111111 << 16) | PureTypes, // bits 16-22 - up to 127 values

            Complex = 1 << 16, // .NET Framework 4.0 and above
            Vector2 = 2 << 16, // .NET Framework 4.6 and above
            Vector3 = 3 << 16, // .NET Framework 4.6 and above
            Vector4 = 4 << 16, // .NET Framework 4.6 and above
            Quaternion = 5 << 16, // .NET Framework 4.6 and above
            Plane = 6 << 16, // .NET Framework 4.6 and above
            Matrix3x2 = 7 << 16, // .NET Framework 4.6 and above
            Matrix4x4 = 8 << 16, // .NET Framework 4.6 and above

            CompareInfo = 9 << 16,
            CultureInfo = 10 << 16,

            Comparer = 11 << 16,
            CaseInsensitiveComparer = 12 << 16,
            StringComparer = 13 << 16,
            StringSegmentComparer = 14 << 16,

            // TODO candidates:
            //Quad = // float128: to extended types - https://github.com/dotnet/csharplang/issues/1252
            //BigNumber/BigRational, // https://source.dot.net/#System.Runtime.Numerics/System/Numerics/BigNumber.cs,969928e529663ace / https://github.com/dotnet/runtime/issues/71791
            //BigDecimal, // https://github.com/dotnet/runtime/issues/20681

            // ..... impure types (they are ambiguous without a type name): - only if needed in the future .....
            //ImpureTypeExtended = 0b10000000 << 16

            // MyNewExtendedImpureType1 = (128 << 16) | ImpureType, // and 129, 130, etc.

            // ===== BYTE 3. =====

            // ----- collection types: ----- // 24-30 bits (7 bits - up to 127 types without zero)
            CollectionTypesExtended = 0b11111111u << 24, // NOTE: covers also the NullableExtendedCollection flag
            CollectionTypesAll = CollectionTypesLow | CollectionTypesExtended,

            // ..... value tuples: .....
            ValueTuple1 = 1 << 24,
            ValueTuple2 = 2 << 24,
            ValueTuple3 = 3 << 24,
            ValueTuple4 = 4 << 24,
            ValueTuple5 = 5 << 24,
            ValueTuple6 = 6 << 24,
            ValueTuple7 = 7 << 24,
            ValueTuple8 = 8 << 24,

            // ..... array backed collections: .....
            ArraySegment = 9 << 24,
            ArraySection = 10 << 24,
            Array2D = 11 << 24,
            Array3D = 12 << 24,

            // ..... generic fixed-size SIMD vectors: .....
            Vector64 = 13 << 24, // .NET Core 3.0 and above
            Vector128 = 14 << 24, // .NET Core 3.0 and above
            Vector256 = 15 << 24, // .NET Core 3.0 and above
            Vector512 = 16 << 24, // .NET 8.0 and above

            // 17-20: Reserved for bigger vectors

            // ..... Immutable collections: .....
            ImmutableArray = 21 << 24,
            ImmutableArrayBuilder = 22 << 24,
            ImmutableList = 23 << 24,
            ImmutableListBuilder = 24 << 24,
            ImmutableHashSet = 25 << 24,
            ImmutableHashSetBuilder = 26 << 24,
            ImmutableSortedSet = 27 << 24,
            ImmutableSortedSetBuilder = 28 << 24,
            ImmutableQueue = 29 << 24,
            ImmutableStack = 30 << 24,
            FrozenSet = 31 << 24, // Special one because the actual deserialized type may depend on the current runtime
            // 32: Reserved

            // ..... more array backed collections (with reinterpreted element type): .....
            CastArray = 33 << 24,
            CastArray2D = 34 << 24,
            CastArray3D = 35 << 24,

            // ..... memory (backed by array/string/manager/none): ..... - NOTE: the backing object has to be perfectly serialized (unlike in XML/Json serialization)
            Memory = 36 << 24,
            ReadOnlyMemory = 37 << 24,

            // ...... generic dictionaries:
            ExtendedDictionary = 0b01000000 << 24, // serves only as a flag
            ImmutableDictionary = 65 << 24,
            ImmutableDictionaryBuilder = 66 << 24,
            ImmutableSortedDictionary = 67 << 24,
            ImmutableSortedDictionaryBuilder = 68 << 24,
            FrozenDictionary = 69 << 24, // Special one because the actual deserialized type may depend on the current runtime

            // ----- flags: -----
            NullableExtendedCollection = 1u << 31, // Only for extended collections. For non-extended ones (KeyValuePair, DictionaryEntry) nullable types are separate items due to compatibility reasons
        }

        /// <summary>
        /// Special serialization info for collections
        /// </summary>
        [Flags]
        private enum CollectionInfo
        {
            None = 0,

            /// <summary>
            /// Indicates that the collection has a Capacity property that has to be (re)stored.
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
            /// For types that can be both read-only and read-write (only in non-generic OrderedDictionary)
            /// </summary>
            HasReadOnly = 1 << 7,

            /// <summary>
            /// Indicates that the "collection" is a single element
            /// (now for StrongBox, DictionaryEntry, KeyValuePair: special "collections" with exactly one (pair of) elements, whose type arguments are easy to encode along with collection types)
            /// </summary>
            IsSingleElement = 1 << 8,

            /// <summary>
            /// Indicates that the default comparer is determined by <see cref="ComparerHelper{T}"/>. The result can be different for enums on different platforms.
            /// </summary>
            UsesComparerHelper = 1 << 9,

            /// <summary>
            /// Indicates that even default comparer cannot be null (only for ConcurrentDictionary in .NET Framework)
            /// </summary>
            NonNullDefaultComparer = 1 << 10,

            /// <summary>
            /// Indicates that the backing array is an actually wrapped reference, so it is stored in the id cache and can be even null (e.g. ArraySegment).
            /// </summary>
            IsBackingArrayActuallyStored = 1 << 11,

            /// <summary>
            /// Indicates that the backing array has a known size (e.g. Vector*)
            /// </summary>
            BackingArrayHasKnownSize = 1 << 12,

            /// <summary>
            /// Indicates that the collection is a tuple
            /// </summary>
            IsTuple = 1 << 13,

            /// <summary>
            /// Indicates that the collection has a bool field that tells whether the collection uses bitwise AND hash (ThreadSafeHashSet/ThreadSafeDictionary)
            /// </summary>
            HasBitwiseAndHash = 1 << 14,

            /// <summary>
            /// Indicates that the collection has a StringSegmentComparer that can be passed to the constructor
            /// </summary>
            HasStringSegmentComparer = 1 << 15,

            /// <summary>
            /// Indicates that the non-generic collection has string items or keys-values, or that a generic dictionary with only one generic argument has string keys.
            /// </summary>
            HasStringItemsOrKeys = 1 << 16,

            /// <summary>
            /// Indicates that the backing array is always a byte array, regardless of the actual element type.
            /// </summary>
            CreateResultFromByteArray = 1 << 17,

            /// <summary>
            /// Indicates that the dictionary has an additional EqualityComparer for TValue that can be passed to a constructor or factory method
            /// </summary>
            HasValueComparer = 1 << 18,

            /// <summary>
            /// Indicates that the generic type is a known comparer rather than an actual collection, thus it has no actual elements, it's just it can be encoded like a collection.
            /// </summary>
            IsComparer = 1 << 19,

            /// <summary>
            /// Indicates that the collection is ordered. As lists are naturally ordered, this is needed for ordered dictionaries only.
            /// </summary>
            IsOrdered = 1 << 20,

            /// <summary>
            /// Indicates that the collection is a [ReadOnly]Memory struct.
            /// </summary>
            IsMemory = 1 << 21,
        }

        /// <summary>
        /// Possible arguments of a collection constructor or factory method
        /// </summary>
        private enum CollectionCtorArguments
        {
            Capacity,
            Comparer,
            CaseInsensitivity,
            HashingStrategy,
            ValueComparer,
        }

        /// <summary>
        /// Contains some serialization-time attributes for non-primitive types.
        /// This ensures that the deserializer can process a type (or at least throw a reasonable exception)
        /// if it changed since serialization (e.g. sealed vs non-sealed, serialization way, etc.)
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

        /// <summary>
        /// Gets the possibly known singleton identity of a comparer.
        /// Used for non-generic comparers whose instances can be either singletons or regular instances to be serialized.
        /// </summary>
        private enum ComparerType
        {
            Default, // Used only as a fallback if internal implementation is not recognizable because normally default comparer is serialized as a culture-aware one
            CultureSpecific, // Not a known singleton but the comparer type is supported and the instance should be serialized by its compare info and possible other data
            Invariant, // Comparer.DefaultInvariant, CaseInsensitiveComparer.DefaultInvariant, StringComparer.InvariantCulture, StringSegmentComparer.InvariantCulture
            Ordinal, // StringComparer, StringSegmentComparer
            OrdinalIgnoreCase, // StringComparer, StringSegmentComparer
            InvariantIgnoreCase, // StringComparer, StringSegmentComparer
            OrdinalRandomized, // StringSegmentComparer
            OrdinalIgnoreCaseRandomized, // StringSegmentComparer
            OrdinalNonRandomized, // StringSegmentComparer, TODO StringComparer - https://github.com/dotnet/runtime/issues/77679
            OrdinalIgnoreCaseNonRandomized, // StringSegmentComparer, TODO StringComparer - https://github.com/dotnet/runtime/issues/77679
        }

        #endregion

        #region Nested Structs

        #region TypeIdentity struct

        private readonly struct TypeIdentity : IEquatable<TypeIdentity>
        {
            #region Fields

            internal readonly object Identity; // Type or string

            #endregion

            #region Constructors

            internal TypeIdentity(object identity)
            {
                Debug.Assert(identity is Type or string, "Type or string identity expected");
                Identity = identity;
            }

            #endregion

            #region Methods

            public bool Equals(TypeIdentity other) => Identity.Equals(other.Identity);
            public override bool Equals(object? obj) => obj is TypeIdentity other && Equals(other);
            public override int GetHashCode() => Identity.GetHashCode();
            public override string ToString() => Identity.ToString()!;

            #endregion
        }

        #endregion

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

        private static readonly Dictionary<DataTypes, CollectionSerializationInfo> serializationInfo = new Dictionary<DataTypes, CollectionSerializationInfo>(ComparerHelper<DataTypes>.EqualityComparer)
        {
            #region Generic collections (DataTypes 1..15 << 8)

            { DataTypes.Array, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric } },
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
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(LinkedList<_>.AddLast), new[] { t.GetGenericArguments()[0] })!),
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
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(HashSet<_>.Add))!),
                }
            },
            {
                DataTypes.Queue, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity },
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(Queue<_>.Enqueue))!),
                }
            },
            {
                DataTypes.Stack, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.ReverseElements,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity },
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(Stack<_>.Push))!),
                }
            },
            {
                DataTypes.CircularList, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasCapacity,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity }
                }
            },
            {
                DataTypes.ThreadSafeHashSet, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasEqualityComparer | CollectionInfo.UsesComparerHelper | CollectionInfo.HasBitwiseAndHash,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer, CollectionCtorArguments.HashingStrategy },
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(ThreadSafeHashSet<_>.Add))!),
                    WriteSpecificPropertiesCallback = (bw, o) =>
                    {
                        bw.Write((bool)Accessors.GetPropertyValue(o, nameof(ThreadSafeHashSet<_>.PreserveMergedItems))!);
                        Write7BitLong(bw, (ulong)((TimeSpan)Accessors.GetPropertyValue(o, nameof(ThreadSafeHashSet<_>.MergeInterval))!).Ticks);
                    },
                    RestoreSpecificPropertiesCallback = (br, o) =>
                    {
                        Accessors.SetPropertyValue(o, nameof(ThreadSafeHashSet<_>.PreserveMergedItems), br.ReadBoolean());
                        Accessors.SetPropertyValue(o, nameof(ThreadSafeHashSet<_>.MergeInterval), TimeSpan.FromTicks(Read7BitLong(br)));
                    }
                }
            },
#if !NET35
            {
                DataTypes.SortedSet, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer },
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(SortedSet<_>.Add))!),
                }
            },
            {
                DataTypes.ConcurrentBag, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric,
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(ConcurrentBag<_>.Add))!),
                }
            },
            {
                DataTypes.ConcurrentQueue, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric,
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(ConcurrentQueue<_>.Enqueue))!),
                }
            },
            {
                DataTypes.ConcurrentStack, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.ReverseElements,
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(ConcurrentStack<_>.Push))!),
                }
            },
#endif
            {
                DataTypes.GenericEqualityComparerDefault, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsComparer,
                    ReferenceAbstractGenericType = typeof(EqualityComparer<>),
                    CreateInstanceCallback = (t, _) => t.GetPropertyValue(nameof(EqualityComparer<_>.Default))!
                }
            },
            {
                DataTypes.GenericComparerDefault, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsComparer,
                    ReferenceAbstractGenericType = typeof(Comparer<>),
                    CreateInstanceCallback = (t, _) => t.GetPropertyValue(nameof(Comparer<_>.Default))!
                }
            },
            {
                DataTypes.EnumComparer, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsComparer,
                    ReferenceAbstractGenericType = typeof(EnumComparer<>),
                    CreateInstanceCallback = (t, _) => t.GetPropertyValue(nameof(EnumComparer<_>.Comparer))!
                }
            },

            #endregion

            #region Non-generic or special collections (DataTypes 16..31 << 8)

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
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(Queue.Enqueue))!),
                }
            },
            {
                DataTypes.StackNonGeneric, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.ReverseElements,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity },
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(Stack.Push))!),
                }
            },
            { DataTypes.StringCollection, new CollectionSerializationInfo { Info = CollectionInfo.HasStringItemsOrKeys } },
            { DataTypes.StrongBox, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.IsSingleElement } },
#if !NET35
            { DataTypes.Tuple1, CollectionSerializationInfo.Tuple },
            { DataTypes.Tuple2, CollectionSerializationInfo.Tuple },
            { DataTypes.Tuple3, CollectionSerializationInfo.Tuple },
            { DataTypes.Tuple4, CollectionSerializationInfo.Tuple },
            { DataTypes.Tuple5, CollectionSerializationInfo.Tuple },
            { DataTypes.Tuple6, CollectionSerializationInfo.Tuple },
            { DataTypes.Tuple7, CollectionSerializationInfo.Tuple },
            { DataTypes.Tuple8, CollectionSerializationInfo.Tuple },
#endif

            #endregion

            #region Generic dictionaries (DataTypes 32..47 << 8)

            {
                DataTypes.Dictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer, // NOTE: HasCapacity could be added in .NET 9+ but that would break compatibility
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
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasCapacity | CollectionInfo.HasComparer | CollectionInfo.UsesComparerHelper,
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
            {
                DataTypes.ThreadSafeDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer | CollectionInfo.UsesComparerHelper | CollectionInfo.HasBitwiseAndHash,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer, CollectionCtorArguments.HashingStrategy },
                    WriteSpecificPropertiesCallback = (bw, o) =>
                    {
                        bw.Write((bool)Accessors.GetPropertyValue(o, nameof(ThreadSafeDictionary<_,_>.PreserveMergedKeys))!);
                        Write7BitLong(bw, (ulong)((TimeSpan)Accessors.GetPropertyValue(o, nameof(ThreadSafeDictionary<_,_>.MergeInterval))!).Ticks);
                    },
                    RestoreSpecificPropertiesCallback = (br, o) =>
                    {
                        Accessors.SetPropertyValue(o, nameof(ThreadSafeDictionary<_,_>.PreserveMergedKeys), br.ReadBoolean());
                        Accessors.SetPropertyValue(o, nameof(ThreadSafeDictionary<_,_>.MergeInterval), TimeSpan.FromTicks(Read7BitLong(br)));
                    }
                }
            },
            {
                DataTypes.AllowNullDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer,
                    CtorArguments = [CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer]
                }
            },
#if NET9_0_OR_GREATER
            {
                DataTypes.OrderedDictionaryGeneric, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.IsOrdered | CollectionInfo.HasEqualityComparer | CollectionInfo.HasCapacity,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer },
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(OrderedDictionary<_,_>.Insert))!),
                }
            },
#endif

            #endregion

            #region Non-generic or specialized dictionaries (DataTypes 48..63 << 8)

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
                    Info = CollectionInfo.IsDictionary | CollectionInfo.IsOrdered | CollectionInfo.HasEqualityComparer | CollectionInfo.HasReadOnly,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer }
                }
            },
            {
                DataTypes.StringDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsDictionary | CollectionInfo.HasStringItemsOrKeys,
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(StringDictionary.Add))!),
                }
            },
            {
                DataTypes.StringKeyedDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasStringSegmentComparer | CollectionInfo.HasStringItemsOrKeys,
                    CtorArguments = new [] { CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer  }
                }
            },
            { DataTypes.DictionaryEntry, new CollectionSerializationInfo { Info = CollectionInfo.IsDictionary | CollectionInfo.IsSingleElement } },
            { DataTypes.DictionaryEntryNullable, new CollectionSerializationInfo { Info = CollectionInfo.IsDictionary | CollectionInfo.IsSingleElement } },

            #endregion

            #region Extended collections (DataTypes 1..63 << 24)

#if NET47_OR_GREATER || !NETFRAMEWORK
            { DataTypes.ValueTuple1, CollectionSerializationInfo.Tuple },
            { DataTypes.ValueTuple2, CollectionSerializationInfo.Tuple },
            { DataTypes.ValueTuple3, CollectionSerializationInfo.Tuple },
            { DataTypes.ValueTuple4, CollectionSerializationInfo.Tuple },
            { DataTypes.ValueTuple5, CollectionSerializationInfo.Tuple },
            { DataTypes.ValueTuple6, CollectionSerializationInfo.Tuple },
            { DataTypes.ValueTuple7, CollectionSerializationInfo.Tuple },
            { DataTypes.ValueTuple8, CollectionSerializationInfo.Tuple }, 
#endif
            {
                DataTypes.ArraySegment, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsBackingArrayActuallyStored,
                    GetBackingArray = o => (Array?)Accessors.GetPropertyValue(o, nameof(ArraySegment<_>.Array)),
                    WriteSpecificPropertiesCallback = (bw, o) =>
                    {
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(ArraySegment<_>.Offset))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(ArraySegment<_>.Count))!);
                    },
                    CreateArrayBackedCollectionInstanceFromArray = (br, t, a) =>
                    {
                        Type[] args = { a.GetType(), Reflector.IntType, Reflector.IntType };
                        ConstructorInfo ctor = t.GetConstructor(args)!;
                        return CreateInstanceAccessor.GetAccessor(ctor).CreateInstance(a, Read7BitInt(br), Read7BitInt(br));
                    }
                }
            },
            {
                DataTypes.ArraySection, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsBackingArrayActuallyStored,
                    GetBackingArray = o => (Array?)Accessors.GetPropertyValue(o, nameof(ArraySection<_>.UnderlyingArray)),
                    WriteSpecificPropertiesCallback = (bw, o) =>
                    {
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(ArraySection<_>.Offset))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(ArraySection<_>.Length))!);
                    },
                    CreateArrayBackedCollectionInstanceFromArray = (br, t, a)
                        => t.CreateInstance(new[] { a.GetType(), Reflector.IntType, Reflector.IntType }, a, Read7BitInt(br), Read7BitInt(br)),
                }
            },
            {
                DataTypes.Array2D, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsBackingArrayActuallyStored,
                    GetBackingArray = o => (Array?)Accessors.GetPropertyValue(Accessors.GetPropertyValue(o, nameof(Array2D<_>.Buffer))!, nameof(ArraySection<_>.UnderlyingArray)),
                    WriteSpecificPropertiesCallback = (bw, o) =>
                    {
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(Accessors.GetPropertyValue(o, nameof(Array2D<_>.Buffer))!, nameof(ArraySection<_>.Offset))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(Array2D<_>.Height))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(Array2D<_>.Width))!);
                    },
                    CreateArrayBackedCollectionInstanceFromArray = (br, t, a) =>
                    {
                        Type bufferType = typeof(ArraySection<>).GetGenericType(t.GetGenericArguments()[0]);
                        var buffer = bufferType.CreateInstance(a, Read7BitInt(br));
                        return t.CreateInstance(new[] { bufferType, Reflector.IntType, Reflector.IntType }, buffer, Read7BitInt(br), Read7BitInt(br));
                    }
                }
            },
            {
                DataTypes.Array3D, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsBackingArrayActuallyStored,
                    GetBackingArray = o => (Array?)Accessors.GetPropertyValue(Accessors.GetPropertyValue(o, nameof(Array3D<_>.Buffer))!, nameof(ArraySection<_>.UnderlyingArray)),
                    WriteSpecificPropertiesCallback = (bw, o) =>
                    {
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(Accessors.GetPropertyValue(o, nameof(Array3D<_>.Buffer))!, nameof(ArraySection<_>.Offset))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(Array3D<_>.Depth))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(Array3D<_>.Height))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(Array3D<_>.Width))!);
                    },
                    CreateArrayBackedCollectionInstanceFromArray = (br, t, a) =>
                    {
                        Type bufferType = typeof(ArraySection<>).GetGenericType(t.GetGenericArguments()[0]);
                        var buffer = bufferType.CreateInstance(a, Read7BitInt(br));
                        return t.CreateInstance(new[] { bufferType, Reflector.IntType, Reflector.IntType, Reflector.IntType }, buffer, Read7BitInt(br), Read7BitInt(br), Read7BitInt(br));
                    }
                }
            },

#if NETCOREAPP3_0_OR_GREATER
            { DataTypes.Vector64, CollectionSerializationInfo.FixedSizeGenericStruct },
            { DataTypes.Vector128, CollectionSerializationInfo.FixedSizeGenericStruct },
            { DataTypes.Vector256, CollectionSerializationInfo.FixedSizeGenericStruct },
#endif
#if NET8_0_OR_GREATER
            { DataTypes.Vector512, CollectionSerializationInfo.FixedSizeGenericStruct },
#endif

#if NETCOREAPP
            {
                // Trick: Normally we should use a builder for ImmutableArray but as there is an internal constructor that can wrap a pre-created array so we can use the BackingArray approach.
                //        This makes also possible to have circular references in an ImmutableArray, which is not possible by a builder that replaces the reference on every update.
                DataTypes.ImmutableArray, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsBackingArrayActuallyStored,
                    GetBackingArray = o => (bool)Accessors.GetPropertyValue(o, nameof(ImmutableArray<_>.IsDefault))! ? null : (Array)typeof(Enumerable).InvokeMethod(nameof(Enumerable.ToArray), o.GetType().GetGenericArguments()[0], o)!,
                    CreateArrayBackedCollectionInstanceFromArray = (_, t, a) => t.CreateInstance(a),
                }
            },
            {
                DataTypes.ImmutableArrayBuilder, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasCapacity,
                    CtorArguments = new[] { CollectionCtorArguments.Capacity },
                    CreateInstanceCallback = (t, args)
                        => typeof(ImmutableArray).InvokeMethod(nameof(ImmutableArray.CreateBuilder), t.GetGenericArguments()[0], Reflector.IntType, args)!,
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(ImmutableArray<_>.Builder.Add))!),
                }
            },
            {
                DataTypes.ImmutableList, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric,
                    CreateInstanceCallback = (t, _)
                        => typeof(ImmutableList).InvokeMethod(nameof(ImmutableList.CreateBuilder), t.GetGenericArguments()[0])!,
                    CreateFinalCollectionCallback = o => Accessors.InvokeMethod(o, nameof(ImmutableList<_>.Builder.ToImmutable))!,
                }
            },
            {
                DataTypes.ImmutableListBuilder, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric,
                    CreateInstanceCallback = (t, _)
                        => typeof(ImmutableList).InvokeMethod(nameof(ImmutableList.CreateBuilder), t.GetGenericArguments()[0])!,
                }
            },
            {
                DataTypes.ImmutableHashSet, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasEqualityComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer },
                    CreateInstanceCallback = (t, args) =>
                    {
                        Type genericArg = t.GetGenericArguments()[0];
                        return typeof(ImmutableHashSet).InvokeMethod(nameof(ImmutableHashSet.CreateBuilder), genericArg, typeof(IEqualityComparer<>).GetGenericType(genericArg), args)!;
                    },
                    CreateFinalCollectionCallback = o => Accessors.InvokeMethod(o, nameof(ImmutableHashSet<_>.Builder.ToImmutable))!,
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(ImmutableHashSet<_>.Builder.Add))!),
                }
            },
            {
                DataTypes.ImmutableHashSetBuilder, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasEqualityComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer },
                    CreateInstanceCallback = (t, args) =>
                    {
                        Type genericArg = t.GetGenericArguments()[0];
                        return typeof(ImmutableHashSet).InvokeMethod(nameof(ImmutableHashSet.CreateBuilder), genericArg, typeof(IEqualityComparer<>).GetGenericType(genericArg), args)!;
                    },
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(ImmutableHashSet<_>.Builder.Add))!),
                }
            },
            {
                DataTypes.ImmutableSortedSet, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer },
                    CreateInstanceCallback = (t, args) =>
                    {
                        Type genericArg = t.GetGenericArguments()[0];
                        return typeof(ImmutableSortedSet).InvokeMethod(nameof(ImmutableSortedSet.CreateBuilder), genericArg, typeof(IComparer<>).GetGenericType(genericArg), args)!;
                    },
                    CreateFinalCollectionCallback = o => Accessors.InvokeMethod(o, nameof(ImmutableSortedSet<_>.Builder.ToImmutable))!,
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(ImmutableSortedSet<_>.Builder.Add))!),
                }
            },
            {
                DataTypes.ImmutableSortedSetBuilder, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer },
                    CreateInstanceCallback = (t, args) =>
                    {
                        Type genericArg = t.GetGenericArguments()[0];
                        return typeof(ImmutableSortedSet).InvokeMethod(nameof(ImmutableSortedSet.CreateBuilder), genericArg, typeof(IComparer<>).GetGenericType(genericArg), args)!;
                    },
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(ImmutableSortedSet<_>.Builder.Add))!),
                }
            },
            {
                // Trick: Serializing as array (because ImmutableQueue has no Count and we want to avoid double enumeration of the queue)
                //        but deserializing from List (because CreateArrayBackedCollectionInstanceFromArray would allow resolving circular references
                //        to the backing array itself, which should not be allowed as ImmutableQueue does not actually wrap the array)
                DataTypes.ImmutableQueue, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric,
                    GetBackingArray = o =>
                    {
                        Type genericArg = o.GetType().GetGenericArguments()[0];
                        return (Array)typeof(Enumerable).InvokeMethod(nameof(Enumerable.ToArray), genericArg, Reflector.IEnumerableGenType.GetGenericType(genericArg), o)!;
                    },
                    CtorArguments = new[] { CollectionCtorArguments.Capacity },
                    CreateInstanceCallback = (t, args) => Reflector.ListGenType.GetGenericType(t.GetGenericArguments()[0]).CreateInstance(args!),
                    CreateFinalCollectionCallback = o =>
                    {
                        Type genericArg = o.GetType().GetGenericArguments()[0];
                        return typeof(ImmutableQueue).InvokeMethod(nameof(ImmutableQueue.CreateRange), genericArg, Reflector.IEnumerableGenType.GetGenericType(genericArg), o)!;
                    },
                }
            },
            {
                // Trick: Serializing as array (because ImmutableStack has no Count and we want to avoid double enumeration of the stack)
                //        but deserializing from List after reserving elements (because CreateArrayBackedCollectionInstanceFromArray would allow resolving circular references
                //        to the backing array itself, which should not be allowed as ImmutableStack does not actually wrap the array)
                DataTypes.ImmutableStack, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric, // | CollectionInfo.ReverseElements - would be ignored here because serialized as an array. We reverse the elements on deserialization instead.
                    GetBackingArray = o =>
                    {
                        Type genericArg = o.GetType().GetGenericArguments()[0];
                        return (Array)typeof(Enumerable).InvokeMethod(nameof(Enumerable.ToArray), genericArg, Reflector.IEnumerableGenType.GetGenericType(genericArg), o)!;
                    },
                    CtorArguments = new[] { CollectionCtorArguments.Capacity },
                    CreateInstanceCallback = (t, args) => Reflector.ListGenType.GetGenericType(t.GetGenericArguments()[0]).CreateInstance(args!),
                    CreateFinalCollectionCallback = o =>
                    {
                        Accessors.InvokeMethod(o, nameof(List<_>.Reverse), Type.EmptyTypes);
                        Type genericArg = o.GetType().GetGenericArguments()[0];
                        return typeof(ImmutableStack).InvokeMethod(nameof(ImmutableStack.CreateRange), genericArg, Reflector.IEnumerableGenType.GetGenericType(genericArg), o)!;
                    },
                }
            },
#endif
#if NET8_0_OR_GREATER
            {
                DataTypes.FrozenSet, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.HasEqualityComparer,
                    ReferenceAbstractGenericType = typeof(FrozenSet<>),
#if NET35 || NET40 || NET45 || NETSTANDARD2_0
                    CtorArguments = [CollectionCtorArguments.Comparer],
#else
                    CtorArguments = [CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer],
#endif
                    GetSpecificAddMethod = t => MethodAccessor.GetAccessor(t.GetMethod(nameof(HashSet<_>.Add))!),
                    CreateInstanceCallback = (t, args) =>
                    {
                        // Using a HashSet<T> as a builder with the actual comparer
                        Type genericArg = t.GetGenericArguments()[0];
#if NET35 || NET40 || NET45 || NETSTANDARD2_0
                        return typeof(HashSet<>).GetGenericType(genericArg).CreateInstance(new[] { typeof(IEqualityComparer<>).GetGenericType(genericArg) }, args);
#else
                        return typeof(HashSet<>).GetGenericType(genericArg).CreateInstance(new[] { Reflector.IntType, typeof(IEqualityComparer<>).GetGenericType(genericArg) }, args);
#endif
                    },
                    CreateFinalCollectionCallback = o =>
                    {
                        Type genericArg = o.GetType().GetGenericArguments()[0];
                        return typeof(FrozenSet).InvokeMethod(nameof(FrozenSet.ToFrozenSet),
                            [genericArg],
                            [Reflector.IEnumerableGenType.GetGenericType(genericArg), typeof(IEqualityComparer<>).GetGenericType(genericArg)],
                            o, ((IEnumerable)o).GetComparer())!;
                    },
                }
            },
#endif
            {
                DataTypes.CastArray, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsBackingArrayActuallyStored,
                    GetBackingArray = o => (Array?)Accessors.GetPropertyValue(Accessors.GetPropertyValue(o, nameof(CastArray<_,_>.Buffer))!, nameof(ArraySection<_>.UnderlyingArray)),
                    WriteSpecificPropertiesCallback = (bw, o) =>
                    {
                        var buffer = Accessors.GetPropertyValue(o, nameof(CastArray<_,_>.Buffer))!;
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(buffer, nameof(ArraySection<_>.Offset))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(buffer, nameof(ArraySection<_>.Length))!);
                    },
                    CreateArrayBackedCollectionInstanceFromArray = (br, t, a) =>
                    {
                        Type arrayType = a.GetType();
                        var buffer = typeof(ArraySection<>).GetGenericType(arrayType.GetElementType()!).CreateInstance([arrayType, Reflector.IntType, Reflector.IntType], a, Read7BitInt(br), Read7BitInt(br));
                        return t.CreateInstance(buffer);
                    },
                }
            },
            {
                DataTypes.CastArray2D, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsBackingArrayActuallyStored,
                    GetBackingArray = o => (Array?)Accessors.GetPropertyValue(Accessors.GetPropertyValue(Accessors.GetPropertyValue(o, nameof(CastArray2D<_,_>.Buffer))!, nameof(CastArray<_,_>.Buffer))!, nameof(ArraySection<_>.UnderlyingArray)),
                    WriteSpecificPropertiesCallback = (bw, o) =>
                    {
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(Accessors.GetPropertyValue(Accessors.GetPropertyValue(o, nameof(CastArray2D<_,_>.Buffer))!, nameof(CastArray<_,_>.Buffer))!, nameof(ArraySection<_>.Offset))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(CastArray2D<_,_>.Height))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(CastArray2D<_,_>.Width))!);
                    },
                    CreateArrayBackedCollectionInstanceFromArray = (br, t, a) =>
                    {
                        Type arrayType = a.GetType();
                        var buffer = typeof(ArraySection<>).GetGenericType(arrayType.GetElementType()!).CreateInstance([arrayType, Reflector.IntType], a, Read7BitInt(br));
                        return t.CreateInstance([buffer.GetType(), Reflector.IntType, Reflector.IntType], buffer, Read7BitInt(br), Read7BitInt(br));
                    }
                }
            },
            {
                DataTypes.CastArray3D, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsBackingArrayActuallyStored,
                    GetBackingArray = o => (Array?)Accessors.GetPropertyValue(Accessors.GetPropertyValue(Accessors.GetPropertyValue(o, nameof(CastArray3D<_,_>.Buffer))!, nameof(CastArray<_,_>.Buffer))!, nameof(ArraySection<_>.UnderlyingArray)),
                    WriteSpecificPropertiesCallback = (bw, o) =>
                    {
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(Accessors.GetPropertyValue(Accessors.GetPropertyValue(o, nameof(CastArray3D<_,_>.Buffer))!, nameof(CastArray<_,_>.Buffer))!, nameof(ArraySection<_>.Offset))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(CastArray3D<_,_>.Depth))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(CastArray3D<_,_>.Height))!);
                        Write7BitInt(bw, (int)Accessors.GetPropertyValue(o, nameof(CastArray3D<_,_>.Width))!);
                    },
                    CreateArrayBackedCollectionInstanceFromArray = (br, t, a) =>
                    {
                        Type arrayType = a.GetType();
                        var buffer = typeof(ArraySection<>).GetGenericType(arrayType.GetElementType()!).CreateInstance([arrayType, Reflector.IntType], a, Read7BitInt(br));
                        return t.CreateInstance([buffer.GetType(), Reflector.IntType, Reflector.IntType, Reflector.IntType], buffer, Read7BitInt(br), Read7BitInt(br), Read7BitInt(br));
                    }
                }
            },
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            { DataTypes.Memory, CollectionSerializationInfo.Memory },
            { DataTypes.ReadOnlyMemory, CollectionSerializationInfo.Memory },
#endif

            #endregion

            #region Extended dictionaries (DataTypes 64..127 << 24)

#if NETCOREAPP
            {
                DataTypes.ImmutableDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer | CollectionInfo.HasValueComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer, CollectionCtorArguments.ValueComparer },
                    CreateInstanceCallback = (t, args) =>
                    {
                        Type[] genericArgs = t.GetGenericArguments();
                        return typeof(ImmutableDictionary).InvokeMethod(nameof(ImmutableDictionary.CreateBuilder), genericArgs,
                            new[] { typeof(IEqualityComparer<>).GetGenericType(genericArgs[0]), typeof(IEqualityComparer<>).GetGenericType(genericArgs[1]) }, args)!;
                    },
                    CreateFinalCollectionCallback = o => Accessors.InvokeMethod(o, nameof(ImmutableDictionary<_,_>.Builder.ToImmutable))!,
                }
            },
            {
                DataTypes.ImmutableDictionaryBuilder, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer | CollectionInfo.HasValueComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer, CollectionCtorArguments.ValueComparer },
                    CreateInstanceCallback = (t, args) =>
                    {
                        Type[] genericArgs = t.GetGenericArguments();
                        return typeof(ImmutableDictionary).InvokeMethod(nameof(ImmutableDictionary.CreateBuilder), genericArgs,
                            new[] { typeof(IEqualityComparer<>).GetGenericType(genericArgs[0]), typeof(IEqualityComparer<>).GetGenericType(genericArgs[1]) }, args)!;
                    },
                }
            },
            {
                DataTypes.ImmutableSortedDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasComparer | CollectionInfo.HasValueComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer, CollectionCtorArguments.ValueComparer },
                    CreateInstanceCallback = (t, args) =>
                    {
                        Type[] genericArgs = t.GetGenericArguments();
                        return typeof(ImmutableSortedDictionary).InvokeMethod(nameof(ImmutableSortedDictionary.CreateBuilder), genericArgs,
                            new[] { typeof(IComparer<>).GetGenericType(genericArgs[0]), typeof(IEqualityComparer<>).GetGenericType(genericArgs[1]) }, args)!;
                    },
                    CreateFinalCollectionCallback = o => Accessors.InvokeMethod(o, nameof(ImmutableSortedDictionary<_,_>.Builder.ToImmutable))!,
                }
            },
            {
                DataTypes.ImmutableSortedDictionaryBuilder, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasComparer | CollectionInfo.HasValueComparer,
                    CtorArguments = new[] { CollectionCtorArguments.Comparer, CollectionCtorArguments.ValueComparer },
                    CreateInstanceCallback = (t, args) =>
                    {
                        Type[] genericArgs = t.GetGenericArguments();
                        return typeof(ImmutableSortedDictionary).InvokeMethod(nameof(ImmutableSortedDictionary.CreateBuilder), genericArgs,
                            new[] { typeof(IComparer<>).GetGenericType(genericArgs[0]), typeof(IEqualityComparer<>).GetGenericType(genericArgs[1]) }, args)!;
                    },
                }
            },
#endif
#if NET8_0_OR_GREATER
            {
                DataTypes.FrozenDictionary, new CollectionSerializationInfo
                {
                    Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer,
                    ReferenceAbstractGenericType = typeof(FrozenDictionary<,>),
                    CtorArguments = [CollectionCtorArguments.Capacity, CollectionCtorArguments.Comparer],
                    CreateInstanceCallback = (t, args) =>
                    {
                        // Using a Dictionary<TKey, TValue> as a builder with the actual comparer
                        Type[] genericArgs = t.GetGenericArguments();
                        return typeof(Dictionary<,>).GetGenericType(genericArgs).CreateInstance([Reflector.IntType, typeof(IEqualityComparer<>).GetGenericType(genericArgs[0])], args);
                    },
                    CreateFinalCollectionCallback = o =>
                    {
                        Type[] genericArgs = o.GetType().GetGenericArguments();
                        return typeof(FrozenDictionary).InvokeMethod(nameof(FrozenDictionary.ToFrozenDictionary),
                            genericArgs,
                            [Reflector.IEnumerableGenType.GetGenericType(Reflector.KeyValuePairType.GetGenericType(genericArgs)), typeof(IEqualityComparer<>).GetGenericType(genericArgs[0])],
                            o, ((IDictionary)o).GetComparer())!;
                    },
                }
            },
#endif

            #endregion
        };

        private static readonly LockFreeCache<Type, Dictionary<Type, IEnumerable<MethodInfo>?>> methodsByAttributeCache
            = new(_ => new Dictionary<Type, IEnumerable<MethodInfo>?>(4), null, LockFreeCacheOptions.Profile256);

        /// <summary>
        /// Types that cannot be serialized recursively even by a surrogate, including string and void
        /// </summary>
        private static readonly Dictionary<Type, DataTypes> primitiveTypes = new()
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

        /// <summary>
        /// Natively supported non-primitive simple types.
        /// </summary>
        private static readonly Dictionary<Type, DataTypes> supportedNonPrimitiveElementTypes = new()
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
            { typeof(StringSegment), DataTypes.StringSegment },
            { typeof(CompareInfo), DataTypes.CompareInfo },
            { typeof(CultureInfo), DataTypes.CultureInfo },
            { typeof(Comparer), DataTypes.Comparer },
            { typeof(CaseInsensitiveComparer), DataTypes.CaseInsensitiveComparer },
#if !NET35
            { Reflector.BigIntegerType, DataTypes.BigInteger },
            { typeof(Complex), DataTypes.Complex },
#endif
#if NET46_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
            { typeof(Vector2), DataTypes.Vector2 },
            { typeof(Vector3), DataTypes.Vector3 },
            { typeof(Vector4), DataTypes.Vector4 },
            { typeof(Quaternion), DataTypes.Quaternion },
            { typeof(Plane), DataTypes.Plane },
            { typeof(Matrix3x2), DataTypes.Matrix3x2 },
            { typeof(Matrix4x4), DataTypes.Matrix4x4 },
#endif
#if NET47_OR_GREATER || !NETFRAMEWORK
            { typeof(ValueTuple), DataTypes.ValueTuple0 },
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
#if NET7_0_OR_GREATER
            { Reflector.Int128Type, DataTypes.Int128 },
            { Reflector.UInt128Type, DataTypes.UInt128 },
#endif
        };

        /// <summary>
        /// Supported collection types, including some types that are not collections but their elements can be encoded similar to collections.
        /// </summary>
        private static readonly Dictionary<Type, DataTypes> supportedCollections = new()
        {
            // Array is not here because that is an abstract type. Arrays are handled separately.
            { Reflector.ListGenType, DataTypes.List },
            { typeof(Queue<>), DataTypes.Queue },
            { typeof(Stack<>), DataTypes.Stack },
            { typeof(LinkedList<>), DataTypes.LinkedList },
            { typeof(HashSet<>), DataTypes.HashSet },
            { typeof(CircularList<>), DataTypes.CircularList },
            { typeof(ThreadSafeHashSet<>), DataTypes.ThreadSafeHashSet },
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
            { typeof(ThreadSafeDictionary<,>), DataTypes.ThreadSafeDictionary },
            { typeof(AllowNullDictionary<,>), DataTypes.AllowNullDictionary },
#if NET9_0_OR_GREATER
            { typeof(OrderedDictionary<,>), DataTypes.OrderedDictionaryGeneric },
#endif

            { typeof(Hashtable), DataTypes.Hashtable },
            { typeof(SortedList), DataTypes.SortedListNonGeneric },
            { typeof(ListDictionary), DataTypes.ListDictionary },
            { typeof(HybridDictionary), DataTypes.HybridDictionary },
            { typeof(OrderedDictionary), DataTypes.OrderedDictionary },
            { typeof(StringDictionary), DataTypes.StringDictionary },
            { typeof(StringKeyedDictionary<>), DataTypes.StringKeyedDictionary },

            // Array backed collections
            { typeof(ArraySegment<>), DataTypes.ArraySegment },
            { typeof(ArraySection<>), DataTypes.ArraySection },
            { typeof(Array2D<>), DataTypes.Array2D },
            { typeof(Array3D<>), DataTypes.Array3D },
            { typeof(CastArray<,>), DataTypes.CastArray },
            { typeof(CastArray2D<,>), DataTypes.CastArray2D },
            { typeof(CastArray3D<,>), DataTypes.CastArray3D },

            // Tuple-like types. Added to collections for practical reasons such as handling generics or encoding type of items
            { Reflector.KeyValuePairType, DataTypes.KeyValuePair },
            { Reflector.DictionaryEntryType, DataTypes.DictionaryEntry },
            { typeof(StrongBox<>), DataTypes.StrongBox },
#if !NET35
            { typeof(Tuple<>), DataTypes.Tuple1 },
            { typeof(Tuple<,>), DataTypes.Tuple2 },
            { typeof(Tuple<,,>), DataTypes.Tuple3 },
            { typeof(Tuple<,,,>), DataTypes.Tuple4 },
            { typeof(Tuple<,,,,>), DataTypes.Tuple5 },
            { typeof(Tuple<,,,,,>), DataTypes.Tuple6 },
            { typeof(Tuple<,,,,,,>), DataTypes.Tuple7 },
            { typeof(Tuple<,,,,,,,>), DataTypes.Tuple8 },
#endif
#if NET47_OR_GREATER || !NETFRAMEWORK
            { typeof(ValueTuple<>), DataTypes.ValueTuple1 },
            { typeof(ValueTuple<,>), DataTypes.ValueTuple2 },
            { typeof(ValueTuple<,,>), DataTypes.ValueTuple3 },
            { typeof(ValueTuple<,,,>), DataTypes.ValueTuple4 },
            { typeof(ValueTuple<,,,,>), DataTypes.ValueTuple5 },
            { typeof(ValueTuple<,,,,,>), DataTypes.ValueTuple6 },
            { typeof(ValueTuple<,,,,,,>), DataTypes.ValueTuple7 },
            { typeof(ValueTuple<,,,,,,,>), DataTypes.ValueTuple8 },
#endif

            // Fixed size vectors. Added to collections for practical reasons such as handling generics.
#if NETCOREAPP3_0_OR_GREATER
            { typeof(Vector64<>), DataTypes.Vector64 },
            { typeof(Vector128<>), DataTypes.Vector128 },
            { typeof(Vector256<>), DataTypes.Vector256 },
#endif
#if NET8_0_OR_GREATER
            { typeof(Vector512<>), DataTypes.Vector512 },
#endif

            // Immutable collections
#if NETCOREAPP
            { typeof(ImmutableArray<>), DataTypes.ImmutableArray },
            { typeof(ImmutableArray<>.Builder), DataTypes.ImmutableArrayBuilder },
            { typeof(ImmutableList<>), DataTypes.ImmutableList },
            { typeof(ImmutableList<>.Builder), DataTypes.ImmutableListBuilder },
            { typeof(ImmutableHashSet<>), DataTypes.ImmutableHashSet },
            { typeof(ImmutableHashSet<>.Builder), DataTypes.ImmutableHashSetBuilder },
            { typeof(ImmutableSortedSet<>), DataTypes.ImmutableSortedSet },
            { typeof(ImmutableSortedSet<>.Builder), DataTypes.ImmutableSortedSetBuilder },
            { typeof(ImmutableQueue<>), DataTypes.ImmutableQueue },
            { typeof(ImmutableStack<>), DataTypes.ImmutableStack },
            { typeof(ImmutableDictionary<,>), DataTypes.ImmutableDictionary },
            { typeof(ImmutableDictionary<,>.Builder), DataTypes.ImmutableDictionaryBuilder },
            { typeof(ImmutableSortedDictionary<,>), DataTypes.ImmutableSortedDictionary },
            { typeof(ImmutableSortedDictionary<,>.Builder), DataTypes.ImmutableSortedDictionaryBuilder },
            // FrozenSet and FrozenDictionary are not here because they are abstract types
#endif

            // Memory
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            { typeof(Memory<>), DataTypes.Memory },
            { typeof(ReadOnlyMemory<>), DataTypes.ReadOnlyMemory },
#endif
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
        /// <note type="security"><para>If the <see cref="BinarySerializationOptions.SafeMode"/> flag is set in the <see cref="Options"/> property,
        /// then it is not allowed to use any binders other than the <see cref="ForwardedTypesSerializationBinder"/> with its <see cref="ForwardedTypesSerializationBinder.SafeMode"/> enabled.</para>
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
        /// <remarks>
        /// <note type="security"><para>If the <see cref="BinarySerializationOptions.SafeMode"/> flag is set in the <see cref="Options"/> property,
        /// then it is not allowed to use any surrogate selectors.</para>
        /// <para>See the security notes at the <strong>Remarks</strong> section of the <see cref="BinarySerializationFormatter"/> class for more details.</para></note>
        /// </remarks>
        public ISurrogateSelector? SurrogateSelector { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="BinarySerializationFormatter"/> class.
        /// </summary>
        /// <param name="options">Options used for serialization or deserialization. This parameter is optional.
        /// <br/>Default value: <see cref="BinarySerializationOptions.SafeMode"/>, <see cref="BinarySerializationOptions.CompactSerializationOfStructures"/>.</param>
        public BinarySerializationFormatter(BinarySerializationOptions options = BinarySerializer.DefaultSerializationOptions | BinarySerializer.DefaultDeserializationOptions)
        {
            Context = new StreamingContext(StreamingContextStates.All);
            Options = options;
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Internal Methods

        internal static bool IsKnownType(Type type)
        {
            if (primitiveTypes.ContainsKey(type) || supportedNonPrimitiveElementTypes.ContainsKey(type))
                return true;

            if (type.IsConstructedGenericType())
                type = type.GetGenericTypeDefinition();
            return supportedCollections.ContainsKey(type);
        }

        #endregion

        #region Private Methods

        private static DataTypes GetCollectionDataType(DataTypes dt) => dt & DataTypes.CollectionTypesAll;
        private static DataTypes GetUnderlyingCollectionDataType(DataTypes dt) => dt & (DataTypes.CollectionTypesAll & ~DataTypes.NullableExtendedCollection);
        private static DataTypes GetElementDataType(DataTypes dt) => dt & ~DataTypes.CollectionTypesAll;
        private static DataTypes GetUnderlyingSimpleType(DataTypes dt) => dt & DataTypes.SimpleTypesAll;
        private static DataTypes GetCollectionOrElementType(DataTypes dt) => (dt & DataTypes.CollectionTypesAll) != DataTypes.Null ? dt & DataTypes.CollectionTypesAll : dt & ~DataTypes.CollectionTypesAll;
        private static bool IsElementType(DataTypes dt) => (dt & ~DataTypes.CollectionTypesAll) != DataTypes.Null;
        private static bool IsCollectionType(DataTypes dt) => (dt & DataTypes.CollectionTypesAll) != DataTypes.Null;
        private static bool IsCompressible(DataTypes dt) => (dt & DataTypes.SimpleTypesLow) - DataTypes.Int16 <= DataTypes.UIntPtr - DataTypes.Int16;
        private static bool IsCompressed(DataTypes dt) => (dt & DataTypes.Store7BitEncoded) != DataTypes.Null;
        private static bool IsEnum(DataTypes dt) => (dt & DataTypes.Enum) != DataTypes.Null;
        private static bool IsPureType(DataTypes dt) => !IsEnum(dt) && (dt & DataTypes.ImpureType) != DataTypes.ImpureType;
        private static bool IsPureSimpleType(DataTypes dt) => (dt & (DataTypes.SimpleTypesAll | DataTypes.Nullable)) == dt && IsPureType(dt);
        private static bool IsDictionary(DataTypes dt) => (dt & (DataTypes.Dictionary | DataTypes.ExtendedDictionary)) != DataTypes.Null;
        private static bool CanHaveRecursion(DataTypes dt) => (dt & DataTypes.SimpleTypesLow) is DataTypes.RecursiveObjectGraph or DataTypes.BinarySerializable or DataTypes.Object;
        private static bool CanBeEncoded(DataTypes dt) => IsCollectionType(dt) || dt is DataTypes.Pointer or DataTypes.ByRef;
        private static bool IsImpureTypeButEnum(DataTypes dt) => (dt & DataTypes.ImpureType) == DataTypes.ImpureType;
        private static bool IsImpureType(DataTypes dt) => IsEnum(dt) || IsImpureTypeButEnum(dt);
        private static bool IsExtended(DataTypes dt) => (dt & DataTypes.Extended) != DataTypes.Null;
        private static bool IsTuple(DataTypes dt) => GetUnderlyingCollectionDataType(dt) is >= DataTypes.Tuple1 and <= DataTypes.Tuple8 or >= DataTypes.ValueTuple1 and <= DataTypes.ValueTuple8;
        private static bool IsReinterpretedCollection(DataTypes dt) => GetUnderlyingCollectionDataType(dt) is >= DataTypes.CastArray and <= DataTypes.CastArray3D;

        private static bool IsNullable(DataTypes dt) => IsCollectionType(dt)
            ? (dt & DataTypes.NullableExtendedCollection) != DataTypes.Null || GetCollectionDataType(dt) is DataTypes.DictionaryEntryNullable or DataTypes.KeyValuePairNullable
            : (dt & DataTypes.Nullable) != DataTypes.Null;

        private static bool HasNonGenericItemOrKey(DataTypes dt) => GetUnderlyingCollectionDataType(dt) is DataTypes.StringKeyedDictionary
            or >= DataTypes.ArrayList and <= DataTypes.StringCollection
            or >= DataTypes.Hashtable and <= DataTypes.DictionaryEntryNullable;

        private static int GetNumberOfTupleElements(DataTypes dt) => GetUnderlyingCollectionDataType(dt) switch
        {
            DataTypes.Tuple1 or DataTypes.ValueTuple1 => 1,
            DataTypes.Tuple2 or DataTypes.ValueTuple2 => 2,
            DataTypes.Tuple3 or DataTypes.ValueTuple3 => 3,
            DataTypes.Tuple4 or DataTypes.ValueTuple4 => 4,
            DataTypes.Tuple5 or DataTypes.ValueTuple5 => 5,
            DataTypes.Tuple6 or DataTypes.ValueTuple6 => 6,
            DataTypes.Tuple7 or DataTypes.ValueTuple7 => 7,
            DataTypes.Tuple8 or DataTypes.ValueTuple8 => 8,
            _ => 0,
        };

        private static int GetNumberOfElementDataTypes(DataTypes dt) => IsDictionary(dt) ? HasNonGenericItemOrKey(dt) ? 1 : 2
            : IsTuple(dt) ? GetNumberOfTupleElements(dt)
            : IsCollectionType(dt) ? IsReinterpretedCollection(dt) ? 2 : 1
            : dt is DataTypes.Pointer or DataTypes.ByRef ? 1
            : 0;

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
            // using the lowest byte only
            if ((dataType & (DataTypes)0xFF) == dataType)
            {
                Debug.Assert(!IsExtended(dataType));
                bw.Write((byte)dataType);
                return;
            }

            if ((dataType & DataTypes.SimpleTypesExtended) != DataTypes.Null)
            {
                Debug.Assert((dataType & DataTypes.SimpleTypesLow) == DataTypes.Null);
                dataType |= DataTypes.ExtendedSimpleType;
            }

            if ((dataType & DataTypes.CollectionTypesExtended) != DataTypes.Null)
            {
                Debug.Assert((dataType & DataTypes.CollectionTypesLow) == DataTypes.Null);
                dataType |= DataTypes.ExtendedCollectionType;
            }

            if (((uint)dataType & 0xFF00u) != 0u)
                dataType |= DataTypes.Extended;

            // writing the low half
            if ((dataType & (DataTypes)0xFFFF) == dataType)
            {
                bw.Write((ushort)dataType);
                return;
            }

            // writing all 4 bytes (e.g. extended collection with extended nullable element)
            if (((uint)dataType & 0xFF00u) != 0u && ((uint)dataType & 0xFF0000u) != 0u && ((uint)dataType & 0xFF000000u) != 0u)
            {
                bw.Write((uint)dataType);
                return;
            }

            // writing the lowest byte along with nonzero higher bytes
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            Span<byte> buffer = stackalloc byte[3];
#else
            var buffer = new byte[3];
#endif
            int len = 1;
            buffer[0] = (byte)dataType;
            if (((uint)dataType & 0xFF00u) != 0u)
                buffer[len++] = (byte)((uint)dataType >> 8);
            if (((uint)dataType & 0xFF0000u) != 0u)
                buffer[len++] = (byte)((uint)dataType >> 16);
            if (((uint)dataType & 0xFF000000u) != 0u)
                buffer[len++] = (byte)((uint)dataType >> 24);
            Debug.Assert(len is > 1 and < 4);
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            bw.Write(buffer.Slice(0, len));
#else
            bw.Write(buffer, 0, len);
#endif
        }

        private static void WriteDataType(BinaryWriter bw, DataTypesEnumerator enumerator)
        {
            enumerator.Save();
            do
            {
                WriteDataType(bw, enumerator.Current);
            } while (enumerator.MoveNext());
            enumerator.Restore();
        }

        private static DataTypes ReadDataType(BinaryReader br)
        {
            var result = (DataTypes)br.ReadByte();
            if (IsExtended(result))
            {
                result |= (DataTypes)(br.ReadByte() << 8);
                result &= ~DataTypes.Extended;
            }

            if ((result & DataTypes.SimpleTypesLow) == DataTypes.ExtendedSimpleType)
            {
                result |= (DataTypes)(br.ReadByte() << 16);
                result &= ~DataTypes.ExtendedSimpleType;
            }

            if ((result & DataTypes.CollectionTypesLow) == DataTypes.ExtendedCollectionType)
            {
                result |= (DataTypes)(br.ReadByte() << 24);
                result &= ~DataTypes.ExtendedCollectionType;
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
            if (dataType is DataTypes.Null or DataTypes.SimpleTypesLow or DataTypes.SimpleTypesAll or DataTypes.CollectionTypesLow or DataTypes.CollectionTypesAll)
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
            using BinaryWriter bw = new BinaryWriter(result = new MemoryStream());
            var manager = new SerializationManager(Context, Options, Binder, SurrogateSelector);
            manager.WriteRoot(bw, data);
            return result.ToArray();
        }

        /// <summary>
        /// Deserializes the specified part of a byte array into an object. If <see cref="BinarySerializationOptions.SafeMode"/> is enabled
        /// in <see cref="Options"/> and <paramref name="rawData"/> contains natively not supported types by name, then you should use
        /// the other overloads to specify the expected types.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Deserialize{T}(byte[], int, Type[])"/> overload for details.
        /// </summary>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="offset">Points to the starting position of the object data in <paramref name="rawData"/>. This parameter is optional.
        /// <br/>Default value: <c>0</c>.</param>
        /// <returns>The deserialized data.</returns>
        public object? Deserialize(byte[] rawData, int offset = 0)
            => Deserialize<object?>(rawData, offset, (IEnumerable<Type>?)null);

        /// <summary>
        /// Deserializes the specified part of a byte array into an object.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Deserialize{T}(byte[], int, Type[])"/> overload for details.
        /// </summary>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="offset">Points to the starting position of the object data in <paramref name="rawData"/>.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in <paramref name="rawData"/> by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or <paramref name="rawData"/> does not contain any types by name, then this parameter is optional.</param>
        /// <returns>The deserialized object.</returns>
        public object? Deserialize(byte[] rawData, int offset, params Type[]? expectedCustomTypes)
            => Deserialize<object?>(rawData, offset, (IEnumerable<Type>?)expectedCustomTypes);

        /// <summary>
        /// Deserializes a byte array into an object.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Deserialize{T}(byte[], int, Type[])"/> overload for details.
        /// </summary>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in <paramref name="rawData"/> by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or <paramref name="rawData"/> does not contain any types by name, then this parameter is optional.</param>
        /// <returns>The deserialized object.</returns>
        public object? Deserialize(byte[] rawData, params Type[]? expectedCustomTypes)
            => Deserialize<object?>(rawData, 0, (IEnumerable<Type>?)expectedCustomTypes);

        /// <summary>
        /// Deserializes the specified part of a byte array into an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of the result.</typeparam>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="offset">Points to the starting position of the object data in <paramref name="rawData"/>.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in <paramref name="rawData"/> by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or <paramref name="rawData"/> does not contain any types by name, then this parameter is optional.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// <para><paramref name="expectedCustomTypes"/> must be specified if <see cref="BinarySerializationOptions.SafeMode"/> is enabled in <see cref="Options"/>
        /// and <paramref name="rawData"/> contains types encoded by their names. Natively supported types are not needed to be included
        /// unless the original object was serialized with the <see cref="BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes"/> option enabled.</para>
        /// <para><typeparamref name="T"/> is allowed to be an interface or abstract type but if it's different from the actual type of the result,
        /// then the actual type also might needed to be included in <paramref name="expectedCustomTypes"/>.</para>
        /// <para>You can specify <paramref name="expectedCustomTypes"/> even if <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// as it may improve the performance of type resolving and can help avoiding possible ambiguities if types were not serialized with full assembly identity
        /// (e.g. if <see cref="BinarySerializationOptions.OmitAssemblyQualifiedNames"/> was enabled on serialization).</para>
        /// <para>If a type in <paramref name="expectedCustomTypes"/> has a different assembly identity in the deserialization stream, and it is not indicated
        /// by a <see cref="TypeForwardedFromAttribute"/> declared on the type, then you should set the <see cref="Binder"/> property to
        /// a <see cref="ForwardedTypesSerializationBinder"/> instance to specify the expected types.</para>
        /// <para>For arrays it is enough to specify the element type and for generic types you can specify the
        /// natively not supported generic type definition and generic type arguments separately.
        /// If <paramref name="expectedCustomTypes"/> contains constructed generic types, then the generic type definition and
        /// the type arguments will be treated as expected types in any combination.</para>
        /// </remarks>
        public T Deserialize<T>(byte[] rawData, int offset, params Type[]? expectedCustomTypes)
            => Deserialize<T>(rawData, offset, (IEnumerable<Type>?)expectedCustomTypes);

        /// <summary>
        /// Deserializes a byte array into an instance of <typeparamref name="T"/>.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Deserialize{T}(byte[], int, Type[])"/> overload for details.
        /// </summary>
        /// <typeparam name="T">The expected type of the result.</typeparam>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in <paramref name="rawData"/> by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or <paramref name="rawData"/> does not contain any types by name, then this parameter is optional.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        public T Deserialize<T>(byte[] rawData, params Type[]? expectedCustomTypes)
            => Deserialize<T>(rawData, 0, (IEnumerable<Type>?)expectedCustomTypes);

        /// <summary>
        /// Deserializes the specified part of a byte array into an object.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Deserialize{T}(byte[], int, Type[])"/> overload for details.
        /// </summary>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="offset">Points to the starting position of the object data in <paramref name="rawData"/>.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in <paramref name="rawData"/> by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or <paramref name="rawData"/> does not contain any types by name, then this parameter can be <see langword="null"/>.</param>
        /// <returns>The deserialized object.</returns>
        public object? Deserialize(byte[] rawData, int offset, IEnumerable<Type>? expectedCustomTypes)
            => Deserialize<object?>(rawData, offset, expectedCustomTypes);

        /// <summary>
        /// Deserializes a byte array into an object.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Deserialize{T}(byte[], int, Type[])"/> overload for details.
        /// </summary>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in <paramref name="rawData"/> by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or <paramref name="rawData"/> does not contain any types by name, then this parameter can be <see langword="null"/>.</param>
        /// <returns>The deserialized object.</returns>
        public object? Deserialize(byte[] rawData, IEnumerable<Type>? expectedCustomTypes)
            => Deserialize<object?>(rawData, 0, expectedCustomTypes);

        /// <summary>
        /// Deserializes the specified part of a byte array into an instance of <typeparamref name="T"/>.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Deserialize{T}(byte[], int, Type[])"/> overload for details.
        /// </summary>
        /// <typeparam name="T">The expected type of the result.</typeparam>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="offset">Points to the starting position of the object data in <paramref name="rawData"/>.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in <paramref name="rawData"/> by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or <paramref name="rawData"/> does not contain any types by name, then this parameter can be <see langword="null"/>.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        public T Deserialize<T>(byte[] rawData, int offset, IEnumerable<Type>? expectedCustomTypes)
        {
            using var br = new BinaryReader(offset == 0 ? new MemoryStream(rawData) : new MemoryStream(rawData, offset, rawData.Length - offset));
            return DeserializeByReader<T>(br, expectedCustomTypes);
        }

        /// <summary>
        /// Deserializes a byte array into an instance of <typeparamref name="T"/>.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Deserialize{T}(byte[], int, Type[])"/> overload for details.
        /// </summary>
        /// <typeparam name="T">The expected type of the result.</typeparam>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in <paramref name="rawData"/> by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or <paramref name="rawData"/> does not contain any types by name, then this parameter can be <see langword="null"/>.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        public T Deserialize<T>(byte[] rawData, IEnumerable<Type>? expectedCustomTypes)
            => Deserialize<T>(rawData, 0, expectedCustomTypes);

        /// <summary>
        /// Serializes the given <paramref name="data"/> into a <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream, into which the data is written. The stream must support writing and will remain open after serialization.</param>
        /// <param name="data">The data that will be written into the stream.</param>
        [SecuritySafeCritical]
        public void SerializeToStream(Stream stream, object? data) => SerializeByWriter(new BinaryWriter(stream), data);

        /// <summary>
        /// Deserializes the content of the specified serialization <paramref name="stream"/> from its current position into an object.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is enabled in <see cref="Options"/> and <paramref name="stream"/>
        /// contains natively not supported types by name, then you should use the other overloads to specify the expected types.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="DeserializeFromStream{T}(Stream, Type[])"/> overload for details.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing the serialized data. The stream must support reading and will remain open after deserialization.</param>
        /// <returns>The deserialized data.</returns>
        public object? DeserializeFromStream(Stream stream)
            => DeserializeByReader<object?>(new BinaryReader(stream), (IEnumerable<Type>?)null);

        /// <summary>
        /// Deserializes the content of the specified serialization <paramref name="stream"/> from its current position into an object.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="DeserializeFromStream{T}(Stream, Type[])"/> overload for details.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing the serialized data. The stream must support reading and will remain open after deserialization.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in the serialization <paramref name="stream"/> by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or <paramref name="stream"/> does not contain any types by name, then this parameter is optional.</param>
        /// <returns>The deserialized object.</returns>
        public object? DeserializeFromStream(Stream stream, params Type[]? expectedCustomTypes)
            => DeserializeByReader<object?>(new BinaryReader(stream), (IEnumerable<Type>?)expectedCustomTypes);

        /// <summary>
        /// Deserializes the content of the specified serialization <paramref name="stream"/> from its current position into an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of the result.</typeparam>
        /// <param name="stream">The <see cref="Stream"/> containing the serialized data. The stream must support reading and will remain open after deserialization.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in the serialization <paramref name="stream"/> by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or <paramref name="stream"/> does not contain any types by name, then this parameter is optional.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// <para><paramref name="expectedCustomTypes"/> must be specified if <see cref="BinarySerializationOptions.SafeMode"/> is enabled in <see cref="Options"/>
        /// and the serialization <paramref name="stream"/> contains types encoded by their names. Natively supported types are not needed to be included
        /// unless the original object was serialized with the <see cref="BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes"/> option enabled.</para>
        /// <para><typeparamref name="T"/> is allowed to be an interface or abstract type but if it's different from the actual type of the result,
        /// then the actual type also might needed to be included in <paramref name="expectedCustomTypes"/>.</para>
        /// <para>You can specify <paramref name="expectedCustomTypes"/> even if <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// as it may improve the performance of type resolving and can help avoiding possible ambiguities if types were not serialized with full assembly identity
        /// (e.g. if <see cref="BinarySerializationOptions.OmitAssemblyQualifiedNames"/> was enabled on serialization).</para>
        /// <para>If a type in <paramref name="expectedCustomTypes"/> has a different assembly identity in the deserialization stream, and it is not indicated
        /// by a <see cref="TypeForwardedFromAttribute"/> declared on the type, then you should set the <see cref="Binder"/> property to
        /// a <see cref="ForwardedTypesSerializationBinder"/> instance to specify the expected types.</para>
        /// <para>For arrays it is enough to specify the element type and for generic types you can specify the
        /// natively not supported generic type definition and generic type arguments separately.
        /// If <paramref name="expectedCustomTypes"/> contains constructed generic types, then the generic type definition and
        /// the type arguments will be treated as expected types in any combination.</para>
        /// </remarks>
        public T DeserializeFromStream<T>(Stream stream, params Type[]? expectedCustomTypes)
            => DeserializeByReader<T>(new BinaryReader(stream), (IEnumerable<Type>?)expectedCustomTypes);

        /// <summary>
        /// Deserializes the content of the specified serialization <paramref name="stream"/> from its current position into an object.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="DeserializeFromStream{T}(Stream, Type[])"/> overload for details.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing the serialized data. The stream must support reading and will remain open after deserialization.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in the serialization <paramref name="stream"/> by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or <paramref name="stream"/> does not contain any types by name, then this parameter can be <see langword="null"/>.</param>
        /// <returns>The deserialized object.</returns>
        public object? DeserializeFromStream(Stream stream, IEnumerable<Type>? expectedCustomTypes)
            => DeserializeByReader<object?>(new BinaryReader(stream), expectedCustomTypes);

        /// <summary>
        /// Deserializes the content of the specified serialization <paramref name="stream"/> from its current position into an instance of <typeparamref name="T"/>.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="DeserializeFromStream{T}(Stream, Type[])"/> overload for details.
        /// </summary>
        /// <typeparam name="T">The expected type of the result.</typeparam>
        /// <param name="stream">The <see cref="Stream"/> containing the serialized data. The stream must support reading and will remain open after deserialization.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in the serialization <paramref name="stream"/> by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or <paramref name="stream"/> does not contain any types by name, then this parameter can be <see langword="null"/>.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        public T DeserializeFromStream<T>(Stream stream, IEnumerable<Type>? expectedCustomTypes)
            => DeserializeByReader<T>(new BinaryReader(stream), expectedCustomTypes);

        /// <summary>
        /// Serializes the given <paramref name="data"/> by using the provided <paramref name="writer"/>.
        /// </summary>
        /// <remarks>
        /// <note>This method produces compatible serialized data with <see cref="Serialize">Serialize</see>
        /// and <see cref="SerializeToStream">SerializeToStream</see> methods only when encoding of the writer is UTF-8.
        /// Otherwise, you must use <see cref="O:KGySoft.Serialization.Binary.BinarySerializationFormatter.DeserializeByReader">DeserializeByReader</see> with the same encoding as here.</note>
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
        /// Deserializes the content of a serialization stream wrapped by the specified <paramref name="reader"/> from its current position into an object.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is enabled in <see cref="Options"/> and the stream
        /// contains natively not supported types by name, then you should use the other overloads to specify the expected types.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="DeserializeByReader{T}(BinaryReader, Type[])"/> overload for details.
        /// </summary>
        /// <param name="reader">The reader that wraps the stream containing the serialized data. The reader will remain open after deserialization.</param>
        /// <returns>The deserialized data.</returns>
        public object? DeserializeByReader(BinaryReader reader)
            => DeserializeByReader<object?>(reader, (IEnumerable<Type>?)null);

        /// <summary>
        /// Deserializes the content of a serialization stream wrapped by the specified <paramref name="reader"/> from its current position into an object.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="DeserializeByReader{T}(BinaryReader, Type[])"/> overload for details.
        /// </summary>
        /// <param name="reader">The reader that wraps the stream containing the serialized data. The reader will remain open after deserialization.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in the serialization stream by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or the stream does not contain any types by name, then this parameter is optional.</param>
        /// <returns>The deserialized object.</returns>
        public object? DeserializeByReader(BinaryReader reader, params Type[]? expectedCustomTypes)
            => DeserializeByReader<object?>(reader, (IEnumerable<Type>?)expectedCustomTypes);

        /// <summary>
        /// Deserializes the content of a serialization stream wrapped by the specified <paramref name="reader"/> from its current position
        /// into an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of the result.</typeparam>
        /// <param name="reader">The reader that wraps the stream containing the serialized data. The reader will remain open after deserialization.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in the serialization stream by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or the stream does not contain any types by name, then this parameter is optional.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        /// <remarks>
        /// <note>If data was serialized by <see cref="Serialize">Serialize</see> or <see cref="SerializeToStream">SerializeToStream</see> methods, then
        /// <paramref name="reader"/> must use UTF-8 encoding to get the correct result. If data was serialized by
        /// the <see cref="SerializeByWriter">SerializeByWriter</see> method, then you must use the same encoding as was used there.</note>
        /// <para><paramref name="expectedCustomTypes"/> must be specified if <see cref="BinarySerializationOptions.SafeMode"/> is enabled in <see cref="Options"/>
        /// and the serialization stream contains types encoded by their names. Natively supported types are not needed to be included
        /// unless the original object was serialized with the <see cref="BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes"/> option enabled.</para>
        /// <para><typeparamref name="T"/> is allowed to be an interface or abstract type but if it's different from the actual type of the result,
        /// then the actual type also might needed to be included in <paramref name="expectedCustomTypes"/>.</para>
        /// <para>You can specify <paramref name="expectedCustomTypes"/> even if <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// as it may improve the performance of type resolving and can help avoiding possible ambiguities if types were not serialized with full assembly identity
        /// (e.g. if <see cref="BinarySerializationOptions.OmitAssemblyQualifiedNames"/> was enabled on serialization).</para>
        /// <para>If a type in <paramref name="expectedCustomTypes"/> has a different assembly identity in the deserialization stream, and it is not indicated
        /// by a <see cref="TypeForwardedFromAttribute"/> declared on the type, then you should set the <see cref="Binder"/> property to
        /// a <see cref="ForwardedTypesSerializationBinder"/> instance to specify the expected types.</para>
        /// <para>For arrays it is enough to specify the element type and for generic types you can specify the
        /// natively not supported generic type definition and generic type arguments separately.
        /// If <paramref name="expectedCustomTypes"/> contains constructed generic types, then the generic type definition and
        /// the type arguments will be treated as expected types in any combination.</para>
        /// </remarks>
        public T DeserializeByReader<T>(BinaryReader reader, params Type[]? expectedCustomTypes)
            => DeserializeByReader<T>(reader, (IEnumerable<Type>?)expectedCustomTypes);

        /// <summary>
        /// Deserializes the content of a serialization stream wrapped by the specified <paramref name="reader"/> from its current position into an object.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="DeserializeByReader{T}(BinaryReader, Type[])"/> overload for details.
        /// </summary>
        /// <param name="reader">The reader that wraps the stream containing the serialized data. The reader will remain open after deserialization.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in the serialization stream by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or the stream does not contain any types by name, then this parameter can be <see langword="null"/>.</param>
        /// <returns>The deserialized object.</returns>
        public object? DeserializeByReader(BinaryReader reader, IEnumerable<Type>? expectedCustomTypes)
            => DeserializeByReader<object?>(reader, expectedCustomTypes);

        /// <summary>
        /// Deserializes the content of a serialization stream wrapped by the specified <paramref name="reader"/> from its current position
        /// into an instance of <typeparamref name="T"/>.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="DeserializeByReader{T}(BinaryReader, Type[])"/> overload for details.
        /// </summary>
        /// <typeparam name="T">The expected type of the result.</typeparam>
        /// <param name="reader">The reader that wraps the stream containing the serialized data. The reader will remain open after deserialization.</param>
        /// <param name="expectedCustomTypes">The types that are expected to present in the serialization stream by name.
        /// If <see cref="BinarySerializationOptions.SafeMode"/> is not enabled in <see cref="Options"/>
        /// or the stream does not contain any types by name, then this parameter can be <see langword="null"/>.</param>
        /// <returns>The deserialized instance of <typeparamref name="T"/>.</returns>
        [SecuritySafeCritical]
        public T DeserializeByReader<T>(BinaryReader reader, IEnumerable<Type>? expectedCustomTypes)
        {
            if (reader == null!)
                Throw.ArgumentNullException(Argument.reader);
            var manager = new DeserializationManager(Context, Options, Binder, SurrogateSelector, expectedCustomTypes, typeof(T));
            return (T)manager.Deserialize(reader)!;
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
