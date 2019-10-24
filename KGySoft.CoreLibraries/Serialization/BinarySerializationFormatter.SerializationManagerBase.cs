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
using System.Diagnostics.CodeAnalysis;
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
        private abstract class SerializationManagerBase
        {
            #region Nested Types

            #region Enumerations

            private protected enum GenericTypeSpecifier
            {
                TypeDefinition,
                ConstructedType,
                GenericParameter,
            }

            #endregion

            #endregion

            #region Fields

            #region Static Fields

            private protected static readonly Assembly[] KnownAssemblies =
            {
                // Do not add more assemblies. We must stay consistent on different platforms.
                Reflector.SystemCoreLibrariesAssembly,
                Reflector.KGySoftCoreLibrariesAssembly
            };

            private protected static readonly Type[] KnownTypes =
            {
                // Apart from natively supported types, these are always dumped by index and are never passed to a binder.
                // Object is relevant when recursive serialization of known types is forced.
                // Type/Array/Enum are abstract base types of supported types
                // Nullable and the rest are treated in a special way
                Reflector.NullableType,
                Reflector.ObjectType,
                Reflector.Type,
                Reflector.ArrayType,
                Reflector.EnumType,
                typeof(Compressible<>),
                typeof(GenericMethodDefinitionPlaceholder)
            };

            #endregion

            #region Instance Fields

            #region Private Protected Fields

            private protected readonly BinarySerializationOptions Options;
            private protected readonly StreamingContext Context;
            private protected readonly SerializationBinder Binder;

            #endregion

            #region Private Fields

            private readonly ISurrogateSelector surrogateSelector;
            private readonly Dictionary<Type, KeyValuePair<ISerializationSurrogate, ISurrogateSelector>> surrogates;

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

            #endregion

            #region Constructors

            private protected SerializationManagerBase(StreamingContext context, BinarySerializationOptions options, SerializationBinder binder, ISurrogateSelector surrogateSelector)
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

                if (type.IsPrimitive || type.IsArray || type == Reflector.StringType || type == Reflector.ObjectType || type.IsPointer || type.IsByRef)
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

            /// <summary>
            /// Gets the <see cref="DataTypes"/> representation of <paramref name="type"/>.
            /// </summary>
            [SecurityCritical]
            [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
                Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
            internal DataTypes GetDataType(Type type)
            {
                #region Local methods to reduce complexity

                bool TryGetKnownDataType(Type t, out DataTypes result)
                {
                    // Primitive type
                    if (primitiveTypes.TryGetValue(t, out result))
                        return true;

                    // Primitive nullable (must be before surrogate-support checks)
                    bool isNullable = t.IsNullable();
                    if (isNullable)
                    {
                        // the Nullable<> definition or open generic types are encoded recursively
                        if (t.IsGenericTypeDefinition || t.ContainsGenericParameters)
                        {
                            result = DataTypes.RecursiveObjectGraph;
                            return true;
                        }

                        result = GetDataType(t.GetGenericArguments()[0]);
                        if (IsElementType(result) && IsPureType(result))
                        {
                            result |= DataTypes.Nullable;
                            return true;
                        }
                    }

                    // Non-primitive types that cannot be serialized recursively
                    if (t.IsArray)
                    {
                        result = DataTypes.Array;
                        return true;
                    }

                    if (t.IsPointer)
                    {
                        result = DataTypes.Pointer;
                        return true;
                    }

                    if (t.IsByRef)
                    {
                        result = DataTypes.ByRef;
                        return true;
                    }

                    // Recursion for any type (except primitives and array)
                    if (ForceRecursiveSerializationOfSupportedTypes || TryUseSurrogateSelectorForAnyType && CanUseSurrogate(t))
                    {
                        result = DataTypes.RecursiveObjectGraph;
                        if (isNullable)
                            result |= DataTypes.Nullable;
                        return true;
                    }

                    // Non-primitive nullable
                    if (isNullable)
                    {
                        // result is now the result of the recursive call
                        switch (result)
                        {
                            case DataTypes.DictionaryEntry:
                                result = DataTypes.DictionaryEntryNullable;
                                return true;
                            case DataTypes.KeyValuePair:
                                result = DataTypes.KeyValuePairNullable;
                                return true;
                            default:
                                result |= DataTypes.Nullable;
                                return true;
                        }
                    }

                    // Natively supported non-primitive type
                    if (supportedNonPrimitiveElementTypes.TryGetValue(t, out result))
                        return true;

                    // enum
                    if (t.IsEnum)
                    {
                        result = DataTypes.Enum | primitiveTypes[Enum.GetUnderlyingType(t)];
                        return true;
                    }

                    // supported collection
                    Type collType = t.IsGenericType ? t.GetGenericTypeDefinition()
                        : t.IsGenericParameter && t.DeclaringMethod == null ? t.DeclaringType
                        : t;

                    // ReSharper disable once AssignNullToNotNullAttribute
                    if (supportedCollections.TryGetValue(collType, out result))
                        return true;

                    return false;
                }

                DataTypes GetImpureDataType(Type t)
                {
                    // IBinarySerializable implementation
                    if (!IgnoreIBinarySerializable && typeof(IBinarySerializable).IsAssignableFrom(t))
                        return DataTypes.BinarySerializable;

                    // Any struct if can be serialized
                    if (CompactSerializationOfStructures && t.IsValueType && BinarySerializer.CanSerializeValueType(t, false))
                        return DataTypes.RawStruct;

                    // Recursive serialization
                    if (RecursiveSerializationAsFallback || t.IsInterface || t.IsSerializable || CanUseSurrogate(t))
                        return DataTypes.RecursiveObjectGraph;

#pragma warning disable 618, 612
                    // Any struct (obsolete but still supported as backward compatibility)
                    if (ForcedSerializationValueTypesAsFallback && t.IsValueType)
                        return DataTypes.RawStruct;
#pragma warning restore 618, 612

                    // It is alright for a collection element type. If no recursive serialization is allowed it will turn out for the items.
                    return DataTypes.RecursiveObjectGraph;
                }

                #endregion

                // a.) Well-known types or forced recursion
                if (TryGetKnownDataType(type, out DataTypes dataType))
                    return dataType;

                // b.) Non-pure types
                return GetImpureDataType(type);
            }

            #endregion

            #region Private Protected Methods

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
