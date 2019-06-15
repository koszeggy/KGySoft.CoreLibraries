#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PrimeHelper.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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

namespace KGySoft.Collections
{
    internal static class PrimeHelper
    {
        #region Constants

        private const int maxPrime = 0x7FEFFFFD;

        #endregion

        #region Fields

        /// <summary>
        /// Contains nearest primes for 2 and 10 powers can be used for typical cache capacities
        /// </summary>
        private static readonly int[] primes =
        {
            2, 5, 11, 17, 37, 67, 101, 131, 257, 521, 1009, 1031, 2053, 4099, 8209, 10007, 16411, 32771, 65537,
            100003, 131101, 262147, 524309, 1000003, 1048583, 2097169, 4194319, 8388617, 10000019, 16777259,
            33554467, 67108879, 100000007, 134217757, 268435459, 536870923, 1000000007, 1073741827
        };

        #endregion

        #region Methods

        #region Internal Methods

        internal static int GetPrime(int min)
        {
            if (min >= maxPrime)
                return maxPrime;

            for (int i = 0; i < primes.Length; i++)
            {
                int prime = primes[i];
                if (prime < min)
                    continue;

                // Returning stored prime if it is close enough to the desired value. By using powers of 2 or 10
                // this will be always true (the largest difference is 43 for 2^24).
                if (prime - min < 100)
                    return prime;
                break;
            }

            // Outside of predefined values or difference is too big: brute force
            return GetNextPrime(min);
        }

        #endregion

        #region Private Methods

        private static int GetNextPrime(int min)
        {
            for (int i = min | 1; i < Int32.MaxValue; i += 2)
            {
                if (IsPrime(i))
                    return i;
            }

            // no larger prime in Int32 range than min
            return min;
        }

        /// <summary>
        /// Determines whether the specified n is prime using the Miller-Rabin test.
        /// See https://www.geeksforgeeks.org/primality-test-set-3-miller-rabin/
        /// </summary>
        private static bool IsPrime(int n)
        {
            // handling cases for 1, 2, 3, 5, 7 and even numbers
            if (n < 2)
                return false;
            if (n == 2 || n == 3 || n == 5 || n == 7)
                return true;
            if ((n & 1) == 0)
                return false;

            // n-1 = d * 2^r
            int d = n - 1;
            int r = 1;
            while ((d & 1) == 0)
            {
                r++;
                d >>= 1;
            }

            // for Int32 range a = 2, 7, 61 are always enough
            if (!MillerTest(2, r, d, n))
                return false;
            if (n < 2047)
                return true;
            return MillerTest(7, r, d, n) && MillerTest(61, r, d, n);
        }

        private static bool MillerTest(int a, int r, int d, int n)
        {
            var x = Power(a, d, n);
            if (x == 1 || x == n - 1)
                return true;

            while (r > 1)
            {
                x = Power(x, 2, n);
                if (x == 1)
                    return false;
                if (x == n - 1)
                    return true;
                r--;
            }

            return false;
        }

        /// <summary>
        /// Returns (x^y) % p
        /// </summary>
        private static int Power(int x, int y, int p)
        {
            long result = 1;
            while (y > 0)
            {
                // If y is odd, multiply x with result
                if ((y & 1) == 1)
                    result = result * x % p;

                // y must be even now
                x = x * x % p;
                y >>= 1;
            }

            return (int)result;
        }

        #endregion

        #endregion
    }
}
