#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: XmlSerializerPerformanceTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.ComponentModel;

#region Used Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Xml;

using KGySoft.Serialization.Xml;

using NUnit.Framework;

#endregion

#region Used Aliases

using SystemXmlSerializer = System.Xml.Serialization.XmlSerializer;

#endregion

#endregion

namespace KGySoft.CoreLibraries.PerformanceTests.Serialization
{
    [TestFixture]
    public class XmlSerializerPerformanceTest
    {
        #region Nested classes

        #region Person class

        public class Person
        {
            #region Properties

            public Guid Id { get; set; }
            public string FirstName { get; set; }

            [DefaultValue(null)]
            public string MiddleName { get; set; }
            public string LastName { get; set; }
            public DateTime BirthDate { get; set; }
            public PersonalDocument[] Documents { get; set; }

            #endregion
        }

        #endregion

        #region PersonalDocument class

        public class PersonalDocument
        {
            #region Properties

            public string DocumentType { get; set; }
            public string DocumentId { get; set; }

            #endregion
        }

        #endregion

        #endregion

        #region Properties

        private static object[] SerializerTestSource => new object[]
        {
            1,
            new byte[] { 1, 2, 3, 4, 5 },
            new int[] { 1, 2, 3, 4, 5 },
            new ConsoleColor[] { ConsoleColor.Black, ConsoleColor.Blue, ConsoleColor.Cyan, ConsoleColor.Magenta },
            new List<int>(new int[] { 1, 2, 3, 4, 5 }),
            new HashSet<int> { 1, 2, 3, 4, 5 },
            new HashSet<int[]> { new int[] { 1, 2, 3, 4, 5 }, null },
            new Collection<int> { 1, 2, 3, 4, 5 },
            new DictionaryEntry(new object(), "dummy"),
#if NET47_OR_GREATER || !NETFRAMEWORK
            (1, "alpha"),  
#endif
            ThreadSafeRandom.Instance.NextObject<Person>()
        };

        #endregion

        #region Methods

        [TestCaseSource(nameof(SerializerTestSource))]
        public void SerializerTest(object obj)
        {
            new PerformanceTest<object> { TestName = $"XmlSerializer Speed Test - {obj.GetType()}", Iterations = 1000, Repeat = 3 }
                .AddCase(() =>
                {
                    var serializer = new SystemXmlSerializer(obj.GetType());
                    var sb = new StringBuilder();
                    using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                        serializer.Serialize(writer, obj);
                    return serializer.Deserialize(new StringReader(sb.ToString()));
                }, "System XmlSerializer")
                .AddCase(() =>
                {
                    var sb = new StringBuilder();
                    using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                        XmlSerializer.Serialize(writer, obj);
                    return XmlSerializer.Deserialize(new StringReader(sb.ToString()));
                }, "KGy SOFT XmlSerializer by XmlWriter")
                .AddCase(() => XmlSerializer.Deserialize(XmlSerializer.Serialize(obj)),
                    "KGy SOFT XmlSerializer by XElement")
                .DoTest()
                .DumpResults(Console.Out);

            new PerformanceTest<string> { TestName = $"XmlSerializer Size Test - {obj.GetType()}", Iterations = 1, SortBySize = true }
                .AddCase(() =>
                {
                    var serializer = new SystemXmlSerializer(obj.GetType());
                    var sb = new StringBuilder();
                    using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                        serializer.Serialize(writer, obj);
                    return sb.ToString();
                }, "System XmlSerializer")
                .AddCase(() =>
                {
                    var sb = new StringBuilder();
                    using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                        XmlSerializer.Serialize(writer, obj);
                    return sb.ToString();
                }, "KGy SOFT XmlSerializer")
                .DoTest()
                .DumpResults(Console.Out, dumpReturnValue: true);

        }

        #endregion
    }
}
