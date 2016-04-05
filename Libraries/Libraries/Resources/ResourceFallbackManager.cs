using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace KGySoft.Libraries.Resources
{
    internal sealed class ResourceFallbackManager : IEnumerable<CultureInfo>
    {
        private CultureInfo startingCulture;
        private CultureInfo neutralResourcesCulture;
        private bool useParents;

        internal  ResourceFallbackManager(CultureInfo startingCulture, CultureInfo neutralResourcesCulture, bool useParents)
        {
            this.startingCulture = startingCulture ?? Thread.CurrentThread.CurrentUICulture;
            this.neutralResourcesCulture = neutralResourcesCulture;
            this.useParents = useParents;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

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
    }
}
