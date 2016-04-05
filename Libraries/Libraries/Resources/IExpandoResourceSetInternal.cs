namespace KGySoft.Libraries.Resources
{
    /// <summary>
    /// Represents a class that can hold replaceable resources
    /// </summary>
    internal interface IExpandoResourceSetInternal
    {
        object GetResource(string name, bool ignoreCase, bool isString, bool asSafe);
        object GetMeta(string name, bool ignoreCase, bool isString, bool asSafe);
        bool SafeMode { set; }
    }
}
