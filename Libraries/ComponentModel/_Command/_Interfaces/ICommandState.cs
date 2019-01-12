#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICommandState.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Collections.Generic;
using System.ComponentModel;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents the states of a command for a specific command binding. When a state value is set (eg. <see cref="Enabled"/>) it can be applied for all of the
    /// command sources. By default, no application occurs but this can be overridden if an <see cref="ICommandStateUpdater"/> is added to the binding by the
    /// <see cref="ICommandBinding.AddStateUpdater">ICommandBinding.AddStateUpdater</see> method.
    /// For example, if the command sources are UI elements (eg. a button and a menu item), then the <see cref="Enabled"/>
    /// or any arbitrary state (eg. text, image, shortcut, etc.) can be applied to the sources as properties.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <para>If a binding has no updaters, then the states are not synchronized back to the sources.</para>
    /// <para>A state updater can be added to a binding by the <see cref="ICommandBinding.AddStateUpdater">ICommandBinding.AddStateUpdater</see> method.
    /// Multiple updaters can be added so if the first one cannot apply a state entry, then the second one will be used as a fallback and so on.</para>
    /// <para>If state entries represent properties on the source use can add the <see cref="PropertyCommandStateUpdater"/> to the <see cref="ICommandBinding"/>.</para>
    /// <para>See the <strong>Remarks</strong> section of <see cref="ICommand"/> for examples.</para>
    /// </remarks>
    /// <seealso cref="ICommand" />
    /// <seealso cref="ICommandStateUpdater" />
    /// <seealso cref="ICommandBinding" />
    public interface ICommandState : IDictionary<string, object>, INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// Gets or sets whether the command is enabled in the current binding.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        /// <value><see langword="true"/>&#160;if the command enabled and can be executed; otherwise, <see langword="false"/>.</value>
        bool Enabled { get; set; }

#if !NET35
        /// <summary>
        /// Gets the state as a dynamic object so the states can be set by simple property setting syntax.
        /// </summary>
        dynamic AsDynamic { get; }
#endif

        #endregion
    }
}
