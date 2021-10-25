#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObjectExtensions.ObjectConverter.cs
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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
#if NETCOREAPP3_0_OR_GREATER
using System.Linq;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Text; 
#endif

using KGySoft.Collections;
using KGySoft.Reflection;
using KGySoft.Serialization;

#endregion

namespace KGySoft.CoreLibraries
{
    public partial class ObjectExtensions
    {
        private static class ObjectConverter
        {
            #region ConversionContext struct

            private struct ConversionContext
            {
                #region Constants

                private const int cacheCapacity = 1024;

                #endregion

                #region Fields

                #region Internal Fields

                internal readonly CultureInfo Culture;
                internal Exception? Error;
                internal Dictionary<(Type SourceType, Type TargetType), Delegate>? LastUsedConversion;

                #endregion

                #region Private Fields
                
                // A null value indicates a failed attempt or an unfinished conversion
                private Cache<(object Instance, Type SourceType, Type TargetType), StrongBox<object?>?>? resultsCache;

                #endregion

                #endregion

                #region Constructors

                [SuppressMessage("Globalization", "CA1304:Specify CultureInfo",
                    Justification = "False alarm, culture is set after calling this()")]
                internal ConversionContext(CultureInfo culture) : this() => Culture = culture;

                #endregion

                #region Methods

                [MethodImpl(MethodImpl.AggressiveInlining)]
                internal bool TryGetCachedResult(object instance, Type sourceType, Type targetType, out StrongBox<object?>? value)
                {
                    var key = (instance, sourceType, targetType);
                    if (resultsCache == null)
                    {
                        resultsCache = new Cache<(object, Type, Type), StrongBox<object?>?> { Capacity = cacheCapacity };
                        resultsCache[key] = null;
                        value = null;
                        return false;
                    }

                    // box itself is null if a conversion failed or has not been finished, in which case we return true to prevent endless recursion
                    if (resultsCache.TryGetValue(key, out value))
                        return true;

                    // adding the key to the cache to mark that a conversion has been started
                    resultsCache[key] = null;
                    value = null;
                    return false;
                }

                [MethodImpl(MethodImpl.AggressiveInlining)]
                internal void SetCachedResult(object instance, Type sourceType, Type targetType, object? result)
                {
                    var key = (instance, sourceType, targetType);
                    if (resultsCache == null)
                    {
                        resultsCache = new Cache<(object, Type, Type), StrongBox<object?>?> { Capacity = cacheCapacity };
                        resultsCache[key] = new StrongBox<object?>(result);
                        return;
                    }

                    resultsCache[key] = new StrongBox<object?>(result);
                }

                #endregion
            }

            #endregion

            #region Constructors

            static ObjectConverter()
            {
                // KeyValuePair and Dictionary entry conversions
                Reflector.KeyValuePairType.RegisterConversion(Reflector.KeyValuePairType, TryConvertKeyValuePair);
                Reflector.DictionaryEntryType.RegisterConversion(Reflector.KeyValuePairType, TryConvertDictionaryEntryToKeyValuePair);
                Reflector.KeyValuePairType.RegisterConversion(Reflector.DictionaryEntryType, ConvertKeyValuePairToDictionaryEntry);

#if NETCOREAPP3_0_OR_GREATER
                // char-rune collections conversion
                typeof(IEnumerable<char>).RegisterConversion(typeof(IEnumerable<Rune>), (o, _, _)
                    => (o as string ?? new String(o as char[] ?? ((IEnumerable<char>)o).ToArray())).EnumerateRunes());
                typeof(IEnumerable<Rune>).RegisterConversion(typeof(IEnumerable<char>), (o, _, _)
                    => ((IEnumerable<Rune>)o).Select(r => r.ToString()).Join(String.Empty));
#endif
            }

            #endregion

            #region Methods

            #region Internal Methods

            internal static bool TryConvert(object? obj, Type targetType, CultureInfo? culture, out object? value, out Exception? error)
            {
                if (targetType == null!)
                    Throw.ArgumentNullException(Argument.targetType);

                culture ??= CultureInfo.InvariantCulture;
                var context = new ConversionContext(culture);
                bool result = DoConvert(ref context, obj, targetType, out value, true);
                error = context.Error;
                return result;
            }

            #endregion

            #region Private Methods

            private static bool TryConvertKeyValuePair(object obj, Type targetType, CultureInfo? culture, [MaybeNullWhen(false)]out object result)
            {
                if (obj.GetType() == targetType)
                {
                    result = obj;
                    return true;
                }

                Type[] types = targetType.GetGenericArguments();
                if (!Accessors.GetPropertyValue(obj, nameof(KeyValuePair<_, _>.Key)).TryConvert(types[0], culture, out object? key)
                    || !Accessors.GetPropertyValue(obj, nameof(KeyValuePair<_, _>.Value)).TryConvert(types[1], culture, out object? value))
                {
                    result = null;
                    return false;
                }

                result = Activator.CreateInstance(targetType)!;
                Accessors.SetKeyValue(result, key, value);
                return true;
            }

            private static bool TryConvertDictionaryEntryToKeyValuePair(object obj, Type targetType, CultureInfo? culture, [MaybeNullWhen(false)]out object result)
            {
                var source = (DictionaryEntry)obj;
                Type[] types = targetType.GetGenericArguments();
                if (!source.Key.TryConvert(types[0], culture, out object? key) || !source.Value.TryConvert(types[1], culture, out object? value))
                {
                    result = null;
                    return false;
                }

                result = Activator.CreateInstance(targetType)!;
                Accessors.SetKeyValue(result, key, value);
                return true;
            }

            private static object ConvertKeyValuePairToDictionaryEntry(object obj, Type targetType, CultureInfo? culture)
                => new DictionaryEntry(Accessors.GetPropertyValue(obj, nameof(KeyValuePair<_, _>.Key))!, Accessors.GetPropertyValue(obj, nameof(KeyValuePair<_, _>.Value)));

            private static bool DoConvert(ref ConversionContext context, object? obj, Type targetType, out object? value, bool isRoot = false)
            {
                if (targetType.IsInstanceOfType(obj))
                {
                    value = obj;
                    return true;
                }

                if (obj is null or DBNull)
                {
                    value = null;
                    return targetType.CanAcceptValue(null);
                }

                if (targetType.IsNullable())
                    targetType = Nullable.GetUnderlyingType(targetType)!;

                Type sourceType = obj.GetType();

                // Trying to obtain a previous result first. If the box is null, then a previous attempt failed or we are in a recursion.
                if (!isRoot && context.TryGetCachedResult(obj, sourceType, targetType, out StrongBox<object?>? cachedResult))
                {
                    value = cachedResult?.Value;
                    return cachedResult != null;
                }

                value = null;
                bool success = false;
                try
                {
                    // First of all, trying registered direct conversions between the source and target types
                    if (TryConvertByRegisteredConversion(ref context, obj, targetType, out value, true)
                        // If it fails, then trying to parse from string. It includes TypeConverter from string but not collection conversions.
                        || obj is string strValue && strValue.TryParse(targetType, context.Culture, out value))
                    {
                        return success = true;
                    }

                    // Shortcut for converting to string of some known types
                    if (targetType == Reflector.StringType && sourceType.CanBeParsedNatively())
                    {
                        value = obj.ToStringInternal(context.Culture);
                        return success = true;
                    }

                    // Trying to convert between IConvertibles...
                    if (obj is IConvertible convertible && typeof(IConvertible).IsAssignableFrom(targetType) && TryConvertConvertible(ref context, convertible, targetType, out value)
                        // ...by TypeConverter (except converting to string by default/collection converter)...
                        || TryConvertByTypeConverter(ref context, obj, targetType, out value)
                        // ...or by non-direct conversions between source and target types (by base class/interface/generic type)
                        || TryConvertByRegisteredConversion(ref context, obj, targetType, out value, false))
                    {
                        return success = true;
                    }

                    // If both source and target types are enumerable, trying to convert their types, too (including string-collection conversions)
                    if (obj is IEnumerable collection && Reflector.IEnumerableType.IsAssignableFrom(targetType) && TryConvertCollection(ref context, collection, targetType, out value))
                        return success = true;

                    // String target ultimate fallback
                    if (targetType == Reflector.StringType)
                    {
                        value = obj is IFormattable f ? f.ToString(null, context.Culture) : obj.ToString();
                        return success = true;
                    }

                    return success = TryConvertByIntermediateTypes(ref context, obj, targetType, out value);
                }
                finally
                {
                    if (!isRoot && success)
                        context.SetCachedResult(obj, sourceType, targetType, value);
                }
            }

            private static bool TryConvertByRegisteredConversion(ref ConversionContext context, object obj, Type targetType, out object? value, bool exactTypeMatch)
            {
                Type sourceType = obj.GetType();
                if (context.LastUsedConversion != null && context.LastUsedConversion.TryGetValue((sourceType, targetType), out Delegate? conversion) && TryUseConversion(ref context, obj, targetType, conversion, out value))
                    return true;

                IList<Delegate> conversions = sourceType.GetConversions(targetType, exactTypeMatch);
                value = null;
                if (conversions.Count == 0)
                    return false;

                // ReSharper disable once ForCanBeConvertedToForeach - preventing allocation and boxing a possible List enumerator
                for (var i = 0; i < conversions.Count; i++)
                {
                    Delegate conversionDelegate = conversions[i];
                    if (!TryUseConversion(ref context, obj, targetType, conversionDelegate, out value))
                        continue;

                    context.LastUsedConversion ??= new Dictionary<(Type, Type), Delegate>();
                    (Type, Type) key = (sourceType, targetType);
                    if (!context.LastUsedConversion.TryGetValue(key, out Delegate? storedConversion) || storedConversion != conversionDelegate)
                        context.LastUsedConversion[key] = conversionDelegate;

                    return true;
                }

                return false;
            }

            private static bool TryUseConversion(ref ConversionContext context, object obj, Type targetType, Delegate conversionDelegate, out object? value)
            {
                try
                {
                    switch (conversionDelegate)
                    {
                        case ConversionAttempt conversionAttempt:
                            return conversionAttempt.Invoke(obj, targetType, context.Culture, out value)
                                && (targetType.CanAcceptValue(value) || DoConvert(ref context, value, targetType, out value));
                        case Conversion conversion:
                            value = conversion.Invoke(obj, targetType, context.Culture);
                            return targetType.CanAcceptValue(value) || DoConvert(ref context, value, targetType, out value);
                        default:
                            value = null;
                            return Throw.InternalError<bool>($"Invalid conversion delegate type: {targetType}");
                    }
                }
                catch (Exception e) when (!e.IsCritical())
                {
                    context.Error = e;
                    value = null;
                    return false;
                }
            }

            private static bool TryConvertConvertible(ref ConversionContext context, IConvertible convertible, Type targetType, out object? value)
            {
                try
                {
                    if (targetType.IsEnum)
                    {
                        value = Enum.ToObject(targetType, convertible);
                        return true;
                    }

                    value = convertible.ToType(targetType, context.Culture);
                    return true;

                }
                catch (Exception e) when (!e.IsCritical())
                {
                    context.Error = e;
                    value = null;
                    return false;
                }
            }

            private static bool TryConvertByTypeConverter(ref ConversionContext context, object source, Type targetType, out object? value)
            {
                value = null;
                Type sourceType = source.GetType();

                // 1.) by target
                TypeConverter converter = TypeDescriptor.GetConverter(targetType);
                if (converter.CanConvertFrom(sourceType))
                {
                    try
                    {
                        // ReSharper disable once AssignNullToNotNullAttribute - actually it CAN be null...
                        value = converter.ConvertFrom(null, context.Culture, source);
                        return true;
                    }
                    catch (Exception e) when (!e.IsCritical())
                    {
                        context.Error = e;
                    }
                }

                // 2.) by source
                converter = TypeDescriptor.GetConverter(sourceType);

                // except if the default converter is found (this prevents simple ToString for all types),
                // or when target is string and the non-derived CollectionConverter or the ArrayConverter would be used that only return "(Collection)"
                Type converterType = converter.GetType();
                if (converterType == typeof(TypeConverter)
                    || (targetType == Reflector.StringType && (converterType == typeof(CollectionConverter) || converter is ArrayConverter))
                    || !converter.CanConvertTo(targetType))
                {
                    return false;
                }

                try
                {
                    value = converter.ConvertTo(null, context.Culture, source, targetType);
                    return true;
                }
                catch (Exception e) when (!e.IsCritical())
                {
                    context.Error = e;
                    return false;
                }
            }

            private static bool TryConvertCollection(ref ConversionContext context, IEnumerable collection, Type targetType, out object? value)
            {
                if (targetType.IsArray)
                    return TryConvertToArray(ref context, collection, targetType, out value);

                value = null;
                if (!targetType.IsSupportedCollectionForReflection(out ConstructorInfo? defaultCtor, out ConstructorInfo? collectionCtor, out Type? targetElementType, out bool isDictionary))
                    return false;

                if (defaultCtor == null && !targetType.IsValueType)
                    return TryPopulateByInitializerCollection(ref context, collection, collectionCtor!, targetElementType, isDictionary, out value);

                var targetCollection = (IEnumerable)CreateInstanceAccessor.GetAccessor(targetType).CreateInstance();
                if (!targetType.IsPopulatableCollection(targetCollection))
                {
                    // read-only collection: trying again by initializer collection
                    if (collectionCtor != null)
                        return TryPopulateByInitializerCollection(ref context, collection, collectionCtor, targetElementType, isDictionary, out value);
                    return false;
                }

                if (!TryPopulateCollection(ref context, collection, targetCollection, targetElementType))
                    return false;
                value = targetCollection;
                return true;
            }

            private static bool TryConvertToArray(ref ConversionContext context, IEnumerable sourceCollection, Type targetType, out object? value)
            {
                value = null;
                int rank = targetType.GetArrayRank();
                Type targetElementType = targetType.GetElementType()!;
                Array targetArray;

                // multi dimension target array is supported only if the source is also an array and has the same dimension
                if (rank > 1)
                {
                    if (sourceCollection is not Array sourceArray || sourceArray.Rank != rank)
                        return false;

                    int[] lengths = new int[rank];
                    int[] lowerBounds = new int[rank];
                    for (int i = 0; i < rank; i++)
                    {
                        lengths[i] = sourceArray.GetLength(i);
                        lowerBounds[i] = sourceArray.GetLowerBound(i);
                    }

                    // ReSharper disable once AssignNullToNotNullAttribute - sourceType is an array here
                    targetArray = Array.CreateInstance(targetType.GetElementType()!, lengths, lowerBounds);
                    var indexer = new ArrayIndexer(lengths, lowerBounds);
                    foreach (object? sourceItem in sourceArray)
                    {
                        indexer.MoveNext();
                        if (!DoConvert(ref context, sourceItem, targetElementType, out object? targetItem))
                            return false;
                        targetArray.SetValue(targetItem, indexer.Current);
                    }

                    value = targetArray;
                    return true;
                }

                // single dimension target array below - case 1: source size is known
                if (sourceCollection is ICollection collection)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute - target is array in this method
                    targetArray = Array.CreateInstance(targetElementType, collection.Count);
                    int i = 0;
                    foreach (object? sourceItem in collection)
                    {
                        if (!DoConvert(ref context, sourceItem, targetElementType, out object? targetItem))
                            return false;
                        targetArray.SetValue(targetItem, i++);
                    }

                    value = targetArray;
                    return true;
                }

                // case 2: source size is not known: using a List
                IList resultList = (IList)CreateInstanceAccessor.GetAccessor(Reflector.ListGenType.GetGenericType(targetElementType)).CreateInstance();
                foreach (object? sourceItem in sourceCollection)
                {
                    if (!DoConvert(ref context, sourceItem, targetElementType, out object? targetItem))
                        return false;
                    resultList.Add(targetItem);
                }

                // ReSharper disable once AssignNullToNotNullAttribute - target is array in this method
                targetArray = Array.CreateInstance(targetElementType, resultList.Count);
                resultList.CopyTo(targetArray, 0);
                value = targetArray;
                return true;
            }

            private static bool TryPopulateByInitializerCollection(ref ConversionContext context, IEnumerable sourceCollection, ConstructorInfo collectionCtor, Type targetElementType, bool isDictionary, out object? value)
            {
                IEnumerable initializerCollection = targetElementType.CreateInitializerCollection(isDictionary);
                if (!TryPopulateCollection(ref context, sourceCollection, initializerCollection, targetElementType))
                {
                    value = null;
                    return false;
                }

                initializerCollection = initializerCollection.AdjustInitializerCollection(collectionCtor);
                value = CreateInstanceAccessor.GetAccessor(collectionCtor).CreateInstance(initializerCollection);
                return true;
            }

            private static bool TryPopulateCollection(ref ConversionContext context, IEnumerable sourceCollection, IEnumerable targetCollection, Type targetElementType)
            {
                try
                {
                    foreach (object? sourceItem in sourceCollection)
                    {
                        if (!DoConvert(ref context, sourceItem, targetElementType, out object? targetItem) || !targetCollection.TryAdd(targetItem, false))
                            return false;
                    }

                    return true;
                }
                catch (Exception e) when (!e.IsCritical())
                {
                    context.Error = e;
                    return false;
                }
            }

            private static bool TryConvertByIntermediateTypes(ref ConversionContext context, object obj, Type targetType, out object? value)
            {
                Type sourceType = obj.GetType();
                
                // Firstly, trying to use whatever source types of the exact target type
                return DoTryConvertByIntermediateTypes(ref context, obj, targetType.GetConversionSourceTypes(), targetType, out value)
                    // Alternatively, we consider all registered conversions whose source or target type are compatible with our actual types, and the string type
                    || DoTryConvertByIntermediateTypes(ref context, obj, sourceType.GetNonExactConversionIntermediateTypes(targetType), targetType, out value);
            }

            private static bool DoTryConvertByIntermediateTypes(ref ConversionContext context, object obj, ICollection<Type> intermediateTypes, Type targetType, out object? value)
            {
                if (intermediateTypes.Count > 0)
                {
                    foreach (Type intermediateType in intermediateTypes)
                    {
                        if (intermediateType.IsAbstract || intermediateType.IsInterface || intermediateType.IsAssignableFrom(targetType))
                            continue;
                        if (DoConvert(ref context, obj, intermediateType, out object? intermediateResult)
                            && DoConvert(ref context, intermediateResult, targetType, out value))
                        {
                            return true;
                        }
                    }
                }

                value = null;
                return false;
            }

            #endregion

            #endregion
        }
    }
}

