﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationOptions.cs
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
#if !NET35
using System.Runtime.CompilerServices; 
#endif
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

#endregion

namespace KGySoft.Serialization.Binary
{
#if NET35
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

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
        /// <para>Apart from primitive types, strings and arrays forces to serialize every type recursively. If <see cref="BinarySerializationFormatter.SurrogateSelector"/> is set,
        /// then the surrogate selectors will be tried to used even for the supported types (as if <see cref="TryUseSurrogateSelectorForAnyType"/> was also enabled).
        /// <note>Even if this flag is enabled, non-serializable types will not be serialized automatically. Use the <see cref="RecursiveSerializationAsFallback"/> to
        /// enable serialization of such types.</note></para>
        /// <para>This flag is considered on serialization.</para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        ForceRecursiveSerializationOfSupportedTypes = 1,

        /// <summary>
        /// <para>This option makes possible to serialize <see cref="ValueType"/>s (<see langword="struct"/>) that are not marked by <see cref="SerializableAttribute"/>.
        /// <note type="caution">
        /// Using this flag allows serializing value types with reference (non-value type) fields by marshaling. Deserializing such value may fail if the <see cref="SafeMode"/> flag
        /// if enabled. To be able to serialize string and array reference fields they must be decorated by <see cref="MarshalAsAttribute"/> using
        /// <see cref="UnmanagedType.ByValTStr"/> or <see cref="UnmanagedType.ByValArray"/>, respectively.
        /// </note></para>
        /// <note>This option is obsolete. Use the <see cref="CompactSerializationOfStructures"/> flag instead.</note>
        /// <para>This flag is considered on serialization.</para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        [Obsolete("Now RecursiveSerializationAsFallback works also for structs. To serialize structs in a compact format if possible, use CompactSerializationOfStructures flag instead.")]
        ForcedSerializationValueTypesAsFallback = 1 << 1,

        /// <summary>
        /// <para>Makes possible to serialize any non-natively supported types if they are not marked by <see cref="SerializableAttribute"/>.</para>
        /// <para>This flag is considered on serialization.
        /// <note type="caution">Though this flag makes possible to serialize non-serializable types, deserializing such stream will not work
        /// when the <see cref="SafeMode"/> flag is enabled (unless the <see cref="AllowNonSerializableExpectedCustomTypes"/> option is also set).</note>
        /// </para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        RecursiveSerializationAsFallback = 1 << 2,

        /// <summary>
        /// <para>If a type has methods decorated by <see cref="OnSerializingAttribute"/>, <see cref="OnSerializedAttribute"/>, <see cref="OnDeserializingAttribute"/> or <see cref="OnDeserializedAttribute"/>,
        /// or the type implements <see cref="IDeserializationCallback"/>, then these methods are called during the process. By setting this flag these methods can be ignored.</para>
        /// <para>This flag is considered both on serialization and deserialization.</para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreSerializationMethods = 1 << 3,

        /// <summary>
        /// <para>This flag ignores <see cref="IBinarySerializable"/> implementations.</para>
        /// <para>This flag is considered on serialization.</para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreIBinarySerializable = 1 << 4,

        /// <summary>
        /// <para>If enabled, type references will be stored without assembly identification. This can make possible
        /// to restore a type even if the version of the assembly has been modified since last serialization while makes serialized data more compact;
        /// however, it cannot be guaranteed that the correct type will be even found on deserialization without specifying the expected types.
        /// <note type="caution">If there are types with the same name in the same namespace in different assemblies, then by using this flag, these types cannot be distinguished.
        /// If the expected types are not specified on deserialization, then any of the types with identical full names might be picked.</note>
        /// <note>If you want to deserialize a type that was stored with strong assembly identity (without this flag) from a different version of an assembly,
        /// then use the <see cref="ForwardedTypesSerializationBinder"/> or <see cref="WeakAssemblySerializationBinder"/> classes instead (the latter cannot be used in <see cref="SafeMode"/>).</note></para>
        /// <para>This flag is considered on serialization.</para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        OmitAssemblyQualifiedNames = 1 << 5,

        /// <summary>
        /// <para>This option makes possible to deserialize an object, which has been changed since last serialization.
        /// When this option is enabled, names of the base classes, and fields that have been serialized but have been since then
        /// removed, will be ignored.
        /// <note type="caution">When this flag is enabled, an erroneous deserialization may silently succeed. When a field has
        /// been renamed or relocated into another base class, use an <see cref="ISurrogateSelector"/> implementation to apply mappings instead
        /// (but note that surrogate selectors cannot be used in safe mode).</note></para>
        /// <para>This flag is considered on deserialization.</para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreObjectChanges = 1 << 6,

        /// <summary>
        /// <para>When this flag is enabled, every type will be serialized with its actual assembly identity rather than considering
        /// the value of an existing <see cref="TypeForwardedFromAttribute"/>.</para>
        /// <para>This flag is ignored if <see cref="OmitAssemblyQualifiedNames"/> is enabled.</para>
        /// <para>This flag is considered on serialization.
        /// <note>Enabling this flag may cause that the type will not be able to be deserialized on a different platform, or at least not without using a <see cref="SerializationBinder"/>.</note></para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreTypeForwardedFromAttribute = 1 << 7,

        /// <summary>
        /// <para>This flag ignores <see cref="ISerializable"/> implementations for natively not supported types,
        /// forcing to serialize a default object graph (unless an applicable surrogate selector is defined).</para>
        /// <para>This flag is considered both on serialization and deserialization.
        /// <note>Usually this flag must have the same value at serialization and deserialization; otherwise, the deserialization may fail.</note></para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreISerializable = 1 << 8,

        /// <summary>
        /// <para>This flag ignores <see cref="IObjectReference"/> implementations.
        /// <note>Using this flag may cause that the deserialized object or its elements will have the wrong type, or the deserialization will fail.</note></para>
        /// <para>This flag is considered on deserialization.</para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        IgnoreIObjectReference = 1 << 9,

        /// <summary>
        /// <para>If a <see cref="ValueType"/> (<see langword="struct"/>) contains no references,
        /// then by enabling this option the instance will be serialized in a compact way form if possible.</para>
        /// <para>This flag is considered on serialization.</para>
        /// <note>
        /// Note: This option has higher priority than <see cref="RecursiveSerializationAsFallback"/> flag,
        /// except for natively supported structures. This option affects only instances that have no references at all.
        /// </note>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Enabled</strong></para>
        /// </summary>
        CompactSerializationOfStructures = 1 << 10,

        /// <summary>
        /// <para>If this flag is enabled while <see cref="BinarySerializationFormatter.SurrogateSelector"/> is set, then the selector is tried to be used
        /// even for natively supported types.</para>
        /// <para>This flag is considered on serialization.</para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        TryUseSurrogateSelectorForAnyType = 1 << 11,

        /// <summary>
        /// <para>If this flag is enabled, then it is ensured that no assembly loading is allowed during deserialization. All of the assemblies that are referred
        /// by the serialization stream must be preloaded before starting the deserialization. Non-natively supported types, whose assembly qualified names are
        /// stored in the serialization stream must be explicitly declared as expected types in the deserialization methods, including <see langword="enum"/>s.</para>
        /// <para>Additionally, safe mode ensures that during the deserialization natively supported collections are allocated with limited capacity to prevent
        /// possible attacks that can cause <see cref="OutOfMemoryException"/>. Deserializing an invalid stream still may cause to throw a <see cref="SerializationException"/>.</para>
        /// <para>It also disallows deserializing the natively not supported non-serializable types, though this can be relaxed by enabling
        /// the <see cref="AllowNonSerializableExpectedCustomTypes"/> flag.</para>
        /// <note>In safe mode no version mismatch is tolerated even for system assemblies. If you want to deserialize a stream in safe mode that contains
        /// different assembly identities from the loaded ones, then use <see cref="ForwardedTypesSerializationBinder"/>, and set
        /// its <see cref="ForwardedTypesSerializationBinder.SafeMode"/> property to <see langword="true"/>.</note>
        /// <note type="security">In safe mode it is not allowed to set the <see cref="BinarySerializationFormatter.SurrogateSelector"/>
        /// or <see cref="BinarySerializationFormatter.Binder"/> properties (except for using the <see cref="ForwardedTypesSerializationBinder"/> when
        /// its <see cref="ForwardedTypesSerializationBinder.SafeMode"/> is set to <see langword="true"/>)
        /// because they could be used to weaken the security actions described above.</note>
        /// <para>This flag is considered on deserialization.</para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Enabled</strong></para>
        /// </summary>
        SafeMode = 1 << 12,

        /// <summary>
        /// <para>Indicates that recursively serialized types that are indicated as expected types at the deserialization methods should be able to be
        /// deserialized in <see cref="SafeMode"/> even if they are not marked by the <see cref="SerializableAttribute"/>.</para>
        /// <para>This flag is considered on deserialization.</para>
        /// <para>Default state at serialization methods in <see cref="BinarySerializer"/>: <strong>Disabled</strong></para>
        /// </summary>
        AllowNonSerializableExpectedCustomTypes = 1 << 13,
    }
}
