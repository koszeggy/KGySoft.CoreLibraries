#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomExtensions.cs
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
#if NETCOREAPP3_0_OR_GREATER
using System.Globalization;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.InteropServices;
#endif
#if !NET35
using System.Numerics;
#endif
using System.Security; 
using System.Text;

using KGySoft.Reflection;
using KGySoft.Security.Cryptography;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides extension methods for the <see cref="Random"/> type.
    /// <br/>See the <strong>Examples</strong> section for an example.
    /// </summary>
    /// <example>
    /// <note type="tip">Try also <a href="https://dotnetfiddle.net/EPHRIx" target="_blank">online</a>.</note>
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Collections;
    /// using System.Collections.Generic;
    /// using System.Globalization;
    /// using System.Linq;
    /// using System.Reflection;
    /// using KGySoft.CoreLibraries;
    /// 
    /// public static class Example
    /// {
    ///     public static void Main()
    ///     {
    ///         // Or FastRandom for the fastest results, or SecureRandom for cryptographically safe results.
    ///         var rnd = new Random();
    /// 
    ///         // Next... for all simple types:
    ///         Console.WriteLine(rnd.NextBoolean());
    ///         Console.WriteLine(rnd.NextDouble(Double.PositiveInfinity)); // see also the overloads
    ///         Console.WriteLine(rnd.NextString()); // see also the overloads
    ///         Console.WriteLine(rnd.NextDateTime()); // also NextDate, NextDateTimeOffset, NextTimeSpan
    ///         Console.WriteLine(rnd.NextEnum<ConsoleColor>());
    ///         // and NextByte, NextSByte, NextInt16, NextDecimal, etc.
    /// 
    ///         // NextObject: for practically anything. See also GenerateObjectSettings.
    ///         Console.WriteLine(rnd.NextObject<Person>().Dump()); // custom type
    ///         Console.WriteLine(rnd.NextObject<(int, string)>()); // tuple
    ///         Console.WriteLine(rnd.NextObject<IConvertible>()); // interface implementation
    ///         Console.WriteLine(rnd.NextObject<MarshalByRefObject>()); // abstract type implementation
    ///         Console.WriteLine(rnd.NextObject<int[]>().Dump()); // array
    ///         Console.WriteLine(rnd.NextObject<IList<IConvertible>>().Dump()); // some collection of an interface
    ///         Console.WriteLine(rnd.NextObject<Func<DateTime>>().Invoke()); // delegate with random result
    /// 
    ///         // specific type for object (useful for non-generic collections)
    ///         Console.WriteLine(rnd.NextObject<ArrayList>(new GenerateObjectSettings
    ///         {
    ///             SubstitutionForObjectType = typeof(ConsoleColor)
    ///         }).Dump());
    /// 
    ///         // literally any random object
    ///         Console.WriteLine(rnd.NextObject<object>(new GenerateObjectSettings
    ///         {
    ///             AllowDerivedTypesForNonSealedClasses = true
    ///         })/*.Dump()*/); // dump may end up in an exception for property getters or even in an endless recursion
    ///     }
    /// 
    ///     private static string Dump(this object o)
    ///     {
    ///         if (o == null)
    ///             return "<null>";
    /// 
    ///         if (o is IConvertible convertible)
    ///             return convertible.ToString(CultureInfo.InvariantCulture);
    /// 
    ///         if (o is IEnumerable enumerable)
    ///             return $"[{String.Join(", ", enumerable.Cast<object>().Select(Dump))}]";
    /// 
    ///         return $"{{{String.Join("; ", o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => $"{p.Name} = {Dump(p.GetValue(o))}"))}}}";
    ///     }
    /// }
    /// 
    /// public class Person
    /// {
    ///     public string FirstName { get; set; }
    ///     public string LastName { get; set; }
    ///     public DateTime BirthDate { get; set; }
    ///     public List<string> PhoneNumbers { get; set; }
    /// }
    /// 
    /// // A possible output of the code above can be the following:
    /// // False
    /// // 1,65543763243888E+266
    /// // }\&qc54# d
    /// // 8806. 02. 18. 6:25:21
    /// // White
    /// // {FirstName = Jemp; LastName = Aeltep; BirthDate = 07/04/2003 00:00:00; PhoneNumbers = [17251827, 7099649772]}
    /// // (1168349297, oufos)
    /// // Renegotiated
    /// // System.Net.Sockets.NetworkStream
    /// // [336221768]
    /// // [Off, Invalid]
    /// // 1956. 08. 24. 4:28:57
    /// // [Yellow, Gray]
    /// // System.Xml.XmlCharCheckingReader+<ReadElementContentAsBinHexAsync>d__40 *
    /// ]]></code>
    /// </example>
    public static partial class RandomExtensions
    {
        #region Methods

        #region Public Methods

        #region Byte Array

        /// <summary>
        /// Returns an <see cref="Array"/> of random bytes that has the specified <paramref name="length"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="length">The desired length of the result.</param>
        /// <returns>An array of random bytes that has the specified <paramref name="length"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than 0.</exception>
        public static byte[] NextBytes(this Random random, int length)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (length < 0)
                Throw.ArgumentOutOfRangeException(Argument.length, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if (length == 0)
                return Reflector.EmptyArray<byte>();

            var result = new byte[length];
            random.NextBytes(result);
            return result;
        }

        #endregion

        #region Boolean

        /// <summary>
        /// Returns a random <see cref="bool"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A <see cref="bool"/> value that is either <see langword="true"/>&#160;or <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static bool NextBoolean(this Random random)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            return (random.Next() & 1) == 0;
        }

        #endregion

        #region Integer Types

        #region SByte

        /// <summary>
        /// Returns a random <see cref="sbyte"/> that can have any value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>An 8-bit signed integer that is greater than or equal to <see cref="SByte.MinValue">SByte.MinValue</see> and less or equal to <see cref="SByte.MaxValue">SByte.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static sbyte SampleSByte(this Random random) => (sbyte)random.Next();

        /// <summary>
        /// Returns a non-negative random 8-bit signed integer that is less than <see cref="SByte.MaxValue">SByte.MaxValue</see>.
        /// To return any <see cref="sbyte"/> use the <see cref="SampleSByte">SampleSByte</see> method instead.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>An 8-bit signed integer that is greater than or equal to 0 and less than <see cref="SByte.MaxValue">SByte.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static sbyte NextSByte(this Random random) => (sbyte)random.Next(SByte.MaxValue);

        /// <summary>
        /// Returns a non-negative random 8-bit signed integer that is within the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>An 8-bit signed integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
        [CLSCompliant(false)]
        public static sbyte NextSByte(this Random random, sbyte maxValue, bool inclusiveUpperBound = false)
            => (sbyte)random.NextInt32(maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random 8-bit signed integer that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>An 8-bit signed integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        [CLSCompliant(false)]
        public static sbyte NextSByte(this Random random, sbyte minValue, sbyte maxValue, bool inclusiveUpperBound = false)
            => (sbyte)random.NextInt32(minValue, maxValue, inclusiveUpperBound);

        #endregion

        #region Byte

        /// <summary>
        /// Returns a random <see cref="byte"/> that can have any value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>An 8-bit unsigned integer that is greater than or equal to 0 and less or equal to <see cref="Byte.MaxValue">Byte.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static byte SampleByte(this Random random) => (byte)random.Next();

        /// <summary>
        /// Returns a random 8-bit unsigned integer that is less than <see cref="Byte.MaxValue">Byte.MaxValue</see>.
        /// To return any <see cref="byte"/> use the <see cref="SampleByte">SampleByte</see> method instead.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>An 8-bit unsigned integer that is greater than or equal to 0 and less than <see cref="Byte.MaxValue">Byte.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static byte NextByte(this Random random) => (byte)random.Next(Byte.MaxValue);

        /// <summary>
        /// Returns a random 8-bit unsigned integer that is within the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>An 8-bit unsigned integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static byte NextByte(this Random random, byte maxValue, bool inclusiveUpperBound = false)
            => (byte)random.NextInt32(maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random 8-bit unsigned integer that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>An 8-bit unsigned integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        public static byte NextByte(this Random random, byte minValue, byte maxValue, bool inclusiveUpperBound = false)
            => (byte)random.NextInt32(minValue, maxValue, inclusiveUpperBound);

        #endregion

        #region Int16

        /// <summary>
        /// Returns a random <see cref="short"/> that can have any value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 16-bit signed integer that is greater than or equal to <see cref="Int16.MinValue">Int16.MinValue</see> and less or equal to <see cref="Int16.MaxValue">Int16.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static short SampleInt16(this Random random) => (short)random.Next();

        /// <summary>
        /// Returns a non-negative random 16-bit signed integer that is less than <see cref="Int16.MaxValue">Int16.MaxValue</see>.
        /// To return any <see cref="short"/> use the <see cref="SampleInt16">SampleInt16</see> method instead.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 16-bit signed integer that is greater than or equal to 0 and less than <see cref="Int16.MaxValue">Int16.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static short NextInt16(this Random random) => (short)random.Next(Int16.MaxValue);

        /// <summary>
        /// Returns a non-negative random 16-bit signed integer that is within the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 16-bit signed integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
        public static short NextInt16(this Random random, short maxValue, bool inclusiveUpperBound = false)
            => (short)random.NextInt32(maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random 16-bit signed integer that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 16-bit signed integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        public static short NextInt16(this Random random, short minValue, short maxValue, bool inclusiveUpperBound = false)
            => (short)random.NextInt32(minValue, maxValue, inclusiveUpperBound);

        #endregion

        #region UInt16

        /// <summary>
        /// Returns a random <see cref="ushort"/> that can have any value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 16-bit unsigned integer that is greater than or equal to 0 and less or equal to <see cref="UInt16.MaxValue">UInt16.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static ushort SampleUInt16(this Random random) => (ushort)random.Next();

        /// <summary>
        /// Returns a random 16-bit unsigned integer that is less than <see cref="UInt16.MaxValue">UInt16.MaxValue</see>.
        /// To return any <see cref="ushort"/> use the <see cref="SampleUInt16">SampleUInt16</see> method instead.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 16-bit unsigned integer that is greater than or equal to 0 and less than <see cref="UInt16.MaxValue">UInt16.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static ushort NextUInt16(this Random random) => (ushort)random.Next(UInt16.MaxValue);

        /// <summary>
        /// Returns a random 16-bit unsigned integer that is within the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 16-bit unsigned integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static ushort NextUInt16(this Random random, ushort maxValue, bool inclusiveUpperBound = false)
            => (ushort)random.NextInt32(maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random 16-bit unsigned integer that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 16-bit unsigned integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        [CLSCompliant(false)]
        public static ushort NextUInt16(this Random random, ushort minValue, ushort maxValue, bool inclusiveUpperBound = false)
            => (ushort)random.NextInt32(minValue, maxValue, inclusiveUpperBound);

        #endregion

        #region Int32

        /// <summary>
        /// Returns a random <see cref="int"/> that can have any value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to <see cref="Int32.MinValue">Int32.MinValue</see> and less or equal to <see cref="Int32.MaxValue">Int32.MaxValue</see>.</returns>
        /// <remarks>Similarly to the <see cref="Random.Next()">Random.Next()</see> method this one returns an <see cref="int"/> value; however, the result can be negative and
        /// the maximum possible value can be <see cref="Int32.MaxValue">Int32.MaxValue</see>.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static int SampleInt32(this Random random)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            return (int)GenerateSampleUInt32(random);
        }

        /// <summary>
        /// Returns a non-negative random 32-bit signed integer that is less than <see cref="Int32.MaxValue">Int32.MaxValue</see>.
        /// To return any <see cref="int"/> use the <see cref="SampleInt32">SampleInt32</see> method instead.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="Int32.MaxValue">Int32.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static int NextInt32(this Random random) => random.Next();

        /// <summary>
        /// Returns a non-negative random 32-bit signed integer that is within the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static int NextInt32(this Random random, int maxValue, bool inclusiveUpperBound = false)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (maxValue < 0)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0));

            return !inclusiveUpperBound ? random.Next(maxValue)
                : maxValue == Int32.MaxValue ? random.Next(-1, maxValue) + 1
                : random.Next(maxValue + 1);
        }

        /// <summary>
        /// Returns a random 32-bit signed integer that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static int NextInt32(this Random random, int minValue, int maxValue, bool inclusiveUpperBound = false)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            return !inclusiveUpperBound ? random.Next(minValue, maxValue)
                : maxValue != Int32.MaxValue ? random.Next(minValue, maxValue + 1)
                : minValue != Int32.MinValue ? random.Next(minValue - 1, maxValue) + 1
                : (int)GenerateSampleUInt32(random);
        }

        #endregion

        #region UInt32

        /// <summary>
        /// Returns a random <see cref="uint"/> that can have any value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 32-bit unsigned integer that is greater than or equal to 0 and less or equal to <see cref="UInt32.MaxValue">UInt32.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static uint SampleUInt32(this Random random)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            return GenerateSampleUInt32(random);
        }

        /// <summary>
        /// Returns a random 32-bit unsigned integer that is less than <see cref="UInt32.MaxValue">UInt32.MaxValue</see>.
        /// To return any <see cref="uint"/> use the <see cref="SampleUInt32">SampleUInt32</see> method instead.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 32-bit unsigned integer that is greater than or equal to 0 and less than <see cref="UInt32.MaxValue">UInt32.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static uint NextUInt32(this Random random)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

            return (uint)(random.Next(Int32.MinValue, Int32.MaxValue) + Int32.MinValue);
        }

        /// <summary>
        /// Returns a random 32-bit unsigned integer that is within the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 32-bit unsigned integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static uint NextUInt32(this Random random, uint maxValue, bool inclusiveUpperBound = false)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

            if (inclusiveUpperBound)
            {
                if (maxValue == UInt32.MaxValue)
                    return GenerateSampleUInt32(random);
                maxValue += 1;
            }

            if (maxValue <= Int32.MaxValue)
                return (uint)random.Next((int)maxValue);
            int low = (int)unchecked(Int32.MaxValue - maxValue);
            return (uint)(random.Next(low, Int32.MaxValue) - low);
        }

        /// <summary>
        /// Returns a random 32-bit unsigned integer that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 32-bit unsigned integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        [CLSCompliant(false)]
        public static uint NextUInt32(this Random random, uint minValue, uint maxValue, bool inclusiveUpperBound = false)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            uint range = maxValue - minValue;

            if (inclusiveUpperBound)
            {
                if (range == UInt32.MaxValue)
                    return GenerateSampleUInt32(random);
                range += 1;
            }

            if (range <= Int32.MaxValue)
                return (uint)random.Next((int)range) + minValue;
            int low = (int)unchecked(Int32.MaxValue - range);
            return (uint)(random.Next(low, Int32.MaxValue) - low) + minValue;
        }

        #endregion

        #region Int64

        /// <summary>
        /// Returns a random <see cref="long"/> that can have any value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to <see cref="Int64.MinValue">Int64.MinValue</see> and less or equal to <see cref="Int64.MaxValue">Int64.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static long SampleInt64(this Random random)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            return (long)GenerateSampleUInt64(random);
        }

        /// <summary>
        /// Returns a non-negative random 64-bit signed integer that is less than <see cref="Int64.MaxValue">Int64.MaxValue</see>.
        /// To return any <see cref="long"/> use the <see cref="SampleInt64">SampleInt64</see> method instead.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0 and less than <see cref="Int64.MaxValue">Int64.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static long NextInt64(this Random random)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

#if NET6_0_OR_GREATER
            return random.NextInt64();
#else
            long result;
            do
            {
                result = (long)GenerateSampleUInt64(random) & Int64.MaxValue;
            } while (result == Int64.MaxValue);

            return result;
#endif
        }

        /// <summary>
        /// Returns a non-negative random 64-bit signed integer that is within the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
        public static long NextInt64(this Random random, long maxValue, bool inclusiveUpperBound = false)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (maxValue < 0L)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0));

#if NET6_0_OR_GREATER
            return !inclusiveUpperBound ? random.NextInt64(maxValue)
                : maxValue == Int64.MaxValue ? random.NextInt64(-1, maxValue) + 1
                : random.NextInt64(maxValue + 1);
#else
            // fallback for 32-bit range
            if (maxValue <= UInt32.MaxValue)
                return random.NextUInt32((uint)maxValue, inclusiveUpperBound);

            if (inclusiveUpperBound)
            {
                if (maxValue == Int64.MaxValue)
                    return (long)GenerateSampleUInt64(random) & Int64.MaxValue;
                maxValue += 1;
            }

            ulong mask = ((ulong)maxValue).GetBitMask();
            ulong result;
            do
                result = GenerateSampleUInt64(random) & mask;
            while (result >= (ulong)maxValue);

            return (long)result;
#endif
        }

        /// <summary>
        /// Returns a random <see cref="long"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 64-bit signed integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        public static long NextInt64(this Random random, long minValue, long maxValue, bool inclusiveUpperBound = false)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

#if NET6_0_OR_GREATER
            return !inclusiveUpperBound ? random.NextInt64(minValue, maxValue)
                : maxValue != Int64.MaxValue ? random.NextInt64(minValue, maxValue + 1)
                : minValue != Int64.MinValue ? random.NextInt64(minValue - 1, maxValue) + 1
                : (long)GenerateSampleUInt64(random);
#else
            ulong range = (ulong)(maxValue - minValue);

            // fallback for 32-bit range
            if (range <= UInt32.MaxValue)
                return random.NextUInt32((uint)range, inclusiveUpperBound) + minValue;

            if (inclusiveUpperBound)
            {
                if (range == UInt64.MaxValue)
                    return (long)GenerateSampleUInt64(random);
                range += 1;
            }

            ulong mask = range.GetBitMask();
            ulong result;
            do
                result = GenerateSampleUInt64(random) & mask;
            while (result >= range);

            return (long)result + minValue;
#endif
        }

        #endregion

        #region UInt64

        /// <summary>
        /// Returns a random <see cref="ulong"/> that can have any value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 64-bit unsigned integer that is greater than or equal to 0 and less or equal to <see cref="UInt64.MaxValue">UInt64.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static ulong SampleUInt64(this Random random)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            return GenerateSampleUInt64(random);
        }

        /// <summary>
        /// Returns a random 64-bit unsigned integer that is less than <see cref="UInt64.MaxValue">UInt64.MaxValue</see>.
        /// To return any <see cref="ulong"/> use the <see cref="SampleUInt64">SampleUInt64</see> method instead.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 64-bit unsigned integer that is greater than or equal to 0 and less than <see cref="UInt64.MaxValue">UInt64.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static ulong NextUInt64(this Random random)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

#if NET6_0_OR_GREATER
            return (ulong)(random.NextInt64(Int64.MinValue, Int64.MaxValue) + Int64.MinValue);
#else
            ulong result;
            do
            {
                result = GenerateSampleUInt64(random);
            } while (result == UInt64.MaxValue);

            return result;
#endif
        }

        /// <summary>
        /// Returns a random 64-bit unsigned integer that is within the specified maximum.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 64-bit unsigned integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static ulong NextUInt64(this Random random, ulong maxValue, bool inclusiveUpperBound = false)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

#if NET6_0_OR_GREATER
            if (inclusiveUpperBound)
            {
                if (maxValue == UInt64.MaxValue)
                    return GenerateSampleUInt64(random);
                maxValue += 1;
            }

            if (maxValue <= Int64.MaxValue)
                return (ulong)random.NextInt64((long)maxValue);
            long low = (long)unchecked(Int64.MaxValue - maxValue);
            return (ulong)(random.NextInt64(low, Int64.MaxValue) - low);
#else
            // fallback for 32-bit range
            if (maxValue <= UInt32.MaxValue)
                return random.NextUInt32((uint)maxValue, inclusiveUpperBound);

            if (inclusiveUpperBound)
            {
                if (maxValue == UInt64.MaxValue)
                    return GenerateSampleUInt64(random);
                maxValue += 1;
            }

            ulong mask = maxValue.GetBitMask();
            ulong result;
            do
                result = GenerateSampleUInt64(random) & mask;
            while (result >= maxValue);

            return result;
#endif
        }

        /// <summary>
        /// Returns a random 64-bit unsigned integer that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 64-bit unsigned integer that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        [CLSCompliant(false)]
        public static ulong NextUInt64(this Random random, ulong minValue, ulong maxValue, bool inclusiveUpperBound = false)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            ulong range = maxValue - minValue;

#if NET6_0_OR_GREATER
            if (inclusiveUpperBound)
            {
                if (range == UInt64.MaxValue)
                    return GenerateSampleUInt64(random);
                range += 1;
            }

            if (range <= Int64.MaxValue)
                return (ulong)random.NextInt64((long)range) + minValue;
            long low = (long)unchecked(Int64.MaxValue - range);
            return (ulong)(random.NextInt64(low, Int64.MaxValue) - low) + minValue;
#else
            // fallback for 32-bit range
            if (range <= UInt32.MaxValue)
                return random.NextUInt32((uint)range, inclusiveUpperBound) + minValue;

            if (inclusiveUpperBound)
            {
                if (range == UInt64.MaxValue)
                    return GenerateSampleUInt64(random);
                range += 1;
            }

            ulong mask = range.GetBitMask();
            ulong result;
            do
                result = GenerateSampleUInt64(random) & mask;
            while (result >= range);

            return result + minValue;
#endif
        }

        #endregion

        #region BigInteger
#if !NET35

        /// <summary>
        /// Returns a random <see cref="BigInteger"/> that represents an integer of <paramref name="byteSize"/> bytes.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="byteSize">Determines the range of the generated value in bytes. For example, if this parameter is <c>1</c>, then
        /// the result will be between 0 and 255 if <paramref name="isSigned"/> is <see langword="false"/>,
        /// or between -128 and 127 if <paramref name="isSigned"/> is <see langword="true"/>.</param>
        /// <param name="isSigned"><see langword="true"/>&#160;to generate a signed result; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A random <see cref="BigInteger"/> that represents an integer of <paramref name="byteSize"/> bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteSize"/> is negative.</exception>
        /// <exception cref="OverflowException"><paramref name="byteSize"/> is too large.</exception>
        /// <exception cref="OutOfMemoryException"><paramref name="byteSize"/> is too large.</exception>
        public static BigInteger SampleBigInteger(this Random random, int byteSize, bool isSigned = false)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (byteSize <= 0)
            {
                if (byteSize == 0)
                    return BigInteger.Zero;
                Throw.ArgumentOutOfRangeException(nameof(byteSize), Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            }

            // If an unsigned result is requested, then adding one extra byte to ensure that our result is not interpreted as two's complement.
            // Using uint just to prevent using a negative size (and throwing OverflowException instead) when byteSize is Int32.MaxValue
            var bytes = new byte[(uint)byteSize + (isSigned ? 0U : 1U)];
            random.NextBytes(bytes);

            // clearing the extra added byte if needed
            if (!isSigned)
                bytes[byteSize] = 0;

            return new BigInteger(bytes);
        }

        /// <summary>
        /// Returns a non-negative random <see cref="BigInteger"/> that is within the specified maximum.
        /// To generate a random n-byte integer use the <see cref="SampleBigInteger">SampleBigInteger</see> method instead.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A <see cref="BigInteger"/> that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
        public static BigInteger NextBigInteger(this Random random, BigInteger maxValue, bool inclusiveUpperBound = false)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (maxValue < BigInteger.Zero)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0));

            if (inclusiveUpperBound)
                maxValue += 1;
            if (maxValue.IsZero || maxValue.IsOne)
                return BigInteger.Zero;

            return DoGenerateBigInteger(random, maxValue);
        }

        /// <summary>
        /// Returns a random <see cref="BigInteger"/> value that is within a specified range.
        /// To generate a random n-byte integer use the <see cref="SampleBigInteger">SampleBigInteger</see> method instead.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A <see cref="BigInteger"/> that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="maxValue"/> is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        public static BigInteger NextBigInteger(this Random random, BigInteger minValue, BigInteger maxValue, bool inclusiveUpperBound = false)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (maxValue <= minValue)
            {
                if (minValue == maxValue)
                    return minValue;
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);
            }

            if (minValue.IsZero)
                return random.NextBigInteger(maxValue, inclusiveUpperBound);

            BigInteger range = maxValue - minValue;
            if (inclusiveUpperBound)
                range += 1;

            if (range.IsZero || range.IsOne)
                return minValue;

            return DoGenerateBigInteger(random, range) + minValue;
        }

#endif
        #endregion

        #endregion

        #region Floating-point Types

        #region Half
#if NET5_0_OR_GREATER

        /// <summary>
        /// Returns a random <see cref="Half"/> value that is greater than or equal to 0.0 and less than 1.0.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A half-precision floating point number that is greater than or equal to 0.0 and less than 1.0.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static Half NextHalf(this Random random)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

#if NET6_0_OR_GREATER
            return (Half)random.NextSingle();
#else
            return (Half)random.NextDouble();
#endif
        }

        /// <summary>
        /// Returns a non-negative random <see cref="Half"/> value that is less or equal to the specified <paramref name="maxValue"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A half-precision floating point number that is greater than or equal to 0.0 and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases.
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.</para>
        /// <para>If <paramref name="scale"/> is <see cref="FloatScale.Auto"/>, then always the <see cref="FloatScale.ForceLinear"/> option is used.</para>
        /// <para>Generating random numbers by this method on the logarithmic scale is about 3 times slower than on the linear scale.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.0
        /// <br/>-or-.
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static Half NextHalf(this Random random, Half maxValue, FloatScale scale = FloatScale.Auto)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if ((uint)scale > (uint)FloatScale.ForceLogarithmic)
                Throw.EnumArgumentOutOfRange(Argument.scale, scale);
            if (maxValue <= (Half)0f)
            {
                if (maxValue < (Half)0f)
                    Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo((Half)0f));
                return (Half)0f;
            }

            if (Half.IsNaN(maxValue))
                Throw.ArgumentOutOfRangeException(Argument.maxValue);

            return (Half)DoGetNextDouble(random, Half.IsPositiveInfinity(maxValue) ? (double)Half.MaxValue : (double)maxValue, scale == FloatScale.Auto ? FloatScale.ForceLinear : scale);
        }

        /// <summary>
        /// Returns a random <see cref="Half"/> value that is within a specified range.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A half-precision floating point number that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases such as
        /// when <paramref name="minValue"/> is equal to <paramref name="maxValue"/> or when integer parts of both limits are beyond the precision of the <see cref="Half"/> type.
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.</para>
        /// <para>If <paramref name="scale"/> is <see cref="FloatScale.Auto"/>, then the <see cref="FloatScale.ForceLinear"/> option is used.</para>
        /// <para>Generating random numbers by this method on the logarithmic scale is about 3 times slower than on the linear scale.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>
        /// <br/>-or-
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static Half NextHalf(this Random random, Half minValue, Half maxValue, FloatScale scale = FloatScale.Auto)
        {
            static Half AdjustValue(Half value) => Half.IsNegativeInfinity(value) ? Half.MinValue : (Half.IsPositiveInfinity(value) ? Half.MaxValue : value);

            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (Half.IsPositiveInfinity(minValue) && Half.IsPositiveInfinity(maxValue) || Half.IsNegativeInfinity(minValue) && Half.IsNegativeInfinity(maxValue))
                Throw.ArgumentOutOfRangeException(Argument.minValue);
            if (Half.IsNaN(minValue) || Half.IsNaN(maxValue))
                Throw.ArgumentOutOfRangeException(Half.IsNaN(minValue) ? Argument.minValue : Argument.maxValue);
            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);
            if ((uint)scale > (uint)FloatScale.ForceLogarithmic)
                Throw.EnumArgumentOutOfRange(Argument.scale, scale);

            return (Half)DoGetNextDouble(random, (double)AdjustValue(minValue), (double)AdjustValue(maxValue), scale == FloatScale.Auto ? FloatScale.ForceLinear : scale);
        }

#endif
        #endregion

        #region Single

        /// <summary>
        /// Returns a random <see cref="float"/> value that is greater than or equal to 0.0 and less than 1.0.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A single-precision floating point number that is greater than or equal to 0.0 and less than 1.0.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static float NextSingle(this Random random)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

#if NET6_0_OR_GREATER
            return random.NextSingle();
#else
            return (float)random.NextDouble();
#endif
        }

        /// <summary>
        /// Returns a non-negative random <see cref="float"/> value that is less or equal to the specified <paramref name="maxValue"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A single-precision floating point number that is greater than or equal to 0.0 and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases.
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.</para>
        /// <para>If <paramref name="scale"/> is <see cref="FloatScale.Auto"/>, then the <see cref="FloatScale.ForceLinear"/> option is used
        /// if <paramref name="maxValue"/> is less than or equal to 65535. For larger range the <see cref="FloatScale.ForceLogarithmic"/> option is used.</para>
        /// <para>Generating random numbers by this method on the logarithmic scale is about 3 times slower than on the linear scale.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.0
        /// <br/>-or-.
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static float NextSingle(this Random random, float maxValue, FloatScale scale = FloatScale.Auto)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if ((uint)scale > (uint)FloatScale.ForceLogarithmic)
                Throw.EnumArgumentOutOfRange(Argument.scale, scale);
            if (maxValue <= 0f)
            {
                if (maxValue < 0f)
                    Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0f));
                return 0f;
            }

            if (Single.IsNaN(maxValue))
                Throw.ArgumentOutOfRangeException(Argument.maxValue);

            return (float)DoGetNextDouble(random, Single.IsPositiveInfinity(maxValue) ? Single.MaxValue : maxValue, scale);
        }

        /// <summary>
        /// Returns a random <see cref="float"/> value that is within a specified range.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A single-precision floating point number that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases such as
        /// when <paramref name="minValue"/> is equal to <paramref name="maxValue"/> or when integer parts of both limits are beyond the precision of the <see cref="float"/> type.
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.</para>
        /// <para>If <paramref name="scale"/> is <see cref="FloatScale.Auto"/>, then the <see cref="FloatScale.ForceLinear"/> option is used
        /// if the absolute value of both <paramref name="minValue"/> and <paramref name="maxValue"/> are less than or equal to 65535, or when they have the same sign
        /// and the absolute value of <paramref name="maxValue"/> is less than 16 times greater than the absolute value of <paramref name="minValue"/>.
        /// For larger ranges the <see cref="FloatScale.ForceLogarithmic"/> option is used.</para>
        /// <para>Generating random numbers by this method on the logarithmic scale is about 3 times slower than on the linear scale.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>
        /// <br/>-or-
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static float NextSingle(this Random random, float minValue, float maxValue, FloatScale scale = FloatScale.Auto)
        {
            static float AdjustValue(float value) => Single.IsNegativeInfinity(value) ? Single.MinValue : (Single.IsPositiveInfinity(value) ? Single.MaxValue : value);

            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (Single.IsPositiveInfinity(minValue) && Single.IsPositiveInfinity(maxValue) || Single.IsNegativeInfinity(minValue) && Single.IsNegativeInfinity(maxValue))
                Throw.ArgumentOutOfRangeException(Argument.minValue);
            if (Single.IsNaN(minValue) || Single.IsNaN(maxValue))
                Throw.ArgumentOutOfRangeException(Single.IsNaN(minValue) ? Argument.minValue : Argument.maxValue);
            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);
            if ((uint)scale > (uint)FloatScale.ForceLogarithmic)
                Throw.EnumArgumentOutOfRange(Argument.scale, scale);

            return (float)DoGetNextDouble(random, AdjustValue(minValue), AdjustValue(maxValue), scale);
        }

        #endregion

        #region Double

        /// <summary>
        /// Returns a non-negative random <see cref="double"/> value that is less or equal to the specified <paramref name="maxValue"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0 and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases.
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.</para>
        /// <para>If <paramref name="scale"/> is <see cref="FloatScale.Auto"/>, then the <see cref="FloatScale.ForceLinear"/> option is used
        /// if <paramref name="maxValue"/> is less than or equal to 65535. For larger range the <see cref="FloatScale.ForceLogarithmic"/> option is used.</para>
        /// <para>Generating random numbers by this method on the logarithmic scale is about 3 times slower than on the linear scale.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.0
        /// <br/>-or-
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static double NextDouble(this Random random, double maxValue, FloatScale scale = FloatScale.Auto)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if ((uint)scale > (uint)FloatScale.ForceLogarithmic)
                Throw.EnumArgumentOutOfRange(Argument.scale, scale);
            if (maxValue <= 0d)
            {
                if (maxValue < 0d)
                    Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0d));
                return 0d;
            }

            if (Double.IsNaN(maxValue))
                Throw.ArgumentOutOfRangeException(Argument.maxValue);

            return DoGetNextDouble(random, Double.IsPositiveInfinity(maxValue) ? Double.MaxValue : maxValue, scale);
        }

        /// <summary>
        /// Returns a random <see cref="double"/> value that is within a specified range.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A double-precision floating point number that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases such as
        /// when <paramref name="minValue"/> is equal to <paramref name="maxValue"/> or when integer parts of both limits are beyond the precision of the <see cref="double"/> type.
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.</para>
        /// <para>If <paramref name="scale"/> is <see cref="FloatScale.Auto"/>, then the <see cref="FloatScale.ForceLinear"/> option is used
        /// if the absolute value of both <paramref name="minValue"/> and <paramref name="maxValue"/> are less than or equal to 65535, or when they have the same sign
        /// and the absolute value of <paramref name="maxValue"/> is less than 16 times greater than the absolute value of <paramref name="minValue"/>.
        /// For larger ranges the <see cref="FloatScale.ForceLogarithmic"/> option is used.</para>
        /// <para>Generating random numbers by this method on the logarithmic scale is about 3 times slower than on the linear scale.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>
        /// <br/>-or-
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static double NextDouble(this Random random, double minValue, double maxValue, FloatScale scale = FloatScale.Auto)
        {
            static double AdjustValue(double value) => Double.IsNegativeInfinity(value) ? Double.MinValue : (Double.IsPositiveInfinity(value) ? Double.MaxValue : value);

            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (Double.IsPositiveInfinity(minValue) && Double.IsPositiveInfinity(maxValue) || Double.IsNegativeInfinity(minValue) && Double.IsNegativeInfinity(maxValue))
                Throw.ArgumentOutOfRangeException(Argument.minValue);
            if (Double.IsNaN(minValue) || Double.IsNaN(maxValue))
                Throw.ArgumentOutOfRangeException(Double.IsNaN(minValue) ? Argument.minValue : Argument.maxValue);
            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);
            if ((uint)scale > (uint)FloatScale.ForceLogarithmic)
                Throw.EnumArgumentOutOfRange(Argument.scale, scale);

            return DoGetNextDouble(random, AdjustValue(minValue), AdjustValue(maxValue), scale);
        }

        #endregion

        #region Decimal

        /// <summary>
        /// Returns a random <see cref="decimal"/> value that is greater than or equal to 0.0 and less than 1.0.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A decimal floating point number that is greater than or equal to 0.0 and less than 1.0.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static decimal NextDecimal(this Random random)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

            decimal result;
            do
            {
                // The hi argument of 0.9999999999999999999999999999m is 542101086.
                // (MaxInt, MaxInt, 542101086) is actually bigger than 1 but in practice the loop almost never repeats.
                result = new Decimal((int)GenerateSampleUInt32(random), (int)GenerateSampleUInt32(random), random.Next(542101087), false, 28);
            } while (result >= 1m);

            return result;
        }

        /// <summary>
        /// Returns a non-negative random <see cref="decimal"/> value that is less or equal to the specified <paramref name="maxValue"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A decimal floating point number that is greater than or equal to 0.0 and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases,
        /// such as when <paramref name="maxValue"/> is near <see cref="DecimalExtensions.Epsilon"/>.</para>
        /// <para>If <paramref name="scale"/> is <see cref="FloatScale.Auto"/>, then the <see cref="FloatScale.ForceLinear"/> option is used
        /// if <paramref name="maxValue"/> is less than or equal to 65535. For larger range the <see cref="FloatScale.ForceLogarithmic"/> option is used.</para>
        /// <para>Generating random numbers by this method on the logarithmic scale is about 25-50 times slower than on the linear scale.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.0
        /// <br/>-or-
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static decimal NextDecimal(this Random random, decimal maxValue, FloatScale scale = FloatScale.Auto)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if ((uint)scale > (uint)FloatScale.ForceLogarithmic)
                Throw.EnumArgumentOutOfRange(Argument.scale, scale);
            if (maxValue <= 0m)
            {
                if (maxValue < 0m)
                    Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.ArgumentMustBeGreaterThanOrEqualTo(0d));
                return 0m;
            }

            // if linear scaling is forced...
            if (scale == FloatScale.ForceLinear
                // or we use auto scaling and maximum is UInt16
                || (scale == FloatScale.Auto && maxValue <= UInt16.MaxValue))
            {
                return random.NextDecimal() * maxValue;
            }

            // We don't generate too small exponents for big ranges because
            // that would cause too many almost zero results
            decimal minExponent = -5m;
            decimal maxExponent = maxValue.Log10();

            // We decrease exponents only if the given range is already small.
            if (maxExponent < minExponent)
                minExponent = maxExponent - 4;

            decimal result = 10m.Pow(NextDecimalLinear(random, minExponent, maxExponent));

            // protecting ourselves against inaccurate calculations; however, in practice result is always in range.
            return result > maxValue ? maxValue : result;
        }

        /// <summary>
        /// Returns a random <see cref="decimal"/> value that is within a specified range.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A decimal floating point number that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases such as
        /// when <paramref name="minValue"/> is equal to <paramref name="maxValue"/> or when the range is near <see cref="DecimalExtensions.Epsilon"/>.
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.</para>
        /// <para>If <paramref name="scale"/> is <see cref="FloatScale.Auto"/>, then the <see cref="FloatScale.ForceLinear"/> option is used
        /// if the absolute value of both <paramref name="minValue"/> and <paramref name="maxValue"/> are less than or equal to 65535, or when they have the same sign
        /// and the absolute value of <paramref name="maxValue"/> is less than 16 times greater than the absolute value of <paramref name="minValue"/>.
        /// For larger ranges the <see cref="FloatScale.ForceLogarithmic"/> option is used.</para>
        /// <para>Generating random numbers by this method on the logarithmic scale is about 25-50 times slower than on the linear scale.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>
        /// <br/>-or-
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static decimal NextDecimal(this Random random, decimal minValue, decimal maxValue, FloatScale scale = FloatScale.Auto)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            if ((uint)scale > (uint)FloatScale.ForceLogarithmic)
                Throw.EnumArgumentOutOfRange(Argument.scale, scale);

            if (minValue == maxValue)
                return minValue;

            bool posAndNeg = minValue < 0m && maxValue > 0m;
            decimal minAbs = Math.Min(Math.Abs(minValue), Math.Abs(maxValue));
            decimal maxAbs = Math.Max(Math.Abs(minValue), Math.Abs(maxValue));

            // if linear scaling is forced...
            if (scale == FloatScale.ForceLinear
                // or we use auto scaling and maximum is UInt16 or when the difference of order of magnitude is smaller than 4
                || (scale == FloatScale.Auto && (maxAbs <= UInt16.MaxValue || !posAndNeg && maxAbs / 16m < minAbs)))
            {
                return NextDecimalLinear(random, minValue, maxValue);
            }

            int sign;
            if (!posAndNeg)
                sign = minValue < 0m ? -1 : 1;
            else
            {
                // if both negative and positive results are expected we select the sign based on the size of the ranges
                decimal sample = (decimal)random.NextDouble(); // NextDecimal is almost always slower
                decimal rate = minAbs / maxAbs;
                decimal absMinValue = Math.Abs(minValue);
                bool isNeg = absMinValue <= maxValue
                    ? rate / 2m > sample
                    : rate / 2m < sample;
                sign = isNeg ? -1 : 1;

                // now adjusting the limits for 0..[selected range]
                minAbs = 0m;
                maxAbs = isNeg ? absMinValue : Math.Abs(maxValue);
            }

            // We don't generate too small exponents for big ranges because
            // that would cause too many almost zero results
            decimal minExponent = minAbs == 0m ? -5m : minAbs.Log10();
            decimal maxExponent = maxAbs.Log10();
            if (minExponent.Equals(maxExponent))
                return minValue;

            // We decrease exponents only if the given range is already small.
            if (maxExponent < minExponent)
                minExponent = maxExponent - 4;

            decimal result = sign * 10m.Pow(NextDecimalLinear(random, minExponent, maxExponent));

            // protecting ourselves against inaccurate calculations; however, in practice result is always in range.
            return result < minValue ? minValue : (result > maxValue ? maxValue : result);
        }

        #endregion

        #endregion

        #region Chars/Runes and Strings

        #region Chars

        /// <summary>
        /// Returns a random <see cref="char"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random character returned. This parameter is optional.
        /// <br/>Default value: <see cref="Char.MinValue">Char.MinValue</see>.</param>
        /// <param name="maxValue">The inclusive upper bound of the random character returned. Must be greater or equal to <paramref name="minValue"/>. This parameter is optional.
        /// <br/>Default value: <see cref="Char.MaxValue">Char.MaxValue</see>.</param>
        /// <returns>A <see cref="char"/> value that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        public static char NextChar(this Random random, char minValue = Char.MinValue, char maxValue = Char.MaxValue)
            => minValue == Char.MinValue && maxValue == Char.MaxValue
                ? (char)random.SampleUInt16()
                : (char)random.NextUInt16(minValue, maxValue, true);

        /// <summary>
        /// Returns an <see cref="Array"/> of random characters that has the specified <paramref name="length"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="length">The desired length of the result.</param>
        /// <param name="allowedCharacters">An string containing the allowed characters. Recurring characters may appear in the result more frequently than others.</param>
        /// <returns>An array of random characters that has the specified <paramref name="length"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="allowedCharacters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="allowedCharacters"/> is empty.</exception>
        [SecuritySafeCritical]
        public static unsafe char[] NextChars(this Random random, int length, string allowedCharacters)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (length < 0)
                Throw.ArgumentOutOfRangeException(Argument.length, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if (allowedCharacters == null!)
                Throw.ArgumentNullException(Argument.allowedCharacters);
            if (allowedCharacters.Length == 0)
                Throw.ArgumentException(Argument.allowedCharacters, Res.ArgumentEmpty);

            if (length == 0)
                return Reflector.EmptyArray<char>();

            var result = new char[length];
            fixed (char* s = result)
            fixed (char* set = allowedCharacters)
                FillChars(random, new MutableString(s, length), new MutableString(set, allowedCharacters.Length));
            return result;
        }

        /// <summary>
        /// Returns an <see cref="Array"/> of random characters that has the specified <paramref name="length"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="length">The desired length of the result.</param>
        /// <param name="allowedCharacters">An array of the allowed characters. Recurring characters may appear in the result more frequently than others.</param>
        /// <returns>An array of random characters that has the specified <paramref name="length"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="allowedCharacters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="allowedCharacters"/> is empty.</exception>
        [SecuritySafeCritical]
        public static unsafe char[] NextChars(this Random random, int length, char[] allowedCharacters)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (length < 0)
                Throw.ArgumentOutOfRangeException(Argument.length, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if (allowedCharacters == null!)
                Throw.ArgumentNullException(Argument.allowedCharacters);
            if (allowedCharacters.Length == 0)
                Throw.ArgumentException(Argument.allowedCharacters, Res.ArgumentEmpty);

            if (length == 0)
                return Reflector.EmptyArray<char>();

            var result = new char[length];
            fixed (char* s = result)
            fixed (char* set = allowedCharacters)
                FillChars(random, new MutableString(s, length), new MutableString(set, allowedCharacters.Length));
            return result;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Returns an <see cref="Array"/> of random characters that has the specified <paramref name="length"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="length">The desired length of the result.</param>
        /// <param name="allowedCharacters">A <see cref="ReadOnlySpan{T}"/> containing the allowed characters. Recurring characters may appear in the result more frequently than others.</param>
        /// <returns>An array of random characters that has the specified <paramref name="length"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than 0.</exception>
        /// <exception cref="ArgumentException"><paramref name="allowedCharacters"/> is empty.</exception>
        [SecuritySafeCritical]
        public static unsafe char[] NextChars(this Random random, int length, ReadOnlySpan<char> allowedCharacters)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (length < 0)
                Throw.ArgumentOutOfRangeException(Argument.length, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if (allowedCharacters.Length == 0)
                Throw.ArgumentException(Argument.allowedCharacters, Res.ArgumentEmpty);

            if (length == 0)
                return Reflector.EmptyArray<char>();

            var result = new char[length];
            fixed (char* s = result)
            fixed (char* set = allowedCharacters)
                FillChars(random, new MutableString(s, length), new MutableString(set, allowedCharacters.Length));
            return result;
        }
#endif

        /// <summary>
        /// Returns an <see cref="Array"/> of random characters using the specified <paramref name="strategy"/> that has the specified <paramref name="length"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="length">The desired length of the result.</param>
        /// <param name="strategy">The strategy to use. This parameter is optional.
        /// <br/>Default value: <see cref="StringCreation.Ascii"/>.</param>
        /// <returns>An array of characters <see cref="string"/> value generated by the specified <paramref name="strategy"/> that has the specified <paramref name="length"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than 0
        /// <br/>-or-
        /// <br/><paramref name="strategy"/> is not a valid value of <see cref="StringCreation"/>.</exception>
        [SecuritySafeCritical]
        public static unsafe char[] NextChars(this Random random, int length, StringCreation strategy = StringCreation.Ascii)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (length < 0)
                Throw.ArgumentOutOfRangeException(Argument.length, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if ((uint)strategy > (uint)StringCreation.Sentence)
                Throw.EnumArgumentOutOfRange(Argument.strategy, strategy);

            if (length == 0)
                return Reflector.EmptyArray<char>();

            var result = new char[length];
            fixed (char* s = result)
                FillChars(random, new MutableString(s, length), strategy);
            return result;
        }

        /// <summary>
        /// Fills the elements of a <paramref name="buffer"/> of character array with random characters using the specified <paramref name="allowedCharacters"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="buffer">An array of characters to contain random values.</param>
        /// <param name="allowedCharacters">A string containing the allowed characters. Recurring characters may appear in the result more frequently than others.</param>
        /// <exception cref="ArgumentNullException"><paramref name="random"/>, <paramref name="buffer"/> or <paramref name="allowedCharacters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="allowedCharacters"/> is empty.</exception>
        [SecuritySafeCritical]
        public static unsafe void NextChars(this Random random, char[] buffer, string allowedCharacters)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (buffer == null!)
                Throw.ArgumentNullException(Argument.buffer);
            if (allowedCharacters == null!)
                Throw.ArgumentNullException(Argument.allowedCharacters);
            if (allowedCharacters.Length == 0)
                Throw.ArgumentException(Argument.allowedCharacters, Res.ArgumentEmpty);

            if (buffer.Length == 0)
                return;

            fixed (char* s = buffer)
            fixed (char* set = allowedCharacters)
                FillChars(random, new MutableString(s, buffer.Length), new MutableString(set, allowedCharacters.Length));
        }

        /// <summary>
        /// Fills the elements of a <paramref name="buffer"/> of character array with random characters using the specified <paramref name="allowedCharacters"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="buffer">An array of characters to contain random values.</param>
        /// <param name="allowedCharacters">An array of the allowed characters. Recurring characters may appear in the result more frequently than others.</param>
        /// <exception cref="ArgumentNullException"><paramref name="random"/>, <paramref name="buffer"/> or <paramref name="allowedCharacters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="allowedCharacters"/> is empty.</exception>
        [SecuritySafeCritical]
        public static unsafe void NextChars(this Random random, char[] buffer, char[] allowedCharacters)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (buffer == null!)
                Throw.ArgumentNullException(Argument.buffer);
            if (allowedCharacters == null!)
                Throw.ArgumentNullException(Argument.allowedCharacters);
            if (allowedCharacters.Length == 0)
                Throw.ArgumentException(Argument.allowedCharacters, Res.ArgumentEmpty);

            if (buffer.Length == 0)
                return;

            fixed (char* s = buffer)
            fixed (char* set = allowedCharacters)
                FillChars(random, new MutableString(s, buffer.Length), new MutableString(set, allowedCharacters.Length));
        }

        /// <summary>
        /// Fills the elements of a <paramref name="buffer"/> of character array with random characters using the specified <paramref name="strategy"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="buffer">An array of characters to contain random values.</param>
        /// <param name="strategy">The strategy to use. This parameter is optional.
        /// <br/>Default value: <see cref="StringCreation.Ascii"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="strategy"/> is not a valid value of <see cref="StringCreation"/>.</exception>
        [SecuritySafeCritical]
        public static unsafe void NextChars(this Random random, char[] buffer, StringCreation strategy = StringCreation.Ascii)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (buffer == null!)
                Throw.ArgumentNullException(Argument.buffer);
            if ((uint)strategy > (uint)StringCreation.Sentence)
                Throw.EnumArgumentOutOfRange(Argument.strategy, strategy);

            if (buffer.Length == 0)
                return;

            fixed (char* s = buffer)
                FillChars(random, new MutableString(s, buffer.Length), strategy);
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Fills the elements of a <paramref name="buffer"/> with random characters using the specified <paramref name="allowedCharacters"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="buffer">A <see cref="Span{T}"/> of characters to contain random values.</param>
        /// <param name="allowedCharacters">A <see cref="ReadOnlySpan{T}"/> containing the allowed characters. Recurring characters may appear in the result more frequently than others.</param>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="allowedCharacters"/> is empty.</exception>
        [SecuritySafeCritical]
        public static unsafe void NextChars(this Random random, Span<char> buffer, ReadOnlySpan<char> allowedCharacters)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (allowedCharacters.Length == 0)
                Throw.ArgumentException(Argument.allowedCharacters, Res.ArgumentEmpty);

            if (buffer.Length == 0)
                return;

            fixed (char* s = buffer)
            fixed (char* set = allowedCharacters)
                FillChars(random, new MutableString(s, buffer.Length), new MutableString(set, allowedCharacters.Length));
        }

        /// <summary>
        /// Fills the elements of a <paramref name="buffer"/> with random characters using the specified <paramref name="strategy"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="buffer">A <see cref="Span{T}"/> of characters to contain random values.</param>
        /// <param name="strategy">The strategy to use. This parameter is optional.
        /// <br/>Default value: <see cref="StringCreation.Ascii"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="strategy"/> is not a valid value of <see cref="StringCreation"/>.</exception>
        [SecuritySafeCritical]
        public static unsafe void NextChars(this Random random, Span<char> buffer, StringCreation strategy = StringCreation.Ascii)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if ((uint)strategy > (uint)StringCreation.Sentence)
                Throw.EnumArgumentOutOfRange(Argument.strategy, strategy);

            if (buffer.Length == 0)
                return;

            fixed (char* s = buffer)
                FillChars(random, new MutableString(s, buffer.Length), strategy);
        }
#endif

        #endregion

        #region String

        /// <summary>
        /// Returns a random <see cref="string"/> that has the length between the specified range and consists of the specified <paramref name="allowedCharacters"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minLength">The inclusive lower bound of the length of the returned string.</param>
        /// <param name="maxLength">The inclusive upper bound of the length of the returned string. Must be greater or equal to <paramref name="minLength"/>.</param>
        /// <param name="allowedCharacters">A string containing the allowed characters. Recurring characters may appear in the result more frequently than others.</param>
        /// <returns>A <see cref="string"/> value that has the length greater than or equal to <paramref name="minLength"/> and less and less or equal to <paramref name="maxLength"/>
        /// and contains only the specified characters.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="allowedCharacters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> is less than 0 or <paramref name="maxLength"/> is less than <paramref name="minLength"/></exception>
        /// <exception cref="ArgumentException"><paramref name="allowedCharacters"/> is empty.</exception>
        [SecuritySafeCritical]
        public static unsafe string NextString(this Random random, int minLength, int maxLength, string allowedCharacters)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (minLength < 0)
                Throw.ArgumentOutOfRangeException(Argument.minLength, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if (maxLength < minLength)
                Throw.ArgumentOutOfRangeException(Argument.maxLength, Res.MaxLengthLessThanMinLength);
            if (allowedCharacters == null!)
                Throw.ArgumentNullException(Argument.allowedCharacters);
            if (allowedCharacters.Length == 0)
                Throw.ArgumentException(Argument.allowedCharacters, Res.ArgumentEmpty);

            int length = random.NextInt32(minLength, maxLength, true);
            if (length == 0)
                return String.Empty;

            string result = new String('\0', length);
            fixed (char* s = result)
            fixed (char* set = allowedCharacters)
                FillChars(random, new MutableString(s, length), new MutableString(set, allowedCharacters.Length));
            return result;
        }

        /// <summary>
        /// Returns a random <see cref="string"/> that has the length between the specified range and consists of the specified <paramref name="allowedCharacters"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minLength">The inclusive lower bound of the length of the returned string.</param>
        /// <param name="maxLength">The inclusive upper bound of the length of the returned string. Must be greater or equal to <paramref name="minLength"/>.</param>
        /// <param name="allowedCharacters">An array containing the allowed characters. Recurring characters may appear in the result more frequently than others.</param>
        /// <returns>A <see cref="string"/> value that has the length greater than or equal to <paramref name="minLength"/> and less and less or equal to <paramref name="maxLength"/>
        /// and contains only the specified characters.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="allowedCharacters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> is less than 0 or <paramref name="maxLength"/> is less than <paramref name="minLength"/></exception>
        /// <exception cref="ArgumentException"><paramref name="allowedCharacters"/> is empty.</exception>
        [SecuritySafeCritical]
        public static unsafe string NextString(this Random random, int minLength, int maxLength, char[] allowedCharacters)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (minLength < 0)
                Throw.ArgumentOutOfRangeException(Argument.minLength, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if (maxLength < minLength)
                Throw.ArgumentOutOfRangeException(Argument.maxLength, Res.MaxLengthLessThanMinLength);
            if (allowedCharacters == null!)
                Throw.ArgumentNullException(Argument.allowedCharacters);
            if (allowedCharacters.Length == 0)
                Throw.ArgumentException(Argument.allowedCharacters, Res.ArgumentEmpty);

            int length = random.NextInt32(minLength, maxLength, true);
            if (length == 0)
                return String.Empty;

            string result = new String('\0', length);
            fixed (char* s = result)
            fixed (char* set = allowedCharacters)
                FillChars(random, new MutableString(s, length), new MutableString(set, allowedCharacters.Length));
            return result;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Returns a random <see cref="string"/> that has the length between the specified range and consists of the specified <paramref name="allowedCharacters"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minLength">The inclusive lower bound of the length of the returned string.</param>
        /// <param name="maxLength">The inclusive upper bound of the length of the returned string. Must be greater or equal to <paramref name="minLength"/>.</param>
        /// <param name="allowedCharacters">A <see cref="ReadOnlySpan{T}"/> containing the allowed characters. Recurring characters may appear in the result more frequently than others.</param>
        /// <returns>A <see cref="string"/> value that has the length greater than or equal to <paramref name="minLength"/> and less and less or equal to <paramref name="maxLength"/>
        /// and contains only the specified characters.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> is less than 0 or <paramref name="maxLength"/> is less than <paramref name="minLength"/></exception>
        /// <exception cref="ArgumentException"><paramref name="allowedCharacters"/> is empty.</exception>
        [SecuritySafeCritical]
        public static unsafe string NextString(this Random random, int minLength, int maxLength, ReadOnlySpan<char> allowedCharacters)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (minLength < 0)
                Throw.ArgumentOutOfRangeException(Argument.minLength, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if (maxLength < minLength)
                Throw.ArgumentOutOfRangeException(Argument.maxLength, Res.MaxLengthLessThanMinLength);
            if (allowedCharacters.Length == 0)
                Throw.ArgumentException(Argument.allowedCharacters, Res.ArgumentEmpty);

            int length = random.NextInt32(minLength, maxLength, true);
            if (length == 0)
                return String.Empty;

            string result = new String('\0', length);
            fixed (char* s = result)
            fixed (char* set = &allowedCharacters.GetPinnableReference())
                FillChars(random, new MutableString(s, length), new MutableString(set, allowedCharacters.Length));
            return result;
        }
#endif

        /// <summary>
        /// Returns a random <see cref="string"/> using the specified <paramref name="strategy"/> that has the length between the specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minLength">The inclusive lower bound of the length of the returned string.</param>
        /// <param name="maxLength">The inclusive upper bound of the length of the returned string. Must be greater or equal to <paramref name="minLength"/>.</param>
        /// <param name="strategy">The strategy to use. This parameter is optional.
        /// <br/>Default value: <see cref="StringCreation.Ascii"/>.</param>
        /// <returns>A <see cref="string"/> value generated by the specified <paramref name="strategy"/> that has the length greater than or equal to <paramref name="minLength"/> and less and less or equal to <paramref name="maxLength"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> is less than 0 or <paramref name="maxLength"/> is less than <paramref name="minLength"/>
        /// <br/>-or-
        /// <br/><paramref name="strategy"/> is not a valid value of <see cref="StringCreation"/>.</exception>
        [SecuritySafeCritical]
        public static unsafe string NextString(this Random random, int minLength, int maxLength, StringCreation strategy = StringCreation.Ascii)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (minLength < 0)
                Throw.ArgumentOutOfRangeException(Argument.minLength, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if (maxLength < minLength)
                Throw.ArgumentOutOfRangeException(Argument.maxLength, Res.MaxLengthLessThanMinLength);
            if ((uint)strategy > (uint)StringCreation.Sentence)
                Throw.EnumArgumentOutOfRange(Argument.strategy, strategy);

            int length = random.NextInt32(minLength, maxLength, true);
            if (length == 0)
                return String.Empty;

            string result = new String('\0', length);
            fixed (char* s = result)
                FillChars(random, new MutableString(s, length), strategy);
            return result;
        }

        /// <summary>
        /// Returns a random <see cref="string"/> of the specified <paramref name="length"/> using the specified <paramref name="strategy"/> that has the length between the specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="length">The length of the string to be generated.</param>
        /// <param name="strategy">The strategy to use. This parameter is optional.
        /// <br/>Default value: <see cref="StringCreation.Ascii"/>.</param>
        /// <returns>A <see cref="string"/> value generated by the specified <paramref name="strategy"/> and <paramref name="length"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than 0
        /// <br/>-or-
        /// <br/><paramref name="strategy"/> is not a valid value of <see cref="StringCreation"/>.</exception>
        public static string NextString(this Random random, int length, StringCreation strategy = StringCreation.Ascii)
            => NextString(random, length, length, strategy);

        /// <summary>
        /// Returns a random <see cref="string"/> using the specified <paramref name="strategy"/> that has the length between 4 and 10 inclusive.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="strategy">The strategy to use. This parameter is optional.
        /// <br/>Default value: <see cref="StringCreation.Ascii"/>.</param>
        /// <returns>A <see cref="string"/> value generated by the specified <paramref name="strategy"/> that has the length between 4 and 10 inclusive.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="strategy"/> is not a valid value of <see cref="StringCreation"/>.</exception>
        public static string NextString(this Random random, StringCreation strategy = StringCreation.Ascii)
            => NextString(random, 4, 10, strategy);

        #endregion

        #region Rune
#if NETCOREAPP3_0_OR_GREATER

        /// <summary>
        /// Returns a random <see cref="Rune"/> (Unicode character).
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A <see cref="Rune"/> (Unicode character).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static Rune NextRune(this Random random) => NextRune(random, RuneInfo.MinValue, RuneInfo.MaxValue);

        /// <summary>
        /// Returns a random <see cref="Rune"/> (Unicode character) that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random character returned.</param>
        /// <param name="maxValue">The inclusive upper bound of the random character returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <returns>A <see cref="Rune"/> (Unicode character) that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        public static Rune NextRune(this Random random, Rune minValue, Rune maxValue)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (maxValue <= minValue)
            {
                if (maxValue == minValue)
                    return minValue;
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);
            }

            return RuneInfo.GetRuneByIndex(minValue.Value + random.Next(RuneInfo.GetInclusiveRange(minValue, maxValue)));
        }

        /// <summary>
        /// Returns a random <see cref="Rune"/> (Unicode character) that is within the specified <paramref name="category"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="category">The category of the character to be returned.</param>
        /// <returns>A <see cref="Rune"/> (Unicode character) that is greater that is within the specified <paramref name="category"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="category"/> is <see cref="UnicodeCategory.Surrogate"/> or an undefined value.</exception>
        public static Rune NextRune(this Random random, UnicodeCategory category)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            return RuneInfo.GetRandomRune(random, category);
        }

#endif  
        #endregion

        #endregion

        #region Date and Time

        /// <summary>
        /// Returns a random <see cref="DateTime"/> that is between the specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random <see cref="DateTime"/> returned or <see langword="null"/>&#160;to use <see cref="DateTime.MinValue">DateTime.MinValue</see>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="maxValue">The inclusive upper bound of the random <see cref="DateTime"/> returned or <see langword="null"/>&#160;to use <see cref="DateTime.MaxValue">DateTime.MaxValue</see>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A <see cref="DateTime"/> value that is in the specified range.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        /// <remarks>
        /// <para>The <see cref="DateTime.Kind"/> property of <paramref name="minValue"/> and <paramref name="maxValue"/> is ignored.</para>
        /// <para>The <see cref="DateTime.Kind"/> property of the generated <see cref="DateTime"/> instances is always <see cref="DateTimeKind.Unspecified"/>.</para>
        /// </remarks>
        public static DateTime NextDateTime(this Random random, DateTime? minValue = null, DateTime? maxValue = null)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            long minTicks = minValue.GetValueOrDefault(DateTime.MinValue).Ticks;
            long maxTicks = maxValue.GetValueOrDefault(DateTime.MaxValue).Ticks;
            if (maxTicks < minTicks)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            return new DateTime(random.NextInt64(minTicks, maxTicks, true));
        }

        /// <summary>
        /// Returns a random <see cref="DateTime"/> that is between the specified range and has only date component.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random <see cref="DateTime"/> returned or <see langword="null"/>&#160;to use <see cref="DateTime.MinValue">DateTime.MinValue</see>.
        /// The time component is ignored. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="maxValue">The inclusive upper bound of the random <see cref="DateTime"/> returned or <see langword="null"/>&#160;to use <see cref="DateTime.MaxValue">DateTime.MaxValue</see>.
        /// The time component is ignored. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A <see cref="DateTime"/> value that is in the specified range and has only date component.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        /// <remarks>
        /// <para>The <see cref="DateTime.Kind"/> property of <paramref name="minValue"/> and <paramref name="maxValue"/> is ignored.</para>
        /// <para>The time component of <paramref name="minValue"/> and <paramref name="maxValue"/> is ignored.</para>
        /// <para>The <see cref="DateTime.Kind"/> property of the generated <see cref="DateTime"/> instances is always <see cref="DateTimeKind.Unspecified"/>.</para>
        /// </remarks>
        public static DateTime NextDate(this Random random, DateTime? minValue = null, DateTime? maxValue = null)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            DateTime minDate = minValue.GetValueOrDefault(DateTime.MinValue).Date;
            DateTime maxDate = maxValue.GetValueOrDefault(DateTime.MaxValue).Date;
            if (maxDate < minDate)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            int range = (maxDate - minDate).Days;
            return minDate.AddDays(random.NextInt32(range, true));
        }

        /// <summary>
        /// Returns a random <see cref="DateTimeOffset"/> that is between the specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random <see cref="DateTimeOffset"/> returned or <see langword="null"/>&#160;to use <see cref="DateTimeOffset.MinValue">DateTimeOffset.MinValue</see>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="maxValue">The inclusive upper bound of the random <see cref="DateTimeOffset"/> returned or <see langword="null"/>&#160;to use <see cref="DateTimeOffset.MaxValue">DateTimeOffset.MaxValue</see>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A <see cref="DateTimeOffset"/> value that is in the specified range.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        public static DateTimeOffset NextDateTimeOffset(this Random random, DateTimeOffset? minValue = null, DateTimeOffset? maxValue = null)
        {
            const int maximumOffset = 14 * 60;
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            DateTime minDateTime = minValue?.UtcDateTime ?? DateTime.MinValue;
            DateTime maxDateTime = maxValue?.UtcDateTime ?? DateTime.MaxValue;
            if (maxDateTime < minDateTime)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            DateTime result = random.NextDateTime(minDateTime, maxDateTime);
            double diffInMinutes;
            int minOffset = (diffInMinutes = (maxDateTime - result).TotalMinutes) < maximumOffset ? (int)-diffInMinutes : -maximumOffset;
            int maxOffset = (diffInMinutes = (result - minDateTime).TotalMinutes) < maximumOffset ? (int)diffInMinutes : maximumOffset;
            return new DateTimeOffset(result, TimeSpan.FromMinutes(random.NextInt32(minOffset, maxOffset, true)));
        }

        /// <summary>
        /// Returns a random <see cref="TimeSpan"/> that is between the specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random <see cref="TimeSpan"/> returned or <see langword="null"/>&#160;to use <see cref="TimeSpan.MinValue">TimeSpan.MinValue</see>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="maxValue">The inclusive upper bound of the random <see cref="TimeSpan"/> returned or <see langword="null"/>&#160;to use <see cref="TimeSpan.MaxValue">TimeSpan.MaxValue</see>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A <see cref="TimeSpan"/> value that is in the specified range.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        public static TimeSpan NextTimeSpan(this Random random, TimeSpan? minValue = null, TimeSpan? maxValue = null)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

            long minTicks = minValue.GetValueOrDefault(TimeSpan.MinValue).Ticks;
            long maxTicks = maxValue.GetValueOrDefault(TimeSpan.MaxValue).Ticks;
            if (maxTicks < minTicks)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            return new TimeSpan(random.NextInt64(minTicks, maxTicks, true));
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Returns a random <see cref="DateOnly"/> that is between the specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random <see cref="DateOnly"/> returned or <see langword="null"/>&#160;to
        /// use <see cref="DateOnly.MinValue">DateOnly.MinValue</see>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="maxValue">The inclusive upper bound of the random <see cref="DateOnly"/> returned or <see langword="null"/>&#160;to
        /// use <see cref="DateOnly.MaxValue">DateTime.MaxValue</see>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A <see cref="DateOnly"/> value that is in the specified range.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        public static DateOnly NextDateOnly(this Random random, DateOnly? minValue = null, DateOnly? maxValue = null)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            DateOnly minDate = minValue.GetValueOrDefault(DateOnly.MinValue);
            DateOnly maxDate = maxValue.GetValueOrDefault(DateOnly.MaxValue);
            if (maxDate < minDate)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            int range = maxDate.DayNumber - minDate.DayNumber;
            return minDate.AddDays(random.NextInt32(range, true));
        }

        /// <summary>
        /// Returns a random <see cref="TimeOnly"/> that is between the specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The inclusive lower bound of the random <see cref="TimeOnly"/> returned or <see langword="null"/>&#160;to use <see cref="TimeOnly.MinValue">TimeOnly.MinValue</see>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="maxValue">The inclusive upper bound of the random <see cref="TimeOnly"/> returned or <see langword="null"/>&#160;to use <see cref="TimeOnly.MaxValue">TimeOnly.MaxValue</see>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>A <see cref="TimeOnly"/> value that is in the specified range.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>.</exception>
        public static TimeOnly NextTimeOnly(this Random random, TimeOnly? minValue = null, TimeOnly? maxValue = null)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

            long minTicks = minValue.GetValueOrDefault(TimeOnly.MinValue).Ticks;
            long maxTicks = maxValue.GetValueOrDefault(TimeOnly.MaxValue).Ticks;
            if (maxTicks < minTicks)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            return new TimeOnly(random.NextInt64(minTicks, maxTicks, true));
        }
#endif

        #endregion

        #region Enum

        /// <summary>
        /// Returns a random <typeparamref name="TEnum"/> value.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see langword="enum"/>. Must be an <see cref="Enum"/> type.</typeparam>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A random <typeparamref name="TEnum"/> value or the default value of <typeparamref name="TEnum"/> if it has no defined values.</returns>
        public static TEnum NextEnum<TEnum>(this Random random)
            where TEnum : struct, Enum
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            return Enum<TEnum>.GetValues().GetRandomElement(random, true);
        }

        #endregion

        #region GUID

        /// <summary>
        /// Returns a random RFC 4122 compliant <see cref="Guid"/> value generated by using the specified <see cref="Random"/> instance.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use. Note that if it is a non-derived <see cref="Random">System.Random</see> instance, then
        /// the result cannot be considered as a cryptographically secure identifier.</param>
        /// <returns>An RFC 4122 compliant <see cref="Guid"/> value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <note type="security">To create cryptographically secure <see cref="Guid"/> values use a derived type of <see cref="Random"/>,
        /// such as <see cref="SecureRandom"/>, which can be considered as secure, or just call <see cref="Guid.NewGuid">Guid.NewGuid</see> instead.</note>
        /// </remarks>
        /// <seealso cref="SecureRandom"/>
        public static Guid NextGuid(this Random random)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            // Interestingly, in .NET Core 3.0 this is still slower than pure byte arrays.
            // Still, we hope that Span performance (or the cast to ReadOnlySpan?) will be faster later
            // and that sparing heap allocation is worth it.
            Span<byte> bytes = stackalloc byte[16];
#else
            byte[] bytes = new byte[16];
#endif
            random.NextBytes(bytes);
            bytes[6] = (byte)((bytes[6] & 0x0F) | 0x40); // the high nibble of 6th byte is 4
            bytes[8] = (byte)((bytes[8] & 0b0011_1111) & 0b1000_0000); // the two MSBs of 8th byte are 10b
            return new Guid(bytes);
        }

        #endregion

        #region Object

        /// <summary>
        /// Returns a random object of type <typeparamref name="T"/> or the default value of <typeparamref name="T"/>
        /// if <typeparamref name="T"/> cannot be instantiated with the provided <paramref name="settings"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <typeparam name="T">The type of the object to be created. If <see cref="GenerateObjectSettings.TryResolveInterfacesAndAbstractTypes"/> property
        /// in <paramref name="settings"/> is <see langword="true"/>, then it can be also an interface or abstract type;
        /// however, if no implementation or usable constructor found, then a <see langword="null"/>&#160;value will be returned.</typeparam>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="settings">The settings to use or <see langword="null"/>&#160;to use the default settings.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An instance of <typeparamref name="T"/> or <see langword="null"/>&#160;if the type cannot be
        /// instantiated with the provided <paramref name="settings"/> See the <strong>Remarks</strong> section for details.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para><note type="caution">The generated object is not guaranteed to be in a consistent format, especially if <see cref="GenerateObjectSettings.AllowCreateObjectWithoutConstructor"/>
        /// property is <see langword="true"/>&#160;or <see cref="GenerateObjectSettings.ObjectInitialization"/> property is <see cref="ObjectInitialization.Fields"/> in <paramref name="settings"/>.</note></para>
        /// <para><typeparamref name="T"/> can be basically any type as long as it has a default constructor or (in case of collections) a constructor with a parameter that can accept a collection.</para>
        /// <para>If <typeparamref name="T"/> is an interface or an abstract class you can set the <see cref="GenerateObjectSettings.TryResolveInterfacesAndAbstractTypes"/> property to
        /// use a random implementation of <typeparamref name="T"/>. If no implementation is found among the loaded assemblies with a proper constructor, then the result will be <see langword="null"/>.</para>
        /// <para>If <typeparamref name="T"/> is a non-sealed class you can set the <see cref="GenerateObjectSettings.AllowDerivedTypesForNonSealedClasses"/> property to
        /// allow to use a random derived class of <typeparamref name="T"/>.</para>
        /// <para>All types, which can be generated by the <c>Next...</c> methods of the <see cref="RandomExtensions"/> class, are supported.
        /// Some other types have some special handling for better support:
        /// <list type="bullet">
        /// <item><term><see cref="StringBuilder"/></term><description>The same behavior as for strings.</description></item>
        /// <item><term><see cref="Uri"/></term><description>The result will match the following pattern: <c>http://&lt;lowercase word-like string of length between 4 and 10&gt;.&lt;3 lower case letters&gt;</c></description></item>
        /// <item><term><see cref="IntPtr"/></term><description>The same behavior as for 32 or 64 bit signed integers, based on the used platform.</description></item>
        /// <item><term><see cref="UIntPtr"/></term><description>The same behavior as for 32 or 64 bit unsigned integers, based on the used platform.</description></item>
        /// <item><term><see cref="KeyValuePair{TKey,TValue}"/></term><description>Using its parameterized constructor to create an instance.</description></item>
        /// <item><term><see cref="Assembly"/></term><description>A random loaded assembly will be picked.</description></item>
        /// <item><term><see cref="Type"/></term><description>A random type will be picked from one of the loaded assemblies.</description></item>
        /// <item><term><see cref="MemberInfo"/> types</term><description>A random member will be picked from one of the types of the loaded assemblies.</description></item>
        /// <item><term><see cref="Delegate"/> types</term><description>A dynamic method will be created for the specified delegate, which returns random objects both by return value and by the possible <c>out</c> parameters.</description></item>
        /// </list>
        /// <note>The generated delegates do not use the specified <paramref name="random"/> instance because in that case the <paramref name="random"/> instance could
        /// never be reclaimed by the garbage collector. To avoid leaking memory generated delegates use an internal static <see cref="Random"/> instance.</note>
        /// </para>
        /// <note type="tip">See the <strong>Examples</strong> section of the <see cref="RandomExtensions"/> class for some examples.</note>
        /// </remarks>
        [SecuritySafeCritical]
        public static T? NextObject<T>(this Random random, GenerateObjectSettings? settings = null)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            return (T?)ObjectGenerator.GenerateObject(random, typeof(T), settings ?? GenerateObjectSettings.DefaultSettings);
        }

        /// <summary>
        /// Returns a random object of the specified <paramref name="type"/> or <see langword="null"/>&#160;
        /// if <paramref name="type"/> cannot be instantiated with the provided <paramref name="settings"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="type">The type of the object to be created. If <see cref="GenerateObjectSettings.TryResolveInterfacesAndAbstractTypes"/> property
        /// in <paramref name="settings"/> is <see langword="true"/>, then it can be also an interface or abstract type;
        /// however, if no implementation or usable constructor found, then a <see langword="null"/>&#160;value will be returned.</param>
        /// <param name="settings">The settings to use or <see langword="null"/>&#160;to use the default settings.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An instance of <paramref name="type"/> or <see langword="null"/>&#160;if the type cannot be
        /// instantiated with the provided <paramref name="settings"/> See the <strong>Remarks</strong> section of the <see cref="NextObject{T}"/> overload for details.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="type"/> is <see langword="null"/>.</exception>
        [SecuritySafeCritical]
        public static object? NextObject(this Random random, Type type, GenerateObjectSettings? settings = null)
        {
            if (random == null!)
                Throw.ArgumentNullException(Argument.random);
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);
            return ObjectGenerator.GenerateObject(random, type, settings ?? GenerateObjectSettings.DefaultSettings);
        }

        #endregion

        #endregion

        #region Private Methods

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static unsafe uint GenerateSampleUInt32(Random random)
        {
#if NETCOREAPP3_0_OR_GREATER
            Span<byte> bytes = stackalloc byte[4];
            random.NextBytes(bytes);
            return Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(bytes));
#elif NETCOREAPP2_1 || NETSTANDARD2_1_OR_GREATER
            Span<byte> bytes = stackalloc byte[4];
            random.NextBytes(bytes);
            fixed (byte* p = bytes)
                return *(uint*)p;
#else
            byte[] bytes = new byte[4];
            random.NextBytes(bytes);
            fixed (byte* p = bytes)
                return *(uint*)p;
#endif
        }

        [SecuritySafeCritical]
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static unsafe ulong GenerateSampleUInt64(Random random)
        {
#if NETCOREAPP3_0_OR_GREATER
            Span<byte> bytes = stackalloc byte[8];
            random.NextBytes(bytes);
            return Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(bytes));
#elif NETCOREAPP2_1 || NETSTANDARD2_1_OR_GREATER
            Span<byte> bytes = stackalloc byte[8];
            random.NextBytes(bytes);
            fixed (byte* p = bytes)
                return *(ulong*)p;
#else
            byte[] bytes = new byte[8];
            random.NextBytes(bytes);
            fixed (byte* p = bytes)
                return *(ulong*)p;
#endif
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        private static BigInteger DoGenerateBigInteger(Random random, BigInteger maxValue)
        {
            #region Local Methods

            // Similar to GetBitMask but gets the "ceiling" mask for a single byte
            static byte GetCeilBitMask(int value)
            {
                Debug.Assert(value != 0 && value <= 255);

                // Inlined "Is power of two" test. Decrementing the initial value only if not a power of two,
                // that's how it will be 'ceiling' mask (because we assume that this is just the MSB of a possibly large value)
                if ((value & (value - 1)) != 0)
                    --value;

                value |= value >> 1;
                value |= value >> 2;
                value |= value >> 4;
                return (byte)value;
            }

            // Performing a compare using little endian representation returned by BigInteger.ToByteArray
            static bool IsGreaterOrEqual(Span<byte> actual, Span<byte> limit)
            {
                Debug.Assert(actual.Length == limit.Length);
                for (int i = actual.Length - 1; i >= 0; i--)
                {
                    if (actual[i] < limit[i])
                        return false;
                    if (actual[i] > limit[i])
                        return true;
                }

                return true;
            }

            #endregion

            Debug.Assert(maxValue.Sign > 0, "A positive range is expected");

            // We need to determine the length and the value of the most significant byte to generate a new value
            // so we obtain the bytes and also reuse the buffer to prevent unnecessary copies.
            byte[] bytes = maxValue.ToByteArray();
            int byteCount = bytes.Length;

            // the last byte can be zero to prevent interpreting the value as a negative two's complement number
            if (bytes[byteCount - 1] == 0)
                --byteCount;

            // We need to store the original bytes for comparisons. A BigInteger can have any size but
            // below 64K we use stack allocation (for comparison: Double.MaxValue [~1.8E+308] uses 128 bytes as a BigInteger).
            const int stackAllocationLimit = 1 << 16;
            Span<byte> comparisonBytes = byteCount > stackAllocationLimit ? new byte[byteCount] : stackalloc byte[byteCount];
            Span<byte> bytesSpan = bytes.AsSpan(0, byteCount);
            bytesSpan.CopyTo(comparisonBytes);

            // Storing a reference to the most significant byte and determining the mask for it.
            ref byte msb = ref bytes[byteCount - 1];
            Debug.Assert(msb != 0);
            byte mask = GetCeilBitMask(msb);

            // Filling up the buffer with random bytes. Not creating any intermediate BigInteger instances
            // to prevent unnecessary buffer copies. Applying the mask for MSB, which has been overwritten.
            random.NextBytes(bytesSpan);
            msb &= mask;

            // Applying a low-level comparison on the generated bytes. If the generated sample is too large, then regenerating only the MSB.
            // Depending on the mask we have 0-50% chance that we need to generate the MSB again.
            while (IsGreaterOrEqual(bytesSpan, comparisonBytes))
                msb = (byte)(random.Next() & mask);

            return new BigInteger(bytes);
        }
#elif !NET35
        private static BigInteger DoGenerateBigInteger(Random random, BigInteger maxValue)
        {
            // Similar to GetBitMask but gets the "ceiling" mask for a single byte
            static byte GetCeilBitMask(int value)
            {
                Debug.Assert(value != 0 && value <= 255);

                // Inlined "Is power of two" test. Decrementing the initial value only if not a power of two,
                // that's how it will be 'ceiling' mask (because we assume that this is just the MSB of a possibly large value)
                if ((value & (value - 1)) != 0)
                    --value;

                value |= value >> 1;
                value |= value >> 2;
                value |= value >> 4;
                return (byte)value;
            }

            // Performing a compare using little endian representation returned by BigInteger.ToByteArray
            static bool IsGreaterOrEqual(byte[] actual, byte[] limit, int length)
            {
                for (int i = length - 1; i >= 0; i--)
                {
                    if (actual[i] < limit[i])
                        return false;
                    if (actual[i] > limit[i])
                        return true;
                }

                return true;
            }

            Debug.Assert(maxValue.Sign > 0, "A positive range is expected");

            // We need to determine the length and the value of the most significant byte to generate a new value
            // so we obtain the bytes and also reuse the buffer to prevent unnecessary copies.
            byte[] bytes = maxValue.ToByteArray();
            int byteCount = bytes.Length;

            // the last byte can be zero to prevent interpreting the value as a negative two's complement number
            if (bytes[byteCount - 1] == 0)
                --byteCount;

            // We need to store the original bytes for comparisons. A BigInteger can have any size so
            // though we could use stack allocation in most cases we use always a new array to prevent running out of stack.
            byte[] comparisonBytes = new byte[byteCount];
            Buffer.BlockCopy(bytes, 0, comparisonBytes, 0, byteCount);

            // Storing a reference to the most significant byte and determining the mask for it.
            ref byte msb = ref bytes[byteCount - 1];
            Debug.Assert(msb != 0);
            byte mask = GetCeilBitMask(msb);

            // Filling up the buffer with random bytes. Not creating any intermediate BigInteger instances
            // to prevent unnecessary buffer copies. Applying the mask for MSB, which has been overwritten.
            random.NextBytes(bytes);
            msb &= mask;

            // Using only the MSB to check whether the generated sample is too large. If so, then regenerating it.
            // Depending on maxMsb we have 0-50% chance that we need to generate the MSB again.
            while (IsGreaterOrEqual(bytes, comparisonBytes, byteCount))
                msb = (byte)(random.Next() & mask);

            // If the last byte was empty in the original buffer, then we need to clear it again.
            if (bytes.Length > byteCount)
                bytes[byteCount] = 0;

            return new BigInteger(bytes);
        }
#endif

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static double DoGetNextDouble(Random random, double maxValue, FloatScale scale)
        {
            // if linear scaling is forced...
            if (scale == FloatScale.ForceLinear
                // or we use auto scaling and maximum is UInt16
                || (scale == FloatScale.Auto && maxValue <= UInt16.MaxValue))
            {
                return random.NextDouble() * maxValue;
            }

            // Possible double exponents are -1022..1023 but we don't generate too small exponents for big ranges because
            // that would cause too many almost zero results, which are much smaller than the original NextDouble values.
            double minExponent = -16d;
            double maxExponent = Math.Log(maxValue, 2d);

            // We decrease exponents only if the given range is already small. Even lower than -1022 is no problem, the result may be 0
            if (maxExponent < minExponent)
                minExponent = maxExponent - 4;

            double result = Math.Pow(2d, NextDoubleLinear(random, minExponent, maxExponent));

            // protecting ourselves against inaccurate calculations; however, in practice result is always in range.
            return result > maxValue ? maxValue : result;
        }

        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False alarm for ReSharper issue")]
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator", Justification = "In this method this is intended")]
        private static double DoGetNextDouble(Random random, double minValue, double maxValue, FloatScale scale)
        {
            if (minValue == maxValue)
                return minValue;

            bool posAndNeg = minValue < 0d && maxValue > 0d;
            double minAbs = Math.Min(Math.Abs(minValue), Math.Abs(maxValue));
            double maxAbs = Math.Max(Math.Abs(minValue), Math.Abs(maxValue));

            // if linear scaling is forced...
            if (scale == FloatScale.ForceLinear
                // or we use auto scaling and maximum is UInt16 or when the difference of order of magnitude is smaller than 4
                || (scale == FloatScale.Auto && (maxAbs <= UInt16.MaxValue || !posAndNeg && maxAbs < minAbs * 16)))
            {
                return NextDoubleLinear(random, minValue, maxValue);
            }

            int sign;
            if (!posAndNeg)
                sign = minValue < 0d ? -1 : 1;
            else
            {
                // if both negative and positive results are expected we select the sign based on the size of the ranges
                double sample = random.NextDouble();
                double rate = minAbs / maxAbs;
                double absMinValue = Math.Abs(minValue);
                bool isNeg = absMinValue <= maxValue
                    ? rate / 2d > sample
                    : rate / 2d < sample;
                sign = isNeg ? -1 : 1;

                // now adjusting the limits for 0..[selected range]
                minAbs = 0d;
                maxAbs = isNeg ? absMinValue : Math.Abs(maxValue);
            }

            // Possible double exponents are -1022..1023 but we don't generate too small exponents for big ranges because
            // that would cause too many almost zero results, which are much smaller than the original NextDouble values.
            double minExponent = minAbs == 0d ? -16d : Math.Log(minAbs, 2d);
            double maxExponent = Math.Log(maxAbs, 2d);
            if (minExponent == maxExponent)
                return minValue;

            // We decrease exponents only if the given range is already small. Even lower than -1022 is no problem, the result may be 0
            if (maxExponent < minExponent)
                minExponent = maxExponent - 4;

            double result = sign * Math.Pow(2d, NextDoubleLinear(random, minExponent, maxExponent));

            // protecting ourselves against inaccurate calculations; however, in practice result is always in range.
            return result < minValue ? minValue : (result > maxValue ? maxValue : result);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static double NextDoubleLinear(Random random, double minValue, double maxValue)
        {
            double sample = random.NextDouble();
            double result = (maxValue * sample) + (minValue * (1d - sample));

            // protecting ourselves against inaccurate calculations; occurs only near MaxValue.
            return result < minValue ? minValue : (result > maxValue ? maxValue : result);
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static decimal NextDecimalLinear(Random random, decimal minValue, decimal maxValue)
        {
            decimal sample = random.NextDecimal();
            return Math.Sign(minValue) * Math.Sign(maxValue) >= 0
                // ranged version (because the other branch may overflow by 0.5 if both min and max are near MaxValue)
                ? (maxValue - minValue) * sample + minValue
                // wide-range proof version (max - min can be larger than MaxValue)
                : (maxValue * sample) + (minValue * (1m - sample));
        }

        [SecurityCritical]
        private static void FillChars(Random random, in MutableString target, bool checkInvalid = false)
        {
            for (int i = 0; i < target.Length; i++)
            {
                do
                {
                    target[i] = random.NextChar();
                } while (checkInvalid && !target[i].IsValidCharacter());
            }
        }

        [SecurityCritical]
        private static void FillChars(Random random, in MutableString target, CharSet allowedCharacters)
        {
            for (int i = 0; i < target.Length; i++)
                target[i] = allowedCharacters[random.Next(allowedCharacters.Length)];
        }

        [SecurityCritical]
        private static void FillChars(Random random, in MutableString target, in MutableString allowedCharacters)
        {
            for (int i = 0; i < target.Length; i++)
                target[i] = allowedCharacters[random.Next(allowedCharacters.Length)];
        }

        [SecurityCritical]
        private static void FillChars(Random random, in MutableString target, StringCreation strategy)
        {
            switch (strategy)
            {
                case StringCreation.AnyChars:
                    FillChars(random, target);
                    break;

                case StringCreation.AnyValidChars:
                    FillChars(random, target, true);
                    break;

                case StringCreation.Ascii:
                    FillChars(random, target, CharSet.Ascii);
                    break;

                case StringCreation.Digits:
                    FillChars(random, target, CharSet.Digits);
                    break;

                case StringCreation.DigitsNoLeadingZeros:
                    FillChars(random, target.Substring(0, 1), CharSet.NonZeroDigits);
                    if (target.Length > 1)
                        FillChars(random, target.Substring(1), CharSet.Digits);
                    break;

                case StringCreation.Letters:
                    FillChars(random, target, CharSet.Letters);
                    break;

                case StringCreation.LettersAndDigits:
                    FillChars(random, target, CharSet.LettersAndDigits);
                    break;

                case StringCreation.UpperCaseLetters:
                    FillChars(random, target, CharSet.UpperCaseLetters);
                    break;

                case StringCreation.LowerCaseLetters:
                    FillChars(random, target, CharSet.LowerCaseLetters);
                    break;

                case StringCreation.TitleCaseLetters:
                    FillChars(random, target.Substring(0, 1), CharSet.UpperCaseLetters);
                    if (target.Length > 1)
                        FillChars(random, target.Substring(1), CharSet.LowerCaseLetters);
                    break;

                case StringCreation.UpperCaseWord:
                    WordGenerator.GenerateWord(random, target);
                    target.ToUpper();
                    break;

                case StringCreation.LowerCaseWord:
                    WordGenerator.GenerateWord(random, target);
                    break;

                case StringCreation.TitleCaseWord:
                    WordGenerator.GenerateWord(random, target);
                    target.Substring(0, 1).ToUpper();
                    break;

                case StringCreation.Sentence:
                    WordGenerator.GenerateSentence(random, target);
                    break;

                default:
                    Throw.InternalError($"Unexpected strategy: {strategy}");
                    break;
            }
        }

        #endregion

        #endregion
    }
}
