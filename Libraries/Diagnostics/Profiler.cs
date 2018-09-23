using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml.Linq;
using KGySoft.Libraries;
using KGySoft.Libraries.Resources;

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Represents a profiler for performance monitoring.
    /// </summary>
    public static class Profiler
    {
        #region Fields

        private readonly static Dictionary<string, MeasureItem> items;

        private static string profilerDir;

        #endregion

        #region Constructors

        static Profiler()
        {
            Enabled = true;
            AutoSaveResults = true;

            // According to MSDN, DomainUnload is never raised in main app domain so using ProcessExit there
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
            {
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            }
            else
            {
                AppDomain.CurrentDomain.DomainUnload += new EventHandler(CurrentDomain_DomainUnload);
            }

            items = new Dictionary<string, MeasureItem>();

            try
            {
                profilerDir = GetDefaultDir();
            }
            catch (SecurityException)
            {
                profilerDir = "Profiler";
            }
            catch (UnauthorizedAccessException)
            {
                profilerDir = "Profiler";
            }
        }

        #endregion

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
        /// Gets or sets the output folder of the profiler. When <see langword="null"/> or empty value is assigned,
        /// sets the <c>Profiler</c> subdirectory of the executing assembly.
        /// </summary>
        /// <remarks>
        /// <para>Results are dumped only if <see cref="AutoSaveResults"/> property is <see langword="true"/>.</para>
        /// <para>By default, the value of this property is the <c>Profiler</c> subdirectory of the executing assembly.
        /// When <see langword="null"/> or empty value is assigned, this default value is reset.</para>
        /// <para>When the directory does not exist, it will be created automatically. The profiler results are
        /// dumped when the current <see cref="AppDomain"/> is unloaded. This happens typically when the application is closed.</para>
        /// </remarks>
        /// <seealso cref="AutoSaveResults"/>
        public static string ProfilerDirectory
        {
            get { return profilerDir; }
            set
            {
                profilerDir = String.IsNullOrEmpty(value) ? GetDefaultDir() : value;
            }
        }

        /// <summary>
        /// Gets the measurement results so far.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="AutoSaveResults"/> is <see langword="true"/>, the measurement results are automatically dumped in
        /// XML files on application exit, so accessing this property is required only
        /// when measurements are needed to be accessed programatically.
        /// </para>
        /// <para>Getting this property is an O(1) operation. The returned value is a lazy enumerator. If <see cref="Measure"/>
        /// method is called during the enumeratation an exception might be thrown.</para>
        /// </remarks>
        public static IEnumerable<IMeasureItem> GetMeasurementResults()
        {
            return items.Select(i => (IMeasureItem)i.Value);
        }

        /// <summary>
        /// Gets the measurement results of the given <paramref name="category"/> so far.
        /// </summary>
        /// <remarks>
        /// <para>If <see cref="AutoSaveResults"/> is <see langword="true"/>, the measurement results are automatically dumped in
        /// XML files on application exit, so accessing this property is required only
        /// when measurements are needed to be accessed programatically.
        /// </para>
        /// <para>Getting this property is an O(1) operation. The returned value is a lazy enumerator. If <see cref="Measure"/>
        /// method is called during the enumeratation an exception might be thrown.</para>
        /// </remarks>
        public static IEnumerable<IMeasureItem> GetMeasurementResults(string category)
        {
            return items.Where(i => i.Value.Category == category).Select(i => (IMeasureItem)i.Value);
        }

        /// <summary>
        /// Gets a measurement result as an <see cref="IMeasureItem"/> instance, or <see langword="null"/>, if the
        /// measurement result is not found with the given <paramref name="category"/> and <paramref name="operation"/>.
        /// </summary>
        /// <param name="category">The category name of the operation.
        /// If <see langword="null"/> or empty, looks for an uncategorized operation.</param>
        /// <param name="operation">Name of the operation.</param>
        /// <returns>An <see cref="IMeasureItem"/> instance that that contains the measurement results of the required
        /// operation, or <see langword="null"/>, if the measurement result is not found with the given <paramref name="category"/>
        /// and <paramref name="operation"/>.</returns>
        /// <remarks>Unless <see cref="Reset"/> is called, there is no need to retrieve the measurement result of the same
        /// <paramref name="category"/> and <see paramref="operation"/> again and again because the returned <see cref="IMeasureItem"/>
        /// instance reflects the changes of the measurement operation.</remarks>
        public static IMeasureItem GetMeasurementResult(string category, string operation)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation), Res.Get(Res.ArgumentNull));

            if (String.IsNullOrEmpty(category))
                category = Res.Get(Res.Uncategorized);

            string key = category + ":" + operation;
            lock (items)
            {
                MeasureItem item;
                if (items.TryGetValue(key, out item))
                    return item;
            }

            return null;
        }

        #region Methods

        #region Public Methods

        /// <summary>
        /// If <see cref="Enabled"/> is <see langword="true"/>, starts a profiling measure. Use in <see langword="using"/> block.
        /// </summary>
        /// <param name="category">A category that contains the operation. Can be the name of the caller type, for example.
        /// If <see langword="null"/> or empty, the measurement will be uncategorized.</param>
        /// <param name="operation">Name of the operation.</param>
        /// <returns>An <see cref="IDisposable"/> instance that should be enclosed into a <see langword="using"/> block.
        /// When <see cref="Enabled"/> is <see langword="false"/>, this method returns <see langword="null"/>.</returns>
        // todo: remarks example
        public static IDisposable Measure(string category, string operation)
        {
            if (!Enabled)
                return null;

            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (String.IsNullOrEmpty(category))
                category = Res.Get(Res.Uncategorized);

            string key = category + ":" + operation;
            MeasureItem item;
            lock (items)
            {
                if (!items.TryGetValue(key, out item))
                {
                    item = new MeasureItem(category, operation);
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

        #region Static Methods

        private static string GetDefaultDir()
        {
            return Path.Combine(Files.GetExecutingPath() ?? String.Empty, "Profiler");
        }

        private static void DumpResults()
        {
            if (!Enabled || !AutoSaveResults || items.Count == 0)
            {
                return;
            }

            XElement result = new XElement("ProfilerResult");

            lock (items)
            {
                foreach (MeasureItem item in items.Values)
                {
                    XElement xItem = new XElement("item", new XAttribute("Category", item.Category),
                        new XAttribute("Operation", item.Operation),
                        new XAttribute("NumberOfCalls", item.NumberOfCalls),
                        new XAttribute("FirstCall", item.FirstCall.ToString()),
                        new XAttribute("TotalTime", item.TotalElapsed.ToString()),
                        new XAttribute("AverageCallTime", TimeSpan.FromTicks(item.TotalElapsed.Ticks / item.NumberOfCalls).ToString())
                        );

                    result.Add(xItem);
                }
            }

            try
            {
                StringBuilder fileName = new StringBuilder(AppDomain.CurrentDomain.FriendlyName);
                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    fileName.Replace(c, '_');
                }

                if (!Directory.Exists(profilerDir))
                {
                    Directory.CreateDirectory(profilerDir);
                }

                result.Save(Path.Combine(profilerDir, Files.GetNextFileName(String.Format("{0}_{1}.xml", DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss.fffffff", CultureInfo.InvariantCulture), fileName))));
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

        #region Static Event Handlers

        static void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            DumpResults();
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            DumpResults();
        }

        #endregion

        #endregion

        #endregion
    }
}
