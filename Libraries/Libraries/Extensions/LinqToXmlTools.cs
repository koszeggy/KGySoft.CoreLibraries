using System.Text;
using System.Xml.Linq;

namespace KGySoft.Libraries
{
    using System;

    using KGySoft.Libraries.Resources;

    /// <summary>
    /// <see cref="XElement"/> extension methods.
    /// </summary>
    public static class LinqToXmlTools
    {
        /// <summary>
        /// Gets multiline string from an <see cref="XElement"/> with correct line endings.
        /// </summary>
        /// <remarks>
        /// In .NET 3.5 <see cref="XElement.Value"/> returns incorrect line endings if
        /// the XML file was read from file (contains only "\n" characters) but is correct
        /// when built in memory.
        /// </remarks>
        public static string GetMultilineValue(this XElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element), Res.Get(Res.ArgumentNull));

            return element.IsEmpty ? null : CorrectNewLine(element.Value);
        }

        /// <summary>
        /// Gets multiline string from an <see cref="XComment"/> with correct line endings.
        /// </summary>
        /// <remarks>
        /// In .NET 3.5 <see cref="XComment.Value"/> returns incorrect line endings if
        /// the XML file was read from file (contains only "\n" characters) but is correct
        /// when built in memory.
        /// </remarks>
        public static string GetMultilineValue(this XComment element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element), Res.Get(Res.ArgumentNull));

            return CorrectNewLine(element.Value);
        }

        /// <summary>
        /// Gets multiline string from an <see cref="XText"/> with correct line endings.
        /// </summary>
        /// <remarks>
        /// In .NET 3.5 <see cref="XText.Value"/> returns incorrect line endings if
        /// the XML file was read from file (contains only "\n" characters) but is correct
        /// when built in memory.
        /// </remarks>
        public static string GetMultilineValue(this XText element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element), Res.Get(Res.ArgumentNull));

            return CorrectNewLine(element.Value);
        }

        private static string CorrectNewLine(string s)
        {
            StringBuilder result = new StringBuilder(s);
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] == '\n' && (i == 0 || result[i - 1] != '\r'))
                    result.Insert(i, "\r");
            }
            return result.ToString();          
        }
    }
}
