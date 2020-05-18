#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AssemblyResolver.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
#if NETFRAMEWORK
using KGySoft.Reflection.WinApi;
#endif

#endregion

namespace KGySoft.Reflection
{
    internal static class AssemblyResolver
    {
        #region Constants

#if !NETFRAMEWORK
        private const string mscorlibName = "mscorlib"; 
#endif

        #endregion

        #region Fields

        #region Internal Fields

        internal static readonly Assembly KGySoftCoreLibrariesAssembly = typeof(AssemblyResolver).Assembly;
        internal static readonly Assembly CoreLibrariesAssembly = typeof(object).Assembly; // it is mscorlib only in .NET Framework

        #endregion

        #region Private Fields

        private static Assembly mscorlibAssembly;
        private static LockingDictionary<string, Assembly> assemblyCache;

#if !NET35
        private static IThreadSafeCacheAccessor<Type, (string, bool)> forwardedNamesCache; 
#endif

        private static readonly string[] coreLibNames =
        {
            CoreLibrariesAssembly.FullName.Split(',')[0].ToLowerInvariant(), // could be by GetName but that requires FileIOPermission
#if !NETFRAMEWORK
            mscorlibName
#endif
        };

        #endregion

        #endregion

        #region Properties

        #region Internal Properties

        internal static Assembly MscorlibAssembly => mscorlibAssembly ??=
#if !NETFRAMEWORK
            ResolveAssembly(mscorlibName, ResolveAssemblyOptions.TryToLoadAssembly) ??
#endif
            typeof(object).Assembly;

        #endregion

        #region Private Properties

        private static LockingDictionary<string, Assembly> AssemblyCache
            => assemblyCache ??= new Cache<string, Assembly>().AsThreadSafe();

#if !NET35
        private static IThreadSafeCacheAccessor<Type, (string ForwardedAssemblyName, bool IsCoreIdentity)> ForwardedNamesCache
            => forwardedNamesCache ??= new Cache<Type, (string, bool)>(DoGetForwardedAssemblyName).GetThreadSafeAccessor();
#endif

        #endregion

        #endregion

        #region Methods

        #region Internal Methods

        internal static Assembly ResolveAssembly(string assemblyName, ResolveAssemblyOptions options)
        {
            if (assemblyName == null)
                Throw.ArgumentNullException(Argument.assemblyName);
            if (assemblyName.Length == 0)
                Throw.ArgumentException(Argument.assemblyName, Res.ArgumentEmpty);
            if (!options.AllFlagsDefined())
                Throw.FlagsEnumArgumentOutOfRange(Argument.options, options);

            AssemblyName asmName;
            try
            {
                asmName = new AssemblyName(assemblyName);
            }
            catch (Exception e) when (!e.IsCritical())
            {
                if ((options & ResolveAssemblyOptions.ThrowError) != ResolveAssemblyOptions.None)
                    Throw.ArgumentException(Argument.assemblyName, Res.ReflectionInvalidAssemblyName(assemblyName), e);
                return null;
            }

            return GetOrResolve(asmName, options);
        }

        internal static Assembly ResolveAssembly(AssemblyName assemblyName, ResolveAssemblyOptions options)
        {
            if (assemblyName == null)
                Throw.ArgumentNullException(Argument.assemblyName);
            if (!options.AllFlagsDefined())
                Throw.FlagsEnumArgumentOutOfRange(Argument.options, options);
            return GetOrResolve(assemblyName, options);
        }

        internal static bool IdentityMatches(AssemblyName refName, AssemblyName toCheck, bool allowPartialMatch)
        {
            if (toCheck == null)
                return false;

            // Different name: skip
            if (!String.Equals(toCheck.Name, refName.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            // Here name matches. In case of partial match we are done.
            if (allowPartialMatch)
                return true;

            // Checking version, culture and public key token
            Version version = refName.Version;
            if (version != null && toCheck.Version != version)
                return false;

            CultureInfo culture = refName.CultureInfo;
            if (culture != null && toCheck.CultureInfo.Name != culture.Name)
                return false;

            byte[] publicKeyTokenRef, publicKeyTokenCheck;
            return (publicKeyTokenRef = refName.GetPublicKeyToken()) == null
                || (publicKeyTokenCheck = toCheck.GetPublicKeyToken()) == null
                || publicKeyTokenRef.SequenceEqual(publicKeyTokenCheck);
        }

        internal static bool IsCoreLibAssemblyName(string assemblyName)
        {
            string name = assemblyName.Split(new[] { ',' }, 2)[0].ToLowerInvariant();
            return name.In(coreLibNames);
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "Same signature for every target platform.")]
        internal static string GetForwardedAssemblyName(Type type, in bool omitIfCoreLibrary)
        {
#if NET35
            return null;
#else
            var name = ForwardedNamesCache[type];
            return omitIfCoreLibrary && name.IsCoreIdentity ? null : name.ForwardedAssemblyName;
#endif
        }

        #endregion

        #region Private Methods

        private static Assembly GetOrResolve(AssemblyName assemblyName, ResolveAssemblyOptions options)
        {
            string key = ((int)(options & ~ResolveAssemblyOptions.ThrowError)).ToString(CultureInfo.InvariantCulture) + assemblyName.FullName;
            if (AssemblyCache.TryGetValue(key, out Assembly result))
                return result;

            try
            {
                result = Resolve(assemblyName, options);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is ReflectionException))
            {
                if ((options & ResolveAssemblyOptions.ThrowError) != ResolveAssemblyOptions.None)
                    Throw.ReflectionException(Res.ReflectionCannotResolveAssembly(assemblyName.FullName));
                return null;
            }

            if (result == null)
            {
                if ((options & ResolveAssemblyOptions.ThrowError) != ResolveAssemblyOptions.None)
                    Throw.ReflectionException(Res.ReflectionCannotResolveAssembly(assemblyName.FullName));
                return null;
            }

            assemblyCache[key] = result;
            return result;
        }

        private static Assembly Resolve(AssemblyName assemblyName, ResolveAssemblyOptions options)
        {
            // 1.) Iterating through loaded assemblies, checking names
            bool allowPartialMatch = (options & ResolveAssemblyOptions.AllowPartialMatch) != ResolveAssemblyOptions.None;
            string fullName = assemblyName.FullName;
            foreach (Assembly asm in Reflector.GetLoadedAssemblies())
            {
                // Simple match. As fullName is via property, for fully qualified names this will work for sure.
                if (asm.FullName == fullName)
                    return asm;

                // Otherwise, we check the provided information (we still can accept simple name if nothing else is provided)
                if (IdentityMatches(assemblyName, asm.GetName(), allowPartialMatch))
                    return asm;
            }

            if ((options & ResolveAssemblyOptions.TryToLoadAssembly) == ResolveAssemblyOptions.None)
                return null;

            // 2.) Trying to load the assembly
            Assembly result = LoadAssembly(assemblyName, options);
            return result?.FullName == fullName || IdentityMatches(assemblyName, result?.GetName(), allowPartialMatch) ? result : null;
        }

        /// <summary>
        /// Loads the assembly with partial name. It is needed because Assembly.LoadWithPartialName is obsolete.
        /// </summary>
        [SecuritySafeCritical]
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom",
            Justification = "The way it is used ensures that only GAC assemblies are loaded. This is how the obsolete Assembly.LoadWithPartialName can be avoided.")]
        private static Assembly LoadAssembly(AssemblyName assemblyName, ResolveAssemblyOptions options)
        {
            static Assembly TryLoad(AssemblyName asmName, out Exception error)
            {
                error = null;

                try
                {
                    return Assembly.Load(asmName);
                }
                catch (IOException io) // including FileNotFoundException and FileLoadException
                {
                    error = io;
                    return null;
                }
            }

#if NETFRAMEWORK
            // 1. In case of a system assembly, returning it from the GAC (if version does not matter)
            if ((options & ResolveAssemblyOptions.AllowPartialMatch) != ResolveAssemblyOptions.None)
            {
                string gacPath = Fusion.GetGacPath(assemblyName.Name);
                if (gacPath != null)
                    return Assembly.LoadFrom(gacPath);
            }
#endif

            // 2. Trying to load the assembly with full name
            Assembly result = TryLoad(assemblyName, out Exception e);
            if (result != null)
                return result;

            // 3. Trying to load the assembly without version info
            if (assemblyName.Version != null)
            {
                var strippedName = (AssemblyName)assemblyName.Clone();
                strippedName.Version = null;
                result = TryLoad(strippedName, out var _);
                if (result != null)
                    return result;
            }

            // 4. Trying by simple name only (might not work on every platform)
            if (assemblyName.FullName != assemblyName.Name)
            {
                var strippedName = new AssemblyName(assemblyName.Name);
                result = TryLoad(strippedName, out var _);
                if (result != null)
                    return result;
            }

            if ((options & ResolveAssemblyOptions.ThrowError) != ResolveAssemblyOptions.None)
                Throw.ReflectionException(Res.ReflectionCannotLoadAssembly(assemblyName.FullName), e);
            return null;
        }

#if !NET35
        private static (string, bool) DoGetForwardedAssemblyName(Type type)
        {
            if (Attribute.GetCustomAttribute(type, typeof(TypeForwardedFromAttribute), false) is TypeForwardedFromAttribute attr)
                return (attr.AssemblyFullName, IsCoreLibAssemblyName(attr.AssemblyFullName));

            return default;
        }
#endif

        #endregion

        #endregion
    }
}
