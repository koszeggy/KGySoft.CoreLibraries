#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypeConvertersTest.cs
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
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;

using KGySoft.ComponentModel;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.ComponentModel
{
    [TestFixture]
    public class TypeConvertersTest
    {
        #region Methods

        [TestCase(42, typeof(string))]
        [TestCase(42, typeof(byte[]))]
#if !(NETCOREAPP2_0 || NETCOREAPP2_1)
        [TestCase(42, typeof(InstanceDescriptor))]
#endif
        [TestCase(null, typeof(string))]
        [TestCase(null, typeof(byte[]))]
#if !(NETCOREAPP2_0 || NETCOREAPP2_1)
        [TestCase(null, typeof(InstanceDescriptor))]
#endif
        public void BinaryTypeConverterTest(object testData, Type targetType)
        {
            var converter = new BinaryTypeConverter();
            object result = converter.ConvertTo(testData, targetType);
            Assert.IsInstanceOf(targetType, result);

            object retrieved = converter.ConvertFrom(result);
            Assert.AreEqual(testData, retrieved);
        }

        [TestCase(1250, typeof(string))]
        [TestCase(1250, typeof(int))]
#if !(NETCOREAPP2_0 || NETCOREAPP2_1)
        [TestCase(1250, typeof(InstanceDescriptor))]
#endif
        [TestCase(null, typeof(string))]
        [TestCase(null, typeof(int))]
#if !(NETCOREAPP2_0 || NETCOREAPP2_1)
        [TestCase(null, typeof(InstanceDescriptor))]
#endif
        public void EncodingConverterTest(int? codePage, Type targetType)
        {
            Encoding testData = codePage.HasValue ? Encoding.GetEncoding(codePage.Value) : null;
            var converter = new EncodingConverter();
            object result = converter.ConvertTo(testData, targetType);
            Assert.IsInstanceOf(targetType, result);

            object retrieved = converter.ConvertFrom(result);
            Assert.AreEqual(testData, retrieved);
        }

        [TestCase(ConsoleColor.Blue, typeof(string))]
        [TestCase(ConsoleColor.Blue, typeof(Enum[]))]
#if !(NETCOREAPP2_0 || NETCOREAPP2_1)
        [TestCase(ConsoleColor.Blue, typeof(InstanceDescriptor))]
#endif
        [TestCase(ConsoleModifiers.Control | ConsoleModifiers.Shift, typeof(string))]
        [TestCase(ConsoleModifiers.Control | ConsoleModifiers.Shift, typeof(Enum[]))]
#if !(NETCOREAPP2_0 || NETCOREAPP2_1)
        [TestCase(ConsoleModifiers.Control | ConsoleModifiers.Shift, typeof(InstanceDescriptor))]
#endif
        public void FlagsEnumConverterTest<TEnum>(TEnum testData, Type targetType)
            where TEnum : struct, Enum
        {
            var converter = new FlagsEnumConverter(typeof(TEnum));
            object result = converter.ConvertTo(testData, targetType);
            Assert.IsInstanceOf(targetType, result);

            object retrieved = converter.ConvertFrom(result);
            Assert.AreEqual(testData, retrieved);

            PropertyDescriptorCollection properties = converter.GetProperties(null, testData);
            TEnum[] flags = Enum<TEnum>.GetFlags().ToArray();
            Assert.AreEqual(flags.Length, properties!.Count);

            foreach (TEnum flag in flags)
                Assert.AreEqual(Enum<TEnum>.HasFlag(testData, flag), properties[Enum<TEnum>.ToString(flag)].GetValue(testData));
        }

        [TestCase("1.2.0", typeof(string))]
        [TestCase("1.2.0", typeof(Version))]
#if !(NETCOREAPP2_0 || NETCOREAPP2_1)
        [TestCase("1.2.0", typeof(InstanceDescriptor))]
#endif
        [TestCase(null, typeof(string))]
        [TestCase(null, typeof(Version))]
#if !(NETCOREAPP2_0 || NETCOREAPP2_1)
        [TestCase(null, typeof(InstanceDescriptor))]
#endif
        public void VersionConverterTest(string versionString, Type targetType)
        {
#if NETCOREAPP3_0_OR_GREATER
            // because in .NET Core 3.0 a System.ComponentModel.VersionConverter has also been appeared
            var converter = new KGySoft.ComponentModel.VersionConverter();
#else
            var converter = new VersionConverter();
#endif
            Version testData = versionString == null ? null : new Version(versionString);
            object result = converter.ConvertTo(testData, targetType);
            if (result == null)
                Assert.AreEqual(typeof(Version), targetType);
            else
                Assert.IsInstanceOf(targetType, result);

            object retrieved = converter.ConvertFrom(result);
            Assert.AreEqual(testData, retrieved);
        }

        [TestCase("value", typeof(string))]
        [TestCase("", typeof(string))]
        [TestCase(null, typeof(string))]
#if !(NETCOREAPP2_0 || NETCOREAPP2_1)
        [TestCase("value", typeof(InstanceDescriptor))]
        [TestCase("", typeof(InstanceDescriptor))]
        [TestCase(null, typeof(InstanceDescriptor))]
#endif
        public void StringSegmentConverterTest(string s, Type targetType)
        {
            StringSegment testData = s.AsSegment();
            var converter = new StringSegmentConverter();
            object result = converter.ConvertTo(testData, targetType);
            if (result == null)
                Assert.AreEqual(typeof(string), targetType);
            else
                Assert.IsInstanceOf(targetType, result);

            object retrieved = converter.ConvertFrom(result);
            Assert.AreEqual(testData, retrieved);
        }

        #endregion
    }
}