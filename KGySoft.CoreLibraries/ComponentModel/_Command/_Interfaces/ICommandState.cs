#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICommandState.cs
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
    /// <para>The <see cref="Enabled"/> state, which is also predefined as a property, has a special function. By setting the <see cref="Enabled"/> property
    /// the execution of the command can be disabled or enabled.</para>
    /// <para>If a binding has no updaters, then the states are not synchronized back to the sources.</para>
    /// <para>A state updater can be added to a binding by the <see cref="ICommandBinding.AddStateUpdater">ICommandBinding.AddStateUpdater</see> method.
    /// Multiple updaters can be added so if the first one cannot apply a state entry, then the second one will be used as a fallback and so on.</para>
    /// <para>If state entries represent properties on the source you can add the <see cref="PropertyCommandStateUpdater"/> to the <see cref="ICommandBinding"/>
    /// so changing the <c>Enabled</c>, <c>Text</c>, <c>Image</c>, <c>Shortcut</c>, etc. state entries will change the same properties on the command sources as well.</para>
    /// <note type="tip">See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.</note>
    /// </remarks>
    /// <seealso cref="ICommand" />
    /// <seealso cref="ICommandStateUpdater" />
    /// <seealso cref="ICommandBinding" />
    public interface ICommandState : IDictionary<string, object?>, INotifyPropertyChanged
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
