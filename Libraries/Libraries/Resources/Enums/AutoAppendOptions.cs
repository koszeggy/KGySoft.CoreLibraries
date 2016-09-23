using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Text;

namespace KGySoft.Libraries.Resources
{
    /// <summary>
    /// Represents the resource auto append options of a <see cref="DynamicResourceManager"/> instance.
    /// </summary>
    /// <seealso cref="DynamicResourceManager.AutoAppend"/>
    /// <seealso cref="LanguageSettings.DynamicResourceManagersAutoAppend"/>
    [Flags]
    public enum AutoAppendOptions
    {
        /// <summary>
        /// Represents no auto appending.
        /// </summary>
        None,

        /// <summary>
        /// <para>If a resource with an unknown key is requested, a new resource is automatically added to the
        /// invariant resource set.</para>
        /// <para>If the resource is requested as a <see cref="string"/>, the newly added
        /// value will be initialized by the requested key, prefixed by the <see cref="LanguageSettings.UnknownResourcePrefix"/> property.
        /// This new entry can be then merged into other resource sets, too.</para>
        /// <para>If the resource is requested as an <see cref="object"/>, a <see langword="null"/> value will be added to the resource.
        /// The <see langword="null"/> value is never merged into the other resource sets because it has a special meaning:
        /// if a resource has a null value, the parent resources are checked for a non-null resource value.</para>
        /// <para>Enabling this flag causes that <see cref="MissingManifestResourceException"/> will never be thrown for non-existing resources.</para>
        /// <para>This flag is disabled by default.</para>
        /// </summary>
        AddUnknownToInvariantCulture = 1,

        /// <summary>
        /// <para>If a resource is found in the resource set of the <see cref="CultureInfo.InvariantCulture">invariant culture</see>,
        /// the resource will be added to the resource set of the neutral (non-region or country specific) culture of the reqested culture.
        /// For example, if the resource is requested for the <c>en-US</c> culture but the resource is found in the invariant resource set,
        /// then the resource will be automatically added to the <c>en</c> resource set.</para>
        /// <para>If the found resource is a <see cref="string"/>, the newly added
        /// value prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix"/> property.</para>
        /// <para>If the found resource is not a <see cref="string"/>, the found resource will be simply added
        /// to the resource set of the neutral culture, too.</para>
        /// <para>This flag is enabled by default.</para>
        /// </summary>
        AppendNeutralCulture = 1 << 1,

        /// <summary>
        /// <para>If a resource is found in the resource set of the <see cref="CultureInfo.InvariantCulture">invariant culture</see>
        /// or a neutral culture but the requested culture was a region-specific culture,
        /// the resource will be added to the resource set of the reqested culture.
        /// For example, if the resource is requested for the <c>en-US</c> culture but the resource is found in the <c>en</c> or invariant resource set,
        /// then the resource will be automatically added to the <c>en-US</c> resource set.</para>
        /// <para>If the found resource is a <see cref="string"/>, the newly added
        /// value prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix"/> property.</para>
        /// <para>If the found resource is not a <see cref="string"/>, the found resource will be simply added
        /// to the resource set of the specific culture, too.</para>
        /// <para>This flag is disabled by default.</para>
        /// </summary>
        AppendSpecificCulture = 1 << 2,

        /// <summary>
        /// <para>If a resource set loaded, <see cref="AppendNeutralCulture"/> and <see cref="AppendSpecificCulture"/> rules are
        /// automatically applied for all resources.</para>
        /// <para>This flag is enabled by default.</para>
        /// </summary>
        AppendOnLoad = 1 << 3,
    }
}
