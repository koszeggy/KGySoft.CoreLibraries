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
using System.IO;
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
        /// <summary>
        /// A manager class that provides that stored types will be built up in the same order both at serialization and deserialization for complex types.
        /// </summary>
        sealed class SerializationManager : SerializationManagerBase
        {
            #region Nested Types

#if !NET35
            private struct WriteTypeContext
            {
                #region Fields

                internal Type Type;

                internal string BinderAsmName;
                internal string BinderTypeName;

                #endregion
            }
#endif

            #endregion

            #region Fields

            private Dictionary<Assembly, int> assemblyIndexCache;
            private Dictionary<Type, int> typeIndexCache;
#if !NET35
            private Dictionary<string, int> assemblyNameIndexCache;
            private Dictionary<string, int> typeNameIndexCache;
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
                    return (assemblyIndexCache?.Count ?? KnownAssemblies.Length)
#if !NET35
                        + (assemblyNameIndexCache?.Count ?? 0)
#endif
                        ;
                }
            }

            private int TypeIndexCacheCount
            {
                get
                {
                    return (typeIndexCache?.Count ?? KnownTypes.Length)
#if !NET35
                        + (typeNameIndexCache?.Count ?? 0)
#endif
                        ;
                }
            }

            #endregion

            #region Methods

            #region Static Methods

            private static bool IsComparedByValue(Type type)
            {
                // TODO: by hashset (lazy init?), maybe this method ban be deleted
                return type.IsPrimitive || type.BaseType == Reflector.EnumType || // always instance so can be used than the slower IsEnum
                    type.In(Reflector.StringType, Reflector.UIntPtrType, Reflector.DecimalType, Reflector.DateTimeType, Reflector.TimeSpanType,
                        Reflector.DateTimeOffsetType, typeof(Guid));
            }

            #endregion

            #region Instance Methods

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
                if (TryWriteSupportedType(bw, type))
                    return;

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
                    Write7BitInt(bw, index);

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

                // ReSharper disable once AssignNullToNotNullAttribute
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
            [SecurityCritical]
            internal void WriteType(BinaryWriter bw, Type type)
            {
                #region Private Methods to reduce complexity

                int GetAssemblyIndex(ref WriteTypeContext ctx)
                {
                    int index;

                    // initializing asm caches if needed, determining asm index
                    if (ctx.BinderAsmName != null)
                    {
                        // assembly by binder
                        if (assemblyNameIndexCache == null)
                            assemblyNameIndexCache = new Dictionary<string, int>(1);
                        if (!assemblyNameIndexCache.TryGetValue(ctx.BinderAsmName, out index))
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

                        if (!assemblyIndexCache.TryGetValue(ctx.Type.Assembly, out index))
                            index = -1;
                    }

                    // initializing type caches if needed
                    if (ctx.BinderTypeName != null)
                    {
                        if (typeNameIndexCache == null)
                            typeNameIndexCache = new Dictionary<string, int>(1);
                    }
                    else if (typeIndexCache == null)
                    {
                        typeIndexCache = new Dictionary<Type, int>(Math.Max(4, KnownTypes.Length + 1));
                        KnownTypes.ForEach(t => typeIndexCache.Add(t, typeIndexCache.Count));
                    }

                    return index;
                }

                #endregion

                var context = new WriteTypeContext { Type = type };
                Binder?.BindToName(type, out context.BinderAsmName, out context.BinderTypeName);

                if (context.BinderTypeName == null && context.BinderAsmName == null && TryWriteSupportedType(bw, type))
                    return;

                int asmIndex = GetAssemblyIndex(ref context);

                bool isGeneric;
                Type typeDef;

                // new assembly
                if (asmIndex == -1)
                {
                    // storing assembly and type name together and return
                    if ((Options & BinarySerializationOptions.OmitAssemblyQualifiedNames) == BinarySerializationOptions.None)
                    {
                        int indexCacheCount = AssemblyIndexCacheCount;

                        // count + 1: new assembly
                        Write7BitInt(bw, indexCacheCount + 1);

                        // asm by binder
                        if (context.BinderAsmName != null)
                        {
                            bw.Write(context.BinderAsmName);
                            assemblyNameIndexCache.Add(context.BinderAsmName, indexCacheCount);
                        }
                        // asm by itself
                        else
                        {
                            bw.Write(type.Assembly.FullName);
                            assemblyIndexCache.Add(type.Assembly, indexCacheCount);
                        }

                        indexCacheCount = TypeIndexCacheCount;

                        // type by binder: handling conflicts that can be caused by binder, generics are not handled individually
                        if (context.BinderTypeName != null)
                        {
                            bw.Write(context.BinderTypeName);

                            // binder can produce the same type name for different assemblies so prefixing with assembly
                            typeNameIndexCache.Add((context.BinderAsmName ?? type.Assembly.FullName) + ":" + context.BinderTypeName,
                                indexCacheCount);
                            return;
                        }

                        // type by itself: type is unknown here for sure so encoding without looking in cache
                        isGeneric = type.IsGenericType;
                        typeDef = isGeneric ? type.GetGenericTypeDefinition() : null;

                        // ReSharper disable once AssignNullToNotNullAttribute - see the check above
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
                    Write7BitInt(bw, asmIndex);

                // known type
                int typeIndex;
                string key = null;
                if (context.BinderTypeName != null)
                {
                    key = (context.BinderAsmName ?? type.Assembly.FullName) + ":" + context.BinderTypeName;
                    if (typeNameIndexCache.TryGetValue(key, out typeIndex))
                    {
                        Write7BitInt(bw, typeIndex);
                        return;
                    }
                }
                else if (typeIndexCache.TryGetValue(type, out typeIndex))
                {
                    Write7BitInt(bw, typeIndex);
                    return;
                }

                int typeIndexCacheCount = TypeIndexCacheCount;

                // new type by binder (generics are not handled in a special way)
                if (context.BinderTypeName != null)
                {
                    // type is not known yet (count + 1: new type)
                    Write7BitInt(bw, typeIndexCacheCount + 1);
                    bw.Write(context.BinderTypeName);
                    typeNameIndexCache.Add(key, typeIndexCacheCount);
                    return;
                }

                // generic type definition is already known but the constructed type is not yet
                isGeneric = type.IsGenericType;
                typeDef = isGeneric ? type.GetGenericTypeDefinition() : null;

                if (isGeneric && typeIndexCache.TryGetValue(typeDef, out typeIndex))
                {
                    Write7BitInt(bw, typeIndex);
                    WriteGenericType(bw, type);

                    // caching the constructed type (property value must be re-evaluated)
                    typeIndexCache.Add(type, TypeIndexCacheCount);
                    return;
                }

                // type is not known at all (count + 1: new type)
                Write7BitInt(bw, typeIndexCacheCount + 1);

                // ReSharper disable once AssignNullToNotNullAttribute
                bw.Write(isGeneric ? typeDef.FullName : type.FullName);
                if (isGeneric)
                {
                    typeIndexCache.Add(typeDef, typeIndexCacheCount);
                    WriteGenericType(bw, type);
                }

                // when generic, the constructed type is added again (property value must be re-evaluated)
                typeIndexCache.Add(type, TypeIndexCacheCount);
            }

#endif

            /// <summary>
            /// Writes an ID and returns if it was already known.
            /// </summary>
            internal bool WriteId(BinaryWriter bw, object data)
            {
                // null is always known.
                if (data == null)
                {
                    // actually 7-bit encoded 0
                    bw.Write((byte)0);
                    return true;
                }

                // DBNull has no equals so checking just by type.
                if (data is DBNull)
                {
                    // actually 7-bit encoded 1
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
                        if (idCacheByValue.TryGetValue(data, out int id))
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
                    if (idCacheByRef.TryGetValue(data, out int id))
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

            [SecurityCritical]
            private bool TryWriteSupportedType(BinaryWriter bw, Type type)
            {
                // Checking if type is natively supported. Can occur when writing SerializationInfo types on writing custom object graph
                DataTypes nativelySupportedType = GetSupportedElementType(type, BinarySerializationOptions.IgnoreIBinarySerializable, null);
                if (nativelySupportedType != DataTypes.Null && nativelySupportedType != DataTypes.RecursiveObjectGraph &&
                    (nativelySupportedType & DataTypes.Enum) == DataTypes.Null)
                {
                    // count + 2: natively supported type
                    Write7BitInt(bw, AssemblyIndexCacheCount + 2);
                    bw.Write((ushort)nativelySupportedType);
                    return true;
                }

                if (IsSupportedCollection(type))
                {
                    IEnumerable<DataTypes> collectionType = EncodeCollectionType(type, BinarySerializationOptions.IgnoreIBinarySerializable, this);
                    if (collectionType != null)
                    {
                        // count + 2: natively supported type
                        Write7BitInt(bw, AssemblyIndexCacheCount + 2);
                        collectionType.ForEach(dt => bw.Write((ushort)dt));
                        WriteTypeNamesAndRanks(bw, type, BinarySerializationOptions.IgnoreIBinarySerializable, this);
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Writes the generic parameters of a type.
            /// </summary>
            [SecurityCritical]
            private void WriteGenericType(BinaryWriter bw, Type type)
            {
                foreach (Type genericArgument in type.GetGenericArguments())
                    WriteType(bw, genericArgument);
            }

            #endregion

            #endregion

            #endregion
        }
    }
}
