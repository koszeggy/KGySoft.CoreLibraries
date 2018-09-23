using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace KGySoft.Serialization
{
    /// <summary>
    /// Options for serialization methods in <see cref="BinarySerializer"/> and <see cref="BinarySerializationFormatter"/> classes.
    /// </summary>
    /// <seealso cref="BinarySerializer"/>
    /// <seealso cref="BinarySerializationFormatter"/>
    /// <seealso cref="IBinarySerializable"/>
    [Flags]
    public enum BinarySerializationOptions
    {
        /// <summary>
        /// All options are disabled.
        /// </summary>
        None,

        // TODO: ForceRecursiveSerializationOfSupportedTypes = 1 // minden rekurzív, kivéve a legprimitívebbeket és az arrayt. Array elemeknél is figyelembe venni.

        /// <summary>
        /// This option makes possible to serialize <see cref="ValueType"/>s (<see langword="struct"/>) that are not marked by <see cref="SerializableAttribute"/>.
        /// <note type="caution">
        /// Caution warning: Never use this flag on a <see cref="ValueType"/> that has reference (non-value type) fields. Deserializing such value would result an invalid
        /// object with undetermined object references. Only string and array reference fields can be serialized safely if they are decorated by <see cref="MarshalAsAttribute"/> using
        /// <see cref="UnmanagedType.ByValTStr"/> or <see cref="UnmanagedType.ByValArray"/>, respectively.
        /// </note>
        /// <para>
        /// This flag is used at serialization.
        /// </para>
        /// <para>
        /// Default at serializer methods in <see cref="BinarySerializer"/>: Disabled
        /// </para>
        /// </summary>
        [Obsolete("Now RecursiveSerializationAsFallback works also for structs. To serialize structs in a compact format if possible, use CompactSerializationOfStructures flag instead.")]
        ForcedSerializationValueTypesAsFallback = 1 << 1,

        /// <summary>
        /// Makes possible to serialize any object even if object is not marked with <see cref="SerializableAttribute"/>.
        /// <para>
        /// This flag is used at serialization.
        /// <note>Unlike in case of <see cref="BinaryFormatter"/>, <see cref="BinarySerializationFormatter"/> does not check whether the deserialized object has <see cref="SerializableAttribute"/>.</note>
        /// </para>
        /// <para>
        /// Default at serializer methods in <see cref="BinarySerializer"/>: Enabled
        /// </para>
        /// </summary>
        RecursiveSerializationAsFallback = 1 << 2,

        /// <summary>
        /// If a type has methods decorated with <see cref="OnSerializingAttribute"/>, <see cref="OnSerializedAttribute"/>, <see cref="OnDeserializingAttribute"/> or <see cref="OnDeserializedAttribute"/>,
        /// or the type implements <see cref="IDeserializationCallback"/>, then these methods are called during the process. By setting this flag these methods can be ignored.
        /// <para>
        /// This flag is used both at serialization and deserialization.
        /// </para>
        /// <para>
        /// Default at serializer methods in <see cref="BinarySerializer"/>: Disabled
        /// </para>
        /// </summary>
        IgnoreSerializationMethods = 1 << 3, // current options on deserialization

        /// <summary>
        /// This flag ignores <see cref="IBinarySerializable"/> implementations.
        /// <para>
        /// This flag is used at serialization.
        /// </para>
        /// <para>
        /// Default at serializer methods in <see cref="BinarySerializer"/>: Disabled
        /// </para>
        /// </summary>
        IgnoreIBinarySerializable = 1 << 4,

        /// <summary>
        /// If enabled, type references will be stored without assembly identification. This can make possible
        /// to restore a type even if the version of the assembly has been modified since last serialization while makes serialized data more compact;
        /// however, it cannot be guaranteed that the correct type will be even found on deserialization.
        /// <note type="caution">
        /// If there are types with the same name in the same namespace in different assemblies, then by using this flag, these types cannot be distincted.
        /// </note>
        /// <note>
        /// If you want to deserialize a type that was stored with strong assembly reference (without this flag) from a different version of an assembly,
        /// then use <see cref="WeakAssemblySerializationBinder"/> instead.
        /// </note>
        /// <para>
        /// This flag is used at serialization.
        /// </para>
        /// <para>
        /// Default at serializer methods in <see cref="BinarySerializer"/>: Disabled
        /// </para>
        /// </summary>
        OmitAssemblyQualifiedNames = 1 << 5,

        /// <summary>
        /// This option makes possible to deserialize an object, which has been changed since last serialization.
        /// When this options is enabled, names of the base classes, and fields that have been serialized but have been since then
        /// removed, will be ignored.
        /// <note type="caution">When this flag is enabled, an erroneous deserialization may silently succeed.  When a field has
        /// been renamed or relocated into another base class,  use an <see cref="ISurrogateSelector"/> implementation to apply mappings instead.</note>
        /// <para>
        /// This flag is used at deserialization.
        /// </para>
        /// <para>
        /// Default at serializer methods in <see cref="BinarySerializer"/>: Disabled
        /// </para>
        /// </summary>
        IgnoreObjectChanges = 1 << 6,

        // (private) ExtendedFlags = 1 << 7
        // New flags must not cause incompatibility with old working. Note that options are stored only when object is IBinarySerializable or when is a recursive object.

        /// <summary>
        /// This flag ignores <see cref="ISerializable"/> implementations. Meaningful if combined with <see cref="RecursiveSerializationAsFallback"/> option.
        /// <para>
        /// This flag is used both at serialization and deserialization.
        /// <note>
        /// Usually this flag must have the same value at serialization and deserialization; otherwise, the deserialization could fail.
        /// </note>
        /// </para>
        /// <para>
        /// Default at serializer methods in <see cref="BinarySerializer"/>: Disabled
        /// </para>
        /// </summary>
        IgnoreISerializable = 1 << 8,

        /// <summary>
        /// This flag ignores <see cref="IObjectReference"/> implementations.
        /// <note>Using this flag may cause that the deserialized object or its elements will have the wrong type, or the deserialization will fail.</note>
        /// <para>
        /// This flag is used at deserialization.
        /// </para>
        /// <para>
        /// Default at serializer methods in <see cref="BinarySerializer"/>: Disabled
        /// </para>
        /// </summary>
        IgnoreIObjectReference = 1 << 9,

        /// <summary>
        /// If a <see cref="ValueType"/> (<see langword="struct"/>) contains no references,
        /// then by enabling this option the instance will be serialized in a compact way form if possible.
        /// <para>
        /// This flag is used at serialization.
        /// </para>
        /// <note>
        /// Note: This option is stronger than <see cref="RecursiveSerializationAsFallback"/> flag,
        /// except for natively supported structures.
        /// This option affects only instances that either has no reference fields at all, or has only string or array references, which are decorated by <see cref="MarshalAsAttribute"/> using
        /// <see cref="UnmanagedType.ByValTStr"/> or <see cref="UnmanagedType.ByValArray"/>, respectively.
        /// </note>
        /// <para>
        /// Default at serializer methods in <see cref="BinarySerializer"/>: Enabled
        /// </para>
        /// </summary>
        CompactSerializationOfStructures = 1 << 10,

        /// <summary>
        /// If this flag is enabled while <see cref="BinarySerializationFormatter.SurrogateSelector"/> is set, then the selector is tried to be used
        /// even for natively supported types.
        /// <para>
        /// This flag is used at serialization.
        /// </para>
        /// <para>
        /// Default at serializer methods in <see cref="BinarySerializer"/>: Disabled
        /// </para>
        /// </summary>
        TryUseSurrogateSelectorForAnyType = 1 << 11,

        //OmitEnumTypes = 1 << 12, - hidden field. Used when enum fields are serialized recursively

        // Rekurzív serializáláskor; deserializálásnál ellenőrzi, hogy minden field megvan-e (optionalt figyelembe véve). Ehhez használható a serializableFieldsCache
        //CheckFields == 1 << 13,

        // TODO: XML-ből mindig engedni, már ha lesz
        ///// <summary>
        ///// This flag allows to serialize supported collection types with <see cref="bool"/> element type in a compact way.
        ///// <para>
        ///// Default at serializer methods in <see cref="BinarySerializer"/>: Enabled
        ///// </para>
        ///// <para>
        ///// Default at serializer methods in <see cref="BinarySerializer"/>: Enabled
        ///// </para>
        ///// </summary>
        //CompactSerializationOfBoolCollections = 1 << 14,
        // vagy:
        //CompactSerializationOfCollections = 1 << 14, - bármilyen tömöríthető elemű collection-re

        // TODO: remove empty and hidden flags


    }
}
