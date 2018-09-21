using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using KGySoft.Libraries;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a type converter to provide <see cref="bool"/> properties for each flags of an <see cref="Enum"/> instance.
    /// </summary>
    public class FlagsEnumConverter2: EnumConverter
	{
		/// <summary>
		/// This class represents an enumeration field in the property grid.
		/// </summary>
		protected class EnumFieldDescriptor: SimplePropertyDescriptor
		{
			#region Fields

		    private readonly Enum flagValue;
		    private readonly DefaultValueAttribute defaultValue;
		    private readonly FieldInfo valueField;
		    private readonly ITypeDescriptorContext context;
            private readonly Attribute[] attributes;
		    private readonly bool isUInt64;

            #endregion

            #region Methods

            /// <summary>
            /// Creates an instance of the enumeration field descriptor class.
            /// </summary>
            /// <param name="componentType">The type of the enumeration.</param>
            /// <param name="name">The name of the <see langword="enum"/> flag field.</param>
            /// <param name="value">The value of the <see langword="enum"/> flag.</param>
            /// <param name="context">The current context or <see langword="null"/> if no context exists.</param>
            /// <param name="defaultValue">The default value of the <see langword="enum"/> instance specified by the <see cref="DefaultValueAttribute"/> of its property or <see langword="null"/>.</param>
            /// <param name="valueField">The underlying value field of the <see langword="enum"/> type.</param>
            /// <param name="attributes">Custom attributes of the <see langword="enum"/> flag field.</param>
            public EnumFieldDescriptor(Type componentType, string name, Enum value, DefaultValueAttribute defaultValue, FieldInfo valueField, Attribute[] attributes, ITypeDescriptorContext context)
				: base(componentType, name, typeof(bool))
            {
                isUInt64 = valueField.FieldType == typeof(ulong);
			    flagValue = value;
                this.defaultValue = defaultValue;
                this.valueField = valueField;
                this.context = context;
			    this.attributes = attributes;
			}

            /// <summary>
            /// Retrieves the value of the <see cref="Enum"/> flag.
            /// </summary>
            /// <param name="component">The <see cref="Enum"/> instance to retrieve the flag value for.</param>
            /// <returns><see langword="true"/> if the enumeration field is included to the enumeration; otherwise, <see langword="false"/>.</returns>
            public override object GetValue(object component)
            {
                return isUInt64 
                    ? (Convert.ToUInt64(component) & Convert.ToUInt64(flagValue)) != 0UL 
                    : (Convert.ToInt64(component) & Convert.ToInt64(flagValue)) != 0L;
            }

            /// <summary>
            /// Sets the value of the <see cref="Enum"/> flag.
            /// </summary>
            /// <param name="component">The <see cref="Enum"/> instance whose flag is about to be set.</param>
            /// <param name="value">The <see cref="bool"/> value of the flag to set.</param>
            public override void SetValue(object component, object value)
			{
                // bit manipulation as UInt64
				bool bit = (bool)value;
			    ulong result = isUInt64 ? Convert.ToUInt64(component) : (ulong)Convert.ToInt64(component);
                ulong rawFlagValue = isUInt64 ? Convert.ToUInt64(flagValue) : (ulong)Convert.ToInt64(flagValue);
			    if (bit)
			        result |= rawFlagValue;
			    else
			        result &= ~rawFlagValue;

                // setting the value field in the enum instance
                Reflector.SetField(component, valueField, Convert.ChangeType(isUInt64 ? result : (object)(long)result, valueField.FieldType));

                // if there is a context (eg. property grid), then we set the enum property of its parent object
                if (context?.Instance != null)
				    context.PropertyDescriptor?.SetValue(context.Instance, component);
			}

			/// <summary>
			/// Retrieves a value indicating whether the enumeration 
			/// field is set to a non-default value.
			/// </summary>
			public override bool ShouldSerializeValue(object component)
			{
			    // ReSharper disable once AssignNullToNotNullAttribute
				return !Equals(GetValue(component), GetDefaultValue());
			}

			/// <summary>
			/// Resets the enumeration field to its default value.
			/// </summary>
			public override void ResetValue(object component)
			{
				SetValue(component, GetDefaultValue());
			}

			/// <summary>
			/// Retrieves a value indicating whether the enumeration 
			/// field can be reset to the default value.
			/// </summary>
			public override bool CanResetValue(object component)
			{
				return ShouldSerializeValue(component);
			}

			/// <summary>
			/// Retrieves the enumerations field's default value.
			/// </summary>
			private object GetDefaultValue()
			{
			    if (!(defaultValue?.Value is IConvertible convertible))
			        return false;

			    object enumValue = convertible is string strValue
			        ? Enum.Parse(ComponentType, strValue)
			        : Enum.ToObject(ComponentType, convertible);

			    return GetValue(enumValue);
			}

			#endregion

			#region Properties

			public override AttributeCollection Attributes
			{
				get
				{
					return new AttributeCollection(attributes.Union(new Attribute[] { RefreshPropertiesAttribute.Repaint }).ToArray());
				}
			}

			#endregion
		}

		#region Methods

		/// <summary>
		/// Creates an instance of the FlagsEnumConverter class.
		/// </summary>
		/// <param name="type">The type of the enumeration.</param>
		public FlagsEnumConverter2(Type type) : base(type) { }

		/// <summary>
		/// Retrieves the property descriptors for the enumeration fields. 
		/// These property descriptors will be used by the property grid 
		/// to show separate enumeration fields.
		/// </summary>
		/// <param name="context">The current context.</param>
		/// <param name="value">A value of an enumeration type.</param>
        /// <param name="attributes">An array of type <see cref="T:System.Attribute"/> that is used as a filter.</param>
		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
	    {
	        Type enumType = value.GetType();
            if (!enumType.IsEnum)
                throw new ArgumentException(Res.Get(Res.NotAnInstanceOfType, typeof(Enum)), nameof(value));

            // Obtaining enum fields by reflection. GetNames/Values could be also used but this way be get also the attributes.
	        FieldInfo[] fields = enumType.GetFields(BindingFlags.Static | BindingFlags.Public);
	        if (fields.Length == 0)
	            return base.GetProperties(context, value, attributes);

            // this is how value field is obtained in Type.GetEnumUnderlyingType
            FieldInfo valueField = enumType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)[0];
	        DefaultValueAttribute defaultValue = context?.PropertyDescriptor?.Attributes?[typeof(DefaultValueAttribute)] as DefaultValueAttribute;
            PropertyDescriptorCollection enumFields = new PropertyDescriptorCollection(null);
	        foreach (FieldInfo field in fields)
	        {
	            Enum enumValue = (Enum)Reflector.GetField(null, field);
	            if (enumValue.IsSingleFlag())
	                enumFields.Add(new EnumFieldDescriptor(enumType, field.Name, enumValue, defaultValue, valueField, Attribute.GetCustomAttributes(field, false), context));
	        }

	        return enumFields;
	    }

        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
		{
		    return context != null || base.GetPropertiesSupported(context);
		}

	    public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return false;
		}
		#endregion
	}
}
