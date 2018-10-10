using System;

namespace KGySoft.ComponentModel
{
    internal class CommandSource<TEventArgs> : ICommandSource<TEventArgs> where TEventArgs : EventArgs
    {
        public object Source { get; internal set; }
        public string TriggeringEvent { get; internal set; }
        public TEventArgs EventArgs { get; internal set; }
        EventArgs ICommandSource.EventArgs => EventArgs;
    }
}
