#nullable enable

using System;
using System.Drawing;
using System.Threading.Tasks;

using KGySoft.CoreLibraries;
using KGySoft.Drawing.Imaging;
using KGySoft.Threading;

using KGySoft.Drawing;
using KGySoft.Drawing.Imaging;

using AsyncConfig = KGySoft.Threading.AsyncConfig;
using TaskConfig = KGySoft.Threading.TaskConfig;
using IAsyncContext = KGySoft.Threading.IAsyncContext;

namespace KGySoft.Drawing
{
    public enum DrawingOperation
    {
        ProcessingPixels
    }

    //public sealed class AsyncConfig : AsyncConfig<DrawingOperation>{}
    //public sealed class TaskConfig : TaskConfig<DrawingOperation>{ }
    //public interface IAsyncContext : IAsyncContext<DrawingOperation> { }
    //public interface IAsyncContext : IAsyncContext<object> { }
}

public static class Example
{

    // The sync version. This method is blocking, cannot be canceled, does not report progress and uses max parallelization.
    public static Bitmap ToGrayscale(Bitmap bitmap)
    {
        ValidateArguments(bitmap);

        // Just to demonstrate some immediate return (for the sync version it really straightforward).
        if (IsGrayscale(bitmap))
            return bitmap;

        // The actual processing. From the sync version it gets a Null context. The result is never null from here.
        return ProcessToGrayscale(AsyncContext.Null, bitmap)!;
    }

    // The Task-returning version. Requires .NET Framework 4.0 or later and can be awaited in .NET Framework 4.5 or later.
    public static Task<Bitmap?> ToGrayscaleAsync(Bitmap bitmap, TaskConfig? asyncConfig = null)
    {
        ValidateArguments(bitmap);

        // Use AsyncContext.FromResult for immediate return. It handles asyncConfig.ThrowIfCanceled properly.
        if (IsGrayscale(bitmap))
            return AsyncContext.FromResult(bitmap, null, asyncConfig);

        // The actual processing for Task returning async methods.
        return AsyncContext.DoOperationAsync(ctx => ProcessToGrayscale(ctx, bitmap), asyncConfig);
    }

    // The old-style Begin/End methods that work even in .NET Framework 3.5. Can be omitted if not needed.
    public static IAsyncResult BeginToGrayscale(Bitmap bitmap, AsyncConfig? asyncConfig = null)
    {
        ValidateArguments(bitmap);

        // Use AsyncContext.FromResult for immediate return.
        // It handles asyncConfig.ThrowIfCanceled and sets IAsyncResult.CompletedSynchronously.
        if (IsGrayscale(bitmap))
            return AsyncContext.FromResult(bitmap, null, asyncConfig);

        // The actual processing for IAsyncResult returning async methods.
        return AsyncContext.BeginOperation(ctx => ProcessToGrayscale(ctx, bitmap), asyncConfig);
    }

    // Note that the name of "BeginToGrayscale" is explicitly specified here. Older compilers need it also for AsyncContext.BeginOperation.
    public static Bitmap? EndToGrayscale(IAsyncResult asyncResult) => AsyncContext.EndOperation<Bitmap?>(asyncResult, nameof(BeginToGrayscale));

    // The method of the actual processing has the same parameters as the sync version and also an IAsyncContext parameter.
    // The result can be null if the operation is canceled (throwing possible exception due to cancellation is handled by the caller)
    private static Bitmap? ProcessToGrayscale(IAsyncContext context, Bitmap bitmap)
    {
        Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);

        using (IReadableBitmapData source = bitmap.GetReadableBitmapData())
        using (IWritableBitmapData target = result.GetWritableBitmapData())
        {
            // NOTE: The BitmapDataExtensions class has many methods with IAsyncContext parameters that can be used from
            // potentially async operations like this one. A single line solution could be:
            //source.CopyTo(target, context, new Rectangle(0, 0, source.Width, source.Height), Point.Empty,
            //    PredefinedColorsQuantizer.FromCustomFunction(c => c.ToGray()));

            // We can report progress if the caller configured it. Now we will increment progress for each processed rows.
            context.Progress?.New(DrawingOperation.ProcessingPixels, maximumValue: source.Height);

            Parallel.For(0, source.Height,
                new ParallelOptions { MaxDegreeOfParallelism = context.MaxDegreeOfParallelism <= 0 ? -1 : context.MaxDegreeOfParallelism },
                (y, state) =>
                {
                    if (context.IsCancellationRequested)
                    {
                        state.Stop();
                        return;
                    }

                    IReadableBitmapDataRow rowSrc = source[y];
                    IWritableBitmapDataRow rowDst = target[y];
                    for (int x = 0; x < source.Width; x++)
                        rowDst[x] = rowSrc[x].ToGray();

                    context.Progress?.Increment();
                });
        }

        // Do not throw OperationCanceledException explicitly: it will be thrown by the caller
        // if the asyncConfig parameter passed to the async overloads was configured to throw an exception.
        if (context.IsCancellationRequested)
        {
            result.Dispose();
            return null;
        }

        return result;
    }

    private static void ValidateArguments(Bitmap bitmap)
    {
        if (bitmap == null)
            throw new ArgumentNullException(nameof(bitmap));
    }

    private static bool IsGrayscale(Bitmap bitmap) => false;

    private class ProgressReporter : IAsyncProgress
    {
        public void Report(AsyncProgress<object> value) => Report<object>(value);

        public void Report<T>(AsyncProgress<T> progress) => New(progress.OperationType, progress.MaximumValue, progress.CurrentValue);

        public void New<T>(T operationType, int maximumValue = 0, int currentValue = 0)
        {
            string operationName;
            if (operationType is DrawingOperation op)
                operationName = op.ToString<DrawingOperation>();
            else
                operationName = operationType.ToString();
            Console.WriteLine($"{operationName}: {currentValue}/{maximumValue}");
        }

        public void Increment()
        {
            throw new NotImplementedException();
        }

        public void SetProgressValue(int value)
        {
            throw new NotImplementedException();
        }

        public void Complete()
        {
            throw new NotImplementedException();
        }
    }

    public static void Main()
    {

    }
}