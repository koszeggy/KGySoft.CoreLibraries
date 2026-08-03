#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Accessors.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

#region Used Namespaces

using System;
using System.Collections;
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices; 
using System.Runtime.Serialization;
using System.Security; 
#if !NETCOREAPP || NETCOREAPP3_0_OR_GREATER
using System.Text;
#endif
using System.Threading;

using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.CoreLibraries;

#endregion

#region Used Aliases

using CollectionExtensions = KGySoft.CoreLibraries.CollectionExtensions;

#endregion

#endregion

// ReSharper disable InconsistentNaming - Properties are named here: Type_Member. Fields: accessorType_Member
namespace KGySoft.Reflection
{
    /// <summary>
    /// Contains lazy initialized well-known accessors used in the project.
    /// </summary>
    internal static class Accessors
    {
        #region Fields

        #region For Public Members

        #region ICollection<T>

        private static LockFreeCache<Type, PropertyAccessor>? propertiesICollection_IsReadOnly;
        private static LockFreeCache<Type, MethodAccessor>? methodsICollection_Add;
        private static LockFreeCache<Type, MethodAccessor>? methodsICollection_Clear;
        private static LockFreeCache<Type, PropertyAccessor>? propertiesICollection_Count;
        private static LockFreeCache<Type, MethodAccessor>? methodsICollection_Remove;

        #endregion

        #region IProducerConsumerCollection<T>

#if !NET35
        private static LockFreeCache<Type, MethodAccessor>? methodsIProducerConsumerCollection_TryAdd;
#endif

        #endregion

        #region IList<T>

        private static LockFreeCache<Type, MethodAccessor>? methodsIList_Insert;
        private static LockFreeCache<Type, MethodAccessor>? methodsIList_RemoveAt;
        private static LockFreeCache<Type, PropertyAccessor>? propertiesIList_Item;

        #endregion

        #region Serialization
#if NETFRAMEWORK || NETSTANDARD2_0

        private static MethodAccessor? methodISerializable_GetObjectData;
        private static MethodAccessor? methodISurrogateSelector_GetSurrogate;
        private static MethodAccessor? methodISerializationSurrogate_GetObjectData;
        private static MethodAccessor? methodISerializationSurrogate_SetObjectData;
        private static MethodAccessor? methodIObjectReference_GetRealObject;
        private static MethodAccessor? methodFormatterServices_GetUninitializedObject;

#endif
        #endregion

        #endregion

        #region For Non-Public Members
        // In this region non-public accessors need conditions only if they are not applicable for every supported framework.
        // The #else-#error branches for open-ended versions are in the factories.

        #region IIListProvider<T>

        private static LockFreeCache<Type, MethodAccessor?>? methodsIIListProvider_GetCount;
        private static bool? hasIIListProvider;
        private static Type? typeIIListProvider;

        #endregion

        #region Iterator<T>
#if NET9_0_OR_GREATER

        private static LockFreeCache<Type, MethodAccessor?>? methodsIterator_GetCount;
        private static bool? hasIterator;
        private static Type? typeIterator;

#endif
        #endregion

        #endregion

        #region Any Member

        private static LockFreeCache<(Type DeclaringType, string PropertyName), PropertyAccessor?>? properties;
        private static LockFreeCache<(Type DeclaringType, Type? FieldType, string? FieldNamePattern), FieldAccessor?>? fields;
        private static LockFreeCache<(Type DeclaringType, string MethodName), MethodAccessor?>? methodsByName;
        private static LockFreeCache<(Type DeclaringType, string MethodName, TypesKey ParameterTypes), MethodAccessor>? methodsByTypes;
        private static LockFreeCache<(Type DeclaringType, Type T, string MethodName), MethodAccessor>? staticGenericMethodsByName;
        private static LockFreeCache<(Type DeclaringType, TypesKey GenericArguments, string MethodName, TypesKey ParameterTypes), MethodAccessor>? staticGenericMethodsByTypes;
        private static LockFreeCache<(Type DeclaringType, TypesKey ParameterTypes), CreateInstanceAccessor>? constructors;
        private static LockFreeCache<(Type DeclaringType, TypesKey ParameterTypes), ActionMethodAccessor?>? ctorMethods;

        #endregion

        #endregion

        #region Accessor Factories

        #region For Public Members

        #region ICollection<T>

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2070:TypeDynamicallyAccessedMemberTypesAnnotationMismatch",
            Justification = "We could annotate the lambda parameter accordingly, but that would then cause IL2111. And it is called with collectionInterface, which is annotated anyway.")]
        private static PropertyAccessor ICollection_IsReadOnly([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]Type collectionInterface)
        {
            if (propertiesICollection_IsReadOnly == null)
            {
                Interlocked.CompareExchange(ref propertiesICollection_IsReadOnly,
                    new LockFreeCache<Type, PropertyAccessor>(i => PropertyAccessor.GetAccessor(i.GetProperty(nameof(ICollection<>.IsReadOnly))!), null, LockFreeCacheOptions.Profile16),
                    null);
            }

            return propertiesICollection_IsReadOnly[collectionInterface];
        }

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2070:TypeDynamicallyAccessedMemberTypesAnnotationMismatch",
            Justification = "We could annotate the lambda parameter accordingly, but that would then cause IL2111. And it is called with collectionInterface, which is annotated anyway.")]
        private static MethodAccessor ICollection_Add([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type collectionInterface)
        {
            if (methodsICollection_Add == null)
            {
                Interlocked.CompareExchange(ref methodsICollection_Add,
                    new LockFreeCache<Type, MethodAccessor>(i => MethodAccessor.GetAccessor(i.GetMethod(nameof(ICollection<>.Add))!), null, LockFreeCacheOptions.Profile16),
                    null);
            }

            return methodsICollection_Add[collectionInterface];
        }

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2070:TypeDynamicallyAccessedMemberTypesAnnotationMismatch",
            Justification = "We could annotate the lambda parameter accordingly, but that would then cause IL2111. And it is called with collectionInterface, which is annotated anyway.")]
        private static MethodAccessor ICollection_Clear([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type collectionInterface)
        {
            if (methodsICollection_Clear == null)
            {
                Interlocked.CompareExchange(ref methodsICollection_Clear,
                    new LockFreeCache<Type, MethodAccessor>(i => MethodAccessor.GetAccessor(i.GetMethod(nameof(ICollection<>.Clear))!), null, LockFreeCacheOptions.Profile16),
                    null);
            }

            return methodsICollection_Clear[collectionInterface];
        }

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2070:TypeDynamicallyAccessedMemberTypesAnnotationMismatch",
            Justification = "We could annotate the lambda parameter accordingly, but that would then cause IL2111. And it is called with collectionInterface, which is annotated anyway.")]
        private static PropertyAccessor ICollection_Count([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]Type collectionInterface)
        {
            if (propertiesICollection_Count == null)
            {
                Interlocked.CompareExchange(ref propertiesICollection_Count,
                    new LockFreeCache<Type, PropertyAccessor>(i => PropertyAccessor.GetAccessor(i.GetProperty(nameof(ICollection<>.Count))!), null, LockFreeCacheOptions.Profile16),
                    null);
            }

            return propertiesICollection_Count[collectionInterface];
        }

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2070:TypeDynamicallyAccessedMemberTypesAnnotationMismatch",
            Justification = "We could annotate the lambda parameter accordingly, but that would then cause IL2111. And it is called with collectionInterface, which is annotated anyway.")]
        private static MethodAccessor ICollection_Remove([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type collectionInterface)
        {
            if (methodsICollection_Remove == null)
            {
                Interlocked.CompareExchange(ref methodsICollection_Remove,
                    new LockFreeCache<Type, MethodAccessor>(i => MethodAccessor.GetAccessor(i.GetMethod(nameof(ICollection<>.Remove))!), null, LockFreeCacheOptions.Profile16),
                    null);
            }

            return methodsICollection_Remove[collectionInterface];
        }

        #endregion

        #region IProducerConsumerCollection<T>

#if !NET35
        [UnconditionalSuppressMessage("TrimAnalysis", "IL2070:TypeDynamicallyAccessedMemberTypesAnnotationMismatch",
            Justification = "We could annotate the lambda parameter accordingly, but that would then cause IL2111. And it is called with collectionInterface, which is annotated anyway.")]
        private static MethodAccessor IProducerConsumerCollection_TryAdd([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type collectionInterface)
        {
            if (methodsIProducerConsumerCollection_TryAdd == null)
            {
                Interlocked.CompareExchange(ref methodsIProducerConsumerCollection_TryAdd,
                    new LockFreeCache<Type, MethodAccessor>(i => MethodAccessor.GetAccessor(i.GetMethod(nameof(IProducerConsumerCollection<>.TryAdd))!), null, LockFreeCacheOptions.Profile16),
                    null);
            }

            return methodsIProducerConsumerCollection_TryAdd[collectionInterface];
        }
#endif

        #endregion

        #region IList<T>

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2070:TypeDynamicallyAccessedMemberTypesAnnotationMismatch",
            Justification = "We could annotate the lambda parameter accordingly, but that would then cause IL2111. And it is called with listInterface, which is annotated anyway.")]
        private static MethodAccessor IList_Insert([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type listInterface)
        {
            if (methodsIList_Insert == null)
            {
                Interlocked.CompareExchange(ref methodsIList_Insert,
                    new LockFreeCache<Type, MethodAccessor>(i => MethodAccessor.GetAccessor(i.GetMethod(nameof(IList<>.Insert))!), null, LockFreeCacheOptions.Profile16),
                    null);
            }

            return methodsIList_Insert[listInterface];
        }

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2070:TypeDynamicallyAccessedMemberTypesAnnotationMismatch",
            Justification = "We could annotate the lambda parameter accordingly, but that would then cause IL2111. And it is called with listInterface, which is annotated anyway.")]
        private static MethodAccessor IList_RemoveAt([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type listInterface)
        {
            if (methodsIList_RemoveAt == null)
            {
                Interlocked.CompareExchange(ref methodsIList_RemoveAt,
                    new LockFreeCache<Type, MethodAccessor>(i => MethodAccessor.GetAccessor(i.GetMethod(nameof(IList<>.RemoveAt))!), null, LockFreeCacheOptions.Profile16),
                    null);
            }

            return methodsIList_RemoveAt[listInterface];
        }

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2070:TypeDynamicallyAccessedMemberTypesAnnotationMismatch",
            Justification = "We could annotate the lambda parameter accordingly, but that would then cause IL2111. And it is called with listInterface, which is annotated anyway.")]
        private static PropertyAccessor IList_Item([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]Type listInterface)
        {
            if (propertiesIList_Item == null)
            {
                Interlocked.CompareExchange(ref propertiesIList_Item,
                    new LockFreeCache<Type, PropertyAccessor>(i => PropertyAccessor.GetAccessor(i.GetProperty("Item")!), null, LockFreeCacheOptions.Profile16),
                    null);
            }

            return propertiesIList_Item[listInterface];
        }

        #endregion

        #region Serialization
#if NETFRAMEWORK || NETSTANDARD2_0

        private static MethodAccessor ISerializable_GetObjectData => methodISerializable_GetObjectData
            ??= MethodAccessor.GetAccessor(typeof(ISerializable).GetMethod(nameof(ISerializable.GetObjectData))!);

        private static MethodAccessor ISurrogateSelector_GetSurrogate => methodISurrogateSelector_GetSurrogate
            ??= MethodAccessor.GetAccessor(typeof(ISurrogateSelector).GetMethod(nameof(ISurrogateSelector.GetSurrogate))!);

        private static MethodAccessor ISerializationSurrogate_GetObjectData => methodISerializationSurrogate_GetObjectData
            ??= MethodAccessor.GetAccessor(typeof(ISerializationSurrogate).GetMethod(nameof(ISerializationSurrogate.GetObjectData))!);

        private static MethodAccessor ISerializationSurrogate_SetObjectData => methodISerializationSurrogate_SetObjectData
            ??= MethodAccessor.GetAccessor(typeof(ISerializationSurrogate).GetMethod(nameof(ISerializationSurrogate.SetObjectData))!);

        private static MethodAccessor IObjectReference_GetRealObject => methodIObjectReference_GetRealObject
            ??= MethodAccessor.GetAccessor(typeof(IObjectReference).GetMethod(nameof(IObjectReference.GetRealObject))!);

        private static MethodAccessor FormatterServices_GetUninitializedObject => methodFormatterServices_GetUninitializedObject
            ??= MethodAccessor.GetAccessor(typeof(FormatterServices).GetMethod(nameof(FormatterServices.GetUninitializedObject))!);

#endif
        #endregion

        #endregion

        #region For Non-Public Members
        // Make sure every member in this region is in conditions.
        // Use as narrow conditions as possible and provide an #else #error for open-ended versions so new target frameworks have to always be reviewed.
        // Non-Framework versions can be executed on any runtime (even .NET Core picks a semi-random installation) so for .NET Core/Standard the internal methods must be prepared for null MemberInfos.
        // Whenever possible, use some workaround for non-public .NET Core/Standard libraries.

        #region IIListProvider<T>

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "It is handled if the type could not be resolved")]
        private static Type? IIListProviderType
        {
            get
            {
                if (!hasIIListProvider.HasValue)
                {
                    typeIIListProvider = Reflector.ResolveType(typeof(Enumerable).Assembly, "System.Linq.IIListProvider`1");
                    hasIIListProvider = typeIIListProvider != null;
                }

                return typeIIListProvider;
            }
        }

        private static MethodAccessor? IIListProvider_GetCount(Type genericArgument)
        {
            #region Local Methods

            [UnconditionalSuppressMessage("TrimAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "It is handled if the generic type cannot be created.")]
            [UnconditionalSuppressMessage("TrimAnalysis", "IL2075:DynamicallyAccessedMembersReturnValueAnnotationMismatch", Justification = "It is handled if the method is not found.")]
            [UnconditionalSuppressMessage("TrimAnalysis", "IL3050:RequiresDynamicCode", Justification = "It is handled if the generic type cannot be created.")]
            static MethodAccessor? GetGetCountMethod(Type arg)
            {
                Type? listProviderType = IIListProviderType;
                if (listProviderType == null)
                    return null;
                try
                {
                    Type genericType = listProviderType.GetGenericType(arg);
                    MethodInfo? getCountMethod = genericType.GetMethod("GetCount");
                    return getCountMethod == null ? null : MethodAccessor.GetAccessor(getCountMethod);
                }
                catch (Exception e) when (!e.IsCriticalOr(RuntimeFeature.IsDynamicCodeSupported))
                {
                    return null;
                }
            }

            #endregion

            if (methodsIIListProvider_GetCount == null)
                Interlocked.CompareExchange(ref methodsIIListProvider_GetCount, new LockFreeCache<Type, MethodAccessor?>(GetGetCountMethod, null, LockFreeCacheOptions.Profile128), null);

            return methodsIIListProvider_GetCount[genericArgument];
        }

        #endregion

        #region Iterator<T>
#if NET9_0_OR_GREATER

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "It handles if type cannot be resolved.")]
        private static Type? IteratorType
        {
            get
            {
                if (!hasIterator.HasValue)
                {
                    typeIterator = Reflector.ResolveType(typeof(Enumerable).Assembly, "System.Linq.Enumerable+Iterator`1");
                    hasIterator = typeIterator != null;
                }

                return typeIterator;
            }
        }

        private static MethodAccessor? Iterator_GetCount(Type genericArgument)
        {
            #region Local Methods

            [UnconditionalSuppressMessage("TrimAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "It is handled if the generic type cannot be created.")]
            [UnconditionalSuppressMessage("TrimAnalysis", "IL2075:DynamicallyAccessedMembersReturnValueAnnotationMismatch", Justification = "It is handled if the method is not found.")]
            [UnconditionalSuppressMessage("TrimAnalysis", "IL3050:RequiresDynamicCode", Justification = "It is handled if the generic type cannot be created.")]
            static MethodAccessor? GetGetCountMethod(Type arg)
            {
                Type? iteratorType = IteratorType;
                if (iteratorType == null)
                    return null;
                try
                {
                    Type genericType = iteratorType.GetGenericType(arg);
                    MethodInfo? getCountMethod = genericType.GetMethod("GetCount");
                    return getCountMethod == null ? null : MethodAccessor.GetAccessor(getCountMethod);
                }
                catch (Exception e) when (!e.IsCriticalOr(RuntimeFeature.IsDynamicCodeSupported))
                {
                    return null;
                }
            }

            #endregion

            if (methodsIterator_GetCount == null)
                Interlocked.CompareExchange(ref methodsIterator_GetCount, new LockFreeCache<Type, MethodAccessor?>(GetGetCountMethod, null, LockFreeCacheOptions.Profile128), null);

            return methodsIterator_GetCount[genericArgument];
        }

#endif
        #endregion

        #endregion

        #region Any Member

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [UnconditionalSuppressMessage("TrimAnalysis", "IL2080:DynamicallyAccessedMembersFieldAnnotationMismatch",
            Justification = "False alarm, the tuple field gets its value from the type of the outer method, which is annotated.")]
        private static PropertyAccessor? GetProperty([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllProperties)]Type type, string propertyName)
        {
            #region Local Methods
            
            static PropertyAccessor? GetPropertyAccessor((Type DeclaringType, string PropertyName) key)
            {
                // Ignoring case is allowed due to some incompatibilities between platforms (e.g. internal IsSzArray vs. public IsSZArray).
                // This may prevent an InvalidOperationException when the Framework binaries are executed on .NET Core (may occur when using debugger visualizers, for example).
                PropertyInfo? property = key.DeclaringType.GetProperty(key.PropertyName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
                return property == null ? null : PropertyAccessor.GetAccessor(property);
            }

            #endregion

            if (properties == null)
                Interlocked.CompareExchange(ref properties, new LockFreeCache<(Type, string), PropertyAccessor?>(GetPropertyAccessor, null, LockFreeCacheOptions.Profile128), null);
            return properties[(type, propertyName)];
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [UnconditionalSuppressMessage("TrimAnalysis", "IL2075:DynamicallyAccessedMembersReturnValueAnnotationMismatch",
                Justification = "False alarm, just traversing the hierarchy of the same type by BaseType.")]
        [UnconditionalSuppressMessage("TrimAnalysis", "IL2080:DynamicallyAccessedMembersFieldAnnotationMismatch",
            Justification = "False alarm, the tuple field gets its value from the type of the outer method, which is annotated.")]
        private static FieldAccessor? GetField([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllFields)]Type type, Type? fieldType, string? fieldNamePattern)
        {
            #region Local Methods
            
            // Fields are meant to be used for non-visible members either by type or name pattern (or both)
            FieldAccessor? GetFieldAccessor((Type DeclaringType, Type? FieldType, string? FieldNamePattern) key)
            {
                for (Type? t = key.DeclaringType; t != Reflector.ObjectType; t = t.BaseType)
                {
                    FieldInfo[] fieldArray = t!.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    FieldInfo? field = fieldArray.FirstOrDefault(f => (key.FieldType == null || f.FieldType == key.FieldType) && f.Name == key.FieldNamePattern) // exact name first
                        ?? fieldArray.FirstOrDefault(f => (key.FieldType == null || f.FieldType == key.FieldType)
                            && (key.FieldNamePattern == null || f.Name.Contains(key.FieldNamePattern, StringComparison.OrdinalIgnoreCase)));

                    if (field != null)
                        return FieldAccessor.GetAccessor(field);
                }

                return null;
            }

            #endregion

            if (fields == null)
                Interlocked.CompareExchange(ref fields, new LockFreeCache<(Type, Type?, string?), FieldAccessor?>(GetFieldAccessor, null, LockFreeCacheOptions.Profile128), null);
            return fields[(type, fieldType, fieldNamePattern)];
        }

        [RequiresUnreferencedCode("GetField")]
        private static object? GetFieldValue(object obj, string fieldName)
        {
            FieldAccessor? field = GetField(obj.GetType(), null, fieldName);
            if (field == null)
                Throw.InvalidOperationException(Res.ReflectionInstanceFieldDoesNotExist(fieldName, obj.GetType()));
            return field.Get(obj);
        }

        [RequiresUnreferencedCode("GetField")]
        private static T? GetFieldValueOrDefault<T>(object obj, T? defaultValue = default, string? fieldNamePattern = null)
        {
            FieldAccessor? field = GetField(obj.GetType(), typeof(T), fieldNamePattern);
            return field == null ? defaultValue : (T)field.Get(obj)!;
        }

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2072:ParameterDynamicallyAccessedMemberTypesCannotBeDetermined",
            Justification = "Using typeof(TInstance) instead of obj.GetType() would make this warning disappear, but we use the possibly derived types. It is handled if the field is not found.")]
        private static TField? GetFieldValueOrDefault<[DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllFields)]TInstance, TField>(
            TInstance obj, TField? defaultValue = default, string? fieldNamePattern = null)
            where TInstance : class
        {
            FieldAccessor? field = GetField(obj.GetType(), typeof(TField), fieldNamePattern);
            return field == null ? defaultValue : field.GetInstanceValue<TInstance, TField>(obj);
        }

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2072:ParameterDynamicallyAccessedMemberTypesCannotBeDetermined",
            Justification = "Using typeof(TInstance) instead of obj.GetType() would make this warning disappear, but we use the possibly derived types. It is handled if the field is not found.")]
        private static TField GetFieldValueOrDefault<[DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllFields)]TInstance, TField>(
            TInstance obj, Func<TField> defaultValueFactory)
            where TInstance : class
        {
            FieldAccessor? field = GetField(obj.GetType(), typeof(TField), null);
            return field == null ? defaultValueFactory.Invoke() : field.GetInstanceValue<TInstance, TField>(obj);
        }

        [RequiresUnreferencedCode("GetField")]
        private static void SetFieldValue(object obj, string fieldNamePattern, object? value)
        {
            Type type = obj.GetType();
            FieldAccessor? field = GetField(type, null, fieldNamePattern);
            if (field == null)
                Throw.InvalidOperationException(Res.ReflectionInstanceFieldDoesNotExist(fieldNamePattern, type));
#if NETSTANDARD2_0
            if (field.IsReadOnly || field.MemberInfo.DeclaringType?.IsValueType == true)
            {
                ((FieldInfo)field.MemberInfo).SetValue(obj, value);
                return;
            }
#endif

            field.Set(obj, value);
        }

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2080:DynamicallyAccessedMembersFieldAnnotationMismatch",
            Justification = "False alarm, the tuple field gets its value from the type of the outer method, which is annotated.")]
        private static MethodAccessor? GetMethodByName([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllMethods)]Type type, string methodName)
        {
            #region Local Methods
            
            static MethodAccessor? GetMethodAccessor((Type DeclaringType, string MethodName) key)
            {
                MethodInfo? method = key.DeclaringType.GetMethod(key.MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return method == null ? null : MethodAccessor.GetAccessor(method);
            }

            #endregion

            if (methodsByName == null)
                Interlocked.CompareExchange(ref methodsByName, new LockFreeCache<(Type, string), MethodAccessor?>(GetMethodAccessor, null, LockFreeCacheOptions.Profile128), null);
            return methodsByName[(type, methodName)];
        }

        private static MethodAccessor GetMethodByTypes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type type, string methodName, TypesKey parameterTypes)
        {
            #region Local Methods

            [UnconditionalSuppressMessage("TrimAnalysis", "IL2080:DynamicallyAccessedMembersFieldAnnotationMismatch",
                Justification = "False alarm, the tuple field gets its value from the type of the outer method, which is annotated, and it is enough to get the public methods only.")]
            static MethodAccessor GetMethodAccessor((Type DeclaringType, string MethodName, TypesKey ParameterTypes) key)
            {
                // Unlike in GetMethodByName, here result is not nullable because we invoke public methods only
                MethodInfo[] methods = key.DeclaringType.GetMember(key.MethodName, MemberTypes.Method, BindingFlags.Instance | BindingFlags.Public)
                    .Cast<MethodInfo>()
                    .Where(m => !m.IsGenericMethodDefinition && m.GetParameters().Length == key.ParameterTypes.Types.Length)
                    .ToArray();

                foreach (MethodInfo mi in methods)
                {
                    if (!mi.GetParameters().Select(p => p.ParameterType).SequenceEqual(key.ParameterTypes.Types))
                        continue;

                    return MethodAccessor.GetAccessor(mi);
                }

                return Throw.InternalError<MethodAccessor>($"No matching method found: {key}");
            }

            #endregion

            if (methodsByTypes == null)
                Interlocked.CompareExchange(ref methodsByTypes, new LockFreeCache<(Type, string, TypesKey), MethodAccessor>(GetMethodAccessor, null, LockFreeCacheOptions.Profile128), null);
            return methodsByTypes[(type, methodName, parameterTypes)];
        }

        [RequiresDynamicCode("GetGenericMethod")]
        [RequiresUnreferencedCode("GetGenericMethod")]
        private static MethodAccessor GetStaticGenericMethodByName([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type type, // public generic method definitions
            Type typeArgument, string methodName)
        {
            #region Local Methods

            [RequiresDynamicCode("GetGenericMethod")]
            [RequiresUnreferencedCode("GetGenericMethod")]
            static MethodAccessor GetMethodAccessor((Type DeclaringType, Type T, string MethodName) key)
            {
                // Unlike in GetMethodByName, here result is not nullable because we invoke public methods only
                MethodInfo method = key.DeclaringType.GetMethod(key.MethodName, BindingFlags.Static | BindingFlags.Public)!.GetGenericMethod(key.T);
                return MethodAccessor.GetAccessor(method);
            }

            #endregion

            if (staticGenericMethodsByName == null)
                Interlocked.CompareExchange(ref staticGenericMethodsByName, new LockFreeCache<(Type, Type, string), MethodAccessor>(GetMethodAccessor, null, LockFreeCacheOptions.Profile128), null);
            return staticGenericMethodsByName[(type, typeArgument, methodName)];
        }

        [RequiresDynamicCode("MakeGenericMethod")]
        [RequiresUnreferencedCode("MakeGenericMethod")]
        private static MethodAccessor GetStaticGenericMethodByTypes([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type type, // public generic method definitions
            string methodName, TypesKey typeArguments, TypesKey parameterTypes)
        {
            #region Local Methods

            [RequiresDynamicCode("MakeGenericMethod")]
            [RequiresUnreferencedCode("MakeGenericMethod")]
            static MethodAccessor GetMethodAccessor((Type DeclaringType, TypesKey GenericArguments, string MethodName, TypesKey ParameterTypes) key)
            {
                // Unlike in GetMethodByName, here result is not nullable because we invoke public methods only
                MethodInfo[] methods = key.DeclaringType.GetMember(key.MethodName, MemberTypes.Method, BindingFlags.Static | BindingFlags.Public)
                    .Cast<MethodInfo>()
                    .Where(m => m.IsGenericMethodDefinition && m.GetGenericArguments().Length == key.GenericArguments.Types.Length && m.GetParameters().Length == key.ParameterTypes.Types.Length)
                    .ToArray();

                foreach (MethodInfo mi in methods)
                {
                    MethodInfo constructedMethod;
                    try
                    {
                        constructedMethod = mi.MakeGenericMethod(key.GenericArguments.Types);
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }

                    if (!constructedMethod.GetParameters().Select(p => p.ParameterType).SequenceEqual(key.ParameterTypes.Types))
                        continue;

                    return MethodAccessor.GetAccessor(constructedMethod);
                }

                return Throw.InternalError<MethodAccessor>($"No matching method found: {key}");
            }

            #endregion

            if (staticGenericMethodsByTypes == null)
                Interlocked.CompareExchange(ref staticGenericMethodsByTypes, new LockFreeCache<(Type, TypesKey, string, TypesKey), MethodAccessor>(GetMethodAccessor, null, LockFreeCacheOptions.Profile128), null);
            return staticGenericMethodsByTypes[(type, typeArguments, methodName, parameterTypes)];
        }

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2080:DynamicallyAccessedMembersFieldAnnotationMismatch",
            Justification = "False alarm, the tuple field gets its value from the type of the outer method, which is annotated.")]
        private static CreateInstanceAccessor GetConstructor([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllConstructors)]Type type, TypesKey parameterTypes)
        {
            #region Local Methods
            
            static CreateInstanceAccessor GetCreateInstanceAccessor((Type DeclaringType, TypesKey ParameterTypes) key)
            {
                // Here we accept non-public constructors, too. They should be really well-known at least internal members.
                ConstructorInfo? ci = key.DeclaringType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, key.ParameterTypes.Types, null);
                Debug.Assert(ci != null, "Constructor was not found for the specified parameter types");
                return CreateInstanceAccessor.GetAccessor(ci!);
            }

            #endregion

            Debug.Assert(parameterTypes.Types.Length > 0, "At least one parameter is expected.");

            if (constructors == null)
                Interlocked.CompareExchange(ref constructors, new LockFreeCache<(Type, TypesKey), CreateInstanceAccessor>(GetCreateInstanceAccessor, null, LockFreeCacheOptions.Profile128), null);
            return constructors[(type, parameterTypes)];
        }

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2077:DynamicallyAccessedMembersSourceFieldTargetParameterAnnotationMismatch",
            Justification = "False alarm, the tuple field gets its value from the type of the outer method, which is annotated.")]
        [UnconditionalSuppressMessage("TrimAnalysis", "IL2080:DynamicallyAccessedMembersFieldAnnotationMismatch",
            Justification = "False alarm, the tuple field gets its value from the type of the outer method, which is annotated.")]
        private static ActionMethodAccessor? GetCtorMethod([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllConstructors)]Type type, object[] ctorArgs)
        {
            #region Local Methods
            
            static ActionMethodAccessor? GetCtorMethodAccessor((Type Type, TypesKey ParameterTypes) key)
            {
                ConstructorInfo? ci = key.ParameterTypes.Types.Length == 0
                    ? key.Type.GetDefaultConstructor()
                    : key.Type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, key.ParameterTypes.Types, null);
                return ci == null ? null : new ActionMethodAccessor(ci);
            }

            #endregion

            if (ctorMethods == null)
                Interlocked.CompareExchange(ref ctorMethods, new LockFreeCache<(Type, TypesKey), ActionMethodAccessor?>(GetCtorMethodAccessor, null, LockFreeCacheOptions.Profile128), null);
            return ctorMethods[(type, ctorArgs.ToTypesKey())];
        }

        #endregion

        #endregion

        #region Internal Accessor Methods

        #region For Public Members

        #region CollectionExtensions

        [RequiresDynamicCode("InvokeMethod/GetStaticGenericMethodByName")]
        [UnconditionalSuppressMessage("TrimAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "MakeGenericMethod has RequiresUnreferencedCode because the constraints cannot be validated, but CollectionExtensions.AddRange has no constraints.")]
        internal static void AddRange(this IEnumerable target, Type genericArgument, IEnumerable collection)
            => InvokeMethod(typeof(CollectionExtensions), nameof(CollectionExtensions.AddRange), genericArgument, target, collection);

        #endregion

        #region ListExtensions

        [RequiresDynamicCode("InvokeMethod/GetStaticGenericMethodByName")]
        [UnconditionalSuppressMessage("TrimAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "MakeGenericMethod has RequiresUnreferencedCode because the constraints cannot be validated, but ListExtensions.InsertRange has no constraints.")]
        internal static void InsertRange(this IEnumerable target, Type genericArgument, int index, IEnumerable collection)
            => typeof(ListExtensions).InvokeMethod(nameof(ListExtensions.InsertRange), genericArgument, target, index, collection);

        [RequiresDynamicCode("InvokeMethod/GetStaticGenericMethodByName")]
        [UnconditionalSuppressMessage("TrimAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "MakeGenericMethod has RequiresUnreferencedCode because the constraints cannot be validated, but ListExtensions.RemoveRange has no constraints.")]
        internal static void RemoveRange(this IEnumerable collection, Type genericArgument, int index, int count)
            => typeof(ListExtensions).InvokeMethod(nameof(ListExtensions.RemoveRange), genericArgument, collection, index, count);

        [RequiresDynamicCode("InvokeMethod/GetStaticGenericMethodByName")]
        [UnconditionalSuppressMessage("TrimAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "MakeGenericMethod has RequiresUnreferencedCode because the constraints cannot be validated, but ListExtensions.ReplaceRange has no constraints.")]
        internal static void ReplaceRange(this IEnumerable target, Type genericArgument, int index, int count, IEnumerable collection)
            => typeof(ListExtensions).InvokeMethod(nameof(ListExtensions.ReplaceRange), genericArgument, target, index, count, collection);

        #endregion

        #region ICollection<T>

        internal static bool IsReadOnly([NoEnumeration]this IEnumerable collection, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]Type collectionInterface)
            => (bool)ICollection_IsReadOnly(collectionInterface).Get(collection)!;

        internal static void Add([NoEnumeration]this IEnumerable collection, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type collectionInterface, object? item)
            => ICollection_Add(collectionInterface).Invoke(collection, item);
        
        internal static void Clear([NoEnumeration]this IEnumerable collection, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type collectionInterface)
            => ICollection_Clear(collectionInterface).Invoke(collection);
        
        internal static int Count([NoEnumeration]this IEnumerable collection, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]Type collectionInterface)
            => (int)ICollection_Count(collectionInterface).Get(collection)!;
        
        internal static bool Remove([NoEnumeration]this IEnumerable collection, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type collectionInterface, object? item)
            => (bool)ICollection_Remove(collectionInterface).Invoke(collection, item)!;

        #endregion

        #region IProducerConsumerCollection<T>

#if !NET35
        internal static bool TryAddToProducerConsumerCollection([NoEnumeration]this IEnumerable collection,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type collectionInterface, object? item)
            => (bool)IProducerConsumerCollection_TryAdd(collectionInterface).Invoke(collection, item)!;
#endif

        #endregion

        #region IList<T>

        internal static void Insert([NoEnumeration]this IEnumerable list, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type listInterface, int index, object? item)
            => IList_Insert(listInterface).Invoke(list, index, item);

        internal static void RemoveAt([NoEnumeration]this IEnumerable list, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]Type listInterface, int index)
            => IList_RemoveAt(listInterface).Invoke(list, index);
        
        internal static void SetElementAt([NoEnumeration]this IEnumerable list, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]Type listInterface, int index, object? item)
            => IList_Item(listInterface).Set(list, item, index);

        #endregion

        #region Serialization
#if NETFRAMEWORK || NETSTANDARD2_0

        [SecurityCritical]
        internal static void GetObjectDataSafe(this ISerializable serializable, SerializationInfo si, StreamingContext ctx)
        {
            // Direct call: must be a separate method; otherwise, the caller of this method may get a SecurityException in a partially trusted domain
            if (!EnvironmentHelper.IsPartiallyTrustedDomain)
            {
                GetObjectDataDirect(serializable, si, ctx);
                return;
            }

            // Partially trusted access: generating the accessor that skips verification even if SecurityPermissionFlag.SkipVerification is not granted
            ISerializable_GetObjectData.Invoke(serializable, si, ctx);
        }

        [SecurityCritical]
        private static void GetObjectDataDirect(ISerializable serializable, SerializationInfo si, StreamingContext ctx) => serializable.GetObjectData(si, ctx);

        [SecurityCritical]
        internal static ISerializationSurrogate GetSurrogateSafe(this ISurrogateSelector surrogateSelector, Type type, StreamingContext ctx, out ISurrogateSelector selector)
        {
            // Direct call: must be a separate method; otherwise, the caller of this method may get a SecurityException in a partially trusted domain
            if (!EnvironmentHelper.IsPartiallyTrustedDomain)
                return GetSurrogateDirect(surrogateSelector, type, ctx, out selector);

            // Partially trusted access: generating the accessor that skips verification even if SecurityPermissionFlag.SkipVerification is not granted
            object[] parameters = { type, ctx, null! };
            object result = ISurrogateSelector_GetSurrogate.Invoke(surrogateSelector, parameters)!;
            selector = (ISurrogateSelector)parameters[2];
            return (ISerializationSurrogate)result;
        }

        [SecurityCritical]
        private static ISerializationSurrogate GetSurrogateDirect(ISurrogateSelector surrogateSelector, Type type, StreamingContext ctx, out ISurrogateSelector selector)
            => surrogateSelector.GetSurrogate(type, ctx, out selector);

        [SecurityCritical]
        internal static void GetObjectDataSafe(this ISerializationSurrogate surrogate, object obj, SerializationInfo si, StreamingContext ctx)
        {
            // Direct call: must be a separate method; otherwise, the caller of this method may get a SecurityException in a partially trusted domain
            if (!EnvironmentHelper.IsPartiallyTrustedDomain)
            {
                GetObjectDataDirect(surrogate, obj, si, ctx);
                return;
            }

            // Partially trusted access: generating the accessor that skips verification even if SecurityException.SkipVerification is not granted
            ISerializationSurrogate_GetObjectData.Invoke(surrogate, obj, si, ctx);
        }

        [SecurityCritical]
        private static void GetObjectDataDirect(ISerializationSurrogate surrogate, object obj, SerializationInfo si, StreamingContext ctx)
            => surrogate.GetObjectData(obj, si, ctx);

        [SecurityCritical]
        internal static object SetObjectDataSafe(this ISerializationSurrogate surrogate, object obj, SerializationInfo si, StreamingContext ctx, ISurrogateSelector? selector)
        {
            // Direct call: must be a separate method; otherwise, the caller of this method may get a SecurityException in a partially trusted domain
            if (!EnvironmentHelper.IsPartiallyTrustedDomain)
                return SetObjectDataDirect(surrogate, obj, si, ctx, selector);

            // Partially trusted access: generating the accessor that skips verification even if SecurityException.SkipVerification is not granted
            return ISerializationSurrogate_SetObjectData.Invoke(surrogate, obj, si, ctx, selector)!;
        }

        [SecurityCritical]
        private static object SetObjectDataDirect(ISerializationSurrogate surrogate, object obj, SerializationInfo si, StreamingContext ctx, ISurrogateSelector? selector)
            => surrogate.SetObjectData(obj, si, ctx, selector);

        [SecurityCritical]
        internal static object? GetRealObjectSafe(this IObjectReference objRef, StreamingContext ctx)
        {
            // Direct call: must be a separate method; otherwise, the caller of this method may get a SecurityException in a partially trusted domain
            if (!EnvironmentHelper.IsPartiallyTrustedDomain)
                return GetRealObjectDirect(objRef, ctx);

            // Partially trusted access: generating the accessor that skips verification even if SecurityPermissionFlag.SkipVerification is not granted
            return IObjectReference_GetRealObject.Invoke(objRef, ctx);
        }

        [SecurityCritical]
        private static object? GetRealObjectDirect(IObjectReference objRef, StreamingContext ctx) => objRef.GetRealObject(ctx);

        [SecurityCritical]
        internal static object CreateUninitializedObjectSafe(Type type)
        {
            // Direct call: must be a separate method; otherwise, the caller of this method may get a SecurityException in a partially trusted domain
            if (!EnvironmentHelper.IsPartiallyTrustedDomain)
                return CreateUninitializedObjectDirect(type);

            // Partially trusted access: generating the accessor that skips verification even if SecurityPermissionFlag.SkipVerification is not granted
            return FormatterServices_GetUninitializedObject.Invoke(null, type)!;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [SecurityCritical]
        private static object CreateUninitializedObjectDirect(Type t) => FormatterServices.GetUninitializedObject(t);

#endif
        #endregion

        #endregion

        #region For Non-Public Members
        // In this region non-public accessors need conditions only if they are not applicable for every supported framework.
        // The #else-#error branches for open-ended versions are in the factories.

        #region Exception

#if NET35 || NET40
        internal static string? GetSource(this Exception exception) => GetFieldValueOrDefault<Exception, string?>(exception, null, "_source");
        internal static void SetSource(this Exception exception, string? value) => GetField(typeof(Exception), null, "_source")?.SetInstanceValue(exception, value);
        internal static void SetRemoteStackTraceString(this Exception exception, string value) => GetField(typeof(Exception), null, "_remoteStackTraceString")?.SetInstanceValue(exception, value);
        internal static void InternalPreserveStackTrace(this Exception exception) => GetMethodByName(typeof(Exception), nameof(InternalPreserveStackTrace))?.InvokeInstanceAction(exception);
#endif

        #endregion

        #region Point

#if !NETCOREAPP || NETCOREAPP3_0_OR_GREATER
        [RequiresUnreferencedCode("GetPropertyValue")]
        internal static int Point_GetX(object? point) => point == null ? 0 : (int)GetPropertyValue(point, "X")!;
        
        [RequiresUnreferencedCode("GetPropertyValue")]
        internal static int Point_GetY(object? point) => point == null ? 0 : (int)GetPropertyValue(point, "Y")!;
#endif

        #endregion

        #region MemoryStream

        internal static byte[]? InternalGetBuffer(this MemoryStream ms)
            => GetMethodByName(typeof(MemoryStream), "InternalGetBuffer")?.InvokeInstanceFunction<MemoryStream, byte[]>(ms);

        #endregion

        #region Object

        internal static object MemberwiseClone(this object obj)
            => GetMethodByName(typeof(object), nameof(MemberwiseClone))!.InvokeInstanceFunction<object, object>(obj);

        #endregion

        #region IIListProvider<T>

#if !NET6_0_OR_GREATER
        [UnconditionalSuppressMessage("TrimAnalysis", "IL2072:ParameterDynamicallyAccessedMemberTypesCannotBeDetermined",
            Justification = "It is handled if the list provider count cannot be determined.")]
        internal static int? GetListProviderCount<T>([NoEnumeration]this IEnumerable<T> collection)
        {
            MethodAccessor? accessor = IIListProvider_GetCount(typeof(T));
            if (accessor == null || !accessor.MemberInfo.DeclaringType!.IsInstanceOfType(collection))
                return null;
            return accessor.Invoke(collection, true) as int?;
        }
#endif

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2072:ParameterDynamicallyAccessedMemberTypesCannotBeDetermined",
            Justification = "It is handled if the list provider count cannot be determined.")]
        internal static int? GetListProviderCount([NoEnumeration]this IEnumerable collection)
        {
            Type? listProviderType = IIListProviderType;
            if (listProviderType == null || !collection.GetType().IsImplementationOfGenericType(listProviderType, out Type? genericType))
                return null;

            MethodAccessor? accessor = IIListProvider_GetCount(genericType.GetGenericArguments()[0]);
            if (accessor == null)
                return null;
            Debug.Assert(accessor.MemberInfo.DeclaringType!.IsInstanceOfType(collection));
            return accessor.Invoke(collection, true) as int?;
        }

        #endregion

        #region Iterator<T>
#if NET9_0_OR_GREATER

        [UnconditionalSuppressMessage("TrimAnalysis", "IL2072:ParameterDynamicallyAccessedMemberTypesCannotBeDetermined",
            Justification = "It is handled if the iterator count cannot be determined.")]
        internal static int? GetIteratorCount([NoEnumeration]this IEnumerable collection)
        {
            Type? iteratorType = IteratorType;
            if (iteratorType == null || !collection.GetType().IsImplementationOfGenericType(iteratorType, out Type? genericType))
                return null;

            MethodAccessor? accessor = Iterator_GetCount(genericType.GetGenericArguments()[0]);
            if (accessor == null)
                return null;
            Debug.Assert(accessor.MemberInfo.DeclaringType!.IsInstanceOfType(collection));
            return accessor.Invoke(collection, true) as int?;
        }

#endif
        #endregion

        #endregion

        #region Members of Any Type
        // Note: The methods also here should be as specific as possible. "Any Type" means that the caller must know whether these methods can be used for a specific type
        //       and that these members use a common cache for any type.
        // Important: Visible members are allowed to be called on types only where we know these properties exist. Otherwise, an InvalidOperationException can be thrown.
        //            For non-visible members we always have to provide some default value.

        #region Specific Members

        #region ResXFileRef

#if !NETCOREAPP || NETCOREAPP3_0_OR_GREATER
        [RequiresUnreferencedCode("GetPropertyValue")]
        internal static string? ResXFileRef_GetFileName(object fileRef) => (string?)GetPropertyValue(fileRef, "FileName");

        [RequiresUnreferencedCode("GetPropertyValue")]
        internal static string? ResXFileRef_GetTypeName(object fileRef) => (string?)GetPropertyValue(fileRef, "TypeName");
        
        [RequiresUnreferencedCode("GetPropertyValue")]
        internal static Encoding? ResXFileRef_GetTextFileEncoding(object fileRef) => GetPropertyValueOrDefault<Encoding>(fileRef, "TextFileEncoding");
#endif

        #endregion

        #region ResXDataNode

#if !NETCOREAPP || NETCOREAPP3_0_OR_GREATER
        [RequiresUnreferencedCode("GetFieldValueOrDefault")]
        internal static object? ResXDataNode_GetValue(object node) => GetFieldValueOrDefault<object?>(node, null, "value");

        [RequiresUnreferencedCode("GetFieldValueOrDefault")]
        internal static string? ResXDataNode_GetComment(object node) => GetFieldValueOrDefault<string?>(node, null, "comment");
        
        [RequiresUnreferencedCode("GetField")]
        internal static object? ResXDataNode_GetFileRef(object node) => GetField(node.GetType(), null, "fileRef")?.Get(node);

        [RequiresUnreferencedCode("GetField")]
        internal static object? ResXDataNode_GetNodeInfo(object node) => GetField(node.GetType(), null, "nodeInfo")?.Get(node);
#endif

        #endregion

        #region DataNodeInfo

#if !NETCOREAPP || NETCOREAPP3_0_OR_GREATER
        [RequiresUnreferencedCode("GetFieldValueOrDefault")]
        internal static string? DataNodeInfo_GetName(object nodeInfo) => GetFieldValueOrDefault<string?>(nodeInfo, null, "Name");
        
        [RequiresUnreferencedCode("GetFieldValueOrDefault")]
        internal static string? DataNodeInfo_GetComment(object nodeInfo) => GetFieldValueOrDefault<string?>(nodeInfo, null, "Comment");
        
        [RequiresUnreferencedCode("GetFieldValueOrDefault")]
        internal static string? DataNodeInfo_GetTypeName(object nodeInfo) => GetFieldValueOrDefault<string?>(nodeInfo, null, "TypeName");
        
        [RequiresUnreferencedCode("GetFieldValueOrDefault")]
        internal static string? DataNodeInfo_GetMimeType(object nodeInfo) => GetFieldValueOrDefault<string?>(nodeInfo, null, "MimeType");
     
        [RequiresUnreferencedCode("GetFieldValueOrDefault")]
        internal static string? DataNodeInfo_GetValueData(object nodeInfo) => GetFieldValueOrDefault<string?>(nodeInfo, null, "ValueData");
        
        [RequiresUnreferencedCode("GetField")]
        internal static object? DataNodeInfo_GetReaderPosition(object nodeInfo) => GetField(nodeInfo.GetType(), null, "ReaderPosition")?.Get(nodeInfo);
#endif

        #endregion

        #region IEnumerable

        [RequiresUnreferencedCode("GetProperty")]
        internal static int Count([NoEnumeration]this IEnumerable collection)
        {
            if (collection is ICollection c)
                return c.Count;
            PropertyAccessor? property = GetProperty(collection.GetType(), "Count"); // StringDictionary
            if (property == null)
                Throw.InvalidOperationException(Res.ReflectionInstancePropertyDoesNotExist("Count", collection.GetType()));
            return (int)property.Get(collection)!;
        }

        [RequiresUnreferencedCode("GetProperty")]
        internal static int Capacity([NoEnumeration]this IEnumerable collection)
        {
            PropertyAccessor? property = GetProperty(collection.GetType(), "Capacity"); // List<T>, CircularList<T>, SortedList<TKey, TValue>, SortedList, CircularSortedList<TKey, TValue>, ArrayList, OrderedDictionary<TKey, TValue>
            if (property == null)
                Throw.InvalidOperationException(Res.ReflectionInstancePropertyDoesNotExist("Capacity", collection.GetType()));
            return (int)property.Get(collection)!;
        }

        [RequiresUnreferencedCode("GetFieldValueOrDefault")]
        internal static bool IsCaseInsensitive([NoEnumeration]this IEnumerable collection)
            => GetFieldValueOrDefault<bool>(collection, false, "caseInsensitive"); // HybridDictionary

        [RequiresUnreferencedCode("GetFieldValue")]
        internal static bool UsesBitwiseAndHash([NoEnumeration]this IEnumerable collection)
        {
            Debug.Assert(collection.GetType().IsGenericTypeOf(typeof(ThreadSafeHashSet<>)) || collection.GetType().IsGenericTypeOf(typeof(ThreadSafeDictionary<,>)));
            return (bool)GetFieldValue(collection, "bitwiseAndHash")!; // ThreadSafeHashSet<T>, ThreadSafeDictionary<TKey, TValue>
        }

        [RequiresUnreferencedCode("GetProperty, GetField")]
        internal static object? GetComparer([NoEnumeration]this IEnumerable collection)
        {
            // 1.) By Comparer/EqualityComparer/KeyComparer property
            Type type = collection.GetType();
            PropertyAccessor? property =  GetProperty(type, type.Name switch
            {
                nameof(Hashtable) => "EqualityComparer",
                "ImmutableHashSet`1" or "ImmutableSortedSet`1" or "ImmutableDictionary`2" or "ImmutableSortedDictionary`2" or "Builder" => "KeyComparer",
                _ => "Comparer" // Dictionary<TKey, TValue>, HashSet<T>, SortedSet<T>, SortedList<TKey, TValue>, SortedDictionary<TKey, TValue>, CircularSortedList<TKey, TValue>, ThreadSafeHashSet<T>, ThreadSafeDictionary<TKey, TValue>, StringKeyedDictionary<TValue>
            }); 

            if (property != null)
                return property.Get(collection);

            // 2.) By *comparer* field
            return GetField(type, null, "comparer")?.Get(collection); // SortedList, ListDictionary, OrderedDictionary, ConcurrentDictionary (< .NET 6)
        }

        #endregion

        #region Comparer/CaseInsensitiveComparer/StringComparer

        internal static CompareInfo? CompareInfo(this Comparer comparer) => GetFieldValueOrDefault<Comparer, CompareInfo?>(comparer);
        internal static CompareInfo? CompareInfo(this CaseInsensitiveComparer comparer) => GetFieldValueOrDefault<CaseInsensitiveComparer, CompareInfo?>(comparer);

#if !NET6_0_OR_GREATER
        internal static CompareInfo? CompareInfo(this StringComparer comparer)
        {
            Debug.Assert(comparer.GetType() == StringComparer.CurrentCulture.GetType(), "Not a culture aware string comparer.");
            return GetFieldValueOrDefault<StringComparer, CompareInfo>(comparer);
        }

        [RequiresUnreferencedCode("GetField")]
        internal static CompareOptions CompareOptions(this StringComparer comparer)
        {
            Type type = comparer.GetType();
            Debug.Assert(type == StringComparer.CurrentCulture.GetType(), "Not a culture aware string comparer.");

            FieldAccessor? field = GetField(type, typeof(CompareOptions), null);
            if (field != null)
                return (CompareOptions)field.Get(comparer)!;

            field = GetField(type, typeof(bool), "ignoreCase");
            if (field != null)
                return (bool)field.Get(comparer)! ? System.Globalization.CompareOptions.IgnoreCase : default;

            return default;
        }
#endif

        #endregion

        #region BitArray

        internal static int[] GetUnderlyingArray(this BitArray bitArray)
        {
            int[]? result = GetFieldValueOrDefault<BitArray, int[]?>(bitArray);
            if (result != null)
                return result;

            // we need to restore the array from the bits (should never occur, but we must provide a fallback due to private field handling)
            int len = bitArray.Length;
            result = new int[len > 0 ? ((len - 1) >> 5) + 1 : 0];
            for (int i = 0; i < len; i++)
            {
                if (bitArray[i])
                    result[i >> 5] |= 1 << (i % 32);
            }

            return result;
        }

        #endregion

        #region DictionaryEntry/KeyValuePair

        [RequiresUnreferencedCode("GetProperty, SetFieldValue")]
        internal static void SetKeyValue(object instance, object? key, object? value)
        {
            // Though DictionaryEntry.Key/Value have setters they must be set by reflection because of the boxed struct
            if (instance is DictionaryEntry)
            {
                Type type = instance.GetType();
#if NETSTANDARD2_0
                ((PropertyInfo)GetProperty(type, nameof(DictionaryEntry.Key))!.MemberInfo).SetValue(instance, key);
                ((PropertyInfo)GetProperty(type, nameof(DictionaryEntry.Value))!.MemberInfo).SetValue(instance, value);
#else
                GetProperty(type, nameof(DictionaryEntry.Key))!.Set(instance, key);
                GetProperty(type, nameof(DictionaryEntry.Value))!.Set(instance, value);
#endif
                return;
            }

            Debug.Assert(instance.GetType().IsGenericTypeOf(Reflector.KeyValuePairType));
            SetFieldValue(instance, "key", key);
            SetFieldValue(instance, "value", value);
        }

        #endregion

        #region Type
#if NETFRAMEWORK
        internal static bool IsSzArray(this Type type) => EnvironmentHelper.IsMono
            ? type.IsArray && type.Name.EndsWith("[]", StringComparison.Ordinal)
            : GetProperty(Reflector.Type, nameof(IsSzArray))!.GetInstanceValue<Type, bool>(type);

#endif
        #endregion

        #region SerializationInfo

#if NET8_0_OR_GREATER
#pragma warning disable SYSLIB0050 // IFormatterConverter is obsolete 
#endif
        internal static IFormatterConverter GetConverter(this SerializationInfo info)
            => GetFieldValueOrDefault<SerializationInfo, IFormatterConverter>(info, () => new FormatterConverter());
#if NET8_0_OR_GREATER
#pragma warning restore SYSLIB0050 // IFormatterConverter is obsolete 
#endif

        #endregion

        #endregion

        #region Any Member
        // Note: These methods could be completely replaced by Reflector methods but these use a smaller and more direct cache

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static FieldInfo GetFieldInfo([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllFields)]this Type type, string fieldNamePattern)
        {
            FieldInfo? field = (FieldInfo?)GetField(type, null, fieldNamePattern)?.MemberInfo;
            if (field == null)
                Throw.InvalidOperationException(Res.ReflectionInstanceFieldDoesNotExist(fieldNamePattern, type));
            return field;
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [RequiresDynamicCode("GetGenericType")]
        [RequiresUnreferencedCode("GetGenericType")]
        internal static object? GetPropertyValue(this Type genTypeDef, Type t, string propertyName)
        {
            Type type = genTypeDef.GetGenericType(t);
            PropertyAccessor? property = GetProperty(type, propertyName);
            if (property == null)
                Throw.InvalidOperationException(Res.ReflectionStaticPropertyDoesNotExist(propertyName, genTypeDef));
            return property.Get(null);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [RequiresUnreferencedCode("GetProperty")]
        internal static object? GetPropertyValue(object instance, string propertyName)
        {
            PropertyAccessor? property = GetProperty(instance.GetType(), propertyName);
            if (property == null)
                Throw.InvalidOperationException(Res.ReflectionInstancePropertyDoesNotExist(propertyName, instance.GetType()));
            return property.Get(instance);
        }

        [RequiresUnreferencedCode("GetProperty")]
        private static T? GetPropertyValueOrDefault<T>(object obj, string propertyName)
        {
            PropertyAccessor? property = GetProperty(obj.GetType(), propertyName);
            return property == null ? default : (T?)property.Get(obj);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static object? GetPropertyValue([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllProperties)]this Type type, string propertyName)
        {
            PropertyAccessor? property = GetProperty(type, propertyName);
            if (property == null)
                Throw.InvalidOperationException(Res.ReflectionInstancePropertyDoesNotExist(propertyName, type));
            return property.Get(null);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [RequiresUnreferencedCode("GetProperty")]
        internal static void SetPropertyValue(object instance, string propertyName, object? value)
        {
            PropertyAccessor? property = GetProperty(instance.GetType(), propertyName);
            if (property == null)
                Throw.InvalidOperationException(Res.ReflectionInstancePropertyDoesNotExist(propertyName, instance.GetType()));
            property.Set(instance, value);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static object? Get(this FieldInfo field, object? instance) => FieldAccessor.GetAccessor(field).Get(instance);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static void Set(this FieldInfo field, object? instance, object? value)
        {
            Debug.Assert(!field.IsLiteral);
            FieldAccessor.GetAccessor(field).Set(instance, value);
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static object? Get(this PropertyInfo property, object? instance)
        {
            Debug.Assert(property.CanRead);
            return PropertyAccessor.GetAccessor(property).Get(instance);
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static void Set(this PropertyInfo property, object? instance, object? value, params object?[] indexerParams)
        {
            Debug.Assert(property.CanWrite || property.PropertyType.IsByRef);
            PropertyAccessor.GetAccessor(property).Set(instance, value, indexerParams);
        }

        /// <summary>
        /// For possibly mutating value types on every platform.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static object? Invoke(MethodInfo method, object? instance, params object?[] parameters)
        {
#if NETSTANDARD2_0
            if (!method.IsStatic && method.DeclaringType?.IsValueType == true)
                return method.Invoke(instance, parameters);
#endif

            return MethodAccessor.GetAccessor(method).Invoke(instance, parameters);
        }

        /// <summary>
        /// For unambiguous instance methods by name.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [RequiresUnreferencedCode("GetMethodByName")]
        internal static object? InvokeMethod(object instance, string methodName, params object?[] parameters)
            => GetMethodByName(instance.GetType(), methodName)!.Invoke(instance, parameters);

        /// <summary>
        /// For instance methods by name and parameter types.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [RequiresUnreferencedCode("GetMethodByTypes")]
        internal static object? InvokeMethod(object instance, string methodName, Type[] parameterTypes, params object?[] parameters)
            => GetMethodByTypes(instance.GetType(), methodName, parameterTypes.ToTypesKey()).Invoke(instance, parameters);

        /// <summary>
        /// For instance methods by name with exactly one non-base typed parameter.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [RequiresUnreferencedCode("InvokeMethod")]
        internal static object? InvokeMethod(object instance, string methodName, object parameter)
            => InvokeMethod(instance, methodName, [parameter.GetType()], parameter);

        /// <summary>
        /// For unambiguous static methods by name.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static object? InvokeMethod([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllMethods)]this Type type, string methodName, params object?[] parameters)
        {
            Debug.Assert(!type.IsValueType, $"{type}.{methodName} should be invoked by the Invoke(MethodInfo,...) overload to handle value type mutations on all platforms");
            Debug.Assert(!type.IsGenericTypeDefinition);

            return GetMethodByName(type, methodName)!.Invoke(null, parameters);
        }

        /// <summary>
        /// For unambiguous generic static methods by name.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [RequiresDynamicCode("GetStaticGenericMethodByName")]
        [RequiresUnreferencedCode("GetStaticGenericMethodByName")]
        internal static object? InvokeMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]this Type type,
            string methodName, Type genericArgument, params object?[] parameters)
            => GetStaticGenericMethodByName(type, genericArgument, methodName).Invoke(null, parameters);

        /// <summary>
        /// For static methods by name and parameter types.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [RequiresDynamicCode("GetStaticGenericMethodByTypes")]
        [RequiresUnreferencedCode("GetStaticGenericMethodByTypes")]
        internal static object? InvokeMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]this Type type,
            string methodName, Type[] genericArguments, Type[] parameterTypes, params object?[] parameters)
        {
            Debug.Assert(genericArguments.Length > 0, "For non-generic types use the other overload of InvokeMethod");
            Debug.Assert(parameterTypes.Length > 0, "For parameterless methods use the other overload of InvokeMethod");
            return GetStaticGenericMethodByTypes(type, methodName, genericArguments.ToTypesKey(), parameterTypes.ToTypesKey()).Invoke(null, parameters);
        }

        /// <summary>
        /// For static methods by name and parameter type.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [RequiresDynamicCode("InvokeMethod/GetStaticGenericMethodByTypes")]
        [RequiresUnreferencedCode("InvokeMethod/GetStaticGenericMethodByTypes")]
        internal static object? InvokeMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]this Type type,
            string methodName, Type genericArgument, Type parameterType, params object?[] parameters)
            => InvokeMethod(type, methodName, [genericArgument], [parameterType], parameters);

        /// <summary>
        /// For static methods by name and parameter types.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [RequiresDynamicCode("InvokeMethod/GetStaticGenericMethodByTypes")]
        [RequiresUnreferencedCode("InvokeMethod/GetStaticGenericMethodByTypes")]
        internal static object? InvokeMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]this Type type,
            string methodName, Type genericArgument, Type[] parameterTypes, params object?[] parameters)
            => InvokeMethod(type, methodName, [genericArgument], parameterTypes, parameters);

        /// <summary>
        /// For constructors by exact parameter types.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllConstructors)]this Type type,
            Type[] parameterTypes, params object?[] parameters)
        {
            Debug.Assert(parameterTypes.Length == parameters.Length);
            return GetConstructor(type, parameterTypes.ToTypesKey()).CreateInstance(parameters);
        }

        /// <summary>
        /// For constructors for exactly one parameter.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllConstructors)]this Type type, Type parameterType, object? parameter)
            => CreateInstance(type, [parameterType], parameter);

        /// <summary>
        /// For constructors with exactly one non-derived parameter.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllConstructors)]this Type type, object parameter)
            => CreateInstance(type, [parameter.GetType()], parameter);

        /// <summary>
        /// For constructors with non-derived parameters.
        /// </summary>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        internal static object CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMembers.AllConstructors)]this Type type, params object[] parameters)
            => CreateInstance(type, parameters.ToTypesKey().Types, parameters);

        /// <summary>
        /// Invokes a constructor on an already created instance.
        /// </summary>
        [RequiresUnreferencedCode("GetCtorMethod")]
        internal static bool TryInvokeCtor(object instance, params object[] ctorArgs)
        {
            ActionMethodAccessor? accessor = GetCtorMethod(instance.GetType(), ctorArgs);
            if (accessor == null)
                return false;

#if NETSTANDARD2_0
            ((MethodBase)accessor.MemberInfo).Invoke(instance, ctorArgs);
            return true;
#else
            accessor.Invoke(instance, ctorArgs);
            return true;
#endif
        }

#if NETFRAMEWORK || NETSTANDARD2_0
        private static object GetPointerPropertyPartiallyTrusted(PropertyInfo property, object? instance)
            => GetMethodByName(typeof(Pointer), "GetPointerValue")?.InvokeInstanceFunction<Pointer, object>((Pointer)property.GetValue(instance, null))!;
#endif

        private static TypesKey ToTypesKey(this Type[] types) => new TypesKey(types);

        private static TypesKey ToTypesKey(this object[] array)
        {
            Debug.Assert(array.All(i => i != null!), "No null element is expected array.");
            return array.Length == 0 ? TypesKey.Empty : new TypesKey(array.Select(i => i.GetType()).ToArray());
        }

        #endregion

        #endregion

        #endregion
    }
}
