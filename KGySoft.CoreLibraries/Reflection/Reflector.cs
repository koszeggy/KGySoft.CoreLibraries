#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Reflector.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
#if NETFRAMEWORK
using KGySoft.Reflection.WinApi; 
#endif

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Provides reflection routines on objects that are in most case faster than standard System.Reflection ways.
    /// </summary>
    [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "It is due to caching common types (see fields).")]
    public static class Reflector
    {
        #region Nested Classes

        private static class EmptyArrayHelper<T>
        {
            internal static readonly T[] Instance =
#if NET35 || NET40 || NET45
                new T[0];
#else
                Array.Empty<T>();
#endif
        }

        #endregion

        #region Fields

        #region Internal Fields

        internal static readonly object[] EmptyObjects = EmptyArrayHelper<object>.Instance;

        internal static readonly Type VoidType = typeof(void);

        internal static readonly Type ObjectType = typeof(object);
        internal static readonly Type BoolType = typeof(bool);
        internal static readonly Type StringType = typeof(string);
        internal static readonly Type CharType = typeof(char);
        internal static readonly Type ByteType = typeof(byte);
        internal static readonly Type SByteType = typeof(sbyte);
        internal static readonly Type ShortType = typeof(short);
        internal static readonly Type UShortType = typeof(ushort);
        internal static readonly Type IntType = typeof(int);
        internal static readonly Type UIntType = typeof(uint);
        internal static readonly Type LongType = typeof(long);
        internal static readonly Type ULongType = typeof(ulong);
        internal static readonly Type IntPtrType = typeof(IntPtr);
        internal static readonly Type UIntPtrType = typeof(UIntPtr);
        internal static readonly Type FloatType = typeof(float);
        internal static readonly Type DoubleType = typeof(double);
        internal static readonly Type DecimalType = typeof(decimal);
        internal static readonly Type TimeSpanType = typeof(TimeSpan);
        internal static readonly Type DateTimeType = typeof(DateTime);
        internal static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);

        internal static readonly Type ArrayType = typeof(Array);
        internal static readonly Type EnumType = typeof(Enum);
        internal static readonly Type DelegateType = typeof(Delegate);
        internal static readonly Type NullableType = typeof(Nullable<>);
        internal static readonly Type DictionaryEntryType = typeof(DictionaryEntry);
        internal static readonly Type KeyValuePairType = typeof(KeyValuePair<,>);

        // ReSharper disable InconsistentNaming
        internal static readonly Type IEnumerableType = typeof(IEnumerable);
        internal static readonly Type IEnumerableGenType = typeof(IEnumerable<>);
        internal static readonly Type IListType = typeof(IList);
        internal static readonly Type IDictionaryType = typeof(IDictionary);
        internal static readonly Type IDictionaryGenType = typeof(IDictionary<,>);
        internal static readonly Type ICollectionGenType = typeof(ICollection<>);
        internal static readonly Type IListGenType = typeof(IList<>);
        // ReSharper restore InconsistentNaming

        internal static readonly Type ByteArrayType = typeof(byte[]);
        internal static readonly Type ListGenType = typeof(List<>);
        internal static readonly Type BitArrayType = typeof(BitArray);
        internal static readonly Type StringCollectionType = typeof(StringCollection);
        internal static readonly Type DictionaryGenType = typeof(Dictionary<,>);

        internal static readonly Type Type = typeof(Type);
        // ReSharper disable once PossibleMistakenCallToGetType.2
        internal static readonly Type RuntimeType = Type.GetType();
#if !NET35 && !NET40
        internal static readonly Type TypeInfo = typeof(TypeInfo);
#endif

        internal static readonly Assembly SystemCoreLibrariesAssembly = ObjectType.Assembly;
        internal static readonly Assembly KGySoftCoreLibrariesAssembly = typeof(Reflector).Assembly;

        internal static readonly string SystemCoreLibrariesAssemblyName = SystemCoreLibrariesAssembly.FullName;

        #endregion

        #region Private Fields

        private static IThreadSafeCacheAccessor<Type, string> defaultMemberCache;
        private static LockingDictionary<string, Assembly> assemblyCache;
        private static LockingDictionary<string, Type> typeCacheByString;
        private static IThreadSafeCacheAccessor<Assembly, LockingDictionary<string, Type>> typeCacheByAssembly;
        private static bool? canCreateUninitializedObject;

        #endregion

        #endregion

        #region Properties

        private static IThreadSafeCacheAccessor<Type, string> DefaultMemberCache
            => defaultMemberCache ?? (defaultMemberCache = new Cache<Type, string>(GetDefaultMember).GetThreadSafeAccessor());

        private static LockingDictionary<string, Type> TypeCacheByString
            => typeCacheByString ?? (typeCacheByString = new Cache<string, Type>(256).AsThreadSafe());

        private static IThreadSafeCacheAccessor<Assembly, LockingDictionary<string, Type>> TypeCacheByAssembly
            => typeCacheByAssembly ?? (typeCacheByAssembly = new Cache<Assembly, LockingDictionary<string, Type>>(a => new Cache<string, Type>(64).AsThreadSafe()).GetThreadSafeAccessor(true)); // true because the inner creation is fast

        private static LockingDictionary<string, Assembly> AssemblyCache
            => assemblyCache ?? (assemblyCache = new Cache<string, Assembly>().AsThreadSafe());

        #endregion

        #region Methods

        #region SetProperty

        #region By PropertyInfo

        /// <summary>
        /// Sets a <paramref name="property"/> represented by the specified <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="instance">An instance whose property is about to be set. This parameter is ignored for static properties.</param>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="indexParameters">Index parameters if <paramref name="property"/> is an indexer. This parameter is ignored for non-indexed properties.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable here.</param>
        /// <remarks>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the property is an instance member of a value type (<see langword="struct"/>),
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// <note>To set the property explicitly by dynamically created delegates use the <see cref="PropertyAccessor"/> class.</note>
        /// </remarks>
        public static void SetProperty(object instance, PropertyInfo property, object value, ReflectionWays way, params object[] indexParameters)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property), Res.ArgumentNull);

            if (!property.CanWrite)
                throw new InvalidOperationException(Res.ReflectionPropertyHasNoSetter(property.DeclaringType, property.Name));
            bool isStatic = property.GetSetMethod(true).IsStatic;
            if (instance == null && !isStatic)
                throw new ArgumentNullException(nameof(instance), Res.ReflectionInstanceIsNull);

            switch (way)
            {
                case ReflectionWays.Auto:
#if NETSTANDARD2_0
                    if (!isStatic && property.DeclaringType?.IsValueType == true)
                        goto case ReflectionWays.SystemReflection;
                    else
                        goto case ReflectionWays.DynamicDelegate;
#endif
                case ReflectionWays.DynamicDelegate:
                    PropertyAccessor.GetAccessor(property).Set(instance, value, indexParameters);
                    break;
                case ReflectionWays.SystemReflection:
                    property.SetValue(instance, value, indexParameters);
                    break;
                case ReflectionWays.TypeDescriptor:
                    throw new NotSupportedException(Res.ReflectionSetPropertyTypeDescriptorNotSupported);
                default:
                    throw new ArgumentOutOfRangeException(nameof(way), Res.EnumOutOfRange(way));
            }
        }

        /// <summary>
        /// Sets a <paramref name="property"/> represented by the specified <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="instance">An instance whose property is about to be set. This parameter is ignored for static properties.</param>
        /// <param name="property">The property to set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="indexParameters">Index parameters if <paramref name="property"/> is an indexer. This parameter is ignored for non-indexed properties.</param>
        /// <remarks>
        /// <para>For setting the property this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the property is an instance member of a value type (<see langword="struct"/>),
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// <note>To set the property explicitly by dynamically created delegates use the <see cref="PropertyAccessor"/> class.</note>
        /// </remarks>
        public static void SetProperty(object instance, PropertyInfo property, object value, params object[] indexParameters)
            => SetProperty(instance, property, value, ReflectionWays.Auto, indexParameters);

        #endregion

        #region By Name

        /// <summary>
        /// Sets the instance property of an object represented by the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="instance">An instance whose property is about to be set.</param>
        /// <param name="propertyName">The name of the property to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="way">The preferred reflection way.</param>
        /// <param name="indexParameters">Index parameters if <paramref name="propertyName"/> refers to an indexed property. This parameter is ignored for non-indexed properties.</param>
        /// <remarks>
        /// <para><paramref name="propertyName"/> can refer public and non-public properties. To avoid ambiguity (in case of indexers), this method gets
        /// all of the properties of the same name and chooses the first one for which the provided <paramref name="indexParameters"/> match.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance use the <see cref="SetProperty(object,PropertyInfo,object,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a property with the specified <paramref name="propertyName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TrySetProperty">TrySetProperty</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.TypeDescriptor"/> way
        /// for <see cref="ICustomTypeDescriptor"/> implementations and the <see cref="ReflectionWays.DynamicDelegate"/> way otherwise.
        /// If the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the property belongs to a value type (<see langword="struct"/>),
        /// then the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static void SetProperty(object instance, string propertyName, object value, ReflectionWays way, params object[] indexParameters)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName), Res.ArgumentNull);
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);
            if (indexParameters == null)
                indexParameters = EmptyObjects;

            Type type = instance.GetType();
            DoTrySetProperty(propertyName, type, instance, value, way, indexParameters, true);
        }

        /// <summary>
        /// Sets the instance property of an object represented by the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="instance">An instance whose property is about to be set.</param>
        /// <param name="propertyName">The name of the property to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="indexParameters">Index parameters if <paramref name="propertyName"/> refers to an indexed property. This parameter is ignored for non-indexed properties.</param>
        /// <remarks>
        /// <para><paramref name="propertyName"/> can refer public and non-public properties. To avoid ambiguity (in case of indexers), this method gets
        /// all of the properties of the same name and chooses the first one for which the provided <paramref name="indexParameters"/> match.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance use the <see cref="SetProperty(object,PropertyInfo,object,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a property with the specified <paramref name="propertyName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TrySetProperty">TrySetProperty</see> methods instead.</para>
        /// <para>For setting the property this method uses the <see cref="ReflectionWays.TypeDescriptor"/> way
        /// for <see cref="ICustomTypeDescriptor"/> implementations and the <see cref="ReflectionWays.DynamicDelegate"/> way otherwise.
        /// If the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the property belongs to a value type (<see langword="struct"/>),
        /// then the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static void SetProperty(object instance, string propertyName, object value, params object[] indexParameters)
            => SetProperty(instance, propertyName, value, ReflectionWays.Auto, indexParameters);

        /// <summary>
        /// Sets the static property of a <see cref="System.Type"/> represented by the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static property belongs to.</param>
        /// <param name="propertyName">The name of the property to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for static properties. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <remarks>
        /// <para><paramref name="propertyName"/> can refer public and non-public properties.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance use the <see cref="SetProperty(object,PropertyInfo,object,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a property with the specified <paramref name="propertyName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TrySetProperty">TrySetProperty</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way.</para>
        /// </remarks>
        public static void SetProperty(Type type, string propertyName, object value, ReflectionWays way = ReflectionWays.Auto)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName), Res.ArgumentNull);
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            DoTrySetProperty(propertyName, type, null, value, way, EmptyObjects, true);
        }

        /// <summary>
        /// Tries to set the instance property of an object represented by the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="instance">An instance whose property is about to be set.</param>
        /// <param name="propertyName">The name of the property to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="way">The preferred reflection way.</param>
        /// <param name="indexParameters">Index parameters if <paramref name="propertyName"/> refers to an indexed property. This parameter is ignored for non-indexed properties.</param>
        /// <returns><see langword="true"/>, if the property could be set; <see langword="false"/>, if a matching property could not be found.</returns>
        /// <remarks>
        /// <note>If a matching property could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="propertyName"/> can refer public and non-public properties. To avoid ambiguity (in case of indexers), this method gets
        /// all of the properties of the same name and chooses the first one for which the provided <paramref name="indexParameters"/> match.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance use the <see cref="SetProperty(object,PropertyInfo,object,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.TypeDescriptor"/> way
        /// for <see cref="ICustomTypeDescriptor"/> implementations and the <see cref="ReflectionWays.DynamicDelegate"/> way otherwise.
        /// If the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the property belongs to a value type (<see langword="struct"/>),
        /// then the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static bool TrySetProperty(object instance, string propertyName, object value, ReflectionWays way, params object[] indexParameters)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName), Res.ArgumentNull);
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);
            if (indexParameters == null)
                indexParameters = EmptyObjects;

            Type type = instance.GetType();
            return DoTrySetProperty(propertyName, type, instance, value, way, indexParameters, false);
        }

        /// <summary>
        /// Tries to set the instance property of an object represented by the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="instance">An instance whose property is about to be set.</param>
        /// <param name="propertyName">The name of the property to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="indexParameters">Index parameters if <paramref name="propertyName"/> refers to an indexed property. This parameter is ignored for non-indexed properties.</param>
        /// <returns><see langword="true"/>, if the property could be set; <see langword="false"/>, if a matching property could not be found.</returns>
        /// <remarks>
        /// <note>If a matching property could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="propertyName"/> can refer public and non-public properties. To avoid ambiguity (in case of indexers), this method gets
        /// all of the properties of the same name and chooses the first one for which the provided <paramref name="indexParameters"/> match.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance use the <see cref="SetProperty(object,PropertyInfo,object,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>For setting the property this method uses the <see cref="ReflectionWays.TypeDescriptor"/> way
        /// for <see cref="ICustomTypeDescriptor"/> implementations and the <see cref="ReflectionWays.DynamicDelegate"/> way otherwise.
        /// If the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the property belongs to a value type (<see langword="struct"/>),
        /// then the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static bool TrySetProperty(object instance, string propertyName, object value, params object[] indexParameters)
            => TrySetProperty(instance, propertyName, value, ReflectionWays.Auto, indexParameters);

        /// <summary>
        /// Tries to set the static property of a <see cref="System.Type"/> represented by the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static property belongs to.</param>
        /// <param name="propertyName">The name of the property to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for static properties. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <returns><see langword="true"/>, if the property could be set; <see langword="false"/>, if a matching property could not be found.</returns>
        /// <remarks>
        /// <note>If a matching property could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="propertyName"/> can refer public and non-public properties.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance use the <see cref="SetProperty(object,PropertyInfo,object,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way.</para>
        /// </remarks>
        public static bool TrySetProperty(Type type, string propertyName, object value, ReflectionWays way = ReflectionWays.Auto)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName), Res.ArgumentNull);
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            return DoTrySetProperty(propertyName, type, null, value, way, EmptyObjects, false);
        }

        private static bool DoTrySetProperty(string propertyName, Type type, object instance, object value, ReflectionWays way, object[] indexParameters, bool throwError)
        {
            // type descriptor
            if (way == ReflectionWays.TypeDescriptor || (way == ReflectionWays.Auto && instance is ICustomTypeDescriptor && (indexParameters == null || indexParameters.Length == 0)))
                return DoTrySetPropertyByTypeDescriptor(propertyName, type, instance, value, throwError);

            Exception lastException = null;
            for (Type checkedType = type; checkedType.BaseType != null; checkedType = checkedType.BaseType)
            {
                BindingFlags flags = type == checkedType ? BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy : BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                flags |= instance == null ? BindingFlags.Static : BindingFlags.Instance;
                MemberInfo[] properties = checkedType.GetMember(propertyName, MemberTypes.Property, flags);
                bool checkParams = properties.Length > 1; // for performance reasons we skip checking parameters if there is only one property of the given name

                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop - properties are queried
                foreach (PropertyInfo property in properties)
                {
                    ParameterInfo[] indexParams = checkParams ? property.GetIndexParameters() : null;

                    if (checkParams && !CheckParameters(indexParams, indexParameters))
                        continue;

                    if (!throwError && !property.PropertyType.CanAcceptValue(value))
                        return false;

                    try
                    {
                        SetProperty(instance, property, value, way, indexParameters);
                        return true;
                    }
                    catch (TargetInvocationException e)
                    {
                        if (e.InnerException == null)
                            throw;
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    }
                    catch (Exception e) when (!checkParams && !e.IsCritical())
                    {
                        // if parameters check was omitted and the error is due to incorrect parameters we skip the property
                        if (!CheckParameters(property.GetIndexParameters(), indexParameters))
                        {
                            lastException = e;
                            continue;
                        }

                        throw;
                    }
                }
            }

            if (!throwError)
                return false;

            if (instance == null)
                throw new ReflectionException(Res.ReflectionStaticPropertyDoesNotExist(propertyName, type), lastException);
            throw new ReflectionException(Res.ReflectionInstancePropertyDoesNotExist(propertyName, type), lastException);
        }

        private static bool DoTrySetPropertyByTypeDescriptor(string propertyName, Type type, object instance, object value, bool throwError)
        {
            if (instance == null)
                throw new NotSupportedException(Res.ReflectionCannotSetStaticPropertyTypeDescriptor);
            PropertyDescriptor property = TypeDescriptor.GetProperties(instance)[propertyName];
            if (property != null)
            {
                if (!throwError && !property.PropertyType.CanAcceptValue(value))
                    return false;
                property.SetValue(instance, value);
                return true;
            }

            return throwError ? throw new ReflectionException(Res.ReflectionPropertyNotFoundTypeDescriptor(propertyName, type)) : false;
        }

        #endregion

        #region By Indexer

        /// <summary>
        /// Sets the value of an indexable object. It can be either an array instance or an object with default members (indexed properties).
        /// </summary>
        /// <param name="instance">An instance to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable here. This parameter is ignored for arrays.</param>
        /// <param name="indexParameters">The index parameters.</param>
        /// <remarks>
        /// <para>This method ignores explicitly implemented interface properties.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance of the indexed property, then use the <see cref="SetProperty(object,PropertyInfo,object,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the indexer belongs to a value type (<see langword="struct"/>),
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static void SetIndexedMember(object instance, object value, ReflectionWays way, params object[] indexParameters)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);
            if (indexParameters == null)
                throw new ArgumentNullException(nameof(indexParameters), Res.ArgumentNull);
            if (indexParameters.Length == 0)
                throw new ArgumentException(Res.ReflectionEmptyIndices, nameof(indexParameters));
            if (way == ReflectionWays.TypeDescriptor)
                throw new NotSupportedException(Res.ReflectionSetIndexerTypeDescriptorNotSupported);

            DoTrySetIndexedMember(instance, value, way, indexParameters, true);
        }

        /// <summary>
        /// Sets the value of an indexable object. It can be either an array instance or an object with default members (indexed properties).
        /// </summary>
        /// <param name="instance">An instance to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="indexParameters">The index parameters.</param>
        /// <remarks>
        /// <para>This method ignores explicitly implemented interface properties.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance of the indexed property, then use the <see cref="SetProperty(object,PropertyInfo,object,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>For setting an indexed property this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the indexer belongs to a value type (<see langword="struct"/>),
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static void SetIndexedMember(object instance, object value, params object[] indexParameters)
            => SetIndexedMember(instance, value, ReflectionWays.Auto, indexParameters);

        /// <summary>
        /// Tries to set the value of an indexable object. It can be either an array or an object with default members (indexed properties).
        /// </summary>
        /// <param name="instance">An instance to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable here. This parameter is ignored for arrays.</param>
        /// <param name="indexParameters">The index parameters.</param>
        /// <returns><see langword="true"/>, if the indexed member could be set; <see langword="false"/>, if a matching property or array setter could not be found.</returns>
        /// <remarks>
        /// <para>This method ignores explicitly implemented interface properties.</para>
        /// <note>If a matching indexed property could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance of the indexed property, then use the <see cref="SetProperty(object,PropertyInfo,object,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the indexer belongs to a value type (<see langword="struct"/>),
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static bool TrySetIndexedMember(object instance, object value, ReflectionWays way, params object[] indexParameters)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);
            if (indexParameters == null)
                throw new ArgumentNullException(nameof(indexParameters), Res.ArgumentNull);
            if (indexParameters.Length == 0)
                throw new ArgumentException(Res.ReflectionEmptyIndices, nameof(indexParameters));
            if (way == ReflectionWays.TypeDescriptor)
                throw new NotSupportedException(Res.ReflectionSetIndexerTypeDescriptorNotSupported);

            return DoTrySetIndexedMember(instance, value, way, indexParameters, false);
        }

        /// <summary>
        /// Tries to set the value of an indexable object. It can be either an array or an object with default members (indexed properties).
        /// </summary>
        /// <param name="instance">An instance to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="indexParameters">The index parameters.</param>
        /// <returns><see langword="true"/>, if the indexed member could be set; <see langword="false"/>, if a matching property or array setter could not be found.</returns>
        /// <remarks>
        /// <para>This method ignores explicitly implemented interface properties.</para>
        /// <note>If a matching indexed property could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance of the indexed property, then use the <see cref="SetProperty(object,PropertyInfo,object,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>For setting an indexed property this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the indexer belongs to a value type (<see langword="struct"/>),
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static bool TrySetIndexedMember(object instance, object value, params object[] indexParameters)
            => TrySetIndexedMember(instance, value, ReflectionWays.Auto, indexParameters);

        private static bool DoTrySetIndexedMember(object instance, object value, ReflectionWays way, object[] indexParameters, bool throwError)
        {
            // Arrays
            if (instance is Array array)
            {
                if (array.Rank != indexParameters.Length)
                {
                    if (!throwError)
                        return false;
                    throw new ArgumentException(Res.ReflectionIndexParamsLengthMismatch(array.Rank), nameof(indexParameters));
                }

                int[] indices = ToArrayIndices(indexParameters, out Exception error);
                if (indices == null)
                    return throwError ? throw error : false;

                array.SetValue(value, indices);
                return true;
            }

            // Real indexers
            Exception lastException = null;
            Type type = instance.GetType();
            for (Type checkedType = type; checkedType != null; checkedType = checkedType.BaseType)
            {
                string defaultMemberName = DefaultMemberCache[checkedType];
                if (String.IsNullOrEmpty(defaultMemberName))
                    continue;

                MemberInfo[] indexers = checkedType.GetMember(defaultMemberName, MemberTypes.Property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                bool checkParams = indexers.Length > 1; // for performance reasons we skip checking parameters if there is only one indexer

                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop - properties are queried
                foreach (PropertyInfo indexer in indexers)
                {
                    ParameterInfo[] indexParams = checkParams ? indexer.GetIndexParameters() : null;

                    if (checkParams && !CheckParameters(indexParams, indexParameters))
                        continue;

                    if (!throwError && !indexer.PropertyType.CanAcceptValue(value))
                        return false;

                    try
                    {
                        SetProperty(instance, indexer, value, way, indexParameters);
                        return true;
                    }
                    catch (TargetInvocationException e)
                    {
                        if (e.InnerException == null)
                            throw;
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    }
                    catch (Exception e) when (!checkParams && !e.IsCritical())
                    {
                        // if parameters check was omitted and the error is due to incorrect parameters we skip the indexer
                        if (!CheckParameters(indexer.GetIndexParameters(), indexParameters))
                        {
                            lastException = e;
                            continue;
                        }

                        throw;
                    }
                }
            }

            if (!throwError)
                return false;
            throw new ReflectionException(Res.ReflectionIndexerNotFound(type), lastException);
        }

        #endregion

        #endregion

        #region GetProperty

        #region By PropertyInfo

        /// <summary>
        /// Gets a <paramref name="property"/> represented by the specified <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="instance">An instance whose property is about to be retrieved. This parameter is ignored for static properties.</param>
        /// <param name="property">The property to get.</param>
        /// <param name="indexParameters">Index parameters if <paramref name="property"/> is an indexer. This parameter is ignored for non-indexed properties.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable here.</param>
        /// <returns>The value of the property.</returns>
        /// <remarks>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used.</para>
        /// <note>To get the property explicitly by dynamically created delegates use the <see cref="PropertyAccessor"/> class.</note>
        /// </remarks>
        public static object GetProperty(object instance, PropertyInfo property, ReflectionWays way, params object[] indexParameters)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property), Res.ArgumentNull);

            if (!property.CanRead)
                throw new InvalidOperationException(Res.ReflectionPropertyHasNoGetter(property.DeclaringType, property.Name));
            if (instance == null && !property.GetGetMethod(true).IsStatic)
                throw new ArgumentNullException(nameof(instance), Res.ReflectionInstanceIsNull);

            switch (way)
            {
                case ReflectionWays.Auto:
                case ReflectionWays.DynamicDelegate:
                    return PropertyAccessor.GetAccessor(property).Get(instance, indexParameters);
                case ReflectionWays.SystemReflection:
                    return property.GetValue(instance, indexParameters);
                case ReflectionWays.TypeDescriptor:
                    throw new NotSupportedException(Res.ReflectionGetPropertyTypeDescriptorNotSupported);
                default:
                    throw new ArgumentOutOfRangeException(nameof(way), Res.EnumOutOfRange(way));
            }
        }

        /// <summary>
        /// Gets a <paramref name="property"/> represented by the specified <see cref="PropertyInfo"/>.
        /// </summary>
        /// <param name="instance">An instance whose property is about to be retrieved. This parameter is ignored for static properties.</param>
        /// <param name="property">The property to get.</param>
        /// <param name="indexParameters">Index parameters if <paramref name="property"/> is an indexer. This parameter is ignored for non-indexed properties.</param>
        /// <returns>The value of the property.</returns>
        /// <remarks>
        /// <para>For getting the property this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way.</para>
        /// <note>To get the property explicitly by dynamically created delegates use the <see cref="PropertyAccessor"/> class.</note>
        /// </remarks>
        public static object GetProperty(object instance, PropertyInfo property, params object[] indexParameters)
            => GetProperty(instance, property, ReflectionWays.Auto, indexParameters);

        #endregion

        #region By Name

        /// <summary>
        /// Gets the instance property of an object represented by the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="instance">An instance whose property is about to be retrieved.</param>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="way">The preferred reflection way.</param>
        /// <param name="indexParameters">Index parameters if <paramref name="propertyName"/> refers to an indexed property. This parameter is ignored for non-indexed properties.</param>
        /// <returns>The value of the property.</returns>
        /// <remarks>
        /// <para><paramref name="propertyName"/> can refer public and non-public properties. To avoid ambiguity (in case of indexers), this method gets
        /// all of the properties of the same name and chooses the first one for which the provided <paramref name="indexParameters"/> match.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance use the <see cref="GetProperty(object,PropertyInfo,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a property with the specified <paramref name="propertyName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryGetProperty">TryGetProperty</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.TypeDescriptor"/> way
        /// for <see cref="ICustomTypeDescriptor"/> implementations and the <see cref="ReflectionWays.DynamicDelegate"/> way otherwise.</para>
        /// </remarks>
        public static object GetProperty(object instance, string propertyName, ReflectionWays way, params object[] indexParameters)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName), Res.ArgumentNull);
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);
            if (indexParameters == null)
                indexParameters = EmptyObjects;

            Type type = instance.GetType();
            return DoTryGetProperty(propertyName, type, instance, way, indexParameters, true, out object result) ? result : null;
        }

        /// <summary>
        /// Gets the instance property of an object represented by the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="instance">An instance whose property is about to be retrieved.</param>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="indexParameters">Index parameters if <paramref name="propertyName"/> refers to an indexed property. This parameter is ignored for non-indexed properties.</param>
        /// <returns>The value of the property.</returns>
        /// <remarks>
        /// <para><paramref name="propertyName"/> can refer public and non-public properties. To avoid ambiguity (in case of indexers), this method gets
        /// all of the properties of the same name and chooses the first one for which the provided <paramref name="indexParameters"/> match.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance use the <see cref="GetProperty(object,PropertyInfo,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a property with the specified <paramref name="propertyName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryGetProperty">TryGetProperty</see> methods instead.</para>
        /// <para>For getting the property this method uses the <see cref="ReflectionWays.TypeDescriptor"/> way
        /// for <see cref="ICustomTypeDescriptor"/> implementations and the <see cref="ReflectionWays.DynamicDelegate"/> way otherwise.</para>
        /// </remarks>
        public static object GetProperty(object instance, string propertyName, params object[] indexParameters)
            => GetProperty(instance, propertyName, ReflectionWays.Auto, indexParameters);

        /// <summary>
        /// Gets the static property of a <see cref="System.Type"/> represented by the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static property belongs to.</param>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for static properties. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <returns>The value of the property.</returns>
        /// <remarks>
        /// <para><paramref name="propertyName"/> can refer public and non-public properties.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance use the <see cref="GetProperty(object,PropertyInfo,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a property with the specified <paramref name="propertyName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryGetProperty">TryGetProperty</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way.</para>
        /// </remarks>
        public static object GetProperty(Type type, string propertyName, ReflectionWays way = ReflectionWays.Auto)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName), Res.ArgumentNull);
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            return DoTryGetProperty(propertyName, type, null, way, EmptyObjects, true, out object result) ? result : null;
        }

        /// <summary>
        /// Tries to get the instance property of an object represented by the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="instance">An instance whose property is about to be retrieved.</param>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the value of the property.</param>
        /// <param name="way">The preferred reflection way.</param>
        /// <param name="indexParameters">Index parameters if <paramref name="propertyName"/> refers to an indexed property. This parameter is ignored for non-indexed properties.</param>
        /// <returns><see langword="true"/>, if the property could be read; <see langword="false"/>, if a matching property could not be found.</returns>
        /// <remarks>
        /// <note>If a matching property could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="propertyName"/> can refer public and non-public properties. To avoid ambiguity (in case of indexers), this method gets
        /// all of the properties of the same name and chooses the first one for which the provided <paramref name="indexParameters"/> match.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance use the <see cref="GetProperty(object,PropertyInfo,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.TypeDescriptor"/> way
        /// for <see cref="ICustomTypeDescriptor"/> implementations and the <see cref="ReflectionWays.DynamicDelegate"/> way otherwise.</para>
        /// </remarks>
        public static bool TryGetProperty(object instance, string propertyName, ReflectionWays way, out object value, params object[] indexParameters)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName), Res.ArgumentNull);
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);
            if (indexParameters == null)
                indexParameters = EmptyObjects;

            Type type = instance.GetType();
            return DoTryGetProperty(propertyName, type, instance, way, indexParameters, false, out value);
        }

        /// <summary>
        /// Tries to get the instance property of an object represented by the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="instance">An instance whose property is about to be retrieved.</param>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the value of the property.</param>
        /// <param name="indexParameters">Index parameters if <paramref name="propertyName"/> refers to an indexed property. This parameter is ignored for non-indexed properties.</param>
        /// <returns><see langword="true"/>, if the property could be read; <see langword="false"/>, if a matching property could not be found.</returns>
        /// <remarks>
        /// <note>If a matching property could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="propertyName"/> can refer public and non-public properties. To avoid ambiguity (in case of indexers), this method gets
        /// all of the properties of the same name and chooses the first one for which the provided <paramref name="indexParameters"/> match.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance use the <see cref="GetProperty(object,PropertyInfo,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>For getting the property this method uses the <see cref="ReflectionWays.TypeDescriptor"/> way
        /// for <see cref="ICustomTypeDescriptor"/> implementations and the <see cref="ReflectionWays.DynamicDelegate"/> way otherwise.</para>
        /// </remarks>
        public static bool TryGetProperty(object instance, string propertyName, out object value, params object[] indexParameters)
            => TryGetProperty(instance, propertyName, ReflectionWays.Auto, out value, indexParameters);

        /// <summary>
        /// Tries to get the static property of a <see cref="System.Type"/> represented by the specified <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static property belongs to.</param>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the value of the property.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for static properties. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <returns><see langword="true"/>, if the property could be read; <see langword="false"/>, if a matching property could not be found.</returns>
        /// <remarks>
        /// <note>If a matching property could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="propertyName"/> can refer public and non-public properties.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance use the <see cref="GetProperty(object,PropertyInfo,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way.</para>
        /// </remarks>
        public static bool TryGetProperty(Type type, string propertyName, out object value, ReflectionWays way = ReflectionWays.Auto)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName), Res.ArgumentNull);
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            return DoTryGetProperty(propertyName, type, null, way, EmptyObjects, false, out value);
        }

        private static bool DoTryGetProperty(string propertyName, Type type, object instance, ReflectionWays way, object[] indexParameters, bool throwError, out object value)
        {
            value = null;

            // type descriptor
            if (way == ReflectionWays.TypeDescriptor || (way == ReflectionWays.Auto && instance is ICustomTypeDescriptor && (indexParameters == null || indexParameters.Length == 0)))
            {
                if (instance == null)
                    throw new NotSupportedException(Res.ReflectionCannotGetStaticPropertyTypeDescriptor);
                PropertyDescriptor property = TypeDescriptor.GetProperties(instance)[propertyName];
                if (property != null)
                {
                    value = property.GetValue(instance);
                    return true;
                }

                return throwError ? throw new ReflectionException(Res.ReflectionCannotGetPropertyTypeDescriptor(propertyName, type)) : false;
            }

            Exception lastException = null;
            for (Type checkedType = type; checkedType.BaseType != null; checkedType = checkedType.BaseType)
            {
                BindingFlags flags = type == checkedType ? BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy : BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                flags |= instance == null ? BindingFlags.Static : BindingFlags.Instance;
                MemberInfo[] properties = checkedType.GetMember(propertyName, MemberTypes.Property, flags);
                bool checkParams = properties.Length > 1; // for performance reasons we skip checking parameters if there is only one property of the given name

                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop - properties are queried
                foreach (PropertyInfo property in properties)
                {
                    ParameterInfo[] indexParams = checkParams ? property.GetIndexParameters() : null;

                    if (checkParams && !CheckParameters(indexParams, indexParameters))
                        continue;

                    try
                    {
                        value = GetProperty(instance, property, way, indexParameters);
                        return true;
                    }
                    catch (TargetInvocationException e)
                    {
                        if (e.InnerException == null)
                            throw;
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    }
                    catch (Exception e) when (!checkParams && !e.IsCritical())
                    {
                        // if parameters check was omitted and the error is due to incorrect parameters we skip the property
                        if (!CheckParameters(property.GetIndexParameters(), indexParameters))
                        {
                            lastException = e;
                            continue;
                        }

                        throw;
                    }
                }
            }

            if (!throwError)
                return false;

            if (instance == null)
                throw new ReflectionException(Res.ReflectionStaticPropertyDoesNotExist(propertyName, type), lastException);
            throw new ReflectionException(Res.ReflectionInstancePropertyDoesNotExist(propertyName, type), lastException);
        }

        #endregion

        #region By Indexer

        /// <summary>
        /// Gets the value of an indexable object. It can be either an array instance or an object with default members (indexed properties).
        /// </summary>
        /// <param name="instance">An instance to be read.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable here. This parameter is ignored for arrays.</param>
        /// <param name="indexParameters">The index parameters.</param>
        /// <returns>The value returned by the indexable object.</returns>
        /// <remarks>
        /// <para>This method ignores explicitly implemented interface properties.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance of the indexed property, then use the <see cref="GetProperty(object,PropertyInfo,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way.</para>
        /// </remarks>
        public static object GetIndexedMember(object instance, ReflectionWays way, params object[] indexParameters)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);
            if (indexParameters == null)
                throw new ArgumentNullException(nameof(indexParameters), Res.ArgumentNull);
            if (indexParameters.Length == 0)
                throw new ArgumentException(Res.ReflectionEmptyIndices, nameof(indexParameters));
            if (way == ReflectionWays.TypeDescriptor)
                throw new NotSupportedException(Res.ReflectionGetIndexerTypeDescriptorNotSupported);

            return DoTryGetIndexedMember(instance, way, indexParameters, true, out object result) ? result : null;
        }

        /// <summary>
        /// Gets the value of an indexable object. It can be either an array instance or an object with default members (indexed properties).
        /// </summary>
        /// <param name="instance">An instance to be read.</param>
        /// <param name="indexParameters">The index parameters.</param>
        /// <returns>The value returned by the indexable object.</returns>
        /// <remarks>
        /// <para>This method ignores explicitly implemented interface properties.</para>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance of the indexed property, then use the <see cref="GetProperty(object,PropertyInfo,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>For getting an indexed property this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way.</para>
        /// </remarks>
        public static object GetIndexedMember(object instance, params object[] indexParameters)
            => GetIndexedMember(instance, ReflectionWays.Auto, indexParameters);

        /// <summary>
        /// Tries to get the value of an indexable object. It can be either an array or an object with default members (indexed properties).
        /// </summary>
        /// <param name="instance">An instance to be set.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable here. This parameter is ignored for arrays.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the value returned by the indexable object.</param>
        /// <param name="indexParameters">The index parameters.</param>
        /// <returns><see langword="true"/>, if the indexed member could be read; <see langword="false"/>, if a matching property or array getter could not be found.</returns>
        /// <remarks>
        /// <para>This method ignores explicitly implemented interface properties.</para>
        /// <note>If a matching indexed property could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance of the indexed property, then use the <see cref="GetProperty(object,PropertyInfo,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way.</para>
        /// </remarks>
        public static bool TryGetIndexedMember(object instance, ReflectionWays way, out object value, params object[] indexParameters)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);
            if (indexParameters == null)
                throw new ArgumentNullException(nameof(indexParameters), Res.ArgumentNull);
            if (indexParameters.Length == 0)
                throw new ArgumentException(Res.ReflectionEmptyIndices, nameof(indexParameters));
            if (way == ReflectionWays.TypeDescriptor)
                throw new NotSupportedException(Res.ReflectionGetIndexerTypeDescriptorNotSupported);

            return DoTryGetIndexedMember(instance, way, indexParameters, true, out value);
        }

        /// <summary>
        /// Tries to get the value of an indexable object. It can be either an array or an object with default members (indexed properties).
        /// </summary>
        /// <param name="instance">An instance to be set.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the value returned by the indexable object.</param>
        /// <param name="indexParameters">The index parameters.</param>
        /// <returns><see langword="true"/>, if the indexed member could be read; <see langword="false"/>, if a matching property or array getter could not be found.</returns>
        /// <remarks>
        /// <para>This method ignores explicitly implemented interface properties.</para>
        /// <note>If a matching indexed property could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para>If you already have a <see cref="PropertyInfo"/> instance of the indexed property, then use the <see cref="GetProperty(object,PropertyInfo,ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>For getting an indexed property this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way.</para>
        /// </remarks>
        public static bool TryGetIndexedMember(object instance, out object value, params object[] indexParameters)
            => TryGetIndexedMember(instance, ReflectionWays.Auto, out value, indexParameters);

        private static bool DoTryGetIndexedMember(object instance, ReflectionWays way, object[] indexParameters, bool throwError, out object value)
        {
            value = null;

            // Arrays
            if (instance is Array array)
            {
                if (array.Rank != indexParameters.Length)
                {
                    if (!throwError)
                        return false;
                    throw new ArgumentException(Res.ReflectionIndexParamsLengthMismatch(array.Rank), nameof(indexParameters));
                }

                int[] indices = ToArrayIndices(indexParameters, out Exception error);
                if (indices == null)
                    return throwError ? throw error : false;

                value = array.GetValue(indices);
                return true;
            }

            // Real indexers
            Exception lastException = null;
            Type type = instance.GetType();
            for (Type checkedType = type; checkedType != null; checkedType = checkedType.BaseType)
            {
                string defaultMemberName = DefaultMemberCache[checkedType];
                if (String.IsNullOrEmpty(defaultMemberName))
                    continue;

                MemberInfo[] indexers = checkedType.GetMember(defaultMemberName, MemberTypes.Property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                bool checkParams = indexers.Length > 1; // for performance reasons we skip checking parameters if there is only one indexer

                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop - properties are queried
                foreach (PropertyInfo indexer in indexers)
                {
                    ParameterInfo[] indexParams = checkParams ? indexer.GetIndexParameters() : null;

                    if (checkParams && !CheckParameters(indexParams, indexParameters))
                        continue;

                    try
                    {
                        value = GetProperty(instance, indexer, way, indexParameters);
                        return true;
                    }
                    catch (TargetInvocationException e)
                    {
                        if (e.InnerException == null)
                            throw;
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    }
                    catch (Exception e) when (!checkParams && !e.IsCritical())
                    {
                        // if parameters check was omitted and the error is due to incorrect parameters we skip the indexer
                        if (!CheckParameters(indexer.GetIndexParameters(), indexParameters))
                        {
                            lastException = e;
                            continue;
                        }

                        throw;
                    }
                }
            }

            if (!throwError)
                return false;
            throw new ReflectionException(Res.ReflectionIndexerNotFound(type), lastException);
        }

        #endregion

        #endregion

        #region InvokeMethod

        #region By MethodInfo

        /// <summary>
        /// Invokes a <paramref name="method"/> represented by the specified <see cref="MethodInfo"/>.
        /// </summary>
        /// <param name="instance">An instance whose method is about to be invoked. This parameter is ignored for static methods.</param>
        /// <param name="method">The method to be invoked.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="method"/> is a generic method definition. Otherwise, this parameter is ignored.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for invoking methods.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method is an instance member of a value type (<see langword="struct"/>) or has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// <note>To invoke the method explicitly by dynamically created delegates use the <see cref="MethodAccessor"/> class.</note>
        /// </remarks>
        public static object InvokeMethod(object instance, MethodInfo method, Type[] genericParameters, ReflectionWays way, params object[] parameters)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method), Res.ArgumentNull);
            if (instance == null && !method.IsStatic)
                throw new ArgumentNullException(nameof(instance), Res.ReflectionInstanceIsNull);

            // if the method is generic we need the generic arguments and a constructed method with real types
            if (method.IsGenericMethodDefinition)
            {
                if (genericParameters == null)
                    throw new ArgumentNullException(nameof(genericParameters), Res.ReflectionTypeParamsAreNull);
                Type[] genArgs = method.GetGenericArguments();
                if (genericParameters.Length != genArgs.Length)
                    throw new ArgumentException(Res.ReflectionTypeArgsLengthMismatch(genArgs.Length), nameof(genericParameters));
                try
                {
                    method = method.GetGenericMethod(genericParameters);
                }
                catch (Exception e)
                {
                    throw new ReflectionException(Res.ReflectionCannotCreateGenericMethod, e);
                }
            }

            switch (way)
            {
                case ReflectionWays.Auto:
#if NETSTANDARD2_0
                    if (!method.IsStatic && method.DeclaringType?.IsValueType == true || method.GetParameters().Any(p => p.ParameterType.IsByRef))
                        goto case ReflectionWays.SystemReflection;
                    else
                        goto case ReflectionWays.DynamicDelegate;
#endif
                case ReflectionWays.DynamicDelegate:
                    return MethodAccessor.GetAccessor(method).Invoke(instance, parameters);
                case ReflectionWays.SystemReflection:
                    return method.Invoke(instance, parameters);
                case ReflectionWays.TypeDescriptor:
                    throw new NotSupportedException(Res.ReflectionInvokeMethodTypeDescriptorNotSupported);
                default:
                    throw new ArgumentOutOfRangeException(nameof(way), Res.EnumOutOfRange(way));
            }
        }

        /// <summary>
        /// Invokes a <paramref name="method"/> represented by the specified <see cref="MethodInfo"/>.
        /// </summary>
        /// <param name="instance">An instance whose method is about to be invoked. This parameter is ignored for static methods.</param>
        /// <param name="method">The method to be invoked.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="method"/> is a generic method definition. Otherwise, this parameter is ignored.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para>For invoking the <paramref name="method"/> this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method is an instance member of a value type (<see langword="struct"/>) or has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// <note>To invoke the method explicitly by dynamically created delegates use the <see cref="MethodAccessor"/> class.</note>
        /// </remarks>
        public static object InvokeMethod(object instance, MethodInfo method, Type[] genericParameters, params object[] parameters)
            => InvokeMethod(instance, method, genericParameters, ReflectionWays.Auto, parameters);

        /// <summary>
        /// Invokes a <paramref name="method"/> represented by the specified <see cref="MethodInfo"/>.
        /// </summary>
        /// <param name="instance">An instance whose method is about to be invoked. This parameter is ignored for static methods.</param>
        /// <param name="method">The method to be invoked.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for invoking methods.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method is an instance member of a value type (<see langword="struct"/>) or has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// <note>To invoke the method explicitly by dynamically created delegates use the <see cref="MethodAccessor"/> class.</note>
        /// </remarks>
        public static object InvokeMethod(object instance, MethodInfo method, ReflectionWays way, params object[] parameters)
            => InvokeMethod(instance, method, null, way, parameters);

        /// <summary>
        /// Invokes a <paramref name="method"/> represented by the specified <see cref="MethodInfo"/>.
        /// </summary>
        /// <param name="instance">An instance whose method is about to be invoked. This parameter is ignored for static methods.</param>
        /// <param name="method">The method to be invoked.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para>For invoking the <paramref name="method"/> this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method is an instance member of a value type (<see langword="struct"/>) or has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// <note>To invoke the method explicitly by dynamically created delegates use the <see cref="MethodAccessor"/> class.</note>
        /// </remarks>
        public static object InvokeMethod(object instance, MethodInfo method, params object[] parameters)
            => InvokeMethod(instance, method, null, ReflectionWays.Auto, parameters);

        #endregion

        #region By Name

        /// <summary>
        /// Invokes an instance method of an object represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="instance">An instance whose method is about to be invoked.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="methodName"/> refers to a generic method definition. Otherwise, this parameter is ignored.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for invoking methods.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="genericParameters"/> and <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a method with the specified <paramref name="methodName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryInvokeMethod">TryInvokeMethod</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method belongs to a value type (<see langword="struct"/>) or has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static object InvokeMethod(object instance, string methodName, Type[] genericParameters, ReflectionWays way, params object[] parameters)
        {
            if (methodName == null)
                throw new ArgumentNullException(nameof(methodName), Res.ArgumentNull);
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);

            return DoTryInvokeMethod(methodName, instance.GetType(), instance, parameters ?? EmptyObjects, genericParameters ?? Type.EmptyTypes, way, true, out object result) ? result : null;
        }

        /// <summary>
        /// Invokes an instance method of an object represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="instance">An instance whose method is about to be invoked.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="methodName"/> refers to a generic method definition. Otherwise, this parameter is ignored.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="genericParameters"/> and <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a method with the specified <paramref name="methodName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryInvokeMethod">TryInvokeMethod</see> methods instead.</para>
        /// <para>For invoking the method this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method belongs to a value type (<see langword="struct"/>) or has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static object InvokeMethod(object instance, string methodName, Type[] genericParameters, params object[] parameters)
            => InvokeMethod(instance, methodName, genericParameters, ReflectionWays.Auto, parameters);

        /// <summary>
        /// Invokes an instance method of an object represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="instance">An instance whose method is about to be invoked.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for invoking methods.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a method with the specified <paramref name="methodName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryInvokeMethod">TryInvokeMethod</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method belongs to a value type (<see langword="struct"/>) or has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static object InvokeMethod(object instance, string methodName, ReflectionWays way, params object[] parameters)
            => InvokeMethod(instance, methodName, null, way, parameters);

        /// <summary>
        /// Invokes an instance method of an object represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="instance">An instance whose method is about to be invoked.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a method with the specified <paramref name="methodName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryInvokeMethod">TryInvokeMethod</see> methods instead.</para>
        /// <para>For invoking the method this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method belongs to a value type (<see langword="struct"/>) or has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static object InvokeMethod(object instance, string methodName, params object[] parameters)
            => InvokeMethod(instance, methodName, null, ReflectionWays.Auto, parameters);

        /// <summary>
        /// Invokes a static method of a <see cref="System.Type"/> represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static method belongs to.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="methodName"/> refers to a generic method definition. Otherwise, this parameter is ignored.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for invoking methods.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="genericParameters"/> and <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a method with the specified <paramref name="methodName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryInvokeMethod">TryInvokeMethod</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static object InvokeMethod(Type type, string methodName, Type[] genericParameters, ReflectionWays way, params object[] parameters)
        {
            if (methodName == null)
                throw new ArgumentNullException(nameof(methodName), Res.ArgumentNull);
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            return DoTryInvokeMethod(methodName, type, null, parameters ?? EmptyObjects, genericParameters ?? Type.EmptyTypes, way, true, out object result) ? result : null;
        }

        /// <summary>
        /// Invokes a static method of a <see cref="System.Type"/> represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static method belongs to.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="methodName"/> refers to a generic method definition. Otherwise, this parameter is ignored.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="genericParameters"/> and <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a method with the specified <paramref name="methodName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryInvokeMethod">TryInvokeMethod</see> methods instead.</para>
        /// <para>For invoking the method this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static object InvokeMethod(Type type, string methodName, Type[] genericParameters, params object[] parameters)
            => InvokeMethod(type, methodName, genericParameters, ReflectionWays.Auto, parameters);

        /// <summary>
        /// Invokes a static method of a <see cref="System.Type"/> represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static method belongs to.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for invoking methods.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a method with the specified <paramref name="methodName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryInvokeMethod">TryInvokeMethod</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static object InvokeMethod(Type type, string methodName, ReflectionWays way, params object[] parameters)
            => InvokeMethod(type, methodName, null, way, parameters);

        /// <summary>
        /// Invokes a static method of a <see cref="System.Type"/> represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static method belongs to.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a method with the specified <paramref name="methodName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryInvokeMethod">TryInvokeMethod</see> methods instead.</para>
        /// <para>For invoking the method this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static object InvokeMethod(Type type, string methodName, params object[] parameters)
            => InvokeMethod(type, methodName, null, ReflectionWays.Auto, parameters);

        /// <summary>
        /// Tries to invoke an instance method of an object represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="instance">An instance whose method is about to be invoked.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="methodName"/> is a generic method definition. Otherwise, this parameter is ignored.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for invoking methods.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the return value of the method.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns><see langword="true"/>, if the method could be invoked; <see langword="false"/>, if a matching method could not be found.</returns>
        /// <remarks>
        /// <note>If a matching method could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="genericParameters"/> and <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method belongs to a value type (<see langword="struct"/>) or has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static bool TryInvokeMethod(object instance, string methodName, Type[] genericParameters, ReflectionWays way, out object result, params object[] parameters)
        {
            if (methodName == null)
                throw new ArgumentNullException(nameof(methodName), Res.ArgumentNull);
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);

            return DoTryInvokeMethod(methodName, instance.GetType(), instance, parameters ?? EmptyObjects, genericParameters ?? Type.EmptyTypes, way, false, out result);
        }

        /// <summary>
        /// Tries to invoke an instance method of an object represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="instance">An instance whose method is about to be invoked.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="methodName"/> is a generic method definition. Otherwise, this parameter is ignored.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the return value of the method.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns><see langword="true"/>, if the method could be invoked; <see langword="false"/>, if a matching method could not be found.</returns>
        /// <remarks>
        /// <note>If a matching method could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="genericParameters"/> and <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>For invoking the method this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method belongs to a value type (<see langword="struct"/>) or has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static bool TryInvokeMethod(object instance, string methodName, Type[] genericParameters, out object result, params object[] parameters)
            => TryInvokeMethod(instance, methodName, genericParameters, ReflectionWays.Auto, out result, parameters);

        /// <summary>
        /// Tries to invoke an instance method of an object represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="instance">An instance whose method is about to be invoked.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for invoking methods.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the return value of the method.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns><see langword="true"/>, if the method could be invoked; <see langword="false"/>, if a matching method could not be found.</returns>
        /// <remarks>
        /// <note>If a matching method could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method belongs to a value type (<see langword="struct"/>) or has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static bool TryInvokeMethod(object instance, string methodName, ReflectionWays way, out object result, params object[] parameters)
            => TryInvokeMethod(instance, methodName, null, way, out result, parameters);

        /// <summary>
        /// Tries to invoke an instance method of an object represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="instance">An instance whose method is about to be invoked.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the return value of the method.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns><see langword="true"/>, if the method could be invoked; <see langword="false"/>, if a matching method could not be found.</returns>
        /// <remarks>
        /// <note>If a matching method could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>For invoking the method this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method belongs to a value type (<see langword="struct"/>) or has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static bool TryInvokeMethod(object instance, string methodName, out object result, params object[] parameters)
            => TryInvokeMethod(instance, methodName, null, ReflectionWays.Auto, out result, parameters);

        /// <summary>
        /// Tries to invoke a static method of a <see cref="System.Type"/> represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static method belongs to.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="methodName"/> refers to a generic method definition. Otherwise, this parameter is ignored.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for invoking methods.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the return value of the method.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns><see langword="true"/>, if the method could be invoked; <see langword="false"/>, if a matching method could not be found.</returns>
        /// <remarks>
        /// <note>If a matching method could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="genericParameters"/> and <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static bool TryInvokeMethod(Type type, string methodName, Type[] genericParameters, ReflectionWays way, out object result, params object[] parameters)
        {
            if (methodName == null)
                throw new ArgumentNullException(nameof(methodName), Res.ArgumentNull);
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            if (parameters == null)
                parameters = EmptyObjects;
            if (genericParameters == null)
                genericParameters = Type.EmptyTypes;

            return DoTryInvokeMethod(methodName, type, null, parameters, genericParameters, way, false, out result);
        }

        /// <summary>
        /// Tries to invoke a static method of a <see cref="System.Type"/> represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static method belongs to.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="methodName"/> refers to a generic method definition. Otherwise, this parameter is ignored.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the return value of the method.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns><see langword="true"/>, if the method could be invoked; <see langword="false"/>, if a matching method could not be found.</returns>
        /// <remarks>
        /// <note>If a matching method could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="genericParameters"/> and <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>For invoking the method this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static bool TryInvokeMethod(Type type, string methodName, Type[] genericParameters, out object result, params object[] parameters)
            => TryInvokeMethod(type, methodName, genericParameters, ReflectionWays.Auto, out result, parameters);

        /// <summary>
        /// Tries to invoke a static method of a <see cref="System.Type"/> represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static method belongs to.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for invoking methods.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the return value of the method.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns><see langword="true"/>, if the method could be invoked; <see langword="false"/>, if a matching method could not be found.</returns>
        /// <remarks>
        /// <note>If a matching method could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static bool TryInvokeMethod(Type type, string methodName, ReflectionWays way, out object result, params object[] parameters)
            => TryInvokeMethod(type, methodName, null, way, out result, parameters);

        /// <summary>
        /// Tries to invoke a static method of a <see cref="System.Type"/> represented by the specified <paramref name="methodName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static method belongs to.</param>
        /// <param name="methodName">The name of the method to be invoked.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the return value of the method.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns><see langword="true"/>, if the method could be invoked; <see langword="false"/>, if a matching method could not be found.</returns>
        /// <remarks>
        /// <note>If a matching method could be found and the invocation itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para><paramref name="methodName"/> can refer public and non-public methods. To avoid ambiguity this method gets
        /// all of the methods of the same name and chooses the first one for which the provided <paramref name="parameters"/> match.</para>
        /// <para>If you already have a <see cref="MethodInfo"/> instance use the <see cref="InvokeMethod(object,MethodInfo,System.Type[],ReflectionWays,object[])"/> method
        /// for better performance.</para>
        /// <para>For invoking the method this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the method has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static bool TryInvokeMethod(Type type, string methodName, out object result, params object[] parameters)
            => TryInvokeMethod(type, methodName, null, ReflectionWays.Auto, out result, parameters);

        private static bool DoTryInvokeMethod(string methodName, Type type, object instance, object[] parameters, Type[] genericParameters, ReflectionWays way, bool throwError, out object result)
        {
            result = null;

            Exception lastException = null;
            for (Type checkedType = type; checkedType.BaseType != null; checkedType = checkedType.BaseType)
            {
                BindingFlags flags = type == checkedType ? BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy : BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                flags |= instance == null ? BindingFlags.Static : BindingFlags.Instance;
                MemberInfo[] methods = checkedType.GetMember(methodName, MemberTypes.Method, flags);
                bool checkParams = methods.Length > 1; // for performance reasons we skip checking parameters if there is only one method of the given name

                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop - methods are queried
                foreach (MethodInfo method in methods)
                {
                    ParameterInfo[] methodParams = checkParams ? method.GetParameters() : null;
                    if (checkParams && methodParams.Length != parameters.Length)
                        continue;

                    // if the method is generic we need the generic arguments and a constructed method with real types
                    MethodInfo mi = method;
                    if (mi.IsGenericMethodDefinition)
                    {
                        Type[] genArgs = mi.GetGenericArguments();
                        if (genericParameters.Length != genArgs.Length)
                        {
                            if (throwError)
                                lastException = new ArgumentException(Res.ReflectionTypeArgsLengthMismatch(genArgs.Length), nameof(genericParameters));
                            continue;
                        }
                        try
                        {
                            mi = mi.GetGenericMethod(genericParameters);
                            if (checkParams)
                                methodParams = mi.GetParameters();
                        }
                        catch (Exception e) when (!e.IsCritical())
                        {
                            if (throwError)
                                lastException = e;
                            continue;
                        }
                    }

                    if (checkParams && !CheckParameters(methodParams, parameters))
                        continue;

                    try
                    {
                        result = InvokeMethod(instance, mi, null, way, parameters);
                        return true;
                    }
                    catch (TargetInvocationException e)
                    {
                        if (e.InnerException == null)
                            throw;
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    }
                    catch (Exception e) when (!checkParams && !e.IsCritical())
                    {
                        // if parameters check was omitted and the error is due to incorrect parameters we skip the method
                        if (!CheckParameters(mi.GetParameters(), parameters))
                        {
                            lastException = e;
                            continue;
                        }

                        throw;
                    }
                }
            }

            if (!throwError)
                return false;

            if (instance == null)
                throw new ReflectionException(Res.ReflectionStaticMethodNotFound(methodName, type), lastException);
            throw new ReflectionException(Res.ReflectionInstanceMethodNotFound(methodName, type), lastException);
        }

        #endregion

        #endregion

        #region Construction

        #region By ConstructorInfo

        /// <summary>
        /// Creates a new instance by a <see cref="ConstructorInfo"/> specified in the <paramref name="ctor"/> parameter.
        /// </summary>
        /// <param name="ctor">The constructor to be invoked.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable here.</param>
        /// <param name="parameters">The parameters to be used for invoking the constructor.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the constructor has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note>To invoke the constructor explicitly by dynamically created delegates use the <see cref="CreateInstanceAccessor"/> class.</note>
        /// </remarks>
        public static object CreateInstance(ConstructorInfo ctor, ReflectionWays way, params object[] parameters)
        {
            if (ctor == null)
                throw new ArgumentNullException(nameof(ctor), Res.ArgumentNull);

            switch (way)
            {
                case ReflectionWays.Auto:
#if NETSTANDARD2_0
                    if (ctor.GetParameters().Any(p => p.ParameterType.IsByRef))
                        goto case ReflectionWays.SystemReflection;
                    else
                        goto case ReflectionWays.DynamicDelegate;
#endif
                case ReflectionWays.DynamicDelegate:
                    return CreateInstanceAccessor.GetAccessor(ctor).CreateInstance(parameters);
                case ReflectionWays.SystemReflection:
                    return ctor.Invoke(parameters);
                case ReflectionWays.TypeDescriptor:
                    throw new NotSupportedException(Res.ReflectionInvokeCtorTypeDescriptorNotSupported);
                default:
                    throw new ArgumentOutOfRangeException(nameof(way), Res.EnumOutOfRange(way));
            }
        }

        /// <summary>
        /// Creates a new instance by a <see cref="ConstructorInfo"/> specified in the <paramref name="ctor"/> parameter.
        /// </summary>
        /// <param name="ctor">The constructor to be invoked.</param>
        /// <param name="parameters">The parameters to be used for invoking the constructor.</param>
        /// <returns>The return value of the method.</returns>
        /// <remarks>
        /// <para>For creating the instance this method uses the <see cref="ReflectionWays.DynamicDelegate"/> reflection way,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the constructor has ref/out parameters,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note>To invoke the constructor explicitly by dynamically created delegates use the <see cref="CreateInstanceAccessor"/> class.</note>
        /// </remarks>
        public static object CreateInstance(ConstructorInfo ctor, params object[] parameters)
            => CreateInstance(ctor, ReflectionWays.Auto, parameters);

        #endregion

        #region Default Construction (by parameterless constructor or without constructor)

        /// <summary>
        /// Creates a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="type"/> refers to a generic type definition. Otherwise, this parameter is ignored.</param>
        /// <param name="way">The preferred reflection way. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <returns>The created instance of <paramref name="type"/>.</returns>
        /// <remarks>
        /// <para>If you are not sure whether the type can be created without constructor parameters or by the provided <paramref name="genericParameters"/>, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryCreateInstance">TryCreateInstance</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way for reference types,
        /// and the <see cref="ReflectionWays.SystemReflection"/> way for value types, which will use the <see cref="Activator"/> class.</para>
        /// </remarks>
        public static object CreateInstance(Type type, Type[] genericParameters, ReflectionWays way = ReflectionWays.Auto)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            return TryCreateInstanceByType(type, genericParameters ?? Type.EmptyTypes, way, true, out object result) ? result : null;
        }

        /// <summary>
        /// Creates a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="way">The preferred reflection way. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <returns>The created instance of <paramref name="type"/>.</returns>
        /// <remarks>
        /// <para>If you are not sure whether the type can be created without constructor parameters, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryCreateInstance">TryCreateInstance</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way for reference types,
        /// and the <see cref="ReflectionWays.SystemReflection"/> way for value types, which will use the <see cref="Activator"/> class.</para>
        /// </remarks>
        public static object CreateInstance(Type type, ReflectionWays way = ReflectionWays.Auto)
            => CreateInstance(type, null, way);

        /// <summary>
        /// Tries to create a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="type"/> refers to a generic type definition. Otherwise, this parameter is ignored.</param>
        /// <param name="way">The preferred reflection way.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the created instance of <paramref name="type"/>.</param>
        /// <returns><see langword="true"/>, if the instance could be created; <see langword="false"/>, if <paramref name="type"/> cannot be created without parameters or <paramref name="genericParameters"/> do not match to the generic type definition.</returns>
        /// <remarks>
        /// <note>If an instance can be created by its parameterless constructor and the constructor itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way for reference types,
        /// and the <see cref="ReflectionWays.SystemReflection"/> way for value types, which will use the <see cref="Activator"/> class.</para>
        /// </remarks>
        public static bool TryCreateInstance(Type type, Type[] genericParameters, ReflectionWays way, out object result)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);
            return TryCreateInstanceByType(type, genericParameters ?? Type.EmptyTypes, way, false, out result);
        }

        /// <summary>
        /// Tries to create a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="type"/> refers to a generic type definition. Otherwise, this parameter is ignored.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the created instance of <paramref name="type"/>.</param>
        /// <returns><see langword="true"/>, if the instance could be created; <see langword="false"/>, if <paramref name="type"/> cannot be created without parameters or <paramref name="genericParameters"/> do not match to the generic type definition.</returns>
        /// <remarks>
        /// <note>If an instance can be created by its parameterless constructor and the constructor itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para>For creating the instance this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way for reference types,
        /// and the <see cref="ReflectionWays.SystemReflection"/> way for value types, which will use the <see cref="Activator"/> class.</para>
        /// </remarks>
        public static bool TryCreateInstance(Type type, Type[] genericParameters, out object result)
            => TryCreateInstance(type, genericParameters, ReflectionWays.Auto, out result);

        /// <summary>
        /// Tries to create a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="way">The preferred reflection way.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the created instance of <paramref name="type"/>.</param>
        /// <returns><see langword="true"/>, if the instance could be created; <see langword="false"/>, if <paramref name="type"/> cannot be created without parameters.</returns>
        /// <remarks>
        /// <note>If an instance can be created by its parameterless constructor and the constructor itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way for reference types,
        /// and the <see cref="ReflectionWays.SystemReflection"/> way for value types, which will use the <see cref="Activator"/> class.</para>
        /// </remarks>
        public static bool TryCreateInstance(Type type, ReflectionWays way, out object result)
            => TryCreateInstance(type, null, way, out result);

        /// <summary>
        /// Tries to create a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the created instance of <paramref name="type"/>.</param>
        /// <returns><see langword="true"/>, if the instance could be created; <see langword="false"/>, if <paramref name="type"/> cannot be created without parameters.</returns>
        /// <remarks>
        /// <note>If an instance can be created by its parameterless constructor and the constructor itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para>For creating the instance this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way for reference types,
        /// and the <see cref="ReflectionWays.SystemReflection"/> way for value types, which will use the <see cref="Activator"/> class.</para>
        /// </remarks>
        public static bool TryCreateInstance(Type type, out object result)
            => TryCreateInstance(type, null, ReflectionWays.Auto, out result);

        private static bool TryCreateInstanceByType(Type type, Type[] genericParameters, ReflectionWays way, bool throwError, out object result)
        {
            result = null;

            // if the type is generic we need the generic arguments and a constructed type with real types
            if (type.IsGenericTypeDefinition)
            {
                Type[] genArgs = type.GetGenericArguments();
                if (genericParameters.Length != genArgs.Length)
                    return throwError ? throw new ArgumentException(Res.ReflectionTypeArgsLengthMismatch(genArgs.Length), nameof(genericParameters)) : false;
                try
                {
                    type = type.GetGenericType(genericParameters);
                }
                catch (Exception e) when (!e.IsCriticalOr(throwError))
                {
                    return false;
                }
            }

            if (!throwError && !type.CanBeCreatedWithoutParameters())
                return false;

            switch (way)
            {
                case ReflectionWays.Auto:
                    if (type.IsValueType)
                        goto case ReflectionWays.SystemReflection;
                    else
                        goto case ReflectionWays.DynamicDelegate;
                case ReflectionWays.DynamicDelegate:
                    result = CreateInstanceAccessor.GetAccessor(type).CreateInstance();
                    return true;
                case ReflectionWays.SystemReflection:
                    try
                    {
                        result = Activator.CreateInstance(type, true);
                        return true;
                    }
                    catch (TargetInvocationException e)
                    {
                        if (e.InnerException != null)
                            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                        throw;
                    }

                case ReflectionWays.TypeDescriptor:
                    // ReSharper disable AssignNullToNotNullAttribute - false alarm
                    result = TypeDescriptor.CreateInstance(null, type, null, null);
                    // ReSharper restore AssignNullToNotNullAttribute
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(way), Res.EnumOutOfRange(way));
            }
        }

        [SecurityCritical]
        internal static bool TryCreateEmptyObject(Type type, bool preferCtor, bool allowAlternativeWay, out object result)
        {
            result = null;

            // 1.) Value type: fails only if the type cannot be created from this domain
            if (type.IsValueType)
            {
                try
                {
                    result = Activator.CreateInstance(type);
                    return true;
                }
                catch (Exception e) when (!e.IsCritical())
                {
                    return false;
                }
            }

            // 2.) By default constructor if preferred
            ConstructorInfo defaultCtor = null;
            if (preferCtor && (defaultCtor = type.GetDefaultConstructor()) != null)
            {
                try
                {
                    result = CreateInstanceAccessor.GetAccessor(defaultCtor).CreateInstance();
                    return true;
                }
                catch (Exception e) when (!e.IsCritical())
                {
                    if (!allowAlternativeWay)
                        return false;
                }
            }

            // 3.) Without constructor if allowed
            if (!preferCtor || allowAlternativeWay)
            {
                if (TryCreateUninitializedObject(type, out result))
                    return true;
                if (!allowAlternativeWay)
                    return false;
            }

            // default constructor was already checked
            if (defaultCtor != null)
                return false;

            // 4.) By default constructor as a fallback
            if ((defaultCtor = type.GetDefaultConstructor()) != null)
            {
                try
                {
                    result = CreateInstanceAccessor.GetAccessor(defaultCtor).CreateInstance();
                    return true;
                }
                catch (Exception e) when (!e.IsCritical())
                {
                    return false;
                }
            }

            return false;
        }

        [SecurityCritical]
        internal static bool TryCreateUninitializedObject(Type type, out object result)
        {
            result = null;
            if (canCreateUninitializedObject == false)
                return false;

            try
            {
                result = DoCreateUninitializedObject(type);
                canCreateUninitializedObject = true;
                return true;
            }
            catch (SecurityException)
            {
                canCreateUninitializedObject = false;
                return false;
            }
        }

        /// <summary>
        /// At JIT-time this method may throw a SecurityException from a partially trusted domain. A separate method because
        /// the exception is thrown without even executing the code just by recognizing the GetUninitializedObject call in the body.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [SecurityCritical]
        private static object DoCreateUninitializedObject(Type t) => FormatterServices.GetUninitializedObject(t);

        #endregion

        #region By Constructor Parameters

        /// <summary>
        /// Creates a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="type"/> refers to a generic type definition. Otherwise, this parameter is ignored.</param>
        /// <param name="way">The preferred reflection way.</param>
        /// <param name="parameters">The parameters to be used for invoking the constructor.</param>
        /// <returns>The created instance of <paramref name="type"/>.</returns>
        /// <remarks>
        /// <para>If you are not sure whether the type can be created by the provided <paramref name="genericParameters"/> and <paramref name="parameters"/>, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryCreateInstance">TryCreateInstance</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way, unless for value types with
        /// empty or <see langword="null"/>&#160;<paramref name="parameters"/>, in which case the <see cref="ReflectionWays.SystemReflection"/> way is selected, which will use the <see cref="Activator"/> class.
        /// When the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the constructor has ref/out parameters, then the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static object CreateInstance(Type type, Type[] genericParameters, ReflectionWays way, params object[] parameters)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            // In case of value types no parameterless constructor would be found - redirecting
            return type.IsValueType && (parameters?.Length ?? 0) == 0
                ? (TryCreateInstanceByType(type, genericParameters ?? Type.EmptyTypes, way, true, out object result) ? result : null)
                : (TryCreateInstanceByCtor(type, parameters ?? EmptyObjects, genericParameters ?? Type.EmptyTypes, way, true, out result) ? result : null);
        }

        /// <summary>
        /// Creates a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="type"/> refers to a generic type definition. Otherwise, this parameter is ignored.</param>
        /// <param name="parameters">The parameters to be used for invoking the constructor.</param>
        /// <returns>The created instance of <paramref name="type"/>.</returns>
        /// <remarks>
        /// <para>If you are not sure whether the type can be created by the provided <paramref name="genericParameters"/> and <paramref name="parameters"/>, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryCreateInstance">TryCreateInstance</see> methods instead.</para>
        /// <para>For creating the instance this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way, unless for value types with
        /// empty or <see langword="null"/>&#160;<paramref name="parameters"/>, in which case the <see cref="ReflectionWays.SystemReflection"/> way is selected, which will use the <see cref="Activator"/> class.
        /// When the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the constructor has ref/out parameters, then the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static object CreateInstance(Type type, Type[] genericParameters, params object[] parameters)
            => CreateInstance(type, genericParameters, ReflectionWays.Auto, parameters);

        /// <summary>
        /// Creates a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="way">The preferred reflection way.</param>
        /// <param name="parameters">The parameters to be used for invoking the constructor.</param>
        /// <returns>The created instance of <paramref name="type"/>.</returns>
        /// <remarks>
        /// <para>If you are not sure whether the type can be created by the provided <paramref name="parameters"/>, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryCreateInstance">TryCreateInstance</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way, unless for value types with
        /// empty or <see langword="null"/>&#160;<paramref name="parameters"/>, in which case the <see cref="ReflectionWays.SystemReflection"/> way is selected, which will use the <see cref="Activator"/> class.
        /// When the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the constructor has ref/out parameters, then the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static object CreateInstance(Type type, ReflectionWays way, params object[] parameters)
            => CreateInstance(type, null, way, parameters);

        /// <summary>
        /// Creates a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="parameters">The parameters to be used for invoking the constructor.</param>
        /// <returns>The created instance of <paramref name="type"/>.</returns>
        /// <remarks>
        /// <para>If you are not sure whether the type can be created by the provided <paramref name="parameters"/>, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryCreateInstance">TryCreateInstance</see> methods instead.</para>
        /// <para>For creating the instance this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way, unless for value types with
        /// empty or <see langword="null"/>&#160;<paramref name="parameters"/>, in which case the <see cref="ReflectionWays.SystemReflection"/> way is selected, which will use the <see cref="Activator"/> class.
        /// When the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the constructor has ref/out parameters, then the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static object CreateInstance(Type type, params object[] parameters)
            => CreateInstance(type, null, ReflectionWays.Auto, parameters);

        /// <summary>
        /// Tries to create a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="type"/> refers to a generic type definition. Otherwise, this parameter is ignored.</param>
        /// <param name="way">The preferred reflection way.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the created instance of <paramref name="type"/>.</param>
        /// <param name="parameters">The parameters to be used for invoking the constructor.</param>
        /// <returns><see langword="true"/>, if the instance could be created; <see langword="false"/>, if <paramref name="type"/> cannot be created by the provided <paramref name="genericParameters"/> and <paramref name="parameters"/>.</returns>
        /// <remarks>
        /// <note>If a matching constructor could be found and the constructor itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way, unless for value types with
        /// empty or <see langword="null"/>&#160;<paramref name="parameters"/>, in which case the <see cref="ReflectionWays.SystemReflection"/> way is selected, which will use the <see cref="Activator"/> class.
        /// When the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the constructor has ref/out parameters, then the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static bool TryCreateInstance(Type type, Type[] genericParameters, ReflectionWays way, out object result, params object[] parameters)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            // In case of value types no parameterless constructor would be found - redirecting
            return type.IsValueType && (parameters?.Length ?? 0) == 0
                ? TryCreateInstanceByType(type, genericParameters ?? Type.EmptyTypes, way, true, out result)
                : TryCreateInstanceByCtor(type, parameters ?? EmptyObjects, genericParameters ?? Type.EmptyTypes, way, true, out result);
        }

        /// <summary>
        /// Tries to create a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="genericParameters">Type parameters if <paramref name="type"/> refers to a generic type definition. Otherwise, this parameter is ignored.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the created instance of <paramref name="type"/>.</param>
        /// <param name="parameters">The parameters to be used for invoking the constructor.</param>
        /// <returns><see langword="true"/>, if the instance could be created; <see langword="false"/>, if <paramref name="type"/> cannot be created by the provided <paramref name="genericParameters"/> and <paramref name="parameters"/>.</returns>
        /// <remarks>
        /// <note>If a matching constructor could be found and the constructor itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para>For creating the instance this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way, unless for value types with
        /// empty or <see langword="null"/>&#160;<paramref name="parameters"/>, in which case the <see cref="ReflectionWays.SystemReflection"/> way is selected, which will use the <see cref="Activator"/> class.
        /// When the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the constructor has ref/out parameters, then the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static bool TryCreateInstance(Type type, Type[] genericParameters, out object result, params object[] parameters)
            => TryCreateInstance(type, genericParameters, ReflectionWays.Auto, out result, parameters);

        /// <summary>
        /// Tries to create a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="way">The preferred reflection way.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the created instance of <paramref name="type"/>.</param>
        /// <param name="parameters">The parameters to be used for invoking the constructor.</param>
        /// <returns><see langword="true"/>, if the instance could be created; <see langword="false"/>, if <paramref name="type"/> cannot be created by the provided <paramref name="parameters"/>.</returns>
        /// <remarks>
        /// <note>If a matching constructor could be found and the constructor itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way, unless for value types with
        /// empty or <see langword="null"/>&#160;<paramref name="parameters"/>, in which case the <see cref="ReflectionWays.SystemReflection"/> way is selected, which will use the <see cref="Activator"/> class.
        /// When the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the constructor has ref/out parameters, then the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static bool TryCreateInstance(Type type, ReflectionWays way, out object result, params object[] parameters)
            => TryCreateInstance(type, null, way, out result, parameters);

        /// <summary>
        /// Tries to create a new instance of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> of the instance to create.</param>
        /// <param name="result">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the created instance of <paramref name="type"/>.</param>
        /// <param name="parameters">The parameters to be used for invoking the constructor.</param>
        /// <returns><see langword="true"/>, if the instance could be created; <see langword="false"/>, if <paramref name="type"/> cannot be created by the provided <paramref name="parameters"/>.</returns>
        /// <remarks>
        /// <note>If a matching constructor could be found and the constructor itself has thrown an exception, then this method also throws an exception instead of returning <see langword="false"/>.</note>
        /// <para>For creating the instance this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way, unless for value types with
        /// empty or <see langword="null"/>&#160;<paramref name="parameters"/>, in which case the <see cref="ReflectionWays.SystemReflection"/> way is selected, which will use the <see cref="Activator"/> class.
        /// When the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the constructor has ref/out parameters, then the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static bool TryCreateInstance(Type type, out object result, params object[] parameters)
            => TryCreateInstance(type, null, ReflectionWays.Auto, out result, parameters);

        private static bool TryCreateInstanceByCtor(Type type, object[] parameters, Type[] genericParameters, ReflectionWays way, bool throwError, out object result)
        {
            result = null;

            // if the type is generic we need the generic arguments and a constructed type with real types
            if (type.IsGenericTypeDefinition)
            {
                Type[] genArgs = type.GetGenericArguments();
                if (genericParameters.Length != genArgs.Length)
                    return throwError ? throw new ArgumentException(Res.ReflectionTypeArgsLengthMismatch(genArgs.Length), nameof(genericParameters)) : false;
                try
                {
                    type = type.GetGenericType(genericParameters);
                }
                catch (Exception e) when (!e.IsCriticalOr(throwError))
                {
                    return false;
                }
            }

            Exception lastException = null;
            ConstructorInfo[] ctors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            bool checkParams = ctors.Length > 1; // for performance reasons we skip checking parameters if there is only one constructor
            foreach (ConstructorInfo ctor in ctors)
            {
                ParameterInfo[] ctorParams = checkParams ? ctor.GetParameters() : null;
                if (checkParams && !CheckParameters(ctorParams, parameters))
                    continue;

                try
                {
                    if (way == ReflectionWays.TypeDescriptor)
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute - argTypes can be null
                        result = TypeDescriptor.CreateInstance(null, type, ctorParams?.Select(p => p.ParameterType).ToArray(), parameters);
                        return true;
                    }

                    result = CreateInstance(ctor, way, parameters);
                    return true;
                }
                catch (TargetInvocationException e)
                {
                    if (e.InnerException != null)
                        ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                    throw;
                }
                catch (Exception e) when (!checkParams && !e.IsCritical())
                {
                    // if parameters check was omitted and the error is due to incorrect parameters we skip the constructor
                    if (!CheckParameters(ctor.GetParameters(), parameters))
                    {
                        lastException = e;
                        continue;
                    }

                    throw;
                }
            }

            if (!throwError)
                return false;
            throw new ReflectionException(Res.ReflectionCtorNotFound(type), lastException);
        }

        #endregion

        #endregion

        #region SetField

        /// <summary>
        /// Sets a <paramref name="field"/> represented by the specified <see cref="FieldInfo"/>.
        /// </summary>
        /// <param name="instance">An instance whose field is about to be set. This parameter is ignored for static fields.</param>
        /// <param name="field">The field to set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for fields. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <remarks>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the field is read-only or is an instance member of a value type (<see langword="struct"/>),
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// <note>To set the property explicitly by dynamically created delegates use the <see cref="PropertyAccessor"/> class.</note>
        /// </remarks>
        public static void SetField(object instance, FieldInfo field, object value, ReflectionWays way = ReflectionWays.Auto)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field), Res.ArgumentNull);
            bool isStatic = field.IsStatic;
            if (instance == null && !isStatic)
                throw new ArgumentNullException(nameof(instance), Res.ReflectionInstanceIsNull);
            if (field.IsLiteral)
                throw new InvalidOperationException(Res.ReflectionCannotSetConstantField(field.DeclaringType, field.Name));

            switch (way)
            {
                case ReflectionWays.Auto:
#if NETSTANDARD2_0
                    if (field.IsInitOnly || !isStatic && field.DeclaringType?.IsValueType == true)
                        goto case ReflectionWays.SystemReflection;
                    else
                        goto case ReflectionWays.DynamicDelegate;
#endif
                case ReflectionWays.DynamicDelegate:
                    FieldAccessor.GetAccessor(field).Set(instance, value);
                    break;
                case ReflectionWays.SystemReflection:
                    field.SetValue(instance, value);
                    break;
                case ReflectionWays.TypeDescriptor:
                    throw new NotSupportedException(Res.ReflectionSetFieldTypeDescriptorNotSupported);
                default:
                    throw new ArgumentOutOfRangeException(nameof(way), Res.EnumOutOfRange(way));
            }
        }

        /// <summary>
        /// Sets the instance field of an object represented by the specified <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="instance">An instance whose field is about to be set.</param>
        /// <param name="fieldName">The name of the field to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for fields. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <remarks>
        /// <para><paramref name="fieldName"/> can refer public and non-public fields.</para>
        /// <para>If you already have a <see cref="FieldInfo"/> instance use the <see cref="SetField(object,FieldInfo,object,ReflectionWays)"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a field with the specified <paramref name="fieldName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TrySetField">TrySetField</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the field is read-only or belongs to a value type (<see langword="struct"/>),
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static void SetField(object instance, string fieldName, object value, ReflectionWays way = ReflectionWays.Auto)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName), Res.ArgumentNull);
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);

            Type type = instance.GetType();
            DoTrySetField(fieldName, type, instance, value, way, true);
        }

        /// <summary>
        /// Sets the static field of a <see cref="System.Type"/> represented by the specified <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static field belongs to.</param>
        /// <param name="fieldName">The name of the field to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for fields. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <remarks>
        /// <para><paramref name="fieldName"/> can refer public and non-public fields.</para>
        /// <para>If you already have a <see cref="FieldInfo"/> instance use the <see cref="SetField(object,FieldInfo,object,ReflectionWays)"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a field with the specified <paramref name="fieldName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TrySetField">TrySetField</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the field is read-only,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static void SetField(Type type, string fieldName, object value, ReflectionWays way = ReflectionWays.Auto)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName), Res.ArgumentNull);
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            DoTrySetField(fieldName, type, null, value, way, true);
        }

        /// <summary>
        /// Tries to set the instance field of an object represented by the specified <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="instance">An instance whose field is about to be set.</param>
        /// <param name="fieldName">The name of the field to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for fields. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <returns><see langword="true"/>, if the field could be set; <see langword="false"/>, if a field with name <paramref name="fieldName"/> could not be found.</returns>
        /// <remarks>
        /// <para><paramref name="fieldName"/> can refer public and non-public fields.</para>
        /// <para>If you already have a <see cref="FieldInfo"/> instance use the <see cref="SetField(object,FieldInfo,object,ReflectionWays)"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the field is read-only or belongs to a value type (<see langword="struct"/>),
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// <note type="tip">To preserve the changes of a mutable value type embed it into a variable of <see cref="object"/> type and pass it to the <paramref name="instance"/> parameter of this method.</note>
        /// </remarks>
        public static bool TrySetField(object instance, string fieldName, object value, ReflectionWays way = ReflectionWays.Auto)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName), Res.ArgumentNull);
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);

            Type type = instance.GetType();
            return DoTrySetField(fieldName, type, instance, value, way, false);
        }

        /// <summary>
        /// Tries to set the static field of a <see cref="System.Type"/> represented by the specified <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static field belongs to.</param>
        /// <param name="fieldName">The name of the field to be set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for fields. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <returns><see langword="true"/>, if the field could be set; <see langword="false"/>, if a field with name <paramref name="fieldName"/> could not be found.</returns>
        /// <remarks>
        /// <para><paramref name="fieldName"/> can refer public and non-public fields.</para>
        /// <para>If you already have a <see cref="FieldInfo"/> instance use the <see cref="SetField(object,FieldInfo,object,ReflectionWays)"/> method
        /// for better performance.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used,
        /// except when the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is referenced and the field is read-only,
        /// in which case the <see cref="ReflectionWays.SystemReflection"/> way will be used.</para>
        /// </remarks>
        public static bool TrySetField(Type type, string fieldName, object value, ReflectionWays way = ReflectionWays.Auto)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName), Res.ArgumentNull);
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            return DoTrySetField(fieldName, type, null, value, way, false);
        }

        private static bool DoTrySetField(string fieldName, Type type, object instance, object value, ReflectionWays way, bool throwError)
        {
            if (way == ReflectionWays.TypeDescriptor)
                throw new NotSupportedException(Res.ReflectionSetFieldTypeDescriptorNotSupported);

            for (Type checkedType = type; checkedType.BaseType != null; checkedType = checkedType.BaseType)
            {
                BindingFlags flags = type == checkedType
                    ? BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy
                    : BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                flags |= instance == null ? BindingFlags.Static : BindingFlags.Instance;

                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop - fields are queried
                foreach (FieldInfo field in checkedType.GetMember(fieldName, MemberTypes.Field, flags))
                {
                    try
                    {
                        if (!throwError && (field.IsLiteral || !field.FieldType.CanAcceptValue(value)))
                            return false;
                        SetField(instance, field, value, way);
                        return true;
                    }
                    catch (TargetInvocationException e)
                    {
                        if (e.InnerException != null)
                            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                        throw;
                    }
                }
            }

            if (!throwError)
                return false;

            if (instance == null)
                throw new ReflectionException(Res.ReflectionStaticFieldDoesNotExist(fieldName, type));
            throw new ReflectionException(Res.ReflectionInstanceFieldDoesNotExist(fieldName, type));
        }

        #endregion

        #region GetField

        /// <summary>
        /// Gets a <paramref name="field"/> represented by the specified <see cref="FieldInfo"/>.
        /// </summary>
        /// <param name="instance">An instance whose field is about to be retrieved. This parameter is ignored for static fields.</param>
        /// <param name="field">The field to get.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for fields. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <returns>The value of the field.</returns>
        /// <remarks>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then the <see cref="ReflectionWays.DynamicDelegate"/> way will be used.</para>
        /// <note>To get the property explicitly by dynamically created delegates use the <see cref="FieldAccessor"/> class.</note>
        /// </remarks>
        public static object GetField(object instance, FieldInfo field, ReflectionWays way = ReflectionWays.Auto)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field), Res.ArgumentNull);
            if (instance == null && !field.IsStatic)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);

            switch (way)
            {
                case ReflectionWays.Auto:
                case ReflectionWays.DynamicDelegate:
                    return FieldAccessor.GetAccessor(field).Get(instance);
                case ReflectionWays.SystemReflection:
                    return field.GetValue(instance);
                case ReflectionWays.TypeDescriptor:
                    throw new NotSupportedException(Res.ReflectionGetFieldTypeDescriptorNotSupported);
                default:
                    throw new ArgumentOutOfRangeException(nameof(way), Res.EnumOutOfRange(way));
            }
        }

        /// <summary>
        /// Gets the instance field of an object represented by the specified <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="instance">An instance whose field is about to be retrieved.</param>
        /// <param name="fieldName">The name of the field to get.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for fields. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <returns>The value of the field.</returns>
        /// <remarks>
        /// <para><paramref name="fieldName"/> can refer public and non-public fields.</para>
        /// <para>If you already have a <see cref="FieldInfo"/> instance use the <see cref="GetField(object,FieldInfo,ReflectionWays)"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a field with the specified <paramref name="fieldName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryGetField">TryGetField</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way.</para>
        /// </remarks>
        public static object GetField(object instance, string fieldName, ReflectionWays way = ReflectionWays.Auto)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName), Res.ArgumentNull);
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);

            Type type = instance.GetType();
            return DoTryGetField(fieldName, type, instance, way, out object result, true) ? result : null;
        }

        /// <summary>
        /// Gets the static field of a <see cref="System.Type"/> represented by the specified <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static field belongs to.</param>
        /// <param name="fieldName">The name of the field to get.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for fields. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <returns>The value of the field.</returns>
        /// <remarks>
        /// <para><paramref name="fieldName"/> can refer public and non-public fields.</para>
        /// <para>If you already have a <see cref="FieldInfo"/> instance use the <see cref="GetField(object,FieldInfo,ReflectionWays)"/> method
        /// for better performance.</para>
        /// <para>If you are not sure whether a field with the specified <paramref name="fieldName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryGetField">TryGetField</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way.</para>
        /// </remarks>
        public static object GetField(Type type, string fieldName, ReflectionWays way = ReflectionWays.Auto)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName), Res.ArgumentNull);
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            return DoTryGetField(fieldName, type, null, way, out object result, true) ? result : null;
        }

        /// <summary>
        /// Tries to get the instance field of an object represented by the specified <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="instance">An instance whose field is about to be retrieved.</param>
        /// <param name="fieldName">The name of the field to get.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the value of the field.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for fields. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <returns><see langword="true"/>, if the field could be read; <see langword="false"/>, if a field with name <paramref name="fieldName"/> could not be found.</returns>
        /// <remarks>
        /// <para><paramref name="fieldName"/> can refer public and non-public fields.</para>
        /// <para>If you are not sure whether a field with the specified <paramref name="fieldName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryGetField">TryGetField</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way.</para>
        /// </remarks>
        public static bool TryGetField(object instance, string fieldName, out object value, ReflectionWays way = ReflectionWays.Auto)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName), Res.ArgumentNull);
            if (instance == null)
                throw new ArgumentNullException(nameof(instance), Res.ArgumentNull);

            Type type = instance.GetType();
            return DoTryGetField(fieldName, type, instance, way, out value, false);
        }

        /// <summary>
        /// Tries to get the static field of a <see cref="System.Type"/> represented by the specified <paramref name="fieldName"/>.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/> the static field belongs to.</param>
        /// <param name="fieldName">The name of the field to get.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the value of the field.</param>
        /// <param name="way">The preferred reflection way. <see cref="ReflectionWays.TypeDescriptor"/> way is not applicable for fields. This parameter is optional.
        /// <br/>Default value: <see cref="ReflectionWays.Auto"/>.</param>
        /// <returns><see langword="true"/>, if the field could be read; <see langword="false"/>, if a field with name <paramref name="fieldName"/> could not be found.</returns>
        /// <remarks>
        /// <para><paramref name="fieldName"/> can refer public and non-public fields.</para>
        /// <para>If you are not sure whether a field with the specified <paramref name="fieldName"/> exists, then you can use the
        /// <see cref="O:KGySoft.Reflection.Reflector.TryGetField">TryGetField</see> methods instead.</para>
        /// <para>If <paramref name="way"/> is <see cref="ReflectionWays.Auto"/>, then this method uses the <see cref="ReflectionWays.DynamicDelegate"/> way.</para>
        /// </remarks>
        public static bool TryGetField(Type type, string fieldName, out object value, ReflectionWays way = ReflectionWays.Auto)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName), Res.ArgumentNull);
            if (type == null)
                throw new ArgumentNullException(nameof(type), Res.ArgumentNull);

            return DoTryGetField(fieldName, type, null, way, out value, false);
        }

        private static bool DoTryGetField(string fieldName, Type type, object instance, ReflectionWays way, out object value, bool throwError)
        {
            if (way == ReflectionWays.TypeDescriptor)
                throw new NotSupportedException(Res.ReflectionGetFieldTypeDescriptorNotSupported);

            for (Type checkedType = type; checkedType.BaseType != null; checkedType = checkedType.BaseType)
            {
                BindingFlags flags = type == checkedType
                    ? BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy
                    : BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                flags |= instance == null ? BindingFlags.Static : BindingFlags.Instance;

                // ReSharper disable once PossibleInvalidCastExceptionInForeachLoop - fields are queried
                foreach (FieldInfo field in checkedType.GetMember(fieldName, MemberTypes.Field, flags))
                {
                    try
                    {
                        value = GetField(instance, field, way);
                        return true;
                    }
                    catch (TargetInvocationException e)
                    {
                        if (e.InnerException != null)
                            ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                        throw;
                    }
                }
            }

            if (!throwError)
            {
                value = null;
                return false;
            }

            if (instance == null)
                throw new ReflectionException(Res.ReflectionStaticFieldDoesNotExist(fieldName, type));
            throw new ReflectionException(Res.ReflectionInstanceFieldDoesNotExist(fieldName, type));
        }

        #endregion

        #region Parameters

        /// <summary>
        /// Checks whether the awaited parameter list can receive an actual parameter list
        /// </summary>
        private static bool CheckParameters(ParameterInfo[] awaitedParams, object[] actualParams)
        {
            if (awaitedParams.Length != actualParams.Length)
                return false;

            for (int i = 0; i < awaitedParams.Length; i++)
            {
                if (!awaitedParams[i].ParameterType.CanAcceptValue(actualParams[i]))
                    return false;
            }

            return true;
        }

        private static int[] ToArrayIndices(object[] indexParameters, out Exception error)
        {
            error = null;
            var indices = new int[indexParameters.Length];
            for (int i = 0; i < indexParameters.Length; i++)
            {
                var param = indexParameters[i];
                if (param is int intParam)
                {
                    indices[i] = intParam;
                    continue;
                }

                // from primitive types we try to convert just because long arguments must be accepted, too
                if (param?.GetType().IsPrimitive == true)
                {
                    try
                    {
                        indices[i] = Convert.ToInt32(param, CultureInfo.InvariantCulture);
                        continue;
                    }
                    catch (Exception e) when (!e.IsCritical())
                    {
                        error = e;
                    }
                }

                error = new ArgumentException(Res.ReflectionIndexParamsTypeMismatch, nameof(indexParameters), error);
                return null;
            }

            return indices;
        }

        #endregion

        #region Assembly resolve

        /// <summary>
        /// Gets the <see cref="Assembly"/> with the specified <paramref name="assemblyName"/>.
        /// </summary>
        /// <param name="assemblyName">Name of the <see cref="Assembly"/> to retrieve. May contain a fully or partially defined assembly name.</param>
        /// <param name="tryToLoad">If <see langword="false"/>, searches the assembly among the already loaded assemblies. If <see langword="true"/>, tries to load the assembly when it is not already loaded.</param>
        /// <param name="matchBySimpleName"><see langword="true"/>&#160;to ignore version, culture and public key token information differences.</param>
        /// <returns>An <see cref="Assembly"/> instance with the loaded assembly.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="assemblyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="assemblyName"/> is empty.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="tryToLoad"/> is <see langword="true"/>&#160;and the assembly to load from <paramref name="assemblyName"/> cannot be found.</exception>
        /// <exception cref="FileLoadException"><paramref name="tryToLoad"/> is <see langword="true"/>&#160;and the assembly to load from <paramref name="assemblyName"/> could not be loaded.</exception>
        /// <exception cref="BadImageFormatException"><paramref name="tryToLoad"/> is <see langword="true"/>&#160;and the assembly to load from <paramref name="assemblyName"/> has invalid format.</exception>
#if !NET35
        [SecuritySafeCritical]
#endif
        public static Assembly ResolveAssembly(string assemblyName, bool tryToLoad, bool matchBySimpleName)
        {
            if (assemblyName == null)
                throw new ArgumentNullException(nameof(assemblyName), Res.ArgumentNull);
            if (assemblyName.Length == 0)
                throw new ArgumentException(Res.ArgumentEmpty, nameof(assemblyName));

            string key = (matchBySimpleName ? "-" : "+") + assemblyName;
            if (AssemblyCache.TryGetValue(key, out Assembly result))
                return result;

            // 1.) Iterating through loaded assemblies, checking names
            AssemblyName asmName = new AssemblyName(assemblyName);
            string fullName = asmName.FullName;
            string simpleName = asmName.Name;
            foreach (Assembly asm in GetLoadedAssemblies())
            {
                // Simple match. As asmName is parsed, for fully qualified names this will work for sure.
                if (asm.FullName == fullName)
                {
                    result = asm;
                    break;
                }

                AssemblyName nameToCheck = asm.GetName();
                if (nameToCheck.Name != simpleName)
                    continue;

                if (matchBySimpleName)
                {
                    result = asm;
                    break;
                }

                Version version;
                if ((version = asmName.Version) != null && nameToCheck.Version != version)
                    continue;

#if NET35 || NET40
                if (asmName.CultureInfo != null && asmName.CultureInfo.Name != nameToCheck.CultureInfo.Name)
                    continue;
#else
                if (asmName.CultureName != null && nameToCheck.CultureName != asmName.CultureName)
                    continue;
#endif
                byte[] publicKeyTokenRef, publicKeyTokenCheck;
                if ((publicKeyTokenRef = asmName.GetPublicKeyToken()) != null && (publicKeyTokenCheck = nameToCheck.GetPublicKeyToken()) != null
                    && publicKeyTokenRef.SequenceEqual(publicKeyTokenCheck))
                    continue;

                result = asm;
                break;
            }

            // 2.) Trying to load the assembly
            if (result == null && tryToLoad)
                result = matchBySimpleName ? LoadAssemblyWithPartialName(asmName) : Assembly.Load(asmName);

            if (result != null)
                assemblyCache[key] = result;

            return result;
        }

        /// <summary>
        /// Gets the already loaded assemblies in a transparent way of any frameworks.
        /// </summary>
        internal static Assembly[] GetLoadedAssemblies()
            // no caching because can change
            => AppDomain.CurrentDomain.GetAssemblies();

        /// <summary>
        /// Loads the assembly with partial name. It is needed because Assembly.LoadWithPartialName is obsolete.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom",
            Justification = "The way it is used ensures that only GAC assemblies are loaded. This is how the obsolete Assembly.LoadWithPartialName can be avoided.")]
        private static Assembly LoadAssemblyWithPartialName(AssemblyName assemblyName)
        {
#if NETFRAMEWORK
            // 1. In case of a system assembly, returning it from the GAC
            string gacPath = Fusion.GetGacPath(assemblyName.Name);
            if (gacPath != null)
                return Assembly.LoadFrom(gacPath); 
#endif

            Assembly result = null;

            // 2. Non-GAC assembly: Trying to load the assembly with full name first.
            try
            {
                result = Assembly.Load(assemblyName);
                if (result != null)
                    return result;
            }
            catch (IOException)
            {
                // if version is set, we have a second try
                if (assemblyName.Version == null)
                    throw;
            }

            // 3. Trying to load the assembly without version info
            if (assemblyName.Version != null)
            {
                assemblyName = (AssemblyName)assemblyName.Clone();
                assemblyName.Version = null;
                result = Assembly.Load(assemblyName);
            }

            return result;
        }

        #endregion

        #region Type routines

        /// <summary>
        /// Gets the <see cref="System.Type"/> with the specified <paramref name="typeName"/>.
        /// When no assembly is defined in <paramref name="typeName"/>, the type can be defined in any loaded assembly.
        /// </summary>
        /// <param name="typeName">The type name as a string representation with or without assembly name.</param>
        /// <param name="loadPartiallyDefinedAssemblies"><see langword="true"/>&#160;to load assemblies with partially defined names; <see langword="false"/>&#160;to find partially defined names in already loaded assemblies only.</param>
        /// <param name="matchAssemblyByWeakName"><see langword="true"/>&#160;to allow resolving assembly names by simple assembly name, and ignoring version, culture and public key token information even if they present in <paramref name="typeName"/>.</param>
        /// <returns>The resolved <see cref="System.Type"/>, or <see langword="null"/>&#160;if <paramref name="typeName"/> cannot be resolved.</returns>
        /// <remarks>
        /// <para><paramref name="typeName"/> can be generic and may contain fully or partially defined assembly names. When assembly name is partially defined,
        /// then the assembly is attempted to be loaded only when <paramref name="loadPartiallyDefinedAssemblies"/> is <see langword="true"/>.</para>
        /// </remarks>
        /// <example>
        /// <code lang="C#"><![CDATA[
        /// // mscorlib types are defined wihtout assembly, System.Uri is defined with fully qualified assembly name - it will be loaded if possible
        /// var type = ResolveType("System.Collections.Generic.Dictionary`2[System.String,[System.Uri, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", false, false);
        /// 
        /// // System.Uri will be resolved even if the loaded System.dll has different version
        /// var type = ResolveType("System.Collections.Generic.Dictionary`2[System.String,[System.Uri, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]", false, true);
        /// 
        /// // System.Uri is defined with partial assembly name - it will be resolved only when System.dll is already loaded
        /// var type = ResolveType("System.Collections.Generic.Dictionary`2[System.String,[System.Uri, System]]", false, false);
        /// 
        /// // System.Uri is defined with partial assembly name, and we allow to load it with partial name, too
        /// var type = ResolveType("System.Collections.Generic.Dictionary`2[System.String,[System.Uri, System]]", true, false);
        /// 
        /// // all types are defined without assembly names: Dictionary and String will be resolved from mscorlib, Uri will be resolved if there is a System.Uri in any loaded assembly.
        /// var type = ResolveType("System.Collections.Generic.Dictionary`2[System.String, System.Uri]", false, false);
        /// ]]></code>
        /// </example>
        /// <exception cref="ReflectionException"><paramref name="typeName"/> cannot be parsed.</exception>
        public static Type ResolveType(string typeName, bool loadPartiallyDefinedAssemblies = false, bool matchAssemblyByWeakName = false)
        {
            if (String.IsNullOrEmpty(typeName))
                return null;

            if (TypeCacheByString.TryGetValue(typeName, out Type result))
                return result;

            try
            {
                if (typeName[0] == '!')
                    result = ResolveGenericTypeParameter(typeName, loadPartiallyDefinedAssemblies, matchAssemblyByWeakName);
                else
                    // mscorlib type of fully qualified names
                    result = Type.GetType(typeName);
            }
            catch (Exception e)
            {
                throw new ReflectionException(Res.ReflectionNotAType(typeName), e);
            }

            if (result != null)
            {
                typeCacheByString[typeName] = result;
                return result;
            }

            // no success: partial name or non-mscorlib type without assembly name
            SplitTypeName(typeName, out string asmName, out typeName);

            // (partial) assembly name is defined
            if (asmName != null)
            {
                Assembly assembly;
                try
                {
                    assembly = ResolveAssembly(asmName, loadPartiallyDefinedAssemblies, matchAssemblyByWeakName);
                }
                catch (Exception e)
                {
                    throw new ReflectionException(Res.ReflectionCannotLoadAssembly(asmName), e);
                }

                if (assembly == null)
                    return null;

                return ResolveType(assembly, typeName, loadPartiallyDefinedAssemblies, matchAssemblyByWeakName);
            }

            // no assembly name is defined (generics: no assembly name for the main type itself)
            // firstly we try to resolve the type in the calling assembly
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            result = ResolveType(callingAssembly, typeName, loadPartiallyDefinedAssemblies, matchAssemblyByWeakName);
            if (result == null)
            {
                foreach (Assembly assembly in GetLoadedAssemblies())
                {
                    if (assembly == callingAssembly)
                        continue;

                    result = ResolveType(assembly, typeName, loadPartiallyDefinedAssemblies, matchAssemblyByWeakName);
                    if (result != null)
                        break;
                }
            }

            if (result != null)
            {
                typeCacheByString[typeName] = result;
            }

            return result;
        }

        /// <summary>
        /// Gets the <see cref="System.Type"/> with the specified <paramref name="typeName"/> from the specified <paramref name="assembly"/>.
        /// Type name can be generic.
        /// </summary>
        /// <param name="typeName">The type name as a string representation.</param>
        /// <param name="assembly">The assembly that contains the type to retrieve.</param>
        /// <returns>The resolved <see cref="System.Type"/>, or <see langword="null"/>&#160;if <paramref name="typeName"/> cannot be resolved.</returns>
        /// <remarks>
        /// <para>The generic type parameters can contain assembly parts. However, if the main type definition contains an assembly part, then
        /// an <see cref="ArgumentException"/> will be thrown. For such type names use <see cref="ResolveType(string,bool,bool)"/> overload instead.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is <see langword="null"/>.</exception>
        /// <exception cref="ReflectionException"><paramref name="typeName"/> cannot be parsed.</exception>
        /// <exception cref="ArgumentException"><paramref name="typeName"/> contains assembly name part, which is allowed only in generic type parameters.</exception>
        public static Type ResolveType(Assembly assembly, string typeName)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly), Res.ArgumentNull);
            if (String.IsNullOrEmpty(typeName))
                return null;

            int genericEnd = typeName.LastIndexOf(']');
            int asmNamePos = typeName.IndexOf(',', genericEnd + 1);
            if (asmNamePos >= 0)
                throw new ArgumentException(Res.ReflectionTypeWithAssemblyName, nameof(typeName));

            return ResolveType(assembly, typeName, false, false);
        }

        /// <summary>
        /// Resolves the typeName in the specified assembly. loadPartiallyDefinedAssemblies and matchAssemblyByWeakName refer the possible assembly strings in generic arguments.
        /// </summary>
        private static Type ResolveType(Assembly assembly, string typeName, bool loadPartiallyDefinedAssemblies, bool matchAssemblyByWeakName)
        {
            LockingDictionary<string, Type> cache = TypeCacheByAssembly[assembly];
            if (cache.TryGetValue(typeName, out var result))
                return result;

            try
            {
                // Try 1: A type from specified assembly
                result = assembly.GetType(typeName);

                // Try 2: if result is still null we may check whether a generic type is tried to be created (like: System.Collections.Generic.List`1[System.Globalization.CultureInfo])
                if (result == null)
                {
                    GetNameAndIndices(typeName, out string elementTypeName, out string[] genTypeParams, out int[] arrayRanks);
                    if (genTypeParams != null || arrayRanks != null)
                    {
                        // this should have no assembly info in name
                        result = ResolveType(assembly, elementTypeName, false, false);

                        // processing generic parameters
                        if (result != null && genTypeParams != null)
                        {
                            if (!result.IsGenericTypeDefinition)
                                throw new ReflectionException(Res.ReflectionResolveNotAGenericType(elementTypeName, typeName));
                            Type[] genArgs = result.GetGenericArguments();
                            if (genArgs.Length != genTypeParams.Length)
                                throw new ReflectionException(Res.ReflectionResolveTypeArgsLengthMismatch(typeName, genArgs.Length));
                            Type[] typeGenParams = new Type[genTypeParams.Length];
                            for (int i = 0; i < genTypeParams.Length; i++)
                            {
                                if ((typeGenParams[i] = ResolveType(genTypeParams[i], loadPartiallyDefinedAssemblies, matchAssemblyByWeakName)) == null)
                                    throw new ReflectionException(Res.ReflectionCannotResolveTypeArg(elementTypeName, typeName));
                            }

                            result = result.GetGenericType(typeGenParams);
                        }

                        // processing ranks
                        if (result != null && arrayRanks != null)
                        {
                            foreach (int rank in arrayRanks)
                            {
                                switch (rank)
                                {
                                    case 1:
                                        // 1 dimensional zero-based array
                                        result = result.MakeArrayType();
                                        break;
                                    case -1:
                                        // 1 dimensional nonzero-based array
                                        result = result.MakeArrayType(1);
                                        break;
                                    default:
                                        // multidimensional array
                                        result = result.MakeArrayType(rank);
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new ReflectionException(Res.ReflectionNotAType(typeName), e);
            }

            if (result != null)
            {
                cache[typeName] = result;
            }

            return result;
        }

        private static Type ResolveGenericTypeParameter(string typeName, bool loadPartiallyDefinedAssemblies, bool matchAssemblyByWeakName)
        {
            if (typeName.Length == 1)
                return null;
            bool isGenericMethodParam = typeName[1] == '!';
            int begin = isGenericMethodParam ? 2 : 1;
            int end = typeName.IndexOf(':');
            if (end == -1)
                return null;
            string argName = typeName.Substring(begin, end - begin);
            string signature = null;
            if (isGenericMethodParam)
            {
                begin = end + 1;
                end = typeName.IndexOf(':', begin);
                if (end == -1)
                    return null;
                signature = typeName.Substring(begin, end - begin);
            }

            Type declaringType = ResolveType(typeName.Substring(end + 1), loadPartiallyDefinedAssemblies, matchAssemblyByWeakName);

            if (!isGenericMethodParam)
                return declaringType.GetGenericArguments().FirstOrDefault(arg => arg.Name == argName);
            
            MethodInfo method = declaringType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .FirstOrDefault(mi => mi.ToString() == signature);
            return method?.GetGenericArguments().FirstOrDefault(arg => arg.Name == argName);
        }

        /// <summary>
        /// Splits possible assembly qualified name to assembly and type names.
        /// </summary>
        internal static void SplitTypeName(string aqnOrTypeName, out string assemblyName, out string typeName)
        {
            int genericEnd = aqnOrTypeName.LastIndexOf(']');
            int asmNamePos = aqnOrTypeName.IndexOf(',', genericEnd + 1);
            if (asmNamePos >= 0)
            {
                assemblyName = aqnOrTypeName.Substring(asmNamePos + 1).Trim();
                typeName = aqnOrTypeName.Substring(0, asmNamePos).Trim();
                return;
            }

            assemblyName = null;
            typeName = aqnOrTypeName;
        }

        /// <summary>
        /// Separates the name from the indices like TypeGen[gentypeparam, asmname][12,1]. 
        /// Parsed indices are returned as string list.
        /// array rank is -1 for non-zero based one dimensional arrays.
        /// </summary>
        internal static void GetNameAndIndices(string value, out string name, out string[] genericTypeParams, out int[] arrayRanks)
        {
            int posBeginBracket = value.IndexOf('[');
            if (posBeginBracket < 0)
            {
                name = value;
                genericTypeParams = null;
                arrayRanks = null;
                return;
            }

            name = value.Substring(0, posBeginBracket);

            arrayRanks = GetArrayRanks(ref value);
            posBeginBracket = value.IndexOf('[');
            if (posBeginBracket < 0)
            {
                genericTypeParams = null;
                return;
            }

            int posEndBracket = value.LastIndexOf(']');
            if (posEndBracket < posBeginBracket)
                throw new ReflectionException(Res.ReflectionTypeSyntaxError(value));
            StringBuilder sb = new StringBuilder(value.Substring(posBeginBracket + 1, posEndBracket - posBeginBracket - 1));

            const string commaPlaceholder = "<%comma%>";
            int depth = 0;
            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] == '[')
                    depth++;
                else if (sb[i] == ']')
                    depth--;
                else if (sb[i] == ',' && depth > 0)
                {
                    sb.Remove(i, 1);
                    sb.Insert(i, commaPlaceholder);
                    i += commaPlaceholder.Length - 1;
                }
            }

            genericTypeParams = sb.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < genericTypeParams.Length; i++)
            {
                genericTypeParams[i] = genericTypeParams[i].Trim().Replace(commaPlaceholder, ",");
                if (genericTypeParams[i].Length > 1 && genericTypeParams[i][0] == '[' && genericTypeParams[i][genericTypeParams[i].Length - 1] == ']')
                    genericTypeParams[i] = genericTypeParams[i].Substring(1, genericTypeParams[i].Length - 2);
            }
            if (genericTypeParams.Length == 0)
                genericTypeParams = null;
        }

        /// <summary>
        /// Gets array ranks in type definition and removes array part of given value.
        /// </summary>
        private static int[] GetArrayRanks(ref string value)
        {
            int posEndBracket = value.LastIndexOf(']');

            if (posEndBracket == -1)
                throw new ReflectionException(Res.ReflectionTypeSyntaxError(value));

            CircularList<int> result = new CircularList<int>();
            int lastArrayIndex = -1;
            bool inArray = true;
            int rank = 1;
            bool stopped = false;
            for (int i = posEndBracket - 1; i >= 0 && !stopped; i--)
            {
                if (inArray)
                {
                    switch (value[i])
                    {
                        case ',':
                            if (rank < 0)
                                rank = -rank;  // back to + in every dimension
                            rank++;
                            break;
                        case '*':
                            if (rank < 0)
                                stopped = true; // multiple *s in a dimension
                            rank = -rank;
                            break;
                        case '[':
                            // -1 is kept only for 1 dimension  array ([*]) because it is different than [], while [,] and [*,*] are the same.
                            if (rank < -1)
                                rank = -rank;
                            result.Insert(0, rank);
                            inArray = false;
                            lastArrayIndex = i;
                            break;
                        default:
                            if (!Char.IsWhiteSpace(value[i]))
                                stopped = true;
                            break;
                    }
                }
                else // !inArray
                {
                    if (value[i] == ']')
                    {
                        inArray = true;
                        rank = 1;
                    }
                    else if (!Char.IsWhiteSpace(value[i]))
                        stopped = true;
                }
            }

            if (lastArrayIndex != -1)
                value = value.Remove(lastArrayIndex);
            return result.Count == 0 ? null : result.ToArray();
        }

        internal static int SizeOf<T>()
        {
            var type = typeof(T);
            if (type.IsPrimitive)
                return Buffer.ByteLength(new T[1]);
            return type.SizeOf();
        }

        internal static T[] EmptyArray<T>() =>
#if NET35 || NET40 || NET45
            EmptyArrayHelper<T>.Instance;
#else
            Array.Empty<T>();
#endif

        #endregion

        #region Member Reflection

        /// <summary>
        /// Gets the returned member of an expression providing a refactoring-safe way for
        /// referencing a field, property, constructor or function method.
        /// </summary>
        /// <typeparam name="T">Type of the returned member in the expression.</typeparam>
        /// <param name="expression">An expression returning a member.</param>
        /// <returns>A <see cref="MemberInfo"/> instance that represents the returned member of the <paramref name="expression"/></returns>
        /// <remarks>
        /// <para>Similarly to the <see langword="typeof"/> operator, which provides a refactoring-safe reference to a <see cref="System.Type"/>,
        /// this method provides a non-string access to a field, property, constructor or function method:
        /// <example><code lang="C#"><![CDATA[
        /// MemberInfo ctorList = Reflector.MemberOf(() => new List<int>()); // ConstructorInfo: List<int>().ctor()
        /// MemberInfo methodIndexOf = Reflector.MemberOf(() => default(List<int>).IndexOf(default(int))); // MethodInfo: List<int>.IndexOf(int) - works without a reference to a List
        /// MemberInfo fieldEmpty = Reflector.MemberOf(() => string.Empty); // FieldInfo: String.Empty
        /// MemberInfo propertyLength = Reflector.MemberOf(() => default(string).Length); // PropertyInfo: String.Length - works without a reference to a string
        /// ]]></code></example></para>
        /// <para>Constant fields cannot be reflected by this method because the C# compiler emits the value of the constant into
        /// the expression instead of the access of the constant field.</para>
        /// <para>To reflect an action method, you can use the <see cref="MemberOf"/> method.</para>
        /// <para>To reflect methods, you can actually cast the method to a delegate and get its <see cref="Delegate.Method"/> property:
        /// <example><code lang="C#"><![CDATA[
        /// MemberInfo methodIndexOf = ((Action<int>)new List<int>().IndexOf).Method; // MethodInfo: List<int>.IndexOf(int) - a reference to a List is required
        /// ]]></code></example>
        /// <note>
        /// Accessing a method by the delegate cast is usually faster than using this method. However, you must have an instance to
        /// access instance methods. That means that you cannot use the <see langword="default"/>&#160;operator for reference types
        /// to access their instance methods. If the constructor of such a type is slow, then using this method can be
        /// more effective to access an instance method.
        /// </note>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="expression"/> does not return a member.</exception>
        /// <seealso cref="MemberOf"/>
        public static MemberInfo MemberOf<T>(Expression<Func<T>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression), Res.ArgumentNull);

            Expression body = expression.Body;
            if (body is MemberExpression member)
                return member.Member;

            if (body is MethodCallExpression methodCall)
                return methodCall.Method;

            if (body is NewExpression ctor)
                return ctor.Constructor;

            throw new ArgumentException(Res.ReflectionNotAMember(expression.GetType()), nameof(expression));
        }

        /// <summary>
        /// Gets the action method of an expression by a refactoring-safe way.
        /// </summary>
        /// <param name="expression">An expression accessing an action method.</param>
        /// <returns>The <see cref="MethodInfo"/> instance of the <paramref name="expression"/>.</returns>
        /// <remarks>
        /// <para>Similarly to the <see langword="typeof"/> operator, which provides a refactoring-safe reference to a <see cref="System.Type"/>,
        /// this method provides a non-string access to an action method:
        /// <example><code lang="C#"><![CDATA[
        /// MethodInfo methodAdd = Reflector.MemberOf(() => default(List<int>).Add(default(int))); // MethodInfo: List<int>.Add() - works without a reference to a List
        /// ]]></code></example></para>
        /// <para>To reflect a function method, constructor, property or a field, you can use the <see cref="MemberOf{T}"/> method.</para>
        /// <para>To reflect methods, you can actually cast the method to a delegate and get its <see cref="Delegate.Method"/> property:
        /// <example><code lang="C#"><![CDATA[
        /// MethodInfo methodAdd = ((Action<int>)new List<int>().Add).Method; // MethodInfo: List<int>.Add() - a reference to a List is required
        /// ]]></code></example>
        /// <note>
        /// Accessing a method by the delegate cast is usually faster than using this method. However, you must have an instance to
        /// access instance methods. That means that you cannot use the <see langword="default"/> operator for reference types
        /// to access their instance methods. If the constructor of such a type is slow, then using this method can be
        /// more effective to access an instance method.
        /// </note>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="expression"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="expression"/> does not access an action method.</exception>
        /// <seealso cref="MemberOf{T}"/>
        public static MethodInfo MemberOf(Expression<Action> expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression), Res.ArgumentNull);

            Expression body = expression.Body;
            if (body is MethodCallExpression methodCall)
                return methodCall.Method;

            throw new ArgumentException(Res.ReflectionNotAMethod, nameof(expression));
        }

        /// <summary>
        /// Determines whether the specified <paramref name="method"/> is an explicit interface implementation.
        /// </summary>
        /// <param name="method">The method to check.</param>
        /// <returns><see langword="true"/>, if the specified <paramref name="method"/> is an explicit interface implementation; otherwise, <see langword="false"/>.</returns>
        public static bool IsExplicitInterfaceImplementation(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method), Res.ArgumentNull);
            Type declaringType = method.DeclaringType;
            if (declaringType == null)
                return false;

            string methodName = method.Name;
            foreach (Type iface in declaringType.GetInterfaces())
            {
                InterfaceMapping map = declaringType.GetInterfaceMap(iface);
                for (int i = 0; i < map.TargetMethods.Length; i++)
                {
                    if (map.TargetMethods[i] != method)
                        continue;

                    // Now method is an interface implementation for sure.
                    // Explicit, if name does not match. Note: can also be null if type is abstract and implementation is in a derived class.
                    return map.InterfaceMethods[i]?.Name != methodName;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the specified <paramref name="property"/> is an explicit interface implementation.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns><see langword="true"/>, if the specified <paramref name="property"/> is an explicit interface implementation; otherwise, <see langword="false"/>.</returns>
        public static bool IsExplicitInterfaceImplementation(PropertyInfo property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property), Res.ArgumentNull);
            return IsExplicitInterfaceImplementation(property.CanRead ? property.GetGetMethod(true) : property.GetSetMethod(true));
        }

        private static string GetDefaultMember(Type type)
        {
            CustomAttributeData data = CustomAttributeData.GetCustomAttributes(type).FirstOrDefault(a => a.Constructor.DeclaringType == typeof(DefaultMemberAttribute));
            CustomAttributeTypedArgument? argument = data?.ConstructorArguments[0];
            return argument?.Value as string;
        }

        #endregion

        #endregion
    }
}
