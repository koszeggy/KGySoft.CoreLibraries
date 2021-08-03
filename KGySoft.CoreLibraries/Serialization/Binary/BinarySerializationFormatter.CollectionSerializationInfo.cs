#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: BinarySerializationFormatter.CollectionSerializationInfo.cs
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
#if !NET35
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;

using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Serialization.Binary
{
    public sealed partial class BinarySerializationFormatter
    {
        /// <summary>
        /// Static descriptor for collection types. Instance-specific descriptor is in <see cref="DataTypeDescriptor"/>.
        /// </summary>
        private sealed class CollectionSerializationInfo
        {
            #region Constants

            private const int capacityThreshold = 1 << 13;

            #endregion

            #region Fields

            #region Static Fields

            internal static readonly CollectionSerializationInfo Default = new CollectionSerializationInfo();

            #endregion

            #region Instance Fields

            /// <summary>
            /// Can contain more elements only for generic collections. Will be instantiated only on deserialization.
            /// Thread safe accessor because the serialization info is stored in a static shared dictionary.
            /// </summary>
            private IThreadSafeCacheAccessor<Type, CreateInstanceAccessor>? ctorCache;

            private IThreadSafeCacheAccessor<Type, MethodAccessor>? addMethodCache;

            #endregion

            #endregion

            #region Properties

            #region Internal Properties

            internal CollectionInfo Info { private get; set; }

            /// <summary>
            /// Specifies the constructor arguments to be used. Order matters!
            /// </summary>
            internal CollectionCtorArguments[]? CtorArguments { private get; set; }

            /// <summary>
            /// Should be specified only when target collection is not <see cref="IList"/>, <see cref="IDictionary"/> or <see cref="ICollection{T}"/> implementation,
            /// or when defining it results faster access than resolving the generic Add method for each access. Can refer to a generic method definition.
            /// </summary>
            internal string? SpecificAddMethod { get; set; }

#if !NET35
            [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
            [SuppressMessage("ReSharper", "MemberCanBePrivate.Local", Justification = "For some targets it is needed to be internal")] 
#endif
            internal bool IsGeneric => (Info & CollectionInfo.IsGeneric) == CollectionInfo.IsGeneric;
            internal bool ReverseElements => (Info & CollectionInfo.ReverseElements) == CollectionInfo.ReverseElements;
            internal bool IsNonGenericCollection => !IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.None;
            internal bool IsNonGenericDictionary => !IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary;
            internal bool IsGenericCollection => IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.None;
            internal bool IsGenericDictionary => IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary;
            internal bool IsDictionary => (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary;
            internal bool IsSingleElement => (Info & CollectionInfo.IsSingleElement) == CollectionInfo.IsSingleElement;

            #endregion

            #region Private Properties

            private bool HasCapacity => (Info & CollectionInfo.HasCapacity) == CollectionInfo.HasCapacity;
            private bool HasEqualityComparer => (Info & CollectionInfo.HasEqualityComparer) == CollectionInfo.HasEqualityComparer;
            private bool HasAnyComparer => HasEqualityComparer || (Info & CollectionInfo.HasComparer) == CollectionInfo.HasComparer;
            private bool HasCaseInsensitivity => (Info & CollectionInfo.HasCaseInsensitivity) == CollectionInfo.HasCaseInsensitivity;
            private bool HasReadOnly => (Info & CollectionInfo.HasReadOnly) == CollectionInfo.HasReadOnly;
            private bool DefaultEnumComparer => (Info & CollectionInfo.DefaultEnumComparer) == CollectionInfo.DefaultEnumComparer;
            private bool IsNonNullDefaultComparer => (Info & CollectionInfo.NonNullDefaultComparer) == CollectionInfo.NonNullDefaultComparer;

            #endregion

            #endregion

            #region Methods

            #region Public Methods

            public override string ToString() => Info.ToString<CollectionInfo>();

            #endregion

            #region Internal Methods

            /// <summary>
            /// Writes specific properties of a collection that are needed for deserialization
            /// </summary>
            [SecurityCritical]
            internal void WriteSpecificProperties(BinaryWriter bw, [NoEnumeration]IEnumerable collection, SerializationManager manager)
            {
                if (IsSingleElement)
                    return;

                // 1.) Count
                Write7BitInt(bw, collection.Count());

                // 2.) Capacity - public property in all cases
                if (HasCapacity)
                    Write7BitInt(bw, collection.Capacity());

                // 3.) Case sensitivity - only HybridDictionary
                if (HasCaseInsensitivity)
                    bw.Write(collection.IsCaseInsensitive());

                // 4.) ReadOnly
                if (HasReadOnly)
                {
                    switch (collection)
                    {
                        case IList list:
                            bw.Write(list.IsReadOnly);
                            break;
                        case IDictionary dictionary:
                            bw.Write(dictionary.IsReadOnly);
                            break;
                        default:
                            // should never occur for supported collections
                            bw.Write(false);
                            Debug.Fail("Could not write IsReadOnly state of collection " + collection.GetType());
                            break;
                    }
                }

                // 5.) Comparer
                if (HasAnyComparer)
                {
                    object? comparer = collection.GetComparer();
                    bool isDefaultComparer = comparer == null || IsDefaultComparer(collection, comparer);
                    bw.Write(isDefaultComparer);
                    if (!isDefaultComparer)
                        manager.WriteNonRoot(bw, comparer!);
                }
            }

            /// <summary>
            /// Creates collection and reads all serialized specific properties that were written by <see cref="WriteSpecificProperties"/>.
            /// </summary>
            [SecurityCritical]
            internal object InitializeCollection(BinaryReader br, bool addToCache, DataTypeDescriptor descriptor, DeserializationManager manager, bool safeMode, out int count)
            {
                object result;

                // KeyValuePair, DictionaryEntry
                if (IsSingleElement)
                {
                    // Note: If addToCache is true, then the key-value may contain itself via references.
                    // That's why we create the instance first and then set Key and Value (just like at object graphs).
                    result = Activator.CreateInstance(descriptor.GetTypeToCreate())!;
                    if (addToCache)
                        manager.AddObjectToCache(result);

                    count = 1;
                    return result;
                }

                // 1.) Count
                count = Read7BitInt(br);

                // 2.) Capacity
                int capacity = HasCapacity ? Read7BitInt(br) : count;
                if (safeMode && (HasCapacity || CtorArguments?.Contains(CollectionCtorArguments.Capacity) == true))
                    capacity = Math.Min(count, (capacityThreshold >> (IsDictionary ? 1 : 0)) / descriptor.ElementDescriptor!.Type!.SizeOf());

                // 3.) Case sensitivity
                bool caseInsensitive = false;
                if (HasCaseInsensitivity)
                    caseInsensitive = br.ReadBoolean();

                // 4.) Read-only
                if (HasReadOnly)
                    descriptor.IsReadOnly = br.ReadBoolean();

                // In the ID cache the collection comes first and then the comparer so we add a placeholder to the cache.
                // Unlike for KeyValuePairs this works here because we can assume that a comparer does not reference the collection.
                int id = 0;
                if (addToCache)
                    manager.AddObjectToCache(null, out id);

                // 5.) Comparer
                object? comparer = HasAnyComparer
                    ? br.ReadBoolean() // is default?
                        ? IsNonNullDefaultComparer ? GetDefaultComparer(descriptor.Type!) : null
                        : manager.ReadWithType(br)
                    : null;

                result = CreateCollection(descriptor, capacity, caseInsensitive, comparer);
                if (id != 0)
                    manager.ReplaceObjectInCache(id, result);

                return result;
            }

            internal MethodAccessor GetAddMethod(DataTypeDescriptor descriptor)
            {
                MethodAccessor GetAddMethodAccessor(Type type)
                {
                    string methodName = SpecificAddMethod ?? "Add"; // if not specified called for .NET 3.5 with null dictionary/collection values.

                    MethodInfo method = IsGeneric
                        ? type.GetMethod(methodName, type.GetGenericArguments())! // Using type arguments to eliminate ambiguity (LinkedList<T>.AddLast)
                        : type.GetMethod(methodName)!; // For non-generics arguments are not always objects (StringDictionary)
                    return MethodAccessor.GetAccessor(method);
                }

                if (addMethodCache == null)
                    Interlocked.CompareExchange(ref addMethodCache, ThreadSafeCacheFactory.Create<Type, MethodAccessor>(GetAddMethodAccessor, LockFreeCacheOptions.Profile128), null);
                return addMethodCache[descriptor.Type!];
            }

            #endregion

            #region Private Methods

            private bool IsDefaultComparer([NoEnumeration]IEnumerable collection, object? comparer)
            {
                object? defaultComparer = GetDefaultComparer(collection.GetType());
                if (Equals(defaultComparer, comparer))
                    return true;

                if (defaultComparer is Comparer def && comparer is Comparer c)
                    return Equals(def.CompareInfo(), c.CompareInfo());

                return false;
            }

            private object? GetDefaultComparer(Type type)
            {
                if (!IsGeneric)
                    return HasEqualityComparer ? null : Comparer.Default;

                Type elementType = type.GetGenericArguments()[0];
                if (DefaultEnumComparer && elementType.IsEnum)
                    return typeof(EnumComparer<>).GetPropertyValue(elementType, nameof(EnumComparer<_>.Comparer));
                return HasEqualityComparer
                    ? typeof(EqualityComparer<>).GetPropertyValue(elementType, nameof(EqualityComparer<_>.Default))
                    : typeof(Comparer<>).GetPropertyValue(elementType, nameof(Comparer<_>.Default));
            }

            private object CreateCollection(DataTypeDescriptor descriptor, int capacity, bool isCaseInsensitive, object? comparer)
            {
                CreateInstanceAccessor ctor = GetInitializer(descriptor);
                if (CtorArguments == null)
                    return ctor.CreateInstance();

                object?[] parameters = new object?[CtorArguments.Length];
                for (int i = 0; i < CtorArguments.Length; i++)
                {
                    switch (CtorArguments[i])
                    {
                        case CollectionCtorArguments.Capacity:
                            parameters[i] = capacity;
                            break;
                        case CollectionCtorArguments.Comparer:
                            parameters[i] = comparer;
                            break;
                        case CollectionCtorArguments.CaseInsensitivity:
                            parameters[i] = isCaseInsensitive;
                            break;
                        default:
                            return Throw.InternalError<object>($"Unsupported {nameof(CollectionCtorArguments)}");
                    }
                }

                return ctor.CreateInstance(parameters);
            }

            private CreateInstanceAccessor GetInitializer(DataTypeDescriptor descriptor)
            {
                CreateInstanceAccessor GetCtorAccessor(Type type)
                {
                    if (CtorArguments == null)
                        return CreateInstanceAccessor.GetAccessor(type);
                    Type[] args = new Type[CtorArguments.Length];
                    for (int i = 0; i < CtorArguments.Length; i++)
                    {
                        switch (CtorArguments[i])
                        {
                            case CollectionCtorArguments.Capacity:
                                args[i] = Reflector.IntType;
                                break;
                            case CollectionCtorArguments.CaseInsensitivity:
                                args[i] = Reflector.BoolType;
                                break;
                            case CollectionCtorArguments.Comparer:
                                args[i] = IsGeneric
                                    ? HasEqualityComparer
                                        ? typeof(IEqualityComparer<>).GetGenericType(type.GetGenericArguments()[0])
                                        : typeof(IComparer<>).GetGenericType(type.GetGenericArguments()[0])
                                    : HasEqualityComparer
                                        ? typeof(IEqualityComparer)
                                        : typeof(IComparer);
                                break;
                            default:
                                return Throw.InternalError<CreateInstanceAccessor>($"Unsupported {nameof(CollectionCtorArguments)}");
                        }
                    }

                    ConstructorInfo? ctor = type.GetConstructor(args);
                    if (ctor == null)
                        Throw.SerializationException(Res.ReflectionCtorNotFound(type));
                    return CreateInstanceAccessor.GetAccessor(ctor);
                }

                if (ctorCache == null)
                    Interlocked.CompareExchange(ref ctorCache, ThreadSafeCacheFactory.Create<Type, CreateInstanceAccessor>(GetCtorAccessor, LockFreeCacheOptions.Profile128), null);
                return ctorCache[descriptor.Type!];
            }

            #endregion

            #endregion
        }
    }
}
