#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PropertyAccessor.cs
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
using System.Linq;
using System.Reflection;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Provides an efficient way for setting and getting property values via dynamically created delegates.
    /// <br/>See the <strong>Remarks</strong> section for details and an example.
    /// </summary>
    /// <remarks>
    /// <para>You can obtain a <see cref="PropertyAccessor"/> instance by the static <see cref="GetAccessor">GetAccessor</see> method.</para>
    /// <para>The <see cref="Get">Get</see> and <see cref="Set">Set</see> methods can be used to get and set the property, respectively.
    /// The first call of these methods are slow because the delegates are generated on the first access, but further calls are much faster.</para>
    /// <para>The already obtained accessors are cached so subsequent <see cref="GetAccessor">GetAccessor</see> calls return the already created accessors unless
    /// they were dropped out from the cache, which can store about 8000 elements.</para>
    /// <note>If you want to access a property by name rather then by a <see cref="PropertyInfo"/>, then you can use the <see cref="O:KGySoft.Reflection.Reflector.SetProperty">SetProperty</see>
    /// and <see cref="O:KGySoft.Reflection.Reflector.SetProperty">GetProperty</see> methods in the <see cref="Reflector"/> class, which have some overloads with a <c>propertyName</c> parameter.</note>
    /// </remarks>
    /// <example>
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Reflection;
    /// using KGySoft.Diagnostics;
    /// using KGySoft.Reflection;
    /// 
    /// class Example
    /// {
    ///     private class TestClass
    ///     {
    ///         public int TestProperty { get; set; }
    ///     }
    /// 
    ///     static void Main(string[] args)
    ///     {
    ///         var instance = new TestClass();
    ///         PropertyInfo property = instance.GetType().GetProperty(nameof(TestClass.TestProperty));
    ///         PropertyAccessor accessor = PropertyAccessor.GetAccessor(property);
    /// 
    ///         const int iterations = 1000000;
    ///         for (int i = 0; i < iterations; i++)
    ///         {
    ///             int result;
    ///             using (Profiler.Measure(GetCategory(i), "Direct set"))
    ///                 instance.TestProperty = i;
    ///             using (Profiler.Measure(GetCategory(i), "Direct get"))
    ///                 result = instance.TestProperty;
    /// 
    ///             using (Profiler.Measure(GetCategory(i), "PropertyAccessor.Set"))
    ///                 accessor.Set(instance, i);
    ///             using (Profiler.Measure(GetCategory(i), "PropertyAccessor.Get"))
    ///                 result = (int)accessor.Get(instance);
    /// 
    ///             using (Profiler.Measure(GetCategory(i), "PropertyInfo.SetValue"))
    ///                 property.SetValue(instance, i);
    ///             using (Profiler.Measure(GetCategory(i), "PropertyInfo.GetValue"))
    ///                 result = (int)property.GetValue(instance);
    ///         }
    /// 
    ///         string GetCategory(int i) => i < 1 ? "Warm-up" : "Test";
    ///         foreach (IMeasureItem item in Profiler.GetMeasurementResults())
    ///         {
    ///             Console.WriteLine($@"[{item.Category}] {item.Operation}: {item.TotalTime.TotalMilliseconds} ms{(item.NumberOfCalls > 1
    ///                 ? $" (average: {item.TotalTime.TotalMilliseconds / item.NumberOfCalls} ms from {item.NumberOfCalls} calls)" : null)}");
    ///         }
    ///     }
    /// }
    /// 
    /// // This code example produces the following output:
    /// // [Warm-up] Direct set: 0.1437 ms
    /// // [Warm-up] Direct get: 0.0021 ms
    /// // [Warm-up] PropertyAccessor.Set: 1.1377 ms
    /// // [Warm-up] PropertyAccessor.Get: 0.8056 ms
    /// // [Warm-up] PropertyInfo.SetValue: 0.0395 ms
    /// // [Warm-up] PropertyInfo.GetValue: 0.0261 ms
    /// // [Test] Direct set: 30.6068 ms (average: 3.06068306068306E-05 ms from 999999 calls)
    /// // [Test] Direct get: 30.3644 ms (average: 3.03644303644304E-05 ms from 999999 calls)
    /// // [Test] PropertyAccessor.Set: 67.3082 ms (average: 6.73082673082673E-05 ms from 999999 calls)
    /// // [Test] PropertyAccessor.Get: 64.3375 ms (average: 6.43375643375643E-05 ms from 999999 calls)
    /// // [Test] PropertyInfo.SetValue: 257.4104 ms (average: 0.000257410657410657 ms from 999999 calls)
    /// // [Test] PropertyInfo.GetValue: 204.5283 ms (average: 0.000204528504528505 ms from 999999 calls)]]></code>
    /// </example>
    public abstract class PropertyAccessor : MemberAccessor
    {
        #region Fields

        private Delegate getter;
        private Delegate setter;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets whether the property can be read (has get accessor).
        /// </summary>
        public bool CanRead => ((PropertyInfo)MemberInfo).CanRead;

        /// <summary>
        /// Gets whether the property can be written (has set accessor).
        /// </summary>
        public bool CanWrite => ((PropertyInfo)MemberInfo).CanWrite;

        #endregion

        #region Internal Properties

        /// <summary>
        /// Gets the property getter delegate.
        /// </summary>
        internal /*private protected*/ Delegate Getter => getter ?? (getter = CanRead
            ? CreateGetter()
            : throw new NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name)));

        /// <summary>
        /// Gets the property setter delegate.
        /// </summary>
        internal /*private protected*/ Delegate Setter => setter ?? (setter = CanWrite
            ? CreateSetter()
            : throw new NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name)));

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyAccessor"/> class.
        /// </summary>
        /// <param name="property">The property for which the accessor is to be created.</param>
        protected PropertyAccessor(PropertyInfo property) :
            base(property, property.GetIndexParameters().Select(p => p.ParameterType).ToArray())
        {
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        /// <summary>
        /// Gets a <see cref="PropertyAccessor"/> for the specified <paramref name="property"/>.
        /// </summary>
        /// <param name="property">The property for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="PropertyAccessor"/> instance that can be used to get or set the property.</returns>
        public static PropertyAccessor GetAccessor(PropertyInfo property)
            => (PropertyAccessor)GetCreateAccessor(property ?? throw new ArgumentNullException(nameof(property), Res.ArgumentNull));

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates an accessor for a property without caching.
        /// </summary>
        /// <param name="property">The property for which an accessor should be created.</param>
        /// <returns>A <see cref="PropertyAccessor"/> instance that can be used to get or set the property.</returns>
        internal static PropertyAccessor CreateAccessor(PropertyInfo property)
            => property.GetIndexParameters().Length == 0
                ? (PropertyAccessor)new SimplePropertyAccessor(property)
                : new IndexerAccessor(property);

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Sets the property.
        /// For static properties the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// If the property is not an indexer, then <paramref name="indexerParameters"/> parameter is omitted.
        /// </summary>
        /// <param name="instance">The instance that the property belongs to. Can be <see langword="null"/>&#160;for static properties.</param>
        /// <param name="value">The value to be set.</param>
        /// <param name="indexerParameters">The parameters if the property is an indexer.</param>
        /// <remarks>
        /// <note>
        /// Setting the property for the first time is slower than the <see cref="PropertyInfo.SetValue(object,object)">System.Reflection.PropertyInfo.SetValue</see>
        /// method but further calls are much faster.
        /// </note>
        /// </remarks>
        public abstract void Set(object instance, object value, params object[] indexerParameters);

        /// <summary>
        /// Gets the value of the property.
        /// For static properties the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// If the property is not an indexer, then <paramref name="indexerParameters"/> parameter is omitted.
        /// </summary>
        /// <param name="instance">The instance that the property belongs to. Can be <see langword="null"/>&#160;for static properties.</param>
        /// <param name="indexerParameters">The parameters if the property is an indexer.</param>
        /// <returns>The value of the property.</returns>
        /// <remarks>
        /// <note>
        /// Getting the property for the first time is slower than the <see cref="PropertyInfo.GetValue(object,object[])">System.Reflection.PropertyInfo.GetValue</see>
        /// method but further calls are much faster.
        /// </note>
        /// </remarks>
        public abstract object Get(object instance, params object[] indexerParameters);

        #endregion

        #region Internal Methods

        /// <summary>
        /// In a derived class returns a delegate that executes the getter method of the property.
        /// </summary>
        /// <returns>A delegate instance that can be used to get the value of the property.</returns>
        internal /*private protected*/ abstract Delegate CreateGetter();

        /// <summary>
        /// In a derived class returns a delegate that executes the setter method of the property.
        /// </summary>
        /// <returns>A delegate instance that can be used to set the property.</returns>
        internal /*private protected*/ abstract Delegate CreateSetter();

        #endregion

        #endregion

        #endregion
    }
}
