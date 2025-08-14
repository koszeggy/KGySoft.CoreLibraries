﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: LanguageSettingsSignal.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

namespace KGySoft.Resources
{
    internal enum LanguageSettingsSignal
    {
        EnsureResourcesGenerated,
        SavePendingResources,
        SavePendingResourcesCompatible,
        SavePendingResourcesNonCompatible,
        ReleaseAllResourceSets,
        EnsureInvariantResourcesMerged
    }
}