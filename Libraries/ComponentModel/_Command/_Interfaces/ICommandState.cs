#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ICommandState.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Collections.Generic;
using System.ComponentModel;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents the states of a command for a specific command binding. When a state property is set it is tried to be applied for all of the command sources.
    /// By default, they are tried to be set as a property on the sources but this behavior can be overridden if an <see cref="ICommandStateUpdater"/> is added
    /// for the binding by the <see cref="ICommandBinding.AddStateUpdater">ICommandBinding.AddStateUpdater</see> method.
    /// <br/>See the <strong>Remarks</strong> section of <see cref="ICommand"/> for examples.
    /// </summary>
    public interface ICommandState : IDictionary<string, object>, INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// Gets or sets whether the command is enabled in the current binding.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        /// <value><see langword="true"/> if the command enabled and can be executed; otherwise, <see langword="false"/>.</value>
        bool Enabled { get; set; }

#if !NET35
        /// <summary>
        /// Gets the state as a dynamic object so the states can be set by simple property setting syntax.
        /// </summary>
        dynamic AsDynamic { get; }
#endif

        #endregion
    }
}
