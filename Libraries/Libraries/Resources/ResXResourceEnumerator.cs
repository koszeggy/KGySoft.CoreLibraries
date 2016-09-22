using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace KGySoft.Libraries.Resources
{

    /// <summary>
    /// Provides an enumerator for resx resource classes, which have already cached resource data.
    /// </summary>
    internal sealed class ResXResourceEnumerator : IDictionaryEnumerator
    {
        private enum States
        {
            BeforeFirst,
            Enumerating,
            AfterLast
        }

        private readonly IResXResourceContainer owner;
        private readonly ResXEnumeratorModes mode;
        private readonly int version;
        private IEnumerator<KeyValuePair<string, ResXDataNode>> wrappedEnumerator;
        private States state;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceEnumerator"/> class.
        /// Should be called from lock.
        /// </summary>
        internal ResXResourceEnumerator(IResXResourceContainer owner, ResXEnumeratorModes mode, int version)
        {
            this.owner = owner;
            this.mode = mode;
            this.version = version;
            state = States.BeforeFirst;
            switch (mode)
            {
                case ResXEnumeratorModes.Resources:
                    if (owner.Resources != null)
                        wrappedEnumerator = owner.Resources.GetEnumerator();
                    break;
                case ResXEnumeratorModes.Metadata:
                    if (owner.Metadata != null)
                        wrappedEnumerator = owner.Metadata.GetEnumerator();
                    break;
                case ResXEnumeratorModes.Aliases:
                    if (owner.Aliases != null)
                        wrappedEnumerator = owner.Aliases.Select(SelectAlias).GetEnumerator();
                    break;
            }
        }

        public DictionaryEntry Entry
        {
            get
            {
                if (state != States.Enumerating)
                    throw new InvalidOperationException(Res.Get(Res.EnumerationNotStartedOrFinished));

                KeyValuePair<string, ResXDataNode> current = wrappedEnumerator.Current;
                if (mode == ResXEnumeratorModes.Aliases)
                    return new DictionaryEntry(current.Key, current.Value.ValueInternal);

                return owner.UseResXDataNodes
                    ? new DictionaryEntry(current.Key, current.Value)
                    : new DictionaryEntry(current.Key, current.Value.GetValue(owner.TypeResolver, owner.BasePath, owner.AutoFreeXmlData));
            }
        }

        public object Key
        {
            get
            {
                if (state != States.Enumerating)
                    throw new InvalidOperationException(Res.Get(Res.EnumerationNotStartedOrFinished));

                // if only key is requested, Value is not deserialized
                return wrappedEnumerator.Current.Key;
            }
        }

        public object Value
        {
            get { return Entry.Value; }
        }

        public object Current
        {
            get { return Entry; }
        }

        internal int OwnerVersion => owner.Version;

        public bool MoveNext()
        {
            if (state == States.AfterLast)
                return false;

            if (state == States.BeforeFirst)
                state = States.Enumerating;

            if (version != owner.Version)
                throw new InvalidOperationException(Res.Get(Res.EnumerationCollectionModified));

            if (wrappedEnumerator.MoveNext())
                return true;

            state = States.AfterLast;
            return false;
        }

        public void Reset()
        {
            state = States.BeforeFirst;

            // The select iterator does not support Reset so re-creating it
            if (mode == ResXEnumeratorModes.Aliases)
            {
                var aliases = owner.Aliases;
                if (aliases == null)
                    throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                wrappedEnumerator = aliases.Select(SelectAlias).GetEnumerator();
                return;
            }

            wrappedEnumerator.Reset();
        }

        internal static KeyValuePair<string, ResXDataNode> SelectAlias(KeyValuePair<string, string> alias)
        {
            return new KeyValuePair<string, ResXDataNode>(alias.Key, new ResXDataNode(alias.Key, alias.Value));
        }
    }
}
