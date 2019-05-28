#if NET35 || NET40
using KGySoft.Reflection;

// ReSharper disable once CheckNamespace
namespace System.Runtime.ExceptionServices
{
    internal sealed class ExceptionDispatchInfo
    {
        private readonly Exception exception;
        private readonly string stackTrace;
        private readonly string source;

        private ExceptionDispatchInfo(Exception source)
        {
            exception = source ?? throw new ArgumentNullException(nameof(source));
            stackTrace = source.StackTrace + Environment.NewLine;
            this.source = source.GetSource();
        }

        internal static ExceptionDispatchInfo Capture(Exception source) => new ExceptionDispatchInfo(source);

        internal void Throw()
        {
            try
            {
                throw exception;
            }
            catch
            {
                exception.InternalPreserveStackTrace();
                exception.SetRemoteStackTraceString(stackTrace);
                exception.SetSource(source);
                throw;
            }
        }
    }
}
#endif