#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PropertyCommandStateUpdater.cs
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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using KGySoft.Reflection;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides an updater for the <see cref="ICommandState"/> entries, which treats state entries as properties on the command sources.
    /// When a state entry in the <see cref="ICommandState"/> changes, this updater tries to set the properties of the same name on the bound sources.
    /// For example, if a command represents a UI action bound to a menu item or a button (or both), then changing the <see cref="ICommandState.Enabled"/>
    /// property changes the <c>Enabled</c> property of the bound sources as well. You can adjust the text, shortcuts, associated image, checked state, etc. of
    /// the sources similarly.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <para>A state updater can be added to a binding by the <see cref="ICommandBinding.AddStateUpdater">ICommandBinding.AddStateUpdater</see> method.</para>
    /// <para>If a state entry does not represent an existing property on a source, there will no error occur.</para>
    /// <para>The updater considers both <see cref="ICustomTypeDescriptor"/> properties and reflection instance properties.</para>
    /// </remarks>
    /// <seealso cref="ICommandStateUpdater" />
    public sealed class PropertyCommandStateUpdater : ICommandStateUpdater
    {
        #region Properties

        /// <summary>
        /// Gets the <see cref="PropertyCommandStateUpdater"/> instance.
        /// </summary>
        public static PropertyCommandStateUpdater Updater { get; } = new PropertyCommandStateUpdater();

        #endregion

        #region Constructors

        private PropertyCommandStateUpdater()
        {
        }

        #endregion

        #region Methods

        bool ICommandStateUpdater.TryUpdateState(object commandSource, string stateName, object? value)
            => Reflector.TrySetProperty(commandSource, stateName, value);

        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Does nothing and the class is sealed.")]
        void IDisposable.Dispose()
        {
        }

        #endregion
    }
}
