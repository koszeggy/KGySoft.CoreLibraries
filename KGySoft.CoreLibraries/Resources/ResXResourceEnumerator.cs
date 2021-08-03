#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXResourceEnumerator.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

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
                    Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);

                KeyValuePair<string, ResXDataNode> current = wrappedEnumerator.Current;
                if (mode == ResXEnumeratorModes.Aliases)
                    return new DictionaryEntry(current.Key, current.Value.ValueInternal);

                return new DictionaryEntry(current.Key, owner.SafeMode 
                    ? current.Value.GetSafeValueInternal(false, owner.CloneValues) 
                    : current.Value.GetUnsafeValueInternal(owner.TypeResolver, false, owner.CloneValues, owner.AutoFreeXmlData, owner.BasePath));
            }
        }

        public object Key
        {
            get
            {
                if (state != States.Enumerating)
                    Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);

                // if only key is requested, Value is not deserialized
                return wrappedEnumerator.Current.Key;
            }
        }

        public object? Value => Entry.Value;

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
            wrappedEnumerator = mode switch
            {
                ResXEnumeratorModes.Resources => owner.Resources?.GetEnumerator() ?? Throw.ObjectDisposedException<IEnumerator<KeyValuePair<string, ResXDataNode>>>(),
                ResXEnumeratorModes.Metadata => owner.Metadata?.GetEnumerator() ?? Throw.ObjectDisposedException<IEnumerator<KeyValuePair<string, ResXDataNode>>>(),
                ResXEnumeratorModes.Aliases => owner.Aliases?.Select(SelectAlias).GetEnumerator() ?? Throw.ObjectDisposedException<IEnumerator<KeyValuePair<string, ResXDataNode>>>(),
                _ => Throw.InternalError<IEnumerator<KeyValuePair<string, ResXDataNode>>>($"Unexpected mode: {mode}")
            };
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
                Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

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
                    Throw.ObjectDisposedException();
                wrappedEnumerator = aliases.Select(SelectAlias).GetEnumerator();
                return;
            }

            wrappedEnumerator.Reset();
        }

        #endregion

        #endregion
    }
}
