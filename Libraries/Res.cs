#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Res.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2017 - All Rights Reserved
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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using KGySoft.Annotations;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Resources;

#endregion

namespace KGySoft
{
    /// <summary>
    /// Contains the IDs of string resources.
    /// </summary>
    internal static class Res
    {
        #region Constants

        #region Internal Constants

#error TODO: all to methods
        internal const string SerializationNotSupported = nameof(SerializationNotSupported);
        internal const string IEnumerableExpected = nameof(IEnumerableExpected);
        internal const string InvalidStreamData = nameof(InvalidStreamData);
        internal const string InvalidEnumBase = nameof(InvalidEnumBase);
        internal const string CannotDeserializeObject = nameof(CannotDeserializeObject);
        internal const string ObjectHierarchyChanged = nameof(ObjectHierarchyChanged);
        internal const string MissingField = nameof(MissingField);
        internal const string MissingFieldBase = nameof(MissingFieldBase);
        internal const string MissingISerializableCtor = nameof(MissingISerializableCtor);
        internal const string SurrogateChangedObject = nameof(SurrogateChangedObject);
        internal const string CannotDecodeDataType = nameof(CannotDecodeDataType);
        internal const string CannotDecodeCollectionType = nameof(CannotDecodeCollectionType);
        internal const string ReadOnlyCollectionNotSupported = nameof(ReadOnlyCollectionNotSupported);
        internal const string CannotResolveType = nameof(CannotResolveType);
        internal const string CircularIObjectReference = nameof(CircularIObjectReference);
        internal const string DeserializeUnexpectedId = nameof(DeserializeUnexpectedId);
        internal const string CannotResolveTypeInAssembly = nameof(CannotResolveTypeInAssembly);
        internal const string ValueTypeExpected = nameof(ValueTypeExpected);
        internal const string DataLenghtTooSmall = nameof(DataLenghtTooSmall);
        internal const string UnexpectedSerializationInfoElement = nameof(UnexpectedSerializationInfoElement);
        internal const string ObjectHierarchyChangedSurrogate = nameof(ObjectHierarchyChangedSurrogate);
        internal const string MissingFieldSurrogate = nameof(MissingFieldSurrogate);
        internal const string UnexpectedFieldType = nameof(UnexpectedFieldType);
        internal const string Undefined = nameof(Undefined);
        internal const string XmlCannotSerialize = nameof(XmlCannotSerialize);
        internal const string XmlRootExpected = nameof(XmlRootExpected);
        internal const string XmlCannotResolveType = nameof(XmlCannotResolveType);
        internal const string XmlRootTypeMissing = nameof(XmlRootTypeMissing);
        internal const string XmlDeserializeNotSupported = nameof(XmlDeserializeNotSupported);
        internal const string XmlSerializeReadOnlyCollection = nameof(XmlSerializeReadOnlyCollection);
        internal const string XmlBinarySerializationFailed = nameof(XmlBinarySerializationFailed);
        internal const string XmlCannotSerializeCollection = nameof(XmlCannotSerializeCollection);
        internal const string XmlCannotSerializeUnsupportedCollection = nameof(XmlCannotSerializeUnsupportedCollection);
        internal const string NotAnIXmlSerializable = nameof(NotAnIXmlSerializable);
        internal const string XmlNoDefaultCtor = nameof(XmlNoDefaultCtor);
        internal const string XmlPropertyTypeMismatch = nameof(XmlPropertyTypeMismatch);
        internal const string XmlDeserializeReadOnlyCollection = nameof(XmlDeserializeReadOnlyCollection);
        internal const string XmlSerializeNonPopulatableCollection = nameof(XmlSerializeNonPopulatableCollection);
        internal const string XmlPropertyHasNoSetter = nameof(XmlPropertyHasNoSetter);
        internal const string XmlPropertyHasNoSetterCantSetNull = nameof(XmlPropertyHasNoSetterCantSetNull);
        internal const string XmlPropertyHasNoSetterGetsNull = nameof(XmlPropertyHasNoSetterGetsNull);
        internal const string XmlItemExpected = nameof(XmlItemExpected);
        internal const string XmlCannotDetermineElementType = nameof(XmlCannotDetermineElementType);
        internal const string XmlNotACollection = nameof(XmlNotACollection);
        internal const string XmlHasNoProperty = nameof(XmlHasNoProperty);
        internal const string XmlNoContent = nameof(XmlNoContent);
        internal const string XmlLengthInvalidType = nameof(XmlLengthInvalidType);
        internal const string XmlArrayNoLength = nameof(XmlArrayNoLength);
        internal const string XmlArraySizeMismatch = nameof(XmlArraySizeMismatch);
        internal const string XmlArrayRankMismatch = nameof(XmlArrayRankMismatch);
        internal const string XmlArrayDimensionSizeMismatch = nameof(XmlArrayDimensionSizeMismatch);
        internal const string XmlArrayLowerBoundMismatch = nameof(XmlArrayLowerBoundMismatch);
        internal const string XmlCrcError = nameof(XmlCrcError);
        internal const string XmlInconsistentArrayLength = nameof(XmlInconsistentArrayLength);
        internal const string XmlCrcFormat = nameof(XmlCrcFormat);
        internal const string XmlMixedArrayFormats = nameof(XmlMixedArrayFormats);
        internal const string XmlUnexpectedElement = nameof(XmlUnexpectedElement);
        internal const string XmlKeyValueMissingKey = nameof(XmlKeyValueMissingKey);
        internal const string XmlKeyValueMissingValue = nameof(XmlKeyValueMissingValue);
        internal const string XmlMultipleKeys = nameof(XmlMultipleKeys);
        internal const string XmlMultipleValues = nameof(XmlMultipleValues);
        internal const string XmlInvalidEscapedContent = nameof(XmlInvalidEscapedContent);
        internal const string XmlUnexpectedEnd = nameof(XmlUnexpectedEnd);
        internal const string XmlCircularReference = nameof(XmlCircularReference);
        internal const string ExceptionMessage = nameof(ExceptionMessage);
        internal const string ExceptionMessageNotAvailable = nameof(ExceptionMessageNotAvailable);
        internal const string InnerException = nameof(InnerException);
        internal const string InnerExceptionEnd = nameof(InnerExceptionEnd);
        internal const string Win32ErrorCode = nameof(Win32ErrorCode);
        internal const string SystemInformation = nameof(SystemInformation);
        internal const string UserInformation = nameof(UserInformation);
        internal const string ExceptionSource = nameof(ExceptionSource);
        internal const string ExceptionSourceNotAvailable = nameof(ExceptionSourceNotAvailable);
        internal const string ExceptionType = nameof(ExceptionType);
        internal const string ExceptionTypeNotAvailable = nameof(ExceptionTypeNotAvailable);
        internal const string ExceptionTargetSite = nameof(ExceptionTargetSite);
        internal const string ExceptionTargetSiteNotAvailable = nameof(ExceptionTargetSiteNotAvailable);
        internal const string ExceptionTargetSiteNotAccessible = nameof(ExceptionTargetSiteNotAccessible);
        internal const string RemoteStackTrace = nameof(RemoteStackTrace);
        internal const string StackTrace = nameof(StackTrace);
        internal const string LocalStackTrace = nameof(LocalStackTrace);
        internal const string NativeOffset = nameof(NativeOffset);
        internal const string SourceOffset = nameof(SourceOffset);
        internal const string ILOffset = nameof(ILOffset);
        internal const string DateAndTime = nameof(DateAndTime);
        internal const string OperatingSystem = nameof(OperatingSystem);
        internal const string Environment = nameof(Environment);
        internal const string ProcessorCount = nameof(ProcessorCount);
        internal const string ClrVersion = nameof(ClrVersion);
        internal const string WorkingSet = nameof(WorkingSet);
        internal const string CommandLine = nameof(CommandLine);
        internal const string ApplicationDomain = nameof(ApplicationDomain);
        internal const string MachineName = nameof(MachineName);
        internal const string UserName = nameof(UserName);
        internal const string CurrentUser = nameof(CurrentUser);
        internal const string CannotGetDomain = nameof(CannotGetDomain);
        internal const string AssemblyCodebase = nameof(AssemblyCodebase);
        internal const string AssemblyFullName = nameof(AssemblyFullName);
        internal const string AssemblyVersion = nameof(AssemblyVersion);
        internal const string AssemblyBuildDate = nameof(AssemblyBuildDate);
        internal const string Uncategorized = nameof(Uncategorized);
        internal const string InvalidResXReaderPropertyChange = nameof(InvalidResXReaderPropertyChange);
        internal const string InvalidResXResourceNoName = nameof(InvalidResXResourceNoName);
        internal const string XmlMissingAttribute = nameof(XmlMissingAttribute);
        internal const string ResXFileMimeTypeNotSupported = nameof(ResXFileMimeTypeNotSupported);
        internal const string ResXMimeTypeNotSupported = nameof(ResXMimeTypeNotSupported);
        internal const string ResXReaderNotSupported = nameof(ResXReaderNotSupported);
        internal const string ResXWriterNotSupported = nameof(ResXWriterNotSupported);
        internal const string TypeLoadException = nameof(TypeLoadException);
        internal const string TypeLoadExceptionShort = nameof(TypeLoadExceptionShort);
        internal const string NonStringResourceWithType = nameof(NonStringResourceWithType);
        internal const string ConvertFromStringNotSupportedAt = nameof(ConvertFromStringNotSupportedAt);
        internal const string ConvertFromStringNotSupported = nameof(ConvertFromStringNotSupported);
        internal const string ConvertFromByteArrayNotSupportedAt = nameof(ConvertFromByteArrayNotSupportedAt);
        internal const string ConvertFromByteArrayNotSupported = nameof(ConvertFromByteArrayNotSupported);
        internal const string InvalidResXWriterPropertyChange = nameof(InvalidResXWriterPropertyChange);
        internal const string ResXResourceWriterSaved = nameof(ResXResourceWriterSaved);
        internal const string ResXFileRefFileNotFound = nameof(ResXFileRefFileNotFound);
        internal const string SeparatorInvalidHex = nameof(SeparatorInvalidHex);
        internal const string SeparatorInvalidDec = nameof(SeparatorInvalidDec);
        internal const string HybridResSourceBinary = nameof(HybridResSourceBinary);
        internal const string InvalidDrmPropertyChange = nameof(InvalidDrmPropertyChange);
        internal const string SourceLengthNotEven = nameof(SourceLengthNotEven);
        internal const string CannotGetProperty = nameof(CannotGetProperty);
        internal const string CannotSetProperty = nameof(CannotSetProperty);
        internal const string ReturnedTypeInvalid = nameof(ReturnedTypeInvalid);
        internal const string PropertyValueNotExist = nameof(PropertyValueNotExist);
        internal const string DeclaringTypeExpected = nameof(DeclaringTypeExpected);
        internal const string NotEditing = nameof(NotEditing);
        internal const string MissingPropertyReference = nameof(MissingPropertyReference);
        internal const string DoValidationNull = nameof(DoValidationNull);
        internal const string ObservableObjectHasNoDefaultCtor = nameof(ObservableObjectHasNoDefaultCtor);
        internal const string EnabledMustBeBool = nameof(EnabledMustBeBool);

        #endregion

        #region Private Constants

        private const string nullReference = "NullReference";
        private const string unavailableResource = "Resource ID not found: {0}";
        private const string invalidResource = "Resource text is not valid for {0} arguments: {1}";

        #endregion

        #endregion

        #region Fields

        private static readonly DynamicResourceManager resourceManager = new DynamicResourceManager("KGySoft.CoreLibraries.Messages", Reflector.KGySoftLibrariesAssembly);

        #endregion

        #region Properties

        #region Private Properties

        private static string QuoteStart => Get("General_QuoteStart");
        private static string QuoteEnd => Get("General_QuoteEnd");

        #endregion

        #region Internal Properties

        #region General

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

        /// <summary>Cannot add new item to the binding list because AllowNew is false.</summary>
        internal static string IBindingListAddNewDisabled => Get("IBindingList_AddNewDisabled");

        /// <summary>Cannot remove item from the binding list because AllowRemove is false.</summary>
        internal static string IBindingListRemoveDisabled => Get("IBindingList_RemoveDisabled");

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

        /// <summary>Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.</summary>
        internal static string CircularListInvalidOffsLen => Get("CircularList_InvalidOffsLen");

        /// <summary>Failed to compare two elements in the collection.</summary>
        internal static string CircularListComparerFail => Get("CircularList_ComparerFail");

        /// <summary>Capacity cannot be less than number of stored elements.</summary>
        internal static string CircularListCapacityTooSmall => Get("CircularList_CapacityTooSmall");

        #endregion

        #region CircularSortedList<T>

        /// <summary>Adding an element by index is not supported.</summary>
        internal static string CircularSortedListInsertByIndexNotSupported => Get("CircularSortedList_InsertByIndexNotSupported");

        #endregion

        #region Enum/EnumComparer

        /// <summary>Type parameter is expected to be a System.Enum type.</summary>
        internal static string EnumTypeParameterInvalid => Get("Enum_TypeParameterInvalid");

        #endregion

        #region ObservableBindingList

        /// <summary>Cannot change ObservableBindingList during a CollectionChanged or ListChanged event.</summary>
        internal static string ObservableBindingListReentrancyNotAllowed => Get("ObservableBindingList_ReentrancyNotAllowed");

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

        /// <summary>Index parameters cannot be converted to integer values.</summary>
        internal static string ReflectionIndexParamsTypeMismatch => Get("Reflection_IndexParamsTypeMismatch");

        /// <summary>Instance is null for a non-static member.</summary>
        internal static string ReflectionInstanceIsNull => Get("Reflection_InstanceIsNull");

        /// <summary>Method to invoke is generic but no type parameters are passed.</summary>
        internal static string ReflectionTypeParamsAreNull => Get("Reflection_TypeParamsAreNull");

        /// <summary>Could not create generic method. For details see inner exception.</summary>
        internal static string ReflectionCannotCreateGenericMethod => Get("Reflection_CannotCreateGenericMethod");

        /// <summary>Invoking a method via TypeDescriptor is not supported.</summary>
        internal static string ReflectionInvokeMethodTypeDescriptorNotSupported => Get("Reflection_InvokeMethodTypeDescriptorNotSupported");

        /// <summary>A field cannot be set via TypeDescriptor.</summary>
        internal static string ReflectionSetFieldTypeDescriptorNotSupported => Get("Reflection_SetFieldTypeDescriptorNotSupported");

        /// <summary>A field cannot be retrieved via TypeDescriptor.</summary>
        internal static string ReflectionGetFieldTypeDescriptorNotSupported => Get("Reflection_GetFieldTypeDescriptorNotSupported");

        /// <summary>Expression is not a method call.</summary>
        internal static string ReflectionNotAMethod => Get("Reflection_NotAMethod");

        /// <summary>In this ResolveType overload the type name should not contain the assembly name.</summary>
        internal static string ReflectionTypeWithAssemblyName => Get("Reflection_TypeWithAssemblyName");

        #endregion

        #region StreamExtensions

        /// <summary>Source stream cannot be read.</summary>
        internal static string StreamExtensionsStreamCannotRead => Get("StreamExtensions_StreamCannotRead");

        /// <summary>Destination stream cannot be written.</summary>
        internal static string StreamExtensionsStreamCannotWrite => Get("StreamExtensions_StreamCannotWrite");

        /// <summary>Cannot seek to the beginning of the stream.</summary>
        internal static string StreamExtensionsStreamCannotSeek => Get("StreamExtensions_StreamCannotSeek");

        #endregion

        #region StringExtensions

        /// <summary>Separator is null or empty.</summary>
        internal static string StringExtensionsSeparatorNullOrEmpty => Get("StringExtensions_SeparatorNullOrEmpty");

        #endregion

        #endregion

        #endregion

        #region Methods

        #region Internal Methods

        #region General

        /// <summary>Specified argument must be greater or equal than {0}.</summary>
        internal static string ArgumentMustBeGreaterOrEqualThan(object limit) => Get("General_ArgumentMustBeGreaterOrEqualThanFormat", limit);

        /// <summary>Specified argument must be between {0} and {1}.</summary>
        internal static string ArgumentMustBeBetween(object low, object high) => Get("General_ArgumentMustBeBetweenFormat", low, high);

        /// <summary>Enum instance of '{0}' type must be one of the following values: {1}.</summary>
        internal static string EnumOutOfRange<TEnum>(TEnum value) where TEnum : struct, IConvertible => Get("General_EnumOutOfRangeFormat", value.GetType().Name, FormatValues<TEnum>());

        /// <summary>Enum instance of '{0}' type must consist of the following flags: {1}.</summary>
        internal static string FlagsEnumOutOfRange<TEnum>(TEnum value) where TEnum : struct, IConvertible => Get("General_EnumFlagsOutOfRangeFormat", value.GetType().Name, FormatFlags<TEnum>());

        /// <summary>Specified argument is expected to be an instance of type {0}.</summary>
        internal static string NotAnInstanceOfType(Type type) => Get("General_NotAnInstanceOfTypeFormat", type);

        /// <summary>Value "{0}" contains illegal path characters.</summary>
        internal static string ValueContainsIllegalPathCharacters(string path) => Get("General_ValueContainsIllegalPathCharactersFormat", path);

        /// <summary>The value "{0}" is not of type "{1}" and cannot be used in this generic collection.</summary>
        internal static string ICollectionNongenericValueTypeInvalid(object value, Type type) => Get("ICollection_NongenericValueTypeInvalidFormat", value, type);

        /// <summary>The key "{0}" is not of type "{1}" and cannot be used in this generic collection.</summary>
        internal static string IDictionaryNongenericKeyTypeInvalid(object key, Type type) => Get("Collection_NongenericKeyTypeInvalidFormat", key, type);

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

        #region Command

        /// <summary>The property binding command state does not contain the expected entry '{0}'.</summary>
        internal static string CommandBindingMissingEvent(string stateName) => Get("Command_PropertyBindingMissingStateFormat", stateName);

        #endregion

        #region CommandBinding

        /// <summary>There is no event '{0}' in type '{1}'.</summary>
        internal static string CommandBindingMissingEvent(string eventName, Type type) => Get("CommandBinding_MissingEventFormat", eventName, type);

        /// <summary>Event '{0}' does not have regular event handler delegate type.</summary>
        internal static string CommandBindingInvalidEvent(string eventName) => Get("CommandBinding_InvalidEventFormat", eventName);

        #endregion

        #region Enum

        /// <summary>Value '{0}' cannot be parsed as enumeration type {1}</summary>
        internal static string EnumValueCannotBeParsedAsEnum(string value, Type enumType) => Get("Enum_ValueCannotBeParsedAsEnumFormat", value, enumType);

        #endregion

        #region EnumerableExtensions

        /// <summary>Cannot add element to type {0} because it implements neither IList nor ICollection&lt;T&gt; interfaces.</summary>
        internal static string EnumerableExtensionsCannotAdd(Type type) => Get("EnumerableExtensions_CannotAddFormat", type);

        /// <summary>Cannot clear items of type {0} because it implements neither IList nor ICollection&lt;T&gt; interfaces.</summary>
        internal static string EnumerableExtensionsCannotClear(Type type) => Get("EnumerableExtensions_CannotClearFormat", type);

        #endregion

        #region FastBindingList

        /// <summary>Property '{0}' of descriptor type '{1}' does not belong to type '{2}'.</summary>
        internal static string FastBindingListInvalidProperty(PropertyDescriptor property, Type t) => Get("FastBindingList_InvalidPropertyFormat", property.Name, property.GetType(), t);

        /// <summary>Cannot add new item to the binding list because type '{0}' cannot be constructed without parameters. Subscribe the AddingNew event or override the AddNewCore or OnAddingNew methods to create a new item to add.</summary>
        internal static string FastBindingListCannotAddNew(Type t) => Get("FastBindingList_CannotAddNewFormat", t);

        /// <summary>No property descriptor found for property name '{0}' in type '{1}'.</summary>
        internal static string FastBindingListPropertyNotExists(string propertyName, Type type) => Get("FastBindingList_PropertyNotExistsFormat", propertyName, type);

        #endregion

        #region ObjectExtensions

        /// <summary>The specified argument cannot be converted to type {0}.</summary>
        internal static string ObjectExtensionsCannotConvertToType(Type type) => Get("ObjectExtensions_CannotConvertToTypeFormat", type);

        #endregion

        #region ObservableBindingList

        /// <summary>Cannot add new item to the binding list because type '{0}' cannot be constructed without parameters.</summary>
        internal static string ObservableBindingListCannotAddNew(Type t) => Get("ObservableBindingList_CannotAddNewFormat", t);

        #endregion

        #region Reflection

        /// <summary>The constant field cannot be set: {0}.{1}</summary>
        internal static string ReflectionCannotSetConstantField(Type type, string memberName) => Get("Reflection_CannotSetConstantFieldFormat", type, memberName);

        /// <summary>Member type {0} is not supported.</summary>
        internal static string ReflectionNotSupportedMemberType(MemberTypes memberType) => Get("Reflection_NotSupportedMemberTypeFormat", memberType);

        /// <summary>Property has no getter accessor: {0}.{1}</summary>
        internal static string ReflectionPropertyHasNoGetter(Type type, string memberName) => Get("Reflection_PropertyHasNoGetterFormat", type, memberName);

        /// <summary>Property has no setter accessor: {0}.{1}</summary>
        internal static string ReflectionPropertyHasNoSetter(Type type, string memberName) => Get("Reflection_PropertyHasNoSetterFormat", type, memberName);

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

        /// <summary>Expected number of type arguments: {0}.</summary>
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

        /// <summary>"{0}" is not a generic type, however, it is used so in the definition "{1}".</summary>
        internal static string ReflectionResolveNotAGenericType(string elementTypeName, string typeName) => Get("Reflection_ResolveNotAGenericTypeFormat", elementTypeName, typeName);

        /// <summary>Number of awaited and actual type parameters mismatch in type definition "{0}". Expected number of type arguments: {1}.</summary>
        internal static string ReflectionResolveTypeArgsLengthMismatch(string typeName, int length) => Get("Reflection_ResolveTypeArgsLengthMismatchFormat", typeName, length);

        /// <summary>Cannot resolve type parameter "{0}" in generic type "{1}".</summary>
        internal static string ReflectionCannotResolveTypeArg(string elementTypeName, string typeName) => Get("Reflection_CannotResolveTypeArgFormat", elementTypeName, typeName);

        /// <summary>Syntax error in generic/array type: "{0}".</summary>
        internal static string ReflectionTypeSyntaxError(string typeName) => Get("Reflection_TypeSyntaxErrorFormat", typeName);

        /// <summary>No MemberInfo can be returned from expression type "{0}".</summary>
        internal static string ReflectionNotAMember(Type type) => Get("Reflection_NotAMemberFormat", type);

        /// <summary>Failed to load assembly by name: "{0}".</summary>
        internal static string ReflectionCannotLoadAssembly(string name) => Get("Reflection_CannotLoadAssemblyFormat", name);

        #endregion

        #region ResourceManagers

        /// <summary>Resource file not found: {0}</summary>
        internal static string NeutralResourceFileNotFoundResX(string fileName) => Get("ResXResourceManager_NeutralResourceFileNotFoundResXFormat", fileName);

        /// <summary>Could not find any resources appropriate for the specified culture or the neutral culture. Make sure "{0}" was correctly embedded or linked into assembly "{1}" at compile time, or that all the satellite assemblies required are loadable and fully signed.</summary>
        internal static string NeutralResourceNotFoundCompiled(string baseNameField, string fileName) => Get("HybridResourceManager_NeutralResourceNotFoundCompiledFormat", baseNameField, fileName);

        /// <summary>Could not find any resources appropriate for the specified culture or the neutral culture. Make sure "{0}" was correctly embedded or linked into assembly "{1}" at compile time, or that all the satellite assemblies required are loadable and fully signed, or that XML resource file exists: {2}</summary>
        internal static string NeutralResourceNotFoundHybrid(string baseNameField, string assemblyFile, string resxFile) => Get("HybridResourceManager_NeutralResourceNotFoundHybridFormat", baseNameField, assemblyFile, resxFile);

        #endregion

        #region StringExtensions

        /// <summary>The specified string '{0}' cannot be parsed as type {1}.</summary>
        internal static string StringExtensionsCannotParseAsType(string s, Type type) => Get("StringExtensions_CannotParseAsTypeFormat", s, type);

        #endregion

        // TODO: private
        internal static string Get([NotNull]string id)
        {
            return resourceManager.GetString(id, LanguageSettings.DisplayLanguage) ?? String.Format(unavailableResource, id);
        }

        // TODO: private
        internal static string Get([NotNull]string id, params object[] args)
        {
            string format = Get(id);
            return args == null || args.Length == 0 ? format : SafeFormat(format, args);
        }

        #endregion

        #region Private Methods

        private static string FormatValues<TEnum>() where TEnum : struct, IConvertible
            => String.Join(", ", Enum<TEnum>.GetNames().Select(v => QuoteStart + v + QuoteEnd));

        private static string FormatFlags<TEnum>() where TEnum : struct, IConvertible
            => String.Join(", ", Enum<TEnum>.GetFlags().Select(f => QuoteStart + f + QuoteEnd));

        private static string SafeFormat(string format, object[] args)
        {
            try
            {
                int i = Array.IndexOf(args, null);
                if (i >= 0)
                {
                    string nullRef = Get(nullReference);
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
                return String.Format(invalidResource, args.Length, format);
            }
        }

        #endregion

        #endregion
    }
}
