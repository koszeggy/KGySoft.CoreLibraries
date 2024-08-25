#if NET9_0_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TypeResolver.TypeNameResolver.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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
using System.Reflection;
using System.Reflection.Metadata;

using KGySoft.Collections;
using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Reflection
{
    internal sealed partial class TypeResolver
    {
        #region TypeNameResolver class

        private sealed class TypeNameResolver
        {
            #region Fields

            private readonly TypeName rootName;
            private readonly ResolveTypeOptions options;
            private readonly CircularList<int> modifiers = new CircularList<int>();
            private readonly List<TypeNameResolver> genericArgs = new List<TypeNameResolver>();

            #endregion

            #region Constructors

            public TypeNameResolver(TypeName typeName, ResolveTypeOptions resolveTypeOptions)
            {
                if (typeName == null!)
                    Throw.ArgumentNullException(Argument.typeName);
                options = resolveTypeOptions;

                // NOTE: Normally we wouldn't need to analyze the modifiers in this depth (only the generic arguments) but we promise in the description that
                // if typeResolver is specified we guarantee that always the root types are resolved.
                rootName = typeName;
                while (true)
                {
                    if (rootName.IsArray)
                    {
                        modifiers.AddFirst(rootName.IsSZArray ? 0 : rootName.GetArrayRank());
                        rootName = rootName.GetElementType();
                        continue;
                    }

                    if (rootName.IsPointer)
                    {
                        modifiers.AddFirst(pointer);
                        rootName = rootName.GetElementType();
                        continue;
                    }

                    if (rootName.IsByRef)
                    {
                        modifiers.AddFirst(byRef);
                        rootName = rootName.GetElementType();
                        continue;
                    }

                    if (rootName.IsConstructedGenericType)
                    {
                        foreach (TypeName genericArgument in rootName.GetGenericArguments())
                            genericArgs.Add(new TypeNameResolver(genericArgument, options));

                        rootName = rootName.GetGenericTypeDefinition();
                        continue;
                    }

                    break;
                }
            }

            #endregion

            #region Methods

            #region Public Methods

            public Type? Resolve(Func<TypeName, Type?> typeResolver)
            {
                // 1. Resolving root type
                Type? result = ResolveRootType(typeResolver);
                if (result == null)
                    return null;

                // 2. Applying generic arguments
                if (genericArgs.Count > 0)
                {
                    Debug.Assert(result.IsGenericTypeDefinition, "Root type is expected to be a generic type definition");
                    Type?[] args = new Type[genericArgs.Count];
                    for (int i = 0; i < args.Length; i++)
                    {
                        args[i] = genericArgs[i].Resolve(typeResolver);
                        if (args[i] == null)
                            return null;
                    }

                    result = result.GetGenericType(args!);
                }

                // 3. Applying modifiers
                foreach (int modifier in modifiers)
                {
                    switch (modifier)
                    {
                        case 0: // zero based array
                            result = result.MakeArrayType();
                            break;
                        case byRef:
                            result = result.MakeByRefType();
                            break;
                        case pointer:
                            result = result.MakePointerType();
                            break;
                        default: // array rank
                            result = result.MakeArrayType(modifier);
                            break;
                    }
                }

                return result;
            }

            #endregion

            #region Private Methods

            private Type? ResolveRootType(Func<TypeName, Type?> typeResolver)
            {
                bool throwError = (options & ResolveTypeOptions.ThrowError) != ResolveTypeOptions.None;
                bool allowIgnoreAssembly = (options & ResolveTypeOptions.AllowIgnoreAssemblyName) != ResolveTypeOptions.None;
                bool ignoreCase = (options & ResolveTypeOptions.IgnoreCase) != ResolveTypeOptions.None;

                // 1.) By resolver
                Type? result = typeResolver.Invoke(rootName);
                if (result != null)
                    return result;

                // 2. Resolving assembly if needed
                Assembly? assembly = null;
                if (rootName.AssemblyName != null)
                {
                    var resolveAssemblyOptions = (ResolveAssemblyOptions)options & Enum<ResolveAssemblyOptions>.GetFlagsMask();
                    if ((options & ResolveTypeOptions.AllowIgnoreAssemblyName) != ResolveTypeOptions.None)
                        resolveAssemblyOptions &= ~ResolveAssemblyOptions.ThrowError;
                    assembly = AssemblyResolver.ResolveAssembly(rootName.AssemblyName, resolveAssemblyOptions);
                    if (assembly == null && (options & ResolveTypeOptions.AllowIgnoreAssemblyName) == ResolveTypeOptions.None)
                        return null;
                }

                // 3/a. Resolving the type from a specific assembly
                if (assembly != null)
                {
                    result = assembly.GetType(rootName.FullName, throwError && !allowIgnoreAssembly, ignoreCase);
                    if (result != null || !allowIgnoreAssembly)
                        return result;
                }
                // 3/b. If there is no assembly defined we try to use the mscorlib.dll in the first place, which contains forwarded types on non-framework platforms.
                else if (rootName.AssemblyName == null)
                {
                    // We are not throwing an exception from here because on failure we try all assemblies
                    result = AssemblyResolver.MscorlibAssembly.GetType(rootName.FullName, false, ignoreCase);
                    if (result != null)
                        return result;
                }

                // 3/c. Resolving the type from any assembly
                // Type.GetType is not redundant even if we tried mscorlib.dll above because it still can load core library types.
                result = Type.GetType(rootName.FullName, false, ignoreCase);
                if (result != null)
                    return result;

                // Looking for the type in the loaded assemblies
                foreach (Assembly asm in Reflector.GetLoadedAssemblies())
                {
                    result = asm.GetType(rootName.FullName, false, ignoreCase);
                    if (result != null)
                        return result;
                }

                if (throwError)
                    Throw.ReflectionException(Res.ReflectionNotAType(rootName.AssemblyQualifiedName));
                return null;
            }

            #endregion

            #endregion
        }

        #endregion
    }
}
#endif