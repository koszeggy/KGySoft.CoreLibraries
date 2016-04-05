using System.Collections.Generic;
using System.ComponentModel.Design;

namespace KGySoft.Libraries.Resources
{
    /// <summary>
    /// Represents a class that can contain cached ResX resources: resource entries, meta and aliases.
    /// </summary>
    internal interface IResXResourceContainer
    {
        Dictionary<string, ResXDataNode> Resources { get; }
        Dictionary<string, ResXDataNode> Metadata { get; }
        Dictionary<string, string> Aliases { get; }
        bool UseResXDataNodes { get; }
        bool AutoFreeXmlData { get; }
        ITypeResolutionService TypeResolver { get; }
        string BasePath { get; }
        int Version { get; }
    }
}
