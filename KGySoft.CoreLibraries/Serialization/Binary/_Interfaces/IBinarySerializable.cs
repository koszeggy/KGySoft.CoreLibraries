#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IBinarySerializable.cs
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
using System.Runtime.Serialization;

#endregion

namespace KGySoft.Serialization.Binary
{
    /// <summary>
    /// Makes possible quick and compact custom serialization of a class by <see cref="BinarySerializer"/> and <see cref="BinarySerializationFormatter"/>.
    /// <br/>See the <strong>Remarks</strong> section for examples and details.
    /// </summary>
    /// <remarks>
    /// <para>By this interface a class can be serialized into a compact byte array. Unlike in case of system serialization and <see cref="ISerializable"/> implementations, saved data
    /// does not have to contain a name based mapping. Thus the saved content can be more compact but this solution can be discouraged on very large object graphs
    /// because the whole object has to be written into memory. In case of very large object hierarchies you might consider to implement <see cref="ISerializable"/>
    /// interface instead, which is also supported by <see cref="BinarySerializer"/> and <see cref="BinarySerializationFormatter"/>.
    /// </para>
    /// <para>If the implementer has a constructor with <c>(<see cref="BinarySerializationOptions"/>, <see cref="Array">byte[]</see>)</c> parameters, then it will be called
    /// for deserialization. This makes possible to initialize read-only fields. If no such constructor exists, then the <see cref="Deserialize">Deserialize</see> method will be called. If the implementer has a parameterless constructor,
    /// then it will be called before calling the <see cref="Deserialize">Deserialize</see> method; otherwise, no constructor will be used at all.</para>
    /// <para>Methods decorated by <see cref="OnSerializingAttribute"/>, <see cref="OnSerializedAttribute"/>, <see cref="OnDeserializingAttribute"/> and <see cref="OnDeserializedAttribute"/> as well as calling <see cref="IDeserializationCallback.OnDeserialization">IDeserializationCallback.OnDeserialization</see> method
    /// of implementers are fully supported also for <see cref="IBinarySerializable"/> implementers. Attributes should be used on methods that have a single <see cref="StreamingContext"/> parameter.</para>
    /// </remarks>
    /// <example>
    /// Following example demonstrates the usage of the special constructor version.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.IO;
    /// using KGySoft.Serialization.Binary;
    ///
    /// // This is a simple sealed class that will never be derived
    /// public sealed class ExampleSimple : IBinarySerializable
    /// {
    ///     public int IntProp { get; }
    ///     public string StringProp { get; }
    ///
    ///     // this is the ordinary constructor
    ///     public ExampleSimple(int intValue, string stringValue)
    ///     {
    ///         IntProp = intValue;
    ///         StringProp = stringValue;
    ///     }
    ///
    ///     // this is the special constructor used by the deserializer
    ///     // if you have read-only fields you must implement this constructor
    ///     private ExampleSimple(BinarySerializationOptions options, byte[] serData)
    ///     {
    ///         using (BinaryReader reader = new BinaryReader(new MemoryStream(serData)))
    ///         {
    ///             IntProp = reader.ReadInt32();
    ///             bool isStringPropNull = reader.ReadBoolean();
    ///             StringProp = isStringPropNull ? null : reader.ReadString();
    ///         }
    ///     }
    ///
    ///     public byte[] Serialize(BinarySerializationOptions options)
    ///     {
    ///         MemoryStream ms = new MemoryStream();
    ///         using (BinaryWriter writer = new BinaryWriter(ms))
    ///         {
    ///             writer.Write(IntProp);
    ///             writer.Write(StringProp == null);
    ///             if (StringProp != null)
    ///                 writer.Write(StringProp);
    ///         }
    ///         return ms.ToArray();
    ///     }
    ///
    ///     public void Deserialize(BinarySerializationOptions options, byte[] serData)
    ///     {
    ///         throw new InvalidOperationException("Will not be called because special constructor is implemented");
    ///     }
    /// }]]>
    /// </code>
    /// The following example introduces a pattern that can be used for serialization and deserialization serializable base and derived classes
    /// and with versioned content (optional fields):
    /// <code lang="C#"><![CDATA[
    /// using System.IO;
    /// using KGySoft.Serialization.Binary;
    ///
    /// public class SerializableBase : IBinarySerializable
    /// {
    ///     public int IntProp { get; set; }
    ///     public string StringProp { get; set; }
    ///
    ///     private static int currentVersionBase = 1;
    ///
    ///     // this is the ordinary constructor
    ///     public SerializableBase(int intValue, string stringValue)
    ///     {
    ///         IntProp = intValue;
    ///         StringProp = stringValue;
    ///     }
    ///
    ///     // parameterless constructor: will be called on deserialization
    ///     // if exists and there is no special constructor
    ///     protected SerializableBase()
    ///     {
    ///     }
    ///
    ///     byte[] IBinarySerializable.Serialize(BinarySerializationOptions options)
    ///     {
    ///         MemoryStream ms = new MemoryStream();
    ///         using (BinaryWriter writer = new BinaryWriter(ms))
    ///         {
    ///             SerializeContent(writer);
    ///         }
    ///         return ms.ToArray();
    ///     }
    ///
    ///     void IBinarySerializable.Deserialize(BinarySerializationOptions options, byte[] serData)
    ///     {
    ///         using (BinaryReader reader = new BinaryReader(new MemoryStream(serData)))
    ///         {
    ///             DeserializeContent(reader);
    ///         }
    ///     }
    ///
    ///     protected virtual void SerializeContent(BinaryWriter writer)
    ///     {
    ///         writer.Write(currentVersionBase);
    ///         writer.Write(IntProp);
    ///         writer.Write(StringProp == null);
    ///         if (StringProp != null)
    ///             writer.Write(StringProp);
    ///     }
    ///
    ///     protected virtual void DeserializeContent(BinaryReader reader)
    ///     {
    ///         int version = reader.ReadInt32();
    ///         IntProp = reader.ReadInt32();
    ///         bool isStringPropNull = reader.ReadBoolean();
    ///         StringProp = isStringPropNull ? null : reader.ReadString();
    ///         // TODO: Read rest if version changes
    ///     }
    /// }
    ///
    /// public class SerializableDerived: SerializableBase
    /// {
    ///     public bool BoolProp { get; set; }
    ///
    ///     // This property is new in this class (optional content)
    ///     public int NewIntProp { get; set; }
    ///
    ///     private static int currentVersionDerived = 2;
    ///
    ///     // this is the ordinary constructor
    ///     public SerializableDerived(int intValue, string stringValue, bool boolValue)
    ///         : base(intValue, stringValue)
    ///     {
    ///         BoolProp = boolValue;
    ///     }
    ///
    ///     // parameterless constructor: will be called on deserialization
    ///     // if exists and there is no special constructor
    ///     protected SerializableDerived()
    ///         : base()
    ///     {
    ///     }
    ///
    ///     protected override void SerializeContent(BinaryWriter writer)
    ///     {
    ///         base.SerializeContent(writer);
    ///         writer.Write(currentVersionDerived);
    ///         writer.Write(BoolProp);
    ///         writer.Write(NewIntProp);
    ///     }
    ///
    ///     protected override void DeserializeContent(BinaryReader reader)
    ///     {
    ///         base.DeserializeContent(reader);
    ///         int version = reader.ReadInt32();
    ///         BoolProp = reader.ReadBoolean();
    ///         if (version < 2)
    ///             return;
    ///         NewIntProp = reader.ReadInt32();
    ///     }
    /// }]]>
    /// </code>
    /// <note type="implement">
    /// Of course the special constructor way can be used here, too.
    /// Derived constructors should just call the base constructor, which should call <c>DeserializeContent</c>.
    /// In that case <strong>FxCop</strong> and <strong>ReSharper</strong> may emit a warning that
    /// virtual method is called from a constructor but that is alright here because this is a clean initialization pattern.</note>
    /// </example>
    public interface IBinarySerializable
    {
        #region Methods

        /// <summary>
        /// Serializes the object into a byte array.
        /// </summary>
        /// <param name="options">Options used for the serialization.</param>
        /// <returns>The byte data representation of the object that can be used to restore the original object state by the <see cref="Deserialize">Deserialize</see> method.</returns>
        byte[] Serialize(BinarySerializationOptions options);

        /// <summary>
        /// Deserializes the inner state of the object from a byte array. Called only when the implementer does not have a constructor with <c>(<see cref="BinarySerializationOptions"/>, <see cref="Array">byte[]</see>)</c> parameters.
        /// Without such constructor parameterless constructor will be called if any (otherwise, no constructors will be executed). The special constructor should be used if the class has read-only fields to be restored.
        /// </summary>
        /// <param name="serData">Serialized raw data of the object created by the <see cref="Serialize">Serialize</see> method.</param>
        /// <param name="options">Options used for the deserialization.</param>
        void Deserialize(BinarySerializationOptions options, byte[] serData);

        #endregion
    }
}
