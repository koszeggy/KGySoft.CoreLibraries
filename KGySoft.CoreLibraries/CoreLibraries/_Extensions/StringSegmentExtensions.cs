using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace KGySoft.CoreLibraries
{
    public static class StringSegmentExtensions
    {
        /// <summary>
        /// Advances the specified <paramref name="rest"/> parameter after the next whitespace character and returns
        /// the consumed part without the whitespace. If the first character of <paramref name="rest"/> was a whitespace
        /// before the call, then an empty segment is returned. If the whole string is consumed, then <paramref name="rest"/>
        /// will be <see cref="StringSegment.Null"/>.
        /// </summary>
        /// <param name="rest">The rest.</param>
        /// <returns></returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToWhiteSpace(ref this StringSegment rest)
        // TODO: handle trivial cases here (see asserts and conditions from Split)
            => StringSegment.GetNextSegment(ref rest);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, char separator)
            // TODO: handle trivial cases here (see asserts and conditions from Split)
            => StringSegment.GetNextSegment(ref rest, separator);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, StringSegment separator)
            // TODO: handle trivial cases here (see asserts and conditions from Split)
            => StringSegment.GetNextSegment(ref rest, separator);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static StringSegment ReadToSeparator(ref this StringSegment rest, string separator)
            // TODO: handle trivial cases here (see asserts and conditions from Split)
            => StringSegment.GetNextSegment(ref rest, separator);
    }
}