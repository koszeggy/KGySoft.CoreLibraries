#if NETCOREAPP3_0_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DynamicDependencyGenerator.cs
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

#endregion

#nullable enable

namespace KGySoft.CoreLibraries
{
    [TestFixture]
    public class DynamicDependencyGenerator
    {
        #region Fields

        private const string template = /* lang=c# */ """
// <generated>
// This is a generated file. It can be updated by explicitly running the GenerateDynamicDependencies test method.
// Alternatively, execute the test project as a (non-trimmed) console app, using the TestName=GenerateDynamicDependencies parameter.
// </generated>

#if NETCOREAPP3_0_OR_GREATER && AOT

#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FallbackTestRunner.g.cs
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

using System.Diagnostics.CodeAnalysis;
		
#endregion

namespace KGySoft.CoreLibraries
{{
    internal sealed partial class FallbackTestRunner
    {{
{0}
        private FallbackTestRunner()
        {{
        }}
    }}
}}

#endif
""";

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// This test is needed to regenerate the [DynamicDependency] attributes for the FallbackTestRunner when testing the libraries in AOT mode with trimming.
        /// It could be a source generator as well, but being an [Explicit] test, it will not affect the build time that much whenever the test project is compiled.
        /// </summary>
        [Test]
        [Explicit]
        public void GenerateDynamicDependencies()
        {
            string targetFile = Path.GetFullPath(Path.Combine(Files.GetExecutingPath(), "..", "..", "..", "FallbackTestRunner.g.cs"));
            if (!File.Exists(targetFile))
                Assert.Fail($"File not found: {targetFile}. Execute this test directly from the default binary output without publishing.");

            List<string> lines = GetAttributeLines();
            string result = String.Format(template, lines.Join(Environment.NewLine));
            File.WriteAllText(targetFile, result);
        }

        #endregion

        #region Private Methods
        
        private List<string> GetAttributeLines()
        {
            var result = new List<string>();
            foreach (Type fixtureType in GetType().Assembly.GetTypes().OrderBy(t => t.FullName))
            {
                string? line = GetDynamicDependencyLine(fixtureType);
                if (line != null)
                    result.Add(line);
            }

            return result;
        }

        private string? GetDynamicDependencyLine(Type fixtureType)
        {
            if (fixtureType.GetCustomAttribute<TestFixtureAttribute>() == null)
                return null;

            MethodInfo[] testMethods = fixtureType.GetMethods()
                .Where(m => (m.IsDefined(typeof(TestAttribute)) || m.IsDefined(typeof(TestCaseAttribute)) || m.IsDefined(typeof(TestCaseSourceAttribute))) && !m.IsDefined(typeof(ExplicitAttribute)))
                .ToArray();

            // no test methods or only explicit tests
            if (testMethods.Length == 0)
                return null;

            DynamicallyAccessedMemberTypes memberTypes = DynamicallyAccessedMemberTypes.PublicMethods;
            foreach (MethodInfo testMethod in testMethods)
            {
                foreach (TestCaseSourceAttribute source in testMethod.GetCustomAttributes<TestCaseSourceAttribute>())
                {
                    if (source.SourceName == null)
                        Assert.Fail($"Test case source name is not defined on test method {fixtureType}.{testMethod}");
                    foreach (MemberInfo memberInfo in fixtureType.GetMember(source.SourceName!, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        switch (memberInfo)
                        {
                            case FieldInfo field:
                                memberTypes |= field.IsPublic ? DynamicallyAccessedMemberTypes.PublicFields : DynamicallyAccessedMemberTypes.NonPublicFields;
                                break;
                            case PropertyInfo property:
                                memberTypes |= property.GetMethod.IsPublic ? DynamicallyAccessedMemberTypes.PublicProperties : DynamicallyAccessedMemberTypes.NonPublicProperties;
                                break;
                            case MethodInfo method:
                                memberTypes |= method.IsPublic ? DynamicallyAccessedMemberTypes.PublicProperties : DynamicallyAccessedMemberTypes.NonPublicProperties;
                                break;
                            default:
                                Assert.Fail($"Unexpected TestCaseSource name '{source.SourceName}' on test method {fixtureType}.{testMethod}");
                                break;
                        }
                    }
                }
            }

            return $"        [DynamicDependency({memberTypes.GetFlags().Select(f => $"DynamicallyAccessedMemberTypes.{f}").Join(" | ")}, typeof({fixtureType.FullName}))]";
        }

        #endregion

        #endregion
    }
}
#endif