#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICommandStateUpdater.cs
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

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an updater for the <see cref="ICommandState"/> entries that apply the state values on the command source instances.
    /// For example, if the command source is a UI element such as a button or menu item, then the <see cref="ICommandState.Enabled"/> property
    /// or any arbitrary state (eg. text, image, shortcut, etc.) can be applied to the sources.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <para>If a binding has no updaters, then the states are not synchronized back to the sources.</para>
    /// <para>A state updater can be added to a binding by the <see cref="ICommandBinding.AddStateUpdater">ICommandBinding.AddStateUpdater</see> method.
    /// Multiple updaters can be added so if the first one cannot apply a state entry, then the second one will be used as a fallback and so on.</para>
    /// <para>If state entries represent properties on the source use can add the <see cref="PropertyCommandStateUpdater"/> to the <see cref="ICommandBinding"/>.</para>
    /// <para>If you want to seal the falling back logic of the updaters you can use the <see cref="NullStateUpdater"/> after the last updater you want to allow to work.
    /// If the <see cref="NullStateUpdater"/> is the first added updater, then synchronization of the states will be completely disabled even if other updaters are chained.</para>
    /// <note type="tip">See the <strong>Remarks</strong> section of the <see cref="ICommand"/> interface for details and examples about commands.</note>
    /// </remarks>
    /// <seealso cref="ICommand" />
    /// <seealso cref="ICommandState" />
    /// <seealso cref="ICommandBinding" />
    public interface ICommandStateUpdater : IDisposable
    {
        #region Methods

        /// <summary>
        /// Tries to apply the specified state on the command source. If returns <see langword="false"/>, then the possible chained other updaters can
        /// try to update the state.
        /// </summary>
        /// <param name="commandSource">The command source, whose state should be applied.</param>
        /// <param name="stateName">Name of the state. The default updater handles it as a property on the <paramref name="commandSource"/>.</param>
        /// <param name="value">The new value of the state to be applied.</param>
        /// <returns><see langword="true"/>&#160;if the state was applied successfully; <see langword="false"/>&#160;if other possibly chained updaters or the
        /// default updater can try to apply the new state.</returns>
        bool TryUpdateState(object commandSource, string stateName, object? value);

        #endregion
    }
}
