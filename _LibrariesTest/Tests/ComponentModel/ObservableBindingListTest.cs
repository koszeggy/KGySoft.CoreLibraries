using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KGySoft.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace _LibrariesTest.Tests.ComponentModel
{
    [TestClass]
    public class ObservableBindingListTest
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
        [TestMethod]
        public void Test()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void RaiseItemChangedEventsDefault()
        {
            Assert.IsFalse(new ObservableBindingList<int>().RaiseItemChangedEvents);
            Assert.IsTrue(new ObservableBindingList<ObservableObjectBase>().RaiseItemChangedEvents);
        }

    }
}
