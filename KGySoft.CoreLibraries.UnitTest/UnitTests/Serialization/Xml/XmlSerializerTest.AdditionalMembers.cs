#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: XmlSerializerTest.AdditionalMembers.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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
using System.Linq;
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

        private static void SystemSerializeObject(object obj)
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

        private static void SystemSerializeObjects(object[] referenceObjects)
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

        private static XElement KGySerializeObject(object obj, XmlSerializationOptions options, bool hasRandomContent = false, bool safeMode = true, IList<Type> expectedTypes = null)
        {
            Type type = obj.GetType();
            if (expectedTypes == null && safeMode)
                expectedTypes = GetExpectedTypes(obj);
            Console.WriteLine($"------------------KGySoft XmlSerializer ({type} - options: {options.ToString<XmlSerializationOptions>()})--------------------");
            try
            {
                // XElement - as object
                //Console.WriteLine(".....As object.....");
                XElement xElement = KGyXmlSerializer.Serialize(obj, options);
                Console.WriteLine(xElement);
                object deserializedObject = safeMode ? KGyXmlSerializer.DeserializeSafe(xElement, expectedTypes) : KGyXmlSerializer.Deserialize(xElement);
                AssertDeepEquals(obj, deserializedObject, true);

                // XmlReader/Writer - as object
                StringBuilder sb = new StringBuilder();
                using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true }))
                {
                    KGyXmlSerializer.Serialize(writer, obj, options);
                }

                // deserialize by reader - if file already contains unescaped newlines: // new XmlTextReader(new StringReader(sb.ToString()));
                using (var reader = XmlReader.Create(new StringReader(sb.ToString()), new XmlReaderSettings { CloseInput = true }))
                {
                    deserializedObject = safeMode ? KGyXmlSerializer.DeserializeSafe(reader, expectedTypes) : KGyXmlSerializer.Deserialize(reader);
                }

                AssertDeepEquals(obj, deserializedObject, true);
                if (!hasRandomContent)
                    Assert.AreEqual(xElement.ToString(), sb.ToString(), "XElement and XmlWriter Serialize are not compatible");

                // XElement - as component
                //Console.WriteLine();
                //Console.WriteLine(".....As component.....");
                var xElementComp = new XElement("test");
                KGyXmlSerializer.SerializeContent(xElementComp, obj, options);
                //Console.WriteLine(xElementComp);
                deserializedObject = type.IsArray ? Array.CreateInstance(type.GetElementType(), ((Array)obj).Length) : Reflector.CreateInstance(type);
                if (safeMode)
                    KGyXmlSerializer.DeserializeContentSafe(xElementComp, deserializedObject, expectedTypes);
                else
                    KGyXmlSerializer.DeserializeContent(xElementComp, deserializedObject);
                AssertDeepEquals(obj, deserializedObject, true);

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
                    if (safeMode)
                        KGyXmlSerializer.DeserializeContentSafe(reader, deserializedObject, expectedTypes);
                    else
                        KGyXmlSerializer.DeserializeContent(reader, deserializedObject);
                    reader.ReadEndElement();
                }

                AssertDeepEquals(obj, deserializedObject, true);
                Assert.AreEqual(xElementComp.ToString(), sb.ToString(), "XElement and XmlWriter SerializeContent are not compatible");
                return xElement;
            }
            catch (Exception e)
            {
                Console.WriteLine($"KGySoft serialization failed: {e}");
                throw;
            }
        }

        private static void KGySerializeObjects(object[] referenceObjects, XmlSerializationOptions options, bool alsoAsContent = true, bool safeMode = true, IList<Type> expectedTypes = null)
        {
            Console.WriteLine($"------------------KGySoft XmlSerializer (Items Count: {referenceObjects.Length}; options: {options.ToString<XmlSerializationOptions>()})--------------------");
            if (expectedTypes == null && safeMode)
                expectedTypes = GetExpectedTypes(referenceObjects);
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
                            if (safeMode)
                                KGyXmlSerializer.DeserializeContentSafe(xItem, deserXElement, expectedTypes);
                            else
                                KGyXmlSerializer.DeserializeContent(xItem, deserXElement);
                            deserReader = itemType.IsArray ? item.MemberwiseClone() : Reflector.CreateInstance(itemType);
                            itemReader.Read(); // to node "itemContent"
                            if (safeMode)
                                KGyXmlSerializer.DeserializeContentSafe(itemReader, deserReader, expectedTypes);
                            else
                                KGyXmlSerializer.DeserializeContent(itemReader, deserReader);
                            itemReader.ReadEndElement();
                        }

                        AssertDeepEquals(item, deserXElement, true);
                        AssertDeepEquals(item, deserReader, true);
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
                            object deserXElement = safeMode ?  KGyXmlSerializer.DeserializeSafe(element, expectedTypes) : KGyXmlSerializer.Deserialize(element);
                            object deserReader = safeMode ? KGyXmlSerializer.DeserializeSafe(reader, expectedTypes) : KGyXmlSerializer.Deserialize(reader);
                            AssertDeepEquals(deserXElement, deserReader, true);

                            deserializedObjects.Add(deserXElement);
                        }

                    }
                    finally
                    {
                        reader.Close();
                    }
                }

                AssertItemsEqual(referenceObjects, deserializedObjects.ToArray(), true);

                Assert.AreEqual(xElement.ToString(), sb.ToString(), "XElement and XmlWriter serializers are not compatible");
            }
            catch (Exception e)
            {
                Console.WriteLine($"KGySoft serialization failed: {e}");
                throw;
            }
        }

        private static IList<Type> GetExpectedTypes(object o)
        {
            if (o == null)
                return null;

            var result = new List<Type> { o.GetType() };
            if (o is IList<object> list)
                result.AddRange(list.Where(i => i != null).Select(i => i.GetType()));
            return result;
        }

        #endregion
    }
}
