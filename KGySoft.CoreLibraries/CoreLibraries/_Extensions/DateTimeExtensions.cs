#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DateTimeExtensions.cs
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

using System;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="DateTime"/> type.
    /// </summary>
    public static class DateTimeExtensions
    {
        #region Methods

        /// <summary>
        /// Converts the specified <paramref name="dateTime"/> correctly to UTC time.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> to convert.</param>
        /// <returns>A <see cref="DateTime"/> instance with <see cref="DateTimeKind.Utc"/>&#160;<see cref="DateTime.Kind"/>.</returns>
        /// <remarks>Use this method instead of <see cref="DateTime.ToUniversalTime">DateTime.ToUniversalTime</see> to make sure <see cref="DateTime"/>
        /// instances with <see cref="DateTimeKind.Unspecified"/>&#160;<see cref="DateTime.Kind"/> are not considered as local times before conversion.</remarks>
        public static DateTime AsUtc(this DateTime dateTime)
        {
            switch (dateTime.Kind)
            {
                case DateTimeKind.Local:
                    return dateTime.ToUniversalTime();
                case DateTimeKind.Unspecified:
                    return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                default:
                    return dateTime;
            }
        }

        #endregion
    }
}
