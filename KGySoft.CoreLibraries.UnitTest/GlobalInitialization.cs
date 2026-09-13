#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: GlobalInitialization.cs
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
#if NETCOREAPP && !NETCOREAPP3_0_OR_GREATER
using System.Drawing;
#endif
#if NETCOREAPP
using System.IO;
#endif
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
#if AOT
using System.Runtime.Versioning;
#endif
#if !NETFRAMEWORK
using System.Text;
#endif

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries
{
    [SetUpFixture]
    public class GlobalInitialization
    {
        #region Methods

        [OneTimeSetUp]
        public void Initialize()
        {
            if (Program.ConsoleWriter != null)
                Console.SetOut(Program.ConsoleWriter);
#if NETCOREAPP3_0_OR_GREATER
            if (RuntimeFeature.IsDynamicCodeSupported)
#endif
            {
                Console.WriteLine($"Referenced runtime by KGySoft.CoreLibraries: {typeof(Module).Assembly.GetReferencedAssemblies()[0]}");
            }
#if NET35
            if (typeof(object).Assembly.GetName().Version != new Version(2, 0, 0, 0))
                Assert.Inconclusive($"mscorlib version does not match to .NET 3.5: {typeof(object).Assembly.GetName().Version}. Change the executing framework to .NET 2.0 or execute the tests as a console application.");
#elif NETFRAMEWORK
            if (typeof(object).Assembly.GetName().Version != new Version(4, 0, 0, 0))
                Assert.Inconclusive($"mscorlib version does not match to .NET 4.x: {typeof(object).Assembly.GetName().Version}. Change the executing framework to .NET 4.x");
#elif AOT
            TargetFrameworkAttribute attr = (TargetFrameworkAttribute)Attribute.GetCustomAttribute(typeof(GlobalInitialization).Assembly, typeof(TargetFrameworkAttribute))!;
            string frameworkName = attr.FrameworkDisplayName is { Length: > 0 } name ? name : attr.FrameworkName;
            Console.WriteLine($"Tests are executing on {frameworkName}");
#elif NETCOREAPP
            Console.WriteLine($"Tests are executing on .NET Core version {Path.GetFileName(Path.GetDirectoryName(typeof(object).Assembly.Location))}");
#else
#error unknown .NET version
#endif

#if !NETFRAMEWORK
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
#if NETCOREAPP && !NETCOREAPP3_0_OR_GREATER
            typeof(Bitmap).RegisterTypeConverter<BitmapConverter>();
            typeof(Icon).RegisterTypeConverter<IconConverter>();
#endif
#if NET8_0
            // Only for .NET 8: enabling BinaryFormatter, which is used for the tests only (comparing functionality, payload sizes, performance, etc.)
            // It is safe as serialization and deserialization is performed within the same process so no potentially dangerous external payload is processed.
            // In .NET 9 it will be removed completely.
            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
#endif
        }

        #endregion
    }
}