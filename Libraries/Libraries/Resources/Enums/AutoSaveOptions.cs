using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KGySoft.Libraries.Resources
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
        /// Represents that no auto saving.
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
    }
}
