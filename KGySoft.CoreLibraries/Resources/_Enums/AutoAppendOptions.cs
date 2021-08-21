#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AutoAppendOptions.cs
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
using System.Globalization;
using System.Resources;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents the resource auto append options of a <see cref="DynamicResourceManager"/> instance.
    /// These options are ignored if <see cref="DynamicResourceManager.Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.
    /// </summary>
    /// <seealso cref="DynamicResourceManager"/>
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
        /// value will be initialized by the requested key, prefixed by the <see cref="LanguageSettings.UnknownResourcePrefix">LanguageSettings.UnknownResourcePrefix</see> property.
        /// This new entry can be then merged into other resource sets, too.</para>
        /// <para>If the resource is requested as an <see cref="object"/>, a <see langword="null"/>&#160;value will be added to the resource.
        /// The <see langword="null"/>&#160;value is never merged into the other resource sets because it has a special meaning:
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
        /// value will be prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix">LanguageSettings.UntranslatedResourcePrefix</see> property.</para>
        /// <para>If the found non-<see langword="null"/>&#160;resource is not a <see cref="string"/>, the found value will be simply copied.</para>
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
        /// value will be prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix">LanguageSettings.UntranslatedResourcePrefix</see> property.</para>
        /// <para>If the found non-<see langword="null"/>&#160;resource is not a <see cref="string"/>, the found value will be simply copied.</para>
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
        /// value will be prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix">LanguageSettings.UntranslatedResourcePrefix</see> property.</para>
        /// <para>If the found non-<see langword="null"/>&#160;resource is not a <see cref="string"/>, the found value will be simply copied.</para>
        /// <para>This option includes <see cref="AppendFirstNeutralCulture"/> and <see cref="AppendLastNeutralCulture"/> options.</para>
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
        /// value will be prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix">LanguageSettings.UntranslatedResourcePrefix</see> property.</para>
        /// <para>If the found non-<see langword="null"/>&#160;resource is not a <see cref="string"/>, the found value will be simply copied.</para>
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
        /// value will be prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix">LanguageSettings.UntranslatedResourcePrefix</see> property.</para>
        /// <para>If the found non-<see langword="null"/>&#160;resource is not a <see cref="string"/>, the found value will be simply copied.</para>
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
        /// value will be prefixed by the <see cref="LanguageSettings.UntranslatedResourcePrefix">LanguageSettings.UntranslatedResourcePrefix</see> property.</para>
        /// <para>If the found non-<see langword="null"/>&#160;resource is not a <see cref="string"/>, the found value will be simply copied.</para>
        /// <para>This option includes <see cref="AppendFirstSpecificCulture"/> and <see cref="AppendLastSpecificCulture"/> options.</para>
        /// <para>This option is disabled by default.</para>
        /// </summary>
        AppendSpecificCultures = (1 << 6) | AppendFirstSpecificCulture | AppendLastSpecificCulture,

        /// <summary>
        /// <para>If a resource set is being loaded, <see cref="AppendNeutralCultures"/> and <see cref="AppendSpecificCultures"/> rules are
        /// automatically applied for all resources.</para>
        /// <para>This flag is enabled by default.</para>
        /// </summary>
        AppendOnLoad = 1 << 7,
    }
}
