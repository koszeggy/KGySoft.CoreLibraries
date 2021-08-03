#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AutoSaveOptions.cs
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

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents the auto saving options of a <see cref="DynamicResourceManager"/> instance.
    /// </summary>
    /// <seealso cref="DynamicResourceManager.AutoSave"/>
    /// <seealso cref="DynamicResourceManager.AutoSaveError"/>
    /// <seealso cref="LanguageSettings.DynamicResourceManagersAutoSave"/>
    [Flags]
    public enum AutoSaveOptions
    {
        /// <summary>
        /// Represents no auto saving.
        /// </summary>
        None,

        /// <summary>
        /// Represents the auto saving of resources when <see cref="LanguageSettings.DisplayLanguage">LanguageSettings.DisplayLanguage</see> is changed.
        /// </summary>
        LanguageChange = 1,

        /// <summary>
        /// Represents the auto saving of resources when application exists or the current application domain is unloaded.
        /// </summary>
        DomainUnload = 1 << 1,

        /// <summary>
        /// Represents the auto saving of resources when <see cref="DynamicResourceManager.Source">DynamicResourceManager.Source</see> or
        /// <see cref="LanguageSettings.DynamicResourceManagersSource">LanguageSettings.DynamicResourceManagersSource</see> is changed.
        /// </summary>
        SourceChange = 1 << 2,

        /// <summary>
        /// Represents the auto saving of resources when a <see cref="DynamicResourceManager"/> is being disposed explicitly.
        /// </summary>
        Dispose = 1 << 3
    }
}
