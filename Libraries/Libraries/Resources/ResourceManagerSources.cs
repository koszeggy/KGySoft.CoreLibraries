namespace KGySoft.Libraries.Resources
{
    /// <summary>
    /// Represents the possible sources of <see cref="HybridResourceManager"/> and <see cref="DynamicResourceManager"/>
    /// classes.
    /// </summary>
    public enum ResourceManagerSources
    {
        /// <summary>
        /// Indicates that the resources must be taken only from compiled binary resources.
        /// </summary>
        CompiledOnly,

        /// <summary>
        /// Indicates that the resources must be taken only from .resx XML files.
        /// </summary>
        ResXOnly,

        /// <summary>
        /// Indicates that the resources must be taken both from copiled resources binary and .resx files.
        /// If a resource exists in both sources, then the one in the .resx will be returned.
        /// </summary>
        CompiledAndResX
    }
}
