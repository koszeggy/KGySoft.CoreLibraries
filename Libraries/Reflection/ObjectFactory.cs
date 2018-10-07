using System;
using System.Linq;
using System.Reflection;

namespace KGySoft.Reflection
{
    /// <summary>
    /// Base class of constructor invokation classes.
    /// Provides static <see cref="GetObjectFactory(System.Reflection.ConstructorInfo)"/> method to obtain invoker of any constructor.
    /// </summary>
    public abstract class ObjectFactory: MemberAccessor
    {
        private Delegate factory;

        /// <summary>
        /// The object creation delegate.
        /// </summary>
        protected Delegate Factory
        {
            get
            {
                if (factory == null)
                {
                    factory = CreateFactory();
                }
                return factory;
            }
        }

        /// <summary>
        /// When overridden, returns a delegate that creates the associated object instance.
        /// </summary>
        protected abstract Delegate CreateFactory();

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectFactory"/> class.
        /// </summary>
        /// <param name="member">Can be a <see cref="Type"/> or a <see cref="ConstructorInfo"/>.</param>
        protected ObjectFactory(MemberInfo member) :
            base(member, (member as ConstructorInfo)?.GetParameters().Select(p => p.ParameterType).ToArray())
        {
        }

        /// <summary>
        /// Retrieves a factory for an object based on a <see cref="Type"/>. Given type must have
        /// a parameterless constructor or type must be <see cref="ValueType"/>.
        /// </summary>
        /// <param name="type"><see cref="Type"/> of the object to create.</param>
        /// <returns>A new instance of <paramref name="type"/>.</returns>
        public static ObjectFactory GetObjectFactory(Type type) 
            => (ObjectFactory)GetCreateAccessor(type ?? throw new ArgumentNullException(nameof(type), Res.Get(Res.ArgumentNull)));

        /// <summary>
        /// Retrieves a factory for an object based on a <see cref="ConstructorInfo"/>.
        /// </summary>
        /// <param name="ctor">The <see cref="ConstructorInfo"/> metadata of the object to create.</param>
        /// <returns>A new instance of the object created by the provided constructor.</returns>
        public static ObjectFactory GetObjectFactory(ConstructorInfo ctor) 
            => (ObjectFactory)GetCreateAccessor(ctor ?? throw new ArgumentNullException(nameof(ctor), Res.Get(Res.ArgumentNull)));

        /// <summary>
        /// Non-caching version of object factory creation.
        /// </summary>
        internal static ObjectFactory CreateObjectFactory(MemberInfo member)
        {
            switch (member)
            {
                case ConstructorInfo ci:
                    return new ObjectFactoryParameterized(ci);
                case Type t:
                    return new ObjectFactoryDefault(t);
                default:
                    throw new ArgumentException(Res.Get(Res.TypeOrCtorInfoExpected), nameof(member));
            }
        }

        /// <summary>
        /// Creates a new instance of the object.
        /// In case of a parameterless constructor <paramref name="parameters"/> parameter is omitted (can be <see langword="null"/>).
        /// </summary>
        /// <remarks>
        /// <note>
        /// First usage of an object creator can be even slower than using <see cref="MethodBase.Invoke(object,object[])"/> of System.Reflection
        /// but further calls are much more fast.
        /// </note>
        /// </remarks>
        public abstract object Create(params object[] parameters);
    }
}
