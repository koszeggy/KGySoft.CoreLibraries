#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FlagsEnumConverter.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// By extending the <see cref="EnumConverter"/>, this class provides a type converter for flags <see cref="Enum"/> instances (not necessarily but typically marked by <see cref="FlagsAttribute"/>) by providing <see cref="bool"/> properties for each flags in the specific <see cref="Enum"/> type.
    /// </summary>
    public class FlagsEnumConverter : EnumConverter
    {
        #region EnumFieldDescriptor class

        /// <summary>
        /// Represents an enumeration flag as a <see cref="bool"/> property.
        /// </summary>
        private class EnumFlagDescriptor : SimplePropertyDescriptor
        {
            #region Fields

            private readonly ulong flagValue;
            private readonly ulong defaultValue;
            private readonly FieldInfo valueField;
            private readonly ITypeDescriptorContext? context;
            private readonly Attribute[] attributes;

            #endregion

            #region Properties

            /// <summary>
            /// Gets the collection of attributes for this member.
            /// </summary>
            public override AttributeCollection Attributes => new AttributeCollection(attributes.Union(new Attribute[] { RefreshPropertiesAttribute.Repaint }).ToArray());

            #endregion

            #region Constructors

            /// <summary>
            /// Creates an instance of the enumeration field descriptor class.
            /// </summary>
            /// <param name="componentType">The type of the enumeration.</param>
            /// <param name="name">The name of the <see langword="enum"/>&#160;flag field.</param>
            /// <param name="flagValue">The value of the <see langword="enum"/>&#160;flag.</param>
            /// <param name="context">The current context or <see langword="null"/>&#160;if no context exists.</param>
            /// <param name="defaultValue">The default value of the <see langword="enum"/>&#160;instance specified by the <see cref="DefaultValueAttribute"/> of its property or <see langword="null"/>.</param>
            /// <param name="valueField">The underlying value field of the <see langword="enum"/>&#160;type.</param>
            /// <param name="attributes">Custom attributes of the <see langword="enum"/>&#160;flag field.</param>
            internal EnumFlagDescriptor(Type componentType, string name, ulong flagValue, ulong defaultValue, FieldInfo valueField, Attribute[] attributes, ITypeDescriptorContext? context)
                : base(componentType, name, Reflector.BoolType)
            {
                this.flagValue = flagValue;
                this.defaultValue = defaultValue;
                this.valueField = valueField;
                this.context = context;
                this.attributes = attributes;
            }

            #endregion

            #region Methods

            #region Public Methods

            /// <summary>
            /// Retrieves the value of the <see cref="Enum"/> flag.
            /// </summary>
            /// <param name="component">The <see cref="Enum"/> instance to retrieve the flag value for.</param>
            /// <returns><see langword="true"/>&#160;if the enumeration field is included to the enumeration; otherwise, <see langword="false"/>.</returns>
            public override object GetValue(object? component)
                => (((Enum)component!).ToUInt64() & flagValue) != 0UL;

            /// <summary>
            /// Sets the value of the <see cref="Enum"/> flag.
            /// </summary>
            /// <param name="component">The <see cref="Enum"/> instance whose flag is about to be set.</param>
            /// <param name="value">The <see cref="bool"/> value of the flag to set.</param>
            public override void SetValue(object? component, object? value)
            {
                // bit manipulation as UInt64
                bool bit = (bool)value!;
                ulong result = ((Enum)component!).ToUInt64();
                if (bit)
                    result |= flagValue;
                else
                    result &= ~flagValue;

                // trick: setting the value field in the enum instance so the boxed component variable will be reassigned
                Reflector.SetField(component, valueField, Reflector.GetField(Enum.ToObject(ComponentType, result), valueField));

                // if there is a context (eg. property grid), then we set the enum property of its parent instance
                if (context?.Instance != null)
                    context.PropertyDescriptor?.SetValue(context.Instance, component);
            }

            /// <summary>
            /// Returns whether the value of this property can persist.
            /// </summary>
            /// <param name="component">The <see cref="Enum"/> instance with the property that is to be examined for persistence.</param>
            /// <returns><see langword="true"/>&#160;if the value of the property can persist; otherwise, <see langword="false" />.</returns>
            public override bool ShouldSerializeValue(object component) => !Equals(GetValue(component), GetDefaultValue());

            /// <summary>
            /// Resets the value for this property of the component.
            /// </summary>
            /// <param name="component">The <see cref="Enum"/> instance with the property value to be reset.</param>
            public override void ResetValue(object component) => SetValue(component, GetDefaultValue());

            /// <summary>
            /// Returns whether resetting the component changes the value of the component.
            /// </summary>
            /// <param name="component">The <see cref="Enum"/> instance to test for reset capability.</param>
            /// <returns><see langword="true"/>&#160;if resetting the component changes the value of the component; otherwise, <see langword="false" />.</returns>
            public override bool CanResetValue(object component) => ShouldSerializeValue(component);

            #endregion

            #region Private Methods

            /// <summary>
            /// Retrieves the default value of the <see langword="enum"/>&#160;flag.
            /// </summary>
            private object GetDefaultValue() => (defaultValue & flagValue) != 0UL;

            #endregion

            #endregion
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance of the <see cref="FlagsEnumConverter"/> class.
        /// </summary>
        /// <param name="type">The type of the enumeration.</param>
        public FlagsEnumConverter(Type type) : base(type) { }

        #endregion

        #region Methods

        /// <summary>
        /// Retrieves the property descriptors for the enumeration fields.
        /// These property descriptors will be used by the property grid
        /// to show separate enumeration fields.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. If specified, will be used to determine the <see cref="DefaultValueAttribute"/> of
        /// the converted <see langword="enum"/>, and to set the <see langword="enum"/>&#160;property of the container instance if one of the flags are set.</param>
        /// <param name="value">The <see cref="Enum" /> instance to get the flags for.</param>
        /// <param name="attributes">An array of type <see cref="Attribute"/> that is used as a filter. In this method this parameter is ignored.</param>
        /// <returns>A <see cref="PropertyDescriptorCollection" /> with the flags of the <see cref="Enum"/> type designated by <paramref name="value"/> as <see cref="bool"/> properties.</returns>
        public override PropertyDescriptorCollection? GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes)
        {
            if (value == null!)
                Throw.ArgumentNullException(Argument.value);
            Type enumType = value.GetType();
            if (!enumType.IsEnum)
                Throw.ArgumentException(Argument.value, Res.NotAnInstanceOfType(Reflector.EnumType));

            // Obtaining enum fields by reflection. GetNames/Values could be also used but this way we get also the attributes.
            FieldInfo[] fields = enumType.GetFields(BindingFlags.Static | BindingFlags.Public);
            if (fields.Length == 0)
                return base.GetProperties(context, value, attributes);

            // this is how value field is obtained in Type.GetEnumUnderlyingType
            FieldInfo valueField = enumType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)[0];
            DefaultValueAttribute? defaultAttr = context?.PropertyDescriptor?.Attributes[typeof(DefaultValueAttribute)] as DefaultValueAttribute;
            ulong defaultValue = defaultAttr?.Value?.GetType() == enumType ? ((Enum)defaultAttr.Value).ToUInt64() : 0UL;
            PropertyDescriptorCollection enumFields = new PropertyDescriptorCollection(null);
            foreach (FieldInfo field in fields)
            {
                Enum enumValue = (Enum)Reflector.GetField(null, field)!;
                if (enumValue.IsSingleFlag())
                    enumFields.Add(new EnumFlagDescriptor(enumType, field.Name, enumValue.ToUInt64(), defaultValue, valueField, Attribute.GetCustomAttributes(field, false), context));
            }

            return enumFields;
        }

        /// <summary>
        /// Returns whether this object supports properties, using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this method this parameter is ignored.</param>
        /// <returns>This method always returns <see langword="true" />.</returns>
        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;

        /// <summary>
        /// Gets whether this object supports a standard set of values that can be picked from a list using the specified context.
        /// </summary>
        /// <param name="context">An <see cref="ITypeDescriptorContext" /> that provides a format context. In this method this parameter is ignored.</param>
        /// <returns>This method always returns <see langword="false" />.</returns>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => false;

        #endregion
    }
}
