#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CommandsTest.cs
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
using System.Globalization;
using System.IO;

using KGySoft.ComponentModel;
using KGySoft.Diagnostics;
using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.ComponentModel
{
    [TestFixture]
    public class CommandsTest : TestBase
    {
        #region Nested classes

        #region TestClass class

        private class TestClass : ObservableObjectBase
        {
            #region Events

            internal event EventHandler StringPropChangedTestEvent;

            #endregion

            #region Properties

            public string StringProp { get => Get<string>(); set => Set(value); }
            public int IntProp { get => Get<int>(); set => Set(value); }

            #endregion

            #region Methods

            protected internal override void OnPropertyChanged(PropertyChangedExtendedEventArgs e)
            {
                base.OnPropertyChanged(e);
                if (e.PropertyName == nameof(StringProp))
                    StringPropChangedTestEvent?.Invoke(this, EventArgs.Empty);
            }

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        public static readonly ICommand LogPropChangeCommand
            = new SourceAwareTargetedCommand<PropertyChangedExtendedEventArgs, TextWriter>((src, state, writer) =>
            {
                writer.WriteLine($"{src.EventArgs.PropertyName}: {src.EventArgs.OldValue} -> {src.EventArgs.NewValue}");
                state["TriggerCount"] = state.GetValueOrDefault<int>("TriggerCount") + 1;
            });

        public static readonly ICommand LogLanguageChangeCommand
            = new TargetedCommand<TextWriter>((state, writer) =>
            {
                writer.WriteLine($"New display language: {LanguageSettings.DisplayLanguage.Name}");
                state[nameof(LanguageSettings.DisplayLanguage)] = LanguageSettings.DisplayLanguage.Name;
            });

        #endregion

        #region Methods

        [Test]
        public void CreateBindingAndTriggerCommand()
        {
            var test = new TestClass();
            ICommandBinding binding = LogPropChangeCommand.CreateBinding(test, nameof(test.PropertyChanged), Console.Out);

            Assert.IsFalse(binding.State.ContainsKey("TriggerCount"));
            test.StringProp = "Alpha";
            Assert.AreEqual(2, binding.State["TriggerCount"]); // IsModified, TestProp

            // not triggered again after disposing
            binding.Dispose();
            test.StringProp = "Beta";
            Assert.AreEqual(2, binding.State["TriggerCount"]);

            // creating alternatively (command itself was not disposed)
            binding = LogPropChangeCommand.CreateBinding()
                .AddSource(test, nameof(test.PropertyChanged))
                .AddTarget(Console.Out);

            Assert.IsFalse(binding.State.ContainsKey("TriggerCount"));
            test.StringProp = "Gamma";
            Assert.AreEqual(1, binding.State["TriggerCount"]); // new state, only TestProp changed

            binding.InvokeCommand(this, "Fake event name", new PropertyChangedExtendedEventArgs("old", "new", "Fake property name"));
            Assert.AreEqual(2, binding.State["TriggerCount"]); // our manual trigger

            // binding to static event
            binding.Dispose();
            binding = LogLanguageChangeCommand.CreateBinding(typeof(LanguageSettings), nameof(LanguageSettings.DisplayLanguageChanged), Console.Out);
            CultureInfo origLanguage = LanguageSettings.DisplayLanguage;
            LanguageSettings.DisplayLanguage = CultureInfo.InvariantCulture;
            LanguageSettings.DisplayLanguage = origLanguage;
            Assert.AreEqual(origLanguage.Name, binding.State[nameof(LanguageSettings.DisplayLanguage)]);
            binding.Dispose();
        }

        [Test]
        public void CreateByCommandBindingsCollection()
        {
            var test = new TestClass();
            var bindings = new CommandBindingsCollection();
            ICommandBinding binding = bindings.Add(LogPropChangeCommand)
                    .AddSource(test, nameof(test.PropertyChanged))
                    .AddTarget(Console.Out);

            Assert.IsFalse(binding.State.ContainsKey("TriggerCount"));
            test.StringProp = "Alpha";
            Assert.AreEqual(2, binding.State["TriggerCount"]); // IsModified, TestProp

            // not triggered again after disposing the collection
            bindings.Dispose();
            test.StringProp = "Beta";
            Assert.AreEqual(2, binding.State["TriggerCount"]);

            // explicit dispose before disposing the collection is not a problem
            binding = bindings.Add(LogPropChangeCommand)
                .AddSource(test, nameof(test.PropertyChanged))
                .AddTarget(Console.Out);
            binding.Dispose();
            bindings.Dispose();
        }

        [Test]
        public void EnabledTest()
        {
            var test = new TestClass();
            var state = new CommandState { Enabled = false };
            ICommandBinding binding = LogPropChangeCommand.CreateBinding(state)
                    .AddSource(test, nameof(test.PropertyChanged))
                    .AddTarget(Console.Out);

            // Disabled command is not executed
            Assert.IsFalse(state.ContainsKey("TriggerCount"));
            test.StringProp = "Alpha";
            Assert.IsFalse(state.ContainsKey("TriggerCount"));

            // enabling by push
            state.Enabled = true;
            test.StringProp = "Beta";
            Assert.AreEqual(1, binding.State["TriggerCount"]);

            // disabling by poll
            binding.Executing += (sender, args) => args.State.Enabled = false;
            test.StringProp = "Gamma";
            Assert.AreEqual(1, binding.State["TriggerCount"]);
        }

        [Test]
        public void StateUpdaterTest()
        {
            var test = new TestClass();
            ICommandBinding binding = LogPropChangeCommand.CreateBinding()
                    .AddSource(test, nameof(test.PropertyChanged))
                    .AddTarget(Console.Out)
                    .AddStateUpdater(PropertyCommandStateUpdater.Updater);

            // setting state property, which is synced back to source
            Assert.IsFalse(binding.State.ContainsKey("TriggerCount"));
            binding.State[nameof(test.StringProp)] = "ByUpdater";
            Assert.AreEqual("ByUpdater", test.StringProp);
            Assert.AreEqual(2, binding.State["TriggerCount"]);
        }

        [Test]
        public void DynamicTargetTest()
        {
            var test = new TestClass();
            ICommandBinding binding = LogPropChangeCommand.CreateBinding()
                    .AddSource(test, nameof(test.PropertyChanged))
                    .AddTarget(() => Console.Out);

            // setting state property, which is synced back to source
            Assert.IsFalse(binding.State.ContainsKey("TriggerCount"));
            test.StringProp = "Alpha";
            Assert.AreEqual(2, binding.State["TriggerCount"]); // IsModified, TestProp
        }

        [Test]
        public void PropertyBindingTest()
        {
            var source = new TestClass { StringProp = "Alpha" };
            var target = new TestClass { StringProp = "Beta" };
            Assert.AreNotEqual(source.StringProp, target.StringProp);

            // they are synced immediately
            ICommandBinding binding = source.CreatePropertyBinding(nameof(source.StringProp), nameof(target.StringProp), target);
            Assert.AreEqual(source.StringProp, target.StringProp);

            // or when source changes
            source.StringProp = "Gamma";
            Assert.AreEqual(source.StringProp, target.StringProp);

            // but only until binding is disposed
            binding.Dispose();
            source.StringProp = "Delta";
            Assert.AreNotEqual(source.StringProp, target.StringProp);
        }

        [Test]
        public void BindingErrorTest()
        {
            const string bindingFormatErrorTestMessage = nameof(bindingFormatErrorTestMessage);
            var source = new TestClass { StringProp = "42" };
            var target = new TestClass();
            CommandBindingErrorEventArgs errorEventArgs = null;

            static object FormatStringAsInt(object value) => Int32.TryParse((string)value, out int result) ? result : throw new ArgumentException(bindingFormatErrorTestMessage);

            void HandleBindingError(object sender, CommandBindingErrorEventArgs e)
            {
                Console.WriteLine($"{e.Context}: {e.Error.Message}");
                errorEventArgs = e;
                e.Handled = true;
            }

            // creating a binding from a string to int
            using ICommandBinding binding = source.CreatePropertyBinding(nameof(source.StringProp), nameof(target.IntProp), FormatStringAsInt, target);
            binding.Executing += (_, _) => errorEventArgs = null;
            binding.Error += HandleBindingError;

            // creating a binding already triggered an execution
            Assert.AreEqual(42, target.IntProp);
            Assert.IsNull(errorEventArgs);

            // setting invalid number: error, previous target value is preserved
            source.StringProp = "-";
            Assert.AreEqual(42, target.IntProp);
            Assert.IsNotNull(errorEventArgs);
            Assert.IsNotNull(errorEventArgs.Error);
            Assert.AreEqual(CommandBindingErrorContext.CommandExecute, errorEventArgs.Context);
            Assert.AreEqual(bindingFormatErrorTestMessage, errorEventArgs.Error.Message);
            errorEventArgs = null;

            // setting valid number: the error goes away and the target is updated
            source.StringProp = "-1";
            Assert.AreEqual(-1, target.IntProp);
            Assert.IsNull(errorEventArgs);

            // removing the subscription will not handle the error anymore
            binding.Error -= HandleBindingError;
            Throws<ArgumentException>(() => source.StringProp = "x", bindingFormatErrorTestMessage);
        }

        [Test]
        public void NonPublicEventTest()
        {
            bool executed = false;
            var test = new TestClass();
            using var bindings = new CommandBindingsCollection();
            bindings.Add(() => executed = true)
                .AddSource(test, nameof(test.StringPropChangedTestEvent));

            // triggering command
            test.StringProp = "Alpha";
            Assert.IsTrue(executed);
        }

        [Test]
        public void ParameterizedCommandTest()
        {
            bool executed = false;
            var test = new TestClass();
            using var bindings = new CommandBindingsCollection();
            bindings.Add(OnExecute, () => test.StringProp)
                .AddSource(test, nameof(test.PropertyChanged));

            void OnExecute(string value)
            {
                Assert.AreEqual(test.StringProp, value);
                executed = true;
            }

            // triggering command
            test.StringProp = "Alpha";
            Assert.IsTrue(executed);
        }

        #endregion
    }
}
