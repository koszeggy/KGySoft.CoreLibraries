#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: StreamExtensionsTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2025 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.IO;
using System.Text;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries.Extensions
{
    [TestFixture]
    public class StreamExtensionsTest
    {
        #region Methods

        [Test]
        public void EncryptDecrypt()
        {
            string password = "testPassword";
            string message = "Hello, World!";
            using var rawMessage = new MemoryStream(Encoding.UTF8.GetBytes(message));
            using var encrypted = new MemoryStream();
            using var decrypted = new MemoryStream();

            rawMessage.Encrypt(encrypted, password, out byte[] salt);
            Console.WriteLine(encrypted.ToArray().ToHexValuesString(","));
            encrypted.Position = 0;
            encrypted.Decrypt(decrypted, password, salt);
            Assert.AreEqual(message, Encoding.UTF8.GetString(decrypted.ToArray()));
        }

        #endregion
    }
}