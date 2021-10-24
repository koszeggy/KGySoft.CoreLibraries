#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Res.cs
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
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

using KGySoft.Annotations;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Resources;
using KGySoft.Serialization.Binary;
using KGySoft.Serialization.Xml;

#endregion

namespace KGySoft
{
    /// <summary>
    /// Contains the string resources of the project.
    /// </summary>
    internal static class Res
    {
        #region Constants

        private const string unavailableResource = "Resource ID not found: {0}";
        private const string invalidResource = "Resource text is not valid for {0} arguments: {1}";

        #endregion

        #region Fields

        private static readonly DynamicResourceManager resourceManager = new DynamicResourceManager("KGySoft.CoreLibraries.Messages", AssemblyResolver.KGySoftCoreLibrariesAssembly)
        {
            SafeMode = true,
            UseLanguageSettings = true,
            ThrowException = false // prevents endless loop if Source is ResXOnly and trying to obtain missing resource for MissingManifestResourceException 
        };

        #endregion

        #region Properties

        #region General

        /// <summary>&lt;undefined&gt;</summary>
        internal static string Undefined => Get("General_Undefined");

        /// <summary>&lt;null&gt;</summary>
        internal static string Null => Get("General_Null");

        /// <summary>Value cannot be null.</summary>
        internal static string ArgumentNull => Get("General_ArgumentNull");

        /// <summary>Value cannot be empty.</summary>
        internal static string ArgumentEmpty => Get("General_ArgumentEmpty");

        /// <summary>The collection contains no elements.</summary>
        internal static string CollectionEmpty => Get("General_CollectionEmpty");

        /// <summary>Specified argument contains a null element.</summary>
        internal static string ArgumentContainsNull => Get("General_ArgumentContainsNull");

        /// <summary>Specified argument was out of the range of valid values.</summary>
        internal static string ArgumentOutOfRange => Get("General_ArgumentOutOfRange");

        /// <summary>Index was outside the bounds of the array.</summary>
        internal static string IndexOutOfRange => Get("General_IndexOutOfRange");

        /// <summary>Cannot access a disposed object.</summary>
        internal static string ObjectDisposed => Get("General_ObjectDisposed");

        /// <summary>This operation is not supported.</summary>
        internal static string NotSupported => Get("General_NotSupported");

        /// <summary>Input string contains an invalid value.</summary>
        internal static string ArgumentInvalidString => Get("General_ArgumentInvalidString");

        /// <summary>Maximum value must be greater than or equal to minimum value.</summary>
        internal static string MaxValueLessThanMinValue => Get("General_MaxValueLessThanMinValue");

        /// <summary>Maximum length must be greater than or equal to minimum length.</summary>
        internal static string MaxLengthLessThanMinLength => Get("General_MaxLengthLessThanMinLength");

        /// <summary>Yes</summary>
        internal static string Yes => Get("General_Yes");

        /// <summary>No</summary>
        internal static string No => Get("General_No");

        /// <summary>'</summary>
        internal static string QuoteStart => Get("General_QuoteStart");

        /// <summary>'</summary>
        internal static string QuoteEnd => Get("General_QuoteEnd");

        /// <summary>ms</summary>
        internal static string Millisecond => Get("General_Millisecond");

        /// <summary>Offset and length were out of bounds for the array.</summary>
        internal static string ArrayInvalidOffsLen => Get("Array_InvalidOffsLen");

        /// <summary>Enumeration has either not started or has already finished.</summary>
        internal static string IEnumeratorEnumerationNotStartedOrFinished => Get("IEnumerator_EnumerationNotStartedOrFinished");

        /// <summary>Collection was modified; enumeration operation may not execute.</summary>
        internal static string IEnumeratorCollectionModified => Get("IEnumerator_CollectionModified");

        /// <summary>The given key was not present in the dictionary.</summary>
        internal static string IDictionaryKeyNotFound => Get("IDictionary_KeyNotFound");

        /// <summary>An item with the same key has already been added.</summary>
        internal static string IDictionaryDuplicateKey => Get("IDictionary_DuplicateKey");

        /// <summary>Destination array is not long enough to copy all the items in the collection. Check array index and length.</summary>
        internal static string ICollectionCopyToDestArrayShort => Get("ICollection_CopyToDestArrayShort");

        /// <summary>Only single dimensional arrays are supported for the requested action.</summary>
        internal static string ICollectionCopyToSingleDimArrayOnly => Get("ICollection_CopyToSingleDimArrayOnly");

        /// <summary>Target array type is not compatible with the type of items in the collection.</summary>
        internal static string ICollectionArrayTypeInvalid => Get("ICollection_ArrayTypeInvalid");

        /// <summary>Modifying a read-only collection is not supported.</summary>
        internal static string ICollectionReadOnlyModifyNotSupported => Get("ICollection_ReadOnlyModifyNotSupported");

        /// <summary>Offset and length were out of bounds for the list or count is greater than the number of elements from index to the end of the source collection.</summary>
        internal static string IListInvalidOffsLen => Get("IList_InvalidOffsLen");

        #endregion

        #region ArraySection

        /// <summary>The underlying array is null.</summary>
        internal static string ArraySectionNull => Get("ArraySection_Null");

        /// <summary>The array section has no elements.</summary>
        internal static string ArraySectionEmpty => Get("ArraySection_Empty");

        /// <summary>The specified buffer has insufficient capacity.</summary>
        internal static string ArraySectionInsufficientCapacity => Get("ArraySection_InsufficientCapacity");

        #endregion

        #region BinarySerialization

        /// <summary>Invalid stream data.</summary>
        internal static string BinarySerializationInvalidStreamData => Get("BinarySerialization_InvalidStreamData");

        /// <summary>Deserialization of an IObjectReference instance has an unresolvable circular reference to itself.</summary>
        internal static string BinarySerializationCircularIObjectReference => Get("BinarySerialization_CircularIObjectReference");

        /// <summary>Unexpected id on deserialization. Serialization stream corrupted?</summary>
        internal static string BinarySerializationDeserializeUnexpectedId => Get("BinarySerialization_DeserializeUnexpectedId");

        /// <summary>Specified type must be a value type.</summary>
        internal static string BinarySerializationValueTypeExpected => Get("BinarySerialization_ValueTypeExpected");

        /// <summary>Data length is too small.</summary>
        internal static string BinarySerializationDataLengthTooSmall => Get("BinarySerialization_DataLengthTooSmall");

        #endregion

        #region ByteArrayExtensions

        /// <summary>The separator contains invalid characters. Hex digits are not allowed in separator.</summary>
        internal static string ByteArrayExtensionsSeparatorInvalidHex => Get("ByteArrayExtensions_SeparatorInvalidHex");

        /// <summary>The separator is empty or contains invalid characters. Decimal digits are not allowed in separator.</summary>
        internal static string ByteArrayExtensionsSeparatorInvalidDec => Get("ByteArrayExtensions_SeparatorInvalidDec");

        #endregion

        #region Cache<TKey, TValue>

        /// <summary>Cache&lt;TKey, TValue&gt; was initialized without an item loader so elements must be added explicitly either by the Add method or by setting the indexer.</summary>
        internal static string CacheNullLoaderInvoke => Get("Cache_NullLoaderInvoke");

        /// <summary>The given key was not found in the cache.</summary>
        internal static string CacheKeyNotFound => Get("Cache_KeyNotFound");

        /// <summary>Minimum cache size is 1.</summary>
        internal static string CacheMinSize => Get("Cache_MinSize");

        #endregion

        #region CircularList<T>

        /// <summary>Failed to compare two elements in the collection.</summary>
        internal static string CircularListComparerFail => Get("CircularList_ComparerFail");

        /// <summary>Capacity cannot be less than number of stored elements.</summary>
        internal static string CircularListCapacityTooSmall => Get("CircularList_CapacityTooSmall");

        #endregion

        #region CircularSortedList<T>

        /// <summary>Adding an element by index is not supported.</summary>
        internal static string CircularSortedListInsertByIndexNotSupported => Get("CircularSortedList_InsertByIndexNotSupported");

        #endregion

        #region ComponentModel

        /// <summary>Cannot add new item to the binding list because AllowNew is false.</summary>
        internal static string ComponentModelAddNewDisabled => Get("ComponentModel_AddNewDisabled");

        /// <summary>Cannot remove item from the binding list because AllowRemove is false.</summary>
        internal static string ComponentModelRemoveDisabled => Get("ComponentModel_RemoveDisabled");

        /// <summary>Cannot change ObservableBindingList during a CollectionChanged or ListChanged event.</summary>
        internal static string ComponentModelReentrancyNotAllowed => Get("ComponentModel_ReentrancyNotAllowed");

        /// <summary>Object is not in editing state.</summary>
        internal static string ComponentModelNotEditing => Get("ComponentModel_NotEditing");

        /// <summary>&lt;Missing property value&gt;</summary>
        internal static string ComponentModelMissingPropertyReference => Get("ComponentModel_MissingPropertyReference");

        /// <summary>The DoValidation method returned null.</summary>
        internal static string ComponentModelDoValidationNull => Get("ComponentModel_DoValidationNull");

        /// <summary>'Enabled' state must have a boolean value.</summary>
        internal static string ComponentModelEnabledMustBeBool => Get("ComponentModel_EnabledMustBeBool");

        /// <summary>Source must be an object instance for instance events and a Type for static events.</summary>
        internal static string ComponentModelInvalidCommandSource => Get("ComponentModel_InvalidCommandSource");

        /// <summary>Cannot add an already disposed binding to this collection.</summary>
        internal static string ComponentModelCannotAddDisposedBinding => Get("ComponentModel_CannotAddDisposedBinding");

        #endregion

        #region Enum/EnumComparer

        /// <summary>Type parameter is expected to be a System.Enum type.</summary>
        internal static string EnumTypeParameterInvalid => Get("Enum_TypeParameterInvalid");

        #endregion

        #region PerformanceTest

        /// <summary>No test cases are added.</summary>
        internal static string PerformanceTestNoTestCases => Get("PerformanceTest_NoTestCases");

        /// <summary>item</summary>
        internal static string PerformanceTestItem => Get("PerformanceTest_Item");

        /// <summary>byte</summary>
        internal static string PerformanceTestByte => Get("PerformanceTest_Byte");

        /// <summary> No difference</summary>
        internal static string PerformanceTestNoDifference => Get("PerformanceTest_NoDifference");

        /// <summary>Performance Test</summary>
        internal static string PerformanceTestDefaultName => Get("PerformanceTest_DefaultName");

        /// <summary>size (shortest first)</summary>
        internal static string PerformanceTestSortBySize => Get("PerformanceTest_SortBySize");

        /// <summary>time (quickest first)</summary>
        internal static string PerformanceTestSortByTime => Get("PerformanceTest_SortByTime");

        /// <summary>fulfilled iterations (the most first)</summary>
        internal static string PerformanceTestSortByIterations => Get("PerformanceTest_SortByIterations");

        /// <summary>--------------------------------------------------</summary>
        internal static string PerformanceTestSeparator => Get("PerformanceTest_Separator");

        /// <summary>&#x9; &lt;---- Best</summary>
        internal static string PerformanceTestBestMark => Get("PerformanceTest_BestMark");

        /// <summary>&#x9; &lt;---- Worst</summary>
        internal static string PerformanceTestWorstMark => Get("PerformanceTest_WorstMark");

        /// <summary>  Worst-Best difference: </summary>
        internal static string PerformanceTestWorstBestDiff => Get("PerformanceTest_WorstBestDiff");

        /// <summary>Dumped result:</summary>
        internal static string PerformanceTestDumpedResult => Get("PerformanceTest_DumpedResult");

        /// <summary>Error dump:</summary>
        internal static string PerformanceTestDumpedError => Get("PerformanceTest_DumpedError");

        #endregion

        #region Profiler

        /// <summary>&lt;Uncategorized&gt;</summary>
        internal static string ProfilerUncategorized => Get("Profiler_Uncategorized");

        #endregion

        #region Reflection

        /// <summary>MethodInfo or ConstructorInfo expected.</summary>
        internal static string ReflectionInvalidMethodBase => Get("Reflection_InvalidMethodBase");

        /// <summary>Cannot treat method as a property setter.</summary>
        internal static string ReflectionCannotTreatPropertySetter => Get("Reflection_CannotTreatPropertySetter");

        /// <summary>Argument must be either Type or ConstructorInfo.</summary>
        internal static string ReflectionTypeOrCtorInfoExpected => Get("Reflection_TypeOrCtorInfoExpected");

        /// <summary>Getting property via TypeDescriptor is not supported in this overload of GetProperty method.</summary>
        internal static string ReflectionGetPropertyTypeDescriptorNotSupported => Get("Reflection_GetPropertyTypeDescriptorNotSupported");

        /// <summary>Setting property via TypeDescriptor is not supported in this overload of SetProperty method.</summary>
        internal static string ReflectionSetPropertyTypeDescriptorNotSupported => Get("Reflection_SetPropertyTypeDescriptorNotSupported");

        /// <summary>A static property cannot be retrieved via TypeDescriptor.</summary>
        internal static string ReflectionCannotGetStaticPropertyTypeDescriptor => Get("Reflection_CannotGetStaticPropertyTypeDescriptor");

        /// <summary>A static property cannot be set via TypeDescriptor.</summary>
        internal static string ReflectionCannotSetStaticPropertyTypeDescriptor => Get("Reflection_CannotSetStaticPropertyTypeDescriptor");

        /// <summary>Indexer parameters are empty.</summary>
        internal static string ReflectionEmptyIndices => Get("Reflection_EmptyIndices");

        /// <summary>An indexer cannot be retrieved via TypeDescriptor.</summary>
        internal static string ReflectionGetIndexerTypeDescriptorNotSupported => Get("Reflection_GetIndexerTypeDescriptorNotSupported");

        /// <summary>An indexer cannot be set via TypeDescriptor.</summary>
        internal static string ReflectionSetIndexerTypeDescriptorNotSupported => Get("Reflection_SetIndexerTypeDescriptorNotSupported");

        /// <summary>Index parameters cannot be cast to integer values.</summary>
        internal static string ReflectionIndexParamsTypeMismatch => Get("Reflection_IndexParamsTypeMismatch");

        /// <summary>Instance is null for a non-static member.</summary>
        internal static string ReflectionInstanceIsNull => Get("Reflection_InstanceIsNull");

        /// <summary>The method to invoke is generic but no generic parameters were passed.</summary>
        internal static string ReflectionTypeParamsAreNull => Get("Reflection_TypeParamsAreNull");

        /// <summary>Could not create generic method. For details see inner exception.</summary>
        internal static string ReflectionCannotCreateGenericMethod => Get("Reflection_CannotCreateGenericMethod");

        /// <summary>Invoking a method via TypeDescriptor is not supported.</summary>
        internal static string ReflectionInvokeMethodTypeDescriptorNotSupported => Get("Reflection_InvokeMethodTypeDescriptorNotSupported");

        /// <summary>Invoking a constructor via TypeDescriptor is not supported in this overload of CreateInstance method.</summary>
        internal static string ReflectionInvokeCtorTypeDescriptorNotSupported => Get("Reflection_InvokeCtorTypeDescriptorNotSupported");

        /// <summary>A field cannot be set via TypeDescriptor.</summary>
        internal static string ReflectionSetFieldTypeDescriptorNotSupported => Get("Reflection_SetFieldTypeDescriptorNotSupported");

        /// <summary>A field cannot be retrieved via TypeDescriptor.</summary>
        internal static string ReflectionGetFieldTypeDescriptorNotSupported => Get("Reflection_GetFieldTypeDescriptorNotSupported");

        /// <summary>Expression is not a method call.</summary>
        internal static string ReflectionNotAMethod => Get("Reflection_NotAMethod");

        /// <summary>In this ResolveType overload the type name should not contain the assembly name.</summary>
        internal static string ReflectionTypeWithAssemblyName => Get("Reflection_TypeWithAssemblyName");

        /// <summary>DeclaringType of the provided member should not be null.</summary>
        internal static string ReflectionDeclaringTypeExpected => Get("Reflection_DeclaringTypeExpected");

        /// <summary>This operation is not supported if the executing assembly specifies both the AllowPartiallyTrustedCallersAttribute and the SecurityRulesAttribute with SecurityRuleSet.Level2 (which is the default if not defined). You can try the following options:
        /// - Use the SecurityRulesAttribute with SecurityRuleSet.Level1. This is the recommended solution.
        /// - Use the SecurityRulesAttribute with SkipVerificationInFullTrust = true. If used with SecurityRuleSet.Level2 this will not solve the problem from a partially trusted domain though.
        /// - Remove the AllowPartiallyTrustedCallersAttribute. This will not allow partially trusted callers to use your assembly though.</summary>
        internal static string ReflectionSecuritySettingsConflict => Get("Reflection_SecuritySettingsConflict");

        #endregion

        #region Resources

        /// <summary>Property can be changed only before the enumeration.</summary>
        internal static string ResourcesInvalidResXReaderPropertyChange => Get("Resources_InvalidResXReaderPropertyChange");

        /// <summary>Property can be changed only before adding a row or generating any content.</summary>
        internal static string ResourcesInvalidResXWriterPropertyChange => Get("Resources_InvalidResXWriterPropertyChange");

        /// <summary>Resource writer has already been saved. You may not edit it.</summary>
        internal static string ResourcesWriterSaved => Get("Resources_WriterSaved");

        /// <summary>This operation is invalid when Source is CompiledOnly.</summary>
        internal static string ResourcesHybridResSourceBinary => Get("Resources_HybridResSourceBinary");

        /// <summary>Setting this property is invalid when UseLanguageSettings is true.</summary>
        internal static string ResourcesInvalidDrmPropertyChange => Get("Resources_InvalidDrmPropertyChange");

        #endregion

        #region Serialization (any ways)

        /// <summary>Simple runtime element types or generic type definitions are expected.</summary>
        internal static string SerializationRootTypeExpected => Get("Serialization_RootTypeExpected");

        #endregion

        #region StreamExtensions

        /// <summary>Source stream cannot be read.</summary>
        internal static string StreamExtensionsStreamCannotRead => Get("StreamExtensions_StreamCannotRead");

        /// <summary>Destination stream cannot be written.</summary>
        internal static string StreamExtensionsStreamCannotWrite => Get("StreamExtensions_StreamCannotWrite");

        #endregion

        #region StringExtensions

        /// <summary>Separator is null or empty.</summary>
        internal static string StringExtensionsSeparatorNullOrEmpty => Get("StringExtensions_SeparatorNullOrEmpty");

        /// <summary>Source must consist of even amount of hex digits.</summary>
        internal static string StringExtensionsSourceLengthNotEven => Get("StringExtensions_SourceLengthNotEven");

        #endregion

        #region StringSegment

        /// <summary>The string segment represents a null string.</summary>
        internal static string StringSegmentNull => Get("StringSegment_Null");

        #endregion

        #region XmlSerialization

        /// <summary>Type of the root element is not specified.</summary>
        internal static string XmlSerializationRootTypeMissing => Get("XmlSerialization_RootTypeMissing");

        /// <summary>Array length or dimensions are not specified.</summary>
        internal static string XmlSerializationArrayNoLength => Get("XmlSerialization_ArrayNoLength");

        /// <summary>Corrupt array data: Bad CRC.</summary>
        internal static string XmlSerializationCrcError => Get("XmlSerialization_CrcError");

        /// <summary>Mixed compact and non-compact array content found.</summary>
        internal static string XmlSerializationMixedArrayFormats => Get("XmlSerialization_MixedArrayFormats");

        /// <summary>Key element not found in key/value pair element.</summary>
        internal static string XmlSerializationKeyValueMissingKey => Get("XmlSerialization_KeyValueMissingKey");

        /// <summary>Value element not found in key/value pair element.</summary>
        internal static string XmlSerializationKeyValueMissingValue => Get("XmlSerialization_KeyValueMissingValue");

        /// <summary>Multiple Key elements occurred in key-value element.</summary>
        internal static string XmlSerializationMultipleKeys => Get("XmlSerialization_MultipleKeys");

        /// <summary>Multiple Value elements occurred in key-value element.</summary>
        internal static string XmlSerializationMultipleValues => Get("XmlSerialization_MultipleValues");

        /// <summary>Unexpected end of XML content.</summary>
        internal static string XmlSerializationUnexpectedEnd => Get("XmlSerialization_UnexpectedEnd");

        /// <summary>It is not allowed to deserialize a BinarySerializationFormatter content in safe mode.</summary>
        internal static string XmlSerializationBinarySerializerSafe => Get("XmlSerialization_BinarySerializerSafe");

        #endregion

        #endregion

        #region Methods

        #region Internal Methods

        #region General

        /// <summary>
        /// Just an empty method to be able to trigger the static constructor without running any code other than field initializations.
        /// </summary>
        internal static void Initialize()
        {
        }

        /// <summary>Specified argument must be greater than {0}.</summary>
        internal static string ArgumentMustBeGreaterThan<T>(T limit) => Get("General_ArgumentMustBeGreaterThanFormat", limit);

        /// <summary>Specified argument must be greater than or equal to {0}.</summary>
        internal static string ArgumentMustBeGreaterThanOrEqualTo<T>(T limit) => Get("General_ArgumentMustBeGreaterThanOrEqualToFormat", limit);

        /// <summary>Specified argument must be less than {0}.</summary>
        internal static string ArgumentMustBeLessThan<T>(T limit) => Get("General_ArgumentMustBeGreaterThanFormat", limit);

        /// <summary>Specified argument must be less than or equal to {0}.</summary>
        internal static string ArgumentMustBeLessThanOrEqualTo<T>(T limit) => Get("General_ArgumentMustBeLessThanOrEqualToFormat", limit);

        /// <summary>Specified argument must be between {0} and {1}.</summary>
        internal static string ArgumentMustBeBetween<T>(T low, T high) => Get("General_ArgumentMustBeBetweenFormat", low, high);

        /// <summary>Property '{0}' must not be null.</summary>
        internal static string PropertyNull(string propertyName) => Get("General_PropertyNullFormat", propertyName);

        /// <summary>Property '{0}' must be greater than {1}.</summary>
        internal static string PropertyMustBeGreaterThan<T>(string propertyName, T limit) => Get("General_PropertyMustBeGreaterThanFormat", propertyName, limit);

        /// <summary>Property '{0}' must be greater than or equal to {1}.</summary>
        internal static string PropertyMustBeGreaterThanOrEqualTo<T>(string propertyName, T limit) => Get("General_PropertyMustBeGreaterThanOrEqualToFormat", propertyName, limit);

        /// <summary>Property '{0}' must be less than {1}.</summary>
        internal static string PropertyMustBeLessThan<T>(string propertyName, T limit) => Get("General_PropertyMustBeLessThanFormat", propertyName, limit);

        /// <summary>Property '{0}' must be less than or equal to {1}.</summary>
        internal static string PropertyMustBeLessThanOrEqualTo<T>(string propertyName, T limit) => Get("General_PropertyMustBeLessThanOrEqualToFormat", propertyName, limit);

        /// <summary>Property '{0}' must be between {1} and {2}.</summary>
        internal static string PropertyMustBeBetween<T>(string propertyName, T low, T high) => Get("General_PropertyMustBeBetweenFormat", propertyName, low, high);

        /// <summary>Property '{0}' must be greater than property '{1}'.</summary>
        internal static string PropertyMustBeGreaterThanProperty(string propertyGreater, string propertyLess) => Get("General_PropertyMustBeGreaterThanPropertyFormat", propertyGreater, propertyLess);

        /// <summary>Property '{0}' must be greater than or equal to property '{1}'.</summary>
        internal static string PropertyMustBeGreaterThanOrEqualToProperty(string propertyGreater, string propertyLess) => Get("General_PropertyMustBeGreaterThanOrEqualToPropertyFormat", propertyGreater, propertyLess);

        /// <summary>Property '{0}': {1}</summary>
        internal static string PropertyMessage(string propertyName, string message) => Get("General_PropertyMessageFormat", propertyName, message);

        /// <summary>Enum instance of '{0}' type must be one of the following values: {1}.</summary>
        internal static string EnumOutOfRangeWithValues<TEnum>(TEnum value = default) where TEnum : struct, Enum => Get("General_EnumOutOfRangeWithValuesFormat", value.GetType().Name, FormatEnumValues<TEnum>());

        /// <summary>Enum instance of '{0}' type must consist of the following flags: {1}.</summary>
        internal static string FlagsEnumOutOfRangeWithValues<TEnum>(TEnum value = default) where TEnum : struct, Enum => Get("General_FlagsEnumOutOfRangeWithValuesFormat", value.GetType().Name, FormatEnumFlags<TEnum>());

        /// <summary>Enum instance of '{0}' type must be one of the defined values.</summary>
        internal static string EnumOutOfRange<TEnum>(TEnum value = default) where TEnum : struct, Enum => Get("General_EnumOutOfRangeFormat", value.GetType().Name);

        /// <summary>Enum instance of '{0}' type must consist of the defined flags.</summary>
        internal static string FlagsEnumOutOfRange<TEnum>(TEnum value = default) where TEnum : struct, Enum => Get("General_FlagsEnumOutOfRangeFormat", value.GetType().Name);

        /// <summary>Specified argument is expected to be an instance of type {0}.</summary>
        internal static string NotAnInstanceOfType(Type type) => Get("General_NotAnInstanceOfTypeFormat", type);

        /// <summary>Value "{0}" contains illegal path characters.</summary>
        internal static string ValueContainsIllegalPathCharacters(string path) => Get("General_ValueContainsIllegalPathCharactersFormat", path);

        /// <summary>The value "{0}" is not of type "{1}" and cannot be used in this generic collection.</summary>
        internal static string ICollectionNonGenericValueTypeInvalid(object? value, Type type) => Get("ICollection_NonGenericValueTypeInvalidFormat", value, type);

        /// <summary>The key "{0}" is not of type "{1}" and cannot be used in this generic collection.</summary>
        internal static string IDictionaryNonGenericKeyTypeInvalid(object key, Type type) => Get("IDictionary_NonGenericKeyTypeInvalidFormat", key, type);

        #endregion

        #region General Internal

        /// <summary>Internal Error: {0}</summary>
        /// <remarks>Use this method to avoid CA1303 for using string literals in internal errors that never supposed to occur.</remarks>
        internal static string InternalError(string msg) => Get("General_InternalErrorFormat", msg);

        #endregion

        #region BinarySerialization

        /// <summary>Serialization of type {0} is not supported with following serialization options: {1}.
        /// You can try to enable the RecursiveSerializationAsFallback flag, though the serialized data will possibly not be able to be deserialized using the SafeMode flag.</summary>
        internal static string BinarySerializationNotSupported(Type type, BinarySerializationOptions options) => Get("BinarySerialization_NotSupportedFormat", type, options.ToString<BinarySerializationOptions>());

        /// <summary>An IEnumerable type expected but {0} found during deserialization.</summary>
        internal static string BinarySerializationIEnumerableExpected(Type type) => Get("BinarySerialization_IEnumerableExpectedFormat", type);

        /// <summary>Invalid enum base type: {0}. Serialization stream corrupted?</summary>
        internal static string BinarySerializationInvalidEnumBase(string dataType) => Get("BinarySerialization_InvalidEnumBaseFormat", dataType);

        /// <summary>Cannot deserialize as a standalone object: {0}</summary>
        internal static string BinarySerializationCannotDeserializeObject(string dataType) => Get("BinarySerialization_CannotDeserializeObjectFormat", dataType);

        /// <summary>Cannot deserialize type on this platform: {0}</summary>
        internal static string BinarySerializationTypePlatformNotSupported(string dataType) => Get("BinarySerialization_TypePlatformNotSupportedFormat", dataType);

        /// <summary>Type "{0}" cannot be deserialized because its type hierarchy has been changed since serialization. Use IgnoreObjectChanges option to suppress this exception.</summary>
        internal static string BinarySerializationObjectHierarchyChanged(Type type) => Get("BinarySerialization_ObjectHierarchyChangedFormat", type);

        /// <summary>Generic method with signature "{0}" was not found in type "{1}".</summary>
        internal static string BinarySerializationGenericMethodNotFound(string signature, Type type) => Get("BinarySerialization_GenericMethodNotFoundFormat", signature, type);

        /// <summary>Type "{0}" cannot be deserialized because it has no field "{1}". To call the deserialization constructor implement the ISerializable interface. Use IgnoreObjectChanges option to suppress this exception.</summary>
        internal static string BinarySerializationMissingField(Type type, string field) => Get("BinarySerialization_MissingFieldFormat", type, field);

        /// <summary>Type "{0}" cannot be deserialized because field "{1}" not found in type "{2}". Use IgnoreObjectChanges option to suppress this exception.</summary>
        internal static string BinarySerializationMissingFieldBase(Type type, string field, Type baseType) => Get("BinarySerialization_MissingFieldBaseFormat", type, field, baseType);

        /// <summary>Type "{0}" does not have a special constructor to deserialize it as ISerializable</summary>
        internal static string BinarySerializationMissingISerializableCtor(Type type) => Get("BinarySerialization_MissingISerializableCtorFormat", type);

        /// <summary>The serialization surrogate has changed the reference of the result object, which prevented resolving circular references to itself. Object type: {0}</summary>
        internal static string BinarySerializationSurrogateChangedObject(Type type) => Get("BinarySerialization_SurrogateChangedObjectFormat", type);

        /// <summary>Could not decode data type: {0}. Serialization stream corrupted?</summary>
        internal static string BinarySerializationCannotDecodeDataType(string dataType) => Get("BinarySerialization_CannotDecodeDataTypeFormat", dataType);

        /// <summary>Could not decode collection type: {0}. Serialization stream corrupted?</summary>
        internal static string BinarySerializationCannotDecodeCollectionType(string dataType) => Get("BinarySerialization_CannotDecodeCollectionTypeFormat", dataType);

        /// <summary>Creating read-only collection of type "{0}" is not supported. Serialization stream corrupted?</summary>
        internal static string BinarySerializationReadOnlyCollectionNotSupported(string dataType) => Get("BinarySerialization_ReadOnlyCollectionNotSupportedFormat", dataType);

        /// <summary>Cannot resolve assembly in safe mode: "{0}".
        /// You may try to preload the assembly before deserialization or to disable SafeMode if the serialization stream is from a trusted source.</summary>
        internal static string BinarySerializationCannotResolveAssemblySafe(string name) => Get("BinarySerialization_CannotResolveAssemblySafeFormat", name);

        /// <summary>Could not resolve type name "{0}".</summary>
        internal static string BinarySerializationCannotResolveType(string dataType) => Get("BinarySerialization_CannotResolveTypeFormat", dataType);

        /// <summary>Could not resolve type "{0}" in assembly "{1}".</summary>
        internal static string BinarySerializationCannotResolveTypeInAssembly(string typeName, string asmName) => Get("BinarySerialization_CannotResolveTypeInAssemblyFormat", typeName, asmName);

        /// <summary>Could not resolve type "{0}" in assembly "{1}".
        /// You may try to preload the assembly before deserialization or disable SafeMode if the serialization stream is from a trusted source.</summary>
        internal static string BinarySerializationCannotResolveTypeInAssemblySafe(string typeName, string asmName) => Get("BinarySerialization_CannotResolveTypeInAssemblySafeFormat", typeName, asmName);

        /// <summary>Unexpected element in serialization info: {0}. Maybe the instance was not serialized by NameInvariantSurrogateSelector.</summary>
        internal static string BinarySerializationUnexpectedSerializationInfoElement(string elementName) => Get("BinarySerialization_UnexpectedSerializationInfoElementFormat", elementName);

        /// <summary>Object hierarchy has been changed since serialization of type "{0}".</summary>
        internal static string BinarySerializationObjectHierarchyChangedSurrogate(Type type) => Get("BinarySerialization_ObjectHierarchyChangedSurrogateFormat", type);

        /// <summary>Number of serializable fields in type "{0}" has been decreased since serialization so cannot deserialize type "{1}".</summary>
        internal static string BinarySerializationMissingFieldSurrogate(Type baseType, Type type) => Get("BinarySerialization_MissingFieldSurrogateFormat", baseType, type);

        /// <summary>Fields might have been reordered since serialization. Cannot deserialize type "{0}" because cannot assign value "{1}" to field "{2}.{3}".</summary>
        internal static string BinarySerializationUnexpectedFieldType(Type type, object? value, Type declaringType, string fieldName) => Get("BinarySerialization_UnexpectedFieldTypeFormat", type, value, declaringType, fieldName);

        /// <summary>The current domain has insufficient permissions to create an empty instance of type "{0}" without a default constructor.</summary>
        internal static string BinarySerializationCannotCreateUninitializedObject(Type type) => Get("BinarySerialization_CannotCreateUninitializedObjectFormat", type);

        /// <summary>In safe mode it is not supported to deserialize type "{0}". Maybe because it is not marked by the SerializableAttribute.</summary>
        internal static string BinarySerializationCannotCreateObjectSafe(Type type) => Get("BinarySerialization_CannotCreateObjectSafeFormat", type);

        /// <summary>Type '{0}' was serialized as an IBinarySerializable instance though it is not IBinarySerializable now.</summary>
        internal static string BinarySerializationNotBinarySerializable(Type type) => Get("BinarySerialization_NotBinarySerializableFormat", type);

        /// <summary>Type '{0}' was serialized as a raw value type, though it is not a value type now.</summary>
        internal static string BinarySerializationNotAValueType(Type type) => Get("BinarySerialization_NotAValueTypeFormat", type);

        /// <summary>Type '{0}' was serialized as an enum type though it is not an enum now.</summary>
        internal static string BinarySerializationNotAnEnum(Type type) => Get("BinarySerialization_NotAnEnumFormat", type);

        /// <summary>Deserialization of an IObjectReference instance has an unresolvable circular reference to itself as an element in a collection of type '{0}'. Either try to use the ForceRecursiveSerializationOfSupportedTypes option on serialization, or avoid serializing circular references in the container object.</summary>
        internal static string BinarySerializationCircularIObjectReferenceCollection(Type type) => Get("BinarySerialization_CircularIObjectReferenceCollectionFormat", type);

        /// <summary>The stream contains a collection of type '{0}', which is not supported on this platform.</summary>
        internal static string BinarySerializationCollectionPlatformNotSupported(string dataType) => Get("BinarySerialization_CollectionPlatformNotSupportedFormat", dataType);

        /// <summary>Type '{0}' cannot be the type argument of this method because it contains references.</summary>
        internal static string BinarySerializationValueTypeContainsReferences<T>() => Get("BinarySerialization_ValueTypeContainsReferencesFormat", typeof(T));

        /// <summary>Value type '{0}' cannot be deserialized from raw data in safe mode because it contains references. If the serialization stream is from a trusted source you may try to disable safe mode to attempt the deserialization with marshaling.</summary>
        internal static string BinarySerializationValueTypeContainsReferenceSafe(Type type) => Get("BinarySerialization_ValueTypeContainsReferenceSafeFormat", type.GetName(TypeNameKind.LongName));

        #endregion

        #region Cache<TKey, TValue>

        /// <summary>Cache&lt;{0}, {1}&gt; cache statistics:
        /// <br/>Count: {2}
        /// <br/>Capacity: {3}
        /// <br/>Number of writes: {4}
        /// <br/>Number of reads: {5}
        /// <br/>Number of cache hits: {6}
        /// <br/>Number of deletes: {7}
        /// <br/>Hit rate: {8:P2}</summary>
        internal static string CacheStatistics(string keyName, string valueName, int count, int capacity, int writes, int reads, int hits, int deletes, float rate) => Get("Cache_StatisticsFormat", keyName, valueName, count, capacity, writes, reads, hits, deletes, rate);

        #endregion

        #region CircularSortedList<T>

        /// <summary>Type of value should be either {0} or DictionaryEntry.</summary>
        internal static string CircularSortedListInvalidKeyValueType(Type type) => Get("CircularSortedList_InvalidKeyValueTypeFormat", type);

        #endregion

        #region ComponentModel

        /// <summary>Property '{0}' of descriptor type '{1}' does not belong to type '{2}'.</summary>
        internal static string ComponentModelInvalidProperty(PropertyDescriptor property, Type t) => Get("ComponentModel_InvalidPropertyFormat", property.Name, property.GetType(), t);

        /// <summary>Cannot add new item to the binding list because type '{0}' cannot be constructed without parameters. Subscribe the AddingNew event or override the AddNewCore or OnAddingNew methods to create a new item to add.</summary>
        internal static string ComponentModelCannotAddNewFastBindingList(Type t) => Get("ComponentModel_CannotAddNewFastBindingListFormat", t);

        /// <summary>No property descriptor found for property name '{0}' in type '{1}'.</summary>
        internal static string ComponentModelPropertyNotExists(string propertyName, Type type) => Get("ComponentModel_PropertyNotExistsFormat", propertyName, type);

        /// <summary>Cannot add new item to the binding list because type '{0}' cannot be constructed without parameters.</summary>
        internal static string ComponentModelCannotAddNewObservableBindingList(Type t) => Get("ComponentModel_CannotAddNewObservableBindingListFormat", t);

        /// <summary>The property binding command state does not contain the expected entry '{0}'.</summary>
        internal static string ComponentModelMissingState(string stateName) => Get("ComponentModel_MissingStateFormat", stateName);

        /// <summary>There is no event '{0}' in type '{1}'.</summary>
        internal static string ComponentModelMissingEvent(string eventName, Type type) => Get("ComponentModel_MissingEventFormat", eventName, type);

        /// <summary>Event '{0}' does not have regular event handler delegate type or accessors.</summary>
        internal static string ComponentModelInvalidEvent(string eventName) => Get("ComponentModel_InvalidEventFormat", eventName);

        /// <summary>Cannot get property '{0}'.</summary>
        internal static string ComponentModelCannotGetProperty(string propertyName) => Get("ComponentModel_CannotGetPropertyFormat", propertyName);

        /// <summary>Cannot set property '{0}'.</summary>
        internal static string ComponentModelCannotSetProperty(string propertyName) => Get("ComponentModel_CannotSetPropertyFormat", propertyName);

        /// <summary>The returned value is not compatible with type {0}</summary>
        internal static string ComponentModelReturnedTypeInvalid(Type type) => Get("ComponentModel_ReturnedTypeInvalidFormat", type);

        /// <summary>No value exists for property '{0}'.</summary>
        internal static string ComponentModelPropertyValueNotExist(string propertyName) => Get("ComponentModel_PropertyValueNotExistFormat", propertyName);

        /// <summary>The type has no parameterless constructor and thus cannot be cloned: {0}</summary>
        internal static string ComponentModelObservableObjectHasNoDefaultCtor(Type type) => Get("ComponentModel_ObservableObjectHasNoDefaultCtorFormat", type);

        #endregion

        #region Enum

        /// <summary>Value '{0}' cannot be parsed as enumeration type {1}</summary>
        internal static string EnumValueCannotBeParsedAsEnum(string value, Type enumType) => Get("Enum_ValueCannotBeParsedAsEnumFormat", value, enumType);

        #endregion

        #region ObjectExtensions

        /// <summary>The specified argument cannot be converted to type {0}.</summary>
        internal static string ObjectExtensionsCannotConvertToType(Type type) => Get("ObjectExtensions_CannotConvertToTypeFormat", type);

        #endregion

        #region PerformanceTest

        /// <summary>Case #{0}</summary>
        internal static string PerformanceTestCaseDefaultName(int testCaseNo) => Get("PerformanceTest_CaseDefaultNameFormat", testCaseNo);

        /// <summary>{0}: </summary>
        internal static string PerformanceTestCaseName(string name) => Get("PerformanceTest_CaseNameFormat", name);

        /// <summary>==[{0} Results]================================================</summary>
        internal static string PerformanceTestHeader(string testName) => Get("PerformanceTest_HeaderFormat", testName);

        /// <summary>Iterations: {0:N0}</summary>
        internal static string PerformanceTestIterations(int iterations) => Get("PerformanceTest_IterationsFormat", iterations);

        /// <summary>Test Time: {0:N0} ms</summary>
        internal static string PerformanceTestTestTime(int testTime) => Get("PerformanceTest_TestTimeFormat", testTime);

        /// <summary>Warming up: {0}</summary>
        internal static string PerformanceTestWarmingUp(bool value) => Get("PerformanceTest_WarmingUpFormat", FormatBool(value));

        /// <summary>Test cases: {0}</summary>
        internal static string PerformanceTestTestCases(int count) => Get("PerformanceTest_TestCasesFormat", count);

        /// <summary>Repeats: {0}</summary>
        internal static string PerformanceTestRepeats(int count) => Get("PerformanceTest_RepeatsFormat", count);

        /// <summary>Calling GC.Collect: {0}</summary>
        internal static string PerformanceTestCallingGcCollect(bool value) => Get("PerformanceTest_CallingGcCollectFormat", FormatBool(value));

        /// <summary>Forced CPU Affinity: {0}</summary>
        internal static string PerformanceTestCpuAffinity(int? affinity) => Get("PerformanceTest_CpuAffinityFormat", affinity == null ? No : affinity.ToString());

        /// <summary>Cases are sorted by {0}</summary>
        internal static string PerformanceTestSortOfCases(string sort) => Get("PerformanceTest_SortOfCasesFormat", sort);

        /// <summary>average time: {0:N2} ms</summary>
        internal static string PerformanceTestCaseAverageTime(double msecs) => Get("PerformanceTest_CaseAverageTimeFormat", msecs);

        /// <summary>{0}. </summary>
        internal static string PerformanceTestCaseOrder(int order) => Get("PerformanceTest_CaseOrderFormat", order);

        /// <summary> ({0}{1:N2}{2} / {3:P2})</summary>
        internal static string PerformanceTestDifference(string sign, double diff, string unit, double currentPerBaseValue) => Get("PerformanceTest_DifferenceFormat", sign, diff, unit, currentPerBaseValue);

        /// <summary>{0:N0} iterations in {1:N2} ms. Adjusted for {2:N0} ms: {3:N2}</summary>
        internal static string PerformanceTestCaseIterations(int totalIterations, double totalMilliseconds, int testTime, double averageIterations) => Get("PerformanceTest_CaseIterationsFormat", totalIterations, totalMilliseconds, testTime, averageIterations);

        /// <summary>  #{0,-2} {1}</summary>
        internal static string PerformanceTestCaseRepetitionOrder(int order, string content) => Get("PerformanceTest_CaseRepetitionOrderFormat", order, content);

        /// <summary>{0,13:N2} ms</summary>
        internal static string PerformanceTestCaseRepetitionTime(double ms) => Get("PerformanceTest_CaseRepetitionTimeFormat", ms);

        /// <summary>{0:N0} iterations in {1:N2} ms. Adjusted: {2:N2}</summary>
        internal static string PerformanceTestCaseRepetitionIterations(int iterations, double totalMilliseconds, double averageIterationsPerTestTime) => Get("PerformanceTest_CaseRepetitionIterationsFormat", iterations, totalMilliseconds, averageIterationsPerTestTime);

        /// <summary>{0}: {1}</summary>
        internal static string PerformanceTestCaseError(Type type, string message) => Get("PerformanceTest_CaseErrorFormat", type, message);

        /// <summary>{0:N2} ms ({1:P2})</summary>
        internal static string PerformanceTestWorstBestDiffTime(double totalMilliseconds, double percent) => Get("PerformanceTest_WorstBestDiffTimeFormat", totalMilliseconds, percent);

        /// <summary>{0:N2} ({1:P2})</summary>
        internal static string PerformanceTestWorstBestDiffIteration(double diff, double percent) => Get("PerformanceTest_WorstBestDiffIterationFormat", diff, percent);

        /// <summary>  Result size: {0:N0} {1}</summary>
        internal static string PerformanceTestResultSize(int length, string unit) => Get("PerformanceTest_ResultSizeFormat", length, unit);

        /// <summary>{0}(s)</summary>
        internal static string PerformanceTestUnitPossiblePlural(string unit) => Get("PerformanceTest_UnitPossiblePluralFormat", unit);

        #endregion

        #region Profiler

        /// <summary>[{0}]{1}: Average Time: {2}; Total Time: {4}; First Call: {3}; Number of Calls: {5:N0}</summary>
        internal static string ProfilerMeasureItemToString(string category, string operation, TimeSpan averageTime, TimeSpan firstCall, TimeSpan totalTime, long calls) => Get("Profiler_MeasureItemToStringFormat", category, operation, averageTime, firstCall, totalTime, calls);

        #endregion

        #region Reflection

        /// <summary>The constant field cannot be set: {0}.{1}</summary>
        internal static string ReflectionCannotSetConstantField(Type? type, string memberName) => Get("Reflection_CannotSetConstantFieldFormat", type, memberName);

        /// <summary>Member type {0} is not supported.</summary>
        internal static string ReflectionNotSupportedMemberType(MemberTypes memberType) => Get("Reflection_NotSupportedMemberTypeFormat", memberType);

        /// <summary>Property has no getter accessor: {0}.{1}</summary>
        internal static string ReflectionPropertyHasNoGetter(Type? type, string memberName) => Get("Reflection_PropertyHasNoGetterFormat", type, memberName);

        /// <summary>Property has no setter accessor: {0}.{1}</summary>
        internal static string ReflectionPropertyHasNoSetter(Type? type, string memberName) => Get("Reflection_PropertyHasNoSetterFormat", type, memberName);

        /// <summary>Value "{0}" cannot be resolved as a System.Type.</summary>
        internal static string ReflectionNotAType(string value) => Get("Reflection_NotATypeFormat", value);

        /// <summary>Property "{0}" not found and cannot be set via TypeDescriptor on type "{1}".</summary>
        internal static string ReflectionPropertyNotFoundTypeDescriptor(string propertyName, Type type) => Get("Reflection_PropertyNotFoundTypeDescriptorFormat", propertyName, type);

        /// <summary>No suitable instance property "{0}" found on type "{1}".</summary>
        internal static string ReflectionInstancePropertyDoesNotExist(string propertyName, Type type) => Get("Reflection_InstancePropertyDoesNotExistFormat", propertyName, type);

        /// <summary>No suitable static property "{0}" found on type "{1}".</summary>
        internal static string ReflectionStaticPropertyDoesNotExist(string propertyName, Type type) => Get("Reflection_StaticPropertyDoesNotExistFormat", propertyName, type);

        /// <summary>Expected number of array index arguments: {0}.</summary>
        internal static string ReflectionIndexParamsLengthMismatch(int length) => Get("Reflection_IndexParamsLengthMismatchFormat", length);

        /// <summary>No suitable indexer found on type "{0}" for the passed parameters.</summary>
        internal static string ReflectionIndexerNotFound(Type type) => Get("Reflection_IndexerNotFoundFormat", type);

        /// <summary>Property "{0}" not found and cannot be retrieved via TypeDescriptor on type "{1}".</summary>
        internal static string ReflectionCannotGetPropertyTypeDescriptor(string propertyName, Type type) => Get("Reflection_CannotGetPropertyTypeDescriptorFormat", propertyName, type);

        /// <summary>Expected number of generic parameters: {0}.</summary>
        internal static string ReflectionTypeArgsLengthMismatch(int length) => Get("Reflection_TypeArgsLengthMismatchFormat", length);

        /// <summary>No suitable instance method "{0}" found on type "{1}" for the given parameters.</summary>
        internal static string ReflectionInstanceMethodNotFound(string methodName, Type type) => Get("Reflection_InstanceMethodNotFoundFormat", methodName, type);

        /// <summary>No suitable static method "{0}" found on type "{1}" for the given parameters.</summary>
        internal static string ReflectionStaticMethodNotFound(string methodName, Type type) => Get("Reflection_StaticMethodNotFoundFormat", methodName, type);

        /// <summary>No suitable constructor found on type "{0}" for the given parameters.</summary>
        internal static string ReflectionCtorNotFound(Type type) => Get("Reflection_CtorNotFoundFormat", type);

        /// <summary>Instance field "{0}" not found on type "{1}".</summary>
        internal static string ReflectionInstanceFieldDoesNotExist(string fieldName, Type type) => Get("Reflection_InstanceFieldDoesNotExistFormat", fieldName, type);

        /// <summary>Static field "{0}" not found on type "{1}".</summary>
        internal static string ReflectionStaticFieldDoesNotExist(string fieldName, Type type) => Get("Reflection_StaticFieldDoesNotExistFormat", fieldName, type);

        /// <summary>No MemberInfo can be returned from expression type "{0}".</summary>
        internal static string ReflectionNotAMember(Type type) => Get("Reflection_NotAMemberFormat", type);

        /// <summary>Failed to load assembly by name: "{0}".</summary>
        internal static string ReflectionCannotLoadAssembly(string name) => Get("Reflection_CannotLoadAssemblyFormat", name);

        /// <summary>Cannot resolve assembly: "{0}".</summary>
        internal static string ReflectionCannotResolveAssembly(string name) => Get("Reflection_CannotResolveAssemblyFormat", name);

        /// <summary>Assembly name is invalid: "{0}".</summary>
        internal static string ReflectionInvalidAssemblyName(string name) => Get("Reflection_InvalidAssemblyNameFormat", name);

        /// <summary>Pointer type '{0}' is not supported.</summary>
        internal static string ReflectionPointerTypeNotSupported(Type type) => Get("Reflection_PointerTypeNotSupportedFormat", type.GetName(TypeNameKind.LongName));

        /// <summary>Setting read-only field '{0}' of type '{1}' is not supported by FieldAccessor in the .NET Standard 2.0 version of this library. If possible, try to use the .NET Standard 2.1 version or any .NET Core/Framework versions instead.</summary>
        internal static string ReflectionSetReadOnlyFieldNetStandard20(string fieldName, Type type) => Get("Reflection_SetReadOnlyFieldNetStandard20Format", fieldName, type.GetName(TypeNameKind.LongName));

        /// <summary>Setting instance field '{0}' of value type '{1}' is not supported by FieldAccessor in the .NET Standard 2.0 version of this library. If possible, try to use the .NET Standard 2.1 version or any .NET Core/Framework versions instead.</summary>
        internal static string ReflectionSetStructFieldNetStandard20(string fieldName, Type type) => Get("Reflection_SetStructFieldNetStandard20Format", fieldName, type.GetName(TypeNameKind.LongName));

        /// <summary>Setting instance property '{0}' of value type '{1}' is not supported by PropertyAccessor in the .NET Standard 2.0 version of this library. If possible, try to use the .NET Standard 2.1 version or any .NET Core/Framework versions instead.</summary>
        internal static string ReflectionSetStructPropertyNetStandard20(string propertyName, Type type) => Get("Reflection_SetStructPropertyNetStandard20Format", propertyName, type.GetName(TypeNameKind.LongName));

        #endregion

        #region Resources

        /// <summary>Unexpected element: "{0}" at line {1}, position {2}.</summary>
        internal static string ResourcesUnexpectedElementAt(string elementName, int line, int pos) => Get("Resources_UnexpectedElementAtFormat", elementName, line, pos);

        /// <summary>Resource file not found: {0}</summary>
        internal static string ResourcesNeutralResourceFileNotFoundResX(string fileName) => Get("Resources_NeutralResourceFileNotFoundResXFormat", fileName);

        /// <summary>Could not find any resources appropriate for the specified culture or the neutral culture. Make sure "{0}" was correctly embedded or linked into assembly "{1}" at compile time, or that all the satellite assemblies required are loadable and fully signed.</summary>
        internal static string ResourcesNeutralResourceNotFoundCompiled(string baseNameField, string? fileName) => Get("Resources_NeutralResourceNotFoundCompiledFormat", baseNameField, fileName);

        /// <summary>Could not find any resources appropriate for the specified culture or the neutral culture. Make sure "{0}" was correctly embedded or linked into assembly "{1}" at compile time, or that all the satellite assemblies required are loadable and fully signed, or that XML resource file exists: {2}</summary>
        internal static string ResourcesNeutralResourceNotFoundHybrid(string baseNameField, string? assemblyFile, string resxFile) => Get("Resources_NeutralResourceNotFoundHybridFormat", baseNameField, assemblyFile, resxFile);

        /// <summary>Cannot find a name for the resource at line {0}, position {1}.</summary>
        internal static string ResourcesNoResXName(int line, int pos) => Get("Resources_NoResXNameFormat", line, pos);

        /// <summary>"{0}" attribute is missing at line {1}, position {2}.</summary>
        internal static string ResourcesMissingAttribute(string name, int line, int pos) => Get("Resources_MissingAttributeFormat", name, line, pos);

        /// <summary>Unsupported ResX header mime type "{0}" at line {1}, position {2}.</summary>
        internal static string ResourcesHeaderMimeTypeNotSupported(string mimeType, int line, int pos) => Get("Resources_HeaderMimeTypeNotSupportedFormat", mimeType, line, pos);

        /// <summary>Unsupported mime type "{0}" at line {1}, position {2}.</summary>
        internal static string ResourcesMimeTypeNotSupported(string mimeType, int line, int pos) => Get("Resources_MimeTypeNotSupportedFormat", mimeType, line, pos);

        /// <summary>Unsupported ResX reader "{0}" at line {1}, position {2}.</summary>
        internal static string ResourcesResXReaderNotSupported(string reader, int line, int pos) => Get("Resources_ResXReaderNotSupportedFormat", reader, line, pos);

        /// <summary>Unsupported ResX writer "{0}" at line {1}, position {2}.</summary>
        internal static string ResourcesResXWriterNotSupported(string writer, int line, int pos) => Get("Resources_ResXWriterNotSupportedFormat", writer, line, pos);

        /// <summary>Type "{0}" in the data at line {1}, position {2} cannot be resolved.</summary>
        internal static string ResourcesTypeLoadExceptionAt(string typeName, int line, int pos) => Get("Resources_TypeLoadExceptionAtFormat", typeName, line, pos);

        /// <summary>Type "{0}" in the data at line {1}, position {2} cannot be resolved.
        /// You may try to preload its assembly before deserialization or use the unsafe GetValue if the resource is from a trusted source.</summary>
        internal static string ResourcesTypeLoadExceptionSafeAt(string typeName, int line, int pos) => Get("Resources_TypeLoadExceptionSafeAtFormat", typeName, line, pos);

        /// <summary>Type "{0}" cannot be resolved.</summary>
        internal static string ResourcesTypeLoadException(string typeName) => Get("Resources_TypeLoadExceptionFormat", typeName);

        /// <summary>Type "{0}" cannot be resolved using safe mode.
        /// You may try to preload its assembly before deserialization or use the unsafe GetValue if the resource is from a trusted source.</summary>
        internal static string ResourcesTypeLoadExceptionSafe(string typeName) => Get("Resources_TypeLoadExceptionSafeFormat", typeName);

        /// <summary>Type of resource "{0}" is not string but "{1}" - enable SafeMode or use GetObject instead.</summary>
        internal static string ResourcesNonStringResourceWithType(string name, string typeName) => Get("Resources_NonStringResourceWithTypeFormat", name, typeName);

        /// <summary>Type of resource "{0}" is not MemoryStream but "{1}" - enable SafeMode or use GetObject instead.</summary>
        internal static string ResourcesNonStreamResourceWithType(string name, Type type) => Get("Resources_NonStreamResourceWithTypeFormat", name, type.GetName(TypeNameKind.LongName));

        /// <summary>Attempting to convert type "{0}" from string on line {1}, position {2} has failed: {3}</summary>
        internal static string ResourcesConvertFromStringNotSupportedAt(string typeName, int line, int pos, string message) => Get("Resources_ConvertFromStringNotSupportedAtFormat", typeName, line, pos, message);

        /// <summary>Converting from string is not supported by {0}.</summary>
        internal static string ResourcesConvertFromStringNotSupported(Type converterType) => Get("Resources_ConvertFromStringNotSupportedFormat", converterType);

        /// <summary>Attempting to convert type "{0}" from byte array on line {1}, position {2} has failed: {3}</summary>
        internal static string ResourcesConvertFromByteArrayNotSupportedAt(string typeName, int line, int pos, string message) => Get("Resources_ConvertFromByteArrayNotSupportedAtFormat", typeName, line, pos, message);

        /// <summary>Converting from byte array is not supported by {0}.</summary>
        internal static string ResourcesConvertFromByteArrayNotSupported(Type converterType) => Get("Resources_ConvertFromByteArrayNotSupportedFormat", converterType);

        /// <summary>File in ResX file reference cannot be found: {0}. Is base path set correctly?</summary>
        internal static string ResourcesFileRefFileNotFound(string path) => Get("Resources_FileRefFileNotFoundFormat", path);

        #endregion

        #region Serialization (any ways)

        /// <summary>Type "{0}" cannot be deserialized because it has no field "{1}". Set IgnoreNonExistingFields to true to suppress this exception.</summary>
        internal static string SerializationMissingField(Type type, string field) => Get("Serialization_MissingFieldFormat", type, field);

        /// <summary>Array of pointer type '{0}' is not supported.</summary>
        internal static string SerializationPointerArrayTypeNotSupported(Type type) => Get("Serialization_PointerArrayTypeNotSupportedFormat", type.GetName(TypeNameKind.LongName));

        #endregion

        #region SpanExtensions
#if !(NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)

        /// <summary>The specified span '{0}' cannot be parsed as type {1}.</summary>
        internal static string SpanExtensionsCannotParseAsType(ReadOnlySpan<char> s, Type type) => Get("SpanExtensions_CannotParseAsTypeFormat", s.ToString(), type);

#endif
        #endregion

        #region StringExtensions

        /// <summary>The specified string '{0}' cannot be parsed as type {1}.</summary>
        internal static string StringExtensionsCannotParseAsType(string s, Type type) => Get("StringExtensions_CannotParseAsTypeFormat", s, type);

        #endregion

        #region XmlSerialization

        /// <summary>Serializing type "{0}" is not supported with following options: {1}. You may either use fallback options or provide a type converter for the type.</summary>
        internal static string XmlSerializationSerializingTypeNotSupported(Type type, XmlSerializationOptions options) => Get("XmlSerialization_SerializingTypeNotSupportedFormat", type, options.ToString<XmlSerializationOptions>());

        /// <summary>Root named "object" expected but "{0}" found.</summary>
        internal static string XmlSerializationRootObjectExpected(string name) => Get("XmlSerialization_RootObjectExpectedFormat", name);

        /// <summary>Could not resolve type: "{0}".</summary>
        internal static string XmlSerializationCannotResolveType(string typeName) => Get("XmlSerialization_CannotResolveTypeFormat", typeName);

        /// <summary>Could not resolve type in safe mode: "{0}".
        /// In safe mode you have to preload the assembly of the type before deserialization, even if the type name is fully qualified.</summary>
        internal static string XmlSerializationCannotResolveTypeSafe(string typeName) => Get("XmlSerialization_CannotResolveTypeSafeFormat", typeName);

        /// <summary>Deserializing type "{0}" is not supported.</summary>
        internal static string XmlSerializationDeserializingTypeNotSupported(Type type) => Get("XmlSerialization_DeserializingTypeNotSupportedFormat", type);

        /// <summary>Content serialization of read-only collection type "{0}" is not supported because populating will not work at deserialization.
        /// If the collection has an initializer constructor, then using XmlSerializer.Serialize method overloads instead of SerializeContent can work.</summary>
        internal static string XmlSerializationSerializingReadOnlyCollectionNotSupported(Type type) => Get("XmlSerialization_SerializingReadOnlyCollectionNotSupportedFormat", type);

        /// <summary>Binary serialization of type "{0}" failed with options "{1}": {2}</summary>
        internal static string XmlSerializationBinarySerializationFailed(Type type, XmlSerializationOptions options, string errorMessage) => Get("XmlSerialization_BinarySerializationFailedFormat", type, options.ToString<XmlSerializationOptions>(), errorMessage);

        /// <summary>Cannot serialize collection "{0}" with following options: "{1}". You may either use fallback options or provide a type converter or apply DesignerSerializationVisibilityAttribute with value Content on the container collection property.</summary>
        internal static string XmlSerializationCannotSerializeCollection(Type type, XmlSerializationOptions options) => Get("XmlSerialization_CannotSerializeCollectionFormat", type, options.ToString<XmlSerializationOptions>());

        /// <summary>Serialization of collection "{0}" is not supported with following options: "{1}", because it does not implement IList, IDictionary or ICollection&lt;T&gt; interfaces and has no initializer constructor that can accept an array or list.
        /// To force the recursive serialization of the collection enable both RecursiveSerializationAsFallback and ForcedSerializationOfReadOnlyMembersAndCollections options; however, deserialization will likely fail in this case. Using BinarySerializationAsFallback option may also work.</summary>
        internal static string XmlSerializationCannotSerializeUnsupportedCollection(Type type, XmlSerializationOptions options) => Get("XmlSerialization_CannotSerializeUnsupportedCollectionFormat", type, options.ToString<XmlSerializationOptions>());

        /// <summary>Type "{0}" does not implement IXmlSerializable.</summary>
        internal static string XmlSerializationNotAnIXmlSerializable(Type type) => Get("XmlSerialization_NotAnIXmlSerializableFormat", type);

        /// <summary>Type "{0}" does not have a parameterless constructor so it can be (de-)serialized either as a root element by SerializeContent and DeserializeContent or as a public property/field value in a parent object if the member value is not null after creating the parent object.</summary>
        internal static string XmlSerializationNoDefaultCtor(Type type) => Get("XmlSerialization_NoDefaultCtorFormat", type);

        /// <summary>Property value of "{0}.{1}" is expected to be a type of "{2}" but was "{3}".</summary>
        internal static string XmlSerializationPropertyTypeMismatch(Type declaringType, string propertyName, Type expectedType, Type actualType) => Get("XmlSerialization_PropertyTypeMismatchFormat", declaringType, propertyName, expectedType, actualType);

        /// <summary>Collection "{0}" is read-only so its content cannot be restored.</summary>
        internal static string XmlSerializationCannotDeserializeReadOnlyCollection(Type type) => Get("XmlSerialization_CannotDeserializeReadOnlyCollectionFormat", type);

        /// <summary>Content serialization of collection type "{0}" is not supported because it cannot be populated by standard interfaces.
        /// If the collection has an initializer constructor, then using XmlSerializer.Serialize method overloads instead of SerializeContent can work.</summary>
        internal static string XmlSerializationSerializingNonPopulatableCollectionNotSupported(Type type) => Get("XmlSerialization_SerializingNonPopulatableCollectionNotSupportedFormat", type);

        /// <summary>Cannot restore property "{0}" in type "{1}" because it has no setter.</summary>
        internal static string XmlSerializationPropertyHasNoSetter(string propertyName, Type type) => Get("XmlSerialization_PropertyHasNoSetterFormat", propertyName, type);

        /// <summary>Cannot set null to non-null property "{0}" in type "{1}" because it has no setter.</summary>
        internal static string XmlSerializationPropertyHasNoSetterCantSetNull(string propertyName, Type type) => Get("XmlSerialization_PropertyHasNoSetterCantSetNullFormat", propertyName, type);

        /// <summary>Cannot restore property "{0}" in type "{1}" because it has no setter and it returned null.</summary>
        internal static string XmlSerializationPropertyHasNoSetterGetsNull(string propertyName, Type type) => Get("XmlSerialization_PropertyHasNoSetterGetsNullFormat", propertyName, type);

        /// <summary>Collection item expected but "{0}" found.</summary>
        internal static string XmlSerializationItemExpected(string name) => Get("XmlSerialization_ItemExpectedFormat", name);

        /// <summary>Could not determine type of element in collection "{0}".</summary>
        internal static string XmlSerializationCannotDetermineElementType(Type type) => Get("XmlSerialization_CannotDetermineElementTypeFormat", type);

        /// <summary>Type "{0}" is not a regular collection so items cannot be added to it.</summary>
        internal static string XmlSerializationNotACollection(Type type) => Get("XmlSerialization_NotACollectionFormat", type);

        /// <summary>Type "{0}" has no public property or field "{1}".</summary>
        internal static string XmlSerializationHasNoMember(Type type, string name) => Get("XmlSerialization_HasNoMemberFormat", type, name);

        /// <summary>Serialized content of type "{0}" not found.</summary>
        internal static string XmlSerializationNoContent(Type type) => Get("XmlSerialization_NoContentFormat", type);

        /// <summary>Invalid array length: {0}</summary>
        internal static string XmlSerializationInvalidArrayLength(string value) => Get("XmlSerialization_InvalidArrayLengthFormat", value);

        /// <summary>Invalid array bounds: {0}</summary>
        internal static string XmlSerializationInvalidArrayBounds(string value) => Get("XmlSerialization_InvalidArrayBoundsFormat", value);

        /// <summary>Cannot restore array "{0}" because size does not match. Expected length: "{1}".</summary>
        internal static string XmlSerializationArraySizeMismatch(Type type, int length) => Get("XmlSerialization_ArraySizeMismatchFormat", type, length);

        /// <summary>Cannot restore array "{0}" because rank does not match. Expected rank: "{1}".</summary>
        internal static string XmlSerializationArrayRankMismatch(Type type, int rank) => Get("XmlSerialization_ArrayRankMismatchFormat", type, rank);

        /// <summary>Cannot restore array "{0}" because length of the {1}. dimension does not match.</summary>
        internal static string XmlSerializationArrayDimensionSizeMismatch(Type type, int length) => Get("XmlSerialization_ArrayDimensionSizeMismatchFormat", type, length);

        /// <summary>Cannot restore array "{0}" because lower bound of the {1}. dimension does not match.</summary>
        internal static string XmlSerializationArrayLowerBoundMismatch(Type type, int dimension) => Get("XmlSerialization_ArrayLowerBoundMismatchFormat", type, dimension);

        /// <summary>Array items length mismatch. Expected items: {0}, found items: {1}.</summary>
        internal static string XmlSerializationInconsistentArrayLength(int expected, int actual) => Get("XmlSerialization_InconsistentArrayLengthFormat", expected, actual);

        /// <summary>The crc attribute should be a hex value but "{0}" found.</summary>
        internal static string XmlSerializationCrcHexExpected(string content) => Get("XmlSerialization_CrcHexExpectedFormat", content);

        /// <summary>Unexpected element: "{0}".</summary>
        internal static string XmlSerializationUnexpectedElement(string elementName) => Get("XmlSerialization_UnexpectedElementFormat", elementName);

        /// <summary>Invalid escaped string content: "{0}".</summary>
        internal static string XmlSerializationInvalidEscapedContent(string content) => Get("XmlSerialization_InvalidEscapedContentFormat", content);

        /// <summary>Circular reference found during serialization. Object is already serialized: "{0}". To avoid circular references use DesignerSerializationVisibilityAttribute with Hidden value on members directly or indirectly reference themselves.</summary>
        internal static string XmlSerializationCircularReference(object obj) => Get("XmlSerialization_CircularReferenceFormat", obj);

        /// <summary>Value type '{0}' cannot be deserialized from raw data in safe mode because it contains references. If the XML data is from a trusted source you may try to use unsafe mode to attempt the deserialization with marshaling.</summary>
        internal static string XmlSerializationValueTypeContainsReferenceSafe(Type type) => Get("XmlSerialization_ValueTypeContainsReferenceSafeFormat", type.GetName(TypeNameKind.LongName));

        #endregion

        #endregion

        #region Private Methods

        private static string Get([NotNull]string id) => resourceManager.GetString(id, LanguageSettings.DisplayLanguage) ?? String.Format(CultureInfo.InvariantCulture, unavailableResource, id);

        private static string Get([NotNull]string id, params object?[]? args)
        {
            string format = Get(id);
            return args == null ? format : SafeFormat(format, args);
        }

        private static string FormatEnumValues<TEnum>() where TEnum : struct, Enum
            => Enum<TEnum>.GetNames().Select(v => QuoteStart + v + QuoteEnd).Join(", ");

        private static string FormatEnumFlags<TEnum>() where TEnum : struct, Enum
            => Enum<TEnum>.GetFlags().Select(f => QuoteStart + f + QuoteEnd).Join(", ");

        private static string FormatBool(bool value) => value ? Yes : No;

        private static string SafeFormat(string format, object?[] args)
        {
            try
            {
                int i = Array.IndexOf(args, null);
                if (i >= 0)
                {
                    string nullRef = Null;
                    for (; i < args.Length; i++)
                    {
                        if (args[i] == null)
                            args[i] = nullRef;
                    }
                }

                return String.Format(LanguageSettings.FormattingLanguage, format, args);
            }
            catch (FormatException)
            {
                return String.Format(CultureInfo.InvariantCulture, invalidResource, args.Length, format);
            }
        }

        #endregion

        #endregion
    }
}
