#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXResourceEnumerator.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2017 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Provides an enumerator for .resx resource classes, which have already cached resource data.
    /// Non-serializable (the original returns a ListDictionary enumerator, which is non-serializable either - and hybrid cannot be serializable either).
    /// </summary>
    internal sealed class ResXResourceEnumerator : IDictionaryEnumerator
    {
        #region Enumerations

        private enum States
        {
            BeforeFirst,
            Enumerating,
            AfterLast
        }

        #endregion

        #region Fields

        private readonly IResXResourceContainer owner;
        private readonly ResXEnumeratorModes mode;
        private readonly int version;

        private IEnumerator<KeyValuePair<string, ResXDataNode>> wrappedEnumerator;
        private States state;

        #endregion

        #region Properties

        #region Public Properties

        public DictionaryEntry Entry
        {
            get
            {
                if (state != States.Enumerating)
                    throw new InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);

                KeyValuePair<string, ResXDataNode> current = wrappedEnumerator.Current;
                if (mode == ResXEnumeratorModes.Aliases)
                    return new DictionaryEntry(current.Key, current.Value.ValueInternal);

                return owner.SafeMode
                    ? new DictionaryEntry(current.Key, current.Value)
                    : new DictionaryEntry(current.Key, current.Value.GetValue(owner.TypeResolver, owner.BasePath, owner.AutoFreeXmlData));
            }
        }

        public object Key
        {
            get
            {
                if (state != States.Enumerating)
                    throw new InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);

                // if only key is requested, Value is not deserialized
                return wrappedEnumerator.Current.Key;
            }
        }

        public object Value => Entry.Value;

        public object Current => Entry;

        #endregion

        #region Internal Properties

        internal int OwnerVersion => owner.Version;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceEnumerator"/> class.
        /// Should be called from lock.
        /// </summary>
        internal ResXResourceEnumerator(IResXResourceContainer owner, ResXEnumeratorModes mode, int version = 0)
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

        #endregion

        #region Methods

        #region Static Methods

        internal static KeyValuePair<string, ResXDataNode> SelectAlias(KeyValuePair<string, string> alias)
            => new KeyValuePair<string, ResXDataNode>(alias.Key, new ResXDataNode(alias.Key, alias.Value));

        #endregion

        #region Instance Methods

        public bool MoveNext()
        {
            if (state == States.AfterLast)
                return false;

            if (state == States.BeforeFirst)
                state = States.Enumerating;

            if (version != owner.Version)
                throw new InvalidOperationException(Res.IEnumeratorCollectionModified);

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
                    throw new ObjectDisposedException(null, Res.ObjectDisposed);
                wrappedEnumerator = aliases.Select(SelectAlias).GetEnumerator();
                return;
            }

            wrappedEnumerator.Reset();
        }

        #endregion

        #endregion
    }
}
