using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Windows.Forms;
using KGySoft;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Resources;
using KGySoft.Serialization;
using NUnit.Framework;

namespace _LibrariesTest.Tests.Resources
{
    [TestFixture]
    //[DeploymentItem("Resources", "Resources")]
    //[DeploymentItem("en", "en")]
    //[DeploymentItem("en-US", "en-US")]
    public class ResXResourceManagerTest: TestBase
    {
        private static CultureInfo inv = CultureInfo.InvariantCulture;

        private static CultureInfo enUS = CultureInfo.GetCultureInfo("en-US");
        private static CultureInfo en = CultureInfo.GetCultureInfo("en");
        private static CultureInfo enGB = CultureInfo.GetCultureInfo("en-GB");

        private static CultureInfo hu = CultureInfo.GetCultureInfo("hu");
        private static CultureInfo huHU = CultureInfo.GetCultureInfo("hu-HU");

        [Test]
        public void GetString()
        {
            var refManager = CreateResourceManager("_LibrariesTest.Resources.TestResourceResX", enUS);
            var manager = new ResXResourceManager("TestResourceResX", typeof(object).Assembly); // typeof(object): mscorlib has en-US invariant resources language
            var resName = "TestString";
            Assert.AreEqual(refManager.GetString(resName, inv), manager.GetString(resName, inv));

            // if assembly has en-US invariant culture, then requiring en-US should return the invariant resource
            Assert.AreEqual(refManager.GetString(resName, inv), refManager.GetString(resName, enUS));
            Assert.AreEqual(refManager.GetString(resName, inv), manager.GetString(resName, enUS));

            // but en is different from invariant
            Assert.AreEqual(refManager.GetString(resName, en), manager.GetString(resName, en));
            Assert.AreNotEqual(refManager.GetString(resName, en), refManager.GetString(resName, enUS));
            Assert.AreNotEqual(manager.GetString(resName, en), manager.GetString(resName, enUS));

            refManager = new ResourceManager("_LibrariesTest.Resources.TestResourceResX", GetType().Assembly); // without patch
            manager = new ResXResourceManager("TestResourceResX", GetType().Assembly); // this assembly has no invariant resources language set
            Assert.AreEqual(refManager.GetString(resName, inv), manager.GetString(resName, inv));

            // if assembly has no specified invariant culture, then requiring en-US should return the en-US resource
            Assert.AreNotEqual(refManager.GetString(resName, inv), refManager.GetString(resName, enUS));
            Assert.AreNotEqual(refManager.GetString(resName, inv), manager.GetString(resName, enUS));

            // but en-US is different from invariant
            Assert.AreEqual(refManager.GetString(resName, enUS), manager.GetString(resName, enUS));
            Assert.AreNotEqual(refManager.GetString(resName, inv), refManager.GetString(resName, enUS));
            Assert.AreNotEqual(manager.GetString(resName, inv), manager.GetString(resName, enUS));

            // and from en as well
            Assert.AreEqual(refManager.GetString(resName, en), manager.GetString(resName, en));
            Assert.AreNotEqual(refManager.GetString(resName, en), refManager.GetString(resName, enUS));
            Assert.AreNotEqual(manager.GetString(resName, en), manager.GetString(resName, enUS));
        }

        [Test]
        public void GetMetaString()
        {
            var manager = new ResXResourceManager("TestResourceResX", typeof(object).Assembly); // typeof(object): mscorlib has en-US invariant resources language
            var resName = "TestString";

            Assert.IsNotNull(manager.GetMetaString(resName, inv));

            // culture=null will use the invariant culture
            Assert.IsNotNull(manager.GetMetaString(resName));

            // if assembly has en-US invariant culture, then requiring en-US should return the invariant resource
            Assert.IsNotNull(manager.GetMetaString(resName, enUS));

            // en is different from invariant, and since there is no fallback for meta, is will not be found
            Assert.IsNull(manager.GetMetaString(resName, en));

            manager = new ResXResourceManager("TestResourceResX", GetType().Assembly); // this assembly has no invariant resources language set

            // en-US is not found if it is not the neutral culture
            Assert.IsNull(manager.GetMetaString(resName, enUS));
        }

        [Test]
        public void GetObject()
        {
            var refManager = CreateResourceManager("_LibrariesTest.Resources.TestResourceResX", enUS);
            var manager = new ResXResourceManager("TestResourceResX", typeof(object).Assembly); // typeof(object): mscorlib has en-US invariant resources language
            var resName = "TestString";
            Assert.AreEqual(refManager.GetObject(resName, inv), manager.GetObject(resName, inv));

            // if assembly has en-US invariant culture, then requiring en-US should return the invariant resource
            Assert.AreEqual(refManager.GetObject(resName, inv), refManager.GetObject(resName, enUS));
            Assert.AreEqual(refManager.GetObject(resName, inv), manager.GetObject(resName, enUS));

            // but en is different from invariant
            Assert.AreEqual(refManager.GetObject(resName, en), manager.GetObject(resName, en));
            Assert.AreNotEqual(refManager.GetObject(resName, en), refManager.GetObject(resName, enUS));
            Assert.AreNotEqual(manager.GetObject(resName, en), manager.GetObject(resName, enUS));

            // TestBytes is defined in invariant only, so en-US returns it if it is the invariant language
            resName = "TestBytes";
            Assert.IsNotNull(refManager.GetResourceSet(inv, true, false).GetObject(resName));
            Assert.IsNull(refManager.GetResourceSet(en, true, false).GetObject(resName));
            Assert.IsNotNull(refManager.GetResourceSet(enUS, true, false).GetObject(resName));
            Assert.IsNotNull(manager.GetResourceSet(inv, true, false).GetObject(resName));
            Assert.IsNull(manager.GetResourceSet(en, true, false).GetObject(resName));
            Assert.IsNotNull(manager.GetResourceSet(enUS, true, false).GetObject(resName));

            // TestBytes are returned by any language
            Assert.IsNotNull(refManager.GetObject(resName, enUS));
            Assert.IsNotNull(refManager.GetObject(resName, en));
            Assert.IsNotNull(refManager.GetObject(resName, inv));
            Assert.IsNotNull(manager.GetObject(resName, enUS));
            Assert.IsNotNull(manager.GetObject(resName, en));
            Assert.IsNotNull(manager.GetObject(resName, inv));

            resName = "TestString";
            refManager = new ResourceManager("_LibrariesTest.Resources.TestResourceResX", GetType().Assembly); // without patch
            manager = new ResXResourceManager("TestResourceResX", GetType().Assembly); // this assembly has no invariant resources language set
            Assert.AreEqual(refManager.GetObject(resName, inv), manager.GetObject(resName, inv));

            // if assembly has no specified invariant culture, then requiring en-US should return the en-US resource
            Assert.AreNotEqual(refManager.GetObject(resName, inv), refManager.GetObject(resName, enUS));
            Assert.AreNotEqual(refManager.GetObject(resName, inv), manager.GetObject(resName, enUS));

            // but en-US is different from invariant
            Assert.AreEqual(refManager.GetObject(resName, enUS), manager.GetObject(resName, enUS));
            Assert.AreNotEqual(refManager.GetObject(resName, inv), refManager.GetObject(resName, enUS));
            Assert.AreNotEqual(manager.GetObject(resName, inv), manager.GetObject(resName, enUS));

            // and from en as well
            Assert.AreEqual(refManager.GetObject(resName, en), manager.GetObject(resName, en));
            Assert.AreNotEqual(refManager.GetObject(resName, en), refManager.GetObject(resName, enUS));
            Assert.AreNotEqual(manager.GetObject(resName, en), manager.GetObject(resName, enUS));
        
            // TestBytes is defined in invariant only
            resName = "TestBytes";
            Assert.IsNotNull(refManager.GetResourceSet(inv, true, false).GetObject(resName));
            Assert.IsNull(refManager.GetResourceSet(en, true, false).GetObject(resName));
            Assert.IsNull(refManager.GetResourceSet(enUS, true, false).GetObject(resName));
            Assert.IsNotNull(manager.GetResourceSet(inv, true, false).GetObject(resName));
            Assert.IsNull(manager.GetResourceSet(en, true, false).GetObject(resName));
            Assert.IsNull(manager.GetResourceSet(enUS, true, false).GetObject(resName));

            // TestBytes are returned by any language
            Assert.IsNotNull(refManager.GetObject(resName, enUS));
            Assert.IsNotNull(refManager.GetObject(resName, en));
            Assert.IsNotNull(refManager.GetObject(resName, inv));
            Assert.IsNotNull(manager.GetObject(resName, enUS));
            Assert.IsNotNull(manager.GetObject(resName, en));
            Assert.IsNotNull(manager.GetObject(resName, inv));
        }

        [Test]
        public void SetObjectTest()
        {
            LanguageSettings.DisplayLanguage = enUS;
            var manager = new ResXResourceManager("UnknownBaseName");

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

            // releasing everything re-enables the exception
            manager.ReleaseAllResources();
            Throws<MissingManifestResourceException>(() => manager.GetObject("unknown"));
        }

        [Test]
        public void SetMetaTest()
        {
            var manager = new ResXResourceManager("UnknownBaseName");

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
        }

        [Test]
        public void SetNullAndRemoveTest()
        {
            var manager = new ResXResourceManager("TestResourceResX");
            var resName = "TestString";
            var resEnUs = manager.GetObject(resName, enUS);

            // enUS has been loaded only so the result came from this rs
            var rsEnUs = manager.GetResourceSet(enUS, false, false);
            Assert.IsNotNull(rsEnUs);
            Assert.IsNull(manager.GetResourceSet(en, false, false));
            Assert.IsNull(manager.GetResourceSet(inv, false, false));

            // if we nullify the resource, it will hide the enUS returns the base value from en
            manager.SetObject(resName, null, enUS);
            var resEn = manager.GetObject(resName, en);
            Assert.AreEqual(resEn, manager.GetObject(resName, enUS));
            Assert.AreNotEqual(resEn, resEnUs);
            Assert.IsNull(rsEnUs.GetObject(resName));

            // though the null value explicitly exists
            Assert.IsTrue(manager.GetResourceSet(enUS, false, false).Cast<DictionaryEntry>().Any(e => e.Key.ToString() == resName));

            // but if we remove the resource, it will disappear
            manager.RemoveObject(resName, enUS);
            Assert.IsFalse(manager.GetResourceSet(enUS, false, false).Cast<DictionaryEntry>().Any(e => e.Key.ToString() == resName));
        }

        /// <summary>
        /// This method indirectly tests that ResX reader supports the different types of encodings/object links/etc.
        /// </summary>
        [Test]
        public void FormatsTest()
        {
            var refManager = new ResourceManager("_LibrariesTest.Resources.TestResourceResX", GetType().Assembly);
            var manager = new ResXResourceManager("TestResourceResX", GetType().Assembly);
            
            // string
            Assert.AreEqual(refManager.GetString("TestString"), manager.GetString("TestString"));

            // text file by reference
            Assert.AreEqual(refManager.GetString("TestTextFile"), manager.GetString("TestTextFile"));

            // icon by reference
            var reference = refManager.GetObject("TestIcon");
            var check = manager.GetObject("TestIcon");
            Assert.IsInstanceOf<Icon>(reference);
            AssertItemsEqual(BinarySerializer.Serialize(reference), BinarySerializer.Serialize(check));

            // icon bmp by reference: system manager retrieves it as a png, while resx manager preserves its icon raw format
            reference = refManager.GetObject("TestIconBitmap");
            check = manager.GetObject("TestIconBitmap");
            Assert.IsInstanceOf<Bitmap>(reference);
            Assert.IsInstanceOf<Bitmap>(check);
            Assert.AreEqual(ImageFormat.Png.Guid, ((Bitmap)reference).RawFormat.Guid);
            Assert.AreEqual(ImageFormat.Icon.Guid, ((Bitmap)check).RawFormat.Guid);
            AssertDeepEquals((Bitmap)reference, (Bitmap)check);

            // multi-res icon by reference
            reference = refManager.GetObject("TestIconMulti");
            check = manager.GetObject("TestIconMulti");
            Assert.IsInstanceOf<Icon>(reference);
            AssertItemsEqual(BinarySerializer.Serialize(reference), BinarySerializer.Serialize(check));

            // multi-res icon bmp by reference
            reference = refManager.GetObject("TestIconMultiBitmap"); // single 32*32 png
            check = manager.GetObject("TestIconMultiBitmap"); // icon of 5 images
            Assert.IsInstanceOf<Bitmap>(reference);
            Assert.IsInstanceOf<Bitmap>(check);
            Assert.AreEqual(ImageFormat.Png.Guid, ((Bitmap)reference).RawFormat.Guid);
            Assert.AreEqual(ImageFormat.Icon.Guid, ((Bitmap)check).RawFormat.Guid);

            // byte array by reference
            reference = refManager.GetObject("TestBinFile");
            check = manager.GetObject("TestBinFile");
            Assert.IsInstanceOf<byte[]>(reference);
            AssertDeepEquals(reference, check);

            // stream by reference
            reference = refManager.GetObject("TestSound");
            check = manager.GetObject("TestSound");
            Assert.IsInstanceOf<MemoryStream>(reference);
            AssertItemsEqual(((MemoryStream)reference).ToArray(), ((MemoryStream)check).ToArray());

            // point embedded by type converter
            reference = refManager.GetObject("TestPoint");
            check = manager.GetObject("TestPoint");
            Assert.IsInstanceOf<Point>(reference);
            Assert.AreEqual(reference, check);

            // bmp embedded as bytearray.base64 (created by a ctor from stream): they are visually equal, however different DPIs are stored
            reference = refManager.GetObject("TestImageEmbedded");
            check = manager.GetObject("TestImageEmbedded");
            Assert.IsInstanceOf<Bitmap>(reference);
            AssertDeepEquals((Bitmap)reference, (Bitmap)check);

            // any object embedded as binary.base64 (created by BinaryFormatter)
            reference = refManager.GetObject("TestObjectEmbedded");
            check = manager.GetObject("TestObjectEmbedded");
            Assert.IsInstanceOf<ImageListStreamer>(reference);
            var il1 = new ImageList { ImageStream = (ImageListStreamer)reference };
            var il2 = new ImageList { ImageStream = (ImageListStreamer)check };
            for (int i = 0; i < il1.Images.Count; i++)
            {
                AssertDeepEquals(il1.Images[i] as Bitmap, il2.Images[i] as Bitmap);
            }

            // icon embedded as bytearray.base64 (created by a ctor from stream)
            reference = refManager.GetObject("TestIconEmbedded");
            check = manager.GetObject("TestIconEmbedded");
            Assert.IsInstanceOf<Icon>(reference);
            AssertItemsEqual(BinarySerializer.Serialize(reference), BinarySerializer.Serialize(check));

            // stream embedded as binary.base64 (created by BinaryFormatter)
            reference = refManager.GetObject("TestSoundEmbedded");
            check = manager.GetObject("TestSoundEmbedded");
            Assert.IsInstanceOf<MemoryStream>(reference);
            AssertItemsEqual(((MemoryStream)reference).ToArray(), ((MemoryStream)check).ToArray());

            // color embedded by type converter without <value> element
            reference = refManager.GetObject("TestColorWithoutValue");
            check = manager.GetObject("TestColorWithoutValue");
            Assert.IsInstanceOf<Color>(reference);
            Assert.AreEqual(reference, check);

            // color embedded by type converter with <value> element
            reference = refManager.GetObject("TestColorData");
            check = manager.GetObject("TestColorData");
            Assert.IsInstanceOf<Color>(reference);
            Assert.AreEqual(reference, check);
        }

        [Test]
        public void GetResourceSetTest()
        {
            var refManager = CreateResourceManager("_LibrariesTest.Resources.TestResourceResX", enUS);
            var manager = new ResXResourceManager("TestResourceResX", typeof(object).Assembly); // typeof(object): mscorlib has en-US invariant resources language
            var rsInv = manager.GetResourceSet(inv, loadIfExists: true, tryParents: false);

            // just checking that invariant exists
            Assert.IsNotNull(refManager.GetResourceSet(inv, createIfNotExists: true, tryParents: false));
            Assert.IsNotNull(rsInv);

            // enUS should return invariant when [assembly: NeutralResourcesLanguage("en-US")] is set
            Assert.AreSame(rsInv, manager.GetResourceSet(enUS, true, false));

            // but en != inv
            Assert.AreNotSame(rsInv, manager.GetResourceSet(en, true, false));

            // hu does not exist
            Assert.IsNull(manager.GetResourceSet(hu, loadIfExists: true, tryParents: false));

            // but returns inv when parents are required
            Assert.AreSame(rsInv, manager.GetResourceSet(hu, loadIfExists: true, tryParents: true));

            // when already obtained, the already obtained sets are returned for createIfNotExists = false
            Assert.IsNotNull(refManager.GetResourceSet(inv, createIfNotExists: false, tryParents: false));
            Assert.IsNotNull(manager.GetResourceSet(inv, loadIfExists: false, tryParents: false));

            // when not obtained, the tryParents=false will simply return null
            refManager.ReleaseAllResources();
            manager.ReleaseAllResources();
            Assert.IsNull(refManager.GetResourceSet(inv, createIfNotExists: false, tryParents: false));
            Assert.IsNull(manager.GetResourceSet(inv, loadIfExists: false, tryParents: false));

            // when not obtained but exists, the tryParents=true will also return null if createIfNotExists=false
            Assert.IsNull(refManager.GetResourceSet(inv, createIfNotExists: false, tryParents: true));
            Assert.IsNull(manager.GetResourceSet(inv, loadIfExists: false, tryParents: true));

            // but for for non-existing name even this will throw an exception
            refManager = CreateResourceManager("NonExisting", enUS);
            manager = new ResXResourceManager("NonExisting", typeof(object).Assembly); // typeof(object): mscorlib has en-US invariant resources language
            Throws<MissingManifestResourceException>(() => refManager.GetResourceSet(inv, createIfNotExists: false, tryParents: true));
            Throws<MissingManifestResourceException>(() => manager.GetResourceSet(inv, loadIfExists: false, tryParents: true));

            // createIfNotExists = true will throw an exception as well
            Throws<MissingManifestResourceException>(() => refManager.GetResourceSet(inv, createIfNotExists: true, tryParents: true));
            Throws<MissingManifestResourceException>(() => manager.GetResourceSet(inv, loadIfExists: true, tryParents: true));

            // except if tryParents=false, because in this case null will be returned
            Assert.IsNull(refManager.GetResourceSet(inv, createIfNotExists: true, tryParents: false));
            Assert.IsNull(manager.GetResourceSet(inv, loadIfExists: true, tryParents: false));

            // in system ResourceManager if a derived culture is required but only a parent is available, then this parent will be
            // cached for derived cultures, too, so groveling is needed only once. 
            refManager = CreateResourceManager("_LibrariesTest.Resources.TestResourceResX", inv);
            manager = new ResXResourceManager("TestResourceResX", inv);

            // System: requiring hu loads inv and caches this for hu, too
            Assert.IsNotNull(rsInv = refManager.GetResourceSet(hu, createIfNotExists: true, tryParents: true));
            Assert.AreSame(rsInv, refManager.GetResourceSet(inv, createIfNotExists: false, tryParents: false));
            Assert.AreSame(rsInv, refManager.GetResourceSet(hu, createIfNotExists: false, tryParents: false));

            // ResX: requiring hu loads inv and caches a proxy for hu, too, and outside this is transparent so proxy returns inv, too
            Assert.IsNotNull(rsInv = manager.GetResourceSet(hu, loadIfExists: true, tryParents: true));
            Assert.AreSame(rsInv, manager.GetResourceSet(inv, loadIfExists: false, tryParents: false));
            Assert.AreSame(rsInv, manager.GetResourceSet(hu, loadIfExists: false, tryParents: false));

            // now if we change something in hu, the cached inv proxy will be replaced
            manager.SetObject("test", 42, hu);
            ResourceSet rsHU;
            Assert.IsNotNull(rsHU = manager.GetResourceSet(hu, loadIfExists: false, tryParents: false));
            Assert.AreNotSame(rsInv, rsHU);

            // though en exist, we haven't load it yet, so if we don't load it, it will return inv, too
            Assert.IsNotNull(rsInv = refManager.GetResourceSet(inv, createIfNotExists: false, tryParents: false));
            Assert.IsNull(refManager.GetResourceSet(enUS, createIfNotExists: false, tryParents: false));
#if !NET35 // these all return null in .NET 3.5
            Assert.AreSame(rsInv, refManager.GetResourceSet(enUS, createIfNotExists: false, tryParents: true));
            Assert.AreSame(rsInv, refManager.GetResourceSet(enUS, createIfNotExists: false, tryParents: false));

            // and though en exists, it will not be loaded anymore if a parent is already cached
            Assert.AreSame(rsInv, refManager.GetResourceSet(enUS, createIfNotExists: true, tryParents: false));
#endif

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
            manager.GetResourceSet(enGB, loadIfExists: false, tryParents: true);
            manager.GetResourceSet(huHU, loadIfExists: false, tryParents: true);

            // now the hu branch is up-to-date but en-GB has unloaded parents because en actually exists but not loaded
            IDictionary resourceSets;
#if NET35
            resourceSets = (IDictionary)Reflector.GetField(manager, "ResourceSets"); // Hashtable
#else
            resourceSets = (IDictionary)Reflector.GetProperty(manager, "ResourceSets"); // Dictionary
#endif
            int sets = resourceSets.Count;

            // "loading" hu does not change anything, since it is up-to date
            Assert.AreSame(rsInv, manager.GetResourceSet(hu, loadIfExists: true, tryParents: false));
            Assert.AreEqual(sets, resourceSets.Count);

            // but loading en clears en-GB, since it depends on that. Re-accessing enGB returns now en
            ResourceSet rsEN;
            Assert.AreNotSame(rsInv, rsEN = manager.GetResourceSet(en, loadIfExists: true, tryParents: false));
            Assert.AreEqual(sets, 1 + resourceSets.Count);
            Assert.AreSame(rsEN, manager.GetResourceSet(enGB, loadIfExists: false, tryParents: true));
            Assert.AreEqual(sets, resourceSets.Count);

            // similarly, creating hu clears hu-HU, and re-accessing hu-HU returns hu
            Assert.AreNotSame(rsInv, rsHU = (ResourceSet)manager.GetExpandoResourceSet(hu, ResourceSetRetrieval.CreateIfNotExists, tryParents: false));
            Assert.AreEqual(sets, 1 + resourceSets.Count);
            Assert.AreSame(rsHU, manager.GetResourceSet(huHU, loadIfExists: true, tryParents: true));
            Assert.AreEqual(sets, resourceSets.Count);
        }

        [Test]
        public void IsModifiedTests()
        {
            var manager = new ResXResourceManager("TestResourceResX");

            // this loads inv and creates proxies for hu-HU and hu
            manager.GetResourceSet(huHU, true, true);
            Assert.IsFalse(manager.IsModified);

            // this replaces hu with ResXResourceSet and removes hu-HU proxy as it is invalidated
            manager.SetObject("new", "new", hu);
            Assert.IsTrue(manager.IsModified);
        }

        [Test]
        public void SaveTest()
        {
            var manager = new ResXResourceManager("TestResourceResX");

            // empty manager: save all is false even if forcing
            Assert.IsFalse(manager.IsModified);
            Assert.IsFalse(manager.SaveAllResources(true));

            // non-empty but unmodified manager: saving on forcing 
            manager.GetResourceSet(inv, true, false);
            Assert.IsFalse(manager.IsModified);
            Assert.IsFalse(manager.SaveAllResources(false));
            Assert.IsTrue(manager.SaveAllResources(true));

            // adding a new value: it will be dirty and saves without forcing, then it is not dirty any more
            manager.SetObject("new value inv", 42, inv);
            Assert.IsTrue(manager.IsModified);
            Assert.IsTrue(manager.SaveAllResources(false));
            Assert.IsFalse(manager.IsModified);

            // adding something to a non-loaded resource: it loads the resource and makes it dirty
            manager.SetObject("new value", 42, enUS);
            Assert.IsTrue(manager.IsModified);
            Assert.IsNotNull(manager.GetResourceSet(enUS, false, false));
            Assert.IsFalse(manager.SaveResourceSet(inv));
            Assert.IsTrue(manager.SaveResourceSet(enUS));
            Assert.IsFalse(manager.IsModified);
        }

        [Test]
        public void SerializationTest()
        {
            var refManager = new ResourceManager("_LibrariesTest.Resources.TestResourceResX", GetType().Assembly);
            var manager = new ResXResourceManager("TestResourceResX", GetType().Assembly);
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

        [Test]
        public void DisposeTest()
        {
            var manager = new ResXResourceManager("TestResourceResX", GetType().Assembly);
            manager.Dispose();
            Throws<ObjectDisposedException>(() => manager.ReleaseAllResources());
            Throws<ObjectDisposedException>(() => manager.GetString("TestString"));
            manager.Dispose(); // this will not throw anything
        }

        private ResourceManager CreateResourceManager(string name, CultureInfo neutralLang)
        {
            var result = new ResourceManager(name, GetType().Assembly);
            Reflector.SetField(result, "_neutralResourcesCulture", neutralLang);
            return result;
        }
    }
}
