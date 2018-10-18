using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a class that combines the features of an <see cref="ObservableCollection{T}"/> and <see cref="BindingList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <seealso cref="ObservableCollection{T}" />
    /// <seealso cref="IBindingList" />
    public class ObservableBindingList<T> : ObservableCollection<T>
        //, IBindingList
        //, ICancelAddNew
        //, IRaiseItemChangedEvents
    {

    }
}
