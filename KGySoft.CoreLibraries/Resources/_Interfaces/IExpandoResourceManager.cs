#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IExpandoResourceManager.cs
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
using System.Globalization;
using System.IO;
using System.Resources;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents a <see cref="ResourceManager"/> with write capabilities.
    /// </summary>
    public interface IExpandoResourceManager : IDisposable
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value that indicates whether the resource manager allows case-insensitive resource lookups in the
        /// <see cref="GetString">GetString</see>/<see cref="GetMetaString">GetMetaString</see> and <see cref="GetObject">GetObject</see>/<see cref="GetMetaObject">GetMetaObject</see> methods.
        /// </summary>
        bool IgnoreCase { get; set; }

        /// <summary>
        /// Gets whether this <see cref="IExpandoResourceManager"/> instance has modified and unsaved data.
        /// </summary>
        bool IsModified { get; }

        /// <summary>
        /// Gets or sets whether the <see cref="IExpandoResourceManager"/> works in safe mode. In safe mode the retrieved
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
        /// For non-string values the raw XML string value will be returned for resources from a .resx source and the result of the <see cref="Object.ToString">ToString</see> method
        /// for resources from a compiled source.</para>
        /// <para>When <see cref="SafeMode"/> is <see langword="true"/>, then <see cref="GetStream">GetStream</see> and <see cref="GetMetaStream">GetMetaStream</see> methods
        /// will return a <see cref="MemoryStream"/> for any object.
        /// For values, which are neither <see cref="MemoryStream"/>, nor <see cref="Array">byte[]</see> instances these methods return a stream wrapper for the same string value
        /// that is returned by the <see cref="GetString">GetString</see>/<see cref="GetMetaString">GetMetaString</see> methods.</para>
        /// </remarks>
        bool SafeMode { get; set; }

        /// <summary>
        /// Gets or sets whether <see cref="GetObject">GetObject</see> and <see cref="GetMetaObject">GetMetaObject</see> methods return always a new copy of the stored values.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>To be compatible with <a href="https://docs.microsoft.com/en-us/dotnet/api/System.Resources.ResourceManager" target="_blank">System.Resources.ResourceManager</a> this
        /// property is <see langword="true"/>&#160;by default. If this <see cref="IExpandoResourceManager"/> instance contains no mutable values or it is known that modifying values is not
        /// an issue, then this property can be set to <see langword="false"/>&#160;for better performance.</para>
        /// <para>String values are not cloned.</para>
        /// <para>The value of this property affects only the objects returned from .resx sources. Non-string values from compiled sources are always cloned.</para>
        /// </remarks>
        bool CloneValues { get; set; }

        /// <summary>
        /// Gets whether this <see cref="IExpandoResourceManager"/> instance is disposed.
        /// </summary>
        bool IsDisposed { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Retrieves the resource set for a particular culture, which can be dynamically modified.
        /// </summary>
        /// <param name="culture">The culture whose resources are to be retrieved.</param>
        /// <param name="behavior">Determines the retrieval behavior of the result <see cref="IExpandoResourceSet"/>. This parameter is optional.
        /// <br/>Default value: <see cref="ResourceSetRetrieval.LoadIfExists"/>.</param>
        /// <param name="tryParents"><see langword="true"/>&#160;to use resource fallback to load an appropriate resource if the resource set cannot be found; <see langword="false"/>&#160;to bypass the resource fallback process. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>The resource set for the specified culture, or <see langword="null"/>&#160;if the specified culture cannot be retrieved by the defined <paramref name="behavior"/>,
        /// or when this <see cref="IExpandoResourceManager"/> instance is configured so that it cannot return an <see cref="IExpandoResourceSet"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="behavior"/> does not fall in the expected range.</exception>
        /// <exception cref="MissingManifestResourceException">Resource file of the neutral culture was not found, while <paramref name="tryParents"/> is <see langword="true"/>
        /// and <paramref name="behavior"/> is not <see cref="ResourceSetRetrieval.CreateIfNotExists"/>.</exception>
        IExpandoResourceSet? GetExpandoResourceSet(CultureInfo culture, ResourceSetRetrieval behavior = ResourceSetRetrieval.LoadIfExists, bool tryParents = false);

        /// <summary>
        /// Tells the resource manager to call the <see cref="ResourceSet.Close">Close</see> method on all <see cref="ResourceSet" /> objects and release all resources.
        /// All unsaved resources will be lost.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <remarks>
        /// <note type="caution">By calling this method all of the unsaved changes will be lost.</note>
        /// <para>By the <see cref="IsModified"/> property you can check whether there are unsaved changes.</para>
        /// <para>To save the changes you can call the <see cref="SaveAllResources">SaveAllResources</see> method.</para>
        /// </remarks>
        void ReleaseAllResources();

        /// <summary>
        /// Returns the value of the string resource localized for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the resource is localized. If the resource is not localized for
        /// this culture, the resource manager uses fallback rules to locate an appropriate resource. If this value is
        /// <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>
        /// The value of the resource localized for the specified <paramref name="culture"/>, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>, then instead of throwing an <see cref="InvalidOperationException"/>
        /// either the raw XML value (for resources from a .resx source) or the string representation of the object (for resources from a compiled source) will be returned for non-string resources.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="CloneValues"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160; and the type of the resource is not <see cref="string"/>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        string? GetString(string name, CultureInfo? culture = null);

        /// <summary>
        /// Returns a <see cref="MemoryStream"/> instance from the resource of the specified <paramref name="name"/> and <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the resource is localized. If the resource is not localized for
        /// this culture, the resource manager uses fallback rules to locate an appropriate resource. If this value is
        /// <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>
        /// A <see cref="MemoryStream"/> object from the specified resource localized for the specified <paramref name="culture"/>, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="CloneValues"/> property, the <see cref="GetObject">GetObject</see> method returns either
        /// a full copy of the specified resource, or always the same instance. For memory streams none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="GetStream">GetStream</see> method
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="CloneValues"/> property.</para>
        /// <para><see cref="GetStream">GetStream</see> can be used also for byte array resources. However, if the value is returned from compiled resources, then always a new copy of the byte array will be wrapped.</para>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>&#160;and <paramref name="name"/> is neither a <see cref="MemoryStream"/> nor a byte array resource, then
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns a stream wrapper for the same string value that is returned by the <see cref="GetString">GetString</see> method,
        /// which will be the raw XML content for non-string resources.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160;and the type of the resource is neither <see cref="MemoryStream"/> nor <see cref="Array">byte[]</see>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        MemoryStream? GetStream(string name, CultureInfo? culture = null);

        /// <summary>
        /// Gets the value of the specified resource localized for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <param name="culture">The culture for which the resource is localized. If the resource is not localized for
        /// this culture, the resource manager uses fallback rules to locate an appropriate resource. If this value is
        /// <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>
        /// If <see cref="SafeMode"/> is <see langword="true"/>, and the resource is from a .resx content, then the method returns a <see cref="ResXDataNode"/> instance instead of the actual deserialized value.
        /// Otherwise, returns the value of the resource localized for the specified <paramref name="culture"/>, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="CloneValues"/> property, the <see cref="GetObject">GetObject</see> method returns either
        /// a full copy of the specified resource, or always the same instance. For memory streams and byte arrays none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="GetStream">GetStream</see> method
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="CloneValues"/> property.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="CloneValues"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        object? GetObject(string name, CultureInfo? culture = null);

        /// <summary>
        /// Adds or replaces a resource object in the current <see cref="IExpandoResourceManager"/> with the specified
        /// <paramref name="name"/> for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to set.</param>
        /// <param name="culture">The culture of the resource to set. If this value is <see langword="null"/>,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property.</param>
        /// <param name="value">The value of the resource to set. If <see langword="null"/>, then a null reference will be explicitly
        /// stored for the specified <paramref name="culture"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>If <paramref name="value" /> is <see langword="null" />, a null reference will be explicitly stored.
        /// As a result, the subsequent <see cref="GetObject">GetObject</see> calls
        /// with the same <paramref name="culture" /> will fall back to the parent culture, or will return <see langword="null"/>&#160;if
        /// <paramref name="name" /> is not found in any parent cultures. However, enumerating the result set returned by
        /// <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> method will return the resources with <see langword="null"/>&#160;value.</para>
        /// <para>If the current <see cref="IExpandoResourceManager"/> is a <see cref="HybridResourceManager"/>, and you want to remove
        /// the user-defined ResX content and reset the original resource defined in the binary resource set (if any), use the <see cref="RemoveObject">RemoveObject</see> method.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException">The current <see cref="IExpandoResourceManager"/> is a <see cref="HybridResourceManager"/>, and
        /// <see cref="HybridResourceManager.Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        void SetObject(string name, object? value, CultureInfo? culture = null);

        /// <summary>
        /// Removes a resource object from the current <see cref="IExpandoResourceManager"/> with the specified
        /// <paramref name="name"/> for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The case-sensitive name of the resource to remove.</param>
        /// <param name="culture">The culture of the resource to remove. If this value is <see langword="null"/>,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>If this <see cref="IExpandoResourceManager"/> instance is a <see cref="HybridResourceManager"/>, and there is a binary resource
        /// defined for <paramref name="name"/> and <paramref name="culture"/>, then after this call the originally defined value will be returned by <see cref="GetObject">GetObject</see> method from the binary resources.
        /// If you want to force hiding the binary resource and make <see cref="GetObject">GetObject</see> to default to the parent <see cref="CultureInfo"/> of the specified <paramref name="culture"/>,
        /// then use the <see cref="SetObject">SetObject</see> method with a <see langword="null"/>&#160;value.</para>
        /// <para><paramref name="name"/> is considered as case-sensitive. If <paramref name="name"/> occurs multiple times
        /// in the resource set in case-insensitive manner, they can be removed one by one only.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException">The current <see cref="IExpandoResourceManager"/> is a <see cref="HybridResourceManager"/>, and
        /// <see cref="HybridResourceManager.Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        void RemoveObject(string name, CultureInfo? culture = null);

        /// <summary>
        /// Returns the value of the string metadata for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the metadata to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the metadata should be returned.
        /// If this value is <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property.
        /// Unlike in case of <see cref="GetString">GetString</see> method, no fallback is used if the metadata is not found in the specified culture. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.
        /// </param>
        /// <returns>
        /// The value of the metadata of the specified culture, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>, then instead of throwing an <see cref="InvalidOperationException"/>
        /// the raw XML value will be returned for non-string metadata.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="CloneValues"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160; and the type of the metadata is not <see cref="string"/>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        string? GetMetaString(string name, CultureInfo? culture = null);

        /// <summary>
        /// Returns a <see cref="MemoryStream"/> instance from the metadata of the specified <paramref name="name"/> and <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the metadata to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the metadata should be returned.
        /// If this value is <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property.
        /// Unlike in case of <see cref="GetStream">GetStream</see> method, no fallback is used if the metadata is not found in the specified culture. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.
        /// </param>
        /// <returns>
        /// A <see cref="MemoryStream"/> object from the specified metadata, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="CloneValues"/> property, the <see cref="GetMetaObject">GetMetaObject</see> method returns either
        /// a full copy of the specified metadata, or always the same instance. For memory streams none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="GetMetaStream">GetMetaStream</see> method
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="CloneValues"/> property.</para>
        /// <para><see cref="GetMetaStream">GetMetaStream</see> can be used also for byte array metadata.</para>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>&#160;and <paramref name="name"/> is neither a <see cref="MemoryStream"/> nor a byte array metadata, then
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns a stream wrapper for the same string value that is returned by the <see cref="GetString">GetString</see> method,
        /// which will be the raw XML content for non-string metadata.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160;and the type of the metadata is neither <see cref="MemoryStream"/> nor <see cref="Array">byte[]</see>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        MemoryStream? GetMetaStream(string name, CultureInfo? culture = null);

        /// <summary>
        /// Returns the value of the specified non-string metadata for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the metadata to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the metadata should be returned.
        /// If this value is <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property.
        /// Unlike in case of <see cref="GetObject">GetObject</see> method, no fallback is used if the metadata is not found in the specified culture. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.
        /// </param>
        /// <returns>
        /// If <see cref="SafeMode"/> is <see langword="true"/>, then the method returns a <see cref="ResXDataNode"/> instance instead of the actual deserialized value.
        /// Otherwise, returns the value of the metadata localized for the specified <paramref name="culture"/>, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="CloneValues"/> property, the <see cref="GetMetaObject">GetMetaObject</see> method returns either
        /// a full copy of the specified metadata, or always the same instance. For memory streams and byte arrays none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="GetMetaStream">GetMetaStream</see> method
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="CloneValues"/> property.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="CloneValues"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        object? GetMetaObject(string name, CultureInfo? culture = null);

        /// <summary>
        /// Adds or replaces a metadata object in the current <see cref="IExpandoResourceManager"/> with the specified
        /// <paramref name="name"/> for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the metadata to set.</param>
        /// <param name="culture">The culture of the metadata to set.
        /// If this value is <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property.</param>
        /// <param name="value">The value of the metadata to set. If <see langword="null"/>,  then a null reference will be explicitly
        /// stored for the specified <paramref name="culture"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>If <paramref name="value" /> is <see langword="null" />, a null reference will be explicitly stored.
        /// Its effect is similar to the <see cref="RemoveMetaObject">RemoveMetaObject</see> method: the subsequent <see cref="GetMetaObject">GetMetaObject</see> calls
        /// with the same <paramref name="culture" /> will return <see langword="null" />.
        /// However, enumerating the result set returned by <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> method will return the meta objects with <see langword="null"/>&#160;value.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException">The current <see cref="IExpandoResourceManager"/> is a <see cref="HybridResourceManager"/>, and
        /// <see cref="HybridResourceManager.Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        void SetMetaObject(string name, object? value, CultureInfo? culture = null);

        /// <summary>
        /// Removes a metadata object from the current <see cref="IExpandoResourceManager"/> with the specified
        /// <paramref name="name"/> for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The case-sensitive name of the metadata to remove.</param>
        /// <param name="culture">The culture of the metadata to remove.
        /// If this value is <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para><paramref name="name"/> is considered as case-sensitive. If <paramref name="name"/> occurs multiple times
        /// in the resource set in case-insensitive manner, they can be removed one by one only.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException">The current <see cref="IExpandoResourceManager"/> is a <see cref="HybridResourceManager"/>, and
        /// <see cref="HybridResourceManager.Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        void RemoveMetaObject(string name, CultureInfo? culture = null);

        /// <summary>
        /// Saves the resource set of a particular <paramref name="culture"/> if it has been already loaded.
        /// </summary>
        /// <param name="culture">The culture of the resource set to save.</param>
        /// <param name="force"><see langword="true"/>&#160;to save the resource set even if it has not been modified; <see langword="false"/>&#160;to save it only if it has been modified. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <param name="compatibleFormat">If set to <see langword="true"/>, the result .resx file can be read by a <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> instance
        /// and the Visual Studio Resource Editor. If set to <see langword="false"/>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />),
        /// but the result can be read only by the <see cref="ResXResourceReader" /> class. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns><see langword="true"/>&#160;if the resource set of the specified <paramref name="culture"/> has been saved;
        /// otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="IOException">The resource set could not be saved.</exception>
        bool SaveResourceSet(CultureInfo culture, bool force = false, bool compatibleFormat = false);

        /// <summary>
        /// Saves all already loaded resources.
        /// </summary>
        /// <param name="force"><see langword="true"/>&#160;to save all of the already loaded resource sets regardless if they have been modified; <see langword="false"/>&#160;to save only the modified resource sets. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <param name="compatibleFormat">If set to <see langword="true"/>, the result .resx files can be read by a <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> instance
        /// and the Visual Studio Resource Editor. If set to <see langword="false"/>, the result .resx files are often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />),
        /// but the result can be read only by the <see cref="ResXResourceReader" /> class. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns><see langword="true"/>&#160;if at least one resource set has been saved; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceManager"/> is already disposed.</exception>
        /// <exception cref="IOException">A resource set could not be saved.</exception>
        bool SaveAllResources(bool force = false, bool compatibleFormat = false);

        #endregion
    }
}
