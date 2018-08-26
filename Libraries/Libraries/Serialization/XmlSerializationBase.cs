using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KGySoft.Libraries.Annotations;

namespace KGySoft.Libraries.Serialization
{
    internal abstract class XmlSerializationBase
    {
        protected static Type GetElementType([NoEnumeration]IEnumerable collection)
        {
            Type type = collection.GetType();
            if (type.IsArray)
                return type.GetElementType();
            if (type == typeof(BitArray))
                return typeof(bool);
            foreach (Type i in type.GetInterfaces())
            {
                if (i.IsGenericTypeOf(typeof(ICollection<>)))
                    return i.GetGenericArguments()[0];
            }
            if (collection is IDictionary)
                return typeof(DictionaryEntry);
            return typeof(object);
        }

    }
}
