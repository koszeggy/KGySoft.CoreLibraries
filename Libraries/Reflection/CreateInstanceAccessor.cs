#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CreateInstanceAccessor.cs
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
    /// Provides an efficient way for creating objects via dynamically created delegates.
    /// You can obtain a <see cref="CreateInstanceAccessor"/> instance by the static <see cref="O:KGySoft.Reflection.CreateInstanceAccessor.GetAccessor">GetAccessor</see> overloads.
    /// </summary>
    public abstract class CreateInstanceAccessor : MemberAccessor
    {
        #region Fields

        private Delegate initializer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the instance creator delegate.
        /// </summary>
        internal /*private protected*/ Delegate Initializer => initializer ?? (initializer = CreateInitializer());

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateInstanceAccessor"/> class.
        /// </summary>
        /// <param name="member">Can be a <see cref="Type"/> or a <see cref="ConstructorInfo"/>.</param>
        protected CreateInstanceAccessor(MemberInfo member) :
            base(member, (member as ConstructorInfo)?.GetParameters().Select(p => p.ParameterType).ToArray())
        {
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        /// <summary>
        /// Gets a <see cref="CreateInstanceAccessor"/> for the specified <see cref="Type"/>.
        /// Given <paramref name="type"/> must have a parameterless constructor or type must be <see cref="ValueType"/>.
        /// </summary>
        /// <param name="type">A <see cref="Type"/> for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="CreateInstanceAccessor"/> instance that can be used to create an instance of <paramref name="type"/>.</returns>
        public static CreateInstanceAccessor GetAccessor(Type type)
            => (CreateInstanceAccessor)GetCreateAccessor(type ?? throw new ArgumentNullException(nameof(type), Res.ArgumentNull));

        /// <summary>
        /// Gets a <see cref="CreateInstanceAccessor"/> for the specified <see cref="ConstructorInfo"/>.
        /// </summary>
        /// <param name="ctor">The constructor for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="CreateInstanceAccessor"/> instance that can be used to create an instance by the constructor.</returns>
        public static CreateInstanceAccessor GetAccessor(ConstructorInfo ctor)
            => (CreateInstanceAccessor)GetCreateAccessor(ctor ?? throw new ArgumentNullException(nameof(ctor), Res.ArgumentNull));

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates an accessor for a constructor or type without caching.
        /// </summary>
        internal static CreateInstanceAccessor CreateAccessor(MemberInfo member)
        {
            switch (member)
            {
                case ConstructorInfo ci:
                    return new ParameterizedCreateInstanceAccessor(ci);
                case Type t:
                    return new DefaultCreateInstanceAccessor(t);
                default:
                    throw new ArgumentException(Res.ReflectionTypeOrCtorInfoExpected, nameof(member));
            }
        }

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Creates a new instance by the associated <see cref="ConstructorInfo"/> or <see cref="Type"/>.
        /// For types and parameterless constructors the <paramref name="parameters"/> parameter is omitted.
        /// </summary>
        /// <param name="parameters">The parameters for parameterized constructors.</param>
        /// <returns>The created instance.</returns>
        /// <remarks>
        /// <note>
        /// Invoking the constructor for the first time is slower than the <see cref="MethodBase.Invoke(object,object[])">System.Reflection.MethodBase.Invoke</see>
        /// method but further calls are much faster.
        /// </note>
        /// </remarks>
        public abstract object CreateInstance(params object[] parameters);

        #endregion

        #region Internal Methods

        /// <summary>
        /// In a derived class returns a delegate that creates the new instance.
        /// </summary>
        /// <returns>A delegate instance that can be used to invoke the method.</returns>
        internal /*private protected*/ abstract Delegate CreateInitializer();

        #endregion

        #endregion

        #endregion
    }
}
