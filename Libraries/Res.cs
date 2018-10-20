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
using KGySoft.Annotations;
using KGySoft.Libraries;
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

        internal const string ArgumentNull = nameof(ArgumentNull);
        internal const string ArgumentOutOfRange = nameof(ArgumentOutOfRange);
        internal const string ArgumentInvalidString = nameof(ArgumentInvalidString);
        internal const string KeyNotFound = nameof(KeyNotFound);
        internal const string ObjectDisposed = nameof(ObjectDisposed);
        internal const string ArgumentEmpty = nameof(ArgumentEmpty);
        internal const string DuplicateKey = nameof(DuplicateKey);
        internal const string ArgumentContainsNull = nameof(ArgumentContainsNull);
        internal const string ArgumentMustBeGreaterOrEqualThan = nameof(ArgumentMustBeGreaterOrEqualThan);
        internal const string ArgumentMustBeBetween = nameof(ArgumentMustBeBetween);
        internal const string MaxValueLessThanMinValue = nameof(MaxValueLessThanMinValue);
        internal const string MaxLengthLessThanMinLength = nameof(MaxLengthLessThanMinLength);
        internal const string CollectionEmpty = nameof(CollectionEmpty);

        internal const string NeutralResourceFileNotFoundResX = nameof(NeutralResourceFileNotFoundResX);
        internal const string NeutralResourceNotFoundCompiled = nameof(NeutralResourceNotFoundCompiled);
        internal const string NeutralResourceNotFoundHybrid = nameof(NeutralResourceNotFoundHybrid);
        internal const string ValueContainsIllegalPathCharacters = nameof(ValueContainsIllegalPathCharacters);
        internal const string TypeParameterIsNotEnum = nameof(TypeParameterIsNotEnum);
        internal const string ValueCannotBeParsedAsEnum = nameof(ValueCannotBeParsedAsEnum);
        internal const string InvalidKeyType = nameof(InvalidKeyType);
        internal const string InvalidValueType = nameof(InvalidValueType);
        internal const string ModifyNotSupported = nameof(ModifyNotSupported);
        internal const string DestArrayShort = nameof(DestArrayShort);
        internal const string EnumerationNotStartedOrFinished = nameof(EnumerationNotStartedOrFinished);
        internal const string EnumerationCollectionModified = nameof(EnumerationCollectionModified);
        internal const string ArrayDimension = nameof(ArrayDimension);
        internal const string ArrayTypeInvalid = nameof(ArrayTypeInvalid);
        internal const string CacheNullLoaderInvoke = nameof(CacheNullLoaderInvoke);
        internal const string CacheKeyNotFound = nameof(CacheKeyNotFound);
        internal const string CacheMinSize = nameof(CacheMinSize);
        internal const string CacheStatistics = nameof(CacheStatistics);
        internal const string InvalidOffsLen = nameof(InvalidOffsLen);
        internal const string ComparerFail = nameof(ComparerFail);
        internal const string CapacityTooSmall = nameof(CapacityTooSmall);
        internal const string InsertByIndexNotSupported = nameof(InsertByIndexNotSupported);
        internal const string InvalidKeyValueType = nameof(InvalidKeyValueType);
        internal const string EnumerableCannotAdd = nameof(EnumerableCannotAdd);
        internal const string EnumerableCannotClear = nameof(EnumerableCannotClear);
        internal const string StreamCannotRead = nameof(StreamCannotRead);
        internal const string StreamCannotWrite = nameof(StreamCannotWrite);
        internal const string StreamCannotSeek = nameof(StreamCannotSeek);
        internal const string SeparatorNullOrEmpty = nameof(SeparatorNullOrEmpty);
        internal const string NotAnInstanceOfType = nameof(NotAnInstanceOfType);
        internal const string SetConstantField = nameof(SetConstantField);
        internal const string NotSupportedMemberType = nameof(NotSupportedMemberType);
        internal const string InvalidMethodBase = nameof(InvalidMethodBase);
        internal const string CannotTreatPropertySetter = nameof(CannotTreatPropertySetter);
        internal const string TypeOrCtorInfoExpected = nameof(TypeOrCtorInfoExpected);
        internal const string PropertyHasNoGetter = nameof(PropertyHasNoGetter);
        internal const string PropertyHasNoSetter = nameof(PropertyHasNoSetter);
        internal const string ParsedValueNull = nameof(ParsedValueNull);
        internal const string NotABool = nameof(NotABool);
        internal const string NotAType = nameof(NotAType);
        internal const string TypeCannotBeParsed = nameof(TypeCannotBeParsed);
        internal const string ParseError = nameof(ParseError);
        internal const string SetPropertyTypeDescriptorNotSupported = nameof(SetPropertyTypeDescriptorNotSupported);
        internal const string CannotSetStaticPropertyTypeDescriptor = nameof(CannotSetStaticPropertyTypeDescriptor);
        internal const string CannotSetPropertyTypeDescriptor = nameof(CannotSetPropertyTypeDescriptor);
        internal const string InstancePropertyDoesNotExist = nameof(InstancePropertyDoesNotExist);
        internal const string StaticPropertyDoesNotExist = nameof(StaticPropertyDoesNotExist);
        internal const string EmptyIndices = nameof(EmptyIndices);
        internal const string SetIndexerTypeDescriptorNotSupported = nameof(SetIndexerTypeDescriptorNotSupported);
        internal const string IndexParamsLengthMismatch = nameof(IndexParamsLengthMismatch);
        internal const string IndexParamsTypeMismatch = nameof(IndexParamsTypeMismatch);
        internal const string IndexerDoesNotExist = nameof(IndexerDoesNotExist);
        internal const string InstanceIsNull = nameof(InstanceIsNull);
        internal const string CannotGetPropertyTypeDescriptor = nameof(CannotGetPropertyTypeDescriptor);
        internal const string CannotGetStaticPropertyTypeDescriptor = nameof(CannotGetStaticPropertyTypeDescriptor);
        internal const string GetIndexerTypeDescriptorNotSupported = nameof(GetIndexerTypeDescriptorNotSupported);
        internal const string TypeParamsAreNull = nameof(TypeParamsAreNull);
        internal const string TypeArgsLengthMismatch = nameof(TypeArgsLengthMismatch);
        internal const string CannotCreateGenericMethod = nameof(CannotCreateGenericMethod);
        internal const string InvokeMethodTypeDescriptorNotSupported = nameof(InvokeMethodTypeDescriptorNotSupported);
        internal const string InstanceMethodDoesNotExist = nameof(InstanceMethodDoesNotExist);
        internal const string StaticMethodDoesNotExist = nameof(StaticMethodDoesNotExist);
        internal const string CtorDoesNotExist = nameof(CtorDoesNotExist);
        internal const string SetFieldTypeDescriptorNotSupported = nameof(SetFieldTypeDescriptorNotSupported);
        internal const string InstanceFieldDoesNotExist = nameof(InstanceFieldDoesNotExist);
        internal const string StaticFieldDoesNotExist = nameof(StaticFieldDoesNotExist);
        internal const string GetFieldTypeDescriptorNotSupported = nameof(GetFieldTypeDescriptorNotSupported);
        internal const string ParseNotAGenericType = nameof(ParseNotAGenericType);
        internal const string ParseTypeArgsLengthMismatch = nameof(ParseTypeArgsLengthMismatch);
        internal const string ParseCannotResolveTypeArg = nameof(ParseCannotResolveTypeArg);
        internal const string TypeSyntaxError = nameof(TypeSyntaxError);
        internal const string NotAMember = nameof(NotAMember);
        internal const string NotAMethod = nameof(NotAMethod);
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
        internal const string CannotLoadAssembly = nameof(CannotLoadAssembly);
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
        internal const string TypeWithAssemblyName = nameof(TypeWithAssemblyName);
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

        #endregion

        #region Private Constants

        private const string nullReference = "NullReference";
        private const string unavailableResource = "Resource ID not found: {0}";
        private const string invalidResource = "Resource text is not valid for {0} arguments: {1}";

        #endregion
        
        #endregion

        #region Fields

        private static readonly DynamicResourceManager resourceManager = new DynamicResourceManager("KGySoft.Libraries.Messages", Reflector.KGySoftLibrariesAssembly);

        #endregion

        #region Methods

        #region Internal Methods

        internal static string Get([NotNull]string id)
        {
            return resourceManager.GetString(id, LanguageSettings.DisplayLanguage) ?? String.Format(unavailableResource, id);
        }

        internal static string Get([NotNull]string id, params object[] args)
        {
            string format = Get(id);
            return args == null || args.Length == 0 ? format : SafeFormat(format, args);
        }

        #endregion

        #region Private Methods

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
