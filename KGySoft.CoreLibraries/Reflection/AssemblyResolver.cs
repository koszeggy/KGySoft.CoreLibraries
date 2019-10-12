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

        #region Public Methods

#if !NET35
        [SecuritySafeCritical]
#endif
        internal static Assembly ResolveAssembly(string assemblyName, bool throwError, bool tryToLoad, bool matchBySimpleName)
        {
            if (assemblyName == null)
                throw new ArgumentNullException(nameof(assemblyName), Res.ArgumentNull);
            if (assemblyName.Length == 0)
                throw new ArgumentException(Res.ArgumentEmpty, nameof(assemblyName));

            string key = (matchBySimpleName ? "-" : "+") + assemblyName;
            if (AssemblyCache.TryGetValue(key, out Assembly result))
                return result;

            // 1.) Iterating through loaded assemblies, checking names
            AssemblyName asmName = new AssemblyName(assemblyName);
            string fullName = asmName.FullName;
            string simpleName = asmName.Name;
            foreach (Assembly asm in Reflector.GetLoadedAssemblies())
            {
                // Simple match. As asmName is parsed, for fully qualified names this will work for sure.
                if (asm.FullName == fullName)
                {
                    result = asm;
                    break;
                }

                AssemblyName nameToCheck = asm.GetName();
                if (nameToCheck.Name != simpleName)
                    continue;

                if (matchBySimpleName)
                {
                    result = asm;
                    break;
                }

                Version version;
                if ((version = asmName.Version) != null && nameToCheck.Version != version)
                    continue;

#if NET35 || NET40
                if (asmName.CultureInfo != null && asmName.CultureInfo.Name != nameToCheck.CultureInfo.Name)
                    continue;
#else
                if (asmName.CultureName != null && nameToCheck.CultureName != asmName.CultureName)
                    continue;
#endif
                byte[] publicKeyTokenRef, publicKeyTokenCheck;
                if ((publicKeyTokenRef = asmName.GetPublicKeyToken()) != null && (publicKeyTokenCheck = nameToCheck.GetPublicKeyToken()) != null
                    && publicKeyTokenRef.SequenceEqual(publicKeyTokenCheck))
                    continue;

                result = asm;
                break;
            }

            // 2.) Trying to load the assembly
            if (result == null && tryToLoad)
            {
                try
                {
                    result = matchBySimpleName ? LoadAssemblyWithPartialName(asmName) : Assembly.Load(asmName);
                }
                catch (Exception e) when (!e.IsCritical())
                {
                    if (throwError)
                        throw new ReflectionException(Res.ReflectionCannotLoadAssembly(assemblyName), e);
                    return null;
                }
            }

            if (result == null && throwError)
                throw new ReflectionException(Res.ReflectionCannotLoadAssembly(assemblyName));

            if (result != null)
                assemblyCache[key] = result;

            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads the assembly with partial name. It is needed because Assembly.LoadWithPartialName is obsolete.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        [SecurityCritical]
        [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", MessageId = "System.Reflection.Assembly.LoadFrom",
                Justification = "The way it is used ensures that only GAC assemblies are loaded. This is how the obsolete Assembly.LoadWithPartialName can be avoided.")]
        private static Assembly LoadAssemblyWithPartialName(AssemblyName assemblyName)
        {
#if NETFRAMEWORK
            // 1. In case of a system assembly, returning it from the GAC
            string gacPath = Fusion.GetGacPath(assemblyName.Name);
            if (gacPath != null)
                return Assembly.LoadFrom(gacPath);
#endif


            // 2. Non-GAC assembly: Trying to load the assembly with full name first.
            try
            {
                Assembly result = Assembly.Load(assemblyName);
                if (result != null)
                    return result;
            }
            catch (IOException)
            {
                // if version is set, we have a second try
                if (assemblyName.Version == null)
                    throw;
            }

            // 3. Trying to load the assembly without version info
            if (assemblyName.Version != null)
            {
                assemblyName = (AssemblyName)assemblyName.Clone();
                assemblyName.Version = null;
                return Assembly.Load(assemblyName);
            }

            return null;
        }

        #endregion

        #endregion
    }
}
