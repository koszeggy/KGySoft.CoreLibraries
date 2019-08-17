#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization
{
    public sealed partial class BinarySerializationFormatter
    {
        abstract class SerializationManagerBase
        {
            #region Fields

            #region Static Fields

            protected static readonly Assembly[] KnownAssemblies =
            {
                Reflector.CoreLibrariesAssembly,
#if NETFRAMEWORK
                Reflector.SystemAssembly,
                Reflector.SystemCoreAssembly,
#elif NETCOREAPP
                Reflector.SystemCollectionsAssembly,
#endif
                Reflector.KGySoftLibrariesAssembly
            };

            protected static readonly Type[] KnownTypes =
            {
                typeof(IBinarySerializable) // KGySoft.CoreLibraries
            };

            #endregion

            #region Instance Fields

            #region Internal Fields

            internal readonly BinarySerializationOptions Options;

            #endregion

            #region Protected Fields

            protected readonly SerializationBinder Binder;

            #endregion

            #region Private Fields

            private readonly StreamingContext context;
            private readonly ISurrogateSelector surrogateSelector;
            private readonly Dictionary<Type, KeyValuePair<ISerializationSurrogate, ISurrogateSelector>> surrogates;

            #endregion

            #endregion

            #endregion

            #region Constructors

            protected SerializationManagerBase(StreamingContext context, BinarySerializationOptions options, SerializationBinder binder, ISurrogateSelector surrogateSelector)
            {
                Options = options;
                this.context = context;
                Binder = binder;
                this.surrogateSelector = surrogateSelector;
                if (surrogateSelector != null)
                    surrogates = new Dictionary<Type, KeyValuePair<ISerializationSurrogate, ISurrogateSelector>>();
            }

            #endregion

            #region Methods

            /// <summary>
            /// Gets if a type can use a surrogate
            /// </summary>
            [SecurityCritical]
            internal bool CanUseSurrogate(Type type)
            {
                if (surrogateSelector == null)
                    return false;

                if (type.IsPrimitive || type.IsArray || type.In(Reflector.StringType, Reflector.ObjectType, Reflector.UIntPtrType))
                    return false;

                return TryGetSurrogate(type, out var _, out var _);
            }

            /// <summary>
            /// Tries to get a surrogate for a type
            /// </summary>
            [SecurityCritical]
            internal bool TryGetSurrogate(Type type, out ISerializationSurrogate surrogate, out ISurrogateSelector selector)
            {
                surrogate = null;
                selector = null;
                if (surrogateSelector == null)
                    return false;

                if (surrogates.TryGetValue(type, out KeyValuePair<ISerializationSurrogate, ISurrogateSelector> result))
                {
                    if (result.Key == null)
                        return false;

                    surrogate = result.Key;
                    selector = result.Value;
                    return true;
                }

                DoGetSurrogate(type, out surrogate, out selector);
                surrogates[type] = new KeyValuePair<ISerializationSurrogate, ISurrogateSelector>(surrogate, selector);
                return surrogate != null;
            }

            [SecurityCritical]
            private void DoGetSurrogate(Type type, out ISerializationSurrogate surrogate, out ISurrogateSelector selector)
                => surrogate = surrogateSelector.GetSurrogate(type, context, out selector);

            #endregion
        }
    }
}
