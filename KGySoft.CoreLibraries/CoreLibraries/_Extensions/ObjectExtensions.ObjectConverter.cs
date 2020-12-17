#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObjectExtensions.cs
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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

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
                #region Fields

                internal readonly CultureInfo Culture;
                internal Exception? Error;
                internal HashSet<(object Instance, Type SourceType, Type TargetType)>? FailedAttempts;
                internal Dictionary<(Type SourceType, Type TargetType), Delegate>? LastUsedConversion;

                #endregion

                #region Constructors

                [SuppressMessage("Globalization", "CA1304:Specify CultureInfo",
                    Justification = "False alarm, culture is set after calling this()")]
                internal ConversionContext(CultureInfo culture) : this() => Culture = culture; 
                
                #endregion
            }

            #endregion

            #region Constructors

            static ObjectConverter()
            {
                // Practically the [u]long -> IConvertible conversion makes no sense but these make possible every IConvertible
                // conversion via an intermediate [u]long step. Thus float types -> char/enum conversions are also possible.
                Reflector.LongType.RegisterConversion(typeof(IConvertible), (obj, type, culture) => obj);
                Reflector.ULongType.RegisterConversion(typeof(IConvertible), (obj, type, culture) => obj);

                // KeyValuePair and Dictionary entry conversions
                Reflector.KeyValuePairType.RegisterConversion(Reflector.KeyValuePairType, TryConvertKeyValuePair);
                Reflector.DictionaryEntryType.RegisterConversion(Reflector.KeyValuePairType, TryConvertDictionaryEntryToKeyValuePair);
                Reflector.KeyValuePairType.RegisterConversion(Reflector.DictionaryEntryType, ConvertKeyValuePairToDictionaryEntry);
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
                bool result = DoConvert(ref context, obj, targetType, out value);
                error = context.Error;
                return result;
            }

            #endregion

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

            private static bool DoConvert(ref ConversionContext context, object? obj, Type targetType, out object? value)
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
                    targetType = Nullable.GetUnderlyingType(targetType)!;

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
                    || obj is IConvertible convertible && typeof(IConvertible).IsAssignableFrom(targetType) && TryConvertConvertible(ref context, convertible, targetType, out value)
                    // ...by TypeConverter, except if target type is string (some type converters do some ToString, might be wrong)...
                    || targetType != Reflector.StringType && TryConvertByTypeConverter(ref context, obj, targetType, out value)
                    // ...or by non-direct conversions between source and target types (by base class/interface/generic type)
                    || TryConvertByRegisteredConversion(ref context, obj, targetType, out value, false);

                if (result)
                    return true;

                // if both source and target types are enumerable, trying to convert their types, too
                if (obj is IEnumerable collection && Reflector.IEnumerableType.IsAssignableFrom(targetType) && TryConvertCollection(ref context, collection, targetType, out value))
                    return true;

                // string target: now we try TypeConverter also for strings but as an ultimate fallback we return ToString
                if (targetType == Reflector.StringType)
                {
                    if (TryConvertByTypeConverter(ref context, obj, targetType, out value))
                        return true;
                    value = obj.ToString();
                    return true;
                }

                context.FailedAttempts ??= new HashSet<(object, Type, Type)>();
                context.FailedAttempts.Add((obj, sourceType, targetType));
                return TryConvertByIntermediateTypes(ref context, obj, targetType, out value);
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

                foreach (Delegate conversionDelegate in conversions)
                {
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
                            return conversionAttempt.Invoke(obj, targetType, context.Culture, out value) && targetType.CanAcceptValue(value);
                        case Conversion conversion:
                            value = conversion.Invoke(obj, targetType, context.Culture);
                            return targetType.CanAcceptValue(value);
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
                if (converter.CanConvertTo(targetType))
                {
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

                return false;
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

                var targetCollection = (IEnumerable)(targetType.IsValueType ? Activator.CreateInstance(targetType)! : CreateInstanceAccessor.GetAccessor(targetType).CreateInstance());
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

            [SuppressMessage("Style", "IDE0083:Use pattern matching",
                Justification = "'is not Type name' is not tolerated by ReSharper")] // TODO: fix when possible
            private static bool TryConvertToArray(ref ConversionContext context, IEnumerable sourceCollection, Type targetType, out object? value)
            {
                value = null;
                int rank = targetType.GetArrayRank();
                Type targetElementType = targetType.GetElementType()!;
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
                value = null;

                // if there are registered converters to the target type, then we try to convert the value for those
                IList<Type> sourceTypes = targetType.GetConversionSourceTypes();
                if (sourceTypes.Count == 0)
                    return false;

                foreach (Type intermediateType in sourceTypes)
                {
                    if (intermediateType.IsAbstract || intermediateType.IsInterface || intermediateType.IsAssignableFrom(targetType))
                        continue;
                    if (DoConvert(ref context, obj, intermediateType, out object? intermediateResult) && DoConvert(ref context, intermediateResult, targetType, out value))
                        return true;
                }

                return false;
            }

            #endregion
        }
    }
}

