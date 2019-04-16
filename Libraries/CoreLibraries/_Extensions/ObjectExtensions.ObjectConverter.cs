﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using KGySoft.Reflection;
using KGySoft.Serialization;

namespace KGySoft.CoreLibraries
{
    public partial class ObjectExtensions
    {
        private static class ObjectConverter
        {
            static ObjectConverter()
            {
                Reflector.KeyValuePairType.RegisterConversion(Reflector.KeyValuePairType, TryConvertKeyValuePair);
                Reflector.DictionaryEntryType.RegisterConversion(Reflector.KeyValuePairType, TryConvertDictionaryEntryToKeyValuePair);
                Reflector.KeyValuePairType.RegisterConversion(Reflector.DictionaryEntryType, ConvertKeyValuePairToDictionaryEntry);
            }

            private static bool TryConvertKeyValuePair(object obj, Type targetType, CultureInfo culture, out object result)
            {
                if (obj.GetType() == targetType)
                {
                    result = obj;
                    return true;
                }

                Type[] types = targetType.GetGenericArguments();
                if (!Reflector.GetProperty(obj, nameof(KeyValuePair<_,_>.Key)).TryConvert(types[0], culture, out object key) || !Reflector.GetProperty(obj, nameof(KeyValuePair<_,_>.Value)).TryConvert(types[1], culture, out object value))
                {
                    result = null;
                    return false;
                }

                result = Reflector.CreateInstance(targetType, key, value);
                return true;
            }

            private static bool TryConvertDictionaryEntryToKeyValuePair(object obj, Type targetType, CultureInfo culture, out object result)
            {
                var source = (DictionaryEntry)obj;
                Type[] types = targetType.GetGenericArguments();
                if (!source.Key.TryConvert(types[0], culture, out object key) || !source.Value.TryConvert(types[1], culture, out object value))
                {
                    result = null;
                    return false;
                }

                result = Reflector.CreateInstance(targetType, key, value);
                return true;
            }

            private static object ConvertKeyValuePairToDictionaryEntry(object obj, Type targetType, CultureInfo culture)
                => new DictionaryEntry(Reflector.GetProperty(obj, nameof(KeyValuePair<_,_>.Key)), Reflector.GetProperty(obj, nameof(KeyValuePair<_,_>.Value)));

            private struct ConversionContext
            {
                internal readonly CultureInfo Culture;
                internal Exception Error;
                internal HashSet<(object Instance, Type SourceType, Type TargetType)> FailedAttempts;
                internal Dictionary<(Type SourceType, Type TargetType), Delegate> LastUsedConversion;

                public ConversionContext(CultureInfo culture) : this() => Culture = culture;
            }

            internal static bool TryConvert(object obj, Type targetType, CultureInfo culture, out object value, out Exception error)
            {
                if (targetType == null)
                    throw new ArgumentNullException(nameof(targetType), Res.ArgumentNull);

                error = null;
                if (culture == null)
                    culture = CultureInfo.InvariantCulture;

                var context = new ConversionContext(culture);
                bool result = DoConvert(ref context, obj, targetType, out value);
                error = context.Error;
                return result;
            }

            private static bool DoConvert(ref ConversionContext context, object obj, Type targetType, out object value)
            {
                if (targetType.IsInstanceOfType(obj))
                {
                    value = obj;
                    return true;
                }

                if (obj == null || obj is DBNull)
                {
                    value = null;
                    return targetType.CanAcceptValue(null);
                }

                if (targetType.IsNullable())
                    targetType = Nullable.GetUnderlyingType(targetType);

                Type sourceType = obj.GetType();
                if (context.FailedAttempts?.Contains((obj, sourceType, targetType)) == true)
                {
                    value = null;
                    return false;
                }

                // direct conversions between the source and target types are used in the first place
                bool result = TryConvertByRegisteredConversion(ref context, obj, targetType, out value, true)
                    // if it fails, then trying to parse from string...
                    || obj is string strValue && strValue.TryParse(targetType, context.Culture, out value)
                    // ...between IConvertibles...
                    || obj is IConvertible convertible && typeof(IConvertible).IsAssignableFrom(targetType) && TryConvertCovertible(ref context, convertible, targetType, out value)
                    // ...by TypeConverter...
                    || TryConvertByTypeConverter(ref context, obj, targetType, out value)
                    // ...or by non-direct conversions between source and target types (by base class/interface/generic type)
                    || TryConvertByRegisteredConversion(ref context, obj, targetType, out value, false);

                if (result)
                    return true;

                // if both source and target types are enumerable, trying to convert their types, too
                if (obj is IEnumerable collection && Reflector.IEnumerableType.IsAssignableFrom(targetType) && TryConvertCollection(ref context, collection, targetType, out value))
                    return true;

                if (targetType == Reflector.StringType)
                {
                    value = obj.ToString();
                    return true;
                }

                if (context.FailedAttempts == null)
                    context.FailedAttempts = new HashSet<(object, Type, Type)>();
                context.FailedAttempts.Add((obj, sourceType, targetType));

                // if there are registered converters to the target type, then we try to convert the value for those
                IList<Type> sourceTypes = targetType.GetConversionSourceTypes();
                if (sourceTypes.Count == 0)
                    return false;

                foreach (Type intermediateType in sourceTypes)
                {
                    if (intermediateType.IsAbstract || intermediateType.IsInterface || intermediateType.IsAssignableFrom(targetType))
                        continue;
                    if (DoConvert(ref context, obj, intermediateType, out object intermediateResult) && DoConvert(ref context, intermediateResult, targetType, out value))
                        return true;
                }

                return false;
            }

            private static bool TryConvertByRegisteredConversion(ref ConversionContext context, object obj, Type targetType, out object value, bool exactTypeMatch)
            {
                Type sourceType = obj.GetType();
                if (context.LastUsedConversion != null && context.LastUsedConversion.TryGetValue((sourceType, targetType), out Delegate conversion) && TryUseConversion(ref context, obj, targetType, conversion, out value))
                    return true;

                IList<Delegate> conversions = sourceType.GetConversions(targetType, exactTypeMatch);
                value = null;
                if (conversions.Count == 0)
                    return false;

                foreach (Delegate conversionDelegate in conversions)
                {
                    if (!TryUseConversion(ref context, obj, targetType, conversionDelegate, out value))
                        continue;

                    if (context.LastUsedConversion == null)
                        context.LastUsedConversion = new Dictionary<(Type, Type), Delegate>();
                    (Type, Type) key = (sourceType, targetType);
                    if (!context.LastUsedConversion.TryGetValue(key, out Delegate storedConversion) || storedConversion != conversionDelegate)
                        context.LastUsedConversion[key] = conversionDelegate;

                    return true;
                }

                return false;
            }

            private static bool TryUseConversion(ref ConversionContext context, object obj, Type targetType, Delegate conversionDelegate, out object value)
            {
                try
                {
                    switch (conversionDelegate)
                    {
                        case ConversionAttempt conversionAttempt:
                            return conversionAttempt.Invoke(obj, targetType, context.Culture, out value) && targetType.CanAcceptValue(value);
                        case Conversion conversion:
                            value = conversion.Invoke(obj, targetType, context.Culture);
                            return targetType.CanAcceptValue(value);
                        default:
                            throw new InvalidOperationException("Invalid conversion delegate type");
                    }
                }
                catch (Exception e)
                {
                    context.Error = e;
                    value = null;
                    return false;
                }
            }

            private static bool TryConvertCovertible(ref ConversionContext context, IConvertible convertible, Type targetType, out object value)
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
                catch (Exception e)
                {
                    context.Error = e;
                    value = null;
                    return false;
                }
            }

            private static bool TryConvertByTypeConverter(ref ConversionContext context, object source, Type targetType, out object value)
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
                    catch (Exception e)
                    {
                        context.Error = e;
                    }
                }

                // 2.) by source
                converter = TypeDescriptor.GetConverter(sourceType);
                if (converter.CanConvertTo(targetType))
                {
                    try
                    {
                        value = converter.ConvertTo(null, context.Culture, source, targetType);
                    }
                    catch (Exception e)
                    {
                        context.Error = e;
                        return false;
                    }
                }

                return false;
            }

            private static bool TryConvertCollection(ref ConversionContext context, IEnumerable collection, Type targetType, out object value)
            {
                if (targetType.IsArray)
                    return TryConvertToArray(ref context, collection, targetType, out value);

                value = null;
                if (!targetType.IsSupportedCollectionForReflection(out var defaultCtor, out var collectionCtor, out Type targetElementType, out bool isDictionary))
                    return false;

                if (defaultCtor == null && !targetType.IsValueType)
                    return TryPopulateByInitializerCollection(ref context, collection, collectionCtor, targetElementType, isDictionary, out value);

				var targetCollection = (IEnumerable)Reflector.CreateInstance(targetType);
                if (!targetType.IsReadWriteCollection(targetCollection))
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

            private static bool TryConvertToArray(ref ConversionContext context, IEnumerable sourceCollection, Type targetType, out object value)
            {
                value = null;
                int rank = targetType.GetArrayRank();
                Type targetElementType = targetType.GetElementType();
                Array targetArray;

                // multi dimension target array is supported only if the source is also an array and has the same dimension
                if (rank > 1)
                {
                    if (!(sourceCollection is Array sourceArray) || sourceArray.Rank != rank)
                        return false;

                    int[] lengths = new int[rank];
                    int[] lowerBounds = new int[rank];
                    for (int i = 0; i < rank; i++)
                    {
                        lengths[i] = sourceArray.GetLength(i);
                        lowerBounds[i] = sourceArray.GetLowerBound(i);
                    }

                    // ReSharper disable once AssignNullToNotNullAttribute - sourceType is an array here
                    targetArray = Array.CreateInstance(targetType.GetElementType(), lengths, lowerBounds);
                    var indexer = new ArrayIndexer(lengths, lowerBounds);
                    foreach (object sourceItem in sourceArray)
                    {
                        indexer.MoveNext();
                        if (!DoConvert(ref context, sourceItem, targetElementType, out object targetItem))
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
                    foreach (object sourceItem in collection)
                    {
                        if (!DoConvert(ref context, sourceItem, targetElementType, out object targetItem))
                            return false;
                        targetArray.SetValue(targetItem, i++);
                    }

                    value = targetArray;
                    return true;
                }

                // case 2: source size is not known: using a List
                IList resultList = (IList)Reflector.CreateInstance(Reflector.ListGenType.MakeGenericType(targetElementType));
                foreach (object sourceItem in sourceCollection)
                {
                    if (!DoConvert(ref context, sourceItem, targetElementType, out object targetItem))
                        return false;
                    resultList.Add(targetItem);
                }

                // ReSharper disable once AssignNullToNotNullAttribute - target is array in this method
                targetArray = Array.CreateInstance(targetElementType, resultList.Count);
                resultList.CopyTo(targetArray, 0);
                value = targetArray;
                return true;
            }

            private static bool TryPopulateByInitializerCollection(ref ConversionContext context, IEnumerable sourceCollection, ConstructorInfo collectionCtor, Type targetElementType, bool isDictionary, out object value)
            {
				IEnumerable initializerCollection = targetElementType.CreateInitializerCollection(isDictionary);
                if (!TryPopulateCollection(ref context, sourceCollection, initializerCollection, targetElementType))
                {
                    value = null;
                    return false;
                }

                initializerCollection = initializerCollection.AdjustInitializerCollection(collectionCtor);
                value = Reflector.CreateInstance(collectionCtor, initializerCollection);
                return true;
            }

            private static bool TryPopulateCollection(ref ConversionContext context, IEnumerable sourceCollection, IEnumerable targetCollection, Type targetElementType)
            {
                try
                {
                    foreach (object sourceItem in sourceCollection)
                    {
                        if (!DoConvert(ref context, sourceItem, targetElementType, out object targetItem) || !targetCollection.TryAdd(targetItem, false))
                            return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    context.Error = e;
                    return false;
                }
            }
        }
    }
}
