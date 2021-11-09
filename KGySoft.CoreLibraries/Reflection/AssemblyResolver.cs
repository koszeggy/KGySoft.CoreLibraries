#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AssemblyResolver.cs
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
#if !NET35
using System.Runtime.CompilerServices;
#endif
using System.Security;
using System.Threading;

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

        private static Assembly? mscorlibAssembly;
        private static IThreadSafeCacheAccessor<(string, int), Assembly?>? assemblyCache; // Key: Assembly name + options (as int due to faster GetHashCode)

#if !NET35
        private static IThreadSafeCacheAccessor<Type, (string?, bool)>? forwardedNamesCache; // Value: Forwarded assembly name + is core identity flag
#endif

        private static readonly string[] coreLibNames =
        {
            CoreLibrariesAssembly.FullName!.Split(new[] { ',' }, 2)[0], // could be by GetName but that requires FileIOPermission
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

        private static IThreadSafeCacheAccessor<(string, int), Assembly?> AssemblyCache
        {
            get
            {
                if (assemblyCache == null)
                    Interlocked.CompareExchange(ref assemblyCache, ThreadSafeCacheFactory.Create<(string, int), Assembly?>(TryResolveAssembly, LockFreeCacheOptions.Profile128), null);
                return assemblyCache;
            }
        }

#if !NET35
        private static IThreadSafeCacheAccessor<Type, (string? ForwardedAssemblyName, bool IsCoreIdentity)> ForwardedNamesCache
        {
            get
            {
                if (forwardedNamesCache == null)
                    Interlocked.CompareExchange(ref forwardedNamesCache, ThreadSafeCacheFactory.Create<Type, (string?, bool)>(DoGetForwardedAssemblyName, LockFreeCacheOptions.Profile128), null);
                return forwardedNamesCache;
            }
        }
#endif

        #endregion

        #endregion

        #region Methods

        #region Internal Methods

        internal static Assembly? ResolveAssembly(string assemblyName, ResolveAssemblyOptions options)
        {
            if (assemblyName == null!)
                Throw.ArgumentNullException(Argument.assemblyName);
            if (assemblyName.Length == 0)
                Throw.ArgumentException(Argument.assemblyName, Res.ArgumentEmpty);
            if (!options.AllFlagsDefined())
                Throw.FlagsEnumArgumentOutOfRange(Argument.options, options);

            return AssemblyCache[(assemblyName, (int)options)];
        }

        internal static Assembly? ResolveAssembly(AssemblyName assemblyName, ResolveAssemblyOptions options)
        {
            if (assemblyName == null!)
                Throw.ArgumentNullException(Argument.assemblyName);
            if (!options.AllFlagsDefined())
                Throw.FlagsEnumArgumentOutOfRange(Argument.options, options);
            return AssemblyCache[(assemblyName.FullName, (int)options)];
        }

        internal static bool IdentityMatches(AssemblyName refName, AssemblyName? toCheck, bool allowPartialMatch)
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
            Version? version = refName.Version;
            if (version != null && toCheck.Version != version)
                return false;

            CultureInfo? culture = refName.CultureInfo;
            if (culture != null && toCheck.CultureInfo?.Name != culture.Name)
                return false;

            byte[]? publicKeyTokenRef, publicKeyTokenCheck;
            return (publicKeyTokenRef = refName.GetPublicKeyToken()) == null
                || (publicKeyTokenCheck = toCheck.GetPublicKeyToken()) == null
                || publicKeyTokenRef.SequenceEqual(publicKeyTokenCheck);
        }

        internal static bool IsCoreLibAssemblyName(string? assemblyName) => assemblyName != null && IsCoreLibAssemblyName(new StringSegmentInternal(assemblyName));

        internal static bool IsCoreLibAssemblyName(StringSegmentInternal assemblyName)
        {
            if (!assemblyName.TryGetNextSegment(',', out StringSegmentInternal name))
                return false;

            // ReSharper disable once LoopCanBeConvertedToQuery - performance, allocation
            // ReSharper disable once ForCanBeConvertedToForeach - performance, allocation
            for (var i = 0; i < coreLibNames.Length; i++)
            {
                string libName = coreLibNames[i];
                if (name.EqualsOrdinalIgnoreCase(libName))
                    return true;
            }

            return false;
        }

        internal static string? GetForwardedAssemblyName(Type type, bool omitIfCoreLibrary)
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

        private static Assembly? TryResolveAssembly((string AssemblyName, int Options) key, out bool storeValue)
        {
            // Note: even if the original source was an AssemblyName we resolve it from string because AssemblyName has no proper GetHashCode/Equals
            // It is not a problem as this is the item loader method of the cache so it will not be recreated most of the time.
            AssemblyName asmName;
            try
            {
                asmName = new AssemblyName(key.AssemblyName);
            }
            catch (Exception e) when (!e.IsCritical())
            {
                if ((key.Options & (int)ResolveAssemblyOptions.ThrowError) != 0)
                    Throw.ArgumentException(Argument.assemblyName, Res.ReflectionInvalidAssemblyName(key.AssemblyName), e);
                storeValue = false;
                return null;
            }

            Assembly? result;
            try
            {
                result = Resolve(asmName, (ResolveAssemblyOptions)key.Options);
            }
            catch (Exception e) when (!e.IsCriticalOr(e is ReflectionException))
            {
                if ((key.Options & (int)ResolveAssemblyOptions.ThrowError) != 0)
                    Throw.ReflectionException(Res.ReflectionCannotResolveAssembly(key.AssemblyName), e);
                storeValue = false;
                return null;
            }

            if (result == null && (key.Options & (int)ResolveAssemblyOptions.ThrowError) != 0)
                Throw.ReflectionException(Res.ReflectionCannotResolveAssembly(key.AssemblyName));

            storeValue = result != null;
            return result;
        }

        private static Assembly? Resolve(AssemblyName assemblyName, ResolveAssemblyOptions options)
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
            Assembly? result = LoadAssembly(assemblyName, options);
            return result?.FullName == fullName || IdentityMatches(assemblyName, result?.GetName(), allowPartialMatch) ? result : null;
        }

        /// <summary>
        /// Loads the assembly with partial name. It is needed because Assembly.LoadWithPartialName is obsolete.
        /// </summary>
        [SecuritySafeCritical]
        private static Assembly? LoadAssembly(AssemblyName assemblyName, ResolveAssemblyOptions options)
        {
            static Assembly? TryLoad(AssemblyName asmName, out Exception? error)
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
                string? gacPath = Fusion.GetGacPath(assemblyName.Name);
                if (gacPath != null)
                    return Assembly.LoadFrom(gacPath);
            }
#endif

            // 2. Trying to load the assembly with full name
            Assembly? result = TryLoad(assemblyName, out Exception? e);
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
                var strippedName = new AssemblyName(assemblyName.Name!);
                result = TryLoad(strippedName, out var _);
                if (result != null)
                    return result;
            }

            if ((options & ResolveAssemblyOptions.ThrowError) != ResolveAssemblyOptions.None)
                Throw.ReflectionException(Res.ReflectionCannotLoadAssembly(assemblyName.FullName), e);
            return null;
        }

#if !NET35
        private static (string?, bool) DoGetForwardedAssemblyName(Type type)
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
