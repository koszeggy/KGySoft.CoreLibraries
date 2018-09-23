using System;
using System.Diagnostics;
using KGySoft.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _PerformanceTest.Libraries.Reflection;

namespace _PerformanceTest.Libraries.Collections
{
    /// <summary>
    /// In this test out-of-cache retrieval is fast to demonstrate that RemoveLeastRecentUsed behavior is just a little bit slower than RemoveOldest.
    /// See <see cref="ReflectorPerformanceTest.TestMethodInvoke"/> to test a simulation of re-using the most recent elements
    /// </summary>
    [TestClass]
    public class CachePerformanceTest
    {
        const int capacity = 10000;
        Cache<int, string> testCache;

        [TestInitialize]
        public void ResetCache()
        {
            testCache = new Cache<int, string>(i => i.ToString(), capacity);
        }

        [TestMethod]
        public void Main()
        {
            var isPrimeCache = new Cache<int, bool>(IsPrime, 2) { Behavior = CacheBehavior.RemoveOldestElement };

            Console.WriteLine("5 is prime: {0}", isPrimeCache[5]);
            Console.WriteLine("6 is prime: {0}", isPrimeCache[6]);
            Console.WriteLine("5 is prime: {0}", isPrimeCache[5]);
            Console.WriteLine("7 is prime: {0}", isPrimeCache[7]);
            Console.WriteLine("5 is prime: {0}", isPrimeCache[5]);
        }

        private static bool IsPrime(int number)
        {
            Console.WriteLine("IsPrime executed for: {0}", number);
            if (number <= 1)
                return false;
            if (number % 2 == 0)
                return true;
            int i = 3;
            int sqrt = (int)Math.Floor(Math.Sqrt(number));
            while (i <= sqrt)
            {
                if (number % i == 0)
                    return false;
                i += 2;
            }
            return true;
        }

        [TestMethod]
        public void PopulateTest()
        {
            const int iterations = 1000;
            Console.WriteLine("==========Populate (iterations: {0:N0})===========", iterations);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                testCache.Clear();
                for (int j = 0; j < capacity; j++)
                {
                    string s = testCache[j];
                }
            }
            watch.Stop();
            Console.WriteLine("Elapsed time: {0} ms", watch.ElapsedMilliseconds);
            /* Circular list:
==========Populate (iterations: 1,000)===========
Elapsed time: 4232 ms
             * CacheItem:
==========Populate (iterations: 1,000)===========
Elapsed time: 6444 ms             
             */
        }

        [TestMethod]
        public void RemoveOldestNoDeleteTest()
        {
            const int iterations = 1000000;
            Console.WriteLine("==========Remove oldest no delete (iterations: {0:N0})===========", iterations);
            testCache.Behavior = CacheBehavior.RemoveOldestElement;
            Random rnd = new Random(0);
            int range = capacity;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                string s = testCache[rnd.Next(range)];
            }
            watch.Stop();
            Console.WriteLine("Elapsed time: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine(testCache.GetStatistics());
            /* Circular list:
==========Remove oldest no delete (iterations: 1,000,000)===========
Elapsed time: 52 ms
Cache<Int32, String> cache statistics:
Count: 10000
Capacity: 10000
Number of writes: 10000
Number of reads: 1000000
Number of cache hits: 990000
Number of deletes: 0
Hit rate: 99,00 %             
             
             * CacheItem:
==========Remove oldest no delete (iterations: 1,000,000)===========
Elapsed time: 147 ms
Cache<Int32, String> cache statistics:
Count: 10000
Capacity: 10000
Number of writes: 10000
Number of reads: 1000000
Number of cache hits: 990000
Number of deletes: 0
Hit rate: 99,00 %
             */
        }

        [TestMethod]
        public void RemoveLeastRecentUsedNoDeleteTest()
        {
            const int iterations = 1000000;
            Console.WriteLine("==========Remove least recent used no delete (iterations: {0:N0})===========", iterations);
            testCache.Behavior = CacheBehavior.RemoveLeastRecentUsedElement;
            Random rnd = new Random(0);
            int range = capacity;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                string s = testCache[rnd.Next(range)];
            }
            watch.Stop();
            Console.WriteLine("Elapsed time: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine(testCache.GetStatistics());
            /* Circular list:
==========Remove least recent used no delete (iterations: 1,000,000)===========
Elapsed time: 7528 ms
Cache<Int32, String> cache statistics:
Count: 10000
Capacity: 10000
Number of writes: 10000
Number of reads: 1000000
Number of cache hits: 990000
Number of deletes: 0
Hit rate: 99,00 %             
             
             * CacheItem:
==========Remove least recent used no delete (iterations: 1,000,000)===========
Elapsed time: 289 ms
Cache<Int32, String> cache statistics:
Count: 10000
Capacity: 10000
Number of writes: 10000
Number of reads: 1000000
Number of cache hits: 990000
Number of deletes: 0
Hit rate: 99,00 %
             */
        }

        [TestMethod]
        public void RemoveOldestTest()
        {
            const int iterations = 1000000;
            Console.WriteLine("==========Remove oldest (iterations: {0:N0})===========", iterations);
            testCache.Behavior = CacheBehavior.RemoveOldestElement;
            Random rnd = new Random(0);
            int range = (int)(capacity * 1.5);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                string s = testCache[rnd.Next(range)];
            }
            watch.Stop();
            Console.WriteLine("Elapsed time: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine(testCache.GetStatistics());
            /* Circular list:
==========Remove oldest (iterations: 1,000,000)===========
Elapsed time: 309 ms
Cache<Int32, String> cache statistics:
Count: 10000
Capacity: 10000
Number of writes: 338326
Number of reads: 1000000
Number of cache hits: 661674
Number of deletes: 328326
Hit rate: 66,17 %
             * CacheItem:
==========Remove oldest (iterations: 1,000,000)===========
Elapsed time: 471 ms
Cache<Int32, String> cache statistics:
Count: 10000
Capacity: 10000
Number of writes: 338326
Number of reads: 1000000
Number of cache hits: 661674
Number of deletes: 328326
Hit rate: 66,17 %
             */
        }

        [TestMethod]
        public void RemoveLeastRecentUsed()
        {
            const int iterations = 1000000;
            Console.WriteLine("==========Remove least recent used (iterations: {0:N0})===========", iterations);
            testCache.Behavior = CacheBehavior.RemoveLeastRecentUsedElement;
            Random rnd = new Random(0);
            int range = (int)(capacity * 1.5);
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < iterations; i++)
            {
                string s = testCache[rnd.Next(range)];
            }
            watch.Stop();
            Console.WriteLine("Elapsed time: {0} ms", watch.ElapsedMilliseconds);
            Console.WriteLine(testCache.GetStatistics());
            /* Circular list:
==========Remove least recent used (iterations: 1,000,000)===========
Elapsed time: 6844 ms
Cache<Int32, String> cache statistics:
Count: 10000
Capacity: 10000
Number of writes: 338451
Number of reads: 1000000
Number of cache hits: 661549
Number of deletes: 328451
Hit rate: 66,15 %
             * CacheItem:
==========Remove least recent used (iterations: 1,000,000)===========
Elapsed time: 458 ms
Cache<Int32, String> cache statistics:
Count: 10000
Capacity: 10000
Number of writes: 338451
Number of reads: 1000000
Number of cache hits: 661549
Number of deletes: 328451
Hit rate: 66,15 %             */
        }
    }
}
