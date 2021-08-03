#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXDataNodeTest.cs
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
using System.Collections.ObjectModel;
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
#if NET5_0 || NET6_0
#pragma warning disable SYSLIB0011 // Type or member is obsolete - this class uses BinaryFormatter for security tests
#pragma warning disable IDE0079 // Remove unnecessary suppression - CS0618 is emitted by ReSharper
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
            var writer = new ResXResourceWriter(new StringWriter(sb));
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
            var rs = new ResXResourceSet(path, null) { SafeMode = true };
            var node = (ResXDataNode)rs.GetObject("string");

            Assert.IsNotNull(node.ValueData);
            node.GetValue(cleanupRawData: true);
            Assert.IsNull(node.ValueData);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SafeModeWithTypeConverterTest(bool customResolver)
        {
            var nodeInfo = new DataNodeInfo()
            {
                Name = "dangerous",
                TypeName = "MyNamespace.DangerousType, DangerousAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            };

            var nodeRaw = new ResXDataNode(nodeInfo, null);
            var resolver = customResolver ? new TestTypeResolver() : null;

            Throws<TypeLoadException>(() => nodeRaw.GetValueSafe(resolver));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SafeModeWithFileRefTest(bool customResolver)
        {
            var fileRef = new ResXFileRef("fileName", "MyNamespace.DangerousType, DangerousAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", null);
            var nodeRaw = new ResXDataNode("dangerous", fileRef);
            var resolver = customResolver ? new TestTypeResolver() : null;

            Throws<TypeLoadException>(() => nodeRaw.GetValueSafe(resolver));
        }


        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SafeModeWithFormatterTest(bool compatibleFormat, bool customResolver)
        {
#if NET35
            if (compatibleFormat)
                Assert.Inconclusive("In .NET 3.5 cannot create the hacked payload in compatible format because SerializationBinder.BindToName method is not supported");
#endif
            const string asmName = "DangerousAssembly, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
            const string typeName = "MyNamespace.DangerousType";
            var proxyType = typeof(object);
            var proxyInstance = Reflector.CreateInstance(proxyType);
            var nodeWithObject = new ResXDataNode("dangerous", proxyInstance);
            var info = nodeWithObject.GetDataNodeInfo(null, compatibleFormat);

            // hacking the content as if it was from another assembly
            IFormatter formatter = compatibleFormat ? new BinaryFormatter() : new BinarySerializationFormatter(BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes);
            formatter.Binder = new CustomSerializationBinder
            {
                AssemblyNameResolver = t => t == proxyType ? asmName : null,
                TypeNameResolver = t => t == proxyType ? typeName : null,
            };
            using var stream = new MemoryStream();
            formatter.Serialize(stream, proxyInstance);
            info.ValueData = Convert.ToBase64String(stream.ToArray());

            var resolver = customResolver ? new TestTypeResolver() : null;
            var nodeRaw = new ResXDataNode(info, null);

            Throws<SerializationException>(() => nodeRaw.GetValue(resolver));
            Throws<SerializationException>(() => nodeRaw.GetValueSafe(resolver));
        }

        #endregion
    }
}
