using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using KGySoft.Libraries;
using KGySoft.Reflection;

namespace KGySoft.Serialization
{

    public sealed partial class BinarySerializationFormatter
    {
        abstract class SerializationManagerBase
        {
            #region Static Fields

            protected static readonly Assembly[] KnownAssemblies =
            {
                Reflector.mscorlibAssembly,
                Reflector.SystemAssembly,
                Reflector.SystemCoreAssembly,
                Reflector.KGySoftLibrariesAssembly
            };

            protected static readonly Type[] KnownTypes =
            {
                typeof(IBinarySerializable) // KGySoft.Libraries
            };

            #endregion

            internal readonly BinarySerializationOptions Options;
            private readonly StreamingContext context;
            protected readonly SerializationBinder Binder;
            private readonly ISurrogateSelector surrogateSelector;
            private readonly Dictionary<Type, KeyValuePair<ISerializationSurrogate, ISurrogateSelector>> surrogates;

            protected SerializationManagerBase(StreamingContext context, BinarySerializationOptions options, SerializationBinder binder, ISurrogateSelector surrogateSelector)
            {
                Options = options;
                this.context = context;
                Binder = binder;
                this.surrogateSelector = surrogateSelector;
                if (surrogateSelector != null)
                {
                    surrogates = new Dictionary<Type, KeyValuePair<ISerializationSurrogate, ISurrogateSelector>>();
                }
            }

            /// <summary>
            /// Gets if a type can use a surrogate
            /// </summary>
            internal bool CanUseSurrogate(Type type)
            {
                if (surrogateSelector == null)
                    return false;

                if (type.IsPrimitive || type.IsArray || type.In(typeof(string), typeof(object), typeof(UIntPtr)))
                    return false;

                ISerializationSurrogate surrogate;
                ISurrogateSelector selector;
                return TryGetSurrogate(type, out surrogate, out selector);
            }

            /// <summary>
            /// Tries to get a surrogate for a type
            /// </summary>
            internal bool TryGetSurrogate(Type type, out ISerializationSurrogate surrogate, out ISurrogateSelector selector)
            {
                surrogate = null;
                selector = null;
                if (surrogateSelector == null)
                    return false;

                KeyValuePair<ISerializationSurrogate, ISurrogateSelector> result;
                if (surrogates.TryGetValue(type, out result))
                {
                    if (result.Key == null)
                        return false;

                    surrogate = result.Key;
                    selector = result.Value;
                    return true;
                }

                surrogate = surrogateSelector.GetSurrogate(type, context, out selector);
                surrogates[type] = new KeyValuePair<ISerializationSurrogate, ISurrogateSelector>(surrogate, selector);
                return surrogate != null;
            }
        }
    }
}
