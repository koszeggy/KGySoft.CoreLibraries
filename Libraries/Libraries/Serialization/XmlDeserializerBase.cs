using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;

namespace KGySoft.Libraries.Serialization
{
    internal abstract class XmlDeserializerBase
    {
        protected static bool CheckArray(Array array, int length, int[] lengths, int[] lowerBounds, bool throwError)
        {
            if (length != array.Length)
                return throwError ? throw new ArgumentException(Res.Get(Res.XmlArraySizeMismatch, array.GetType(), length)) : false;

            if (lengths != null)
            {
                if (lengths.Length != array.Rank)
                    return throwError ? throw new ArgumentException(Res.Get(Res.XmlArrayRankMismatch, array.GetType(), lengths.Length)) : false;

                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] != array.GetLength(i))
                        return throwError ? throw new ArgumentException(Res.Get(Res.XmlArrayDimensionSizeMismatch, array.GetType(), i)) : false;

                    if (lowerBounds[i] != array.GetLowerBound(i))
                        return throwError ? throw new ArgumentException(Res.Get(Res.XmlArrayLowerBoundMismatch, array.GetType(), i)) : false;
                }
            }

            return true;
        }

        protected static object CreateInitializerCollection(Type collectionElementType, bool isDictionary)
            => isDictionary
                ? (collectionElementType.IsGenericType ? Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(collectionElementType.GetGenericArguments())) : new Dictionary<object, object>())
                : Activator.CreateInstance(typeof(List<>).MakeGenericType(collectionElementType));

        protected static object CreateCollectionByInitializerCollection(ConstructorInfo collectionCtor, object initializerCollection, Dictionary<MemberInfo, object> members)
        {
            AdjustInitializerCollection(ref initializerCollection, collectionCtor);
            object result = Reflector.Construct(collectionCtor, initializerCollection);
            foreach (KeyValuePair<MemberInfo, object> member in members)
            {
                var property = member.Key as PropertyInfo;
                var field = property != null ? null : member.Key as FieldInfo;

                // read-only property
                if (!property?.CanWrite == false)
                {
                    if (property.PropertyType.IsValueType)
                        throw new SerializationException(Res.Get(Res.XmlPropertyHasNoSetter, property.Name, collectionCtor.DeclaringType));

                    object existingValue = Reflector.GetProperty(result, property);
                    if (existingValue == null && member.Value == null)
                        continue;
                    if (member.Value == null)
                        throw new ReflectionException(Res.Get(Res.XmlPropertyHasNoSetterCantSetNull, property.Name, collectionCtor.DeclaringType));
                    if (existingValue == null)
                        throw new ReflectionException(Res.Get(Res.XmlPropertyHasNoSetterGetsNull, property.Name, collectionCtor.DeclaringType));
                    if (existingValue.GetType() != member.Value.GetType())
                        throw new ArgumentException(Res.Get(Res.XmlPropertyTypeMismatch, collectionCtor.DeclaringType, property.Name, member.Value.GetType(), existingValue.GetType()));

                    RestoreReadOnlyPropertyValue(existingValue, member.Value);
                    continue;
                }

                // read-write property
                if (property != null)
                {
                    Reflector.SetProperty(result, property, member.Value);
                    continue;
                }

                // field
                Reflector.SetField(result, field, member.Value);
            }

            return result;
        }

        private static void AdjustInitializerCollection(ref object initializerCollection, ConstructorInfo collectionCtor)
        {
            Type collectionType = collectionCtor.DeclaringType;

            // Reverse for Stack
            if (typeof(Stack).IsAssignableFrom(collectionType) || collectionType.IsImplementationOfGenericType(typeof(Stack<>))
#if !NET35
                || collectionType.IsImplementationOfGenericType(typeof(ConcurrentStack<>))
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

        private static void RestoreReadOnlyPropertyValue(object target, object source)
        {
            // TODO: also properties and fields (also for populatable collections!)
            // none of them are null, types are the same
            Type createdType = property.Value?.GetType();
            Type propertyType = property.Key.PropertyType;
            if (target != null && property.Value == null)
            {
                if (propertyType.IsArray)
                    throw new ReflectionException(Res.Get(Res.XmlArrayPropertyHasNoSetterNull, propertyType, collectionRealType, property.Key.Name));
                if (propertyType.IsCollection())
                    throw new ReflectionException(Res.Get(Res.XmlCollectionPropertyHasNoSetterNull, propertyType, collectionRealType, property.Key.Name));
            }
            else if (target == null && property.Value != null)
            {
                if (propertyType.IsArray)
                    throw new ReflectionException(Res.Get(Res.XmlArrayPropertyHasNoSetter, propertyType, collectionRealType, property.Key.Name));
                if (propertyType.IsCollection())
                    throw new ReflectionException(Res.Get(Res.XmlCollectionPropertyHasNoSetter, propertyType, collectionRealType, property.Key.Name));
            }
            else
            {
                if (target == null && property.Value == null)
                    continue;
                if (propertyType != createdType)
                    throw new ArgumentException(Res.Get(Res.XmlPropertyTypeMismatch, collectionRealType, property.Key.Name, createdType, propertyType));

                // copy from existing array
                if (target is Array existingArray && property.Value is Array arrayToSet)
                {
                    int[] lengths = new int[arrayToSet.Rank];
                    int[] lowerBounds = new int[arrayToSet.Rank];
                    for (int i = 0; i < arrayToSet.Rank; i++)
                    {
                        lengths[i] = arrayToSet.GetLength(i);
                        lowerBounds[i] = arrayToSet.GetLowerBound(i);
                    }

                    CheckArray(existingArray, arrayToSet.Length, lengths, lowerBounds);
                    if (collectionElementType.IsPrimitive)
                        Buffer.BlockCopy(arrayToSet, 0, existingArray, 0, Buffer.ByteLength(arrayToSet));
                    else
                    {
                        var indices = new ArrayIndexer(lengths, lowerBounds);
                        while (indices.MoveNext())
                            existingArray.SetValue(existingArray.GetValue(indices.Current), indices.Current);
                    }

                    continue;
                }

                // copy from existing collection
                if (propertyType.IsCollection())
                {
                    if (!propertyType.IsReadWriteCollection(target))
                        throw new SerializationException(Res.Get(Res.XmlDeserializeReadOnlyCollection, propertyType));

                    IEnumerable collection = (IEnumerable)target;
                    collection.Clear();
                    foreach (var item in (IEnumerable)property.Value)
                        collection.Add(item);

                    continue;
                }

                throw new ArgumentException(Res.Get(Res.XmlDeserializeReadOnlyProperty, collectionRealType, property.Key.Name, propertyType));
            }
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
