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
    /// These options are ignored if <see cref="DynamicResourceManager.Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.
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
        /// <para>This option is disabled by default.</para>
        /// </summary>
        AddUnknownToInvariantCulture = 1,

        /// <summary>
        /// <para>If a resource is found in a parent resource set of the requested culture, and the traversal of the
        /// culture hierarchy hits a neutral culture, then the resource set of the first (most derived) neutral culture
        /// will be automatically appended by the found resource.
        /// <br/>Let's consider the following hypothetical culture hierarchy:
        /// <br/><c>en-Runic-GB-Yorkshire (specific) -> en-Runic-GB (specific) -> en-Runic (neutral) -> en (neutral) -> Invariant</c>
        /// <br/>If the requested culture is <c>en-Runic-GB-Yorkshire</c> and the resource is found in the invariant resource set,
        /// then with this option the found resource will be added to the <c>en-Runic</c> resource set.</para>
        /// <para>If the found resource is a <see cref="string"/>, the newly added
        /// value will be prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix"/> property.</para>
        /// <para>If the found non-<see langword="null"/> resource is not a <see cref="string"/>, the found value will be simply copied.</para>
        /// <para>This option is enabled by default.</para>
        /// </summary>
        AppendFirstNeutralCulture = 1 << 1,

        /// <summary>
        /// <para>If a resource is found in the resource set of the <see cref="CultureInfo.InvariantCulture">invariant culture</see>,
        /// then the resource set of the last neutral culture (whose parent is the invariant culture)
        /// will be automatically appended by the found resource.
        /// <br/>Let's consider the following hypothetical culture hierarchy:
        /// <br/><c>en-Runic-GB-Yorkshire (specific) -> en-Runic-GB (specific) -> en-Runic (neutral) -> en (neutral) -> Invariant</c>
        /// <br/>If the requested culture is <c>en-Runic-GB-Yorkshire</c> and the resource is found in the invariant resource set,
        /// then with this option the found resource will be added to the <c>en</c> resource set.</para>
        /// <para>If the found resource is a <see cref="string"/>, the newly added
        /// value will be prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix"/> property.</para>
        /// <para>If the found non-<see langword="null"/> resource is not a <see cref="string"/>, the found value will be simply copied.</para>
        /// <para>This option is disabled by default.</para>
        /// </summary>
        AppendLastNeutralCulture = 1 << 2,

        /// <summary>
        /// <para>If a resource is found in a parent resource set of the requested culture, and the traversal of the
        /// culture hierarchy hits neutral cultures, then the resource sets of the neutral cultures will be automatically appended by the found resource.
        /// <br/>Let's consider the following hypothetical culture hierarchy:
        /// <br/><c>en-Runic-GB-Yorkshire (specific) -> en-Runic-GB (specific) -> en-Runic (neutral) -> en (neutral) -> Invariant</c>
        /// <br/>If the requested culture is <c>en-Runic-GB-Yorkshire</c> and the resource is found in the invariant resource set,
        /// then with this option the found resource will be added to the <c>en</c> and <c>en-Runic</c> resource sets.</para>
        /// <para>If the found resource is a <see cref="string"/>, the newly added
        /// value will be prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix"/> property.</para>
        /// <para>If the found non-<see langword="null"/> resource is not a <see cref="string"/>, the found value will be simply copied.</para>
        /// <para>This option is disabled by default.</para>
        /// </summary>
        AppendNeutralCultures = (1 << 3) | AppendFirstNeutralCulture | AppendLastNeutralCulture,

        /// <summary>
        /// <para>If a resource is found in a parent resource set of the requested specific culture,
        /// then the resource set of the requested culture will be automatically appended by the found resource.
        /// <br/>Let's consider the following hypothetical culture hierarchy:
        /// <br/><c>en-Runic-GB-Yorkshire (specific) -> en-Runic-GB (specific) -> en-Runic (neutral) -> en (neutral) -> Invariant</c>
        /// <br/>If the requested culture is <c>en-Runic-GB-Yorkshire</c> and the resource is found in one of its parents,
        /// then with this option the found resource will be added to the requested <c>en-Runic-GB-Yorkshire</c> resource set.</para>
        /// <para>If the found resource is a <see cref="string"/>, the newly added
        /// value will be prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix"/> property.</para>
        /// <para>If the found non-<see langword="null"/> resource is not a <see cref="string"/>, the found value will be simply copied.</para>
        /// <para>This option is disabled by default.</para>
        /// </summary>
        AppendFirstSpecificCulture = 1 << 4,

        /// <summary>
        /// <para>If a resource is found in a parent resource set of the requested specific culture,
        /// then the resource set of the last specific culture in the traversal hierarchy (whose parent is a non-specific culture)
        /// will be automatically appended by the found resource.
        /// <br/>Let's consider the following hypothetical culture hierarchy:
        /// <br/><c>en-Runic-GB-Yorkshire (specific) -> en-Runic-GB (specific) -> en-Runic (neutral) -> en (neutral) -> Invariant</c>
        /// <br/>If the requested culture is <c>en-Runic-GB-Yorkshire</c> and the resource is found in the invariant resource set,
        /// then with this option the found resource will be added to the <c>en-Runic-GB</c> resource set.</para>
        /// <para>If the found resource is a <see cref="string"/>, the newly added
        /// value will be prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix"/> property.</para>
        /// <para>If the found non-<see langword="null"/> resource is not a <see cref="string"/>, the found value will be simply copied.</para>
        /// <para>This option is disabled by default.</para>
        /// </summary>
        AppendLastSpecificCulture = 1 << 5,

        /// <summary>
        /// <para>If a resource is found in a parent resource set of the requested specific culture,
        /// then the resource sets of the visited specific cultures will be automatically appended by the found resource.
        /// <br/>Let's consider the following hypothetical culture hierarchy:
        /// <br/><c>en-Runic-GB-Yorkshire (specific) -> en-Runic-GB (specific) -> en-Runic (neutral) -> en (neutral) -> Invariant</c>
        /// <br/>If the requested culture is <c>en-Runic-GB-Yorkshire</c> and the resource is found in the invariant resource set,
        /// then with this option the found resource will be added to the <c>en-Runic-GB-Yorkshire</c> and <c>en-Runic-GB</c> resource sets.</para>
        /// <para>If the found resource is a <see cref="string"/>, the newly added
        /// value will be prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix"/> property.</para>
        /// <para>If the found non-<see langword="null"/> resource is not a <see cref="string"/>, the found value will be simply copied.</para>
        /// <para>This option is disabled by default.</para>
        /// </summary>
        AppendSpecificCultures = (1 << 6) | AppendFirstSpecificCulture | AppendLastSpecificCulture,

        /// <summary>
        /// <para>If a resource set loaded, <see cref="AppendNeutralCultures"/> and <see cref="AppendSpecificCultures"/> rules are
        /// automatically applied for all resources.</para>
        /// <para>This flag is enabled by default.</para>
        /// </summary>
        AppendOnLoad = 1 << 7,
    }

    internal static class AutoAppendOptionsExtensions
    {
        internal static void CheckOptions(this AutoAppendOptions value)
        {
            // if there is unknown flag except 3 and 6
            if (!(value & ~((AutoAppendOptions) (1 << 3) | (AutoAppendOptions) (1 << 6))).AllFlagsDefined()
                // or flag 3 is on but any neutral is off
                || ((value & (AutoAppendOptions) (1 << 3)) != 0) && (value & AutoAppendOptions.AppendNeutralCultures) != AutoAppendOptions.AppendNeutralCultures
                // or flag 6 is on but any specific is off
                || ((value & (AutoAppendOptions) (1 << 6)) != 0) && (value & AutoAppendOptions.AppendSpecificCultures) != AutoAppendOptions.AppendSpecificCultures)
            {
                throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
            }
        }

        internal static bool IsWidening(this AutoAppendOptions current, AutoAppendOptions newOptions)
        {
            if (current == newOptions)
                return false;

            for (var i = AutoAppendOptions.AppendFirstNeutralCulture; i < AutoAppendOptions.AppendOnLoad; i = (AutoAppendOptions)((int)i << 1))
            {
                if ((current & i) == 0 && (newOptions & i) != 0)
                    return true;
            }

            return false;
        }
    }
}
