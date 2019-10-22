#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TestSerializationBinder.cs
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
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

using KGySoft.Reflection;
using KGySoft.Serialization;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries
{
    internal class TestSerializationBinder : SerializationBinder, ISerializationBinder
    {
        #region Methods

        #region Methods

#if !NET35
        override
#endif
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            Assert.IsNotNull(serializedType.FullName);
            assemblyName = "rev_" + new string(serializedType.Assembly.FullName.Reverse().ToArray());
            typeName = "rev_" + new string(serializedType.FullName.Reverse().ToArray());
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            Assert.IsTrue(assemblyName.StartsWith("rev_", StringComparison.Ordinal));
            Assert.IsTrue(typeName.StartsWith("rev_", StringComparison.Ordinal));
            //if (assemblyName.StartsWith("rev_", StringComparison.Ordinal))
                assemblyName = new string(assemblyName.Substring(4).Reverse().ToArray());
            //if (typeName.StartsWith("rev_", StringComparison.Ordinal))
                typeName = new string(typeName.Substring(4).Reverse().ToArray());

            Assembly assembly = assemblyName.Length == 0 ? null : Reflector.GetLoadedAssemblies().FirstOrDefault(asm => asm.FullName == assemblyName);
            if (assembly == null && assemblyName.Length > 0)
                return null;

            return assembly == null ? Reflector.ResolveType(typeName) : Reflector.ResolveType(assembly, typeName);
        }

        #endregion

        #endregion
    }
}
