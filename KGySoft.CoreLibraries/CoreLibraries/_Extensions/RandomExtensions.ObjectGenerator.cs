#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomExtensions.ObjectGenerator.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Text;
using System.Threading;

using KGySoft.Collections;
using KGySoft.Reflection;
using KGySoft.Serialization;

#endregion

namespace KGySoft.CoreLibraries
{
    public static partial class RandomExtensions
    {
        #region ObjectGenerator class

        private static class ObjectGenerator
        {
            #region Nested types

            #region Delegates

            private delegate object? GenerateKnownType(ref GeneratorContext context);

            #endregion

            #region Nested structs

            #region DefaultGenericTypeKey struct

            /// <summary>
            /// Cache key for a generic type with its suggested arguments, with Equals/GetHashCode by array elements.
            /// </summary>
            private readonly struct DefaultGenericTypeKey : IEquatable<DefaultGenericTypeKey>
            {
                #region Fields

                private readonly Type[] types;

                #endregion

                #region Properties

                internal Type GenericType => types[0];
                internal ArraySection<Type> SuggestedArguments => types.AsSection(1, types.Length - 1);

                #endregion

                #region Constructors

                public DefaultGenericTypeKey(Type genericType, Type[] suggestedArguments)
                {
                    types = new Type[1 + suggestedArguments.Length];
                    types[0] = genericType;
                    suggestedArguments.CopyTo(types, 1);
                }

                #endregion

                #region Methods

                public override bool Equals(object? obj) => obj is DefaultGenericTypeKey key && Equals(key);
                public bool Equals(DefaultGenericTypeKey other) => types.SequenceEqual(other.types);
                public override int GetHashCode() => types.Aggregate(615762546, (hc, t) => hc * -1521134295 + t.GetHashCode());
                public override string ToString() => $"{GenericType.Name}[{SuggestedArguments.Join(", ")}]";

                #endregion
            }

            #endregion

            #region GeneratorContext struct

            private struct GeneratorContext
            {
                #region Fields

                #region Internal Fields

                internal readonly Random Random;
                internal readonly GenerateObjectSettings Settings;

                #endregion

                #region Private Fields

                private readonly Type requestedType;
                private readonly List<string> membersChain;
                private readonly List<Type> generatedTypes;

                private Type? root;
                private int recursionLevel;

                #endregion

                #endregion

                #region Properties

                public string? ParentMemberName => membersChain.Count == 0 ? null : membersChain.Last();

                #endregion

                #region Constructors

                public GeneratorContext(Type type, Random random, GenerateObjectSettings settings)
                {
                    requestedType = type;
                    Random = random;
                    Settings = settings;
                    membersChain = new List<string>();
                    generatedTypes = new List<Type>();
                    recursionLevel = 0;
                    root = null;
                }

                #endregion

                #region Methods

                #region Public Methods

                public override string ToString() => root == null ? requestedType.ToString() : $"{requestedType} [{root}->{String.Join("->", membersChain.ToArray())}]";

                #endregion

                #region Internal Methods

                internal bool TrySetRoot(Type type)
                {
                    // if the real root is a collection, then it can be set multiple times
                    if (root != null)
                        return false;
                    root = type;
                    return true;
                }

                internal void ClearRoot() => root = null;
                internal void PushMember(string memberName) => membersChain.Add(memberName);
                internal void PopMember() => membersChain.RemoveAt(membersChain.Count - 1);

                internal bool TryPushType(Type type)
                {
                    if (generatedTypes.Any(t => t.IsAssignableFrom(type)))
                    {
                        if (recursionLevel >= Settings.MaxRecursionLevel)
                            return false;
                        recursionLevel += 1;
                    }

                    generatedTypes.Add(type);
                    return true;
                }

                internal void PopType()
                {
                    Type type = generatedTypes.Last();
                    generatedTypes.RemoveAt(generatedTypes.Count - 1);
                    if (generatedTypes.Any(t => t.IsAssignableFrom(type)))
                        recursionLevel -= 1;
                }

                internal int GetNextCollectionLength() => Random.NextInt32(
                    Settings.CollectionsLength.LowerBound, Settings.CollectionsLength.UpperBound, true);

                #endregion

                #endregion
            }

            #endregion

            #endregion

            #endregion

            #region Fields

#if !NETSTANDARD2_0
            /// <summary>
            /// Must be a separate instance because dynamic method references will never freed.
            /// Other problem if the original Random is a disposable secure random: invoking the delegate would throw an exception.
            /// </summary>
            private static readonly Random randomForDelegates = new FastRandom();

            private static readonly FieldInfo randomField = (FieldInfo)Reflector.MemberOf(() => randomForDelegates); 
            private static readonly MethodInfo nextObjectGenMethod = typeof(RandomExtensions).GetMethod(nameof(NextObject), new[] { typeof(Random), typeof(GenerateObjectSettings) })!;
#endif

            private static readonly Dictionary<Type, GenerateKnownType> knownTypes =
                new Dictionary<Type, GenerateKnownType>
                {
                    // primitive types
                    { Reflector.BoolType, GenerateBoolean },
                    { Reflector.ByteType, GenerateByte },
                    { Reflector.SByteType, GenerateSbyte },
                    { Reflector.CharType, GenerateChar },
                    { Reflector.ShortType, GenerateInt16 },
                    { Reflector.UShortType, GenerateUInt16 },
                    { Reflector.IntType, GenerateInt32 },
                    { Reflector.UIntType, GenerateUInt32 },
                    { Reflector.LongType, GenerateInt64 },
                    { Reflector.ULongType, GenerateUInt64 },
                    { Reflector.IntPtrType, GenerateIntPtr },
                    { Reflector.UIntPtrType, GenerateUIntPtr },

                    // floating point types
                    { Reflector.FloatType, GenerateSingle },
                    { Reflector.DoubleType, GenerateDouble },
                    { Reflector.DecimalType, GenerateDecimal },

                    // strings
                    { Reflector.StringType, GenerateString },
                    { typeof(StringBuilder), GenerateStringBuilder },
                    { typeof(Uri), GenerateUri },

                    // guid
                    { Reflector.GuidType, GenerateGuid },

                    // date and time
                    { Reflector.DateTimeType, GenerateDateTime },
                    { Reflector.DateTimeOffsetType, GenerateDateTimeOffset },
                    { Reflector.TimeSpanType, GenerateTimeSpan },

                    // reflection types - no generation but random selection
                    { typeof(Assembly), PickRandomAssembly },
                    { Reflector.Type, PickRandomType },

                    // platform-dependent types
#if !NET35
                    { Reflector.BigIntegerType, GenerateBigInteger },
#endif
#if NETCOREAPP3_0_OR_GREATER
                    { Reflector.RuneType, GenerateRune },
#endif
#if NET5_0_OR_GREATER
                    { Reflector.HalfType, GenerateHalf },
#endif
#if NET6_0_OR_GREATER
                    { Reflector.DateOnlyType, GenerateDateOnly },
                    { Reflector.TimeOnlyType, GenerateTimeOnly },
#endif
                };

            private static readonly Type memberInfoType = typeof(MemberInfo);
            private static readonly Type fieldInfoType = typeof(FieldInfo);
            private static readonly Type rtFieldInfoType = Reflector.MemberOf(() => String.Empty).GetType();
            private static readonly Type mdFieldInfoType = Reflector.IntType.GetField(nameof(Int32.MaxValue))!.GetType(); // MemberOf does not work for constants
            private static readonly Type runtimeFieldInfoType = rtFieldInfoType.BaseType!;
            private static readonly Type propertyInfoType = typeof(PropertyInfo);
            private static readonly Type runtimePropertyInfoType = Reflector.MemberOf(() => ((string)null!).Length).GetType();
            private static readonly Type methodBaseType = typeof(MethodBase);
            private static readonly Type methodInfoType = typeof(MethodInfo);
            private static readonly Type runtimeMethodInfoType = Reflector.MemberOf(() => ((object)null!).ToString()).GetType();
            private static readonly Type ctorInfoType = typeof(ConstructorInfo);
            private static readonly Type runtimeCtorInfoType = Reflector.MemberOf(() => new object()).GetType();
            private static readonly Type eventInfoType = typeof(EventInfo);
            private static readonly Type runtimeEventInfoType = typeof(Console).GetEvent(nameof(Console.CancelKeyPress))!.GetType();

            private static IThreadSafeCacheAccessor<Assembly, Type[]>? assemblyTypesCache;
            private static IThreadSafeCacheAccessor<Type, Type[]>? typeImplementorsCache;
            private static IThreadSafeCacheAccessor<DefaultGenericTypeKey, Type?>? defaultConstructedGenerics;
            private static IThreadSafeCacheAccessor<Type, Delegate?>? delegatesCache;

            #endregion

            #region Properties

            private static IThreadSafeCacheAccessor<Assembly, Type[]> AssemblyTypesCache
            {
                get
                {
                    if (assemblyTypesCache == null)
                        Interlocked.CompareExchange(ref assemblyTypesCache, ThreadSafeCacheFactory.Create<Assembly, Type[]>(LoadAssemblyTypes, LockFreeCacheOptions.Profile128), null);
                    return assemblyTypesCache;
                }
            }

            private static IThreadSafeCacheAccessor<Type, Type[]> TypeImplementorsCache
            {
                get
                {
                    if (typeImplementorsCache == null)
                        Interlocked.CompareExchange(ref typeImplementorsCache, ThreadSafeCacheFactory.Create<Type, Type[]>(SearchForImplementors, LockFreeCacheOptions.Profile128), null);
                    return typeImplementorsCache;
                }
            }

            private static IThreadSafeCacheAccessor<DefaultGenericTypeKey, Type?> DefaultConstructedGenerics
            {
                get
                {
                    if (defaultConstructedGenerics == null)
                        Interlocked.CompareExchange(ref defaultConstructedGenerics, ThreadSafeCacheFactory.Create<DefaultGenericTypeKey, Type?>(TryCreateDefaultGeneric, LockFreeCacheOptions.Profile128), null);
                    return defaultConstructedGenerics;
                }
            }

            private static IThreadSafeCacheAccessor<Type, Delegate?> DelegatesCache
            {
                get
                {
                    if (delegatesCache == null)
                        Interlocked.CompareExchange(ref delegatesCache, ThreadSafeCacheFactory.Create<Type, Delegate?>(CreateDelegate, LockFreeCacheOptions.Profile128), null);
                    return delegatesCache;
                }
            }

            #endregion

            #region Methods

            #region Internal Methods

            [SecurityCritical]
            internal static object? GenerateObject(Random random, Type type, GenerateObjectSettings settings)
            {
                var context = new GeneratorContext(type, random, settings);
                return TryGenerateObject(type, ref context, out object? result) ? result : null;
            }

            #endregion

            #region Private Methods

            private static Type[] LoadAssemblyTypes(Assembly asm)
            {
                try
                {
                    return asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    return e.Types.Where(t => t != null).ToArray()!;
                }
            }

            private static Type[] SearchForImplementors(Type type)
            {
                var result = new List<Type>();
                bool isGenericInterface = type.IsInterface && type.IsGenericType;
                Type[] genericArguments = type.GetGenericArguments();
                foreach (var assembly in Reflector.GetLoadedAssemblies())
                {
                    foreach (Type t in AssemblyTypesCache[assembly])
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
                                    result.Add(t.GetGenericType(genericArguments));
                            }
                            catch (AmbiguousMatchException) { }
                            catch (ArgumentException) { }
                            continue;
                        }

                        // Generic type for non-generic interface or for non-interface (eg. IList -> List<object> or BaseClass<MyType> -> DerivedClass<MyType>)
                        // Trying to resolve its constraints and see whether the construction is compatible with the provided type.
                        Type? constructedType = DefaultConstructedGenerics[new DefaultGenericTypeKey(t, genericArguments)];
                        if (constructedType != null && type.IsAssignableFrom(constructedType))
                            result.Add(constructedType);
                    }
                }

                return result.ToArray();
            }

            private static Type? TryCreateDefaultGeneric(DefaultGenericTypeKey key)
            {
                Type genericTypeDef = key.GenericType;
                ArraySection<Type> suggestedArguments = key.SuggestedArguments;
                Type[] genericParams = genericTypeDef.GetGenericArguments();
                Type[] argumentsToCreate = new Type[genericParams.Length];
                Type[][] constraints = new Type[genericParams.Length][];
                for (int i = 0; i < genericParams.Length; i++)
                {
                    Type argDef = genericParams[i];
                    GenericParameterAttributes attr = argDef.GenericParameterAttributes;
                    bool valueTypeConstraint = (attr & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
                    constraints[i] = argDef.GetGenericParameterConstraints();
                    Type? arg = suggestedArguments.Length == argumentsToCreate.Length ? suggestedArguments[i] : null;

                    // If we could not get the argument from provided type or it is not compatible with first constraint we put either first constraint or int/object
                    if (arg == null || constraints[i].Length > 0 && !constraints[i][0].IsAssignableFrom(arg))
                        arg = constraints[i].Length >= 1 ? constraints[i][0] : valueTypeConstraint ? Reflector.IntType : Reflector.ObjectType;

                    // a last check for value type constraint...
                    if (valueTypeConstraint && !arg.IsValueType)
                        arg = Reflector.IntType;
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
                    Type? arg = argumentsToCreate[i];

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
                    return genericTypeDef.GetGenericType(argumentsToCreate);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            private static Type? SubstituteGenericParameter(Type arg, Type[] definitionArguments, Type[] constructedArguments)
            {
                // generic parameter: replacing
                if (arg.IsGenericParameter)
                {
                    int pos = definitionArguments.IndexOf(arg);

                    // if not found or contains generic parameters recursively, using a simple type based on the value type constraint
                    Type replacement = pos >= 0 && !constructedArguments[pos].ContainsGenericParameters
                            ? constructedArguments[pos]
                            : (arg.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 ? Reflector.IntType : Reflector.ObjectType;

                    // Generic parameters are never compatible with real types so skipping assertion for them
                    return arg.GetGenericParameterConstraints().All(c => c.ContainsGenericParameters || c.IsAssignableFrom(replacement)) ? replacement : null;
                }

                // contains generic parameters: recursion
                Type[] args = arg.GetGenericArguments();
                var replacedArgs = new Type[args.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    Type childArg = args[i];
                    Type? replacement = childArg.ContainsGenericParameters ? SubstituteGenericParameter(childArg, definitionArguments, constructedArguments) : childArg;
                    if (replacement == null)
                        return null;
                    replacedArgs[i] = replacement;
                }

                try
                {
                    // This still can throw exception because we skipped constraints with generic parameters
                    return arg.GetGenericTypeDefinition().GetGenericType(replacedArgs);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            private static Delegate? CreateDelegate(Type type)
            {
#if NETSTANDARD2_0
                return null;
#else
                MethodInfo mi = type.GetMethod(nameof(Action.Invoke))!;
                Type returnType = mi.ReturnType;
                ParameterInfo[] parameters = mi.GetParameters();
                var dm = new DynamicMethod($"dm_{type.Name}", returnType, mi.GetParameters().Select(p => p.ParameterType).ToArray(), true);
                ILGenerator il = dm.GetILGenerator();

                // calling NextObject<T> for out parameters
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (!parameters[i].IsOut)
                        continue;

                    Type parameterType = parameters[i].ParameterType.GetElementType()!;

                    il.Emit(OpCodes.Ldarg, i);
                    il.Emit(OpCodes.Ldsfld, randomField);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Call, nextObjectGenMethod.GetGenericMethod(parameterType));

                    if (parameterType.IsValueType)
                        il.Emit(OpCodes.Stobj, parameterType);
                    else
                        il.Emit(OpCodes.Stind_Ref);
                }

                // calling NextObject<T> for return value
                if (mi.ReturnType != Reflector.VoidType)
                {
                    il.Emit(OpCodes.Ldsfld, randomField);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Call, nextObjectGenMethod.GetGenericMethod(returnType));
                }

                il.Emit(OpCodes.Ret);

                try
                {
                    // may throw exceptions from partially trusted domain
                    return dm.CreateDelegate(type);
                }
                catch (Exception e) when (!e.IsCritical())
                {
                    return null;
                }
#endif
            }

            private static object GenerateBoolean(ref GeneratorContext context) => context.Random.NextBoolean();
            private static object GenerateByte(ref GeneratorContext context) => context.Random.SampleByte();
            private static object GenerateSbyte(ref GeneratorContext context) => context.Settings.AllowNegativeValues ? context.Random.SampleSByte() : context.Random.NextSByte(SByte.MaxValue, true);
            private static object GenerateChar(ref GeneratorContext context) => context.Random.NextChar();
            private static object GenerateInt16(ref GeneratorContext context) => context.Settings.AllowNegativeValues ? context.Random.SampleInt16() : context.Random.NextInt16(Int16.MaxValue, true);
            private static object GenerateUInt16(ref GeneratorContext context) => context.Random.SampleUInt16();
            private static object GenerateInt32(ref GeneratorContext context) => context.Settings.AllowNegativeValues ? context.Random.SampleInt32() : context.Random.NextInt32(Int32.MaxValue, true);
            private static object GenerateUInt32(ref GeneratorContext context) => context.Random.SampleUInt32();
            private static object GenerateInt64(ref GeneratorContext context) => context.Settings.AllowNegativeValues ? context.Random.SampleInt64() : context.Random.NextInt64(Int64.MaxValue, true);
            private static object GenerateUInt64(ref GeneratorContext context) => context.Random.SampleUInt64();
            private static object GenerateIntPtr(ref GeneratorContext context) => (IntPtr)(IntPtr.Size == 4 ? (int)GenerateInt32(ref context) : (long)GenerateInt64(ref context));
            private static object GenerateUIntPtr(ref GeneratorContext context) => (UIntPtr)(UIntPtr.Size == 4 ? (uint)GenerateUInt32(ref context) : (ulong)GenerateUInt64(ref context));

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
                string? memberName = context.ParentMemberName;
                if (memberName != null && context.Settings.StringCreation == null)
                {
                    if (memberName.ContainsAny(StringComparison.OrdinalIgnoreCase, "name", "address", "street", "city", "country", "county", "author", "title"))
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
                string? memberName = context.ParentMemberName;
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
                return assemblies.GetRandomElement(context.Random)!;
            }

            private static Type PickRandomType(ref GeneratorContext context)
            {
                Assembly[] assemblies = Reflector.GetLoadedAssemblies();
                Assembly asm = assemblies.GetRandomElement(context.Random)!;
                Type[] types = AssemblyTypesCache[asm];

                if (types.Length == 0)
                {
                    foreach (Assembly candidate in assemblies.Except(new[] { asm }).Shuffle(context.Random))
                    {
                        types = AssemblyTypesCache[candidate];
                        if (types.Length != 0)
                            break;
                    }
                }

                return types.GetRandomElement(context.Random)!;
            }

            private static MemberInfo? PickRandomMemberInfo(Type type, ref GeneratorContext context)
            {
                // type
                if (type.In(Reflector.Type, Reflector.RuntimeType
#if !NET35 && !NET40
                        , Reflector.TypeInfo
#endif
                ))
                {
                    return PickRandomType(ref context);
                }

                MemberTypes memberTypes;
                bool? constants = null;
                if (type.In(fieldInfoType, runtimeFieldInfoType, mdFieldInfoType, rtFieldInfoType))
                {
                    memberTypes = MemberTypes.Field;
                    constants = type == mdFieldInfoType ? true : type == rtFieldInfoType ? false : (bool?)null;
                }
                else if (type.In(propertyInfoType, runtimePropertyInfoType))
                    memberTypes = MemberTypes.Property;
                else if (type.In(methodInfoType, runtimeMethodInfoType))
                    memberTypes = MemberTypes.Method;
                else if (type.In(methodInfoType, runtimeMethodInfoType))
                    memberTypes = MemberTypes.Method;
                else if (type.In(ctorInfoType, runtimeCtorInfoType))
                    memberTypes = MemberTypes.Constructor;
                else if (type.In(ctorInfoType, runtimeCtorInfoType))
                    memberTypes = MemberTypes.Constructor;
                else if (type.In(eventInfoType, runtimeEventInfoType))
                    memberTypes = MemberTypes.Event;
                else if (type == methodBaseType)
                    memberTypes = MemberTypes.Constructor | MemberTypes.Method;
                else if (type == memberInfoType)
                    memberTypes = MemberTypes.All;
                else
                    // others (such as builders, etc): returning null to falling back default object creation
                    return null;

                Assembly[] assemblies = Reflector.GetLoadedAssemblies();
                Assembly asm = assemblies.GetRandomElement(context.Random)!;
                MemberInfo? result = TryPickMemberInfo(AssemblyTypesCache[asm].GetRandomElement(context.Random, true), memberTypes, ref context, constants);

                if (result != null)
                    return result;

                // low performance fallback: shuffling
                foreach (Assembly assembly in assemblies.Except(new[] { asm }).Shuffle(context.Random))
                {
                    foreach (Type t in AssemblyTypesCache[assembly].Shuffle(context.Random))
                    {
                        result = TryPickMemberInfo(t, memberTypes, ref context, constants);
                        if (result != null)
                            return result;

                    }
                }

                return null;
            }

            private static MemberInfo? TryPickMemberInfo(Type? type, MemberTypes memberTypes, ref GeneratorContext context, bool? constants)
            {
                if (type == null)
                    return null;

                MemberInfo[] members;
                try
                {
                    members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                }
                catch (FileNotFoundException)
                {
                    // a dependent assembly cannot be loaded
                    return null;
                }

                // If MemberInfo is requested, the Type itself is ok, too
                if (members.Length == 0 && memberTypes == MemberTypes.All)
                    return type;

                if (memberTypes == MemberTypes.All)
                    return members.GetRandomElement(context.Random);

                foreach (MemberInfo member in members.Where(m => (m.MemberType & memberTypes) != 0).Shuffle(context.Random))
                {
                    if (constants == null)
                        return member;

                    if (member is not FieldInfo field)
                        continue;

                    if (constants == true && field.IsLiteral || constants == false && !field.IsLiteral)
                        return member;
                }

                return null;
            }

            private static object PickRandomEnum(Type type, ref GeneratorContext context)
            {
                Array values = Enum.GetValues(type);
                return values.Length == 0 ? Enum.ToObject(type, 0) : values.GetValue(context.Random.Next(values.Length))!;
            }

            [SecurityCritical]
            private static bool TryGenerateObject(Type type, ref GeneratorContext context, out object? result, bool checkingDerivedType = false)
            {
                result = null;

                // null (if this call is for an implementation of a parent type, then it was already checked previously)
                if (!checkingDerivedType && context.Settings.ChanceOfNull > 0f && type.CanAcceptValue(null) && context.Random.NextDouble() < context.Settings.ChanceOfNull)
                    return true;

                if (!checkingDerivedType)
                {
                    if (!context.TryPushType(type))
                        return type.CanAcceptValue(null);
                }

                try
                {
                    type = Nullable.GetUnderlyingType(type) ?? type;

                    // object substitution
                    if (type == Reflector.ObjectType && context.Settings.SubstitutionForObjectType != null)
                        type = context.Settings.SubstitutionForObjectType;

                    // 1.) known type
                    if (knownTypes.TryGetValue(type, out GenerateKnownType? knownGenerator))
                    {
                        result = knownGenerator.Invoke(ref context);
                        return true;
                    }

                    // 2.) enum
                    if (type.IsEnum)
                    {
                        result = PickRandomEnum(type, ref context);
                        return true;
                    }

                    // 3.) collections
                    if (TryGenerateCollection(type, ref context, out result))
                        return true;

                    // 4.) key-value pair (because its properties are read-only)
                    if (type.IsGenericTypeOf(Reflector.KeyValuePairType))
                    {
                        result = GenerateKeyValuePair(type, ref context);
                        return true;
                    }

                    // 5.) Delegate
                    if (!type.IsAbstract && type.IsDelegate())
                    {
                        result = DelegatesCache[type];
                        return true;
                    }

                    // 6.) Reflection members (Assembly and Type are already handled as known types but RuntimeType is handled here)
                    if (memberInfoType.IsAssignableFrom(type) && (result = PickRandomMemberInfo(type, ref context)) != null)
                        return true;

                    // 7.) any object
                    if (TryGenerateCustomObject(type, ref context, out result))
                        return true;

                    // 8.) null if allowed
                    return !checkingDerivedType && type.CanAcceptValue(null);
                }
                catch (Exception e) when (!e.IsCritical())
                {
                    return !checkingDerivedType && type.CanAcceptValue(null);
                }
                finally
                {
                    if (!checkingDerivedType)
                        context.PopType();
                }
            }

            [SecurityCritical]
            private static object GenerateKeyValuePair(Type type, ref GeneratorContext context)
            {
                Type[] args = type.GetGenericArguments();
                object? key, value;
                object result = Activator.CreateInstance(type)!;

                // if key or value cannot be created just returning a default instance (by Activator, which is fast for value types)
                context.PushMember(nameof(KeyValuePair<_, _>.Key));
                try
                {
                    if (!TryGenerateObject(args[0], ref context, out key))
                        return result;
                }
                finally
                {
                    context.PopMember();
                }

                context.PushMember(nameof(KeyValuePair<_, _>.Value));
                try
                {
                    if (!TryGenerateObject(args[1], ref context, out value))
                        return result;
                }
                finally
                {
                    context.PopMember();
                }

                Accessors.SetKeyValue(result, key, value);
                return result;
            }

            [SecurityCritical]
            private static bool TryGenerateCollection(Type type, ref GeneratorContext context, out object? result)
            {
                // array
                if (type.IsArray)
                {
                    result = GenerateArray(type, ref context);
                    return true;
                }

                result = null;
                if (!type.IsSupportedCollectionForReflection(out ConstructorInfo? defaultCtor, out ConstructorInfo? collectionCtor, out Type? elementType, out bool isDictionary)
                    && (!context.Settings.AllowCreateObjectWithoutConstructor || !type.IsCollection()))
                {
                    return false;
                }

                // supported collection
                IEnumerable? collection = null;

                // preferring default constructor and populating
                // CreateInstance is faster than obtaining object factory by the constructor
                if (defaultCtor != null || type.IsValueType)
                    collection = (IEnumerable)CreateInstanceAccessor.GetAccessor(type).CreateInstance();
                else if (collectionCtor == null && context.Settings.AllowCreateObjectWithoutConstructor && Reflector.TryCreateUninitializedObject(type, out object? uninitialized))
                    collection = (IEnumerable)uninitialized;

                if (collection != null && type.IsReadWriteCollection(collection))
                {
                    PopulateCollection(collection, elementType!, isDictionary, ref context);
                    result = collection;
                    return true;
                }

                // As a fallback using collectionCtor if possible
                if (collectionCtor != null)
                {
                    result = GenerateCollectionByCtor(collectionCtor, elementType!, isDictionary, ref context);
                    return true;
                }

                return false;
            }

            [SecurityCritical]
            private static bool TryGenerateCustomObject(Type type, ref GeneratorContext context, out object? result)
            {
                result = null;
                bool resolveType = false;
                if (type.IsAbstract || type.IsInterface)
                {
                    if (!context.Settings.TryResolveInterfacesAndAbstractTypes)
                        return false;
                    resolveType = true;
                }
                else if (!type.IsSealed && context.Settings.AllowDerivedTypesForNonSealedClasses)
                    resolveType = true;

                IList<Type>? typeCandidates = null;
                Type? typeToCreate = type;
                if (resolveType)
                {
                    typeCandidates = TypeImplementorsCache[type];
                    typeToCreate = typeCandidates.GetRandomElement(context.Random, true);
                    if (typeToCreate == null)
                        return false;
                }

                if (typeToCreate == type && TryCreateConcreteObject(typeToCreate, ref context, out result)
                    || typeToCreate != type && TryGenerateObject(typeToCreate, ref context, out result, true))
                {
                    return true;
                }

                if (!resolveType)
                    return false;

                // We check all of the compatible types in random order.
                // The try above could be in this foreach below but we try to avoid the shuffling if possible
                if (!type.IsSealed && (context.Settings.TryResolveInterfacesAndAbstractTypes || context.Settings.AllowDerivedTypesForNonSealedClasses))
                {
                    foreach (Type candidateType in typeCandidates!.Except(new[] { typeToCreate }).Shuffle(context.Random))
                    {
                        if (candidateType == type && TryCreateConcreteObject(candidateType, ref context, out result)
                            || candidateType != type && TryGenerateObject(candidateType, ref context, out result, true))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            [SecurityCritical]
            private static bool TryCreateConcreteObject(Type type, ref GeneratorContext context, [MaybeNullWhen(false)]out object result)
            {
                bool isRoot = context.TrySetRoot(type);
                try
                {
                    if (!Reflector.TryCreateEmptyObject(type, true, context.Settings.AllowCreateObjectWithoutConstructor, out result))
                        return false;

                    InitializeMembers(result, ref context);
                    return true;
                }
                finally
                {
                    if (isRoot)
                        context.ClearRoot();
                }
            }

            [SecurityCritical]
            private static void InitializeMembers(object obj, ref GeneratorContext context)
            {
                IList<PropertyInfo> properties = Reflector.EmptyArray<PropertyInfo>();
                IList<FieldInfo> fields = Reflector.EmptyArray<FieldInfo>();
                Type type = obj.GetType();
                switch (context.Settings.ObjectInitialization)
                {
                    case ObjectInitialization.PublicFieldsAndProperties:
                        fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                        goto case ObjectInitialization.PublicProperties;
                    case ObjectInitialization.PublicProperties:
                        properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && (p.CanWrite || p.PropertyType.IsCollection())).ToArray();
                        break;
                    case ObjectInitialization.Fields:
                        var result = new List<FieldInfo>();
                        for (Type? t = type; t != null; t = t.BaseType)
                            result.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
                        fields = result;
                        break;
                }

                foreach (PropertyInfo property in properties)
                {
                    if (property.GetIndexParameters().Length > 0)
                        continue;

                    context.PushMember(property.Name);
                    try
                    {
                        // no sense to use Reflector.TrySetProperty because it also throws exception if the property setter itself throws an exception
                        if (property.CanWrite)
                        {
                            if (!TryGenerateObject(property.PropertyType, ref context, out object? value))
                                continue;
                            property.Set(obj, value);
                            continue;
                        }

                        // collection of read-only property
                        var collection = (IEnumerable?)property.Get(obj);
                        if (collection == null)
                            continue;

                        if (collection is Array array)
                        {
                            PopulateArray(array, ref context);
                            continue;
                        }

                        Type collectionType = collection.GetType();
                        if (!(collectionType.IsReadWriteCollection(collection) && collectionType.IsSupportedCollectionForReflection(out var _, out var _, out Type? elementType, out bool isDictionary)))
                            continue;
                        PopulateCollection(collection, elementType, isDictionary, ref context);
                    }
                    catch (Exception e) when (!e.IsCritical())
                    {
                        // we just skip the property if it cannot be set
                    }
                    finally
                    {
                        context.PopMember();
                    }
                }

                foreach (FieldInfo field in fields)
                {
                    context.PushMember(field.Name);
                    try
                    {
                        if (!TryGenerateObject(field.FieldType, ref context, out object? value))
                            continue;
                        field.Set(obj, value);
                    }
                    catch (Exception e) when (!e.IsCritical())
                    {
                        // we just skip the field if it cannot be set
                    }
                    finally
                    {
                        context.PopMember();
                    }
                }
            }

            [SecurityCritical]
            private static Array GenerateArray(Type arrayType, ref GeneratorContext context)
            {
                if (arrayType == Reflector.ByteArrayType)
                    return NextBytes(context.Random, context.GetNextCollectionLength());

                Type elementType = arrayType.GetElementType()!;
                var lengths = new int[arrayType.GetArrayRank()];
                for (int i = 0; i < lengths.Length; i++)
                    lengths[i] = context.GetNextCollectionLength();

                // ReSharper disable once AssignNullToNotNullAttribute
                var result = Array.CreateInstance(elementType, lengths);
                PopulateArray(result, ref context);
                return result;
            }

            [SecurityCritical]
            private static void PopulateArray(Array array, ref GeneratorContext context)
            {
                Type elementType = array.GetType().GetElementType()!;
                if (array.Rank == 1)
                {
                    int length = array.Length;
                    for (int i = 0; i < length; i++)
                    {
                        if (!TryGenerateObject(elementType, ref context, out object? value))
                            continue;
                        array.SetValue(value, i);
                    }
                    return;
                }

                var indexer = new ArrayIndexer(array);
                while (indexer.MoveNext())
                {
                    if (!TryGenerateObject(elementType, ref context, out object? value))
                        continue;
                    array.SetValue(value, indexer.Current);
                }
            }

            [SecurityCritical]
            private static void PopulateCollection(IEnumerable collection, Type elementType, bool isDictionary, ref GeneratorContext context)
            {
                int count = context.GetNextCollectionLength();

                // though this could populate dictionaries, too; duplicates and null keys are not checked
                if (!isDictionary)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (!TryGenerateObject(elementType, ref context, out object? value))
                            continue;
                        collection.TryAdd(value, false);
                    }

                    return;
                }

                Type[] keyValue = GetKeyValueTypes(elementType);
                IDictionary? dictionary = collection as IDictionary;
                PropertyInfo? genericIndexer = dictionary != null ? null : (PropertyInfo)Reflector.IDictionaryGenType.GetGenericType(keyValue).GetDefaultMembers()[0];

                for (int i = 0; i < count; i++)
                {
                    object? key, value;
                    context.PushMember(nameof(DictionaryEntry.Key));
                    try
                    {
                        if (!TryGenerateObject(keyValue[0], ref context, out key) || key == null)
                            continue;
                    }
                    finally
                    {
                        context.PopMember();
                    }

                    context.PushMember(nameof(DictionaryEntry.Value));
                    try
                    {
                        if (!TryGenerateObject(keyValue[1], ref context, out value))
                            continue;
                    }
                    finally
                    {
                        context.PopMember();
                    }

                    if (dictionary != null)
                    {
                        dictionary[key] = value;
                        continue;
                    }

                    genericIndexer!.Set(collection, value, key);
                }
            }

            [SecurityCritical]
            private static object GenerateCollectionByCtor(ConstructorInfo collectionCtor, Type elementType, bool isDictionary, ref GeneratorContext context)
            {
                IEnumerable initializerCollection;
                if (isDictionary)
                {
                    Type[] args = GetKeyValueTypes(elementType);
                    initializerCollection = (IEnumerable)CreateInstanceAccessor.GetAccessor(Reflector.DictionaryGenType.GetGenericType(args[0], args[1])).CreateInstance();
                    PopulateCollection(initializerCollection, elementType, true, ref context);
                }
                else if (collectionCtor.GetParameters()[0].ParameterType.IsAssignableFrom(elementType.MakeArrayType()))
                {
                    initializerCollection = GenerateArray(elementType.MakeArrayType(), ref context);
                }
                else // for non-dictionaries array or list must be accepted by constructor
                {
                    initializerCollection = (IEnumerable)CreateInstanceAccessor.GetAccessor(Reflector.ListGenType.GetGenericType(elementType)).CreateInstance();
                    PopulateCollection(initializerCollection, elementType, false, ref context);
                }

                return CreateInstanceAccessor.GetAccessor(collectionCtor).CreateInstance(initializerCollection);
            }

            private static Type[] GetKeyValueTypes(Type elementType)
                => elementType.IsGenericType ? elementType.GetGenericArguments() : new[] { Reflector.ObjectType, Reflector.ObjectType };

#if !NET35
            private static object GenerateBigInteger(ref GeneratorContext context)
                => context.Random.SampleBigInteger(context.GetNextCollectionLength() * 4, context.Settings.AllowNegativeValues);
#endif

#if NETCOREAPP3_0_OR_GREATER
            private static object GenerateRune(ref GeneratorContext context) => context.Random.NextRune();
#endif

#if NET5_0_OR_GREATER
            private static object GenerateHalf(ref GeneratorContext context)
                => context.Settings.AllowNegativeValues ? context.Random.NextHalf(Half.MinValue, Half.MaxValue, context.Settings.FloatScale) : context.Random.NextHalf(Half.MaxValue, context.Settings.FloatScale);

#endif

#if NET6_0_OR_GREATER
            private static object GenerateDateOnly(ref GeneratorContext context) => DateOnly.FromDateTime((DateTime)GenerateDateTime(ref context));
            private static object GenerateTimeOnly(ref GeneratorContext context) => NextTimeOnly(context.Random);
#endif

            #endregion

            #endregion
        }

        #endregion
    }
}
