#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FilesTest.cs
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

#if NETFRAMEWORK
using System;
#endif
using System.IO;
#if NETFRAMEWORK
using System.Reflection;
using System.Security;
using System.Security.Permissions;
#endif

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries
{
    [TestFixture]
    public class FilesTest : TestBase
    {
        #region Nested classes

#if NETFRAMEWORK
        private class Sandbox : MarshalByRefObject
        {
            internal void DoTest()
            {
                Assert.IsTrue(EnvironmentHelper.IsPartiallyTrustedDomain);
                var test = new FilesTest();
                foreach (string[] args in getRelativePathTestSource)
                    test.GetRelativePathTest(args[0], args[1], args[2]);
            }
        }
#endif

        #endregion

        #region Fields

        private static readonly string[][] getRelativePathTestSource =
        {
#if WINDOWS
            new[] { @"C:\DIR1", @"D:\DIR1", @"C:\DIR1" },
            new[] { @"C:\DIR1", @"C:\DIR1", @"." },
            new[] { @"C:\DIR1", @"C:\DIR2", @"..\DIR1" },
            new[] { @"C:\", @"C:\DIR1\SUBDIR1", @"..\.." },
            new[] { @"C:\DIR1\SUBDIR1", @"C:\", @"DIR1\SUBDIR1" },
#endif
            new[] { @"DIR1", @"DIR1", @"." },
            new[] { @"DIR1", @"DIR2", @"..\DIR1" },
            new[] { @"DIR1\SUBDIR1", @"DIR2", @"..\DIR1\SUBDIR1" },
            new[] { @"DIR1", @"DIR2\SUBDIR2", @"..\..\DIR1" },
            new[] { @"DIR1\SUBDIR1", @"DIR1\SUBDIR2", @"..\SUBDIR1" },
            new[] { @"DIR1\SUBDIR1", @"DIR1", @"SUBDIR1" },
            new[] { @"DIR1", @"DIR1\SUBDIR1", @".." },
        };

        #endregion

        #region Methods

        [TestCaseSource(nameof(getRelativePathTestSource))]
        public void GetRelativePathTest(string target, string baseDir, string expected)
        {
            if (Path.DirectorySeparatorChar != '\\')
            {
                target = target.Replace('\\', Path.DirectorySeparatorChar);
                baseDir = baseDir.Replace('\\', Path.DirectorySeparatorChar);
                expected = expected.Replace('\\', Path.DirectorySeparatorChar);
            }

            Assert.AreEqual(expected, Files.GetRelativePath(target, baseDir));
        }

#if NETFRAMEWORK
        [Test]
        [SecuritySafeCritical]
        public void FilesTest_PartiallyTrusted()
        {
            // These permissions are just for a possible attached VisualStudio when the test fails. The code does not require any special permissions.
            var domain = CreateSandboxDomain(
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode),
                new FileIOPermission(PermissionState.Unrestricted));
            var handle = Activator.CreateInstance(domain, Assembly.GetExecutingAssembly().FullName, typeof(Sandbox).FullName!);
            var sandbox = (Sandbox)handle.Unwrap();
            try
            {
                sandbox.DoTest();
            }
            catch (SecurityException e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
#endif

        #endregion
    }
}