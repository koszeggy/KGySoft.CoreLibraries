#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PropertyChangedEventArgsExtensions.cs
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

using System.ComponentModel;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Extension methods for the <see cref="PropertyChangedEventArgs"/> type.
    /// </summary>
    public static class PropertyChangedEventArgsExtensions
    {
        #region Methods

        /// <summary>
        /// If the specified event <paramref name="args"/> is a <see cref="PropertyChangedExtendedEventArgs"/> instance, then gets the property value before the change.
        /// </summary>
        /// <param name="args">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <param name="oldValue">If the specified event <paramref name="args"/> is a <see cref="PropertyChangedExtendedEventArgs"/> instance, then the property value before the change; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/>&#160;if the specified event <paramref name="args"/> is a <see cref="PropertyChangedExtendedEventArgs"/> instance; otherwise, <see langword="null"/>.</returns>
        public static bool TryGetOldPropertyValue(this PropertyChangedEventArgs args, out object? oldValue)
        {
            if (args is PropertyChangedExtendedEventArgs ext)
            {
                oldValue = ext.OldValue;
                return !ObservableObjectBase.MissingProperty.Equals(oldValue);
            }

            oldValue = null;
            return false;
        }

        /// <summary>
        /// If the specified event <paramref name="args"/> is a <see cref="PropertyChangedExtendedEventArgs"/> instance, then gets the property value after the change.
        /// </summary>
        /// <param name="args">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        /// <param name="newValue">If the specified event <paramref name="args"/> is a <see cref="PropertyChangedExtendedEventArgs"/> instance, then the property value after the change; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/>&#160;if the specified event <paramref name="args"/> is a <see cref="PropertyChangedExtendedEventArgs"/> instance; otherwise, <see langword="null"/>.</returns>
        public static bool TryGetNewPropertyValue(this PropertyChangedEventArgs args, out object? newValue)
        {
            if (args is PropertyChangedExtendedEventArgs ext)
            {
                newValue = ext.NewValue;
                return !ObservableObjectBase.MissingProperty.Equals(newValue);
            }

            newValue = null;
            return false;
        }

        #endregion
    }
}
