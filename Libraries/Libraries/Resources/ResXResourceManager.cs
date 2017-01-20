using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace KGySoft.Libraries.Resources
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text;

    using KGySoft.Libraries.Reflection;
    using System.Threading;

    /// <summary>
    /// Represents a resource manager that provides convenient access to culture-specific XML resources (.resx files) at run time.
    /// New elements can be added as well, which can be saved into the <c>.resx</c> files.
    /// </summary>
    // TODO: ResXResourceManager vs ResourceManager inkompatibilitás:
    // - a gyári GetResourceSet createIfNotExists = false esetén becache-el egy parent culture-t, ha talál, onnantól mindig azt adja vissza, még ha a file létezik is, hiába hívjuk később true-val. Ez itt jól működik.
    [Serializable]
    public sealed class ResXResourceManager : ResourceManager, IExpandoResourceManager
    {
        /// <summary>
        /// Represents a cached resource set for a child culture, which might be replaced later.
        /// </summary>
        private sealed class ProxyResourceSet : ResourceSet
        {
            private bool canHaveLoadableParent;

            /// <summary>
            /// Gets the wrapped resource set. This is always a parent of <see cref="WrappedCulture"/>.
            /// </summary>
            internal ResXResourceSet ResXResourceSet { get; }

            /// <summary>
            /// Gets the culture of the wrapped resource set
            /// </summary>
            internal CultureInfo WrappedCulture { get; }

            /// <summary>
            /// Gets whether this proxy has been loaded by <see cref="ResourceSetRetrieval.GetIfAlreadyLoaded"/> and trying parents.
            /// In this can there might be unloaded parents for this resource set.
            /// </summary>
            internal bool CanHaveLoadableParent
            {
                get
                {
                    lock (this)
                        return canHaveLoadableParent;
                }
                set
                {
                    lock (this)
                        canHaveLoadableParent = value;
                }
            }

            /// <summary>
            /// Gets whether this proxy has been loaded by <see cref="ResourceSetRetrieval.GetIfAlreadyLoaded"/> and trying parents,
            /// and there is an existing file for this resource. File name itself is not stored so it will be re-groveled next time
            /// handling the case if it has been deleted.
            /// </summary>
            internal bool FileExists { get; }

            internal ProxyResourceSet(ResXResourceSet toWrap, CultureInfo wrappedCulture, bool fileExists, bool canHaveLoadableParent)
            {
                ResXResourceSet = toWrap;
                WrappedCulture = wrappedCulture;
                FileExists = fileExists;
                this.canHaveLoadableParent = canHaveLoadableParent;
            }

            internal bool HierarchyLoaded => !CanHaveLoadableParent && !FileExists;
        }

        internal const string resXFileExtension = ".resx";

        private string resxResourcesDir = "Resources";

        [NonSerialized]
        private string resxDirFullPath;

        /// <summary>
        /// Local cache of the base neutral resources culture.
        /// </summary>
        [NonSerialized]
        private CultureInfo neutralResourcesCulture;

#if NET40 || NET45
        /// <summary>
        /// Local cache of the resource sets stored in the base.
        /// </summary>
        private Dictionary<string, ResourceSet> resourceSets;

        private new Dictionary<string, ResourceSet> ResourceSets
        {
            get
            {
                return resourceSets
                    ?? (resourceSets = (Dictionary<string, ResourceSet>)Accessors.ResourceManager_resourceSets.Get(this));
            }
        }

#elif !NET35
#error .NET version is not set or not supported!
#endif

        private bool throwException = true;
        private bool safeMode;

        [NonSerialized]
        private object syncRoot;

        /// <summary>
        /// The lastly used resource set. Unlike in base, this is not necessarily the resource set in which a result
        /// has been found but the resource set was requested last time. In cases there are different this method performs usually better.
        /// </summary>
        [NonSerialized]
        private KeyValuePair<string, ResXResourceSet> lastUsedResourceSet;

        // todo: mese a neutralResourcesLanguage-ről: (a többi overload remarksjába is)
        // - az assembly csak a NeutralResourcesLanguageAttribute miatt, ha az nincs benne, InvariantCulture
        // - Itt azt jelenti, hogy ha ez egybeesik az elkért nyelvvel, akkor mindenképpen az invariant resx-ből próbálunk olvasni (vö. Translate esetén, ahol ilyenkor nincs fordítás, ott InvariantLanguage prop alapján)
        public ResXResourceManager(string baseName, Assembly assembly)
            : base(baseName, assembly, typeof(ResXResourceSet))
        {
            // - this will set MainAssembly and BaseNameField directly
            // - resx will be searched in dynamicResourcesDir\baseName[.Culture].resx
            // - _userResourceSet is set to ResXResourceSet and thus is returned by ResourceSetType; however, it will never be used by base because InternalGetResourceSet is overridden
            // - _neutralResourcesCulture is initialized from assembly (> .NET4 only)
#if NET35
            // .NET 3.5 sets _neutralResourcesCulture in its InternalGetResourceSet only so setting the field here.
            Accessors.ResourceManager_neutralResourcesCulture.Set(this, GetNeutralResourcesLanguage(assembly));
#endif // elif not needed because this will not be needed in newer versions
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceManager"/> class that looks up resources in
        /// resource XML files based on the provided <paramref name="baseName"/>.
        /// </summary>
        /// <param name="baseName">A base name that is the prefix of the resource files.</param>
        /// <param name="neutralResourcesLanguage">Determines the language of the neutral resources. When <see langword="null"/>,
        /// it will be determined by the entry assembly, or if that is not available, then by the assembly of the caller's method.</param>
        public ResXResourceManager(string baseName, CultureInfo neutralResourcesLanguage = null)
            : this(baseName, Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly())
        {
            if (neutralResourcesLanguage != null)
                Accessors.ResourceManager_neutralResourcesCulture.Set(this, neutralResourcesLanguage);
        }

        /// <summary>
        /// Creates a new instance of <see cref="ResXResourceManager"/> class that looks up resources in
        /// resource XML files based on information from the specified type object.
        /// </summary>
        /// <param name="resourceSource">A type from which the resource manager derives all information for finding resource files.</param>
        public ResXResourceManager(Type resourceSource)
            : this(resourceSource?.Name, resourceSource?.Assembly)
        {
        }

        /// <summary>
        /// Gets or sets the relative path to .resx resource files.
        /// <br/>Default value: <c>Resources</c>
        /// </summary>
        // TODO: desc: can throw argex, though not throwing it does not guarantee that it will work
        public string ResXResourcesDir
        {
            get { return resxResourcesDir; }
            set
            {
                if (value == resxResourcesDir)
                    return;

                resxDirFullPath = null;
                if (value == null)
                {
                    resxResourcesDir = String.Empty;
                    return;
                }

                if (value.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    throw new ArgumentException(Res.Get(Res.ValueContainsIllegalPathCharacters, value));

                if (Path.IsPathRooted(value))
                {
                    string baseDir = Files.GetExecutingPath();
                    string relPath = Files.GetRelativePath(value, baseDir);
                    if (!Path.IsPathRooted(relPath))
                    {
                        resxResourcesDir = relPath;
                        return;
                    }
                }

                resxResourcesDir = value;
            }
        }

        /// <summary>
        /// Gets or sets whether a <see cref="MissingManifestResourceException"/> should be thrown when a resource
        /// .resx file is not found even in the neutral culture. Default value: <c>true</c>.
        /// </summary>
        public bool ThrowException
        {
            get { return throwException; }
            set { throwException = value; }
        }

        /// <summary>
        /// Gets or sets whether the <see cref="ResXResourceManager"/> works in safe mode. In safe mode the retrieved
        /// objects are not deserialized automatically. See Remarks section for details.
        /// <br/>Default value: <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>When <c>SafeMode</c> is <c>true</c>, the <see cref="GetObject(string)"/> and <see cref="GetMetaObject"/> methods
        /// return <see cref="ResXDataNode"/> instances instead of deserialized objects. You can retrieve the deserialized
        /// objects on demand by calling the <see cref="ResXDataNode.GetValue"/> method on the <see cref="ResXDataNode"/> instance.</para>
        /// <para>When <c>SafeMode</c> is <c>true</c>, the <see cref="GetString(string)"/> and <see cref="GetMetaString"/> methods
        /// work for every defined item in the resource set. For non-string elements the raw XML string value will be returned.</para>
        /// </remarks>
        /// <seealso cref="ResXResourceReader.SafeMode"/>
        /// <seealso cref="ResXResourceSet.SafeMode"/>
        public bool SafeMode
        {
            get { return safeMode; }
            set
            {
                //if (resources == null)
                //    throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                safeMode = value;
            }
        }

        private object SyncRoot
        {
            get
            {
                if (syncRoot == null)
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);
                return syncRoot;
            }
        }

        /// <summary>
        /// Returns the value of the specified string resource.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <returns>
        /// The value of the resource localized for the caller's current UI culture, or <see langword="null"/> if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        public override string GetString(string name)
        {
            return (string)GetObjectInternal(name, null, true);
        }

        /// <summary>
        /// Returns the value of the string resource localized for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the resource is localized. If the resource is not localized for
        /// this culture, the resource manager uses fallback rules to locate an appropriate resource. If this value is
        /// <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture" /> property.</param>
        /// <returns>
        /// The value of the resource localized for the specified culture, or <see langword="null"/> if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        public override string GetString(string name, CultureInfo culture)
        {
            return (string)GetObjectInternal(name, culture, true);
        }

        /// <summary>
        /// Returns the value of the specified non-string resource.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <returns>
        /// The value of the resource localized for the caller's current UI culture.
        /// If an appropriate resource set exists but <paramref name="name" /> cannot be found, the method returns <see langword="null"/>.
        /// </returns>
        public override object GetObject(string name)
        {
            return GetObjectInternal(name, null, false);
        }

        /// <summary>
        /// Gets the value of the specified non-string resource localized for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <param name="culture">The culture for which the resource is localized. If the resource is not localized for
        /// this culture, the resource manager uses fallback rules to locate an appropriate resource. If this value is
        /// <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture" /> property.</param>
        /// <returns>
        /// The value of the resource, localized for the specified culture. If an appropriate resource set exists but <paramref name="name" /> cannot be found,
        /// the method returns <see langword="null"/>.
        /// </returns>
        public override object GetObject(string name, CultureInfo culture)
        {
            return GetObjectInternal(name, culture, false);
        }

        /// <summary>
        /// Returns the value of the string metadata for the specified culture.
        /// </summary>
        /// <param name="name">The name of the metadata to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the metadata should be returned.
        /// If this value is <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture" /> property.
        /// Unlike in case of <see cref="GetString(string,CultureInfo)"/> method, no fallback is used if the metadata is not found in the specified culture.
        /// </param>
        /// <returns>
        /// The value of the metadata of the specified culture, or <see langword="null"/> if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        public string GetMetaString(string name, CultureInfo culture = null)
        {
            return (string)GetMetaInternal(name, culture, true);
        }

        /// <summary>
        /// Returns the value of the specified non-string metadata for the specified culture.
        /// </summary>
        /// <param name="name">The name of the metadata to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the metadata should be returned.
        /// If this value is <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture" /> property.
        /// Unlike in case of <see cref="GetObject(string,CultureInfo)"/> method, no fallback is used if the metadata is not found in the specified culture.
        /// </param>
        /// <returns>
        /// The value of the metadata of the specified culture, or <see langword="null"/> if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        public object GetMetaObject(string name, CultureInfo culture = null)
        {
            return GetMetaInternal(name, culture, false);
        }

        private object GetObjectInternal(string name, CultureInfo culture, bool isString)
        {
            if (null == name)
                throw new ArgumentNullException("name", Res.Get(Res.ArgumentNull));

            if (culture == null)
                culture = CultureInfo.CurrentUICulture;

            ResXResourceSet last = GetFirstResourceSet(culture);
            object value;
            if (last != null)
            {
                value = last.GetResourceInternal(name, IgnoreCase, isString, safeMode);
                if (value != null)
                    return value;
            }

            // The GetResXResourceSet has also a hierarchy traversal. This outer traversal is required as well because
            // the inner one can return an existing resource set without the searched resource, in which case here is
            // the fallback to the parent resource.
            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, NeutralResourcesCulture, true);
            ResXResourceSet toCache = null;
            foreach (CultureInfo currentCultureInfo in mgr)
            {
                ResXResourceSet rs = GetResXResourceSet(currentCultureInfo, ResourceSetRetrieval.LoadIfExists, true);
                if (rs == null)
                    return null;

                if (rs == last)
                    continue;

                if (toCache == null)
                    toCache = rs;

                value = rs.GetResourceInternal(name, IgnoreCase, isString, safeMode);
                if (value != null)
                {
                    lock (syncRoot)
                    {
                        lastUsedResourceSet = new KeyValuePair<string, ResXResourceSet>(culture.Name, toCache);
                    }

                    return value;
                }

                last = rs;
            }

            return null;
        }

        private object GetMetaInternal(string name, CultureInfo culture, bool isString)
        {
            if (null == name)
                throw new ArgumentNullException(nameof(name), Res.Get(Res.ArgumentNull));

            // in case of metadata there is no hierarchy traversal
            ResXResourceSet rs = GetResXResourceSet(culture ?? CultureInfo.InvariantCulture, ResourceSetRetrieval.LoadIfExists, false);
            return rs?.GetMetaInternal(name, IgnoreCase, isString, safeMode);
        }

        public override void ReleaseAllResources()
        {
            base.ReleaseAllResources();
#if NET40 || NET45
            resourceSets = null; // clearing local cache because here base creates a new instance
#elif !NET35
#error .NET version is not set or not supported!
#endif
            lastUsedResourceSet = default(KeyValuePair<string, ResXResourceSet>);
        }

        private ResXResourceSet GetFirstResourceSet(CultureInfo culture)
        {
            // Logic from ResourceFallbackManager.GetEnumerator()
            if (!ReferenceEquals(culture, CultureInfo.InvariantCulture) && culture.Name == NeutralResourcesCulture.Name)
                culture = CultureInfo.InvariantCulture;

            lock (SyncRoot)
            {
                if (culture.Name == lastUsedResourceSet.Key)
                    return lastUsedResourceSet.Value;

                // Look in the ResourceSet table
                var localResourceSets = ResourceSets; // this is HashTable in .NET 3.5, Dictionary above
                ResourceSet rs;
                if (!TryGetResource(localResourceSets, culture.Name, out rs))
                    return null;

                // update the cache with the most recent ResourceSet
                ResXResourceSet result = GetResXResourceSet(rs);
                lastUsedResourceSet = new KeyValuePair<string, ResXResourceSet>(culture.Name, result);
                return result;
            }
        }

        internal string ResourceFileName
        {
            get { return GetResourceFileName(CultureInfo.InvariantCulture); }
        }

        private CultureInfo NeutralResourcesCulture
        {
            get
            {
                return neutralResourcesCulture ??
                    (neutralResourcesCulture = ((CultureInfo)Accessors.ResourceManager_neutralResourcesCulture.Get(this) 
                    ?? CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Generates the name of the resource file for the given <see cref="CultureInfo" /> object.
        /// </summary>
        /// <param name="culture">The culture object for which a resource file name is constructed.</param>
        /// <returns>
        /// The name that can be used for a resource file for the given <see cref="CultureInfo" /> object.
        /// </returns>
        protected override string GetResourceFileName(CultureInfo culture)
        {
            return GetResourceFileName(culture.Name);
        }

        private string GetResourceFileName(string cultureName)
        {
            StringBuilder result = new StringBuilder(260);
            result.Append(BaseName);
            if (CultureInfo.InvariantCulture.Name != cultureName)
            {
                result.Append('.');
                result.Append(cultureName);
            }

            result.Append(resXFileExtension);
            return Path.Combine(GetResourceDirName(), result.ToString());
        }

        /// <summary>
        /// Retrieves the resource set for a particular culture.
        /// </summary>
        /// <param name="culture">The culture whose resources are to be retrieved.</param>
        /// <param name="loadIfExists"><c>true</c> to load the resource set, if it has not been loaded yet and the corresponding resource file exists; otherwise, <c>false</c>.</param>
        /// <param name="tryParents"><c>true</c> to use resource fallback to load an appropriate resource if the resource set cannot be found; <c>false</c> to bypass the resource fallback process.</param>
        /// <returns>
        /// The resource set for the specified culture.
        /// </returns>
        /// <exception cref="MissingManifestResourceException">The .resx file of the neutral culture was not found, while <paramref name="tryParents"/> and <see cref="ThrowException"/> are both <c>true</c>.</exception>
        public override ResourceSet GetResourceSet(CultureInfo culture, bool loadIfExists, bool tryParents)
        {
            return (ResourceSet)GetExpandoResourceSet(culture, loadIfExists ? ResourceSetRetrieval.LoadIfExists : ResourceSetRetrieval.GetIfAlreadyLoaded, tryParents);
        }

        /// <summary>
        /// Provides the implementation for finding a resource set.
        /// </summary>
        /// <param name="culture">The culture object to look for.</param>
        /// <param name="loadIfExists"><c>true</c> to load the resource set, if it has not been loaded yet; otherwise, <c>false</c>.</param>
        /// <param name="tryParents"><c>true</c> to check parent <see cref="CultureInfo" /> objects if the resource set cannot be loaded; otherwise, <c>false</c>.</param>
        /// <returns>
        /// The specified resource set.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="MissingManifestResourceException">The .resx file of the neutral culture was not found, while <paramref name="tryParents"/> and <see cref="ThrowException"/> are both <c>true</c>.</exception>
        protected override ResourceSet InternalGetResourceSet(CultureInfo culture, bool loadIfExists, bool tryParents)
        {
            Debug.Fail("InternalGetResourceSet is called");

            // the base tries to parse the stream as binary. It would be better if GrovelForResourceSet
            // would be protected in base, so it would be enough to override only that (at least in .NET 4 and above).
            // Or, not exactly because we cache the non-found cultures differently via a proxy.
            return GetResXResourceSet(culture, loadIfExists ? ResourceSetRetrieval.LoadIfExists : ResourceSetRetrieval.GetIfAlreadyLoaded, tryParents);
        }

        internal ResXResourceSet GetResXResourceSet(CultureInfo culture, ResourceSetRetrieval behavior, bool tryParents)
        {
            if (culture == null)
                throw new ArgumentNullException(nameof(culture), Res.Get(Res.ArgumentNull));
            var localResourceSets = ResourceSets; // var is Hashtable in .NET 3.5 and is Dictionary above
            ResourceSet rs;
            bool resourceFound;
            lock (SyncRoot)
            {
                resourceFound = TryGetResource(localResourceSets, culture.Name, out rs);
            }

            ProxyResourceSet proxy;
            if (resourceFound)
            {
                // returning the cached resource if that is not a proxy or when the proxy does not have to be (possibly) replaced
                if (rs is ResXResourceSet // result is not a proxy but an actual resource
                    || behavior == ResourceSetRetrieval.GetIfAlreadyLoaded // nothing new should be loaded 
                    || (behavior == ResourceSetRetrieval.LoadIfExists
                        && ((proxy = (ProxyResourceSet)rs).HierarchyLoaded // nothing new can be loaded in the hierarchy
                            || !tryParents && !proxy.FileExists))) // though there can be unloaded parents, only the child is required, which cannot be loaded
                {
                    return GetResXResourceSet(rs);
                }
            }

            CultureInfo foundCultureToAdd = null;
            CultureInfo foundProxyCulture = null;
            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, NeutralResourcesCulture, tryParents);
            foreach (CultureInfo currentCultureInfo in mgr)
            {
                lock (syncRoot)
                {
                    resourceFound = TryGetResource(localResourceSets, currentCultureInfo.Name, out rs);
                }

                if (resourceFound)
                {
                    // a final result is found in the local cache
                    ResXResourceSet resx = rs as ResXResourceSet;
                    if (resx != null)
                    {
                        // since the first try above we have a result from another thread for the searched culture
                        if (Equals(culture, currentCultureInfo))
                            return resx;

                        // after some proxies, a parent culture has been found: simply return if this was the proxied culture in the children
                        if (Equals(currentCultureInfo, foundProxyCulture))
                            return resx;

                        // othwerwise, we found a parent: we need to re-create the proxies in the cache to the children
                        Debug.Assert(foundProxyCulture == null, "There is a proxy with an incostistent parent in the hierarchy.");
                        foundCultureToAdd = currentCultureInfo;
                        break;
                    }

                    // proxy is found
                    proxy = (ProxyResourceSet)rs;
                    Debug.Assert(foundProxyCulture == null || Equals(foundProxyCulture, proxy.WrappedCulture), "Proxied cultures are different in the hierarchy.");
                    if (foundProxyCulture == null)
                        foundProxyCulture = proxy.WrappedCulture;

                    // if we traversing here because last time the proxy has been loaded by
                    // ResourceSetRetrieval.GetIfAlreadyLoaded, but now we load the possible parents, we clear the 
                    // CanHaveLoadableParent flag in the hierarchy. Unless no new proxy is created (and thus the descendant proxies are deleted),
                    // this will prevent the redundant traversal next time.
                    if (tryParents && behavior == ResourceSetRetrieval.LoadIfExists)
                    {
                        proxy.CanHaveLoadableParent = false;
                    }
                }

                Debug.Assert(foundProxyCulture == null || rs != null, "There is a proxy without parent in the hierarchy.");
                bool exists;
                rs = GrovelForResourceSet(currentCultureInfo, behavior != ResourceSetRetrieval.GetIfAlreadyLoaded, out exists);

                if (throwException && tryParents && !exists && behavior != ResourceSetRetrieval.CreateIfNotExists
                    && Equals(currentCultureInfo, CultureInfo.InvariantCulture))
                {
                    throw new MissingManifestResourceException(Res.Get(Res.NeutralResourceFileNotFoundResX, GetResourceFileName(currentCultureInfo)));
                }

                // a new ResourceSet has been loaded; we're done
                if (rs != null)
                {
                    foundCultureToAdd = currentCultureInfo;
                    break;
                }

                // no resource is found but we need to create one
                if (behavior == ResourceSetRetrieval.CreateIfNotExists)
                {
                    foundCultureToAdd = currentCultureInfo;
                    rs = new ResXResourceSet(GetResourceDirName());
                    break;
                }
            }

            // there is a culture to be added to the cache
            if (foundCultureToAdd != null)
            {
                lock (syncRoot)
                {
                    // we replace a proxy: we must delete proxies, which are children of the found resource.
                    if (foundProxyCulture != null)
                    {
                        Debug.Assert(!Equals(foundProxyCulture, foundCultureToAdd), "The culture to add is the same as the existing proxies.");
                        List<string> keysToRemove;
#if NET35
                        keysToRemove = localResourceSets.Cast<DictionaryEntry>().Where(item => item.Value is ProxyResourceSet && IsParentCulture(foundCultureToAdd, item.Key.ToString())).Select(item => item.Key.ToString()).ToList();
#else
                        keysToRemove = localResourceSets.Where(item => item.Value is ProxyResourceSet && IsParentCulture(foundCultureToAdd, item.Key)).Select(item => item.Key).ToList();
#endif
                        foreach (string key in keysToRemove)
                        {
                            localResourceSets.Remove(key);
                        }
                    }

                    // Add entries to the cache for the cultures we have gone through.
                    // foundCultureToAdd now refers to the culture that had resources.
                    // Update cultures starting from requested culture up to the culture
                    // that had resources, but in place of non-found resources we will place a proxy.
                    foreach (CultureInfo updateCultureInfo in mgr)
                    {
                        // stop when we've added current or reached invariant (top of chain)
                        if (ReferenceEquals(updateCultureInfo, foundCultureToAdd))
                        {
                            AddResourceSet(localResourceSets, updateCultureInfo.Name, ref rs);
                            break;
                        }

                        ResourceSet newProxy = new ProxyResourceSet(GetResXResourceSet(rs), foundCultureToAdd,
                            GetExistingResourceFileName(updateCultureInfo) != null, behavior == ResourceSetRetrieval.GetIfAlreadyLoaded);
                        AddResourceSet(localResourceSets, updateCultureInfo.Name, ref newProxy);
                    }

                    lastUsedResourceSet = default(KeyValuePair<string, ResXResourceSet>);
                }
            }

            return GetResXResourceSet(rs);
        }

        internal static bool IsParentCulture(CultureInfo parent, string childName)
        {
            for (CultureInfo ci = CultureInfo.GetCultureInfo(childName).Parent; !Equals(ci, CultureInfo.InvariantCulture); ci = ci.Parent)
            {
                if (Equals(ci, parent))
                    return true;
            }

            return false;
        }

        private static ResXResourceSet GetResXResourceSet(ResourceSet rs)
        {
            if (rs == null)
                return null;

            ResXResourceSet resx = rs as ResXResourceSet;
            if (resx != null)
                return resx;

            return ((ProxyResourceSet)rs).ResXResourceSet;
        }

        private ResXResourceSet GrovelForResourceSet(CultureInfo culture, bool loadIfExists, out bool exists)
        {
            string fileName = GetExistingResourceFileName(culture);
            exists = fileName != null;
            return exists && loadIfExists ? new ResXResourceSet(fileName, GetResourceDirName()): null;
        }

        private string GetExistingResourceFileName(CultureInfo culture)
        {
            string fileName = GetResourceFileName(culture);
            bool exists = File.Exists(fileName);

            // fallback for neutral culture: if neutral file does not exist but specific does, using that file.
            if (!exists && Equals(CultureInfo.InvariantCulture, culture))
            {
                CultureInfo neutralResources = NeutralResourcesCulture;
                if (!CultureInfo.InvariantCulture.Equals(neutralResources))
                {
                    string neutralFileName = GetResourceFileName(neutralResources);
                    if (File.Exists(neutralFileName))
                    {
                        fileName = neutralFileName;
                        exists = true;
                    }
                }
            }

            return exists ? fileName : null;
        }

#if NET35
        private static bool TryGetResource(Hashtable localResourceSets, string cultureName, out ResourceSet rs)
        {
            rs = (ResourceSet)localResourceSets[cultureName];
            return rs != null;
        }

        private static void AddResourceSet(Hashtable localResourceSets, string cultureName, ref ResourceSet rs)
        {
            // GetResXResourceSet is both recursive and reentrant - 
            // assembly load callbacks in particular are a way we can call
            // back into the ResourceManager in unexpectedly on the same thread.
            lock (localResourceSets)
            {
                // If another thread added this culture, return that.
                ResourceSet lostRace = (ResourceSet)localResourceSets[cultureName];
                if (lostRace != null)
                {
                    if (!ReferenceEquals(lostRace, rs))
                    {
                        // Note: In certain cases, we can be trying to add a ResourceSet for multiple
                        // cultures on one thread, while a second thread added another ResourceSet for one
                        // of those cultures.  So when we lose the race, we must make sure our ResourceSet 
                        // isn't in our dictionary before closing it.
                        // But if a proxy is already in the cache, we replace that.
                        if (!(lostRace is ProxyResourceSet && rs is ResXResourceSet))
                        {
                            if (!localResourceSets.ContainsValue(rs))
                                rs.Dispose();
                            rs = lostRace;
                        }
                        else
                            localResourceSets[cultureName] = rs;
                    }
                }
                else
                {
                    localResourceSets.Add(cultureName, rs);
                }
            }
        }
#elif NET40 || NET45
#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // available only above 4.0
#endif // no else is needed, attribute is added always above 4.0
        private static bool TryGetResource(Dictionary<string, ResourceSet> localResourceSets, string cultureName, out ResourceSet rs)
        {
            return localResourceSets.TryGetValue(cultureName, out rs);
        }

        private static void AddResourceSet(Dictionary<string, ResourceSet> localResourceSets, string cultureName, ref ResourceSet rs)
        {
            // GetResXResourceSet is both recursive and reentrant - 
            // assembly load callbacks in particular are a way we can call
            // back into the ResourceManager in unexpectedly on the same thread.
            lock (localResourceSets)
            {
                // If another thread added this culture, return that.
                ResourceSet lostRace;
                if (TryGetResource(localResourceSets, cultureName, out lostRace))
                {
                    if (!ReferenceEquals(lostRace, rs))
                    {
                        // Note: In certain cases, we can be trying to add a ResourceSet for multiple
                        // cultures on one thread, while a second thread added another ResourceSet for one
                        // of those cultures.  So when we lose the race, we must make sure our ResourceSet 
                        // isn't in our dictionary before closing it.
                        // But if a proxy is already in the cache, we replace that.
                        if (lostRace is ProxyResourceSet && rs is ResXResourceSet)
                            localResourceSets[cultureName] = rs;
                        else
                        {
                            if (!localResourceSets.ContainsValue(rs))
                                rs.Dispose();
                            rs = lostRace;
                        }
                    }
                }
                else
                {
                    localResourceSets.Add(cultureName, rs);
                }
            }
        }
#else
#error .NET version is not set or not supported!
#endif

        private string GetResourceDirName()
        {
            if (resxDirFullPath != null)
                return resxDirFullPath;

            if (String.IsNullOrEmpty(resxResourcesDir))
                resxDirFullPath = Files.GetExecutingPath();
            else if (Path.IsPathRooted(resxResourcesDir))
                resxDirFullPath = resxResourcesDir;
            else
                resxDirFullPath = Path.Combine(Files.GetExecutingPath(), resxResourcesDir);

            return resxDirFullPath;
        }

        /// <summary>
        /// Creates an empty resource set for the given culture so it can be expanded.
        /// Does not make the resource set dirty until it is actually edited.
        /// </summary>
        internal ResXResourceSet CreateResourceSet(CultureInfo culture)
        {
            ResourceSet result = new ResXResourceSet(GetResourceDirName());
            lock (SyncRoot)
            {
                AddResourceSet(ResourceSets, culture.Name, ref result);
                lastUsedResourceSet = default(KeyValuePair<string, ResXResourceSet>);
            }

            Debug.Assert(result is ResXResourceSet, "AddResourceSet has replaced the ResXResourceSet to a proxy.");
            return (ResXResourceSet)result;
        }

        #region IExpandoResourceManager Members

        /// <summary>
        /// Gets whether this <see cref="ResXResourceManager"/> instance has modified and unsaved data.
        /// </summary>
        public bool IsModified
        {
            get
            {
                lock (SyncRoot)
                {
                    return ResourceSets.Values.Cast<ResXResourceSet>().Any(rs => rs.IsModified);
                }
            }
        }

        /// <summary>
        /// Retrieves the resource set for a particular culture, which can be dynamically modified.
        /// </summary>
        /// <param name="culture">The culture whose resources are to be retrieved.</param>
        /// <param name="behavior">Determines the retrieval behavior of the result <see cref="IExpandoResourceSet"/>.
        /// <br/>Default value: <see cref="ResourceSetRetrieval.CreateIfNotExists"/>.</param>
        /// <param name="tryParents"><c>true</c> to use resource fallback to load an appropriate resource if the resource set cannot be found; <c>false</c> to bypass the resource fallback process.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>The resource set for the specified culture, or <see langeword="null"/> if the specified culture cannot be retrieved by the defined <paramref name="behavior"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="behavior"/> does not fall in the expected range.</exception>
        /// <exception cref="MissingManifestResourceException">Resource file of the neutral culture was not found, while <paramref name="tryParents"/> is <c>true</c>
        /// and <paramref name="behavior"/> is not <see cref="ResourceSetRetrieval.CreateIfNotExists"/>.</exception>
        public IExpandoResourceSet GetExpandoResourceSet(CultureInfo culture, ResourceSetRetrieval behavior = ResourceSetRetrieval.LoadIfExists, bool tryParents = false)
        {
            if (!Enum<ResourceSetRetrieval>.IsDefined(behavior))
                throw new ArgumentOutOfRangeException(nameof(behavior), Res.Get(Res.ArgumentOutOfRange));

            ResXResourceSet result = GetResXResourceSet(culture, behavior, tryParents);

            // this.SafeMode is not used to set ResXResourceSet.SafeMode when resources are accessed so setting it only when the user obtains a ResXResourceSet instance.
            if (result != null)
                result.SafeMode = safeMode;

            return result;
        }

        /// <summary>
        /// Adds or replaces a resource object in the current <see cref="ResXResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The name of the resource to set.</param>
        /// <param name="culture">The culture of the resource to set. If this value is <see langword="null"/>,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture" /> property.</param>
        /// <param name="value">The value of the resource to set. If <see langword="null" />.</param>
        /// <remarks>
        /// <para>If <paramref name="value" /> is <see langword="null" />, a null reference will be explicitly stored.
        /// Its effect is similar to the <see cref="RemoveObject"/> method: the subsequent <see cref="GetObject(string, CultureInfo)" /> calls
        /// with the same <paramref name="culture" /> will fall back to the parent culture, or will return <see langword="null" /> if
        /// <paramref name="name" /> is not found in any parent cultures. However, enumerating the result set returned by
        /// <see cref="GetExpandoResourceSet"/> and <see cref="GetResourceSet"/> methods will return the resources with
        /// <see langword="null" /> value.</para>
        /// </remarks>
        public void SetObject(string name, object value, CultureInfo culture = null)
        {
            ResXResourceSet rs = GetResXResourceSet(culture ?? CultureInfo.CurrentUICulture, ResourceSetRetrieval.CreateIfNotExists, false);
            rs.SetObject(name, value);
        }

        /// <summary>
        /// Removes a resource object from the current <see cref="ResXResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The case-sensitive name of the resource to remove.</param>
        /// <param name="culture">The culture of the resource to remove. If this value is <see langword="null"/>,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture" /> property.</param>
        /// <remarks>
        /// <para><paramref name="name"/> is considered as case-sensitive. If <paramref name="name"/> occurs multiple times
        /// in the resource set in case-insensitive manner, they can be removed one by one only.</para>
        /// </remarks>
        public void RemoveObject(string name, CultureInfo culture = null)
        {
            ResXResourceSet rs = GetResXResourceSet(culture ?? CultureInfo.CurrentUICulture, ResourceSetRetrieval.LoadIfExists, false);
            rs?.RemoveObject(name);
        }

        /// <summary>
        /// Adds or replaces a metadata object in the current <see cref="ResXResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The name of the metadata to set.</param>
        /// <param name="culture">The culture of the metadata to set.
        /// If this value is <see langword="null" />, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture" /> property.</param>
        /// <param name="value">The value of the metadata to set. If <see langword="null" />,  then a null reference will be explicitly
        /// stored for the specified <paramref name="culture" />.</param>
        /// <remarks>
        /// If <paramref name="value" /> is <see langword="null" />, a null reference will be explicitly stored.
        /// Its effect is similar to the <see cref="RemoveMetaObject" /> method: the subsequent <see cref="GetMetaObject" /> calls
        /// with the same <paramref name="culture" /> will return <see langword="null" />.
        /// However, enumerating the result set returned by <see cref="GetExpandoResourceSet" /> method will return the meta objects with <see langword="null" /> value.
        /// </remarks>
        public void SetMetaObject(string name, object value, CultureInfo culture = null)
        {
            ResXResourceSet rs = GetResXResourceSet(culture ?? CultureInfo.InvariantCulture, ResourceSetRetrieval.CreateIfNotExists, false);
            rs.SetMetaObject(name, value);
        }

        /// <summary>
        /// Removes a metadata object from the current <see cref="ResXResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The case-sensitive name of the metadata to remove.</param>
        /// <param name="culture">The culture of the metadata to remove.
        /// If this value is <see langword="null" />, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture" /> property.</param>
        /// <remarks>
        /// <paramref name="name" /> is considered as case-sensitive. If <paramref name="name" /> occurs multiple times
        /// in the resource set in case-insensitive manner, they can be removed one by one only.
        /// </remarks>
        public void RemoveMetaObject(string name, CultureInfo culture = null)
        {
            ResXResourceSet rs = GetResXResourceSet(culture ?? CultureInfo.InvariantCulture, ResourceSetRetrieval.LoadIfExists, false);
            rs?.RemoveMetaObject(name);
        }

        /// <summary>
        /// Saves the resource set of a particular <paramref name="culture" /> if it has been already loaded.
        /// </summary>
        /// <param name="culture">The culture of the resource set to save.</param>
        /// <param name="force"><c>true</c> to save the resource set even if it has not been modified; <c>false</c> to save it only if it has been modified.
        /// <br />Default value: <c>false</c>.</param>
        /// <param name="compatibleFormat">If set to <c>true</c>, the result .resx file can be read by the system <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx">ResXResourceReader</a> class
        /// and the Visual Studio Resource Editor. If set to <c>false</c>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />), but the result can be read only by <see cref="ResXResourceReader" /><br />Default value: <c>false</c>.</param>
        /// <returns>
        /// <c>true</c> if the resource set of the specified <paramref name="culture" /> has been saved;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="IOException">The resource set could not be saved.</exception>
        public bool SaveResourceSet(CultureInfo culture, bool force = false, bool compatibleFormat = false)
        {
            if (culture == null)
                throw new ArgumentNullException(nameof(culture), Res.Get(Res.ArgumentNull));
            var localResourceSets = ResourceSets; // var is Hashtable in .NET 3.5 and is Dictionary above
            ResourceSet rs;
            lock (SyncRoot)
            {
                if (!TryGetResource(localResourceSets, culture.Name, out rs))
                    return false;
            }

            var resx = rs as ResXResourceSet;
            if (resx == null || !(force || resx.IsModified))
                return false;

            resx.Save(GetResourceFileName(culture), compatibleFormat);
            return true;
        }

        /// <summary>
        /// Saves all already loaded resources.
        /// </summary>
        /// <param name="force"><c>true</c> to save all of the already loaded resource sets regardless if they have been modified; <c>false</c> to save only the modified resource sets.
        /// <br />Default value: <c>false</c>.</param>
        /// <param name="compatibleFormat">If set to <c>true</c>, the result .resx files can be read by the system <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx">ResXResourceReader</a> class
        /// and the Visual Studio Resource Editor. If set to <c>false</c>, the result .resx files are often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />), but the result can be read only by <see cref="ResXResourceReader" /><br />Default value: <c>false</c>.</param>
        /// <returns>
        ///   <c>true</c> if at least one resource set has been saved; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="IOException">A resource set could not be saved.</exception>
        public bool SaveAllResources(bool force = false, bool compatibleFormat = false)
        {
            var localResourceSets = ResourceSets; // var is Hashtable in .NET 3.5 and is Dictionary above
            bool result = false;
            lock (SyncRoot)
            {
                // this enumerates both Hashtable and Dictionary the same way.
                // The nongeneric enumerator is not a problem, values must be cast anyway.
                IDictionaryEnumerator enumerator = localResourceSets.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    ResXResourceSet rs = (ResXResourceSet)enumerator.Value;
                    if (!rs.IsModified && !force)
                        continue;

                    rs.Save(GetResourceFileName((string)enumerator.Key));
                    result = true;
                }
            }

            return result;
        }

        #endregion

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {
            var resources = ResourceSets; // var is Hashtable in .NET 3.5, and is Dictionary above
            if (resources.Count == 0)
                return;

            lock (SyncRoot)
            {
                var keys = from res in resources // res.Key is object in .NET 3.5, and is string above
#if NET35
                           .Cast<DictionaryEntry>()
#endif
                           where !(res.Value is ResXResourceSet) || !((ResXResourceSet)res.Value).IsModified
                           select res.Key;

                foreach (var key in keys.ToList()) // key is object in .NET 3.5, and is string above
                {
                    resources.Remove(key);
                }
            }
        }
    }
}
