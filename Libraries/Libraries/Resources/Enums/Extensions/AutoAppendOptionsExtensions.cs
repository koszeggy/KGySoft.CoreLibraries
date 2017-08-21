#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AutoAppendOptionsExtensions.cs
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

using System;

#endregion

namespace KGySoft.Libraries.Resources
{
    internal static class AutoAppendOptionsExtensions
    {
        #region Methods

        internal static void CheckOptions(this AutoAppendOptions value)
        {
            // if there is unknown flag except 3 and 6
            if (!(value & ~((AutoAppendOptions)(1 << 3) | (AutoAppendOptions)(1 << 6))).AllFlagsDefined()
                // or flag 3 is on but any neutral is off
                || ((value & (AutoAppendOptions)(1 << 3)) != 0) && (value & AutoAppendOptions.AppendNeutralCultures) != AutoAppendOptions.AppendNeutralCultures
                // or flag 6 is on but any specific is off
                || ((value & (AutoAppendOptions)(1 << 6)) != 0) && (value & AutoAppendOptions.AppendSpecificCultures) != AutoAppendOptions.AppendSpecificCultures)
            {
                throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
            }
        }

        internal static bool IsWidening(this AutoAppendOptions current, AutoAppendOptions newOptions)
        {
            if (current == newOptions)
                return false;

            for (var i = AutoAppendOptions.AppendFirstNeutralCulture; i < AutoAppendOptions.AppendOnLoad; i = (AutoAppendOptions)((int)i << 1))
            {
                if ((current & i) == 0 && (newOptions & i) != 0)
                    return true;
            }

            return false;
        }

        #endregion
    }
}
