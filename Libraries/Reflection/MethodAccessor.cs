#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MethodAccessor.cs
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
    /// Provides an efficient way for invoking methods via dynamically created delegates.
    /// You can obtain a <see cref="MethodAccessor"/> instance by the static <see cref="GetAccessor">GetAccessor</see> method.
    /// </summary>
    public abstract class MethodAccessor : MemberAccessor
    {
        #region Fields

        private Delegate invoker;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the method invoker delegate.
        /// </summary>
        protected Delegate Invoker => invoker ?? (invoker = CreateInvoker());

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodAccessor"/> class.
        /// </summary>
        /// <param name="method">The method for which the accessor is to be created.</param>
        protected MethodAccessor(MethodBase method) :
            base(method, method.GetParameters().Select(p => p.ParameterType).ToArray())
        {
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        /// <summary>
        /// Gets a <see cref="MemberAccessor"/> for the specified <paramref name="method"/>.
        /// </summary>
        /// <param name="method">The method for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="MethodAccessor"/> instance that can be used to invoke the method.</returns>
        public static MethodAccessor GetAccessor(MethodInfo method)
            => (MethodAccessor)GetCreateAccessor(method ?? throw new ArgumentNullException(nameof(method), Res.ArgumentNull));

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates an accessor for a property without caching.
        /// </summary>
        /// <param name="method">The method for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="MethodAccessor"/> instance that can be used to invoke the method.</returns>
        internal static MethodAccessor CreateAccessor(MethodInfo method) => method.ReturnType == Reflector.VoidType
            ? (MethodAccessor)new ActionMethodAccessor(method)
            : new FunctionMethodAccessor(method);

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Invokes the method. The return value of <see cref="Void"/> methods are <see langword="null"/>.
        /// For static methods the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// </summary>
        /// <param name="instance">The instance that the method belongs to. Can be <see langword="null"/>&#160;for static methods.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method, or <see langword="null"/> for <see cref="Void"/> methods.</returns>
        /// <remarks>
        /// <note>
        /// Invoking the method for the first time is slower than the <see cref="MethodBase.Invoke(object,object[])">System.Reflection.MethodBase.Invoke</see>
        /// method but further calls are much faster.
        /// </note>
        /// </remarks>
        public abstract object Invoke(object instance, params object[] parameters);

        #endregion

        #region Protected Methods

        /// <summary>
        /// In a derived class returns a delegate that executes the method.
        /// </summary>
        /// <returns>A delegate instance that can be used to invoke the method.</returns>
        protected abstract Delegate CreateInvoker();

        #endregion

        #endregion

        #endregion
    }
}
