#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializerTest.AdditionalMembers.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Used Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using KGySoft.Reflection;
using KGySoft.Serialization.Binary;

using NUnit.Framework;
using NUnit.Framework.Internal;

#endregion

#region Suppressions

#pragma warning disable 162 // Unreachable code may occur depending on values of constant fields

#if NET
#if NET5_0_OR_GREATER
#pragma warning disable SYSLIB0011 // Type or member is obsolete - this class uses BinaryFormatter for security tests
#pragma warning disable IDE0079 // Remove unnecessary suppression - CS0618 is emitted by ReSharper
#pragma warning disable CS0618 // Use of obsolete symbol - as above  
#else
#error Check whether IFormatter is still available in this .NET version
#endif
#endif

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Serialization.Binary
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
                    Console.WriteLine($"bool: {value} ({Convert.ToInt32(value)}) - {GetStack()}");
                base.Write(value);
            }

            public override void Write(byte value)
            {
                Advance(1);
                if (log)
                {
                    string valueStr = value.ToString("X2");
                    var frames = new StackTrace().GetFrames();
                    string name = frames[1].GetMethod().Name;
                    if (name == "WriteDataType")
                        valueStr += $" [{Reflector.InvokeMethod(typeof(BinarySerializationFormatter), "DataTypeToString", (int)value)}]";
                    else if (name == "WriteTypeAttributes")
                        valueStr += $" [{Reflector.InvokeMethod(typeof(Enum<>).MakeGenericType(typeof(BinarySerializationFormatter).GetNestedType("TypeAttributes", BindingFlags.NonPublic)), "ToString", (int)value, " | ")}]";
                    Console.WriteLine($"byte: {value} ({valueStr}) - {GetStack()}");
                }
                base.Write(value);
            }

            public override void Write(byte[] buffer)
            {
                Advance(buffer.Length);
                if (log)
                    Console.WriteLine($"{buffer.Length} bytes: {buffer.ToDecimalValuesString()} ({buffer.ToHexValuesString(",")}) - {GetStack()}");
                base.Write(buffer);
            }

            public override void Write(byte[] buffer, int index, int count)
            {
                Advance(count);
                if (log)
                    Console.WriteLine($"{count} bytes: {buffer.Skip(index).Take(count).ToArray().ToDecimalValuesString()} ({buffer.Skip(index).Take(count).ToArray().ToHexValuesString(",")}) - {GetStack()}");
                base.Write(buffer, index, count);
            }

            public override void Write(char ch)
            {
                Advance(2);
                if (log)
                    Console.WriteLine($"char: {ch} ({(uint)ch:X4}) - {GetStack()}");
                base.Write(ch);
            }

            public override void Write(char[] chars)
            {
                Advance(2 * chars.Length); // depends on encoding but is alright for comparison
                if (log)
                    Console.WriteLine($"{chars.Length} chars: {new string(chars)} - {GetStack()}");
                base.Write(chars);
            }

            public override void Write(char[] chars, int index, int count)
            {
                Advance(2 * count); // depends on encoding but is alright for comparison
                if (log)
                    Console.WriteLine($"{count} chars: {new string(chars.Skip(index).Take(count).ToArray())} - {GetStack()}");
                base.Write(chars, index, count);
            }

            public override void Write(decimal value)
            {
                Advance(16);
                if (log)
                    Console.WriteLine($"decimal: {value} - {GetStack()}");
                base.Write(value);
            }

            public override void Write(double value)
            {
                Advance(8);
                if (log)
                    Console.WriteLine($"double: {value:R} - {GetStack()}");
                base.Write(value);
            }

            public override void Write(float value)
            {
                Advance(4);
                if (log)
                    Console.WriteLine($"float: {value:R} - {GetStack()}");
                base.Write(value);
            }

            public override void Write(int value)
            {
                Advance(4);
                if (log)
                    Console.WriteLine($"int: {value} ({value:X8}) - {GetStack()}");
                base.Write(value);
            }

            public override void Write(long value)
            {
                Advance(8);
                if (log)
                    Console.WriteLine($"long: {value} ({value:X16}) - {GetStack()}");
                base.Write(value);
            }

            public override void Write(sbyte value)
            {
                Advance(1);
                if (log)
                    Console.WriteLine($"sbyte: {value} ({value:X2}) - {GetStack()}");
                base.Write(value);
            }

            public override void Write(short value)
            {
                Advance(2);
                if (log)
                    Console.WriteLine($"short: {value} ({value:X4}) - {GetStack()}");
                base.Write(value);
            }

            public override void Write(string value)
            {
                base.Write(value);
                Advance(value.Length); // depends on encoding but is alright for comparison
                if (log)
                    Console.WriteLine($"string: {value} - {GetStack()}");
            }

            public override void Write(uint value)
            {
                Advance(4);
                if (log)
                    Console.WriteLine($"uint: {value} ({value:X8}) - {GetStack()}");
                base.Write(value);
            }

            public override void Write(ulong value)
            {
                Advance(8);
                if (log)
                    Console.WriteLine($"ulong: {value} ({value:X16}) - {GetStack()}");
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
                        valueStr += $" [{Reflector.InvokeMethod(typeof(BinarySerializationFormatter), "DataTypeToString", (int)value)}]";
                    Console.WriteLine($"ushort: {value} ({valueStr}) - {GetStack()}");
                }
                base.Write(value);
            }

            public override string ToString() => $"Position: {pos:X8}";

            #endregion

            #region Private Methods

            private void Advance(int offset)
            {
                if (log)
                    Console.Write($"{pos:X8} ");
                pos += offset;
            }

            private static string GetStack() => new StackTrace().GetFrames().Skip(2).Select(f => f.GetMethod().Name).TakeWhile(s => s != "SerializeByWriter").Join(" < ");

            #endregion

            #endregion
        }

        #endregion

        #region TestReader class

        private class TestReader : BinaryReader
        {
            #region Fields

            private readonly bool log;
            private long pos;
            private bool extendedDataType;

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
                    Console.WriteLine($"int char: {result} ({result:X}) - {GetStack()}");
                return result;
            }

            public override int Read(byte[] buffer, int index, int count)
            {
                var result = base.Read(buffer, index, count);
                Advance(result);
                if (log)
                    Console.WriteLine($"{result} bytes: {buffer.Skip(index).Take(result).ToArray().ToDecimalValuesString()} ({buffer.Skip(index).Take(result).ToArray().ToHexValuesString(",")}) - {GetStack()}");
                return result;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                var result = base.Read(buffer, index, count);
                Advance(result * 2); // depends on encoding but ok for comparison
                if (log)
                    Console.WriteLine($"{result} chars: {new string(buffer.Skip(index).Take(result).ToArray())} - {GetStack()}");
                return result;
            }

            public override bool ReadBoolean()
            {
                byte result = base.ReadByte();
                Advance(1);
                if (log)
                    Console.WriteLine($"bool: {result > 0} ({Convert.ToInt32(result)}) - {GetStack()}");
                if (result > 1)
                    Console.WriteLine($"!!! Suspicious bool value: {result}");
                return result == 1;
            }

            public override byte ReadByte()
            {
                var result = base.ReadByte();
                Advance(1);
                if (log)
                {
                    string valueStr = result.ToString("X2");
                    var frames = new StackTrace().GetFrames();
                    string name = frames[1].GetMethod().Name;
                    if (name == "ReadDataType")
                    {
                        int dataType = result;
                        if (extendedDataType)
                        {
                            dataType <<= 8;
                            extendedDataType = false;
                        }
                        else
                            extendedDataType = (result & 128) != 0;
                        valueStr += $" [{Reflector.InvokeMethod(typeof(BinarySerializationFormatter), "DataTypeToString", dataType)}]";
                    }
                    else if (name == "EnsureAttributes")
                        valueStr += $" [{Reflector.InvokeMethod(typeof(Enum<>).MakeGenericType(typeof(BinarySerializationFormatter).GetNestedType("TypeAttributes", BindingFlags.NonPublic)), "ToString", (int)result, " | ")}]";

                    Console.WriteLine($"byte: {result} ({valueStr}) - {GetStack()}");
                }
                return result;
            }

            public override byte[] ReadBytes(int count)
            {
                var result = base.ReadBytes(count);
                Advance(count);
                if (log)
                    Console.WriteLine($"{result.Length} bytes: {result.ToDecimalValuesString()} ({result.ToHexValuesString(",")}) - {GetStack()}");
                return result;
            }

            public override char ReadChar()
            {
                var result = base.ReadChar();
                Advance(2);
                if (log)
                    Console.WriteLine($"char: {result} ({(uint)result:X2}) - {GetStack()}");
                return result;
            }

            public override char[] ReadChars(int count)
            {
                var result = base.ReadChars(count);
                Advance(2 * count); // depends on encoding but ok for comparison
                if (log)
                    Console.WriteLine($"{result.Length} chars: {new string(result)} - {GetStack()}");
                return result;
            }

            public override decimal ReadDecimal()
            {
                var result = base.ReadDecimal();
                Advance(16);
                if (log)
                    Console.WriteLine($"decimal: {result} - {GetStack()}");
                return result;
            }

            public override double ReadDouble()
            {
                var result = base.ReadDouble();
                Advance(8);
                if (log)
                    Console.WriteLine($"double: {result:R} - {GetStack()}");
                return result;
            }

            public override short ReadInt16()
            {
                var result = base.ReadInt16();
                Advance(2);
                if (log)
                    Console.WriteLine($"short: {result} ({result:X4}) - {GetStack()}");
                return result;
            }

            public override int ReadInt32()
            {
                var result = base.ReadInt32();
                Advance(4);
                if (log)
                    Console.WriteLine($"int: {result} ({result:X8}) - {GetStack()}");
                return result;
            }

            public override long ReadInt64()
            {
                var result = base.ReadInt64();
                Advance(8);
                if (log)
                    Console.WriteLine($"long: {result} ({result:X16}) - {GetStack()}");
                return result;
            }

            public override sbyte ReadSByte()
            {
                var result = base.ReadSByte();
                Advance(1);
                if (log)
                    Console.WriteLine($"sbyte: {result} ({result:X2}) - {GetStack()}");
                return result;
            }

            public override float ReadSingle()
            {
                var result = base.ReadSingle();
                Advance(4);
                if (log)
                    Console.WriteLine($"float: {result:R} - {GetStack()}");
                return result;
            }

            public override string ReadString()
            {
                var result = base.ReadString();
                Advance(result.Length); // depends on encoding but ok for comparison
                if (log)
                    Console.WriteLine($"string: {result} - {GetStack()}");
                return result;
            }

            public override ushort ReadUInt16()
            {
                var result = base.ReadUInt16();
                Advance(2);
                if (log)
                    Console.WriteLine($"ushort: {result} ({result:X4}) - {GetStack()}");
                return result;
            }

            public override uint ReadUInt32()
            {
                var result = base.ReadUInt32();
                Advance(4);
                if (log)
                    Console.WriteLine($"uint: {result} ({result:X8}) - {GetStack()}");
                return result;
            }

            public override ulong ReadUInt64()
            {
                var result = base.ReadUInt64();
                Advance(8);
                if (log)
                    Console.WriteLine($"ulong: {result} ({result:X16}) - {GetStack()}");
                return result;
            }

            public override string ToString() => $"Position: {pos:X8}";

            #endregion

            #region Private Methods

            private void Advance(int offset)
            {
                if (log)
                    Console.Write($"{pos:X8} ");
                pos += offset;
            }

            private static string GetStack() => new StackTrace().GetFrames().Skip(2).Select(f => f.GetMethod().Name).TakeWhile(s => s != "Deserialize").Join(" < ");

            #endregion

            #endregion
        }

        #endregion

        #endregion

        #region Methods

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
                    Console.WriteLine(ms.ToArray().ToRawString());
                return ms.ToArray();
            }
        }

        private static byte[] SerializeObjects(IList<object> objects, IFormatter formatter)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                formatter.Serialize(ms, objects.Count);
                BinaryWriter bw = null;
                BinarySerializationFormatter bsf = formatter as BinarySerializationFormatter;
                if (dumpDetails && bsf != null)
                    bw = new TestWriter(ms, dumpDetails);

                for (var i = 0; i < objects.Count; i++)
                {
                    object o = objects[i];
                    long pos = ms.Position;
                    if (bw != null)
                        bsf.SerializeByWriter(bw, o);
                    else
                        formatter.Serialize(ms, o);
                    Console.WriteLine($"{i,2}. {(o == null ? "<null>" : o.GetType().GetName(TypeNameKind.ShortName))} - length: {ms.Position - pos}");
                }

                Console.WriteLine($"Full length: {ms.Length}");
                if (dumpSerContent)
                    Console.WriteLine(ms.ToArray().ToRawString());
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
                object result = br != null ? bsf.DeserializeByReader(br) : formatter.Deserialize(ms);
                Assert.AreEqual(ms.Length, ms.Position, "Stream was not read until the end");
                return result;
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
                Assert.AreEqual(ms.Length, ms.Position, "Stream was not read until the end");
                return result;
            }
        }

        private static void SystemSerializeObject(object obj, string title = null, bool safeCompare = false, SerializationBinder binder = null, ISurrogateSelector surrogateSelector = null)
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
#if NETFRAMEWORK && NET40_OR_GREATER
                    if (!AppDomain.CurrentDomain.IsFullyTrusted)
                        return;
#endif
                    if (safeCompare)
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

        private static void SystemSerializeObjects(IList<object> referenceObjects, string title = null, bool safeCompare = false, SerializationBinder binder = null, ISurrogateSelector surrogateSelector = null)
        {
            if (title == null)
                title = $"Items Count: {referenceObjects.Count}";
            Console.WriteLine($"------------------System BinaryFormatter ({title})--------------------");
            using (new TestExecutionContext.IsolatedContext())
            {
                BinaryFormatter bf = new BinaryFormatter { Binder = binder, SurrogateSelector = surrogateSelector };
                try
                {
                    byte[] serData = SerializeObjects(referenceObjects, bf);
                    object[] deserializedObjects = DeserializeObjects(serData, bf);
#if NETFRAMEWORK && NET40_OR_GREATER
                    if (!AppDomain.CurrentDomain.IsFullyTrusted)
                        return;
#endif
                    if (safeCompare)
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

        private  static void KGySerializeObject(object obj, BinarySerializationOptions options, string title = null, bool safeCompare = false, SerializationBinder binder = null, ISurrogateSelector surrogateSelector = null)
        {
            if (title == null)
                title = obj.GetType().ToString();
            Console.WriteLine($"------------------KGy SOFT BinarySerializer ({title} - {options})--------------------");
            BinarySerializationFormatter bsf = new BinarySerializationFormatter(options) { Binder = binder, SurrogateSelector = surrogateSelector };
            try
            {
                byte[] serData = SerializeObject(obj, bsf);
                object deserializedObject = DeserializeObject(serData, bsf);
                if (safeCompare)
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

        private static void KGySerializeObjects(IList<object> referenceObjects, BinarySerializationOptions options, string title = null, bool safeCompare = false, SerializationBinder binder = null, ISurrogateSelector surrogateSelector = null)
        {
            if (title == null)
                title = $"Items Count: {referenceObjects.Count}";
            Console.WriteLine($"------------------KGy SOFT BinarySerializer ({title} - {options})--------------------");
            BinarySerializationFormatter bsf = new BinarySerializationFormatter(options) { Binder = binder, SurrogateSelector = surrogateSelector };
            try
            {
                byte[] serData = SerializeObjects(referenceObjects, bsf);
                object[] deserializedObjects = DeserializeObjects(serData, bsf);
                if (safeCompare)
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
    }
}
