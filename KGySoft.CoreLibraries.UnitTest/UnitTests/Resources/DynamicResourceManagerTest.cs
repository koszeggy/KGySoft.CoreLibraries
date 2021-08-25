#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DynamicResourceManagerTest.cs
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security;
using System.Security.Policy;
using System.Xml;

using KGySoft.Collections;
using KGySoft.Reflection;
using KGySoft.Resources;

using NUnit.Framework;

#endregion

#region Suppressions

#if !NETFRAMEWORK
#pragma warning disable IDE0044 // The fields are assigned in .NET Framework versions  
#endif

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Resources
{
    /// <summary>
    /// Test of <see cref="DynamicResourceManager"/> class.
    /// </summary>
    [TestFixture]
    public class DynamicResourceManagerTest : TestBase
    {
        #region Nested classes

#if NETFRAMEWORK
        private class RemoteDrmConsumer : MarshalByRefObject
        {
            #region Methods

            internal void UseDrmRemotely(bool useLanguageSettings, CultureInfo testCulture)
            {
                var manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
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

            #endregion
        } 
#endif

        #endregion

        #region Constants

        private const string resXBaseName = "TestResourceResX";

        #endregion

        #region Fields

        #region Static Fields

        private static readonly CultureInfo inv = CultureInfo.InvariantCulture;
        private static readonly CultureInfo enUS = CultureInfo.GetCultureInfo("en-US");
        private static readonly CultureInfo en = CultureInfo.GetCultureInfo("en");
        private static readonly CultureInfo hu = CultureInfo.GetCultureInfo("hu");
        private static readonly CultureInfo huHU = CultureInfo.GetCultureInfo("hu-HU");

        #endregion

        #region Instance Fields

        private CultureInfo huRunic = default!; // hu-Runic: neutral under hu
        private CultureInfo huRunicHU = default!; // hu-Runic-HU: specific under hu-Runic
        private CultureInfo huRunicHULowland = default!; // hu-Runic-HU-lowland: specific under hu-Runic-HU    

        #endregion

        #endregion

        #region Methods

#if NETFRAMEWORK
        /// <summary>
        /// Creates a culture chain with more specific and neutral cultures.
        /// </summary>
        [SecuritySafeCritical]
        [OneTimeSetUp]
        public void CreateCustomCultures()
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
            catch (UnauthorizedAccessException)
            {
                //Assert.Inconclusive("To run the tests in this class, administrator rights are required: " + e);
                return; // no admin rights - basic tests will be executed
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

        [OneTimeTearDown]
        public void RemoveCustomCultures()
        {
            if (huRunic == null)
                return;
            try
            {
                CultureAndRegionInfoBuilder.Unregister(huRunicHULowland.Name);
                CultureAndRegionInfoBuilder.Unregister(huRunicHU.Name);
                CultureAndRegionInfoBuilder.Unregister(huRunic.Name);
            }
            catch (UnauthorizedAccessException)
            {
                // no admin rights
            }
        } 
#endif

        [Test]
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

        [Test]
        public void MergeNeutralTest()
        {
            var manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
            {
                AutoSave = AutoSaveOptions.None,
                AutoAppend = AutoAppendOptions.AppendLastNeutralCulture
            };
            string key = "TestString";
            string custom = "custom";

            if (huRunic != null)
            {
                // creating proxies
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

                // inserting explicitly into a proxy parent: it will be retrieved via proxy child, too
                manager.SetObject(key, custom, huRunicHU);
                Assert.AreEqual(custom, manager.GetString(key, huRunicHULowland));

                // The string will be prefixed even if retrieved as an object
                manager.ReleaseAllResources();
                Assert.IsTrue(((string)manager.GetObject(key, huRunicHULowland)).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
                return;
            }

            // ---basic tests: no admin rights or not .NET Framework---

            // creating proxies
            Assert.IsTrue(manager.GetString(key, huHU).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // this will widen append so proxies are not trustworthy anymore
            manager.AutoAppend = AutoAppendOptions.AppendFirstNeutralCulture;
            Assert.IsTrue(manager.GetString(key, huHU).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // the result will come from proxy eventually
            manager.AutoAppend = AutoAppendOptions.AppendNeutralCultures;
            Assert.IsTrue(manager.GetString(key, huHU).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // this would merge multiple resources but now we have one neutral (.NET Framework with admin rights needed)
            manager.ReleaseAllResources();
            Assert.IsTrue(manager.GetString(key, huHU).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // inserting explicitly into a proxy parent: it will be retrieved via proxy child, too
            manager.SetObject(key, custom, hu);
            Assert.AreEqual(custom, manager.GetString(key, huHU));

            // The string will be prefixed even if retrieved as an object
            manager.ReleaseAllResources();
            Assert.IsTrue(((string)manager.GetObject(key, huHU)).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

#if NETFRAMEWORK
            Assert.Inconclusive("To run the tests in this class with full functionality, administrator rights are required"); 
#endif
        }

        [Test]
        public void MergeNeutralOnLoadTest()
        {
            var manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
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

            if (huRunic == null)
            {
#if NETFRAMEWORK
                Assert.Inconclusive("To run the tests in this class with full functionality, administrator rights are required");
#endif
                return;
            }

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

            // The string will be prefixed even if retrieved as an object
            manager.ReleaseAllResources();
            Assert.IsTrue(((string)manager.GetObject(key, huRunicHULowland)).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
        }

        [Test]
        public void MergeSpecificTest()
        {
            var manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
            {
                AutoSave = AutoSaveOptions.None,
                AutoAppend = AutoAppendOptions.AppendLastSpecificCulture
            };
            string key = "TestString";
            string custom = "custom";

            if (huRunic != null)
            {
                // the result will come from proxy eventually
                Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

                // this will widen append so proxies are not trustworthy anymore but proxy will replaced anyway
                manager.AutoAppend = AutoAppendOptions.AppendFirstSpecificCulture;
                Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

                // this will merge multiple resources
                manager.AutoAppend = AutoAppendOptions.AppendSpecificCultures;
                manager.ReleaseAllResources();
                Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

                // inserting explicitly into a base: it will not be retrieved via child, because they are not proxies
                manager.SetObject(key, custom, hu);
                Assert.AreNotEqual(custom, manager.GetString(key, huRunicHULowland));
                return;
            }

            // ---basic tests: no admin rights or not .NET Framework---

            // the result will come from a new generated child
            Assert.IsTrue(manager.GetString(key, huHU).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

            // inserting explicitly into a base: it will not be retrieved via child, because it is not a proxy
            manager.SetObject(key, custom, hu);
            Assert.AreNotEqual(custom, manager.GetString(key, huHU));

#if NETFRAMEWORK
            Assert.Inconclusive("To run the tests in this class with full functionality, administrator rights are required");
#endif
        }

        [Test]
        public void MergeSpecificOnLoadTest()
        {
            var manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
            {
                AutoSave = AutoSaveOptions.None,
                AutoAppend = AutoAppendOptions.AppendLastSpecificCulture | AutoAppendOptions.AppendOnLoad
            };

            string key = "TestString";
            IExpandoResourceSet rsinv;

            if (huRunic != null)
            {
                // retrieving spec with merge
                rsinv = manager.GetExpandoResourceSet(inv);
                IExpandoResourceSet rshuRunicHu = manager.GetExpandoResourceSet(huRunicHU, ResourceSetRetrieval.LoadIfExists, true); // this will not create a new rs
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
                return;
            }

            // ---basic tests: no admin rights or not .NET Framework---

            // retrieving spec with merge
            rsinv = manager.GetExpandoResourceSet(inv);
            var rshuHu = manager.GetExpandoResourceSet(huHU, ResourceSetRetrieval.LoadIfExists, true); // this will not create a new rs
            Assert.AreSame(rsinv, rshuHu);
            Assert.IsFalse(rshuHu.GetString(key).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
            rshuHu = manager.GetExpandoResourceSet(huHU, ResourceSetRetrieval.CreateIfNotExists, true); // but this will, and performs merge, too
            Assert.AreNotSame(rsinv, huHU);
            Assert.IsTrue(rshuHu.GetString(key).StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));

#if NETFRAMEWORK
            Assert.Inconclusive("To run the tests in this class with full functionality, administrator rights are required");
#endif
        }

        [Test]
        public void NonContiguousProxyTest()
        {
            // now it is like HRM
            var manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
            {
                AutoSave = AutoSaveOptions.None,
                AutoAppend = AutoAppendOptions.None
            };
            string key = "unknown";
            StringKeyedDictionary<ResourceSet> resourceSets;
            string proxyName = "ProxyResourceSet";

            if (huRunic != null)
            {
                // preparing the chain: inv (loaded), hu (proxy), hu-Runic (created), hu-Runic-HU (proxy), hu-Runic-HU-Lowland (created)
                manager.GetExpandoResourceSet(huRunic, ResourceSetRetrieval.CreateIfNotExists);
                manager.GetExpandoResourceSet(huRunicHULowland, ResourceSetRetrieval.CreateIfNotExists);
                Assert.AreSame(manager.GetResourceSet(hu, true, true), manager.GetResourceSet(inv, false, false)); // now hu is proxy, inv is loaded
                Assert.AreSame(manager.GetResourceSet(huRunicHU, true, true), manager.GetResourceSet(huRunic, true, true)); // now huRunicHU is proxy, huRunic is already loaded

                resourceSets = (StringKeyedDictionary<ResourceSet>)Reflector.GetField(manager, "resourceSets");
                Assert.AreEqual(5, resourceSets.Count);
                Assert.AreEqual(2, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

                // through HRM: does not change anything
                Assert.IsNull(manager.GetString(key, huRunicHULowland));
                Assert.AreEqual(5, resourceSets.Count);
                Assert.AreEqual(2, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

                // through DRM but without merging (adding to invariant only): does not change anything, proxies remain intact
                manager.AutoAppend = AutoAppendOptions.AddUnknownToInvariantCulture;
                Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
                Assert.AreEqual(5, resourceSets.Count);
                Assert.AreEqual(2, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

                // huRunic is about to be merged, this already existed so proxies remain intact
                manager.AutoAppend = AutoAppendOptions.AppendFirstNeutralCulture;
                Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
                Assert.AreEqual(5, resourceSets.Count);
                Assert.AreEqual(2, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

                // specifics are merged so result comes from huRunic, hu proxy remains, huRunicHU is replaced
                manager.AutoAppend = AutoAppendOptions.AppendSpecificCultures;
                Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
                Assert.AreEqual(5, resourceSets.Count);
                Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

                // result is coming from the completely merged huRunicHULowland so nothing changes in the base
                manager.AutoAppend = AutoAppendOptions.AppendNeutralCultures;
                Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
                Assert.AreEqual(5, resourceSets.Count);
                Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

                // even if the rs of the proxied hu is retrieved
                manager.GetResourceSet(hu, true, true);
                Assert.AreEqual(5, resourceSets.Count);
                Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

                // result is coming from the cached merged huRunicHULowland so nothing changes in the base even if AppendOnLoad is requested
                manager.AutoAppend |= AutoAppendOptions.AppendOnLoad;
                Assert.IsTrue(manager.GetString(key, huRunicHULowland).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
                Assert.AreEqual(5, resourceSets.Count);
                Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

                // even if the rs of the proxied hu is retrieved with Load only (EnsureMerged is executed but no create occurs so the proxied inv will be returned)
                var rsinv = manager.GetResourceSet(hu, true, true);
                Assert.AreEqual(5, resourceSets.Count);
                Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));
                Assert.AreSame(manager.GetResourceSet(inv, false, false), rsinv);

                // but for Create it is loaded and immediately merged, too
                var rshu = manager.GetExpandoResourceSet(hu, ResourceSetRetrieval.CreateIfNotExists, true);
                Assert.AreEqual(5, resourceSets.Count);
                Assert.AreEqual(0, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));
                Assert.AreNotSame(rsinv, rshu);
                Assert.IsTrue(rshu.ContainsResource(key));
                return;
            }

            // ---basic tests: no admin rights or not .NET Framework---

            // preparing the chain: inv (loaded), hu (proxy), hu-HU (created)
            manager.GetExpandoResourceSet(huHU, ResourceSetRetrieval.CreateIfNotExists);
            Assert.AreSame(manager.GetResourceSet(hu, true, true), manager.GetResourceSet(inv, false, false)); // now hu is proxy, inv is loaded

            resourceSets = (StringKeyedDictionary<ResourceSet>)Reflector.GetField(manager, "resourceSets");
            Assert.AreEqual(3, resourceSets.Count);
            Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

            // through HRM: does not change anything
            Assert.IsNull(manager.GetString(key, huHU));
            Assert.AreEqual(3, resourceSets.Count);
            Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

            // through DRM but without merging (adding to invariant only): does not change anything, the proxy remains intact
            manager.AutoAppend = AutoAppendOptions.AddUnknownToInvariantCulture;
            Assert.IsTrue(manager.GetString(key, huHU).StartsWith(LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            Assert.AreEqual(3, resourceSets.Count);
            Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

            // specific is merged again, so nothing changes
            manager.RemoveObject(key, huHU);
            manager.AutoAppend = AutoAppendOptions.AppendSpecificCultures;
            Assert.IsTrue(manager.GetString(key, huHU).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            Assert.AreEqual(3, resourceSets.Count);
            Assert.AreEqual(1, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

            // neutral is merged so it will be replaced now
            manager.RemoveObject(key, huHU);
            manager.AutoAppend = AutoAppendOptions.AppendNeutralCultures;
            Assert.IsTrue(manager.GetString(key, hu).StartsWith(LanguageSettings.UntranslatedResourcePrefix + LanguageSettings.UnknownResourcePrefix, StringComparison.Ordinal));
            Assert.AreEqual(3, resourceSets.Count);
            Assert.AreEqual(0, resourceSets.Count(kv => kv.Value.GetType().Name == proxyName));

#if NETFRAMEWORK
            Assert.Inconclusive("To run the tests in this class with full functionality, administrator rights are required");
#endif
        }

        [Test]
        public void AutoSaveTest()
        {
            LanguageSettings.DynamicResourceManagersAutoAppend = AutoAppendOptions.None;
            string key = "testKey";
            string value = "test value";
            var manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
            {
                AutoAppend = AutoAppendOptions.None,
            };
            CultureInfo testCulture = hu;

            void Cleanup()
            {
                //foreach (CultureInfo culture in cultures)
                File.Delete(Path.Combine(Path.Combine(Files.GetExecutingPath(), manager.ResXResourcesDir), $"{resXBaseName}.{testCulture.Name}.resx"));
            }

            try
            {
                // SourceChange, individual
                Cleanup();
                manager.Source = ResourceManagerSources.CompiledAndResX;
                manager.UseLanguageSettings = false;
                manager.AutoSave = AutoSaveOptions.SourceChange;

                manager.SetObject(key, value, testCulture);
                manager.Source = ResourceManagerSources.ResXOnly; // save occurs
                manager.ReleaseAllResources();
                Assert.IsNull(manager.GetResourceSet(testCulture, false, false)); // not loaded after release
                Assert.IsNotNull(manager.GetResourceSet(testCulture, true, false)); // but can be loaded from saved

                // SourceChange, central
                Cleanup();
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
                Cleanup();
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
                Cleanup();
                manager.ReleaseAllResources();
                manager.UseLanguageSettings = true;
                LanguageSettings.DisplayLanguage = testCulture;
                LanguageSettings.DynamicResourceManagersAutoSave = AutoSaveOptions.LanguageChange;

                manager.SetObject(key, value); // null: uses DisplayLanguage, which is the same as CurrentUICulture
                LanguageSettings.DisplayLanguage = inv; // save occurs
                manager.ReleaseAllResources();
                Assert.IsNull(manager.GetResourceSet(testCulture, false, false)); // not loaded after release
                Assert.IsNotNull(manager.GetResourceSet(testCulture, true, false)); // but can be loaded from saved

#if NETFRAMEWORK
                // DomainUnload, individual
                Cleanup();
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
                Cleanup();
                manager.ReleaseAllResources();
                evidence = new Evidence(AppDomain.CurrentDomain.Evidence);
                sandboxDomain = AppDomain.CreateDomain("SandboxDomain", evidence, AppDomain.CurrentDomain.BaseDirectory, null, false);
                selfName = Assembly.GetExecutingAssembly().GetName();
                sandboxDomain.Load(selfName);

                remote = (RemoteDrmConsumer)sandboxDomain.CreateInstanceAndUnwrap(selfName.FullName, typeof(RemoteDrmConsumer).FullName);
                remote.UseDrmRemotely(true, testCulture);
                AppDomain.Unload(sandboxDomain);
                Assert.IsNotNull(manager.GetResourceSet(testCulture, true, false)); // can be loaded that has been saved in another domain
#endif

                // Dispose, individual
                Cleanup();
                manager.UseLanguageSettings = false;
                manager.Source = ResourceManagerSources.CompiledAndResX;
                manager.UseLanguageSettings = false;
                manager.AutoSave = AutoSaveOptions.Dispose;

                manager.SetObject(key, value, testCulture);
                manager.Dispose(); // save occurs
                Throws<ObjectDisposedException>(() => manager.GetResourceSet(testCulture, false, false));
                Assert.IsTrue(File.Exists(Path.Combine(Path.Combine(Files.GetExecutingPath(), manager.ResXResourcesDir), $"TestResourceResX.{testCulture.Name}.resx")));

                // Dispose, central
                LanguageSettings.DynamicResourceManagersSource = ResourceManagerSources.CompiledAndResX;
                LanguageSettings.DynamicResourceManagersAutoSave = AutoSaveOptions.Dispose;
                manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
                {
                    UseLanguageSettings = true
                };
                Cleanup();

                manager.SetObject(key, value, testCulture);
                LanguageSettings.DisplayLanguage = inv; // save occurs
                manager.Dispose(); // save occurs
                Throws<ObjectDisposedException>(() => manager.GetResourceSet(testCulture, false, false));
                Assert.IsTrue(File.Exists(Path.Combine(Path.Combine(Files.GetExecutingPath(), manager.ResXResourcesDir), $"TestResourceResX.{testCulture.Name}.resx")));

                // final cleanup
                Cleanup();
            }
            finally
            {
                Cleanup();
            }
        }

#if NETFRAMEWORK
        [Test]
        public void SerializationTest()
        {
            var refManager = new ResourceManager("KGySoft.CoreLibraries.Resources.TestResourceResX", GetType().Assembly);
            var manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
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
#if !NET35 // After deserializing a standard ResourceManager on runtime 2.0 an ObjectDisposedException occurs for GetString
            refManager = refManager.DeepClone();
            Assert.AreEqual(testResRef, refManager.GetString(resName)); 
#endif
            manager = manager.DeepClone();
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
#endif

        [Test]
        public void DisposeTest()
        {
            var manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
            {
                AutoSave = AutoSaveOptions.None
            };
            manager.Dispose();
            Throws<ObjectDisposedException>(() => manager.ReleaseAllResources());
            Throws<ObjectDisposedException>(() => manager.GetString("TestString"));
            manager.Dispose(); // this will not throw anything

            manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName);
            manager.Source = ResourceManagerSources.CompiledOnly;
            manager.Dispose();
            Throws<ObjectDisposedException>(() => manager.ReleaseAllResources());
            Throws<ObjectDisposedException>(() => manager.GetString("TestString"));
            manager.Dispose(); // this will not throw anything
        }

        [Test]
        public void EnsureResourcesGeneratedTest()
        {
            using var manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
            {
                AutoAppend = AutoAppendOptions.AppendFirstNeutralCulture,
                Source = ResourceManagerSources.CompiledAndResX
            };

            // works even if AppendOnLoad was not enabled
            Assert.IsNull(manager.GetResourceSet(hu, true, false));
            manager.EnsureResourcesGenerated(huHU);

            // the merged resource set appears in the neutral culture due to AppendFirstNeutralCulture
            Assert.IsNotNull(manager.GetResourceSet(hu, false, false));
            Assert.IsNull(manager.GetResourceSet(huHU, true, false));

            // adding a new resource to the invariant set
            string key = "new resource";
            manager.SetObject(key, "new invariant resource", inv);

            // without merging the entries explicitly this does not appear in the already created merged resource set
            manager.EnsureResourcesGenerated(huHU);
            IExpandoResourceSet set = manager.GetExpandoResourceSet(hu);
            Assert.IsFalse(set!.ContainsResource(key));

            // but it is merged when explicitly retrieved
            string value = manager.GetString(key, huHU);
            Assert.IsTrue(value!.StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
        }

        [Test]
        public void EnsureInvariantEntriesMergedTest()
        {
            using var manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
            {
                AutoAppend = AutoAppendOptions.AppendFirstNeutralCulture,
                Source = ResourceManagerSources.CompiledAndResX
            };

            // simulating already existing translation for neutral resource
            manager.EnsureResourcesGenerated(huHU);
            Assert.IsNotNull(manager.GetResourceSet(hu, false, false));

            // adding a new resource to the invariant set
            string key = "new resource";
            manager.SetObject(key, "new invariant resource", inv);

            // this merges entries even for already existing resource sets
            manager.EnsureInvariantResourcesMerged(huHU);

            // unlike when just using EnsureResourcesGenerated, now new key is also merged without explicitly getting it from manager
            IExpandoResourceSet set = manager.GetExpandoResourceSet(hu);
            Assert.IsTrue(set!.ContainsResource(key));
            string value = set.GetString(key);
            Assert.IsTrue(value!.StartsWith(LanguageSettings.UntranslatedResourcePrefix, StringComparison.Ordinal));
        }

        [Test]
        public void IgnoreResXParseErrorsTest()
        {
            using var manager = new DynamicResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName)
            {
                Source = ResourceManagerSources.CompiledAndResX,
                IgnoreResXParseErrors = false
            };

            var culture = huHU;
            string path = Path.Combine(Path.Combine(Files.GetExecutingPath(), manager.ResXResourcesDir), $"{resXBaseName}.{culture.Name}.resx");
            manager.GetExpandoResourceSet(culture, ResourceSetRetrieval.CreateIfNotExists);

            try
            {
                // generating a valid but empty resource set
                manager.SaveAllResources(true);
                Assert.IsTrue(File.Exists(path));

                // overwriting it with invalid content
                File.WriteAllText(path, @"invalid");
                manager.ReleaseAllResources();

                // With IgnoreResXParseErrors = false an exception is thrown for the invalid content
                Throws<XmlException>(() => manager.GetString("unknown", culture));

                // But the invalid resource file is ignored if IgnoreResXParseErrors is true
                manager.IgnoreResXParseErrors = true;
                Assert.IsNull(manager.GetString("unknown", culture));
            }
            finally
            {
                File.Delete(path);
            }
        }

        #endregion
    }
}
