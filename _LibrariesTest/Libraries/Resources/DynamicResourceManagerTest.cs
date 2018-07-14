using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Policy;
using KGySoft.Libraries;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Libraries.Resources
{
    /// <summary>
    /// Test of <see cref="DynamicResourceManager"/> class.
    /// </summary>
    [TestClass]
    [DeploymentItem("Resources", "Resources")]
    [DeploymentItem("en", "en")]
    [DeploymentItem("en-US", "en-US")]
    public class DynamicResourceManagerTest: TestBase
    {
        private class RemoteDrmConsumer : MarshalByRefObject
        {
            internal void UseDrmRemotely(bool useLanguageSettings, CultureInfo testCulture)
            {
                var manager = new DynamicResourceManager("_LibrariesTest.Resources.TestCompiledResource", GetType().Assembly, "TestResourceResX")
                {
                    AutoAppend = AutoAppendOptions.None,
                    UseLanguageSettings = useLanguageSettings,
                };

                if (!useLanguageSettings)
                    manager.AutoSave = AutoSaveOptions.DomainUnload;
                else
                {
                    LanguageSettings.DynamicResourceManagersAutoSave = AutoSaveOptions.DomainUnload;
                    LanguageSettings.DynamicResourceManagersSource = ResourceManagerSources.CompiledAndResX;
                }

                manager.SetObject("TestResourceKey", "TestResourceValue", testCulture);
            }
        }

        private static CultureInfo inv = CultureInfo.InvariantCulture;

        private static CultureInfo enUS = CultureInfo.GetCultureInfo("en-US");
        private static CultureInfo en = CultureInfo.GetCultureInfo("en");
        private static CultureInfo enGB = CultureInfo.GetCultureInfo("en-GB");

        private static CultureInfo de = CultureInfo.GetCultureInfo("de");
        private static CultureInfo deDE = CultureInfo.GetCultureInfo("de-DE");

        private static CultureInfo hu = CultureInfo.GetCultureInfo("hu");
        private static CultureInfo huHU = CultureInfo.GetCultureInfo("hu-HU");
        private static CultureInfo huRunic; // hu-Runic: neutral under hu
        private static CultureInfo huRunicHU; // hu-Runic-HU: specific under hu-Runic
        private static CultureInfo huRunicHULowland; // hu-Runic-HU-lowland: specific under hu-Runic-HU

        /// <summary>
        /// Creates a culture chain with more specific and neutral cultures.
        /// </summary>
        [ClassInitialize]
        public static void CreateCustomCultures(TestContext context)
        {
            CultureAndRegionInfoBuilder cibHuRunic = new CultureAndRegionInfoBuilder("hu-Runic", CultureAndRegionModifiers.Neutral);
            cibHuRunic.LoadDataFromCultureInfo(hu);
            cibHuRunic.IsRightToLeft = true;
            cibHuRunic.Parent = hu;
            cibHuRunic.CultureEnglishName = "Hungarian (Runic)";
            cibHuRunic.CultureNativeName = "magyar (Rovásírás)";

            try
            {
                cibHuRunic.Register();
            }
            catch (UnauthorizedAccessException e)
            {
                Assert.Inconclusive("To run the tests in this class, administrator rights are required: " + e);
            }
            catch (InvalidOperationException e) when (e.Message.Contains("already exists"))
            {
                // culture is already registered
            }

            huRunic = CultureInfo.GetCultureInfo("hu-Runic");

            CultureAndRegionInfoBuilder cibHuRunicHU = new CultureAndRegionInfoBuilder("hu-Runic-HU", CultureAndRegionModifiers.None);
            cibHuRunicHU.LoadDataFromCultureInfo(huHU);
            cibHuRunicHU.LoadDataFromRegionInfo(new RegionInfo(huHU.Name));
            cibHuRunicHU.Parent = huRunic;
            cibHuRunicHU.CultureEnglishName = "Hungarian (Runic, Hungary)";
            cibHuRunicHU.CultureNativeName = "magyar (Rovásírás, Magyarország)";

            try
            {
                cibHuRunicHU.Register();
            }
            catch (InvalidOperationException e) when (e.Message.Contains("already exists"))
            {
                // culture is already registered
            }

            huRunicHU = CultureInfo.GetCultureInfo("hu-Runic-HU");

            CultureAndRegionInfoBuilder cibHuRunicHULowland = new CultureAndRegionInfoBuilder("hu-Runic-HU-lowland", CultureAndRegionModifiers.None);
            cibHuRunicHULowland.LoadDataFromCultureInfo(huRunicHU);
            cibHuRunicHULowland.LoadDataFromRegionInfo(new RegionInfo(huRunicHU.Name));
            cibHuRunicHULowland.Parent = huRunicHU;
            cibHuRunicHULowland.CultureEnglishName = "Hungarian (Runic, Hungary, Lowland)";
            cibHuRunicHULowland.CultureNativeName = "magyar (Rovásírás, Magyarország, Alföld)";
            try
            {
                cibHuRunicHULowland.Register();
            }
            catch (InvalidOperationException e) when (e.Message.Contains("already exists"))
            {
                // culture is already registered
            }

            huRunicHULowland = CultureInfo.GetCultureInfo("hu-Runic-HU-lowland");
        }

        [ClassCleanup]
        public static void RemoveCustomCultures()
        {
            CultureAndRegionInfoBuilder.Unregister(huRunicHULowland.Name);
            CultureAndRegionInfoBuilder.Unregister(huRunicHU.Name);
            CultureAndRegionInfoBuilder.Unregister(huRunic.Name);
        }

        [TestMethod]
        public void GetUnknownTest()
        {
            // This is a non-existing resource so MissingManifestResourceException should be thrown by default
            var manager = new DynamicResourceManager(GetType())
            {
                AutoSave = AutoSaveOptions.None,
                AutoAppend = AutoAppendOptions.AppendNeutralCultures,
                ThrowException = true
            };

            string key = "unknown";

            // Exception is thrown through base HRM
            Throws<MissingManifestResourceException>(() => manager.GetString(key, inv));

            // Due to possible append options, exception is thrown through derived DRM
            // For the neutral en culture a resource set is created during the traversal
            Throws<MissingManifestResourceException>(() => manager.GetString(key, enUS));
            Assert.IsNull(manager.GetResourceSet(enUS, false, false));
            Assert.IsNotNull(manager.GetResourceSet(en, false, false));

            // Exception is not thrown any more. Instead, a new resource is automatically added
            manager.AutoAppend = AutoAppendOptions.AddUnknownToInvariantCulture;
            manager.ReleaseAllResources();

            Assert.IsTrue(manager.GetString(key, inv).StartsWith(LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            Assert.IsTrue(manager.GetString(key, en).StartsWith(LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            Assert.IsTrue(manager.GetString(key, enUS).StartsWith(LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            manager.ReleaseAllResources();
            Assert.IsTrue(manager.GetString(key, enUS).StartsWith(LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));

            // If requested as derived with append, the new resource is merged into derived resources
            manager.AutoAppend = AutoAppendOptions.AddUnknownToInvariantCulture | AutoAppendOptions.AppendNeutralCultures;
            manager.ReleaseAllResources();
            Assert.IsTrue(manager.GetString(key, enUS).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            IExpandoResourceSet rsEn = manager.GetExpandoResourceSet(en, ResourceSetRetrieval.GetIfAlreadyLoaded, false);
            Assert.IsTrue(rsEn.ContainsResource(key));
            Assert.AreSame(rsEn, manager.GetResourceSet(enUS, false, false), "en should be proxied for en-US");
            manager.AutoAppend |= AutoAppendOptions.AppendSpecificCultures;
            Assert.IsTrue(manager.GetString(key, enUS).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            IExpandoResourceSet rsEnUs = manager.GetResourceSet(enUS, false, false) as IExpandoResourceSet;
            Assert.IsNotNull(rsEnUs);
            Assert.AreNotSame(rsEn, rsEnUs, "Due to merge a new resource set should have been created for en-US");
            Assert.IsTrue(rsEnUs.ContainsResource(key));

            // As object: null is added to invariant
            manager.ReleaseAllResources();
            Assert.IsNull(manager.GetObject(key, inv));
            manager.GetExpandoResourceSet(inv, ResourceSetRetrieval.GetIfAlreadyLoaded, false).ContainsResource(key);

            // but null is not merged even if the child resource sets are created
            Assert.IsNull(manager.GetObject(key, enUS));
            Assert.IsNotNull(manager.GetResourceSet(enUS, false, false));
            Assert.IsFalse(manager.GetExpandoResourceSet(enUS, ResourceSetRetrieval.GetIfAlreadyLoaded, false).ContainsResource(key));
            Assert.IsNotNull(manager.GetResourceSet(en, false, false));
            Assert.IsFalse(manager.GetExpandoResourceSet(en, ResourceSetRetrieval.GetIfAlreadyLoaded, false).ContainsResource(key));
            Assert.IsNotNull(manager.GetResourceSet(inv, false, false));
            Assert.IsTrue(manager.GetExpandoResourceSet(inv, ResourceSetRetrieval.GetIfAlreadyLoaded, false).ContainsResource(key));

            // a null object does not turn to string
            Assert.IsNull(manager.GetString(key, enUS));

            // changing back to compiled only sources disables appending invariant so exception will be thrown again
            manager.Source = ResourceManagerSources.CompiledOnly;
            Throws<MissingManifestResourceException>(() => manager.GetString(key, enUS));
        }

        [TestMethod]
        public void MergeNeutralTest()
        {
            var manager = new DynamicResourceManager("_LibrariesTest.Resources.TestCompiledResource", GetType().Assembly, "TestResourceResX")
            {
                AutoSave = AutoSaveOptions.None,
                AutoAppend = AutoAppendOptions.AppendLastNeutralCulture
            };

            string key = "TestString";
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // this will widen append so proxies are not trustworthy anymore
            manager.AutoAppend = AutoAppendOptions.AppendFirstNeutralCulture;
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // the result will come from proxy eventually
            manager.AutoAppend = AutoAppendOptions.AppendNeutralCultures;
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // this will merge multiple resources
            manager.ReleaseAllResources();
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // inserting explicitly into a non-merged: it will be retrieved via child, too
            var custom = "custom";
            manager.SetObject(key, custom, huRunicHU);
            Assert.AreEqual(custom, manager.GetString(key, huRunicHULowland));
        }

        [TestMethod]
        public void MergeNeutralOnLoadTest()
        {
            var manager = new DynamicResourceManager("_LibrariesTest.Resources.TestCompiledResource", GetType().Assembly, "TestResourceResX")
            {
                AutoSave = AutoSaveOptions.None,
                AutoAppend = AutoAppendOptions.AppendLastNeutralCulture | AutoAppendOptions.AppendOnLoad
            };

            string key = "TestString";

            // retrieving hu with merge
            var rsinv = manager.GetExpandoResourceSet(inv);
            var rshu = manager.GetExpandoResourceSet(hu, ResourceSetRetrieval.LoadIfExists, true); // this will not create a new rs
            Assert.AreSame(rsinv, rshu);
            Assert.IsFalse(rshu.GetString(key).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
            rshu = manager.GetExpandoResourceSet(hu, ResourceSetRetrieval.CreateIfNotExists, true); // but this will, and performs merge, too
            Assert.AreNotSame(rsinv, rshu);
            Assert.IsTrue(rshu.GetString(key).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // hu is proxied in descendants, too.
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
            Assert.AreSame(rshu, manager.GetExpandoResourceSet(huRunicHULowland, ResourceSetRetrieval.GetIfAlreadyLoaded, false));

            // now hu-Runic is proxied in descendants, content is copied from hu
            manager.AutoAppend = AutoAppendOptions.AppendFirstNeutralCulture | AutoAppendOptions.AppendOnLoad;
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
            Assert.AreNotSame(rshu, manager.GetExpandoResourceSet(huRunicHULowland, ResourceSetRetrieval.GetIfAlreadyLoaded, false));
            Assert.AreSame(manager.GetExpandoResourceSet(huRunic, ResourceSetRetrieval.GetIfAlreadyLoaded, false), manager.GetExpandoResourceSet(huRunicHULowland, ResourceSetRetrieval.GetIfAlreadyLoaded, false));

            // now hu is not proxied, specifics are proxy of hu-Runic
            manager.ReleaseAllResources();
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
            Assert.IsNull(manager.GetExpandoResourceSet(hu, ResourceSetRetrieval.GetIfAlreadyLoaded, false));
            Assert.AreSame(manager.GetExpandoResourceSet(huRunic, ResourceSetRetrieval.GetIfAlreadyLoaded, false), manager.GetExpandoResourceSet(huRunicHULowland, ResourceSetRetrieval.GetIfAlreadyLoaded, false));

            // now every neutral is merged
            manager.ReleaseAllResources();
            manager.AutoAppend = AutoAppendOptions.AppendNeutralCultures | AutoAppendOptions.AppendOnLoad;
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
            rsinv = manager.GetExpandoResourceSet(inv);
            rshu = manager.GetExpandoResourceSet(hu);
            var rshuRunic = manager.GetExpandoResourceSet(huRunic);
            Assert.AreNotSame(rsinv, rshu);
            Assert.AreNotSame(rshu, rshuRunic);
        }

        [TestMethod]
        public void MergeSpecificTest()
        {
            var manager = new DynamicResourceManager("_LibrariesTest.Resources.TestCompiledResource", GetType().Assembly, "TestResourceResX")
            {
                AutoSave = AutoSaveOptions.None,
                AutoAppend = AutoAppendOptions.AppendLastSpecificCulture
            };

            // the result will come from proxy eventually
            string key = "TestString";
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // this will widen append so proxies are not trustworthy anymore but proxy will replaced anyway
            manager.AutoAppend = AutoAppendOptions.AppendFirstSpecificCulture;
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // this will merge multiple resources
            manager.AutoAppend = AutoAppendOptions.AppendSpecificCultures;
            manager.ReleaseAllResources();
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // inserting explicitly into a base: it will not be retrieved via child, because they are not proxies
            var custom = "custom";
            manager.SetObject(key, custom, hu);
            Assert.AreNotEqual(custom, manager.GetString(key, huRunicHULowland));
        }

        [TestMethod]
        public void MergeSpecificOnLoadTest()
        {
            var manager = new DynamicResourceManager("_LibrariesTest.Resources.TestCompiledResource", GetType().Assembly, "TestResourceResX")
            {
                AutoSave = AutoSaveOptions.None,
                AutoAppend = AutoAppendOptions.AppendLastSpecificCulture | AutoAppendOptions.AppendOnLoad
            };

            string key = "TestString";

            // retrieving spec with merge
            var rsinv = manager.GetExpandoResourceSet(inv);
            var rshuRunicHu = manager.GetExpandoResourceSet(huRunicHU, ResourceSetRetrieval.LoadIfExists, true); // this will not create a new rs
            Assert.AreSame(rsinv, rshuRunicHu);
            Assert.IsFalse(rshuRunicHu.GetString(key).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
            rshuRunicHu = manager.GetExpandoResourceSet(huRunicHU, ResourceSetRetrieval.CreateIfNotExists, true); // but this will, and performs merge, too
            Assert.AreNotSame(rsinv, rshuRunicHu);
            Assert.IsTrue(rshuRunicHu.GetString(key).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // huRunicHU is proxied into huRunicHULowland, too.
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
            Assert.AreSame(rshuRunicHu, manager.GetExpandoResourceSet(huRunicHULowland, ResourceSetRetrieval.GetIfAlreadyLoaded, false));

            // now proxy of huRunicHULowland is replaced by a normal resource set
            manager.AutoAppend = AutoAppendOptions.AppendFirstSpecificCulture | AutoAppendOptions.AppendOnLoad;
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
            Assert.AreNotSame(huRunicHU, manager.GetExpandoResourceSet(huRunicHULowland, ResourceSetRetrieval.GetIfAlreadyLoaded, false));

            // now only huRunicHULowland is created (from inv), no proxies because it has no descendants
            manager.ReleaseAllResources();
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
            Assert.IsNull(manager.GetExpandoResourceSet(huRunicHU, ResourceSetRetrieval.GetIfAlreadyLoaded, false));

            // now every specific is merged
            manager.ReleaseAllResources();
            manager.AutoAppend = AutoAppendOptions.AppendSpecificCultures | AutoAppendOptions.AppendOnLoad;
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
            rsinv = manager.GetExpandoResourceSet(inv);
            rshuRunicHu = manager.GetExpandoResourceSet(huRunicHU);
            var rshuRunicHULowland = manager.GetExpandoResourceSet(huRunicHULowland);
            Assert.AreNotSame(rsinv, rshuRunicHu);
            Assert.AreNotSame(rshuRunicHu, rshuRunicHULowland);
        }

        [TestMethod]
        public void NonContinguousProxyTest()
        {
            // now it is like HRM
            var manager = new DynamicResourceManager("_LibrariesTest.Resources.TestCompiledResource", GetType().Assembly, "TestResourceResX")
            {
                AutoSave = AutoSaveOptions.None,
                AutoAppend = AutoAppendOptions.None
            };

            // preparing the chain: inv (loaded), hu (proxy), hu-Runic (created), hu-Runic-HU (proxy), hu-Runic-HU-Lowland (created)
            manager.GetExpandoResourceSet(huRunic, ResourceSetRetrieval.CreateIfNotExists);
            manager.GetExpandoResourceSet(huRunicHULowland, ResourceSetRetrieval.CreateIfNotExists);
            Assert.AreSame(manager.GetResourceSet(hu, true, true), manager.GetResourceSet(inv, false, false)); // now hu is proxy, inv is loaded
            Assert.AreSame(manager.GetResourceSet(huRunicHU, true, true), manager.GetResourceSet(huRunic, true, true)); // now huRunicHU is proxy, huRunic is already loaded

            var resourceSets = (Dictionary<string, ResourceSet>)Reflector.GetInstanceFieldByName(manager, "resourceSets");
            Assert.AreEqual(5, resourceSets.Count);
            Assert.AreEqual(2, resourceSets.Count(kv => kv.Value.GetType().Name.Contains("Proxy")));

            string key = "unknown";

            // through HRM: does not change anything
            Assert.IsNull(manager.GetString(key, huRunicHULowland));
            Assert.AreEqual(5, resourceSets.Count);
            Assert.AreEqual(2, resourceSets.Count(kv => kv.Value.GetType().Name.Contains("Proxy")));

            // through DRM but without merging (adding to invariant only): does not change anything, proxies remain intact
            manager.AutoAppend = AutoAppendOptions.AddUnknownToInvariantCulture;
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            Assert.AreEqual(5, resourceSets.Count);
            Assert.AreEqual(2, resourceSets.Count(kv => kv.Value.GetType().Name.Contains("Proxy")));

            // huRunic is about to be merged, this already existed so proxies remain intact
            manager.AutoAppend = AutoAppendOptions.AppendFirstNeutralCulture;
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            Assert.AreEqual(5, resourceSets.Count);
            Assert.AreEqual(2, resourceSets.Count(kv => kv.Value.GetType().Name.Contains("Proxy")));

            // specifics are merged so result comes from huRunic, hu proxy remains, huRunicHU is replaced
            manager.AutoAppend = AutoAppendOptions.AppendSpecificCultures;
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            Assert.AreEqual(5, resourceSets.Count);
            Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name.Contains("Proxy")));

            // result is coming from the completely merged huRunicHULowland so nothing changes in the base
            manager.AutoAppend = AutoAppendOptions.AppendNeutralCultures;
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            Assert.AreEqual(5, resourceSets.Count);
            Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name.Contains("Proxy")));

            // even if the rs of the proxied hu is retrieved
            manager.GetResourceSet(hu, true, true);
            Assert.AreEqual(5, resourceSets.Count);
            Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name.Contains("Proxy")));

            // result is coming from the cached merged huRunicHULowland so nothing changes in the base even if AppendOnLoad is requested
            manager.AutoAppend |= AutoAppendOptions.AppendOnLoad;
            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            Assert.AreEqual(5, resourceSets.Count);
            Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name.Contains("Proxy")));

            // even if the rs of the proxied hu is retrieved with Load only (EnsureMerged is executed but no create occurs so the proxied inv will be returned)
            var rsinv = manager.GetResourceSet(hu, true, true);
            Assert.AreEqual(5, resourceSets.Count);
            Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name.Contains("Proxy")));
            Assert.AreSame(manager.GetResourceSet(inv, false, false), rsinv);

            // but for Create it is loaded and immediately merged, too
            var rshu = manager.GetExpandoResourceSet(hu, ResourceSetRetrieval.CreateIfNotExists, true);
            Assert.AreEqual(5, resourceSets.Count);
            Assert.AreEqual(0, resourceSets.Count(kv => kv.Value.GetType().Name.Contains("Proxy")));
            Assert.AreNotSame(rsinv, rshu);
            Assert.IsTrue(rshu.ContainsResource(key));
        }

        [TestMethod]
        public void AutoSaveTest()
        {
            LanguageSettings.DynamicResourceManagersAutoAppend = AutoAppendOptions.None;
            string key = "testKey";
            string value = "test value";
            var manager = new DynamicResourceManager("_LibrariesTest.Resources.TestCompiledResource", GetType().Assembly, "TestResourceResX")
            {
                AutoAppend = AutoAppendOptions.None,
            };

            // making sure that the resources, which will be created in the test later do not exist yet as a file
            Assert.IsNull(manager.GetResourceSet(hu, true, false));
            Assert.IsNull(manager.GetResourceSet(huHU, true, false));
            Assert.IsNull(manager.GetResourceSet(huRunic, true, false));
            Assert.IsNull(manager.GetResourceSet(huRunicHU, true, false));
            Assert.IsNull(manager.GetResourceSet(huRunicHULowland, true, false));
            Assert.IsNull(manager.GetResourceSet(enGB, true, false));
            Assert.IsNull(manager.GetResourceSet(de, true, false));
            Assert.IsNull(manager.GetResourceSet(deDE, true, false));

            // SourceChange, individual
            CultureInfo testCulture = hu;
            manager.Source = ResourceManagerSources.CompiledAndResX;
            manager.UseLanguageSettings = false;
            manager.AutoSave = AutoSaveOptions.SourceChange;

            manager.SetObject(key, value, testCulture);
            manager.Source = ResourceManagerSources.ResXOnly; // save occurs
            manager.ReleaseAllResources();
            Assert.IsNull(manager.GetResourceSet(testCulture, false, false)); // not loaded after release
            Assert.IsNotNull(manager.GetResourceSet(testCulture, true, false)); // but can be loaded from saved

            // SourceChange, central
            testCulture = huHU;
            manager.ReleaseAllResources();
            LanguageSettings.DynamicResourceManagersSource = ResourceManagerSources.CompiledAndResX;
            manager.UseLanguageSettings = true;
            LanguageSettings.DynamicResourceManagersAutoSave = AutoSaveOptions.SourceChange;

            manager.SetObject(key, value, testCulture);
            LanguageSettings.DynamicResourceManagersSource = ResourceManagerSources.ResXOnly; // save occurs
            manager.ReleaseAllResources();
            Assert.IsNull(manager.GetResourceSet(testCulture, false, false)); // not loaded after release
            Assert.IsNotNull(manager.GetResourceSet(testCulture, true, false)); // but can be loaded from saved

            // LanguageChange, individual
            testCulture = huRunic;
            manager.ReleaseAllResources();
            manager.UseLanguageSettings = false;
            LanguageSettings.DisplayLanguage = testCulture;
            manager.AutoSave = AutoSaveOptions.LanguageChange;

            manager.SetObject(key, value); // null: uses DisplayLanguage, which is the same as CurrentUICulture
            LanguageSettings.DisplayLanguage = inv; // save occurs
            manager.ReleaseAllResources();
            Assert.IsNull(manager.GetResourceSet(testCulture, false, false)); // not loaded after release
            Assert.IsNotNull(manager.GetResourceSet(testCulture, true, false)); // but can be loaded from saved

            // LanguageChange, central
            testCulture = huRunicHU;
            manager.ReleaseAllResources();
            manager.UseLanguageSettings = true;
            LanguageSettings.DisplayLanguage = testCulture;
            LanguageSettings.DynamicResourceManagersAutoSave = AutoSaveOptions.LanguageChange;

            manager.SetObject(key, value); // null: uses DisplayLanguage, which is the same as CurrentUICulture
            LanguageSettings.DisplayLanguage = inv; // save occurs
            manager.ReleaseAllResources();
            Assert.IsNull(manager.GetResourceSet(testCulture, false, false)); // not loaded after release
            Assert.IsNotNull(manager.GetResourceSet(testCulture, true, false)); // but can be loaded from saved

            // DomainUnload, individual
            testCulture = huRunicHULowland;
            manager.ReleaseAllResources();
            Evidence evidence = new Evidence(AppDomain.CurrentDomain.Evidence);
            AppDomain sandboxDomain = AppDomain.CreateDomain("SandboxDomain", evidence, AppDomain.CurrentDomain.BaseDirectory, null, false);
            AssemblyName selfName = Assembly.GetExecutingAssembly().GetName();
            sandboxDomain.Load(selfName);

            RemoteDrmConsumer remote = (RemoteDrmConsumer)sandboxDomain.CreateInstanceAndUnwrap(selfName.FullName, typeof(RemoteDrmConsumer).FullName);
            remote.UseDrmRemotely(false, testCulture);
            AppDomain.Unload(sandboxDomain);
            Assert.IsNotNull(manager.GetResourceSet(testCulture, true, false)); // can be loaded that has been saved in another domain

            // DomainUnload, central
            testCulture = enGB;
            manager.ReleaseAllResources();
            evidence = new Evidence(AppDomain.CurrentDomain.Evidence);
            sandboxDomain = AppDomain.CreateDomain("SandboxDomain", evidence, AppDomain.CurrentDomain.BaseDirectory, null, false);
            selfName = Assembly.GetExecutingAssembly().GetName();
            sandboxDomain.Load(selfName);

            remote = (RemoteDrmConsumer)sandboxDomain.CreateInstanceAndUnwrap(selfName.FullName, typeof(RemoteDrmConsumer).FullName);
            remote.UseDrmRemotely(true, testCulture);
            AppDomain.Unload(sandboxDomain);
            Assert.IsNotNull(manager.GetResourceSet(testCulture, true, false)); // can be loaded that has been saved in another domain

            // Dispose, individual
            testCulture = de;
            manager.UseLanguageSettings = false;
            manager.Source = ResourceManagerSources.CompiledAndResX;
            manager.UseLanguageSettings = false;
            manager.AutoSave = AutoSaveOptions.Dispose;

            manager.SetObject(key, value, testCulture);
            manager.Dispose(); // save occurs
            Throws<ObjectDisposedException>(() => manager.GetResourceSet(testCulture, false, false));
            Assert.IsTrue(File.Exists("Resources\\TestResourceResX.de.resx"));

            // Dispose, central
            LanguageSettings.DynamicResourceManagersSource = ResourceManagerSources.CompiledAndResX;
            LanguageSettings.DynamicResourceManagersAutoSave = AutoSaveOptions.Dispose;
            manager = new DynamicResourceManager("_LibrariesTest.Resources.TestCompiledResource", GetType().Assembly, "TestResourceResX")
            {
                UseLanguageSettings = true
            };
            testCulture = deDE;

            manager.SetObject(key, value, testCulture);
            LanguageSettings.DisplayLanguage = inv; // save occurs
            manager.Dispose(); // save occurs
            Throws<ObjectDisposedException>(() => manager.GetResourceSet(testCulture, false, false));
            Assert.IsTrue(File.Exists("Resources\\TestResourceResX.de-DE.resx"));

            // cleaning up the newly created resources
            File.Delete("Resources\\TestResourceResX.hu.resx");
            File.Delete("Resources\\TestResourceResX.hu-HU.resx");
            File.Delete("Resources\\TestResourceResX.hu-Runic.resx");
            File.Delete("Resources\\TestResourceResX.hu-Runic-HU.resx");
            File.Delete("Resources\\TestResourceResX.hu-Runic-HU-Lowland.resx");
            File.Delete("Resources\\TestResourceResX.en-GB.resx");
            File.Delete("Resources\\TestResourceResX.de.resx");
            File.Delete("Resources\\TestResourceResX.de-DE.resx");
        }

        [TestMethod]
        public void SerializationTest()
        {
            var refManager = new ResourceManager("_LibrariesTest.Resources.TestResourceResX", GetType().Assembly);
            var manager = new DynamicResourceManager("_LibrariesTest.Resources.TestCompiledResource", GetType().Assembly, "TestResourceResX")
            {
                AutoAppend = AutoAppendOptions.None,
                AutoSave = AutoSaveOptions.None
            };

            var resName = "TestString";

            // serializing and de-serializing removes the unchanged resources
            string testResRef = refManager.GetString(resName);
            string testRes = manager.GetString(resName);
            Assert.IsNotNull(testResRef);
            Assert.IsNotNull(testRes);
            // TODO .NET 3.5: get/set pointer fields by FieldAccessor
            refManager = refManager.DeepClone();
            manager = manager.DeepClone();
            Assert.AreEqual(testResRef, refManager.GetString(resName));
            Assert.AreEqual(testRes, manager.GetString(resName));

            // introducing a change: serialization preserves the change
            Assert.IsFalse(manager.IsModified);
            manager.SetObject(resName, "new string");
            Assert.IsTrue(manager.IsModified);
            CheckTestingFramework(); // the modified resource sets are searched in ResourceManager.ResourceSets Hashtable in .NET 3.5 and in ResXResourceManager.resourceSets Dictionary above.
            manager = manager.DeepClone();
            Assert.IsTrue(manager.IsModified);
            Assert.AreNotEqual(testRes, manager.GetString(resName));
        }

        [TestMethod]
        public void DisposeTest()
        {
            var manager = new DynamicResourceManager("_LibrariesTest.Resources.TestCompiledResource", GetType().Assembly, "TestResourceResX")
            {
                AutoSave = AutoSaveOptions.None
            };
            manager.Dispose();
            Throws<ObjectDisposedException>(() => manager.ReleaseAllResources());
            Throws<ObjectDisposedException>(() => manager.GetString("TestString"));
            manager.Dispose(); // this will not throw anything

            manager = new DynamicResourceManager("_LibrariesTest.Resources.TestCompiledResource", GetType().Assembly, "TestResourceResX");
            manager.Source = ResourceManagerSources.CompiledOnly;
            manager.Dispose();
            Throws<ObjectDisposedException>(() => manager.ReleaseAllResources());
            Throws<ObjectDisposedException>(() => manager.GetString("TestString"));
            manager.Dispose(); // this will not throw anything
        }

    }
}
