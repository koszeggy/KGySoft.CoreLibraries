#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICommand.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#if NET35 || NET40
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents a command, which can be used to create a binding between an event of one or more sources and zero or more target objects. Can be used easily to bind
    /// events with targets with any technology.
    /// <br/>See the <strong>Remarks</strong> section for details.
    /// </summary>
    /// <remarks>
    /// <para>Unlike the <see cref="System.Windows.Input.ICommand">System.Windows.Input.ICommand</see> type, this <see cref="ICommand"/> represents a stateless
    /// command so the implementations are best to be accessed via static members. The command states (such as <c>Enabled</c> or any other status) belong to
    /// the created binding represented by the <see cref="ICommandBinding"/> interface and can be accessed by the <see cref="ICommandBinding.State">ICommandBinding.State</see> property,
    /// which returns an <see cref="ICommandState"/> instance.</para>
    /// <para>To implement a command by using a delegate you can also choose one of the four pairs of predefined classes: <see cref="SimpleCommand"/>/<see cref="SimpleCommand{TParam}"/>,
    /// <see cref="TargetedCommand{TTarget}"/>/<see cref="TargetedCommand{TTarget,TParam}"/>, <see cref="SourceAwareCommand{TEventArgs}"/>/<see cref="SourceAwareCommand{TEventArgs,TParam}"/>
    /// and <see cref="SourceAwareTargetedCommand{TEventArgs, TTarget}"/>/<see cref="SourceAwareTargetedCommand{TEventArgs,TTarget,TParam}"/> depending whether the command is parameterized,
    /// targets specific objects and behaves differently based on the source's state or event arguments.</para>
    /// <para>A binding can be created by the <see cref="O:KGySoft.ComponentModel.Command.CreateBinding">Commands.CreateBinding</see> methods or by the <see cref="CommandBindingsCollection"/> class.
    /// When a binding or a collection of bindings are disposed all of the event subscriptions are released, which makes the cleanup really simple.</para>
    /// <example>
    /// <note type="tip"><list type="bullet">
    /// <item>Try also <a href="https://dotnetfiddle.net/7b0lFq" target="_blank">online</a>.</item>
    /// <item>For a more detailed step-by-step guide see the <strong>Command Binding</strong> section at
    /// the <a href="https://kgysoft.net/corelibraries#command-binding" target="_blank">Project Site</a>.</item>
    /// </list></note>
    /// <para>The following examples demonstrate how to define different kind of commands:
    /// <code lang="C#"><![CDATA[
    /// public static partial class MyCommands
    /// {
    ///     // A simple command with no target and ignored source: (assumes we have an ExitCode state)
    ///     public static ICommand CloseApplicationCommand =>
    ///         new SimpleCommand(state => Environment.Exit((int)state["ExitCode"])); // or: .Exit(state.AsDynamic.ExitCode)
    /// 
    ///     // A source aware command, which can access the source object and the triggering event data
    ///     public static ICommand LogMouseCommand =>
    ///         new SourceAwareCommand<MouseEventArgs>(source => Debug.WriteLine($"Mouse coordinates: {source.EventArgs.X}; {source.EventArgs.Y}"));
    /// 
    ///     // A targeted command (also demonstrates how to change the command state of another command):
    ///     public static ICommand ToggleCommandEnabled =>
    ///         new TargetedCommand<ICommandState>((state, targetState) => targetState.Enabled = !targetState.Enabled);
    /// 
    ///     // A source aware targeted command:
    ///     public static ICommand ProcessKeysCommand => new SourceAwareTargetedCommand<KeyEventArgs, Control>(OnProcessKeysCommand);
    /// 
    ///     private static void OnProcessKeysCommand(ICommandSource<KeyEventArgs> source, Control target)
    ///     {
    ///         // do something with target by source.EventArgs
    ///     }
    /// }]]></code></para>
    /// <para>And a binding for a command can be created in an application, with any kind of UI, which uses events, or even without any UI: only event sources are needed.
    /// <code lang="C#"><![CDATA[
    /// public class MyView : SomeViewBaseWithEvents // base can be a Window in WPF or a Form in WindowsForms or simply any component with events.
    /// {
    ///     private ICommandBinding exitBinding;
    /// 
    ///     private CommandBindingsCollection commandBindings = new CommandBindingsCollection();
    /// 
    ///     public MyView()
    ///     {
    ///         // ...some initialization of our View...
    /// 
    ///         // Simplest case: using the CreateBinding extension on ICommand.
    ///         // Below we assume we have a menu item with a Click event.
    ///         // We set also the initial status. By adding the property state updater the
    ///         // states will be applied on the source as properties.
    ///         exitBinding = MyCommands.CloseApplicationCommand.CreateBinding(
    ///             new Dictionary<string, object>
    ///             {
    ///                 { "Text", "Exit Application" },
    ///                 { "ShortcutKeys", Keys.Alt | Keys.F4 },
    ///                 { "ExitCode", 0 },
    ///             })
    ///            .AddStateUpdater(PropertyCommandStateUpdater.Updater)
    ///            .AddSource(menuItemExit, "Click");
    /// 
    ///         // If we add the created bindings to a CommandBindingsCollection, then all of them can be disposed at once by disposing the collection.
    ///         commandBindings.Add(exitBinding);
    /// 
    ///         // We can create a binding by the Add methods of the collection, too:
    ///         // As we added the property state updater to the exitBinding the menuItemExit.Enabled property will reflect the command state.
    ///         var toggleEnabledBinding = commandBindings.Add(MyCommands.ToggleCommandEnabledCommand, buttonToggle, "Click", exitBinding.State);
    /// 
    ///         // The line above can be written by a more descriptive fluent syntax (and that's how multiple sources can be added):
    ///         var toggleEnabledBinding = commandBindings.Add(MyCommands.ToggleCommandEnabledCommand)
    ///             .AddSource(buttonToggle, nameof(Button.Click))
    ///             .AddTarget(exitBinding.State);
    /// 
    ///         // If we set the state of a binding with a property updater it will be applied for all sources (only if a matching property exists):
    ///         exitBinding.State["Text"] = "A new text for the exit command";
    /// 
    ///         // Or as dynamic:
    ///         toggleEnabledBinding.State.AsDynamic.Text = "A new text for the exit command";
    ///     }
    /// 
    ///     protected override Dispose(bool disposing)
    ///     {
    ///          // disposing a CommandBindingsCollection will release all of the internal event subscriptions at once
    ///         if (disposing)
    ///             commandBindings.Dispose();
    /// 
    ///         base.Dispose(disposing);
    ///     }
    /// }]]></code></para>
    /// <para>
    /// Commands can also have parameter, which is evaluated whenever the command is triggered:
    /// <code lang="C#"><![CDATA[
    /// // A parameterized and targeted command:
    /// public static ICommand SetBackColorCommand =>
    ///     new TargetedCommand<Control, Color>((target, value) => target.BackColor = value);
    ///
    /// // [...]
    /// 
    /// // The parameter is evaluated only once whenever the command is triggered but SetBackColorCommand
    /// // will be invoked three times (once for each target) with the same parameter value.
    /// commandBindings.Add(MyCommands.SetBackColorCommand)
    ///     .AddSource(myViewModel, nameof(myViewModel.SeverityChanged)) // whatever source event
    ///     .WithParameter(() => GetSomeColor(myViewModel.Severity)) // specifying a callback to return a parameter value
    ///     .AddTarget(this)
    ///     .AddTarget(panelInfo) // now multiple targets will be set by the same parameter
    ///     .AddTarget(buttonDoSomething);]]></code></para>
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
        /// <param name="source">An <see cref="ICommandSource"/> object containing information about the source of the command.</param>
        /// <param name="state">An <see cref="ICommandState"/> instance containing the state of the current command binding. The state can be changed during the execution.</param>
        /// <param name="target">The target of the execution. Can be <see langword="null"/>&#160;if the binding has no targets.
        /// If the command binding has multiple targets the <see cref="Execute">Execute</see> method will be invoked multiple times.</param>
        /// <param name="parameter">The parameter of the command. Can be <see langword="null"/>&#160;if the binding has no parameter.
        /// Evaluated once per triggering the command. Even if the binding has multiple targets the <see cref="Execute">Execute</see> method
        /// is invoked with the same <paramref name="parameter"/> value for each target.</param>
        void Execute(ICommandSource source, ICommandState state, object? target, object? parameter);

        #endregion
    }
}
