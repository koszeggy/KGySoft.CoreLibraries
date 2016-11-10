using System;
using System.Globalization;
using System.Resources;
using KGySoft.Libraries;
using KGySoft.Libraries.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Libraries.Resources
{
    /// <summary>
    /// Test of <see cref="DynamicResourceManager"/> class.
    /// </summary>
    [TestClass]
    public class DynamicResourceManagerTest: TestBase
    {
        private static CultureInfo inv = CultureInfo.InvariantCulture;

        private static CultureInfo enUS = CultureInfo.GetCultureInfo("en-US");
        private static CultureInfo en = CultureInfo.GetCultureInfo("en");
        private static CultureInfo enGB = CultureInfo.GetCultureInfo("en-GB");

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

            //// Exception is thrown through base HRM
            //Throws<MissingManifestResourceException>(() => manager.GetString(key, inv));

            //// Due to possible append options, exception is thrown through derived DRM
            //// For the neutral en culture a resource set is created during the traversal
            //Throws<MissingManifestResourceException>(() => manager.GetString(key, enUS));
            //Assert.IsNull(manager.GetResourceSet(enUS, false, false));
            //Assert.IsNotNull(manager.GetResourceSet(en, false, false));

            //// Exception is not thrown any more. Instead, a new resource is automatically added
            //manager.AutoAppend = AutoAppendOptions.AddUnknownToInvariantCulture;
            //manager.ReleaseAllResources();

            //Assert.IsTrue(manager.GetString(key, inv).StartsWith(LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            //Assert.IsTrue(manager.GetString(key, en).StartsWith(LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            //Assert.IsTrue(manager.GetString(key, enUS).StartsWith(LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            //manager.ReleaseAllResources();
            //Assert.IsTrue(manager.GetString(key, enUS).StartsWith(LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));

            //// If requested as derived with append, the new resource is merged into derived resources
            //manager.AutoAppend = AutoAppendOptions.AddUnknownToInvariantCulture | AutoAppendOptions.AppendNeutralCultures;
            //manager.ReleaseAllResources();
            //Assert.IsTrue(manager.GetString(key, enUS).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            //IExpandoResourceSet rsEn = manager.GetExpandoResourceSet(en, ResourceSetRetrieval.GetIfAlreadyLoaded, false);
            //Assert.IsTrue(rsEn.ContainsResource(key));
            //Assert.AreSame(rsEn, manager.GetResourceSet(enUS, false, false), "en should be proxied for en-US");
            //manager.AutoAppend |= AutoAppendOptions.AppendSpecificCultures;
            //Assert.IsTrue(manager.GetString(key, enUS).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            //IExpandoResourceSet rsEnUs = manager.GetResourceSet(enUS, false, false) as IExpandoResourceSet;
            //Assert.IsNotNull(rsEnUs);
            //Assert.AreNotSame(rsEn, rsEnUs, "Due to merge a new resource set should have been created for en-US");
            //Assert.IsTrue(rsEnUs.ContainsResource(key));

            // As object: null is added to invariant
            manager.ReleaseAllResources();
            // tart: ez már ok, megnézni, hogy HRM-ből sem iterál végig a base-ből jövő null-ra
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
            // top/bottom/all, with specific, too
            throw new NotImplementedException();
        }

        [TestMethod]
        public void MergeNeutralOnLoadTest()
        {
            // top/bottom/all, with specific, too
            throw new NotImplementedException();
        }

        [TestMethod]
        public void MergeSpecificTest()
        {
            // top/bottom/all, with neutral, too
            throw new NotImplementedException();
        }

        [TestMethod]
        public void MergeSpecificOnLoadTest()
        {
            // top/bottom/all, with neutral, too
            throw new NotImplementedException();
        }

        // TODO: 
    }
}
