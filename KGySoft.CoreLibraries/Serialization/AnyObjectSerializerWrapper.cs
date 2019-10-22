#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AnyObjectSerializerWrapper.cs
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
using System.Diagnostics.CodeAnalysis;
#if NETFRAMEWORK
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
# endif
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization
{
    /// <summary>
    /// Provides a wrapper class for serializing any kind of object, including the ones
    /// that are not marked by the <see cref="SerializableAttribute"/>, or which are not supported by <see cref="BinaryFormatter"/>.
    /// Can be useful when an object cannot be serialized by <see cref="BinarySerializationFormatter"/> so a <see cref="BinaryFormatter"/> must be used.
    /// When this object is deserialized, the clone of the wrapped original object is returned.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks><para>Since <see cref="BinarySerializationFormatter"/> supports serialization of
    /// any class, this object is not necessarily needed when <see cref="BinarySerializationFormatter"/> is used.</para>
    /// <para>In .NET Framework this class supports serialization of remote objects, too.</para>
    /// <note type="warning">
    /// <para>This class cannot guarantee that an object serialized in a framework can be deserialized in another one.
    /// For such cases some text-based serialization might be better (see also the <see cref="XmlSerializer"/>).</para>
    /// <para>In .NET Core the <see cref="ISerializable"/> implementation of some types throw a <see cref="PlatformNotSupportedException"/>.
    /// For such cases setting the <c>forceSerializationByFields</c> in the constructor can be a solution.</para>
    /// <para>For a more flexible customization use the <see cref="CustomSerializerSurrogateSelector"/> class instead.</para>
    /// </note>
    /// </remarks>
    [Serializable]
    public sealed class AnyObjectSerializerWrapper : ISerializable, IObjectReference
    {
        #region Fields

        [NonSerialized]
        private readonly object obj;
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
        /// field-based serialization. Can be useful for types whose <see cref="ISerializable"/> implementation throw a <see cref="PlatformNotSupportedException"/> on
        /// .NET Core, for example; though it does not guarantee that the object will be deserializable on another platform. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        public AnyObjectSerializerWrapper(object obj, bool useWeakAssemblyBinding, bool forceSerializationByFields = false)
        {
            this.obj = obj;
            isWeak = useWeakAssemblyBinding;
            byFields = forceSerializationByFields;
        }

        #endregion

        #region Private Constructors

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters",
            Justification = "False alarm, serialization constructor has an exact signature.")]
        private AnyObjectSerializerWrapper(SerializationInfo info, StreamingContext context)
        {
            byte[] rawData = (byte[])info.GetValue("data", Reflector.ByteArrayType);
            BinarySerializationFormatter serializer = new BinarySerializationFormatter();
            if (info.GetBoolean(nameof(isWeak)))
                serializer.Binder = new WeakAssemblySerializationBinder();
            if (info.GetValueOrDefault<bool>(nameof(byFields)))
                serializer.SurrogateSelector = new CustomSerializerSurrogateSelector { IgnoreISerializable = true };
            obj = serializer.Deserialize(rawData);
        }

        #endregion

        #endregion

        #region Methods

        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info), Res.ArgumentNull);
            BinarySerializationFormatter serializer = new BinarySerializationFormatter();
            ISurrogateSelector surrogate = null;
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
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
        object IObjectReference.GetRealObject(StreamingContext context) => obj;

        #endregion
    }
}
