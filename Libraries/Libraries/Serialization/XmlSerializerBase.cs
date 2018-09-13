using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using KGySoft.Libraries.Collections;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;

namespace KGySoft.Libraries.Serialization
{
    internal abstract class XmlSerializerBase
    {
        private static readonly HashSet<Type> trustedCollections = new HashSet<Type>
        {
            typeof(List<>),
            typeof(LinkedList<>),
            typeof(Queue),
            typeof(Queue<>),
            typeof(Stack),
            typeof(Stack<>),
            typeof(BitArray),
            typeof(ArrayList),
            typeof(CircularList<>),

            typeof(ConcurrentBag<>),
            typeof(ConcurrentQueue<>),
            typeof(ConcurrentStack<>),
        };

        private static readonly Cache<Type, bool> trustedTypesCache = new Cache<Type, bool>(IsTypeTrusted);

        private static bool IsTypeTrusted(Type type) =>
            // has default constructor
            type.CanBeCreatedWithoutParameters()
            // has only public get/set non-delegate properties, or read-only properties of trusted collections
            && type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).All(p => p.GetGetMethod() != null
                && !p.PropertyType.IsDelegate() 
                && (p.GetSetMethod() != null || typeof(IXmlSerializable).IsAssignableFrom(p.PropertyType) || IsTrustedCollection(p.PropertyType)))
            // and all fields are writable (or read-only of trusted collections) and public (or generated) and non-delegates
            && type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).All(f => (!f.IsInitOnly || IsTrustedCollection(f.FieldType))
                && !f.FieldType.IsDelegate()
                && (f.IsPublic || Attribute.GetCustomAttribute(f, typeof(CompilerGeneratedAttribute), false) != null))
            // and the type has no instance events
            && type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length == 0;

        private HashSet<object> serObjects;

        protected XmlSerializationOptions Options { get; }

        private HashSet<object> SerObjects => serObjects ?? (serObjects = new HashSet<object>(ReferenceEqualityComparer.Comparer));

        protected XmlSerializerBase(XmlSerializationOptions options) => Options = options;

        protected bool IsRecursiveSerializationEnabled => (Options & XmlSerializationOptions.RecursiveSerializationAsFallback) != XmlSerializationOptions.None;

        protected bool IsBinarySerializationEnabled => (Options & XmlSerializationOptions.BinarySerializationAsFallback) != XmlSerializationOptions.None;

        protected bool IsCompactSerializationValueTypesEnabled => (Options & XmlSerializationOptions.CompactSerializationOfStructures) != XmlSerializationOptions.None;

        protected bool ProcessXmlSerializable => (Options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None;

        protected bool ExcludeFields => (Options & XmlSerializationOptions.ExcludeFields) != XmlSerializationOptions.None;

        protected bool ForceReadonlyMembersAndCollections => (Options & XmlSerializationOptions.ForcedSerializationOfReadOnlyMembersAndCollections) != XmlSerializationOptions.None;

        protected BinarySerializationOptions GetBinarySerializationOptions()
        {
            // compact, recursive: always enabled when binary serializing because they cause no problem
            BinarySerializationOptions result = BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.RecursiveSerializationAsFallback; // | CompactSerializationOfBoolCollections

            // no fully qualified names -> omitting even in binary serializer
            if ((Options & XmlSerializationOptions.FullyQualifiedNames) == XmlSerializationOptions.None)
            {
                result |= BinarySerializationOptions.OmitAssemblyQualifiedNames;
            }
            return result;
        }

        protected static bool IsTrustedCollection(Type type)
            => type.IsArray || trustedCollections.Contains(type.IsGenericType ? type.GetGenericTypeDefinition() : type);

        protected bool IsTrustedType(Type type)
        {
            lock (trustedTypesCache)
            {
                return trustedTypesCache[type];
            }
        }

        protected IEnumerable<MemberInfo> GetMembersToSerialize(object obj)
        {
            Type type = obj.GetType();

            // getting read-write non-indexer readable instance properties
            IEnumerable<MemberInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0 
                    && p.CanRead 
                    && (p.CanWrite
                        // read-only are accepted only if forced
                        || ForceReadonlyMembersAndCollections
                        // or is XmlSerializable
                        || (ProcessXmlSerializable && typeof(IXmlSerializable).IsAssignableFrom(p.PropertyType))
                        // or the collection is not read-only (regardless of constructors)
                        || p.PropertyType.IsReadWriteCollection(Reflector.GetProperty(obj, p))));
            if (ExcludeFields)
                return properties;

            // getting non read-only instance fields
            IEnumerable<MemberInfo> fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly
                    // read-only fields are serialized only if forced
                    || ForceReadonlyMembersAndCollections
                    // or if it is a read-write collection or a collection that can be created by a constructor (because a read-only field also can be set by reflection)
                    || f.FieldType.IsSupportedCollectionForReflection(out var _, out var _, out Type elementType, out var _) || elementType != null && f.FieldType.IsReadWriteCollection(Reflector.GetField(obj, f)));
            return fields.Concat(properties);
        }

        protected bool SkipMember(object obj, MemberInfo member, out object value, out DesignerSerializationVisibility visibility)
        {
            value = null;

            // skipping non-serializable members
            // Skip 1.) hidden by DesignerSerializationVisibility
            Attribute[] attrs = Attribute.GetCustomAttributes(member, typeof(DesignerSerializationVisibilityAttribute), true);
            visibility = attrs.Length > 0 ? ((DesignerSerializationVisibilityAttribute)attrs[0]).Visibility : DesignerSerializationVisibilityAttribute.Default.Visibility;
            if (visibility == DesignerSerializationVisibility.Hidden)
                return true;

            // Skip 2.) ShouldSerialize<MemberName> method returns false
            if ((Options & XmlSerializationOptions.IgnoreShouldSerialize) == XmlSerializationOptions.None)
            {
                MethodInfo shouldSerializeMethod = member.DeclaringType?.GetMethod(XmlSerializer.MethodShouldSerialize + member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (shouldSerializeMethod != null && shouldSerializeMethod.ReturnType == typeof(bool) && !(bool)Reflector.RunMethod(obj, shouldSerializeMethod))
                    return true;
            }

            // Skip 3.) DefaultValue equals to property value
            bool hasDefaultValue = false;
            object defaultValue = null;
            if ((Options & XmlSerializationOptions.IgnoreDefaultValueAttribute) == XmlSerializationOptions.None)
            {
                attrs = Attribute.GetCustomAttributes(member, typeof(DefaultValueAttribute), true);
                hasDefaultValue = attrs.Length > 0;
                if (hasDefaultValue)
                    defaultValue = ((DefaultValueAttribute)attrs[0]).Value;
            }

            PropertyInfo property = member as PropertyInfo;
            FieldInfo field = property == null ? (FieldInfo)member : null;
            Type memberType = property != null ? property.PropertyType : field.FieldType;
            if (!hasDefaultValue && (Options & XmlSerializationOptions.AutoGenerateDefaultValuesAsFallback) != XmlSerializationOptions.None)
            {
                hasDefaultValue = true;
                defaultValue = memberType.IsValueType ? Reflector.Construct(memberType) : null;
            }

            value = property != null ? Reflector.GetProperty(obj, property) : Reflector.GetField(obj, field);
            return hasDefaultValue && Equals(value, defaultValue);
        }

        /// <summary>
        /// Registers object to detect circular reference.
        /// Must be called from inside of try-finally to remove lock in finally if necessary.
        /// </summary>
        protected void RegisterSerializedObject(object obj)
        {
            if (obj == null || obj.GetType().IsValueType)
                return;

            if (SerObjects.Contains(obj))
                throw new ReflectionException(Res.Get(Res.XmlCircularReference, obj));
            serObjects.Add(obj);
        }

        protected void UnregisterSerializedObject(object obj)
        {
            if (obj == null || obj.GetType().IsValueType)
                return;
            serObjects.Remove(obj);
        }

        protected string GetTypeString(Type type) => type.GetTypeName((Options & XmlSerializationOptions.FullyQualifiedNames) != XmlSerializationOptions.None);

        protected string GetStringValue(object value, out bool spacePreserve, out bool escaped)
        {
            spacePreserve = false;
            escaped = false;

            if (value is bool)
                return XmlConvert.ToString((bool)value);
            if (value is double)
                return ((double)value).ToRoundtripString();
            if (value is float)
                return ((float)value).ToRoundtripString();
            if (value is decimal)
                return ((decimal)value).ToRoundtripString();
            if (value is DateTime)
                return XmlConvert.ToString((DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
            if (value is DateTimeOffset)
                return XmlConvert.ToString((DateTimeOffset)value);
            Type type = value as Type;
            if (type != null)
            {
                //if (value.GetType() != Reflector.RuntimeType)
                //    throw new NotSupportedException(Res.Get(Res.XmlNonRuntimeType));
                //if (type.IsGenericParameter)
                //    throw new NotSupportedException(Res.Get(Res.XmlGenericTypeParam));
                return GetTypeString(type);
            }

            string result = value.ToString();
            if (result.Length == 0)
                return result;

            //bool prevWhiteSpace = false;
            bool escapeNewline = (Options & XmlSerializationOptions.EscapeNewlineCharacters) != XmlSerializationOptions.None;
            StringBuilder escapedResult = null;
            spacePreserve = IsWhiteSpace(result[0], escapeNewline);

            // checking result for escaping
            for (int i = 0; i < result.Length; i++)
            {
                bool isValidSurrogate;
                if (EscapeNeeded(result, i, escapeNewline, out isValidSurrogate))
                {
                    if (escapedResult == null)
                        escapedResult = new StringBuilder(result.Substring(0, i).Replace(@"\", @"\\"));

                    escapedResult.Append(@"\" + ((ushort)result[i]).ToString("X4"));
                }
                else
                {
                    if (escapedResult != null)
                    {
                        escapedResult.Append(result[i]);
                        if (result[i] == '\\')
                            escapedResult.Append('\\');
                        else if (isValidSurrogate)
                            escapedResult.Append(result[i + 1]);
                    }

                    if (isValidSurrogate)
                        i++;
                }
            }

            if (escapedResult != null)
            {
                escaped = true;
                return escapedResult.ToString();
            }

            return result;
        }

        private static bool IsWhiteSpace(char c, bool ignoreNewline)
        {
            // U+0009 = <control> HORIZONTAL TAB 
            // U+000a = <control> LINE FEED
            // U+000b = <control> VERTICAL TAB 
            // U+000c = <contorl> FORM FEED 
            // U+000d = <control> CARRIAGE RETURN
            // U+0085 = <control> NEXT LINE 
            // U+00a0 = NO-BREAK SPACE

            if (c == ' ' || c == '\t')
                return true;

            if (ignoreNewline)
                return false;

            return c == '\r' || c == '\n';

            //if ((c == ' ') || (c >= '\x0009' && c <= '\x000d') || c == '\x00a0' || c == '\x0085')
            //    return (true);
        }

        /// <summary>
        /// Gets whether a character has to be escaped
        /// </summary>
        private static bool EscapeNeeded(string s, int index, bool escapeNewlines, out bool isValidSurrogate)
        {
            isValidSurrogate = false;
            char c = s[index];
            if (c == '\t' // TAB is ok
                || (c >= 0x20 && c.IsValidCharacter())
                || (!escapeNewlines && (c == 0xA || c == 0xD))) // \n, \r are ok if new lines are not escaped
            {
                return false;
            }

            // valid surrogate pair
            if (index < s.Length - 1 && Char.IsSurrogatePair(c, s[index + 1]))
            {
                isValidSurrogate = true;
                return false;
            }

            return true;
        }
    }
}
