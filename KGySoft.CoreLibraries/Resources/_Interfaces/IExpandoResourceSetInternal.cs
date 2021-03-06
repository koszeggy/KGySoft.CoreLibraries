﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IExpandoResourceSetInternal.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2017 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents a class that can hold replaceable resources
    /// </summary>
    internal interface IExpandoResourceSetInternal
    {
        #region Methods

        object? GetResource(string name, bool ignoreCase, bool isString, bool asSafe, bool cloneValue);

        object? GetMeta(string name, bool ignoreCase, bool isString, bool asSafe, bool cloneValue);

        #endregion
    }
}
