using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

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
    /// Serializes objects in a more effective(*) way than <see cref="BinaryFormatter"/>.
    /// Natively supports all of the primitive types and a sort of other simple types, arrays, generic and non-generic collections.
    /// By implementing <see cref="IBinarySerializable"/> interface, you can control the serialization of any custom type.
    /// <see cref="BinarySerializationFormatter"/> can recognize also <see cref="ISerializable"/> implementations, but can be used to serialize any type
    /// when <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option is enabled.
    /// <para><note>
    /// (*) "Effectiveness" means guaranteed better speed performance only for primitive types. Serialization time of complex types can be
    /// slower in some cases but the serialized result is almost always shorter than the result of <see cref="BinaryFormatter"/>,
    /// especially when generic types are involved.
    /// </note></para>
    /// </summary>
    /// <seealso cref="BinarySerializer"/>
    /// <seealso cref="BinarySerializationOptions"/>
    /// <seealso cref="IBinarySerializable"/>
    /// <remarks>
    /// <para>
    /// There are three ways to serialize/deserialize an object. If the needed result is a byte array, then the recommended choice is <see cref="Serialize(object)"/>.
    /// If the serialized data should be dumped into a stream <see cref="SerializeToStream(System.IO.Stream,object)"/> can be useful. And if you want to use a specific writer with a
    /// predefined <see cref="Encoding"/>, then <see cref="SerializeByWriter(System.IO.BinaryWriter,object)"/> should be chosen. If you want to use specific options
    /// you can use the <see cref="BinarySerializationOptions"/>-specific overloads of these methods. For deserialization <see cref="Deserialize(byte[])"/>, <see cref="DeserializeFromStream"/> and
    /// <see cref="DeserializeByReader"/> can be used, respectively.
    /// <h1 class="heading">Simple types</h1>
    /// Following types are natively supported (with <see cref="BinarySerializationOptions.None"/> option):
    /// <list type="bullet">
    /// <item><description><see langword="null"/> reference</description></item>
    /// <item><description><see cref="object"/></description></item>
    /// <item><description><see cref="DBNull"/></description></item>
    /// <item><description><see cref="bool"/></description></item>
    /// <item><description><see cref="sbyte"/></description></item>
    /// <item><description><see cref="byte"/></description></item>
    /// <item><description><see cref="short"/></description></item>
    /// <item><description><see cref="ushort"/></description></item>
    /// <item><description><see cref="int"/></description></item>
    /// <item><description><see cref="uint"/></description></item>
    /// <item><description><see cref="long"/></description></item>
    /// <item><description><see cref="ulong"/></description></item>
    /// <item><description><see cref="char"/></description></item>
    /// <item><description><see cref="string"/></description></item>
    /// <item><description><see cref="float"/></description></item>
    /// <item><description><see cref="double"/></description></item>
    /// <item><description><see cref="decimal"/></description></item>
    /// <item><description><see cref="DateTime"/></description></item>
    /// <item><description><see cref="TimeSpan"/></description></item>
    /// <item><description><see cref="DateTimeOffset"/></description></item>
    /// <item><description><see cref="IntPtr"/></description></item>
    /// <item><description><see cref="UIntPtr"/></description></item>
    /// <item><description><see cref="Version"/></description></item>
    /// <item><description><see cref="Guid"/></description></item>
    /// <item><description><see cref="Uri"/></description></item>
    /// <item><description><see cref="StringBuilder"/></description></item>
    /// <item><description><see cref="Enum"/> types</description></item>
    /// <item><description><see cref="Nullable{T}"/> types if type parameter is a supported type</description></item>
    /// <item><description>Any object that implements <see cref="IBinarySerializable"/> interface</description></item>
    /// </list>
    /// <note>
    /// <list type="bullet">
    /// <item><description>If a non-derived <see cref="object"/> instance is deserialized the reference will not the same as the original object, thus <see cref="object.Equals(object,object)"/> will return
    /// <see langword="false"/> for the two instances. Using the <c>object</c> type has more meaning in case of a generic collection argument type.</description></item>
    /// <item><description>Serializing <see cref="Enum"/> types will result a longer raw data than serializing their numeric value, though the result will be still shorter than the one produced by <see cref="BinaryFormatter"/>.</description></item>
    /// </list>
    /// </note>
    /// </para>
    /// <h1 class="heading">Generic collections</h1>
    /// Following generic collections are natively supported (with <see cref="BinarySerializationOptions.None"/> option) when their generic arguments are one of the simple types or other supported collections:
    /// <list type="bullet">
    /// <item><description><see cref="Array"/> of types above or compound of other supported collections</description></item>
    /// <item><description><see cref="List{T}"/></description></item>
    /// <item><description><see cref="CircularList{T}"/></description></item>
    /// <item><description><see cref="LinkedList{T}"/></description></item>
    /// <item><description><see cref="HashSet{T}"/></description></item>
    /// <item><description><see cref="Dictionary{TKey,TValue}"/></description></item>
    /// <item><description><see cref="SortedList{TKey,TValue}"/></description></item>
    /// <item><description><see cref="SortedDictionary{TKey,TValue}"/></description></item>
    /// <item><description><see cref="Queue{T}"/></description></item>
    /// <item><description><see cref="Stack{T}"/></description></item>
    /// <item><description><see cref="SortedSet{T}"/> (in .NET 4 and above)</description></item>
    /// </list>
    /// <para>
    /// Derived types of these collections and other types such as <see cref="Collection{T}"/> and <see cref="ReadOnlyCollection{T}"/> types are supported with <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option.
    /// If <see cref="IBinarySerializable"/> interface is implemented on a type, then it becomes natively serializable.
    /// </para>
    /// <para>
    /// <see cref="Array"/>s can be single- or multidimensional, jagged (array of arrays) and don't have to be zero index-based. Arrays and other generic collections can be nested.
    /// If a collection uses a non-default <see cref="IEqualityComparer{T}"/> or <see cref="IComparer{T}"/> implementation, then it is possible that the type cannot be serialized without enabling
    /// <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option, unless the comparer is decorated by <see cref="SerializableAttribute"/> or implements the <see cref="IBinarySerializable"/> interface.
    /// </para>
    /// <note type="caution">
    /// If you use the <see cref="object"/> type as generic argument or <see cref="Array"/> base type, then please note that
    /// <list type="bullet">
    /// <item><description>Even if the element types are of the same type (integers, for example) the result will be longer if they are deemed as objects.</description></item>
    /// <item><description>If an element type is not supported, an exception may be thrown. For collections that have non-natively supported elements you can enable <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/>
    /// option.</description></item>
    /// </list>
    /// </note>
    /// <note>
    /// If you serialize a collection of <see cref="IBinarySerializable"/> type implementations, then using the <see cref="IBinarySerializable"/> interface itself as array base type or generic argument
    /// may produce longer result than using the actual type. The shortest result can be achieved by using <see langword="sealed"/> classes or value types as array base types and generic parameters.
    /// </note>
    /// <h1 class="heading">Non-generic collections</h1>
    /// Following non-generic collections are natively supported (with <see cref="BinarySerializationOptions.None"/> option):
    /// <list type="table">
    /// <listheader><term>Collection type</term><description>Static element type</description></listheader>
    /// <item><term><see cref="System.Collections.ArrayList"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="Queue"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="Stack"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="StringCollection"/></term><description><see cref="string"/></description></item>
    /// <item><term><see cref="System.Collections.Hashtable"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="SortedList"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="ListDictionary"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="HybridDictionary"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="OrderedDictionary"/></term><description><see cref="object"/></description></item>
    /// <item><term><see cref="StringDictionary"/></term><description><see cref="string"/></description></item>
    /// <item><term><see cref="BitArray"/></term><description><see cref="bool"/> (actually the type is stored in a compact way)</description></item>
    /// <item><term><see cref="BitVector32"/></term><description><see cref="bool"/> (actually the type is stored in a compact way)</description></item>
    /// <item><term><see cref="BitVector32.Section"/></term><description>n.a.</description></item>
    /// </list>
    /// <para>
    /// Derived types of these collections are supported with <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option.
    /// You can either implement <see cref="IBinarySerializable"/> interface on a type to make it natively supported.
    /// </para>
    /// <para>
    /// If a collection uses a non-default <see cref="IEqualityComparer"/> or <see cref="IComparer"/> implementation, then it is possible that the type cannot be serialized without enabling
    /// <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> option, unless the comparer is decorated by <see cref="SerializableAttribute"/> or implements the <see cref="IBinarySerializable"/> interface.
    /// </para>
    /// <h1 class="heading">Serialization events</h1>
    /// <see cref="BinarySerializationFormatter"/> supports calling methods decorated by <see cref="OnSerializingAttribute"/>, <see cref="OnSerializedAttribute"/>,
    /// <see cref="OnDeserializingAttribute"/> and <see cref="OnDeserializedAttribute"/> as well as calling <see cref="IDeserializationCallback.OnDeserialization"/> method
    /// of implementers. Attributes should be used on methods that have a single <see cref="StreamingContext"/> parameter.
    /// <note>
    /// Please note that if value type was serialized due to using <see cref="BinarySerializationOptions.CompactSerializationOfStructures"/> option, then method of <see cref="OnDeserializedAttribute"/> can be invoked
    /// only after restoring the whole content so fields will be already restored.
    /// </note>
    /// </remarks>
    public sealed partial class BinarySerializationFormatter: IFormatter
    {
        #region Enums

        /// <summary>
        /// Represents possible types.
        /// One of the simple types can be combined with one of the collection types and the flags.
        /// </summary>
        [Flags]
        [DebuggerDisplay("{BinarySerializationFormatter.ToString(this)}")]
        enum DataTypes: ushort
        {
            // ------ simple types:
            Null = 0,
            Object,
            // ReSharper disable InconsistentNaming
            DBNull,
            // ReSharper restore InconsistentNaming

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

        //enum ObjectGraphType: byte
        //{
        //    Default,
        //    // ReSharper disable InconsistentNaming
        //    ISerializable,
        //    // ReSharper restore InconsistentNaming
        //    SurrogateDriven
        //}

        #endregion

        #region Constants

        private const BinarySerializationOptions extendedFlags = (BinarySerializationOptions)(1 << 7);
        //private const BinarySerializationOptions omitEnumTypes = (BinarySerializationOptions)(1 << 12);

        #endregion

        #region Fields

        #region Static Fields

        private static readonly Dictionary<DataTypes, CollectionSerializationInfo> serializationInfo;
        private static readonly IThreadSafeCacheAccessor<Type, Dictionary<Type, IEnumerable<MethodInfo>>> methodsByAttributeCache 
            = new Cache<Type, Dictionary<Type, IEnumerable<MethodInfo>>>(t => new Dictionary<Type, IEnumerable<MethodInfo>>(4), 256).GetThreadSafeAccessor(true); // true for use just a single lock because the loader is simply a new statement

        #endregion

        #region Instance Fields

        //private volatile int serializationLevel;
        private HashSet<object> serObjects;
        private List<IDeserializationCallback> deserRegObjects;
        //private readonly object syncRootDeserialize = new object(); // to lock on registered objects for IDeserializationCallback
        private BinarySerializationOptions serializationOptions;

        #endregion

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Options used for serialization. On deserializing, always original options are used (the ones when data was serialized).
        /// </summary>
        public BinarySerializationOptions Options
        {
            get { return serializationOptions; }
            set
            {
                if (!value.AllFlagsDefined())
                {
                    throw new ArgumentOutOfRangeException(nameof(value), Res.ArgumentOutOfRange);
                }

                serializationOptions = value;
            }
        }

        #endregion

        #endregion

        #region Constructors

        #region Static Constructor

        static BinarySerializationFormatter()
        {
            serializationInfo = new Dictionary<DataTypes, CollectionSerializationInfo>(EnumComparer<DataTypes>.Comparer)
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
        }

        #endregion

        #region Instance Constructors

        /// <summary>
        /// Creates a new instance of <see cref="BinarySerializationFormatter"/> class.
        /// </summary>
        /// <param name="options">Options used for serialization.</param>
        public BinarySerializationFormatter(BinarySerializationOptions options)
        {
            Context = new StreamingContext(StreamingContextStates.All);
            Options = options;
        }

        /// <summary>
        /// Creates a new instance of <see cref="BinarySerializationFormatter"/> class with default options.
        /// </summary>
        public BinarySerializationFormatter() :
            this(BinarySerializer.DefaultOptions)
        {
        }

        #endregion

        #endregion

        #region Public methods

        #region Object serialization

        /// <summary>
        /// Serializes an object into a byte array.
        /// </summary>
        /// <param name="data">The object to serialize</param>
        /// <returns>Serialized raw data of the object</returns>
        public byte[] Serialize(object data)
        {
            try
            {
                MemoryStream result;
                using (BinaryWriter bw = new BinaryWriter(result = new MemoryStream()))
                {
                    Write(bw, data, true, new SerializationManager(Context, Options, Binder, SurrogateSelector));
                }

                return result.ToArray();
            }
            finally
            {
                ReleaseLocks();
            }
        }

        /// <summary>
        /// Deserializes the specified part of a byte array into an object.
        /// </summary>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="offset">Points to the starting position of the object data in <paramref name="rawData"/>.</param>
        /// <returns>The deserialized data.</returns>
        /// <overloads>In the two-parameter overload the start offset of the data to deserialize can be specified.</overloads>
        public object Deserialize(byte[] rawData, int offset)
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
                    ReleaseLocks();
                }
            }
        }

        /// <summary>
        /// Deserializes a byte array into an object.
        /// </summary>
        /// <param name="rawData">The raw data representation of the object to deserialize.</param>
        /// <returns>The deserialized data.</returns>
        public object Deserialize(byte[] rawData)
        {
            return Deserialize(rawData, 0);
        }

        /// <summary>
        /// Serializes the given <paramref name="data"/> into a <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream, into which the data is written. The stream must support writing and will remain open after serialization.</param>
        /// <param name="data">The data that will be written into the stream.</param>
        public void SerializeToStream(Stream stream, object data)
        {
            try
            {
                BinaryWriter bw = new BinaryWriter(stream);
                Write(bw, data, true, new SerializationManager(Context, Options, Binder, SurrogateSelector));
            }
            finally
            {
                ReleaseLocks();
            }
        }

        /// <summary>
        /// Deserializes data beginning at current position of given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream, from which the data is read. The stream must support reading and will remain open after deserialization.</param>
        /// <returns>The deserialized data.</returns>
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
                ReleaseLocks();
            }
        }

        /// <summary>
        /// Serializes the given <paramref name="data"/> by using the provided <paramref name="writer"/>.
        /// </summary>
        /// <note>
        /// This method produces compatible serialized data with <see cref="Serialize(object)"/>
        /// and <see cref="SerializeToStream(System.IO.Stream,object)"/> only when encoding of the writer is UTF-8. Otherwise, you must use <see cref="DeserializeByReader"/> with the same encoding as here.
        /// </note>
        /// <param name="writer">The writer that will used to serialize data. The writer will remain opened after serialization.</param>
        /// <param name="data">The data that will be written by the writer.</param>
        public void SerializeByWriter(BinaryWriter writer, object data)
        {
            try
            {
                Write(writer, data, true, new SerializationManager(Context, Options, Binder, SurrogateSelector));
            }
            finally
            {
                ReleaseLocks();
            }
        }

        /// <summary>
        /// Deserializes data beginning at current position of given <paramref name="reader"/>.
        /// </summary>
        /// <note>
        /// If data was serialized by <see cref="Serialize(object)"/> or <see cref="SerializeToStream(System.IO.Stream,object)"/>, then
        /// reader must use UTF-8 encoding to get correct result. If data was serialized by <see cref="SerializeByWriter(System.IO.BinaryWriter,object)"/>, then you must use the same encoding as there.
        /// </note>
        /// <param name="reader">The reader that will be used to deserialize data. The reder will remain opened after deserialization.</param>
        /// <returns>The deserialized data.</returns>
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
                ReleaseLocks();
            }
        }

        #endregion

        #endregion

        #region Private Methods

        /// <summary>
        /// Writing an object. Can be used both at root and object element level. Options are copied so parallel serialization/deserialization processes can be started with different options.
        /// </summary>>
        /// <param name="bw">The writer</param>
        /// <param name="data">The object to serialize</param>
        /// <param name="isRoot"><see langword="true"/>, when <paramref name="data"/> is the root level object.</param>
        /// <param name="manager">The serialization manager</param>
        private void Write(BinaryWriter bw, object data, bool isRoot, SerializationManager manager)
        {
            if (!isRoot)
            {
                // if an existing id found, returning
                if (manager.WriteId(bw, data))
                    return;
            }

            // a.) Natively supported primitive types including string and uintptr (no need for distinct nullables here as they are boxed)
            if (data == null)
            {
                bw.Write((ushort)DataTypes.Null);
                return;
            }
            if (data is bool)
            {
                bw.Write((ushort)DataTypes.Bool);
                bw.Write((bool)data);
                return;
            }
            if (data is byte)
            {
                bw.Write((ushort)DataTypes.UInt8);
                bw.Write((byte)data);
                return;
            }
            if (data is sbyte)
            {
                bw.Write((ushort)DataTypes.Int8);
                bw.Write((sbyte)data);
                return;
            }
            if (data is short)
            {
                WriteDynamicInt(bw, DataTypes.Int16, 2, (ulong)(short)data);
                return;
            }
            if (data is ushort)
            {
                WriteDynamicInt(bw, DataTypes.UInt16, 2, (ushort)data);
                return;
            }
            if (data is int)
            {
                WriteDynamicInt(bw, DataTypes.Int32, 4, (ulong)(int)data);
                return;
            }
            if (data is uint)
            {
                WriteDynamicInt(bw, DataTypes.UInt32, 4, (uint)data);
                return;
            }
            if (data is long)
            {
                WriteDynamicInt(bw, DataTypes.Int64, 8, (ulong)(long)data);
                return;
            }
            if (data is ulong)
            {
                WriteDynamicInt(bw, DataTypes.UInt64, 8, (ulong)data);
                return;
            }
            if (data is char)
            {
                WriteDynamicInt(bw, DataTypes.Char, 2, (char)data);
                return;
            }
            string stringData = data as string;
            if (stringData != null)
            {
                bw.Write((ushort)DataTypes.String);
                bw.Write(stringData);
                return;
            }
            if (data is float)
            {
                bw.Write((ushort)DataTypes.Single);
                bw.Write((float)data);
                return;
            }
            if (data is double)
            {
                bw.Write((ushort)DataTypes.Double);
                bw.Write((double)data);
                return;
            }
            if (data is IntPtr)
            {
                bw.Write((ushort)DataTypes.IntPtr);
                bw.Write(((IntPtr)data).ToInt64());
                return;
            }
            if (data is UIntPtr)
            {
                bw.Write((ushort)DataTypes.UIntPtr);
                bw.Write(((UIntPtr)data).ToUInt64());
                return;
            }

            // b.) Surrogate selector for any type
            Type type = null;
            BinarySerializationOptions options = manager.Options;
            if ((options & BinarySerializationOptions.TryUseSurrogateSelectorForAnyType) != BinarySerializationOptions.None && manager.CanUseSurrogate(type = data.GetType()))
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
            if (data is decimal)
            {
                bw.Write((ushort)DataTypes.Decimal);
                bw.Write((decimal)data);
                return;
            }
            if (data is DateTime)
            {
                bw.Write((ushort)DataTypes.DateTime);
                WriteDateTime(bw, (DateTime)data);
                return;
            }
            if (data is TimeSpan)
            {
                bw.Write((ushort)DataTypes.TimeSpan);
                bw.Write(((TimeSpan)data).Ticks);
                return;
            }
            if (data is DateTimeOffset)
            {
                bw.Write((ushort)DataTypes.DateTimeOffset);
                WriteDateTimeOffset(bw, (DateTimeOffset)data);
                return;
            }
            if (data is DBNull)
            {
                bw.Write((ushort)DataTypes.DBNull);
                return;
            }
            if (data is Guid)
            {
                bw.Write((ushort)DataTypes.Guid);
                bw.Write(((Guid)data).ToByteArray());
                return;
            }
            if (data is BitVector32)
            {
                bw.Write((ushort)DataTypes.BitVector32);
                bw.Write(((BitVector32)data).Data);
                return;
            }
            if (data is BitVector32.Section)
            {
                bw.Write((ushort)DataTypes.BitVector32Section);
                WriteSection(bw, (BitVector32.Section)data);
                return;
            }
            Version version = data as Version;
            if (version != null)
            {
                bw.Write((ushort)DataTypes.Version);
                WriteVersion(bw, version);
                return;
            }
            BitArray bitArray = data as BitArray;
            if (bitArray != null)
            {
                bw.Write((ushort)DataTypes.BitArray);
                WriteBitArray(bw, bitArray);
                return;
            }
            StringBuilder sb = data as StringBuilder;
            if (sb != null)
            {
                bw.Write((ushort)DataTypes.StringBuilder);
                WriteStringBuilder(bw, sb);
                return;
            }

            // non-sealed types below
            type = type ?? data.GetType();
            if (type == typeof(object))
            {
                bw.Write((ushort)DataTypes.Object);
                return;
            }
            if (type == typeof(Uri))
            {
                bw.Write((ushort)DataTypes.Uri);
                WriteUri(bw, (Uri)data);
                return;
            }

            // d.) enum: storing enum type, assembly qualified name and value: still shorter than binary formatter
            Enum enumObject = data as Enum;
            if (enumObject != null)
            {
                TypeCode enumType = enumObject.GetTypeCode();
                DataTypes dataType = DataTypes.Enum | GetSupportedElementType(Enum.GetUnderlyingType(type), options, manager);
                // ReSharper disable PossibleInvalidCastException
                ulong enumValue = enumType == TypeCode.UInt64 ? (ulong)data : (ulong)((IConvertible)enumObject).ToInt64(null);
                // ReSharper restore PossibleInvalidCastException

                bool is7Bit = false;
                int size = 1;
                switch (enumType)
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                        break;
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        size = 2;
                        is7Bit = enumValue < (1UL << 7);
                        break;
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        size = 4;
                        is7Bit = enumValue < (1UL << 21);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        size = 8;
                        is7Bit = enumValue < (1UL << 49);
                        break;
                    default:
                        // should never occur, throwing internal error without resource
                        throw new ArgumentOutOfRangeException();
                }

                if (is7Bit)
                    dataType |= DataTypes.Store7BitEncoded;
                bw.Write((ushort)dataType);
                manager.WriteType(bw, type);
                if (is7Bit)
                    Write7BitLong(bw, enumValue);
                else
                    bw.Write(BitConverter.GetBytes(enumValue), 0, size);
                return;
            }

            // e.) Supported collection or compound of collections
            if (IsSupportedCollection(type))
            {
                IEnumerable<DataTypes> collectionType = EncodeCollectionType(type, options, manager);
                if (collectionType != null)
                {
                    CircularList<DataTypes> collectionTypeList = new CircularList<DataTypes>(collectionType);
                    foreach (DataTypes dataType in collectionTypeList)
                    {
                        bw.Write((ushort)dataType);
                    }

                    // on root level writing the id after datatype if the collection may have recursion because its reference can be re-used
                    if (isRoot && CanHaveRecursion(collectionTypeList))
                    {
                        if (manager.WriteId(bw, data))
                            Debug.Fail("Id of recursive object should be unknown on top level.");
                    }

                    WriteOptions(bw, collectionTypeList, options);
                    WriteTypeNamesAndRanks(bw, type, options, manager);
                    WriteCollection(bw, collectionTypeList, data, manager);
                    return;
                }
            }

            // f.) BinarySerializable
            IBinarySerializable binarySerializable;
            if (((options & BinarySerializationOptions.IgnoreIBinarySerializable) == BinarySerializationOptions.None)
                && (binarySerializable = data as IBinarySerializable) != null)
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
            if ((options & BinarySerializationOptions.CompactSerializationOfStructures) != BinarySerializationOptions.None && type.IsValueType && BinarySerializer.CanSerializeStruct(type))
            {
                bw.Write((ushort)DataTypes.RawStruct);
                manager.WriteType(bw, type);
                WriteValueType(bw, data, options);
                return;
            }

            // h.) Recursive serialization: if enabled or surrogate selector supports the type, or when type is serializable
            if ((options & BinarySerializationOptions.RecursiveSerializationAsFallback) != BinarySerializationOptions.None || manager.CanUseSurrogate(type)
                || type.IsSerializable && !type.IsArray) // array is serializable so array with non-serializable element type would be a problem
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

#pragma warning disable 618,612
            // i.) Any struct (obsolete but still supported as backward compatibility)
            if ((options & BinarySerializationOptions.ForcedSerializationValueTypesAsFallback) != BinarySerializationOptions.None && type.IsValueType)
            {
                bw.Write((ushort)DataTypes.RawStruct);
                manager.WriteType(bw, type);
                WriteValueType(bw, data, options);
                return;
            }
#pragma warning restore 618,612

            ThrowNotSupported(options, type);
        }

        private bool CanHaveRecursion(CircularList<DataTypes> collectionType)
        {
            return collectionType.Exists(dt =>
                (dt & DataTypes.SimpleTypes) == DataTypes.BinarySerializable
                || (dt & DataTypes.SimpleTypes) == DataTypes.RecursiveObjectGraph
                || (dt & DataTypes.SimpleTypes) == DataTypes.Object);
        }

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
                {
                    WriteTypeNamesAndRanks(bw, genericArgument, options, manager);
                }
            }
        }

        /// <summary>
        /// Returning a true value just indicates that the type itself supported without the generic parameters or element type.
        /// </summary>
        private static bool IsSupportedCollection(Type type)
        {
            if (type.IsArray)
                return true;
            if (type.IsValueType && type.IsNullable())
                type = Nullable.GetUnderlyingType(type);
            if (type.IsGenericType)
            {
                Type typeDef = type.GetGenericTypeDefinition();
                return typeDef == typeof(List<>) || typeDef == typeof(Dictionary<,>)
                    || typeDef == typeof(HashSet<>)
                    || typeDef == typeof(CircularList<>) || typeDef == typeof(CircularSortedList<,>)
                    || typeDef == typeof(SortedList<,>) || typeDef == typeof(SortedDictionary<,>)
                    || typeDef == typeof(Queue<>) || typeDef == typeof(Stack<>)
#if NET40 || NET45
 || typeDef == typeof(SortedSet<>)
#elif !NET35
#error .NET version is not set or not supported!
#endif
 || typeDef == typeof(LinkedList<>)
                    || typeDef == typeof(KeyValuePair<,>); // not actually a collection but can be encoded more easily as a dictionary
            }

            return type == typeof(ArrayList) || type == typeof(Queue) || type == typeof(Stack)
                || type == typeof(Hashtable) || type == typeof(SortedList) || type == typeof(ListDictionary) || type == typeof(HybridDictionary) || type == typeof(OrderedDictionary)
                || type == typeof(StringCollection) || type == typeof(StringDictionary)
                || type == typeof(DictionaryEntry); // encoded as a non-generic dictionary
        }

        private static IEnumerable<DataTypes> EncodeCollectionType(Type type, BinarySerializationOptions options, SerializationManager manager)
        {
            // array
            if (type.IsArray)
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

            DataTypes collectionType = GetSupportedCollectionType(type);
            if (type.IsNullable())
                type = Nullable.GetUnderlyingType(type);

            // generic type
            if (type.IsGenericType)
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

            // non-generic types
            else
            {
                switch (collectionType)
                {
                    case DataTypes.ArrayList:
                    case DataTypes.QueueNonGeneric:
                    case DataTypes.StackNonGeneric:
                        return new DataTypes[] { collectionType | DataTypes.Object };

                    case DataTypes.Hashtable:
                    case DataTypes.SortedListNonGeneric:
                    case DataTypes.ListDictionary:
                    case DataTypes.HybridDictionary:
                    case DataTypes.OrderedDictionary:
                    case DataTypes.DictionaryEntry:
                    case DataTypes.DictionaryEntryNullable:
                        return new DataTypes[] { collectionType | DataTypes.Object, DataTypes.Object };

                    case DataTypes.StringCollection:
                        return new DataTypes[] { collectionType | DataTypes.String };

                    case DataTypes.StringDictionary:
                        return new DataTypes[] { collectionType | DataTypes.String, DataTypes.String };
                    default:
                        // should never occur, throwing internal error without resource
                        throw new InvalidOperationException("Element type of non-generic collection is not defined: " + ToString(collectionType));
                }
            }
        }

        private static DataTypes GetSupportedCollectionType(Type type)
        {
            if (type.IsArray)
                return DataTypes.Array;

            if (type == typeof(DictionaryEntry))
                return DataTypes.DictionaryEntry;

            Type genType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
            if (genType == typeof(KeyValuePair<,>))
                return DataTypes.KeyValuePair;

            if (type.IsNullable())
            {
                switch (GetSupportedCollectionType(Nullable.GetUnderlyingType(type)))
                {
                    case DataTypes.DictionaryEntry:
                        return DataTypes.DictionaryEntryNullable;
                    case DataTypes.KeyValuePair:
                        return DataTypes.KeyValuePairNullable;
                    default:
                        return DataTypes.Null;
                }
            }

            if (!typeof(IEnumerable).IsAssignableFrom(type))
                return DataTypes.Null;

            if (genType != null)
            {
                if (genType == typeof(List<>))
                    return DataTypes.List;
                //if (genType == typeof(Collection<>))
                //    return DataTypes.Collection;
                if (genType == typeof(Dictionary<,>))
                    return DataTypes.Dictionary;
                if (genType == typeof(HashSet<>))
                    return DataTypes.HashSet;
                if (genType == typeof(CircularList<>))
                    return DataTypes.CircularList;
                if (genType == typeof(CircularSortedList<,>))
                    return DataTypes.CircularSortedList;
                if (genType == typeof(LinkedList<>))
                    return DataTypes.LinkedList;
                if (genType == typeof(SortedList<,>))
                    return DataTypes.SortedList;
                if (genType == typeof(SortedDictionary<,>))
                    return DataTypes.SortedDictionary;
                if (genType == typeof(Queue<>))
                    return DataTypes.Queue;
                if (genType == typeof(Stack<>))
                    return DataTypes.Stack;
                //if (genType == typeof(ReadOnlyCollection<>))
                //    return DataTypes.ReadOnlyCollection;
#if NET40 || NET45
                if (genType == typeof(SortedSet<>))
                    return DataTypes.SortedSet;
#elif !NET35
#error .NET version is not set or not supported!
#endif

                return DataTypes.Null;
            }

            if (type == typeof(ArrayList))
                return DataTypes.ArrayList;
            if (type == typeof(Hashtable))
                return DataTypes.Hashtable;
            if (type == typeof(Queue))
                return DataTypes.QueueNonGeneric;
            if (type == typeof(Stack))
                return DataTypes.StackNonGeneric;
            if (type == typeof(StringCollection))
                return DataTypes.StringCollection;
            if (type == typeof(SortedList))
                return DataTypes.SortedListNonGeneric;
            if (type == typeof(ListDictionary))
                return DataTypes.ListDictionary;
            if (type == typeof(HybridDictionary))
                return DataTypes.HybridDictionary;
            if (type == typeof(OrderedDictionary))
                return DataTypes.OrderedDictionary;
            if (type == typeof(StringDictionary))
                return DataTypes.StringDictionary;

            return DataTypes.Null;
        }

        private static DataTypes GetSupportedElementType(Type type, BinarySerializationOptions options, SerializationManager manager)
        {
            // a.) Natively supported primitive types
            if (type == typeof(bool))
                return DataTypes.Bool;
            if (type == typeof(byte))
                return DataTypes.UInt8;
            if (type == typeof(sbyte))
                return DataTypes.Int8;
            if (type == typeof(short))
                return DataTypes.Int16;
            if (type == typeof(ushort))
                return DataTypes.UInt16;
            if (type == typeof(int))
                return DataTypes.Int32;
            if (type == typeof(uint))
                return DataTypes.UInt32;
            if (type == typeof(long))
                return DataTypes.Int64;
            if (type == typeof(ulong))
                return DataTypes.UInt64;
            if (type == typeof(char))
                return DataTypes.Char;
            if (type == typeof(string))
                return DataTypes.String;
            if (type == typeof(float))
                return DataTypes.Single;
            if (type == typeof(double))
                return DataTypes.Double;
            if (type == typeof(IntPtr))
                return DataTypes.IntPtr;
            if (type == typeof(UIntPtr))
                return DataTypes.UIntPtr;

            // b.) nullable (must be before surrogate-support checks)
            if (type.IsNullable())
            {
                DataTypes elementType = GetSupportedElementType(Nullable.GetUnderlyingType(type), options, manager);
                if (elementType == DataTypes.Null)
                    return elementType;
                return DataTypes.Nullable | elementType;
            }

            // c.) surrogate for any type: check even for sub-collections
            if ((options & BinarySerializationOptions.TryUseSurrogateSelectorForAnyType) != BinarySerializationOptions.None && manager.CanUseSurrogate(type))
                return DataTypes.RecursiveObjectGraph;

            // if type is a collection, then returning null here
            if (GetSupportedCollectionType(type) != DataTypes.Null)
                return DataTypes.Null;

            // d.) Natively supported non-primitive types
            if (type == typeof(decimal))
                return DataTypes.Decimal;
            if (type == typeof(DateTime))
                return DataTypes.DateTime;
            if (type == typeof(object))
                return DataTypes.Object;
            if (type == typeof(DBNull))
                return DataTypes.DBNull;
            if (type == typeof(Version))
                return DataTypes.Version;
            if (type == typeof(Guid))
                return DataTypes.Guid;
            if (type == typeof(TimeSpan))
                return DataTypes.TimeSpan;
            if (type == typeof(DateTimeOffset))
                return DataTypes.DateTimeOffset;
            if (type == typeof(Uri))
                return DataTypes.Uri;
            if (type == typeof(BitArray))
                return DataTypes.BitArray;
            if (type == typeof(BitVector32))
                return DataTypes.BitVector32;
            if (type == typeof(BitVector32.Section))
                return DataTypes.BitVector32Section;
            if (type == typeof(StringBuilder))
                return DataTypes.StringBuilder;

            // e.) enum
            if (type.IsEnum)
                return DataTypes.Enum | GetSupportedElementType(Enum.GetUnderlyingType(type), options, manager);

            // f.) IBinarySerializable implementation
            if (((options & BinarySerializationOptions.IgnoreIBinarySerializable) == BinarySerializationOptions.None)
                && typeof(IBinarySerializable).IsAssignableFrom(type))
            {
                return DataTypes.BinarySerializable;
            }

            // g.) Any struct if can be serialized
            if ((options & BinarySerializationOptions.CompactSerializationOfStructures) != BinarySerializationOptions.None && type.IsValueType && BinarySerializer.CanSerializeStruct(type))
            {
                return DataTypes.RawStruct;
            }

            // h.) Recursive serialization
            if ((options & BinarySerializationOptions.RecursiveSerializationAsFallback) != BinarySerializationOptions.None
                || manager != null && manager.CanUseSurrogate(type)
                || type.IsSerializable || type.IsInterface)
            {
                return DataTypes.RecursiveObjectGraph;
            }

#pragma warning disable 618,612
            // i.) Any struct (obsolete but still supported as backward compatibility)
            if ((options & BinarySerializationOptions.ForcedSerializationValueTypesAsFallback) != BinarySerializationOptions.None && type.IsValueType)
            {
                return DataTypes.RawStruct;
            }
#pragma warning restore 618,612

            return DataTypes.Null;
        }

        /// <summary>
        /// Gets the element types for a dictionary
        /// </summary>
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

        private void WriteCollection(BinaryWriter bw, CircularList<DataTypes> collectionTypeDescriptor, object obj,
            SerializationManager manager)
        {
            if (collectionTypeDescriptor.Count == 0)
                // should never occur, throwing internal error without resource
                throw new ArgumentException("Type description is invalid", nameof(collectionTypeDescriptor));

            DataTypes collectionDataType = collectionTypeDescriptor[0];
            DataTypes elementDataType = collectionDataType & ~(DataTypes.CollectionTypes | DataTypes.Enum);

            // array
            if ((collectionDataType & DataTypes.CollectionTypes) == DataTypes.Array)
            {
                Array array = (Array) obj;
                // 1. Dimensions
                for (int i = 0; i < array.Rank; i++)
                {
                    Write7BitInt(bw, array.GetLowerBound(i));
                    Write7BitInt(bw, array.GetLength(i));
                }

                // 2. Write elements
                Type elementType = array.GetType().GetElementType();
                // 2.a.) Primitive array
                if (elementType.IsPrimitive)
                {
                    byte[] rawData = array as byte[];
                    if (rawData == null)
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
            IEnumerable collection = obj as IEnumerable ?? new object[] {obj};
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
                Type[] argTypes =
                    (obj is IEnumerable ? collection : ((object[]) collection)[0]).GetType().GetGenericArguments();
                Type keyType = argTypes[0];
                Type valueType = argTypes[1];

                CircularList<DataTypes> valueCollectionDataTypes = GetDictionaryValueTypes(collectionTypeDescriptor);
                collectionTypeDescriptor.RemoveAt(0);
                DataTypes valueDataType = DataTypes.Null;
                if ((valueCollectionDataTypes[0] & DataTypes.CollectionTypes) == DataTypes.Null)
                    valueDataType = valueCollectionDataTypes[0] & ~DataTypes.Enum;
                WriteDictionaryElements(bw, collection, collectionTypeDescriptor, elementDataType,
                    valueCollectionDataTypes, valueDataType,
                    keyType, valueType, manager);
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

        private void WriteCollectionElements(BinaryWriter bw, IEnumerable collection, CircularList<DataTypes> elementCollectionDataTypes, DataTypes elementDataType,
            Type collectionElementType, SerializationManager manager)
        {
            foreach (object element in collection)
            {
                WriteElement(bw, element, elementCollectionDataTypes, elementDataType, collectionElementType, manager);
            }
        }

        private void WriteDictionaryElements(BinaryWriter bw, IEnumerable collection, CircularList<DataTypes> keyCollectionDataTypes, DataTypes keyDataType,
            CircularList<DataTypes> valueCollectionDataTypes, DataTypes valueDataType, Type collectionKeyType, Type collectionValueType, SerializationManager manager)
        {
            IDictionary dictionary = collection as IDictionary;
            if (dictionary != null)
            {
                foreach (DictionaryEntry element in (IDictionary)collection)
                {
                    WriteElement(bw, element.Key, keyCollectionDataTypes, keyDataType, collectionKeyType, manager);
                    WriteElement(bw, element.Value, valueCollectionDataTypes, valueDataType, collectionValueType, manager);
                }
            }
            // if cannot be casted an non-generic dictionary, Key and Value properties can be accessed only via reflection
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

        /// <summary>
        /// Writes a <paramref name="length"/> bytes length value in the possible most compact form.
        /// </summary>
        private void WriteDynamicInt(BinaryWriter bw, DataTypes dataType, int length, ulong value)
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
            bw.Write((string)Reflector.RunMethod(uri, "GetParts", UriComponents.SerializationInfoString, UriFormat.UriEscaped));
        }

        private static void WriteBitArray(BinaryWriter bw, BitArray bitArray)
        {
            int length = bitArray.Length;
            Write7BitInt(bw, bitArray.Length);
            if (length > 0)
            {
                int[] value = (int[])Reflector.GetField(bitArray, "m_array");
                foreach (int i in value)
                {
                    bw.Write(i);
                }
            }
        }

        private static void WriteSection(BinaryWriter bw, BitVector32.Section section)
        {
            bw.Write(section.Mask);
            bw.Write(section.Offset);
        }

        private void WriteStringBuilder(BinaryWriter bw, StringBuilder sb)
        {
            Write7BitInt(bw, sb.Capacity);
            bw.Write(sb.ToString());
        }

        private void WriteBinarySerializable(BinaryWriter bw, IBinarySerializable instance, BinarySerializationOptions options)
        {
            OnSerializing(instance, options);
            byte[] rawData = instance.Serialize(options);
            Write7BitInt(bw, rawData.Length);
            bw.Write(rawData);
            OnSerialized(instance, options);
        }

        private void WriteValueType(BinaryWriter bw, object data, BinarySerializationOptions options)
        {
            OnSerializing(data, options);
            byte[] rawData = BinarySerializer.SerializeStruct((ValueType)data);
            Write7BitInt(bw, rawData.Length);
            bw.Write(rawData);
            OnSerialized(data, options);
        }

        /// <summary>
        /// Serializes an object graph.
        /// </summary>
        /// <param name="bw">Writer</param>
        /// <param name="data">The object to serialize</param>
        /// <param name="collectionElementType">Element type of collection or null if not in collection</param>
        /// <param name="manager">Serialization Manager.</param>
        private void WriteObjectGraph(BinaryWriter bw, object data, Type collectionElementType, SerializationManager manager)
        {
            // Common order: 1: not in a collection -> store type, 2: serialize
            BinarySerializationOptions options = manager.Options;
            OnSerializing(data, options);

            Type type = data.GetType();
            ISerializationSurrogate surrogate;
            ISurrogateSelector selector;
            if (manager.TryGetSurrogate(type, out surrogate, out selector) || ((options & BinarySerializationOptions.IgnoreISerializable) == BinarySerializationOptions.None && data is ISerializable))
            {
                WriteCustomObjectGraph(bw, data, collectionElementType, options, manager, surrogate);
            }
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

        private void WriteDefaultObjectGraph(BinaryWriter bw, object data, SerializationManager manager)
        {
            // true for IsDefault object graph
            bw.Write(true);
            Type type = data.GetType();
            Debug.Assert(!type.IsArray, "Array cannot be serialized as object graph");

            // iterating through self and base types
            for (Type t = type; t != Reflector.ObjectType; t = t.BaseType)
            {
                // writing fields of current level
                FieldInfo[] fields = BinarySerializer.GetSerializableFields(t);

                if (fields.Length != 0 || t == type)
                {
                    // writing name of base type
                    if (t != type)
                    {
                        bw.Write(t.Name);
                    }

                    // writing the fields
                    Write7BitInt(bw, fields.Length);
                    foreach (FieldInfo field in fields)
                    {
                        bw.Write(field.Name);
                        Type fieldType = field.FieldType;
                        object fieldValue = FieldAccessor.GetAccessor(field).Get(data);
                        if (fieldValue != null && fieldType.IsEnum)
                            fieldValue = Convert.ChangeType(fieldValue, Enum.GetUnderlyingType(fieldType));
                        Write(bw, fieldValue, false, manager);
                    }
                }
            }

            // marking end of hierarchy
            bw.Write(String.Empty);
        }

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
                bool typeMatch = entry.Value == null && entry.ObjectType == typeof(object)
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

        private static IEnumerable<MethodInfo> GetMethodsWithAttribute(Type attribute, Type type)
        {
            Dictionary<Type, IEnumerable<MethodInfo>> cacheItem = methodsByAttributeCache[type];

            lock (cacheItem)
            {
                IEnumerable<MethodInfo> cachedResult;
                if (cacheItem.TryGetValue(attribute, out cachedResult))
                {
                    return cachedResult;
                }

                List<MethodInfo> result = new List<MethodInfo>();
                for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
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
                {
                    result.Reverse();
                }

                if (result.Count == 0)
                {
                    cacheItem[attribute] = null;
                    return null;
                }

                cacheItem[attribute] = result;
                return result;
            }
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

        /// <summary>
        /// Releases accidentaly opened locks (happens on exceptions)
        /// </summary>
        private void ReleaseLocks()
        {
            //// closing opened serialization lock
            //if (serializationLevel != 0)
            //{
            //    serializationLevel = 0;
            serObjects = null;
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
            {
                Reflector.RunMethod(obj, method, Context);
            }
        }

        /// <summary>
        /// Deserializes an object from the stream.
        /// </summary>
        /// <param name="br">The reader</param>
        /// <param name="isRoot"><see langword="true"/>, when the object to deserialize is the root-level object</param>
        /// <param name="manager">The manager used for deserialization.</param>
        /// <returns>The deserialized object</returns>
        private object Read(BinaryReader br, bool isRoot, DeserializationManager manager)
        {
            object result;
            if (!isRoot && manager.TryGetCachedObject(br, out result))
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
        private object CreateArray(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor, DeserializationManager manager)
        {
            // getting whether the current instance is in cache
            if (descriptor.ParentDescriptor != null)
            {
                object cachedResult;
                if (manager.TryGetCachedObject(br, out cachedResult))
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
                ArrayIndexer arrayIndexer = new ArrayIndexer(lengths, lowerBounds);
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
        private object CreateCollection(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor, DeserializationManager manager)
        {
            if (!descriptor.IsSingleElement && !typeof(IEnumerable).IsAssignableFrom(descriptor.Type))
                throw new InvalidOperationException(Res.BinarySerializationIEnumerableExpected(descriptor.Type));

            // getting whether the current instance is in cache
            if (descriptor.ParentDescriptor != null && (!descriptor.Type.IsValueType || descriptor.Type.IsNullable()))
            {
                object cachedResult;
                if (manager.TryGetCachedObject(br, out cachedResult))
                    return cachedResult;
            }

            // keyvaluepair, dictionary entry
            if (descriptor.IsSingleElement)
            {
                object result = Reflector.CreateInstance(descriptor.GetTypeToCreate());
                if (addToCache)
                    manager.AddObjectToCache(result);
                object key = ReadElement(br, descriptor, manager, false);
                object value = descriptor.IsDictionary ? ReadElement(br, descriptor, manager, true) : null;
                Reflector.SetField(result, descriptor.GetFieldNameToSet(false), key);
                Reflector.SetField(result, descriptor.GetFieldNameToSet(true), value);
                //ConstructorInfo ctor = descriptor.GetTypeToCreate().GetConstructor(new Type[] { descriptor.ElementType, descriptor.DictionaryValueType });
                //result = Reflector.Construct(ctor, key, value);
                return result;
            }

            CollectionSerializationInfo serInfo = serializationInfo[descriptor.CollectionDataType];
            int count;
            IEnumerable collection = (IEnumerable)serInfo.InitializeCollection(this, br, addToCache, descriptor, manager, out count);

            MethodInfo addMethod = serInfo.SpecificAddMethod != null ? collection.GetType().GetMethod(serInfo.SpecificAddMethod) : null;

            for (int i = 0; i < count; i++)
            {
                object element = ReadElement(br, descriptor, manager, false);
                object value = descriptor.IsDictionary ? ReadElement(br, descriptor, manager, true) : null;

                if (descriptor.IsDictionary)
                {
                    if (addMethod != null)
                    {
                        Reflector.RunMethod(collection, addMethod, element, value);
                        continue;
                    }

#if NET35
                    if (value != null || !descriptor.IsGenericDictionary)
                    {
#endif
                        ((IDictionary)collection).Add(element, value);
                        continue;
#if NET35
                    }

                    // generic dictionary with null value: calling generic Add because non-generic one may fail under .NET 4
                    Reflector.RunMethod(collection, nameof(IDictionary<_,_>.Add), element, null);
                    continue;
#endif
                }

                if (addMethod != null)
                {
                    Reflector.RunMethod(collection, addMethod, element);
                    continue;
                }

                // ReSharper disable PossibleMultipleEnumeration
                collection.Add(element);
                // ReSharper restore PossibleMultipleEnumeration
            }

            return descriptor.IsReadOnly ? descriptor.GetAsReadOnly(collection) : collection;
        }

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

            // other nested colletion
            return CreateCollection(br, !elementDescriptor.Type.IsValueType || elementDescriptor.Type.IsNullable(), elementDescriptor, manager);
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
                {
                    throw new InvalidOperationException(Res.BinarySerializationInvalidStreamData);
                }

                b = br.ReadByte();

                result |= (b & 0x7F) << shift;
                shift += 7;
            }
            while ((b & 0x80) != 0);

            return result;
        }

        private long Read7BitLong(BinaryReader br)
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
                        {
                            // occurs on root level: object id is stored only after data type
                            if (isRoot)
                            {
                                if (manager.TryGetCachedObject(br, out cachedResult))
                                {
                                    Debug.Fail("Root level object is not expected in the cache");
                                    return cachedResult;
                                }
                            }

                            BinarySerializationOptions origOptions = collectionDescriptor == null
                                ? ReadOptions(br)
                                : collectionDescriptor.SerializationOptions;

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
                            {
                                objType = manager.ReadType(br);
                            }
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
                            return ReadBinarySerializable(br, addToCache || isRoot, objType, origOptions, manager);
                        }

                    // recursive graph
                    case DataTypes.RecursiveObjectGraph:
                        {
                            // When element types may differ, reading element with data type
                            if (collectionDescriptor != null && collectionDescriptor.AreAllElementsQualified(isTValue))
                                return Read(br, false, manager);

                            // occurs on root level: object id is stored only after data type
                            if (isRoot)
                            {
                                if (manager.TryGetCachedObject(br, out cachedResult))
                                {
                                    Debug.Fail("Root level object is not expected in the cache");
                                    return cachedResult;
                                }
                            }

                            BinarySerializationOptions origOptions = collectionDescriptor == null
                                ? ReadOptions(br)
                                : collectionDescriptor.SerializationOptions;

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
                            return ReadObjectGraph(br, addToCache || isRoot, objType, manager, collectionDescriptor != null);
                        }

                    // raw structure
                    case DataTypes.RawStruct:
                        {
                            Type structType = collectionDescriptor == null
                                ? manager.ReadType(br)
                                : collectionDescriptor.GetElementType(isTValue);
                            byte[] rawData = br.ReadBytes(Read7BitInt(br));
                            result = BinarySerializer.DeserializeStruct(structType, rawData);
                            OnDeserializing(result);
                            OnDeserialized(result);
                            return result;
                        }

                    default:
                        // enum
                        if ((dataType & DataTypes.Enum) == DataTypes.Enum)
                        {
                            Type enumType = collectionDescriptor == null
                                ? manager.ReadType(br)
                                : collectionDescriptor.GetElementType(isTValue);
                            switch (dataType & DataTypes.SimpleTypes)
                            {
                                case DataTypes.Int8:
                                    return result = Enum.ToObject(enumType, br.ReadSByte());
                                case DataTypes.UInt8:
                                    return result = Enum.ToObject(enumType, br.ReadByte());
                                case DataTypes.Int16:
                                    return result = Enum.ToObject(enumType,
                                        is7BitEncoded ? (short)Read7BitInt(br) : br.ReadInt16());
                                case DataTypes.UInt16:
                                    return result = Enum.ToObject(enumType,
                                        is7BitEncoded ? (ushort)Read7BitInt(br) : br.ReadUInt16());
                                case DataTypes.Int32:
                                    return result = Enum.ToObject(enumType, is7BitEncoded ? Read7BitInt(br) : br.ReadInt32());
                                case DataTypes.UInt32:
                                    return result = Enum.ToObject(enumType,
                                        is7BitEncoded ? (uint)Read7BitInt(br) : br.ReadUInt32());
                                case DataTypes.Int64:
                                    return result = Enum.ToObject(enumType, is7BitEncoded ? Read7BitLong(br) : br.ReadInt64());
                                case DataTypes.UInt64:
                                    return result = Enum.ToObject(enumType,
                                        is7BitEncoded ? (ulong)Read7BitLong(br) : br.ReadUInt64());
                                default:
                                    throw new InvalidOperationException(Res.BinarySerializationInvalidEnumBase(ToString(dataType & DataTypes.SimpleTypes)));
                            }
                        }

                        throw new InvalidOperationException(Res.BinarySerializationCannotDeserializeObject(ToString(dataType)));
                }
            }
            finally
            {
                if (addToCache && result != null)
                    manager.AddObjectToCache(result);
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

        private object ReadBinarySerializable(BinaryReader br, bool addToCache, Type type, BinarySerializationOptions origOptions, DeserializationManager manager)
        {
            byte[] serData = br.ReadBytes(Read7BitInt(br));

            // Creating an uninitialized instance if OnSerializing methods are needed to be invoked before constructor
            object result = FormatterServices.GetUninitializedObject(type);
            if (addToCache)
                manager.AddObjectToCache(result);
            OnDeserializing(result);

            // Looking for a serializer constructor
            ConstructorInfo ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(BinarySerializationOptions), typeof(byte[]) }, null);
            if (ctor != null)
            {
                Reflector.InvokeCtor(result, ctor, origOptions, serData);
            }
            // No special constructor - Deserialize method should be executed
            else
            {
                // Looking for parameterless constructor
                ctor = type.GetDefaultConstructor();
                if (ctor != null)
                {
                    Reflector.InvokeCtor(result, ctor);
                }

                ((IBinarySerializable)result).Deserialize(origOptions, serData);
            }

            OnDeserialized(result);
            return result;
        }

        /// <summary>
        /// Deserializing object graph with options that was used on serialization.
        /// </summary>
        private object ReadObjectGraph(BinaryReader br, bool addToCache, Type type, DeserializationManager manager, bool refineType)
        {
            // a.) Graph method
            bool isDefaultObjectGraph = br.ReadBoolean();

            // b.) Possible type change
            if (!isDefaultObjectGraph && refineType && br.ReadBoolean())
                type = manager.ReadType(br);

            // c.) Reading members
            object result = FormatterServices.GetUninitializedObject(type);
            int id = 0;
            if (addToCache)
                manager.AddObjectToCache(result, out id);
            OnDeserializing(result);

            ISerializationSurrogate surrogate;
            ISurrogateSelector selector;
            bool useSurrogate = manager.TryGetSurrogate(type, out surrogate, out selector);
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
            if ((manager.Options & BinarySerializationOptions.IgnoreIObjectReference) == BinarySerializationOptions.None)
            {
                IObjectReference objRef = result as IObjectReference;
                if (objRef != null)
                {
                    result = objRef.GetRealObject(Context);
                    manager.UpdateReferences(objRef, result);
                    if (addToCache)
                        manager.ReplaceObjectInCache(id, result);
                }
            }

            return result;
        }

        private void ReadDefaultObjectGraph(BinaryReader br, object obj, DeserializationManager manager)
        {
            Type type = obj.GetType();

            // iterating through self and base types
            for (Type t = type; t != typeof(object); t = t.BaseType)
            {
                // checking name of base type
                if (t != type)
                {
                    string name = br.ReadString();
                    while (t.Name != name && t != typeof(object))
                    {
                        t = t.BaseType;
                    }

                    if (name.Length == 0 && t == typeof(object))
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
            if (br.ReadString() != String.Empty)
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
                while (br.ReadString() != String.Empty);
            }
        }

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
                Type elementType = value?.GetType() ?? typeof(object);
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

                Reflector.RunMethod(ci, "SerializationInvoke", obj, si, Context);
            }
            else
            {
                // Using surrogate
                object result = surrogate.SetObjectData(obj, si, Context, selector);
                if (obj != result)
                    throw new NotSupportedException(Res.BinarySerializationSurrogateChangedObject(type));
            }
        }

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
            } while (br.ReadString() != String.Empty);

            manager.CheckReferences(si);
            if (surrogate == null)
            {
                // As ISerializable: Invoking serialization constructor
                ConstructorInfo ci = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
                if (ci == null)
                    throw new SerializationException(Res.BinarySerializationMissingISerializableCtor(type));

                Reflector.RunMethod(ci, "SerializationInvoke", obj, si, Context);
            }
            else
            {
                // Using surrogate
                object result = surrogate.SetObjectData(obj, si, Context, selector);
                if (obj != result)
                    throw new NotSupportedException(Res.BinarySerializationSurrogateChangedObject(type));
            }
        }

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
                    value = Convert.ChangeType(value, elementType); // this is what FormatterConverter does as well on SerializationInfo.GetValue
                }

                elements[name] = value;
            }

            if (count == 0)
                return;

            bool checkFields = (manager.Options & BinarySerializationOptions.IgnoreObjectChanges) == BinarySerializationOptions.None;

            // iterating through fields and setting found elements
            for (Type t = obj.GetType(); t != Reflector.ObjectType; t = t.BaseType)
            {
                FieldInfo[] fields = BinarySerializer.GetSerializableFields(t);
                foreach (FieldInfo field in fields)
                {
                    //if (field.IsNotSerialized) TODO: enable when GetSerializableFields is removed
                    //    continue;
                    object value;
                    if (elements.TryGetValue(field.Name, out value))
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

        /// <summary>
        /// Converts a <see cref="DataTypes"/> enumeration into the corresponding string representation.
        /// This method is needed because <see cref="Enum.ToString()"/> and <see cref="Enum{TEnum}.ToString(TEnum)"/>
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

        #region IFormatter Members

        /// <summary>
        /// Gets or sets the <see cref="SerializationBinder"/> that performs type lookups.
        /// </summary>
        /// <returns>
        /// The <see cref="SerializationBinder"/> that performs type lookups.
        /// </returns>
        /// <remarks>
        /// From .NET 4.0 affects both serialization and deserialization. In .NET 3.5 setting this property
        /// has no effect during serialization.
        /// </remarks>
        public SerializationBinder Binder { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="StreamingContext"/> used for serialization and deserialization.
        /// </summary>
        /// <returns>
        /// The <see cref="StreamingContext"/> used for serialization and deserialization.
        /// </returns>
        public StreamingContext Context { get; set; }

        object IFormatter.Deserialize(Stream serializationStream)
        {
            return DeserializeFromStream(serializationStream);
        }

        void IFormatter.Serialize(Stream serializationStream, object graph)
        {
            SerializeToStream(serializationStream, graph);
        }

        /// <summary>
        /// Gets or sets the <see cref="SurrogateSelector"/> used by the current formatter.
        /// </summary>
        /// <returns>
        /// The <see cref="SurrogateSelector"/> used by this formatter.
        /// </returns>
        public ISurrogateSelector SurrogateSelector { get; set; }

        #endregion
    }
}
