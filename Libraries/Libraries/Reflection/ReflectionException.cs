using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace KGySoft.Libraries.Reflection
{
    /// <summary>
    /// Represent a reflection error.
    /// </summary>
    [Serializable]
    public sealed class ReflectionException: Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionException"/> class.
        /// </summary>
        public ReflectionException() {}
        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionException"/> class.
        /// </summary>
        public ReflectionException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionException"/> class.
        /// </summary>
        public ReflectionException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination. </param>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is null. </exception>
        /// <exception cref="SerializationException">The class name is null or <see cref="Exception.HResult"/> is zero (0). </exception>
        private ReflectionException(
            SerializationInfo info,
            StreamingContext context): base(info, context) {}
    }
}
