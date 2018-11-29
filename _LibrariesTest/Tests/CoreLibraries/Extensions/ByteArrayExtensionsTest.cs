using System;
using System.Linq;
using KGySoft.CoreLibraries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Tests.CoreLibraries.Extensions
{

    [TestClass]
    public class ByteArrayExtensionsTest : TestBase
    {
        [TestMethod]
        public void ToBase64String()
        {
            byte[] bytes = Enumerable.Range(0, 255).Select(i => (byte)i).ToArray();
            string result = bytes.ToBase64String();
            Assert.IsFalse(Char.IsWhiteSpace(result[0]));
            Assert.IsFalse(result.Contains(Environment.NewLine));

            result = bytes.ToBase64String(80);
            Assert.IsFalse(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToBase64String(80, 6);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));
        }

        [TestMethod]
        public void ToHexString()
        {
            byte[] bytes = Enumerable.Range(1, 3).Select(i => (byte)i).ToArray();

            string result = bytes.ToHexValuesString(null, 0, 2, ' ', true);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsFalse(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(null, 6, 2, ' ', true);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsFalse(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(null, 4, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(null, 3, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(null, 1, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(", ", 0, 2, ' ', true);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsFalse(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(", ", 10, 2, ' ', true);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsFalse(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(", ", 8, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(", ", 6, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(", ", 4, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(", ", 3, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(", ", 2, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(", ", 1, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            bytes = new byte[1];
            result = bytes.ToHexValuesString(", ", 2, 2, ' ', true);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsFalse(result.Contains(Environment.NewLine));

            result = bytes.ToHexValuesString(", ", 1, 2, ' ', true);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsFalse(result.Contains(Environment.NewLine));

            Throws<ArgumentException>(() => bytes.ToHexValuesString("a"));
        }

        [TestMethod]
        public void ToDecimalString()
        {
            byte[] bytes = Enumerable.Range(1, 3).Select(i => (byte)i).ToArray();

            Throws<ArgumentNullException>(() => bytes.ToDecimalValuesString(null));

            string result = bytes.ToDecimalValuesString();
            Assert.IsFalse(Char.IsWhiteSpace(result[0]));
            Assert.IsFalse(result.Contains(Environment.NewLine));

            result = bytes.ToDecimalValuesString(", ", 0, 2, ' ', true);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsFalse(result.Contains(Environment.NewLine));

            result = bytes.ToDecimalValuesString(", ", 8, 2);
            Assert.IsFalse(Char.IsWhiteSpace(result[0]));
            Assert.IsFalse(result.Contains(Environment.NewLine));

            result = bytes.ToDecimalValuesString(", ", 6, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToDecimalValuesString(", ", 4, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToDecimalValuesString(", ", 3, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToDecimalValuesString(", ", 2, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            result = bytes.ToDecimalValuesString(", ", 1, 2);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsTrue(result.Contains(Environment.NewLine));

            bytes = new byte[1];
            result = bytes.ToDecimalValuesString(", ", 2, 2, ' ', true);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsFalse(result.Contains(Environment.NewLine));

            result = bytes.ToDecimalValuesString(", ", 1, 2, ' ', true);
            Assert.IsTrue(Char.IsWhiteSpace(result[0]));
            Assert.IsFalse(result.Contains(Environment.NewLine));

            Throws<ArgumentException>(() => bytes.ToDecimalValuesString("0"));
        }
    }
}
