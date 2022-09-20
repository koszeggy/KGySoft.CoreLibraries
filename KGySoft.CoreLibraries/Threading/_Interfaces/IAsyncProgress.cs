#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IAsyncProgress.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2022 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

#if !NET35
using System.Threading.Tasks;
#endif

#endregion

#if NET35
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved - in .NET 3.5 not all members are available
#endif

namespace KGySoft.Threading
{
    /// <summary>
    /// Represents a progress updates provider for asynchronous operations.
    /// It provides methods for updating the progress from concurrent threads.
    /// </summary>
    /// <example>
    /// The following example demonstrates how to implement this interface allowing concurrent updates.
    /// <code lang="C#"><![CDATA[
    /// #nullable enable
    /// 
    /// using System;
    /// using System.Threading;
    /// using System.Timers;
    /// 
    /// using KGySoft.Threading;
    /// 
    /// using Timer = System.Timers.Timer;
    /// 
    /// class Example
    /// {
    ///     public static void Main()
    ///     {
    ///         var progressTracker = new ProgressTracker();
    ///         using var progressReporter = new ConsoleProgressReporter(progressTracker);
    ///         var config = new ParallelConfig { Progress = progressTracker };
    /// 
    ///         // imitating some long concurrent task
    ///         progressReporter.Enabled = true; // starts updating the progress periodically
    ///         ParallelHelper.For("Some parallel operation", 0, 1000, config,
    ///             body: i => Thread.Sleep(1));
    ///         progressReporter.UpdateProgress(); // to show the completed progress.
    ///         progressReporter.Enabled = false; // stopping the timer
    ///     }
    /// 
    ///     // This IAsyncProgress implementation just provides an always up-to-date Current property.
    ///     // You can add an event if you want don't want to miss any tiny progress change.
    ///     private class ProgressTracker : IAsyncProgress
    ///     {
    ///         private readonly object syncRoot = new object();
    ///         private string? currentOperation;
    ///         private int maximumValue;
    ///         private int currentValue;
    /// 
    ///         public AsyncProgress<string?> Current
    ///         {
    ///             get
    ///             {
    ///                 lock (syncRoot)
    ///                     return new AsyncProgress<string?>(currentOperation, maximumValue, currentValue);
    ///             }
    ///         }
    /// 
    ///         public void Report<T>(AsyncProgress<T> progress)
    ///         {
    ///             lock (syncRoot)
    ///             {
    ///                 // Converting any T to string (assuming it can be displayed as a text)
    ///                 currentOperation = progress.OperationType?.ToString();
    ///                 maximumValue = progress.MaximumValue;
    ///                 currentValue = progress.CurrentValue;
    ///             }
    ///         }
    /// 
    ///         public void New<T>(T operationType, int maximumValue = 0, int currentValue = 0)
    ///             => Report(new AsyncProgress<T>(operationType, maximumValue, currentValue));
    /// 
    ///         public void Increment()
    ///         {
    ///             lock (syncRoot)
    ///                 currentValue++;
    ///         }
    /// 
    ///         public void SetProgressValue(int value)
    ///         {
    ///             lock (syncRoot)
    ///                 currentValue = value;
    ///         }
    /// 
    ///         public void Complete()
    ///         {
    ///             lock (syncRoot)
    ///                 currentValue = maximumValue;
    ///         }
    ///     }
    /// 
    ///     // This class is the one that displays the actual progress. It can be easily adapted to any UI.
    ///     private class ConsoleProgressReporter : IDisposable
    ///     {
    ///         private readonly Timer timer; // using System.Timers.Timer here but you can use UI-specific versions
    ///         private readonly ProgressTracker progressTracker;
    /// 
    ///         // To turn progress reporting on and off. In a UI you can set also some progress bar visibility here.
    ///         public bool Enabled
    ///         {
    ///             get => timer.Enabled;
    ///             set => timer.Enabled = value;
    ///         }
    /// 
    ///         public ConsoleProgressReporter(ProgressTracker progress)
    ///         {
    ///             progressTracker = progress;
    ///             timer = new Timer { Interval = 100 }; // Displayed progress is updated only in every 100ms.
    ///             timer.Elapsed += Timer_Elapsed;
    ///         }
    /// 
    ///         // Please note that progress is updated only when the timer ticks so can skip some rapid changes.
    ///         private void Timer_Elapsed(object? sender, ElapsedEventArgs e) => UpdateProgress();
    /// 
    ///         public void UpdateProgress()
    ///         {
    ///             // In a UI you can set progress bar value, maximum value and some text here.
    ///             var current = progressTracker.Current; // a local copy to prevent an inconsistent report.
    ///             Console.WriteLine($"{current.OperationType}: {current.CurrentValue}/{current.MaximumValue}");
    ///         }
    /// 
    ///         public void Dispose()
    ///         {
    ///             timer.Elapsed -= Timer_Elapsed;
    ///             timer.Dispose();
    ///         }
    ///     }
    /// }
    /// 
    /// // The example above prints a similar output to this one:
    /// // Some parallel operation: 56/1000
    /// // Some parallel operation: 152/1000
    /// // Some parallel operation: 200/1000
    /// // Some parallel operation: 248/1000
    /// // Some parallel operation: 304/1000
    /// // Some parallel operation: 352/1000
    /// // Some parallel operation: 408/1000
    /// // Some parallel operation: 456/1000
    /// // Some parallel operation: 504/1000
    /// // Some parallel operation: 560/1000
    /// // Some parallel operation: 608/1000
    /// // Some parallel operation: 664/1000
    /// // Some parallel operation: 712/1000
    /// // Some parallel operation: 768/1000
    /// // Some parallel operation: 816/1000
    /// // Some parallel operation: 864/1000
    /// // Some parallel operation: 920/1000
    /// // Some parallel operation: 968/1000
    /// // Some parallel operation: 1000/1000]]></code>
    /// </example>
    public interface IAsyncProgress
    {
        #region Methods

        /// <summary>
        /// Reports a progress update to any arbitrary state.
        /// For parallel operations it is recommended to use the <see cref="Increment">Increment</see> method
        /// after starting a new progress because this method cannot guarantee that <see cref="AsyncProgress{T}.CurrentValue"/> will be a strictly
        /// increasing value when called from <see cref="Parallel"/> members, for example.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="AsyncProgress{T}.OperationType"/> property in the specified <paramref name="progress"/>.</typeparam>
        /// <param name="progress">The value of the updated progress.</param>
        void Report<T>(AsyncProgress<T> progress);

        /// <summary>
        /// Indicates that a new progress session is started that consists of <paramref name="maximumValue"/> steps.
        /// </summary>
        /// <typeparam name="T">The type of the <paramref name="operationType"/> parameter.</typeparam>
        /// <param name="operationType">An instance if <typeparamref name="T"/> that describes the type of the new operation.</param>
        /// <param name="maximumValue">Specifies the possible maximum steps of the new operation (the <see cref="Increment">Increment</see> method is recommended to be called later on
        /// if a parallel processing does not know or may reorder the current step). 0 means an operation with no separate steps. This parameter is optional.
        /// <br/>Default value: <c>0</c>.</param>
        /// <param name="currentValue">Specifies the initial current value for the new progress. Should be between 0 and <paramref name="maximumValue"/>. This parameter is optional.
        /// <br/>Default value: <c>0</c>.</param>
        void New<T>(T operationType, int maximumValue = 0, int currentValue = 0);

        /// <summary>
        /// Indicates a progress update of a single step. Expected to be called after the <see cref="New{T}">New</see> or <see cref="Report{T}">Report</see> methods
        /// if they were called with nonzero maximum steps. The implementation should not be sensitive for concurrency racing conditions.
        /// </summary>
        void Increment();

        /// <summary>
        /// Indicates that the current progress is at a specific position.
        /// </summary>
        /// <param name="value">The current progress value. Should not exceed the maximum value of the last <see cref="New{T}">New</see> or <see cref="Report{T}">Report</see> calls
        /// and should not be sensitive for concurrency racing conditions.
        /// </param>
        void SetProgressValue(int value);

        /// <summary>
        /// Indicates that a progress value of the last <see cref="New{T}">New</see> or <see cref="Report{T}">Report</see> method should be set to the maximum value.
        /// It is not required to be called at the end of each sessions so it just indicates that whatever progress has reached the last step.
        /// </summary>
        void Complete();

        #endregion
    }
}