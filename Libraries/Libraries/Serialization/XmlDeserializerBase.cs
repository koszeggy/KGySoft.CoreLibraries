using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        protected static void ParseArrayDimensions(string attrLength, string attrDim, out int[] lengths, out int[] lowerBounds)
        {
            if (attrLength == null && attrDim == null)
                throw new ArgumentException(Res.Get(Res.XmlArrayNoLength));

            if (attrLength != null)
            {
                lengths = new int[1];
                lowerBounds = new int[1];
                if (!Int32.TryParse(attrLength, out lengths[0]))
                    throw new ArgumentException(Res.Get(Res.XmlLengthInvalidType, attrLength));
                return;
            }

            string[] dims = attrDim.Split(',');
            lengths = new int[dims.Length];
            lowerBounds = new int[dims.Length];
            for (int i = 0; i < dims.Length; i++)
            {
                int boundSep = dims[i].IndexOf("..", StringComparison.InvariantCulture);
                if (boundSep == -1)
                {
                    lowerBounds[i] = 0;
                    lengths[i] = Int32.Parse(dims[i]);
                }
                else
                {
                    lowerBounds[i] = Int32.Parse(dims[i].Substring(0, boundSep));
                    lengths[i] = Int32.Parse(dims[i].Substring(boundSep + 2)) - lowerBounds[i] + 1;
                }
            }
        }

        protected static bool CheckArray(Array array, int[] lengths, int[] lowerBounds, bool throwError)
        {
            if (lengths.Length != array.Rank)
                return throwError ? throw new ArgumentException(Res.Get(Res.XmlArrayRankMismatch, array.GetType(), lengths.Length)) : false;

            for (int i = 0; i < lengths.Length; i++)
            {
                if (lengths[i] != array.GetLength(i))
                {
                    if (!throwError)
                        return false;
                    if (lengths[0] == 1)
                        throw new ArgumentException(Res.Get(Res.XmlArraySizeMismatch, array.GetType(), lengths[0]));
                    throw new ArgumentException(Res.Get(Res.XmlArrayDimensionSizeMismatch, array.GetType(), i));
                }

                if (lowerBounds[i] != array.GetLowerBound(i))
                    return throwError ? throw new ArgumentException(Res.Get(Res.XmlArrayLowerBoundMismatch, array.GetType(), i)) : false;
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

            // restoring fields and properties of the final collection
            foreach (KeyValuePair<MemberInfo, object> member in members)
            {
                var property = member.Key as PropertyInfo;
                var field = property != null ? null : member.Key as FieldInfo;

                // read-only property
                if (property?.CanWrite == false)
                {
                    object existingValue = Reflector.GetProperty(result, property);
                    if (property.PropertyType.IsValueType)
                    {
                        if (Equals(existingValue, member.Value))
                            continue;
                        throw new SerializationException(Res.Get(Res.XmlPropertyHasNoSetter, property.Name, collectionCtor.DeclaringType));
                    }

                    if (existingValue == null && member.Value == null)
                        continue;
                    if (member.Value == null)
                        throw new ReflectionException(Res.Get(Res.XmlPropertyHasNoSetterCantSetNull, property.Name, collectionCtor.DeclaringType));
                    if (existingValue == null)
                        throw new ReflectionException(Res.Get(Res.XmlPropertyHasNoSetterGetsNull, property.Name, collectionCtor.DeclaringType));
                    if (existingValue.GetType() != member.Value.GetType())
                        throw new ArgumentException(Res.Get(Res.XmlPropertyTypeMismatch, collectionCtor.DeclaringType, property.Name, member.Value.GetType(), existingValue.GetType()));

                    CopyContent(existingValue, member.Value);
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

        /// <summary>
        /// Restores target from source. Can be used for read-only properties when source object is already fully serialized.
        /// </summary>
        private static void CopyContent(object target, object source)
        {
            Debug.Assert(target != null && source != null && target.GetType() == source.GetType(), $"Same types are expected in {nameof(CopyContent)}.");

            // 1.) Array
            if (target is Array targetArray && source is Array sourceArray)
            {
                int[] lengths = new int[sourceArray.Rank];
                int[] lowerBounds = new int[sourceArray.Rank];
                for (int i = 0; i < sourceArray.Rank; i++)
                {
                    lengths[i] = sourceArray.GetLength(i);
                    lowerBounds[i] = sourceArray.GetLowerBound(i);
                }

                CheckArray(targetArray, lengths, lowerBounds, true);
                if (targetArray.GetType().GetElementType()?.IsPrimitive == true)
                    Buffer.BlockCopy(sourceArray, 0, targetArray, 0, Buffer.ByteLength(sourceArray));
                else if (lengths.Length == 1)
                    Array.Copy(sourceArray, targetArray, sourceArray.Length);
                else
                {
                    var indices = new ArrayIndexer(lengths, lowerBounds);
                    while (indices.MoveNext())
                        targetArray.SetValue(sourceArray.GetValue(indices.Current), indices.Current);
                }

                return;
            }

            // 2.) non-array: every fields (here we don't know how was the instance serialized but we have a deserialized source)
            for (Type t = target.GetType(); t != null; t = t.BaseType)
            {
                foreach (FieldInfo field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    Reflector.SetField(target, field, Reflector.GetField(source, field));
            }
        }

        protected static TypeConverter GetTypeConverter(MemberInfo member, Type type)
        {
            // Explicitly defined type converter if can convert from string
            Attribute[] attrs = Attribute.GetCustomAttributes(member, typeof(TypeConverterAttribute), true);
            if (attrs.Length > 0 && attrs[0] is TypeConverterAttribute convAttr
                && Reflector.ResolveType(convAttr.ConverterTypeName) is Type convType)
            {
                ConstructorInfo ctor = convType.GetConstructor(new Type[] { Reflector.Type });
                object[] ctorParams = { type };
                if (ctor == null)
                {
                    ctor = convType.GetDefaultConstructor();
                    ctorParams = Reflector.EmptyObjects;
                }

                if (ctor != null)
                    return Reflector.Construct(ctor, ctorParams) as TypeConverter;
            }

            return null;
        }

        protected static void HandleDeserializedMember(object obj, MemberInfo member, object deserializedValue, object existingValue)
        {
            // Field
            if (member is FieldInfo field)
            {
                Reflector.SetField(obj, field, deserializedValue);
                return;
            }

            var property = (PropertyInfo)member;

            // Read-only property
            if (!property.CanWrite)
            {
                if (property.PropertyType.IsValueType)
                {
                    if (Equals(existingValue, deserializedValue))
                        return;
                    throw new SerializationException(Res.Get(Res.XmlPropertyHasNoSetter, property.Name, obj.GetType()));
                }

                if (existingValue == null)
                    throw new ReflectionException(Res.Get(Res.XmlPropertyHasNoSetterGetsNull, property.Name, obj.GetType()));
                if (deserializedValue == null)
                    throw new ReflectionException(Res.Get(Res.XmlPropertyHasNoSetterCantSetNull, property.Name, obj.GetType()));
                if (existingValue.GetType() != deserializedValue.GetType())
                    throw new ArgumentException(Res.Get(Res.XmlPropertyTypeMismatch, obj.GetType(), property.Name, deserializedValue.GetType(), existingValue.GetType()));

                CopyContent(existingValue, deserializedValue);
                return;
            }

            // Read-write property
            Reflector.SetProperty(obj, property, deserializedValue);
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
