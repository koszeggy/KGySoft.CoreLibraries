#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResourceManagerSources.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2017 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

using KGySoft.Libraries;

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents the possible sources of <see cref="HybridResourceManager"/> and <see cref="DynamicResourceManager"/>
    /// classes.
    /// </summary>
    /// <seealso cref="HybridResourceManager.Source"/>
    /// <seealso cref="LanguageSettings.DynamicResourceManagersSource"/>
    /// <seealso cref="DynamicResourceManager.UseLanguageSettings"/>
    public enum ResourceManagerSources
    {
        /// <summary>
        /// Indicates that the resources must be taken only from compiled binary resources.
        /// </summary>
        CompiledOnly,

        /// <summary>
        /// Indicates that the resources must be taken only from .resx XML files.
        /// </summary>
        ResXOnly,

        /// <summary>
        /// Indicates that the resources must be taken both from compiled resources binary and .resx files.
        /// If a resource exists in both sources, then the one in the .resx will be returned.
        /// </summary>
        CompiledAndResX
    }
}
