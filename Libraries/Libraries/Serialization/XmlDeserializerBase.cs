using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;

namespace KGySoft.Libraries.Serialization
{
    internal abstract class XmlDeserializerBase : XmlSerializationBase
    {
        protected static void CheckArray(Array array, int length, int[] lengths, int[] lowerBounds)
        {
            if (length != array.Length)
                throw new ArgumentException(Res.Get(Res.XmlArraySizeMismatch, array.GetType(), length));

            if (lengths != null)
            {
                if (lengths.Length != array.Rank)
                    throw new ArgumentException(Res.Get(Res.XmlArrayRankMismatch, array.GetType(), lengths.Length));

                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] != array.GetLength(i))
                        throw new ArgumentException(Res.Get(Res.XmlArrayDimensionSizeMismatch, array.GetType(), i));

                    if (lowerBounds[i] != array.GetLowerBound(i))
                        throw new ArgumentException(Res.Get(Res.XmlArrayLowerBoundMismatch, array.GetType(), i));
                }
            }
        }

        protected static void AdjustInitializerCollection(ref object initializerCollection, Type collectionRealType, ConstructorInfo collectionCtor)
        {
            // Reverse for Stack
            if (typeof(Stack).IsAssignableFrom(collectionRealType) || collectionRealType.IsImplementationOfGenericType(typeof(Stack<>))
#if !NET35
                || collectionRealType.IsImplementationOfGenericType(typeof(ConcurrentStack<>))
#endif
            )
            {
                IList list = (IList)initializerCollection;
                int length = list.Count;
                int to = length / 2;
                for (int i = 0; i < to; i++)
                {
                    object temp = list[i];
                    list[i] = list[length - i - 1];
                    list[length - i - 1] = temp;
                }
            }

            // ToArray for array ctor parameter
            if (collectionCtor.GetParameters()[0].ParameterType.IsArray)
                initializerCollection = Reflector.RunMethod(initializerCollection, initializerCollection.GetType().GetMethod(nameof(List<_>.ToArray)));
        }

        protected static string Unescape(string s)
        {
            StringBuilder result = new StringBuilder(s);

            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] == '\\')
                {
                    if (i + 1 == result.Length)
                        throw new ArgumentException(Res.Get(Res.XmlInvalidEscapedContent, s));

                    // escaped backslash
                    if (result[i + 1] == '\\')
                    {
                        result.Remove(i, 1);
                    }
                    // escaped character
                    else
                    {
                        if (i + 4 >= result.Length)
                            throw new ArgumentException(Res.Get(Res.XmlInvalidEscapedContent, s));

                        string escapedChar = result.ToString(i + 1, 4);
                        ushort charValue;
                        if (!UInt16.TryParse(escapedChar, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out charValue))
                            throw new ArgumentException(Res.Get(Res.XmlInvalidEscapedContent, s));

                        result.Replace("\\" + escapedChar, ((char)charValue).ToString(null), i, 5);
                    }
                }
            }

            return result.ToString();
        }

    }
}
