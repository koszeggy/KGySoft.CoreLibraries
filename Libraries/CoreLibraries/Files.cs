#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Files.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains file-related methods.
    /// </summary>
    public static class Files
    {
        #region Methods

        /// <summary>
        /// Checks whether a file can be created with given name.
        /// </summary>
        /// <param name="fileName">The name of the file to test.</param>
        /// <param name="canOverwrite">When <see langword="false"/>, then file will not be overwritten if already exists and the result will be <see langword="false"/>.
        /// When <see langword="true"/>, then the already existing file will be overwritten and deleted. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null"/>.</exception>
        /// <returns><see langword="true"/>, if <paramref name="fileName"/> can be created; otherwise, <see langword="false"/>.</returns>
        public static bool CanCreate(string fileName, bool canOverwrite = true)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName), Res.ArgumentNull);

            try
            {
                if (File.Exists(fileName) && !canOverwrite)
                    return false;
                FileStream fs = File.Create(fileName);
                fs.Close();
                File.Delete(fileName);
                return true;
            }
            catch
            {
                return false;
            }
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
        public static string GetNextFileName(string path, string postfixSeparator = null)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path), Res.ArgumentNull);

            postfixSeparator = postfixSeparator ?? String.Empty;

            if (!File.Exists(path))
                return path;

            string dirName = Path.GetDirectoryName(path);
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
        /// <returns>The relative path of <paramref name="target" /> from <paramref name="baseDirectory" />, or the absolute path of <paramref name="target" /> if there is no relative path between them.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> or <paramref name="baseDirectory"/> is <see langword="null"/>.</exception>
        /// <returns>The relative path to <paramref name="target" /> from the <paramref name="baseDirectory" />.</returns>
        public static string GetRelativePath(string target, string baseDirectory)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target), Res.ArgumentNull);
            if (baseDirectory == null)
                throw new ArgumentNullException(nameof(baseDirectory), Res.ArgumentNull);

            if (!Path.IsPathRooted(target))
                target = Path.GetFullPath(target);
            if (!Path.IsPathRooted(baseDirectory))
                baseDirectory = Path.GetFullPath(baseDirectory);

            string[] basePathParts = baseDirectory.Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            string[] targetPathParts = target.Trim(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);

            int commonPathDepth = 0;
            for (int i = 0; i < Math.Min(basePathParts.Length, targetPathParts.Length); i++)
            {
                if (!basePathParts[i].ToLowerInvariant().Equals(targetPathParts[i].ToLowerInvariant()))
                    break;
                commonPathDepth++;
            }

            // no common parts
            if (commonPathDepth == 0)
                return target;

            StringBuilder result = new StringBuilder();
            for (int i = commonPathDepth; i < basePathParts.Length; i++)
            {
                if (i > commonPathDepth)
                    result.Append(Path.DirectorySeparatorChar);
                result.Append("..");
            }

            if (result.Length == 0)
                result.Append(".");

            for (int i = commonPathDepth; i < targetPathParts.Length; i++)
            {
                result.Append(Path.DirectorySeparatorChar);
                result.Append(targetPathParts[i]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns whether a wildcarded pattern matches a file name.
        /// </summary>
        /// <param name="pattern">The pattern that may contain wildcards (<c>*</c>, <c>?</c>).</param>
        /// <param name="fileName">The file name to test.</param>
        /// <returns><see langword="true"/>, when <paramref name="fileName"/> matches <paramref name="pattern"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> or <paramref name="fileName"/> is <see langword="null"/>.</exception>
        public static bool IsWildcardMatch(string pattern, string fileName)
        {
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern), Res.ArgumentNull);
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName), Res.ArgumentNull);

            return new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase).IsMatch(fileName);
        }

        /// <summary>
        /// Gets the real full path of the directory, where executing application resides.
        /// </summary>
        public static string GetExecutingPath() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        #endregion
    }
}
