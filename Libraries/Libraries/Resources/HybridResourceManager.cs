using System.Collections.Generic;
using System.Resources;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using KGySoft.Libraries.Reflection;

namespace KGySoft.Libraries.Resources
{
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// Represents a resource manager that provides convenient access to culture-specific resources at run time.
    /// It can handle both compiled resources from <c>.dll</c> and <c>.exe</c> files, and <c>.resx</c> files at
    /// the same time. New elements can be added as well, which can be saved into <c>.resx</c> files.
    /// </summary>
    // TODO: Említeni a Dynamic-ot, mi a különbség. Itt minden művelet explicit, a bővítés és mentés is.
    [Serializable]
    public class HybridResourceManager : ResourceManager, IExpandoResourceManager, IDisposable
    {
        /// <summary>
        /// Represents a cached resource set for a child culture, which might be replaced later.
        /// </summary>
        private sealed class ProxyResourceSet : ResourceSet
        {
            private const string errorMessage = "Internal error: This operation is invalid on a ProxyResourceSet";
            private bool hierarchyLoaded;

            /// <summary>
            /// Gets the wrapped resource set. This is always a parent of the represented resource set.
            /// </summary>
            internal ResourceSet WrappedResourceSet { get; }

            /// <summary>
            /// Gets the culture of the wrapped resource set
            /// </summary>
            internal CultureInfo WrappedCulture { get; }

            internal bool HierarchyLoaded
            {
                get
                {
                    lock (this)
                        return hierarchyLoaded;
                }
                set
                {
                    lock (this)
                        hierarchyLoaded = value;
                }
            }

            internal ProxyResourceSet(ResourceSet toWrap, CultureInfo wrappedCulture, bool hierarchyLoaded)
            {
                WrappedResourceSet = toWrap;
                WrappedCulture = wrappedCulture;
                this.hierarchyLoaded = hierarchyLoaded;
            }

            public override object GetObject(string name, bool ignoreCase)
            {
                throw new InvalidOperationException(errorMessage);
            }

            public override object GetObject(string name)
            {
                throw new InvalidOperationException(errorMessage);
            }

            public override string GetString(string name)
            {
                throw new InvalidOperationException(errorMessage);
            }

            public override string GetString(string name, bool ignoreCase)
            {
                throw new InvalidOperationException(errorMessage);
            }

            public override IDictionaryEnumerator GetEnumerator()
            {
                throw new InvalidOperationException(errorMessage);
            }
        }

        private readonly ResXResourceManager resxResources; // used as sync obj as well because this reference lives along with parent lifetime and is invisible from outside
        private ResourceManagerSources source = ResourceManagerSources.CompiledAndResX;
        private bool throwException = true;
        private bool safeMode;

        [NonSerialized]
        private Dictionary<string, ResourceSet> resourceSets;

        /// <summary>
        /// The lastly used resource set. Unlike in base, this is not necessarily the resource set in which a result
        /// has been found but the resource set was requested last time. In cases there are different this method performs usually better.
        /// </summary>
        [NonSerialized]
        private KeyValuePair<string, ResourceSet> lastUsedResourceSet;

        /// <summary>
        /// Local cache of the base neutral resources culture.
        /// </summary>
        [NonSerialized]
        private CultureInfo neutralResourcesCulture;

        /// <summary>
        /// Gets or sets whether a <see cref="MissingManifestResourceException"/> should be thrown when a resource
        /// .resx file or compiled manifest is not found even in the neutral culture.
        /// <br/>Default value: <c>true</c>.
        /// </summary>
        public bool ThrowException
        {
            get { return throwException; }
            set
            {
                if (value == throwException)
                    return;

                lock (SyncRoot)
                {
                    resxResources.ThrowException = value;
                    throwException = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the relative path to .resx resource files.
        /// <br/>Default value: <c>Resources</c>
        /// </summary>
        // TODO: desc: can throw argex, though not throwing it does not guarantee that it will work
        public string ResXResourcesDir
        {
            get { return resxResources.ResXResourcesDir; }
            set { resxResources.ResXResourcesDir = value; }
        }

        internal object SyncRoot => resxResources;

        /// <summary>
        /// Gets or sets the source, from which the resources should be taken.
        /// </summary>
        // TODO: doc: - by default, both. - when changed, resources are not cleared
        public virtual ResourceManagerSources Source
        {
            get { return source; }
            set
            {
                if (value == source)
                    return;

                if (!value.IsDefined())
                    throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));

                SetSource(value);
            }
        }

        /// <summary>
        /// Sets the source of the resources.
        /// Actually protected but should be visible only in this project.
        /// </summary>
        internal virtual void SetSource(ResourceManagerSources value)
        {
            lock (SyncRoot)
            {
                // nullifying the local resourceSets cache does not mean we clear the resources.
                // they will be obtained and property merged again on next Get...
                source = value;
                resourceSets = null;
                lastUsedResourceSet = default(KeyValuePair<string, ResourceSet>);
            }
        }

        /// <summary>
        /// Gets whether a non-proxy resource set is present for the specified culture.
        /// Actually protected but should be visible only in this project.
        /// </summary>
        internal bool IsNonProxyLoaded(CultureInfo culture)
        {
            lock (SyncRoot)
            {
                ResourceSet rs;
                return resourceSets != null && resourceSets.TryGetValue(culture.Name, out rs) && !(rs is ProxyResourceSet);
            }
        }

        /// <summary>
        /// Gets whether a proxy resource set is present for any culture.
        /// Actually protected but should be visible only in this project.
        /// </summary>
        internal bool IsAnyProxyLoaded()
        {
            lock (SyncRoot)
            {
                return resourceSets?.Values.Any(v => v is ProxyResourceSet) == true;
            }
        }

        /// <summary>
        /// Gets whether an expando resource set is present for the specified culture.
        /// Actually protected but should be visible only in this project.
        /// </summary>
        internal bool IsExpandoExists(CultureInfo culture)
        {
            lock (SyncRoot)
            {
                ResourceSet rs;
                return resourceSets != null && resourceSets.TryGetValue(culture.Name, out rs) && rs is IExpandoResourceSet;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the resource manager allows case-insensitive resource lookups in the
        /// <see cref="GetString(string)" /> and <see cref="GetObject(string)" /> methods.
        /// </summary>
        public override bool IgnoreCase
        {
            get { return base.IgnoreCase; }
            set
            {
                if (value == base.IgnoreCase)
                    return;

                lock (SyncRoot)
                {
                    resxResources.IgnoreCase = value;
                    base.IgnoreCase = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the <see cref="HybridResourceManager"/> works in safe mode. In safe mode the retrieved
        /// objects returned from .resx sources are not deserialized automatically. See Remarks section for details.
        /// <br/>Default value: <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>When <c>SafeMode</c> is <c>true</c>, the <see cref="GetObject(string)"/> and <see cref="GetMetaObject"/> methods
        /// return <see cref="ResXDataNode"/> instances instead of deserialized objects, if they are returned from .resx resource. You can retrieve the deserialized
        /// objects on demand by calling the <see cref="ResXDataNode.GetValue"/> method on the <see cref="ResXDataNode"/> instance.</para>
        /// <para>When <c>SafeMode</c> is <c>true</c>, the <see cref="GetString(string)"/> and <see cref="GetMetaString"/> methods
        /// will return a <see cref="string"/> for non-string objects, too, if they are from a .resx resource.
        /// For non-string elements the raw XML string value will be returned.</para>
        /// </remarks>
        /// <seealso cref="ResXResourceReader.SafeMode"/>
        /// <seealso cref="ResXResourceManager.SafeMode"/>
        /// <seealso cref="ResXResourceSet.SafeMode"/>
        public bool SafeMode
        {
            get { return safeMode; }
            set { safeMode = value; }
        }

        /// <summary>
        /// Gets the <see cref="CultureInfo"/> that is specified as neutral culture in the <see cref="Assembly"/>
        /// used to initialized this instance, or the <see cref="CultureInfo.InvariantCulture"/> if no such culture is defined.
        /// </summary>
        protected CultureInfo NeutralResourcesCulture => neutralResourcesCulture
            ?? (neutralResourcesCulture =
                (CultureInfo)Accessors.ResourceManager_neutralResourcesCulture.Get(this) ?? CultureInfo.InvariantCulture);

        /// <summary>
        /// Creates a new instance of <see cref="HybridResourceManager"/> class that looks up resources in
        /// satellite assemblies and resource XML files based on information from the specified <paramref name="baseName"/>
        /// and <paramref name="assembly"/>.
        /// </summary>
        /// <param name="baseName">A root name that is the prefix of the resource files without the extension.</param>
        /// <param name="assembly">The main assembly for the resources. The compiled resource will be searched in this assembly.</param>
        /// <param name="explicitResXBaseFileName">When <see langword="null"/>, .resx file name will be constructed based on the
        /// <paramref name="baseName"/> parameter; otherwise, the given <see cref="string"/> will be used. This parameter is optional.</param>
        public HybridResourceManager(string baseName, Assembly assembly, string explicitResXBaseFileName = null)
            : base(baseName, assembly)
        {
            // base will set MainAssembly and BaseNameField directly
            resxResources = new ResXResourceManager(explicitResXBaseFileName ?? baseName, assembly);
#if NET35
            // .NET 3.5 sets _neutralResourcesCulture in its InternalGetResourceSet only so setting the field here.
            Accessors.ResourceManager_neutralResourcesCulture.Set(this, GetNeutralResourcesLanguage(assembly));
#endif // elif not needed because this will not be needed in newer versions
        }

        /// <summary>
        /// Creates a new instance of <see cref="HybridResourceManager"/> class that looks up resources in
        /// satellite assemblies and resource XML files based on information from the specified type object.
        /// </summary>
        /// <param name="resourceSource">A type from which the resource manager derives all information for finding resource files.</param>
        /// <param name="explicitResXBaseFileName">When <see langword="null"/>, .resx file name will be constructed based on the
        /// <paramref name="resourceSource"/> parameter; otherwise, the given <see cref="string"/> will be used.</param>
        public HybridResourceManager(Type resourceSource, string explicitResXBaseFileName = null)
            : base(resourceSource)
        {
            // the base ctor will set MainAssembly and BaseNameField by the provided type
            // resx will be searched in dynamicResourcesDir\explicitResxBaseFileName[.Culture].resx
            resxResources = explicitResXBaseFileName == null
                ? new ResXResourceManager(resourceSource)
                : new ResXResourceManager(explicitResXBaseFileName, resourceSource.Assembly);
#if NET35
            // .NET 3.5 sets _neutralResourcesCulture in its InternalGetResourceSet only so setting the field here.
            Accessors.ResourceManager_neutralResourcesCulture.Set(this, GetNeutralResourcesLanguage(resourceSource.Assembly));
#endif // elif not needed because this will not be needed in newer versions
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
        /// Returns the value of the specified resource.
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
        /// Gets the value of the specified resource localized for the specified <paramref name="culture"/>.
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

        private object GetObjectInternal(string name, CultureInfo culture, bool isString)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name), Res.Get(Res.ArgumentNull));

            if (culture == null)
                culture = CultureInfo.CurrentUICulture;
            object value;
            ResourceSet seen = Unwrap(TryGetFromCachedResourceSet(name, culture, isString, out value));

            // There is a result, or a stored null is returned from invariant
            if (value != null
                || (seen != null
                    && (Equals(culture, CultureInfo.InvariantCulture) || seen is ProxyResourceSet && GetWrappedCulture(seen).Equals(CultureInfo.InvariantCulture))
                    && (Unwrap(seen) as IExpandoResourceSet)?.ContainsResource(name, IgnoreCase) == true))
            {
                return value;
            }

            // The InternalGetResourceSet has also a hierarchy traversal. This outer traversal is required as well because
            // the inner one can return an existing resource set without the searched resource, in which case here is
            // the fallback to the parent resource.
            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, NeutralResourcesCulture, true);
            ResourceSet toCache = null;
            foreach (CultureInfo currentCulture in mgr)
            {
                ResourceSet rs = InternalGetResourceSet(currentCulture, ResourceSetRetrieval.LoadIfExists, true, false);
                if (rs == null)
                    return null;

                // we have already checked this resource
                var unwrapped = Unwrap(rs);
                if (unwrapped == seen)
                    continue;

                if (toCache == null)
                    toCache = rs;

                value = GetResourceFromAny(unwrapped, name, isString);
                if (value != null)
                {
                    lock (SyncRoot)
                    {
                        lastUsedResourceSet = new KeyValuePair<string, ResourceSet>(culture.Name, toCache);
                    }

                    return value;
                }

                seen = unwrapped;
            }

            return null;
        }

        /// <summary>
        /// Actually should be protected AND internal...
        /// Warning: it CAN return a proxy
        /// </summary>
        internal ResourceSet TryGetFromCachedResourceSet(string name, CultureInfo culture, bool isString, out object value)
        {
            ResourceSet cachedRs = GetFirstResourceSet(culture);
            if (cachedRs == null)
            {
                value = null;
                return null;
            }

            value = GetResourceFromAny(cachedRs, name, isString);
            return cachedRs;
        }

        /// <summary>
        /// Updates last used ResourceSet
        /// Actually should be protected AND internal...
        /// </summary>
        internal void SetCache(CultureInfo culture, ResourceSet rs)
        {
            lock (SyncRoot)
            {
                lastUsedResourceSet = new KeyValuePair<string, ResourceSet>(culture.Name, rs);
            }
        }

        /// <summary>
        /// Actually should be protected AND internal...
        /// </summary>
        internal object GetResourceFromAny(ResourceSet rs, string name, bool isString)
        {
            ResourceSet realRs = Unwrap(rs);
            var expandoRs = realRs as IExpandoResourceSetInternal;
            return expandoRs != null
                ? expandoRs.GetResource(name, IgnoreCase, isString, safeMode)
                : (isString ? realRs.GetString(name, IgnoreCase) : realRs.GetObject(name, IgnoreCase));
        }

        /// <summary>
        /// Tries to get the first resource set in the traversal hierarchy,
        /// so the resource set for the culture itself.
        /// Warning: it CAN return a proxy
        /// </summary>
        private ResourceSet GetFirstResourceSet(CultureInfo culture)
        {
            lock (SyncRoot)
            {
                ResourceSet rs;
                ProxyResourceSet proxy;
                if (culture.Name == lastUsedResourceSet.Key)
                {
                    proxy = (rs = lastUsedResourceSet.Value) as ProxyResourceSet;
                    if (proxy == null)
                        return rs;

                    if (IsCachedProxyAccepted(proxy, culture))
                        return rs;

                    Debug.Assert(resourceSets.TryGetValue(culture.Name, out rs) && rs == proxy, "Inconsistent value in cache");
                    return null;
                }

                // Look in the ResourceSet table
                var localResourceSets = resourceSets;

                if (localResourceSets == null || !localResourceSets.TryGetValue(culture.Name, out rs))
                    return null;

                if ((proxy = rs as ProxyResourceSet) != null && !IsCachedProxyAccepted(proxy, culture))
                    return null;

                // update the cache with the most recent ResourceSet
                lastUsedResourceSet = new KeyValuePair<string, ResourceSet>(culture.Name, rs);
                return rs;
            }
        }

        /// <summary>
        /// Gets whether a cached proxy
        /// Actually should be protected AND internal...
        /// </summary>
        /// <param name="proxy">The found proxy</param>
        /// <param name="culture">The requested culture</param>
        internal virtual bool IsCachedProxyAccepted(ResourceSet proxy, CultureInfo culture)
        {
            // In HRM proxy is accepted only if hierarchy is loaded. This is ok because GetFirstResourceSet is called only from
            // methods, which call InternalGetResourceSet with LoadIfExists
            return ((ProxyResourceSet)proxy).HierarchyLoaded;
        }

        internal CultureInfo GetWrappedCulture(ResourceSet proxy)
        {
            Debug.Assert(proxy is ProxyResourceSet);
            return ((ProxyResourceSet)proxy).WrappedCulture;
        }

        /// <summary>
        /// Actually should be protected AND internal...
        /// </summary>
        internal static ResourceSet Unwrap(ResourceSet rs)
        {
            if (rs == null)
                return null;

            ProxyResourceSet proxy = rs as ProxyResourceSet;
            if (proxy != null)
                return proxy.WrappedResourceSet;

            return rs;
        }

        /// <summary>
        /// Actually should be protected AND internal...
        /// </summary>
        internal static bool IsProxy(ResourceSet rs) => rs is ProxyResourceSet;

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
            // base implementation must not be called because it wants to open main assembly in case of invariant culture
            ResourceSet result = Unwrap(InternalGetResourceSet(culture, loadIfExists ? ResourceSetRetrieval.LoadIfExists : ResourceSetRetrieval.GetIfAlreadyLoaded, tryParents, false));

            // this.SafeMode is not used to set the SafeMode of the wrapped ResourceSets when resources are accessed so setting it only when the user obtains a ResX/HybridResourceSet instance.
            IExpandoResourceSetInternal expandoRs = result as IExpandoResourceSetInternal;
            if (expandoRs != null)
                expandoRs.SafeMode = safeMode;

            return result;
        }

        /// <summary>
        /// Provides the implementation for finding a resource set.
        /// </summary>
        /// <param name="culture">The culture object to look for.</param>
        /// <param name="loadIfExists">true to load the resource set, if it has not been loaded yet; otherwise, false.</param>
        /// <param name="tryParents">true to check parent <see cref="CultureInfo" /> objects if the resource set cannot be loaded; otherwise, false.</param>
        /// <returns>
        /// The specified resource set.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="MissingManifestResourceException">The .resx file of the neutral culture was not found, while <paramref name="tryParents"/> and <see cref="ThrowException"/> are both <c>true</c>.</exception>
        protected override ResourceSet InternalGetResourceSet(CultureInfo culture, bool loadIfExists, bool tryParents)
        {
            Debug.Assert(Assembly.GetCallingAssembly() != Assembly.GetExecutingAssembly(), "InternalGetResourceSet is called from Libraries assembly.");
            return Unwrap(InternalGetResourceSet(culture, loadIfExists ? ResourceSetRetrieval.LoadIfExists : ResourceSetRetrieval.GetIfAlreadyLoaded, tryParents, false));
        }

        /// <summary>
        /// Warning: It CAN return a proxy
        /// </summary>
        internal ResourceSet InternalGetResourceSet(CultureInfo culture, ResourceSetRetrieval behavior, bool tryParents, bool forceExpandoResult)
        {
            Debug.Assert(forceExpandoResult || behavior != ResourceSetRetrieval.CreateIfNotExists,
                "Behavior can be CreateIfNotExists only if expando is requested.");

            if (culture == null)
                throw new ArgumentNullException(nameof(culture), Res.Get(Res.ArgumentNull));

            ResourceSet result = null;
            bool resourceFound;
            lock (SyncRoot)
            {
                resourceFound = resourceSets?.TryGetValue(culture.Name, out result) == true;
            }

            ProxyResourceSet proxy;
            if (resourceFound)
            {
                // returning the cached resource if that is not a proxy or when the proxy does not have to be (possibly) replaced
                // if result is not a proxy but an actual resource...
                if ((((proxy = result as ProxyResourceSet) == null)
                       // ...or is a proxy but nothing new should be loaded...
                       || behavior == ResourceSetRetrieval.GetIfAlreadyLoaded
                       // ...or is a proxy but nothing new can be loaded in the hierarchy
                       || (behavior == ResourceSetRetrieval.LoadIfExists && proxy.HierarchyLoaded))
                    // AND not expando is requested or result (wraps) an expando.
                    // Due to the conditions above it will not return proxied expando with Create behavior.
                    && (!forceExpandoResult || Unwrap(result) is IExpandoResourceSet))
                {
                    return result;
                }
            }

            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, NeutralResourcesCulture, tryParents);
            CultureInfo foundCultureToAdd = null;
            CultureInfo foundProxyCulture = null;
            foreach (CultureInfo currentCultureInfo in mgr)
            {
                proxy = null;
                lock (SyncRoot)
                {
                    resourceFound = resourceSets?.TryGetValue(currentCultureInfo.Name, out result) == true;
                }

                if (resourceFound)
                {
                    // a returnable result (non-proxy) is found in the local cache
                    proxy = result as ProxyResourceSet;
                    if (proxy == null && (!forceExpandoResult || result is IExpandoResourceSet))
                    {
                        // since the first try above we have a result from another thread for the searched culture
                        if (Equals(culture, currentCultureInfo))
                            return result;

                        // after some proxies, a parent culture has been found: returning a proxy for this if this was the proxied culture in the children
                        if (Equals(currentCultureInfo, foundProxyCulture))
                        {
                            // The hierarchy is now up-to-date. Creating the possible missing proxies and returning the one for the requested culture
                            lock (SyncRoot)
                            {
                                ResourceSet toWrap = result;
                                result = null;
                                foreach (CultureInfo updateCultureInfo in mgr)
                                {
                                    // We have found again the first proxy in the hierarchy. This is now up-to-date for sure so returning.
                                    ResourceSet rs;
                                    if (resourceSets.TryGetValue(updateCultureInfo.Name, out rs))
                                    {
                                        Debug.Assert(rs is ProxyResourceSet, "A proxy is expected to be found here.");
                                        return result ?? rs;
                                    }
                                    // There is at least one non-existing key (most specific elements in the hierarchy): new proxy creation is needed
                                    else
                                    {
                                        ResourceSet newProxy = new ProxyResourceSet(toWrap, foundProxyCulture, behavior == ResourceSetRetrieval.LoadIfExists);
                                        AddResourceSet(updateCultureInfo.Name, ref newProxy);
                                        if (result == null)
                                            result = newProxy;
                                    }
                                }
                            }
                        }

                        // otherwise, we found a parent: we need to re-create the proxies in the cache to the children
                        Debug.Assert(foundProxyCulture == null, "There is a proxy with an inconsistent parent in the hierarchy.");
                        foundCultureToAdd = currentCultureInfo;
                        break;
                    }

                    // proxy is found
                    if (proxy != null)
                    {
                        Debug.Assert(foundProxyCulture == null || Equals(foundProxyCulture, proxy.WrappedCulture), "Proxied cultures are different in the hierarchy.");
                        if (foundProxyCulture == null)
                            foundProxyCulture = proxy.WrappedCulture;

                        // if we traversing here because last time the proxy has been loaded by
                        // ResourceSetRetrieval.GetIfAlreadyLoaded, but now we load the possible parents, we set the 
                        // HierarchyLoaded flag in the hierarchy. Unless no new proxy is created (and thus the descendant proxies are deleted),
                        // this will prevent the redundant traversal next time.
                        if (tryParents && behavior == ResourceSetRetrieval.LoadIfExists)
                        {
                            proxy.HierarchyLoaded = true;
                        }
                    }

                    // if none of above, we have a non-proxy result, which must be replaced by an expando result
                }

                result = null;

                // from resx
                ResXResourceSet resx = null;
                if (source != ResourceManagerSources.CompiledOnly)
                    resx = resxResources.GetResXResourceSet(currentCultureInfo, behavior, false);

                // from assemblies
                ResourceSet compiled = null;
                if (source != ResourceManagerSources.ResXOnly)
                {
                    // otherwise, disposed state is checked by ResXResourceSet
                    if (source == ResourceManagerSources.CompiledOnly && resxResources.IsDisposed())
                        throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                    compiled = base.InternalGetResourceSet(currentCultureInfo, behavior != ResourceSetRetrieval.GetIfAlreadyLoaded, false);
                }

                // result found
                if (resx != null || compiled != null)
                {
                    foundCultureToAdd = currentCultureInfo;

                    // single result - no merge is needed, unless expando is forced and result is compiled
                    if (resx == null || compiled == null)
                    {
                        result = resx ?? compiled;

                        if (!forceExpandoResult)
                            break;

                        if (resx == null)
                        {
                            // there IS a result, which we cannot return because it is not expando, so returning null
                            // this is ok because neither fallback nor missing manifest is needed in this case - GetExpandoResourceSet returns null instead of ResourceSet
                            if (source == ResourceManagerSources.CompiledOnly)
                                return null;

                            resx = resxResources.CreateResourceSet(culture);
                        }
                    }

                    // creating a merged resource set (merge is applied only when enumerated)
                    if (resx != null && compiled != null)
                        result = new HybridResourceSet(resx, compiled);

                    break;
                }

                // behavior is LoadIfExists, tryParents = false, hierarchy is not loaded, but still no loadable content has been found
                if (!tryParents && proxy != null)
                {
                    Debug.Assert(behavior == ResourceSetRetrieval.LoadIfExists && !proxy.HierarchyLoaded);
                    return proxy.WrappedResourceSet;
                }
            }

            // The code above never throws MissingManifest exception because the wrapped managers are always called with tryParents=false.
            // This block ensures compatible behavior if ThrowException is enabled.
            if (throwException && result == null && tryParents)
            {
                bool raiseException = behavior != ResourceSetRetrieval.GetIfAlreadyLoaded;

                // if behavior is not GetIfAlreadyLoaded, we cannot decide whether manifest is really missing so calling the methods
                // again and ignoring any result just catching the exception if any. Calling with createIfNotExists=false,
                // so no new resources will be loaded. Because of tryParents=true, exception will be thrown if manifest is missing.
                if (!raiseException)
                {
                    try
                    {
                        resxResources.GetResXResourceSet(CultureInfo.InvariantCulture, ResourceSetRetrieval.GetIfAlreadyLoaded, true);
                        base.InternalGetResourceSet(CultureInfo.InvariantCulture, false, true);
                    }
                    catch (MissingManifestResourceException)
                    {
                        raiseException = true;
                    }
                }

                if (raiseException)
                {
                    switch (source)
                    {
                        case ResourceManagerSources.CompiledOnly:
                            throw new MissingManifestResourceException(Res.Get(Res.NeutralResourceNotFoundCompiled, BaseNameField, MainAssembly.Location));
                        case ResourceManagerSources.ResXOnly:
                            throw new MissingManifestResourceException(Res.Get(Res.NeutralResourceFileNotFoundResX, resxResources.ResourceFileName));
                        default:
                            throw new MissingManifestResourceException(Res.Get(Res.NeutralResourceNotFoundHybrid, BaseNameField, MainAssembly.Location, resxResources.ResourceFileName));
                    }
                }
            }

            if (foundCultureToAdd == null)
                return null;

            lock (SyncRoot)
            {
                ResourceSet toReturn = null;

                // we replace a proxy: we must delete proxies, which are children of the found resource.
                if (foundProxyCulture != null && resourceSets != null)
                {
                    Debug.Assert(forceExpandoResult || !Equals(foundProxyCulture, foundCultureToAdd), "The culture to add is the same as the existing proxies, while no expando result is forced.");

                    List<string> keysToRemove = resourceSets.Where(item =>
                                item.Value is ProxyResourceSet && ResXResourceManager.IsParentCulture(foundCultureToAdd, item.Key))
                        .Select(item => item.Key).ToList();

                    foreach (string key in keysToRemove)
                    {
                        resourceSets.Remove(key);
                    }
                }

                // add entries to the cache for the cultures we have gone through
                foreach (CultureInfo updateCultureInfo in mgr)
                {
                    // stop when we've added current or reached invariant (top of chain)
                    if (ReferenceEquals(updateCultureInfo, foundCultureToAdd))
                    {
                        AddResourceSet(updateCultureInfo.Name, ref result);
                        if (toReturn == null)
                            toReturn = result;
                        break;
                    }

                    ResourceSet newProxy = new ProxyResourceSet(result, foundCultureToAdd, behavior == ResourceSetRetrieval.LoadIfExists);
                    AddResourceSet(updateCultureInfo.Name, ref newProxy);
                    if (toReturn == null)
                        toReturn = newProxy;
                }

                return toReturn;
            }
        }

        private void AddResourceSet(string cultureName, ref ResourceSet rs)
        {
            // InternalGetResourceSet is both recursive and reentrant - 
            // assembly load callbacks in particular are a way we can call
            // back into the ResourceManager in unexpectedly on the same thread.
            ResourceSet lostRace;
            if (resourceSets == null)
            {
                resourceSets = new Dictionary<string, ResourceSet> { { cultureName, rs } };
            }
            else if (resourceSets.TryGetValue(cultureName, out lostRace))
            {
                if (!ReferenceEquals(lostRace, rs))
                {
                    // Note: In certain cases, we can try to add a ResourceSet for multiple
                    // cultures on one thread, while a second thread added another ResourceSet for one
                    // of those cultures.  So when we lose the race, we must make sure our ResourceSet 
                    // isn't in our dictionary before closing it.
                    // But if a proxy or non-hybrid resource is already in the cache, we replace that.
                    if (lostRace is ProxyResourceSet && !(rs is ProxyResourceSet)
                        || !(lostRace is HybridResourceSet) && rs is HybridResourceSet)
                    {
                        resourceSets[cultureName] = rs;
                    }
                    else
                    {
                        // we do not dispose the replaced resource set because this manager is just a wrapper, such
                        // conflicting resources sets should be disposed by base or the resx manager.
                        rs = lostRace;
                    }
                }
            }
            else
            {
                resourceSets.Add(cultureName, rs);
            }

            lastUsedResourceSet = default(KeyValuePair<string, ResourceSet>);
        }

        /// <summary>
        /// Tells the resource manager to call the <see cref="ResourceSet.Close" /> method on all <see cref="ResourceSet" /> objects and release all resources.
        /// All unsaved resources will be lost.
        /// </summary>
        public override void ReleaseAllResources()
        {
            // we are in lock; otherwise, the nullification could occur while we access the resourceSets anywhere else
            lock (SyncRoot)
            {
                resourceSets = null;
                lastUsedResourceSet = default(KeyValuePair<string, ResourceSet>);
                resxResources.ReleaseAllResources();
                base.ReleaseAllResources();
            }
        }

        private object GetMetaInternal(string name, CultureInfo culture, bool isString)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name), Res.Get(Res.ArgumentNull));

            if (source == ResourceManagerSources.CompiledOnly)
                return null;

            // in case of metadata there is no hierarchy traversal
            IExpandoResourceSetInternal rs = Unwrap(InternalGetResourceSet(culture ?? CultureInfo.InvariantCulture, ResourceSetRetrieval.LoadIfExists, false, false)) as IExpandoResourceSetInternal;
            return rs?.GetMeta(name, IgnoreCase, isString, safeMode);
        }

        #region IExpandoResourceManager Members

        /// <summary>
        /// Gets whether this <see cref="HybridResourceManager"/> instance has modified and unsaved data.
        /// </summary>
        public bool IsModified
        {
            get { return source != ResourceManagerSources.CompiledOnly && resxResources.IsModified; }
        }

        /// <summary>
        /// Returns the value of the string metadata for the specified culture.
        /// </summary>
        /// <param name="name">The name of the metadata to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the metadata should be returned.
        /// If this value is <see langword="null" />, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture" /> property.
        /// Unlike in case of <see cref="GetString(string,CultureInfo)" /> method, no fallback is used if the metadata is not found in the specified culture.</param>
        /// <returns>
        /// The value of the metadata of the specified culture, or <see langword="null" /> if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual string GetMetaString(string name, CultureInfo culture = null)
        {
            return (string)GetMetaInternal(name, culture, true);
        }

        /// <summary>
        /// Returns the value of the specified non-string metadata for the specified culture.
        /// </summary>
        /// <param name="name">The name of the metadata to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the metadata should be returned.
        /// If this value is <see langword="null" />, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture" /> property.
        /// Unlike in case of <see cref="GetObject(string,CultureInfo)" /> method, no fallback is used if the metadata is not found in the specified culture.</param>
        /// <returns>
        /// The value of the metadata of the specified culture, or <see langword="null" /> if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual object GetMetaObject(string name, CultureInfo culture = null)
        {
            return GetMetaInternal(name, culture, false);
        }

        /// <summary>
        /// Retrieves the resource set for a particular culture, which can be dynamically modified.
        /// </summary>
        /// <param name="culture">The culture whose resources are to be retrieved.</param>
        /// <param name="behavior">Determines the retrieval behavior of the result <see cref="IExpandoResourceSet"/>.
        /// <br/>Default value: <see cref="ResourceSetRetrieval.CreateIfNotExists"/>.</param>
        /// <param name="tryParents"><c>true</c> to use resource fallback to load an appropriate resource if the resource set cannot be found; <c>false</c> to bypass the resource fallback process.
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns>The resource set for the specified culture, or <see langeword="null"/> if the specified culture cannot be retrieved by the defined <paramref name="behavior"/>,
        /// or when <see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>so it cannot return an <see cref="IExpandoResourceSet"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="behavior"/> does not fall in the expected range.</exception>
        /// <exception cref="MissingManifestResourceException">Resource file of the neutral culture was not found, while <paramref name="tryParents"/> is <c>true</c>
        /// and <paramref name="behavior"/> is not <see cref="ResourceSetRetrieval.CreateIfNotExists"/>.</exception>
        public virtual IExpandoResourceSet GetExpandoResourceSet(CultureInfo culture, ResourceSetRetrieval behavior = ResourceSetRetrieval.LoadIfExists, bool tryParents = false)
        {
            if (!Enum<ResourceSetRetrieval>.IsDefined(behavior))
                throw new ArgumentOutOfRangeException(nameof(behavior), Res.Get(Res.ArgumentOutOfRange));

            IExpandoResourceSet result = Unwrap(InternalGetResourceSet(culture, behavior, tryParents, true)) as IExpandoResourceSet;
            if (result != null)
                result.SafeMode = safeMode;

            return result;
        }

        /// <summary>
        /// Adds or replaces a resource object in the current <see cref="HybridResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The name of the resource to set.</param>
        /// <param name="culture">The culture of the resource to set. If this value is <see langword="null"/>,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture" /> property.</param>
        /// <param name="value">The value of the resource to set. If <see langword="null"/>, then a null reference will be explicitly
        /// stored for the specified <paramref name="culture"/>.</param>
        /// <remarks>
        /// <para>If <paramref name="value" /> is <see langword="null" />, a null reference will be explicitly stored.
        /// Its effect is similar to the <see cref="RemoveObject"/> method: the subsequent <see cref="GetObject(string, CultureInfo)" /> calls
        /// with the same <paramref name="culture" /> will fall back to the parent culture, or will return <see langword="null" /> if
        /// <paramref name="name" /> is not found in any parent cultures. However, enumerating the result set returned by
        /// <see cref="GetExpandoResourceSet"/> and <see cref="GetResourceSet"/> methods will return the resources with
        /// <see langword="null" /> value.</para>
        /// <para>If you want to remove the user-defined ResX content and reset the original resource defined in the binary resource set (if any), use the <see cref="RemoveObject"/> method.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException"><see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        public virtual void SetObject(string name, object value, CultureInfo culture = null)
        {
            if (source == ResourceManagerSources.CompiledOnly)
                throw new InvalidOperationException(Res.HybridResSourceBinary);

            // because of create no proxy is returned
            IExpandoResourceSet rs = (IExpandoResourceSet)InternalGetResourceSet(culture ?? CultureInfo.CurrentUICulture, ResourceSetRetrieval.CreateIfNotExists, false, true);
            rs.SetObject(name, value);
        }

        /// <summary>
        /// Removes a resource object from the current <see cref="HybridResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The case-sensitive name of the resource to remove.</param>
        /// <param name="culture">The culture of the resource to remove. If this value is <see langword="null"/>,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture" /> property.</param>
        /// <remarks>
        /// <para>If there is a binary resource defined for <paramref name="name" /> and <paramref name="culture" />,
        /// then after this call the originally defined value will be returned by <see cref="GetObject(string,CultureInfo)" /> method from the binary resources.
        /// If you want to force hiding the binary resource and make <see cref="GetObject(string,CultureInfo)" /> to default to the parent <see cref="CultureInfo" /> of the specified <paramref name="culture" />,
        /// then use the <see cref="SetObject" /> method with a <see langword="null" /> value.</para>
        /// <para><paramref name="name"/> is considered as case-sensitive. If <paramref name="name"/> occurs multiple times
        /// in the resource set in case-insensitive manner, they can be removed one by one only.</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException"><see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        public virtual void RemoveObject(string name, CultureInfo culture = null)
        {
            if (source == ResourceManagerSources.CompiledOnly)
                throw new InvalidOperationException(Res.HybridResSourceBinary);

            // forcing expando result is not needed because there is nothing to remove from compiled resources
            IExpandoResourceSet rs = Unwrap(InternalGetResourceSet(culture ?? CultureInfo.CurrentUICulture, ResourceSetRetrieval.LoadIfExists, false, false)) as IExpandoResourceSet;
            rs?.RemoveObject(name);
        }

        /// <summary>
        /// Adds or replaces a metadata object in the current <see cref="HybridResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The name of the metadata to set.</param>
        /// <param name="value">The value of the metadata to set. If <see langword="null" />,  then a null reference will be explicitly
        /// stored for the specified <paramref name="culture" />.</param>
        /// <param name="culture">The culture of the metadata to set.
        /// If this value is <see langword="null" />, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture" /> property.</param>
        /// <remarks>
        /// If <paramref name="value" /> is <see langword="null" />, a null reference will be explicitly stored.
        /// Its effect is similar to the <see cref="RemoveMetaObject" /> method: the subsequent <see cref="GetMetaObject" /> calls
        /// with the same <paramref name="culture" /> will return <see langword="null" />.
        /// However, enumerating the result set returned by <see cref="GetExpandoResourceSet" /> method will return the meta objects with <see langword="null" /> value.
        /// </remarks>
        /// <exception cref="InvalidOperationException"><see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        public virtual void SetMetaObject(string name, object value, CultureInfo culture = null)
        {
            if (source == ResourceManagerSources.CompiledOnly)
                throw new InvalidOperationException(Res.HybridResSourceBinary);

            // because of create no proxy is returned
            IExpandoResourceSet rs = (IExpandoResourceSet)InternalGetResourceSet(culture ?? CultureInfo.InvariantCulture, ResourceSetRetrieval.CreateIfNotExists, false, true);
            rs.SetMetaObject(name, value);
        }

        /// <summary>
        /// Removes a metadata object from the current <see cref="HybridResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The case-sensitive name of the metadata to remove.</param>
        /// <param name="culture">The culture of the metadata to remove.
        /// If this value is <see langword="null" />, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture" /> property.</param>
        /// <remarks>
        /// <paramref name="name" /> is considered as case-sensitive. If <paramref name="name" /> occurs multiple times
        /// in the resource set in case-insensitive manner, they can be removed one by one only.
        /// </remarks>
        /// <exception cref="InvalidOperationException"><see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        public virtual void RemoveMetaObject(string name, CultureInfo culture = null)
        {
            if (source == ResourceManagerSources.CompiledOnly)
                throw new InvalidOperationException(Res.HybridResSourceBinary);

            // forcing expando result is not needed because there is nothing to remove from compiled resources
            IExpandoResourceSet rs = Unwrap(InternalGetResourceSet(culture ?? CultureInfo.InvariantCulture, ResourceSetRetrieval.LoadIfExists, false, false)) as IExpandoResourceSet;
            rs?.RemoveObject(name);
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
        public virtual bool SaveResourceSet(CultureInfo culture, bool force = false, bool compatibleFormat = false)
        {
            if (source == ResourceManagerSources.CompiledOnly)
                return false;

            return resxResources.SaveResourceSet(culture, force, compatibleFormat);
        }

        /// <summary>
        /// Saves all already loaded resources.
        /// </summary>
        /// <param name="force"><c>true</c> to save all of the already loaded resource sets regardless if they have been modified; <c>false</c> to save only the modified resource sets.
        /// <br/>Default value: <c>false</c>.</param>
        /// <param name="compatibleFormat">If set to <c>true</c>, the result .resx files can be read by the system <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx">ResXResourceReader</a> class
        /// and the Visual Studio Resource Editor. If set to <c>false</c>, the result .resx files are often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />), but the result can be read only by <see cref="ResXResourceReader" />
        /// <br/>Default value: <c>false</c>.</param>
        /// <returns><c>true</c> if at least one resource set has been saved; otherwise, <c>false</c>.</returns>
        /// <exception cref="IOException">A resource set could not be saved.</exception>
        public virtual bool SaveAllResources(bool force = false, bool compatibleFormat = false)
        {
            if (source == ResourceManagerSources.CompiledOnly)
                return false;

            return resxResources.SaveAllResources(force, compatibleFormat);
        }

        #endregion

        /// <summary>
        /// Disposes the resources of the current instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            IDictionary compiledResources = CompiledResourceSets;
            if (compiledResources == null)
                return;

            if (disposing)
            {
                resxResources.Dispose();

                // this enumerates both Hashtable and Dictionary the same way.
                // The nongeneric enumerator is not a problem, values must be cast anyway.
                IDictionaryEnumerator enumerator = compiledResources.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    ((ResourceSet)enumerator.Value).Dispose();
                }
            }

            CompiledResourceSets = null;
            resourceSets = null;
            lastUsedResourceSet = default(KeyValuePair<string, ResourceSet>);
            neutralResourcesCulture = null;
        }

#if NET35
        private Hashtable CompiledResourceSets
        {
            get { return base.ResourceSets; }
            set { base.ResourceSets = value; }
        }

#elif NET40 || NET45
        private new Dictionary<string, ResourceSet> CompiledResourceSets
        {
            get { return (Dictionary<string, ResourceSet>)Accessors.ResourceManager_resourceSets.Get(this); }
            set { Accessors.ResourceManager_resourceSets.Set(this, value); }
        }

#else
#error .NET version is not set or not supported!
#endif
    }
}
