using System;
using System.Reflection;

namespace KGySoft.Libraries.Reflection
{
    using KGySoft.Libraries.Resources;

    /// <summary>
    /// Base class of property and indexer accessor classes.
    /// Provides static <see cref="GetPropertyAccessor"/> method to obtain invoker of any property or indexer.
    /// </summary>
    public abstract class PropertyAccessor: MemberAccessor
    {
        private Delegate getter;
        private Delegate setter;

        /// <summary>
        /// The property getter delegate.
        /// </summary>
        protected Delegate Getter
        {
            get
            {
                if (getter == null)
                {
                    if (!CanRead)
                        throw new NotSupportedException(Res.Get(Res.PropertyHasNoGetter, DeclaringType, MemberInfo));

                    getter = CreateGetter();
                }
                return getter;
            }
        }

        /// <summary>
        /// When overridden, returns a delegate that executes the getter method of associated property.
        /// </summary>
        protected abstract Delegate CreateGetter();

        /// <summary>
        /// The property setter delegate.
        /// </summary>
        protected Delegate Setter
        {
            get
            {
                if (setter == null)
                {
                    if (!CanWrite)
                        throw new NotSupportedException(Res.Get(Res.PropertyHasNoSetter, DeclaringType, MemberInfo));

                    setter = CreateSetter();
                }
                return setter;
            }
        }

        /// <summary>
        /// When overridden, returns a delegate that executes the setter method of associated property.
        /// </summary>
        protected abstract Delegate CreateSetter();

        /// <summary>
        /// Gets whether the property can be read (has get accessor).
        /// </summary>
        public bool CanRead
        {
            get { return ((PropertyInfo)MemberInfo).CanRead; }
        }

        /// <summary>
        /// Gets whether the property can be written to (has set accessor).
        /// </summary>
        public bool CanWrite
        {
            get { return ((PropertyInfo)MemberInfo).CanWrite; }
        }

        /// <summary>
        /// Creates a new PropertyAccessor.
        /// </summary>
        protected PropertyAccessor(PropertyInfo property, Type declaringType, params Type[] indexerParameterTypes) :
            base(property, declaringType, indexerParameterTypes)
        {
        }

        /// <summary>
        /// Creates an accessor for the <paramref name="property"/> that provides faster
        /// property access than <see cref="PropertyInfo"/>.
        /// </summary>
        public static PropertyAccessor GetPropertyAccessor(PropertyInfo property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property), Res.Get(Res.ArgumentNull));
            if (CachingEnabled)
                return (PropertyAccessor)GetCreateAccessor(property);
            else
                return CreatePropertyAccessor(property);
        }

        /// <summary>
        /// Non-caching version of property accessor creation.
        /// </summary>
        internal static PropertyAccessor CreatePropertyAccessor(PropertyInfo property)
        {
            ParameterInfo[] indexParameters = property.GetIndexParameters() ?? new ParameterInfo[0];
            Type[] parameterTypes = new Type[indexParameters.Length];
            for (int i = 0; i < indexParameters.Length; i++)
            {
                parameterTypes[i] = indexParameters[i].ParameterType;
            }

            // late-initialization of MemberInfo to avoid caching
            if (indexParameters.Length == 0)
                return new SimplePropertyAccessor(property.DeclaringType) { MemberInfo = property };
            else
                return new IndexerAccessor(property.DeclaringType, parameterTypes) { MemberInfo = property };
        }

        /// <summary>
        /// Sets the property.
        /// In case of a static static property <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// If property is not an indexer, then <paramref name="indexerParameters"/> parameter is omitted.
        /// </summary>
        /// <remarks>
        /// <note>
        /// First set of a property can be even slower than using <see cref="PropertyInfo.SetValue(object,object,object[])"/> of System.Reflection
        /// but further calls are much more fast.
        /// </note>
        /// </remarks>
        public abstract void Set(object instance, object value, params object[] indexerParameters);

        /// <summary>
        /// Gets and returns the value of a property.
        /// In case of a static static property <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// If property is not an indexer, then <paramref name="indexerParameters"/> parameter is omitted.
        /// </summary>
        /// <remarks>
        /// <note>
        /// Getting a property value at first time can be even slower than using <see cref="PropertyInfo.GetValue(object,object[])"/> of System.Reflection
        /// but further calls are much more fast.
        /// </note>
        /// </remarks>
        public abstract object Get(object instance, params object[] indexerParameters);
    }
}
