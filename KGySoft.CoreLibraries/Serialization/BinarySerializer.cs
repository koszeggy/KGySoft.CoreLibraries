#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializer.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization
{
    /// <summary>
    /// Provides public static methods for binary serialization. Most of its methods will use an <see cref="BinarySerializationFormatter"/> instance internally.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="BinarySerializationFormatter"/> class for details and an example.
    /// </summary>
    /// <seealso cref="BinarySerializationFormatter"/>
    /// <seealso cref="BinarySerializationOptions"/>
    /// <seealso cref="IBinarySerializable"/>
    public static class BinarySerializer
    {
        #region Constants

        internal const BinarySerializationOptions DefaultOptions = BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures; //| BinarySerializationOptions.CompactSerializationOfBoolCollections;

        #endregion

        #region Fields

        private static readonly IThreadSafeCacheAccessor<Type, FieldInfo[]> serializableFieldsCache = new Cache<Type, FieldInfo[]>(t =>
            t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(f => !f.IsNotSerialized)
                .OrderBy(f => f.MetadataToken).ToArray(), 1024).GetThreadSafeAccessor();

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Serializes an object into a byte array.
        /// </summary>
        /// <param name="data">The object to serialize</param>
        /// <param name="options">Options of the serialization. This parameter is optional.
        /// <br/>Default value: <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/>, <see cref="BinarySerializationOptions.CompactSerializationOfStructures"/>.</param>
        /// <returns>Serialized raw data of the object</returns>
        public static byte[] Serialize(object data, BinarySerializationOptions options = DefaultOptions) => new BinarySerializationFormatter(options).Serialize(data);

        /// <summary>
        /// Deserializes the specified part of a byte array into an object.
        /// </summary>
        /// <param name="rawData">Contains the raw data representation of the object to deserialize.</param>
        /// <param name="offset">Points to the starting position of the object data in <paramref name="rawData"/>. This parameter is optional.
        /// <br/>Default value: <c>0</c>.</param>
        /// <returns>The deserialized object.</returns>
        public static object Deserialize(byte[] rawData, int offset = 0) => new BinarySerializationFormatter().Deserialize(rawData, offset);

        /// <summary>
        /// Serializes the given <paramref name="data"/> into a <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream, into which the data is written. The stream must support writing and will remain open after serialization.</param>
        /// <param name="data">The data that will be written into the stream.</param>
        /// <param name="options">Options of the serialization. This parameter is optional.
        /// <br/>Default value: <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/>, <see cref="BinarySerializationOptions.CompactSerializationOfStructures"/>.</param>
        public static void SerializeToStream(Stream stream, object data, BinarySerializationOptions options = DefaultOptions) => new BinarySerializationFormatter(options).SerializeToStream(stream, data);

        /// <summary>
        /// Deserializes data beginning at current position of given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream, from which the data is read. The stream must support reading and will remain open after deserialization.</param>
        /// <returns>The deserialized data.</returns>
        public static object DeserializeFromStream(Stream stream) => new BinarySerializationFormatter().DeserializeFromStream(stream);

        /// <summary>
        /// Serializes the given <paramref name="data"/> by using the provided <paramref name="writer"/>.
        /// </summary>
        /// <remarks>
        /// <note>This method produces compatible serialized data with <see cref="Serialize">Serialize</see>
        /// and <see cref="SerializeToStream">SerializeToStream</see> methods only when encoding of the writer is UTF-8. Otherwise, you must use <see cref="DeserializeByReader">DeserializeByReader</see> with the same encoding as here.</note>
        /// </remarks>
        /// <param name="writer">The writer that will used to serialize data. The writer will remain opened after serialization.</param>
        /// <param name="data">The data that will be written by the writer.</param>
        /// <param name="options">Options of the serialization. This parameter is optional.
        /// <br/>Default value: <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/>, <see cref="BinarySerializationOptions.CompactSerializationOfStructures"/>.</param>
        public static void SerializeByWriter(BinaryWriter writer, object data, BinarySerializationOptions options = DefaultOptions) => new BinarySerializationFormatter(options).SerializeByWriter(writer, data);

        /// <summary>
        /// Deserializes data beginning at current position of given <paramref name="reader"/>.
        /// </summary>
        /// <remarks>
        /// <note>If data was serialized by <see cref="Serialize">Serialize</see> or <see cref="SerializeToStream">SerializeToStream</see> methods, then
        /// <paramref name="reader"/> must use UTF-8 encoding to get correct result. If data was serialized by the <see cref="SerializeByWriter">SerializeByWriter</see> method, then you must use the same encoding as there.</note>
        /// </remarks>
        /// <param name="reader">The reader that will be used to deserialize data. The reader will remain opened after deserialization.</param>
        /// <returns>The deserialized data.</returns>
        public static object DeserializeByReader(BinaryReader reader) => new BinarySerializationFormatter().DeserializeByReader(reader);

        /// <summary>
        /// Serializes a <see cref="ValueType"/> into a byte array.
        /// </summary>
        /// <param name="obj">The <see cref="ValueType"/> object to serialize.</param>
        /// <returns>The byte array representation of the <see cref="ValueType"/> object.</returns>
        /// <remarks>
        /// <note type="caution">Never call this method on a <see cref="ValueType"/> that has reference (non-value type) fields. Deserializing such value would result an invalid
        /// object with undetermined object references. Only string and array reference fields can be serialized safely if they are decorated by <see cref="MarshalAsAttribute"/> using
        /// <see cref="UnmanagedType.ByValTStr"/> or <see cref="UnmanagedType.ByValArray"/>, respectively.</note>
        /// </remarks>
        [SecurityCritical]
        public static byte[] SerializeValueType(ValueType obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), Res.ArgumentNull);
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
#if !NET35
        [SecuritySafeCritical]
#endif
        public static bool TrySerializeValueType(ValueType obj, out byte[] result)
        {
            result = null;

            if (obj == null)
                throw new ArgumentNullException(nameof(obj), Res.ArgumentNull);
            if (CanSerializeValueType(obj.GetType(), false))
            {
                try
                {
                    result = SerializeValueType(obj);
                }
                catch (Exception e) when (!e.IsCritical())
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
        /// For primitive element types, use <see cref="Buffer.BlockCopy">Buffer.BlockCopy</see> instead for better performance.
        /// </note>
        /// <note type="caution">Never call this method on a <typeparamref name="T"/> that has reference (non-value type) fields. Deserializing such value would result an invalid
        /// object with undetermined object references.</note>
        /// </remarks>
        [SecurityCritical]
        public static byte[] SerializeValueArray<T>(T[] array) where T : struct
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array), Res.ArgumentNull);
            if (array.Length == 0)
                return Reflector.EmptyArray<byte>();

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
        /// <returns><see langword="true"/>, if serialization was successful; otherwise, <see langword="false"/>.
        /// The <paramref name="array"/> can be serialized if <typeparamref name="T"/> contains only value type fields.</returns>
#if !NET35
        [SecuritySafeCritical]
#endif
        public static bool TrySerializeValueArray<T>(T[] array, out byte[] result) where T : struct
        {
            result = null;

            if (array == null)
                throw new ArgumentNullException(nameof(array), Res.ArgumentNull);
            if (array.Length == 0)
            {
                result = Reflector.EmptyArray<byte>();
                return true;
            }

            if (!CanSerializeValueType(typeof(T), true))
                return false;

            try
            {
                result = SerializeValueArray(array);
            }
            catch (Exception e) when (!e.IsCritical())
            {
                // CanSerializeStruct filters a sort of conditions but serialization may fail even in that case - this catch is to protect this case.
                return false;
            }

            return true;
        }

        /// <summary>
        /// Deserializes a <see cref="ValueType"/> object from a byte array that was previously serialized by <see cref="SerializeValueType">SerializeValueType</see> method.
        /// </summary>
        /// <param name="type">The type of the target object. Must be a <see cref="ValueType"/>.</param>
        /// <param name="data">The byte array that starts with byte representation of the object.</param>
        /// <returns>The deserialized <see cref="ValueType"/> object.</returns>
        [SecurityCritical]
        public static object DeserializeValueType(Type type, byte[] data)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            if (!type.IsValueType)
                throw new ArgumentException(Res.BinarySerializationValueTypeExpected, nameof(type));
            if (data == null)
                throw new ArgumentNullException(nameof(data), Res.ArgumentNull);
            if (data.Length < Marshal.SizeOf(type))
                throw new ArgumentException(Res.BinarySerializationDataLenghtTooSmall, nameof(data));

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
        /// Deserializes a <see cref="ValueType"/> object from a byte array that was previously serialized by <see cref="SerializeValueType">SerializeValueType</see> method
        /// beginning on a specified <paramref name="offset"/>.
        /// </summary>
        /// <param name="type">The type of the target object. Must be a <see cref="ValueType"/>.</param>
        /// <param name="data">The byte array that contains the byte representation of the object.</param>
        /// <param name="offset">The offset that points to the beginning of the serialized data.</param>
        /// <returns>The deserialized <see cref="ValueType"/> object.</returns>
        [SecurityCritical]
        public static object DeserializeValueType(Type type, byte[] data, int offset)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            if (!type.IsValueType)
                throw new ArgumentException(Res.BinarySerializationValueTypeExpected, nameof(type));
            if (data == null)
                throw new ArgumentNullException(nameof(data), Res.ArgumentNull);

            int len = Marshal.SizeOf(type);
            if (data.Length < len)
                throw new ArgumentException(Res.BinarySerializationDataLenghtTooSmall, nameof(data));
            if (data.Length - offset < len || offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), Res.ArgumentOutOfRange);

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
        /// Deserializes an array of <see cref="ValueType"/> objects from a byte array
        /// that was previously serialized by <see cref="SerializeValueArray{T}">SerializeValueArray</see> method.
        /// </summary>
        /// <typeparam name="T">Type of the elements in the deserialized array. Must be a <see cref="ValueType"/>.</typeparam>
        /// <param name="data">The byte array that contains the byte representation of the structures.</param>
        /// <param name="offset">The offset that points to the beginning of the serialized data.</param>
        /// <param name="count">Number of elements to deserialize from the <paramref name="data"/>.</param>
        /// <returns>The deserialized <see cref="ValueType"/> object.</returns>
        [SecurityCritical]
        public static T[] DeserializeValueArray<T>(byte[] data, int offset, int count)
            where T : struct
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), Res.ArgumentNull);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), Res.ArgumentOutOfRange);

            int len = Marshal.SizeOf(typeof(T)) * count;
            if (data.Length < len)
                throw new ArgumentException(Res.BinarySerializationDataLenghtTooSmall, nameof(data));
            if (data.Length - offset < len || offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), Res.ArgumentOutOfRange);

            if (count == 0)
                return Reflector.EmptyArray<T>();

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

        /// <summary>
        /// Creates a formatter that can be used for serialization and deserialization with given <paramref name="options"/>.
        /// </summary>
        /// <returns>An <see cref="IFormatter"/> instance that can be used for serialization and deserialization with given <paramref name="options"/>.</returns>
        /// <param name="options">Options for the created formatter. This parameter is optional.
        /// <br/>Default value: <see cref="BinarySerializationOptions.RecursiveSerializationAsFallback"/>, <see cref="BinarySerializationOptions.CompactSerializationOfStructures"/>.</param>
        public static IFormatter CreateFormatter(BinarySerializationOptions options = DefaultOptions) => new BinarySerializationFormatter(options);

        #endregion

        #region Internal Methods

        internal static bool CanSerializeValueType(Type type, bool strict)
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
                    if (!CanSerializeValueType(field.FieldType, strict))
                        return false;
                }
                else if (field.FieldType.IsArray || field.FieldType == Reflector.StringType)
                {
                    if (strict)
                        return false;
                    object[] attrs = field.GetCustomAttributes(typeof(MarshalAsAttribute), false);
                    MarshalAsAttribute marshalAs = attrs.Length > 0 ? attrs[0] as MarshalAsAttribute : null;
                    if (marshalAs != null && (field.FieldType.IsArray && marshalAs.Value == UnmanagedType.ByValArray ||
                        field.FieldType == Reflector.StringType && marshalAs.Value == UnmanagedType.ByValTStr))
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

        internal static FieldInfo[] GetSerializableFields(Type t) => serializableFieldsCache[t];

        #endregion

        #endregion
    }
}
