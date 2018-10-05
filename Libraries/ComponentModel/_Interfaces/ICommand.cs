#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICommand.cs
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
    /// Represents a command, which can be used to create a binding between an event of one or more sources and zero or more target objects. Can be used easily to bind
    /// events with targets with any technology, even in Windows Forms environment or without any UI.
    /// See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <para>Unlike the <see cref="System.Windows.Input.ICommand">System.Windows.Input.ICommand</see> type, this <see cref="ICommand"/> represents a stateless
    /// command so the implementations are best to be accessed via static members. The command states (such as <c>Enabled</c> or any other status) belong to
    /// the created binding represented by the <see cref="ICommandBinding"/> interface and can be accessed by the <see cref="ICommandBinding.State">ICommandBinding.State</see> property,
    /// which returns an <see cref="ICommandState"/> instance.</para>
    /// <para>To implement a command by using a delegate you can also choose one of the four predefined classes: <see cref="SimpleCommand"/>, <see cref="TargetedCommand{TTarget}"/>,
    /// <see cref="SourceAwareCommand{TEventArgs}"/> and <see cref="SourceAwareTargetedCommand{TEventArgs, TTarget}"/> depending whether the command targets specific objects and
    /// behaves differently based on the source's state or event arguments.</para>
    /// <para>A binding can be created by the <see cref="Commands.CreateBinding">Commands.CreateBinding</see> method or by the <see cref="CommandBindingsCollection"/> class.
    /// When a binding or a collection of bindings are disposed all of the event subscriptions are released, which makes the cleanup really simple.</para>
    /// <example>
    /// The following examples demonstrate how to define different kind of commands:
    /// <code lang="C#"><![CDATA[
    /// public static class MyCommands
    /// {
    ///     // A lazy initialized simple command with no target and ignored source:
    ///     private static ICommand closeApplicationCommand;
    ///     public static ICommand CloseApplication => closeApplicationCommand ?? (closeApplicationCommand = 
    ///         new SimpleCommand(() => Environment.Exit(0)));
    /// 
    ///     // A source aware command to use arguments for the command:
    ///     private static ICommand closeApplicationWithExitCodeCommand;
    ///     public static ICommand CloseApplicationWithExitCode => closeApplicationWithExitCodeCommand ?? (closeApplicationWithExitCodeCommand =
    ///         new SourceAwareCommand<EventArgs>((source, eventName, e) => Environment.Exit((MySourceObject)source).ExitCode));
    /// 
    ///     // A targeted command (also demonstrates how to change a command state by another command):
    ///     private static ICommand toggleCommandEnabledCommand;
    ///     public static ICommand ToggleCommandEnabled => toggleCommandEnabledCommand ?? (toggleCommandEnabledCommand =
    ///         new TargetedCommand<ICommandState>(state => state.Enabled = !state.Enabled));
    /// 
    ///     // A source aware targeted command:
    ///     private static ICommand processKeysCommand;
    ///     public static ICommand ProcessKeys => processKeysCommand ?? (processKeysCommand =
    ///         new SourceAwareTargetedCommand<KeyEventArgs, Control>((source, eventName, e, target) => HandleKeys(e.KeyData, target)));
    /// }
    /// ]]></code>
    /// And a binding for a command can be created in an application, with any kind of UI, which uses events, or even without any UI: only event sources are needed.
    /// <code lang="C#"><![CDATA[
    /// public class MyView : SomeViewBaseWithEvents // base can be a Window in WPF or a Form in WindowsForms or simply any component with events.
    /// {
    ///     private ICommandBinding exitBinding;
    /// 
    ///     private CommandBindingsCollection commandBindings = new CommandBindingsCollection();
    /// 
    ///     public SomeViewBaseWithEvents()
    ///     {
    ///         // ...some initialization of our View...
    /// 
    ///         // Simplest case: using the CreateBinding extension on ICommand.
    ///         // Below we assume we have a menu item with a Click event. We initialize its Text and shortcut as well.
    ///         exitBinding = MyBindings.CloseApplication.CreateBinding(menuItemExit, "Click",
    ///             new Dictionary<string, object>{ { "Text", "Exit Application" }, { "ShortcutKeys", Keys.Alt | Keys.F4 } });
    /// 
    ///         // If we add the created bindings to a CommandBindingsCollection, then all of them can be disposed at once by disposing the collection.
    ///         commandBindings.Add(exitBinding);
    /// 
    ///         // We can create a binding by the Add methods of the collection, too:
    ///         var toggleEnabledBinding = commandBindings.Add(MyCommands.ToggleCommandEnabled, checkBoxToggle, nameof(CheckBox.Checked),
    ///             new Dictionary<string, object>{ { "Text", "Toggle Enabled of Exit" } }, exitBinding.State);
    /// 
    ///         // The line above can be written by a more descriptive fluent syntax (and that's how multiple sources can be added):
    ///         var toggleEnabledBinding = commandBindings.Add(MyCommands.ToggleCommandEnabled)
    ///             .AddSource(checkBoxToggle, nameof(CheckBox.Checked))
    ///             .AddTarget(exitBinding.State);
    /// 
    ///         // If we now set the state of the binding it will be applied for all sources (only if a matching property exists):
    ///         toggleEnabledBinding.State["Text"] = "Toggle Enabled of Exit";
    /// 
    ///         // Or as dynamic:
    ///         toggleEnabledBinding.State.AsDynamic.Text = "Toggle Enabled of Exit";
    ///     }
    /// 
    ///     protected override Dispose(bool disposing)
    ///     {
    ///         base.Dispose(disposing);
    /// 
    ///         // disposing a CommandBindingsCollection will release all of the internal event subscriptions at once
    ///         if (disposing)
    ///             commandBindings.Dispose();
    ///     }
    /// }
    /// ]]></code>
    /// </example>
    /// </remarks>
    /// <seealso cref="ICommandBinding"/>
    /// <seealso cref="CommandBindingsCollection"/>
    /// <seealso cref="ICommandState"/>
    /// <seealso cref="SimpleCommand"/>
    /// <seealso cref="TargetedCommand{TTarget}"/>
    /// <seealso cref="SourceAwareCommand{TEventArgs}"/>
    /// <seealso cref="SourceAwareTargetedCommand{TEventArgs,TTarget}"/>
    public interface ICommand
    {
        #region Methods

        /// <summary>
        /// Executes the command invoked by the specified <paramref name="source"/> for the specified <paramref name="target"/>.
        /// </summary>
        /// <param name="source">The source invocation.</param>
        /// <param name="triggeringEvent">The triggering event of the source object.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data. The actual type depends on the triggering event.</param>
        /// <param name="target">The target of the execution. Can be <see langword="null"/> if the binding contains no targets.</param>
        void Execute(object source, string triggeringEvent, EventArgs e, object target);

        #endregion
    }
}
