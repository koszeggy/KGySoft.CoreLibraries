﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICanUndoInternal.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.ComponentModel
{
    internal interface ICanUndoInternal
    {
        #region Methods

        void SuspendUndo();
        void ResumeUndo();
        void ClearUndoHistory();

        #endregion
    }
}
