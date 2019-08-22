#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationOptions.cs
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
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

#endregion

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

        /// <summary>
        /// Apart from primitive types, strings and arrays forces to serialize every type recursively. If <see cref="BinarySerializationFormatter.SurrogateSelector"/> is set,
        /// then the surrogate selectors will be tried to used even for the supported types (as if <see cref="TryUseSurrogateSelectorForAnyType"/> was also enabled).
        /// <para>This flag is considered on serialization.</para>
        /// <para>Default at serializer methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        ForceRecursiveSerializationOfSupportedTypes = 1,

        /// <summary>
        /// <para>This option makes possible to serialize <see cref="ValueType"/>s (<see langword="struct"/>) that are not marked by <see cref="SerializableAttribute"/>.
        /// <note type="caution">
        /// Never use this flag on a <see cref="ValueType"/> that has reference (non-value type) fields. Deserializing such value would result an invalid
        /// object with undetermined object references. Only string and array reference fields can be serialized safely if they are decorated by <see cref="MarshalAsAttribute"/> using
        /// <see cref="UnmanagedType.ByValTStr"/> or <see cref="UnmanagedType.ByValArray"/>, respectively.
        /// </note></para>
        /// <para>This flag is considered on serialization.</para>
        /// <para>Default at serializer methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        [Obsolete("Now RecursiveSerializationAsFallback works also for structs. To serialize structs in a compact format if possible, use CompactSerializationOfStructures flag instead.")]
        ForcedSerializationValueTypesAsFallback = 1 << 1,

        /// <summary>
        /// <para>Makes possible to serialize any object even if object is not marked with <see cref="SerializableAttribute"/>.</para>
        /// <para>This flag is considered on serialization.
        /// <note>Unlike in case of <see cref="BinaryFormatter"/>, <see cref="BinarySerializationFormatter"/> does not check whether the deserialized object has <see cref="SerializableAttribute"/>.</note>
        /// </para>
        /// <para>Default at serializer methods in <see cref="BinarySerializer"/>: <strong>Enabled</strong></para>
        /// </summary>
        RecursiveSerializationAsFallback = 1 << 2,

        /// <summary>
        /// <para>If a type has methods decorated with <see cref="OnSerializingAttribute"/>, <see cref="OnSerializedAttribute"/>, <see cref="OnDeserializingAttribute"/> or <see cref="OnDeserializedAttribute"/>,
        /// or the type implements <see cref="IDeserializationCallback"/>, then these methods are called during the process. By setting this flag these methods can be ignored.</para>
        /// <para>This flag is considered both on serialization and deserialization.</para>
        /// <para>Default at serializer methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreSerializationMethods = 1 << 3,

        /// <summary>
        /// <para>This flag ignores <see cref="IBinarySerializable"/> implementations.</para>
        /// <para>This flag is considered on serialization.</para>
        /// <para>Default at serializer methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreIBinarySerializable = 1 << 4,

        /// <summary>
        /// <para>If enabled, type references will be stored without assembly identification. This can make possible
        /// to restore a type even if the version of the assembly has been modified since last serialization while makes serialized data more compact;
        /// however, it cannot be guaranteed that the correct type will be even found on deserialization.
        /// <note type="caution">If there are types with the same name in the same namespace in different assemblies, then by using this flag, these types cannot be distincted.</note>
        /// <note>If you want to deserialize a type that was stored with strong assembly reference (without this flag) from a different version of an assembly,
        /// then use <see cref="WeakAssemblySerializationBinder"/> instead.</note></para>
        /// <para>This flag is considered on serialization.</para>
        /// <para>Default at serializer methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        OmitAssemblyQualifiedNames = 1 << 5,

        /// <summary>
        /// <para>If a <see cref="ValueType"/> (<see langword="struct"/>) contains no references,
        /// then by enabling this option the instance will be serialized in a compact way form if possible.</para>
        /// <para>This flag is considered on serialization.</para>
        /// <note>
        /// Note: This option has higher priority than <see cref="RecursiveSerializationAsFallback"/> flag,
        /// except for natively supported structures.
        /// This option affects only instances that have either no reference fields at all or have only string or array references, which are decorated by <see cref="MarshalAsAttribute"/> using
        /// <see cref="UnmanagedType.ByValTStr"/> or <see cref="UnmanagedType.ByValArray"/>, respectively.
        /// </note>
        /// <para>Default at serializer methods in <see cref="BinarySerializer"/>: <strong>Enabled</strong></para>
        /// </summary>
        CompactSerializationOfStructures = 1 << 6,

        // Hidden flag: indicates that the options are stored on two bytes
        // (private) ExtendedOptions = 1 << 7 // defined in SerializationManagerBase

        // New flags must be incompatible with single byte version. Note that options are stored only when object is IBinarySerializable or when is a recursive object.

        /// <summary>
        /// <para>This flag ignores <see cref="ISerializable"/> implementations forcing to serialize a default object graph (unless an applicable surrogate is defined).</para>
        /// <para>This flag is considered both on serialization and deserialization.
        /// <note>Usually this flag must have the same value at serialization and deserialization; otherwise, the deserialization may fail.</note></para>
        /// <para>Default at serializer methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreISerializable = 1 << 8,

        /// <summary>
        /// <para>This flag ignores <see cref="IObjectReference"/> implementations.
        /// <note>Using this flag may cause that the deserialized object or its elements will have the wrong type, or the deserialization will fail.</note></para>
        /// <para>This flag is considered on deserialization.</para>
        /// <para>Default at serializer methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreIObjectReference = 1 << 9,

        /// <summary>
        /// <para>This option makes possible to deserialize an object, which has been changed since last serialization.
        /// When this options is enabled, names of the base classes, and fields that have been serialized but have been since then
        /// removed, will be ignored.
        /// <note type="caution">When this flag is enabled, an erroneous deserialization may silently succeed. When a field has
        /// been renamed or relocated into another base class,  use an <see cref="ISurrogateSelector"/> implementation to apply mappings instead.</note></para>
        /// <para>This flag is considered on deserialization.</para>
        /// <para>Default at serializer methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreObjectChanges = 1 << 10,

        /// <summary>
        /// <para>If this flag is enabled while <see cref="BinarySerializationFormatter.SurrogateSelector"/> is set, then the selector is tried to be used
        /// even for natively supported types.</para>
        /// <para>This flag is considered on serialization.</para>
        /// <para>Default at serializer methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        TryUseSurrogateSelectorForAnyType = 1 << 11,
    }
}
