using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// File utilities.
    /// </summary>
    public static class Files
    {
        /// <summary>
        /// Checks whether a file can be created with given name.
        /// </summary>
        /// <param name="fileName">The name of the file to test.</param>
        /// <param name="canOverwrite">When <see langword="false"/>, file will not be overwritten if already exists and result will be <see langword="false"/>.
        /// When <see langword="true"/>, already existing file will be overwritten and deleted.</param>
        static public bool CanCreate(string fileName, bool canOverwrite)
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
        /// Checks whether a file can be created with given name.
        /// If a file with the same neme already exists it will be overwritten and deleted.
        /// </summary>
        /// <param name="filename">The name of the file to test.</param>
        static public bool CanCreate(string filename)
        {
            return CanCreate(filename, true);
        }

        /// <summary>
        /// Returns <paramref name="path"/> if there is no already existing file with specified name.
        /// Otherwise returns the first non-existing file name with a number postfix.
        /// </summary>
        /// <param name="path">Full path of the file to test.</param>
        /// <returns>Returns <paramref name="path"/> if that is a non-existing file name.
        /// Otherwise returns a non-existing file name with a number postfix in the file name part (the extension willnot be changed).</returns>
        static public string GetNextFileName(string path)
        {
            return GetNextFileName(path, null);
        }

        /// <summary>
        /// Returns <paramref name="path"/> if there is no already existing file with specified name.
        /// Otherwise returns the first non-existing file name with a number postfix.
        /// </summary>
        /// <param name="path">Full path of the file to test.</param>
        /// <param name="postfixSeparator">A postfix between the file name and the numbering.</param>
        /// <returns>Returns <paramref name="path"/> if that is a non-existing file name. Returns <see langword="null"/> if <paramref name="path"/> denotes a root directory.
        /// Otherwise, returns a non-existing file name with a number postfix in the file name part (the extension will not be changed).</returns>
        static public string GetNextFileName(string path, string postfixSeparator)
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

        ///// <summary>
        ///// Runs an executable file.
        ///// </summary>
        //public static bool RunExe(string filepath, string parameters)
        //{
        //    try
        //    {
        //        if (File.Exists(filepath))
        //        {
        //            ProcessStartInfo psi = new ProcessStartInfo();
        //            psi.FileName = filepath;
        //            psi.Arguments = parameters;
        //            psi.ErrorDialog = true;
        //            psi.UseShellExecute = false;
        //            Process.Start(psi);
        //        }
        //        else
        //        {
        //            return false;
        //        }
        //    }
        //    catch
        //    {
        //        return false;
        //    }

        //    return true;
        //}

        /// <summary>
        /// Gets relative path to a <paramref name="target"/> according to a <paramref name="baseDirectory"/>.
        /// </summary>
        /// <param name="target">The target file or directory name. Can be either relative or absolute path to current directory.</param>
        /// <param name="baseDirectory">The relative base directory.</param>
        /// <returns>The relative path of <paramref name="target"/> to <paramref name="baseDirectory"/> or abolute path of <paramref name="target"/> if there is no relative path between them.</returns>
        public static string GetRelativePath(string target, string baseDirectory)
        {
            // TODO: test this:
            //return new Uri(baseDirectory).MakeRelativeUri(new Uri(target)).LocalPath;
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
                {
                    result.Append(Path.DirectorySeparatorChar);
                }
                result.Append("..");
            }

            if (result.Length == 0)
            {
                result.Append(".");
            }

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
        /// <param name="pattern">The pattern that may contain wildcards (*, ?).</param>
        /// <param name="fileName">The file name to test.</param>
        /// <returns><see langword="true"/>, when <paramref name="fileName"/> matches <paramref name="pattern"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="pattern"/> or <paramref name="fileName"/> is <see langword="null"/>.</exception>
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
        public static string GetExecutingPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }
    }
}
