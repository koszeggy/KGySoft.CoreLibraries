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
        internal static GenerateObjectSettings DefaultSettings { get; } = new GenerateObjectSettings();
        private float chanceOfNull;
        private Range<int> collectionsLength = new Range<int>(1, 2);
        private Range<int> stringsLength = new Range<int>(4, 10);
        private Range<int> sentencesLength = new Range<int>(30, 60);
        private FloatScale floatScale;
        private StringCreation? stringCreation;
        private ObjectInitialization objectInitialization;

        /// <summary>
        /// Gets or sets the length of the collections to generate.
        /// <br/>Default value: <c>1..2</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="Range{T}.LowerBound"/> is less than 0.</exception>
        public Range<int> CollectionsLength
        {
            get => collectionsLength;
            set => collectionsLength = value.LowerBound >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentMustBeGreaterOrEqualThan, 0));
        }

        /// <summary>
        /// Gets or sets the length of the non-sentence strings to generate.
        /// <br/>Default value: <c>4..10</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="Range{T}.LowerBound"/> is less than 0.</exception>
        public Range<int> StringsLength
        {
            get => stringsLength;
            set => stringsLength = value.LowerBound >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentMustBeGreaterOrEqualThan, 0));
        }

        public Range<int> SentencesLength
        {
            get => sentencesLength;
            set => sentencesLength = value.LowerBound >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentMustBeGreaterOrEqualThan, 0));
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
            set => chanceOfNull = value >= 0f && value <= 1f ? value : throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentMustBeBetween, 0f, 1f));
        }

        /// <summary>
        /// Gets or sets the strategy for generating strings. <see langword="null"/> to auto select.
        /// <br/>Default value: <see langword="null"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a valid value of <see cref="StringCreation"/>.</exception>
        public StringCreation? StringCreation
        {
            get => stringCreation;
            set => stringCreation = value == null || Enum<StringCreation>.IsDefined(value.Value) ? value : throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
        }

        /// <summary>
        /// Gets or sets the strategy for initializing a random generated object.
        /// <br/>Default value: <see cref="Libraries.ObjectInitialization.PublicFieldsAndPropeties"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a valid value of <see cref="Libraries.ObjectInitialization"/>.</exception>
        public ObjectInitialization ObjectInitialization
        {
            get => objectInitialization;
            set => objectInitialization = Enum<ObjectInitialization>.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
        }

        /// <summary>
        /// Gets or sets the scale for generating floating point numbers.
        /// <br/>Default value: <see cref="Libraries.FloatScale.Auto"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a valid value of <see cref="Libraries.FloatScale"/>.</exception>
        public FloatScale FloatScale
        {
            get => floatScale;
            set => floatScale = Enum<FloatScale>.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
        }

        public bool AllowNegativeValues { get; set; }
        public bool? PastDateTimes { get; set; }
        public bool CloseDateTimes { get; set; } = true;

        public bool TryResolveInterfacesAndAbstractTypes { get; set; } = true;
        public bool AllowDerivedTypesForNonSealedClasses { get; set; } = true;
        public bool AllowCreateObjectWithoutConstructor { get; set; }

        /// <summary>
        /// Gets or sets whether it is allowed to substitute <see cref="object"/> type with other simple types when an <see cref="object"/> type has to be generated.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>If this property is <see langword="true"/>, then the result of generating an <see cref="object"/> type can have the following types:
        /// <list type="bullet">
        /// <item><see cref="object"/></item>
        /// <item><see cref="bool"/></item>
        /// <item><see cref="byte"/></item>
        /// <item><see cref="sbyte"/></item>
        /// <item><see cref="char"/></item>
        /// <item><see cref="short"/></item>
        /// <item><see cref="ushort"/></item>
        /// <item><see cref="int"/></item>
        /// <item><see cref="uint"/></item>
        /// <item><see cref="long"/></item>
        /// <item><see cref="ulong"/></item>
        /// <item><see cref="float"/></item>
        /// <item><see cref="double"/></item>
        /// <item><see cref="decimal"/></item>
        /// <item><see cref="string"/></item>
        /// <item><see cref="DateTime"/></item>
        /// <item><see cref="DateTimeOffset"/></item>
        /// <item><see cref="TimeSpan"/></item>
        /// <item><see cref="Guid"/></item>
        /// </list>
        /// </para>
        /// <para>If this property is <see langword="false"/> and <see cref="AllowDerivedTypesForNonSealedClasses"/> property is <see langword="true"/>, then
        /// when an <see cref="object"/> type has to be generated the result can be any random type from the <c>mscorlib</c> assembly.</para>
        /// </remarks>
        public bool SubstituteObjectWithSimpleTypes { get; set; } = true;
    }
}
