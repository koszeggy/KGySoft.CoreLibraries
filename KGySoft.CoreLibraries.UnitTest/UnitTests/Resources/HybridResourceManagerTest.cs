#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: HybridResourceManagerTest.cs
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
using System.Resources;
using System.Text;

using KGySoft.Collections;
using KGySoft.Reflection;
using KGySoft.Resources;

using NUnit.Framework;

using ResXResourceSet = KGySoft.Resources.ResXResourceSet;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Resources
{
    [TestFixture]
    public class HybridResourceManagerTest : TestBase
    {
        #region Constants

        private const string resXBaseName = "TestResourceResX";

        #endregion

        #region Fields

        private static readonly CultureInfo inv = CultureInfo.InvariantCulture;
        private static readonly CultureInfo enUS = CultureInfo.GetCultureInfo("en-US");
        private static readonly CultureInfo en = CultureInfo.GetCultureInfo("en");
        private static readonly CultureInfo enGB = CultureInfo.GetCultureInfo("en-GB");
        private static readonly CultureInfo hu = CultureInfo.GetCultureInfo("hu");
        private static readonly CultureInfo huHU = CultureInfo.GetCultureInfo("hu-HU");

        #endregion

        #region Methods

        #region Public Methods

        [Test]
        public void GetStringTest()
        {
            var manager = new HybridResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName);

            // When a resource exists in both compiled and resx: resx is taken first
            var resName = "TestString";

            manager.Source = ResourceManagerSources.CompiledAndResX;
            var hybrid = manager.GetString(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledOnly;
            var compiled = manager.GetString(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.ResXOnly;
            var resx = manager.GetString(resName, inv);

            Assert.AreEqual(resx, hybrid);
            Assert.AreNotEqual(resx, compiled);
            Assert.AreNotEqual(compiled, hybrid);

            // When a resource exists only in compiled: .resx is null, others hybrid and compiled are the same
            resName = "TestStringCompiled";

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledAndResX;
            hybrid = manager.GetString(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledOnly;
            compiled = manager.GetString(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.ResXOnly;
            resx = manager.GetString(resName, inv);

            Assert.AreEqual(compiled, hybrid);
            Assert.IsNull(resx);

            // When a resource exists only in resx: compiled is null, others hybrid and resx are the same
            resName = "TestStringResX";

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledAndResX;
            hybrid = manager.GetString(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledOnly;
            compiled = manager.GetString(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.ResXOnly;
            resx = manager.GetString(resName, inv);

            Assert.AreEqual(resx, hybrid);
            Assert.IsNull(compiled);

            // Non string throws an exception if not is in safe mode
            resName = "TestBinFile";
            Assert.IsFalse(manager.SafeMode);
            manager.Source = ResourceManagerSources.CompiledOnly;
            Throws<InvalidOperationException>(() => manager.GetString(resName, inv));
            manager.Source = ResourceManagerSources.ResXOnly;
            Throws<InvalidOperationException>(() => manager.GetString(resName, inv));

            // but in safe mode they succeed - the content is different though: ToString vs. raw XML content
            manager.SafeMode = true;
            manager.Source = ResourceManagerSources.CompiledOnly;
            compiled = manager.GetString(resName, inv);
            Assert.AreEqual(manager.GetObject(resName, inv).ToString(), compiled);
            manager.Source = ResourceManagerSources.ResXOnly;
            resx = manager.GetString(resName, inv);
            Assert.AreEqual(manager.GetObject(resName, inv).ToString(), resx);
            Assert.AreNotEqual(compiled, resx);
        }

        [Test]
        public void GetMetaStringTest()
        {
            var manager = new HybridResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName);
            var resName = "TestString";

            manager.Source = ResourceManagerSources.CompiledAndResX;
            var hybrid = manager.GetMetaString(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledOnly;
            var compiled = manager.GetMetaString(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.ResXOnly;
            var resx = manager.GetMetaString(resName, inv);

            // meta exists only in non-compiled resources
            Assert.IsNotNull(hybrid);
            Assert.IsNotNull(resx);
            Assert.IsNull(compiled);

            manager.Source = ResourceManagerSources.CompiledAndResX;
            hybrid = manager.GetMetaString(resName, en);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledOnly;
            compiled = manager.GetMetaString(resName, en);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.ResXOnly;
            resx = manager.GetMetaString(resName, en);

            // there is no fallback for meta
            Assert.IsNull(hybrid);
            Assert.IsNull(resx);
            Assert.IsNull(compiled);
        }

        [Test]
        public void GetObjectTest()
        {
            var manager = new HybridResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName);

            // When a resource exists in both compiled and resx: resx is taken first
            var resName = "TestString";

            manager.Source = ResourceManagerSources.CompiledAndResX;
            var hybrid = manager.GetObject(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledOnly;
            var compiled = manager.GetObject(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.ResXOnly;
            var resx = manager.GetObject(resName, inv);

            Assert.AreEqual(resx, hybrid);
            Assert.AreNotEqual(resx, compiled);
            Assert.AreNotEqual(compiled, hybrid);

            // When a resource exists only in compiled: resx is null, others hybrid and compiled are the same
            resName = "TestStringCompiled";

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledAndResX;
            hybrid = manager.GetObject(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledOnly;
            compiled = manager.GetObject(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.ResXOnly;
            resx = manager.GetObject(resName, inv);

            Assert.AreEqual(compiled, hybrid);
            Assert.IsNull(resx);

            // When a resource exists only in resx: compiled is null, others hybrid and resx are the same
            resName = "TestStringResX";

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledAndResX;
            hybrid = manager.GetObject(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledOnly;
            compiled = manager.GetObject(resName, inv);

            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.ResXOnly;
            resx = manager.GetObject(resName, inv);

            Assert.AreEqual(resx, hybrid);
            Assert.IsNull(compiled);

            // if a resource exists only in a base, it will be returned
            resName = "TestBytes";
            manager.Source = ResourceManagerSources.CompiledAndResX;
            Assert.IsNotNull(manager.GetObject(resName, enUS));

            // switching source will return the correct resource even without releasing the resources
            resName = "TestString";
            hybrid = manager.GetObject(resName, inv);
            manager.Source = ResourceManagerSources.CompiledOnly;
            compiled = manager.GetObject(resName, inv);
            Assert.AreNotEqual(hybrid, compiled);

            // proxy test
            manager.Source = ResourceManagerSources.CompiledAndResX;
            manager.ReleaseAllResources();
            hybrid = manager.GetObject(resName, huHU); // hu and hu-HU are now proxies, last=inv
            Assert.IsNotNull(hybrid);
            Assert.AreSame(hybrid, manager.GetObject(resName, huHU)); // returned from cached lastUsedResourceSet
            Assert.AreSame(hybrid, manager.GetObject(resName, hu)); // returned from cached proxy in resourceSets
            Assert.AreSame(hybrid, manager.GetObject(resName, inv)); // returned from cached non-proxy in resourceSets
        }

        [Test]
        public void GetStreamTest()
        {
            var manager = new HybridResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName);

            // Memory stream can be obtained from both compiled and resx, compiled is an unmanaged memory stream
            var resName = "TestSound";
            manager.Source = ResourceManagerSources.CompiledOnly;
            var compiled = manager.GetStream(resName, inv);
            manager.Source = ResourceManagerSources.ResXOnly;
            var resx = manager.GetStream(resName, inv);
            Assert.IsInstanceOf<MemoryStream>(compiled);
            Assert.IsInstanceOf<MemoryStream>(resx);
            Assert.AreNotEqual(compiled.GetType(), resx.GetType());

            // Works also for byte[], now MemoryStream is returned for both
            resName = "TestBinFile";
            manager.Source = ResourceManagerSources.CompiledOnly;
            compiled = manager.GetStream(resName, inv);
            manager.Source = ResourceManagerSources.ResXOnly;
            resx = manager.GetStream(resName, inv);
            Assert.IsInstanceOf<MemoryStream>(compiled);
            Assert.IsInstanceOf<MemoryStream>(resx);
            Assert.AreEqual(compiled.GetType(), resx.GetType());

            // For a string exception is thrown when SafeMode = false
            resName = "TestString";
            Assert.IsFalse(manager.SafeMode);
            manager.Source = ResourceManagerSources.CompiledOnly;
            Assert.Throws<InvalidOperationException>(() => manager.GetStream(resName, inv));
            manager.Source = ResourceManagerSources.ResXOnly;
            Throws<InvalidOperationException>(() => manager.GetStream(resName, inv), Res.ResourcesNonStreamResourceWithType(resName, Reflector.StringType));

            // but when SafeMode is true, a string stream is returned
            manager.SafeMode = true;
            manager.Source = ResourceManagerSources.CompiledOnly;
            compiled = manager.GetStream(resName, inv);
            Assert.IsInstanceOf<MemoryStream>(compiled);
            Assert.AreEqual(manager.GetString(resName, inv), new StreamReader(compiled, Encoding.Unicode).ReadToEnd());
            manager.Source = ResourceManagerSources.ResXOnly;
            resx = manager.GetStream(resName, inv);
            Assert.IsInstanceOf<MemoryStream>(resx);
            Assert.AreEqual(manager.GetString(resName, inv), new StreamReader(resx, Encoding.Unicode).ReadToEnd());

#if !(NETCOREAPP2_0 || NETCOREAPP2_1) // System.NotSupportedException : Cannot read resources that depend on serialization.
            // even for non-string resources
            resName = "TestImage";
            manager.Source = ResourceManagerSources.CompiledOnly;
            compiled = manager.GetStream(resName, inv);
            Assert.IsInstanceOf<MemoryStream>(compiled);
            Assert.AreEqual(manager.GetString(resName, inv), new StreamReader(compiled, Encoding.Unicode).ReadToEnd());
            manager.Source = ResourceManagerSources.ResXOnly;
            resx = manager.GetStream(resName, inv);
            Assert.IsInstanceOf<MemoryStream>(resx);
            Assert.AreEqual(manager.GetString(resName, inv), new StreamReader(resx, Encoding.Unicode).ReadToEnd()); 
#endif
        }

        [Test]
        public void GetResourceSetTest()
        {
            var manager = new HybridResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName);

            // checking that invariant exists in all strategies and it has the correct type
            manager.Source = ResourceManagerSources.ResXOnly;
            Assert.AreEqual("ResXResourceSet", manager.GetResourceSet(inv, loadIfExists: true, tryParents: false).GetType().Name);
            manager.Source = ResourceManagerSources.CompiledOnly;
            Assert.AreEqual("RuntimeResourceSet", manager.GetResourceSet(inv, loadIfExists: true, tryParents: false).GetType().Name);
            manager.Source = ResourceManagerSources.CompiledAndResX;
            var rsInv = manager.GetResourceSet(inv, loadIfExists: true, tryParents: false);
            Assert.AreEqual("HybridResourceSet", rsInv.GetType().Name);

            // enUS should not return invariant when [assembly: NeutralResourcesLanguage("en-US")] is not set
            Assert.AreNotSame(rsInv, manager.GetResourceSet(enUS, true, false));

            // and en != inv
            Assert.AreNotSame(rsInv, manager.GetResourceSet(en, true, false));

            // hu does not exist
            Assert.IsNull(manager.GetResourceSet(hu, loadIfExists: true, tryParents: false));

            // but returns inv when parents are required
            Assert.AreSame(rsInv, manager.GetResourceSet(hu, loadIfExists: true, tryParents: true));

            // when already obtained, the already obtained sets are returned for createIfNotExists = false
            Assert.IsNotNull(manager.GetResourceSet(inv, loadIfExists: false, tryParents: false));

            // when not obtained, the tryParents=false will simply return null
            manager.ReleaseAllResources();
            Assert.IsNull(manager.GetResourceSet(inv, loadIfExists: false, tryParents: false));

            // when not obtained but exists, the tryParents=true will also return null if createIfNotExists=false
            Assert.IsNull(manager.GetResourceSet(inv, loadIfExists: false, tryParents: true));

            // but for for non-existing name even this will throw an exception
            manager = new HybridResourceManager("NonExisting", typeof(object).Assembly); // typeof(object): mscorlib has en-US invariant resources language
            Throws<MissingManifestResourceException>(() => manager.GetResourceSet(inv, loadIfExists: false, tryParents: true));

            // createIfNotExists = true will throw an exception as well
            Throws<MissingManifestResourceException>(() => manager.GetResourceSet(inv, loadIfExists: true, tryParents: true));

            // except if tryParents=false, because in this case null will be returned
            Assert.IsNull(manager.GetResourceSet(inv, loadIfExists: true, tryParents: false));

            // loading a resource set where there are more compiled ones and just invariant resx
            manager = new HybridResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, "TestRes");
            Assert.AreEqual("HybridResourceSet", manager.GetResourceSet(inv, true, false).GetType().Name);

            // enUS exists only in compiled
            var rsEnUS = manager.GetResourceSet(enUS, true, false);
#if NET35
            if (rsEnUS == null)
                Assert.Inconclusive(".NET Runtime 2.x issue: satellite assembly is not loaded");
#endif
            Assert.IsNotNull(rsEnUS);

            Assert.AreEqual("RuntimeResourceSet", rsEnUS.GetType().Name);

            // but an expando RS can be forced for it
            Assert.AreEqual("HybridResourceSet", manager.GetExpandoResourceSet(enUS, ResourceSetRetrieval.LoadIfExists, false).GetType().Name);

            // except if source is compiled only
            manager.Source = ResourceManagerSources.CompiledOnly;
            Assert.IsNull(manager.GetExpandoResourceSet(enUS, ResourceSetRetrieval.LoadIfExists, false));

            // in system ResourceManager if a derived culture is required but only a parent is available, then this parent will be
            // cached for derived cultures, too, so groveling is needed only once. Hybrid caches cultures, too, but uses
            // proxies to remark when a cached resource must be replaced.

            // System: requiring hu loads inv and caches this for hu, too
            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledOnly;
            Assert.IsNotNull(rsInv = manager.GetResourceSet(hu, loadIfExists: true, tryParents: true));
            Assert.AreSame(rsInv, manager.GetResourceSet(inv, loadIfExists: false, tryParents: false));
            Assert.AreSame(rsInv, manager.GetResourceSet(hu, loadIfExists: false, tryParents: false));

            // ResX: requiring hu loads inv and caches a proxy for hu, too, and outside this is transparent so proxy returns inv, too
            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.ResXOnly;
            Assert.IsNotNull(rsInv = manager.GetResourceSet(hu, loadIfExists: true, tryParents: true));
            Assert.AreSame(rsInv, manager.GetResourceSet(inv, loadIfExists: false, tryParents: false));
            Assert.AreSame(rsInv, manager.GetResourceSet(hu, loadIfExists: false, tryParents: false));

            // Hybrid: requiring hu loads inv and caches a proxy for hu, too, and outside this is transparent so proxy returns inv, too
            manager.ReleaseAllResources();
            manager.Source = ResourceManagerSources.CompiledAndResX;
            Assert.IsNotNull(rsInv = manager.GetResourceSet(hu, loadIfExists: true, tryParents: true));
            Assert.AreSame(rsInv, manager.GetResourceSet(inv, loadIfExists: false, tryParents: false));
            Assert.AreSame(rsInv, manager.GetResourceSet(hu, loadIfExists: false, tryParents: false));

            // now if we change something in hu, the cached inv proxy will be replaced
            manager.SetObject("test", 42, hu);
            ResourceSet rsHU;
            Assert.IsNotNull(rsHU = manager.GetResourceSet(hu, loadIfExists: false, tryParents: false));
            Assert.AreNotSame(rsInv, rsHU);

            // though en exist, we haven't load it yet, so if we don't load it, it will return a proxy for inv, too
            Assert.IsNotNull(rsInv = manager.GetResourceSet(inv, loadIfExists: false, tryParents: false));
            Assert.IsNull(manager.GetResourceSet(enUS, loadIfExists: false, tryParents: false));
            Assert.AreSame(rsInv, manager.GetResourceSet(enUS, loadIfExists: false, tryParents: true));
            Assert.AreSame(rsInv, manager.GetResourceSet(enUS, loadIfExists: false, tryParents: false));

            // but this proxy is replaced when loading the existing file is really requested (this is a difference to system version)
            Assert.AreNotSame(rsInv, manager.GetResourceSet(enUS, loadIfExists: true, tryParents: false));

            // creating inv, inv(en), inv(enGB) (these have unloaded parent); inv(hu), inv(huHU) (these have no unloaded parents)
            manager.ReleaseAllResources();
            Assert.IsNotNull(rsInv = manager.GetResourceSet(inv, loadIfExists: true, tryParents: false));
            Assert.AreSame(rsInv, manager.GetResourceSet(enGB, loadIfExists: false, tryParents: true));
            Assert.AreSame(rsInv, manager.GetResourceSet(huHU, loadIfExists: false, tryParents: true));

            // now if we re-access enGB with load, it returns en, but huHU still returns inv
            Assert.AreNotSame(rsInv, manager.GetResourceSet(enGB, loadIfExists: true, tryParents: true));
            Assert.AreSame(rsInv, manager.GetResourceSet(huHU, loadIfExists: true, tryParents: true));

            // creating inv, inv(en), inv(enGB) (these have unloaded parent); inv(hu), inv(huHU) (these have no unloaded parents)
            manager.ReleaseAllResources();
            rsInv = manager.GetResourceSet(inv, loadIfExists: true, tryParents: false);
            Assert.AreSame(rsInv, manager.GetResourceSet(enGB, loadIfExists: false, tryParents: true));
            Assert.AreSame(rsInv, manager.GetResourceSet(huHU, loadIfExists: false, tryParents: true));

            // now the hu branch is up-to-date but en-GB has unloaded parents because en actually exists but not loaded
            var resourceSets = (StringKeyedDictionary<ResourceSet>)Reflector.GetField(manager, "resourceSets");
            int sets = resourceSets.Count;

            // "loading" hu does not change anything, since it is up-to date
            Assert.AreSame(rsInv, manager.GetResourceSet(hu, loadIfExists: true, tryParents: false));
            Assert.AreEqual(sets, resourceSets.Count);

            // but loading en clears en-GB, since it depends on that. Re-accessing enGB returns now en
            ResourceSet rsEN;
            Assert.AreNotSame(rsInv, rsEN = manager.GetResourceSet(en, loadIfExists: true, tryParents: false));
            Assert.AreEqual(sets - 1, resourceSets.Count);
            Assert.AreSame(rsEN, manager.GetResourceSet(enGB, loadIfExists: false, tryParents: true));
            Assert.AreEqual(sets, resourceSets.Count);

            // similarly, creating hu clears hu-HU, and re-accessing hu-HU returns hu
            Assert.AreNotSame(rsInv, rsHU = (ResourceSet)manager.GetExpandoResourceSet(hu, ResourceSetRetrieval.CreateIfNotExists, tryParents: false));
            Assert.AreEqual(sets - 1, resourceSets.Count);
            Assert.AreSame(rsHU, manager.GetResourceSet(huHU, loadIfExists: true, tryParents: true));
            Assert.AreEqual(sets, resourceSets.Count);

            // creating inv, inv(en) (unloaded resource); inv(hu), (no unloaded resource)
            manager.ReleaseAllResources();
            rsInv = manager.GetResourceSet(inv, loadIfExists: true, tryParents: false);
            Assert.AreSame(rsInv, manager.GetResourceSet(en, loadIfExists: false, tryParents: true));
            Assert.AreSame(rsInv, manager.GetResourceSet(hu, loadIfExists: true, tryParents: true));
            resourceSets = (StringKeyedDictionary<ResourceSet>)Reflector.GetField(manager, "resourceSets");
            sets = resourceSets.Count;

            // accessing en-GB will replace en proxy and returns that for en-GB
            var rsENGB = manager.GetResourceSet(enGB, loadIfExists: true, tryParents: true);
            Assert.AreEqual(sets + 1, resourceSets.Count);
            rsEN = manager.GetResourceSet(en, loadIfExists: false, tryParents: false);
            Assert.AreSame(rsEN, rsENGB);
            Assert.AreNotSame(inv, rsENGB);
            sets = resourceSets.Count;

            // but accessing hu-HU just returns the proxy of hu (=inv) and creates a new proxy for hu-HU
            var rsHUHU = manager.GetResourceSet(huHU, loadIfExists: true, tryParents: true);
            Assert.AreEqual(sets + 1, resourceSets.Count);
            rsHU = manager.GetResourceSet(hu, loadIfExists: false, tryParents: false);
            Assert.AreSame(rsHU, rsHUHU);
            Assert.AreSame(rsInv, rsHU);
        }

        [Test]
        public void SetObjectTest()
        {
            LanguageSettings.DisplayLanguage = enUS;
            var manager = new HybridResourceManager(GetType());

            // not existing base: an exception is thrown when an object is about to obtain
            Throws<MissingManifestResourceException>(() => manager.GetObject("unknown"));

            // setting something in display language creates a resource set but the invariant is still missing
            manager.SetObject("StringValue", "String " + LanguageSettings.DisplayLanguage.Name);
            Assert.IsNotNull(manager.GetObject("StringValue"));
            Throws<MissingManifestResourceException>(() => manager.GetObject("unknown"));

            // this creates the invariant resource set, no exception anymore for unknown values
            manager.SetObject("InvariantOnly", 42, inv);
            Assert.IsNull(manager.GetObject("unknown"));

            // accessing something via a derived culture we can obtain the invariant value after all
            Assert.IsNotNull(manager.GetObject("InvariantOnly"));

            // setting something both in derived and invariant: they both can be obtained and they can be different
            manager.SetObject("StringValue", "String invariant", inv);
            Assert.IsNotNull(manager.GetObject("StringValue", inv));
            Assert.AreNotEqual(manager.GetObject("StringValue", inv), manager.GetObject("StringValue"));
            Assert.IsTrue(manager.IsModified);

            // in compiled mode any set operation throws InvalidOperationException and the changes disappear
            manager.Source = ResourceManagerSources.CompiledOnly;
            Throws<InvalidOperationException>(() => manager.SetObject("SetTest", "does not work"));
            Assert.IsFalse(manager.IsModified);
            Throws<MissingManifestResourceException>(() => manager.GetString("StringValue"));

            // is we change to non-compiled mode, changes re-appear
            manager.Source = ResourceManagerSources.ResXOnly;
            Assert.IsTrue(manager.IsModified);
            Assert.IsNotNull(manager.GetString("StringValue"));
        }

        [Test]
        public void SetMetaTest()
        {
            var manager = new HybridResourceManager(GetType());

            // not existing base: missing manifest exception
            Throws<MissingManifestResourceException>(() => manager.GetMetaObject("unknown"));

            // setting something without culture sets the invariant language so there is no exception anymore
            manager.SetMetaObject("StringValue", "String invariant");
            Assert.IsNotNull(manager.GetMetaObject("StringValue"));
            Assert.IsNull(manager.GetMetaObject("unknown"));

            // this creates a derived en resource set
            manager.SetMetaObject("enOnly", 42, en);

            // however, there is no resource fallback for metadata
            Assert.IsNull(manager.GetMetaObject("StringValue", en));

            // in compiled mode any set operation throws InvalidOperationException and the changes disappear
            manager.Source = ResourceManagerSources.CompiledOnly;
            Throws<InvalidOperationException>(() => manager.SetMetaObject("SetTest", "does not work"));
            Assert.IsFalse(manager.IsModified);
            Assert.IsNull(manager.GetMetaString("StringValue"));

            // is we change to non-compiled mode, changes re-appear
            manager.Source = ResourceManagerSources.ResXOnly;
            Assert.IsTrue(manager.IsModified);
            Assert.IsNotNull(manager.GetMetaString("StringValue"));
        }

        [Test]
        public void SetNullAndRemoveTest()
        {
            var manager = new HybridResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName);
            var resName = "TestString";
            var resEnUs = manager.GetObject(resName, enUS);

            // enUS has been loaded only so the result came from this rs
            var rsEnUs = manager.GetResourceSet(enUS, false, false);
            Assert.IsNotNull(rsEnUs);
            Assert.IsNull(manager.GetResourceSet(en, false, false));
            Assert.IsNull(manager.GetResourceSet(inv, false, false));

#if NET35
            if (rsEnUs is ResXResourceSet)
                Assert.Inconclusive(".NET Runtime 2.x issue: satellite assembly is not loaded");
#endif

            // enUS is hybrid
            Assert.IsInstanceOf<HybridResourceSet>(rsEnUs);

            // if we nullify the resource, it will hide the compiled one and getting the enUS returns the base value from en
            manager.SetObject(resName, null, enUS);
            var resEn = manager.GetObject(resName, en);
            Assert.AreEqual(resEn, manager.GetObject(resName, enUS));
            Assert.AreNotEqual(resEn, resEnUs);
            Assert.IsNull(rsEnUs.GetObject(resName));

            // but if we remove the resource, the compiled one will be visible
            manager.ReleaseAllResources();
            manager.RemoveObject(resName, enUS);
            var resEnUsCompiled = manager.GetObject(resName, enUS);
            Assert.AreNotEqual(resEnUs, resEnUsCompiled);

            // it came from the enUS, too: after releasing all, only this has been loaded
            rsEnUs = manager.GetResourceSet(enUS, false, false);
            Assert.IsNotNull(rsEnUs);
            Assert.IsNull(manager.GetResourceSet(en, false, false));
            Assert.IsNull(manager.GetResourceSet(inv, false, false));
            Assert.IsNotNull(rsEnUs.GetObject(resName));
        }

        [Test]
        public void EnumeratorTest()
        {
            var manager = new HybridResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName);
            var resName = "TestString";

            manager.Source = ResourceManagerSources.CompiledOnly;
            string resCompiled = manager.GetString(resName, inv);
            var enumCompiled = manager.GetResourceSet(inv, true, false).GetEnumerator();

            manager.Source = ResourceManagerSources.ResXOnly;
            string resResx = manager.GetString(resName, inv);
            var enumResx = manager.GetResourceSet(inv, true, false).GetEnumerator();

            manager.Source = ResourceManagerSources.CompiledAndResX;
            string resHybrid = manager.GetString(resName, inv);
            var enumHybrid = manager.GetResourceSet(inv, true, false).GetEnumerator();

            // the hybrid enumerator filters the duplicates
            string[] keysCompiled = enumCompiled.GetKeysEnumerator().ToArray();
            string[] keysResx = enumResx.GetKeysEnumerator().ToArray();
            string[] keysHybrid = enumHybrid.GetKeysEnumerator().ToArray();
            Assert.IsTrue(keysCompiled.Length + keysResx.Length > keysHybrid.Length);
            Assert.AreEqual(keysHybrid.Length, keysCompiled.Union(keysResx).Count());
            Assert.AreEqual(keysHybrid.Length, keysHybrid.Distinct().Count());

            // the duplicated values are returned from the resx
            Assert.AreEqual(resResx, resHybrid);
            Assert.AreNotEqual(resCompiled, resHybrid);

            // reset works properly
            enumHybrid.Reset();
            Assert.IsTrue(keysHybrid.SequenceEqual(enumHybrid.GetKeysEnumerator()));

            // during the enumeration an exception occurs in any state of the enumeration
            // 1. during the resx enumeration
            enumHybrid.Reset();
            enumHybrid.MoveNext();
            Assert.IsTrue(keysResx.Contains(enumHybrid.Key.ToString()));
            manager.SetObject("new", 42, inv);
            Throws<InvalidOperationException>(() => enumHybrid.MoveNext());

            // 2. during the compiled enumeration
            enumHybrid = manager.GetResourceSet(inv, true, false).GetEnumerator();
            string compiledOnlyKey = keysCompiled.Except(keysResx).First();
            do
            {
                enumHybrid.MoveNext();
            } while (enumHybrid.Key.ToString() != compiledOnlyKey);
            manager.SetObject("new", -42, inv);
            Throws<InvalidOperationException>(() => enumHybrid.MoveNext());
        }

        [Test]
        public void SaveTest()
        {
            var manager = new HybridResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName);

            // empty manager: save all is false even if forcing
            Assert.IsFalse(manager.IsModified);
            Assert.IsFalse(manager.SaveAllResources(true));

            // non-empty but unmodified manager: saving on forcing
            manager.GetResourceSet(inv, true, false);
            Assert.IsFalse(manager.IsModified);
            Assert.IsFalse(manager.SaveAllResources(false));
            //Assert.IsTrue(manager.SaveAllResources(true)); // - was OK in MSTest as it supports deployment
            manager.ReleaseAllResources();

            // adding a new value to a non-existing resource
            // it will be dirty and can be saved without forcing, then it is not dirty any more
            manager.SetObject("new value en-GB", 42, enGB);
            Assert.IsTrue(manager.IsModified);
            Assert.IsTrue(manager.SaveAllResources(false));
            Assert.IsFalse(manager.IsModified);

            // adding a new value: it will be dirty and saves without forcing, then it is not dirty any more
            manager.SetObject("new value", 42, enUS);
            Assert.IsTrue(manager.IsModified);
            Assert.IsNotNull(manager.GetResourceSet(enUS, false, false));
            Assert.IsFalse(manager.SaveResourceSet(inv));
            //Assert.IsTrue(manager.SaveResourceSet(enUS)); // - was OK in MSTest as it supports deployment
            manager.GetExpandoResourceSet(enUS, ResourceSetRetrieval.GetIfAlreadyLoaded).Save(new MemoryStream()); // in NUnit saving into memory so output folder will not change
            Assert.IsFalse(manager.IsModified);

            // in compiled only mode save returns always false
            manager.SetObject("new value inv", -42, inv);
            Assert.IsTrue(manager.IsModified);
            manager.Source = ResourceManagerSources.CompiledOnly;
            Assert.IsFalse(manager.IsModified);
            Assert.IsFalse(manager.SaveResourceSet(inv));
            Assert.IsFalse(manager.SaveAllResources(true));
            manager.Source = ResourceManagerSources.ResXOnly;
            Assert.IsTrue(manager.IsModified);
            //Assert.IsTrue(manager.SaveResourceSet(inv)); // - was OK in MSTest as it supports deployment
            manager.GetExpandoResourceSet(inv, ResourceSetRetrieval.GetIfAlreadyLoaded).Save(new MemoryStream()); // in NUnit saving into memory so output folder will not change
            Assert.IsFalse(manager.IsModified);

            // removing added new files
            Clean(manager, enGB);
        }

#if NETFRAMEWORK
        [Test]
        public void SerializationTest()
        {
            var refManager = new ResourceManager("KGySoft.CoreLibraries.Resources.TestResourceResX", GetType().Assembly);
            var manager = new HybridResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName);
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
            var manager = new HybridResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName);
            manager.Dispose();
            Throws<ObjectDisposedException>(() => manager.ReleaseAllResources());
            Throws<ObjectDisposedException>(() => manager.GetString("TestString"));
            manager.Dispose(); // this will not throw anything

            manager = new HybridResourceManager("KGySoft.CoreLibraries.Resources.TestCompiledResource", GetType().Assembly, resXBaseName);
            manager.Source = ResourceManagerSources.CompiledOnly;
            manager.Dispose();
            Throws<ObjectDisposedException>(() => manager.ReleaseAllResources());
            Throws<ObjectDisposedException>(() => manager.GetString("TestString"));
            manager.Dispose(); // this will not throw anything
        }

        #endregion

        #region Private Methods

        private static void Clean(HybridResourceManager manager, CultureInfo culture)
            => File.Delete(Path.Combine(Path.Combine(Files.GetExecutingPath(), manager.ResXResourcesDir), $"{resXBaseName}.{culture.Name}.resx"));

        #endregion

        #endregion
    }
}
