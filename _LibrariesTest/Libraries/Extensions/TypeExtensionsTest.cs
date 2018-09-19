using System;
using System.Collections;
#if !NET35
using System.Collections.Concurrent;
# endif
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
#if !NET35
using System.Threading.Tasks;
#endif
using KGySoft.Libraries;
using KGySoft.Libraries.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Libraries.Extensions
{
    [TestClass]
    public class TypeExtensionsTest
    {
        [TestMethod]
        public void IsSupportedCollectionForReflectionTest()
        {
            void Test<T>(bool expectedResult, bool expectedDefaultCtor, Type expectedCollCtorParam, Type expectedElementType, bool expectedIsDictionary)
            {
                bool result = typeof(T).IsSupportedCollectionForReflection(out ConstructorInfo defCtor, out ConstructorInfo collCtor, out Type elementType, out bool isDictionary);
                Console.WriteLine($"{typeof(T)} is {(result ? String.Empty : "NOT ")}supported.");
                if (result)
                {
                    Console.WriteLine($"  By default ctor: {defCtor != null}");
                    Console.WriteLine($"  By collection ctor: {collCtor?.GetParameters()[0]?.ParameterType.ToString() ?? Boolean.FalseString }");
                    Console.WriteLine($"  Element type: {elementType}");
                    Console.WriteLine($"  Dictionary: {isDictionary}");
                }

                Assert.AreEqual(expectedResult, result);
                Assert.AreEqual(expectedDefaultCtor, defCtor != null);
                Assert.AreEqual(expectedCollCtorParam, collCtor?.GetParameters()[0]?.ParameterType);
                Assert.AreEqual(expectedElementType, elementType);
                Assert.AreEqual(expectedIsDictionary, isDictionary);
            }

            Test<object>(false, false, null, null, false);
            Test<string>(true, false, typeof(char[]), typeof(char), false);
            Test<byte[]>(false, false, null, typeof(byte), false);
            Test<List<byte>>(true, true, typeof(IEnumerable<byte>), typeof(byte), false);
            Test<Dictionary<int, string>>(true, true, typeof(IDictionary<int, string>), typeof(KeyValuePair<int, string>), true);
            Test<ArrayList>(true, true, typeof(ICollection), typeof(object), false);
            Test<Hashtable>(true, true, typeof(IDictionary), typeof(DictionaryEntry), true);
            Test<Queue<int>>(true, false, typeof(IEnumerable<int>), typeof(int), false);
            Test<Queue>(true, false, typeof(ICollection), typeof(object), false);
            Test<BitArray>(true, false, typeof(bool[]), typeof(bool), false);
            Test<StringDictionary>(false, false, null, null, false);
            Test<HybridDictionary>(true, true, null, typeof(DictionaryEntry), true);
            Test<ListDictionary>(true, true, null, typeof(DictionaryEntry), true);
            Test<OrderedDictionary>(true, true, null, typeof(DictionaryEntry), true);
            Test<Collection<int>>(true, true, typeof(IList<int>), typeof(int), false);
            Test<ReadOnlyCollection<int>>(true, false, typeof(IList<int>), typeof(int), false);
            Test<HashSet<int>>(true, true, typeof(IEnumerable<int>), typeof(int), false);
            Test<SortedList<int, string>>(true, true, typeof(IDictionary<int, string>), typeof(KeyValuePair<int, string>), true);
            Test<Cache<int, string>>(true, true, null, typeof(KeyValuePair<int, string>), true);
            Test<ArraySegment<int>>(true, false, typeof(int[]), typeof(int), false);
#if !NET35
            Test<ConcurrentDictionary<int, string>>(true, true, typeof(IEnumerable<KeyValuePair<int, string>>), typeof(KeyValuePair<int, string>), true);
            Test<ConcurrentQueue<int>>(true, false, typeof(IEnumerable<int>), typeof(int), false);
#endif
        }
    }
}
