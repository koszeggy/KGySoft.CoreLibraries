#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FilesTest.cs
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

using System.IO;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.CoreLibraries
{
    [TestFixture]
    public class FilesTest
    {
        #region Methods

#if WINDOWS
        [TestCase(@"C:\DIR1", @"D:\DIR1", @"C:\DIR1")]
        [TestCase(@"C:\DIR1", @"C:\DIR1", @".")]
        [TestCase(@"C:\DIR1", @"C:\DIR2", @"..\DIR1")]
        [TestCase(@"C:\", @"C:\DIR1\SUBDIR1", @"..\..")]
        [TestCase(@"C:\DIR1\SUBDIR1", @"C:\", @"DIR1\SUBDIR1")]
#endif
        [TestCase(@"DIR1", @"DIR1", @".")]
        [TestCase(@"DIR1", @"DIR2", @"..\DIR1")]
        [TestCase(@"DIR1\SUBDIR1", @"DIR2", @"..\DIR1\SUBDIR1")]
        [TestCase(@"DIR1", @"DIR2\SUBDIR2", @"..\..\DIR1")]
        [TestCase(@"DIR1\SUBDIR1", @"DIR1\SUBDIR2", @"..\SUBDIR1")]
        [TestCase(@"DIR1\SUBDIR1", @"DIR1", @"SUBDIR1")]
        [TestCase(@"DIR1", @"DIR1\SUBDIR1", @"..")]
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

        #endregion
    }
}