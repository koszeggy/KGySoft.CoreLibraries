#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using KGySoft.Reflection;
using KGySoft.Security.Cryptography;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="Random"/> type.
    /// <br/>See the <strong>Examples</strong> section for an example.
    /// </summary>
    /// <example>
    /// <note type="tip">Try also <a href="https://dotnetfiddle.net/hQRVoZ" target="_blank">online</a>.</note>
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
        // ReSharper disable CompareOfFloatsByEqualityOperator - in this class this is intended

        #region Constants

        private const string digits = "0123456789";
        private const string lowerCaseLetters = "abcdefghijklmnopqrstuvwxyz";
        private const string upperCaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string letters = lowerCaseLetters + upperCaseLetters;
        private const string lettersAndDigits = digits + letters;

        #endregion

        #region Fields

        private static readonly string ascii = new String(Enumerable.Range(32, 95).Select(i => (char)i).ToArray());

        #endregion

        #region Methods

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
            if (random == null)
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
            if (random == null)
                Throw.ArgumentNullException(Argument.random);
            return (random.Next() & 1) == 0;
        }

        #endregion

        #region Integers

        /// <summary>
        /// Returns a random <see cref="sbyte"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>An 8-bit signed integer that is greater than or equal to <see cref="SByte.MinValue">SByte.MinValue</see> and less or equal to <see cref="SByte.MaxValue">SByte.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static sbyte NextSByte(this Random random)
            => (sbyte)random.NextBytes(1)[0];

        /// <summary>
        /// Returns a random <see cref="sbyte"/> value that is less or equal to the specified <paramref name="maxValue"/>.
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
            => (sbyte)random.NextInt64(0L, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="sbyte"/> value that is within a specified range.
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
            => (sbyte)random.NextInt64(minValue, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="byte"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>An 8-bit unsigned integer that is greater than or equal to 0 and less or equal to <see cref="Byte.MaxValue">Byte.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static byte NextByte(this Random random)
            => random.NextBytes(1)[0];

        /// <summary>
        /// Returns a random <see cref="byte"/> value that is less or equal to the specified <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>An 8-bit unsigned integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static byte NextByte(this Random random, byte maxValue, bool inclusiveUpperBound = false)
            => (byte)random.NextUInt64(0UL, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="byte"/> value that is within a specified range.
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
            => (byte)random.NextUInt64(minValue, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="short"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 16-bit signed integer that is greater than or equal to <see cref="Int16.MinValue">Int16.MinValue</see> and less or equal to <see cref="Int16.MaxValue">Int16.MaxValue</see>.</returns>
        /// <remarks>Similarly to the <see cref="Random.Next()">Random.Next()</see> method this one returns an <see cref="int"/> value; however, the result can be negative and
        /// the maximum possible value can be <see cref="Int32.MaxValue">Int32.MaxValue</see>.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static short NextInt16(this Random random)
            => BitConverter.ToInt16(random.NextBytes(2), 0);

        /// <summary>
        /// Returns a random <see cref="short"/> value that is less or equal to the specified <paramref name="maxValue"/>.
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
            => (short)random.NextInt64(0L, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="short"/> value that is within a specified range.
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
            => (short)random.NextInt64(minValue, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="ushort"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 16-bit unsigned integer that is greater than or equal to 0 and less or equal to <see cref="UInt16.MaxValue">UInt16.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static ushort NextUInt16(this Random random)
            => BitConverter.ToUInt16(random.NextBytes(2), 0);

        /// <summary>
        /// Returns a random <see cref="ushort"/> value that is less or equal to the specified <paramref name="maxValue"/>.
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
            => (ushort)random.NextUInt64(0UL, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="ushort"/> value that is within a specified range.
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
            => (ushort)random.NextUInt64(minValue, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="int"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to <see cref="Int32.MinValue">Int32.MinValue</see> and less or equal to <see cref="Int32.MaxValue">Int32.MaxValue</see>.</returns>
        /// <remarks>Similarly to the <see cref="Random.Next()">Random.Next()</see> method this one returns an <see cref="int"/> value; however, the result can be negative and
        /// the maximum possible value can be <see cref="Int32.MaxValue">Int32.MaxValue</see>.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static int NextInt32(this Random random)
            => BitConverter.ToInt32(random.NextBytes(4), 0);

        /// <summary>
        /// Returns a random <see cref="int"/> value that is less or equal to the specified <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="inclusiveUpperBound"><see langword="true"/>&#160;to allow that the generated value is equal to <paramref name="maxValue"/>; otherwise, <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less or equal to <paramref name="maxValue"/>.
        /// If <paramref name="inclusiveUpperBound"/> is <see langword="false"/>, then <paramref name="maxValue"/> is an exclusive upper bound; however, if <paramref name="maxValue"/> equals 0, then 0 is returned.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
        public static int NextInt32(this Random random, int maxValue, bool inclusiveUpperBound = false)
            => (int)random.NextInt64(0L, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="int"/> value that is within a specified range.
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
        public static int NextInt32(this Random random, int minValue, int maxValue, bool inclusiveUpperBound = false)
            => (int)random.NextInt64(minValue, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="uint"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 32-bit unsigned integer that is greater than or equal to 0 and less or equal to <see cref="UInt32.MaxValue">UInt32.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static uint NextUInt32(this Random random)
            => BitConverter.ToUInt32(random.NextBytes(4), 0);

        /// <summary>
        /// Returns a random <see cref="uint"/> value that is less or equal to the specified <paramref name="maxValue"/>.
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
            => (uint)random.NextUInt64(0UL, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="uint"/> value that is within a specified range.
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
            => (uint)random.NextUInt64(minValue, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="long"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 64-bit unsigned integer that is greater than or equal to <see cref="Int64.MinValue">Int64.MinValue</see> and less or equal to <see cref="Int64.MaxValue">Int64.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static long NextInt64(this Random random)
            => BitConverter.ToInt64(random.NextBytes(8), 0);

        /// <summary>
        /// Returns a random <see cref="long"/> value that is less or equal to the specified <paramref name="maxValue"/>.
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
            => random.NextInt64(0L, maxValue, inclusiveUpperBound);

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
            if (random == null)
                Throw.ArgumentNullException(Argument.random);
            if (minValue == maxValue)
                return minValue;

            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            ulong range = (ulong)(maxValue - minValue);
            if (inclusiveUpperBound)
            {
                if (range == UInt64.MaxValue)
                    return random.NextInt64();
                range += 1;
            }

            ulong limit = UInt64.MaxValue - (UInt64.MaxValue % range);
            ulong sample;
            do
            {
                sample = random.NextUInt64();
            }
            while (sample > limit);
            return (long)((sample % range) + (ulong)minValue);
        }

        /// <summary>
        /// Returns a random <see cref="ulong"/> value.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A 64-bit unsigned integer that is greater than or equal to 0 and less or equal to <see cref="UInt64.MaxValue">UInt64.MaxValue</see>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        [CLSCompliant(false)]
        public static ulong NextUInt64(this Random random)
            => BitConverter.ToUInt64(random.NextBytes(8), 0);

        /// <summary>
        /// Returns a random <see cref="ulong"/> value that is less or equal to the specified <paramref name="maxValue"/>.
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
            => random.NextUInt64(0UL, maxValue, inclusiveUpperBound);

        /// <summary>
        /// Returns a random <see cref="ulong"/> value that is within a specified range.
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
            if (random == null)
                Throw.ArgumentNullException(Argument.random);
            if (minValue == maxValue)
                return minValue;

            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            ulong range = maxValue - minValue;
            if (inclusiveUpperBound)
            {
                if (range == UInt64.MaxValue)
                    return random.NextUInt64();
                range += 1;
            }

            ulong limit = UInt64.MaxValue - (UInt64.MaxValue % range);
            ulong sample;
            do
            {
                sample = random.NextUInt64();
            }
            while (sample > limit);

            return (sample % range) + minValue;
        }

        #endregion

        #region Floating-point types

        /// <summary>
        /// Returns a random <see cref="float"/> value that is greater than or equal to 0.0 and less than 1.0.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A single-precision floating point number that is greater than or equal to 0.0 and less than 1.0.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static float NextSingle(this Random random)
        {
            if (random == null)
                Throw.ArgumentNullException(Argument.random);

            return (float)random.NextDouble();
        }

        /// <summary>
        /// Returns a random <see cref="float"/> value that is less or equal to the specified <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A single-precision floating point number that is greater than or equal to 0.0 and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases.
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.0
        /// <br/>-or-.
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static float NextSingle(this Random random, float maxValue, FloatScale scale = FloatScale.Auto)
            => random.NextSingle(0f, maxValue, scale);

        /// <summary>
        /// Returns a random <see cref="float"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A single-precision floating point number that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases such as
        /// when <paramref name="minValue"/> is equal to <paramref name="maxValue"/> or when integer parts of both limits are beyond the precision of the <see cref="double"/> type.</para>
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>
        /// <br/>-or-
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static float NextSingle(this Random random, float minValue, float maxValue, FloatScale scale = FloatScale.Auto)
        {
            static float AdjustValue(float value) => Single.IsNegativeInfinity(value) ? Single.MinValue : (Single.IsPositiveInfinity(value) ? Single.MaxValue : value);

            // both are the same infinity
            if (Single.IsPositiveInfinity(minValue) && Single.IsPositiveInfinity(maxValue) || Single.IsNegativeInfinity(minValue) && Single.IsNegativeInfinity(maxValue))
                Throw.ArgumentOutOfRangeException(Argument.minValue);

            return (float)random.NextDouble(AdjustValue(minValue), AdjustValue(maxValue), scale);
        }

        /// <summary>
        /// Returns a random <see cref="double"/> value that is less or equal to the specified <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0 and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases.
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.0
        /// <br/>-or-
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static double NextDouble(this Random random, double maxValue, FloatScale scale = FloatScale.Auto)
            => random.NextDouble(0d, maxValue, scale);
        
        /// <summary>
        /// Returns a random <see cref="double"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A double-precision floating point number that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases such as
        /// when <paramref name="minValue"/> is equal to <paramref name="maxValue"/> or when integer parts of both limits are beyond the precision of the <see cref="double"/> type.</para>
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>
        /// <br/>-or-
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static double NextDouble(this Random random, double minValue, double maxValue, FloatScale scale = FloatScale.Auto)
        {
            if (random == null)
                Throw.ArgumentNullException(Argument.random);
            if (Double.IsPositiveInfinity(minValue) && Double.IsPositiveInfinity(maxValue) || Double.IsNegativeInfinity(minValue) && Double.IsNegativeInfinity(maxValue))
                Throw.ArgumentOutOfRangeException(Argument.minValue);
            if (Double.IsNaN(minValue) || Double.IsNaN(maxValue))
                Throw.ArgumentOutOfRangeException(Double.IsNaN(minValue) ? Argument.minValue : Argument.maxValue);
            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);
            if (!Enum<FloatScale>.IsDefined(scale))
                Throw.EnumArgumentOutOfRange(Argument.scale, scale);

            return DoGetNextDouble(random, minValue, maxValue, scale);
        }

        /// <summary>
        /// Returns a random <see cref="decimal"/> value that is greater than or equal to 0.0 and less than 1.0.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A decimal floating point number that is greater than or equal to 0.0 and less than 1.0.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        public static decimal NextDecimal(this Random random)
        {
            if (random == null)
                Throw.ArgumentNullException(Argument.random);

            decimal result;
            do
            {
                // The hi argument of 0.9999999999999999999999999999m is 542101086.
                // (MaxInt, MaxInt, 542101086) is actually bigger than 1 but in practice the loop almost never repeats.
                result = new Decimal(random.NextInt32(), random.NextInt32(), random.Next(542101087), false, 28);
            } while (result >= 1m);

            return result;
        }

        /// <summary>
        /// Returns a random <see cref="decimal"/> value that is less or equal to the specified <paramref name="maxValue"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="maxValue">The upper bound of the random number returned.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A decimal floating point number that is greater than or equal to 0.0 and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases.
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.0
        /// <br/>-or-
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static decimal NextDecimal(this Random random, decimal maxValue, FloatScale scale = FloatScale.Auto)
            => NextDecimal(random, 0m, maxValue, scale);

        /// <summary>
        /// Returns a random <see cref="decimal"/> value that is within a specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minValue">The lower bound of the random number returned.</param>
        /// <param name="maxValue">The upper bound of the random number returned. Must be greater or equal to <paramref name="minValue"/>.</param>
        /// <param name="scale">The scale to use to generate the random number. This parameter is optional.
        /// <br/>Default value: <see cref="FloatScale.Auto"/>.</param>
        /// <returns>A single-precision floating point number that is greater than or equal to <paramref name="minValue"/> and less or equal to <paramref name="maxValue"/>.</returns>
        /// <remarks>
        /// <para>In most cases return value is less than <paramref name="maxValue"/>. Return value can be equal to <paramref name="maxValue"/> in very edge cases such as
        /// when <paramref name="minValue"/> is equal to <paramref name="maxValue"/>.</para>
        /// With <see cref="FloatScale.ForceLinear"/>&#160;<paramref name="scale"/> the result will be always less than <paramref name="maxValue"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than <paramref name="minValue"/>
        /// <br/>-or-
        /// <br/><paramref name="scale"/> is not a valid value of <see cref="FloatScale"/>.</exception>
        public static decimal NextDecimal(this Random random, decimal minValue, decimal maxValue, FloatScale scale = FloatScale.Auto)
        {
            if (random == null)
                Throw.ArgumentNullException(Argument.random);

            if (maxValue < minValue)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            if (!Enum<FloatScale>.IsDefined(scale))
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
                decimal sample = random.NextDecimal();
                var rate = minAbs / maxAbs;
                var absMinValue = Math.Abs(minValue);
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

            decimal result;
            do
            {
                result = sign * 10m.Pow(NextDecimalLinear(random, minExponent, maxExponent));
            } while (result < minValue || result > maxValue);
            return result;
        }

        #endregion

        #region Char/String

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
                ? (char)random.NextUInt16()
                : (char)random.NextUInt64(minValue, maxValue, true);

        /// <summary>
        /// Returns a random <see cref="string"/> that has the length between the specified range and consists of the specified <paramref name="allowedCharacters"/>.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minLength">The inclusive lower bound of the length of the returned string.</param>
        /// <param name="maxLength">The inclusive upper bound of the length of the returned string. Must be greater or equal to <paramref name="minLength"/>.</param>
        /// <param name="allowedCharacters">A string containing the allowed characters. Recurrence is not checked.</param>
        /// <returns>A <see cref="string"/> value that has the length greater than or equal to <paramref name="minLength"/> and less and less or equal to <paramref name="maxLength"/>
        /// and contains only the specified characters.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="allowedCharacters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> is less than 0 or <paramref name="maxLength"/> is less than <paramref name="minLength"/></exception>
        /// <exception cref="ArgumentException"><paramref name="allowedCharacters"/> is empty.</exception>
        public static string NextString(this Random random, int minLength, int maxLength, string allowedCharacters)
        {
            if (random == null)
                Throw.ArgumentNullException(Argument.random);
            if (minLength < 0)
                Throw.ArgumentOutOfRangeException(Argument.minLength, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if (maxLength < minLength)
                Throw.ArgumentOutOfRangeException(Argument.maxLength, Res.MaxLengthLessThanMinLength);
            if (allowedCharacters == null)
                Throw.ArgumentNullException(Argument.allowedCharacters);
            if (allowedCharacters.Length == 0)
                Throw.ArgumentException(Argument.allowedCharacters, Res.ArgumentEmpty);

            return GenerateString(random, random.NextInt32(minLength, maxLength, true), allowedCharacters);
        }

        /// <summary>
        /// Returns a random <see cref="string"/> using the specified <paramref name="strategy"/> that has the length between the specified range.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="minLength">The inclusive lower bound of the length of the returned string. This parameter is optional.
        /// <br/>Default value: <c>4</c>.</param>
        /// <param name="maxLength">The inclusive upper bound of the length of the returned string. Must be greater or equal to <paramref name="minLength"/>. This parameter is optional.
        /// <br/>Default value: <c>10</c>.</param>
        /// <param name="strategy">The strategy to use. This parameter is optional.
        /// <br/>Default value: <see cref="StringCreation.Ascii"/>.</param>
        /// <returns>A <see cref="string"/> value generated by the specified <paramref name="strategy"/> that has the length greater than or equal to <paramref name="minLength"/> and less and less or equal to <paramref name="maxLength"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minLength"/> is less than 0 or <paramref name="maxLength"/> is less than <paramref name="minLength"/>
        /// <br/>-or-
        /// <br/><paramref name="strategy"/> is not a valid value of <see cref="StringCreation"/>.</exception>
        public static string NextString(this Random random, int minLength = 4, int maxLength = 10, StringCreation strategy = StringCreation.Ascii)
        {
            if (random == null)
                Throw.ArgumentNullException(Argument.random);
            if (minLength < 0)
                Throw.ArgumentOutOfRangeException(Argument.minLength, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
            if (maxLength < minLength)
                Throw.ArgumentOutOfRangeException(Argument.maxLength, Res.MaxLengthLessThanMinLength);
            if (!Enum<StringCreation>.IsDefined(strategy))
                Throw.EnumArgumentOutOfRange(Argument.strategy, strategy);

            int length = random.NextInt32(minLength, maxLength, true);
            if (length == 0)
                return String.Empty;

            switch (strategy)
            {
                case StringCreation.AnyChars:
                    return GenerateString(random, length, null);

                case StringCreation.AnyValidChars:
                    return GenerateString(random, length, null, true);

                case StringCreation.Ascii:
                    return GenerateString(random, length, ascii);

                case StringCreation.Digits:
                    return GenerateString(random, length, digits);

                case StringCreation.DigitsNoLeadingZeros:
                    return digits[random.Next(1, digits.Length)] + GenerateString(random, length - 1, digits);

                case StringCreation.Letters:
                    return GenerateString(random, length, letters);

                case StringCreation.LettersAndDigits:
                    return GenerateString(random, length, lettersAndDigits);

                case StringCreation.UpperCaseLetters:
                    return GenerateString(random, length, upperCaseLetters);

                case StringCreation.LowerCaseLetters:
                    return GenerateString(random, length, lowerCaseLetters);

                case StringCreation.TitleCaseLetters:
                    return upperCaseLetters.GetRandomElement(random) + GenerateString(random, length - 1, lowerCaseLetters);

                case StringCreation.UpperCaseWord:
                    return WordGenerator.GenerateWord(random, length).ToUpperInvariant();

                case StringCreation.LowerCaseWord:
                    return WordGenerator.GenerateWord(random, length);

                case StringCreation.TitleCaseWord:
                    string word = WordGenerator.GenerateWord(random, length);
                    return Char.ToUpperInvariant(word[0]) + word.Substring(1);

                case StringCreation.Sentence:
                    return WordGenerator.GenerateSentence(random, length);

                default:
                    return Throw.InternalError<string>($"Unexpected strategy: {strategy}");
            }
        }

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
            if (random == null)
                Throw.ArgumentNullException(Argument.random);
            var minTicks = minValue.GetValueOrDefault(DateTime.MinValue).Ticks;
            var maxTicks = maxValue.GetValueOrDefault(DateTime.MaxValue).Ticks;
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
            if (random == null)
                Throw.ArgumentNullException(Argument.random);
            var minDate = minValue.GetValueOrDefault(DateTime.MinValue).Date;
            var maxDate = maxValue.GetValueOrDefault(DateTime.MaxValue).Date;
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
            if (random == null)
                Throw.ArgumentNullException(Argument.random);
            var minDateTime = minValue?.UtcDateTime ?? DateTime.MinValue;
            var maxDateTime = maxValue?.UtcDateTime ?? DateTime.MaxValue;
            if (maxDateTime < minDateTime)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            var result = random.NextDateTime(minDateTime, maxDateTime);
            double diffInMinutes;
            var minOffset = (diffInMinutes = (maxDateTime - result).TotalMinutes) < maximumOffset ? (int)-diffInMinutes : -maximumOffset;
            var maxOffset = (diffInMinutes = (result - minDateTime).TotalMinutes) < maximumOffset ? (int)diffInMinutes : maximumOffset;
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
            if (random == null)
                Throw.ArgumentNullException(Argument.random);

            var minTicks = minValue.GetValueOrDefault(TimeSpan.MinValue).Ticks;
            var maxTicks = maxValue.GetValueOrDefault(TimeSpan.MaxValue).Ticks;
            if (maxTicks < minTicks)
                Throw.ArgumentOutOfRangeException(Argument.maxValue, Res.MaxValueLessThanMinValue);

            return new TimeSpan(random.NextInt64(minTicks, maxTicks, true));
        }

        #endregion

        #region Enum

#pragma warning disable CS3024 // Constraint type is not CLS-compliant - IConvertible is replaced to System.Enum by RecompILer
        /// <summary>
        /// Returns a random <typeparamref name="TEnum"/> value.
        /// </summary>
        /// <typeparam name="TEnum">The type of the <see langword="enum"/>. Must be an <see cref="Enum"/> type.</typeparam>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>A random <typeparamref name="TEnum"/> value or the default value of <typeparamref name="TEnum"/> if it has no defined values.</returns>
        public static TEnum NextEnum<TEnum>(this Random random)
#pragma warning restore CS3024 // Constraint type is not CLS-compliant
            where TEnum : struct, Enum
        {
            if (random == null)
                Throw.ArgumentNullException(Argument.random);
            return Enum<TEnum>.GetValues().GetRandomElement(random, true);
        }

        #endregion

        #region GUID

        /// <summary>
        /// Returns a random RFC 4122 compliant <see cref="Guid"/> value generated by using the specified <see cref="Random"/> instance.
        /// </summary>
        /// <param name="random">The <see cref="Random"/> instance to use. Note that if it is a non-derived <see cref="System.Random">System.Random</see> instance, then
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
            if (random == null)
                Throw.ArgumentNullException(Argument.random);

            var result = random.NextBytes(16);
            result[6] = (byte)((result[6] & 0x0F) | 0x40); // the high nibble of 6th byte is 4
            result[8] = (byte)((result[8] & 0b0011_1111) & 0b1000_0000); // the two MSBs of 8th byte are 10b
            return new Guid(result);
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
#if !NET35
        [SecuritySafeCritical]
#endif
        public static T NextObject<T>(this Random random, GenerateObjectSettings settings = null)
        {
            if (random == null)
                Throw.ArgumentNullException(Argument.random);
            return (T)ObjectGenerator.GenerateObject(random, typeof(T), settings ?? GenerateObjectSettings.DefaultSettings);
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
#if !NET35
        [SecuritySafeCritical]
#endif
        public static object NextObject(this Random random, Type type, GenerateObjectSettings settings = null)
        {
            if (random == null)
                Throw.ArgumentNullException(Argument.random);
            if (type == null)
                Throw.ArgumentNullException(Argument.type);
            return ObjectGenerator.GenerateObject(random, type, settings ?? GenerateObjectSettings.DefaultSettings);
        }

        #endregion

        #region Private Methods

#if !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static double DoGetNextDouble(Random random, double minValue, double maxValue, FloatScale scale)
        {
            static double AdjustValue(double value) => Double.IsNegativeInfinity(value) ? Double.MinValue : (Double.IsPositiveInfinity(value) ? Double.MaxValue : value);

            minValue = AdjustValue(minValue);
            maxValue = AdjustValue(maxValue);
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
                var rate = minAbs / maxAbs;
                var absMinValue = Math.Abs(minValue);
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

#if !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static double NextDoubleLinear(Random random, double minValue, double maxValue)
        {
            double sample = random.NextDouble();
            var result = (maxValue * sample) + (minValue * (1d - sample));

            // protecting ourselves against inaccurate calculations; occurs only near MaxValue.
            return result < minValue ? minValue : (result > maxValue ? maxValue : result);
        }

#if !NET35 && !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static decimal NextDecimalLinear(Random random, decimal minValue, decimal maxValue)
        {
            decimal sample = random.NextDecimal();
            return Math.Sign(minValue) * Math.Sign(maxValue) >= 0 
                // ranged version (because the other branch may overflow by 0.5 if both min and max are near MaxValue)
                ? (maxValue - minValue) * sample + minValue
                // wide-range proof version (max - min can be larger than MaxValue)
                : (maxValue * sample) + (minValue * (1m - sample));
        }

        private static string GenerateString(Random random, int length, string allowedCharacters, bool checkInvalid = false)
        {
            if (length == 0)
                return String.Empty;

            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                do
                {
                    result[i] = allowedCharacters?[random.Next(allowedCharacters.Length)] ?? random.NextChar();
                } while (checkInvalid && !result[i].IsValidCharacter());
            }

            return new String(result);
        }

        #endregion

        #endregion
    }
}
