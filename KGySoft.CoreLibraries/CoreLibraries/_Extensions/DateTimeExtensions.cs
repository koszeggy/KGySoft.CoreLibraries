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
        /// Converts the specified <paramref name="dateTime"/> to UTC time. Unlike <see cref="DateTime.ToUniversalTime">DateTime.ToUniversalTime</see>, this
        /// method does not treat <see cref="DateTime"/> instances with <see cref="DateTimeKind.Unspecified"/>&#160;<see cref="DateTime.Kind"/> as local times.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> to convert.</param>
        /// <returns>A <see cref="DateTime"/> instance with <see cref="DateTimeKind.Utc"/>&#160;<see cref="DateTime.Kind"/>.</returns>
        public static DateTime AsUtc(this DateTime dateTime) => dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
        };

        /// <summary>
        /// Converts the specified <paramref name="dateTime"/> to a local time. Unlike <see cref="DateTime.ToLocalTime">DateTime.ToLocalTime</see>, this
        /// method does not treat <see cref="DateTime"/> instances with <see cref="DateTimeKind.Unspecified"/>&#160;<see cref="DateTime.Kind"/> as UTC times.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> to convert.</param>
        /// <returns>A <see cref="DateTime"/> instance with <see cref="DateTimeKind.Local"/>&#160;<see cref="DateTime.Kind"/>.</returns>
        public static DateTime AsLocal(this DateTime dateTime) => dateTime.Kind switch
        {
            DateTimeKind.Local => dateTime,
            DateTimeKind.Utc => dateTime.ToLocalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Local),
        };

        #endregion
    }
}
