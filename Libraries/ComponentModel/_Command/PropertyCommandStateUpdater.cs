using System;
using System.ComponentModel;
using KGySoft.Reflection;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides an updater for the <see cref="ICommandState"/> entries, which treats state entries as properties on the command sources.
    /// When a state entry in the <see cref="ICommandState"/> changes, this updater tries to set the properties of the same name on the bound sources.
    /// For example, if a command represents a UI action bound to a menu item or a button (or both), then changing the <see cref="ICommandState.Enabled"/>
    /// property changes the <c>Enabled</c> property of the bound sources as well. You can adjust the text, shortcuts, associated image, checked state, etc. of
    /// the sources similarly.
    /// </summary>
    /// <remarks>
    /// <para>A state updater can be added to a binding by the <see cref="ICommandBinding.AddStateUpdater">ICommandBinding.AddStateUpdater</see> method.</para>
    /// <para>If a state entry does not represent an existing property on a source, there will no error occur.</para>
    /// <para>The updater considers both <see cref="ICustomTypeDescriptor"/> properties and reflection instance properties.</para>
    /// </remarks>
    /// <seealso cref="ICommandStateUpdater" />
    public sealed class PropertyCommandStateUpdater : ICommandStateUpdater
    {
        private PropertyCommandStateUpdater()
        {
        }

        /// <summary>
        /// Gets the <see cref="PropertyCommandStateUpdater"/> instance.
        /// </summary>
        public static PropertyCommandStateUpdater Updater { get; } = new PropertyCommandStateUpdater();

        bool ICommandStateUpdater.TryUpdateState(object commandSource, string stateName, object value) 
            => Reflector.TrySetProperty(commandSource, stateName, value);

        void IDisposable.Dispose()
        {
        }
    }
}
