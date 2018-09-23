#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IResXResourceContainer.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2017 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Collections.Generic;
using System.ComponentModel.Design;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents a class that can contain cached ResX resources: resource entries, meta and aliases.
    /// </summary>
    internal interface IResXResourceContainer
    {
        #region Properties

        ICollection<KeyValuePair<string, ResXDataNode>> Resources { get; }

        ICollection<KeyValuePair<string, ResXDataNode>> Metadata { get; }

        ICollection<KeyValuePair<string, string>> Aliases { get; }

        bool SafeMode { get; }

        bool AutoFreeXmlData { get; }

        ITypeResolutionService TypeResolver { get; }

        string BasePath { get; }

        int Version { get; }

        #endregion
    }
}
