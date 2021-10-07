#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: XmlSerializerTest.AdditionalMembers.cs
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

#region Used Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

using KGySoft.Reflection;
using KGySoft.Serialization.Xml;

using NUnit.Framework;
using NUnit.Framework.Internal;

#endregion

#region Used Aliases

using KGyXmlSerializer = KGySoft.Serialization.Xml.XmlSerializer;
using SystemXmlSerializer = System.Xml.Serialization.XmlSerializer;

#endregion

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Serialization.Xml
{
    partial class XmlSerializerTest
    {
        #region Methods

        private void SystemSerializeObject(object obj)
        {
            using (new TestExecutionContext.IsolatedContext())
            {
                Type type = obj.GetType();
                Console.WriteLine($"------------------System XmlSerializer ({type})--------------------");
                try
                {
                    SystemXmlSerializer serializer = new SystemXmlSerializer(type);
                    StringBuilder sb = new StringBuilder();
                    using XmlWriter xmlWriter = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true });
                    serializer.Serialize(xmlWriter, obj);

                    Console.WriteLine(sb);
                    object deserializedObject = serializer.Deserialize(new StringReader(sb.ToString()));
                    AssertDeepEquals(obj, deserializedObject);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"System serialization failed: {e}");
                }
            }
        }

        private void SystemSerializeObjects(object[] referenceObjects)
        {
            using (new TestExecutionContext.IsolatedContext())
            {
                Console.WriteLine($"------------------System XmlSerializer (Items Count: {referenceObjects.Length})--------------------");
                try
                {
                    List<object> deserializedObjects = new List<object>();
                    foreach (object item in referenceObjects)
                    {
                        if (item == null)
                        {
                            Console.WriteLine("Skipping null");
                            deserializedObjects.Add(null);
                            continue;
                        }

                        SystemXmlSerializer serializer = new SystemXmlSerializer(item.GetType());
                        StringBuilder sb = new StringBuilder();
                        using XmlWriter xmlWriter = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true });
                        serializer.Serialize(xmlWriter, item);

                        Console.WriteLine(sb);
                        Console.WriteLine();
                        deserializedObjects.Add(serializer.Deserialize(new StringReader(sb.ToString())));
                    }

                    AssertItemsEqual(referenceObjects, deserializedObjects.ToArray());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"System serialization failed: {e}");
                }
            }
        }

        private void KGySerializeObject(object obj, XmlSerializationOptions options, bool randomContent = false)
        {
            Type type = obj.GetType();
            Console.WriteLine($"------------------KGySoft XmlSerializer ({type} - options: {options.ToString<XmlSerializationOptions>()})--------------------");
            try
            {
                // XElement - as object
                //Console.WriteLine(".....As object.....");
                XElement xElement = KGyXmlSerializer.Serialize(obj, options);
                Console.WriteLine(xElement);
                object deserializedObject = KGyXmlSerializer.Deserialize(xElement);
                AssertDeepEquals(obj, deserializedObject);

                // XmlReader/Writer - as object
                StringBuilder sb = new StringBuilder();
                using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                {
                    KGyXmlSerializer.Serialize(writer, obj, options);
                }

                // deserialize by reader - if file already contains unescaped newlines: // new XmlTextReader(new StringReader(sb.ToString()));
                using (var reader = XmlReader.Create(new StringReader(sb.ToString()), new XmlReaderSettings { CloseInput = true }))
                {
                    deserializedObject = KGyXmlSerializer.Deserialize(reader);
                }

                AssertDeepEquals(obj, deserializedObject);
                if (!randomContent)
                    Assert.AreEqual(xElement.ToString(), sb.ToString(), "XElement and XmlWriter Serialize are not compatible");

                // XElement - as component
                //Console.WriteLine();
                //Console.WriteLine(".....As component.....");
                var xElementComp = new XElement("test");
                KGyXmlSerializer.SerializeContent(xElementComp, obj, options);
                //Console.WriteLine(xElementComp);
                deserializedObject = type.IsArray ? Array.CreateInstance(type.GetElementType(), ((Array)obj).Length) : Reflector.CreateInstance(type);
                KGyXmlSerializer.DeserializeContent(xElementComp, deserializedObject);
                AssertDeepEquals(obj, deserializedObject);

                // XmlReader/Writer - as component
                sb = new StringBuilder();
                using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                {
                    writer.WriteStartElement("test");
                    KGyXmlSerializer.SerializeContent(writer, obj, options);
                    writer.WriteFullEndElement();
                    writer.Flush();
                }

                // deserialize by reader - if file already contains unescaped newlines: // new XmlTextReader(new StringReader(sb.ToString()));
                using (var reader = XmlReader.Create(new StringReader(sb.ToString()), new XmlReaderSettings { CloseInput = true, IgnoreWhitespace = true }))
                {
                    deserializedObject = type.IsArray ? Array.CreateInstance(type.GetElementType(), ((Array)obj).Length) : Reflector.CreateInstance(type);
                    reader.Read(); // to node "test"
                    KGyXmlSerializer.DeserializeContent(reader, deserializedObject);
                    reader.ReadEndElement();
                }

                AssertDeepEquals(obj, deserializedObject);
                Assert.AreEqual(xElementComp.ToString(), sb.ToString(), "XElement and XmlWriter SerializeContent are not compatible");
            }
            catch (Exception e)
            {
                Console.WriteLine($"KGySoft serialization failed: {e}");
                throw;
            }
        }

        private void KGySerializeObjects(object[] referenceObjects, XmlSerializationOptions options, bool alsoAsContent = true)
        {
            Console.WriteLine($"------------------KGySoft XmlSerializer (Items Count: {referenceObjects.Length}; options: {options.ToString<XmlSerializationOptions>()})--------------------");
            try
            {
                XElement xElement = new XElement("test");
                StringBuilder sb = new StringBuilder();
                using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true }))
                {
                    writer.WriteStartElement("test");

                    foreach (var item in referenceObjects)
                    {
                        xElement.Add(KGyXmlSerializer.Serialize(item, options));
                        KGyXmlSerializer.Serialize(writer, item, options);

                        if (!alsoAsContent)
                            continue;

                        // content serialization test for element
                        if (item == null)
                        {
                            Console.WriteLine("Skipping null");
                            continue;
                        }

                        XElement xItem = new XElement("itemContent");
                        StringBuilder sbItem = new StringBuilder();
                        using (XmlWriter itemWriter = XmlWriter.Create(sbItem, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true }))
                        {
                            KGyXmlSerializer.SerializeContent(xItem, item, options);
                            Console.WriteLine(xItem);
                            Console.WriteLine();
                            itemWriter.WriteStartElement("itemContent");
                            KGyXmlSerializer.SerializeContent(itemWriter, item, options);
                            itemWriter.WriteFullEndElement();
                        }

                        object deserXElement;
                        object deserReader;
                        using (XmlReader itemReader = XmlReader.Create(new StringReader(sbItem.ToString()), new XmlReaderSettings { IgnoreWhitespace = true }))
                        {
                            var itemType = item.GetType();
                            deserXElement = itemType.IsArray ? item.MemberwiseClone() : Reflector.CreateInstance(itemType);
                            KGyXmlSerializer.DeserializeContent(xItem, deserXElement);
                            deserReader = itemType.IsArray ? item.MemberwiseClone() : Reflector.CreateInstance(itemType);
                            itemReader.Read(); // to node "itemContent"
                            KGyXmlSerializer.DeserializeContent(itemReader, deserReader);
                            itemReader.ReadEndElement();
                        }

                        AssertDeepEquals(item, deserXElement);
                        AssertDeepEquals(item, deserReader);
                        Assert.AreEqual(xItem.ToString(), sbItem.ToString(), "XElement and XmlWriter serializers are not compatible");
                    }
                    writer.WriteEndDocument();
                    writer.Flush();
                }

                Console.WriteLine(xElement);

                List<object> deserializedObjects = new List<object>();
                // deserialize by reader - if file already contains unescaped newlines: // new XmlTextReader(new StringReader(sb.ToString()));
                using (XmlReader reader = XmlReader.Create(new StringReader(sb.ToString()), new XmlReaderSettings { IgnoreWhitespace = true }))
                {
                    try
                    {
                        reader.Read(); // test
                        foreach (XElement element in xElement.Elements())
                        {
                            object deserXElement = KGyXmlSerializer.Deserialize(element);
                            object deserReader = KGyXmlSerializer.Deserialize(reader);
                            AssertDeepEquals(deserXElement, deserReader);

                            deserializedObjects.Add(deserXElement);
                        }

                    }
                    finally
                    {
                        reader.Close();
                    }
                }

                AssertItemsEqual(referenceObjects, deserializedObjects.ToArray());

                Assert.AreEqual(xElement.ToString(), sb.ToString(), "XElement and XmlWriter serializers are not compatible");
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
