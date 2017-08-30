using System.IO;
using System.Text;
using KGySoft.Libraries;
using KGySoft.Libraries.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Libraries.Resources
{
    [TestClass]
    public class ResXDataNodeTest: TestBase
    {
        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public void FromNodeInfo()
        {
            var path = Path.Combine(Files.GetExecutingPath(), "Resources\\TestRes.resx");
            var rs = new ResXResourceSet(path, null) { SafeMode = true };
            var node = (ResXDataNode)rs.GetObject("string");

            Assert.IsNotNull(node.ValueData);
            node.GetValue(cleanupRawData: true);
            Assert.IsNull(node.ValueData);
        }
    }
}
