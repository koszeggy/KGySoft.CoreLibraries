#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResourceFallbackManager.cs
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

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

#endregion

namespace KGySoft.Resources
{
    internal sealed class ResourceFallbackManager : IEnumerable<CultureInfo>
    {
        #region Fields

        private readonly CultureInfo startingCulture;
        private readonly CultureInfo neutralResourcesCulture;
        private readonly bool useParents;

        #endregion

        #region Constructors

        internal ResourceFallbackManager(CultureInfo? startingCulture, CultureInfo neutralResourcesCulture, bool useParents)
        {
            this.startingCulture = startingCulture ?? Thread.CurrentThread.CurrentUICulture;
            this.neutralResourcesCulture = neutralResourcesCulture;
            this.useParents = useParents;
        }

        #endregion

        #region Methods

        #region Public Methods

        public IEnumerator<CultureInfo> GetEnumerator()
        {
            bool reachedNeutralResourcesCulture = false;

            // starting culture chain, up to neutral
            CultureInfo currentCulture = startingCulture;
            do
            {
                if (currentCulture.Name == neutralResourcesCulture.Name)
                {
                    yield return CultureInfo.InvariantCulture;
                    reachedNeutralResourcesCulture = true;
                    break;
                }

                yield return currentCulture;
                currentCulture = currentCulture.Parent;
            } while (useParents && !ReferenceEquals(CultureInfo.InvariantCulture, currentCulture));

            if (!useParents || Equals(CultureInfo.InvariantCulture, startingCulture))
            {
                yield break;
            }

            // Don't return invariant twice though.
            if (reachedNeutralResourcesCulture)
                yield break;

            yield return CultureInfo.InvariantCulture;
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #endregion
    }
}
