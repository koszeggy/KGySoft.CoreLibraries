using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security;
using System.Threading;
using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

namespace KGySoft.Serialization
{
    public sealed partial class BinarySerializationFormatter
    {
        /// <summary>
        /// Static descriptor for collection types. Instance-specific descriptor is in <see cref="DataTypeDescriptor"/>.
        /// </summary>
        private sealed class CollectionSerializationInfo
        {
            #region Fields

            #region Static Fields

            internal static readonly CollectionSerializationInfo Default = new CollectionSerializationInfo();

            #endregion

            #region Instance Fields

            /// <summary>
            /// Can contain more elements only for generic collections. Will be instantiated only on deserialization.
            /// Locking accessor because the serialization info is stored in a static shared dictionary.
            /// </summary>
            private IThreadSafeCacheAccessor<Type, CreateInstanceAccessor> ctorCache;
            private IThreadSafeCacheAccessor<Type, MethodAccessor> addMethodCache;

            #endregion

            #endregion

            #region Properties

            #region Internal Properties

            internal CollectionInfo Info { private get; set; }

            /// <summary>
            /// Specifies the constructor arguments to be used. Order matters!
            /// </summary>
            internal CollectionCtorArguments[] CtorArguments { private get; set; }

            /// <summary>
            /// Should be specified only when target collection is not <see cref="IList"/> or <see cref="ICollection{T}"/> implementation,
            /// or when defining it results faster access than resolving the generic Add method for each access. Can refer to a generic method definition.
            /// </summary>
            internal string SpecificAddMethod { get; set; }

            internal bool ReverseElements => (Info & CollectionInfo.ReverseElements) == CollectionInfo.ReverseElements;
            internal bool IsNonGenericCollection => !IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.None;
            internal bool IsNonGenericDictionary => !IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary;
            internal bool IsGenericCollection => IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.None;
            internal bool IsGenericDictionary => IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary;
            internal bool IsDictionary => (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary;
            internal bool IsSingleElement => (Info & CollectionInfo.IsSingleElement) == CollectionInfo.IsSingleElement;

            #endregion

            #region Private Properties

            private bool IsGeneric => (Info & CollectionInfo.IsGeneric) == CollectionInfo.IsGeneric;
            private bool HasCapacity => (Info & CollectionInfo.HasCapacity) == CollectionInfo.HasCapacity;
            private bool HasEqualityComparer => (Info & CollectionInfo.HasEqualityComparer) == CollectionInfo.HasEqualityComparer;
            private bool HasAnyComparer => HasEqualityComparer || (Info & CollectionInfo.HasComparer) == CollectionInfo.HasComparer;
            private bool HasCaseInsensitivity => (Info & CollectionInfo.HasCaseInsensitivity) == CollectionInfo.HasCaseInsensitivity;
            private bool HasReadOnly => (Info & CollectionInfo.HasReadOnly) == CollectionInfo.HasReadOnly;
            private bool DefaultEnumComparer => (Info & CollectionInfo.DefaultEnumComparer) == CollectionInfo.DefaultEnumComparer;

            #endregion

            #endregion

            #region Methods

            #region Internal Methods

            /// <summary>
            /// Writes specific properties of a collection that are needed for deserialization
            /// </summary>
            [SecurityCritical]
            internal void WriteSpecificProperties(BinarySerializationFormatter owner, BinaryWriter bw, [NoEnumeration]IEnumerable collection, SerializationManager manager)
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
                            // should never occur for supported collections, throwing internal error without resource
                            Debug.Fail("Could not write IsReadOnly state of collection " + collection.GetType());
                            bw.Write(false);
                            break;
                    }
                }

                // 5.) Comparer
                if (HasAnyComparer)
                {
                    object comparer = collection.GetComparer();
                    bool isDefaultComparer = comparer == null || IsDefaultComparer(collection, comparer);
                    bw.Write(isDefaultComparer);
                    if (!isDefaultComparer)
                        owner.Write(bw, comparer, false, manager);
                }
            }

            /// <summary>
            /// Creates collection and reads all serialized specific properties that were written by <see cref="WriteSpecificProperties"/>.
            /// </summary>
            [SecurityCritical]
            internal object InitializeCollection(BinarySerializationFormatter owner, BinaryReader br, bool addToCache, DataTypeDescriptor descriptor, DeserializationManager manager, out int count)
            {
                if (IsSingleElement)
                {
                    count = 1;
                    return null;
                }

                // 1.) Count
                count = Read7BitInt(br);

                // 2.) Capacity
                int capacity = HasCapacity ? Read7BitInt(br) : count;

                // 3.) Case sensitivity
                bool caseInsensitive = false;
                if (HasCaseInsensitivity)
                    caseInsensitive = br.ReadBoolean();

                // 4.) Read-only
                if (HasReadOnly)
                    descriptor.IsReadOnly = br.ReadBoolean();

                // 5.) Comparer
                object comparer = null;
                if (HasAnyComparer && !br.ReadBoolean())
                    comparer = owner.Read(br, false, manager);

                object result = CreateCollection(descriptor, capacity, caseInsensitive, comparer);
                if (addToCache)
                    manager.AddObjectToCache(result);

                return result;
            }

            internal CreateInstanceAccessor GetInitializer(DataTypeDescriptor descriptor)
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
                                        ? typeof(IEqualityComparer<>).GetGenericType(descriptor.ElementType)
                                        : typeof(IComparer<>).GetGenericType(descriptor.ElementType)
                                    : HasEqualityComparer
                                        ? typeof(IEqualityComparer)
                                        : typeof(IComparer);
                                break;
                            case CollectionCtorArguments.Key:
                                args[i] = descriptor.ElementType;
                                break;
                            case CollectionCtorArguments.Value:
                                args[i] = descriptor.DictionaryValueType;
                                break;
                            default:
                                throw new InvalidOperationException($"Unsupported {nameof(CollectionCtorArguments)}");
                        }
                    }

                    return CreateInstanceAccessor.GetAccessor(type.GetConstructor(args) ?? throw new InvalidOperationException(Res.ReflectionCtorNotFound(type)));
                }

                if (ctorCache == null)
                    Interlocked.CompareExchange(ref ctorCache, new Cache<Type, CreateInstanceAccessor>(GetCtorAccessor).GetThreadSafeAccessor(), null);
                return ctorCache[descriptor.Type];
            }

            internal MethodAccessor GetAddMethod(DataTypeDescriptor descriptor, string addMethodName)
            {
                MethodAccessor GetAddMethodAccessor(Type type)
                {
                    Type[] args = descriptor.IsDictionary ? new[] { descriptor.ElementType, descriptor.DictionaryValueType } : new[] { descriptor.ElementType };
                    return MethodAccessor.GetAccessor(type.GetMethod(addMethodName, args));
                }

                if (addMethodName == null)
                    return null;
                if (addMethodCache == null)
                    Interlocked.CompareExchange(ref addMethodCache, new Cache<Type, MethodAccessor>(GetAddMethodAccessor).GetThreadSafeAccessor(), null);
                return addMethodCache[descriptor.Type];
            }

            #endregion

            #region Private Methods

            private bool IsDefaultComparer([NoEnumeration]IEnumerable collection, object comparer)
            {
                object GetDefaultComparer(Type type)
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

                object defaultComparer = GetDefaultComparer(collection.GetType());
                if (Equals(defaultComparer, comparer))
                    return true;

                if (defaultComparer is Comparer def && comparer is Comparer c)
                    return Equals(def.CompareInfo(), c.CompareInfo());

                return false;
            }

            private object CreateCollection(DataTypeDescriptor descriptor, int capacity, bool isCaseInsensitive, object comparer)
            {
                CreateInstanceAccessor ctor = GetInitializer(descriptor);
                if (CtorArguments == null)
                    return ctor.CreateInstance();

                object[] parameters = new object[CtorArguments.Length];
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
                            throw new InvalidOperationException($"Unsupported {nameof(CollectionCtorArguments)}");
                    }
                }

                return ctor.CreateInstance(parameters);
            }

            #endregion

            #endregion
        }
    }
}
