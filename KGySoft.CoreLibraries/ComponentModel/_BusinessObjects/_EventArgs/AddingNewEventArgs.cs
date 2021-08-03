#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: AddingNewEventArgs.cs
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

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>Provides data for the <see cref="FastBindingList{T}.AddingNew"><![CDATA[FastBindingList<T>.AddingNew]]></see> event.</summary>
    /// <typeparam name="T">The type of the element to add.</typeparam>
    public class AddingNewEventArgs<T> : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets or sets the object to be added to the binding list. If <see langword="null"/>,
        /// then a new instance of <typeparamref name="T"/> is tried to be created automatically.
        /// </summary>
        public T? NewObject { get; set; }

        #endregion
    }
}
