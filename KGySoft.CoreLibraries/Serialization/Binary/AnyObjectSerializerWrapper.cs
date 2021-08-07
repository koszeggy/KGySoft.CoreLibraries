#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AnyObjectSerializerWrapper.cs
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
#if NETFRAMEWORK
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#endif
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;

using KGySoft.Reflection;
using KGySoft.Serialization.Xml;

#endregion

#region Suppressions

#if NETCOREAPP3_0_OR_GREATER
#pragma warning disable CS8768 // Nullability of return type does not match implemented member - BinarySerializationFormatter supports de/serializing null
#endif

#endregion

namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Provides a wrapper class for serializing any kind of object, including the ones
    /// that are not marked by the <see cref="SerializableAttribute"/>, or which are not supported by <see cref="BinaryFormatter"/>.
    /// Can be useful when a <see cref="BinarySerializationFormatter"/> payload cannot be used, so a <see cref="BinaryFormatter"/>-compatible stream must be produced.
    /// When this object is deserialized, the clone of the wrapped original object is returned.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <note type="security">
    /// <para>This type has been made obsolete because just from the stream to deserialize it cannot be determined whether the consumer formatter
    /// is used in a safe context. Therefore <see cref="AnyObjectSerializerWrapper"/> deserialization uses safe mode,
    /// which denies deserializing non-serializable types. It renders this type practically useless, but it was
    /// meant for <see cref="BinaryFormatter"/> anyway, which is also being obsoleted in upcoming .NET versions. To serialize
    /// non-serializable types you still can use <see cref="BinarySerializationFormatter"/>, which now supports <see cref="BinarySerializationOptions.SafeMode"/>,
    /// which should be enabled when deserializing anything from an untrusted source.</para>
    /// <para>When deserializing a stream that has an <see cref="AnyObjectSerializerWrapper"/> reference, it is ensured that no assemblies
    /// are loaded while unwrapping its content (it may not be true for other entries in the serialization stream, if the formatter is a <see cref="BinaryFormatter"/>, for example).
    /// Therefore all of the assemblies that are involved by the types wrapped into an <see cref="AnyObjectSerializerWrapper"/> must be preloaded before deserializing such a stream.</para>
    /// <para>See the security notes at the <strong>Remarks</strong> section of the <see cref="BinarySerializationFormatter"/> class for more details.</para></note>
    /// <para>Since <see cref="BinarySerializationFormatter"/> supports serialization of
    /// any class, this object is not necessarily needed when <see cref="BinarySerializationFormatter"/> is used.</para>
    /// <para>In .NET Framework this class supports serialization of remote objects, too.</para>
    /// <note type="warning"><para>This class cannot guarantee that an object serialized in one platform can be deserialized in another one.
    /// For such cases some text-based serialization might be better (see also the <see cref="XmlSerializer"/>).</para>
    /// <para>In .NET Core and above the <see cref="ISerializable"/> implementation of some types throw a <see cref="PlatformNotSupportedException"/>.
    /// For such cases setting the <c>forceSerializationByFields</c> in the constructor can be a solution.</para>
    /// <para>For a more flexible customization use the <see cref="CustomSerializerSurrogateSelector"/> class instead.</para></note>
    /// </remarks>
    [Serializable]
    [Obsolete("This type cannot be used anymore to make any type serializable by BinaryFormatter due to security reasons. " +
        "Use BinarySerializationFormatter instead, whose entire deserialization can work in safe mode if needed.")]
    public sealed class AnyObjectSerializerWrapper : ISerializable, IObjectReference
    {
        #region Fields

        [NonSerialized]private readonly object? obj;
        private readonly bool isWeak;
        private readonly bool byFields;

        #endregion

        #region Constructors

        #region Public Constructors

        /// <summary>
        /// Creates a new instance of <see cref="AnyObjectSerializerWrapper"/> with
        /// the provided object to be serialized.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to serialize. Non-serializable, remote objects, and <see langword="null"/>&#160;instances are supported, too.</param>
        /// <param name="useWeakAssemblyBinding">When <see langword="true"/>, the assembly versions of types do not need to match on deserialization.
        /// This makes possible to deserialize objects stored in different versions of the original assembly.</param>
        /// <param name="forceSerializationByFields"><see langword="true"/>&#160;to ignore <see cref="ISerializable"/> and <see cref="IObjectReference"/> implementations
        /// as well as serialization constructors and serializing methods; <see langword="false"/>&#160;to consider all of these techniques instead performing a forced
        /// field-based serialization. Can be useful for types that implement <see cref="ISerializable"/> but the implementation throws a <see cref="PlatformNotSupportedException"/>
        /// (on .NET Core, for example). It still does not guarantee that the object will be deserializable on another platform. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        public AnyObjectSerializerWrapper(object? obj, bool useWeakAssemblyBinding, bool forceSerializationByFields = false)
        {
            this.obj = obj;
            isWeak = useWeakAssemblyBinding;
            byFields = forceSerializationByFields;
        }

        #endregion

        #region Private Constructors

        private AnyObjectSerializerWrapper(SerializationInfo info, StreamingContext context)
        {
            byte[] rawData = (byte[])info.GetValue("data", Reflector.ByteArrayType)!;
            var serializer = new BinarySerializationFormatter(BinarySerializationOptions.SafeMode);
            if (info.GetBoolean(nameof(isWeak)))
                serializer.Binder = new WeakAssemblySerializationBinder { SafeMode = true };
            if (info.GetValueOrDefault<bool>(nameof(byFields)))
                serializer.SurrogateSelector = new CustomSerializerSurrogateSelector { IgnoreISerializable = true, SafeMode = true };
            obj = serializer.Deserialize(rawData);
        }

        #endregion

        #endregion

        #region Methods

        [SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null!)
                Throw.ArgumentNullException(Argument.info);
            BinarySerializationFormatter serializer = new BinarySerializationFormatter();
            ISurrogateSelector? surrogate = null;
#if NETFRAMEWORK
            // ReSharper disable once LocalVariableHidesMember - intended, in non-Framework platforms the field is used.
            bool byFields = this.byFields;
            if (RemotingServices.IsTransparentProxy(obj))
            {
                surrogate = new RemotingSurrogateSelector();
                byFields = false;
            }
            else
#endif
            if (byFields)
            {
                serializer.Options |= BinarySerializationOptions.IgnoreSerializationMethods | BinarySerializationOptions.IgnoreIObjectReference | BinarySerializationOptions.IgnoreIBinarySerializable;
                surrogate = new CustomSerializerSurrogateSelector { IgnoreISerializable = true, IgnoreNonSerializedAttribute = true };
            }

            serializer.SurrogateSelector = surrogate;
            info.AddValue(nameof(isWeak), isWeak);
            info.AddValue(nameof(byFields), byFields);
            info.AddValue("data", serializer.Serialize(obj));
        }

        [SecurityCritical]
        object? IObjectReference.GetRealObject(StreamingContext context) => obj;

        #endregion
    }
}
