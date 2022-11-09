#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: NamespaceDoc.cs
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

using System.Runtime.CompilerServices;
#if !NET35
using System.Threading.Tasks; 
#endif

using KGySoft.Collections;

#endregion

#region Suppressions

#if NET35 || NET40
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.Threading
{
    /// <summary>
    /// The <see cref="N:KGySoft.Threading"/> namespace contains the static <see cref="AsyncHelper"/> class that makes possible to implement sync and async versions of a possibly parallel operation
    /// supporting cancellation and reporting progress using a single shared implementation. One built-in example is the <see cref="ParallelHelper"/> class
    /// (which is basically a target framework independent advanced version of <see cref="Parallel.For(int,int,System.Action{int})">Parallel.For</see>)
    /// but you can find many examples in other dependent libraries such as the <a href="https://www.nuget.org/packages/KGySoft.Drawing.Core/" target="_blank">KGySoft.Drawing.Core</a> package
    /// that uses many parallel operations for image processing. Apart from these you can find here the <see cref="WaitHandleExtensions"/> and some
    /// other types related to configuring or tracking asynchronous operations.
    /// <br/>If you are looking for thread-safe collections such as the <see cref="ThreadSafeDictionary{TKey,TValue}"/>,
    /// then see the <see cref="N:KGySoft.Collections">KGySoft.Collections</see> namespace instead.
    /// </summary>
    [CompilerGenerated]
    internal static class NamespaceDoc
    {
    }
}
