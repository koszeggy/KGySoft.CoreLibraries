#if !NET35
using KGySoft.ComponentModel;
using NUnit.Framework;

namespace KGySoft.CoreLibraries.UnitTests.ComponentModel
{
    [TestFixture]
    public class ObservableBindingListTest
    {
        [Test]
        public void Test()
        {
            // TODO:
            // Ctor
            // - T INotifyPropertyChanged, default ctor: self hook
            // - T INotifyPropertyChanged, IBindingList ctor: delegating item change
            // Below everything also with WinForms app (by embedded ObservableCollection) and WPF app (by embedded BindingList and simple list - see whether both events are captured or just one category)
            // - Explicit Clear/Set/Add/Remove
            // - Underlying BindingList Clear/Set/Add/Remove/AllowNew(reset miatt)/AllowRemove/AllowEdit
            // - Underlying ObservableCollection Clear/Set/Add/Remove
            // - Item property change
        }

        [Test]
        public void RaiseItemChangedEventsDefault()
        {
            Assert.IsFalse(new ObservableBindingList<int>().RaiseItemChangedEvents);
            Assert.IsTrue(new ObservableBindingList<ObservableObjectBase>().RaiseItemChangedEvents);
        }
    }
}
#endif
