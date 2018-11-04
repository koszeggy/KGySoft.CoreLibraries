using System;

namespace KGySoft.ComponentModel
{
    /// <summary>Provides data for the <see cref="FastBindingList{T}.AddingNew" /> event.</summary>
    /// <typeparam name="T">The type of the element to add.</typeparam>
    public class AddingNewEventArgs<T> : EventArgs
    {
        /// <summary>Gets or sets the object to be added to the binding list.</summary>
        /// <returns>The <typeparamref name="T"/> to be added as a new item to the associated collection. </returns>
        public T NewObject { get; set; }
    }
}
