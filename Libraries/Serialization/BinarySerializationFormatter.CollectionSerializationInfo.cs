using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

namespace KGySoft.Serialization
{
    public sealed partial class BinarySerializationFormatter
    {
        /// <summary>
        /// Static descriptor for collection types. Instance-specific descriptor is in <see cref="DataTypeDescriptor"/>.
        /// </summary>
        sealed class CollectionSerializationInfo
        {
            #region Fields

            internal readonly static CollectionSerializationInfo Default = new CollectionSerializationInfo();

            #endregion

            #region Properties

            internal CollectionInfo Info { private get; set; }
            /// <summary>
            /// Should be specified only when target collection is not <see cref="IList"/> or <see cref="ICollection{T}"/> implementation,
            /// or when defining it results faster access that resolving the generic Add method for each access
            /// </summary>
            internal string SpecificAddMethod { get; set; }

            internal string ComparerFieldName { private get; set; }

            internal bool ReverseElements { get { return (Info & CollectionInfo.ReverseElements) == CollectionInfo.ReverseElements; } }
            internal bool IsNonGenericCollection { get { return !IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.None; } }
            internal bool IsNonGenericDictionary { get { return !IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary; } }
            internal bool IsGenericCollection { get { return IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.None; } }
            internal bool IsGenericDictionary { get { return IsGeneric && (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary; } }
            internal bool IsDictionary { get { return (Info & CollectionInfo.IsDictionary) == CollectionInfo.IsDictionary; } }
            internal bool IsSingleElement { get { return (Info & CollectionInfo.IsSingleElement) == CollectionInfo.IsSingleElement; } }
            private bool IsGeneric { get { return (Info & CollectionInfo.IsGeneric) == CollectionInfo.IsGeneric; } }
            private bool HasCapacity { get { return (Info & CollectionInfo.HasCapacity) == CollectionInfo.HasCapacity; } }
            private bool HasEqualityComparer { get { return (Info & CollectionInfo.HasEqualityComparer) == CollectionInfo.HasEqualityComparer; } }
            private bool HasAnyComparer { get { return HasEqualityComparer || (Info & CollectionInfo.HasComparer) == CollectionInfo.HasComparer; } }
            private bool HasCaseInsensitivity { get { return (Info & CollectionInfo.HasCaseInsensitivity) == CollectionInfo.HasCaseInsensitivity; } }
            private bool HasReadOnly { get { return (Info & CollectionInfo.HasReadOnly) == CollectionInfo.HasReadOnly; } }
            private bool DefaultEnumComparer { get { return (Info & CollectionInfo.DefaultEnumComparer) == CollectionInfo.DefaultEnumComparer; } }

            #endregion

            #region Methods

            /// <summary>
            /// Writes specific properties of a collection that are needed for deserialization
            /// </summary>
            internal void WriteSpecificProperties(BinarySerializationFormatter owner, BinaryWriter bw, IEnumerable collection, SerializationManager manager)
            {
                if (IsSingleElement)
                    return;

                // 1.) Count
                ICollection c = collection as ICollection;
                if (c != null)
                    Write7BitInt(bw, c.Count);
                else
                    Write7BitInt(bw, (int)Reflector.GetProperty(collection, "Count"));

                // 2.) Capacity
                if (HasCapacity)
                    Write7BitInt(bw, (int)Reflector.GetProperty(collection, "Capacity"));

                // 3.) Case sensitivity
                if (HasCaseInsensitivity)
                    bw.Write((bool)Reflector.GetField(collection, "caseInsensitive"));

                // 4.) ReadOnly
                if (HasReadOnly)
                {
                    IList list = collection as IList;
                    if (list != null)
                        bw.Write(list.IsReadOnly);
                    else
                    {
                        IDictionary dictionary = collection as IDictionary;
                        if (dictionary != null)
                            bw.Write(dictionary.IsReadOnly);
                        else
                            // should never occur for suppoerted collections, throwing internal error without resource
                            throw new SerializationException("Could not write IsReadOnly state of collection " + collection.GetType());
                    }
                }

                // 5.) Comparer
                if (HasAnyComparer)
                {
                    object comparer = GetComparer(collection);
                    object referenceComparer = GetDefaultComparer(collection.GetType(), DefaultEnumComparer);

                    // is default comparer
                    bool isDefaultComparer = AreComparersEquals(referenceComparer, comparer);
                    bw.Write(isDefaultComparer);

                    if (!isDefaultComparer)
                        owner.Write(bw, comparer, false, manager);
                }
            }

            /// <summary>
            /// Creates collection and reads all serialized specific properties that were written by <see cref="WriteSpecificProperties"/>.
            /// </summary>
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

                // Creating collection based on the infos above (comparer is added later because of id caching)
                List<Type> ctorParamTypes = new List<Type> { typeof(int) }; // a collection can have capacity ctor parameter even is does not have Capacity property
                List<object> ctorParams = new List<object> { capacity };
                //if (comparer != null)
                //{
                //    if (IsGeneric)
                //        if (HasEqualityComparer)
                //            ctorParamTypes.Add(typeof(IEqualityComparer<>).MakeGenericType(new Type[] { descriptor.ElementType }));
                //        else
                //            ctorParamTypes.Add(typeof(IComparer<>).MakeGenericType(new Type[] { descriptor.ElementType }));
                //    else
                //        if (HasEqualityComparer)
                //            ctorParamTypes.Add(typeof(IEqualityComparer));
                //        else
                //            ctorParamTypes.Add(typeof(IComparer));
                //    ctorParams.Add(comparer);
                //}
                if (HasCaseInsensitivity)
                {
                    ctorParamTypes.Add(typeof(bool));
                    ctorParams.Add(caseInsensitive);
                }

                // try 1: capacity and case insesitivity if specified
                Type collectionType = descriptor.GetTypeToCreate();
                ConstructorInfo ctor = collectionType.GetConstructor(ctorParamTypes.ToArray());

                // try 2: trying without capacity
                if (ctor == null)
                {
                    ctorParamTypes.RemoveAt(0);
                    ctor = collectionType.GetConstructor(ctorParamTypes.ToArray());
                    if (ctor != null)
                        ctorParams.RemoveAt(0);
                }

                // should never occur for supported collections, throwing internal error without resource
                if (ctor == null)
                    throw new SerializationException(String.Format("Could not create type {0} because no appropriate constructor found", collectionType));

                object result = Reflector.CreateInstance(ctor, ctorParams.ToArray());
                if (addToCache)
                    manager.AddObjectToCache(result);

                // 5.) Comparer
                if (HasAnyComparer && !br.ReadBoolean())
                {
                    object comparer = owner.Read(br, false, manager);
                    SetComparer(result, comparer);
                }

                return result;
            }

            private object GetDefaultComparer(Type type, bool defaultEnumComparer)
            {
                // non-generic
                if (!IsGeneric)
                {
                    // non-generic equality default: null
                    return HasEqualityComparer ? null : Comparer.Default;
                }

                // enum comparer
                Type elementType = type.GetGenericArguments()[0];
                if (defaultEnumComparer && elementType.IsEnum)
                {
                    Type comparerType = typeof(EnumComparer<>).MakeGenericType(new Type[] { elementType });
                    return Reflector.GetProperty(comparerType, "Comparer");
                }

                // generic equality comparer
                if (HasEqualityComparer)
                {
                    Type comparerType = typeof(EqualityComparer<>).MakeGenericType(new Type[] { elementType });
                    return Reflector.GetProperty(comparerType, "Default");
                }
                // generic relation comparer
                else
                {
                    Type comparerType = typeof(Comparer<>).MakeGenericType(new Type[] { elementType });
                    return Reflector.GetProperty(comparerType, "Default");
                }
            }

            private static bool AreComparersEquals(object referenceComparer, object comparer)
            {
                if (Equals(referenceComparer, comparer))
                    return true;

                if (referenceComparer is Comparer && comparer is Comparer)
                    return Equals(Reflector.GetField(referenceComparer, "m_compareInfo"), Reflector.GetField(comparer, "m_compareInfo"));

                return false;
            }

            private object GetComparer(object collection)
            {
                if (!ComparerFieldName.Contains("."))
                    return Reflector.GetField(collection, ComparerFieldName);
                return ComparerFieldName.Split('.').Aggregate(collection, Reflector.GetField);
            }

            private void SetComparer(object collection, object comparer)
            {
                if (!ComparerFieldName.Contains("."))
                {
                    Reflector.SetField(collection, ComparerFieldName, comparer);
                    return;
                }

                string[] chain = ComparerFieldName.Split('.');
                object obj = collection;
                for (int i = 0; i < chain.Length - 1; i++)
                {
                    obj = Reflector.GetField(obj, chain[i]);
                }

                Reflector.SetField(obj, chain[chain.Length - 1], comparer);
            }

            #endregion
        }
    }
}
