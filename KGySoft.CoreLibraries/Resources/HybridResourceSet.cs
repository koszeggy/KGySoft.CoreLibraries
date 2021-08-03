#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: HybridResourceSet.cs
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Resources;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents a resource set of hybrid sources (both .resx and compiled source).
    /// </summary>
    [Serializable]
    internal sealed class HybridResourceSet : ResourceSet, IExpandoResourceSet, IExpandoResourceSetInternal, IEnumerable
    {
        #region Nested classes

        /// <summary>
        /// An enumerator for a HybridResourceSet. If both .resx and compiled resources contain the same key, returns only the value from the .resx.
        /// Must be implemented because yield return does not work for IDictionaryEnumerator.
        /// Cannot be serializable because the compiled enumerator is not serializable (supports reset, though).
        /// </summary>
        private sealed class Enumerator : IDictionaryEnumerator
        {
            #region Enumerations

            enum State
            {
                NotStarted = -1,
                EnumeratingResX,
                EnumeratingCompiled,
                Finished = -2
            }

            #endregion

            #region Fields

            private readonly int version;
            private readonly HybridResourceSet owner;
            private readonly ResXResourceEnumerator resxEnumerator;
            private readonly IDictionaryEnumerator compiledEnumerator;

            private State state;
            private HashSet<string>? resxKeys;
            private HashSet<string>? compiledKeys;

            #endregion

            #region Properties

            public DictionaryEntry Entry
            {
                get
                {
                    switch (state)
                    {
                        case State.EnumeratingResX:
                            return resxEnumerator.Entry;
                        case State.EnumeratingCompiled:
                            return compiledEnumerator.Entry;
                        default:
                            Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                            return default;
                    }
                }
            }

            public object Key
            {
                get
                {
                    switch (state)
                    {
                        case State.EnumeratingResX:
                            return resxEnumerator.Key;
                        case State.EnumeratingCompiled:
                            return compiledEnumerator.Key!;
                        default:
                            Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                            return default;
                    }
                }
            }

            public object? Value
            {
                get
                {
                    switch (state)
                    {
                        case State.EnumeratingResX:
                            return resxEnumerator.Value;
                        case State.EnumeratingCompiled:
                            return compiledEnumerator.Value;
                        default:
                            Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);
                            return default;
                    }
                }
            }

            public object Current => Entry;

            #endregion

            #region Constructors

            internal Enumerator(HybridResourceSet owner, ResXResourceEnumerator resx, IDictionaryEnumerator compiled, int version)
            {
                this.owner = owner;
                state = State.NotStarted;
                this.version = version;
                resxEnumerator = resx;
                compiledEnumerator = compiled;
            }

            #endregion

            #region Methods

            public bool MoveNext()
            {
                switch (state)
                {
                    case State.NotStarted:
                        state = State.EnumeratingResX;
                        resxKeys = new HashSet<string>();
                        goto case State.EnumeratingResX;

                    case State.EnumeratingResX:
                        // version is checked internally here
                        if (resxEnumerator.MoveNext())
                        {
                            resxKeys!.Add(resxEnumerator.Key.ToString()!);
                            return true;
                        }

                        state = State.EnumeratingCompiled;
                        compiledKeys = new HashSet<string>();
                        goto case State.EnumeratingCompiled;

                    case State.EnumeratingCompiled:
                        if (version != resxEnumerator.OwnerVersion)
                            Throw.InvalidOperationException(Res.IEnumeratorCollectionModified);

                        while (compiledEnumerator.MoveNext())
                        {
                            string key = compiledEnumerator.Key!.ToString()!;
                            compiledKeys!.Add(key);
                            if (resxKeys!.Contains(key))
                                continue;

                            return true;
                        }

                        resxKeys = null;
                        state = State.Finished;
                        owner.compiledKeys ??= compiledKeys;
                        return false;

                    case State.Finished:
                        return false;

                    default:
                        return Throw.InternalError<bool>($"Invalid state: {state}");
                }
            }

            public void Reset()
            {
                resxEnumerator.Reset();
                compiledEnumerator.Reset();
                resxKeys = null;
                state = State.NotStarted;
            }

            #endregion
        }

        #endregion

        #region Fields

        private ResXResourceSet? resxResourceSet;
        private ResourceSet? compiledResourceSet;
        [NonSerialized]private HashSet<string>? compiledKeys;
        [NonSerialized]private HashSet<string>? compiledKeysCaseInsensitive;

        #endregion

        #region Properties

        #region Public Properties

        public bool IsModified
        {
            get
            {
                ResXResourceSet? resx = resxResourceSet;
                if (resx == null)
                    Throw.ObjectDisposedException();
                return resx.IsModified;
            }
        }

        public bool CloneValues
        {
            get
            {
                ResXResourceSet? resx = resxResourceSet;
                if (resx == null)
                    Throw.ObjectDisposedException();

                return resx.CloneValues;
            }
            set
            {
                ResXResourceSet? resx = resxResourceSet;
                if (resx == null)
                    Throw.ObjectDisposedException();

                resx.CloneValues = value;
            }
        }

        #endregion

        #region Explicitly Implemented Interface Properties

        bool IExpandoResourceSet.SafeMode
        {
            get
            {
                ResXResourceSet? resx = resxResourceSet;
                if (resx == null)
                    Throw.ObjectDisposedException();

                return resx.SafeMode;
            }
            set
            {
                ResXResourceSet? resx = resxResourceSet;
                if (resx == null)
                    Throw.ObjectDisposedException();

                resx.SafeMode = value;
            }
        }

        #endregion

        #endregion

        #region Constructors

        internal HybridResourceSet(ResXResourceSet resx, ResourceSet compiled)
        {
            if (resx == null!)
                Throw.ArgumentNullException(Argument.resx);
            if (compiled == null!)
                Throw.ArgumentNullException(Argument.compiled);
            resxResourceSet = resx;
            compiledResourceSet = compiled;

#if NETFRAMEWORK
            // base ctor initializes a Hashtable that we don't need (and the base(false) ctor is not available).
            Table = null;
#endif
        }

        #endregion

        #region Methods

        #region Public Methods

        public override Type GetDefaultReader()
        {
            // actually there is no HybridResourceReader so returning the more dynamic XML version here
            return typeof(ResXResourceReader);
        }

        public override Type GetDefaultWriter()
        {
            // actually there is no HybridResourceWriter so returning the more dynamic XML version here
            return typeof(ResXResourceWriter);
        }

        public override IDictionaryEnumerator GetEnumerator()
        {
            ResXResourceSet? resx = resxResourceSet;
            ResourceSet? compiled = compiledResourceSet;
            if (resx == null || compiled == null)
                Throw.ObjectDisposedException();

            // changing is checked in resx resource set
            return new Enumerator(this, (ResXResourceEnumerator)resx.GetEnumerator(), compiled.GetEnumerator(), ((IResXResourceContainer)resx).Version);
        }

        public override object? GetObject(string name)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            return GetResource(name, false, false, resx.SafeMode, resx.CloneValues);
        }

        public override object? GetObject(string name, bool ignoreCase)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            return GetResource(name, ignoreCase, false, resx.SafeMode, resx.CloneValues);
        }

        public override string? GetString(string name)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            return (string?)GetResource(name, false, true, resx.SafeMode, resx.CloneValues);
        }

        public override string? GetString(string name, bool ignoreCase)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            return (string?)GetResource(name, ignoreCase, true, resx.SafeMode, resx.CloneValues);
        }

        public object? GetResource(string name, bool ignoreCase, bool isString, bool asSafe, bool cloneValue)
        {
            ResXResourceSet? resx = resxResourceSet;
            ResourceSet? compiled = compiledResourceSet;
            if (resx == null || compiled == null)
                Throw.ObjectDisposedException();

            object? result = resx.GetResourceInternal(name, ignoreCase, isString, asSafe, cloneValue);

            if (result != null)
                return result;

            // if the null result is because it is explicitly stored, hiding the compiled value
            if (resx.ContainsResource(name, ignoreCase))
                return null;

            return isString ? compiled.GetString(name, ignoreCase) : compiled.GetObject(name, ignoreCase);
        }

        public object? GetMeta(string name, bool ignoreCase, bool isString, bool asSafe, bool cloneValue)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            return resx.GetMetaInternal(name, ignoreCase, isString, asSafe, cloneValue);
        }

        public IDictionaryEnumerator GetMetadataEnumerator()
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            return resx.GetMetadataEnumerator();
        }

        public IDictionaryEnumerator GetAliasEnumerator()
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            return resx.GetAliasEnumerator();
        }

        public bool ContainsResource(string name, bool ignoreCase)
        {
            ResXResourceSet? resx = resxResourceSet;
            ResourceSet? compiled = compiledResourceSet;
            if (resx == null || compiled == null)
                Throw.ObjectDisposedException();

            if (resx.ContainsResource(name, ignoreCase))
                return true;

            HashSet<string>? binKeys = compiledKeys;
            if (binKeys == null)
            {
                // no foreach because that would evaluate (deserialize) the values, too
                binKeys = new HashSet<string>();
                IDictionaryEnumerator compiledEnumerator = compiled.GetEnumerator();
                while (compiledEnumerator.MoveNext())
                    binKeys.Add(compiledEnumerator.Key!.ToString()!);

                compiledKeys = binKeys;
            }

            if (binKeys.Contains(name))
                return true;

            if (!ignoreCase)
                return false;

            HashSet<string>? binKeysIgnoreCase = compiledKeysCaseInsensitive;
            if (binKeysIgnoreCase == null)
                compiledKeysCaseInsensitive = binKeysIgnoreCase = new HashSet<string>(binKeys, StringComparer.OrdinalIgnoreCase);

            return binKeysIgnoreCase.Contains(name);
        }

        public bool ContainsMeta(string name, bool ignoreCase)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            return resx.ContainsMeta(name, ignoreCase);
        }

        public object? GetMetaObject(string name, bool ignoreCase = false)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            return resx.GetMetaInternal(name, ignoreCase, false, resx.SafeMode, resx.CloneValues);
        }

        public string? GetMetaString(string name, bool ignoreCase = false)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            return (string?)resx.GetMetaInternal(name, ignoreCase, true, resx.SafeMode, resx.CloneValues);
        }

        public string? GetAliasValue(string alias)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            return resx.GetAliasValue(alias);
        }

        public void SetObject(string name, object? value)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            resx.SetObject(name, value);
        }

        public void SetMetaObject(string name, object? value)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            resx.SetMetaObject(name, value);
        }

        public void SetAliasValue(string alias, string assemblyName)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            resx.SetAliasValue(alias, assemblyName);
        }

        public void RemoveObject(string name)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            resx.RemoveObject(name);
        }

        public void RemoveMetaObject(string name)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            resx.RemoveMetaObject(name);
        }

        public void RemoveAliasValue(string alias)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            resx.RemoveAliasValue(alias);
        }

        public void Save(string fileName, bool compatibleFormat = false, bool forceEmbeddedResources = false, string? newBasePath = null)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            resx.Save(fileName, compatibleFormat, forceEmbeddedResources, newBasePath);
        }

        public void Save(Stream stream, bool compatibleFormat = false, bool forceEmbeddedResources = false, string? newBasePath = null)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            resx.Save(stream, compatibleFormat, forceEmbeddedResources, newBasePath);
        }

        public void Save(TextWriter textWriter, bool compatibleFormat = false, bool forceEmbeddedResources = false, string? newBasePath = null)
        {
            ResXResourceSet? resx = resxResourceSet;
            if (resx == null)
                Throw.ObjectDisposedException();

            resx.Save(textWriter, compatibleFormat, forceEmbeddedResources, newBasePath);
        }

        #endregion

        #region Protected Methods

        protected override void Dispose(bool disposing)
        {
            ResourceSet? resx = resxResourceSet;
            ResourceSet? compiled = compiledResourceSet;
            if (resx == null || compiled == null)
                return;

            // not disposing the wrapped resource sets just nullifying them because their life cycle can be longer
            // than the hybrid one (eg. changing source from mixed to single one).
            resxResourceSet = null;
            compiledResourceSet = null;
            compiledKeys = null;
            compiledKeysCaseInsensitive = null;
            base.Dispose(disposing);
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #endregion
    }
}
