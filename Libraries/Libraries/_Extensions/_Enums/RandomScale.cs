namespace KGySoft.Libraries
{
    /// <summary>
    /// Represents the scaling strategy when generating random numbers.
    /// </summary>
    public enum RandomScale
    {
        /// <summary>
        /// The scaling will be chosen automatically based on the provided range.
        /// </summary>
        Auto,

        /// <summary>
        /// Forces to use linear scaling when generating random numbers.
        /// </summary>
        ForceLinear,

        /// <summary>
        /// Prefers logarithmic scaling when generating random numbers.
        /// If the order of magnitude between the minimum and maximum value is too narrow may chose linear scaling instead. 
        /// </summary>
        PreferLogarithmic
    }
}
