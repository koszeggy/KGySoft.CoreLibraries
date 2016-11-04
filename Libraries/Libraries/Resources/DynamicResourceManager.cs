using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using KGySoft.Libraries.Reflection;

namespace KGySoft.Libraries.Resources
{
    /// <summary>
    /// Represents a resource manager that provides convenient access to culture-specific resources at run time.
    /// As it is derived from <see cref="HybridResourceManager"/>, it can handle both compiled resources from <c>.dll</c> and
    /// <c>.exe</c> files, and <c>.resx</c> files at the same time. Based on the selected strategies, when a resource
    /// is not found in a language, it can automatically expand the resource with the text of the neutral resource,
    /// which can be saved into <c>.resx</c> files.
    /// </summary>
    /// <remarks></remarks>
    // TODO: - Már az õs legyen disposable (meg a resx is).
    // TODO: Remarks: - Míg a ResxRM/HRM konkrét add metódusokat tartalmaz, addig a bõvítés itt automatikus, a stratégia property-kkel szabályozható. Itt a mentés is lehet automatikus, pl kilépésnél, ha éppen resx/mixed mód van.
    //                - Disposable, muszáj dispose-olni, mert static eventekre iratkozik fel
    [Serializable] // TODO: static eventekre feliratkozás az OnDeserialized-ben (az õs miatt nem nagyon lehet custom ISerializable)
    public class DynamicResourceManager : HybridResourceManager, IDisposable
    {
        private bool useLanguageSettings;
        private AutoSaveOptions autoSave = LanguageSettings.AutoSaveDefault;
        private AutoAppendOptions autoAppend = LanguageSettings.AutoAppendDefault;

        /// <summary>
        /// If <see cref="autoAppend"/> contains <see cref="AutoAppendOptions.AppendOnLoad"/> flag,
        /// contains the up-to-date cultures. Value is <c>true</c> if that culture is merged so it can be taken as a base for merge.
        /// </summary>
        private Dictionary<CultureInfo, bool> mergedCultures;

        /// <summary>
        /// Gets or sets whether values of <see cref="AutoAppend"/>, <see cref="AutoSave"/> and <see cref="Source"/> properties
        /// are centrally taken from the <see cref="LanguageSettings"/> class.
        /// <br/>
        /// Default value: <c>false</c>.
        /// </summary>
        /// <seealso cref="LanguageSettings.DynamicResourceManagersSource"/>
        /// <seealso cref="LanguageSettings.DynamicResourceManagersAutoAppend"/>
        /// <seealso cref="LanguageSettings.DynamicResourceManagersAutoSave"/>
        public bool UseLanguageSettings
        {
            get { return useLanguageSettings; }
            set
            {
                if (value == useLanguageSettings)
                    return;

                lock (SyncRoot)
                {                    
                    useLanguageSettings = value;
                    UnhookEvents();
                    if (value)
                    {
                        if (base.Source != LanguageSettings.DynamicResourceManagersSource)
                            SetSource(LanguageSettings.DynamicResourceManagersSource);
                        if (LanguageSettings.DynamicResourceManagersAutoAppend.IsWidening(autoAppend))
                            mergedCultures = null;
                    }
                    else if (autoAppend.IsWidening(LanguageSettings.DynamicResourceManagersAutoAppend))
                        mergedCultures = null;

                    HookEvents();
                }
            }
        }

        /// <summary>
        /// When <see cref="UseLanguageSettings"/> is <c>false</c>,
        /// gets or sets the auto saving options.
        /// When <see cref="UseLanguageSettings"/> is <c>true</c>,
        /// auto saving is controlled by <see cref="LanguageSettings.DynamicResourceManagersAutoSave">LanguageSettings.DynamicResourceManagersAutoSave</see> property.
        /// <br/>
        /// Default value: <see cref="AutoSaveOptions.LanguageChange"/>, <see cref="AutoSaveOptions.DomainUnload"/>, <see cref="AutoSaveOptions.SourceChange"/>
        /// </summary>
        public AutoSaveOptions AutoSave
        {
            get { return useLanguageSettings ? LanguageSettings.DynamicResourceManagersAutoSave : autoSave; }
            set
            {
                if (useLanguageSettings)
                    throw new InvalidOperationException(Res.Get(Res.InvalidDrmPropertyChange));

                if (autoSave == value)
                    return;

                if (!value.AllFlagsDefined())
                    throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));

                lock (SyncRoot)
                {
                    UnhookEvents();
                    autoSave = value;
                    HookEvents();
                }
            }
        }

        /// <summary>
        /// When <see cref="UseLanguageSettings"/> is <c>false</c>,
        /// gets or sets the resource auto append options.
        /// When <see cref="UseLanguageSettings"/> is <c>true</c>,
        /// auto appending of resources is controlled by <see cref="LanguageSettings.DynamicResourceManagersAutoAppend">LanguageSettings.DynamicResourceManagersAutoAppend</see> property.
        /// <br/>
        /// Default value: <see cref="AutoAppendOptions.AppendFirstNeutralCulture"/>, <see cref="AutoAppendOptions.AppendOnLoad"/>
        /// </summary>
        /// <remarks>
        /// <para>Auto appending affects the resources only. Meta data are never merged.</para>
        /// <para>Auto appending options are ignored if <see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/></para>
        /// </remarks>
        /// <seealso cref="AutoAppendOptions"/>
        public AutoAppendOptions AutoAppend
        {
            get { return useLanguageSettings ? LanguageSettings.DynamicResourceManagersAutoAppend : autoAppend; }
            set
            {
                if (useLanguageSettings)
                    throw new InvalidOperationException(Res.Get(Res.InvalidDrmPropertyChange));

                if (autoAppend == value)
                    return;

                value.CheckOptions();
                if (autoAppend.IsWidening(value))
                    mergedCultures = null;

                autoAppend = value;
            }
        }

        /// <summary>
        /// When <see cref="UseLanguageSettings"/> is <c>false</c>,
        /// gets or sets the source, from which the resources should be taken.
        /// When <see cref="UseLanguageSettings"/> is <c>true</c>,
        /// the source is controlled by <see cref="LanguageSettings.DynamicResourceManagersSource">LanguageSettings.DynamicResourceManagersSource</see> property.
        /// </summary>
        /// <seealso cref="UseLanguageSettings"/>
        /// <seealso cref="LanguageSettings.DynamicResourceManagersSource"/>
        /// <exception cref="InvalidOperationException">The property is set and <see cref="UseLanguageSettings"/> is <c>true</c>.</exception>
        public override ResourceManagerSources Source
        {
            get { return useLanguageSettings ? LanguageSettings.DynamicResourceManagersSource : base.Source; }
            set
            {
                if (useLanguageSettings)
                    throw new InvalidOperationException(Res.Get(Res.InvalidDrmPropertyChange));

                base.Source = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the .resx files should use a compatible format when the resources are
        /// automatically saved.
        /// <br/>
        /// Default value: <c>false</c>
        /// </summary>
        public bool CompatibleFormat { get; set; }

        internal override void SetSource(ResourceManagerSources value)
        {
            lock (SyncRoot)
            {
                OnSourceChanging();
                mergedCultures = null;
                base.SetSource(value);
            }
        }

        private void OnSourceChanging()
        {
            if ((AutoSave & AutoSaveOptions.SourceChange) != AutoSaveOptions.None)
                Save();
        }

        private void OnDomainUnload()
        {
            if ((AutoSave & AutoSaveOptions.DomainUnload) != AutoSaveOptions.None)
                Save();
        }

        private void OnLanguageChanged()
        {
            if ((AutoSave & AutoSaveOptions.LanguageChange) != AutoSaveOptions.None)
                Save();
        }

        private void Save()
        {
            SaveAllResources(compatibleFormat: CompatibleFormat);
        }

        private void HookEvents()
        {
            if (useLanguageSettings)
            {
                LanguageSettings.DynamicResourceManagersSourceChanged += LanguageSettings_DynamicResourceManagersSourceChanged;
                LanguageSettings.DynamicResourceManagersAutoSaveChanged += LanguageSettings_DynamicResourceManagersAutoSaveChanged;
            }

            if ((AutoSave & AutoSaveOptions.LanguageChange) != AutoSaveOptions.None)
                LanguageSettings.DisplayLanguageChanged += LanguageSettings_DisplayLanguageChanged;
            if ((AutoSave & AutoSaveOptions.DomainUnload) != AutoSaveOptions.None)
            {
                if (AppDomain.CurrentDomain.IsDefaultAppDomain())
                    AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                else
                    AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            }
        }

        private void UnhookEvents()
        {
            LanguageSettings.DynamicResourceManagersSourceChanged -= LanguageSettings_DynamicResourceManagersSourceChanged;
            LanguageSettings.DynamicResourceManagersAutoSaveChanged -= LanguageSettings_DynamicResourceManagersAutoSaveChanged;
            LanguageSettings.DisplayLanguageChanged -= LanguageSettings_DisplayLanguageChanged;
            AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.DomainUnload -= CurrentDomain_DomainUnload;
        }

        /// <summary>
        /// Creates a new instance of <see cref="DynamicResourceManager"/> class that looks up resources in
        /// satellite assemblies and resource XML files based on information from the specified <paramref name="baseName"/>
        /// and <paramref name="assembly"/>.
        /// </summary>
        /// <param name="baseName">A root name that is the prefix of the resource files without the extension.</param>
        /// <param name="assembly">The main assembly for the resources.</param>
        /// <param name="explicitResXBaseFileName">When <see langword="null"/>, .resx file name will be constructed based on the
        /// <paramref name="baseName"/> parameter; otherwise, the given <see cref="string"/> will be used. This parameter is optional.</param>
        public DynamicResourceManager(string baseName, Assembly assembly, string explicitResXBaseFileName = null)
            : base(baseName, assembly, explicitResXBaseFileName)
        {
            HookEvents();
        }

        /// <summary>
        /// Creates a new instance of <see cref="DynamicResourceManager"/> class that looks up resources in
        /// satellite assemblies and resource XML files based on information from the specified type object.
        /// </summary>
        /// <param name="resourceSource">A type from which the resource manager derives all information for finding resource files.</param>
        /// <param name="explicitResxBaseFileName">When <see langword="null"/>, .resx file name will be constructed based on the
        /// <paramref name="resourceSource"/> parameter; otherwise, the given <see cref="string"/> will be used.</param>
        public DynamicResourceManager(Type resourceSource, string explicitResxBaseFileName = null)
            : base(resourceSource, explicitResxBaseFileName)
        {
            HookEvents();
        }

        /// <summary>
        /// Disposes the resources of the current instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // TODO: to base, ahol pl. a resx kinullozása, rs-ek disposeolása stb. is megtörténik
        protected virtual void Dispose(bool disposing)
        {
            mergedCultures = null;
            UnhookEvents();
        }

        /// <summary>
        /// Gets the value of the specified resource localized for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <param name="culture">The culture for which the resource is localized. If the resource is not localized for this culture,
        /// the resource manager uses fallback rules to locate an appropriate resource. If this value is null,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="LanguageSettings.DisplayLanguage" /> property.</param>
        /// <returns>
        /// The value of the resource, localized for the specified culture. If an appropriate resource set exists
        /// but <paramref name="name" /> cannot be found, the method returns null.
        /// </returns>
        // TODO: returns/remarks: autoappend
        public override object GetObject(string name, CultureInfo culture)
        {
            if (null == name)
                throw new ArgumentNullException(nameof(name), Res.Get(Res.ArgumentNull));

            AdjustCulture(ref culture);
            if (!IsAppendPossible(culture))
                return base.GetObject(name, culture);

            return GetObjectWithAppend(name, culture, false);
        }

        /// <summary>
        /// Returns the value of the specified resource.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <returns>
        /// The value of the resource localized for the caller's current culture settings.
        /// If an appropriate resource set exists but <paramref name="name" /> cannot be found, the method returns null.
        /// </returns>
        public override object GetObject(string name)
        {
            return GetObject(name, null);
        }

        /// <summary>
        /// Returns the value of the string resource localized for the specified culture.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the resource is localized.</param>
        /// <returns>
        /// The value of the resource localized for the specified culture, or null if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        public override string GetString(string name, CultureInfo culture)
        {
            if (null == name)
                throw new ArgumentNullException(nameof(name), Res.Get(Res.ArgumentNull));

            AdjustCulture(ref culture);
            if (!IsAppendPossible(culture))
                return base.GetString(name, culture);

            return (string)GetObjectWithAppend(name, culture, true);
        }

        public override string GetString(string name)
        {
            return GetString(name, null);
        }

        // summary TODO: Unlike the protected InternalGetResourceSet, this gets an appended ResourceSet (and Expando, too)
        //         - appending is applied only if both loadIfExists and tryParents are true but no new resx resource set will be created. To create them use GetExpandoResourceSet with Create instead.
        //         - if no append is performed now due to the params, itt will not happen later for the retrieved resource set until a ReleaseAllResources call
        public override ResourceSet GetResourceSet(CultureInfo culture, bool loadIfExists, bool tryParents)
        {
            if (culture == null)
                throw new ArgumentNullException(nameof(culture), Res.Get(Res.ArgumentNull));

            if (loadIfExists && tryParents && IsAppendPossible(culture))
                EnsureMerged(culture, ResourceSetRetrieval.LoadIfExists);

            return base.GetResourceSet(culture, loadIfExists, tryParents);
        }

        // summary TODO: Unlike the protected InternalGetResourceSet, this gets an appended ResourceSet (and GetResourceSet, too)
        //         - appending is applied only if behavior is load/create and tryParents is true. If behavior is only load, then no new resx resource set will be created.
        //         - if no append is performed now due to the params, itt will not happen later for the retrieved resource set until a ReleaseAllResources call
        public override IExpandoResourceSet GetExpandoResourceSet(CultureInfo culture, ResourceSetRetrieval behavior = ResourceSetRetrieval.LoadIfExists, bool tryParents = false)
        {
            if (culture == null)
                throw new ArgumentNullException(nameof(culture), Res.Get(Res.ArgumentNull));

            if (!Enum<ResourceSetRetrieval>.IsDefined(behavior))
                throw new ArgumentOutOfRangeException(nameof(behavior), Res.Get(Res.ArgumentOutOfRange));

            if (behavior != ResourceSetRetrieval.GetIfAlreadyLoaded && tryParents && IsAppendPossible(culture))
                EnsureMerged(culture, behavior);

            return base.GetExpandoResourceSet(culture, behavior, tryParents);
        }

        private void AdjustCulture(ref CultureInfo culture)
        {
            if (culture == null)
                culture = CultureInfo.CurrentUICulture;

            // Logic from ResourceFallbackManager.GetEnumerator()
            if (culture.Name == NeutralResourcesCulture.Name)
                culture = CultureInfo.InvariantCulture;
        }

        /// <summary>
        /// Similar to base.GetObjectInternal but applies appending rules
        /// </summary>
        private object GetObjectWithAppend(string name, CultureInfo culture, bool isString)
        {
            object value;
            ResourceSet seen = TryGetFromCachedResourceSet(name, culture, isString, out value);

            // There is a result, or a stored null is returned from invariant resource as object
            if (value != null
                || (seen != null && !isString
                    && (Equals(culture, CultureInfo.InvariantCulture) || IsProxy(seen) && GetWrappedCulture(seen).Equals(CultureInfo.InvariantCulture))
                    && (Unwrap(seen) as IExpandoResourceSet)?.ContainsResource(name, IgnoreCase) == true))
            {
                return value;
            }

            EnsureMerged(culture, ResourceSetRetrieval.CreateIfNotExists);

            // Phase 1: finding the resource and meanwhile collecting cultures to merge
            AutoAppendOptions append = AutoAppend;
            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, NeutralResourcesCulture, true);
            var toMerge = new Stack<IExpandoResourceSet>();
            bool isFirstNeutral = true;

            // The resource to cache normally should be the resource to which the requested culture belongs.
            // Since DRM does not use tryParents = true during the traversal, the cached proxies are created on caching.
            ResourceSet toCache = null;

            // Crawling from specific to neutral and collecting the cultures to merge
            foreach (CultureInfo currentCulture in mgr)
            {
                bool isMergeNeeded = IsMergeNeeded(culture, currentCulture, isFirstNeutral);
                if (isFirstNeutral && currentCulture.IsNeutralCulture)
                    isFirstNeutral = false;

                // can occur from the 2nd iteration: the proxy to cache will be invalidated so it should be re-created
                if (isMergeNeeded && IsProxy(toCache) && !IsExpandoExists(currentCulture))
                {
                    toCache = null;
                }

                // using tryParents only if invariant is requested without appending so the exception can come from the base
                bool tryParents = ReferenceEquals(currentCulture, CultureInfo.InvariantCulture)
                    && (append & AutoAppendOptions.AddUnknownToInvariantCulture) == AutoAppendOptions.None;
                ResourceSet rs = InternalGetResourceSet(currentCulture, isMergeNeeded ? ResourceSetRetrieval.CreateIfNotExists : ResourceSetRetrieval.LoadIfExists, tryParents, isMergeNeeded);

                // Proxies are considered only at TryGetFromCachedResourceSet.
                // When traversing, proxies are skipped because we must track the exact levels at merging and we don't know what is between the current and proxied levels.
                if (rs != null && rs != seen && !IsProxy(rs))
                {
                    value = GetResourceFromAny(rs, name, isString);

                    // there is a result
                    if (value != null)
                    {
                        // returning the value immediately only if there is nothing to merge and to proxy
                        if (toMerge.Count == 0 && currentCulture.Equals(culture))
                        {
                            SetCache(culture, rs);
                            return value;
                        }
                    }
                    // null as object from invariant can be returned. null is never merged so ignoring toMerge if any.
                    else if (!isString && ReferenceEquals(currentCulture, CultureInfo.InvariantCulture) && (rs as IExpandoResourceSet)?.ContainsResource(name, IgnoreCase) == true)
                    {
                        SetCache(culture, toCache
                            ?? (Equals(culture, CultureInfo.InvariantCulture)
                                ? rs
                                : InternalGetResourceSet(culture, ResourceSetRetrieval.LoadIfExists, true, false)));
                        return null;
                    }

                    // Unlike in base, no unwrapping for this check because proxies are skipped here
                    seen = rs;
                }

                if (Equals(culture, currentCulture))
                    toCache = rs;

                if (value != null)
                    break;

                if (isMergeNeeded)
                    toMerge.Push((IExpandoResourceSet) rs);
            }

            // Phase 2: handling null
            if (value == null)
            {
                // no append of invariant: exit
                if ((append & AutoAppendOptions.AddUnknownToInvariantCulture) == AutoAppendOptions.None)
                    return null;

                IExpandoResourceSet inv = (IExpandoResourceSet)InternalGetResourceSet(CultureInfo.InvariantCulture, ResourceSetRetrieval.CreateIfNotExists, false, true);

                // as string: adding unknown to invariant; otherwise, adding null explicitly
                if (isString)
                    value = LanguageSettings.UnknownResourcePrefix + name;

                inv.SetObject(name, value);

                // if there is nothing to merge, returning (as object, null is never merged)
                if (!isString || toMerge.Count == 0)
                {
                    SetCache(culture, toCache
                        ?? (Equals(culture, CultureInfo.InvariantCulture) 
                            ? (ResourceSet)inv 
                            : InternalGetResourceSet(culture, ResourceSetRetrieval.LoadIfExists, true, false)));
                    return value;
                }
            }

            // Phase 3: processing the merge
            // Unlike in EnsureMerged, not merged levels are missing here because we can be sure that the resource does not exist at mid-level.
            foreach (IExpandoResourceSet rsToMerge in toMerge)
            {
                if (isString)
                    value = AdjustStringToAppend(value);
                rsToMerge.SetObject(name, value);
            }

            // If the resource set of the requested level does not exist, it can be created (as a proxy) by the base class by using tryParent=true in all levels.
            SetCache(culture, toCache ?? InternalGetResourceSet(culture, ResourceSetRetrieval.LoadIfExists, true, false));
            return value;
        }

        private object AdjustStringToAppend(object value)
        {
            string strValue = (string)value;
            string prefix = LanguageSettings.UntranslatedResourcePrefix;
            return !strValue.StartsWith(prefix, StringComparison.Ordinal) ? prefix + strValue : value;
        }

        /// <summary>
        /// Called by GetFirstResourceSet if cache is a proxy.
        /// There is always a traversal if this is called.
        /// Proxy is accepted if it is no problem if a result is found in the proxied resource set.
        /// </summary>
        /// <param name="proxy">The found proxy</param>
        /// <param name="culture">The requested culture</param>
        internal override bool IsCachedProxyAccepted(ResourceSet proxy, CultureInfo culture)
        {
            // if invariant culture is requested, this method should not be reached
            Debug.Assert(!Equals(culture, CultureInfo.InvariantCulture), "There should be no proxy for the nnvariant culture");

            if (!base.IsCachedProxyAccepted(proxy, culture))
                return false;

            AutoAppendOptions autoAppend = AutoAppend;

            // There is no append for existing entries (only AddUnknownToInvariantCulture).
            if ((autoAppend & (AutoAppendOptions.AppendNeutralCultures | AutoAppendOptions.AppendSpecificCultures)) == AutoAppendOptions.None)
                return true;

            // This check matters if called by base.GetObjectInternal. Otherwise, already checked earlier.
            if (!IsAppendPossible(culture))
                return true;

            // Note: we don't check AddUnknownToInvariantCulture flag here because in the cache for the invariant key the resource is never proxied

            // Neutral is requested: a parent neutral (most cases: invariant) is in the proxy.
            // This is ok if the neutral is not about to be appended.
            if (culture.IsNeutralCulture)
                return (autoAppend & AutoAppendOptions.AppendNeutralCultures) == AutoAppendOptions.None;

            // Specific is requested: the proxy usually contains a neutral or the invariant.
            // This is ok if only neutral should be appended and the proxy contains a neutral (non-invariant) culture.
            // This is ok because if the neutral in the proxy contains a result, it can be returned and nothing should be appended.
            return (autoAppend & (AutoAppendOptions.AppendNeutralCultures | AutoAppendOptions.AppendSpecificCultures)) == AutoAppendOptions.AppendNeutralCultures
                && !GetWrappedCulture(proxy).Equals(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Checks whether append is possible
        /// </summary>
        private bool IsAppendPossible(CultureInfo culture)
        {
            AutoAppendOptions append;

            // append is not possible if only compiled resources are used...
            return !(base.Source == ResourceManagerSources.CompiledOnly
                // ...or there is no appending at all...
                || (append = AutoAppend) == AutoAppendOptions.None
                // ...invariant culture is requested but invariant is not appended...
                || (append & AutoAppendOptions.AddUnknownToInvariantCulture) == AutoAppendOptions.None && Equals(culture, CultureInfo.InvariantCulture)
                // ...a neutral culture is requested but only specific culture is appended
                || (append & (AutoAppendOptions.AddUnknownToInvariantCulture | AutoAppendOptions.AppendNeutralCultures | AutoAppendOptions.AppendOnLoad)) == AutoAppendOptions.None && culture.IsNeutralCulture);
        }

        /// <summary>
        /// Tells the resource manager to call the <see cref="ResourceSet.Close" /> method on all <see cref="ResourceSet" /> objects and release all resources.
        /// All unsaved resources will be lost.
        /// </summary>
        public override void ReleaseAllResources()
        {
            lock (SyncRoot)
            {
                base.ReleaseAllResources();
                mergedCultures = null;
            }
        }

        private void EnsureMerged(CultureInfo culture, ResourceSetRetrieval behavior)
        {
            Debug.Assert(behavior == ResourceSetRetrieval.LoadIfExists || behavior == ResourceSetRetrieval.CreateIfNotExists);
            AutoAppendOptions append = AutoAppend;

            lock (SyncRoot)
            {
                // return if there is no append on load...
                if ((append & AutoAppendOptions.AppendOnLoad) == AutoAppendOptions.None
                    // ...or append flags are not set...
                    || (append & (AutoAppendOptions.AppendNeutralCultures | AutoAppendOptions.AppendSpecificCultures)) == AutoAppendOptions.None
                    // ...or the requested culture is invariant...
                    || culture.Equals(CultureInfo.InvariantCulture)
                    // ...or only AppendSpecific is on but the requested culture is neutral...
                    || ((append & AutoAppendOptions.AppendNeutralCultures) == AutoAppendOptions.None && culture.IsNeutralCulture)
                    // ...or merge status is already up-to-date
                    || (mergedCultures?.ContainsKey(culture) ?? false))
                    //// ...or culture is already loaded/created
                    //|| IsNonProxyLoaded(culture))
                {
                    return;
                }

                // Phase 1: collecting cultures to merge
                Debug.Assert(Source != ResourceManagerSources.CompiledOnly);
                if (mergedCultures == null)
                    mergedCultures = new Dictionary<CultureInfo, bool> { {CultureInfo.InvariantCulture, true} };
                ResourceFallbackManager mgr = new ResourceFallbackManager(culture, NeutralResourcesCulture, true);
                var toMerge = new Stack<KeyValuePair<CultureInfo, bool>>();
                bool isFirstNeutral = true;
                CultureInfo stopMerge = null;

                // crawling from specific to neutral...
                foreach (CultureInfo currentCulture in mgr)
                {
                    bool merged;
                    // if culture is not up-to-date
                    if (!mergedCultures.TryGetValue(currentCulture, out merged))
                    {
                        bool isMergeNeeded = IsMergeNeeded(culture, currentCulture, isFirstNeutral);
                        if (isFirstNeutral && currentCulture.IsNeutralCulture)
                            isFirstNeutral = false;

                        toMerge.Push(new KeyValuePair<CultureInfo, bool>(currentCulture, isMergeNeeded));

                        // storing the last culture to be merged in this hierarchy
                        if (isMergeNeeded && stopMerge == null)
                            stopMerge = currentCulture;
                    }
                    // we reached an up-to-date culture: searching for a merged one to take a base or invariant
                    else
                    {
                        toMerge.Push(new KeyValuePair<CultureInfo, bool>(currentCulture, merged));
                        if (merged)
                            break;
                    }
                }

                // Phase 2: Performing the merge
                Debug.Assert(toMerge.Count > 0 && toMerge.Peek().Value, "A merged culture is expected as top element on the stack ");

                // actually there is nothing to merge: updating mergedCultures and exit
                if (stopMerge == null)
                {
                    foreach (KeyValuePair<CultureInfo, bool> item in toMerge)
                    {
                        if (!mergedCultures.ContainsKey(item.Key))
                        {
                            Debug.Assert(!item.Value, "No merging is expected here");
                            mergedCultures.Add(item.Key, false);
                        }
                    }

                    return;
                }

                // taking the first element of the stack, which is merged (or is the invariant culture) and doing the merges
                // tryParents is always true in callers; otherwise, there would be no merge.
                bool tryParents = ThrowException && ReferenceEquals(toMerge.Peek().Key, CultureInfo.InvariantCulture)
                    && (append & AutoAppendOptions.AddUnknownToInvariantCulture) == AutoAppendOptions.None;
                ResourceSet rs = InternalGetResourceSet(toMerge.Peek().Key, ResourceSetRetrieval.LoadIfExists, tryParents, false);
                Debug.Assert(rs != null || ReferenceEquals(toMerge.Peek().Key, CultureInfo.InvariantCulture), "A merged culture is expected to be exist. Only invariant can be missing.");
                Debug.Assert(rs == null || IsNonProxyLoaded(toMerge.Peek().Key), "The base culture for merge should not be a proxy");

                // populating an accumulator with the first, fully merged resource
                toMerge.Pop();
                Debug.Assert(toMerge.Count > 0, "Cultures to be merged are expected on the stack");
                KeyValuePair<CultureInfo, bool> current;
                Dictionary<string, object> acc = new Dictionary<string, object>();
                if (rs != null)
                {
                    ToDictionary(rs, acc);
                }

                do
                {
                    current = toMerge.Pop();
                    ResourceSet rsTarget = InternalGetResourceSet(current.Key,
                        behavior == ResourceSetRetrieval.CreateIfNotExists && current.Value
                            ? ResourceSetRetrieval.CreateIfNotExists
                            : ResourceSetRetrieval.LoadIfExists, false, current.Value);

                    // cannot be loaded
                    if (rsTarget == null)
                    {
                        mergedCultures[current.Key] = false;
                        continue;
                    }

                    // merging accumulator to target resource set
                    if (current.Value)
                    {
                        MergeResourceSet(acc, (IExpandoResourceSet)rsTarget, !ReferenceEquals(current.Key, stopMerge));
                    }
                    // updating accumulator
                    else if (!ReferenceEquals(current.Key, stopMerge))
                    {
                        ToDictionary(rsTarget, acc);
                    }

                    mergedCultures[current.Key] = current.Value;
                } while (!ReferenceEquals(current.Key, stopMerge));

                // updating mergedCultures with the rest of the cultures (they are not merged)
                foreach (KeyValuePair<CultureInfo, bool> item in toMerge)
                {
                    mergedCultures[item.Key] = false;
                }
            }
        }

        private bool IsMergeNeeded(CultureInfo requestedCulture, CultureInfo currentCulture, bool isFirstNeutral)
        {
            var append = AutoAppend;
            return
                // append neutral cultures is on and current culture is a neutral one
                ((append & AutoAppendOptions.AppendNeutralCultures) == AutoAppendOptions.AppendNeutralCultures && currentCulture.IsNeutralCulture)
                // append first neutral is on and current culture is the first neutral one
                || ((append & AutoAppendOptions.AppendFirstNeutralCulture) != AutoAppendOptions.None && isFirstNeutral && currentCulture.IsNeutralCulture)
                // append last neutral is on and parent of current neutral culture is invariant
                || ((append & AutoAppendOptions.AppendLastNeutralCulture) != AutoAppendOptions.None && currentCulture.IsNeutralCulture && ReferenceEquals(currentCulture.Parent, CultureInfo.InvariantCulture))
                // append specific cultures is on and current culture is a specific one
                || ((append & AutoAppendOptions.AppendSpecificCultures) == AutoAppendOptions.AppendSpecificCultures && !ReferenceEquals(currentCulture, CultureInfo.InvariantCulture) && !currentCulture.IsNeutralCulture)
                // append first specific is on and requested culture is a specific one
                || ((append & AutoAppendOptions.AppendFirstSpecificCulture) != AutoAppendOptions.None && !ReferenceEquals(currentCulture, CultureInfo.InvariantCulture) && currentCulture.Equals(requestedCulture) && !currentCulture.IsNeutralCulture)
                // append last specific is on and parent of current specific is neutral or invariant
                || ((append & AutoAppendOptions.AppendLastSpecificCulture) != AutoAppendOptions.None && !ReferenceEquals(currentCulture, CultureInfo.InvariantCulture) && !currentCulture.IsNeutralCulture && (currentCulture.Parent.IsNeutralCulture || ReferenceEquals(currentCulture.Parent, CultureInfo.InvariantCulture)))
                ;
        }

        private void ToDictionary(ResourceSet source, Dictionary<string, object> target)
        {
            var expandoRs = source as IExpandoResourceSetInternal;
            IDictionaryEnumerator enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string key = enumerator.Key.ToString();
                target[key] = expandoRs != null ? expandoRs.GetResource(key, false, false, true) : enumerator.Value;
            }
        }

        private void MergeResourceSet(Dictionary<string, object> source, IExpandoResourceSet target, bool rebuildSource)
        {
            string prefix = LanguageSettings.UntranslatedResourcePrefix;

            foreach (KeyValuePair<string, object> resource in source)
            {
                if (target.ContainsResource(resource.Key))
                    continue;

                object newValue;
                string strValue = AsString(resource.Value);
                if (strValue == null)
                    newValue = resource.Value;
                else
                    newValue = strValue.StartsWith(prefix, StringComparison.Ordinal)
                        ? strValue
                        : prefix + strValue;

                target.SetObject(resource.Key, newValue);
            }

            if (rebuildSource)
            {
                source.Clear();
                ToDictionary((ResourceSet)target, source);
            }
        }

        private string AsString(object value)
        {
            string result = value as string;
            if (result != null)
                return result;

            // even if a fileref contains a string, we cannot add prefix to the referenced file so returning null here
            ResXDataNode node = value as ResXDataNode;
            if (node == null || node.FileRef != null)
                return null;

            // already deserialized: returning the string or null
            result = node.ValueInternal as string;
            if (node.ValueInternal != null)
                return result;

            // the default way of storing a string
            if (node.TypeName == null && node.MimeType == null && node.ValueData != null)
                return node.ValueData;

            // string type is explicitly defined
            if (node.TypeName != null)
            {
                string[] typeParts = node.TypeName.Split(',');
                string assembly = node.AssemblyAliasValue?.Split(',')[0] ?? (typeParts.Length > 1 ? typeParts[1] : null);
                if (typeParts[0].Trim() == "System.String" && (assembly == null || assembly.Trim() == "mscorlib"))
                {
                    return node.ValueData;
                }
            }

            // otherwise: non string (theoretically it could be a serialized string but the writer never creates it so)
            return null;
        }

        private void LanguageSettings_DynamicResourceManagersSourceChanged(object sender, EventArgs e)
        {
            SetSource(LanguageSettings.DynamicResourceManagersSource);
        }

        private void LanguageSettings_DynamicResourceManagersAutoSaveChanged(object sender, EventArgs e)
        {
            lock (SyncRoot)
            {
                UnhookEvents();
                HookEvents();
            }
        }

        private void LanguageSettings_DisplayLanguageChanged(object sender, EventArgs e)
        {
            OnLanguageChanged();
        }

        private void CurrentDomain_DomainUnload(object sender, EventArgs e)
        {
            OnDomainUnload();
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            OnDomainUnload();
        }
    }
}