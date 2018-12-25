#if NET35 || NET40
using KGySoft.Reflection;

// ReSharper disable once CheckNamespace
namespace System.Runtime.ExceptionServices
{
    internal sealed class ExceptionDispatchInfo
    {
        private readonly Exception exception;
        private readonly string stackTrace;
        private readonly object source;

        private ExceptionDispatchInfo(Exception source)
        {
            exception = source ?? throw new ArgumentNullException(nameof(source));
            stackTrace = source.StackTrace + Environment.NewLine;
            this.source = Accessors.Exception_source.Get(source);
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
                Accessors.Exception_remoteStackTraceString.Set(exception, stackTrace);
                Accessors.Exception_source.Set(exception, source);
                throw;
            }
        }
    }
}
#endif