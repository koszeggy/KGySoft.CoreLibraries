#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PropertyCommandStateUpdater.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
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
    /// the sources in a similar way.
    /// </summary>
    /// <remarks>
    /// <para>A state updater can be added to a binding by the <see cref="ICommandBinding.AddStateUpdater">ICommandBinding.AddStateUpdater</see> method.</para>
    /// <para>If a state entry does not represent an existing property on a source, there will no error occur.</para>
    /// <para>If the command source is an object instance, the updater considers both <see cref="ICustomTypeDescriptor"/> properties and reflection instance properties.</para>
    /// <para>If the command source is a <see cref="Type"/>, the updater considers reflection static properties of the source type.</para>
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

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "It makes little sense to annotate an explicit interface implementation if the interface member is not annotated, but the generic ICommandBinding.AddSource methods preserve properties.")]
        [UnconditionalSuppressMessage("TrimAnalysis", "IL2067:TargetArgumentDynamicallyAccessedMemberTypesAnnotationMismatch",
            Justification = "It makes little sense to annotate an explicit interface implementation if the interface member is not annotated, but the generic ICommandBinding.AddSource methods preserve properties.")]
        bool ICommandStateUpdater.TryUpdateState(object commandSource, string stateName, object? value) => commandSource is Type type
            ? Reflector.TrySetProperty(type, stateName, value)
            : Reflector.TrySetProperty(commandSource, stateName, value);

        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Does nothing and the class is sealed.")]
        void IDisposable.Dispose()
        {
        }

        #endregion
    }
}
