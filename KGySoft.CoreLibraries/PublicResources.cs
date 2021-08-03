#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PublicResources.cs
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
using KGySoft.Resources;

#endregion

namespace KGySoft
{
    /// <summary>
    /// Provides localizable public string resources that can be used in any project.
    /// <br/>See the <strong>Remarks</strong> section of the <see cref="LanguageSettings"/> and <see cref="DynamicResourceManager"/> classes for examples.
    /// </summary>
    public static class PublicResources
    {
        #region Properties

        /// <summary>Looks up a localized string similar to <c>&lt;undefined&gt;</c></summary>
        /// <returns>A localized string similar to <c>&lt;undefined&gt;</c></returns>
        public static string Undefined => Res.Undefined;

        /// <summary>Looks up a localized string similar to <c>&lt;null&gt;</c></summary>
        /// <returns>A localized string similar to <c>&lt;null&gt;</c></returns>
        public static string Null => Res.Null;

        /// <summary>Looks up a localized string similar to <c>Value cannot be null.</c></summary>
        /// <returns>A localized string similar to <c>Value cannot be null.</c></returns>
        public static string ArgumentNull => Res.ArgumentNull;

        /// <summary>Looks up a localized string similar to <c>Value cannot be empty.</c></summary>
        /// <returns>A localized string similar to <c>Value cannot be empty.</c></returns>
        public static string ArgumentEmpty => Res.ArgumentEmpty;

        /// <summary>Looks up a localized string similar to <c>The collection contains no elements.</c></summary>
        /// <returns>A localized string similar to <c>The collection contains no elements.</c></returns>
        public static string CollectionEmpty => Res.CollectionEmpty;

        /// <summary>Looks up a localized string similar to <c>Specified argument contains a null element.</c></summary>
        /// <returns>A localized string similar to <c>Specified argument contains a null element.</c></returns>
        public static string ArgumentContainsNull => Res.ArgumentContainsNull;

        /// <summary>Looks up a localized string similar to <c>Specified argument was out of the range of valid values.</c></summary>
        /// <returns>A localized string similar to <c>Specified argument was out of the range of valid values.</c></returns>
        public static string ArgumentOutOfRange => Res.ArgumentOutOfRange;

        /// <summary>Looks up a localized string similar to <c>Index was outside the bounds of the array.</c></summary>
        /// <returns>A localized string similar to <c>Index was outside the bounds of the array.</c></returns>
        public static string IndexOutOfRange => Res.IndexOutOfRange;

        /// <summary>Looks up a localized string similar to <c>Cannot access a disposed object.</c></summary>
        /// <returns>A localized string similar to <c>Cannot access a disposed object.</c></returns>
        public static string ObjectDisposed => Res.ObjectDisposed;

        /// <summary>Looks up a localized string similar to <c>This operation is not supported.</c></summary>
        /// <returns>A localized string similar to <c>This operation is not supported.</c></returns>
        public static string NotSupported => Res.NotSupported;

        /// <summary>Looks up a localized string similar to <c>Input string contains an invalid value.</c></summary>
        /// <returns>A localized string similar to <c>Input string contains an invalid value.</c></returns>
        public static string ArgumentInvalidString => Res.ArgumentInvalidString;

        /// <summary>Looks up a localized string similar to <c>Maximum value must be greater than or equal to minimum value.</c></summary>
        /// <returns>A localized string similar to <c>Maximum value must be greater than or equal to minimum value.</c></returns>
        public static string MaxValueLessThanMinValue => Res.MaxValueLessThanMinValue;

        /// <summary>Looks up a localized string similar to <c>Maximum length must be greater than or equal to minimum length.</c></summary>
        /// <returns>A localized string similar to <c>Maximum length must be greater than or equal to minimum length.</c></returns>
        public static string MaxLengthLessThanMinLength => Res.MaxLengthLessThanMinLength;

        /// <summary>Looks up a localized string similar to <c>Yes</c></summary>
        /// <returns>A localized string similar to <c>Yes</c></returns>
        public static string Yes => Res.Yes;

        /// <summary>Looks up a localized string similar to <c>No</c></summary>
        /// <returns>A localized string similar to <c>No</c></returns>
        public static string No => Res.No;

        /// <summary>Looks up a localized string similar to <c>ms</c></summary>
        /// <returns>A localized string similar to <c>ms</c></returns>
        public static string Millisecond => Res.Millisecond;

        /// <summary>Looks up a localized string similar to <c>'</c></summary>
        /// <returns>A localized string similar to <c>'</c></returns>
        public static string QuoteStart => Res.QuoteStart;

        /// <summary>Looks up a localized string similar to <c>'</c></summary>
        /// <returns>A localized string similar to <c>'</c></returns>
        public static string QuoteEnd => Res.QuoteEnd;

        /// <summary>Looks up a localized string similar to <c>Offset and length were out of bounds for the array.</c></summary>
        /// <returns>A localized string similar to <c>Offset and length were out of bounds for the array.</c></returns>
        internal static string ArrayInvalidOffsLen => Res.ArrayInvalidOffsLen;

        /// <summary>Looks up a localized string similar to <c>Enumeration has either not started or has already finished.</c></summary>
        /// <returns>A localized string similar to <c>Enumeration has either not started or has already finished.</c></returns>
        public static string IEnumeratorEnumerationNotStartedOrFinished => Res.IEnumeratorEnumerationNotStartedOrFinished;

        /// <summary>Looks up a localized string similar to <c>Collection was modified; enumeration operation may not execute.</c></summary>
        /// <returns>A localized string similar to <c>Collection was modified; enumeration operation may not execute.</c></returns>
        public static string IEnumeratorCollectionModified => Res.IEnumeratorCollectionModified;

        /// <summary>Looks up a localized string similar to <c>The given key was not present in the dictionary.</c></summary>
        /// <returns>A localized string similar to <c>The given key was not present in the dictionary.</c></returns>
        public static string IDictionaryKeyNotFound => Res.IDictionaryKeyNotFound;

        /// <summary>Looks up a localized string similar to <c>An item with the same key has already been added.</c></summary>
        /// <returns>A localized string similar to <c>An item with the same key has already been added.</c></returns>
        public static string IDictionaryDuplicateKey => Res.IDictionaryDuplicateKey;

        /// <summary>Looks up a localized string similar to <c>Destination array is not long enough to copy all the items in the collection. Check array index and length.</c></summary>
        /// <returns>A localized string similar to <c>Destination array is not long enough to copy all the items in the collection. Check array index and length.</c></returns>
        public static string ICollectionCopyToDestArrayShort => Res.ICollectionCopyToDestArrayShort;

        /// <summary>Looks up a localized string similar to <c>Only single dimensional arrays are supported for the requested action.</c></summary>
        /// <returns>A localized string similar to <c>Only single dimensional arrays are supported for the requested action.</c></returns>
        public static string ICollectionCopyToSingleDimArrayOnly => Res.ICollectionCopyToSingleDimArrayOnly;

        /// <summary>Looks up a localized string similar to <c>Target array type is not compatible with the type of items in the collection.</c></summary>
        /// <returns>A localized string similar to <c>Target array type is not compatible with the type of items in the collection.</c></returns>
        public static string ICollectionArrayTypeInvalid => Res.ICollectionArrayTypeInvalid;

        /// <summary>Looks up a localized string similar to <c>Modifying a read-only collection is not supported.</c></summary>
        /// <returns>A localized string similar to <c>Modifying a read-only collection is not supported.</c></returns>
        public static string ICollectionReadOnlyModifyNotSupported => Res.ICollectionReadOnlyModifyNotSupported;

        /// <summary>Looks up a localized string similar to <c>Offset and length were out of bounds for the list or count is greater than the number of elements from index to the end of the source collection.</c></summary>
        /// <returns>A localized string similar to <c>Offset and length were out of bounds for the list or count is greater than the number of elements from index to the end of the source collection.</c></returns>
        public static string IListInvalidOffsLen => Res.IListInvalidOffsLen;

        #endregion

        #region Methods

        /// <summary>Looks up a localized string similar to <c>Specified argument must be greater than {0}.</c></summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="limit">The value of the limit.</param>
        /// <returns>A localized string similar to <c>Specified argument must be greater than {0}.</c></returns>
        public static string ArgumentMustBeGreaterThan<T>(T limit) => Res.ArgumentMustBeGreaterThan(limit);

        /// <summary>Looks up a localized string similar to <c>Specified argument must be greater than or equal to {0}.</c></summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="limit">The value of the limit.</param>
        /// <returns>A localized string similar to <c>Specified argument must be greater than or equal to {0}.</c></returns>
        public static string ArgumentMustBeGreaterThanOrEqualTo<T>(T limit) => Res.ArgumentMustBeGreaterThanOrEqualTo(limit);

        /// <summary>Looks up a localized string similar to <c>Specified argument must be less than {0}.</c></summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="limit">The value of the limit.</param>
        /// <returns>A localized string similar to <c>Specified argument must be less than {0}.</c></returns>
        public static string ArgumentMustBeLessThan<T>(T limit) => Res.ArgumentMustBeLessThan(limit);

        /// <summary>Looks up a localized string similar to <c>Specified argument must be less than or equal to {0}.</c></summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="limit">The value of the limit.</param>
        /// <returns>A localized string similar to <c>Specified argument must be less than or equal to {0}.</c></returns>
        public static string ArgumentMustBeLessThanOrEqualTo<T>(T limit) => Res.ArgumentMustBeLessThanOrEqualTo(limit);

        /// <summary>Looks up a localized string similar to <c>Specified argument must be between {0} and {1}.</c></summary>
        /// <typeparam name="T">Type of the values.</typeparam>
        /// <param name="low">The low limit.</param>
        /// <param name="high">The high limit.</param>
        /// <returns>A localized string similar to <c>Specified argument must be between {0} and {1}.</c></returns>
        public static string ArgumentMustBeBetween<T>(T low, T high) => Res.ArgumentMustBeBetween(low, high);

        /// <summary>Looks up a localized string similar to <c>Property '{0}' must not be null.</c></summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>A localized string similar to <c>Property '{0}' must not be null.</c></returns>
        public static string PropertyNull(string propertyName) => Res.PropertyNull(propertyName);

        /// <summary>Looks up a localized string similar to <c>Property '{0}' must be greater than {1}.</c></summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="limit">The value of the limit.</param>
        /// <returns>A localized string similar to <c>Property '{0}' must be greater than {1}.</c></returns>
        public static string PropertyMustBeGreaterThan<T>(string propertyName, T limit) => Res.PropertyMustBeGreaterThan(propertyName, limit);

        /// <summary>Looks up a localized string similar to <c>Property '{0}' must be greater than or equal to {1}.</c></summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="limit">The value of the limit.</param>
        /// <returns>A localized string similar to <c>Property '{0}' must be greater than or equal to {1}.</c></returns>
        public static string PropertyMustBeGreaterThanOrEqualTo<T>(string propertyName, T limit) => Res.PropertyMustBeGreaterThanOrEqualTo(propertyName, limit);

        /// <summary>Looks up a localized string similar to <c>Property '{0}' must be less than {1}.</c></summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="limit">The value of the limit.</param>
        /// <returns>A localized string similar to <c>Property '{0}' must be less than {1}.</c></returns>
        public static string PropertyMustBeLessThan<T>(string propertyName, T limit) => Res.PropertyMustBeLessThan(propertyName, limit);

        /// <summary>Looks up a localized string similar to <c>Property '{0}' must be less than or equal to {1}.</c></summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="limit">The value of the limit.</param>
        /// <returns>A localized string similar to <c>Property '{0}' must be less than or equal to {1}.</c></returns>
        public static string PropertyMustBeLessThanOrEqualTo<T>(string propertyName, T limit) => Res.PropertyMustBeLessThanOrEqualTo(propertyName, limit);

        /// <summary>Looks up a localized string similar to <c>Property '{0}' must be between {1} and {2}.</c></summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="low">The low limit.</param>
        /// <param name="high">The high limit.</param>
        /// <returns>A localized string similar to <c>Property '{0}' must be between {1} and {2}.</c></returns>
        public static string PropertyMustBeBetween<T>(string propertyName, T low, T high) => Res.PropertyMustBeBetween(propertyName, low, high);

        /// <summary>Looks up a localized string similar to <c>Property '{0}' must be greater than property '{1}'.</c></summary>
        /// <param name="propertyGreater">The name of the property to be expected to have the greater value.</param>
        /// <param name="propertyLess">The name of the property to be expected to have the less value.</param>
        /// <returns>A localized string similar to <c>Property '{0}' must be greater than property '{1}'.</c></returns>
        public static string PropertyMustBeGreaterThanProperty(string propertyGreater, string propertyLess) => Res.PropertyMustBeGreaterThanProperty(propertyGreater, propertyLess);

        /// <summary>Looks up a localized string similar to <c>Property '{0}' must be greater than or equal to property '{1}'.</c></summary>
        /// <param name="propertyGreater">The name of the property to be expected to have the greater value.</param>
        /// <param name="propertyLess">The name of the property to be expected to have the less value.</param>
        /// <returns>A localized string similar to <c>Property '{0}' must be greater than or equal to property '{1}'.</c></returns>
        public static string PropertyMustBeGreaterThanOrEqualToProperty(string propertyGreater, string propertyLess) => Res.PropertyMustBeGreaterThanOrEqualToProperty(propertyGreater, propertyLess);

        /// <summary>Looks up a localized string similar to <c>Property '{0}': {1}</c></summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="message">The message to display.</param>
        /// <returns>A localized string similar to <c>Property '{0}': {1}</c></returns>
        public static string PropertyMessage(string propertyName, string message) => Res.PropertyMessage(propertyName, message);

        /// <summary>Looks up a localized string similar to <c>Enum instance of '{0}' type must be one of the following values: {1}.</c></summary>
        /// <typeparam name="TEnum">Type of the value.</typeparam>
        /// <param name="value">The enum value.</param>
        /// <returns>A localized string similar to <c>Enum instance of '{0}' type must be one of the following values: {1}.</c></returns>
        public static string EnumOutOfRangeWithValues<TEnum>(TEnum value = default) where TEnum : struct, Enum => Res.EnumOutOfRangeWithValues(value);

        /// <summary>Looks up a localized string similar to <c>Enum instance of '{0}' type must consist of the following flags: {1}.</c></summary>
        /// <typeparam name="TEnum">Type of the value.</typeparam>
        /// <param name="value">The enum value.</param>
        /// <returns>A localized string similar to <c>Enum instance of '{0}' type must consist of the following flags: {1}.</c></returns>
        public static string FlagsEnumOutOfRangeWithValues<TEnum>(TEnum value = default) where TEnum : struct, Enum => Res.FlagsEnumOutOfRangeWithValues(value);

        /// <summary>Looks up a localized string similar to <c>Enum instance of '{0}' type must be one of the defined values.</c></summary>
        /// <typeparam name="TEnum">Type of the value.</typeparam>
        /// <param name="value">The enum value.</param>
        /// <returns>A localized string similar to <c>Enum instance of '{0}' type must be one of the defined values.</c></returns>
        public static string EnumOutOfRange<TEnum>(TEnum value = default) where TEnum : struct, Enum => Res.EnumOutOfRange(value);

        /// <summary>Looks up a localized string similar to <c>Enum instance of '{0}' type must consist of the defined flags.</c></summary>
        /// <typeparam name="TEnum">Type of the value.</typeparam>
        /// <param name="value">The enum value.</param>
        /// <returns>A localized string similar to <c>Enum instance of '{0}' type must consist of the defined flags.</c></returns>
        public static string FlagsEnumOutOfRange<TEnum>(TEnum value = default) where TEnum : struct, Enum => Res.FlagsEnumOutOfRange(value);

        /// <summary>Looks up a localized string similar to <c>Specified argument is expected to be an instance of type {0}.</c></summary>
        /// <param name="type">The expected type.</param>
        /// <returns>A localized string similar to <c>Specified argument is expected to be an instance of type {0}.</c></returns>
        public static string NotAnInstanceOfType(Type type) => Res.NotAnInstanceOfType(type);

        /// <summary>Looks up a localized string similar to <c>Value "{0}" contains illegal path characters.</c></summary>
        /// <param name="path">The invalid path.</param>
        /// <returns>A localized string similar to <c>Value "{0}" contains illegal path characters.</c></returns>
        public static string ValueContainsIllegalPathCharacters(string path) => Res.ValueContainsIllegalPathCharacters(path);

        /// <summary>Looks up a localized string similar to <c>The value "{0}" is not of type "{1}" and cannot be used in this generic collection.</c></summary>
        /// <param name="value">The provided value.</param>
        /// <param name="type">The expected type.</param>
        /// <returns>A localized string similar to <c>The value "{0}" is not of type "{1}" and cannot be used in this generic collection.</c></returns>
        public static string ICollectionNonGenericValueTypeInvalid(object value, Type type) => Res.ICollectionNonGenericValueTypeInvalid(value, type);

        /// <summary>Looks up a localized string similar to <c>The key "{0}" is not of type "{1}" and cannot be used in this generic collection.</c></summary>
        /// <param name="key">The provided key.</param>
        /// <param name="type">The expected type.</param>
        /// <returns>A localized string similar to <c>The key "{0}" is not of type "{1}" and cannot be used in this generic collection.</c></returns>
        public static string IDictionaryNonGenericKeyTypeInvalid(object key, Type type) => Res.IDictionaryNonGenericKeyTypeInvalid(key, type);

        #endregion
    }
}
