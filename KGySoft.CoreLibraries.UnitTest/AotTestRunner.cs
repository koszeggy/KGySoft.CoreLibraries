#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AotTestRunner.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
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
using System.Linq;
using System.Reflection;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

#endregion

#nullable enable

namespace KGySoft.CoreLibraries
{
    internal sealed partial class AotTestRunner
    {
        #region Nested classes

        #region TestResultContainer class

        private class TestResultContainer : TestResult
        {
            #region Fields

            private readonly List<ITestResult> children = new();

            #endregion

            #region Properties

            public override int TotalCount => children.Count == 0 ? FailCount : children.Sum(r => r.TotalCount);
            public override int FailCount => (ResultState.In(ResultState.Failure, ResultState.Error, ResultState.SetUpFailure, ResultState.SetUpError, ResultState.TearDownError) ? 1 : 0) + children.Sum(r => r.FailCount);
            public override int WarningCount => ResultState == ResultState.Warning ? 1 : children.Sum(r => r.WarningCount);
            public override int PassCount => children.Sum(r => r.PassCount);
            public override int SkipCount => children.Sum(r => r.SkipCount);
            public override int InconclusiveCount => children.Sum(r => r.InconclusiveCount);
            public override bool HasChildren => children.Count > 0;
            public override IEnumerable<ITestResult> Children => children;

            #endregion

            #region Constructors

            public TestResultContainer(ITest test)
                : base(test)
            {
            }

            #endregion

            #region Methods

            internal void AddResult(ITestResult result) => children.Add(result);

            #endregion
        }

        #endregion

        #region TestCaseResult class

        private class TestCaseResult : TestResult
        {
            #region Properties

            public override int TotalCount => 1;
            public override int FailCount => ResultState.In(ResultState.Failure, ResultState.Error, ResultState.SetUpFailure, ResultState.SetUpError, ResultState.TearDownError) ? 1 : 0;
            public override int WarningCount => 0;
            public override int PassCount => ResultState == ResultState.Success ? 1 : 0;
            public override int SkipCount => ResultState == ResultState.Skipped ? 1 : 0;
            public override int InconclusiveCount => ResultState == ResultState.Inconclusive ? 1 : 0;
            public override bool HasChildren => false;
            public override IEnumerable<ITestResult> Children => [];

            #endregion

            #region Constructors

            internal TestCaseResult(MethodInfo methodInfo, ResultState resultState, string? name = null, Exception? exception = null)
                : base(new TestMethod(new MethodWrapper(methodInfo.DeclaringType!, methodInfo)) { Name = name ?? methodInfo.Name })
            {
                SetResult(resultState, exception?.Message, exception?.StackTrace);
            }

            internal TestCaseResult(ITest test)
                : base(test)
            {
            }

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        private readonly TestRunConfig config;

        #endregion

        #region Constructors

        internal AotTestRunner(TestRunConfig config)
            : this() // to ensure the [DynamicDependency] attributes take effect
        {
            this.config = config;
        }

        #endregion

        #region Methods

        #region Static Methods

        private static void RunTestCases(object instance, TestResultContainer classResult, ITestListener listener, MethodInfo method, IReadOnlyCollection<MethodInfo> setupMethods, IReadOnlyCollection<MethodInfo> tearDownMethods)
        {
            foreach ((object?[] Parameters, Type[]? TypeArguments) testCaseInfo in GetTestCases(method, instance))
            {
                string caseName = testCaseInfo.Parameters.Length == 0 ? method.Name : $"{method.Name}({testCaseInfo.Parameters.Select(p => p?.ToString() ?? "null").Join(", ")})";
                MethodInfo invocationMethod = method;
                try
                {
                    if (method.IsGenericMethodDefinition)
                        invocationMethod = method.MakeGenericMethod(testCaseInfo.TypeArguments!);
                }
                catch (Exception e)
                {
                    classResult.AddResult(new TestCaseResult(method, ResultState.Error, caseName, e));
                    return;
                }

                var testCase = new TestMethod(new MethodWrapper(invocationMethod.DeclaringType!, invocationMethod)) { Name = caseName };
                listener.TestStarted(testCase);
                var testResult = new TestCaseResult(testCase);
                try
                {
                    // [SetUp]
                    try
                    {
                        foreach (MethodInfo setup in setupMethods)
                            setup.Invoke(instance, null);
                    }
                    catch (Exception e)
                    {
                        var state = e is AssertionException ? ResultState.SetUpFailure : ResultState.SetUpError;
                        testResult.SetResult(state, e.Message, e.StackTrace);
                        return;
                    }

                    try
                    {
                        invocationMethod.Invoke(instance, testCaseInfo.Parameters);
                        testResult.SetResult(ResultState.Success);
                    }
                    catch (Exception e)
                    {
                        if (e is TargetInvocationException { InnerException: not null })
                            e = e.InnerException!;
                        var state = e switch
                        {
                            SuccessException => ResultState.Success,
                            InconclusiveException => ResultState.Inconclusive,
                            AssertionException => ResultState.Failure,
                            _ => ResultState.Error
                        };
                        testResult.SetResult(state, e.Message, e.StackTrace);
                    }
                    finally
                    {
                        // [TearDown]
                        try
                        {
                            foreach (MethodInfo tearDown in tearDownMethods)
                                tearDown.Invoke(instance, null);
                        }
                        catch (Exception e)
                        {
                            if (testResult.ResultState == ResultState.Success || testResult.ResultState == ResultState.Inconclusive)
                                testResult.SetResult(ResultState.TearDownError, e.Message, e.StackTrace);
                        }
                    }
                }
                finally
                {
                    listener.TestFinished(testResult);
                    classResult.AddResult(testResult);
                }
            }
        }

        private static IEnumerable<(object?[] Parameters, Type[]? TypeArguments)> GetTestCases(MethodInfo method, object fixtureInstance)
        {
            // simple [Test] method
            if (!method.IsDefined(typeof(TestCaseAttribute)) && !method.IsDefined(typeof(TestCaseSourceAttribute)))
            {
                yield return (Array.Empty<object>(), null);
                yield break;
            }

            Type fixtureType = method.DeclaringType!;
            ParameterInfo[] parameters = method.GetParameters();

            // [TestCase], [TestCaseGeneric]
            foreach (TestCaseAttribute attribute in method.GetCustomAttributes<TestCaseAttribute>())
            {
                Type[]? typeArguments = (attribute as TestCaseGenericAttribute)?.TypeArguments;

                // Generic method but no [TestCase<...>] or [TestCaseGeneric] was used: we need to infer the type arguments
                if (typeArguments == null && method.IsGenericMethodDefinition)
                {
                    typeArguments = method.GetGenericArguments();
                    for (int i = 0; i < typeArguments.Length; i++)
                    {
                        Type typeArg = typeArguments[i];
                        int index = parameters.IndexOf(p => p.ParameterType == typeArg);
                        typeArguments[i] = index >= 0
                            ? attribute.Arguments[index]?.GetType() ?? typeof(object)
                            : throw new InvalidOperationException($"Cannot infer the type argument for generic parameter '{typeArg.Name}' of method '{method.Name}' from the provided test case arguments.");
                    }
                }

                yield return (attribute.Arguments, typeArguments);
            }

            // [TestCaseSource], [TestCaseSourceGeneric]
            foreach (TestCaseSourceAttribute attribute in method.GetCustomAttributes<TestCaseSourceAttribute>())
            {
                Type sourceType = attribute.SourceType ?? fixtureType;
                string? sourceName = attribute.SourceName;
                if (sourceName == null)
                    continue;

                MemberInfo sourceMember = sourceType.GetMember(sourceName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)[0];
                object? sourceInstance = sourceMember is MethodInfo sourceMethod && sourceMethod.IsStatic ? null : fixtureInstance;
                object? source = sourceMember switch
                {
                    FieldInfo field => field.GetValue(sourceInstance),
                    PropertyInfo property => property.GetValue(sourceInstance),
                    MethodInfo methodInfo => methodInfo.Invoke(sourceInstance, null),
                    _ => null
                };

                if (source is not IEnumerable cases)
                    continue;

                foreach (object testCase in cases)
                {
                    object?[] arguments = testCase is TestCaseData data ? data.Arguments
                        : testCase is object[] args && parameters.Length > 1 ? args
                        : [testCase];
                    Type[]? typeArguments = (attribute as TestCaseSourceGenericAttribute)?.TypeArguments;
                    yield return (arguments, typeArguments);
                }
            }
        }

        #endregion

        #region Instance Methods

        #region Internal Methods

        internal ITestResult Run(ITestListener listener)
        {
            var rootResult = new TestResultContainer(new TestSuite("KGySoft.CoreLibraries.UnitTest"));
            new GlobalInitialization().Initialize(); // not bothering with [SetUpFixture]

            // Discoverability in AOT mode is provided by the parameterless constructor, see the other part of this partial class.
            foreach (Type fixtureType in typeof(AotTestRunner).Assembly.GetTypes())
            {
                //if (fixtureType.GetCustomAttribute<TestFixtureAttribute>() == null) // the [TextFixture] attribute is actually optional
                //    continue;
                if (fixtureType.IsAbstract || fixtureType.IsGenericTypeDefinition || fixtureType.GetConstructor(Type.EmptyTypes) == null)
                    continue;

                // Unlike NUnit, we support filtering both by class and test name, and also support simple class names
                if (config.ClassName != null && fixtureType.FullName != config.ClassName && fixtureType.Name != config.ClassName)
                    continue;

                // Shortcut: Skipping the whole class if the requested test name is not in this class.
                // It is required to skip OneTimeSetUp and OneTimeTearDown methods of other classes, which would otherwise run even if the tests of the class are all skipped.
                if (config.TestName != null && fixtureType.GetMethod(config.TestName) == null)
                    continue;

                var classResult = new TestResultContainer(new TestSuite(fixtureType));
                try
                {
                    object instance = Activator.CreateInstance(fixtureType)!;
                    if (RunTestFixture(fixtureType, instance, classResult, listener))
                        rootResult.AddResult(classResult);
                }
                catch (Exception e)
                {
                    classResult.SetResult(ResultState.Error, e.Message, e.StackTrace);
                    rootResult.AddResult(classResult);
                }
            }

            return rootResult;
        }

        #endregion

        #region Private Methods

        private bool RunTestFixture(Type fixtureType, object instance, TestResultContainer classResult, ITestListener listener)
        {
            #region Local Methods

            static IReadOnlyCollection<MethodInfo> FilterMethods<TAttribute>(MethodInfo[] methods)
                where TAttribute : Attribute
            {
                return methods.Where(method => method.IsDefined(typeof(TAttribute))).ToList();
            }

            #endregion

            MethodInfo[] methods = fixtureType.GetMethods(BindingFlags.Instance | BindingFlags.Public);

            // Skipping the class if it has no methods with NUnit attributes at all.
            if (FilterMethods<NUnitAttribute>(methods).Count == 0)
                return false;

            // No [TextFixture]: warning, unless other NUnit attributes are present, which is a valid case (e.g., [SetUpFixture]).
            if (fixtureType.GetCustomAttribute<TestFixtureAttribute>() == null && !fixtureType.IsDefined(typeof(NUnitAttribute)))
                classResult.SetResult(ResultState.Warning, $"{fixtureType.FullName}: [TestFixture] attribute is missing. The test class might be trimmed when publishing in in AOT mode.");

            IReadOnlyCollection<MethodInfo> oneTimeSetUpMethods = FilterMethods<OneTimeSetUpAttribute>(methods);
            IReadOnlyCollection<MethodInfo> setupMethods = FilterMethods<SetUpAttribute>(methods);
            IReadOnlyCollection<MethodInfo> tearDownMethods = FilterMethods<TearDownAttribute>(methods);
            IReadOnlyCollection<MethodInfo> oneTimeTearDownMethods = FilterMethods<OneTimeTearDownAttribute>(methods);

            // [OneTimeSetUp]
            try
            {
                foreach (MethodInfo setup in oneTimeSetUpMethods)
                    setup.Invoke(instance, null);
            }
            catch (Exception e)
            {
                var state = e is AssertionException ? ResultState.SetUpFailure : ResultState.SetUpError;
                classResult.SetResult(state, e.Message, e.StackTrace);
                return true;
            }

            foreach (MethodInfo method in methods)
            {
                if (method.GetCustomAttribute<TestAttribute>() == null
                    && !method.IsDefined(typeof(TestCaseAttribute))
                    && !method.IsDefined(typeof(TestCaseSourceAttribute)))
                    continue;

                if (config.TestName != null && config.TestName != method.Name)
                    continue;

                // [Explicit]: skipping, unless requested by the config
                if (method.GetCustomAttribute<ExplicitAttribute>() != null)
                {
                    if (config.TestName != method.Name)
                    {
                        classResult.AddResult(new TestCaseResult(method, ResultState.Skipped));
                        continue;
                    }
                }

                RunTestCases(instance, classResult, listener, method, setupMethods, tearDownMethods);
            }

            // [OneTimeTearDown]
            try
            {
                foreach (MethodInfo tearDown in oneTimeTearDownMethods)
                    tearDown.Invoke(instance, null);
            }
            catch (Exception e)
            {
                classResult.SetResult(ResultState.TearDownError, e.Message, e.StackTrace);
            }

            return true;
        }

        #endregion

        #endregion

        #endregion
    }
}
