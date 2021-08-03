#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXResourceReader.cs
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
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;

using KGySoft.Collections;
using KGySoft.Reflection;
using KGySoft.Serialization.Binary;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Enumerates XML resource (.resx) files and streams, and reads the sequential resource name and value pairs.
    /// <br/>See the <strong>Remarks</strong> section for examples and for the differences compared to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> class.
    /// </summary>
    /// <remarks>
    /// <note>This class is similar to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a>
    /// in <c>System.Windows.Forms.dll</c>. See the <a href="#comparison">Comparison with System.Resources.ResXResourceReader</a> section for the differences.</note>
    /// <note type="tip">To see when to use the <see cref="ResXResourceReader"/>, <see cref="ResXResourceWriter"/>, <see cref="ResXResourceSet"/>, <see cref="ResXResourceManager"/>, <see cref="HybridResourceManager"/> and <see cref="DynamicResourceManager"/>
    /// classes see the documentation of the <see cref="N:KGySoft.Resources">KGySoft.Resources</see> namespace.</note>
    /// <para>You can use the <see cref="ResXResourceReader"/> class to enumerate resources in .resx files by traversing the dictionary enumerator (<see cref="IDictionaryEnumerator"/>) that is returned by the
    /// <see cref="GetEnumerator">GetEnumerator</see> method. You call the methods provided by <see cref="IDictionaryEnumerator"/> to advance to the next resource and to read the name and value of each resource in the .resx file.
    /// <note>The <see cref="ResXResourceReader"/> class provides more enumerators.
    /// <list type="bullet">
    /// <item>The <see cref="GetEnumerator">GetEnumerator</see> method returns an <see cref="IDictionaryEnumerator"/> object, which enumerates the resources.
    /// The <see cref="IDictionaryEnumerator.Key">IDictionaryEnumerator.Key</see> property returns the resource names, while <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see>
    /// returns either <see cref="ResXDataNode"/> instances, if <see cref="SafeMode"/> property is <see langword="true"/>; or returns deserialized <see cref="object"/> instances if <see cref="SafeMode"/> property is <see langword="false"/>.</item>
    /// <item>The <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> method returns an <see cref="IDictionaryEnumerator"/> object, which enumerates the metadata entries.
    /// The <see cref="IDictionaryEnumerator.Key">IDictionaryEnumerator.Key</see> property returns the metadata names, while <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see>
    /// returns either <see cref="ResXDataNode"/> instances, if <see cref="SafeMode"/> property is <see langword="true"/>; or returns deserialized <see cref="object"/> instances if <see cref="SafeMode"/> property is <see langword="false"/>.</item>
    /// <item>The <see cref="GetAliasEnumerator">GetAliasEnumerator</see> method returns an <see cref="IDictionaryEnumerator"/> object, which enumerates the aliases in the .resx file.
    /// The <see cref="IDictionaryEnumerator.Key">IDictionaryEnumerator.Key</see> property returns the alias names, while <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see>
    /// returns the corresponding assembly names for the alias names.</item>
    /// <item>As an explicit interface implementation, <see cref="ResXResourceReader"/> implements <see cref="IEnumerable.GetEnumerator">IEnumerable.GetEnumerator</see> method, which returns the same enumerator as
    /// the <see cref="GetEnumerator">GetEnumerator</see> method as an <see cref="IEnumerator"/> instance. The <see cref="IEnumerator.Current">IEnumerator.Current</see> property will return <see cref="DictionaryEntry"/> instances.</item>
    /// </list>
    /// </note>
    /// </para>
    /// <para>If the <see cref="SafeMode"/> property is <see langword="true"/>, the value of the <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> property is a <see cref="ResXDataNode"/>
    /// instance rather than the resource value. This makes possible to check the raw .resx content before deserialization if the .resx file is from an untrusted source. See also the example at <see cref="ResXDataNode"/>.</para>
    /// <para>If you want to retrieve named resources from a .resx file rather than enumerating its resources, then you can instantiate a <see cref="ResXResourceSet"/> object and call its
    /// <see cref="ResXResourceSet.GetString(string)">GetString</see>/<see cref="ResXResourceSet.GetObject(string)">GetObject</see>, <see cref="ResXResourceSet.GetMetaString">GetMetaString</see>/<see cref="ResXResourceSet.GetMetaObject">GetMetaObject</see> and <see cref="ResXResourceSet.GetAliasValue">GetAliasValue</see> methods.
    /// Also <see cref="ResXResourceSet"/> supports <see cref="ResXResourceSet.SafeMode"/>.</para>
    /// <example>
    /// The following example shows how to enumerate the resources, metadata and aliases of a .resx file and what is the difference between safe and non-safe mode.
    /// Please note that <see cref="SafeMode"/> property can be switched on and off during the enumeration, too. Please also note that the values returned by the <see cref="GetAliasEnumerator">GetAliasEnumerator</see> are always
    /// strings, regardless of the value of <see cref="SafeMode"/> property. See also the example of the <see cref="ResXDataNode"/> class to see how to examine the properties of the <see cref="ResXDataNode"/> instances
    /// in safe mode.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Collections;
    /// using System.IO;
    /// using KGySoft.Resources;
    /// 
    /// public class Example
    /// {
    ///     private const string resx = @"<?xml version='1.0' encoding='utf-8'?>
    /// <root>
    ///   <data name='string'>
    ///     <value>Test string</value>
    ///     <comment>Default data type is string.</comment>
    ///   </data>
    ///
    ///   <metadata name='meta string'>
    ///     <value>Meta String</value>
    ///   </metadata>
    ///
    ///   <data name='int' type='System.Int32'>
    ///     <value>42</value>
    ///   </data>
    ///
    ///   <assembly alias='CustomAlias' name='System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a' />
    ///
    ///   <data name='color' type='System.Drawing.Color, CustomAlias'>
    ///     <value>Red</value>
    ///     <comment>When this entry is deserialized, System.Drawing assembly will be loaded.</comment>
    ///   </data>
    ///
    ///   <data name='bytes' type='System.Byte[]'>
    ///     <value>VGVzdCBieXRlcw==</value>
    ///   </data>
    ///
    ///   <data name='dangerous' mimetype='application/x-microsoft.net.object.binary.base64'>
    ///     <value>YmluYXJ5</value>
    ///     <comment>BinaryFormatter will throw an exception for this invalid content.</comment>
    ///   </data>
    ///
    /// </root>";
    ///
    ///    public static void Main()
    ///    {
    ///        var reader = new ResXResourceReader(new StringReader(resx));
    ///        Console.WriteLine("____Resources in .resx:____");
    ///        Dump(reader, reader.GetEnumerator);
    ///        Console.WriteLine("____Metadata in .resx:____");
    ///        Dump(reader, reader.GetMetadataEnumerator);
    ///        Console.WriteLine("____Aliases in .resx:____");
    ///        Dump(reader, reader.GetAliasEnumerator);
    ///    }
    /// 
    ///    private static void Dump(ResXResourceReader reader, Func<IDictionaryEnumerator> getEnumeratorFunction)
    ///    {
    ///        var enumerator = getEnumeratorFunction();
    ///        while (enumerator.MoveNext())
    ///        {
    ///            Console.WriteLine($"Name: {enumerator.Key}");
    ///            reader.SafeMode = true;
    ///            Console.WriteLine($"  Value in SafeMode:     {enumerator.Value} ({enumerator.Value.GetType()})");
    ///            try
    ///            {
    ///                reader.SafeMode = false;
    ///                Console.WriteLine($"  Value in non-SafeMode: {enumerator.Value} ({enumerator.Value.GetType()})");
    ///            }
    ///            catch (Exception e)
    ///            {
    ///                Console.WriteLine($"Getting the deserialized value thrown an exception: {e.Message}");
    ///            }
    ///            Console.WriteLine();
    ///        }
    ///    }
    ///}]]>
    ///
    /// // The example displays the following output:
    /// // ____Resources in .resx:____
    /// // Name: string
    /// // Value in SafeMode:     Test string (KGySoft.Resources.ResXDataNode)
    /// // Value in non-SafeMode: Test string (System.String)
    ///
    /// // Name: int
    /// // Value in SafeMode:     42 (KGySoft.Resources.ResXDataNode)
    /// // Value in non-SafeMode: 42 (System.Int32)
    ///
    /// // Name: color
    /// // Value in SafeMode:     Red (KGySoft.Resources.ResXDataNode)
    /// // Value in non-SafeMode: Color[Red] (System.Drawing.Color)
    ///
    /// // Name: bytes
    /// // Value in SafeMode:     VGVzdCBieXRlcw== (KGySoft.Resources.ResXDataNode)
    /// // Value in non-SafeMode: System.Byte[] (System.Byte[])
    ///
    /// // Name: dangerous
    /// // Value in SafeMode:     YmluYXJ5 (KGySoft.Resources.ResXDataNode)
    /// // Getting the deserialized value thrown an exception: End of Stream encountered before parsing was completed.
    ///
    /// // ____Metadata in .resx:____
    /// // Name: meta string
    /// // Value in SafeMode:     Meta String (KGySoft.Resources.ResXDataNode)
    /// // Value in non-SafeMode: Meta String (System.String)
    ///
    /// // ____Aliases in .resx:____
    /// // Name: CustomAlias
    /// // Value in SafeMode:     System.Drawing, Version= 4.0.0.0, Culture= neutral, PublicKeyToken= b03f5f7f11d50a3a (System.String)
    /// // Value in non-SafeMode: System.Drawing, Version= 4.0.0.0, Culture= neutral, PublicKeyToken= b03f5f7f11d50a3a (System.String)</code>
    /// </example>
    /// <para>
    /// By default, <see cref="ResXResourceReader"/> allows duplicated keys with different values (see <see cref="AllowDuplicatedKeys"/> property). Though such a .resx file is not strictly valid, its
    /// complete content can be retrieved. When <see cref="AllowDuplicatedKeys"/> is <see langword="true"/>, <see cref="GetEnumerator">GetEnumerator</see>, <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> and
    /// <see cref="GetAliasEnumerator">GetAliasEnumerator</see> return a lazy enumerator for the first time meaning the .resx file is parsed only during the enumeration. When any of the enumerators are obtained
    /// for the second time, a cached enumerator is returned with the whole parsed .resx content. If duplicates are disabled, the lastly defined values will be returned of a redefined name. This behavior is
    /// similar to the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> class, which does not allow duplicates.
    /// </para>
    /// <example>
    /// The following example demonstrates the difference of lazy (allowing duplicates) and greedy (disabling duplicates) reading.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Collections;
    /// using System.IO;
    /// using KGySoft.Resources;
    ///
    /// public class Example
    /// {
    ///     private const string resx = @"<?xml version='1.0' encoding='utf-8'?>
    /// <root>
    ///   <data name='item'>
    ///     <value>Test string</value>
    ///   </data>
    ///
    ///   <data name='item'>
    ///     <value>This is a duplicate for key 'item'.</value>
    ///   </data>
    /// </root>";
    ///
    ///     public static void Main()
    ///     {
    ///         // Allowing duplicates and lazy reading.
    ///         Console.WriteLine("-------Lazy reading------");
    ///         var reader = new ResXResourceReader(new StringReader(resx)) { AllowDuplicatedKeys = true };
    ///         IDictionaryEnumerator enumerator = reader.GetEnumerator();
    ///         Dump(enumerator); // if resx contains a syntax error, an exception is thrown during the enumeration.
    ///
    ///         // Disabling duplicates and lazy reading
    ///         Console.WriteLine("-------Greedy reading------");
    ///         reader = new ResXResourceReader(new StringReader(resx)) { AllowDuplicatedKeys = false };
    ///         enumerator = reader.GetEnumerator(); // if resx contains a syntax error, an exception is thrown here.
    ///         Dump(enumerator);
    ///     }
    ///
    ///     private static void Dump(IDictionaryEnumerator enumerator)
    ///     {
    ///         while (enumerator.MoveNext())
    ///         {
    ///             Console.WriteLine($"Key: {enumerator.Key}");
    ///             Console.WriteLine($"Value: {enumerator.Value}");
    ///             Console.WriteLine();
    ///         }
    ///     }
    /// }]]>
    ///
    /// // The example displays the following output:
    /// // -------Lazy reading------
    /// // Key: item
    /// // Value: Test string
    /// //
    /// // Key: item
    /// // Value: This is a duplicate for key 'item'.
    /// //
    /// // -------Greedy reading------
    /// // Key: item
    /// // Value: This is a duplicate for key 'item'.</code>
    /// </example>
    /// <h1 class="heading">Comparison with System.Resources.ResXResourceReader<a name="comparison">&#160;</a></h1>
    /// <para><see cref="ResXResourceReader"/> can read .resx files produced both by <see cref="ResXResourceWriter"/> and <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcewriter" target="_blank">System.Resources.ResXResourceWriter</a>.
    /// <note>When reading a .resx file written by the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcewriter" target="_blank">System.Resources.ResXResourceWriter</a> class,
    /// the <c>System.Windows.Forms.dll</c> is not loaded during resolving <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a>
    /// and <strong>System.Resources.ResXNullRef</strong> types.</note>
    /// </para>
    /// <para><strong>Incompatibility</strong> with <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a>:
    /// <list type="bullet">
    /// <item>Constructors do not have overloads with <see cref="AssemblyName">AssemblyName[]</see> parameters. The <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a>
    /// uses them to load the assemblies in advance occasionally by calling the obsolete <see cref="Assembly.LoadWithPartialName(string)">Assembly.LoadPartial</see> method. However, this <see cref="ResXResourceReader"/>
    /// implementation uses the <see cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</see> method, which does not use obsolete techniques (and if <see cref="SafeMode"/> is <see langword="true"/>,
    /// then no type resolving, assembly loading and deserialization occurs at all, until explicit request).
    /// If you need a completely custom type resolution the constructor overloads with <see cref="ITypeResolutionService"/> parameters still can be used.</item>
    /// <item>This <see cref="ResXResourceReader"/> is a sealed class.</item>
    /// <item>After disposing the <see cref="ResXResourceReader"/> instance or calling the <see cref="Close">Close</see> method the enumerators cannot be obtained: an <see cref="ObjectDisposedException"/> will be thrown
    /// on calling <see cref="GetEnumerator">GetEnumerator</see>, <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> and <see cref="GetAliasEnumerator">GetAliasEnumerator</see> methods.</item>
    /// <item>After disposing the <see cref="ResXResourceReader"/> instance or calling the <see cref="Close">Close</see> method every source stream will be closed (if any).</item>
    /// <item>Unlike <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a>, this implementation returns every resources and metadata of the
    /// same name by default. This behavior can be adjusted by <see cref="AllowDuplicatedKeys"/> property.</item>
    /// <item><a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> often throws <see cref="ArgumentException"/> on getting the enumerator
    /// or on retrieving the value of a <see cref="ResXDataNode"/> instance, which contains invalid data. In contrast, this implementation may throw <see cref="XmlException"/>, <see cref="TypeLoadException"/> or <see cref="NotSupportedException"/> instead.</item>
    /// <item>Though the <see cref="UseResXDataNodes"/> property is still supported, it is obsolete in favor of <see cref="SafeMode"/> property.</item>
    /// <item>In <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> if <see cref="UseResXDataNodes"/> property is <see langword="true"/>,
    /// the resource and metadata entries are mixed in the returned enumerator, while when it is <see langword="false"/>, then only the resources are returned. In this implementation the <see cref="GetEnumerator">GetEnumerator</see> always
    /// returns only the resources and <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> returns the metadata regardless of the value of the <see cref="UseResXDataNodes"/> and <see cref="SafeMode"/> properties.</item>
    /// </list>
    /// </para>
    /// <para><strong>New features and improvements</strong> compared to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a>:
    /// <list type="bullet">
    /// <item><term>Lazy processing</term>
    /// <description>If <see cref="AllowDuplicatedKeys"/> is <see langword="true"/>, the .resx file is processed on demand, during the actual enumeration. The .resx file is processed immediately if
    /// <see cref="AllowDuplicatedKeys"/> is <see langword="false"/>. If <see cref="AllowDuplicatedKeys"/> is <see langword="true"/>&#160;and any enumerator is obtained after getting one, the rest of the .resx file is immediately processed.</description></item>
    /// <item><term>Handling duplicates</term>
    /// <description>If <see cref="AllowDuplicatedKeys"/> is <see langword="true"/>, every occurrence of a duplicated name is returned by the enumerators. Otherwise, only the last occurrence of
    /// a name is returned.</description></item>
    /// <item><term>Headers</term>
    /// <description>The .resx header is allowed to be completely missing; however, it is checked when exists and <see cref="CheckHeader"/> property is <see langword="true"/>. If header tags contain invalid values a <see cref="NotSupportedException"/> may be thrown during the enumeration.
    /// You can configure the <see cref="ResXResourceWriter"/> class to omit the header by the <see cref="ResXResourceWriter.OmitHeader">ResXResourceWriter.OmitHeader</see> property.</description></item>
    /// <item><term>Using <see cref="ResXDataNode"/> instances</term>
    /// <description>The <see cref="SafeMode"/> (<see cref="UseResXDataNodes"/>) property can be toggled also after getting an enumerator or even during the enumeration.</description></item>
    /// <item><term>Clear purpose of the enumerators</term>
    /// <description>The <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader.getenumerator" target="_blank">System.Resources.ResXResourceReader.GetEnumerator</a> either returns resources only or returns both resources and metadata mixed together
    /// depending on the value of the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader.useresxdatanodes" target="_blank">System.Resources.ResXResourceReader.UseResXDataNodes</a> property.
    /// This <see cref="ResXResourceReader"/> implementation has separated <see cref="GetEnumerator">GetEnumerator</see>, <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> and <see cref="GetAliasEnumerator">GetAliasEnumerator</see>
    /// methods, which return always the resources, metadata and aliases, respectively.</description></item>
    /// <item><term>Security</term>
    /// <description>If <see cref="SafeMode"/> is <see langword="true"/>, no deserialization, assembly loading and type resolving occurs until a deserialization is explicitly requested
    /// by calling the <see cref="ResXDataNode.GetValue">ResXDataNode.GetValue</see> or <see cref="ResXDataNode.GetValueSafe">ResXDataNode.GetValueSafe</see> method on the <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see>
    /// instances returned by the <see cref="GetEnumerator">GetEnumerator</see> and <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> methods.</description></item>
    /// <item><term>Base path</term>
    /// <description>The <see cref="BasePath"/> property, which is used for resolving file references can be set during the enumeration, too.</description></item>
    /// <item><term>New MIME type</term>
    /// <description>A new MIME type <c>text/kgysoft.net/object.binary.base64</c> is supported, indicating that an object is serialized by <see cref="BinarySerializationFormatter"/> instead of <see cref="BinaryFormatter"/>.
    /// The <see cref="ResXResourceWriter"/> can produce such .resx content if <see cref="ResXResourceWriter.CompatibleFormat">ResXResourceWriter.CompatibleFormat</see> is <see langword="false"/>.</description></item>
    /// <item><term>Soap formatter support</term>
    /// <description>The Soap formatter support is provided without referencing the <c>System.Runtime.Serialization.Formatters.Soap.dll</c> assembly. If the assembly cannot be loaded from the GAC (platform dependent),
    /// then a <see cref="NotSupportedException"/> will be thrown.</description></item>
    /// <item><term>Type resolving</term>
    /// <description>If an <see cref="ITypeResolutionService"/> instance is passed to one of the constructors, it is used also for the type references in <see cref="ResXFileRef"/> instances.</description></item>
    /// </list></para>
    /// </remarks>
    /// <seealso cref="ResXDataNode"/>
    /// <seealso cref="ResXResourceWriter"/>
    /// <seealso cref="ResXResourceSet"/>
    /// <seealso cref="ResXResourceManager"/>
    /// <seealso cref="HybridResourceManager"/>
    /// <seealso cref="DynamicResourceManager"/>
    public sealed class ResXResourceReader : IResourceReader, IResXResourceContainer
    {
        #region Nested types

        #region Enumerations

        private enum States { Created, Reading, Read, Disposed };

        #endregion

        #region Nested classes

        #region LazyEnumerator class

        /// <summary>
        /// An enumerator that reads the underlying .resx on-demand. Returns the duplicated elements, too.
        /// </summary>
        private sealed class LazyEnumerator : IDictionaryEnumerator
        {
            #region Enumerations

            private enum EnumeratorStates
            {
                BeforeFirst,
                Enumerating,
                AfterLast
            }

            #endregion

            #region Fields

            private readonly ResXResourceReader owner;
            private readonly ResXEnumeratorModes mode;

            private EnumeratorStates state;
            private string? key;
            private ResXDataNode? value;

            /// <summary>
            /// Represents buffered items, which should be returned before reading the next items from the underlying XML.
            /// Reset and ReadToEnd may produce buffered items.
            /// </summary>
            private IEnumerator<KeyValuePair<string, ResXDataNode>>? bufferedEnumerator;

            #endregion

            #region Properties

            public DictionaryEntry Entry
            {
                get
                {
                    if (state != EnumeratorStates.Enumerating)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);

                    if (mode == ResXEnumeratorModes.Aliases)
                        return new DictionaryEntry(key!, value!.ValueInternal);

                    return owner.safeMode
                        ? new DictionaryEntry(key!, value)
                        : new DictionaryEntry(key!, value!.GetValue(owner.typeResolver, owner.basePath));
                }
            }

            public object Key
            {
                get
                {
                    if (state != EnumeratorStates.Enumerating)
                        Throw.InvalidOperationException(Res.IEnumeratorEnumerationNotStartedOrFinished);

                    return key!;
                }
            }

            public object? Value => Entry.Value;

            public object Current => Entry;

            #endregion

            #region Constructors

            internal LazyEnumerator(ResXResourceReader owner, ResXEnumeratorModes mode)
            {
                this.owner = owner;
                this.mode = mode;
                state = EnumeratorStates.BeforeFirst;
            }

            #endregion

            #region Methods

            #region Public Methods

            public bool MoveNext()
            {
                if (state == EnumeratorStates.AfterLast)
                    return false;

                if (state == EnumeratorStates.BeforeFirst)
                    state = EnumeratorStates.Enumerating;

                lock (owner.syncRoot)
                {
                    // if we have an enumerator with buffered data, we should return the buffered entries first
                    if (bufferedEnumerator != null)
                    {
                        if (bufferedEnumerator.MoveNext())
                        {
                            KeyValuePair<string, ResXDataNode> current = bufferedEnumerator.Current;
                            key = current.Key;
                            value = current.Value;
                            return true;
                        }

                        bufferedEnumerator = null;
                    }

                    // otherwise, we read the next item from the source XML
                    if (owner.ReadNext(mode, out key, out value))
                        return true;

                    state = EnumeratorStates.AfterLast;
                }

                return false;
            }

            public void Reset()
            {
                lock (owner.syncRoot)
                {
                    bufferedEnumerator = null;
                    state = EnumeratorStates.BeforeFirst;
                    bufferedEnumerator = mode switch
                    {
                        ResXEnumeratorModes.Resources => owner.resources?.GetEnumerator() ?? Throw.ObjectDisposedException<IEnumerator<KeyValuePair<string, ResXDataNode>>>(),
                        ResXEnumeratorModes.Metadata => owner.metadata?.GetEnumerator() ?? Throw.ObjectDisposedException<IEnumerator<KeyValuePair<string, ResXDataNode>>>(),
                        ResXEnumeratorModes.Aliases => owner.aliases?.Select(ResXResourceEnumerator.SelectAlias).GetEnumerator() ?? Throw.ObjectDisposedException<IEnumerator<KeyValuePair<string, ResXDataNode>>>(),
                        _ => Throw.InternalError<IEnumerator<KeyValuePair<string, ResXDataNode>>>($"Unexpected mode: {mode}")
                    };
                }
            }

            #endregion

            #region Internal Methods

            /// <summary>
            /// Hasting the enumeration and reading all of the elements into a buffer. Occurs on a second GetEnumerator
            /// call while the first enumeration has not been finished.
            /// </summary>
            internal void ReadToEnd()
            {
                lock (owner.syncRoot)
                {
                    if (bufferedEnumerator == null)
                        bufferedEnumerator = owner.ReadToEnd(mode).GetEnumerator();
                    else
                    {
                        // there is already a buffer: occurs if the enumerator has been reset.
                        var result = new List<KeyValuePair<string, ResXDataNode>>();
                        while (bufferedEnumerator.MoveNext())
                            result.Add(bufferedEnumerator.Current);

                        IEnumerable<KeyValuePair<string, ResXDataNode>> rest = owner.ReadToEnd(mode);
                        if (result.Count > 0)
                        {
                            result.AddRange(rest);
                            bufferedEnumerator = result.GetEnumerator();
                        }
                        else
                            bufferedEnumerator = rest.GetEnumerator();
                    }
                }
            }

            #endregion

            #endregion
        }

        #endregion

        #region ResXReader class

        /// <summary>
        /// Required because a reader returned by XmlReader.Create would normalize the \r characters
        /// </summary>
        private sealed class ResXReader : XmlTextReader
        {
            #region Constructors

#if NET35
            [SuppressMessage("Security", "CA3077:InsecureDTDProcessing", Justification = "False alarm, DTD processing is set to prohibited in the constructor body.")] 
#endif
            internal ResXReader(Stream stream)
                : base(stream, InitNameTable())
            {
                WhitespaceHandling = WhitespaceHandling.Significant;
                XmlResolver = null;
#if NET35
                ProhibitDtd = true;
#else
                DtdProcessing = DtdProcessing.Prohibit;
#endif
            }

#if NET35 || NET40 || NET45
            [SuppressMessage("Security", "CA3077:InsecureDTDProcessing", Justification = "False alarm, DTD processing is set to prohibited in the called overloaded constructor.")] 
#endif
            internal ResXReader(string fileName)
                : this(File.OpenRead(fileName))
            {
            }

#if NET35
            [SuppressMessage("Security", "CA3077:InsecureDTDProcessing", Justification = "False alarm, DTD processing is set to prohibited in the constructor body.")]
#endif
            internal ResXReader(TextReader reader)
                : base(reader, InitNameTable())
            {
                WhitespaceHandling = WhitespaceHandling.Significant;
                XmlResolver = null;
#if NET35
                ProhibitDtd = true;
#else
                DtdProcessing = DtdProcessing.Prohibit;
#endif
            }

            #endregion

            #region Methods

            private static XmlNameTable InitNameTable()
            {
                // mime types are not compared by reference so they are not here
                XmlNameTable nameTable = new NameTable();
                nameTable.Add(ResXCommon.TypeStr);
                nameTable.Add(ResXCommon.NameStr);
                nameTable.Add(ResXCommon.DataStr);
                nameTable.Add(ResXCommon.MetadataStr);
                nameTable.Add(ResXCommon.CommentStr);
                nameTable.Add(ResXCommon.MimeTypeStr);
                nameTable.Add(ResXCommon.ValueStr);
                nameTable.Add(ResXCommon.ResHeaderStr);
                nameTable.Add(ResXCommon.VersionStr);
                nameTable.Add(ResXCommon.ResMimeTypeStr);
                nameTable.Add(ResXCommon.ReaderStr);
                nameTable.Add(ResXCommon.WriterStr);
                nameTable.Add(ResXCommon.AssemblyStr);
                nameTable.Add(ResXCommon.AliasStr);
                return nameTable;
            }

            #endregion
        }

        #endregion

        #endregion

        #endregion

        #region Fields

        private readonly object syncRoot = new object();
        private readonly ITypeResolutionService? typeResolver;

        /// <summary>
        /// The internally created reader. Will be closed automatically when stream ends or on Dispose
        /// </summary>
        private XmlReader? reader;

        private string? basePath;
        private States state = States.Created;

        /// <summary>
        /// The currently active aliases. Same as <see cref="aliases"/> if duplication is disabled.
        /// </summary>
        private StringKeyedDictionary<string>? activeAliases;

        private ICollection<KeyValuePair<string, string>>? aliases;
        private ICollection<KeyValuePair<string, ResXDataNode>>? resources;
        private ICollection<KeyValuePair<string, ResXDataNode>>? metadata;

        /// <summary>
        /// Stored in a field so first enumeration can be handled in a special way if duplicates are allowed.
        /// </summary>
        private LazyEnumerator? enumerator;

        private bool safeMode;
        private bool checkHeader;
        private bool allowDuplicatedKeys = true;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets the base path for the relative file path specified in a <see cref="ResXFileRef"/> object.
        /// <br/>Default value: <see langword="null"/>.
        /// </summary>
        /// <returns>
        /// A path that, if prepended to the relative file path specified in a <see cref="ResXFileRef"/> object, yields an absolute path to a resource file.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The <see cref="Close">Close</see> or <see cref="IDisposable.Dispose">IDisposable.Dispose</see> method has already been called on this
        /// <see cref="ResXResourceReader"/> instance.</exception>
        /// <remarks>
        /// Unlike in case of <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> class, in this
        /// <see cref="ResXResourceReader"/> implementation this property can be set even after calling the <see cref="GetEnumerator">GetEnumerator</see>, <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see>
        /// or <see cref="GetAliasEnumerator">GetAliasEnumerator</see> methods.
        /// </remarks>
        public string? BasePath
        {
            get => basePath;
            set
            {
                switch (state)
                {
                    case States.Disposed:
                        Throw.ObjectDisposedException();
                        break;
                    default:
                        basePath = value;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether <see cref="ResXDataNode"/> objects are returned when reading the current XML resource file or stream.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <note>This property is maintained due to compatibility reasons with the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> class.
        /// Use <see cref="SafeMode"/> property instead.</note>
        /// </remarks>
        /// <seealso cref="ResXResourceReader"/>
        /// <seealso cref="SafeMode"/>
        /// <seealso cref="ResXResourceSet.SafeMode"/>
        /// <seealso cref="ResXResourceManager.SafeMode"/>
        [Obsolete("This property is maintained due to compatibility reasons with the System.Windows.Forms.ResXResourceReader class. Use SafeMode property instead.")]
        public bool UseResXDataNodes
        {
            get => safeMode;
            set => SafeMode = value;
        }

        /// <summary>
        /// Gets or sets whether <see cref="ResXDataNode"/> objects are returned when reading the current XML resource file or stream.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="Close">Close</see> or <see cref="IDisposable.Dispose">IDisposable.Dispose</see> method has already been called on this
        /// <see cref="ResXResourceReader"/> instance.</exception>
        /// <remarks>
        /// <para>When <see cref="SafeMode"/> is <see langword="true"/>, then objects returned by the <see cref="GetEnumerator">GetEnumerator</see> and <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> methods
        /// return <see cref="ResXDataNode"/> instances instead of deserialized objects. You can retrieve the deserialized
        /// objects on demand by calling the <see cref="ResXDataNode.GetValue">ResXDataNode.GetValue</see> method on the <see cref="ResXDataNode"/> instance.
        /// <br/>See also the examples at the <strong>Remarks</strong> section of the <see cref="ResXResourceReader"/> class.</para>
        /// </remarks>
        /// <seealso cref="ResXResourceReader"/>
        /// <seealso cref="ResXResourceSet.SafeMode"/>
        /// <seealso cref="ResXResourceManager.SafeMode"/>
        public bool SafeMode
        {
            get => safeMode;
            set
            {
                if (state == States.Disposed)
                    Throw.ObjectDisposedException();
                safeMode = value;
            }
        }

        /// <summary>
        /// Gets or sets whether "resheader" entries are checked in the .resx file. When <see langword="true"/>, a <see cref="NotSupportedException"/>
        /// can be thrown during the enumeration when "resheader" entries contain invalid values. When header entries are
        /// missing, no exception is thrown.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">In a set operation, a value cannot be specified because the XML resource file has already been accessed and is in use.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="Close">Close</see> or <see cref="IDisposable.Dispose">IDisposable.Dispose</see> method has already been called on this
        /// <see cref="ResXResourceReader"/> instance.</exception>
        public bool CheckHeader
        {
            get => checkHeader;
            set
            {
                switch (state)
                {
                    case States.Created:
                        checkHeader = value;
                        break;
                    case States.Disposed:
                        Throw.ObjectDisposedException();
                        break;
                    default:
                        Throw.InvalidOperationException(Res.ResourcesInvalidResXReaderPropertyChange);
                        break;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether all entries of same name of the .resx file should be returned.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>If an element is defined more than once and <see cref="AllowDuplicatedKeys"/> is <see langword="true"/>,
        /// then the enumeration returns every occurrence of the entries with identical names.
        /// If <see cref="AllowDuplicatedKeys"/> is <see langword="false"/>&#160;the enumeration returns always the last occurrence of the entries with identical names.</para>
        /// <para>If duplicated keys are allowed, the enumeration of the .resx file is lazy for the first time.
        /// A lazy enumeration means that the underlying .resx file is read only on demand. It is possible that
        /// not the whole .resx is read if enumeration is canceled. After the first enumeration elements are cached.</para>
        /// <note>To be compatible with the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a>
        /// class set the value of this property to <see langword="false"/>.</note>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="Close">Close</see> or <see cref="IDisposable.Dispose">IDisposable.Dispose</see> method has already been called on this
        /// <see cref="ResXResourceReader"/> instance.</exception>
        /// <exception cref="InvalidOperationException">In a set operation, a value cannot be specified because the XML resource file has already been accessed and is in use.</exception>
        public bool AllowDuplicatedKeys
        {
            get => allowDuplicatedKeys;
            set
            {
                switch (state)
                {
                    case States.Created:
                        allowDuplicatedKeys = value;
                        break;
                    case States.Disposed:
                        Throw.ObjectDisposedException();
                        break;
                    default:
                        Throw.InvalidOperationException(Res.ResourcesInvalidResXReaderPropertyChange);
                        break;
                }
            }
        }

        #endregion

        #region Explicitly Implemented Interface Properties

        ICollection<KeyValuePair<string, ResXDataNode>>? IResXResourceContainer.Resources => resources;
        ICollection<KeyValuePair<string, ResXDataNode>>? IResXResourceContainer.Metadata => metadata;
        ICollection<KeyValuePair<string, string>>? IResXResourceContainer.Aliases => aliases;
        ITypeResolutionService? IResXResourceContainer.TypeResolver => typeResolver;
        bool IResXResourceContainer.AutoFreeXmlData => false;
        int IResXResourceContainer.Version => 0;
        bool IResXResourceContainer.CloneValues => false;

        #endregion

        #endregion

        #region Construction and Destruction

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceReader"/> class for the specified resource file.
        /// </summary>
        /// <param name="fileName">The name of an XML resource file that contains resources.</param>
        /// <param name="typeResolver">An object that resolves type names specified in a resource. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <note type="tip">To create a <see cref="ResXResourceReader"/> from a string use the <see cref="FromFileContents">FromFileContents</see> method.</note>
        /// </remarks>
#if NETFRAMEWORK
        [SuppressMessage("Security", "CA3075:Insecure DTD processing in XML",
            Justification = "False alarm, DTD processing is set to prohibited in ResXReader constructor.")] 
#endif
        public ResXResourceReader(string fileName, ITypeResolutionService? typeResolver = null)
        {
            if (fileName == null!)
                Throw.ArgumentNullException(Argument.fileName);

            reader = new ResXReader(fileName);
            this.typeResolver = typeResolver;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceReader"/> class for the specified <see cref="TextReader"/>.
        /// </summary>
        /// <param name="reader">A text stream reader that contains resources.</param>
        /// <param name="typeResolver">An object that resolves type names specified in a resource. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
#if NETFRAMEWORK
        [SuppressMessage("Security", "CA3075:Insecure DTD processing in XML",
            Justification = "False alarm, DTD processing is set to prohibited in ResXReader constructor.")]
#endif
        public ResXResourceReader(TextReader reader, ITypeResolutionService? typeResolver = null)
        {
            if (reader == null!)
                Throw.ArgumentNullException(Argument.reader);

            this.reader = new ResXReader(reader);
            this.typeResolver = typeResolver;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceReader"/> class for the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">An input stream that contains resources.</param>
        /// <param name="typeResolver">An object that resolves type names specified in a resource. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
#if NETFRAMEWORK
        [SuppressMessage("Security", "CA3075:Insecure DTD processing in XML",
            Justification = "False alarm, DTD processing is set to prohibited in ResXReader constructor.")]
#endif
        public ResXResourceReader(Stream stream, ITypeResolutionService? typeResolver = null)
        {
            if (stream == null!)
                Throw.ArgumentNullException(Argument.stream);

            reader = new ResXReader(stream);
            this.typeResolver = typeResolver;
        }

        #endregion

        #region Destructor

        /// <summary>
        /// This member overrides the <see cref="Object.Finalize"/> method.
        /// </summary>
        ~ResXResourceReader() => Dispose(false);

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        /// <summary>
        /// Creates a new <see cref="ResXResourceReader"/> object and initializes it to read a string whose contents are in the form of an XML resource file.
        /// </summary>
        /// <returns>A <see cref="ResXResourceReader"/> object that reads resources from the <paramref name="fileContents"/> string.</returns>
        /// <param name="fileContents">A string containing XML resource-formatted information.</param>
        /// <param name="typeResolver">An object that resolves type names specified in a resource. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        public static ResXResourceReader FromFileContents(string fileContents, ITypeResolutionService? typeResolver = null) => new ResXResourceReader(new StringReader(fileContents), typeResolver);

        #endregion

        #region Private Methods

        private static void AddNode(ICollection<KeyValuePair<string, ResXDataNode>> collection, string key, ResXDataNode value)
        {
            if (collection is StringKeyedDictionary<ResXDataNode> dict)
                dict[key] = value;
            else
                collection.Add(new KeyValuePair<string, ResXDataNode>(key, value));
        }

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Releases all resources used by the <see cref="ResXResourceReader"/>.
        /// </summary>
        /// <remarks>
        /// If the <see cref="ResXResourceReader"/> is initialized in a <see langword="using"/>&#160;statement, it is not needed to call this method explicitly.
        /// </remarks>
        public void Close() => ((IDisposable)this).Dispose();

        /// <summary>
        /// Returns an <see cref="IDictionaryEnumerator"/> instance for the current <see cref="ResXResourceReader"/> object that enumerates the resources
        /// in the source XML resource file or stream.
        /// </summary>
        /// <returns>An <see cref="IDictionaryEnumerator" /> for that can be used to iterate through the resources from the current XML resource file or stream.</returns>
        /// <remarks>
        /// <para>In <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> if <see cref="UseResXDataNodes"/> property is <see langword="true"/>,
        /// the resource and metadata entries are mixed in the returned enumerator, while when it is <see langword="false"/>, then only the resources are returned. In this <see cref="ResXResourceReader"/> implementation the <see cref="GetEnumerator">GetEnumerator</see> method always
        /// returns only the resources and <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> returns the metadata regardless of the value of the <see cref="UseResXDataNodes"/> or <see cref="SafeMode"/> properties.</para>
        /// <para>If the <see cref="SafeMode"/> property is <see langword="true"/>, the <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> property of the returned enumerator is a <see cref="ResXDataNode"/>
        /// instance rather than the resource value. This makes possible to check the raw .resx content before deserialization if the .resx file is from an untrusted source. See also the example at <see cref="ResXDataNode"/>.</para>
        /// <para>If <see cref="AllowDuplicatedKeys"/> property is <see langword="true"/>, then this method returns a lazy enumerator for the first time meaning the .resx file is parsed only during the enumeration. When any of the enumerators are obtained
        /// for the second time, a cached enumerator is returned with the whole parsed .resx content. If duplicates are disabled, the lastly defined value will be returned of a redefined name.</para>
        /// <para>See also the <strong>Remarks</strong> section of the <see cref="ResXResourceReader"/> class for examples.</para>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        /// <seealso cref="ResXResourceReader"/>
        /// <seealso cref="ResXDataNode"/>
        /// <seealso cref="SafeMode"/>
        /// <seealso cref="GetMetadataEnumerator"/>
        /// <seealso cref="GetAliasEnumerator"/>
        public IDictionaryEnumerator GetEnumerator() => GetEnumeratorInternal(ResXEnumeratorModes.Resources);

        /// <summary>
        /// Returns an <see cref="IDictionaryEnumerator"/> instance for the current <see cref="ResXResourceReader"/> object that enumerates the design-time properties (<c>&lt;metadata&gt;</c> elements)
        /// in the source XML resource file or stream.
        /// </summary>
        /// <returns>An <see cref="IDictionaryEnumerator" /> for that can be used to iterate through the design-time properties (<c>&lt;metadata&gt;</c>; elements) from the current XML resource file or stream.</returns>
        /// <remarks>
        /// <para>If the <see cref="SafeMode"/> property is <see langword="true"/>, the <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> property of the returned enumerator is a <see cref="ResXDataNode"/>
        /// instance rather than the resource value. This makes possible to check the raw .resx content before deserialization if the .resx file is from an untrusted source. See also the example at <see cref="ResXDataNode"/>.</para>
        /// <para>If <see cref="AllowDuplicatedKeys"/> property is <see langword="true"/>, then this method returns a lazy enumerator for the first time meaning the .resx file is parsed only during the enumeration. When any of the enumerators are obtained
        /// for the second time, a cached enumerator is returned with the whole parsed .resx content. If duplicates are disabled, the lastly defined value will be returned of a redefined name.</para>
        /// <para>See also the <strong>Remarks</strong> section of the <see cref="ResXResourceReader"/> class for examples.</para>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        /// <seealso cref="ResXResourceReader"/>
        /// <seealso cref="ResXDataNode"/>
        /// <seealso cref="SafeMode"/>
        /// <seealso cref="GetEnumerator"/>
        /// <seealso cref="GetAliasEnumerator"/>
        public IDictionaryEnumerator GetMetadataEnumerator() => GetEnumeratorInternal(ResXEnumeratorModes.Metadata);

        /// <summary>
        /// Provides an <see cref="IDictionaryEnumerator"/> instance that can retrieve the aliases from the current XML resource file or stream.
        /// </summary>
        /// <returns>An <see cref="IDictionaryEnumerator" /> for that can be used to iterate through the aliases from the current XML resource file or stream.</returns>
        /// <remarks>
        /// <para>The <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> property of the returned enumerator is always a <see cref="string"/> regardless of the value of the <see cref="SafeMode"/> property.</para>
        /// <para>The <see cref="IDictionaryEnumerator.Key">IDictionaryEnumerator.Key</see> property of the returned enumerator is the alias name, whereas <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> is the corresponding assembly name.</para>
        /// <para>If <see cref="AllowDuplicatedKeys"/> property is <see langword="true"/>, then this method returns a lazy enumerator for the first time meaning the .resx file is parsed only during the enumeration. When any of the enumerators are obtained
        /// for the second time, a cached enumerator is returned with the whole parsed .resx content. If duplicates are disabled, the lastly defined value will be returned of a redefined alias.</para>
        /// <para>See also the <strong>Remarks</strong> section of the <see cref="ResXResourceReader"/> class for examples.</para>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        /// <seealso cref="ResXResourceReader"/>
        /// <seealso cref="SafeMode"/>
        /// <seealso cref="GetEnumerator"/>
        /// <seealso cref="GetMetadataEnumerator"/>
        public IDictionaryEnumerator GetAliasEnumerator() => GetEnumeratorInternal(ResXEnumeratorModes.Aliases);

        #endregion

        #region Internal Methods

        /// <summary>
        /// Special initialization for ResXResourceSet. No lock is needed because called from ctor. Reads raw xml content only.
        /// </summary>
        internal void ReadAllInternal(StringKeyedDictionary<ResXDataNode> linkedResources, StringKeyedDictionary<ResXDataNode> linkedMetadata, StringKeyedDictionary<string> linkedAliases)
        {
            Debug.Assert(state == States.Created);
            resources = linkedResources;
            metadata = linkedMetadata;
            aliases = activeAliases = linkedAliases;
            ReadAll();
        }

        #endregion

        #region Private Methods

        private void Dispose(bool disposing)
        {
            if (state == States.Disposed)
                return;

            if (disposing)
#if NET35 || NET40
                reader?.Close();
#else
                reader?.Dispose();
#endif

            reader = null;
            aliases = null;
            resources = null;
            metadata = null;
            enumerator = null;
            state = States.Disposed;
        }

        private IDictionaryEnumerator GetEnumeratorInternal(ResXEnumeratorModes mode)
        {
            lock (syncRoot)
            {
                switch (state)
                {
                    // enumerating for the first time
                    case States.Created:

                        // returning a lazy enumerator for the first time if duplication is enabled
                        if (allowDuplicatedKeys)
                        {
                            resources = new List<KeyValuePair<string, ResXDataNode>>();
                            metadata = new List<KeyValuePair<string, ResXDataNode>>();
                            aliases = new List<KeyValuePair<string, string>>();
                            activeAliases = new StringKeyedDictionary<string>();
                            state = States.Reading;
                            enumerator = new LazyEnumerator(this, mode);
                            return enumerator;
                        }

                        // no duplication (non-lazy mode): allocating dictionaries and caching for the first time, too.
                        resources = new StringKeyedDictionary<ResXDataNode>();
                        metadata = new StringKeyedDictionary<ResXDataNode>();
                        aliases = activeAliases = new StringKeyedDictionary<string>();
                        ReadAll();
                        state = States.Read;
                        return new ResXResourceEnumerator(this, mode);

                    // getting an enumerator while the first lazy enumeration has not finished: buffering the items
                    // for the first enumeration and returning a cached enumerator
                    case States.Reading:
                        enumerator!.ReadToEnd();
                        state = States.Read;
                        enumerator = null;
                        return new ResXResourceEnumerator(this, mode);

                    // .resx contents are already cached
                    case States.Read:
                        return new ResXResourceEnumerator(this, mode);

                    default:
                        Throw.ObjectDisposedException();
                        return default;
                }
            }
        }

        private int GetLineNumber() => (reader as IXmlLineInfo)?.LineNumber ?? 0;
        private int GetLinePosition() => (reader as IXmlLineInfo)?.LinePosition ?? 0;

        /// <summary>
        /// Parses the resource header node. Header can be completely missing; however, it is checked when required and exists.
        /// </summary>
        private void ParseResHeaderNode()
        {
            object? name = reader![ResXCommon.NameStr];
            if (name == null)
                return;

            reader.ReadStartElement();

#pragma warning disable 252, 253 // reference equality is intended because names are added to NameTable
            if (name == ResXCommon.VersionStr)
            {
                // no check, just skipping (the system version sets a version field, which is never checked)
                if (reader.NodeType == XmlNodeType.Element)
                    reader.ReadElementString();
            }
            else if (name == ResXCommon.ResMimeTypeStr)
            {
                string resHeaderMimeType = reader.NodeType == XmlNodeType.Element ? reader.ReadElementString() : reader.Value.Trim();
                if (resHeaderMimeType != ResXCommon.ResMimeType)
                    Throw.NotSupportedException(Res.ResourcesHeaderMimeTypeNotSupported(resHeaderMimeType, GetLineNumber(), GetLinePosition()));
            }
            else if (name == ResXCommon.ReaderStr || name == ResXCommon.WriterStr)
            {
                string typeName = reader.NodeType == XmlNodeType.Element
                    ? reader.ReadElementString()
                    : reader.Value.Trim();

                if (typeName.IndexOf(',') != -1)
                    typeName = typeName.Split(new char[] { ',' })[0].Trim();

                if (name == ResXCommon.ReaderStr)
                {
                    if (!ResXCommon.ResXResourceReaderNameWinForms.StartsWith(typeName, StringComparison.Ordinal) && typeName != typeof(ResXResourceReader).FullName)
                        Throw.NotSupportedException(Res.ResourcesResXReaderNotSupported(typeName, GetLineNumber(), GetLinePosition()));
                }
                else
                {
                    if (!ResXCommon.ResXResourceWriterNameWinForms.StartsWith(typeName, StringComparison.Ordinal) && typeName != typeof(ResXResourceReader).FullName)
                        Throw.NotSupportedException(Res.ResourcesResXWriterNotSupported(typeName, GetLineNumber(), GetLinePosition()));
                }
            }
#pragma warning restore 252, 253
        }

        private void ParseAssemblyNode(out string key, out string value)
        {
            key = reader![ResXCommon.AliasStr]!;
            if (key == null)
            {
                int line = GetLineNumber();
                int col = GetLinePosition();
                throw ResXCommon.CreateXmlException(Res.ResourcesMissingAttribute(ResXCommon.AliasStr, line, col), line, col);
            }

            value = reader[ResXCommon.NameStr]!;
            if (value == null)
            {
                int line = GetLineNumber();
                int col = GetLinePosition();
                throw ResXCommon.CreateXmlException(Res.ResourcesMissingAttribute(ResXCommon.NameStr, line, col), line, col);
            }
        }

        private string? GetAliasValueFromTypeName(string? typeName)
        {
            // value is string
            if (String.IsNullOrEmpty(typeName))
                return null;

            // full name only
            int posComma = typeName!.IndexOf(',');
            if (posComma < 0)
                return null;

            // there is an assembly or alias name after the full name
            string alias = typeName.Substring(posComma + 1).Trim();

            // no, sorry
            if (alias.Length == 0)
                return null;

            // alias value found
            if (activeAliases!.TryGetValue(alias, out string? asmName))
                return asmName;

            // type name is with assembly name
            return null;
        }

        /// <summary>
        /// Reads next element (depending on mode) from the XML. Skipped elements (on mode mismatch) are stored into the appropriate caches.
        /// Callers must be in a lock.
        /// </summary>
        private bool ReadNext(ResXEnumeratorModes mode, [MaybeNullWhen(false)]out string key, [MaybeNullWhen(false)]out ResXDataNode value)
        {
            key = null;
            value = null;
            switch (state)
            {
                case States.Created:
                    return Throw.InternalError<bool>(Res.InternalError($"State should not be in {States.Created} in {nameof(ReadNext)}"));
                case States.Reading:
                    if (!Advance(mode, out key, out value))
                    {
                        state = States.Read;
                        enumerator = null;
                        return false;
                    }

                    return true;
                case States.Read:
                    return false;
                default:
                    Throw.ObjectDisposedException();
                    return default;
            }
        }

        /// <summary>
        /// Reads the rest of the elements and returns the passed read elements.
        /// Must not be implemented as an iterator because it must read all of the remaining elements immediately.
        /// </summary>
        private IEnumerable<KeyValuePair<string, ResXDataNode>> ReadToEnd(ResXEnumeratorModes mode)
        {
            ICollection<KeyValuePair<string, ResXDataNode>> result = allowDuplicatedKeys
                ? new List<KeyValuePair<string, ResXDataNode>>()
                : new StringKeyedDictionary<ResXDataNode>();
            while (ReadNext(mode, out string? key, out ResXDataNode? value))
                AddNode(result, key, value);

            return result;
        }

        /// <summary>
        /// Reads the whole .resx file into the internal caches. This is just simple parsing, no deserialization occurs.
        /// </summary>
        private void ReadAll()
        {
            while (Advance(null, out var _, out var _))
            {
            }
        }

        /// <summary>
        /// Advances in the XML file based on the specified mode or the whole file if mode is null.
        /// Calls must be in a lock or from a ctor.
        /// </summary>
        private bool Advance(ResXEnumeratorModes? mode, [MaybeNullWhen(false)]out string key, [MaybeNullWhen(false)]out ResXDataNode value)
        {
            if (reader == null)
            {
                key = null;
                value = null;
                return false;
            }

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                    continue;

#pragma warning disable 252, 253 // reference equality is intended because names are added to NameTable
                object name = reader.LocalName;
                if (name == ResXCommon.DataStr)
                {
                    ParseDataNode(out key, out value);
                    AddNode(resources!, key, value);
                    if (mode == ResXEnumeratorModes.Resources)
                        return true;
                }
                else if (name == ResXCommon.MetadataStr)
                {
                    ParseDataNode(out key, out value);
                    AddNode(metadata!, key, value);
                    if (mode == ResXEnumeratorModes.Metadata)
                        return true;
                }
                else if (name == ResXCommon.AssemblyStr)
                {
                    ParseAssemblyNode(out key, out string assemblyName);
                    AddAlias(key, assemblyName);
                    if (mode == ResXEnumeratorModes.Aliases)
                    {
                        value = new ResXDataNode(key, assemblyName);
                        return true;
                    }
                }
                else if (name == ResXCommon.ResHeaderStr && checkHeader)
                    ParseResHeaderNode();
#pragma warning restore 252, 253
            }

            key = null;
            value = null;
            reader.Close();
            reader = null;

            return false;
        }

        private void AddAlias(string key, string assemblyName)
        {
            if (aliases is StringKeyedDictionary<string> dict)
            {
                dict[key] = assemblyName;
                Debug.Assert(ReferenceEquals(aliases, activeAliases), "activeAliases should be the same as aliases");
                return;
            }

            activeAliases![key] = assemblyName;
            aliases!.Add(new KeyValuePair<string, string>(key, assemblyName));
        }

        /// <summary>
        /// Parses a data or metadata node.
        /// Must be called in a lock or from a ctor.
        /// </summary>
        private void ParseDataNode(out string key, out ResXDataNode value)
        {
            key = reader![ResXCommon.NameStr]!;
            int line = GetLineNumber();
            int col = GetLinePosition();
            if (key == null)
                throw ResXCommon.CreateXmlException(Res.ResourcesNoResXName(line, col), line, col);

            DataNodeInfo nodeInfo = new DataNodeInfo
            {
                Name = key,
                TypeName = reader[ResXCommon.TypeStr],
                MimeType = reader[ResXCommon.MimeTypeStr],
                Line = line,
                Column = col
            };

            nodeInfo.AssemblyAliasValue = GetAliasValueFromTypeName(nodeInfo.TypeName);
            nodeInfo.DetectCompatibleFormat();

            bool finishedReadingDataNode = false;
            while (!finishedReadingDataNode && reader.Read())
            {
#pragma warning disable 252, 253 // reference equality is intended because names are added to NameTable
                object name = reader.LocalName;
                if (reader.NodeType == XmlNodeType.EndElement && (name == ResXCommon.DataStr || name == ResXCommon.MetadataStr))
                {
                    // we just found </data> or </metadata>
                    finishedReadingDataNode = true;
                }
                else
                {
                    // could be a <value> or a <comment>
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        // Compatibility: <value/> will be an empty string for strings and null for other types
                        if (name == ResXCommon.ValueStr)
                            nodeInfo.ValueData = reader.IsEmptyElement && nodeInfo.TypeName != null ? null : reader.ReadString();
                        else if (name == ResXCommon.CommentStr)
                            nodeInfo.Comment = reader.ReadString();
                        else
                        {
                            line = GetLineNumber();
                            col = GetLinePosition();
                            throw ResXCommon.CreateXmlException(Res.ResourcesUnexpectedElementAt(name.ToString()!, line, col), line, col);
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Text)
                    {
                        // or there is no <value> tag, just the inside of <data> as text
                        nodeInfo.ValueData = reader.Value.Trim();
                    }
                }
#pragma warning restore 252, 253
            }

            value = new ResXDataNode(nodeInfo, basePath);
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorInternal(ResXEnumeratorModes.Resources);

        #endregion

        #endregion

        #endregion
    }
}
