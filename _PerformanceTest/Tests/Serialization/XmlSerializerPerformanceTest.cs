using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using KGySoft.Diagnostics;
using KGySoft.Serialization;
using NUnit.Framework;
using SystemXmlSerializer = System.Xml.Serialization.XmlSerializer;

namespace _PerformanceTest.Tests.Serialization
{
    /// <summary>
    /// Summary description for BinarySerializerTest
    /// </summary>
    [TestFixture]
    public class XmlSerializerPerformanceTest
    {
        public class FullExtraComponent
        {
            public class TestInner
            {
                public string InnerString { get; set; }

                public int InnerInt { get; set; }

                public TestInner()
                {
                    InnerString = "InnerStringValue";
                    InnerInt = 15;
                }

                public override bool Equals(object obj)
                {
                    if (obj == null || obj.GetType() != typeof(TestInner))
                        return base.Equals(obj);

                    TestInner other = (TestInner)obj;
                    return InnerString == other.InnerString && InnerInt == other.InnerInt;
                }
            }

            public struct InnerStructure
            {
                public string InnerString { get; set; }

                public int InnerInt { get; set; }

                public InnerStructure(string s, int i)
                    : this()
                {
                    InnerString = s;
                    InnerInt = i;
                }
            }

            [DefaultValue(0)]
            public int IntProp { get; set; }

            public FullExtraComponent()
            {
            }

            public int[] IntArray { get; set; }

            readonly int[] readOnlyIntArray = new int[5];
            public int[] ReadOnlyIntArray
            {
                get { return readOnlyIntArray; }
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public TestInner Inner { get; set; }

            private readonly List<TestInner> innerList = new List<TestInner>();

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public List<TestInner> InnerList
            {
                get { return innerList; }
            }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public TestInner[] InnerArray { get; set; }

            public Point Point { get; set; }

            public Point[] PointArray { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public InnerStructure Structure { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public InnerStructure[] StructureArray { get; set; }

            [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
            public List<InnerStructure> StructureList { get; set; }

            public string StringValue { get; set; }

            public FullExtraComponent(bool init)
            {
                if (init)
                {
                    IntProp = 1;
                    Inner = new TestInner();
                    IntArray = new int[] { 1, 2, 3, 4, 5 };
                    readOnlyIntArray = new int[] { 1, 2, 3, 4, 5 };
                    innerList = new List<TestInner>
                    {
                        new TestInner {InnerInt = 1, InnerString = "Egy"},
                        new TestInner {InnerInt = 2, InnerString = "Kettő"},
                        null
                    };
                    Point = new Point(13, 13);
                    PointArray = new Point[] { new Point(1, 2), new Point(3, 4) };
                    InnerArray = new TestInner[] { new TestInner { InnerInt = 1, InnerString = "Egy" }, new TestInner { InnerInt = 2, InnerString = "Kettő" } };
                    Structure = new InnerStructure("InnerStructureString", 13);
                    StructureArray = new InnerStructure[] { new InnerStructure("Egyeske", 1), new InnerStructure("Ketteske", 2), };
                    StructureList = new List<InnerStructure> { new InnerStructure("Első", 1), new InnerStructure("Második", 2) };
                    StringValue = String.Empty;
                }
            }
        }

        private static object[] SerializerTestSource =>
            new object[]
            {
                new byte[] { 1, 2, 3, 4, 5 },
                new int[] { 1, 2, 3, 4, 5 },
                1,
                new List<int>(new int[] { 1, 2, 3, 4, 5 }),
                new HashSet<int> { 1, 2, 3, 4, 5 },
                new HashSet<int[]> { new int[] { 1, 2, 3, 4, 5 }, null },
                new Collection<int>{ 1, 2, 3, 4, 5 },
                new DictionaryEntry(new object(), "alma"),
                new FullExtraComponent(true)
            };

        [TestCaseSource(nameof(SerializerTestSource))]
        public void SerializerTest(object obj)
        {
            new PerformanceTest<string>
                {
                    TestName = "XmlSerializer performance test",
                    Iterations = 1, WarmUp = false, SortBySize = true, Repeat = 2 // comment this line for time test and uncomment for size test
                }
                .AddCase(() =>
                {
                    var serializer = new SystemXmlSerializer(obj.GetType());
                    var sb = new StringBuilder();
                    using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                        serializer.Serialize(writer, obj);
                    return sb.ToString();
                }, "System.XmlSerializer")
                .AddCase(() =>
                {
                    var sb = new StringBuilder();
                    using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                        XmlSerializer.Serialize(writer, obj, XmlSerializationOptions.RecursiveSerializationAsFallback | XmlSerializationOptions.IgnoreShouldSerialize | XmlSerializationOptions.IgnoreDefaultValueAttribute);
                    return sb.ToString();
                }, "KGySoft.XmlSerializer by XmlWriter")
                .AddCase(() => XmlSerializer.Serialize(obj, XmlSerializationOptions.RecursiveSerializationAsFallback | XmlSerializationOptions.IgnoreShouldSerialize | XmlSerializationOptions.IgnoreDefaultValueAttribute).ToString(),
                    "KGySoft.XmlSerializer by LINQ")
                .DoTest()
                .DumpResults(Console.Out, dumpReturnValue: true);
        }
    }
}
