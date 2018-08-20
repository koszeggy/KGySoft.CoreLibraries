#region Used namespaces

using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

#endregion

namespace KGySoft.Libraries.Serialization
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
        private object obj;
        private bool useWeakBinding;

        #endregion

        #region Constructors

        #region Public Constructors

        /// <summary>
        /// Creates a new instace of <see cref="AnyObjectSerializerWrapper"/> with
        /// the provided object to be serialized.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to serialize. Non-serializable, remote objects, and <see langword="null"/> instances are supported, too.</param>
        /// <param name="useWeakAssemblyBinding">When <see langword="true"/>, the assembly version of types does not need to match on deserialization.
        /// This makes possible to deserialize objects stored in different version of the original assembly.</param>
        public AnyObjectSerializerWrapper(object obj, bool useWeakAssemblyBinding)
        {
            this.obj = obj;
            useWeakBinding = useWeakAssemblyBinding;
        }

        #endregion

        #region Private Constructors

        private AnyObjectSerializerWrapper(SerializationInfo info, StreamingContext context)
        {
            byte[] rawData = (byte[])info.GetValue("data", typeof(byte[]));
            BinarySerializationFormatter serializer = new BinarySerializationFormatter();
            if (info.GetBoolean("isWeak"))
                serializer.Binder = new WeakAssemblySerializationBinder();
            obj = serializer.Deserialize(rawData);
        }

        #endregion

        #endregion

        #region Methods

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("isWeak", useWeakBinding);
            BinarySerializationFormatter serializer = new BinarySerializationFormatter();
            if (RemotingServices.IsTransparentProxy(obj))
                serializer.SurrogateSelector = new RemotingSurrogateSelector();
            info.AddValue("data", serializer.Serialize(obj));
        }

        object IObjectReference.GetRealObject(StreamingContext context)
        {
            return obj;
        }

        #endregion
    }
}
