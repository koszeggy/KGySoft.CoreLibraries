#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IExpandoResourceSet.cs
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
using System.Collections;
using System.IO;
using System.Reflection;
using System.Resources;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents a <see cref="ResourceSet"/> class that can hold replaceable resources.
    /// </summary>
    public interface IExpandoResourceSet : IDisposable
    {
        #region Properties

        /// <summary>
        /// Gets or sets whether the <see cref="IExpandoResourceSet"/> works in safe mode. In safe mode the retrieved
        /// objects returned from .resx sources are not deserialized automatically.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>When <see cref="SafeMode"/> is <see langword="true"/>, then <see cref="GetObject">GetObject</see> and <see cref="GetMetaObject">GetMetaObject</see> methods
        /// return <see cref="ResXDataNode"/> instances instead of deserialized objects, if they are returned from .resx resource. You can retrieve the deserialized
        /// objects by calling the <see cref="ResXDataNode.GetValue">ResXDataNode.GetValue</see> or <see cref="ResXDataNode.GetValueSafe">ResXDataNode.GetValueSafe</see> method.</para>
        /// <para>When <see cref="SafeMode"/> is <see langword="true"/>, then <see cref="GetString">GetString</see> and <see cref="GetMetaString">GetMetaString</see> methods
        /// will return a <see cref="string"/> also for non-string objects.
        /// For non-string values the raw XML string value will be returned for resources from a .resx source and the result of the <see cref="object.ToString">ToString</see> method
        /// for resources from a compiled source.</para>
        /// </remarks>
        bool SafeMode { get; set; }

        /// <summary>
        /// Gets whether this <see cref="IExpandoResourceSet"/> instance is modified.
        /// </summary>
        /// <value>
        /// <see langword="true"/>&#160;if this instance is modified; otherwise, <see langword="false"/>.
        /// </value>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        bool IsModified { get; }

        /// <summary>
        /// Gets or sets whether <see cref="GetObject">GetObject</see>/<see cref="GetMetaObject">GetMetaObject</see>
        /// and <see cref="GetEnumerator">GetEnumerator</see>/<see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> methods return always a new copy of the stored values.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>To be compatible with <a href="https://docs.microsoft.com/en-us/dotnet/api/System.Resources.ResXResourceSet" target="_blank">System.Resources.ResXResourceSet</a> this
        /// property is <see langword="false"/>&#160;by default. However, it can be a problem for mutable types if the returned value is changed by the consumer.</para>
        /// <para>To be compatible with <see cref="ResourceSet"/> set this property to <see langword="true"/>.</para>
        /// <para>String values are not cloned.</para>
        /// <para>The value of this property affects only the objects returned from .resx sources. Values from compiled sources are always cloned.</para>
        /// </remarks>
        bool CloneValues { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Returns an <see cref="IDictionaryEnumerator" /> that can iterate through the resources of the <see cref="IExpandoResourceSet" />.
        /// </summary>
        /// <returns>
        /// An <see cref="IDictionaryEnumerator" /> for the resources of this <see cref="IExpandoResourceSet" />.
        /// </returns>
        /// <remarks>
        /// <para>The returned enumerator iterates through the resources of the <see cref="IExpandoResourceSet"/>.
        /// To obtain a specific resource by name, use the <see cref="GetObject">GetObject</see> or <see cref="GetString">GetString</see> methods.
        /// To obtain an enumerator for the metadata entries instead, use the <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> method instead.</para>
        /// <para>If the <see cref="SafeMode"/> property is <see langword="true"/>, the <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> property of the returned enumerator is a <see cref="ResXDataNode"/>
        /// instance rather than the resource value. This makes possible to check the raw .resx content before deserialization if the .resx file is from an untrusted source. See also the examples at <see cref="ResXDataNode"/> and <see cref="ResXResourceSet"/> classes.</para>
        /// <para>The returned enumerators in this assembly support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</para>
        /// </remarks>
        /// <seealso cref="GetObject"/>
        /// <seealso cref="GetString"/>
        /// <seealso cref="GetMetadataEnumerator"/>
        /// <seealso cref="GetAliasEnumerator"/>
        IDictionaryEnumerator GetEnumerator();

        /// <summary>
        /// Returns an <see cref="IDictionaryEnumerator" /> that can iterate through the metadata of the <see cref="IExpandoResourceSet" />.
        /// </summary>
        /// <returns>
        /// An <see cref="IDictionaryEnumerator" /> for the metadata of this <see cref="IExpandoResourceSet" />.
        /// </returns>
        /// <remarks>
        /// <para>The returned enumerator iterates through the metadata entries of the <see cref="IExpandoResourceSet"/>.
        /// To obtain a specific metadata by name, use the <see cref="GetMetaObject">GetMetaObject</see> or <see cref="GetMetaString">GetMetaString</see> methods.
        /// To obtain an enumerator for the resources use the <see cref="GetEnumerator">GetEnumerator</see> method instead.</para>
        /// <para>If the <see cref="SafeMode"/> property is <see langword="true"/>, the <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> property of the returned enumerator is a <see cref="ResXDataNode"/>
        /// instance rather than the resource value. This makes possible to check the raw .resx content before deserialization if the .resx file is from an untrusted source. See also the examples at <see cref="ResXDataNode"/> and <see cref="ResXResourceSet"/> classes.</para>
        /// <para>The returned enumerators in this assembly support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</para>
        /// </remarks>
        /// <seealso cref="GetMetaObject"/>
        /// <seealso cref="GetMetaString"/>
        /// <seealso cref="GetEnumerator"/>
        /// <seealso cref="GetAliasEnumerator"/>
        IDictionaryEnumerator GetMetadataEnumerator();

        /// <summary>
        /// Returns an <see cref="IDictionaryEnumerator" /> that can iterate through the aliases of the <see cref="IExpandoResourceSet" />.
        /// </summary>
        /// <returns>
        /// An <see cref="IDictionaryEnumerator" /> for the aliases of this <see cref="IExpandoResourceSet" />.
        /// </returns>
        /// <remarks>
        /// <para>The returned enumerator iterates through the assembly aliases of the <see cref="IExpandoResourceSet"/>.
        /// To obtain a specific alias value by assembly name, use the <see cref="GetAliasValue">GetAliasValue</see> method.
        /// To obtain an enumerator for the resources use the <see cref="GetEnumerator">GetEnumerator</see> method instead.</para>
        /// <para>The <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> property of the returned enumerator is always a <see cref="string"/> regardless of the value of the <see cref="SafeMode"/> property.</para>
        /// <para>The <see cref="IDictionaryEnumerator.Key">IDictionaryEnumerator.Key</see> property of the returned enumerator is the alias name, whereas <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> is the corresponding assembly name.</para>
        /// <para>The returned enumerators in this assembly support the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</para>
        /// </remarks>
        /// <seealso cref="GetAliasValue"/>
        /// <seealso cref="GetEnumerator"/>
        /// <seealso cref="GetMetadataEnumerator"/>
        IDictionaryEnumerator GetAliasEnumerator();

        /// <summary>
        /// Searches for a resource object with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the resource to search for.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored.</param>
        /// <returns>
        /// The requested resource, or when <see cref="SafeMode"/> is <see langword="true"/>&#160;and the resource is found in a .resx source, a <see cref="ResXDataNode"/> instance
        /// from which the resource can be obtained. If the requested <paramref name="name"/> cannot be found, <see langword="null"/>&#160;is returned.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        object? GetObject(string name, bool ignoreCase = false);

        /// <summary>
        /// Searches for a <see cref="string" /> resource with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the resource to search for.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored.</param>
        /// <returns>
        /// The <see cref="string"/> value of a resource.
        /// If <see cref="SafeMode"/> is <see langword="false"/>, then an <see cref="InvalidOperationException"/> will be thrown for
        /// non-string resources. If <see cref="SafeMode"/> is <see langword="true"/>, then either the raw XML value (for resources from a .resx source)
        /// or the string representation of the object (for resources from a compiled source) will be returned for non-string resources.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160; and the type of the resource is not <see cref="string"/>.</exception>
        string? GetString(string name, bool ignoreCase = false);

        /// <summary>
        /// Searches for a metadata object with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the metadata to search for.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored. This parameter is optional
        /// <br/>Default value: <see langword="false"/></param>
        /// <returns>
        /// The requested metadata, or when <see cref="SafeMode"/> is <see langword="true"/>, a <see cref="ResXDataNode"/> instance
        /// from which the metadata can be obtained. If the requested <paramref name="name"/> cannot be found, <see langword="null"/>&#160;is returned.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        object? GetMetaObject(string name, bool ignoreCase = false);

        /// <summary>
        /// Searches for a <see cref="string" /> metadata with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the metadata to search for.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored. This parameter is optional
        /// <br/>Default value: <see langword="false"/></param>
        /// <returns>
        /// The <see cref="string"/> value of a metadata.
        /// If <see cref="SafeMode"/> is <see langword="false"/>, an <see cref="InvalidOperationException"/> will be thrown for
        /// non-string metadata. If <see cref="SafeMode"/> is <see langword="true"/>, the raw XML value will be returned for non-string metadata.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160;and the type of the metadata is not <see cref="string"/>.</exception>
        string? GetMetaString(string name, bool ignoreCase = false);

        /// <summary>
        /// Gets the assembly name for the specified <paramref name="alias"/>.
        /// </summary>
        /// <param name="alias">The alias of the assembly name, which should be retrieved.</param>
        /// <returns>The assembly name of the <paramref name="alias"/>, or <see langword="null"/>&#160;if there is no such alias defined.</returns>
        /// <remarks>If an alias is redefined in the .resx file, then this method returns the last occurrence of the alias value.</remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="alias"/> is <see langword="null"/>.</exception>
        string? GetAliasValue(string alias);

        /// <summary>
        /// Gets whether the current <see cref="IExpandoResourceSet"/> contains a resource with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the resource to check.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored. This parameter is optional.
        /// <br/>Default value: <see langword="false"/></param>
        /// <returns><see langword="true"/>, if the current <see cref="IExpandoResourceSet"/> contains a resource with name <paramref name="name"/>; otherwise, <see langword="false"/>.</returns>
        bool ContainsResource(string name, bool ignoreCase = false);

        /// <summary>
        /// Gets whether the current <see cref="IExpandoResourceSet"/> contains a metadata with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the metadata to check.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored. This parameter is optional.
        /// <br/>Default value: <see langword="false"/></param>
        /// <returns><see langword="true"/>, if the current <see cref="IExpandoResourceSet"/> contains a metadata with name <paramref name="name"/>; otherwise, <see langword="false"/>.</returns>
        bool ContainsMeta(string name, bool ignoreCase = false);

        /// <summary>
        /// Adds or replaces a resource object in the current <see cref="IExpandoResourceSet"/> with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the resource value to set.</param>
        /// <param name="value">The resource value to set. If <see langword="null"/>, a null reference will be explicitly stored.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>If <paramref name="value"/> is <see langword="null"/>, and this <see cref="IExpandoResourceSet"/> instance
        /// is a hybrid resource set, <see cref="GetObject">GetObject</see> will always return <see langword="null"/>, even if <paramref name="name"/> is
        /// defined in the original binary resource set. Thus you can force to take the parent resource set for example in case of a <see cref="HybridResourceManager"/>.</para>
        /// <para>To remove the user-defined content and reset the original resource defined in the binary resource set (if any), use
        /// the <see cref="RemoveObject">RemoveObject</see> method.</para>
        /// <para><paramref name="value"/> can be a <see cref="ResXDataNode"/> as well, its value will be interpreted correctly and added to the <see cref="IExpandoResourceSet"/> with the specified <paramref name="name"/>.</para>
        /// <para>If <paramref name="value"/> is a <see cref="ResXFileRef"/>, then a file reference will be added to the <see cref="IExpandoResourceSet"/>.
        /// On saving its path will be made relative to the specified <c>basePath</c> argument of the <see cref="O:KGySoft.Resources.IExpandoResourceSet.Save">Save</see> methods.
        /// If <c>forceEmbeddedResources</c> is <see langword="true"/>&#160;on saving, the file references will be converted to embedded ones.</para>
        /// <note>Not just <see cref="ResXDataNode"/> and <see cref="ResXFileRef"/> are handled but <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a>
        /// and <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a> as well. The compatibility with the system versions
        /// is provided without any reference to <c>System.Windows.Forms.dll</c>, where those types are located.</note>
        /// </remarks>
        void SetObject(string name, object? value);

        /// <summary>
        /// Adds or replaces a metadata object in the current <see cref="IExpandoResourceSet"/> with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the metadata value to set.</param>
        /// <param name="value">The metadata value to set. If <see langword="null"/>, a null reference will be explicitly stored.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>To remove the user-defined content use the <see cref="RemoveMetaObject">RemoveMetaObject</see> method.</para>
        /// <para><paramref name="value"/> can be a <see cref="ResXDataNode"/> as well, its value will be interpreted correctly and added to the <see cref="IExpandoResourceSet"/> with the specified <paramref name="name"/>.</para>
        /// <para>If <paramref name="value"/> is a <see cref="ResXFileRef"/>, then a file reference will be added to the <see cref="IExpandoResourceSet"/>.
        /// On saving its path will be made relative to the specified <c>basePath</c> argument of the <see cref="O:KGySoft.Resources.IExpandoResourceSet.Save">Save</see> methods.
        /// If <c>forceEmbeddedResources</c> is <see langword="true"/>&#160;on saving, the file references will be converted to embedded ones.</para>
        /// <note>Not just <see cref="ResXDataNode"/> and <see cref="ResXFileRef"/> are handled but <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a>
        /// and <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a> as well. The compatibility with the system versions
        /// is provided without any reference to <c>System.Windows.Forms.dll</c>, where those types are located.</note>
        /// </remarks>
        void SetMetaObject(string name, object? value);

        /// <summary>
        /// Adds or replaces an assembly alias value in the current <see cref="IExpandoResourceSet"/>.
        /// </summary>
        /// <param name="alias">The alias name to use instead of <paramref name="assemblyName"/> in the saved .resx file.</param>
        /// <param name="assemblyName">The fully or partially qualified name of the assembly.</param>
        /// <remarks>
        /// <note>The added alias values are dumped on demand when saving: only when a resource type is defined in the <see cref="Assembly"/>, whose name is the <paramref name="assemblyName"/>.
        /// Other alias names will be auto generated for non-specified assemblies.</note>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="assemblyName"/> or <paramref name="alias"/> is <see langword="null"/>.</exception>
        void SetAliasValue(string alias, string assemblyName);

        /// <summary>
        /// Removes a resource object from the current <see cref="IExpandoResourceSet"/> with the specified <paramref name="name"/>.
        /// If this <see cref="IExpandoResourceSet"/> represents a hybrid resource set, then the original value of <paramref name="name"/>
        /// will be restored (if existed).
        /// </summary>
        /// <param name="name">Name of the resource value to remove. Name is treated case sensitive.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>If this <see cref="IExpandoResourceSet"/> instance is a hybrid resource set, and there is a binary resource
        /// defined for <paramref name="name"/>, then after this call the originally defined value will be returned by <see cref="GetObject">GetObject</see> method.
        /// If you want to force <see cref="GetObject">GetObject</see> to return always <see langword="null"/>&#160;for this resource set, then use the <see cref="SetObject">SetObject</see> method with a <see langword="null"/>&#160;value</para>
        /// </remarks>
        void RemoveObject(string name);

        /// <summary>
        /// Removes a metadata object in the current <see cref="IExpandoResourceSet"/> with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the metadata value to remove. Name is treated case sensitive.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        void RemoveMetaObject(string name);

        /// <summary>
        /// Removes an assembly alias value in the current <see cref="IExpandoResourceSet"/>.
        /// </summary>
        /// <param name="alias">The alias, which should be removed.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="alias"/> is <see langword="null"/>.</exception>
        void RemoveAliasValue(string alias);

        /// <summary>
        /// Saves the <see cref="IExpandoResourceSet"/> to the specified file. If the current <see cref="IExpandoResourceSet"/> instance
        /// represents a hybrid resource set, saves the expando-part (.resx content) only.
        /// </summary>
        /// <param name="fileName">The location of the file where you want to save the resources.</param>
        /// <param name="compatibleFormat">If set to <see langword="true"/>, the result .resx file can be read by the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> class
        /// and the Visual Studio Resource Editor. If set to <see langword="false"/>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter"/>),
        /// but the result can be read only by <see cref="ResXResourceReader"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <param name="forceEmbeddedResources">If set to <see langword="true"/>&#160;the resources using a file reference (<see cref="ResXFileRef"/>) will be replaced by embedded resources. This parameter is optional.
        /// <br/>Default value: <see langword="false"/></param>
        /// <param name="newBasePath">A new base path for the file paths specified in the <see cref="ResXFileRef"/> objects. If <see langword="null"/>,
        /// the original base path will be used. The file paths in the saved .resx file will be relative to <paramref name="newBasePath"/>.
        /// Applicable if <paramref name="forceEmbeddedResources"/> is <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <c><see langword="null"/>.</c></param>
        /// <seealso cref="ResXResourceWriter"/>
        /// <seealso cref="ResXResourceWriter.CompatibleFormat"/>
        void Save(string fileName, bool compatibleFormat = false, bool forceEmbeddedResources = false, string? newBasePath = null);

        /// <summary>
        /// Saves the <see cref="IExpandoResourceSet"/> to the specified <paramref name="stream"/>. If the current <see cref="IExpandoResourceSet"/> instance
        /// represents a hybrid resource set, saves the expando-part (.resx content) only.
        /// </summary>
        /// <param name="stream">The stream to which you want to save.</param>
        /// <param name="compatibleFormat">If set to <see langword="true"/>, the result .resx file can be read by the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> class
        /// and the Visual Studio Resource Editor. If set to <see langword="false"/>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter"/>),
        /// but the result can be read only by <see cref="ResXResourceReader"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/></param>
        /// <param name="forceEmbeddedResources">If set to <see langword="true"/>&#160;the resources using a file reference (<see cref="ResXFileRef"/>) will be replaced by embedded resources. This parameter is optional.
        /// <br/>Default value: <see langword="false"/></param>
        /// <param name="newBasePath">A new base path for the file paths specified in the <see cref="ResXFileRef"/> objects. If <see langword="null"/>,
        /// the original base path will be used. The file paths in the saved .resx file will be relative to <paramref name="newBasePath"/>.
        /// Applicable if <paramref name="forceEmbeddedResources"/> is <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <c><see langword="null"/>.</c></param>
        /// <seealso cref="ResXResourceWriter"/>
        /// <seealso cref="ResXResourceWriter.CompatibleFormat"/>
        void Save(Stream stream, bool compatibleFormat = false, bool forceEmbeddedResources = false, string? newBasePath = null);

        /// <summary>
        /// Saves the <see cref="IExpandoResourceSet"/> by the specified <paramref name="textWriter"/>. If the current <see cref="IExpandoResourceSet"/> instance
        /// represents a hybrid resource set, saves the expando-part (.resx content) only.
        /// </summary>
        /// <param name="textWriter">The text writer to which you want to save.</param>
        /// <param name="compatibleFormat">If set to <see langword="true"/>, the result .resx file can be read by the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> class
        /// and the Visual Studio Resource Editor. If set to <see langword="false"/>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter"/>),
        /// but the result can be read only by <see cref="ResXResourceReader"/>. This parameter is optional.
        /// <br/>Default value: <see langword="false"/></param>
        /// <param name="forceEmbeddedResources">If set to <see langword="true"/>&#160;the resources using a file reference (<see cref="ResXFileRef"/>) will be replaced by embedded resources. This parameter is optional.
        /// <br/>Default value: <see langword="false"/></param>
        /// <param name="newBasePath">A new base path for the file paths specified in the <see cref="ResXFileRef"/> objects. If <see langword="null"/>,
        /// the original base path will be used. The file paths in the saved .resx file will be relative to <paramref name="newBasePath"/>.
        /// Applicable if <paramref name="forceEmbeddedResources"/> is <see langword="false"/>. This parameter is optional.
        /// <br/>Default value: <c><see langword="null"/>.</c></param>
        /// <seealso cref="ResXResourceWriter"/>
        /// <seealso cref="ResXResourceWriter.CompatibleFormat"/>
        void Save(TextWriter textWriter, bool compatibleFormat = false, bool forceEmbeddedResources = false, string? newBasePath = null);

        #endregion
    }
}
