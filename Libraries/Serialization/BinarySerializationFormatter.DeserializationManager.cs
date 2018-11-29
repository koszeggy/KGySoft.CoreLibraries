using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        /// A manager class that provides that stored types will be built up in the same order both at serialization and deserialization for complex types.
        /// </summary>
        sealed class DeserializationManager: SerializationManagerBase
        {
            #region Fields

            private List<Assembly> readAssemblies;
            private List<Type> readTypes;
            private Dictionary<string, Assembly> assemblyByNameCache;
            private Dictionary<string, Type> typeByNameCache;
            private Dictionary<int, object> idCache;
            private Dictionary<object, List<KeyValuePair<FieldInfo, object>>> objectReferences;

            #endregion

            #region Constructor

            internal DeserializationManager(StreamingContext context, BinarySerializationOptions options, SerializationBinder binder, ISurrogateSelector surrogateSelector)
                : base(context, options, binder, surrogateSelector)
            {
            }

            #endregion

            #region Properties

            private Dictionary<int, object> IdCache
            {
                get { return idCache ?? (idCache = new Dictionary<int, object> {{0, null}, {1, DBNull.Value}}); }
            }

            #endregion

            #region Methods

            #region Internal Methods

            /// <summary>
            /// Reads a type from the serialization stream
            /// </summary>
            internal Type ReadType(BinaryReader br)
            {
                // assembly index
                int index = Read7BitInt(br);
                if (readAssemblies == null)
                    readAssemblies = new List<Assembly>(KnownAssemblies);

                // natively supported type
                if (index == readAssemblies.Count + 2)
                {
                    DataTypes dataType = (DataTypes)br.ReadUInt16();
                    DataTypeDescriptor desc = new DataTypeDescriptor(null, dataType, br);
                    desc.DecodeType(br, this);
                    return desc.Type;
                }

                if (readTypes == null)
                {
                    readTypes = new List<Type>(Math.Max(4, KnownTypes.Length));
                    readTypes.AddRange(KnownTypes);
                }

                // new assembly: assembly and type are stored together
                if (index == readAssemblies.Count + 1)
                {
                    // assembly qualified name (GetType uses binder if set)
                    Type type = GetType(br.ReadString(), br.ReadString());
                    readAssemblies.Add(type.Assembly);
                    readTypes.Add(type);
                    if (type.IsGenericTypeDefinition)
                    {
                        type = ReadGenericType(br, type);
                    }

                    return type;
                }

                Assembly assembly = null;

                // type with assembly (unless assembly is omitted)
                if (index != readAssemblies.Count)
                {
                    Debug.Assert(index >= 0 && index < readAssemblies.Count, "Invalid assembly index");
                    assembly = readAssemblies[index];
                }

                // type index
                index = Read7BitInt(br);

                // reading type
                if (index == readTypes.Count + 1)
                {
                    string typeName = br.ReadString();
                    Type type = null;
                    // ReSharper disable AssignNullToNotNullAttribute
                    if (Binder != null)
                        type = Binder.BindToType(assembly == null ? String.Empty : assembly.FullName, typeName);
                    // ReSharper restore AssignNullToNotNullAttribute
                    if (type == null)
                        type = assembly == null
                            ? Reflector.ResolveType(typeName)
                            : Reflector.ResolveType(assembly, typeName);
                    if (type == null)
                        throw new SerializationException(Res.Get(Res.CannotResolveType, typeName));
                    readTypes.Add(type);
                    if (type.IsGenericTypeDefinition)
                    {
                        type = ReadGenericType(br, type);
                    }

                    return type;
                }

                Debug.Assert(index >= 0 && index < readTypes.Count, "Invalid type index");
                Type result = readTypes[index];
                if (result.IsGenericTypeDefinition)
                {
                    result = ReadGenericType(br, result);
                }

                // ReSharper disable AssignNullToNotNullAttribute
                return Binder != null
                    ? (Binder.BindToType(assembly == null ? String.Empty : assembly.FullName, result.FullName) ?? result)
                    : result;
                // ReSharper restore AssignNullToNotNullAttribute
            }

            internal bool TryGetCachedObject(BinaryReader br, out object result)
            {
                Dictionary<int, object> cache = IdCache;
                int id = Read7BitInt(br);
                if (cache.TryGetValue(id, out result))
                    return true;

                if (id > cache.Count)
                    throw new SerializationException(Res.Get(Res.DeserializeUnexpectedId));
                return false;
            }

            internal void AddObjectToCache(object obj)
            {
                Dictionary<int, object> cache = IdCache;
                cache.Add(idCache.Count, obj);
            }

            internal void AddObjectToCache(object obj, out int id)
            {
                Dictionary<int, object> cache = IdCache;
                id = idCache.Count;
                cache.Add(id, obj);
            }

            internal void ReplaceObjectInCache(int id, object obj)
            {
                Dictionary<int, object> cache = IdCache;
                cache[id] = obj;
            }

            internal void TrySetField(FieldInfo field, object obj, object value)
            {
                IObjectReference objRef;
                if ((Options & BinarySerializationOptions.IgnoreIObjectReference) == BinarySerializationOptions.None
                    && (objRef = value as IObjectReference) != null)
                {
                    // the object reference cannot be set yet so storing the new usage of the reference to be set later.
                    if (objectReferences == null)
                        objectReferences = new Dictionary<object, List<KeyValuePair<FieldInfo, object>>>(1, ReferenceEqualityComparer.Comparer);

                    List<KeyValuePair<FieldInfo, object>> refUsages;
                    if (!objectReferences.TryGetValue(objRef, out refUsages))
                    {
                        refUsages = new List<KeyValuePair<FieldInfo, object>>();
                        objectReferences.Add(objRef, refUsages);
                    }

                    refUsages.Add(new KeyValuePair<FieldInfo, object>(field, obj));
                    return;
                }

                FieldAccessor.GetFieldAccessor(field).Set(obj, value);
            }

            internal void CheckReferences(SerializationInfo si)
            {
                if (objectReferences == null)
                    return;

                // circular IObjectReferences can be resolved after all, except if custom deserialization is used for unresolved references
                foreach (SerializationEntry entry in si)
                {
                    IObjectReference objRef = entry.Value as IObjectReference;
                    if (objRef != null && objectReferences.ContainsKey(objRef))
                        throw new SerializationException(Res.Get(Res.CircularIObjectReference));
                }
            }

            internal void UpdateReferences(IObjectReference objRef, object realObject)
            {
                List<KeyValuePair<FieldInfo, object>> refUsages;
                if (objectReferences == null || !objectReferences.TryGetValue(objRef, out refUsages))
                    return;

                foreach (KeyValuePair<FieldInfo, object> usage in refUsages)
                {
                    FieldAccessor.GetFieldAccessor(usage.Key).Set(usage.Value, realObject);
                }

                objectReferences.Remove(objRef);
            }

            #endregion

            #region Private Methods

            /// <summary>
            /// Resolves a type by string
            /// </summary>
            private Type GetType(string assemblyName, string typeName)
            {
                Type result;
                string key = assemblyName + ":" + typeName;

                if (typeByNameCache != null && typeByNameCache.TryGetValue(key, out result))
                {
                    return result;
                }

                if (Binder != null)
                {
                    result = Binder.BindToType(assemblyName, typeName);
                    if (result != null)
                    {
                        AddTypeToCache(key, result);
                        return result;
                    }
                }

                Assembly assembly = GetAssembly(assemblyName);
                result = Reflector.ResolveType(assembly, typeName);
                if (result == null)
                {
                    throw new SerializationException(Res.Get(Res.CannotResolveTypeInAssembly, typeName, assemblyName));
                }

                AddTypeToCache(key, result);
                return result;
            }

            private void AddTypeToCache(string key, Type result)
            {
                if (typeByNameCache == null)
                {
                    typeByNameCache = new Dictionary<string, Type>();
                }

                typeByNameCache.Add(key, result);
            }

            private Type ReadGenericType(BinaryReader br, Type genTypeDef)
            {
                int len = genTypeDef.GetGenericArguments().Length;
                Type[] args = new Type[len];
                for (int i = 0; i < len; i++)
                {
                    args[i] = ReadType(br);
                }

                Type result = genTypeDef.MakeGenericType(args);
                readTypes.Add(result);
                return Binder != null ? (Binder.BindToType(result.Assembly.FullName, result.FullName) ?? result) : result;
            }

            /// <summary>
            /// Resolves an assembly by string
            /// </summary>
            private Assembly GetAssembly(string name)
            {
                Assembly result;
                if (assemblyByNameCache != null && assemblyByNameCache.TryGetValue(name, out result))
                {
                    return result;
                }

                // 1.) Iterating through loaded assemblies
                result = Reflector.GetLoadedAssemblies().FirstOrDefault(asm => asm.FullName == name);

                // 2.) Trying to load assembly
                if (result == null)
                {
                    try
                    {
                        result = Assembly.Load(new AssemblyName(name));
                    }
                    catch
                    {
                        try
                        {
                            result = Assembly.Load(name);
                        }
                        catch (Exception e)
                        {
                            throw new SerializationException(Res.Get(Res.CannotLoadAssembly, name), e);
                        }
                    }
                }

                if (result == null)
                {
                    throw new SerializationException(Res.Get(Res.CannotLoadAssembly, name));
                }

                if (assemblyByNameCache == null)
                {
                    assemblyByNameCache = new Dictionary<string, Assembly>(1);
                }

                assemblyByNameCache.Add(name, result);

                return result;
            }

            #endregion

            #endregion
        }
    }
}
