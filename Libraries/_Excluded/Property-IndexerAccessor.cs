using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace KGySoft.Libraries.Reflection
{
    /// <summary>
    /// Represents a non-indexed property.
    /// </summary>
    /// <typeparam name="T">The type that contains the property.</typeparam>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    public sealed class PropertyAccessor<T, TProperty>: PropertyAccessor
    {
        /// <summary>
        /// Private constructor to make possible fast dynamic creation.
        /// </summary>
        private PropertyAccessor()
            : base(null, typeof(T))
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="PropertyAccessor&lt;T, TProperty&gt;"/> class.
        /// </summary>
        public PropertyAccessor(PropertyInfo property)
            : base(property, typeof(T))
        {
            if (property == null)
                throw new ArgumentNullException("property");
        }

        /// <summary>
        /// Sets the property in a non-generic way.
        /// If possible use the <see cref="Set(T, TProperty)"/> overload for better performance.
        /// </summary>
        /// <param name="instance">The object instance that contains the property to set.</param>
        /// <param name="value">The new value of the property.</param>
        /// <param name="indexerParameters">Ignored because this class represents a non-indexed property.</param>
        public override void Set(object instance, object value, object[] indexerParameters)
        {
            ((Action<T, TProperty>)Setter)((T)instance, (TProperty)value);
        }

        /// <summary>
        /// Sets the property.
        /// </summary>
        /// <param name="instance">The object instance that contains the property to set.</param>
        /// <param name="value">The new value of the property.</param>
        public void Set(T instance, TProperty value)
        {
            ((Action<T, TProperty>)Setter)(instance, value);
        }

        public override object Get(object instance, object[] indexerParameters)
        {
            throw new NotImplementedException();
        }
    }
}
