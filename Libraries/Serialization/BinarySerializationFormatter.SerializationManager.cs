using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using KGySoft.Libraries;
using KGySoft.Reflection;

namespace KGySoft.Serialization
{
    public sealed partial class BinarySerializationFormatter
    {
        /// <summary>
        /// A manager class that provides that stored types will be built up in the same order both at serialization and deserialization for complex types.
        /// </summary>
        sealed class SerializationManager: SerializationManagerBase
        {
            #region Fields

            private Dictionary<Assembly, int> assemblyIndexCache;
            private Dictionary<Type, int> typeIndexCache;
#if NET40 || NET45
            private Dictionary<string, int> assemblyNameIndexCache;
            private Dictionary<string, int> typeNameIndexCache;
#elif !NET35
#error .NET version is not set or not supported!
#endif
            private int idCounter = 1; // 0/1 are reserved for null/dbnull
            private Dictionary<object, int> idCacheByValue;
            private Dictionary<object, int> idCacheByRef;

            #endregion

            #region Constructor

            internal SerializationManager(StreamingContext context, BinarySerializationOptions options, SerializationBinder binder, ISurrogateSelector surrogateSelector) :
                base(context, options, binder, surrogateSelector)
            {
            }

            #endregion

            #region Properties

            private int AssemblyIndexCacheCount
            {
                get
                {
                    return (assemblyIndexCache == null ? KnownAssemblies.Length : assemblyIndexCache.Count)
#if NET40 || NET45
 + (assemblyNameIndexCache == null ? 0 : assemblyNameIndexCache.Count)
#elif !NET35
#error .NET version is not set or not supported!
#endif
;
                }
            }

            private int TypeIndexCacheCount
            {
                get
                {
                    return (typeIndexCache == null ? KnownTypes.Length : typeIndexCache.Count)
#if NET40 || NET45
 + (typeNameIndexCache == null ? 0 : typeNameIndexCache.Count)
#elif !NET35
#error .NET version is not set or not supported!
#endif
;
                }
            }

            #endregion

            #region Internal Methods

#if NET35
            /// <summary>
            /// Writes a type into the serialization stream
            /// </summary>
            /// <remarks>
            /// Assembly indices:
            /// 0..count - 1: known or already dumped assembly
            /// count: assembly is omitted
            /// count + 1: new assembly (name is to be stored)
            /// count + 2: natively supported type, assembly is not needed (retrieved from DataType)
            /// 
            /// Type indices:
            /// 0..count - 1: known or already dumped type
            /// count + 1: new type (name is to be stored)
            /// </remarks>
            internal void WriteType(BinaryWriter bw, Type type)
            {
                // Checking if type is natively supported. Can occur when writing SerializationInfo types on writing custom object graph
                DataTypes nativelySupportedType = GetSupportedElementType(
                    type,
                    BinarySerializationOptions.IgnoreIBinarySerializable,
                    null);
                if (nativelySupportedType != DataTypes.Null
                    && nativelySupportedType != DataTypes.RecursiveObjectGraph
                    && (nativelySupportedType & DataTypes.Enum) == DataTypes.Null)
                {
                    // count + 2: natively supported type
                    Write7BitInt(bw, AssemblyIndexCacheCount + 2);
                    bw.Write((ushort)nativelySupportedType);
                    return;
                }

                if (IsSupportedCollection(type))
                {
                    IEnumerable<DataTypes> collectionType = EncodeCollectionType(
                        type,
                        BinarySerializationOptions.IgnoreIBinarySerializable,
                        this);
                    if (collectionType != null)
                    {
                        // count + 2: natively supported type
                        Write7BitInt(bw, AssemblyIndexCacheCount + 2);
                        collectionType.ForEach(dt => bw.Write((ushort)dt));
                        WriteTypeNamesAndRanks(bw, type, BinarySerializationOptions.IgnoreIBinarySerializable, this);
                        return;
                    }
                }
                int index;
                bool isGeneric;
                Type typeDef;

                // initializing asm cache if needed, determining asm index
                if (assemblyIndexCache == null)
                {
                    assemblyIndexCache = new Dictionary<Assembly, int>(KnownAssemblies.Length + 1);
                    KnownAssemblies.ForEach(a => assemblyIndexCache.Add(a, assemblyIndexCache.Count));
                }

                if (!assemblyIndexCache.TryGetValue(type.Assembly, out index))
                    index = -1;

                // initializing type cache if needed
                if (typeIndexCache == null)
                {
                    typeIndexCache = new Dictionary<Type, int>(Math.Max(4, KnownTypes.Length + 1));
                    KnownTypes.ForEach(t => typeIndexCache.Add(t, typeIndexCache.Count));
                }

                // new assembly
                if (index == -1)
                {
                    // storing assembly and type name together and return
                    if ((Options & BinarySerializationOptions.OmitAssemblyQualifiedNames)
                        == BinarySerializationOptions.None)
                    {
                        int indexCacheCount = AssemblyIndexCacheCount;

                        // count + 1: new assembly
                        Write7BitInt(bw, indexCacheCount + 1);

                        // asm
                        bw.Write(type.Assembly.FullName);
                        assemblyIndexCache.Add(type.Assembly, indexCacheCount);

                        indexCacheCount = TypeIndexCacheCount;

                        // type: type is unknown here for sure so encoding without looking in cache
                        isGeneric = type.IsGenericType;
                        typeDef = isGeneric ? type.GetGenericTypeDefinition() : null;

                        // ReSharper disable once PossibleNullReferenceException
                        bw.Write(isGeneric ? typeDef.FullName : type.FullName);
                        // ReSharper restore once PossibleNullReferenceException
                        if (isGeneric)
                        {
                            this.typeIndexCache.Add(typeDef, indexCacheCount);
                            this.WriteGenericType(bw, type);
                        }

                        // when generic, the constructed type is added again (property value must be re-evaluated)
                        this.typeIndexCache.Add(type, this.TypeIndexCacheCount);
                        return;
                    }
                        // omitting assembly
                    else
                    {
                        // count: omitting assembly
                        Write7BitInt(bw, AssemblyIndexCacheCount);
                    }
                }
                    // known assembly
                else
                {
                    Write7BitInt(bw, index);
                }

                // known type
                if (typeIndexCache.TryGetValue(type, out index))
                {
                    Write7BitInt(bw, index);
                    return;
                }

                int typeIndexCacheCount = TypeIndexCacheCount;

                // generic type definition is already known but the constructed type is not yet
                isGeneric = type.IsGenericType;
                typeDef = isGeneric ? type.GetGenericTypeDefinition() : null;

                // ReSharper disable AssignNullToNotNullAttribute
                if (isGeneric && typeIndexCache.TryGetValue(typeDef, out index))
                {
                    Write7BitInt(bw, index);
                    WriteGenericType(bw, type);

                    // caching the constructed type (property value must be re-evaluated)
                    typeIndexCache.Add(type, TypeIndexCacheCount);
                    return;
                }
                // ReSharper restore AssignNullToNotNullAttribute

                // type is not known at all (count + 1: new type)
                Write7BitInt(bw, typeIndexCacheCount + 1);

                // ReSharper disable PossibleNullReferenceException
                bw.Write(isGeneric ? typeDef.FullName : type.FullName);
                // ReSharper restore PossibleNullReferenceException
                if (isGeneric)
                {
                    typeIndexCache.Add(typeDef, typeIndexCacheCount);
                    WriteGenericType(bw, type);
                }

                // when generic, the constructed type is added again (property value must be re-evaluated)
                typeIndexCache.Add(type, TypeIndexCacheCount);
            }

#elif NET40 || NET45
            /// <summary>
            /// Writes a type into the serialization stream
            /// </summary>
            /// <remarks>
            /// Assembly indices:
            /// 0..count - 1: known or already dumped assembly
            /// count: assembly is omitted
            /// count + 1: new assembly (name is to be stored)
            /// count + 2: natively supported type, assembly is not needed (retrieved from DataType)
            /// 
            /// Type indices:
            /// 0..count - 1: known or already dumped type
            /// count + 1: new type (name is to be stored)
            /// </remarks>
            internal void WriteType(BinaryWriter bw, Type type)
            {
                string binderAsmName = null;
                string binderTypeName = null;
                if (Binder != null)
                    Binder.BindToName(type, out binderAsmName, out binderTypeName);

                // Checking if type is natively supported. Can occur when writing SerializationInfo types on writing custom object graph
                DataTypes nativelySupportedType;
                if (binderAsmName == null && binderTypeName == null
                    &&
                    (nativelySupportedType =
                        GetSupportedElementType(type, BinarySerializationOptions.IgnoreIBinarySerializable, null)) !=
                    DataTypes.Null
                    && nativelySupportedType != DataTypes.RecursiveObjectGraph &&
                    (nativelySupportedType & DataTypes.Enum) == DataTypes.Null)
                {
                    // count + 2: natively supported type
                    Write7BitInt(bw, AssemblyIndexCacheCount + 2);
                    bw.Write((ushort)nativelySupportedType);
                    return;
                }

                if (binderAsmName == null && binderTypeName == null && IsSupportedCollection(type))
                {
                    IEnumerable<DataTypes> collectionType = EncodeCollectionType(type,
                        BinarySerializationOptions.IgnoreIBinarySerializable, this);
                    if (collectionType != null)
                    {
                        // count + 2: natively supported type
                        Write7BitInt(bw, AssemblyIndexCacheCount + 2);
                        collectionType.ForEach(dt => bw.Write((ushort)dt));
                        WriteTypeNamesAndRanks(bw, type, BinarySerializationOptions.IgnoreIBinarySerializable, this);
                        return;
                    }
                }

                int index;
                bool isGeneric;
                Type typeDef;

                // initializing asm caches if needed, determining asm index
                if (binderAsmName != null)
                {
                    // assembly by binder
                    if (assemblyNameIndexCache == null)
                        assemblyNameIndexCache = new Dictionary<string, int>(1);
                    if (!assemblyNameIndexCache.TryGetValue(binderAsmName, out index))
                        index = -1;
                }
                else
                {
                    // assembly by type
                    if (assemblyIndexCache == null)
                    {
                        assemblyIndexCache = new Dictionary<Assembly, int>(KnownAssemblies.Length + 1);
                        KnownAssemblies.ForEach(a => assemblyIndexCache.Add(a, assemblyIndexCache.Count));
                    }

                    if (!assemblyIndexCache.TryGetValue(type.Assembly, out index))
                        index = -1;
                }

                // initializing type caches if needed
                if (binderTypeName != null)
                {
                    if (typeNameIndexCache == null)
                        typeNameIndexCache = new Dictionary<string, int>(1);
                }
                else if (typeIndexCache == null)
                {
                    typeIndexCache = new Dictionary<Type, int>(Math.Max(4, KnownTypes.Length + 1));
                    KnownTypes.ForEach(t => typeIndexCache.Add(t, typeIndexCache.Count));
                }

                // new assembly
                if (index == -1)
                {
                    // storing assembly and type name together and return
                    if ((Options & BinarySerializationOptions.OmitAssemblyQualifiedNames) ==
                        BinarySerializationOptions.None)
                    {
                        int indexCacheCount = AssemblyIndexCacheCount;

                        // count + 1: new assembly
                        Write7BitInt(bw, indexCacheCount + 1);

                        // asm by binder
                        if (binderAsmName != null)
                        {
                            bw.Write(binderAsmName);
                            assemblyNameIndexCache.Add(binderAsmName, indexCacheCount);
                        }
                        // asm by itself
                        else
                        {
                            bw.Write(type.Assembly.FullName);
                            assemblyIndexCache.Add(type.Assembly, indexCacheCount);
                        }

                        indexCacheCount = TypeIndexCacheCount;

                        // type by binder: handling conflicts that can be caused by binder, generics are not handled individually
                        if (binderTypeName != null)
                        {
                            bw.Write(binderTypeName);

                            // binder can produce the same type name for different assemblies so prefixing with assembly
                            typeNameIndexCache.Add((binderAsmName ?? type.Assembly.FullName) + ":" + binderTypeName,
                                indexCacheCount);
                            return;
                        }

                        // type by itself: type is unknown here for sure so encoding without looking in cache
                        isGeneric = type.IsGenericType;
                        typeDef = isGeneric ? type.GetGenericTypeDefinition() : null;

                        bw.Write(isGeneric ? typeDef.FullName : type.FullName);
                        if (isGeneric)
                        {
                            typeIndexCache.Add(typeDef, indexCacheCount);
                            WriteGenericType(bw, type);
                        }

                        // when generic, the constructed type is added again (property value must be re-evaluated)
                        typeIndexCache.Add(type, TypeIndexCacheCount);
                        return;
                    }
                    // omitting assembly
                    // count: omitting assembly
                    Write7BitInt(bw, AssemblyIndexCacheCount);
                }
                // known assembly
                else
                {
                    Write7BitInt(bw, index);
                }

                // known type
                string key = null;
                if (binderTypeName != null)
                {
                    key = (binderAsmName ?? type.Assembly.FullName) + ":" + binderTypeName;
                    if (typeNameIndexCache.TryGetValue(key, out index))
                    {
                        Write7BitInt(bw, index);
                        return;
                    }
                }
                else if (typeIndexCache.TryGetValue(type, out index))
                {
                    Write7BitInt(bw, index);
                    return;
                }

                int typeIndexCacheCount = TypeIndexCacheCount;

                // new type by binder (generics are not handled in a special way)
                if (binderTypeName != null)
                {
                    // type is not known yet (count + 1: new type)
                    Write7BitInt(bw, typeIndexCacheCount + 1);
                    bw.Write(binderTypeName);
                    typeNameIndexCache.Add(key, typeIndexCacheCount);
                    return;
                }

                // generic type definition is already known but the constructed type is not yet
                isGeneric = type.IsGenericType;
                typeDef = isGeneric ? type.GetGenericTypeDefinition() : null;

                if (isGeneric && typeIndexCache.TryGetValue(typeDef, out index))
                {
                    Write7BitInt(bw, index);
                    WriteGenericType(bw, type);

                    // caching the constructed type (property value must be re-evaluated)
                    typeIndexCache.Add(type, TypeIndexCacheCount);
                    return;
                }

                // type is not known at all (count + 1: new type)
                Write7BitInt(bw, typeIndexCacheCount + 1);

                bw.Write(isGeneric ? typeDef.FullName : type.FullName);
                if (isGeneric)
                {
                    typeIndexCache.Add(typeDef, typeIndexCacheCount);
                    WriteGenericType(bw, type);
                }

                // when generic, the constructed type is added again (property value must be re-evaluated)
                typeIndexCache.Add(type, TypeIndexCacheCount);
            }
#else
#error .NET version is not set or not supported!
#endif

            /// <summary>
            /// Writes an ID and returns if it was already known.
            /// </summary>
            internal bool WriteId(BinaryWriter bw, object data)
            {
                // null is always known.
                if (data == null)
                {
                    // actally 7-bit encoded 0
                    bw.Write((byte)0);
                    return true;
                }

                // DBNull has no equals so checking just by type.
                if (data is DBNull)
                {
                    // actally 7-bit encoded 1
                    bw.Write((byte)1);
                    return true;
                }

                // some dedicated immutable type are compared by value
                if (IsComparedByValue(data.GetType()))
                {
                    if (idCacheByValue == null)
                        idCacheByValue = new Dictionary<object, int>();
                    else
                    {
                        int id;
                        if (idCacheByValue.TryGetValue(data, out id))
                        {
                            Write7BitInt(bw, id);
                            return true;
                        }
                    }

                    idCacheByValue.Add(data, ++idCounter);
                    Write7BitInt(bw, idCounter);
                    return false;
                }

                // Others are compared by reference. Structs as well, which are boxed into a reference here.
                if (idCacheByRef == null)
                    idCacheByRef = new Dictionary<object, int>(ReferenceEqualityComparer.Comparer);
                else
                {
                    int id;
                    if (idCacheByRef.TryGetValue(data, out id))
                    {
                        Write7BitInt(bw, id);
                        return true;
                    }
                }

                idCacheByRef.Add(data, ++idCounter);
                Write7BitInt(bw, idCounter);
                return false;
            }

            #endregion

            #region Private Methods

            private bool IsComparedByValue(Type type)
            {
                // TODO: by hashset (lazy init?), maybe this method ban be deleted
                return type.IsPrimitive || type.BaseType == Reflector.EnumType || // always instance so can be used than the slower IsEnum
                       type.In(typeof(string), typeof(UIntPtr), typeof(decimal), typeof(DateTime), typeof(TimeSpan),
                           typeof(DateTimeOffset), typeof(Guid));
            }

            /// <summary>
            /// Writes the generic parameters of a type.
            /// </summary>
            private void WriteGenericType(BinaryWriter bw, Type type)
            {
                foreach (Type genericArgument in type.GetGenericArguments())
                {
                    WriteType(bw, genericArgument);
                }
            }

            #endregion
        }
    }
}
