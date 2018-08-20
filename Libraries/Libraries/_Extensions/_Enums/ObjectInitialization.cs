namespace KGySoft.Libraries
{
    /// <summary>
    /// Represents a strategy for initializing types when generating random objects.
    /// </summary>
    public enum ObjectInitialization
    {
        /// <summary>
        /// Setting the public fields and public read-write properties (including non-public setters).
        /// </summary>
        PublicFieldsAndPropeties,

        /// <summary>
        /// Setting the public read-write properties (including non-public setters).
        /// </summary>
        PublicProperties,

        /// <summary>
        /// Setting the fields (including read-only ones). It has a high chance that the object will contain inconsistent data.
        /// </summary>
        Fields
    }
}
