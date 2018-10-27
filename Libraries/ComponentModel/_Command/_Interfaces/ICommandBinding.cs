#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICommandBinding.cs
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
    /// Represents a binding for a command.
    /// </summary>
    /// <remarks>
    /// <para>Whereas an <see cref="ICommand"/> is a static logic without state, the created binding is a dynamic entity: it has a state,
    /// which can store variable elements (see <see cref="ICommandState"/>), and has sources and targets, which can be added and removed
    /// during the lifetime of the binding.</para>
    /// <para>The binding should be disposed when it is not used anymore so it releases the events it used internally. If more bindings are used it is recommended
    /// to create them by a <see cref="CommandBindingsCollection"/> instance so when it is disposed releases all of the added bindings at once.</para>
    /// <para>For examples see the <strong>Remarks</strong> section of <see cref="ICommand"/>.</para>
    /// </remarks>
    /// <seealso cref="ICommand" />
    /// <seealso cref="ICommandState" />
    /// <seealso cref="CommandBindingsCollection" />
    public interface ICommandBinding : IDisposable
    {
        #region Properties

        /// <summary>
        /// Gets the managed set of states of this <see cref="ICommandBinding"/> instance. Whenever a new source is added or an entry of
        /// the returned <see cref="ICommandState"/> is changed, the entries are applied for all of the sources of the binding.
        /// So for example, if the sources have <c>Enabled</c>, <c>Text</c> and <c>Visible</c> properties, then changing these entries
        /// will be applied to the sources. By default, they are handled as properties on the sources but this behavior can be overridden
        /// by adding custom updaters by the <see cref="AddStateUpdater">AddStateUpdater</see> method.
        /// </summary>
        /// <value>
        /// An <see cref="ICommandState"/> instance that represents the managed states of the binding. Can be also used as a dynamic object
        /// to set and get state entries as properties.
        /// </value>
        ICommandState State { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a source to this <see cref="ICommandBinding"/> instance. The <see cref="State"/> entries will be applied to the new source.
        /// At least one source has to be added to the binding to be able to invoke the underlying <see cref="ICommand"/>.
        /// </summary>
        /// <param name="source">The new source to add.</param>
        /// <param name="eventName">Name of the event on the source, which will trigger the underlying <see cref="ICommand"/>.</param>
        /// <returns>This <see cref="ICommandBinding"/> instance to provide fluent initialization.</returns>
        /// <seealso cref="ICommand"/>
        ICommandBinding AddSource(object source, string eventName);

        /// <summary>
        /// Adds the target to this <see cref="ICommandBinding"/> instance. The underlying <see cref="ICommand"/> will be invoked for each added target.
        /// If no targets are added the command will be invoked with a <see langword="null"/> target.
        /// </summary>
        /// <returns>This <see cref="ICommandBinding"/> instance to provide fluent initialization.</returns>
        ICommandBinding AddTarget(object target);

        /// <summary>
        /// Adds a target getter the target to this <see cref="ICommandBinding"/> instance. Whenever the underlying <see cref="ICommand"/> executes it will evaluate the specified getter callback.
        /// </summary>
        /// <returns>This <see cref="ICommandBinding"/> instance to provide fluent initialization.</returns>
        ICommandBinding AddTarget(Func<object> getTarget);

        /// <summary>
        /// Adds a state updater to the binding. If no updaters are added, then changing the entries of the <see cref="State"/> property will be
        /// applied on all added sources by setting the matching properties. This behavior can be overridden by adding new updaters.
        /// If we don't want to any property synchronization, we can use the <see cref="NullStateUpdater"/>.
        /// </summary>
        /// <param name="updater">The updater to add.</param>
        /// <returns>This <see cref="ICommandBinding"/> instance to provide fluent initialization.</returns>
        ICommandBinding AddStateUpdater(ICommandStateUpdater updater);

        /// <summary>
        /// Removes the specified <paramref name="source"/> from this <see cref="ICommandBinding"/> instance. The used events of the removed source will be released.
        /// </summary>
        /// <param name="source">The source to remove.</param>
        /// <returns><see langword="true"/>, if the source was successfully removed; otherwise, <see langword="false"/>.</returns>
        bool RemoveSource(object source);

        /// <summary>
        /// Removes the specified <paramref name="target"/> from this <see cref="ICommandBinding"/> instance.
        /// </summary>
        /// <param name="target">The target to remove.</param>
        /// <returns><see langword="true"/>, if the target was successfully removed; otherwise, <see langword="false"/>.</returns>
        bool RemoveTarget(object target);

        /// <summary>
        /// Removes the specified state updater. The removed updater will not be disposed.
        /// </summary>
        /// <param name="updater">The updater to remove.</param>
        /// <returns><see langword="true"/>, if the updater was successfully removed; otherwise, <see langword="false"/>.</returns>
        bool RemoveStateUpdater(ICommandStateUpdater updater);

        #endregion
    }
}
