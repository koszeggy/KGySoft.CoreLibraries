#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: GenerateObjectSettings.cs
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

using System;
#if !NET35
using System.Numerics;
#endif

#endregion

#region Suppressions

#if NET35
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents the settings for generating an object by the <see cref="O:KGySoft.CoreLibraries.RandomExtensions.NextObject">NextObject</see> extension methods.
    /// </summary>
    public sealed class GenerateObjectSettings
    {
        #region Fields

        private float chanceOfNull;
        private Range<int> collectionsLength = new Range<int>(1, 2);
        private Range<int> stringsLength = new Range<int>(4, 10);
        private Range<int> sentencesLength = new Range<int>(30, 60);
        private FloatScale floatScale;
        private StringCreation? stringCreation;
        private ObjectInitialization objectInitialization;
        private int maxRecursionLevel = 1;

        #endregion

        #region Properties

        #region Static Properties

        internal static GenerateObjectSettings DefaultSettings { get; } = new GenerateObjectSettings();

        #endregion

        #region Instance Properties

        /// <summary>
        /// Gets or sets the length of the collections to generate.
        /// It also affects the size of generated <see cref="BigInteger"/> instances (interpreted as the amount of 4 byte chunks to generate).
        /// <br/>Default value: <c>1..2</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="Range{T}.LowerBound"/> is less than 0.</exception>
        public Range<int> CollectionsLength
        {
            get => collectionsLength;
            set
            {
                if (value.LowerBound < 0)
                    Throw.ArgumentOutOfRangeException(Argument.value, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
                collectionsLength = value;
            }
        }

        /// <summary>
        /// Gets or sets the length of the non-sentence strings to generate.
        /// <br/>Default value: <c>4..10</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="Range{T}.LowerBound"/> is less than 0.</exception>
        public Range<int> StringsLength
        {
            get => stringsLength;
            set
            {
                if (value.LowerBound < 0)
                    Throw.ArgumentOutOfRangeException(Argument.value, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
                stringsLength = value;
            }
        }

        /// <summary>
        /// Gets or sets the length of the sentence strings to generate.
        /// <br/>Default value: <c>30..60</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="Range{T}.LowerBound"/> is less than 0.</exception>
        public Range<int> SentencesLength
        {
            get => sentencesLength;
            set
            {
                if (value.LowerBound < 0)
                    Throw.ArgumentOutOfRangeException(Argument.value, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
                sentencesLength = value;
            }
        }

        /// <summary>
        /// Gets or sets the chance of generating a <see langword="null"/>&#160;value when the type is compatible with <see langword="null"/>.
        /// <br/>Default value: <c>0.0</c>.
        /// </summary>
        /// <value>
        /// A floating point number between 0.0 and 1.0.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is smaller than 0.0 or greater than 1.0.</exception>
        /// <remarks><note>If a reference type cannot be instantiated by the current settings a <see langword="null"/>&#160;value will be created even if the value of this property is <c>0.0</c>.</note></remarks>
        public float ChanceOfNull
        {
            get => chanceOfNull;
            set
            {
                if (value < 0f || value > 1f)
                    Throw.ArgumentOutOfRangeException(Argument.value, Res.ArgumentMustBeBetween(0f, 1f));
                chanceOfNull = value;
            }
        }

        /// <summary>
        /// Gets or sets the strategy for generating strings. Set <see langword="null"/>&#160;to auto select strategy by member name.
        /// <br/>Default value: <see langword="null"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a valid value of <see cref="StringCreation"/>.</exception>
        public StringCreation? StringCreation
        {
            get => stringCreation;
            set
            {
                if (value != null && !Enum<StringCreation>.IsDefined(value.Value))
                    Throw.EnumArgumentOutOfRange(Argument.value, value.Value);
                stringCreation = value;
            }
        }

        /// <summary>
        /// Gets or sets the strategy for initializing a random generated object.
        /// <br/>Default value: <see cref="CoreLibraries.ObjectInitialization.PublicFieldsAndProperties"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a valid value of <see cref="CoreLibraries.ObjectInitialization"/>.</exception>
        public ObjectInitialization ObjectInitialization
        {
            get => objectInitialization;
            set
            {
                if (!Enum<ObjectInitialization>.IsDefined(value))
                    Throw.EnumArgumentOutOfRange(Argument.value, value);
                objectInitialization = value;
            }
        }

        /// <summary>
        /// Gets or sets the scale for generating floating point numbers.
        /// <br/>Default value: <see cref="CoreLibraries.FloatScale.Auto"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not a valid value of <see cref="CoreLibraries.FloatScale"/>.</exception>
        public FloatScale FloatScale
        {
            get => floatScale;
            set
            {
                if (!Enum<FloatScale>.IsDefined(value))
                    Throw.EnumArgumentOutOfRange(Argument.value, value);
                floatScale = value;
            }
        }

        /// <summary>
        /// Gets or sets whether negative values are allowed when generating numbers.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        public bool AllowNegativeValues { get; set; }

        /// <summary>
        /// Gets or sets whether past date and time values should be produced when generating <see cref="DateTime"/> and <see cref="DateTimeOffset"/> instances.
        /// <br/>Default value: <see langword="null"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/>: Use past date and time values only.
        /// <br/><see langword="false"/>: Use future date and time values only.
        /// <br/><see langword="null"/>: Allow both past and future date and time values.
        /// </value>
        public bool? PastDateTimes { get; set; }

        /// <summary>
        /// Gets or sets whether close date and time values (current date plus-minus 100 years) should be produced when generating <see cref="DateTime"/> and <see cref="DateTimeOffset"/> values.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public bool CloseDateTimes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether a random implementation should be picked for interfaces and abstract types.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public bool TryResolveInterfacesAndAbstractTypes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether a random derived type is allowed to be picked for non-sealed classes.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        public bool AllowDerivedTypesForNonSealedClasses { get; set; }

        /// <summary>
        /// Gets or sets whether non-value type objects are allowed to be created without using the default constructor or (in case of collections) a constructor with a collection parameter.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks><note type="caution">If the value of this property is <see langword="true"/>, then it cannot be guaranteed that a generated object will be in a consistent state.</note></remarks>
        public bool AllowCreateObjectWithoutConstructor { get; set; }

        /// <summary>
        /// Gets or sets the type to be used when a type of <see cref="object"/> has to be generated.
        /// <br/>Default value: <see langword="null"/>.
        /// </summary>
        /// <value>
        /// The <see cref="Type"/> to be used to generate <see cref="object"/> instances or <see langword="null"/>&#160;for not using a substitution.
        /// </value>
        /// <remarks>
        /// <para>Specifying a substitution for the <see cref="object"/> type can be useful for non-generic collections or when it is known that <see cref="object"/> type
        /// has to be always replaced by a more specific type.</para>
        /// <para>If this property is not <see langword="null"/>, then whenever an instance of <see cref="object"/> has to be generated the specified type will be used.</para>
        /// <para>The value of this property can be also an interface or abstract type. If <see cref="TryResolveInterfacesAndAbstractTypes"/> property is <see langword="true"/>,
        /// then a random implementation of the specified type will be used for every generated instance.</para>
        /// <para>If the value of this property is a non-sealed class and <see cref="AllowDerivedTypesForNonSealedClasses"/> property is <see langword="true"/>,
        /// then a random derived type can be used for every generated instance.</para>
        /// </remarks>
        public Type? SubstitutionForObjectType { get; set; }

        /// <summary>
        /// Gets or sets the maximum level of allowed recursion when generating objects, which contain members or elements of assignable types from their container types.
        /// <br/>Default value: <c>1</c>.
        /// </summary>
        public int MaxRecursionLevel
        {
            get => maxRecursionLevel;
            set
            {
                if (value < 0)
                    Throw.ArgumentOutOfRangeException(Argument.value, Res.ArgumentMustBeGreaterThanOrEqualTo(0));
                maxRecursionLevel = value;
            }
        }

        #endregion

        #endregion
    }
}
