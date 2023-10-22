#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXDataNodeTest.cs
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using KGySoft.Reflection;
using KGySoft.Resources;
using KGySoft.Serialization.Binary;

using NUnit.Framework;

#endregion

#region Suppressions

#if NET
#if NET5_0 || NET6_0 || NET7_0 || NET8_0
#pragma warning disable SYSLIB0011 // Type or member is obsolete - this class uses BinaryFormatter for security tests
#pragma warning disable CS0618 // Use of obsolete symbol - as above  
#else
#error Check whether IFormatter is still available in this .NET version
#endif
#endif

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Resources
{
    [TestFixture]
    public class ResXDataNodeTest : TestBase
    {
        #region Methods

        [Test]
        public void FromObject()
        {
            var node = new ResXDataNode("int", 1);

            Assert.IsNull(node.MimeType);
            Assert.IsNull(node.TypeName);
            Assert.IsNull(node.ValueData);
            Assert.IsNull(node.FileRef);

            var sb = new StringBuilder();
            var writer = new ResXResourceWriter(new StringWriter(sb)) { CompatibleFormat = false };
            writer.AddResource(node);

            // serializing generates the .resx info - int: type and ValueData
            Assert.IsNull(node.MimeType);
            Assert.IsNull(node.FileRef);
            Assert.IsNotNull(node.TypeName);
            Assert.IsNotNull(node.ValueData);

            // a cleanup deletes the .resx info
            node.GetValue(cleanupRawData: true);
            Assert.IsNull(node.MimeType);
            Assert.IsNull(node.TypeName);
            Assert.IsNull(node.ValueData);
            Assert.IsNull(node.FileRef);

            node = new ResXDataNode("object", new object());
            Assert.IsNull(node.MimeType);
            Assert.IsNull(node.TypeName);
            Assert.IsNull(node.ValueData);
            Assert.IsNull(node.FileRef);

            // serializing generates the .resx info - object: mime and ValueData
            writer.AddResource(node);
            Assert.IsNotNull(node.MimeType);
            Assert.IsNull(node.FileRef);
            Assert.IsNull(node.TypeName);
            Assert.IsNotNull(node.ValueData);
        }

        [Test]
        public void FromFileRef()
        {
            var node = new ResXDataNode("fileref", new ResXFileRef("path", typeof(string)));

            Assert.IsNull(node.MimeType);
            Assert.IsNull(node.TypeName);
            Assert.IsNull(node.ValueData);
            Assert.IsNotNull(node.FileRef);

            var sb = new StringBuilder();
            var writer = new ResXResourceWriter(new StringWriter(sb));
            writer.AddResource(node);

            // serializing generates the .resx info - int: type and ValueData
            Assert.IsNull(node.MimeType);
            Assert.IsNotNull(node.FileRef);
            Assert.IsNotNull(node.TypeName);
            Assert.IsNotNull(node.ValueData);
        }

        [Test]
        public void FromNodeInfo()
        {
            var path = Combine(Files.GetExecutingPath(), "Resources", "TestRes.resx");
            var rs = new ResXResourceSet(path) { SafeMode = true };
            var node = (ResXDataNode)rs.GetObject("string");

            Assert.IsNotNull(node!.ValueData);
            node.GetValue(cleanupRawData: true);
            Assert.IsNull(node.ValueData);
        }

        [Test]
        [Obsolete]
        public void SafeModeWithTypeConverterTest()
        {
            var nodeInfo = new DataNodeInfo
            {
                Name = "dangerous",
                TypeName = "MyNamespace.DangerousType, DangerousAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            };

            var nodeRaw = new ResXDataNode(nodeInfo, null);
            Throws<NotSupportedException>(() => nodeRaw.GetValueSafe(new TestTypeResolver()));
        }

        [Test]
        public void SafeModeWithFileRefTest()
        {
            var fileRef = new ResXFileRef("fileName", "MyNamespace.DangerousType, DangerousAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", null);
            var nodeRaw = new ResXDataNode("dangerous", fileRef);
            Throws<NotSupportedException>(() => nodeRaw.GetValueSafe(), Res.ResourcesFileRefFileNotSupportedSafeMode(nodeRaw.Name));
        }

        [Test]
        [Obsolete]
        public void SafeModeWithFileRefTestWithResolver()
        {
            var fileRef = new ResXFileRef("fileName", "MyNamespace.DangerousType, DangerousAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", null);
            var nodeRaw = new ResXDataNode("dangerous", fileRef);

            Throws<NotSupportedException>(() => nodeRaw.GetValueSafe(new TestTypeResolver()), Res.ResourcesTypeResolverInSafeModeNotSupported);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SafeModeWithCompatibleFormatterTest(bool customResolver)
        {
#if NET35
            Assert.Inconclusive("In .NET 3.5 cannot create the hacked payload in compatible format because SerializationBinder.BindToName method is not supported");
#elif NET8_0_OR_GREATER
            Assert.Inconclusive("Cannot test compatible format in .NET 8.0 and above because BinaryFormatter is no longer supported");
#endif
            const string asmName = "DangerousAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            const string typeName = "MyNamespace.DangerousType";
            var proxyType = typeof(object);
            var proxyInstance = Reflector.CreateInstance(proxyType);
            var nodeWithObject = new ResXDataNode("dangerous", proxyInstance);
            var info = nodeWithObject.GetDataNodeInfo(null, true);

            // hacking the content as if it was from another assembly
            var formatter = new BinaryFormatter
            {
                Binder = new CustomSerializationBinder
                {
                    AssemblyNameResolver = t => t == proxyType ? asmName : null,
                    TypeNameResolver = t => t == proxyType ? typeName : null,
                }
            };

            using var stream = new MemoryStream();
            formatter.Serialize(stream, proxyInstance);
            info.ValueData = Convert.ToBase64String(stream.ToArray());

            var resolver = customResolver ? new TestTypeResolver() : null;
            var nodeRaw = new ResXDataNode(info, null);

            Throws<SerializationException>(() => nodeRaw.GetValue(resolver), asmName);
            Throws<SerializationException>(() => nodeRaw.GetValueSafe(), Res.ResourcesBinaryFormatterSafeModeNotSupported(nodeRaw.Name, 0, 0));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SafeModeWithIncompatibleFormatterTest(bool customResolver)
        {
            const string asmName = "DangerousAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            const string typeName = "MyNamespace.DangerousType";
            var proxyType = typeof(object);
            var proxyInstance = Reflector.CreateInstance(proxyType);
            var nodeWithObject = new ResXDataNode("dangerous", proxyInstance);
            var info = nodeWithObject.GetDataNodeInfo(null, false);

            // hacking the content as if it was from another assembly
            var formatter = new BinarySerializationFormatter(BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes)
            {
                Binder = new CustomSerializationBinder
                {
                    AssemblyNameResolver = t => t == proxyType ? asmName : null,
                    TypeNameResolver = t => t == proxyType ? typeName : null,
                }
            };

            using var stream = new MemoryStream();
            formatter.SerializeToStream(stream, proxyInstance);
            info.ValueData = Convert.ToBase64String(stream.ToArray());

            var resolver = customResolver ? new TestTypeResolver() : null;
            var nodeRaw = new ResXDataNode(info, null);

            Throws<SerializationException>(() => nodeRaw.GetValue(resolver), asmName);
            Throws<SerializationException>(() => nodeRaw.GetValueSafe(), "Unexpected type name in safe mode: MyNamespace.DangerousType, DangerousAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null.");
        }

        [Test]
        public void RegenerateInCompatibleMode()
        {
            // Creating a node with a cached data
            var nodeSrc = new ResXDataNode("test", new List<ConsoleColor> { ConsoleColor.Blue });
            
            // List is written by BinarySerializationFormatter where ConsoleColor type is stored by name
            DataNodeInfo nodeInfoNonCompatible = nodeSrc.GetDataNodeInfo(null, false);
            Assert.IsFalse(nodeInfoNonCompatible.CompatibleFormat);

            // creating a clone in non-compatible format without the cached value
            var clone = new ResXDataNode(nodeInfoNonCompatible, null);

            // Trying to convert the clone to a compatible-format: deserialization occurs without an expected type so safe mode should fail here to prevent a security hole
            Throws<SerializationException>(() => clone.GetDataNodeInfo(null, true), "System.ConsoleColor");

#if !NET8_0_OR_GREATER
            // but it works in non-safe mode (when BinaryFormatter is available)
            DataNodeInfo nodeInfoCompatible = clone.GetDataNodeInfo(null, true, false);
            Assert.IsTrue(nodeInfoCompatible.CompatibleFormat);
#endif
        }

        #endregion
    }
}
