#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResourceSetRetrieval.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents the retrieval behavior of an <see cref="IExpandoResourceSet"/> in <see cref="IExpandoResourceManager.GetExpandoResourceSet">IExpandoResourceManager.GetExpandoResourceSet</see> method.
    /// </summary>
    public enum ResourceSetRetrieval
    {
        /// <summary>
        /// The <see cref="IExpandoResourceSet"/> will be returned only if it is already loaded; otherwise, no resource set will be retrieved.
        /// </summary>
        GetIfAlreadyLoaded,

        /// <summary>
        /// The <see cref="IExpandoResourceSet"/> will be returned if the corresponding resource file exists and can be loaded.
        /// </summary>
        LoadIfExists,

        /// <summary>
        /// An <see cref="IExpandoResourceSet"/> will be created for the requested culture even if no corresponding file can be loaded.
        /// </summary>
        CreateIfNotExists
    }
}
