#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializerTest.AdditionalMembers.cs
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

#region Used Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using KGySoft.Reflection;
using KGySoft.Serialization;

using NUnit.Framework.Internal;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Serialization
{
    partial class BinarySerializerTest
    {
        #region Nested classes

        #region TestWriter class

        private class TestWriter : BinaryWriter
        {
            #region Fields

            private readonly bool log;

            private long pos;

            #endregion

            #region Constructors

            public TestWriter(Stream stream, bool log)
                : base(stream)
            {
                this.log = log;
            }

            #endregion

            #region Methods

            #region Public Methods

            public override void Write(bool value)
            {
                Advance(1);
                if (log)
                    Console.WriteLine($"bool: {value} ({Convert.ToInt32(value)}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(value);
            }

            public override void Write(byte value)
            {
                Advance(1);
                if (log)
                {
                    var frames = new StackTrace().GetFrames();
                    string name = frames[1].GetMethod().Name;
                    if (name == "Write7BitInt")
                        name += " (" + frames[2].GetMethod().Name + ")";
                    string valueStr = value.ToString("X2");
                    if (name == "WriteDataType")
                        valueStr += $" [{Reflector.InvokeMethod(typeof(BinarySerializationFormatter), "DataTypeToString", (ushort)value)}]";
                    Console.WriteLine($"byte: {value} ({valueStr}) - {name}");
                }
                base.Write(value);
            }

            public override void Write(byte[] buffer)
            {
                Advance(buffer.Length);
                if (log)
                    Console.WriteLine($"{buffer.Length} bytes: {buffer.ToDecimalValuesString()} ({buffer.ToHexValuesString(",")}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(buffer);
            }

            public override void Write(byte[] buffer, int index, int count)
            {
                Advance(count);
                if (log)
                    Console.WriteLine($"{count} bytes: {buffer.Skip(index).Take(count).ToArray().ToDecimalValuesString()} ({buffer.Skip(index).Take(count).ToArray().ToHexValuesString(",")}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(buffer, index, count);
            }

            public override void Write(char ch)
            {
                Advance(2);
                if (log)
                    Console.WriteLine($"char: {ch} ({(uint)ch:X4}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(ch);
            }

            public override void Write(char[] chars)
            {
                Advance(2 * chars.Length); // depends on encoding but is alright for comparison
                if (log)
                    Console.WriteLine($"{chars.Length} chars: {new string(chars)} - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(chars);
            }

            public override void Write(char[] chars, int index, int count)
            {
                Advance(2 * count); // depends on encoding but is alright for comparison
                if (log)
                    Console.WriteLine($"{count} chars: {new string(chars.Skip(index).Take(count).ToArray())} - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(chars, index, count);
            }

            public override void Write(decimal value)
            {
                Advance(16);
                if (log)
                    Console.WriteLine($"decimal: {value} - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(value);
            }

            public override void Write(double value)
            {
                Advance(8);
                if (log)
                    Console.WriteLine($"double: {value:R} - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(value);
            }

            public override void Write(float value)
            {
                Advance(4);
                if (log)
                    Console.WriteLine($"float: {value:R} - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(value);
            }

            public override void Write(int value)
            {
                Advance(4);
                if (log)
                    Console.WriteLine($"int: {value} ({value:X8}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(value);
            }

            public override void Write(long value)
            {
                Advance(8);
                if (log)
                    Console.WriteLine($"long: {value} ({value:X16}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(value);
            }

            public override void Write(sbyte value)
            {
                Advance(1);
                if (log)
                    Console.WriteLine($"sbyte: {value} ({value:X2}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(value);
            }

            public override void Write(short value)
            {
                Advance(2);
                if (log)
                    Console.WriteLine($"short: {value} ({value:X4}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(value);
            }

            public override void Write(string value)
            {
                base.Write(value);
                Advance(value.Length); // depends on encoding but is alright for comparison
                if (log)
                    Console.WriteLine($"string: {value} - {new StackTrace().GetFrames()[1].GetMethod().Name}");
            }

            public override void Write(uint value)
            {
                Advance(4);
                if (log)
                    Console.WriteLine($"uint: {value} ({value:X8}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(value);
            }

            public override void Write(ulong value)
            {
                Advance(8);
                if (log)
                    Console.WriteLine($"ulong: {value} ({value:X16}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                base.Write(value);
            }

            public override void Write(ushort value)
            {
                Advance(2);
                if (log)
                {
                    var frames = new StackTrace().GetFrames();
                    string name = frames[1].GetMethod().Name;
                    string valueStr = value.ToString("X4");
                    if (name == "WriteDataType")
                        valueStr += $" [{Reflector.InvokeMethod(typeof(BinarySerializationFormatter), "DataTypeToString", value)}]";
                    Console.WriteLine($"ushort: {value} ({valueStr}) - {name}");
                }
                base.Write(value);
            }

            #endregion

            #region Private Methods

            private void Advance(int offset)
            {
                if (log)
                    Console.Write($"{pos:X8} ");
                pos += offset;
            }

            #endregion

            #endregion
        }

        #endregion

        #region TestReader class

        private class TestReader : BinaryReader
        {
            #region Fields

            private bool log;
            private long pos;

            #endregion

            #region Constructors

            public TestReader(Stream s, bool log)
                : base(s)
            {
                this.log = log;
            }

            #endregion

            #region Methods

            #region Public Methods

            public override int Read()
            {
                var result = base.Read();
                Advance(result >= 0 ? 1 : 0);
                if (log)
                    Console.WriteLine($"int char: {result} ({result:X}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override int Read(byte[] buffer, int index, int count)
            {
                var result = base.Read(buffer, index, count);
                Advance(result);
                if (log)
                    Console.WriteLine($"{result} bytes: {buffer.Skip(index).Take(result).ToArray().ToDecimalValuesString()} ({buffer.Skip(index).Take(result).ToArray().ToHexValuesString(",")}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                var result = base.Read(buffer, index, count);
                Advance(result * 2); // depends on encoding but ok for comparison
                if (log)
                    Console.WriteLine($"{result} chars: {new string(buffer.Skip(index).Take(result).ToArray())} - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override bool ReadBoolean()
            {
                var result = base.ReadBoolean();
                Advance(1);
                if (log)
                    Console.WriteLine($"bool: {result} ({Convert.ToInt32(result)}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override byte ReadByte()
            {
                var result = base.ReadByte();
                Advance(1);
                if (log)
                {
                    var frames = new StackTrace().GetFrames();
                    string name = frames[1].GetMethod().Name;
                    if (name == "Read7BitInt")
                        name += " (" + frames[2].GetMethod().Name + ")";
                    Console.WriteLine($"byte: {result} ({result:X2}) - {name}");
                }
                return result;
            }

            public override byte[] ReadBytes(int count)
            {
                var result = base.ReadBytes(count);
                Advance(count);
                if (log)
                    Console.WriteLine($"{result.Length} bytes: {result.ToDecimalValuesString()} ({result.ToHexValuesString(",")}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override char ReadChar()
            {
                var result = base.ReadChar();
                Advance(2);
                if (log)
                    Console.WriteLine($"char: {result} ({(uint)result:X2}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override char[] ReadChars(int count)
            {
                var result = base.ReadChars(count);
                Advance(2 * count); // depends on encoding but ok for comparison
                if (log)
                    Console.WriteLine($"{result.Length} chars: {new string(result)} - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override decimal ReadDecimal()
            {
                var result = base.ReadDecimal();
                Advance(16);
                if (log)
                    Console.WriteLine($"decimal: {result} - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override double ReadDouble()
            {
                var result = base.ReadDouble();
                Advance(8);
                if (log)
                    Console.WriteLine($"double: {result:R} - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override short ReadInt16()
            {
                var result = base.ReadInt16();
                Advance(2);
                if (log)
                    Console.WriteLine($"short: {result} ({result:X4}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override int ReadInt32()
            {
                var result = base.ReadInt32();
                Advance(4);
                if (log)
                    Console.WriteLine($"int: {result} ({result:X8}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override long ReadInt64()
            {
                var result = base.ReadInt64();
                Advance(8);
                if (log)
                    Console.WriteLine($"long: {result} ({result:X16}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override sbyte ReadSByte()
            {
                var result = base.ReadSByte();
                Advance(1);
                if (log)
                    Console.WriteLine($"sbyte: {result} ({result:X2}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override float ReadSingle()
            {
                var result = base.ReadSingle();
                Advance(4);
                if (log)
                    Console.WriteLine($"float: {result:R} - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override string ReadString()
            {
                var result = base.ReadString();
                Advance(result.Length); // depends on encoding but ok for comparison
                if (log)
                    Console.WriteLine($"string: {result} - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override ushort ReadUInt16()
            {
                var result = base.ReadUInt16();
                Advance(2);
                if (log)
                    Console.WriteLine($"ushort: {result} ({result:X4}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override uint ReadUInt32()
            {
                var result = base.ReadUInt32();
                Advance(4);
                if (log)
                    Console.WriteLine($"uint: {result} ({result:X8}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            public override ulong ReadUInt64()
            {
                var result = base.ReadUInt64();
                Advance(8);
                if (log)
                    Console.WriteLine($"ulong: {result} ({result:X16}) - {new StackTrace().GetFrames()[1].GetMethod().Name}");
                return result;
            }

            #endregion

            #region Private Methods

            private void Advance(int offset)
            {
                if (log)
                    Console.Write($"{pos:X8} ");
                pos += offset;
            }

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        private static byte[] SerializeObject(object obj, IFormatter formatter)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = null;
                BinarySerializationFormatter bsf = formatter as BinarySerializationFormatter;
                if (dumpDetails && bsf != null)
                    bw = new TestWriter(ms, dumpDetails);

                if (bw != null)
                    bsf.SerializeByWriter(bw, obj);
                else
                    formatter.Serialize(ms, obj);

                Console.WriteLine($"Length: {ms.Length}");
                if (dumpSerContent)
                    Console.WriteLine(ToRawString(ms.ToArray()));
                return ms.ToArray();
            }
        }

        private static byte[] SerializeObjects(object[] objects, IFormatter formatter)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, objects.Length);
                BinaryWriter bw = null;
                BinarySerializationFormatter bsf = formatter as BinarySerializationFormatter;
                if (dumpDetails && bsf != null)
                    bw = new TestWriter(ms, dumpDetails);

                foreach (object o in objects)
                {
                    long pos = ms.Position;
                    if (bw != null)
                        bsf.SerializeByWriter(bw, o);
                    else
                        formatter.Serialize(ms, o);
                    Console.WriteLine($"{(o == null ? "<null>" : o.GetType().ToString())} - length: {ms.Position - pos}");
                }
                Console.WriteLine($"Full length: {ms.Length}");
                if (dumpSerContent)
                    Console.WriteLine(ToRawString(ms.ToArray()));
                return ms.ToArray();
            }
        }

        private static object DeserializeObject(byte[] rawData, IFormatter formatter)
        {
            using (MemoryStream ms = new MemoryStream(rawData))
            {
                BinaryReader br = null;
                BinarySerializationFormatter bsf = formatter as BinarySerializationFormatter;
                if (dumpDetails && bsf != null)
                    br = new TestReader(ms, dumpDetails);
                return br != null ? bsf.DeserializeByReader(br) : formatter.Deserialize(ms);
            }
        }

        private static object[] DeserializeObjects(byte[] rawData, IFormatter formatter)
        {
            using (MemoryStream ms = new MemoryStream(rawData))
            {
                int length;
                object[] result = new object[length = (int)formatter.Deserialize(ms)];

                BinaryReader br = null;
                BinarySerializationFormatter bsf = formatter as BinarySerializationFormatter;
                if (dumpDetails && bsf != null)
                    br = new TestReader(ms, dumpDetails);

                for (int i = 0; i < length; i++)
                    result[i] = br != null ? bsf.DeserializeByReader(br) : formatter.Deserialize(ms);
                return result;
            }
        }

        /// <summary>
        /// Converts the byte array (deemed as extended 8-bit ASCII characters) to raw Unicode UTF-8 string representation.
        /// </summary>
        /// <param name="bytes">The bytes to visualize as a raw UTF-8 data.</param>
        /// <remarks>
        /// <note type="caution">
        /// Please note that the .NET <see cref="string"/> type is always UTF-16 encoded. What this method does is
        /// not parsing an UTF-8 encoded stream but a special conversion that makes possible to display a byte array as a raw UTF-8 data.
        /// To convert a byte array to a regular <see cref="string"/> for usual purposes
        /// use <see cref="Encoding.Convert(System.Text.Encoding,System.Text.Encoding,byte[])"/> method instead.
        /// </note>
        /// </remarks>
        /// <returns>
        /// A <see cref="string"/> instance that is good for visualizing a raw UTF-8 string.</returns>
        private static string ToRawString(byte[] bytes)
        {
            string s = Encoding.Default.GetString(bytes);
            var chars = new char[s.Length];
            var whitespaceControls = new[] { '\t', '\r', '\n' };
            for (int i = 0; i < s.Length; i++)
                chars[i] = s[i] < 32 && !s[i].In(whitespaceControls) ? '□' : s[i];
            return new String(chars);
        }

        #endregion

        #region Instance Methods

        private void SystemSerializeObject(object obj, string title = null, bool recursionProofCompare = false, SerializationBinder binder = null, ISurrogateSelector surrogateSelector = null)
        {
            using (new TestExecutionContext.IsolatedContext())
            {
                if (title == null)
                    title = obj.GetType().ToString();
                Console.WriteLine($"------------------System BinaryFormatter ({title})--------------------");
                BinaryFormatter bf = new BinaryFormatter { Binder = binder, SurrogateSelector = surrogateSelector };
                try
                {
                    byte[] serData = SerializeObject(obj, bf);
                    object deserializedObject = DeserializeObject(serData, bf);
                    if (recursionProofCompare)
                        AssertDeepEquals(serData, SerializeObject(deserializedObject, bf));
                    else
                        AssertDeepEquals(obj, deserializedObject);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"System serialization failed: {e}");
                }
            }
        }

        private void SystemSerializeObjects(object[] referenceObjects, string title = null, bool recursionProofCompare = false, SerializationBinder binder = null, ISurrogateSelector surrogateSelector = null)
        {
            if (title == null)
                title = $"Items Count: {referenceObjects.Length}";
            Console.WriteLine($"------------------System BinaryFormatter ({title})--------------------");
            using (new TestExecutionContext.IsolatedContext())
            {
                BinaryFormatter bf = new BinaryFormatter { Binder = binder, SurrogateSelector = surrogateSelector };
                try
                {
                    byte[] serData = SerializeObjects(referenceObjects, bf);
                    object[] deserializedObjects = DeserializeObjects(serData, bf);
                    if (recursionProofCompare)
                        AssertItemsEqual(serData, SerializeObjects(deserializedObjects, bf));
                    else
                        AssertItemsEqual(referenceObjects, deserializedObjects);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"System serialization failed: {e}");
                }
            }
        }

        private void KGySerializeObject(object obj, BinarySerializationOptions options, string title = null, bool recursionProofCompare = false, SerializationBinder binder = null, ISurrogateSelector surrogateSelector = null)
        {
            if (title == null)
                title = obj.GetType().ToString();
            Console.WriteLine($"------------------KGy SOFT BinarySerializer ({title} - {options})--------------------");
            BinarySerializationFormatter bsf = new BinarySerializationFormatter(options) { Binder = binder, SurrogateSelector = surrogateSelector };
            try
            {
                byte[] serData = SerializeObject(obj, bsf);
                object deserializedObject = DeserializeObject(serData, bsf);
                if (recursionProofCompare)
                    AssertDeepEquals(serData, SerializeObject(deserializedObject, bsf));
                else
                    AssertDeepEquals(obj, deserializedObject);
            }
            catch (Exception e)
            {
                Console.WriteLine($"KGySoft serialization failed: {e}");
                throw;
            }
        }

        private void KGySerializeObjects(object[] referenceObjects, BinarySerializationOptions options, string title = null, bool recursionProofCompare = false, SerializationBinder binder = null, ISurrogateSelector surrogateSelector = null)
        {
            if (title == null)
                title = $"Items Count: {referenceObjects.Length}";
            Console.WriteLine($"------------------KGy SOFT BinarySerializer ({title} - {options})--------------------");
            BinarySerializationFormatter bsf = new BinarySerializationFormatter(options) { Binder = binder, SurrogateSelector = surrogateSelector };
            try
            {
                byte[] serData = SerializeObjects(referenceObjects, bsf);
                object[] deserializedObjects = DeserializeObjects(serData, bsf);
                if (recursionProofCompare)
                    AssertItemsEqual(serData, SerializeObjects(deserializedObjects, bsf));
                else
                    AssertItemsEqual(referenceObjects, deserializedObjects);
            }
            catch (Exception e)
            {
                Console.WriteLine($"KGySoft serialization failed: {e}");
                throw;
            }
        }

        #endregion

        #endregion
    }
}
