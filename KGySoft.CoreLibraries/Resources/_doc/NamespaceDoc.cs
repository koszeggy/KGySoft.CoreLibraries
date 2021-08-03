#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: NamespaceDoc.cs
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
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Contains classes for resource management, which support read-write access directly to .resx files (<see cref="ResXResourceManager"/>) or even combined access to compiled and .resx resources
    /// (<see cref="HybridResourceManager"/>, <see cref="DynamicResourceManager"/>).
    /// <br/>See the <strong>Remarks</strong> section to see when to use the difference classes of this namespace.
    /// </summary>
    /// <remarks>
    /// The <see cref="N:KGySoft.Resources"/> namespace contains several classes, which can handle resources from XML sources (.resx files).
    /// The table below can help you to choose the best one for your needs.
    /// <list type="table">
    /// <listheader><term>Class</term><term>When to choose this one; added functionality compared to previous levels.</term></listheader>
    /// <item><term><see cref="ResXResourceReader"/></term>
    /// <term>Using <see cref="ResXResourceReader"/> is the most low-level option to read the content of a .resx file. It provides enumerators to retrieve the resources, metadata and aliases of a .resx content.
    /// In most cases you can use the more specialized classes, such as <see cref="ResXResourceSet"/> but there are some cases when you need to use <see cref="ResXResourceReader"/>:
    /// <list type="bullet">
    /// <item>The <see cref="ResXResourceReader"/> is able to read the .resx content in a lazy manner if <see cref="ResXResourceReader.AllowDuplicatedKeys">ResXResourceReader.AllowDuplicatedKeys</see> is <see langword="true"/>.
    /// That means the .resx content is read on demand as you enumerate the contents (the contents are cached so if you retrieve an enumerator for the second time it will not process the .resx file again). It can be useful
    /// if you are looking for one specific key, after which you break the enumeration or if you want to process an incomplete or corrupted .resx file up to the point it can be parsed correctly.</item>
    /// <item>A .resx file may contain a key more than once (though it is somewhat incorrect). If <see cref="ResXResourceReader.AllowDuplicatedKeys">ResXResourceReader.AllowDuplicatedKeys</see> is <see langword="true"/>,
    /// you can retrieve all of the redefined values.</item>
    /// <item><see cref="ResXResourceReader"/> allows you to handle type names in a customized way (how names are mapped to a <see cref="Type"/>). To use custom name resolution pass an <see cref="ITypeResolutionService"/> instance to one of the constructors.</item>
    /// </list>
    /// </term></item>
    /// <item><term><see cref="ResXResourceWriter"/></term>
    /// <term>Using <see cref="ResXResourceWriter"/> is the most low-level option to write the content of a .resx file. Use this if at least one of the following points are applicable for you:
    /// <list type="bullet">
    /// <item>You want to have full control over the order of the dumped resources, metadata and aliases or you want to re-use a key or redefine an alias during the dump (though it is not recommended).</item>
    /// <item>You want to dump the full .resx header including the comments dumped also by Visual Studio when you create a resource file (see <see cref="ResXResourceWriter.OmitHeader">ResXResourceWriter.OmitHeader</see> property).</item>
    /// <item>You want to use customized type names when non-string resources are added. You can achieve this by passing a <see cref="Func{T,TResult}">Func&lt;Type, string&gt;</see> delegate to one of the constructors.</item>
    /// </list></term></item>
    /// <item><term><see cref="ResXResourceSet"/></term>
    /// <term>The <see cref="ResXResourceSet"/> class represents the full content of a single .resx file in memory. It provides both enumerators and dictionary-like direct key access to the resources, metadata and aliases.
    /// It uses <see cref="ResXResourceReader"/> and <see cref="ResXResourceWriter"/> internally to load/save .resx content.
    /// As an <see cref="IExpandoResourceSet"/> implementation it is able to add/replace/remove entries and save the content.
    /// Use this class in the following cases:
    /// <list type="bullet">
    /// <item>You want to access the resources/metadata/aliases of a single .resx file directly by name just like in a dictionary.</item>
    /// <item>You want to load the .resx content not just from a file but also a <see cref="Stream"/>/<see cref="TextReader"/> or to save it to a <see cref="Stream"/>/<see cref="TextWriter"/>.</item>
    /// </list>
    /// Specialization compared to <see cref="ResXResourceReader"/>/<see cref="ResXResourceWriter"/>:
    /// <list type="bullet">
    /// <item>Default type name handling and type resolution is used.</item>
    /// <item>When loading a .resx file, duplications are not allowed. For redefined names the last value will be stored.</item>
    /// <item>When saving a .resx file, header comment is always omitted. If content is saved in non-compatible format, the .resx schema header is omitted, too.</item>
    /// <item>When saving a .resx file, assembly aliases are auto generated for assemblies, which are not explicitly defined.</item></list>
    /// </term></item>
    /// <item><term><see cref="ResXResourceManager"/></term>
    /// <term>The <see cref="ResXResourceManager"/> handles resources in the same manner as <see cref="ResourceManager"/> does but instead of working with binary compiled resources it uses XML resources (.resx files) directly.
    /// It takes the culture hierarchy into account so querying a resource by a specific <see cref="CultureInfo"/> may end up loading multiple resource files.
    /// It stores <see cref="ResXResourceSet"/> instances internally to store the resources of the different cultures.
    /// As an <see cref="IExpandoResourceManager"/> implementation it is able to add/replace/remove entries in the resource sets belonging to specified cultures and it can save the changed contents.
    /// <br/>Specialization compared to <see cref="ResXResourceSet"/>:
    /// <list type="bullet">
    /// <item>The <see cref="ResXResourceManager"/> loads/saves the resource sets always from/into files.
    /// (Though you can call the <see cref="ResXResourceManager.GetExpandoResourceSet">GetExpandoResourceSet</see> method to access the various <see cref="O:KGySoft.Resources.IExpandoResourceSet.Save">Save</see> overloads)</item>
    /// <item>The <see cref="ResXResourceManager"/> does not contain methods to manipulate the aliases directly.
    /// (Though you can call the <see cref="ResXResourceManager.GetExpandoResourceSet">GetExpandoResourceSet</see> method to access the aliases directly)</item>
    /// </list>
    /// </term></item>
    /// <item><term><see cref="HybridResourceManager"/></term>
    /// <term>The <see cref="HybridResourceManager"/> combines the functionality of the regular <see cref="ResourceManager"/> and the <see cref="ResXResourceManager"/> classes.
    /// The source of the resources can be chosen by the <see cref="HybridResourceManager.Source"/> property (see <see cref="ResourceManagerSources"/> enumeration).
    /// Enabling both binary and .resx resources makes possible to expand or override the resources originally come from binary resources.
    /// Just like the <see cref="ResXResourceManager"/> it is an <see cref="IExpandoResourceManager"/> implementation. The replacement and newly added content is saved into .resx files.</term></item>
    /// <item><term><see cref="DynamicResourceManager"/></term>
    /// <term>The <see cref="DynamicResourceManager"/> is derived from <see cref="HybridResourceManager"/> and adds the functionality of automatically appending the resources with the non-translated and/or unknown entries as well as
    /// auto-saving the changes. This makes possible to automatically create the .resx files if the language of the application is changed to a language, which has no translation yet. See also the static <see cref="LanguageSettings"/> class.
    /// The strategy of auto appending and saving can be chosen by the <see cref="DynamicResourceManager.AutoAppend"/> and <see cref="DynamicResourceManager.AutoSave"/> properties (see <see cref="AutoAppendOptions"/> and <see cref="AutoSaveOptions"/> enumerations).</term></item>
    /// </list>
    /// </remarks>
    [CompilerGenerated]
    internal static class NamespaceDoc
    {
    }
}
