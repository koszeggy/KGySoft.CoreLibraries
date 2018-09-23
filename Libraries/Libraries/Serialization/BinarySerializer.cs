using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using KGySoft.Collections;

namespace KGySoft.Libraries.Serialization
{
    using KGySoft.Libraries.Resources;

    /// <summary>
    /// Provides public static methods for binary serialization. Exception with struct serialization routines,
    /// serialization is performed by a <see cref="BinarySerializationFormatter"/> instance.
    /// See description of <see cref="BinarySerializationFormatter"/> for further details.
    /// </summary>
    /// <seealso cref="BinarySerializationFormatter"/>
    /// <seealso cref="BinarySerializationOptions"/>
    /// <seealso cref="IBinarySerializable"/>
    public static class BinarySerializer
    {
        #region Fields

        internal const BinarySerializationOptions DefaultOptions = BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures; //| BinarySerializationOptions.CompactSerializationOfBoolCollections;

        private static readonly Cache<Type, FieldInfo[]> serializableFieldsCache = new Cache<Type, FieldInfo[]>(t => t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(f => !f.IsNotSerialized).OrderBy(f => f.MetadataToken).ToArray(), 1024);

        #endregion

        #region Public methods

        #region Object serialization

        /// <summary>
        /// Serializes an object into a byte array.
        /// </summary>
        /// <param name="data">The object to serialize</param>
        /// <param name="options">Options of the serialization.</param>
        /// <returns>Serialized raw data of the object</returns>
        public static byte[] Serialize(object data, BinarySerializationOptions options)
        {
            return new BinarySerializationFormatter(options).Serialize(data);
        }

        /// <summary>
        /// Serializes an object into a byte array.
        /// </summary>
        /// <param name="data">The object to serialize</param>
        /// <returns>Serialized raw data of the object</returns>
        /// <overloads>The one-parameter version of this method uses <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> and <see cref="BinarySerializationOptions.CompactSerializationOfStructures"/> options.
        /// To override this, use the <see cref="Serialize(object,KGySoft.Libraries.Serialization.BinarySerializationOptions)"/> overload.</overloads>
        public static byte[] Serialize(object data)
        {
            return Serialize(data, DefaultOptions);
        }

        /// <summary>
        /// Deserializes the specified part of a byte array into an object.
        /// </summary>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="offset">Points to the starting position of the object data in <paramref name="rawData"/>.</param>
        /// <returns>The deserialized data.</returns>
        /// <overloads>In the two-parameter overload the start offset of the data to deserialize can be specified.</overloads>
        public static object Deserialize(byte[] rawData, int offset)
        {
            return new BinarySerializationFormatter().Deserialize(rawData, offset);
        }

        /// <summary>
        /// Deserializes a byte array into an object.
        /// </summary>
        /// <param name="rawData">The raw data representation of the object to deserialize.</param>
        /// <returns>The deserialized data.</returns>
        public static object Deserialize(byte[] rawData)
        {
            return new BinarySerializationFormatter().Deserialize(rawData, 0);
        }

        /// <summary>
        /// Serializes the given <paramref name="data"/> into a <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream, into which the data is written. The stream must support writing and will remain open after serialization.</param>
        /// <param name="data">The data that will be written into the stream.</param>
        /// <param name="options">Options of the serialization.</param>
        /// <overloads>The one-parameter version of this method uses <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> and <see cref="BinarySerializationOptions.CompactSerializationOfStructures"/> options.
        /// To override this, use the <see cref="SerializeToStream(System.IO.Stream,object,KGySoft.Libraries.Serialization.BinarySerializationOptions)"/> overload.</overloads>
        public static void SerializeToStream(Stream stream, object data, BinarySerializationOptions options)
        {
            new BinarySerializationFormatter(options).SerializeToStream(stream, data);
        }

        /// <summary>
        /// Serializes the given <paramref name="data"/> into a <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream, into which the data is written. The stream must support writing and will remain open after serialization.</param>
        /// <param name="data">The data that will be written into the stream.</param>
        public static void SerializeToStream(Stream stream, object data)
        {
            SerializeToStream(stream, data, DefaultOptions);
        }

        /// <summary>
        /// Deserializes data beginning at current position of given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream, from which the data is read. The stream must support reading and will remain open after deserialization.</param>
        /// <returns>The deserialized data.</returns>
        public static object DeserializeFromStream(Stream stream)
        {
            return new BinarySerializationFormatter().DeserializeFromStream(stream);
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
        /// <param name="options">Options of the serialization.</param>
        /// <overloads>The one-parameter version of this method uses <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/> and <see cref="BinarySerializationOptions.CompactSerializationOfStructures"/> options.
        /// To override this, use the <see cref="SerializeByWriter(System.IO.BinaryWriter,object,KGySoft.Libraries.Serialization.BinarySerializationOptions)"/> overload.</overloads>
        public static void SerializeByWriter(BinaryWriter writer, object data, BinarySerializationOptions options)
        {
            new BinarySerializationFormatter(options).SerializeByWriter(writer, data);
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
        public static void SerializeByWriter(BinaryWriter writer, object data)
        {
            SerializeByWriter(writer, data, DefaultOptions);
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
        public static object DeserializeByReader(BinaryReader reader)
        {
            return new BinarySerializationFormatter().DeserializeByReader(reader);
        }

        #endregion

        #region Struct serialization

        // fast and unsafe solution:
        //public static T ReadUsingMarshalUnsafe<T>(byte[] data) where T : struct
        //{
        //    unsafe
        //    {
        //        fixed (byte* p = &data[0])
        //        {
        //            return (T)Marshal.PtrToStructure(new IntPtr(p), typeof(T));
        //        }
        //    }
        //}

        //public unsafe static byte[] WriteUsingMarshalUnsafe<selectedT>(selectedT structure) where selectedT : struct
        //{
        //    byte[] byteArray = new byte[Marshal.SizeOf(structure)];
        //    fixed (byte* byteArrayPtr = byteArray)
        //    {
        //        Marshal.StructureToPtr(structure, (IntPtr)byteArrayPtr, true);
        //    }
        //    return byteArray;
        //}

        /// <summary>
        /// Serializes a <see cref="ValueType"/> into a byte array.
        /// </summary>
        /// <param name="obj">The <see cref="ValueType"/> object to serialize.</param>
        /// <returns>The byte array representation of the <see cref="ValueType"/> object.</returns>
        /// <remarks>
        /// <note type="caution">
        /// Caution warning: Never call this method on a <see cref="ValueType"/> that has reference (non-value type) fields. Deserializing such value would result an invalid
        /// object with undetermined object references. Only string and array reference fields can be serialized safely if they are decorated by <see cref="MarshalAsAttribute"/> using
        /// <see cref="UnmanagedType.ByValTStr"/> or <see cref="UnmanagedType.ByValArray"/>, respectively.
        /// </note>
        /// </remarks>
        public static byte[] SerializeStruct(ValueType obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), Res.Get(Res.ArgumentNull));
            byte[] rawdata = new byte[Marshal.SizeOf(obj)];
            GCHandle handle = GCHandle.Alloc(rawdata, GCHandleType.Pinned);
            try
            {
                Marshal.StructureToPtr(obj, handle.AddrOfPinnedObject(), false);
            }
            finally
            {
                handle.Free();
            }
            return rawdata;
        }

        /// <summary>
        /// Tries to serialize a <see cref="ValueType"/> into a byte array.
        /// </summary>
        /// <param name="obj">The <see cref="ValueType"/> object to serialize.</param>
        /// <param name="result">The byte array representation of the <see cref="ValueType"/> object.</param>
        /// <returns><see langword="true"/>, if serialization was successful; otherwise, <see langword="false"/>.</returns>
        public static bool TrySerializeStruct(ValueType obj, out byte[] result)
        {
            result = null;

            if (obj == null)
                throw new ArgumentNullException(nameof(obj), Res.Get(Res.ArgumentNull));
            if (CanSerializeStruct(obj.GetType()))
            {
                try
                {
                    result = SerializeStruct(obj);
                }
                catch (Exception)
                {
                    // CanSerializeStruct filters a sort of conditions but serialization may fail even in that case - this catch is to protect this case.
                    return false;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Serializes an <see cref="Array"/> of <see cref="ValueType"/>s into a byte array.
        /// </summary>
        /// <param name="array">The array to serialize.</param>
        /// <typeparam name="T">Element type of the array. Must be a <see cref="ValueType"/>.</typeparam>
        /// <returns>The byte array representation of the <paramref name="array"/>.</returns>
        /// <remarks>
        /// <note>
        /// For primitive element types, use <see cref="Buffer.BlockCopy"/> instead for better performance.
        /// </note>
        /// <note type="caution">
        /// Caution warning: Never call this method on a <typeparamref name="T"/> that has reference (non-value type) fields. Deserializing such value would result an invalid
        /// object with undetermined object references. Only string and array reference fields can be serialized safely if they are decorated by <see cref="MarshalAsAttribute"/> using
        /// <see cref="UnmanagedType.ByValTStr"/> or <see cref="UnmanagedType.ByValArray"/>, respectively.
        /// </note>
        /// </remarks>
        public static byte[] SerializeStructArray<T>(T[] array) where T : struct
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), Res.Get(Res.ArgumentNull));
            if (array.Length == 0)
                return new byte[0];

            byte[] rawData = new byte[Marshal.SizeOf(typeof(T)) * array.Length];
            GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(handle.AddrOfPinnedObject(), rawData, 0, rawData.Length);
            }
            finally
            {
                handle.Free();
            }

            return rawData;
        }

        /// <summary>
        /// Tries to serialize an <see cref="Array"/> of <see cref="ValueType"/>s into a byte array.
        /// </summary>
        /// <param name="array">The array to serialize.</param>
        /// <typeparam name="T">Element type of the array. Must be a <see cref="ValueType"/>.</typeparam>
        /// <param name="result">The byte array representation of the <paramref name="array"/>.</param>
        /// <returns><see langword="true"/>, if serialization was successful; otherwise, <see langword="false"/>.</returns>
        public static bool TrySerializeStructArray<T>(T[] array, out byte[] result) where T : struct
        {
            result = null;

            if (array == null)
                throw new ArgumentNullException(nameof(array), Res.Get(Res.ArgumentNull));
            if (array.Length == 0)
            {
                result = new byte[0];
                return true;
            }

            if (!CanSerializeStruct(typeof(T)))
                return false;

            try
            {
                result = SerializeStructArray(array);
            }
            catch (Exception)
            {
                // CanSerializeStruct filters a sort of conditions but serialization may fail even in that case - this catch is to protect this case.
                return false;
            }

            return true;
        }


        internal static bool CanSerializeStruct(Type type)
        {
            HashSet<FieldInfo> fields = new HashSet<FieldInfo>(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));

            // adding private fields from base types
            while (type.BaseType != null)
            {
                type = type.BaseType;
                foreach (FieldInfo field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (!fields.Contains(field))
                        fields.Add(field);
                }
            }

            // checking fields
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType.IsValueType)
                {
                    if (field.FieldType.IsPrimitive)
                        continue;
                    if (!CanSerializeStruct(field.FieldType))
                        return false;
                }
                else if (field.FieldType.IsArray || field.FieldType == typeof(string))
                {
                    object[] attrs = field.GetCustomAttributes(typeof(MarshalAsAttribute), false);
                    MarshalAsAttribute marshalAs = attrs.Length > 0 ? attrs[0] as MarshalAsAttribute : null;
                    if (marshalAs != null && (field.FieldType.IsArray && marshalAs.Value == UnmanagedType.ByValArray ||
                                              field.FieldType == typeof(string) && marshalAs.Value == UnmanagedType.ByValTStr))
                    {
                        continue;
                    }
                    return false;
                }
                else
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Deserializes a <see cref="ValueType"/> object from a byte array that was previously serialized by <see cref="SerializeStruct"/> method.
        /// </summary>
        /// <param name="type">The type of the target object. Must be a <see cref="ValueType"/>.</param>
        /// <param name="data">The byte array that starts with byte representation of the object.</param>
        /// <returns>The deserialized <see cref="ValueType"/> object.</returns>
        public static object DeserializeStruct(Type type, byte[] data)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull));
            if (!type.IsValueType)
                throw new ArgumentException(Res.Get(Res.ValueTypeExpected), nameof(type));
            if (data == null)
                throw new ArgumentNullException(nameof(data), Res.Get(Res.ArgumentNull));
            if (data.Length < Marshal.SizeOf(type))
                throw new ArgumentException(Res.Get(Res.DataLenghtTooSmall), nameof(data));

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure(handle.AddrOfPinnedObject(), type);
            }
            finally
            {
                handle.Free();
            }
        }

        /// <summary>
        /// Deserializes a <see cref="ValueType"/> object from a byte array that was previously serialized by <see cref="SerializeStruct"/> method
        /// beginning on a specified <paramref name="offset"/>.
        /// </summary>
        /// <param name="type">The type of the target object. Must be a <see cref="ValueType"/>.</param>
        /// <param name="data">The byte array that contains the byte representation of the object.</param>
        /// <param name="offset">The offset that points to the beginning of the serialized data.</param>
        /// <returns>The deserialized <see cref="ValueType"/> object.</returns>
        public static object DeserializeStruct(Type type, byte[] data, int offset)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull));
            if (!type.IsValueType)
                throw new ArgumentException(Res.Get(Res.ValueTypeExpected), nameof(type));
            if (data == null)
                throw new ArgumentNullException(nameof(data), Res.Get(Res.ArgumentNull));

            int len = Marshal.SizeOf(type);
            if (data.Length < len)
                throw new ArgumentException(Res.Get(Res.DataLenghtTooSmall), nameof(data));
            if (data.Length - offset < len || offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), Res.Get(Res.ArgumentOutOfRange));

            IntPtr p = Marshal.AllocHGlobal(len);
            try
            {
                Marshal.Copy(data, offset, p, len);
                return Marshal.PtrToStructure(p, type);
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        /// <summary>
        /// Deserializes an array of <see cref="ValueType"/> objects (structures) from a byte array
        /// that was previously serialized by <see cref="SerializeStructArray{T}"/> method.
        /// </summary>
        /// <typeparam name="T">Type of the elements in the deserialized array. Must be a <see cref="ValueType"/>.</typeparam>
        /// <param name="data">The byte array that contains the byte representation of the structures.</param>
        /// <param name="offset">The offset that points to the beginning of the serialized data.</param>
        /// <param name="count">Number of elements to deserialize from the <paramref name="data"/>.</param>
        /// <returns>The deserialized <see cref="ValueType"/> object.</returns>
        /// <remarks>
        /// <note>
        /// For primitive element types, use <see cref="Buffer.BlockCopy"/> instead for better performance.
        /// </note>
        /// <note type="caution">
        /// Caution warning: Never call this method on a <typeparamref name="T"/> that has reference (non-value type) fields. Deserializing such value would result an invalid
        /// object with undetermined object references. Only string and array reference fields can be serialized safely if they are decorated by <see cref="MarshalAsAttribute"/> using
        /// <see cref="UnmanagedType.ByValTStr"/> or <see cref="UnmanagedType.ByValArray"/>, respectively.
        /// </note>
        /// </remarks>
        public static T[] DeserializeStructArray<T>(byte[] data, int offset, int count)
            where T : struct
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), Res.Get(Res.ArgumentNull));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Res.Get(Res.ArgumentOutOfRange));

            int len = Marshal.SizeOf(typeof(T)) * count;
            if (data.Length < len)
                throw new ArgumentException(Res.Get(Res.DataLenghtTooSmall), nameof(data));
            if (data.Length - offset < len || offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), Res.Get(Res.ArgumentOutOfRange));

            if (count == 0)
                return new T[0];

            T[] result = new T[count];
            GCHandle handle = GCHandle.Alloc(result, GCHandleType.Pinned);
            try
            {
                Marshal.Copy(data, offset, handle.AddrOfPinnedObject(), len);
            }
            finally
            {
                handle.Free();
            }

            return result;
        }

        #endregion

        #region Other Methods

        /// <summary>
        /// Creates a new <see cref="IFormatter"/> instance that can be used for serialization and deserialization with given <paramref name="options"/>
        /// </summary>
        /// <param name="options">Options for serializing objects.</param>
        public static IFormatter CreateFormatter(BinarySerializationOptions options)
        {
            return new BinarySerializationFormatter(options);
        }

        #endregion

        #endregion

        #region Internal Methods

        internal static FieldInfo[] GetSerializableFields(Type t)
        {
            lock (serializableFieldsCache)
            {
                return serializableFieldsCache[t];
            }
        }

        #endregion
    }
}
