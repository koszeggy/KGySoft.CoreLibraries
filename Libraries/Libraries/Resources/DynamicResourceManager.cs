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
                        // TODO: autosave, autoappend
                    }

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
        /// Default value: <see cref="AutoAppendOptions.AppendNeutralCulture"/>, <see cref="AutoAppendOptions.AppendOnLoad"/>
        /// </summary>
        /// <remarks>
        /// Auto appending affects the resources only. Metadata are never merged.
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

                if (!value.AllFlagsDefined())
                    throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));

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

        // TODO: to base
        protected virtual void Dispose(bool disposing)
        {
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
        /// Similar to base.GetObjectInternal but applies append rules
        /// </summary>
        private object GetObjectWithAppend(string name, CultureInfo culture, bool isString)
        {
            EnsureMerged(culture, ResourceSetRetrieval.CreateIfNotExists);

            object value;
            ResourceSet seen = TryGetFromCachedResourceSet(name, culture ?? CultureInfo.CurrentUICulture, isString, out value);
            if (value != null)
                return value;

            // TODO: ha value null és !isString: ha seen != null && culture == inv, akkor ha seen.Contains(name), visszaadható az invariantban tárolt null
            // megfontolni: culture == inv helyett ha a wrapped culture inv, ugyanis ha ez nem jó, azt az IsCachedProxyAccepted sem adhatná vissza

            // The InternalGetResourceSet has also a hierarchy traversal. This outer traversal is required as well because
            // the inner one can return an existing resource set without the searched resource, in which case here is
            // the fallback to the parent resource.
            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, NeutralResourcesCulture, true);
            IExpandoResourceSet specific = null, neutral = null;
            ResourceSet invariant = null;
            AutoAppendOptions append = AutoAppend;
            bool createProxy = false;
            CultureInfo neutralCulture = null;
            ResourceSet toCache = null;

            foreach (CultureInfo currentCulture in mgr)
            {
                // if there should be a result set at this level due to merging, we use create; otherwise, load
                ResourceSetRetrieval behavior =
                    // if specific has to be appended and this is the first (topmost) specific...
                    ((append & AutoAppendOptions.AppendSpecificCulture) != AutoAppendOptions.None && specific == null && !currentCulture.IsNeutralCulture)
                    // ...or if neutral has to be appended and this is the first (topmost) non-invariant neutral...
                    || (!ReferenceEquals(currentCulture, CultureInfo.InvariantCulture) && (append & AutoAppendOptions.AppendNeutralCulture) != AutoAppendOptions.None && neutral == null && currentCulture.IsNeutralCulture)
                    // ...or if invariant has to be appended and this is the invariant...
                    || ReferenceEquals(currentCulture, CultureInfo.InvariantCulture) && (append & AutoAppendOptions.AddUnknownToInvariantCulture) != AutoAppendOptions.None
                    // ...then create; otherwise, load
                    ? ResourceSetRetrieval.CreateIfNotExists : ResourceSetRetrieval.LoadIfExists;

                // using tryParents only if invariant is requested without appending so the exception can come from the base
                bool tryParents = ReferenceEquals(currentCulture, CultureInfo.InvariantCulture)
                    && (append & AutoAppendOptions.AddUnknownToInvariantCulture) != AutoAppendOptions.None;

                // Expando is not forced for invariant. If needed, it will be created if unknown resource is found.
                ResourceSet rs = InternalGetResourceSet(currentCulture, behavior, tryParents, behavior == ResourceSetRetrieval.CreateIfNotExists && !ReferenceEquals(currentCulture, CultureInfo.InvariantCulture));

                if (currentCulture.Equals(culture))
                {
                    // if there is no stored result set for the requested culture, a proxy should be created at the end
                    // (as if we obtained the result by tryParents=true). This should make subsequent calls faster and
                    // we will not come here again with for the same culture.
                    if (rs == null)
                        createProxy = true;
                    else
                        toCache = rs;
                }

                if (rs == null)
                {
                    // invariant not found but ThrowExecption is off and no AppendInvariant is set: return
                    if (ReferenceEquals(currentCulture, CultureInfo.InvariantCulture))
                        return null;

                    continue;
                }

                if (rs == seen)
                    continue;

                value = GetResourceFromAny(rs, name, isString);
                if (ReferenceEquals(currentCulture, CultureInfo.InvariantCulture))
                {
                    invariant = rs;
                    break;
                }

                if (value != null)
                    break;

                // specific (we don't need to unwrap rs because in this case we have Create behavior)
                if (specific == null && (append & AutoAppendOptions.AppendSpecificCulture) != AutoAppendOptions.None && !currentCulture.IsNeutralCulture)
                    specific = (IExpandoResourceSet)rs;
                // neutral (we don't need to unwrap rs because in this case we have Create behavior)
                else if (neutral == null && (append & AutoAppendOptions.AppendNeutralCulture) != AutoAppendOptions.None && currentCulture.IsNeutralCulture)
                {
                    neutral = (IExpandoResourceSet)rs;
                    neutralCulture = currentCulture;
                }
            }

            // append invariant
            if (value == null && (append & AutoAppendOptions.AddUnknownToInvariantCulture) != AutoAppendOptions.None)
            {
                // now we force expando for invariant
                var expandoInv = invariant as IExpandoResourceSet ?? (IExpandoResourceSet)InternalGetResourceSet(CultureInfo.InvariantCulture, ResourceSetRetrieval.CreateIfNotExists, false, true);

                // new string
                if (isString)
                {
                    value = LanguageSettings.UnknownResourcePrefix + name;
                    expandoInv.SetObject(name, value);
                }
                // if requested as object, we may add null. This is the only case when requesting the same resource again and again
                else
                {
                    if (!expandoInv.ContainsResource(name))
                        expandoInv.SetObject(name, null);
                }
            }

            // TODO: Ha több neutral és specific is lehet egy hieararchiában, az összeset össze kéne gyûjteni, mert az alábbi rosszul merge-ölhet
            // Ekkor az append neutral és specific ág helyett egy mgr.Reverse-en végigmenni, és amíg az appendOptions szerint szükséges,
            // adott culture szerint rs bõvítés, ha nem tartalmazza a name-et. Jelenleg nincs Contains mert a nem null neutral/specific épp azt jelenti, hogy az a kettõ nem tartalmazza

            // adjust string (todo: nem így lesz, ha a fenti szerint full merge lesz, mert akkor minden szinten elkérünk egy object/string-et)
            if (isString && value != null)
            {
                Debug.Assert(value is string);
                value = AdjustStringToAppend(value);
            }

            // append neutral
            if (neutral != null && value != null)
            {
                neutral.SetObject(name, value);
            }

            // append specific
            if (specific != null && value != null)
            {
                // See if the value is in neutral. If neutral is not null, the merge has already been performed.
                // TODO: valami ilyesmi kéne majd a full bejárásba, ha lehetséges több neutral és specific is
                if (neutral == null)
                {
                    ResourceSet rs = InternalGetResourceSet(neutralCulture, ResourceSetRetrieval.LoadIfExists, false, false);
                    value = GetResourceFromAny(rs, name, isString) ?? value;

                    if (isString)
                        value = AdjustStringToAppend(value);
                }

                specific.SetObject(name, value);
            }

            // create proxy and cache
            if (createProxy)
            {
                // since the foreach above does not use tryParents for children, this will create a proxy if needed:
                toCache = InternalGetResourceSet(culture, ResourceSetRetrieval.LoadIfExists, true, false);
            }

            Debug.Assert(toCache != null);

            // updates last used ResourceSet in base
            SetCache(culture, toCache);
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

            // occurs if called by base.GetObjectInternal
            if (!IsAppendPossible(culture))
                return true;

            // Note: we don't check AddUnknownToInvariantCulture flag here because if invariant cached resource is found, it is never proxied

            // Neutral is requested: a parent nutral (most cases: invariant) is in the proxy.
            // This is ok if the neutral is not about to be appended.
            if (culture.IsNeutralCulture)
                return (AutoAppend & AutoAppendOptions.AppendNeutralCulture) == AutoAppendOptions.None;

            // Specific is requested: the proxy usually contains a neutral or the invariant.
            // This is ok if only neutral should be appended and the proxy contains a neutral (non-invariant) culture.
            // This is ok because if the neutral in the proxy contains a result, it can be reurned and nothing should be appended.
            return (AutoAppend & (AutoAppendOptions.AppendNeutralCulture | AutoAppendOptions.AppendSpecificCulture)) == AutoAppendOptions.AppendNeutralCulture
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
                // ...invariant culture is requested but invariant is never appended...
                || (append & AutoAppendOptions.AddUnknownToInvariantCulture) == AutoAppendOptions.None && Equals(culture, CultureInfo.InvariantCulture)
                // ...a neutral culture is requested but only specific culture is appended
                || (append & (AutoAppendOptions.AddUnknownToInvariantCulture | AutoAppendOptions.AppendNeutralCulture | AutoAppendOptions.AppendOnLoad)) == AutoAppendOptions.None && culture.IsNeutralCulture);
        }


        private void EnsureMerged(CultureInfo culture, ResourceSetRetrieval behavior)
        {
            Debug.Assert(behavior == ResourceSetRetrieval.LoadIfExists || behavior == ResourceSetRetrieval.CreateIfNotExists);
            AutoAppendOptions append = AutoAppend;

            // return if there is no append on load...
            if ((append & AutoAppendOptions.AppendOnLoad) == AutoAppendOptions.None
                // ...or append flags are not set...
                || (append & (AutoAppendOptions.AppendNeutralCulture | AutoAppendOptions.AppendSpecificCulture)) == AutoAppendOptions.None
                // ...or the requested culture is invariant...
                || culture.Equals(CultureInfo.InvariantCulture)
                // ...or only AppendSpecific is on but the requested culture is neutral...
                || ((append & AutoAppendOptions.AppendNeutralCulture) == AutoAppendOptions.None && culture.IsNeutralCulture)
                // ...or culture is already loaded/created
                || IsNonProxyLoaded(culture))
            {
                return;
            }

            Debug.Assert(Source != ResourceManagerSources.CompiledOnly);
            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, NeutralResourcesCulture, true);
            IExpandoResourceSet specific = null, neutral = null;
            CultureInfo neutralculture = null;

            // crawling from specific to neutral...
            foreach (CultureInfo currentCulture in mgr)
            {
                if (ReferenceEquals(currentCulture, CultureInfo.InvariantCulture) || (specific != null && neutral != null))
                    break;

                // specific (we could use GetExpandoResourceSet, too; but that can switch the SafeMode, which can be a surprise for the user who already obtained a ResourceSet)
                // if behavior is LoadIfExists we may get a proxied expando but that does not belong to this culture so it is ok to not to unwrap and to make the cast null
                if (specific == null && (append & AutoAppendOptions.AppendSpecificCulture) != AutoAppendOptions.None && !currentCulture.IsNeutralCulture)
                    specific = InternalGetResourceSet(currentCulture, behavior, false, behavior == ResourceSetRetrieval.CreateIfNotExists) as IExpandoResourceSet;
                // neutral
                else if (neutral == null && (append & AutoAppendOptions.AppendNeutralCulture) != AutoAppendOptions.None && currentCulture.IsNeutralCulture)
                {
                    neutral = InternalGetResourceSet(currentCulture, behavior, false, behavior == ResourceSetRetrieval.CreateIfNotExists) as IExpandoResourceSet;
                    neutralculture = currentCulture;
                }
            }

            ResourceSet invariant;

            // 1.) Merging invariant to neutral
            if (neutral != null)
            {
                invariant = InternalGetResourceSet(CultureInfo.InvariantCulture, ResourceSetRetrieval.LoadIfExists, false, false);
                if (invariant != null)
                    MergeResourceSets(invariant, neutral);
            }

            // 2.) Merging neutral and/or invariant to specific
            if (specific != null)
            {
                // if neutral is not null, then it is already merged so one merge step is enough
                ResourceSet source = neutral as ResourceSet;

                // twoSteps can occur if
                // - neutral was a proxy (in case of LoadIfExist)
                // - no AppendNeutral was requested. But we must merge from the right rs so loading it now if possible
                bool twoSteps = source == null;
                if (source == null)
                    source = Unwrap(InternalGetResourceSet(neutralculture, ResourceSetRetrieval.LoadIfExists, false, false));
                if (source != null && IsNonProxyLoaded(neutralculture))
                    MergeResourceSets(source, specific);
                if (twoSteps)
                {
                    invariant = InternalGetResourceSet(CultureInfo.InvariantCulture, ResourceSetRetrieval.LoadIfExists, false, false);
                    if (invariant != null)
                        MergeResourceSets(invariant, specific);
                }
            }

            // TODO: Ha több neutral és specific is lehet egy hieararchiában, az összeset össze kéne gyûjteni,
            // mert a fenti rosszul merge-ölhet
        }

        private void MergeResourceSets(ResourceSet source, IExpandoResourceSet target)
        {
            string prefix = LanguageSettings.UntranslatedResourcePrefix;
            IDictionaryEnumerator enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string key = enumerator.Key.ToString();
                if (target.ContainsResource(key))
                    continue;

                var expandoSource = source as IExpandoResourceSetInternal;
                object value = expandoSource != null
                    ? expandoSource.GetResource(key, false, false, true)
                    : source.GetObject(key, false);

                object newValue;
                string strValue = AsString(value);
                if (strValue == null)
                    newValue = value;
                else
                    newValue = strValue.StartsWith(prefix, StringComparison.Ordinal)
                        ? strValue
                        : prefix + strValue;

                target.SetObject(key, newValue);
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

            // the default way of stoting a string
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

            // othwerwise: non string (theoretically it could be a serialized string but the writer never creates it so)
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