#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Profiler.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml.Linq;

using KGySoft.Collections;
using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Provides members for performance monitoring.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="Profiler"/> class can be used to place measurement sections into the code. The results can be accessed either directly
    /// by the <see cref="GetMeasurementResults(string)">GetMeasurementResults</see> method or when the <see cref="AutoSaveResults"/> property is <see langword="true"/>,
    /// then the results will be dumped into an .XML file into a folder designated by the <see cref="ProfilerDirectory"/> property.</para>
    /// <para>The profiling can be turned on and off globally by the <see cref="Enabled"/> property.</para>
    /// <note>It is recommended to measure performance with <c>Release</c> builds.</note>
    /// </remarks>
    /// <example>
    /// The following example demonstrates how to place measurement sections into the code:
    /// <note type="tip">Try also <a href="https://dotnetfiddle.net/BuuisW" target="_blank">online</a>.</note>
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Threading;
    /// using KGySoft.Diagnostics;
    ///
    /// class Example
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         Profiler.Enabled = true; // set false to turn off profiling
    ///         Profiler.AutoSaveResults = true; // if true a result .XML file is saved on exit
    ///
    ///         // put the measurement into a using block. You can specify a category and operation name.
    ///         using (Profiler.Measure(nameof(Example), $"{nameof(Main)} total"))
    ///         {
    ///             for (int i = 0; i < 10; i++)
    ///             {
    ///                 using (Profiler.Measure(nameof(Example), $"{nameof(Main)}/1 iteration"))
    ///                 {
    ///                     DoSmallTask();
    ///                     DoBigTask();
    ///                 }
    ///             }
    ///         }
    ///
    ///         // if Profiler.AutoSaveResults is false you might want to check the results here: Profiler.GetMeasurementResults(nameof(Example));
    ///     }
    ///
    ///     private static void DoSmallTask()
    ///     {
    ///         using (Profiler.Measure(nameof(Example), nameof(DoSmallTask)))
    ///             Console.WriteLine(nameof(DoSmallTask));
    ///     }
    ///
    ///     private static void DoBigTask()
    ///     {
    ///         using (Profiler.Measure(nameof(Example), nameof(DoBigTask)))
    ///         {
    ///             for (int i = 0; i < 5; i++)
    ///             {
    ///                 Thread.Sleep(10);
    ///                 DoSmallTask();
    ///             }
    ///         }
    ///     }
    /// }]]></code>
    /// As a result, in the folder of the application a new <c>Profiler</c> folder has been created with a file named something like <c>[time stamp]_ConsoleApp1.exe.xml</c>
    /// with a similar content to the following:
    /// <code lang="XML"><![CDATA[
    /// <?xml version="1.0" encoding="utf-8"?>
    /// <ProfilerResult>
    ///   <item Category = "Example" Operation="Main total" NumberOfCalls="1" FirstCall="00:00:00.5500736" TotalTime="00:00:00.5500736" AverageCallTime="00:00:00.5500736" />
    ///   <item Category = "Example" Operation="Main/1 iteration" NumberOfCalls="10" FirstCall="00:00:00.0555439" TotalTime="00:00:00.5500554" AverageCallTime="00:00:00.0550055" />
    ///   <item Category = "Example" Operation="DoSmallTask" NumberOfCalls="60" FirstCall="00:00:00.0005378" TotalTime="00:00:00.0124114" AverageCallTime="00:00:00.0002068" />
    ///   <item Category = "Example" Operation="DoBigTask" NumberOfCalls="10" FirstCall="00:00:00.0546513" TotalTime="00:00:00.5455339" AverageCallTime="00:00:00.0545533" />
    /// </ProfilerResult>]]></code>
    /// You can open the result even in Microsoft Excel as a table, which allows you to filter and sort the results easily:
    /// <br/><img src="../Help/Images/ProfilerResults.png" alt="The Profiler results opened in Microsoft Excel."/>
    /// </example>
    public static class Profiler
    {
        #region Constants

        private const string defaultDir = "Profiler";

        #endregion

        #region Fields

        private static readonly StringKeyedDictionary<MeasureItem> items;
        private static string profilerDir;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether profiling is enabled.
        /// <br/>Default value: <see langword="true"/>
        /// </summary>
        public static bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets whether results are automatically saved into the directory determined by the <see cref="ProfilerDirectory"/> property.
        /// <br/>Default value: <see langword="true"/>
        /// </summary>
        /// <remarks>
        /// If the value of this property is <see langword="true"/>, the profiler results are dumped automatically when the current
        /// <see cref="AppDomain"/> is unloaded. This happens typically when the application is closed.
        /// </remarks>
        /// <seealso cref="ProfilerDirectory"/>
        public static bool AutoSaveResults { get; set; }

        /// <summary>
        /// Gets or sets the output folder of the profiler. When <see langword="null"/>&#160;or empty value is assigned,
        /// sets the <c>Profiler</c> subdirectory of the executing assembly.
        /// </summary>
        /// <remarks>
        /// <para>Results are dumped only if <see cref="AutoSaveResults"/> property is <see langword="true"/>.</para>
        /// <para>By default, the value of this property is the <c>Profiler</c> subdirectory of the executing assembly.
        /// When <see langword="null"/>&#160;or empty value is assigned, this default value is reset.</para>
        /// <para>When the directory does not exist, it will be created automatically. The profiler results are
        /// dumped when the current <see cref="AppDomain"/> is unloaded. This happens typically when the application is closed.</para>
        /// </remarks>
        /// <seealso cref="AutoSaveResults"/>
        [AllowNull]
        public static string ProfilerDirectory
        {
            get => profilerDir;
            set => profilerDir = String.IsNullOrEmpty(value) ? GetDefaultDir() : value!;
        }

        #endregion

        #region Constructors

        static Profiler()
        {
            Enabled = true;
            AutoSaveResults = true;

            // According to MSDN, DomainUnload is never raised in main app domain so using ProcessExit there
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            else
                AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;

            items = new StringKeyedDictionary<MeasureItem>();
            profilerDir = GetDefaultDir();
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Gets the measurement results so far.
        /// </summary>
        /// <returns>The measurement results collected so far.</returns>
        /// <remarks>
        /// <para>If <see cref="AutoSaveResults"/> is <see langword="true"/>, the measurement results are automatically dumped in
        /// XML files on application exit, so accessing this property is required only
        /// when measurements are needed to be accessed programmatically.
        /// </para>
        /// <para>Getting this property is an O(1) operation. The returned value is a lazy enumerator. If <see cref="Measure"/>
        /// method is called during the enumeration an exception might be thrown.</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public static IEnumerable<IMeasureItem> GetMeasurementResults()
            // ReSharper disable once InconsistentlySynchronizedField - see remarks above
            => items.Select(i => (IMeasureItem)i.Value);

        /// <summary>
        /// Gets the measurement results of the given <paramref name="category"/> so far.
        /// </summary>
        /// <param name="category">The category of the measurement results to obtain.</param>
        /// <returns>The measurement results of the given <paramref name="category"/> collected so far.</returns>
        /// <remarks>
        /// <para>If <see cref="AutoSaveResults"/> is <see langword="true"/>, the measurement results are automatically dumped in
        /// XML files on application exit, so accessing this property is required only
        /// when measurements are needed to be accessed programmatically.
        /// </para>
        /// <para>Getting this property is an O(1) operation. The returned value is a lazy enumerator. If <see cref="Measure"/>
        /// method is called during the enumeration an exception might be thrown.</para>
        /// <note>The enumerator of the returned collection does not support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public static IEnumerable<IMeasureItem> GetMeasurementResults(string category)
            // ReSharper disable once InconsistentlySynchronizedField - see remarks above
            => items.Where(i => i.Value.Category == category).Select(i => (IMeasureItem)i.Value);

        /// <summary>
        /// Gets a measurement result as an <see cref="IMeasureItem"/> instance, or <see langword="null"/>, if the
        /// measurement result is not found with the given <paramref name="category"/> and <paramref name="operation"/>.
        /// </summary>
        /// <param name="category">The category name of the operation. If <see langword="null"/>&#160;or empty, looks for an uncategorized operation.</param>
        /// <param name="operation">Name of the operation.</param>
        /// <returns>An <see cref="IMeasureItem"/> instance that contains the measurement results of the required
        /// operation, or <see langword="null"/>, if the measurement result is not found with the given <paramref name="category"/>
        /// and <paramref name="operation"/>.</returns>
        /// <remarks>Unless <see cref="Reset"/> is called, there is no need to retrieve the measurement result of the same
        /// <paramref name="category"/> and <see paramref="operation"/> again and again because the returned <see cref="IMeasureItem"/>
        /// instance reflects the changes of the measurement operation.</remarks>
        public static IMeasureItem? GetMeasurementResult(string? category, string operation)
        {
            if (operation == null!)
                Throw.ArgumentNullException(Argument.operation);

            if (String.IsNullOrEmpty(category))
                category = Res.ProfilerUncategorized;

            string key = category + ":" + operation;
            lock (items)
            {
                if (items.TryGetValue(key, out MeasureItem? item))
                    return item;
            }

            return null;
        }

        /// <summary>
        /// If <see cref="Enabled"/> is <see langword="true"/>, starts a profiling measure. Use in <see langword="using"/>&#160;block.
        /// </summary>
        /// <param name="category">A category that contains the operation. Can be the name of the caller type, for example.
        /// If <see langword="null"/>&#160;or empty, the measurement will be uncategorized.</param>
        /// <param name="operation">Name of the operation.</param>
        /// <returns>An <see cref="IDisposable"/> instance that should be enclosed into a <see langword="using"/>&#160;block.
        /// When <see cref="Enabled"/> is <see langword="false"/>, this method returns <see langword="null"/>.</returns>
        /// <remarks>
        /// <note type="tip">See the <strong>Remarks</strong> section of the <see cref="Profiler"/> class for details and an example.</note>
        /// </remarks>
        public static IDisposable? Measure(string? category, string operation)
        {
            if (!Enabled)
                return null;

            if (operation == null!)
                Throw.ArgumentNullException(Argument.operation);

            if (String.IsNullOrEmpty(category))
                category = Res.ProfilerUncategorized;

            string key = category + ":" + operation;
            MeasureItem? item;
            lock (items)
            {
                if (!items.TryGetValue(key, out item))
                {
                    item = new MeasureItem(category!, operation);
                    items.Add(key, item);
                }
            }

            return new MeasureOperation(item);
        }

        /// <summary>
        /// Resets the profiler results. Every measurement performed earlier will be lost.
        /// </summary>
        public static void Reset()
        {
            lock (items)
            {
                items.Clear();
            }
        }

        #endregion

        #region Private Methods

        private static string GetDefaultDir()
        {
            try
            {
                return Path.Combine(Files.GetExecutingPath(), defaultDir);
            }
            catch (SecurityException)
            {
                return defaultDir;
            }
            catch (UnauthorizedAccessException)
            {
                return defaultDir;
            }
        }

        private static void DumpResults()
        {
            var result = new XElement("ProfilerResult");
            lock (items)
            {
                if (!Enabled || !AutoSaveResults || items.Count == 0)
                    return;
                foreach (MeasureItem item in items.Values)
                {
                    XElement xItem = new XElement("item", new XAttribute(nameof(item.Category), item.Category),
                        new XAttribute(nameof(item.Operation), item.Operation),
                        new XAttribute(nameof(item.NumberOfCalls), item.NumberOfCalls),
                        new XAttribute(nameof(item.FirstCall), item.FirstCall.ToString()),
                        new XAttribute(nameof(item.TotalTime), item.TotalTime.ToString()),
                        new XAttribute("AverageCallTime", TimeSpan.FromTicks(item.TotalTime.Ticks / item.NumberOfCalls).ToString()));

                    result.Add(xItem);
                }
            }

            try
            {
                StringBuilder fileName = new StringBuilder(AppDomain.CurrentDomain.FriendlyName);
                foreach (char c in Path.GetInvalidFileNameChars())
                    fileName.Replace(c, '_');

                if (!Directory.Exists(profilerDir))
                    Directory.CreateDirectory(profilerDir);

                result.Save(Path.Combine(profilerDir, Files.GetNextFileName($"{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss.fffffff", CultureInfo.InvariantCulture)}_{fileName}.xml")!));
            }
            catch (IOException)
            {
            }
            catch (SecurityException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        #endregion

        #region Event handlers

        static void CurrentDomain_DomainUnload(object? sender, EventArgs e) => DumpResults();
        static void CurrentDomain_ProcessExit(object? sender, EventArgs e) => DumpResults();

        #endregion

        #endregion
    }
}
