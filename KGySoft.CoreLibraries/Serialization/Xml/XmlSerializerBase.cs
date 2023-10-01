#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: XmlSerializerBase.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

#region Used Namespaces

using System;
using System.Collections;
#if !NET35
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;

using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;
using KGySoft.Serialization.Binary;

#endregion

#region Used Aliases

using ReferenceEqualityComparer = KGySoft.CoreLibraries.ReferenceEqualityComparer;

#endregion

#endregion

namespace KGySoft.Serialization.Xml
{
    internal abstract class XmlSerializerBase
    {
        #region Member struct

        private protected readonly struct Member
        {
            #region Fields

            #region Internal Fields

            internal readonly MemberInfo MemberInfo;

            #endregion

            #region Private Fields

            private readonly StringKeyedDictionary<int> memberNamesCounts;

            #endregion

            #endregion

            #region Properties

            internal PropertyInfo? Property => MemberInfo as PropertyInfo;
            internal FieldInfo? Field => MemberInfo as FieldInfo;
            internal bool SpecifyDeclaringType => memberNamesCounts[MemberInfo.Name] > 1 || MemberInfo.Name == XmlSerializer.ElementItem && Reflector.IEnumerableType.IsAssignableFrom(Property?.PropertyType ?? Field!.FieldType);

            #endregion

            #region Constructors

            internal Member(MemberInfo memberInfo, StringKeyedDictionary<int> memberNamesCounts)
            {
                MemberInfo = memberInfo;
                this.memberNamesCounts = memberNamesCounts;
            }

            #endregion

            #region Methods

            public override string ToString() => MemberInfo.Name;

            #endregion
        }

        #endregion

        #region Fields

        #region Static Fields

        private static readonly HashSet<Type> trustedCollections = new HashSet<Type>
        {
            Reflector.ListGenType,
            typeof(LinkedList<>),
            typeof(Queue<>),
            typeof(Stack<>),

            typeof(ArrayList),
            typeof(Queue),
            typeof(Stack),
            Reflector.BitArrayType,
            Reflector.StringCollectionType,

            typeof(CircularList<>),

#if !NET35
            typeof(ConcurrentBag<>),
            typeof(ConcurrentQueue<>),
            typeof(ConcurrentStack<>),
#endif
        };

        private static readonly Dictionary<Type, ComparerType> knownCollectionsWithComparer = new()
        {
            { typeof(HashSet<>), ComparerType.Default },
            { typeof(ThreadSafeHashSet<>), ComparerType.Default },
            { Reflector.DictionaryGenType, ComparerType.Default },
            { typeof(SortedList<,>), ComparerType.Default },
            { typeof(SortedDictionary<,>), ComparerType.Default },
            { typeof(CircularSortedList<,>), ComparerType.Default },
            { typeof(ThreadSafeDictionary<,>), ComparerType.Default },
            { typeof(StringKeyedDictionary<>), ComparerType.StringSegmentOrdinal },
#if !NET35
            { typeof(SortedSet<>), ComparerType.Default },
            { typeof(ConcurrentDictionary<,>), ComparerType.Default },
#endif
            { typeof(Hashtable), ComparerType.None },
            { typeof(SortedList), ComparerType.Default },
            { typeof(ListDictionary), ComparerType.None },
            { typeof(HybridDictionary), ComparerType.None },
            { typeof(OrderedDictionary), ComparerType.None }, // ISSUE: can be read-only
        };

        private static readonly Type[] escapedNativelySupportedTypes = 
        {
            Reflector.StringType, Reflector.CharType, Reflector.RuntimeType,
#if NETCOREAPP3_0_OR_GREATER
            Reflector.RuneType  
#endif
        };

        private static readonly IThreadSafeCacheAccessor<Type, bool> trustedTypesCache = ThreadSafeCacheFactory.Create<Type, bool>(IsTypeTrusted, LockFreeCacheOptions.Profile128);

        #endregion

        #region Instance Fields

        private HashSet<object>? serObjects;

        #endregion

        #endregion

        #region Properties

        #region Internal Properties

        internal static HashSet<Type> TrustedCollections => trustedCollections;

        #endregion

        #region Private Protected Properties

        private protected XmlSerializationOptions Options { get; }
        private protected bool FullyQualifiedNames => (Options & XmlSerializationOptions.FullyQualifiedNames) != XmlSerializationOptions.None;
        private protected bool BinarySerializationAsFallback => (Options & XmlSerializationOptions.BinarySerializationAsFallback) != XmlSerializationOptions.None;
        private protected bool RecursiveSerializationAsFallback => (Options & XmlSerializationOptions.RecursiveSerializationAsFallback) != XmlSerializationOptions.None;
        private protected bool CompactSerializationOfStructures => (Options & XmlSerializationOptions.CompactSerializationOfStructures) != XmlSerializationOptions.None;
        private protected bool ProcessXmlSerializable => (Options & XmlSerializationOptions.IgnoreIXmlSerializable) == XmlSerializationOptions.None;
        private protected bool ExcludeFields => (Options & XmlSerializationOptions.ExcludeFields) != XmlSerializationOptions.None;
        private protected bool ForceReadonlyMembersAndCollections => (Options & XmlSerializationOptions.ForcedSerializationOfReadOnlyMembersAndCollections) != XmlSerializationOptions.None;
        private protected bool IgnoreTypeForwardedFromAttribute => (Options & XmlSerializationOptions.IgnoreTypeForwardedFromAttribute) != XmlSerializationOptions.None;
        private protected bool IncludeRefProperties => (Options & XmlSerializationOptions.IncludeRefProperties) != XmlSerializationOptions.None;

        #endregion

        #region Private Properties

        private HashSet<object> SerObjects => serObjects ??= new HashSet<object>(ReferenceEqualityComparer.Comparer);

        #endregion

        #endregion

        #region Constructors

        private protected XmlSerializerBase(XmlSerializationOptions options)
        {
            if (!options.AllFlagsDefined())
                Throw.EnumArgumentOutOfRange(Argument.options, options);
            Options = options;
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Private Protected Methods

        private protected static bool IsTrustedType(Type type) => trustedTypesCache[type];

        private protected static bool IsTrustedCollection(Type type)
            => type.IsArray || trustedCollections.Contains(type.IsGenericType ? type.GetGenericTypeDefinition() : type);

        private protected static bool IsKnownCollection(Type type)
            => knownCollectionsWithComparer.ContainsKey(type.IsGenericType ? type.GetGenericTypeDefinition() : type);

        private protected static ComparerType? GetComparer([NoEnumeration]IEnumerable collection)
        {
            Type type = collection.GetType();

            bool isGeneric = type.IsGenericType;
            if (!knownCollectionsWithComparer.TryGetValue(isGeneric ? type.GetGenericTypeDefinition() : type, out ComparerType defaultComparer))
                return null;

            // special handling for HybridDictionary, which has no actual comparer but can be case insensitive
            if (type == typeof(HybridDictionary))
                return collection.IsCaseInsensitive() ? ComparerType.CaseInsensitive : ComparerType.None;

            object? comparer = collection.GetComparer();
            ComparerType comparerType = ToComparerType(comparer, isGeneric ? type.GetGenericArguments()[0] : null);
#if NETFRAMEWORK
            if (comparerType == ComparerType.EnumComparer && type.Assembly == typeof(XmlSerializerBase).Assembly)
                comparerType = ComparerType.Default;
#endif
            return comparerType == defaultComparer ? ComparerType.None : comparerType;
        }

        #endregion

        #region Private Methods

        private static bool IsTypeTrusted(Type type) =>
            // has default constructor
            type.CanBeCreatedWithoutParameters()
            // properties:
            && type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).All(p =>
                // have public getter
                (p.GetGetMethod() != null
                    // of a non-delegate type
                    && !p.PropertyType.IsDelegate()
                    // and must have public setter, unless its type is IXmlSerializable or a trusted collection
                    && (p.GetSetMethod() != null
                        || typeof(IXmlSerializable).IsAssignableFrom(p.PropertyType)
                        || IsTrustedCollection(p.PropertyType) || IsKnownCollection(p.PropertyType)))
                // or, if it is an explicit interface implementation, we just ignore it
                || Reflector.IsExplicitInterfaceImplementation(p))
            // fields:
            && type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).All(f =>
                // must be public
                (f.IsPublic
                    // of a non delegate type
                    && !f.FieldType.IsDelegate()
                    // and must be non read-only unless its type is a trusted collection
                    && (!f.IsInitOnly || IsTrustedCollection(f.FieldType) || IsKnownCollection(f.FieldType)))
                // or, if it is a compiler-generated field, we just ignore it
                || Attribute.GetCustomAttribute(f, typeof(CompilerGeneratedAttribute), false) != null)
            // and the type has no instance events
            && type.GetEvents(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length == 0;

        private static bool IsWhiteSpace(char c, bool ignoreNewline)
        {
            if (c is ' ' or '\t')
                return true;

            if (ignoreNewline)
                return false;

            return c is '\r' or '\n';

            // U+0009 = <control> HORIZONTAL TAB
            // U+000a = <control> LINE FEED
            // U+000b = <control> VERTICAL TAB
            // U+000c = <control> FORM FEED
            // U+000d = <control> CARRIAGE RETURN
            // U+0085 = <control> NEXT LINE
            // U+00a0 = NO-BREAK SPACE
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

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Very straightforward switch with many branches")]
        private static ComparerType ToComparerType(object? comparer, Type? collectionGenericArgument) => comparer switch
        {
            null => ComparerType.None,

            StringComparer stringComparer => Equals(StringComparer.Ordinal, stringComparer) ? ComparerType.Ordinal
                : Equals(StringComparer.OrdinalIgnoreCase, stringComparer) ? ComparerType.OrdinalIgnoreCase
                : Equals(StringComparer.InvariantCulture, stringComparer) ? ComparerType.Invariant
                : Equals(StringComparer.InvariantCultureIgnoreCase, stringComparer) ? ComparerType.InvariantIgnoreCase
#if NET9_0_OR_GREATER // TODO - https://github.com/dotnet/runtime/issues/77679
#error check if already available
                : Equals(StringComparer.OrdinalNonRandomized, stringComparer) ? ComparerType.OrdinalNonRandomized
                : Equals(StringComparer.OrdinalIgnoreCaseNonRandomized, stringComparer) ? ComparerType.OrdinalIgnoreCaseNonRandomized
#endif
                : ComparerType.Unknown,

            StringSegmentComparer stringSegmentComparer => Equals(StringSegmentComparer.Ordinal, stringSegmentComparer) ? ComparerType.StringSegmentOrdinal
                : Equals(StringSegmentComparer.OrdinalIgnoreCase, stringSegmentComparer) ? ComparerType.StringSegmentOrdinalIgnoreCase
                : Equals(StringSegmentComparer.InvariantCulture, stringSegmentComparer) ? ComparerType.StringSegmentInvariant
                : Equals(StringSegmentComparer.InvariantCultureIgnoreCase, stringSegmentComparer) ? ComparerType.StringSegmentInvariantIgnoreCase
                : Equals(StringSegmentComparer.OrdinalRandomized, stringSegmentComparer) ? ComparerType.StringSegmentOrdinalRandomized
                : Equals(StringSegmentComparer.OrdinalIgnoreCaseRandomized, stringSegmentComparer) ? ComparerType.StringSegmentOrdinalIgnoreCaseRandomized
                : Equals(StringSegmentComparer.OrdinalNonRandomized, stringSegmentComparer) ? ComparerType.StringSegmentOrdinalNonRandomized
                : Equals(StringSegmentComparer.OrdinalIgnoreCaseNonRandomized, stringSegmentComparer) ? ComparerType.StringSegmentOrdinalIgnoreCaseNonRandomized
                : ComparerType.Unknown,

            // Comparer does not override Equals so comparing its CompareInfo and defaulting to reference equality of Comparer if needed
            Comparer nonGenericComparer => nonGenericComparer.CompareInfo() is CompareInfo compareInfo 
                    ? Equals(compareInfo, Comparer.DefaultInvariant.CompareInfo()) ? ComparerType.DefaultInvariant
                    : Equals(compareInfo, Comparer.Default.CompareInfo()) ? ComparerType.Default
                    : ComparerType.Unknown
                : Equals(nonGenericComparer, Comparer.DefaultInvariant) ? ComparerType.DefaultInvariant
                : Equals(nonGenericComparer, Comparer.Default) ? ComparerType.Default
                : ComparerType.Unknown,

            // Same as for Comparer but CaseInsensitiveComparer is not even sealed
            CaseInsensitiveComparer caseInsensitiveComparer => caseInsensitiveComparer.GetType() != typeof(CaseInsensitiveComparer) ? ComparerType.Unknown
                : caseInsensitiveComparer.CompareInfo() is CompareInfo compareInfo
                    ? Equals(compareInfo, CaseInsensitiveComparer.DefaultInvariant.CompareInfo()) ? ComparerType.CaseInsensitiveInvariant
                    : Equals(compareInfo, CaseInsensitiveComparer.Default.CompareInfo()) ? ComparerType.CaseInsensitive
                    : ComparerType.Unknown
                : Equals(caseInsensitiveComparer, CaseInsensitiveComparer.DefaultInvariant) ? ComparerType.CaseInsensitiveInvariant
                : Equals(caseInsensitiveComparer, CaseInsensitiveComparer.Default) ? ComparerType.CaseInsensitive
                : ComparerType.Unknown,

            _ => collectionGenericArgument is null ? ComparerType.Unknown
                : Equals(comparer, typeof(EqualityComparer<>).GetPropertyValue(collectionGenericArgument, nameof(EqualityComparer<_>.Default)))
                    || Equals(comparer, typeof(Comparer<>).GetPropertyValue(collectionGenericArgument, nameof(Comparer<_>.Default))) ? ComparerType.Default
                : Equals(comparer, typeof(EnumComparer<>).GetPropertyValue(collectionGenericArgument, nameof(EnumComparer<_>.Comparer))) ? ComparerType.EnumComparer
                : ComparerType.Unknown
        };

        private static bool IsReadOnly(MemberInfo member)
#if NET471_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
            => Attribute.GetCustomAttribute(member, typeof(IsReadOnlyAttribute), false) != null;
#else
            // newer compilers may auto-generate an IsReadOnlyAttribute in every assembly that uses ref readonly members
            => Attribute.GetCustomAttributes(member, false).Any(a => a.GetType().FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute");
#endif

        #endregion

        #endregion

        #region Instance Methods

        private protected BinarySerializationOptions GetBinarySerializationOptions()
        {
            // compact, recursive: always enabled when binary serializing because they cause no problem
            BinarySerializationOptions result = BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.RecursiveSerializationAsFallback; // | CompactSerializationOfBoolCollections

            // no fully qualified names -> omitting even in binary serializer
            if (!FullyQualifiedNames)
                result |= BinarySerializationOptions.OmitAssemblyQualifiedNames;
            else if (IgnoreTypeForwardedFromAttribute)
                result |= BinarySerializationOptions.IgnoreTypeForwardedFromAttribute;

            return result;
        }

        private protected IEnumerable<Member> GetMembersToSerialize(object obj)
        {
            Type type = obj.GetType();

            // getting read-write non-indexer readable instance properties
            IEnumerable<MemberInfo> properties = (type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => (Info: p, Type: p.PropertyType.IsByRef ? p.PropertyType.GetElementType()! : p.PropertyType, p.PropertyType.IsByRef ))
                    .Where(p => p.Info.GetIndexParameters().Length == 0
                        && p.Info.CanRead && (!p.IsByRef || IncludeRefProperties)
                        && (p.Info.CanWrite
                            // read-only is accepted only if forced,
                            || ForceReadonlyMembersAndCollections
                            // or if it's a non-readonly ref property
                            || IncludeRefProperties && p.IsByRef && !IsReadOnly(p.Info)
                            // or is XmlSerializable
                            || (ProcessXmlSerializable && typeof(IXmlSerializable).IsAssignableFrom(p.Type))
                            // or the collection is not read-only (regardless of constructors)
                            || p.Type.IsCollection() && p.Type.IsReadWriteCollection(p.Info.Get(obj))))
                    .Select(p => p.Info))
#if NET35
                    .Cast<MemberInfo>()
#endif
                ;

            // getting non read-only instance fields
            IEnumerable<MemberInfo> fields = ExcludeFields
                    ? Reflector.EmptyArray<MemberInfo>()
                    : type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                        .Where(f => !f.IsInitOnly
                            // read-only fields are serialized only if forced
                            || ForceReadonlyMembersAndCollections
                            // or if it is a read-write collection or a collection that can be created by a constructor (because a read-only field also can be set by reflection)
                            || f.FieldType.IsSupportedCollectionForReflection(out var _, out var _, out Type? elementType, out var _)
                            || elementType != null && f.FieldType != Reflector.StringType && f.FieldType.IsReadWriteCollection(Reflector.GetField(obj, f)))
#if NET35
                    .Cast<MemberInfo>()
#endif
                ;

            var result = new List<Member>();
            var memberNameCounts = new StringKeyedDictionary<int>();
            foreach (MemberInfo mi in fields.Concat(properties))
            {
                if (!memberNameCounts.TryGetValue(mi.Name, out int count))
                    memberNameCounts[mi.Name] = 1;
                else
                    memberNameCounts[mi.Name] = count + 1;
                result.Add(new Member(mi, memberNameCounts));
            }

            return result;
        }

        private protected bool SkipMember(object obj, MemberInfo member, out object? value, ref DesignerSerializationVisibility visibility)
        {
            value = null;

            // skipping non-serializable members
            // Skip 1.) hidden by DesignerSerializationVisibility
            Attribute[] attrs = Attribute.GetCustomAttributes(member, typeof(DesignerSerializationVisibilityAttribute), true);
            visibility = attrs.Length > 0 ? ((DesignerSerializationVisibilityAttribute)attrs[0]).Visibility : visibility;
            if (visibility == DesignerSerializationVisibility.Hidden)
                return true;

            // Skip 2.) ShouldSerialize<MemberName> method returns false
            if ((Options & XmlSerializationOptions.IgnoreShouldSerialize) == XmlSerializationOptions.None)
            {
                MethodInfo? shouldSerializeMethod = member.DeclaringType?.GetMethod(XmlSerializer.MethodShouldSerialize + member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (shouldSerializeMethod != null && shouldSerializeMethod.ReturnType == Reflector.BoolType && !(bool)Reflector.InvokeMethod(obj, shouldSerializeMethod)!)
                    return true;
            }

            // Skip 3.) DefaultValue equals to member value
            bool hasDefaultValue = false;
            object? defaultValue = null;
            if ((Options & XmlSerializationOptions.IgnoreDefaultValueAttribute) == XmlSerializationOptions.None)
            {
                attrs = Attribute.GetCustomAttributes(member, typeof(DefaultValueAttribute), true);
                hasDefaultValue = attrs.Length > 0;
                if (hasDefaultValue)
                    defaultValue = ((DefaultValueAttribute)attrs[0]).Value;
            }

            PropertyInfo? property = member as PropertyInfo;
            FieldInfo? field = property == null ? (FieldInfo)member : null;
            Type memberType = property != null ? property.PropertyType : field!.FieldType;
            if (!hasDefaultValue && (Options & XmlSerializationOptions.AutoGenerateDefaultValuesAsFallback) != XmlSerializationOptions.None)
            {
                hasDefaultValue = true;
                defaultValue = memberType.GetDefaultValue();
            }

            value = property != null
                ? property.Get(obj)
                : field!.Get(obj);
            if (hasDefaultValue && Equals(value, defaultValue))
                return true;

            // Skip 4.) The member returns the same reference as the object itself (eg. a self cast to an interface)
            return ReferenceEquals(value, obj);
        }

        /// <summary>
        /// Registers object to detect circular reference.
        /// Must be called from inside of try-finally to remove lock in finally if necessary.
        /// </summary>
        private protected void RegisterSerializedObject(object? obj)
        {
            if (obj == null || obj.GetType().IsValueType)
                return;

            if (SerObjects.Contains(obj))
                Throw.ReflectionException(Res.XmlSerializationCircularReference(obj));
            serObjects!.Add(obj);
        }

        private protected void UnregisterSerializedObject(object? obj)
        {
            if (obj == null || obj.GetType().IsValueType)
                return;
            serObjects!.Remove(obj);
        }

        private protected string GetTypeString(Type type)
        {
            #region Local Methods

            static AssemblyName GetAssemblyName(Type t)
            {
                string? legacyName = AssemblyResolver.GetForwardedAssemblyName(t, false);
                return legacyName != null ? new AssemblyName(legacyName) : t.Assembly.GetName();
            }

            #endregion

            if (!FullyQualifiedNames)
                return type.GetName(TypeNameKind.LongName);
            return IgnoreTypeForwardedFromAttribute
                ? type.GetName(TypeNameKind.AssemblyQualifiedName)
                : type.GetName(TypeNameKind.AssemblyQualifiedName, GetAssemblyName, null);
        }

        private protected string? GetStringValue(object? value, out bool spacePreserve, out bool escaped)
        {
            spacePreserve = false;
            escaped = false;
            if (value == null)
                return null;

            Type type = value.GetType();

            // these types never have to be escaped so we can return a result immediately
            if (type.CanBeParsedNatively() && !type.In(escapedNativelySupportedTypes))
                return value.ToStringInternal(CultureInfo.InvariantCulture);

            string? result = (value as string) ?? (value is Type t ? GetTypeString(t) : value.ToStringInternal(CultureInfo.InvariantCulture));
            if (String.IsNullOrEmpty(result))
                return result;

            bool escapeNewline = (Options & XmlSerializationOptions.EscapeNewlineCharacters) != XmlSerializationOptions.None;
            StringBuilder? escapedResult = null;
            spacePreserve = IsWhiteSpace(result![0], escapeNewline);

            // checking result for escaping
            for (int i = 0; i < result.Length; i++)
            {
                if (EscapeNeeded(result, i, escapeNewline, out bool isValidSurrogate))
                {
                    escapedResult ??= new StringBuilder(result.Substring(0, i).Replace(@"\", @"\\"));
                    escapedResult.Append(@"\" + ((ushort)result[i]).ToString("X4", CultureInfo.InvariantCulture));
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
                        i += 1;
                }
            }

            if (escapedResult != null)
            {
                escaped = true;
                return escapedResult.ToString();
            }

            return result;
        }

        #endregion

        #endregion
    }
}
