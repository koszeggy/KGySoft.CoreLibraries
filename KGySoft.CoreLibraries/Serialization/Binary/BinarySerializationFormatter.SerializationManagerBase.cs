#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.SerializationManagerBase.cs
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
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;

#if NETFRAMEWORK || NETSTANDARD2_0
using KGySoft.CoreLibraries; 
#endif
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization.Binary
{
    public sealed partial class BinarySerializationFormatter
    {
        private abstract class SerializationManagerBase
        {
            #region Enumerations

            private protected enum GenericTypeSpecifier
            {
                TypeDefinition,
                ConstructedType,
                GenericParameter,
            }

            #endregion

            #region Fields

            #region Static Fields

            private protected static readonly Assembly[] KnownAssemblies =
            {
                // Do not add more assemblies. We must stay consistent on different platforms.
                AssemblyResolver.CoreLibrariesAssembly, // and for compatibility, mscorlib maps also here on every platform
                AssemblyResolver.KGySoftCoreLibrariesAssembly
            };

            /// <summary>
            /// These types are always dumped by index and are never passed to a binder.
            /// </summary>
            private protected static readonly Type[] KnownTypes =
            {
                Reflector.NullableType,
                //Reflector.ObjectType,

                // These types are just added for sparing 1 byte when they are stored for the fist time.
                // Other primitives (U/IntPtr) are also protected from binder but they are stored as new type first
                Reflector.BoolType,
                Reflector.SByteType,
                Reflector.ByteType,
                Reflector.ShortType,
                Reflector.UShortType,
                Reflector.IntType,
                Reflector.UIntType,
                Reflector.LongType,
                Reflector.ULongType,
                Reflector.FloatType,
                Reflector.DoubleType,
                Reflector.CharType,
                Reflector.StringType,

                // Also for sparing. Other compressible types are added for the first time
                typeof(Compressible<short>),
                typeof(Compressible<ushort>),
                typeof(Compressible<int>),
                typeof(Compressible<uint>),
                typeof(Compressible<long>),
                typeof(Compressible<ulong>),
                typeof(Compressible<char>),

                // Technical helper types for special cases, must not be passed to binders
                compressibleType,
                genericMethodDefinitionPlaceholderType
            };

            #endregion

            #region Instance Fields

            #region Private Protected Fields

            private protected readonly BinarySerializationOptions Options;
            private protected readonly StreamingContext Context;
            private protected readonly SerializationBinder? Binder;

            #endregion

            #region Private Fields

            private readonly ISurrogateSelector? surrogateSelector;
            private readonly Dictionary<Type, KeyValuePair<ISerializationSurrogate?, ISurrogateSelector?>>? surrogates;
            private Dictionary<MemberInfo, TypeAttributes>? typeAttributes;

            #endregion

            #endregion

            #endregion

            #region Properties

            private protected bool ForceRecursiveSerializationOfSupportedTypes => (Options & BinarySerializationOptions.ForceRecursiveSerializationOfSupportedTypes) != BinarySerializationOptions.None;
#pragma warning disable 618
            private protected bool ForcedSerializationValueTypesAsFallback => (Options & BinarySerializationOptions.ForcedSerializationValueTypesAsFallback) != BinarySerializationOptions.None;
#pragma warning restore 618
            private protected bool RecursiveSerializationAsFallback => (Options & BinarySerializationOptions.RecursiveSerializationAsFallback) != BinarySerializationOptions.None;
            private protected bool IgnoreSerializationMethods => (Options & BinarySerializationOptions.IgnoreSerializationMethods) != BinarySerializationOptions.None;
            private protected bool IgnoreIBinarySerializable => (Options & BinarySerializationOptions.IgnoreIBinarySerializable) != BinarySerializationOptions.None;
            private protected bool OmitAssemblyQualifiedNames => (Options & BinarySerializationOptions.OmitAssemblyQualifiedNames) != BinarySerializationOptions.None;
            private protected bool CompactSerializationOfStructures => (Options & BinarySerializationOptions.CompactSerializationOfStructures) != BinarySerializationOptions.None;
            private protected bool IgnoreISerializable => (Options & BinarySerializationOptions.IgnoreISerializable) != BinarySerializationOptions.None;
            private protected bool IgnoreIObjectReference => (Options & BinarySerializationOptions.IgnoreIObjectReference) != BinarySerializationOptions.None;
            private protected bool IgnoreObjectChanges => (Options & BinarySerializationOptions.IgnoreObjectChanges) != BinarySerializationOptions.None;
            private protected bool TryUseSurrogateSelectorForAnyType => (Options & BinarySerializationOptions.TryUseSurrogateSelectorForAnyType) != BinarySerializationOptions.None;
            private protected bool IgnoreTypeForwardedFromAttribute => (Options & BinarySerializationOptions.IgnoreTypeForwardedFromAttribute) != BinarySerializationOptions.None;
            private protected bool SafeMode => (Options & BinarySerializationOptions.SafeMode) != BinarySerializationOptions.None;

            private protected Dictionary<MemberInfo, TypeAttributes> TypeAttributesCache => typeAttributes ??= new Dictionary<MemberInfo, TypeAttributes>();

            #endregion

            #region Constructors

            private protected SerializationManagerBase(StreamingContext context, BinarySerializationOptions options, SerializationBinder? binder, ISurrogateSelector? surrogateSelector)
            {
                Options = options;
                Context = context;
                Binder = binder;
                this.surrogateSelector = surrogateSelector;
                if (surrogateSelector != null)
                    surrogates = new Dictionary<Type, KeyValuePair<ISerializationSurrogate?, ISurrogateSelector?>>();
            }

            #endregion

            #region Methods

            #region Static Methods

            private static IEnumerable<MethodInfo>? GetMethodsWithAttribute(Type attribute, Type type)
            {
                Dictionary<Type, IEnumerable<MethodInfo>?> cacheItem = methodsByAttributeCache[type];

                lock (cacheItem)
                {
                    if (cacheItem.TryGetValue(attribute, out IEnumerable<MethodInfo>? cachedResult))
                        return cachedResult;

                    List<MethodInfo> result = new List<MethodInfo>();
                    for (Type? t = type; t != null && t != Reflector.ObjectType; t = t.BaseType)
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

                    // to make sure that most derived is considered first (could be a CircularList but a List consumes less memory)
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

            #region Private Protected Methods

            /// <summary>
            /// Gets if a type can use a surrogate
            /// </summary>
            [SecurityCritical]
            private protected bool CanUseSurrogate(Type type)
            {
                if (surrogateSelector == null)
                    return false;

                if (type.IsPrimitive || type.IsArray || type == Reflector.StringType || type == Reflector.ObjectType || type.IsPointer || type.IsByRef)
                    return false;

                return TryGetSurrogate(type, out var _, out var _);
            }

            /// <summary>
            /// Tries to get a surrogate for a type
            /// </summary>
            [SecurityCritical]
            private protected bool TryGetSurrogate(Type type, out ISerializationSurrogate? surrogate, out ISurrogateSelector? selector)
            {
                surrogate = null;
                selector = null;
                if (surrogateSelector == null)
                    return false;

                if (surrogates!.TryGetValue(type, out KeyValuePair<ISerializationSurrogate?, ISurrogateSelector?> result))
                {
                    if (result.Key == null)
                        return false;

                    surrogate = result.Key;
                    selector = result.Value;
                    return true;
                }

                DoGetSurrogate(type, out surrogate, out selector);
                surrogates[type] = new KeyValuePair<ISerializationSurrogate?, ISurrogateSelector?>(surrogate, selector);
                return surrogate != null;
            }

            private protected void ExecuteMethodsOfAttribute(object obj, Type attributeType)
            {
                if (IgnoreSerializationMethods)
                    return;

                var methods = GetMethodsWithAttribute(attributeType, obj.GetType());
                if (methods == null)
                    return;
                foreach (MethodInfo method in methods)
                    method.Invoke(obj, Context);
            }

            private protected bool IsValueType(DataTypeDescriptor descriptor)
            {
                MemberInfo type = (MemberInfo?)descriptor.StoredType ?? descriptor.Type!;
                Debug.Assert(!IsImpureTypeButEnum(GetCollectionOrElementType(descriptor.DataType)) || TypeAttributesCache.ContainsKey(type), $"Attributes of type is not cached: {descriptor}");
                return IsImpureTypeButEnum(GetCollectionOrElementType(descriptor.DataType))
                    ? (TypeAttributesCache.GetValueOrDefault(type) & TypeAttributes.ValueType) != TypeAttributes.None
                    : descriptor.Type!.IsValueType;
            }

            private protected bool IsSealed(DataTypeDescriptor descriptor)
            {
                MemberInfo type = (MemberInfo?)descriptor.StoredType ?? descriptor.Type!;
                Debug.Assert(!IsImpureTypeButEnum(GetCollectionOrElementType(descriptor.DataType)) || TypeAttributesCache.ContainsKey(type), $"Attributes of type is not cached: {descriptor}");
                return IsImpureTypeButEnum(GetCollectionOrElementType(descriptor.DataType))
                    ? (TypeAttributesCache.GetValueOrDefault(type) & TypeAttributes.Sealed) != TypeAttributes.None
                    : descriptor.Type!.IsSealed;
            }

            #endregion

            #region Private Methods

            [SecurityCritical]
            private void DoGetSurrogate(Type type, out ISerializationSurrogate? surrogate, out ISurrogateSelector? selector)
                => surrogate = surrogateSelector!.GetSurrogate(type, Context, out selector);

            #endregion

            #endregion

            #endregion
        }
    }
}
