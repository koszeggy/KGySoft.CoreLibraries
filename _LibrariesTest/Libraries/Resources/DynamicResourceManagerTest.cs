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
                AutoAppend = AutoAppendOptions.AppendFirstNeutralCulture
            };

            string key = "TestString";

            // TODO: a cached proxyt nem fogadja el, pedig az abban lévő eredmény jó.
            // Átgondolni, mennyire specifikusan lehet a problémát megfogalmazni:
            // a.) Konkrét: Specifiusat kérünk, a első neutral appendeléssel, a proxyban, ahol az eredmény van, pont az első neutral van, tehát jó
            // b.) Általános: Ha proxy van cache-elve, és van benne eredmény olyan rs-ből, amit már kell merge-ölni, akkor jó
            // c.) Gordiuszi: Mindig jó a proxy, ha a hierarchia be van töltve (biztos?). Ha változik az AutoAppend, törölni a resource set cache-t és a last-ot, és akkor megint jól fog felépülni.
            //     -> nem igaz, pl. GetResourceSet tryParenttel, miközben nincs AppendOnLoad és a culture lánc nem létezik: inv-hez jönnek létre a proxyk.
            //        IsProxyAccepted tehát kell, de alapvetően csak azt kell ellenőrizni, hogy a wrapped culture az nem felsőbb parent-e, mint az első merge-ölendő culture a láncban. Minden hozzáadott plusz ellenőrzés is csak ezt döntse el gyorsan tesztelhető esetekben.
            //        - Egyik megoldás a fenti, tehát mindig ellenőrzünk
            //        - Félgordiuszi: ha csak a GetResourceSet tudja elrontani (tryParent és nem AppendOnLoad esetén), akkor egy field: canAcceptProxy, alapból true
            //                        - set false: Get(E)ResourceSet, ha tryParents=true, culture != inv, és nincs AppendOnLoad
            //                        - set true: Release, és minden olyan property állítás, ami töröl (AutoAppend talán törölhet csak akkor, ha false volt)
            //                        - ha éppen false, az IsProxyAccepted ellenőrizheti a wrapped culture dolgot, egyébként csak a base-t és a bool flaget


            Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // beleszúrni közepébe

            // top/bottom/all, with specific, too
        }

        // TODO: igazából HRM-hez: ha nagyon hosszú a culture lánc, lehet-e olyat létrehozni, hogy proxy lesz középen, és ez gondot okoz-e
        // pl: explicit hu-Runic, majd elkérés hu-szerint: proxy hu-ra, alatta valódi hu-Runic
        // majd explicit hu-Runic-HU-Lowland, elkérés hu-Runic-HU-ra: 2 proxy (hu és hu-Runic-HU)
        // majd unknown elkérése hu-Runic-HU-Lowland-re HRM-ben: ekkor be kell járnia tryParents-szel az egészet
        //                                              és DRM-ben: elvileg nem gond, tryParents=false a base-re, aztán true a legspecifikusabbra, abból baj nem lehet

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
