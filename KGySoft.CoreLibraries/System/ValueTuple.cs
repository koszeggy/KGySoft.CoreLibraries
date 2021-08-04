#if NET35 || NET40 || NET45
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable FieldCanBeMadeReadOnly.Global
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace System
{
    internal struct ValueTuple
    {
        internal static int CombineHashCodes(int h1, int h2)
        {
            uint num = (uint)((h1 << 5) | (h1 >> 27));
            return ((int)num + h1) ^ h2;
        }

        internal static int CombineHashCodes(int h1, int h2, int h3) => CombineHashCodes(CombineHashCodes(h1, h2), h3);
    }

    [Serializable]
    internal struct ValueTuple<T1, T2> : IEquatable<ValueTuple<T1, T2>>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }

        public bool Equals(ValueTuple<T1, T2> other) => EqualityComparer<T1>.Default.Equals(Item1, other.Item1) && EqualityComparer<T2>.Default.Equals(Item2, other.Item2);
        public override bool Equals(object obj) => obj is ValueTuple<T1, T2> tuple && Equals(tuple);
        public override int GetHashCode() => ValueTuple.CombineHashCodes(EqualityComparer<T1>.Default.GetHashCode(Item1), EqualityComparer<T2>.Default.GetHashCode(Item2));
        public override string ToString() => $"({Item1}, {Item2})";
        public static bool operator ==(ValueTuple<T1, T2> left, ValueTuple<T1, T2> right) => left.Equals(right);
        public static bool operator !=(ValueTuple<T1, T2> left, ValueTuple<T1, T2> right) => !left.Equals(right);
    }

    [Serializable]
    internal struct ValueTuple<T1, T2, T3> : IEquatable<ValueTuple<T1, T2, T3>>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;

        public ValueTuple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }

        public bool Equals(ValueTuple<T1, T2, T3> other) 
            => EqualityComparer<T1>.Default.Equals(Item1, other.Item1) 
                && EqualityComparer<T2>.Default.Equals(Item2, other.Item2)
                && EqualityComparer<T3>.Default.Equals(Item3, other.Item3);

        public override bool Equals(object obj) => obj is ValueTuple<T1, T2, T3> tuple && Equals(tuple);

        public override int GetHashCode()
            => ValueTuple.CombineHashCodes(EqualityComparer<T1>.Default.GetHashCode(Item1),
                EqualityComparer<T2>.Default.GetHashCode(Item2),
                EqualityComparer<T3>.Default.GetHashCode(Item3));

        public override string ToString() => $"({Item1}, {Item2}, {Item3})";

        public static bool operator ==(ValueTuple<T1, T2, T3> left, ValueTuple<T1, T2, T3> right) => left.Equals(right);
        public static bool operator !=(ValueTuple<T1, T2, T3> left, ValueTuple<T1, T2, T3> right) => !left.Equals(right);
    }
}
#endif
