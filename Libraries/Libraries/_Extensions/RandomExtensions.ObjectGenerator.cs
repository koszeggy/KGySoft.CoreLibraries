#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomExtensions.ObjectGenerator.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using KGySoft.Collections;
using KGySoft.Reflection;
using KGySoft.Serialization;

#endregion

namespace KGySoft.Libraries
{
    public static partial class RandomExtensions
    {
        #region ObjectGenerator class

        private static class ObjectGenerator
        {
            #region Nested types

            #region Delegates

            private delegate object GenerateKnownType(ref GeneratorContext context);

            #endregion

            #region Nested structs

            #region DefaultGenericTypeKey struct

            /// <summary>
            /// Cache key for a generic type with its suggested arguments, with Equals/GetHashCode by array elements.
            /// </summary>
            private struct DefaultGenericTypeKey : IEquatable<DefaultGenericTypeKey>
            {
                #region Fields

                private readonly Type[] types;

                #endregion

                #region Properties

                internal Type GenericType => types[0];
                internal
#if NET35 || NET40
                    ArraySegment<Type> 
#else
                    IList<Type>
#endif
                        SuggestedArguments => new ArraySegment<Type>(types, 1, types.Length - 1);

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

                public override bool Equals(object obj) => obj is DefaultGenericTypeKey key && Equals(key);
                public bool Equals(DefaultGenericTypeKey other) => types.SequenceEqual(other.types);
                public override int GetHashCode() => types.Aggregate(615762546, (hc, t) => hc * -1521134295 + t.GetHashCode());

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

                private readonly Stack<string> memberNameStack;
                private readonly HashSet<Type> typesBeingGenerated;

                #endregion

                #endregion

                #region Properties

                public string MemberName => memberNameStack.Count == 0 ? null : memberNameStack.Peek();

                #endregion

                #region Constructors

                public GeneratorContext(Random random, GenerateObjectSettings settings)
                {
                    Random = random;
                    Settings = settings;
                    memberNameStack = new Stack<string>();
                    typesBeingGenerated = new HashSet<Type>();
                }

                #endregion

                #region Methods

                internal void PushMember(string memberName) => memberNameStack.Push(memberName);
                internal void PopMember() => memberNameStack.Pop();
                internal bool IsGenerating(Type type) => typesBeingGenerated.Contains(type);
                internal void PushType(Type type) => typesBeingGenerated.Add(type);
                internal void PopType(Type type) => typesBeingGenerated.Remove(type);

                #endregion
            }

            #endregion

            #endregion

            #endregion

            #region Fields

            private static readonly Cache<Assembly, Type[]> assemblyTypesCache = new Cache<Assembly, Type[]>(LoadAssemblyTypes);
            private static readonly Cache<Type, Type[]> typeImplementorsCache = new Cache<Type, Type[]>(SearchForImplementors);
            private static readonly Cache<DefaultGenericTypeKey, Type> defaultConstructedGenerics = new Cache<DefaultGenericTypeKey, Type>(TryCreateDefaultGeneric);
            private static readonly Cache<Type, Delegate> delegatesCache = new Cache<Type, Delegate>(CreateDelegate);

            /// <summary>
            /// Must be a separate instance because dynamic method references will never freed.
            /// Other problem if the original Random is a disposable secure random: invoking the delegate would throw an exception.
            /// </summary>
            private static readonly Random randomForDelegates = new Random();

            private static readonly FieldInfo randomField = (FieldInfo)Reflector.MemberOf(() => randomForDelegates);
            private static readonly MethodInfo nextObjectMethod = ((MethodInfo)typeof(RandomExtensions).GetMethod(nameof(NextObject)));

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
                    { typeof(IntPtr), GenerateIntPtr },
                    { typeof(UIntPtr), GenerateUIntPtr },

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

            private static readonly Type memberInfoType = typeof(MemberInfo);
            private static readonly Type fieldInfoType = typeof(FieldInfo);
            private static readonly Type rtFieldInfoType = Reflector.MemberOf(() => String.Empty).GetType();
            private static readonly Type mdFieldInfoType = typeof(int).GetField(nameof(Int32.MaxValue)).GetType(); // MemberOf does not work for constants
            private static readonly Type runtimeFieldInfoType = rtFieldInfoType.BaseType;
            private static readonly Type propertyInfoType = typeof(PropertyInfo);
            private static readonly Type runtimePropertyInfoType = Reflector.MemberOf(() => ((string)null).Length).GetType();
            private static readonly Type methodBaseType = typeof(MethodBase);
            private static readonly Type methodInfoType = typeof(MethodInfo);
            private static readonly Type runtimeMethodInfoType = Reflector.MemberOf(() => ((object)null).ToString()).GetType();
            private static readonly Type ctorInfoType = typeof(ConstructorInfo);
            private static readonly Type runtimeCtorInfoType = Reflector.MemberOf(() => new object()).GetType();
            private static readonly Type eventInfoType = typeof(EventInfo);
            private static readonly Type runtimeEventInfoType = typeof(Console).GetEvent(nameof(Console.CancelKeyPress)).GetType();

            #endregion

            #region Methods

            #region Internal Methods

            internal static object GenerateObject(Random random, Type type, GenerateObjectSettings settings)
            {
                var context = new GeneratorContext(random, settings);
                return GenerateObject(type, ref context);
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
                    return e.Types.Where(t => t != null).ToArray();
                }
            }

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

            private static Type TryCreateDefaultGeneric(DefaultGenericTypeKey key)
            {
                Type genericTypeDef = key.GenericType;
                var suggestedArguments = key.SuggestedArguments; // var is ArraySegment<Type> in .NET 3.4/4.0 and IList<Type> above
                Type[] genericParams = genericTypeDef.GetGenericArguments();
                Type[] argumentsToCreate = new Type[genericParams.Length];
                Type[][] constraints = new Type[genericParams.Length][];
                for (int i = 0; i < genericParams.Length; i++)
                {
                    Type argDef = genericParams[i];
                    GenericParameterAttributes attr = argDef.GenericParameterAttributes;
                    bool valueTypeConstraint = (attr & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
                    constraints[i] = argDef.GetGenericParameterConstraints();

                    Type arg = suggestedArguments.Count == argumentsToCreate.Length
#if (NET35 || NET40)
                        // ReSharper disable once PossibleNullReferenceException
                        ? suggestedArguments.Array[suggestedArguments.Offset + i]
#else
                        ? suggestedArguments[i]
#endif
                        : null;

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

            private static Delegate CreateDelegate(Type type)
            {
                MethodInfo mi = type.GetMethod(nameof(Action.Invoke));
                Type returnType = mi.ReturnType;
                var parameters = mi.GetParameters();

                // ReSharper disable once PossibleNullReferenceException
                var dm = new DynamicMethod($"dm_{type.Name}", returnType, mi.GetParameters().Select(p => p.ParameterType).ToArray(), true);
                ILGenerator il = dm.GetILGenerator();

                // calling NextObject<T> for out parameters
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (!parameters[i].IsOut)
                        continue;

                    Type parameterType = parameters[i].ParameterType.GetElementType();

                    il.Emit(OpCodes.Ldarg, i);
                    il.Emit(OpCodes.Ldsfld, randomField);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Call, nextObjectMethod.MakeGenericMethod(parameterType));

                    // ReSharper disable once PossibleNullReferenceException
                    if (parameterType.IsValueType)
                        il.Emit(OpCodes.Stobj, parameterType);
                    else
                        il.Emit(OpCodes.Stind_Ref);
                }

                // calling NextObject<T> for return value
                if (mi.ReturnType != typeof(void))
                {
                    il.Emit(OpCodes.Ldsfld, randomField);
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Call, nextObjectMethod.MakeGenericMethod(returnType));
                }

                il.Emit(OpCodes.Ret);
                return dm.CreateDelegate(type);
            }

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
                return assemblies.GetRandomElement(context.Random);
            }

            private static Type PickRandomType(ref GeneratorContext context)
            {
                Assembly[] assemblies = Reflector.GetLoadedAssemblies();
                Assembly asm = assemblies.GetRandomElement(context.Random);
                Type[] types = GetAssemblyTypes(asm);

                if (types.Length == 0)
                {
                    foreach (Assembly candidate in assemblies.Except(new[] { asm }).Shuffle(context.Random))
                    {
                        types = GetAssemblyTypes(candidate);
                        if (types.Length != 0)
                            break;
                    }
                }

                return types.GetRandomElement(context.Random);
            }

            private static MemberInfo PickRandomMemberInfo(Type type, ref GeneratorContext context)
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
                Assembly asm = assemblies.GetRandomElement(context.Random);
                MemberInfo result = TryPickMemberInfo(GetAssemblyTypes(asm).GetRandomElement(context.Random, true), memberTypes, ref context, constants);

                if (result != null)
                    return result;

                // low performance fallback: shuffling
                foreach (Assembly assembly in assemblies.Except(new[] { asm }).Shuffle(context.Random))
                {
                    foreach (Type t in GetAssemblyTypes(assembly).Shuffle(context.Random))
                    {
                        result = TryPickMemberInfo(t, memberTypes, ref context, constants);
                        if (result != null)
                            return result;

                    }
                }

                return null;
            }

            private static MemberInfo TryPickMemberInfo(Type type, MemberTypes memberTypes, ref GeneratorContext context, bool? constants)
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

                    if (!(member is FieldInfo field))
                        continue;

                    if (constants == true && field.IsLiteral || constants == false && !field.IsLiteral)
                        return member;
                }

                return null;
            }

            private static object PickRandomEnum(Type type, ref GeneratorContext context)
            {
                Array values = Enum.GetValues(type);
                return values.Length == 0 ? Enum.ToObject(type, 0) : values.GetValue(context.Random.Next(values.Length));
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
                if (type == Reflector.ObjectType && context.Settings.SubstitutionForObjectType != null)
                    type = context.Settings.SubstitutionForObjectType;

                // ReSharper disable once AssignNullToNotNullAttribute
                // 1.) known type
                if (knownTypes.TryGetValue(type, out var knownGenerator))
                    return knownGenerator.Invoke(ref context);

                // 2.) enum
                if (type.IsEnum)
                    return PickRandomEnum(type, ref context);

                // 3.) array
                if (type.IsArray)
                    return GenerateArray(type, ref context);

                // 4.) supported collection
                if (type.IsSupportedCollectionForReflection(out var defaultCtor, out var collectionCtor, out var elementType, out bool isDictionary)
                    || context.Settings.AllowCreateObjectWithoutConstructor && type.IsCollection())
                {
                    IEnumerable collection = null;

                    // preferring default constructor and populating
                    // CreateInstance is faster than obtaining object factory by the constructor
                    if (defaultCtor != null || type.IsValueType)
                        collection = (IEnumerable)Activator.CreateInstance(type, true);
                    else if (collectionCtor == null && context.Settings.AllowCreateObjectWithoutConstructor)
                        collection = (IEnumerable)FormatterServices.GetUninitializedObject(type);

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
                if (type.IsGenericTypeOf(typeof(KeyValuePair<,>)))
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

                object result;

                // 6.) Reflection members (Assembly and Type are already handled as known types but RuntimeType is handled here)
                if (memberInfoType.IsAssignableFrom(type) && (result = PickRandomMemberInfo(type, ref context)) != null)
                    return result;

                // 7.) any object
                bool resolveType = false;
                if (type.IsAbstract || type.IsInterface)
                {
                    if (!context.Settings.TryResolveInterfacesAndAbstractTypes)
                        return null;
                    resolveType = true;
                }
                else if (!type.IsSealed && context.Settings.AllowDerivedTypesForNonSealedClasses)
                    resolveType = true;

                IList<Type> typeCandidates = null;
                Type typeToCreate = type;
                if (resolveType)
                {
                    typeCandidates = GetResolvedTypes(type);
                    typeToCreate = typeCandidates.GetRandomElement(context.Random, true);
                    if (typeToCreate == null)
                        return null;
                }

                result = GenerateAnyObject(typeToCreate, ref context);
                if (result != null || !resolveType)
                    return result;

                // We check all of the compatible types in random order.
                // The try above could be in this foreach below but we try to avoid the shuffling if possible
                if (!type.IsSealed && (context.Settings.TryResolveInterfacesAndAbstractTypes || context.Settings.AllowNegativeValues))
                {
                    foreach (Type candidateType in typeCandidates.Except(new[] { typeToCreate }).Shuffle(context.Random))
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
                if (lengths.Length == 1)
                {
                    int length = lengths[0];
                    for (int i = 0; i < length; i++)
                        result.SetValue(GenerateObject(context.Random, elementType, context.Settings), i);
                }
                else
                {
                    var indexer = new ArrayIndexer(lengths);
                    while (indexer.MoveNext())
                        result.SetValue(GenerateObject(context.Random, elementType, context.Settings), indexer.Current);
                }

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
                // Special case 1: Enum by System.Enum type or an interface. Handling separately to avoid non-existing elements.
                if (type.IsEnum)
                    return PickRandomEnum(type, ref context);

                // Special case 2: Known type by interface.
                if (knownTypes.TryGetValue(type, out var knownGenerator))
                    return knownGenerator.Invoke(ref context);

                // Special case 3: Delegate
                if (type.IsDelegate())
                    return GetDelegate(type, ref context);

                if (context.IsGenerating(type))
                    return null;

                context.PushType(type);
                try
                {
                    object result = null;
                    if (type.CanBeCreatedWithoutParameters())
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

            private static Delegate GetDelegate(Type type, ref GeneratorContext context)
            {
                lock (delegatesCache)
                {
                    return delegatesCache[type];
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

            #endregion

            #endregion
        }

        #endregion
    }
}
