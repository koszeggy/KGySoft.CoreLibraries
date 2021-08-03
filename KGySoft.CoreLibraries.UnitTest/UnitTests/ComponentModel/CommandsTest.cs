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
    public class CommandsTest
    {
        #region Nested classes

        #region TestClass class

        private class TestClass : ObservableObjectBase
        {
            #region Events

            internal event EventHandler TestEvent;

            #endregion

            #region Properties

            public string TestProp { get => Get<string>(); set => Set(value); }

            #endregion

            #region Methods

            protected internal override void OnPropertyChanged(PropertyChangedExtendedEventArgs e)
            {
                base.OnPropertyChanged(e);
                if (e.PropertyName == nameof(TestProp))
                    TestEvent?.Invoke(this, EventArgs.Empty);
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
            test.TestProp = "Alpha";
            Assert.AreEqual(2, binding.State["TriggerCount"]); // IsModified, TestProp

            // not triggered again after disposing
            binding.Dispose();
            test.TestProp = "Beta";
            Assert.AreEqual(2, binding.State["TriggerCount"]);

            // creating alternatively (command itself was not disposed)
            binding = LogPropChangeCommand.CreateBinding()
                .AddSource(test, nameof(test.PropertyChanged))
                .AddTarget(Console.Out);

            Assert.IsFalse(binding.State.ContainsKey("TriggerCount"));
            test.TestProp = "Gamma";
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
            test.TestProp = "Alpha";
            Assert.AreEqual(2, binding.State["TriggerCount"]); // IsModified, TestProp

            // not triggered again after disposing the collection
            bindings.Dispose();
            test.TestProp = "Beta";
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
            test.TestProp = "Alpha";
            Assert.IsFalse(state.ContainsKey("TriggerCount"));

            // enabling by push
            state.Enabled = true;
            test.TestProp = "Beta";
            Assert.AreEqual(1, binding.State["TriggerCount"]);

            // disabling by poll
            binding.Executing += (sender, args) => args.State.Enabled = false;
            test.TestProp = "Gamma";
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
            binding.State[nameof(test.TestProp)] = "ByUpdater";
            Assert.AreEqual("ByUpdater", test.TestProp);
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
            test.TestProp = "Alpha";
            Assert.AreEqual(2, binding.State["TriggerCount"]); // IsModified, TestProp
        }

        [Test]
        public void PropertyBinding()
        {
            var source = new TestClass { TestProp = "Alpha" };
            var target = new TestClass { TestProp = "Beta" };
            Assert.AreNotEqual(source.TestProp, target.TestProp);

            // they are synced immediately
            ICommandBinding binding = source.CreatePropertyBinding(nameof(source.TestProp), nameof(target.TestProp), target);
            Assert.AreEqual(source.TestProp, target.TestProp);

            // or when source changes
            source.TestProp = "Gamma";
            Assert.AreEqual(source.TestProp, target.TestProp);

            // but only until binding is disposed
            binding.Dispose();
            source.TestProp = "Delta";
            Assert.AreNotEqual(source.TestProp, target.TestProp);
        }

        [Test]
        public void NonPublicEventTest()
        {
            bool executed = false;
            var test = new TestClass();
            using var bindings = new CommandBindingsCollection();
            bindings.Add(() => executed = true)
                .AddSource(test, nameof(test.TestEvent));

            // triggering command
            test.TestProp = "Alpha";
            Assert.IsTrue(executed);
        }

        [Test]
        public void ParameterizedCommandTest()
        {
            bool executed = false;
            var test = new TestClass();
            using var bindings = new CommandBindingsCollection();
            bindings.Add(OnExecute, () => test.TestProp)
                .AddSource(test, nameof(test.PropertyChanged));

            void OnExecute(string value)
            {
                Assert.AreEqual(test.TestProp, value);
                executed = true;
            }

            // triggering command
            test.TestProp = "Alpha";
            Assert.IsTrue(executed);
        }

        #endregion
    }
}
