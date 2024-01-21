#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParallelHelperTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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
using System.Linq;
using System.Threading;
#if !NET35
using System.Threading.Tasks;
#endif

using KGySoft.Threading;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Threading
{
    [TestFixture]
    public class ParallelHelperTest : TestBase
    {
        #region Methods

        [Test]
        public void ForCompletedSuccessfullyTest()
        {
            var bools = new bool[1000];
            ParallelHelper.For(0, bools.Length, i => bools[i] = true);
            Assert.IsTrue(bools.All(b => b));
        }

        [Test]
        public void ForNoIterationsTest()
        {
            bool executed = false;
            Action<int> callback = _ => executed = true;

            Assert.IsTrue(ParallelHelper.For(0, -1, null, callback));
            Assert.IsFalse(executed);
        }

        [Test]
        public void ForCanceledTest()
        {
            Func<bool> isCanceled = () => true;
            bool executed = false;
            Action<int> callback = _ => executed = true;
            var config = new ParallelConfig { IsCancelRequestedCallback = isCanceled };

            Throws<OperationCanceledException>(() => ParallelHelper.For(0, 1, config, callback), Res.OperationCanceled);
            config.ThrowIfCanceled = false;
            Assert.IsFalse(ParallelHelper.For(0, 1, config, callback));
            Assert.IsFalse(executed);
        }

        [Test]
        public void ForWithProgressTest()
        {
            var bools = new bool[1000];
            var testProgress = new TestProgress();
            var config = new ParallelConfig { Progress = testProgress };
            ParallelHelper.For<object?>(null, 0, bools.Length, config, i => bools[i] = true);
            Assert.IsTrue(bools.All(b => b));
            Assert.IsNull(testProgress.Current.OperationType);
            Assert.AreEqual(0, testProgress.Current.MaximumValue);
            Assert.AreEqual(0, testProgress.Current.CurrentValue);

            Array.Clear(bools, 0, bools.Length);
            ParallelHelper.For("Test operation", 0, bools.Length, config, i => bools[i] = true);
            Assert.IsTrue(bools.All(b => b));
            Assert.AreEqual("Test operation", testProgress.Current.OperationType);
            Assert.AreEqual(bools.Length, testProgress.Current.MaximumValue);
            Assert.AreEqual(bools.Length, testProgress.Current.CurrentValue);
        }

        [Test]
        public void BeginForBlockingWaitTest()
        {
            var bools = new bool[1000];
            IAsyncResult ar = ParallelHelper.BeginFor(0, bools.Length, i => bools[i] = true);
            bool result = ParallelHelper.EndFor(ar);
            Assert.IsTrue(ar.IsCompleted);
            Assert.IsFalse(ar.CompletedSynchronously);
            Assert.IsTrue(result);
            Assert.IsTrue(bools.All(b => b));
        }

        [Test]
        public void BeginForActiveWaitingTest()
        {
            var bools = new bool[1000];
            IAsyncResult ar = ParallelHelper.BeginFor(0, bools.Length, i => bools[i] = true);
            while (!ar.IsCompleted)
                Thread.Sleep(1);
            bool result = ParallelHelper.EndFor(ar);
            Assert.IsTrue(ar.IsCompleted);
            Assert.IsFalse(ar.CompletedSynchronously);
            Assert.IsTrue(result);
            Assert.IsTrue(bools.All(b => b));
        }

        [Test]
        public void BeginForWithCallbackTest()
        {
            var bools = new bool[1000];
            var callbackCalled = new ManualResetEvent(false);
            var asyncConfig = new AsyncConfig(ar => callbackCalled.Set());
            IAsyncResult ar = ParallelHelper.BeginFor(0, bools.Length, asyncConfig, i => bools[i] = true);
            ar.AsyncWaitHandle.WaitOne();
            Assert.IsTrue(ar.IsCompleted);
            Assert.IsTrue(callbackCalled.WaitOne());
            bool result = ParallelHelper.EndFor(ar);
            Assert.IsFalse(ar.CompletedSynchronously);
            Assert.IsTrue(result);
            Assert.IsTrue(bools.All(b => b));
        }

        [Test]
        public void BeginForImmediateCancelTest()
        {
            var bools = new bool[1000];
            var asyncConfig = new AsyncConfig(null, () => true);
            IAsyncResult ar = ParallelHelper.BeginFor(0, bools.Length, asyncConfig, i => bools[i] = true);
            Assert.IsTrue(ar.IsCompleted);
            Assert.IsTrue(ar.CompletedSynchronously);
            Throws<OperationCanceledException>(() => ParallelHelper.EndFor(ar), Res.OperationCanceled);
            Assert.IsTrue(bools.All(b => !b));
        }

        [Test]
        public void BeginForCancelWithoutExceptionTest()
        {
            var bools = new bool[1000];
            var asyncConfig = new AsyncConfig(null, () => true) { ThrowIfCanceled = false };
            IAsyncResult ar = ParallelHelper.BeginFor(0, bools.Length, asyncConfig, i => bools[i] = true);
            Assert.IsTrue(ar.IsCompleted);
            Assert.IsTrue(ar.CompletedSynchronously);
            Assert.IsFalse(ParallelHelper.EndFor(ar));
            Assert.IsTrue(bools.All(b => !b));
        }

#if !NET35
        [Test]
        public void ForAsyncBlockingWaitTest()
        {
            var bools = new bool[1000];
            Task task = ParallelHelper.ForAsync(0, bools.Length, i => bools[i] = true);
            task.Wait();
            Assert.IsTrue(task.IsCompleted);
            Assert.IsTrue(bools.All(b => b));
        }

        [Test]
        public void ForAsyncImmediateCancelTest()
        {
            var bools = new bool[1000];
            var asyncConfig = new TaskConfig(new CancellationToken(true));
            Task<bool> task = ParallelHelper.ForAsync(0, bools.Length, asyncConfig, i => bools[i] = true);
            Assert.IsTrue(task.IsCanceled);
            var ex = Assert.Throws<AggregateException>(() => { var _ = task.Result; });
            Assert.IsInstanceOf<OperationCanceledException>(ex!.InnerExceptions[0]);
            Assert.IsTrue(bools.All(b => !b));
        }

        [Test]
        public void ForAsyncCancelWithoutExceptionTest()
        {
            var bools = new bool[1000];
            var asyncConfig = new TaskConfig(new CancellationToken(true)) { ThrowIfCanceled = false };
            Task<bool> task = ParallelHelper.ForAsync(0, bools.Length, asyncConfig, i => bools[i] = true);
            Assert.IsFalse(task.IsCanceled);
            Assert.IsFalse(task.Result);
            Assert.IsTrue(bools.All(b => !b));
        }
#endif


#if !(NET35 || NET40)
        [Test]
        public async Task ForAsyncWithAwaitTest()
        {
            var bools = new bool[1000];
            await ParallelHelper.ForAsync(0, bools.Length, i => bools[i] = true);
            Assert.IsTrue(bools.All(b => b));
        }
#endif

        #endregion
    }
}