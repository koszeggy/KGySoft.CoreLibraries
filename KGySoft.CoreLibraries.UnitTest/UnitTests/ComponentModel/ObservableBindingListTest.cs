#if !NET35
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObservableBindingListTest.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;

using KGySoft.ComponentModel;

using NUnit.Framework;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.ComponentModel
{
    [TestFixture]
    public class ObservableBindingListTest
    {
        #region Methods

        // TODO: (these are now tested in a desktop app: https://github.com/koszeggy/KGySoft.ComponentModelDemo)
        // Ctor
        // - T INotifyPropertyChanged, default ctor: self hook
        // - T INotifyPropertyChanged, IBindingList ctor: delegating item change
        // Below everything also with WinForms app (by embedded ObservableCollection) and WPF app (by embedded BindingList and simple list - see whether both events are captured or just one category)
        // - Explicit Clear/Set/Add/Remove
        // - Underlying BindingList Clear/Set/Add/Remove/AllowNew(due to reset)/AllowRemove/AllowEdit
        // - Underlying ObservableCollection Clear/Set/Add/Remove
        // - Item property change

        [Test]
        public void RaiseItemChangedEventsDefault()
        {
            Assert.IsFalse(new ObservableBindingList<int>().RaiseItemChangedEvents);
            Assert.IsTrue(new ObservableBindingList<ObservableObjectBase>().RaiseItemChangedEvents);
        }

        #endregion
    }
}
#endif
