using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using KGySoft.Libraries.Reflection;

namespace KGySoft.ComponentModel
{
    // TODO: A beágyazott osztály GetValue/SetValue/GetDefaultValue-ja int-re épít, sima reflection-t használ, a küls? osztály GetProperties/IsSingleFlag-je int-re épít
	/// <summary>
	/// Makes possible for an enumeration that is marked by <see cref="FlagsAttribute"/>
	/// to set each bits as a bool field instead of selecting exactly one element of
	/// the enumeration.
	/// </summary>
	public class FlagsEnumConverter: EnumConverter
	{
		/// <summary>
		/// This class represents an enumeration field in the property grid.
		/// </summary>
		protected class EnumFieldDescriptor: SimplePropertyDescriptor
		{
			#region Fields
			/// <summary>
			/// Stores the context which the enumeration field descriptor was created in.
			/// </summary>
			ITypeDescriptorContext context;

            private IEnumerable<Attribute> attributes;

			#endregion

			#region Methods
			/// <summary>
			/// Creates an instance of the enumeration field descriptor class.
			/// </summary>
			/// <param name="componentType">The type of the enumeration.</param>
			/// <param name="name">The name of the enumeration field.</param>
			/// <param name="context">The current context.</param>
			/// <param name="attributes">Custom attributes of enum field.</param>
			public EnumFieldDescriptor(Type componentType, string name, object[] attributes, ITypeDescriptorContext context)
				: base(componentType, name, typeof(bool))
			{
				this.context = context;
			    this.attributes = attributes.Cast<Attribute>();
			}

			/// <summary>
			/// Retrieves the value of the enumeration field.
			/// </summary>
			/// <param name="component">
			/// The instance of the enumeration type which to retrieve the field value for.
			/// </param>
			/// <returns>
			/// True if the enumeration field is included to the enumeration; 
			/// otherwise, False.
			/// </returns>
			public override object GetValue(object component)
			{
				return ((int)component & (int)Enum.Parse(ComponentType, Name)) != 0;
			}

			/// <summary>
			/// Sets the value of the enumeration field.
			/// </summary>
			/// <param name="component">
			/// The instance of the enumeration type which to set the field value to.
			/// </param>
			/// <param name="value">
			/// True if the enumeration field should included to the enumeration; 
			/// otherwise, False.
			/// </param>
			public override void SetValue(object component, object value)
			{
				bool myValue = (bool)value;
				int myNewValue;
				if (myValue)
					myNewValue = ((int)component) | (int)Enum.Parse(ComponentType, Name);
				else
					myNewValue = ((int)component) & ~(int)Enum.Parse(ComponentType, Name);

				FieldInfo myField = component.GetType().GetField("value__", BindingFlags.Instance | BindingFlags.Public);
				myField.SetValue(component, myNewValue);
				context.PropertyDescriptor.SetValue(context.Instance, component);
			}

			/// <summary>
			/// Retrieves a value indicating whether the enumeration 
			/// field is set to a non-default value.
			/// </summary>
			public override bool ShouldSerializeValue(object component)
			{
				return (bool)GetValue(component) != GetDefaultValue();
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
			private bool GetDefaultValue()
			{
				object myDefaultValue = null;
				string myPropertyName = context.PropertyDescriptor.Name;
				Type myComponentType = context.PropertyDescriptor.ComponentType;

				// Get DefaultValueAttribute
				DefaultValueAttribute myDefaultValueAttribute = (DefaultValueAttribute)Attribute.GetCustomAttribute(
						myComponentType.GetProperty(myPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
						typeof(DefaultValueAttribute));
				if (myDefaultValueAttribute != null)
					myDefaultValue = myDefaultValueAttribute.Value;

				if (myDefaultValue != null)
					return ((int)myDefaultValue & (int)Enum.Parse(ComponentType, Name)) != 0;
				return false;
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
		public FlagsEnumConverter(Type type) : base(type) { }

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
		    if (context == null)
		        return base.GetProperties(context, value, attributes);

		    Type enumType = value.GetType();
		    FieldInfo[] fields = enumType.GetFields(BindingFlags.Static | BindingFlags.Public);
                
		    if (fields.Length > 0)
		    {
		        PropertyDescriptorCollection enumFields = new PropertyDescriptorCollection(null);
		        for (int i = 0; i < fields.Length; i++)
		        {
		            int enumValue = (int)Reflector.GetField(null, fields[i]);
		            if (enumValue != 0 && IsSingleFlag(enumValue))
		            {
		                enumFields.Add(new EnumFieldDescriptor(enumType, fields[i].Name, fields[i].GetCustomAttributes(false), context));
		            }
		        }
		        return enumFields;
		    }
		    return base.GetProperties(context, value, attributes);
		}

        private static bool IsSingleFlag(int i)
        {
            if (i == 0)
                return false;

            uint u = (uint)i;
            while ((u & 1U) == 0U)
            {
                u >>= 1;
            }

            return u == 1U;
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
