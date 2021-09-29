#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Files.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2019 - All Rights Reserved
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
using System.Linq;
#if NETFRAMEWORK
using System.Reflection;
#endif
using System.Security;
using System.Text.RegularExpressions;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains file-related methods.
    /// </summary>
    public static class Files
    {
        #region Fields

        private static char[]? illegalPathChars;

        #endregion

        #region Properties

        internal static char[] IllegalPathChars => illegalPathChars ??= Path.GetInvalidPathChars().Concat(new[] { '*', '?' }).ToArray();

        #endregion

        #region Methods

        /// <summary>
        /// Creates or overwrites a file of the specified <paramref name="path"/> along with possibly non-existing parent directories.
        /// </summary>
        /// <param name="path">The name of the file to be created with path.</param>
        /// <returns>The created <see cref="FileStream"/>.</returns>
        public static FileStream CreateWithPath(string path)
        {
            if (path == null!)
                Throw.ArgumentNullException(Argument.path);
            if (path.Length == 0)
                Throw.ArgumentException(Argument.path, Res.ArgumentEmpty);

            string? dir = Path.GetDirectoryName(path);
            if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir!);
            return File.Create(path);
        }

        /// <summary>
        /// Tries to create a file of the specified <paramref name="path"/> along with possibly non-existing parent directories.
        /// </summary>
        /// <param name="path">The name of the file to be created with path.</param>
        /// <param name="overwriteIfExists"><see langword="true"/>&#160;to allow an already existing file to be overwritten; otherwise, <see langword="false"/>.</param>
        /// <returns>A <see cref="FileStream"/> instance if the file could be created or overwritten; otherwise, <see langword="null"/>.</returns>
        public static FileStream? TryCreateWithPath(string path, bool overwriteIfExists = true)
        {
            if (path == null!)
                Throw.ArgumentNullException(Argument.path);
            try
            {
                if (!overwriteIfExists && File.Exists(path))
                    return null;
                return CreateWithPath(path);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is ArgumentException))
            {
                return null;
            }
        }

        /// <summary>
        /// Checks whether a file can be created with given name.
        /// </summary>
        /// <param name="fileName">The name of the file to test.</param>
        /// <param name="canOverwrite">When <see langword="false"/>, then file will not be overwritten if already exists and the result will be <see langword="false"/>.
        /// When <see langword="true"/>, then the already existing file will be overwritten and deleted. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null"/>.</exception>
        /// <returns><see langword="true"/>, if <paramref name="fileName"/> can be created; otherwise, <see langword="false"/>.</returns>
        [Obsolete("This method is obsolete. Use " + nameof(TryCreateWithPath) + " method instead")]
        public static bool CanCreate(string fileName, bool canOverwrite = true)
        {
            bool result;
            using (FileStream? fs = TryCreateWithPath(fileName, canOverwrite))
                result = fs != null;

            if (result)
            {
                try
                {
                    File.Delete(fileName);
                }
                catch (Exception e) when (!e.IsCritical())
                {
                    return false;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns <paramref name="path"/> if a file with specified name does not exist yet.
        /// Otherwise, returns the first non-existing file name with a number postfix.
        /// </summary>
        /// <param name="path">Full path of the file to check.</param>
        /// <param name="postfixSeparator">A postfix between the file name and the numbering. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>Returns <paramref name="path"/>, if that is a non-existing file name. Returns <see langword="null"/>, if <paramref name="path"/> denotes a root directory.
        /// Otherwise, returns a non-existing file name with a number postfix in the file name part (the extension will not be changed).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        public static string? GetNextFileName(string path, string? postfixSeparator = null)
        {
            if (path == null!)
                Throw.ArgumentNullException(Argument.path);

            postfixSeparator ??= String.Empty;

            if (!File.Exists(path))
                return path;

            string? dirName = Path.GetDirectoryName(path);
            if (dirName == null)
                return null;

            string fileName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);

            for (int i = 1; i < Int32.MaxValue; i++)
            {
                string file = Path.Combine(dirName, fileName) + postfixSeparator + i + ext;

                if (!File.Exists(file))
                    return file;
            }

            return path;
        }

        /// <summary>
        /// Gets the relative path to <paramref name="target" /> from the <paramref name="baseDirectory" />.
        /// </summary>
        /// <param name="target">The target file or directory name. Can be either an absolute path or a relative one to current directory.</param>
        /// <param name="baseDirectory">The base directory to which the relative <paramref name="target" /> path should be determined.</param>
        /// <param name="isCaseSensitive"><see langword="true"/>&#160;to perform a case-sensitive comparison;
        /// <see langword="false"/>&#160;to perform a case-insensitive comparison.</param>
        /// <returns>The relative path of <paramref name="target" /> from <paramref name="baseDirectory" />, or the absolute path of <paramref name="target" /> if there is no relative path between them.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> or <paramref name="baseDirectory"/> is <see langword="null"/>.</exception>
        /// <returns>The relative path to <paramref name="target" /> from the <paramref name="baseDirectory" />.</returns>
        [SecuritySafeCritical]
        public static unsafe string GetRelativePath(string target, string baseDirectory, bool isCaseSensitive)
        {
            if (target == null!)
                Throw.ArgumentNullException(Argument.target);
            if (target.Length == 0)
                Throw.ArgumentException(Argument.target, Res.ArgumentEmpty);
            if (baseDirectory == null!)
                Throw.ArgumentNullException(Argument.baseDirectory);
            if (baseDirectory.Length == 0)
                Throw.ArgumentException(Argument.baseDirectory, Res.ArgumentEmpty);

            target = Path.GetFullPath(target);
            baseDirectory = Path.GetFullPath(baseDirectory);

            var srcDir = new StringSegmentInternal(baseDirectory);
            srcDir.TrimEnd(Path.DirectorySeparatorChar);
            List<StringSegmentInternal> basePathParts = srcDir.Split(Path.DirectorySeparatorChar);

            var dstDir = new StringSegmentInternal(target);
            dstDir.TrimEnd(Path.DirectorySeparatorChar);
            List<StringSegmentInternal> targetPathParts = dstDir.Split(Path.DirectorySeparatorChar);

            int commonPathDepth = 0;
            int len = Math.Min(basePathParts.Count, targetPathParts.Count);
            if (isCaseSensitive)
            {
                for (int i = 0; i < len; i++)
                {
                    if (!basePathParts[i].Equals(targetPathParts[i]))
                        break;
                    commonPathDepth += 1;
                }
            }
            else
            {
                for (int i = 0; i < len; i++)
                {
                    if (!basePathParts[i].EqualsOrdinalIgnoreCase(targetPathParts[i]))
                        break;
                    commonPathDepth += 1;
                }
            }

            // no common parts
            if (commonPathDepth == 0)
                return target;

            int baseOnlyCount = basePathParts.Count - commonPathDepth;
            int targetPathCount = targetPathParts.Count;
            len = baseOnlyCount > 0 ? baseOnlyCount * 2 + (baseOnlyCount - 1) : 0; // .. and path separators
            for (int i = commonPathDepth; i < targetPathCount; i++)
            {
                if (len > 0)
                    len += 1;
                len += targetPathParts[i].Length;
            }

            string result = new String('\0', len);
            fixed (char* pResult = result)
            {
                var sb = new MutableStringBuilder(pResult, len);
                for (int i = 0; i < baseOnlyCount; i++)
                {
                    if (i > 0)
                        sb.Append(Path.DirectorySeparatorChar);
                    sb.Append("..");
                }

                for (int i = commonPathDepth; i < targetPathCount; i++)
                {
                    if (sb.Length > 0)
                        sb.Append(Path.DirectorySeparatorChar);
                    sb.Append(targetPathParts[i]);
                }
            }

            if (result.Length == 0)
                return ".";
            return result;
        }

        /// <summary>
        /// Gets the relative path to <paramref name="target" /> from the <paramref name="baseDirectory" />.
        /// This overload performs a case insensitive comparison.
        /// </summary>
        /// <param name="target">The target file or directory name. Can be either an absolute path or a relative one to current directory.</param>
        /// <param name="baseDirectory">The base directory to which the relative <paramref name="target" /> path should be determined.</param>
        /// <returns>The relative path of <paramref name="target" /> from <paramref name="baseDirectory" />, or the absolute path of <paramref name="target" /> if there is no relative path between them.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> or <paramref name="baseDirectory"/> is <see langword="null"/>.</exception>
        /// <returns>The relative path to <paramref name="target" /> from the <paramref name="baseDirectory" />.</returns>
        public static string GetRelativePath(string target, string baseDirectory) => GetRelativePath(target, baseDirectory, false);

        /// <summary>
        /// Returns whether a wildcarded pattern matches a file name.
        /// </summary>
        /// <param name="pattern">The pattern that may contain wildcards (<c>*</c>, <c>?</c>).</param>
        /// <param name="fileName">The file name to test.</param>
        /// <returns><see langword="true"/>, when <paramref name="fileName"/> matches <paramref name="pattern"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> or <paramref name="fileName"/> is <see langword="null"/>.</exception>
        public static bool IsWildcardMatch(string pattern, string fileName)
        {
            if (pattern == null!)
                Throw.ArgumentNullException(Argument.pattern);
            if (fileName == null!)
                Throw.ArgumentNullException(Argument.fileName);

            return new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase).IsMatch(fileName);
        }

        /// <summary>
        /// Gets the real full path of the directory, where the executing application resides.
        /// </summary>
        /// <returns>The full path of the directory where the executing application resides.</returns>
        public static string GetExecutingPath()
        {
#if NETFRAMEWORK
            try
            {
                // We keep this code for compatibility reason. It may differ from AppDomain.BaseDirectory in special cases
                // (eg. when an the code is executed from a sandbox domain using a subdirectory).
                // Example: for debugger visualizers GetExecutingPath returns the location of the deployed visualizer (eg. Documents/VS version/Visualizers
                // instead of the path of the Visual Studio installation).
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            }
            catch (Exception e) when (e is SecurityException or UnauthorizedAccessException)
            {
                // Using GetDirectoryName because BaseDirectory is always postfixed by directory separator.
                // If base directory is a root path, then GetDirectoryName returns null
                return Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? AppDomain.CurrentDomain.BaseDirectory;
            }
#else
            // Using GetDirectoryName because BaseDirectory is always postfixed by directory separator.
            // If base directory is a root path, then GetDirectoryName returns null
            return Path.GetDirectoryName(AppContext.BaseDirectory) ?? AppContext.BaseDirectory;
#endif
        }

        #endregion
    }
}
