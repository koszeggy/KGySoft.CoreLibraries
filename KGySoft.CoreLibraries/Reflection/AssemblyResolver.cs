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
        #region Fields

        private static LockingDictionary<string, Assembly> assemblyCache;

        #endregion

        #region Properties

        private static LockingDictionary<string, Assembly> AssemblyCache
            => assemblyCache ??= new Cache<string, Assembly>().AsThreadSafe();

        #endregion

        #region Methods

        #region Internal Methods

        internal static Assembly ResolveAssembly(string assemblyName, ResolveAssemblyOptions options)
        {
            if (assemblyName == null)
                throw new ArgumentNullException(nameof(assemblyName), Res.ArgumentNull);
            if (assemblyName.Length == 0)
                throw new ArgumentException(Res.ArgumentEmpty, nameof(assemblyName));
            if (!options.AllFlagsDefined())
                throw new ArgumentOutOfRangeException(nameof(options), Res.FlagsEnumOutOfRange(options));

            AssemblyName asmName;
            try
            {
                asmName = new AssemblyName(assemblyName);
            }
            catch (Exception e) when (!e.IsCritical())
            {
                if ((options & ResolveAssemblyOptions.ThrowError) != ResolveAssemblyOptions.None)
                    throw new ArgumentException(Res.ReflectionInvalidAssemblyName(assemblyName), nameof(assemblyName), e);
                return null;
            }

            return GetOrResolve(asmName, options);
        }

        internal static Assembly ResolveAssembly(AssemblyName assemblyName, ResolveAssemblyOptions options)
        {
            if (assemblyName == null)
                throw new ArgumentNullException(nameof(assemblyName), Res.ArgumentNull);
            if (!options.AllFlagsDefined())
                throw new ArgumentOutOfRangeException(nameof(options), Res.FlagsEnumOutOfRange(options));
            return GetOrResolve(assemblyName, options);
        }

        #endregion

        #region Private Methods

        private static Assembly GetOrResolve(AssemblyName assemblyName, ResolveAssemblyOptions options)
        {
            string key = ((int)options).ToString(CultureInfo.InvariantCulture) + assemblyName.FullName;
            if (AssemblyCache.TryGetValue(key, out Assembly result))
                return result;

            try
            {
                result = Resolve(assemblyName, options);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is ReflectionException))
            {
                if ((options & ResolveAssemblyOptions.ThrowError) != ResolveAssemblyOptions.None)
                    throw new ReflectionException(Res.ReflectionCannotResolveAssembly(assemblyName.FullName));
                return null;
            }

            if (result == null)
            {
                if ((options & ResolveAssemblyOptions.ThrowError) != ResolveAssemblyOptions.None)
                    throw new ReflectionException(Res.ReflectionCannotResolveAssembly(assemblyName.FullName));
                return null;
            }

            assemblyCache[key] = result;
            return result;
        }

        private static Assembly Resolve(AssemblyName assemblyName, ResolveAssemblyOptions options)
        {
            #region Local Methods

            static bool IdentityMatches(AssemblyName refName, Assembly asm, ResolveAssemblyOptions o)
            {
                if (asm == null)
                    return false;

                AssemblyName toCheck = asm.GetName();

                // Different name: skip
                if (toCheck.Name != refName.Name)
                    return false;

                // Here name matches. In case of partial match we are done.
                if ((o & ResolveAssemblyOptions.AllowPartialMatch) != ResolveAssemblyOptions.None)
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
                    || !publicKeyTokenRef.SequenceEqual(publicKeyTokenCheck);
            }

            #endregion

            // 1.) Iterating through loaded assemblies, checking names
            string fullName = assemblyName.FullName;
            foreach (Assembly asm in Reflector.GetLoadedAssemblies())
            {
                // Simple match. As fullName is via property, for fully qualified names this will work for sure.
                if (asm.FullName == fullName)
                    return asm;

                // Otherwise, we check the provided information (we still can accept simple name if nothing else is provided)
                if (IdentityMatches(assemblyName, asm, options))
                    return asm;
            }

            if ((options & ResolveAssemblyOptions.TryToLoadAssembly) == ResolveAssemblyOptions.None)
                return null;

            // 2.) Trying to load the assembly
            Assembly result = LoadAssembly(assemblyName, (options & ResolveAssemblyOptions.ThrowError) != ResolveAssemblyOptions.None);
            return result?.FullName == fullName || IdentityMatches(assemblyName, result, options) ? result : null;
        }

        /// <summary>
        /// Loads the assembly with partial name. It is needed because Assembly.LoadWithPartialName is obsolete.
        /// </summary>
#if NETFRAMEWORK && !NET35
        [SecuritySafeCritical]
#endif
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom",
            Justification = "The way it is used ensures that only GAC assemblies are loaded. This is how the obsolete Assembly.LoadWithPartialName can be avoided.")]
        private static Assembly LoadAssembly(AssemblyName assemblyName, bool throwError)
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
            // 1. In case of a system assembly, returning it from the GAC
            string gacPath = Fusion.GetGacPath(assemblyName.Name);
            if (gacPath != null)
                return Assembly.LoadFrom(gacPath);
#endif

            // 2. Non-GAC assembly: Trying to load the assembly with full name first.
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

            if (!throwError)
                return null;
            throw new ReflectionException(Res.ReflectionCannotLoadAssembly(assemblyName.FullName), e);
        }

        #endregion

        #endregion
    }
}
