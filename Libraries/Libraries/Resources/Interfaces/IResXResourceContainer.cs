using System.Collections.Generic;
using System.ComponentModel.Design;

namespace KGySoft.Libraries.Resources
{
    /// <summary>
    /// Represents a class that can contain cached ResX resources: resource entries, meta and aliases.
    /// </summary>
    internal interface IResXResourceContainer
    {
        ICollection<KeyValuePair<string, ResXDataNode>> Resources { get; }
        ICollection<KeyValuePair<string, ResXDataNode>> Metadata { get; }
        ICollection<KeyValuePair<string, string>> Aliases { get; }
        bool SafeMode { get; }
        bool AutoFreeXmlData { get; }
        ITypeResolutionService TypeResolver { get; }
        string BasePath { get; }
        int Version { get; }
    }
}
