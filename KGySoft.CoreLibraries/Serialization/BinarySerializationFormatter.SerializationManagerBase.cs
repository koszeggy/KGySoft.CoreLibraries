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

using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization
{
    public sealed partial class BinarySerializationFormatter
    {
        private abstract class SerializationManagerBase
        {
            #region Enumerations

            protected enum GenericTypeSpecifier
            {
                TypeDefinition,
                ConstructedType,
                GenericParameter
            }

            #endregion

            #region Fields

            #region Static Fields

            protected static readonly Assembly[] KnownAssemblies =
            {
                Reflector.SystemCoreLibrariesAssembly,
                Reflector.KGySoftLibrariesAssembly,
#if NETFRAMEWORK
                typeof(Queue<>).Assembly, // System.dll
                typeof(HashSet<>).Assembly // System.Core.dll
#endif

            };

            protected static readonly Type[] KnownTypes =
            {
                typeof(IBinarySerializable) // KGySoft.CoreLibraries
            };

            #endregion

            #region Instance Fields

            #region Protected Fields

            protected readonly BinarySerializationOptions Options;

            protected readonly StreamingContext Context;

            protected readonly SerializationBinder Binder;

            #endregion

            #region Private Fields

            private readonly ISurrogateSelector surrogateSelector;
            private readonly Dictionary<Type, KeyValuePair<ISerializationSurrogate, ISurrogateSelector>> surrogates;

            #endregion

            #endregion

            #endregion

            #region Properties

            protected bool ForceRecursiveSerializationOfSupportedTypes => (Options & BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes) != BinarySerializationOptions.None;
#pragma warning disable 618
            protected bool ForcedSerializationValueTypesAsFallback => (Options & BinarySerializationOptions.ForcedSerializationValueTypesAsFallback) != BinarySerializationOptions.None;
#pragma warning restore 618
            protected bool RecursiveSerializationAsFallback => (Options & BinarySerializationOptions.RecursiveSerializationAsFallback) != BinarySerializationOptions.None;
            protected bool IgnoreSerializationMethods => (Options & BinarySerializationOptions.IgnoreSerializationMethods) != BinarySerializationOptions.None;
            protected bool IgnoreIBinarySerializable => (Options & BinarySerializationOptions.IgnoreIBinarySerializable) != BinarySerializationOptions.None;
            protected bool OmitAssemblyQualifiedNames => (Options & BinarySerializationOptions.OmitAssemblyQualifiedNames) != BinarySerializationOptions.None;
            protected bool CompactSerializationOfStructures => (Options & BinarySerializationOptions.CompactSerializationOfStructures) != BinarySerializationOptions.None;
            protected bool IgnoreISerializable => (Options & BinarySerializationOptions.IgnoreISerializable) != BinarySerializationOptions.None;
            protected bool IgnoreIObjectReference => (Options & BinarySerializationOptions.IgnoreIObjectReference) != BinarySerializationOptions.None;
            protected bool IgnoreObjectChanges => (Options & BinarySerializationOptions.IgnoreObjectChanges) != BinarySerializationOptions.None;
            protected bool TryUseSurrogateSelectorForAnyType => (Options & BinarySerializationOptions.TryUseSurrogateSelectorForAnyType) != BinarySerializationOptions.None;

            #endregion

            #region Constructors

            protected SerializationManagerBase(StreamingContext context, BinarySerializationOptions options, SerializationBinder binder, ISurrogateSelector surrogateSelector)
            {
                Options = options;
                Context = context;
                Binder = binder;
                this.surrogateSelector = surrogateSelector;
                if (surrogateSelector != null)
                    surrogates = new Dictionary<Type, KeyValuePair<ISerializationSurrogate, ISurrogateSelector>>();
            }

            #endregion

            #region Methods

            #region Static Methods

            private static IEnumerable<MethodInfo> GetMethodsWithAttribute(Type attribute, Type type)
            {
                Dictionary<Type, IEnumerable<MethodInfo>> cacheItem = methodsByAttributeCache[type];

                lock (cacheItem)
                {
                    if (cacheItem.TryGetValue(attribute, out IEnumerable<MethodInfo> cachedResult))
                        return cachedResult;

                    List<MethodInfo> result = new List<MethodInfo>();
                    for (Type t = type; t != null && t != Reflector.ObjectType; t = t.BaseType)
                    {
                        foreach (MethodInfo method in t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                        {
                            if (method.IsDefined(attribute, false))
                            {
                                ParameterInfo[] parameters = method.GetParameters();
                                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(StreamingContext))
                                {
                                    result.Add(method);
                                }
                            }
                        }
                    }

                    if (result.Count > 1)
                        result.Reverse();

                    if (result.Count == 0)
                    {
                        cacheItem[attribute] = null;
                        return null;
                    }

                    cacheItem[attribute] = result;
                    return result;
                }
            }

            #endregion

            #region Instance Methods

            #region Internal Methods

            /// <summary>
            /// Gets if a type can use a surrogate
            /// </summary>
            [SecurityCritical]
            internal bool CanUseSurrogate(Type type)
            {
                if (surrogateSelector == null)
                    return false;

                if (type.IsPrimitive || type.IsArray || type == Reflector.StringType || type == Reflector.ObjectType)
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

            #endregion

            #region Protected Methods

            protected void ExecuteMethodsOfAttribute(object obj, Type attributeType)
            {
                if (IgnoreSerializationMethods)
                    return;

                var methods = GetMethodsWithAttribute(attributeType, obj.GetType());
                if (methods == null)
                    return;
                foreach (MethodInfo method in methods)
                    MethodAccessor.GetAccessor(method).Invoke(obj, Context);
            }

            #endregion

            #region Private Methods

            [SecurityCritical]
            private void DoGetSurrogate(Type type, out ISerializationSurrogate surrogate, out ISurrogateSelector selector)
                => surrogate = surrogateSelector.GetSurrogate(type, Context, out selector);

            #endregion

            #endregion

            #endregion
        }
    }
}
