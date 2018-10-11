#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICommandStateUpdater.cs
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

using System;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents an updater for the <see cref="ICommandState"/> entries that can override the default behavior of synchronizing the state properties with the command sources.
    /// A state updater can be added to a binding by the <see cref="ICommandBinding.AddStateUpdater">ICommandBinding.AddStateUpdater</see> method.
    /// If we don't want to any property synchronization, we can use the <see cref="NullStateUpdater"/>.
    /// </summary>
    /// <seealso cref="ICommand" />
    /// <seealso cref="ICommandState" />
    /// <seealso cref="ICommandBinding" />
    public interface ICommandStateUpdater : IDisposable
    {
        #region Methods

        /// <summary>
        /// Tries to apply the specified state on the command source. If returns <see langword="false"/>, then the possible chained other updaters can
        /// try to update the state. If no other updaters are added to the <see cref="ICommandBinding"/> instance, then the default updater tries to
        /// set the property on the <paramref name="commandSource"/> with name of <paramref name="stateName"/> to <paramref name="value"/>.
        /// </summary>
        /// <param name="commandSource">The command source, whose state should be applied.</param>
        /// <param name="stateName">Name of the state. The default updater handles it as a property on the <paramref name="commandSource"/>.</param>
        /// <param name="value">The new value of the state to be applied.</param>
        /// <returns><see langword="true"/> if the state was applied successfully; <see langword="false"/> if other possibly chained updaters or the
        /// default updater can try to apply the new state.</returns>
        bool TryUpdateState(object commandSource, string stateName, object value);

        #endregion
    }
}
