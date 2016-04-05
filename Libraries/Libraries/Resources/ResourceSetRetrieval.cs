namespace KGySoft.Libraries.Resources
{
    /// <summary>
    /// Represents te retrieval behavior of an <see cref="IExpandoResourceSet"/> in <see cref="IExpandoResourceManager.GetExpandoResourceSet"/> method.
    /// </summary>
    public enum ResourceSetRetrieval
    {
        /// <summary>
        /// The <see cref="IExpandoResourceSet"/> will be returned only if it is already loaded; otherwise, no resource set will be retrieved.
        /// </summary>
        GetIfAlreadyLoaded,

        /// <summary>
        /// The <see cref="IExpandoResourceSet"/> will be returned if the corresponding resource file exists and can be loaded.
        /// </summary>
        LoadIfExists,

        /// <summary>
        /// An <see cref="IExpandoResourceSet"/> will be created for the requested culture even if no corresponding file can be loaded.
        /// </summary>
        CreateIfNotExists
    }
}