#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: GenerateObjectSettings.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2018 - All Rights Reserved
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
using System.Collections.Generic;
using KGySoft.Libraries.Resources;

#endregion

namespace KGySoft.Libraries
{
    public sealed class GenerateObjectSettings
    {
        private float chanceOfNull;
        private Range<int> collectionsLength = (1, 2);
        private Range<int> stringsLength = (4, 10);
        private Range<int> sentencesLength = (30, 60);

        /// <summary>
        /// Gets or sets the length of the collections to generate.
        /// <br/>Default value: <c>(1, 2)</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="Range{T}.LowerBound"/> is less than 0.</exception>
        public Range<int> CollectionsLength
        {
            get => collectionsLength;
            set => collectionsLength = value.LowerBound >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
        }

        /// <summary>
        /// Gets or sets the length of the non-sentence strings to generate.
        /// <br/>Default value: <c>(4, 10)</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="Range{T}.LowerBound"/> is less than 0.</exception>
        public Range<int> StringsLength
        {
            get => stringsLength;
            set => stringsLength = value.LowerBound >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
        }

        public Range<int> SentencesLength
        {
            get => sentencesLength;
            set => sentencesLength = value.LowerBound >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
        }

        /// <summary>
        /// Gets or sets the chance of generating a <see langword="null"/> value when the type is compatible with null.
        /// </summary>
        /// <value>
        /// A floating point number between 0.0 and 1.0.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is smaller than 0.0 or greater than 1.0.</exception>
        public float ChanceOfNull
        {
            get => chanceOfNull;
            set => chanceOfNull = value >= 0f && value <= 1f ? value : throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
        }

        public RandomString? StringCreationOptions { get; set; }

        // TODO: stringek hossza? Egyéb típusok min/maxa (akár csak allow negative)? Float stratégia?
        // TODO: datetime/ simadate/múlt/jövő/vagypl. 100 éven belül?

        public bool TryAutoResolveDelegates { get; set; }
        public bool TryAutoResolveInterfaces { get; set; } = true;
        public bool TryAutoResolveAbstractTypes { get; set; } = true;
        public bool TryUseDerivedTypes { get; set; }

        // vagy ctor nélkül és 
        public bool UseDefaultCtorAndPublicProperties { get; set; }

        // todo - kell egyáltalán? Hiszen akkor már nem véletlen - inkább legyen jó az auto set
        // Vagy: pl. Assemblyre/typeokra oldjon fel, ahol matcheket keres, vagy MethodInfo collectionre, amikből választ... ehh, ez már béna
        public Func<Type, Delegate> ResolveDelegate { get; set; }

        // called for interfaces, abstract types (including base delegate, multicastdelegate, array, enum) and every non-sealed classes
        public Func<Type, Type> ResolveType { get; set; }
    }
}
