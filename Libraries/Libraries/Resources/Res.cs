using System;

using KGySoft.Libraries.Reflection;

namespace KGySoft.Libraries.Resources
{
    /// <summary>
    /// Contains the IDs of string resources.
    /// </summary>
    internal static class Res
    {
        private static readonly DynamicResourceManager resourceManager = new DynamicResourceManager("KGySoft.Libraries.Messages", Reflector.KGySoftLibrariesAssembly);

        internal const string ArgumentNull = "ArgumentNull";
        internal const string ArgumentOutOfRange = "ArgumentOutOfRange";
        internal const string ArgumentInvalidString = "ArgumentInvalidString";
        internal const string KeyNotFound = "KeyNotFound";
        internal const string ObjectDisposed = "ObjectDisposed";
        internal const string ArgumentEmpty = "ArgumentEmpty";
        internal const string DuplicateKey = "DuplicateKey";

        internal const string NeutralResourceFileNotFoundResX = "NeutralResourceFileNotFoundResX";
        internal const string NeutralResourceNotFoundCompiled = "NeutralResourceNotFoundCompiled";
        internal const string NeutralResourceNotFoundHybrid = "NeutralResourceNotFoundHybrid";
        internal const string ValueContainsIllegalPathCharacters = "ValueContainsIllegalPathCharacters";
        internal const string TypeParameterIsNotEnum = "TypeParameterIsNotEnum";
        internal const string ValueCannotBeParsedAsEnum = "ValueCannotBeParsedAsEnum";
        internal const string InvalidKeyType = "InvalidKeyType";
        internal const string InvalidValueType = "InvalidValueType";
        internal const string ModifyNotSupported = "ModifyNotSupported";
        internal const string DestArrayShort = "DestArrayShort";
        internal const string EnumerationNotStartedOrFinished = "EnumerationNotStartedOrFinished";
        internal const string EnumerationCollectionModified = "EnumerationCollectionModified";
        internal const string ArrayDimension = "ArrayDimension";
        internal const string ArrayTypeInvalid = "ArrayTypeInvalid";
        internal const string CacheNullLoaderInvoke = "CacheNullLoaderInvoke";
        internal const string CacheNullLoader = "CacheNullLoader";
        internal const string CacheKeyNotFound = "CacheKeyNotFound";
        internal const string CacheMinSize = "CacheMinSize";
        internal const string CacheStatistics = "CacheStatistics";
        internal const string InvalidOffsLen = "InvalidOffsLen";
        internal const string ComparerFail = "ComparerFail";
        internal const string CapacityTooSmall = "CapacityTooSmall";
        internal const string InsertByIndexNotSupported = "InsertByIndexNotSupported";
        internal const string InvalidKeyValueType = "InvalidKeyValueType";
        internal const string EnumerableCannotAdd = "EnumerableCannotAdd";
        internal const string EnumerableCannotClear = "EnumerableCannotClear";
        internal const string StreamCannotRead = "StreamCannotRead";
        internal const string StreamCannotWrite = "StreamCannotWrite";
        internal const string StreamCannotSeek = "StreamCannotSeek";
        internal const string SeparatorNullOrEmpty = "SeparatorNullOrEmpty";
        internal const string NotAnInstanceOfType = "NotAnInstanceOfType";
        internal const string SetConstantField = "SetConstantField";
        internal const string NotSupportedMemberType = "NotSupportedMemberType";
        internal const string InvalidMethodBase = "InvalidMethodBase";
        internal const string CannotTreatPropertySetter = "CannotTreatPropertySetter";
        internal const string TypeOrCtorInfoExpected = "TypeOrCtorInfoExpected";
        internal const string PropertyHasNoGetter = "PropertyHasNoGetter";
        internal const string PropertyHasNoSetter = "PropertyHasNoSetter";
        internal const string ParsedValueNull = "ParsedValueNull";
        internal const string NotABool = "NotABool";
        internal const string NotAType = "NotAType";
        internal const string TypeCannotBeParsed = "TypeCannotBeParsed";
        internal const string ParseError = "ParseError";
        internal const string SetPropertyTypeDescriptorNotSupported = "SetPropertyTypeDescriptorNotSupported";
        internal const string CannotSetStaticPropertyTypeDescriptor = "CannotSetStaticPropertyTypeDescriptor";
        internal const string CannotSetPropertyTypeDescriptor = "CannotSetPropertyTypeDescriptor";
        internal const string InstancePropertyDoesNotExist = "InstancePropertyDoesNotExist";
        internal const string StaticPropertyDoesNotExist = "StaticPropertyDoesNotExist";
        internal const string EmptyIndices = "EmptyIndices";
        internal const string SetIndexerTypeDescriptorNotSupported = "SetIndexerTypeDescriptorNotSupported";
        internal const string IndexParamsLengthMismatch = "IndexParamsLengthMismatch";
        internal const string IndexParamsTypeMismatch = "IndexParamsTypeMismatch";
        internal const string IndexerDoesNotExist = "IndexerDoesNotExist";
        internal const string InstanceIsNull = "InstanceIsNull";
        internal const string CannotGetPropertyTypeDescriptor = "CannotGetPropertyTypeDescriptor";
        internal const string CannotGetStaticPropertyTypeDescriptor = "CannotGetStaticPropertyTypeDescriptor";
        internal const string GetIndexerTypeDescriptorNotSupported = "GetIndexerTypeDescriptorNotSupported";
        internal const string TypeParamsAreNull = "TypeParamsAreNull";
        internal const string TypeArgsLengthMismatch = "TypeArgsLengthMismatch";
        internal const string CannotCreateGenericMethod = "CannotCreateGenericMethod";
        internal const string InvokeMethodTypeDescriptorNotSupported = "InvokeMethodTypeDescriptorNotSupported";
        internal const string InstanceMethodDoesNotExist = "InstanceMethodDoesNotExist";
        internal const string StaticMethodDoesNotExist = "StaticMethodDoesNotExist";
        internal const string CtorDoesNotExist = "CtorDoesNotExist";
        internal const string SetFieldTypeDescriptorNotSupported = "SetFieldTypeDescriptorNotSupported";
        internal const string InstanceFieldDoesNotExist = "InstanceFieldDoesNotExist";
        internal const string StaticFieldDoesNotExist = "StaticFieldDoesNotExist";
        internal const string GetFieldTypeDescriptorNotSupported = "GetFieldTypeDescriptorNotSupported";
        internal const string ParseNotAGenericType = "ParseNotAGenericType";
        internal const string ParseTypeArgsLengthMismatch = "ParseTypeArgsLengthMismatch";
        internal const string ParseCannotResolveTypeArg = "ParseCannotResolveTypeArg";
        internal const string TypeSyntaxError = "TypeSyntaxError";
        internal const string NotAMember = "NotAMember";
        internal const string NotAMethod = "NotAMethod";
        internal const string SerializationNotSupported = "SerializationNotSupported";
        internal const string IEnumerableExpected = "IEnumerableExpected";
        internal const string InvalidStreamData = "InvalidStreamData";
        internal const string InvalidEnumBase = "InvalidEnumBase";
        internal const string CannotDeserializeObject = "CannotDeserializeObject";
        internal const string ObjectHierarchyChanged = "ObjectHierarchyChanged";
        internal const string MissingField = "MissingField";
        internal const string MissingFieldBase = "MissingFieldBase";
        internal const string MissingISerializableCtor = "MissingISerializableCtor";
        internal const string SurrogateChangedObject = "SurrogateChangedObject";
        internal const string CannotDecodeDataType = "CannotDecodeDataType";
        internal const string CannotDecodeCollectionType = "CannotDecodeCollectionType";
        internal const string ReadOnlyCollectionNotSupported = "ReadOnlyCollectionNotSupported";
        internal const string CannotResolveType = "CannotResolveType";
        internal const string CircularIObjectReference = "CircularIObjectReference";
        internal const string DeserializeUnexpectedId = "DeserializeUnexpectedId";
        internal const string CannotResolveTypeInAssembly = "CannotResolveTypeInAssembly";
        internal const string CannotLoadAssembly = "CannotLoadAssembly";
        internal const string ValueTypeExpected = "ValueTypeExpected";
        internal const string DataLenghtTooSmall = "DataLenghtTooSmall";
        internal const string UnexpectedSerializationInfoElement = "UnexpectedSerializationInfoElement";
        internal const string ObjectHierarchyChangedSurrogate = "ObjectHierarchyChangedSurrogate";
        internal const string MissingFieldSurrogate = "MissingFieldSurrogate";
        internal const string UnexpectedFieldType = "UnexpectedFieldType";
        internal const string Undefined = "Undefined";
        internal const string XmlSerializeReadOnlyRoot = "XmlSerializeReadOnlyRoot";
        internal const string XmlCannotSerialize = "XmlCannotSerialize";
        internal const string XmlRootExpected = "XmlRootExpected";
        internal const string XmlCannotResolveType = "XmlCannotResolveType";
        internal const string XmlRootTypeMissing = "XmlRootTypeMissing";
        internal const string XmlDeserializeNotSupported = "XmlDeserializeNotSupported";
        internal const string XmlSerializeReadOnlyCollection = "XmlSerializeReadOnlyCollection";
        internal const string XmlCannotSerializeProperty = "XmlCannotSerializeProperty";
        internal const string XmlCannotSerializeValueType = "XmlCannotSerializeValueType";
        internal const string XmlBinarySerializationFailed = "XmlBinarySerializationFailed";
        internal const string XmlCannotSerializeArrayElement = "XmlCannotSerializeArrayElement";
        internal const string XmlCannotSerializeCollectionElement = "XmlCannotSerializeCollectionElement";
        internal const string NotAnIXmlSerializable = "NotAnIXmlSerializable";
        internal const string XmlArrayPropertyHasNoSetter = "XmlArrayPropertyHasNoSetter";
        internal const string XmlArrayPropertyHasNoSetterNull = "XmlArrayPropertyHasNoSetterNull";
        internal const string XmlCollectionPropertyHasNoSetterNull = "XmlCollectionPropertyHasNoSetterNull";
        internal const string XmlCollectionPropertyHasNoSetter = "XmlCollectionPropertyHasNoSetter";
        internal const string XmlCannotCreateCollection = "XmlCannotCreateCollection";
        internal const string XmlPropertyHasNoSetter = "XmlPropertyHasNoSetter";
        internal const string XmlItemExpected = "XmlItemExpected";
        internal const string XmlCannotDetermineElementType = "XmlCannotDetermineElementType";
        internal const string XmlNotACollection = "XmlNotACollection";
        internal const string XmlHasNoProperty = "XmlHasNoProperty";
        internal const string XmlNoContent = "XmlNoContent";
        internal const string XmlLengthInvalidType = "XmlLengthInvalidType";
        internal const string XmlArraySizeMismatch = "XmlArraySizeMismatch";
        internal const string XmlArrayRankMismatch = "XmlArrayRankMismatch";
        internal const string XmlArrayDimensionSizeMismatch = "XmlArrayDimensionSizeMismatch";
        internal const string XmlArrayLowerBoundMismatch = "XmlArrayLowerBoundMismatch";
        internal const string XmlCrcError = "XmlCrcError";
        internal const string XmlInconsistentArrayLength = "XmlInconsistentArrayLength";
        internal const string XmlCrcFormat = "XmlCrcFormat";
        internal const string XmlMixedArrayFormats = "XmlMixedArrayFormats";
        internal const string XmlUnexpectedElement = "XmlUnexpectedElement";
        internal const string XmlKeyValueTypeMissing = "XmlKeyValueTypeMissing";
        internal const string XmlKeyValueMissingKey = "XmlKeyValueMissingKey";
        internal const string XmlKeyValueMissingValue = "XmlKeyValueMissingValue";
        internal const string XmlMultipleKeys = "XmlMultipleKeys";
        internal const string XmlMultipleValues = "XmlMultipleValues";
        internal const string XmlInvalidEscapedContent = "XmlInvalidEscapedContent";
        internal const string XmlUnexpectedEnd = "XmlUnexpectedEnd";
        internal const string XmlCircularReference = "XmlCircularReference";
        internal const string ExceptionMessage = "ExceptionMessage";
        internal const string ExceptionMessageNotAvailable = "ExceptionMessageNotAvailable";
        internal const string InnerException = "InnerException";
        internal const string InnerExceptionEnd = "InnerExceptionEnd";
        internal const string Win32ErrorCode = "Win32ErrorCode";
        internal const string SystemInformation = "SystemInformation";
        internal const string UserInformation = "UserInformation";
        internal const string ExceptionSource = "ExceptionSource";
        internal const string ExceptionSourceNotAvailable = "ExceptionSourceNotAvailable";
        internal const string ExceptionType = "ExceptionType";
        internal const string ExceptionTypeNotAvailable = "ExceptionTypeNotAvailable";
        internal const string ExceptionTargetSite = "ExceptionTargetSite";
        internal const string ExceptionTargetSiteNotAvailable = "ExceptionTargetSiteNotAvailable";
        internal const string ExceptionTargetSiteNotAccessible = "ExceptionTargetSiteNotAccessible";
        internal const string RemoteStackTrace = "RemoteStackTrace";
        internal const string StackTrace = "StackTrace";
        internal const string LocalStackTrace = "LocalStackTrace";
        internal const string NativeOffset = "NativeOffset";
        internal const string SourceOffset = "SourceOffset";
        internal const string ILOffset = "ILOffset";
        internal const string DateAndTime = "DateAndTime";
        internal const string OperatingSystem = "OperatingSystem";
        internal const string Environment = "Environment";
        internal const string ProcessorCount = "ProcessorCount";
        internal const string ClrVersion = "ClrVersion";
        internal const string WorkingSet = "WorkingSet";
        internal const string CommandLine = "CommandLine";
        internal const string ApplicationDomain = "ApplicationDomain";
        internal const string MachineName = "MachineName";
        internal const string UserName = "UserName";
        internal const string CurrentUser = "CurrentUser";
        internal const string CannotGetDomain = "CannotGetDomain";
        internal const string AssemblyCodebase = "AssemblyCodebase";
        internal const string AssemblyFullName = "AssemblyFullName";
        internal const string AssemblyVersion = "AssemblyVersion";
        internal const string AssemblyBuildDate = "AssemblyBuildDate";
        internal const string Uncategorized = "Uncategorized";
        internal const string InvalidResXReaderPropertyChange = "InvalidResXReaderPropertyChange";
        internal const string InvalidResXResourceNoName = "InvalidResXResourceNoName";
        internal const string XmlMissingAttribute = "XmlMissingAttribute";
        internal const string ResXFileMimeTypeNotSupported = "ResXFileMimeTypeNotSupported";
        internal const string ResXMimeTypeNotSupported = "ResXMimeTypeNotSupported";
        internal const string ResXReaderNotSupported = "ResXReaderNotSupported";
        internal const string ResXWriterNotSupported = "ResXWriterNotSupported";
        internal const string InvalidResXFile = "InvalidResXFile";
        internal const string TypeLoadException = "TypeLoadException";
        internal const string TypeLoadExceptionShort = "TypeLoadExceptionShort";
        internal const string TypeWithAssemblyName = "TypeWithAssemblyName";
        internal const string NonStringResourceWithType = "NonStringResourceWithType";
        internal const string ConvertFromStringNotSupportedAt = "ConvertFromStringNotSupportedAt";
        internal const string ConvertFromStringNotSupported = "ConvertFromStringNotSupported";
        internal const string ConvertFromByteArrayNotSupportedAt = "ConvertFromByteArrayNotSupportedAt";
        internal const string ConvertFromByteArrayNotSupported = "ConvertFromByteArrayNotSupported";
        internal const string InvalidResXWriterPropertyChange = "InvalidResXWriterPropertyChange";
        internal const string ResXResourceWriterSaved = "ResXResourceWriterSaved";
        internal const string ResXFileRefFileNotFound = "ResXFileRefFileNotFound";
        internal const string SeparatorInvalidHex = "SeparatorInvalidHex";
        internal const string SeparatorInvalidDec = "SeparatorInvalidDec";
        internal const string HybridResSourceBinary = "HybridResSourceBinary";
        internal const string InvalidDrmPropertyChange = "InvalidDrmPropertyChange";

        private const string nullReference = "NullReference";
        private const string unavailableResource = "Resource ID not found: {0}";
        private const string invalidResource = "Resource text is not valid for {0} arguments: {1}";

        internal static string Get(string id)
        {
            return resourceManager.GetString(id, LanguageSettings.DisplayLanguage) ?? String.Format(unavailableResource, id);
        }

        internal static string Get(string id, params object[] args)
        {
            string format = Get(id);

            if ((args == null) || (args.Length == 0))
                return format;

            return SafeFormat(format, args);
        }

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
    }
}
