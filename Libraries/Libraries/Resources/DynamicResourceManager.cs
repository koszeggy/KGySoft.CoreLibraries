using System;

namespace KGySoft.Libraries.Resources
{
    using System.Globalization;
    using System.Reflection;
    using System.Resources;

    /// <summary>
    /// Represents a resource manager that provides convenient access to culture-specific resources at run time.
    /// As it wraps a <see cref="HybridResourceManager"/>, it can handle both compiled resources from <c>.dll</c> and
    /// <c>.exe</c> files, and <c>.resx</c> files at the same time. Based on the selected strategies, when a resource
    /// is not found in a language, it can automatically expand the resource with the text of the neutral resource,
    /// which can be saved into <c>.resx</c> files.
    /// </summary>
    /// <remarks></remarks>
    // TODO: Remarks: Míg a ResxRM/HRM konkrét add metódusokat tartalmaz, addig a bõvítés itt automatikus, a stratégia property-kkel szabályozható. Itt a mentés is lehet automatikus, pl kilépésnél, ha éppen resx/mixed mód van.
    public class DynamicResourceManager : ResourceManager
    {
        private readonly HybridResourceManager resourceManager;

        /// <summary>
        /// Creates a new instance of <see cref="DynamicResourceManager"/> class that looks up resources in
        /// satellite assemblies and resource XML files based on information from the specified <paramref name="baseName"/>
        /// and <paramref name="assembly"/>.
        /// </summary>
        /// <param name="baseName">A root name that is the prefix of the resource files without the extension.</param>
        /// <param name="assembly">The main assembly for the resources.</param>
        /// <param name="explicitResxBaseFileName">When <see langword="null"/>, .resx file name will be constructed based on the
        /// <paramref name="baseName"/> parameter; otherwise, the given <see cref="string"/> will be used. This parameter is optional.</param>
        public DynamicResourceManager(string baseName, Assembly assembly, string explicitResxBaseFileName = null)
        {
            resourceManager = new HybridResourceManager(explicitResxBaseFileName ?? baseName, assembly);
        }

        /// <summary>
        /// Creates a new instance of <see cref="DynamicResourceManager"/> class that looks up resources in
        /// satellite assemblies and resource XML files based on information from the specified type object.
        /// </summary>
        /// <param name="resourceSource">A type from which the resource manager derives all information for finding resource files.</param>
        /// <param name="explicitResxBaseFileName">When <see langword="null"/>, .resx file name will be constructed based on the
        /// <paramref name="resourceSource"/> parameter; otherwise, the given <see cref="string"/> will be used.</param>
        public DynamicResourceManager(Type resourceSource, string explicitResxBaseFileName = null)
        {
            resourceManager = new HybridResourceManager(resourceSource, explicitResxBaseFileName);
        }

        /// <summary>
        /// Gets the root name of the resource files that the <see cref="DynamicResourceManager" /> searches for resources.
        /// </summary>
        public override string BaseName
        {
            get { return resourceManager.BaseName; }
        }

        /// <summary>
        /// Returns the value of the specified non-string resource.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <returns>
        /// The value of the resource localized for the caller's current culture settings.
        /// If an appropriate resource set exists but <paramref name="name" /> cannot be found, the method returns null.
        /// </returns>
        public override object GetObject(string name)
        {
            // this overload must be overridden as well, because both public base methods call a private method,
            // which tries to read the object from an unmanaged stream.
            return resourceManager.GetObject(name);
            // TODO: if null... -> update description, too!
        }

        /// <summary>
        /// Gets the value of the specified non-string resource localized for the specified culture.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <param name="culture">The culture for which the resource is localized. If the resource is not localized for this culture,
        /// the resource manager uses fallback rules to locate an appropriate resource.If this value is null,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="LanguageSettings.DisplayLanguage" /> property.</param>
        /// <returns>
        /// The value of the resource, localized for the specified culture. If an appropriate resource set exists
        /// but <paramref name="name" /> cannot be found, the method returns null.
        /// </returns>
        public override object GetObject(string name, CultureInfo culture)
        {
            return resourceManager.GetObject(name, culture);
            // TODO: if null... -> update description, too!
        }

        /// <summary>
        /// Retrieves the resource set for a particular culture.
        /// </summary>
        /// <param name="culture">The culture whose resources are to be retrieved.</param>
        /// <param name="createIfNotExists"><c>true</c> to load the resource set, if it has not been loaded yet; otherwise, <c>false</c>.</param>
        /// <param name="tryParents"><c>true</c> to use resource fallback to load an appropriate resource if the resource
        /// set cannot be found; <c>false</c> to bypass the resource fallback process.</param>
        /// <returns>
        /// The resource set for the specified culture.
        /// </returns>
        public override ResourceSet GetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
        {
            return resourceManager.GetResourceSet(culture, createIfNotExists, tryParents);
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
            return resourceManager.GetString(name, culture);
            // TODO: if null... -> update description, too! ?: override other overload just for description? yes->call this instead of base
        }

        //TODO override sealed and throw NotSupported, or map to resourceManager.InternalGetResourceSet by reflection, if it is overridden in HRM, too.
        //protected override ResourceSet InternalGetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
        //{
        //    return resourceManager.InternalGetResourceSet(culture, createIfNotExists, tryParents);
        //}

        /// <summary>
        /// Tells the resource manager to call the <see cref="ResourceSet.Close" /> method on all <see cref="ResourceSet" /> objects and release all resources.
        /// </summary>
        public override void ReleaseAllResources()
        {
            // releasing base only because ResourceSets hashtable is protected, thus visible to implementers
            base.ReleaseAllResources();
            resourceManager.ReleaseAllResources();
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the resource manager allows case-insensitive resource lookups in
        /// the <see cref="GetString(string,CultureInfo)" /> and <see cref="GetObject(string,CultureInfo)" /> methods.
        /// </summary>
        public override bool IgnoreCase
        {
            get { return resourceManager.IgnoreCase; }
            set { resourceManager.IgnoreCase = value; }
        }
    }
}