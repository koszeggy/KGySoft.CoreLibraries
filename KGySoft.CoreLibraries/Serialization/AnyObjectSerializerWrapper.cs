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
#if !(NETCOREAPP2_0 || NETCOREAPP3_0 || NETSTANDARD2_0 || NETSTANDARD2_1)
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
    /// A wrapper class for serializing any kind of object, including the ones
    /// that are not marked with <see cref="SerializableAttribute"/> or which are not supported by <see cref="BinaryFormatter"/>.
    /// Can be useful when an object is needed to be serialized with <see cref="BinaryFormatter"/>.
    /// When this object is deserialized, the clone of the wrapped original object is returned.
    /// </summary>
    /// <remarks><para>Since <see cref="BinarySerializationFormatter"/> supports serialization of
    /// any class, this object is not necessarily needed when <see cref="BinarySerializationFormatter"/> is used.</para>
    /// <para>This class supports serialization of remote objects, too.</para></remarks>
    [Serializable]
    public sealed class AnyObjectSerializerWrapper : ISerializable, IObjectReference
    {
        #region Fields

        [NonSerialized]
        private readonly object obj;
        private readonly bool useWeakBinding;

        #endregion

        #region Constructors

        #region Public Constructors

        /// <summary>
        /// Creates a new instance of <see cref="AnyObjectSerializerWrapper"/> with
        /// the provided object to be serialized.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to serialize. Non-serializable, remote objects, and <see langword="null"/>&#160;instances are supported, too.</param>
        /// <param name="useWeakAssemblyBinding">When <see langword="true"/>, the assembly version of types does not need to match on deserialization.
        /// This makes possible to deserialize objects stored in different version of the original assembly.</param>
        public AnyObjectSerializerWrapper(object obj, bool useWeakAssemblyBinding)
        {
            this.obj = obj;
            useWeakBinding = useWeakAssemblyBinding;
        }

        #endregion

        #region Private Constructors

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters",
            Justification = "False alarm, serialization constructor has an exact signature.")]
        private AnyObjectSerializerWrapper(SerializationInfo info, StreamingContext context)
        {
            byte[] rawData = (byte[])info.GetValue("data", Reflector.ByteArrayType);
            BinarySerializationFormatter serializer = new BinarySerializationFormatter();
            if (info.GetBoolean("isWeak"))
                serializer.Binder = new WeakAssemblySerializationBinder();
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
            info.AddValue("isWeak", useWeakBinding);
            BinarySerializationFormatter serializer = new BinarySerializationFormatter();
#if !(NETCOREAPP2_0 || NETCOREAPP3_0 || NETSTANDARD2_0 || NETSTANDARD2_1)
            if (RemotingServices.IsTransparentProxy(obj))
                serializer.SurrogateSelector = new RemotingSurrogateSelector();
#endif
            info.AddValue("data", serializer.Serialize(obj));
        }

        [SecurityCritical]
        [SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase", Justification = "False alarm, SecurityCriticalAttribute is applied.")]
        object IObjectReference.GetRealObject(StreamingContext context) => obj;

        #endregion
    }
}
