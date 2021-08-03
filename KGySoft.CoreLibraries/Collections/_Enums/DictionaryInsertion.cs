#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DictionaryInsertion.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.Collections
{
    internal enum DictionaryInsertion
    {
        OverwriteIfExists,
        DoNotOverwrite,
        ThrowIfExists
    }
}