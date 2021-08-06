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
        #region Constants

        private const long unixEpochTicks = 621_355_968_000_000_000; // new DateTime(1970, 1, 1).Ticks
        private const long unixEpochMilliseconds = unixEpochTicks / TimeSpan.TicksPerMillisecond;
        private const long unixEpochSeconds = unixEpochTicks / TimeSpan.TicksPerSecond;
        private const long minUnixMilliseconds = -62_135_596_800_000; // DateTimeOffset.MinValue.ToUnixTimeMilliseconds()
        private const long maxUnixMilliseconds = 253_402_300_799_999; // DateTimeOffset.MaxValue.ToUnixTimeMilliseconds()
        private const long minUnixSeconds = -62_135_596_800;
        private const long maxUnixSeconds = 253_402_300_799;

        #endregion

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

        /// <summary>
        /// Gets the time elapsed since the Unix Epoch time (1970-01-01T00:00Z) in milliseconds, not counting leap seconds.
        /// </summary>
        /// <param name="value">A <see cref="DateTime"/> value to get the Unix time from.</param>
        /// <returns>The number of milliseconds that have elapsed since 1970-01-01T00:00Z.</returns>
        /// <remarks>
        /// <para>This method is similar to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.datetimeoffset.tounixtimemilliseconds" target="_blank">DateTimeOffset.ToUnixTimeMilliseconds</a>
        /// but is available even below .NET Framework 4.6.</para>
        /// <para>If <paramref name="value"/> is a <see cref="DateTimeKind.Local"/> time, then its <see cref="DateTime.Kind"/> is converted to UTC first.</para>
        /// <para>This method returns a negative value for times before 1970-01-01T00:00Z.</para>
        /// </remarks>
        public static long ToUnixMilliseconds(this DateTime value) => value.AsUtc().Ticks / TimeSpan.TicksPerMillisecond - unixEpochMilliseconds;

        /// <summary>
        /// Gets the time elapsed since the Unix Epoch time (1970-01-01T00:00Z) in seconds, not counting leap seconds.
        /// </summary>
        /// <param name="value">A <see cref="DateTime"/> value to get the Unix time from.</param>
        /// <returns>The number of seconds that have elapsed since 1970-01-01T00:00Z.</returns>
        /// <remarks>
        /// <para>This method is similar to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.datetimeoffset.tounixtimeseconds" target="_blank">DateTimeOffset.ToUnixTimeSeconds</a>
        /// but is available even below .NET Framework 4.6.</para>
        /// <para>If <paramref name="value"/> is a <see cref="DateTimeKind.Local"/> time, then its <see cref="DateTime.Kind"/> is converted to UTC first.</para>
        /// <para>This method returns a negative value for times before 1970-01-01T00:00Z.</para>
        /// </remarks>
        public static long ToUnixSeconds(this DateTime value) => value.AsUtc().Ticks / TimeSpan.TicksPerSecond - unixEpochSeconds;

        /// <summary>
        /// Gets a <see cref="DateTimeKind.Utc"/>&#160;<see cref="DateTime"/> from the milliseconds elapsed since the Unix Epoch time (1970-01-01T00:00Z).
        /// </summary>
        /// <param name="milliseconds">The number of milliseconds elapsed since the Unix Epoch time (1970-01-01T00:00Z), not counting leap seconds. Negative values as also allowed.</param>
        /// <returns>A <see cref="DateTimeKind.Utc"/>&#160;<see cref="DateTime"/> that represents the same moment as the specified <paramref name="milliseconds"/> parameter.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="milliseconds"/> is less than -62,135,596,800,000, or greater than 253,402,300,799,999.</exception>
        public static DateTime FromUnixMilliseconds(long milliseconds)
        {
            if (milliseconds is < minUnixMilliseconds or > maxUnixMilliseconds)
                Throw.ArgumentOutOfRangeException(nameof(milliseconds),  Res.ArgumentMustBeBetween(minUnixMilliseconds, maxUnixMilliseconds));

            return new DateTime(milliseconds * TimeSpan.TicksPerMillisecond + unixEpochTicks, DateTimeKind.Utc);
        }

        /// <summary>
        /// Gets a <see cref="DateTimeKind.Utc"/>&#160;<see cref="DateTime"/> from the seconds elapsed since the Unix Epoch time (1970-01-01T00:00Z).
        /// </summary>
        /// <param name="seconds">The number of seconds elapsed since the Unix Epoch time (1970-01-01T00:00Z), not counting leap seconds. Negative values as also allowed.</param>
        /// <returns>A <see cref="DateTimeKind.Utc"/>&#160;<see cref="DateTime"/> that represents the same moment as the specified <paramref name="seconds"/> parameter.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is less than -62,135,596,800, or greater than 253,402,300,799.</exception>
        public static DateTime FromUnixSeconds(long seconds)
        {
            if (seconds is < minUnixSeconds or > maxUnixSeconds)
                Throw.ArgumentOutOfRangeException(nameof(seconds), Res.ArgumentMustBeBetween(minUnixSeconds, maxUnixSeconds));

            return new DateTime(seconds * TimeSpan.TicksPerSecond + unixEpochTicks, DateTimeKind.Utc);
        }

        #endregion
    }
}
