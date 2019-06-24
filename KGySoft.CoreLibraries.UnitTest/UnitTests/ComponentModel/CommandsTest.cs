using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KGySoft.ComponentModel;
using NUnit.Framework;

namespace KGySoft.CoreLibraries.UnitTests.ComponentModel
{
    [TestFixture]
    public class CommandsTest
    {
        public static readonly ICommand LogPropChangeCommand
            = new SourceAwareTargetedCommand<PropertyChangedExtendedEventArgs, TextWriter>((src, state, writer) =>
            {
                writer.WriteLine($"{src.EventArgs.PropertyName}: {src.EventArgs.OldValue} -> {src.EventArgs.NewValue}");
                state.AsDynamic.TriggerCount = state.GetValueOrDefault<int>("TriggerCount") + 1;
            });

        private class TestClass : ObservableObjectBase
        {
            public string TestProp { get => Get<string>(); set => Set(value); }
        }

        [Test]
        public void CreateBindingAndTriggerCommand()
        {
            var test = new TestClass();
            ICommandBinding binding = LogPropChangeCommand.CreateBinding(test, nameof(test.PropertyChanged), Console.Out);

            Assert.IsFalse(binding.State.ContainsKey("TriggerCount"));
            test.TestProp = "Alpha";
            Assert.AreEqual(2, binding.State.AsDynamic.TriggerCount); // IsModified, TestProp

            // not triggered again after disposing
            binding.Dispose();
            test.TestProp = "Beta";
            Assert.AreEqual(2, binding.State.AsDynamic.TriggerCount);

            // creating alternatively (command itself was not disposed)
            binding = LogPropChangeCommand.CreateBinding()
                .AddSource(test, nameof(test.PropertyChanged))
                .AddTarget(Console.Out);

            Assert.IsFalse(binding.State.ContainsKey("TriggerCount"));
            test.TestProp = "Gamma";
            Assert.AreEqual(1, binding.State.AsDynamic.TriggerCount); // new state, only TestProp changed

            binding.InvokeCommand(this, "Fake event name", new PropertyChangedExtendedEventArgs("old", "new", "Fake property name"));
            Assert.AreEqual(2, binding.State.AsDynamic.TriggerCount); // our manual trigger
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
            Assert.AreEqual(2, binding.State.AsDynamic.TriggerCount); // IsModified, TestProp

            // not triggered again after disposing the collection
            bindings.Dispose();
            test.TestProp = "Beta";
            Assert.AreEqual(2, binding.State.AsDynamic.TriggerCount);

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
            Assert.AreEqual(1, binding.State.AsDynamic.TriggerCount);

            // disabling by poll
            binding.Executing += (sender, args) => args.State.Enabled = false;
            test.TestProp = "Gamma";
            Assert.AreEqual(1, binding.State.AsDynamic.TriggerCount);
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
            Assert.AreEqual(2, binding.State.AsDynamic.TriggerCount);
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
            Assert.AreEqual(2, binding.State.AsDynamic.TriggerCount); // IsModified, TestProp
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
    }
}
