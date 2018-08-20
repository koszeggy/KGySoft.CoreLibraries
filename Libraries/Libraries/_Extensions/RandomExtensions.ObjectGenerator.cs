using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using KGySoft.Libraries.Collections;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Serialization;

namespace KGySoft.Libraries
{
    public static partial class RandomExtensions
    {
        private static class ObjectGenerator
        {
            private static readonly Type[] simpleTypes =
            {
                typeof(object),
                typeof(bool),
                typeof(byte),
                typeof(sbyte),
                typeof(char),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(float),
                typeof(double),
                typeof(decimal),
                typeof(string),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(TimeSpan),
                typeof(Guid),
            };

            private static readonly Cache<Assembly, Type[]> assemblyTypesCache = new Cache<Assembly, Type[]>(LoadAssemblyTypes);

            private static Type[] LoadAssemblyTypes(Assembly asm)
            {
                try
                {
                    return asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    return e.Types.Where(t => t != null).ToArray();
                }
            }

            private static readonly Cache<Type, Type[]> typeImplementorsCache = new Cache<Type, Type[]>(SearchForImplementors);

            private static Type[] SearchForImplementors(Type type)
            {
                var result = new List<Type>();
                bool isGenericInterface = type.IsInterface && type.IsGenericType;
                Type[] genericArguments = type.GetGenericArguments();
                foreach (var assembly in Reflector.GetLoadedAssemblies())
                {
                    foreach (Type t in GetAssemblyTypes(assembly))
                    {
                        // Skipping interfaces, abstract (and static) classes and delegates unless delegate types are searched
                        if (t.IsInterface || t.IsAbstract || (t.IsDelegate() && !type.IsDelegate()))
                            continue;

                        // 1.) Non-generic type
                        if (!t.IsGenericTypeDefinition)
                        {
                            if (type.IsAssignableFrom(t))
                                result.Add(t);
                            continue;
                        }

                        // Skipping if the requested type was non generic and is not compatible with current type (eg. EventArgs -> List<>)
                        // Explanation: for example, IList is assignable from List<> definition but IList<int> is not assignable from List<> but only from List<int>
                        if (!type.IsGenericType && !type.IsAssignableFrom(t))
                            continue;

                        // 2.) Generic type for generic interface (eg. IList<int> -> List<int>)
                        if (isGenericInterface)
                        {
                            try
                            {
                                if (t.GetGenericArguments().Length == genericArguments.Length && t.GetInterface(type.Name) != null)
                                    result.Add(t.MakeGenericType(genericArguments));
                            }
                            catch (AmbiguousMatchException) { }
                            catch (ArgumentException) { }
                            continue;
                        }

                        // Generic type for non-generic interface or for non-interface (eg. IList -> List<object> or BaseClass<MyType> -> DerivedClass<MyType>)
                        // Trying to resolve its constraints and see whether the construction is compatible with the provided type.
                        Type constructedType = TryMakeGenericType(t, genericArguments);
                        if (constructedType != null && type.IsAssignableFrom(constructedType))
                            result.Add(constructedType);
                    }
                }

                return result.ToArray();
            }

            private static Type TryMakeGenericType(Type typeDef, Type[] suggestedArguments)
            {
                lock (defaultConstructedGenerics)
                {
                    return defaultConstructedGenerics[new DefaultGenericTypeKey(typeDef, suggestedArguments)];
                }
            }

            private static readonly Cache<DefaultGenericTypeKey, Type> defaultConstructedGenerics = new Cache<DefaultGenericTypeKey, Type>(TryCreateDefaultGeneric);

            private struct DefaultGenericTypeKey : IEquatable<DefaultGenericTypeKey>
            {
                private readonly Type[] types;

                internal Type GenericType => types[0];
                internal IList<Type> SuggestedArguments => new ArraySegment<Type>(types, 1, types.Length - 1);

                public DefaultGenericTypeKey(Type genericType, Type[] suggestedArguments)
                {
                    types = new Type[1 + suggestedArguments.Length];
                    types[0] = genericType;
                    suggestedArguments.CopyTo(types, 1);
                }

                public override bool Equals(object obj) => obj is DefaultGenericTypeKey key && Equals(key);

                public bool Equals(DefaultGenericTypeKey other) => types.SequenceEqual(other.types);

                public override int GetHashCode() => types.Aggregate(615762546, (hc, t) => hc * -1521134295 + t.GetHashCode());
            }

            private static Type TryCreateDefaultGeneric(DefaultGenericTypeKey key)
            {
                Type genericTypeDef = key.GenericType;
                IList<Type> suggestedArguments = key.SuggestedArguments;
                Type[] genericParams = genericTypeDef.GetGenericArguments();
                Type[] argumentsToCreate = new Type[genericParams.Length];
                Type[][] constraints = new Type[genericParams.Length][];
                for (int i = 0; i < genericParams.Length; i++)
                {
                    Type argDef = genericParams[i];
                    GenericParameterAttributes attr = argDef.GenericParameterAttributes;
                    bool valueTypeConstraint = (attr & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
                    constraints[i] = argDef.GetGenericParameterConstraints();

                    Type arg = suggestedArguments.Count == argumentsToCreate.Length ? suggestedArguments[i] : null;

                    // If we could not get the argument from provided type or it is not compatible with first constraint we put either first constraint or int/object
                    if (arg == null || constraints.Length > 0 && !constraints[i][0].IsAssignableFrom(arg))
                        arg = constraints[i].Length >= 1 ? constraints[i][0] : valueTypeConstraint ? typeof(int) : Reflector.ObjectType;

                    // a last check for value type constraint...
                    if (valueTypeConstraint && !arg.IsValueType)
                        arg = typeof(int);
                    // ...for class constraint...
                    else if ((attr & GenericParameterAttributes.ReferenceTypeConstraint) != 0 && !arg.IsClass)
                        arg = Reflector.ObjectType;
                    // ...and for default ctor constraint
                    else if ((attr & GenericParameterAttributes.DefaultConstructorConstraint) != 0 && (arg.IsInterface || (arg.IsClass && arg.GetConstructor(Type.EmptyTypes) == null)))
                        arg = Reflector.ObjectType;

                    argumentsToCreate[i] = arg;
                }

                // Cleaning up arguments: due to substituting constraints arguments still can have generic argument definitions.
                // This is another cycle because now every arguments are substituted.
                for (int i = 0; i < argumentsToCreate.Length; i++)
                {
                    Type arg = argumentsToCreate[i];

                    // does not contain generic parameter
                    if (!arg.ContainsGenericParameters)
                    {
                        // asserting non-generic constraints failed: giving up
                        if (!constraints[i].All(c => c.ContainsGenericParameters || c.IsAssignableFrom(arg)))
                            return null;

                        continue;
                    }

                    // Still contains generic parameters: trying to resolve.
                    arg = SubstituteGenericParameter(arg, genericParams, argumentsToCreate);

                    // substitution failed: giving up
                    if (arg == null)
                        return null;

                    argumentsToCreate[i] = arg;
                }

                // the checks above cannot be perfect (especially if constraints contain generics) so creating the type in try-catch
                try
                {
                    return genericTypeDef.MakeGenericType(argumentsToCreate);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            private static Type SubstituteGenericParameter(Type arg, Type[] definitionArguments, Type[] constructedArguments)
            {
                // generic parameter: replacing
                if (arg.IsGenericParameter)
                {
                    int pos = definitionArguments.IndexOf(arg);

                    // if not found or contains generic parameters recursively, using a simple type based on the value type constraint
                    Type replacement = pos >= 0 && !constructedArguments[pos].ContainsGenericParameters
                        ? constructedArguments[pos]
                        : (arg.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 ? typeof(int) : Reflector.ObjectType;

                    // Generic parameters are never compatible with real types so skipping assertion for them
                    return arg.GetGenericParameterConstraints().All(c => c.ContainsGenericParameters || c.IsAssignableFrom(replacement)) ? replacement : null;
                }

                // contains generic parameters: recursion
                var args = arg.GetGenericArguments();
                var replacedArgs = new Type[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    var childArg = args[i];
                    Type replacement = childArg.ContainsGenericParameters ? SubstituteGenericParameter(childArg, definitionArguments, constructedArguments) : childArg;
                    if (replacement == null)
                        return null;
                    replacedArgs[i] = replacement;
                }

                try
                {
                    // This still can throw exception because we skipped constraints with generic parameters
                    return arg.GetGenericTypeDefinition().MakeGenericType(replacedArgs);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            private struct GeneratorContext
            {
                internal readonly Random Random;
                internal readonly GenerateObjectSettings Settings;

                private readonly Stack<string> memberNameStack;
                private readonly HashSet<Type> typesBeingGenerated;

                public string MemberName => memberNameStack.Count == 0 ? null : memberNameStack.Peek();

                public GeneratorContext(Random random, GenerateObjectSettings settings)
                {
                    Random = random;
                    Settings = settings;
                    memberNameStack = new Stack<string>();
                    typesBeingGenerated = new HashSet<Type>();
                }

                internal void PushMember(string memberName) => memberNameStack.Push(memberName);
                internal void PopMember() => memberNameStack.Pop();

                internal bool IsGenerating(Type type) => typesBeingGenerated.Contains(type);
                internal void PushType(Type type) => typesBeingGenerated.Add(type);
                internal void PopType(Type type) => typesBeingGenerated.Remove(type);
            }

            private delegate object GenerateKnownType(ref GeneratorContext context);

            private static readonly Dictionary<Type, GenerateKnownType> knownTypes =
                new Dictionary<Type, GenerateKnownType>
                {
                    // primitive types
                    { typeof(bool), GenerateBoolean },
                    { typeof(byte), GenerateByte },
                    { typeof(sbyte), GenerateSbyte },
                    { typeof(char), GenerateChar },
                    { typeof(short), GenerateInt16 },
                    { typeof(ushort), GenerateUInt16 },
                    { typeof(int), GenerateInt32 },
                    { typeof(uint), GenerateUInt32 },
                    { typeof(long), GenerateInt64 },
                    { typeof(ulong), GenerateUInt64 },

                    // floating points
                    { typeof(float), GenerateSingle },
                    { typeof(double), GenerateDouble },
                    { typeof(decimal), GenerateDecimal },

                    // strings
                    { typeof(string), GenerateString },
                    { typeof(StringBuilder), GenerateStringBuilder },
                    { typeof(Uri), GenerateUri },

                    // guid
                    { typeof(Guid), GenerateGuid },

                    // date and time
                    { typeof(DateTime), GenerateDateTime },
                    { typeof(DateTimeOffset), GenerateDateTimeOffset },
                    { typeof(TimeSpan), GenerateTimeSpan },

                    // reflection types - no generation but random selection
                    { typeof(Assembly), PickRandomAssembly },
                    { typeof(Type), PickRandomType },
                };

            private static object GenerateBoolean(ref GeneratorContext context) => context.Random.NextBoolean();
            private static object GenerateByte(ref GeneratorContext context) => context.Random.NextByte();
            private static object GenerateSbyte(ref GeneratorContext context) => context.Settings.AllowNegativeValues ? context.Random.NextSByte() : context.Random.NextSByte(SByte.MaxValue, true);
            private static object GenerateChar(ref GeneratorContext context) => context.Random.NextChar();
            private static object GenerateInt16(ref GeneratorContext context) => context.Settings.AllowNegativeValues ? context.Random.NextInt16() : context.Random.NextInt16(Int16.MaxValue, true);
            private static object GenerateUInt16(ref GeneratorContext context) => context.Random.NextUInt16();
            private static object GenerateInt32(ref GeneratorContext context) => context.Settings.AllowNegativeValues ? context.Random.NextInt32() : context.Random.NextInt32(Int32.MaxValue, true);
            private static object GenerateUInt32(ref GeneratorContext context) => context.Random.NextUInt32();
            private static object GenerateInt64(ref GeneratorContext context) => context.Settings.AllowNegativeValues ? context.Random.NextInt64() : context.Random.NextInt64(Int64.MaxValue, true);
            private static object GenerateUInt64(ref GeneratorContext context) => context.Random.NextUInt64();

            private static object GenerateSingle(ref GeneratorContext context)
                => context.Settings.AllowNegativeValues ? context.Random.NextSingle(Single.MinValue, Single.MaxValue, context.Settings.FloatScale) : context.Random.NextSingle(Single.MaxValue, context.Settings.FloatScale);

            private static object GenerateDouble(ref GeneratorContext context)
                => context.Settings.AllowNegativeValues ? context.Random.NextDouble(Double.MinValue, Double.MaxValue, context.Settings.FloatScale) : context.Random.NextDouble(Double.MaxValue, context.Settings.FloatScale);

            private static object GenerateDecimal(ref GeneratorContext context)
                => context.Settings.AllowNegativeValues ? context.Random.NextDecimal(Decimal.MinValue, Decimal.MaxValue, context.Settings.FloatScale) : context.Random.NextDecimal(Decimal.MaxValue, context.Settings.FloatScale);

            private static string GenerateString(ref GeneratorContext context)
            {
                Range<int> range = context.Settings.StringsLength;
                StringCreation strategy;
                string memberName = context.MemberName;
                if (memberName != null && context.Settings.StringCreation == null)
                {
                    if (memberName.ContainsAny(StringComparison.OrdinalIgnoreCase, "name", "address", "city", "country", "county", "author", "title"))
                        strategy = StringCreation.TitleCaseWord;
                    else if (memberName.ContainsAny(StringComparison.OrdinalIgnoreCase, "description", "info", "comment", "remark", "detail"))
                    {
                        strategy = StringCreation.Sentence;
                        range = context.Settings.SentencesLength;
                    }
                    else if (memberName.Contains("password", StringComparison.OrdinalIgnoreCase))
                        strategy = StringCreation.Ascii;
                    else if (memberName.Contains("code", StringComparison.OrdinalIgnoreCase))
                        strategy = StringCreation.UpperCaseLetters;
                    else if (memberName.ContainsAny(StringComparison.OrdinalIgnoreCase, "number", "phone") || memberName.EndsWith("id", StringComparison.OrdinalIgnoreCase) || memberName.Equals("pin", StringComparison.OrdinalIgnoreCase))
                        strategy = StringCreation.Digits;
                    else
                        strategy = StringCreation.LowerCaseWord;
                }
                else
                {
                    range = context.Settings.StringCreation == StringCreation.Sentence ? context.Settings.SentencesLength : context.Settings.StringsLength;
                    strategy = context.Settings.StringCreation ?? StringCreation.Ascii;
                }

                return context.Random.NextString(range.LowerBound, range.UpperBound, strategy);
            }

            private static StringBuilder GenerateStringBuilder(ref GeneratorContext context)
                => new StringBuilder(GenerateString(ref context));

            private static Uri GenerateUri(ref GeneratorContext context)
                => new Uri($"http://{context.Random.NextString(strategy: StringCreation.LowerCaseWord)}.{context.Random.NextString(3, 3, StringCreation.LowerCaseLetters)}");

            private static object GenerateGuid(ref GeneratorContext context)
                => context.Random.NextGuid();

            private static object GenerateDateTime(ref GeneratorContext context)
            {
                bool? pastOnly = context.Settings.PastDateTimes;
                string memberName = context.MemberName;
                if (pastOnly == null && memberName != null)
                {
                    if (memberName.Contains("birth", StringComparison.OrdinalIgnoreCase))
                        pastOnly = true;
                    else if (memberName.ContainsAny(StringComparison.OrdinalIgnoreCase, "expire", "expiration", "valid"))
                        pastOnly = false;
                }

                DateTime minDate = pastOnly != false
                    ? (context.Settings.CloseDateTimes ? DateTime.UtcNow.AddYears(-100) : DateTime.MinValue)
                    : DateTime.Today.AddDays(1);
                DateTime maxDate = pastOnly != true
                    ? (context.Settings.CloseDateTimes ? DateTime.UtcNow.AddYears(100) : DateTime.MaxValue)
                    : DateTime.Today.AddDays(-1);

                return memberName?.Contains("date", StringComparison.OrdinalIgnoreCase) == true && !memberName.Contains("time", StringComparison.OrdinalIgnoreCase)
                    ? NextDate(context.Random, minDate, maxDate)
                    : NextDateTime(context.Random, minDate, maxDate);
            }

            private static object GenerateDateTimeOffset(ref GeneratorContext context)
            {
                DateTimeOffset minDate = context.Settings.PastDateTimes != false
                    ? (context.Settings.CloseDateTimes ? DateTime.UtcNow.AddYears(-100) : DateTimeOffset.MinValue)
                    : DateTime.Today.AddDays(1);
                DateTimeOffset maxDate = context.Settings.PastDateTimes != true
                    ? (context.Settings.CloseDateTimes ? DateTime.UtcNow.AddYears(100) : DateTimeOffset.MaxValue)
                    : DateTime.Today.AddDays(-1);

                return NextDateTimeOffset(context.Random, minDate, maxDate);
            }

            private static object GenerateTimeSpan(ref GeneratorContext context)
            {
                TimeSpan min = context.Settings.AllowNegativeValues
                    ? (context.Settings.CloseDateTimes ? TimeSpan.FromDays(-100) : TimeSpan.MinValue)
                    : TimeSpan.Zero;
                TimeSpan max = context.Settings.CloseDateTimes ? TimeSpan.FromDays(100) : TimeSpan.MaxValue;

                return NextTimeSpan(context.Random, min, max);
            }

            private static Assembly PickRandomAssembly(ref GeneratorContext context)
            {
                Assembly[] assemblies = Reflector.GetLoadedAssemblies();
                return GetRandomElement(context.Random, assemblies);
            }

            private static Type PickRandomType(ref GeneratorContext context)
            {
                Assembly[] assemblies = Reflector.GetLoadedAssemblies();
                Assembly asm = GetRandomElement(context.Random, assemblies);
                Type[] types = GetAssemblyTypes(asm);

                if (types.Length == 0)
                {
                    foreach (Assembly candidate in context.Random.Shuffle(assemblies.Except(new[] { asm })))
                    {
                        types = GetAssemblyTypes(candidate);
                        if (types.Length != 0)
                            break;
                    }
                }

                return GetRandomElement(context.Random, types);
            }

            private static object PickRandomEnum(Type type, ref GeneratorContext context)
            {
                Array values = Enum.GetValues(type);
                return values.Length == 0 ? Enum.ToObject(type, 0) : values.GetValue(context.Random.Next(values.Length));
            }

            internal static object GenerateObject(Random random, Type type, GenerateObjectSettings settings)
            {
                var context = new GeneratorContext(random, settings);
                return GenerateObject(type, ref context);
            }

            private static object GenerateObject(Type type, ref GeneratorContext context)
            {
                // null
                if (context.Settings.ChanceOfNull > 0 && type.CanAcceptValue(null) && context.Random.NextDouble() > context.Settings.ChanceOfNull)
                    return null;

                // nullable
                if (type.IsNullable())
                    type = Nullable.GetUnderlyingType(type);

                // object substitution
                if (type == Reflector.ObjectType && context.Settings.SubstituteObjectWithSimpleTypes)
                    type = GetRandomElement(context.Random, simpleTypes);

                // ReSharper disable once AssignNullToNotNullAttribute
                // 1.) known type
                if (knownTypes.TryGetValue(type, out var knownGenerator))
                    return knownGenerator.Invoke(ref context);

                // 2.) enum
                // ReSharper disable once PossibleNullReferenceException
                if (type.IsEnum)
                    return PickRandomEnum(type, ref context);

                // 3.) array
                if (type.IsArray)
                    return GenerateArray(type, ref context);

                // 4.) supported collection
                if (type.IsSupportedCollectionForReflection(out var defaultCtor, out var collectionCtor, out var elementType, out bool isDictionary))
                {
                    IEnumerable collection = null;

                    // preferring default constructor and populating
                    // CreateInstance is faster than obtaining object factory by the constructor
                    if (defaultCtor != null || type.IsValueType)
                        collection = (IEnumerable)Activator.CreateInstance(type, true);

                    if (collection != null && type.IsReadWriteCollection(collection))
                    {
                        PopulateCollection(collection, elementType, isDictionary, ref context);
                        return collection;
                    }

                    // As a fallback using collectionCtor if possible
                    if (collectionCtor != null)
                        return GenerateCollectionByCtor(collectionCtor, elementType, isDictionary, ref context);
                }

                // 5.) key-value pair (because its properties are read-only)
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    var args = type.GetGenericArguments();
                    context.PushMember(nameof(KeyValuePair<_, _>.Key));
                    var key = GenerateObject(args[0], ref context);
                    context.PopMember();
                    context.PushMember(nameof(KeyValuePair<_, _>.Value));
                    var value = GenerateObject(args[1], ref context);
                    context.PopMember();
                    return Reflector.Construct(type, key, value);
                }

                // 6.) any object
                bool resolveType = false;
                if (type.IsAbstract || type.IsInterface)
                {
                    if (!context.Settings.TryResolveInterfacesAndAbstractTypes)
                        return null;
                    resolveType = true;
                }
                // SubstituteObjectWithSimpleTypes is checked because object can be generated, too, and if so, no derived types should be searched
                else if (!type.IsSealed && context.Settings.AllowDerivedTypesForNonSealedClasses
                    && !(context.Settings.SubstituteObjectWithSimpleTypes && type == Reflector.ObjectType))
                {
                    resolveType = true;
                }

                IList<Type> typeCandidates = null;
                Type typeToCreate = type;
                if (resolveType)
                {
                    typeCandidates = GetResolvedTypes(type);
                    typeToCreate = GetRandomElement(context.Random, typeCandidates, true);
                    if (typeToCreate == null)
                        return null;
                }

                object result = GenerateAnyObject(typeToCreate, ref context);
                if (result != null || !resolveType)
                    return result;

                // We check all of the compatible types in random order.
                // The try above could be in this foreach below but we try to avoid the shuffling if possible
                if (!type.IsSealed && (context.Settings.TryResolveInterfacesAndAbstractTypes || context.Settings.AllowNegativeValues))
                {
                    foreach (Type candidateType in context.Random.Shuffle(typeCandidates.Except(new[] { typeToCreate })))
                    {
                        result = GenerateAnyObject(candidateType, ref context);
                        if (result != null)
                            return result;
                    }
                }
                return null;
            }

            private static IList<Type> GetResolvedTypes(Type type)
            {
                lock (typeImplementorsCache)
                {
                    return typeImplementorsCache[type];
                }
            }

            private static Type[] GetAssemblyTypes(Assembly asm)
            {
                lock (assemblyTypesCache)
                {
                    return assemblyTypesCache[asm];
                }
            }

            private static Array GenerateArray(Type arrayType, ref GeneratorContext context)
            {
                if (arrayType == Reflector.ByteArrayType)
                    return NextBytes(context.Random, NextInt32(context.Random, context.Settings.CollectionsLength.LowerBound, context.Settings.CollectionsLength.UpperBound, true));

                Type elementType = arrayType.GetElementType();
                var lengths = new int[arrayType.GetArrayRank()];
                for (int i = 0; i < lengths.Length; i++)
                    lengths[i] = NextInt32(context.Random, context.Settings.CollectionsLength.LowerBound, context.Settings.CollectionsLength.UpperBound, true);

                // ReSharper disable once AssignNullToNotNullAttribute
                var result = Array.CreateInstance(elementType, lengths);
                var indexer = new ArrayIndexer(lengths);
                while (indexer.MoveNext())
                    result.SetValue(GenerateObject(context.Random, elementType, context.Settings), indexer.Current);

                return result;
            }

            private static void PopulateCollection(IEnumerable collection, Type elementType, bool isDictionary, ref GeneratorContext context)
            {
                int count = context.Random.NextInt32(context.Settings.CollectionsLength.LowerBound, context.Settings.CollectionsLength.UpperBound, true);
                
                // though this could populate dictionaries, too; duplicates and null keys are not checked
                if (!isDictionary)
                {
                    for (int i = 0; i < count; i++)
                        collection.Add(GenerateObject(elementType, ref context));

                    return;
                }

                Type[] keyValue = GetKeyValueTypes(elementType);
                IDictionary dictionary = collection as IDictionary;
                PropertyAccessor genericIndexer = dictionary != null ? null : PropertyAccessor.GetPropertyAccessor((PropertyInfo)typeof(IDictionary<,>).MakeGenericType(keyValue).GetDefaultMembers()[0]);

                for (int i = 0; i < count; i++)
                {
                    context.PushMember(nameof(DictionaryEntry.Key));
                    var key = GenerateObject(keyValue[0], ref context);
                    context.PopMember();
                    if (key == null)
                        continue;

                    context.PushMember(nameof(DictionaryEntry.Value));
                    var value = GenerateObject(keyValue[1], ref context);
                    context.PopMember();
                    if (dictionary != null)
                    {
                        dictionary[key] = value;
                        continue;
                    }

                    genericIndexer.Set(collection, value, key);
                }
            }

            private static object GenerateCollectionByCtor(ConstructorInfo collectionCtor, Type elementType, bool isDictionary, ref GeneratorContext context)
            {
                IEnumerable initializerCollection;
                if (isDictionary)
                {
                    Type[] args = GetKeyValueTypes(elementType);
                    initializerCollection = (IEnumerable)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(args[0], args[1]));
                    PopulateCollection(initializerCollection, elementType, true, ref context);
                }
                else if (collectionCtor.GetParameters()[0].ParameterType.IsAssignableFrom(elementType.MakeArrayType()))
                {
                    initializerCollection = GenerateArray(elementType.MakeArrayType(), ref context);
                }
                else // for non-dictionaries array or list must be accepted by constructor
                {
                    initializerCollection = (IEnumerable)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                    PopulateCollection(initializerCollection, elementType, false, ref context);
                }

                return ObjectFactory.GetObjectFactory(collectionCtor).Create(initializerCollection);
            }

            private static object GenerateAnyObject(Type type, ref GeneratorContext context)
            {
                if (context.IsGenerating(type))
                    return null;

                context.PushType(type);
                try
                {
                    object result = null;
                    if (type.IsValueType || type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) != null)
                    {
                        try
                        {
                            result = Activator.CreateInstance(type, true);
                        }
                        catch
                        {
                            // the constructor threw an exception: skip
                            return null;
                        }
                    }
                    else if (context.Settings.AllowCreateObjectWithoutConstructor)
                        result = FormatterServices.GetUninitializedObject(type);

                    if (result != null)
                        InitializeObject(result, ref context);

                    return result;
                }
                finally
                {
                    context.PopType(type);
                }
            }

            private static void InitializeObject(object obj, ref GeneratorContext context)
            {
                IList<PropertyInfo> properties = null;
                IList<FieldInfo> fields = null;
                Type type = obj.GetType();
                switch (context.Settings.ObjectInitialization)
                {
                    case ObjectInitialization.PublicFieldsAndPropeties:
                        fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                        goto case ObjectInitialization.PublicProperties;
                    case ObjectInitialization.PublicProperties:
                        properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite).ToArray();
                        break;
                    case ObjectInitialization.Fields:
                        var result = new List<FieldInfo>();
                        for (Type t = type; t != null; t = t.BaseType)
                            result.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
                        fields = result;
                        break;
                }

                if (properties != null)
                {
                    foreach (PropertyInfo property in properties)
                    {
                        if (property.GetIndexParameters().Length > 0)
                            continue;

                        context.PushMember(property.Name);
                        try
                        {
                            PropertyAccessor.GetPropertyAccessor(property).Set(obj, GenerateObject(property.PropertyType, ref context));
                        }
                        // ReSharper disable once EmptyGeneralCatchClause - we just skip the property if it cannot be set
                        catch
                        {
                        }

                        context.PopMember();
                    }
                }

                if (fields != null)
                {
                    foreach (FieldInfo field in fields)
                    {
                        context.PushMember(field.Name);
                        try
                        {
                            FieldAccessor.GetFieldAccessor(field).Set(obj, GenerateObject(field.FieldType, ref context));
                        }
                        // ReSharper disable once EmptyGeneralCatchClause - we just skip the field if it cannot be set
                        catch
                        {
                        }

                        context.PopMember();
                    }
                }
            }

            private static Type[] GetKeyValueTypes(Type elementType)
                => elementType.IsGenericType ? elementType.GetGenericArguments() : new[] { typeof(object), typeof(object) };
        }
    }
}
