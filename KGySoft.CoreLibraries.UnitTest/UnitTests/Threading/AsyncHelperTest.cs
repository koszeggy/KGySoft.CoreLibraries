#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AsyncHelperTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2022 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#nullable enable

#region Usings

using System;
using System.Threading;

using KGySoft.Threading;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Threading
{
    [TestFixture]
    public class AsyncHelperTest
    {
        #region Methods

        #region Static Methods

        private static void TestAction(IAsyncContext ctx, ManualResetEvent startedSignal, WaitHandle? canLeaveSignal)
        {
            startedSignal.Set();
            ctx.Progress?.New("Waiting", 1);
            canLeaveSignal?.WaitOne();
            ctx.Progress?.Complete();
        }

        private static int TestFunc(IAsyncContext ctx, ManualResetEvent startedSignal, WaitHandle? canLeaveSignal)
        {
            startedSignal.Set();
            ctx.Progress?.New("Waiting", 1);
            canLeaveSignal?.WaitOne();
            ctx.Progress?.Complete();
            return ctx.IsCancellationRequested ? -1 : 1;
        }

        #endregion

        #region Instance Methods

        [TestCase(null, default(bool))]
        [TestCase(false, default(bool))]
        [TestCase(true, true)]
        [TestCase(true, false)]
        public void DoOperationSynchronouslyActionTest(bool? cancel, bool throwIfCanceled)
        {
            // testing if operation is invoked and if requested, canceling during the operation
            var startedSignal = new ManualResetEvent(false);
            var canLeaveSignal = new ManualResetEvent(false);
            bool canceled = false;

            // ReSharper disable once AccessToModifiedClosure - that's the point, we want to observe the change if any
            ParallelConfig? config = cancel == null ? null : new ParallelConfig
            {
                IsCancelRequestedCallback = () => canceled,
                ThrowIfCanceled = throwIfCanceled
            };

            Exception? error = null;
            var thread = new Thread(() =>
            {
                try
                {
                    AsyncHelper.DoOperationSynchronously(ctx => TestAction(ctx, startedSignal, canLeaveSignal), config);
                }
                catch (Exception e)
                {
                    error = e;
                }
            });
            thread.Start();
            Assert.IsTrue(startedSignal.WaitOne());

            // if requested, canceling after starting the operation and then letting it finish
            if (cancel == true)
                canceled = true;
            canLeaveSignal.Set();
            thread.Join();
            Assert.That(error, cancel == true && throwIfCanceled ? Is.InstanceOf<OperationCanceledException>() : Is.Null);
        }

        [Test]
        public void DoOperationSynchronouslyActionEarlyCancelTest()
        {
            // Testing that the delegate is not even called if the operation is canceled in advance
            var startedSignal = new ManualResetEvent(false);
            ParallelConfig config = new ParallelConfig { IsCancelRequestedCallback = () => true };
            Assert.Throws<OperationCanceledException>(() => AsyncHelper.DoOperationSynchronously(ctx => TestAction(ctx, startedSignal, null), config));
            Assert.IsFalse(startedSignal.WaitOne(0));
        }

        [TestCase(null, default(bool))]
        [TestCase(false, default(bool))]
        [TestCase(true, true)]
        [TestCase(true, false)]
        public void HandleCompletedTest(bool? cancel, bool throwIfCanceled)
        {
            ParallelConfig? config = cancel == null ? null : new ParallelConfig
            {
                IsCancelRequestedCallback = () => cancel.Value,
                ThrowIfCanceled = throwIfCanceled
            };
            TestDelegate test = () => AsyncHelper.HandleCompleted(config);
            Assert.That(test, cancel == true && throwIfCanceled ? Throws.Exception.InstanceOf<OperationCanceledException>() : Throws.Nothing);
        }

        [TestCase(null, default(bool))]
        [TestCase(false, default(bool))]
        [TestCase(true, true)]
        [TestCase(true, false)]
        public void DoOperationSynchronouslyFuncTest(bool? cancel, bool throwIfCanceled)
        {
            // testing if operation is invoked and if requested, canceling during the operation
            var startedSignal = new ManualResetEvent(false);
            var canLeaveSignal = new ManualResetEvent(false);
            bool canceled = false;

            // ReSharper disable once AccessToModifiedClosure - that's the point, we want to observe the change if any
            ParallelConfig? config = cancel == null ? null : new ParallelConfig
            {
                IsCancelRequestedCallback = () => canceled,
                ThrowIfCanceled = throwIfCanceled
            };

            Exception? error = null;
            int? result = null;
            var thread = new Thread(() =>
            {
                try
                {
                    result = AsyncHelper.DoOperationSynchronously(ctx => TestFunc(ctx, startedSignal, canLeaveSignal), config);
                }
                catch (Exception e)
                {
                    error = e;
                }
            });
            thread.Start();
            Assert.IsTrue(startedSignal.WaitOne());

            // if requested, canceling after starting the operation and then letting it finish
            if (cancel == true)
                canceled = true;
            canLeaveSignal.Set();
            thread.Join();
            Assert.That(error, cancel == true && throwIfCanceled ? Is.InstanceOf<OperationCanceledException>() : Is.Null);
            Assert.That(result, cancel == true ? (throwIfCanceled ? Is.Null : Is.EqualTo(0)) : Is.EqualTo(1));
        }

        [Test]
        public void DoOperationSynchronouslyFuncEarlyCancelTest()
        {
            // Testing that the delegate is not even called if the operation is canceled in advance
            var startedSignal = new ManualResetEvent(false);
            ParallelConfig config = new ParallelConfig { IsCancelRequestedCallback = () => true };
            Assert.Throws<OperationCanceledException>(() => AsyncHelper.DoOperationSynchronously(ctx => TestFunc(ctx, startedSignal, null), config));
            Assert.IsFalse(startedSignal.WaitOne(0));

            config.ThrowIfCanceled = false;
            Assert.AreEqual(0, AsyncHelper.DoOperationSynchronously(ctx => TestFunc(ctx, startedSignal, null), config));
            Assert.IsFalse(startedSignal.WaitOne(0));

            Assert.AreEqual(42, AsyncHelper.DoOperationSynchronously(ctx => TestFunc(ctx, startedSignal, null), 42, config));
            Assert.IsFalse(startedSignal.WaitOne(0));
        }

        [TestCase(null, default(bool))]
        [TestCase(false, default(bool))]
        [TestCase(true, true)]
        [TestCase(true, false)]
        public void FromResultSyncTest(bool? cancel, bool throwIfCanceled)
        {
            ParallelConfig? config = cancel == null ? null : new ParallelConfig
            {
                IsCancelRequestedCallback = () => cancel.Value,
                ThrowIfCanceled = throwIfCanceled
            };

            int? result = null;
            TestDelegate test = () => result = AsyncHelper.FromResult(42, config);
            Assert.That(test, cancel == true && throwIfCanceled ? Throws.Exception.InstanceOf<OperationCanceledException>() : Throws.Nothing);
            Assert.That(result, cancel == true ? (throwIfCanceled ? Is.Null : Is.EqualTo(0)) : Is.EqualTo(42));

            result = null;
            test = () => result = AsyncHelper.FromResult(42, -42, config);
            Assert.That(test, cancel == true && throwIfCanceled ? Throws.Exception.InstanceOf<OperationCanceledException>() : Throws.Nothing);
            Assert.That(result, cancel == true ? (throwIfCanceled ? Is.Null : Is.EqualTo(-42)) : Is.EqualTo(42));
        }

        // TODO: Begin/End
        // TODO: Async

        #endregion

        #endregion
    }
}
