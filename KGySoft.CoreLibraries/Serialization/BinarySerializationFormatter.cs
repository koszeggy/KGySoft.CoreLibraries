#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

/* HOWTO
 * =====
 * 
 * I. Add a simple type
 * ~~~~~~~~~~~~~~~~~~~~
 * 1. Add type to DataTypes 0-5 bits (adjust free places in comments)
 * 2. Handle type in Write c.) - if type is non sealed, below non-sealed types: create a WriteXXX (where XXX is the type)
 * 3. Handle type in GetSupportedElementType d.)
 * 4. Handle type in WriteElement: check null for reference types, then simply call WriteXXX
 * 5. Add type to DataTypeDescriptor.GetElementType
 * 6. Handle type in ReadObject: for reference types check null if collectionDescriptor is not null, then create object
 * 7. Add type to unit test:
 *    - SerializeSimpleTypes
 *    - SerializeSimpleArrays
 *    - SerializeNullableArrays (value types)
 *    - SerializationSurrogateTest
 * 8. Add type to description - Simple types
 * 
 * II. Add a collection type
 * ~~~~~~~~~~~~~~~~~~~~~~~~~
 * 1. Add type to DataTypes 8-13 bits (adjust free places in comments)
 *    - 0..15 << 8: Generic collections
 *    - 16..31 << 8: Generic dictionaries
 *    - 32..47 << 8: Non-generic collections
 *    - 48..63 << 8: Non-generic dictionaries
 * 2. Update serializationInfo dictionary in static constructor - mind the groups of 1.
 *    - If new CollectionInfo flag has to be defined, a property in CollectionSerializationInfo might be also needed
 * 3. Add type to IsSupportedCollection (more common types first)
 * 4. Handle type in GetSupportedCollectionType - mind generic/non generic groups
 * 5. Handle type in GetDictionaryValueTypes - mind non-dictionary/dictionary types
 * 6. Add type to DataTypeDescriptor.GetCollectionType - mind groups
 * 7. If needed, update CollectionSerializationInfo.WriteSpecificProperties and InitializeCollection (e.g. new flag in 2.)
 * 8. Add type to unit test:
 *    - SerializeSimpleGenericCollections or SerializeSimpleNonGenericCollections
 *    - SerializeSupportedDictionaryValues - twice when generic dictionary type; otherwise, only once
 *   [- SerializeComplexGenericCollections  - when generic]
 *   [- SerializationSurrogateTest]
 * 9. Add type to description - Collections
*/
namespace KGySoft.Serialization
{
    /// <summary>
    /// Serializes and deserialized objects in binary format.
    /// <br/>See the <strong>Remarks</strong> section for details and for the differences to <see cref="BinaryFormatter"/>.
    /// </summary>
    /// <seealso cref="BinarySerializer"/>
    /// <seealso cref="BinarySerializationOptions"/>
    /// <seealso cref="IBinarySerializable"/>
    /// <remarks>
    /// <para><see cref="BinarySerializationFormatter"/> aims to serialize objects effectively where the serialized data is almost always more compact than the results produced by the <see cref="BinaryFormatter"/> class.</para>
    /// <para><see cref="BinarySerializationFormatter"/> natively supports all of the primitive types and a sort of other simple types, arrays, generic and non-generic collections.
    /// <note>Serialization of natively supported types produce an especially compact result because these types are not serialized by traversing and storing the fields of the object graph recursively. This also means better speed performance
    /// for these types. Serialization of complex types can be sometimes slower than by <see cref="BinaryFormatter"/> (especially for the first time) but the serialized result is almost always shorter than the one by <see cref="BinaryFormatter"/>,
    /// especially when generic types are involved.</note></para>
    /// <para>Even if a type is not marked to be serializable by the <see cref="SerializableAttribute"/>, then you can use the <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option to force their serialization.
    /// Alternatively, you can implement the <see cref="IBinarySerializable"/> interface, which can be used to produce a more compact custom serialization than the one provided by implementing the <see cref="ISerializable"/> interface.
    /// A custom serialization logic can be applied also by setting the <see cref="SurrogateSelector"/> property.<para>
    /// </para>Similarly to <see cref="BinaryFormatter"/>, <see cref="ISerializable"/> implementations are also supported, and they are considered only for types marked by the <see cref="SerializableAttribute"/>, unless
    /// the <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option is enabled for the serialization.</para>
    /// <para>As <see cref="BinarySerializationFormatter"/> implements <see cref="IFormatter"/> it fully supports <see cref="SerializationBinder"/> and <see cref="ISurrogateSelector"/> implementations.</para>
    /// <para>There are three ways to serialize/deserialize an object. To serialize into a byte array use the <see cref="Serialize">Serialize</see> method. Its result can be deserialized by the <see cref="Deserialize">Deserialize</see> method.
    /// Additionally, you can use the <see cref="SerializeToStream">SerializeToStream</see>/<see cref="DeserializeFromStream">DeserializeFromStream</see> methods to dump/read the result to and from a <see cref="Stream"/>, and the
    /// the <see cref="SerializeByWriter">SerializeByWriter</see>/<see cref="DeserializeByReader">DeserializeByReader</see> methods to use specific <see cref="BinaryWriter"/> and <see cref="BinaryReader"/> instances for
    /// serialization and deserialization, respectively.</para>
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
    /// <item><see cref="Enum"/> types</item>
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
    /// <item><see cref="Dictionary{TKey,TValue}"/></item>
    /// <item><see cref="SortedList{TKey,TValue}"/></item>
    /// <item><see cref="SortedDictionary{TKey,TValue}"/></item>
    /// <item><see cref="CircularSortedList{TKey,TValue}"/></item>
    /// <item><see cref="Queue{T}"/></item>
    /// <item><see cref="Stack{T}"/></item>
    /// <item><see cref="SortedSet{T}"/> (in .NET 4 and above)</item>
    /// </list>
    /// <note>
    /// <list type="bullet">
    /// <item><see cref="Array"/>s can be single- and multidimensional, jagged (array of arrays) and don't have to be zero index-based. Arrays and other generic collections can be nested.</item>
    /// <item>If a collection uses a non-default <see cref="IEqualityComparer{T}"/> or <see cref="IComparer{T}"/> implementation, then it is possible that the type cannot be serialized without enabling
    /// <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option, unless the comparer is decorated by <see cref="SerializableAttribute"/> or implements the <see cref="IBinarySerializable"/> interface.</item>
    /// <item>If an <see cref="Array"/> has <see cref="object"/> element type or <see cref="object"/> is used in generic arguments of the collection sabove and an element is not a natively supported type, then recursive serialization of fields
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
    /// <note type="tip">Try also <a href="https://dotnetfiddle.net/Q6t0le" target="_blank">online</a>.</note>
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
    /// using KGySoft.CoreLibraries;
    /// using KGySoft.Serialization;
    /// 
    /// public static class Example
    /// {
    ///     public static void Main()
    ///     {
    ///         // feel free to change the type in NextObject<>
    ///         var instance = ThreadSafeRandom.Instance.NextObject<Dictionary<int, List<string>>>();
    ///         Console.WriteLine("Generated object:   " + Dump(instance));
    /// 
    ///         using (var ms = new MemoryStream())
    ///         {
    ///             // serializing by KGy SOFT version:
    ///             IFormatter formatter = new BinarySerializationFormatter();
    ///             formatter.Serialize(ms, instance);
    /// 
    ///             // deserialization:
    ///             ms.Position = 0L;
    ///             object deserialized = formatter.Deserialize(ms);
    /// 
    ///             Console.WriteLine("Deserialized object " + Dump(deserialized));
    ///             Console.WriteLine("Length by BinarySerializationFormatter: " + ms.Length);
    /// 
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
    ///             return $"[{String.Join(", ", enumerable.Cast<object>().Select(Dump))}]";
    /// 
    ///         return $"{{{String.Join("; ", o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => $"{p.Name} = {Dump(p.GetValue(o))}"))}}}";
    ///     }
    /// }
    /// 
    /// // This code example produces a similar output to this one:
    /// // Generated object:   [{Key = 908558467; Value = [abufaji, xica]}, {Key = 2026569158; Value = [hivelu]}]
    /// // Deserialized object [{Key = 908558467; Value = [abufaji, xica]}, {Key = 2026569158; Value = [hivelu]}]
    /// // Length by BinarySerializationFormatter: 43
    /// // Length by BinaryFormatter: 2214]]></code>
    /// </example>
    /// <seealso cref="BinarySerializer"/>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Supports many types natively, which is intended. See also DataTypes enum.")]
    public sealed partial class BinarySerializationFormatter : IFormatter
    {
        #region Enumerations

        /// <summary>
        /// Represents possible types.
        /// One of the simple types can be combined with one of the collection types and the flags.
        /// </summary>
        [Flags]
        [DebuggerDisplay("{BinarySerializationFormatter.ToString(this)}")] // If debugger cannot display it: Tools/Options/Debugging/General: Use Managed Compatibility Mode
        enum DataTypes : ushort
        {
            // ------ simple types:
            Null = 0,
            Object,
            // ReSharper disable once InconsistentNaming
            DBNull,

            Bool = 3,
            Int8,
            UInt8,
            Int16,
            UInt16,
            Int32,
            UInt32,
            Int64,
            UInt64,

            IntPtr = 12,
            UIntPtr,

            Single = 14,
            Double,
            Decimal,

            Char = 17,
            String,
            StringBuilder,
            Uri,

            DateTime = 21,
            TimeSpan,
            DateTimeOffset,

            Version = 24,
            Guid,

            BitArray = 26, // too complex special handling would be needed as collection so treated as simple type
            BitVector32, // too complex special handling would be needed as collection so treated as simple type
            BitVector32Section = 28,

            // free: 29-58

            // not concrete types encoded as simply types:
            //SerializationEnd = 59, // TODO: a reference to a single private static object, which represents the end added objects by custom serialization
            BinarySerializable = 60, // Implements IBinarySerializable
            RawStruct = 61, // any ValueType
            RecursiveObjectGraph = 62, // Represents an object graph with serialized fields or custom name/value data

            // 63: Reserved. If needed, can be re-used, though SimpleTypes has the same value as mask

            SimpleTypes = 0x3F, // Simple types: 0-5 bits - up to 63 types

            // ------ flags that can be combined with simple types:
            Enum = 1 << 6,
            Nullable = 1 << 7,

            // ------ collection types that can be combined with flags and simple types:
            Array = 1 << 8,
            List = 2 << 8,
            LinkedList = 3 << 8,
            HashSet = 4 << 8,
            Queue = 5 << 8,
            Stack = 6 << 8,
            CircularList = 7 << 8,
            SortedSet = 8 << 8,
            // 9-15 << 8: 7 reserved generic collections

            Dictionary = 16 << 8,
            SortedList = 17 << 8,
            SortedDictionary = 18 << 8,
            CircularSortedList = 19 << 8,

            KeyValuePair = 29 << 8, // special "collection" of exactly one key-value pair
            KeyValuePairNullable = 30 << 8, // Special "collection" of exactly one key-value pair. Nullable flag can be combined only with simple types (here: key) so stored separately.
            // 20-28, 31 << 8 : 9 + 1 reserved generic dictionaries

            ArrayList = 32 << 8,
            QueueNonGeneric = 33 << 8,
            StackNonGeneric = 34 << 8,
            StringCollection = 35 << 8,
            // 36-47 << 8: 12 reserved non-generic collection

            Hashtable = 48 << 8,
            SortedListNonGeneric = 49 << 8,
            ListDictionary = 50 << 8,
            HybridDictionary = 51 << 8,
            OrderedDictionary = 52 << 8,
            StringDictionary = 53 << 8,

            DictionaryEntry = 61 << 8, // special "collection" of exactly one key-value pair
            DictionaryEntryNullable = 62 << 8, // special "collection" of one key-value pair. Nullable flag can be combined only simple types so stored separately.
            // 54-60, 63 << 8 : 7 + 1 reserved non-generic dictionaries

            CollectionTypes = 0x3F00, // Collection types: 8-13 bits (6 bits) - up to 63 types

            // ------ further flags
            Store7BitEncoded = 1 << 14, // Applicable for every >1 byte fix-length data type
            //Reserved = 1 << 15, // TODO: Pointer - similarly used as Nullable flag
        }

        /// <summary>
        /// Special serialization info for collections
        /// </summary>
        [Flags]
        enum CollectionInfo
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
        }

        #endregion

        #region Constants

        private const BinarySerializationOptions extendedFlags = (BinarySerializationOptions)(1 << 7);

        #endregion

        #region Fields

        #region Static Fields

        private static readonly Dictionary<DataTypes, CollectionSerializationInfo> serializationInfo = new Dictionary<DataTypes, CollectionSerializationInfo>(EnumComparer<DataTypes>.Comparer)
        {
            // generic collections
            { DataTypes.Array, CollectionSerializationInfo.Default }, // Could be IsGeneric, but does not matter as arrays are handled separately
            { DataTypes.List, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.HasCapacity } },
            { DataTypes.LinkedList, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric } },
            { DataTypes.HashSet, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.HasEqualityComparer,
                SpecificAddMethod = "Add", ComparerFieldName = "m_comparer"} },
            { DataTypes.Queue, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric,
                SpecificAddMethod = "Enqueue" } },
            { DataTypes.Stack, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.ReverseElements,
                SpecificAddMethod = "Push" } },
            { DataTypes.CircularList, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.HasCapacity } },
            { DataTypes.SortedSet, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.HasComparer,
                ComparerFieldName = "comparer" } },

            // generic dictionaries
            { DataTypes.Dictionary, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer,
                ComparerFieldName = "comparer"} },
            { DataTypes.SortedList, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.HasCapacity | CollectionInfo.IsDictionary | CollectionInfo.HasComparer,
                ComparerFieldName = "comparer" } },
            { DataTypes.SortedDictionary, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasComparer,
                ComparerFieldName = "_set.comparer.keyComparer" } },
            { DataTypes.KeyValuePair, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.IsSingleElement } },
            { DataTypes.KeyValuePairNullable, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.IsSingleElement } },
            { DataTypes.CircularSortedList, new CollectionSerializationInfo { Info = CollectionInfo.IsGeneric | CollectionInfo.IsDictionary | CollectionInfo.HasCapacity | CollectionInfo.HasComparer | CollectionInfo.DefaultEnumComparer,
                ComparerFieldName = "comparer" } },

            // non-generic collections
            { DataTypes.ArrayList, new CollectionSerializationInfo { Info = CollectionInfo.HasCapacity } },
            { DataTypes.QueueNonGeneric, new CollectionSerializationInfo { Info = CollectionInfo.None,
                SpecificAddMethod = "Enqueue" } },
            { DataTypes.StackNonGeneric, new CollectionSerializationInfo { Info = CollectionInfo.ReverseElements,
                SpecificAddMethod = "Push" } },
            { DataTypes.StringCollection, CollectionSerializationInfo.Default },

            // non-generic dictionaries
            { DataTypes.Hashtable, new CollectionSerializationInfo { Info = CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer,
                ComparerFieldName = "_keycomparer" } },
            { DataTypes.SortedListNonGeneric, new CollectionSerializationInfo { Info = CollectionInfo.HasCapacity | CollectionInfo.IsDictionary | CollectionInfo.HasComparer,
                ComparerFieldName = "comparer" } },
            { DataTypes.ListDictionary, new CollectionSerializationInfo { Info = CollectionInfo.IsDictionary | CollectionInfo.HasComparer, // yes, comparer and not equalitycomparer
                ComparerFieldName = "comparer" } },
            { DataTypes.HybridDictionary, new CollectionSerializationInfo { Info = CollectionInfo.IsDictionary | CollectionInfo.HasCaseInsensitivity } },
            { DataTypes.OrderedDictionary, new CollectionSerializationInfo { Info = CollectionInfo.IsDictionary | CollectionInfo.HasEqualityComparer | CollectionInfo.HasReadOnly,
                ComparerFieldName = "_comparer"} },
            { DataTypes.StringDictionary, new CollectionSerializationInfo { Info = CollectionInfo.IsDictionary,
                SpecificAddMethod = "Add"} },
            { DataTypes.DictionaryEntry, new CollectionSerializationInfo { Info = CollectionInfo.IsDictionary | CollectionInfo.IsSingleElement } },
            { DataTypes.DictionaryEntryNullable, new CollectionSerializationInfo { Info = CollectionInfo.IsDictionary | CollectionInfo.IsSingleElement } },
        };

        private static readonly IThreadSafeCacheAccessor<Type, Dictionary<Type, IEnumerable<MethodInfo>>> methodsByAttributeCache
            = new Cache<Type, Dictionary<Type, IEnumerable<MethodInfo>>>(t => new Dictionary<Type, IEnumerable<MethodInfo>>(4), 256).GetThreadSafeAccessor(true); // true for use just a single lock because the loader is simply a new statement

        private static readonly Dictionary<Type, DataTypes> primitiveTypes = new Dictionary<Type, DataTypes> // including non-primitives such as string and UIntPtr
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
        };

        private static readonly Dictionary<Type, DataTypes> supportedNonPrimitiveElementTypes = new Dictionary<Type, DataTypes>
        {
            { Reflector.DecimalType, DataTypes.Decimal },
            { Reflector.DateTimeType, DataTypes.DateTime },
            { Reflector.DateTimeOffsetType, DataTypes.DateTimeOffset },
            { Reflector.TimeSpanType, DataTypes.TimeSpan },
            { Reflector.ObjectType, DataTypes.Object },
            { typeof(DBNull), DataTypes.DBNull },
            { typeof(Version), DataTypes.Version },
            { typeof(Guid), DataTypes.Guid },
            { typeof(Uri), DataTypes.Uri },
            { typeof(StringBuilder), DataTypes.StringBuilder },
            { typeof(BitArray), DataTypes.BitArray },
            { typeof(BitVector32), DataTypes.BitVector32 },
            { typeof(BitVector32.Section), DataTypes.BitVector32Section },
        };


        private static readonly Dictionary<Type, DataTypes> supportedCollections = new Dictionary<Type, DataTypes>
        {
            { typeof(Array), DataTypes.Array },
            { typeof(List<>), DataTypes.List },
            { typeof(Queue<>), DataTypes.Queue },
            { typeof(Stack<>), DataTypes.Stack },
            { typeof(LinkedList<>), DataTypes.LinkedList },
            { typeof(HashSet<>), DataTypes.HashSet },
#if NET40 || NET45
            { typeof(SortedSet<>), DataTypes.SortedSet },
#elif !NET35
#error .NET version is not set or not supported!
#endif

            { typeof(Dictionary<,>), DataTypes.Dictionary },
            { typeof(SortedList<,>), DataTypes.SortedList },
            { typeof(SortedDictionary<,>), DataTypes.SortedDictionary },
            { typeof(CircularSortedList<,>), DataTypes.CircularSortedList },

            { typeof(ArrayList), DataTypes.ArrayList },
            { typeof(Queue), DataTypes.QueueNonGeneric },
            { typeof(Stack), DataTypes.StackNonGeneric },
            { typeof(StringCollection), DataTypes.StringCollection },

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

        private List<IDeserializationCallback> deserRegObjects;
        private BinarySerializationOptions serializationOptions;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Options used for serialization and deserialization.
        /// See the <see cref="BinarySerializationOptions"/> enumeration for details.
        /// </summary>
        public BinarySerializationOptions Options
        {
            get => serializationOptions;
            set
            {
                if (!value.AllFlagsDefined())
                    throw new ArgumentOutOfRangeException(nameof(value), Res.FlagsEnumOutOfRange(value));

                serializationOptions = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="SerializationBinder"/> that performs type lookups.
        /// </summary>
        /// <remarks>
        /// In .NET 4.0 and above affects both serialization and deserialization. In .NET 3.5 setting this property
        /// has no effect during serialization.
        /// </remarks>
        public SerializationBinder Binder { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="StreamingContext"/> used for serialization and deserialization.
        /// </summary>
        public StreamingContext Context { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="ISurrogateSelector"/> can be used to customize serialization and deserialization.
        /// </summary>
        public ISurrogateSelector SurrogateSelector { get; set; }

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

        private static void ThrowNotSupported(BinarySerializationOptions options, Type type) => throw new NotSupportedException(Res.BinarySerializationNotSupported(type, options));

        /// <summary>
        /// Writes options if needed
        /// </summary>
        private static void WriteOptions(BinaryWriter bw, CircularList<DataTypes> collectionType, BinarySerializationOptions options)
        {
            // options are needed if there is a BinarySerializable or recursively saved element anywhere
            if (collectionType == null || collectionType.Exists(dt => (dt & DataTypes.SimpleTypes) == DataTypes.BinarySerializable || (dt & DataTypes.SimpleTypes) == DataTypes.RecursiveObjectGraph))
            {
                // 1 byte is enough
                if (((int)options & 255) == (int)options)
                {
                    bw.Write((byte)options);
                    return;
                }

                // storing options on 2 bytes
                bw.Write((ushort)(options | extendedFlags));
            }
        }

        /// <summary>
        /// Writes AssemblyQiualifiedName of element types and array ranks if needed
        /// </summary>
        [SecurityCritical]
        private static void WriteTypeNamesAndRanks(BinaryWriter bw, Type type, BinarySerializationOptions options, SerializationManager manager)
        {
            // Enum, BinarySerializable, RawStruct, recursive serialization: type name
            DataTypes elementType = GetSupportedElementType(type, options, manager);
            if ((elementType & DataTypes.Enum) != DataTypes.Null
                || (elementType & DataTypes.SimpleTypes) == DataTypes.BinarySerializable
                || (elementType & DataTypes.SimpleTypes) == DataTypes.RawStruct
                || (elementType & DataTypes.SimpleTypes) == DataTypes.RecursiveObjectGraph)
            {
                if ((elementType & DataTypes.Nullable) == DataTypes.Nullable)
                    type = Nullable.GetUnderlyingType(type);
                manager.WriteType(bw, type);
            }
            // Array: element type name and rank
            else if (type.IsArray)
            {
                WriteTypeNamesAndRanks(bw, type.GetElementType(), options, manager);
                bw.Write((byte)type.GetArrayRank());
            }
            // recursion for generic arguments
            else if (IsSupportedCollection(type))
            {
                foreach (Type genericArgument in type.GetGenericArguments())
                    WriteTypeNamesAndRanks(bw, genericArgument, options, manager);
            }
        }

        /// <summary>
        /// Writes a <paramref name="length"/> bytes length value in the possible most compact form.
        /// </summary>
        private static void WriteDynamicInt(BinaryWriter bw, DataTypes dataType, int length, ulong value)
        {
            switch (length)
            {
                case 2:
                    if (value >= (1UL << 7)) // up to 7 bits
                    {
                        bw.Write((ushort)dataType);
                        bw.Write((ushort)value);
                        return;
                    }
                    break;

                case 4:
                    if (value >= (1UL << 21)) // up to 3*7 bits
                    {
                        bw.Write((ushort)dataType);
                        bw.Write((uint)value);
                        return;
                    }
                    break;

                case 8:
                    if (value >= (1UL << 49)) // up to 7*7 bits
                    {
                        bw.Write((ushort)dataType);
                        bw.Write(value);
                        return;
                    }
                    break;

                default:
                    // should never occur, throwing internal error without resource
                    throw new ArgumentOutOfRangeException(nameof(length));
            }

            // storing the value as 7-bit encoded int, which will be shorter
            dataType |= DataTypes.Store7BitEncoded;
            bw.Write((ushort)dataType);
            Write7BitLong(bw, value);
        }

        /// <summary>
        /// Returning a true value just indicates that the type itself supported without the generic parameters or element type.
        /// </summary>
        private static bool IsSupportedCollection(Type type)
        {
            if (type.IsArray)
                return true;
            if (type.IsValueType)
                type = Nullable.GetUnderlyingType(type) ?? type;
            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();
            return supportedCollections.ContainsKey(type);
        }

        [SecurityCritical]
        private static IEnumerable<DataTypes> EncodeCollectionType(Type type, BinarySerializationOptions options, SerializationManager manager)
        {
            // array
            if (type.IsArray)
                return EncodeArray(type, options, manager);

            DataTypes collectionType = GetSupportedCollectionType(type);
            type = Nullable.GetUnderlyingType(type) ?? type;

            // generic type
            if (type.IsGenericType)
                return EncodeGenericCollection(type, collectionType, options, manager);

            // non-generic types
            switch (collectionType)
            {
                case DataTypes.ArrayList:
                case DataTypes.QueueNonGeneric:
                case DataTypes.StackNonGeneric:
                    return new[] { collectionType | DataTypes.Object };

                case DataTypes.Hashtable:
                case DataTypes.SortedListNonGeneric:
                case DataTypes.ListDictionary:
                case DataTypes.HybridDictionary:
                case DataTypes.OrderedDictionary:
                case DataTypes.DictionaryEntry:
                case DataTypes.DictionaryEntryNullable:
                    return new[] { collectionType | DataTypes.Object, DataTypes.Object };

                case DataTypes.StringCollection:
                    return new[] { collectionType | DataTypes.String };

                case DataTypes.StringDictionary:
                    return new[] { collectionType | DataTypes.String, DataTypes.String };
                default:
                    // should never occur, throwing internal error without resource
                    throw new InvalidOperationException("Element type of non-generic collection is not defined: " + ToString(collectionType));
            }
        }

        [SecurityCritical]
        private static IEnumerable<DataTypes> EncodeArray(Type type, BinarySerializationOptions options, SerializationManager manager)
        {
            Type elementType = type.GetElementType();
            if ((options & BinarySerializationOptions.TryUseSurrogateSelectorForAnyType) != BinarySerializationOptions.None
                && manager.CanUseSurrogate(elementType))
            {
                DataTypes[] result = { DataTypes.Array | DataTypes.RecursiveObjectGraph };
                if (elementType.IsNullable())
                    result[0] |= DataTypes.Nullable;
                return result;
            }

            DataTypes elementDataType = GetSupportedElementType(elementType, options, manager);
            if (elementDataType != DataTypes.Null)
                return new[] { DataTypes.Array | elementDataType };

            if (IsSupportedCollection(elementType))
            {
                IEnumerable<DataTypes> innerType = EncodeCollectionType(elementType, options, manager);
                if (innerType != null)
                    return (new[] { DataTypes.Array }).Concat(innerType);
            }

            return null;
        }

        [SecurityCritical]
        private static IEnumerable<DataTypes> EncodeGenericCollection(Type type, DataTypes collectionType, BinarySerializationOptions options, SerializationManager manager)
        {
            if (collectionType == DataTypes.Null)
                return null;

            Type[] args = type.GetGenericArguments();
            Type elementType = args[0];
            DataTypes elementDataType = GetSupportedElementType(elementType, options, manager);

            // generics with 1 argument
            if (args.Length == 1)
            {
                if (elementDataType != DataTypes.Null)
                    return new[] { collectionType | elementDataType };

                if (IsSupportedCollection(elementType))
                {
                    IEnumerable<DataTypes> innerType = EncodeCollectionType(elementType, options, manager);
                    if (innerType != null)
                        return (new[] { collectionType }).Concat(innerType);
                }

                return null;
            }

            // dictionaries
            Type valueType = args[1];
            DataTypes valueDataType = GetSupportedElementType(valueType, options, manager);

            IEnumerable<DataTypes> keyTypes;
            IEnumerable<DataTypes> valueTypes;

            // key
            if (elementDataType != DataTypes.Null)
                keyTypes = new DataTypes[] { collectionType | elementDataType };
            else if (IsSupportedCollection(elementType))
            {
                keyTypes = EncodeCollectionType(elementType, options, manager);
                if (keyTypes == null)
                    return null;
                keyTypes = (new DataTypes[] { collectionType }).Concat(keyTypes);
            }
            else
                return null;

            // value
            if (valueDataType != DataTypes.Null)
                valueTypes = new DataTypes[] { valueDataType };
            else if (IsSupportedCollection(valueType))
            {
                valueTypes = EncodeCollectionType(valueType, options, manager);
                if (valueTypes == null)
                    return null;
            }
            else
                return null;

            return keyTypes.Concat(valueTypes);
        }

        private static DataTypes GetSupportedCollectionType(Type type)
        {
            if (type.IsArray)
                return DataTypes.Array;

            if (type.IsNullable())
            {
                switch (GetSupportedCollectionType(type.GetGenericArguments()[0]))
                {
                    case DataTypes.DictionaryEntry:
                        return DataTypes.DictionaryEntryNullable;
                    case DataTypes.KeyValuePair:
                        return DataTypes.KeyValuePairNullable;
                    default:
                        return DataTypes.Null;
                }
            }

            if (type.IsGenericType)
                type = type.GetGenericTypeDefinition();
            return supportedCollections.GetValueOrDefault(type);
        }

        [SecurityCritical]
        private static DataTypes GetSupportedElementType(Type type, BinarySerializationOptions options, SerializationManager manager)
        {
            DataTypes elementType;

            // a.) nullable (must be before surrogate-support checks)
            if (type.IsNullable())
            {
                elementType = GetSupportedElementType(type.GetGenericArguments()[0], options, manager);
                if (elementType == DataTypes.Null)
                    return elementType;
                return DataTypes.Nullable | elementType;
            }

            // b.) Natively supported primitive types
            if (primitiveTypes.TryGetValue(type, out elementType))
                return elementType;

            // c.) surrogate for any type: check even for sub-collections
            if ((options & BinarySerializationOptions.TryUseSurrogateSelectorForAnyType) != BinarySerializationOptions.None && manager.CanUseSurrogate(type))
                return DataTypes.RecursiveObjectGraph;

            // if type is a collection, then returning null here
            if (GetSupportedCollectionType(type) != DataTypes.Null)
                return DataTypes.Null;

            // d.) enum
            if (type.IsEnum)
                return DataTypes.Enum | GetSupportedElementType(Enum.GetUnderlyingType(type), options, manager);

            // e.) Natively supported non-primitive types
            if (supportedNonPrimitiveElementTypes.TryGetValue(type, out elementType))
                return elementType;

            // f.) IBinarySerializable implementation
            if (((options & BinarySerializationOptions.IgnoreIBinarySerializable) == BinarySerializationOptions.None) && typeof(IBinarySerializable).IsAssignableFrom(type))
                return DataTypes.BinarySerializable;

            // g.) Any struct if can be serialized
            if ((options & BinarySerializationOptions.CompactSerializationOfStructures) != BinarySerializationOptions.None && type.IsValueType && BinarySerializer.CanSerializeValueType(type, false))
                return DataTypes.RawStruct;

            // h.) Recursive serialization
            if ((options & BinarySerializationOptions.RecursiveSerializationAsFallback) != BinarySerializationOptions.None
                || manager != null && manager.CanUseSurrogate(type)
                || type.IsSerializable || type.IsInterface)
            {
                return DataTypes.RecursiveObjectGraph;
            }

#pragma warning disable 618, 612
            // i.) Any struct (obsolete but still supported as backward compatibility)
            if ((options & BinarySerializationOptions.ForcedSerializationValueTypesAsFallback) != BinarySerializationOptions.None && type.IsValueType)
                return DataTypes.RawStruct;
#pragma warning restore 618, 612

            return DataTypes.Null;
        }

        /// <summary>
        /// Gets the element types for a dictionary
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Very simple method with many common cases")]
        private static CircularList<DataTypes> GetDictionaryValueTypes(CircularList<DataTypes> collectionTypeDescriptor)
        {
            // descriptor must refer a generic dictionary type here
            Debug.Assert(collectionTypeDescriptor.Count > 0, "Type description is invalid: not enough data");
#if DEBUG
            int collType = ((int)(collectionTypeDescriptor[0] & DataTypes.CollectionTypes) >> 8);
            Debug.Assert(collType >= 16 && collType < 32
                || collType >= 48 && collType < 64, "Type description is invalid: dictionary type is expected");
#endif

            CircularList<DataTypes> result = new CircularList<DataTypes>();
            int skipLevel = 0; // starting from -1 because dictionary will increase it by 1 or 2
            bool startingDictionaryResolved = false;
            foreach (DataTypes dataType in collectionTypeDescriptor)
            {
                if (startingDictionaryResolved && skipLevel == 0) // 0 means we are in value already
                    result.Add(dataType);
                else
                {
                    switch (dataType & DataTypes.CollectionTypes)
                    {
                        case DataTypes.Null:
                            // simple type: leaf element of previous collection
                            skipLevel--;
                            break;
                        case DataTypes.Array:
                        case DataTypes.List:
                        //case DataTypes.Collection:
                        case DataTypes.LinkedList:
                        case DataTypes.HashSet:
                        case DataTypes.Queue:
                        case DataTypes.Stack:
                        //case DataTypes.ReadOnlyCollection:
                        case DataTypes.CircularList:
                        case DataTypes.SortedSet:
                        case DataTypes.ArrayList:
                        case DataTypes.QueueNonGeneric:
                        case DataTypes.StackNonGeneric:
                        case DataTypes.StringCollection:
                            // collections with a single element: decreasing level if element is specified
                            if ((dataType & ~DataTypes.CollectionTypes) != DataTypes.Null)
                                skipLevel--;
                            break;
                        case DataTypes.Dictionary:
                        case DataTypes.SortedList:
                        case DataTypes.SortedDictionary:
                        case DataTypes.CircularSortedList:
                        case DataTypes.Hashtable:
                        case DataTypes.SortedListNonGeneric:
                        case DataTypes.ListDictionary:
                        case DataTypes.HybridDictionary:
                        case DataTypes.OrderedDictionary:
                        case DataTypes.StringDictionary:
                        case DataTypes.KeyValuePair:
                        case DataTypes.DictionaryEntry:
                        case DataTypes.KeyValuePairNullable:
                        case DataTypes.DictionaryEntryNullable:
                            // dictionary types: increasing level by 1 if key is not specified, otherwise current level remains
                            if ((dataType & ~DataTypes.CollectionTypes) == DataTypes.Null)
                                skipLevel++;
                            startingDictionaryResolved = true;
                            break;
                    }
                }
            }
            return result;
        }

        private static bool CanHaveRecursion(CircularList<DataTypes> collectionType)
            => collectionType.Exists(dt =>
                (dt & DataTypes.SimpleTypes) == DataTypes.BinarySerializable
                || (dt & DataTypes.SimpleTypes) == DataTypes.RecursiveObjectGraph
                || (dt & DataTypes.SimpleTypes) == DataTypes.Object);

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

        private static bool TryWritePrimitive(BinaryWriter bw, object data)
        {
            if (data == null)
            {
                bw.Write((ushort)DataTypes.Null);
                return true;
            }

            switch (primitiveTypes.GetValueOrDefault(data.GetType()))
            {
                case DataTypes.Bool:
                    bw.Write((ushort)DataTypes.Bool);
                    bw.Write((bool)data);
                    return true;
                case DataTypes.UInt8:
                    bw.Write((ushort)DataTypes.UInt8);
                    bw.Write((byte)data);
                    return true;
                case DataTypes.Int8:
                    bw.Write((ushort)DataTypes.Int8);
                    bw.Write((sbyte)data);
                    return true;
                case DataTypes.Int16:
                    WriteDynamicInt(bw, DataTypes.Int16, 2, (ulong)(short)data);
                    return true;
                case DataTypes.UInt16:
                    WriteDynamicInt(bw, DataTypes.UInt16, 2, (ushort)data);
                    return true;
                case DataTypes.Int32:
                    WriteDynamicInt(bw, DataTypes.Int32, 4, (ulong)(int)data);
                    return true;
                case DataTypes.UInt32:
                    WriteDynamicInt(bw, DataTypes.UInt32, 4, (uint)data);
                    return true;
                case DataTypes.Int64:
                    WriteDynamicInt(bw, DataTypes.Int64, 8, (ulong)(long)data);
                    return true;
                case DataTypes.UInt64:
                    WriteDynamicInt(bw, DataTypes.UInt64, 8, (ulong)data);
                    return true;
                case DataTypes.Char:
                    WriteDynamicInt(bw, DataTypes.Char, 2, (char)data);
                    return true;
                case DataTypes.String:
                    bw.Write((ushort)DataTypes.String);
                    bw.Write((string)data);
                    return true;
                case DataTypes.Single:
                    bw.Write((ushort)DataTypes.Single);
                    bw.Write((float)data);
                    return true;
                case DataTypes.Double:
                    bw.Write((ushort)DataTypes.Double);
                    bw.Write((double)data);
                    return true;
                case DataTypes.IntPtr:
                    bw.Write((ushort)DataTypes.IntPtr);
                    bw.Write(((IntPtr)data).ToInt64());
                    return true;
                case DataTypes.UIntPtr:
                    bw.Write((ushort)DataTypes.UIntPtr);
                    bw.Write(((UIntPtr)data).ToUInt64());
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryWriteSimpleNonPrimitive(BinaryWriter bw, object data)
        {
            switch (supportedNonPrimitiveElementTypes.GetValueOrDefault(data.GetType()))
            {
                case DataTypes.Decimal:
                    bw.Write((ushort)DataTypes.Decimal);
                    bw.Write((decimal)data);
                    return true;
                case DataTypes.DateTime:
                    bw.Write((ushort)DataTypes.DateTime);
                    WriteDateTime(bw, (DateTime)data);
                    return true;
                case DataTypes.DateTimeOffset:
                    bw.Write((ushort)DataTypes.DateTimeOffset);
                    WriteDateTimeOffset(bw, (DateTimeOffset)data);
                    return true;
                case DataTypes.TimeSpan:
                    bw.Write((ushort)DataTypes.TimeSpan);
                    bw.Write(((TimeSpan)data).Ticks);
                    return true;
                case DataTypes.DBNull:
                    bw.Write((ushort)DataTypes.DBNull);
                    return true;
                case DataTypes.Guid:
                    bw.Write((ushort)DataTypes.Guid);
                    bw.Write(((Guid)data).ToByteArray());
                    return true;
                case DataTypes.BitVector32:
                    bw.Write((ushort)DataTypes.BitVector32);
                    bw.Write(((BitVector32)data).Data);
                    return true;
                case DataTypes.BitVector32Section:
                    bw.Write((ushort)DataTypes.BitVector32Section);
                    WriteSection(bw, (BitVector32.Section)data);
                    return true;
                case DataTypes.Version:
                    bw.Write((ushort)DataTypes.Version);
                    WriteVersion(bw, (Version)data);
                    return true;
                case DataTypes.BitArray:
                    bw.Write((ushort)DataTypes.BitArray);
                    WriteBitArray(bw, (BitArray)data);
                    return true;
                case DataTypes.StringBuilder:
                    bw.Write((ushort)DataTypes.StringBuilder);
                    WriteStringBuilder(bw, (StringBuilder)data);
                    return true;
                case DataTypes.Object:
                    bw.Write((ushort)DataTypes.Object);
                    return true;
                case DataTypes.Uri:
                    bw.Write((ushort)DataTypes.Uri);
                    WriteUri(bw, (Uri)data);
                    return true;
                default:
                    return false;
            }
        }

        [SecurityCritical]
        private static void WriteEnum(BinaryWriter bw, object enumObject, SerializationManager manager)
        {
            Type type = enumObject.GetType();
            DataTypes dataType = primitiveTypes.GetValueOrDefault(Enum.GetUnderlyingType(type));

            ulong enumValue = dataType == DataTypes.UInt64 ? (ulong)enumObject : (ulong)((IConvertible)enumObject).ToInt64(null);

            bool is7Bit = false;
            int size = 1;
            switch (dataType)
            {
                case DataTypes.Int8:
                case DataTypes.UInt8:
                    break;
                case DataTypes.Int16:
                case DataTypes.UInt16:
                    size = 2;
                    is7Bit = enumValue < (1UL << 7);
                    break;
                case DataTypes.Int32:
                case DataTypes.UInt32:
                    size = 4;
                    is7Bit = enumValue < (1UL << 21);
                    break;
                case DataTypes.Int64:
                case DataTypes.UInt64:
                    size = 8;
                    is7Bit = enumValue < (1UL << 49);
                    break;
                default:
                    // should never occur, throwing internal error without resource
                    throw new ArgumentOutOfRangeException(nameof(enumObject));
            }

            dataType |= DataTypes.Enum;
            if (is7Bit)
                dataType |= DataTypes.Store7BitEncoded;

            bw.Write((ushort)dataType);
            manager.WriteType(bw, type);
            if (is7Bit)
                Write7BitLong(bw, enumValue);
            else
                bw.Write(BitConverter.GetBytes(enumValue), 0, size);
        }

        private static void WriteDateTime(BinaryWriter bw, DateTime dateTime)
        {
            bw.Write((byte)dateTime.Kind);
            bw.Write(dateTime.Ticks);
        }

        private static void WriteDateTimeOffset(BinaryWriter bw, DateTimeOffset dateTimeOffset)
        {
            bw.Write(((DateTime)Reflector.GetField(dateTimeOffset, "m_dateTime")).Ticks);
            bw.Write((short)Reflector.GetField(dateTimeOffset, "m_offsetMinutes"));
        }

        private static void WriteVersion(BinaryWriter bw, Version version)
        {
            bw.Write(version.Major);
            bw.Write(version.Minor);
            bw.Write(version.Build);
            bw.Write(version.Revision);
        }

        private static void WriteUri(BinaryWriter bw, Uri uri)
        {
            bw.Write(uri.IsAbsoluteUri);
            bw.Write((string)Reflector.InvokeMethod(uri, "GetParts", UriComponents.SerializationInfoString, UriFormat.UriEscaped));
        }

        private static void WriteBitArray(BinaryWriter bw, BitArray bitArray)
        {
            int length = bitArray.Length;
            Write7BitInt(bw, bitArray.Length);
            if (length > 0)
            {
                int[] value = (int[])Reflector.GetField(bitArray, "m_array");
                foreach (int i in value)
                    bw.Write(i);
            }
        }

        private static void WriteStringBuilder(BinaryWriter bw, StringBuilder sb)
        {
            Write7BitInt(bw, sb.Capacity);
            bw.Write(sb.ToString());
        }

        private static void WriteSection(BinaryWriter bw, BitVector32.Section section)
        {
            bw.Write(section.Mask);
            bw.Write(section.Offset);
        }

        private static IEnumerable<MethodInfo> GetMethodsWithAttribute(Type attribute, Type type)
        {
            Dictionary<Type, IEnumerable<MethodInfo>> cacheItem = methodsByAttributeCache[type];

            lock (cacheItem)
            {
                if (cacheItem.TryGetValue(attribute, out IEnumerable<MethodInfo> cachedResult))
                    return cachedResult;

                List<MethodInfo> result = new List<MethodInfo>();
                for (Type t = type; t != null && t != Reflector.ObjectType; t = t.BaseType)
                {
                    foreach (MethodInfo method in t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        if (method.IsDefined(attribute, false))
                        {
                            ParameterInfo[] parameters = method.GetParameters();
                            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(StreamingContext))
                            {
                                result.Add(method);
                            }
                        }
                    }
                }

                if (result.Count > 1)
                    result.Reverse();

                if (result.Count == 0)
                {
                    cacheItem[attribute] = null;
                    return null;
                }

                cacheItem[attribute] = result;
                return result;
            }
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
                    throw new InvalidOperationException(Res.BinarySerializationInvalidStreamData);

                b = br.ReadByte();

                result |= (b & 0x7F) << shift;
                shift += 7;
            }
            while ((b & 0x80) != 0);

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
                {
                    throw new InvalidOperationException(Res.BinarySerializationInvalidStreamData);
                }

                b = br.ReadByte();

                result |= (b & 0x7FL) << shift;
                shift += 7;
            }
            while ((b & 0x80) != 0);

            return result;
        }

        private static object ReadEnum(BinaryReader br, DataTypes dataType, DataTypeDescriptor collectionDescriptor, DeserializationManager manager, bool isTValue)
        {
            Type enumType = collectionDescriptor == null
                ? manager.ReadType(br)
                : collectionDescriptor.GetElementType(isTValue);
            bool is7BitEncoded = (dataType & DataTypes.Store7BitEncoded) != DataTypes.Null;
            switch (dataType & DataTypes.SimpleTypes)
            {
                case DataTypes.Int8:
                    return Enum.ToObject(enumType, br.ReadSByte());
                case DataTypes.UInt8:
                    return Enum.ToObject(enumType, br.ReadByte());
                case DataTypes.Int16:
                    return Enum.ToObject(enumType,
                        is7BitEncoded ? (short)Read7BitInt(br) : br.ReadInt16());
                case DataTypes.UInt16:
                    return Enum.ToObject(enumType,
                        is7BitEncoded ? (ushort)Read7BitInt(br) : br.ReadUInt16());
                case DataTypes.Int32:
                    return Enum.ToObject(enumType, is7BitEncoded ? Read7BitInt(br) : br.ReadInt32());
                case DataTypes.UInt32:
                    return Enum.ToObject(enumType,
                        is7BitEncoded ? (uint)Read7BitInt(br) : br.ReadUInt32());
                case DataTypes.Int64:
                    return Enum.ToObject(enumType, is7BitEncoded ? Read7BitLong(br) : br.ReadInt64());
                case DataTypes.UInt64:
                    return Enum.ToObject(enumType,
                        is7BitEncoded ? (ulong)Read7BitLong(br) : br.ReadUInt64());
                default:
                    throw new InvalidOperationException(Res.BinarySerializationInvalidEnumBase(ToString(dataType & DataTypes.SimpleTypes)));
            }
        }

        private static BinarySerializationOptions ReadOptions(BinaryReader br)
        {
            BinarySerializationOptions options = (BinarySerializationOptions)br.ReadByte();

            // if stored on 2 bytes
            if ((options & extendedFlags) == extendedFlags)
            {
                options &= ~extendedFlags;
                options |= (BinarySerializationOptions)(br.ReadByte() << 8);
            }

            return options;
        }

        /// <summary>
        /// Converts a <see cref="DataTypes"/> enumeration into the corresponding string representation.
        /// This method is needed because <see cref="Enum.ToString()"/> and <see cref="Enum{TEnum}.ToString(TEnum,EnumFormattingOptions,string)"/>
        /// cannot always handle the fields and flags structure of <see cref="DataTypes"/> enum.
        /// </summary>
        private static string ToString(DataTypes dataType)
        {
            if (dataType.In(DataTypes.Null, DataTypes.SimpleTypes, DataTypes.CollectionTypes))
                return dataType.ToString<DataTypes>();

            StringBuilder result = new StringBuilder();
            if ((dataType & DataTypes.CollectionTypes) != DataTypes.Null)
                result.Append(Enum<DataTypes>.ToString(dataType & DataTypes.CollectionTypes, EnumFormattingOptions.CompoundFlagsAndNumber, " | "));
            if ((dataType & ~DataTypes.CollectionTypes) != DataTypes.Null)
            {
                if (result.Length > 0)
                    result.Insert(0, " | ");
                result.Insert(0, Enum<DataTypes>.ToString(dataType & ~DataTypes.CollectionTypes, EnumFormattingOptions.CompoundFlagsAndNumber, " | "));
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
#if !NET35
        [SecuritySafeCritical]
#endif
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "This BinaryWriter constructor will not leave the stream open.")]
        public byte[] Serialize(object data)
        {
            try
            {
                MemoryStream result;
                using (BinaryWriter bw = new BinaryWriter(result = new MemoryStream()))
                {
                    Write(bw, data, true, new SerializationManager(Context, Options, Binder, SurrogateSelector));
                    return result.ToArray();
                }
            }
            finally
            {
                Release();
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
#if !NET35
        [SecuritySafeCritical]
#endif
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "This BinaryReader constructor will not leave the stream open.")]
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "rawData will be checked by MemoryStream constructor.")]
        public object Deserialize(byte[] rawData, int offset = 0)
        {
            using (BinaryReader br = new BinaryReader(offset == 0 ? new MemoryStream(rawData) : new MemoryStream(rawData, offset, rawData.Length - offset)))
            {
                try
                {
                    object result = Read(br, true, new DeserializationManager(Context, Options, Binder, SurrogateSelector));
                    DeserializatonCallback();
                    return result;
                }
                finally
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Serializes the given <paramref name="data"/> into a <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream, into which the data is written. The stream must support writing and will remain open after serialization.</param>
        /// <param name="data">The data that will be written into the stream.</param>
#if !NET35
        [SecuritySafeCritical]
#endif
        public void SerializeToStream(Stream stream, object data)
        {
            try
            {
                BinaryWriter bw = new BinaryWriter(stream);
                Write(bw, data, true, new SerializationManager(Context, Options, Binder, SurrogateSelector));
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Deserializes data beginning at current position of given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream, from which the data is read. The stream must support reading and will remain open after deserialization.</param>
        /// <returns>The deserialized data.</returns>
#if !NET35
        [SecuritySafeCritical]
#endif
        public object DeserializeFromStream(Stream stream)
        {
            try
            {
                BinaryReader br = new BinaryReader(stream);
                object result = Read(br, true, new DeserializationManager(Context, Options, Binder, SurrogateSelector));
                DeserializatonCallback();
                return result;
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Serializes the given <paramref name="data"/> by using the provided <paramref name="writer"/>.
        /// </summary>
        /// <remarks>
        /// <note>This method produces compatible serialized data with <see cref="Serialize">Serialize</see>
        /// and <see cref="SerializeToStream">SerializeToStream</see> methods only when encoding of the writer is UTF-8. Otherwise, you must use <see cref="DeserializeByReader">DeserializeByReader</see> with the same encoding as here.</note>
        /// </remarks>
        /// <param name="writer">The writer that will used to serialize data. The writer will remain opened after serialization.</param>
        /// <param name="data">The data that will be written by the writer.</param>
#if !NET35
        [SecuritySafeCritical]
#endif
        public void SerializeByWriter(BinaryWriter writer, object data)
        {
            try
            {
                Write(writer, data, true, new SerializationManager(Context, Options, Binder, SurrogateSelector));
            }
            finally
            {
                Release();
            }
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
#if !NET35
        [SecuritySafeCritical]
#endif
        public object DeserializeByReader(BinaryReader reader)
        {
            try
            {
                object result = Read(reader, true, new DeserializationManager(Context, Options, Binder, SurrogateSelector));
                DeserializatonCallback();
                return result;
            }
            finally
            {
                Release();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Writing an object. Can be used both at root and object element level. Options are copied so parallel serialization/deserialization processes can be started with different options.
        /// </summary>>
        /// <param name="bw">The writer</param>
        /// <param name="data">The object to serialize</param>
        /// <param name="isRoot"><see langword="true"/>, when <paramref name="data"/> is the root level object.</param>
        /// <param name="manager">The serialization manager</param>
        [SecurityCritical]
        private void Write(BinaryWriter bw, object data, bool isRoot, SerializationManager manager)
        {
            // if an existing id found, returning
            if (!isRoot && manager.WriteId(bw, data))
                return;

            // a.) Natively supported primitive types including string and uintptr (no need for distinct nullables here as they are boxed)
            if (TryWritePrimitive(bw, data))
                return;

            // b.) Surrogate selector for any type
            Type type = data.GetType();
            BinarySerializationOptions options = manager.Options;
            if ((options & BinarySerializationOptions.TryUseSurrogateSelectorForAnyType) != BinarySerializationOptions.None && manager.CanUseSurrogate(type))
            {
                bw.Write((ushort)DataTypes.RecursiveObjectGraph);

                // on root level writing the id after datatype even if the object is value type because the boxed reference can be shared
                if (isRoot)
                {
                    if (manager.WriteId(bw, data))
                        Debug.Fail("Id of recursive object should be unknown on top level.");
                }
                WriteOptions(bw, null, options);
                WriteObjectGraph(bw, data, null, manager);
                return;
            }

            // c.) Natively supported non-primitive single types
            if (TryWriteSimpleNonPrimitive(bw, data))
                return;

            // d.) enum: storing enum type, assembly qualified name and value: still shorter than by BinaryFormatter
            if (data is Enum)
            {
                WriteEnum(bw, data, manager);
                return;
            }

            // e.) Supported collection or compound of collections
            if (TryWriteCollection(bw, data, isRoot, manager))
                return;

            // f.) BinarySerializable
            if (((options & BinarySerializationOptions.IgnoreIBinarySerializable) == BinarySerializationOptions.None) && data is IBinarySerializable binarySerializable)
            {
                bw.Write((ushort)DataTypes.BinarySerializable);

                // on root level writing the id after datatype even if the object is value type because the boxed reference can be shared
                if (isRoot)
                {
                    if (manager.WriteId(bw, data))
                        Debug.Fail("Id of recursive object should be unknown on top level.");
                }

                WriteOptions(bw, null, options);
                manager.WriteType(bw, type);
                WriteBinarySerializable(bw, binarySerializable, options);
                return;
            }

            // g.) Any struct if can serialize
            if ((options & BinarySerializationOptions.CompactSerializationOfStructures) != BinarySerializationOptions.None && type.IsValueType && BinarySerializer.CanSerializeValueType(type, false))
            {
                bw.Write((ushort)DataTypes.RawStruct);
                manager.WriteType(bw, type);
                WriteValueType(bw, data, options);
                return;
            }

            // h.) Recursive serialization: if enabled or surrogate selector supports the type, or when type is serializable
            if (TryWriteRecursively(bw, data, isRoot, manager))
                return;

#pragma warning disable 618, 612
            // i.) Any struct (obsolete but still supported as backward compatibility)
            if ((options & BinarySerializationOptions.ForcedSerializationValueTypesAsFallback) != BinarySerializationOptions.None && type.IsValueType)
            {
                bw.Write((ushort)DataTypes.RawStruct);
                manager.WriteType(bw, type);
                WriteValueType(bw, data, options);
                return;
            }
#pragma warning restore 618, 612

            ThrowNotSupported(options, type);
        }

        [SecurityCritical]
        private bool TryWriteCollection(BinaryWriter bw, object data, bool isRoot, SerializationManager manager)
        {
            Type type = data.GetType();

            if (!IsSupportedCollection(type))
                return false;

            IEnumerable<DataTypes> collectionType = EncodeCollectionType(type, manager.Options, manager);
            if (collectionType == null)
                return false;

            CircularList<DataTypes> collectionTypeList = new CircularList<DataTypes>(collectionType);
            foreach (DataTypes dataType in collectionTypeList)
                bw.Write((ushort)dataType);

            // on root level writing the id after data type if the collection may have recursion because its reference can be re-used
            if (isRoot && CanHaveRecursion(collectionTypeList))
            {
                if (manager.WriteId(bw, data))
                    Debug.Fail("Id of recursive object should be unknown on top level.");
            }

            WriteOptions(bw, collectionTypeList, manager.Options);
            WriteTypeNamesAndRanks(bw, type, manager.Options, manager);
            WriteCollection(bw, collectionTypeList, data, manager);
            return true;
        }

        [SecurityCritical]
        private void WriteCollection(BinaryWriter bw, CircularList<DataTypes> collectionTypeDescriptor, object obj, SerializationManager manager)
        {
            if (collectionTypeDescriptor.Count == 0)
                // should never occur, throwing internal error without resource
                throw new ArgumentException("Type description is invalid", nameof(collectionTypeDescriptor));

            DataTypes collectionDataType = collectionTypeDescriptor[0];
            DataTypes elementDataType = collectionDataType & ~(DataTypes.CollectionTypes | DataTypes.Enum);

            // array
            if ((collectionDataType & DataTypes.CollectionTypes) == DataTypes.Array)
            {
                Array array = (Array)obj;
                // 1. Dimensions
                for (int i = 0; i < array.Rank; i++)
                {
                    Write7BitInt(bw, array.GetLowerBound(i));
                    Write7BitInt(bw, array.GetLength(i));
                }

                // 2. Write elements
                Type elementType = array.GetType().GetElementType();
                // 2.a.) Primitive array
                // ReSharper disable once PossibleNullReferenceException - it is an array
                if (elementType.IsPrimitive)
                {
                    if (!(array is byte[] rawData))
                    {
                        rawData = new byte[Buffer.ByteLength(array)];
                        Buffer.BlockCopy(array, 0, rawData, 0, rawData.Length);
                    }
                    bw.Write(rawData);
                    return;
                }

                // 2.b.) Complex array
                collectionTypeDescriptor.RemoveAt(0);
                WriteCollectionElements(bw, array, collectionTypeDescriptor, elementDataType, elementType, manager);
                return;
            }

            // other collections
            CollectionSerializationInfo serInfo = serializationInfo[collectionDataType & DataTypes.CollectionTypes];
            var enumerable = obj as IEnumerable;
            IEnumerable collection = enumerable ?? new object[] { obj };
            // as object[] for DictionaryEntry and KeyValuePair

            // 1. Write specific properties
            serInfo.WriteSpecificProperties(this, bw, collection, manager);

            // 2. Stack: reversing elements
            if (serInfo.ReverseElements)
                collection = collection.Cast<object>().Reverse();

            // 3. Write elements
            // 3.a.) generic collection with single argument
            if (serInfo.IsGenericCollection)
            {
                Type elementType = collection.GetType().GetGenericArguments()[0];
                collectionTypeDescriptor.RemoveAt(0);
                WriteCollectionElements(bw, collection, collectionTypeDescriptor, elementDataType, elementType, manager);
                return;
            }
            // 3.b.) generic dictionary
            if (serInfo.IsGenericDictionary)
            {
                Type[] argTypes = (enumerable ?? ((object[])collection)[0]).GetType().GetGenericArguments();
                Type keyType = argTypes[0];
                Type valueType = argTypes[1];

                CircularList<DataTypes> valueCollectionDataTypes = GetDictionaryValueTypes(collectionTypeDescriptor);
                collectionTypeDescriptor.RemoveAt(0);
                DataTypes valueDataType = DataTypes.Null;
                if ((valueCollectionDataTypes[0] & DataTypes.CollectionTypes) == DataTypes.Null)
                    valueDataType = valueCollectionDataTypes[0] & ~DataTypes.Enum;
                WriteDictionaryElements(bw, collection, collectionTypeDescriptor, elementDataType, valueCollectionDataTypes, valueDataType, keyType, valueType, manager);
                return;
            }
            // 3.c.) non-generic collection
            if (serInfo.IsNonGenericCollection)
            {
                WriteCollectionElements(bw, collection, null, elementDataType, null, manager);
                return;
            }
            // 3.d.) non-generic dictionary
            if (serInfo.IsNonGenericDictionary)
            {
                DataTypes valueDataType = GetDictionaryValueTypes(collectionTypeDescriptor)[0];
                WriteDictionaryElements(bw, collection, null, elementDataType, null, valueDataType, null, null, manager);
                return;
            }

            // should never occur, throwing internal error without resource
            throw new InvalidOperationException("A supported collection expected here but other type found: " + collection.GetType());
        }

        [SecurityCritical]
        private void WriteCollectionElements(BinaryWriter bw, IEnumerable collection, CircularList<DataTypes> elementCollectionDataTypes, DataTypes elementDataType,
            Type collectionElementType, SerializationManager manager)
        {
            foreach (object element in collection)
                WriteElement(bw, element, elementCollectionDataTypes, elementDataType, collectionElementType, manager);
        }

        [SecurityCritical]
        private void WriteDictionaryElements(BinaryWriter bw, IEnumerable collection, CircularList<DataTypes> keyCollectionDataTypes, DataTypes keyDataType,
            CircularList<DataTypes> valueCollectionDataTypes, DataTypes valueDataType, Type collectionKeyType, Type collectionValueType, SerializationManager manager)
        {
            if (collection is IDictionary dictionary)
            {
                foreach (DictionaryEntry element in dictionary)
                {
                    WriteElement(bw, element.Key, keyCollectionDataTypes, keyDataType, collectionKeyType, manager);
                    WriteElement(bw, element.Value, valueCollectionDataTypes, valueDataType, collectionValueType, manager);
                }
            }
            // if cannot be cast to a non-generic dictionary, Key and Value properties can be accessed only via reflection
            else
            {
                foreach (object element in collection)
                {
                    WriteElement(bw, Reflector.GetProperty(element, "Key"), keyCollectionDataTypes, keyDataType, collectionKeyType, manager);
                    WriteElement(bw, Reflector.GetProperty(element, "Value"), valueCollectionDataTypes, valueDataType, collectionValueType, manager);
                }
            }
        }

        /// <summary>
        /// Writes a collection element
        /// </summary>
        /// <param name="bw">Binary writer</param>
        /// <param name="element">A collection element instance (can be null)</param>
        /// <param name="elementCollectionDataTypes">Data types of embedded elements. Needed in case of arrays and generic collections where embedded types are handled.</param>
        /// <param name="elementDataType">A base data type that is valid for all elements in the collection. <see cref="DataTypes.Null"/> means that element is a nested collection.</param>
        /// <param name="collectionElementType">Needed if <paramref name="elementDataType"/> is <see cref="DataTypes.BinarySerializable"/> or <see cref="DataTypes.RecursiveObjectGraph"/>.
        /// Contains the actual generic type parameter or array base type from which <see cref="IBinarySerializable"/> or the type of the recursively serialized object is assignable.</param>
        /// <param name="manager">An <see cref="SerializationManager"/> instance.</param>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Simple switch with many cases")]
        private void WriteElement(BinaryWriter bw, object element, CircularList<DataTypes> elementCollectionDataTypes, DataTypes elementDataType,
            Type collectionElementType, SerializationManager manager)
        {
            switch (elementDataType)
            {
                case DataTypes.Null:
                    // Null element type means that element is a nested collection type: recursion.
                    // Writing id except for value types (KeyValuePair, DictionaryEntry) - for nullables IsNotNull was written in default
                    if (!collectionElementType.IsValueType || collectionElementType.IsNullable())
                    {
                        if (manager.WriteId(bw, element))
                            break;
                    }

                    Debug.Assert(element != null, "When element is null, WriteId should return true");
                    WriteCollection(bw, new CircularList<DataTypes>(elementCollectionDataTypes), element, manager);
                    break;
                case DataTypes.Bool:
                    bw.Write((bool)element);
                    break;
                case DataTypes.Int8:
                    bw.Write((sbyte)element);
                    break;
                case DataTypes.UInt8:
                    bw.Write((byte)element);
                    break;
                case DataTypes.Int16:
                    bw.Write((short)element);
                    break;
                case DataTypes.UInt16:
                    bw.Write((ushort)element);
                    break;
                case DataTypes.Int32:
                    bw.Write((int)element);
                    break;
                case DataTypes.UInt32:
                    bw.Write((uint)element);
                    break;
                case DataTypes.Int64:
                    bw.Write((long)element);
                    break;
                case DataTypes.UInt64:
                    bw.Write((ulong)element);
                    break;
                case DataTypes.Char:
                    bw.Write((ushort)(char)element);
                    break;
                case DataTypes.String:
                    if (manager.WriteId(bw, element))
                        break;
                    Debug.Assert(element != null, "When element is null, WriteId should return true");
                    bw.Write((string)element);
                    break;
                case DataTypes.Single:
                    bw.Write((float)element);
                    break;
                case DataTypes.Double:
                    bw.Write((double)element);
                    break;
                case DataTypes.Decimal:
                    bw.Write((decimal)element);
                    break;
                case DataTypes.DateTime:
                    WriteDateTime(bw, (DateTime)element);
                    break;
                case DataTypes.DBNull:
                    manager.WriteId(bw, element);
                    break;
                case DataTypes.IntPtr:
                    bw.Write(((IntPtr)element).ToInt64());
                    break;
                case DataTypes.UIntPtr:
                    bw.Write(((UIntPtr)element).ToUInt64());
                    break;
                case DataTypes.Version:
                    if (manager.WriteId(bw, element))
                        break;
                    WriteVersion(bw, (Version)element);
                    break;
                case DataTypes.Guid:
                    bw.Write(((Guid)element).ToByteArray());
                    break;
                case DataTypes.TimeSpan:
                    bw.Write(((TimeSpan)element).Ticks);
                    break;
                case DataTypes.DateTimeOffset:
                    WriteDateTimeOffset(bw, (DateTimeOffset)element);
                    break;
                case DataTypes.Uri:
                    if (manager.WriteId(bw, element))
                        break;
                    Debug.Assert(element != null, "When element is null, WriteId should return true");
                    WriteUri(bw, (Uri)element);
                    break;
                case DataTypes.BitArray:
                    if (manager.WriteId(bw, element))
                        break;
                    Debug.Assert(element != null, "When element is null, WriteId should return true");
                    WriteBitArray(bw, (BitArray)element);
                    break;
                case DataTypes.BitVector32:
                    bw.Write(((BitVector32)element).Data);
                    break;
                case DataTypes.BitVector32Section:
                    WriteSection(bw, (BitVector32.Section)element);
                    break;
                case DataTypes.StringBuilder:
                    if (manager.WriteId(bw, element))
                        break;
                    Debug.Assert(element != null, "When element is null, WriteId should return true");
                    WriteStringBuilder(bw, (StringBuilder)element);
                    break;

                case DataTypes.BinarySerializable:
                    {
                        // 1. instance id for classes or when element is defined as interface in the collection (for nullables IsNotNull was already written in default case)
                        if ((!collectionElementType.IsValueType) && manager.WriteId(bw, element))
                            break;

                        Debug.Assert(element != null, "When element is null, WriteId should return true");
                        Type elementType = element.GetType();

                        // 2. Serialize (1: qualify -> is element type, 2: different type -> store type, 3: serialize)
                        bool qualifyAllElements = collectionElementType.CanBeDerived();
                        bool typeNeeded = qualifyAllElements && elementType != collectionElementType;

                        // is type the same as collection element type
                        if (qualifyAllElements)
                            bw.Write(!typeNeeded);

                        if (typeNeeded)
                            manager.WriteType(bw, elementType);
                        WriteBinarySerializable(bw, (IBinarySerializable)element, manager.Options);
                    }
                    break;
                case DataTypes.RecursiveObjectGraph:
                    {
                        // When element types may differ, writing element with data type. This prevents the following errors:
                        // - Writing array element as a graph - new IList<int>[] { new int[] {1} }
                        // - Writing primitive/enum/other supported element as a graph - new ValueType[] { 1, ConsoleColor.Black }
                        // - Writing compressible struct or IBinarySerializable as a graph - new IAnything[] { new BinarySerializable(), new MyStruct() }
                        if (collectionElementType.CanBeDerived())
                        {
                            Write(bw, element, false, manager);
                            break;
                        }

                        // 1. instance id for classes or when element is defined as interface in the collection (for nullables IsNotNull was already written in default case)
                        if (!collectionElementType.IsValueType && manager.WriteId(bw, element))
                            break;

                        Debug.Assert(element != null, "When element is null, WriteId should return true");

                        // 2. Serialize
                        WriteObjectGraph(bw, element, collectionElementType, manager);
                    }
                    break;
                case DataTypes.RawStruct:
                    WriteValueType(bw, element, manager.Options);
                    break;
                case DataTypes.Object:
                    Write(bw, element, false, manager);
                    break;
                default:
                    if ((elementDataType & DataTypes.Nullable) == DataTypes.Nullable)
                    {
                        // When boxed, nullable elements are either a null reference or a non-nullable instance in the object.
                        // Here writing IsNotNull instead of id; othwerise, nullables would get an id while non-nullables would not.
                        bw.Write(element != null);
                        if (element != null)
                            WriteElement(bw, element, elementCollectionDataTypes, elementDataType & ~DataTypes.Nullable, collectionElementType, manager);
                        break;
                    }

                    // should never occur, throwing internal error without resource
                    throw new InvalidOperationException("Can not serialize elementType " + ToString(elementDataType));
            }
        }

        private void WriteBinarySerializable(BinaryWriter bw, IBinarySerializable instance, BinarySerializationOptions options)
        {
            OnSerializing(instance, options);
            byte[] rawData = instance.Serialize(options);
            Write7BitInt(bw, rawData.Length);
            bw.Write(rawData);
            OnSerialized(instance, options);
        }

        [SecurityCritical]
        private void WriteValueType(BinaryWriter bw, object data, BinarySerializationOptions options)
        {
            OnSerializing(data, options);
            byte[] rawData = BinarySerializer.SerializeValueType((ValueType)data);
            Write7BitInt(bw, rawData.Length);
            bw.Write(rawData);
            OnSerialized(data, options);
        }

        [SecurityCritical]
        private bool TryWriteRecursively(BinaryWriter bw, object data, bool isRoot, SerializationManager manager)
        {
            Type type = data.GetType();
            if ((manager.Options & BinarySerializationOptions.RecursiveSerializationAsFallback) == BinarySerializationOptions.None && !manager.CanUseSurrogate(type) && (!type.IsSerializable || type.IsArray))
                return false;

            bw.Write((ushort)DataTypes.RecursiveObjectGraph);

            // on root level writing the id after data type even if the object is value type because the boxed reference can be shared
            if (isRoot)
            {
                if (manager.WriteId(bw, data))
                    Debug.Fail("Id of recursive object should be unknown on top level.");
            }

            WriteOptions(bw, null, manager.Options);
            WriteObjectGraph(bw, data, null, manager);
            return true;
        }

        /// <summary>
        /// Serializes an object graph.
        /// </summary>
        /// <param name="bw">Writer</param>
        /// <param name="data">The object to serialize</param>
        /// <param name="collectionElementType">Element type of collection or null if not in collection</param>
        /// <param name="manager">Serialization Manager.</param>
        [SecurityCritical]
        private void WriteObjectGraph(BinaryWriter bw, object data, Type collectionElementType, SerializationManager manager)
        {
            // Common order: 1: not in a collection -> store type, 2: serialize
            BinarySerializationOptions options = manager.Options;
            OnSerializing(data, options);

            Type type = data.GetType();
            if (manager.TryGetSurrogate(type, out ISerializationSurrogate surrogate, out var _) || ((options & BinarySerializationOptions.IgnoreISerializable) == BinarySerializationOptions.None && data is ISerializable))
                WriteCustomObjectGraph(bw, data, collectionElementType, options, manager, surrogate);
            else
            {
                // type
                if (collectionElementType == null)
                {
                    if ((options & BinarySerializationOptions.RecursiveSerializationAsFallback) == BinarySerializationOptions.None && !type.IsSerializable)
                        ThrowNotSupported(options, type);
                    manager.WriteType(bw, type);
                }

                WriteDefaultObjectGraph(bw, data, manager);
            }

            OnSerialized(data, options);
        }

        [SecurityCritical]
        private void WriteDefaultObjectGraph(BinaryWriter bw, object data, SerializationManager manager)
        {
            // true for IsDefault object graph
            bw.Write(true);
            Type type = data.GetType();
            Debug.Assert(!type.IsArray, "Array cannot be serialized as object graph");

            // iterating through self and base types
            // ReSharper disable once PossibleNullReferenceException - data is an object in all cases
            for (Type t = type; t != Reflector.ObjectType; t = t.BaseType)
            {
                // writing fields of current level
                FieldInfo[] fields = BinarySerializer.GetSerializableFields(t);

                if (fields.Length != 0 || t == type)
                {
                    // ReSharper disable once PossibleNullReferenceException - type is never null
                    // writing name of base type
                    if (t != type)
                        bw.Write(t.Name);

                    // writing the fields
                    Write7BitInt(bw, fields.Length);
                    foreach (FieldInfo field in fields)
                    {
                        bw.Write(field.Name);
                        Type fieldType = field.FieldType;
                        object fieldValue = FieldAccessor.GetAccessor(field).Get(data);
                        if (fieldValue != null && fieldType.IsEnum)
                            fieldValue = Convert.ChangeType(fieldValue, Enum.GetUnderlyingType(fieldType), CultureInfo.InvariantCulture);
                        Write(bw, fieldValue, false, manager);
                    }
                }
            }

            // marking end of hierarchy
            bw.Write(String.Empty);
        }

        [SecurityCritical]
        private void WriteCustomObjectGraph(BinaryWriter bw, object data, Type collectionElementType, BinarySerializationOptions options, SerializationManager manager, ISerializationSurrogate surrogate)
        {
            // Common order: 1: not in a collection -> store type, 2: serialize

            Type type = data.GetType();
            SerializationInfo si = new SerializationInfo(type, new FormatterConverter());

            if (surrogate != null)
                surrogate.GetObjectData(data, si, Context);
            else
            {
                if ((options & BinarySerializationOptions.RecursiveSerializationAsFallback) == BinarySerializationOptions.None && !type.IsSerializable)
                    ThrowNotSupported(options, type);
                ((ISerializable)data).GetObjectData(si, Context);
            }

            bool typeChanged = si.AssemblyName != type.Assembly.FullName || si.FullTypeName != type.FullName;
            if (typeChanged)
                type = Type.GetType(si.FullTypeName + ", " + si.AssemblyName);

            // 1. type if needed
            if (collectionElementType == null)
                manager.WriteType(bw, type);

            // 2. Serialization part.
            // a.) writing false for not default object graph method
            bw.Write(false);

            // b.) Here we can sign if type has changed while element types are the same in a collection (sealed class or struct element type)
            if (collectionElementType != null)
            {
                bw.Write(typeChanged);
                if (typeChanged)
                    manager.WriteType(bw, type);
            }

            // c.) writing members
            Write7BitInt(bw, si.MemberCount);
            foreach (SerializationEntry entry in si)
            {
                // name
                bw.Write(entry.Name);

                // value
                Write(bw, entry.Value, false, manager);

                // type
                bool typeMatch = entry.Value == null && entry.ObjectType == Reflector.ObjectType
                        || entry.Value != null && entry.Value.GetType() == entry.ObjectType;
                bw.Write(typeMatch);
                if (!typeMatch)
                    manager.WriteType(bw, entry.ObjectType);
            }
        }

        private void OnSerializing(object obj, BinarySerializationOptions options)
        {
            if ((options & BinarySerializationOptions.IgnoreSerializationMethods) != BinarySerializationOptions.None)
                return;

            ExecuteMethods(obj, GetMethodsWithAttribute(typeof(OnSerializingAttribute), obj.GetType()));
        }

        private void OnSerialized(object obj, BinarySerializationOptions options)
        {
            if ((options & BinarySerializationOptions.IgnoreSerializationMethods) != BinarySerializationOptions.None)
                return;

            ExecuteMethods(obj, GetMethodsWithAttribute(typeof(OnSerializedAttribute), obj.GetType()));
        }

        private void OnDeserializing(object obj)
        {
            // as it is in description, using current Options instead of the one used at serialization time
            if ((Options & BinarySerializationOptions.IgnoreSerializationMethods) != BinarySerializationOptions.None)
                return;

            ExecuteMethods(obj, GetMethodsWithAttribute(typeof(OnDeserializingAttribute), obj.GetType()));
        }

        private void OnDeserialized(object obj)
        {
            // as it is in description, using current Options instead of the one used at serialization time
            if ((Options & BinarySerializationOptions.IgnoreSerializationMethods) != BinarySerializationOptions.None)
                return;

            ExecuteMethods(obj, GetMethodsWithAttribute(typeof(OnDeserializedAttribute), obj.GetType()));
            RegisterDeserializedObject(obj as IDeserializationCallback);
        }

        /// <summary>
        /// Registers object to detect circular reference.
        /// Must be called from inside of try-finally to remove lock in finally if neccessary.
        /// </summary>
        private void RegisterDeserializedObject(IDeserializationCallback obj)
        {
            if (obj == null)
                return;

            // putting lock when deserialization contains IDeserializationCallback instances
            if (deserRegObjects == null)
            {
                //Monitor.Enter(syncRootDeserialize);
                deserRegObjects = new List<IDeserializationCallback>();
            }

            deserRegObjects.Add(obj);
        }

        private void DeserializatonCallback()
        {
            if (deserRegObjects == null)
                return;

            // reverse walking to be compatible with BinaryFormatter
            for (int i = deserRegObjects.Count - 1; i >= 0; i--)
            {
                deserRegObjects[i].OnDeserialization(this);
            }

            deserRegObjects = null;
            //Monitor.Exit(syncRootDeserialize);
        }

        private void Release()
        {
            //// closing opened serialization lock
            //if (serializationLevel != 0)
            //{
            //    serializationLevel = 0;
            //    serObjects = null;
            //    Monitor.Exit(syncRootSerialize);
            //}

            //// closing opened deserialization lock
            //if (deserRegObjects != null)
            //{
            deserRegObjects = null;
            //    Monitor.Exit(syncRootDeserialize);
            //}
        }

        private void ExecuteMethods(object obj, IEnumerable<MethodInfo> methods)
        {
            if (methods == null)
                return;

            foreach (MethodInfo method in methods)
                Reflector.InvokeMethod(obj, method, Context);
        }

        /// <summary>
        /// Deserializes an object from the stream.
        /// </summary>
        /// <param name="br">The reader</param>
        /// <param name="isRoot"><see langword="true"/>, when the object to deserialize is the root-level object</param>
        /// <param name="manager">The manager used for deserialization.</param>
        /// <returns>The deserialized object</returns>
        [SecurityCritical]
        private object Read(BinaryReader br, bool isRoot, DeserializationManager manager)
        {
            if (!isRoot && manager.TryGetCachedObject(br, out object result))
                return result;

            DataTypes dataType = (DataTypes)br.ReadUInt16();

            // 1.) null value
            if (dataType == DataTypes.Null)
                return null;

            // 2.) other supported non-collection type
            if ((dataType & DataTypes.CollectionTypes) == DataTypes.Null)
                return ReadObject(br, isRoot, !isRoot, dataType, null, manager, false);

            // 3.) compound collection type
            DataTypeDescriptor descriptor = new DataTypeDescriptor(null, dataType, br);

            // on root level id is written only after data type and only when the collection can have recursion except DictionaryEntry/KeyValuePair
            bool addToCache = !isRoot;
            if (isRoot && descriptor.CanHaveRecursion)
            {
                addToCache = true;
                if (manager.TryGetCachedObject(br, out result))
                {
                    Debug.Fail("Root level object is not expected in the cache");
                    return result;
                }
            }

            descriptor.TryReadOptions(br);
            descriptor.DecodeType(br, manager);

            // 3/a.) array
            if (descriptor.IsArray)
                return CreateArray(br, addToCache, descriptor, manager);

            // 3/b.) non-array collection or key-value
            return CreateCollection(br, addToCache, descriptor, manager);
        }

        /// <summary>
        /// Creates and populates array
        /// </summary>
        [SecurityCritical]
        private object CreateArray(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor, DeserializationManager manager)
        {
            // getting whether the current instance is in cache
            if (descriptor.ParentDescriptor != null)
            {
                if (manager.TryGetCachedObject(br, out object cachedResult))
                    return cachedResult;
            }

            // creating the array
            int rank = descriptor.Type.GetArrayRank();
            int[] lengths = new int[rank];
            int[] lowerBounds = new int[rank];
            for (int i = 0; i < rank; i++)
            {
                lowerBounds[i] = Read7BitInt(br);
                lengths[i] = Read7BitInt(br);
            }
            Array result = Array.CreateInstance(descriptor.ElementType, lengths, lowerBounds);
            if (addToCache)
                manager.AddObjectToCache(result);

            // primitive array
            if (descriptor.ElementType.IsPrimitive)
            {
                int length = Buffer.ByteLength(result);
                Buffer.BlockCopy(br.ReadBytes(length), 0, result, 0, length);
            }

            // 1D array
            else if (lengths.Length == 1)
            {
                int offset = lowerBounds[0];
                for (int i = 0; i < result.Length; i++)
                {
                    object value = ReadElement(br, descriptor, manager, false);
                    result.SetValue(value, i + offset);
                }
            }

            // multidimensional array
            else
            {
                var arrayIndexer = new ArrayIndexer(lengths, lowerBounds);
                while (arrayIndexer.MoveNext())
                {
                    object value = ReadElement(br, descriptor, manager, false);
                    result.SetValue(value, arrayIndexer.Current);
                }
            }

            return result;
        }

        /// <summary>
        /// Creates and populates a collection
        /// </summary>
        [SecurityCritical]
        private object CreateCollection(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor, DeserializationManager manager)
        {
            if (!descriptor.IsSingleElement && !Reflector.IEnumerableType.IsAssignableFrom(descriptor.Type))
                throw new InvalidOperationException(Res.BinarySerializationIEnumerableExpected(descriptor.Type));

            // getting whether the current instance is in cache
            if (descriptor.ParentDescriptor != null && (!descriptor.Type.IsValueType || descriptor.Type.IsNullable()))
            {
                if (manager.TryGetCachedObject(br, out object cachedResult))
                    return cachedResult;
            }

            // KeyValuePair, dictionary entry
            if (descriptor.IsSingleElement)
            {
                object result = Reflector.CreateInstance(descriptor.GetTypeToCreate());
                if (addToCache)
                    manager.AddObjectToCache(result);
                object key = ReadElement(br, descriptor, manager, false);
                object value = descriptor.IsDictionary ? ReadElement(br, descriptor, manager, true) : null;
                Reflector.SetField(result, descriptor.GetFieldNameToSet(false), key);
                Reflector.SetField(result, descriptor.GetFieldNameToSet(true), value);
                return result;
            }

            CollectionSerializationInfo serInfo = serializationInfo[descriptor.CollectionDataType];
            IEnumerable collection = (IEnumerable)serInfo.InitializeCollection(this, br, addToCache, descriptor, manager, out int count);

            MethodInfo addMethod = serInfo.SpecificAddMethod != null ? collection.GetType().GetMethod(serInfo.SpecificAddMethod) : null;

            for (int i = 0; i < count; i++)
            {
                object element = ReadElement(br, descriptor, manager, false);
                object value = descriptor.IsDictionary ? ReadElement(br, descriptor, manager, true) : null;

                if (descriptor.IsDictionary)
                {
                    if (addMethod != null)
                    {
                        Reflector.InvokeMethod(collection, addMethod, element, value);
                        continue;
                    }

#if NET35
                    if (value != null || !descriptor.IsGenericDictionary)
#endif
                    {
                        ((IDictionary)collection).Add(element, value);
                        continue;
                    }
#if NET35

                    // generic dictionary with null value: calling generic Add because non-generic one may fail under .NET 4
                    Reflector.InvokeMethod(collection, nameof(IDictionary<_,_>.Add), element, null);
                    continue;
#endif

                }

                if (addMethod != null)
                {
                    Reflector.InvokeMethod(collection, addMethod, element);
                    continue;
                }

                collection.TryAdd(element, false);
            }

            return descriptor.IsReadOnly ? descriptor.GetAsReadOnly(collection) : collection;
        }

        [SecurityCritical]
        private object ReadElement(BinaryReader br, DataTypeDescriptor collectionDescriptor, DeserializationManager manager, bool isTValue)
        {
            DataTypes elementDataType = collectionDescriptor.GetElementDataType(isTValue);

            // single element
            if (elementDataType != DataTypes.Null)
                return ReadObject(br, false, !collectionDescriptor.GetElementType(isTValue).IsValueType, elementDataType, collectionDescriptor, manager, isTValue);

            DataTypeDescriptor elementDescriptor = collectionDescriptor.GetElementDescriptor(isTValue);
            // nested array
            if (elementDescriptor.IsArray)
                return CreateArray(br, true, elementDescriptor, manager);

            // other nested collection
            return CreateCollection(br, !elementDescriptor.Type.IsValueType || elementDescriptor.Type.IsNullable(), elementDescriptor, manager);
        }

        /// <summary>
        /// Reads a non-collection object from the stream.
        /// </summary>
        /// <param name="br">The reader</param>
        /// <param name="isRoot"><see langword="true"/>, when the object to deserialize is the root-level object</param>
        /// <param name="addToCache">When <see langword="true"/>, the result must be added to the ID cache. Otherwise, only reference types in a collection might be added to cache.</param>
        /// <param name="dataType">The already read data type of the object.</param>
        /// <param name="collectionDescriptor">When a collection element is deserialized, the collection descriptor.</param>
        /// <param name="manager">The manager used for deserialization.</param>
        /// <param name="isTValue"><see langword="true"/>, when element to deserialize is the value in a dictionary collection.</param>
        /// <returns>The deserialized object.</returns>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Long but very straightforward switch")]
        private object ReadObject(BinaryReader br, bool isRoot, bool addToCache, DataTypes dataType, DataTypeDescriptor collectionDescriptor, DeserializationManager manager, bool isTValue)
        {
            bool is7BitEncoded = (dataType & DataTypes.Store7BitEncoded) != DataTypes.Null;

            // nullable type
            if ((dataType & DataTypes.Nullable) == DataTypes.Nullable)
            {
                // no need to check collection descriptor because there is no nullable type on root level so checking for null in any case
                if (!br.ReadBoolean())
                    return null;
            }

            object result = null;
            try
            {
                object cachedResult;
                switch (dataType & ~DataTypes.Store7BitEncoded & ~DataTypes.Nullable)
                {
                    case DataTypes.Bool:
                        return result = br.ReadBoolean();
                    case DataTypes.UInt8:
                        return result = br.ReadByte();
                    case DataTypes.Int8:
                        return result = br.ReadSByte();
                    case DataTypes.Int16:
                        return result = is7BitEncoded ? (short)Read7BitInt(br) : br.ReadInt16();
                    case DataTypes.UInt16:
                        return result = is7BitEncoded ? (ushort)Read7BitInt(br) : br.ReadUInt16();
                    case DataTypes.Int32:
                        return result = is7BitEncoded ? Read7BitInt(br) : br.ReadInt32();
                    case DataTypes.UInt32:
                        return result = is7BitEncoded ? (uint)Read7BitInt(br) : br.ReadUInt32();
                    case DataTypes.Int64:
                        return result = is7BitEncoded ? Read7BitLong(br) : br.ReadInt64();
                    case DataTypes.UInt64:
                        return result = is7BitEncoded ? (ulong)Read7BitLong(br) : br.ReadUInt64();
                    case DataTypes.Char:
                        return result = is7BitEncoded ? (char)Read7BitInt(br) : (char)br.ReadUInt16();
                    case DataTypes.String:
                        if (collectionDescriptor != null)
                        {
                            Debug.Assert(addToCache, "Reference element types of collections should be cached");
                            if (manager.TryGetCachedObject(br, out cachedResult))
                                return cachedResult;
                        }

                        return result = br.ReadString();
                    case DataTypes.Single:
                        return result = br.ReadSingle();
                    case DataTypes.Double:
                        return result = br.ReadDouble();
                    case DataTypes.Decimal:
                        return result = br.ReadDecimal();
                    case DataTypes.DateTime:
                        DateTimeKind kind = (DateTimeKind)br.ReadByte();
                        return result = new DateTime(br.ReadInt64(), kind);
                    case DataTypes.TimeSpan:
                        return result = new TimeSpan(br.ReadInt64());
                    case DataTypes.DateTimeOffset:
                        result = new DateTimeOffset();
                        Reflector.SetField(result, "m_dateTime", new DateTime(br.ReadInt64()));
                        Reflector.SetField(result, "m_offsetMinutes", br.ReadInt16());
                        return result;
                    case DataTypes.DBNull:
                        if (collectionDescriptor != null)
                        {
                            Debug.Assert(addToCache, "Reference element types of collections should be cached");
                            if (manager.TryGetCachedObject(br, out cachedResult))
                                return cachedResult;
                            Debug.Fail("DBNull singleton instance must be in the cache");
                        }

                        Debug.Assert(!addToCache, "DBNull should be returned without cache only when not in collection.");
                        return DBNull.Value;
                    case DataTypes.IntPtr:
                        return result = new IntPtr(br.ReadInt64());
                    case DataTypes.UIntPtr:
                        return result = new UIntPtr(br.ReadUInt64());
                    case DataTypes.Object:
                        // object - returning object instance on root level, otherwise, doing recursion because can mean any type as an element type
                        if (collectionDescriptor == null)
                            return result = new object();
                        // result is not set here - when caching is needed, will be done in the recursion
                        return Read(br, false, manager);
                    case DataTypes.Version:
                        if (collectionDescriptor != null)
                        {
                            Debug.Assert(addToCache, "Reference element types of collections should be cached");
                            if (manager.TryGetCachedObject(br, out cachedResult))
                                return cachedResult;
                        }

                        int major = br.ReadInt32();
                        int minor = br.ReadInt32();
                        int build = br.ReadInt32();
                        int revision = br.ReadInt32();
                        if (revision == -1)
                            return result = new Version(major, minor);
                        if (build == -1)
                            return result = new Version(major, minor, build);
                        return result = new Version(major, minor, build, revision);
                    case DataTypes.Guid:
                        return result = new Guid(br.ReadBytes(16));
                    case DataTypes.Uri:
                        if (collectionDescriptor != null)
                        {
                            Debug.Assert(addToCache, "Reference element types of collections should be cached");
                            if (manager.TryGetCachedObject(br, out cachedResult))
                                return cachedResult;
                        }

                        bool isAbsolute = br.ReadBoolean();
                        return result = new Uri(br.ReadString(), isAbsolute ? UriKind.Absolute : UriKind.Relative);
                    case DataTypes.BitArray:
                        if (collectionDescriptor != null)
                        {
                            Debug.Assert(addToCache, "Reference element types of collections should be cached");
                            if (manager.TryGetCachedObject(br, out cachedResult))
                                return cachedResult;
                        }

                        int length = Read7BitInt(br);
                        result = new BitArray(length);
                        if (length > 0)
                        {
                            int[] value = new int[(length + 31) >> 5]; // ">> 5" is "/ 32" but faster
                            for (int i = 0; i < value.Length; i++)
                            {
                                value[i] = br.ReadInt32();
                            }

                            Reflector.SetField(result, "m_array", value);
                        }
                        return result;
                    case DataTypes.BitVector32:
                        return result = new BitVector32(br.ReadInt32());
                    case DataTypes.BitVector32Section:
                        return result = Reflector.CreateInstance(typeof(BitVector32.Section), br.ReadInt16(), br.ReadInt16());
                    case DataTypes.StringBuilder:
                        if (collectionDescriptor != null)
                        {
                            Debug.Assert(addToCache, "Reference element types of collections should be cached");
                            if (manager.TryGetCachedObject(br, out cachedResult))
                                return cachedResult;
                        }

                        int capacity = Read7BitInt(br);
                        return result = new StringBuilder(br.ReadString(), capacity);

                    // IBinarySerializable
                    case DataTypes.BinarySerializable:
                        return ReadBinarySerializable(br, isRoot, addToCache, collectionDescriptor, manager, isTValue);

                    // recursive graph
                    case DataTypes.RecursiveObjectGraph:
                        return ReadObjectGraph(br, isRoot, addToCache, collectionDescriptor, manager, isTValue);

                    // raw structure
                    case DataTypes.RawStruct:
                        Type structType = collectionDescriptor == null
                            ? manager.ReadType(br)
                            : collectionDescriptor.GetElementType(isTValue);
                        byte[] rawData = br.ReadBytes(Read7BitInt(br));
                        result = BinarySerializer.DeserializeValueType(structType, rawData);
                        OnDeserializing(result);
                        OnDeserialized(result);
                        return result;

                    default:
                        // enum
                        if ((dataType & DataTypes.Enum) == DataTypes.Enum)
                            return result = ReadEnum(br, dataType, collectionDescriptor, manager, isTValue);

                        throw new InvalidOperationException(Res.BinarySerializationCannotDeserializeObject(ToString(dataType)));
                }
            }
            finally
            {
                if (addToCache && result != null)
                    manager.AddObjectToCache(result);
            }
        }

        [SecurityCritical]
        private object ReadBinarySerializable(BinaryReader br, bool isRoot, bool addToCache, DataTypeDescriptor collectionDescriptor, DeserializationManager manager, bool isTValue)
        {
            object cachedResult;

            // occurs on root level: object id is stored only after data type
            if (isRoot)
            {
                if (manager.TryGetCachedObject(br, out cachedResult))
                {
                    Debug.Fail("Root level object is not expected in the cache");
                    return cachedResult;
                }
            }

            BinarySerializationOptions origOptions = collectionDescriptor?.SerializationOptions ?? ReadOptions(br);

            // checking instance id
            Type elementType = null;
            if (collectionDescriptor != null &&
                (!(elementType = collectionDescriptor.GetElementType(isTValue)).IsValueType))
            {
                if (manager.TryGetCachedObject(br, out cachedResult))
                    return cachedResult;
            }

            Type objType;
            if (collectionDescriptor == null)
                objType = manager.ReadType(br);
            else
            {
                // Common order: 1: qualify -> is element type, 2: different type -> read type, 3: deserialize
                // 1. If elements should be qualified and element is not the same as collection element type
                if (collectionDescriptor.AreAllElementsQualified(isTValue) && !br.ReadBoolean())
                {
                    // 2. then read type
                    objType = manager.ReadType(br);
                }
                else
                    objType = elementType ?? collectionDescriptor.GetElementType(isTValue);
            }

            // 3. deserialize (result is not set here - object will be cached immediately after creation so circular references will be found in time)
            return DoReadBinarySerializable(br, addToCache || isRoot, objType, origOptions, manager);
        }

        [SecurityCritical]
        private object DoReadBinarySerializable(BinaryReader br, bool addToCache, Type type, BinarySerializationOptions origOptions, DeserializationManager manager)
        {
            byte[] serData = br.ReadBytes(Read7BitInt(br));

            if (!Reflector.TryCreateEmptyObject(type, false, true, out object result))
                throw new SerializationException(Res.BinarySerializationCannotCreateUninitializedObject(type));

            if (addToCache)
                manager.AddObjectToCache(result);
            OnDeserializing(result);

            // Looking for a serializer constructor
            ConstructorInfo ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(BinarySerializationOptions), typeof(byte[]) }, null);
            if (ctor != null)
                Reflector.InvokeCtor(result, ctor, origOptions, serData);
            else
            {
                // Looking for parameterless constructor
                ctor = type.GetDefaultConstructor();
                if (ctor != null)
                    Reflector.InvokeCtor(result, ctor);

                ((IBinarySerializable)result).Deserialize(origOptions, serData);
            }

            OnDeserialized(result);
            return result;
        }

        [SecurityCritical]
        private object ReadObjectGraph(BinaryReader br, bool isRoot, bool addToCache, DataTypeDescriptor collectionDescriptor, DeserializationManager manager, bool isTValue)
        {
            // When element types may differ, reading element with data type
            if (collectionDescriptor != null && collectionDescriptor.AreAllElementsQualified(isTValue))
                return Read(br, false, manager);

            // occurs on root level: object id is stored only after data type
            object cachedResult;
            if (isRoot)
            {
                if (manager.TryGetCachedObject(br, out cachedResult))
                {
                    Debug.Fail("Root level object is not expected in the cache");
                    return cachedResult;
                }
            }

            // TODO: options is not used here anymore
            if (collectionDescriptor?.SerializationOptions == null)
                ReadOptions(br); // just reading it to pass through but not used

            // checking instance id
            Type elementType = null;
            if (collectionDescriptor != null &&
                (!(elementType = collectionDescriptor.GetElementType(isTValue)).IsValueType))
            {
                if (manager.TryGetCachedObject(br, out cachedResult))
                    return cachedResult;
            }

            // in collection, type is already known, otherwise, reading it
            Type objType = collectionDescriptor == null
                ? manager.ReadType(br)
                : (elementType ?? collectionDescriptor.GetElementType(isTValue));

            // 2. deserialize (result is not set here - object will be cached immediately after creation so circular references will be found in time)
            return DoReadObjectGraph(br, addToCache || isRoot, objType, manager, collectionDescriptor != null);
        }

        /// <summary>
        /// Deserializing object graph with options that was used on serialization.
        /// </summary>
        [SecurityCritical]
        private object DoReadObjectGraph(BinaryReader br, bool addToCache, Type type, DeserializationManager manager, bool refineType)
        {
            // a.) Graph method
            bool isDefaultObjectGraph = br.ReadBoolean();

            // b.) Possible type change
            if (!isDefaultObjectGraph && refineType && br.ReadBoolean())
                type = manager.ReadType(br);

            // c.) Reading members
            if (!Reflector.TryCreateEmptyObject(type, false, true, out object result))
                throw new SerializationException(Res.BinarySerializationCannotCreateUninitializedObject(type));
            int id = 0;
            if (addToCache)
                manager.AddObjectToCache(result, out id);
            OnDeserializing(result);

            bool useSurrogate = manager.TryGetSurrogate(type, out ISerializationSurrogate surrogate, out ISurrogateSelector selector);
            bool isISerializable = result is ISerializable;

            // default graph was serialized
            if (isDefaultObjectGraph)
            {
                if (!isISerializable && !useSurrogate)
                {
                    // default graph should be deserialized
                    ReadDefaultObjectGraph(br, result, manager);
                }
                else
                {
                    // the default graph should be deserialized either as ISerializable or by a surrogate
                    ReadDefaultObjectGraphAsCustom(br, result, manager, surrogate, selector);
                }
            }
            // custom graph was serialized
            else if (isISerializable || useSurrogate)
            {
                // custom graph should be deserialized
                ReadCustomObjectGraph(br, result, manager, surrogate, selector);
            }
            else
            {
                // the custom graph should be deserialized as a default object by setting fields
                ReadCustomObjectGraphAsDefault(br, result, manager);
            }

            OnDeserialized(result);

            // if type result is IObjectReference, then calling its GetRealObject to return something
            if ((manager.Options & BinarySerializationOptions.IgnoreIObjectReference) == BinarySerializationOptions.None && result is IObjectReference objRef)
            {
                result = objRef.GetRealObject(Context);
                manager.UpdateReferences(objRef, result);
                if (addToCache)
                    manager.ReplaceObjectInCache(id, result);
            }

            return result;
        }

        [SecurityCritical]
        private void ReadDefaultObjectGraph(BinaryReader br, object obj, DeserializationManager manager)
        {
            Type type = obj.GetType();

            // iterating through self and base types
            for (Type t = type; t != Reflector.ObjectType; t = t.BaseType)
            {
                // checking name of base type
                if (t != type)
                {
                    string name = br.ReadString();

                    // ReSharper disable once PossibleNullReferenceException - obj is object in all cases
                    while (t.Name != name && t != Reflector.ObjectType)
                        t = t.BaseType;

                    if (name.Length == 0 && t == Reflector.ObjectType)
                        return;

                    if (t.Name != name && (manager.Options & BinarySerializationOptions.IgnoreObjectChanges) == BinarySerializationOptions.None)
                        throw new SerializationException(Res.BinarySerializationObjectHierarchyChanged(type));
                }

                // reading fields of current level
                int count = Read7BitInt(br);
                for (int i = 0; i < count; i++)
                {
                    string name = br.ReadString();
                    object value = Read(br, false, manager);

                    FieldInfo field = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    if (field == null)
                    {
                        if ((manager.Options & BinarySerializationOptions.IgnoreObjectChanges) == BinarySerializationOptions.None)
                        {
                            if (t == type)
                                throw new SerializationException(Res.BinarySerializationMissingField(type, name));
                            throw new SerializationException(Res.BinarySerializationMissingFieldBase(type, name, t));
                        }

                        continue;
                    }

                    if (field.IsNotSerialized)
                        continue;

                    manager.TrySetField(field, obj, value);
                }
            }

            // checking end of hierarchy
            if (br.ReadString().Length != 0)
            {
                if ((manager.Options & BinarySerializationOptions.IgnoreObjectChanges) == BinarySerializationOptions.None)
                    throw new SerializationException(Res.BinarySerializationObjectHierarchyChanged(type));

                // skipping fields until the end of the serialized hierarchy
                do
                {
                    int count = Read7BitInt(br);
                    for (int i = 0; i < count; i++)
                    {
                        br.ReadString();
                        Read(br, false, manager);
                    }
                }
                while (br.ReadString().Length != 0);
            }
        }

        [SecurityCritical]
        private void ReadCustomObjectGraph(BinaryReader br, object obj, DeserializationManager manager, ISerializationSurrogate surrogate, ISurrogateSelector selector)
        {
            Type type = obj.GetType();
            SerializationInfo si = new SerializationInfo(type, new FormatterConverter());
            int count = Read7BitInt(br);

            // reading content into si
            for (int i = 0; i < count; i++)
            {
                string name = br.ReadString();
                object value = Read(br, false, manager);
                Type elementType = value?.GetType() ?? Reflector.ObjectType;
                if (!br.ReadBoolean())
                    elementType = manager.ReadType(br);
                si.AddValue(name, value, elementType);
            }

            manager.CheckReferences(si);
            if (surrogate == null)
            {
                // As ISerializable: Invoking serialization constructor
                ConstructorInfo ci = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
                if (ci == null)
                    throw new SerializationException(Res.BinarySerializationMissingISerializableCtor(type));

                ci.SerializationInvoke(obj, si, Context);
            }
            else
            {
                // Using surrogate
                object result = surrogate.SetObjectData(obj, si, Context, selector);
                if (obj != result)
                    throw new NotSupportedException(Res.BinarySerializationSurrogateChangedObject(type));
            }
        }

        [SecurityCritical]
        private void ReadDefaultObjectGraphAsCustom(BinaryReader br, object obj, DeserializationManager manager, ISerializationSurrogate surrogate, ISurrogateSelector selector)
        {
            Type type = obj.GetType();

            // reading original fields into si
            SerializationInfo si = new SerializationInfo(type, new FormatterConverter());
            do
            {
                // reading fields of current level
                int count = Read7BitInt(br);
                for (int i = 0; i < count; i++)
                {
                    string name = br.ReadString();
                    object value = Read(br, false, manager);
                    si.AddValue(name, value);
                }

                // end level is marked with empty string
            } while (br.ReadString().Length != 0);

            manager.CheckReferences(si);
            if (surrogate == null)
            {
                // As ISerializable: Invoking serialization constructor
                ConstructorInfo ci = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
                if (ci == null)
                    throw new SerializationException(Res.BinarySerializationMissingISerializableCtor(type));

                ci.SerializationInvoke(obj, si, Context);
            }
            else
            {
                // Using surrogate
                object result = surrogate.SetObjectData(obj, si, Context, selector);
                if (obj != result)
                    throw new NotSupportedException(Res.BinarySerializationSurrogateChangedObject(type));
            }
        }

        [SecurityCritical]
        private void ReadCustomObjectGraphAsDefault(BinaryReader br, object obj, DeserializationManager manager)
        {
            int count = Read7BitInt(br);
            Dictionary<string, object> elements = new Dictionary<string, object>(count);

            // reading content into the dictionary
            for (int i = 0; i < count; i++)
            {
                string name = br.ReadString();
                object value = Read(br, false, manager);
                if (!br.ReadBoolean())
                {
                    Type elementType = manager.ReadType(br);
                    if (value != null && value.GetType() != elementType)
                        value = Convert.ChangeType(value, elementType, CultureInfo.InvariantCulture); // this is what FormatterConverter does as well on SerializationInfo.GetValue
                }

                elements[name] = value;
            }

            if (count == 0)
                return;

            bool checkFields = (manager.Options & BinarySerializationOptions.IgnoreObjectChanges) == BinarySerializationOptions.None;

            // ReSharper disable once PossibleNullReferenceException - obj is object in all cases
            // iterating through fields and setting found elements
            for (Type t = obj.GetType(); t != Reflector.ObjectType; t = t.BaseType)
            {
                FieldInfo[] fields = BinarySerializer.GetSerializableFields(t);
                foreach (FieldInfo field in fields)
                {
                    //if (field.IsNotSerialized) TODO: enable when GetSerializableFields is removed
                    //    continue;
                    if (elements.TryGetValue(field.Name, out object value))
                    {
                        manager.TrySetField(field, obj, value);
                        if (checkFields)
                            elements.Remove(field.Name);
                    }
                }
            }

            if (checkFields && elements.Count > 0)
                throw new SerializationException(Res.BinarySerializationMissingField(obj.GetType(), elements.First().Key));
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        object IFormatter.Deserialize(Stream serializationStream) => DeserializeFromStream(serializationStream);

        void IFormatter.Serialize(Stream serializationStream, object graph) => SerializeToStream(serializationStream, graph);

        #endregion

        #endregion

        #endregion
    }
}
