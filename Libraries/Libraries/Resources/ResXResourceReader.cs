using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Xml;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Serialization;

namespace KGySoft.Libraries.Resources
{
#pragma warning disable 618
    /// <summary>
    /// Enumerates XML resource (.resx) files and streams, and reads the sequential resource name and value pairs.
    /// <br/>See the <strong>Remarks</strong> section to see the differences compared to <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a> class.
    /// </summary>
    /// <remarks>
    /// <note>This class is similar to <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a>
    /// in <c>System.Windows.Forms.dll</c>. See the <a href="#comparison">Comparison with System.Resources.ResXResourceReader</a> section to see the differences.</note>
    /// <para>You can use the <see cref="ResXResourceReader"/> class to enumerate resources in .resx files by traversing the dictionary enumerator (<see cref="IDictionaryEnumerator"/>) that is returned by the
    /// <see cref="GetEnumerator">GetEnumerator</see> method. You call the methods provided by <see cref="IDictionaryEnumerator"/> to advance to the next resource and to read the name and value of each resource in the .resx file.
    /// <note>The <see cref="ResXResourceReader"/> class provides more enumerators.
    /// <list type="bullet">
    /// <item>The <see cref="GetEnumerator">GetEnumerator</see> method returns an <see cref="IDictionaryEnumerator"/> object, which enumerates the resources.
    /// The <see cref="IDictionaryEnumerator.Key">IDictionaryEnumerator.Key</see> property returns the resource names, while <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see>
    /// returns either <see cref="ResXDataNode"/> instances, if <see cref="SafeMode"/> property is <c>true</c>; or returns deserialized <see cref="object"/> instances if <see cref="SafeMode"/> property is <c>false</c>.</item>
    /// <item>The <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> method returns an <see cref="IDictionaryEnumerator"/> object, which enumerates the metadata entries.
    /// The <see cref="IDictionaryEnumerator.Key">IDictionaryEnumerator.Key</see> property returns the metadata names, while <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see>
    /// returns either <see cref="ResXDataNode"/> instances, if <see cref="SafeMode"/> property is <c>true</c>; or returns deserialized <see cref="object"/> instances if <see cref="SafeMode"/> property is <c>false</c>.</item>
    /// <item>The <see cref="GetAliasEnumerator">GetAliasEnumerator</see> method returns an <see cref="IDictionaryEnumerator"/> object, which enumerates the aliases in the .resx file.
    /// The <see cref="IDictionaryEnumerator.Key">IDictionaryEnumerator.Key</see> property returns the alias names, while <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see>
    /// returns the corresponding assembly names for the alias names.</item>
    /// <item>As an explicit interface implementation, <see cref="ResXResourceReader"/> implements <see cref="IEnumerable.GetEnumerator">IEnumerable.GetEnumerator</see> method, which returns the same enumerator as
    /// <see cref="GetEnumerator">GetEnumerator</see> as an <see cref="IEnumerator"/> instance. The <see cref="IEnumerator.Current">IEnumerator.Current</see> property will return <see cref="DictionaryEntry"/> instances.</item>
    /// </list>
    /// </note>
    /// </para>
    /// <para>If you want to retrieve named resources from a .resx file rather than enumerating its resources, you can instantiate a <see cref="ResXResourceSet"/> object and call its
    /// <see cref="ResXResourceSet.GetString(string)">GetString</see>/<see cref="ResXResourceSet.GetObject(string)">GetObject</see>, <see cref="ResXResourceSet.GetMetaString">GetMetaString</see>/<see cref="ResXResourceSet.GetMetaObject">GetMetaObject</see> and <see cref="ResXResourceSet.GetAliasValue">GetAliasValue</see> and  methods.
    /// <see cref="ResXResourceSet"/> supports <see cref="ResXResourceSet.SafeMode"/>, too.</para>
    /// <para>If the <see cref="SafeMode"/> property is <c>true</c>, the value of the <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> property is a <see cref="ResXDataNode"/>
    /// instance rather than the resource value. This makes possible to check the raw .resx content before deserialization if the .resx file is from an untrusted source. See also the example at <see cref="ResXDataNode"/>.</para>
    /// <example>
    /// The following example shows how to enumerate the resources, metadata and aliases of a .resx file and what is the difference between safe and non-safe mode.
    /// Please note that <see cref="SafeMode"/> property can be switched on and off during the enumeration, too. Please also note that the values returned by the <see cref="GetAliasEnumerator">GetAliasEnumerator</see> are always
    /// strings, regardless of the value of <see cref="SafeMode"/> property. See also the example of the <see cref="ResXDataNode"/> class to see how to examine the properties of the <see cref="ResXDataNode"/> instances
    /// in safe mode.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Collections;
    /// using System.IO;
    /// using KGySoft.Libraries.Resources;
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
    ///
    ///        Console.WriteLine("____Resources in .resx:____");
    ///        Dump(reader, reader.GetEnumerator);
    ///
    ///        Console.WriteLine("____Metadata in .resx:____");
    ///        Dump(reader, reader.GetMetadataEnumerator);
    ///
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
    /// // Value in SafeMode:     Test string (KGySoft.Libraries.Resources.ResXDataNode)
    /// // Value in non-SafeMode: Test string (System.String)
    /// 
    /// // Name: int
    /// // Value in SafeMode:     42 (KGySoft.Libraries.Resources.ResXDataNode)
    /// // Value in non-SafeMode: 42 (System.Int32)
    /// 
    /// // Name: color
    /// // Value in SafeMode:     Red (KGySoft.Libraries.Resources.ResXDataNode)
    /// // Value in non-SafeMode: Color[Red] (System.Drawing.Color)
    /// 
    /// // Name: bytes
    /// // Value in SafeMode:     VGVzdCBieXRlcw== (KGySoft.Libraries.Resources.ResXDataNode)
    /// // Value in non-SafeMode: System.Byte[] (System.Byte[])
    /// 
    /// // Name: dangerous
    /// // Value in SafeMode:     YmluYXJ5 (KGySoft.Libraries.Resources.ResXDataNode)
    /// // Getting the deserialized value thrown an exception: End of Stream encountered before parsing was completed.
    /// 
    /// // ____Metadata in .resx:____
    /// // Name: meta string
    /// // Value in SafeMode:     Meta String (KGySoft.Libraries.Resources.ResXDataNode)
    /// // Value in non-SafeMode: Meta String (System.String)
    /// 
    /// // ____Aliases in .resx:____
    /// // Name: CustomAlias
    /// // Value in SafeMode:     System.Drawing, Version= 4.0.0.0, Culture= neutral, PublicKeyToken= b03f5f7f11d50a3a (System.String)
    /// // Value in non-SafeMode: System.Drawing, Version= 4.0.0.0, Culture= neutral, PublicKeyToken= b03f5f7f11d50a3a (System.String)</code>
    /// </example>
    /// <para>
    /// By default, <see cref="ResXResourceReader"/> allows duplicated keys with different values (see <see cref="AllowDuplicatedKeys"/> property). Though such a .resx file is not strictly valid, its
    /// complete content can be retrieved. When <see cref="AllowDuplicatedKeys"/> is <c>true</c>, <see cref="GetEnumerator">GetEnumerator</see>, <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> and
    /// <see cref="GetAliasEnumerator">GetAliasEnumerator</see> return a lazy enumerator for the first time meaning the .resx file is parsed only during the enumeration. When any of the enumerators are obtained
    /// for the second time, a cached enumerator is returned with the whole parsed .resx content. If duplicates are disabled, the lastly defined values will be returned of a redefined name. This behavior is
    /// similar to the <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a> class, which does not allow duplicates.
    /// </para>
    /// <example>
    /// The following example demonstrates the difference of lazy (allowing duplicates) and greedy (disabling duplicates) reading.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Collections;
    /// using System.IO;
    /// using KGySoft.Libraries.Resources;
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
    /// <h1 class="heading">When to use <c>ResXResourceReader</c> instead of <see cref="ResXResourceSet"/> and <see cref="ResXResourceManager"/></h1>
    /// <para><see cref="ResXResourceReader"/> is the most low-level option to read the content of a .resx file. <see cref="ResXResourceSet"/> and <see cref="ResXResourceManager"/>
    /// classes also instantiate it internally but there are some cases when it is needed to use a <see cref="ResXResourceReader"/> explicitly:
    /// <list type="bullet">
    /// <item>The .resx file may contain redefined keys and duplications needed to be retrieved (see <see cref="AllowDuplicatedKeys"/> property). <see cref="ResXResourceSet"/> and <see cref="ResXResourceManager"/> classes do not allow duplicates.</item>
    /// <item>Custom type resolution is needed. You can pass a <see cref="ITypeResolutionService"/> instance to one of the constructors to handle the type resolutions.</item>
    /// </list>
    /// </para>
    /// <h1 class="heading">Comparison with System.Resources.ResXResourceReader<a name="comparison">&#160;</a></h1>
    /// <para><see cref="ResXResourceReader"/> can read .resx files produced both by <see cref="ResXResourceWriter"/> and <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcewriter.aspx" target="_blank">System.Resources.ResXResourceWriter</a>.</para>
    /// <para><strong>Incompatibility</strong> with <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a>:
    /// <list type="bullet">
    /// <item>Constructors do not have overloads with <see cref="AssemblyName">AssemblyName[]</see> parameters. The <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a>
    /// uses them to load the assemblies in advance occasionally by calling the obsolete <see cref="Assembly.LoadWithPartialName(string)">Assembly.LoadPartial</see> method. However, this <see cref="ResXResourceReader"/>
    /// implementation can handle finding and loading assemblies automatically. If an assembly needs to be loaded from a partial name, the <see cref="Reflector.ResolveAssembly">Reflector.ResolveAssembly</see> method is called, which does not use
    /// obsolete techniques. If you need a completely custom type resolution the constructor overloads with <see cref="ITypeResolutionService"/> parameters still can be used.</item>
    /// <item>This <see cref="ResXResourceReader"/> is a sealed class.</item>
    /// <item>After disposing the <see cref="ResXResourceReader"/> instance or calling the <see cref="Close">Close</see> method the enumerators cannot be obtained: an <see cref="ObjectDisposedException"/> will be thrown
    /// on calling <see cref="GetEnumerator">GetEnumerator</see>, <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> and <see cref="GetAliasEnumerator">GetAliasEnumerator</see> methods.</item>
    /// <item>After disposing the <see cref="ResXResourceReader"/> instance or calling the <see cref="Close">Close</see> method every source stream will be closed (if any).</item>
    /// <item>Unlike <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a>, this implementation returns every resources and metadata of the
    /// same name by default. This behavior can be adjusted by <see cref="AllowDuplicatedKeys"/> property.</item>
    /// <item><a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a> often throws <see cref="ArgumentException"/> on getting the enumerator
    /// or on retrieving the value of a <see cref="ResXDataNode"/> instance, which contains invalid data. In contrast, this implementation may throw <see cref="XmlException"/>, <see cref="TypeLoadException"/> or <see cref="NotSupportedException"/> instead.</item>
    /// <item>Though the <see cref="UseResXDataNodes"/> property is still supported, it is obsolete in favor of <see cref="SafeMode"/> property.</item>
    /// <item>In the <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a> version if <see cref="UseResXDataNodes"/> property is <c>true</c>,
    /// the resource and metadata entries are mixed in the returned enumerator, while when it is <c>false</c>, then only the resources are returned. In this implementation the <see cref="GetEnumerator">GetEnumerator</see> always
    /// returns only the resources and <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> returns the metadata regardless of the value of the <see cref="UseResXDataNodes"/> or <see cref="SafeMode"/> value.</item>
    /// </list>
    /// </para>
    /// <para><strong>New features and improvements</strong> compared to <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a>:
    /// <list type="bullet">
    /// <item><term>Lazy processing</term>
    /// <description>If <see cref="AllowDuplicatedKeys"/> is <c>true</c>, the .resx file is processed on demand, during the actual enumeration. The .resx file is processed immediately if
    /// <see cref="AllowDuplicatedKeys"/> is <c>false</c>. If <see cref="AllowDuplicatedKeys"/> is <c>true</c> and any enumerator is obtained after getting one, the rest of the .resx file is immediately processed.</description></item>
    /// <item><term>Handling duplicates</term>
    /// <description>If <see cref="AllowDuplicatedKeys"/> is <c>true</c>, every occurrence of a duplicated name is returned by the enumerators. Otherwise, only the last occurrence of
    /// a name is returned.</description></item>
    /// <item><term>Headers</term>
    /// <description>The .resx header is allowed to be completely missing; however, it is checked when exists and <see cref="CheckHeader"/> property is <c>true</c>. If header tags contain invalid values a <see cref="NotSupportedException"/> may be thrown during the enumeration.
    /// You can configure the <see cref="ResXResourceWriter"/> class to omit the header by the <see cref="ResXResourceWriter.OmitHeader">ResXResourceWriter.OmitHeader</see> property.</description></item>
    /// <item><term>Using <see cref="ResXDataNode"/> instances</term>
    /// <description>The <see cref="SafeMode"/> (<see cref="UseResXDataNodes"/>) property can be toggled also after getting an enumerator or even during the enumeration.</description></item>
    /// <item><term>Clear purpose of the enumerators</term>
    /// <description>The <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.getenumerator.aspx" target="_blank">System.Resources.ResXResourceReader.GetEnumerator</a> either returns resources only or returns both resources and metadata mixed together
    /// depending on the value of the <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.useresxdatanodes.aspx" target="_blank">System.Resources.ResXResourceReader.UseResXDataNodes</a> property.
    /// This <see cref="ResXResourceReader"/> implementation has separated <see cref="GetEnumerator">GetEnumerator</see>, <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> and <see cref="GetAliasEnumerator">GetAliasEnumerator</see>
    /// methods, which return always the resources, metadata and aliases, respectively.</description></item>
    /// <item><term>Security</term>
    /// <description>If <see cref="SafeMode"/> is <c>true</c>, no deserialization, assembly loading and type resolving occurs until a deserialization is explicitly requested by calling the <see cref="ResXDataNode.GetValue">ResXDataNode.GetValue</see> method
    /// on the <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> instances returned by the <see cref="GetEnumerator">GetEnumerator</see> and <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> methods.</description></item>
    /// <item><term>Base path</term>
    /// <description>The <see cref="BasePath"/> property, which is used for resolving file references can be set during the enumeration, too.</description></item>
    /// <item><term>New MIME type</term>
    /// <description>A new MIME type <c>text/kgysoft.net/object.binary.base64</c> is supported, indicating that an object is serialized by <see cref="BinarySerializationFormatter"/> instead of <see cref="BinaryFormatter"/>.
    /// The <see cref="ResXResourceWriter"/> can produce such .resx content if <see cref="ResXResourceWriter.CompatibleFormat">ResXResourceWriter.CompatibleFormat</see> is <c>false</c>.</description></item>
    /// <item><term>Soap formatter support</term>
    /// <description>The Soap formatter support is provided without referencing the <c>System.Runtime.Serialization.Formatters.Soap.dll</c> assembly. If the assembly cannot be loaded from the GAC (platform dependent),
    /// then a <see cref="NotSupportedException"/> will be thrown.</description></item>
    /// <item><term>Type resolving</term>
    /// <description>If a <see cref="ITypeResolutionService"/> instance is passed to one of the constructors, it is used also for the type references in <see cref="ResXFileRef"/> instances.</description></item>
    /// </list></para>
    /// </remarks>
    /// <seealso cref="ResXDataNode"/>
    /// <seealso cref="ResXResourceWriter"/>
    /// <seealso cref="ResXResourceSet"/>
    /// <seealso cref="ResXResourceManager"/>
    /// <seealso cref="HybridResourceManager"/>
    /// <seealso cref="DynamicResourceManager"/>
#pragma warning restore 618
    public sealed class ResXResourceReader : IResourceReader, IResXResourceContainer
    {
        /// <summary>
        /// An enumerator that reads the underlying .resx on-demand. Returns the duplicated elements, too.
        /// </summary>
        private sealed class LazyEnumerator : IDictionaryEnumerator
        {
            private enum EnumeratorStates
            {
                BeforeFirst,
                Enumerating,
                AfterLast
            }

            private readonly ResXResourceReader owner;
            private readonly ResXEnumeratorModes mode;
            private EnumeratorStates state;
            private string key;
            private ResXDataNode value;

            /// <summary>
            /// Represents buffered items, which should be returned before reading the next items from the underlying XML.
            /// Reset and ReadToEnd may produce buffered items.
            /// </summary>
            private IEnumerator<KeyValuePair<string, ResXDataNode>> bufferedEnumerator;

            internal LazyEnumerator(ResXResourceReader owner, ResXEnumeratorModes mode)
            {
                this.owner = owner;
                this.mode = mode;
                state = EnumeratorStates.BeforeFirst;
            }

            public DictionaryEntry Entry
            {
                get
                {
                    if (state != EnumeratorStates.Enumerating)
                        throw new InvalidOperationException(Res.Get(Res.EnumerationNotStartedOrFinished));

                    if (mode == ResXEnumeratorModes.Aliases)
                        return new DictionaryEntry(key, value.ValueInternal);

                    return owner.safeMode
                        ? new DictionaryEntry(key, value)
                        : new DictionaryEntry(key, value.GetValue(owner.typeResolver, owner.basePath, false));
                }
            }

            public object Key
            {
                get
                {
                    if (state != EnumeratorStates.Enumerating)
                        throw new InvalidOperationException(Res.Get(Res.EnumerationNotStartedOrFinished));

                    return key;
                }
            }

            public object Value
            {
                get { return Entry.Value; }
            }

            public object Current
            {
                get { return Entry; }
            }

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
                    switch (mode)
                    {
                        case ResXEnumeratorModes.Resources:
                            if (owner.resources != null)
                                bufferedEnumerator = owner.resources.GetEnumerator();
                            break;
                        case ResXEnumeratorModes.Metadata:
                            if (owner.metadata != null)
                                bufferedEnumerator = owner.metadata.GetEnumerator();
                            break;
                        case ResXEnumeratorModes.Aliases:
                            if (owner.aliases != null)
                                bufferedEnumerator = owner.aliases.Select(ResXResourceEnumerator.SelectAlias).GetEnumerator();
                            break;
                    }
                }
            }

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
                        {
                            result.Add(bufferedEnumerator.Current);
                        }

                        IEnumerable<KeyValuePair<string, ResXDataNode>> rest = owner.ReadToEnd(mode);
                        if (result.Count > 0)
                        {
                            result.AddRange(rest);
                            bufferedEnumerator = result.GetEnumerator();
                        }
                        else
                        {
                            bufferedEnumerator = rest.GetEnumerator();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Required because a reader returned by XmlReader.Create would normalize the \r characters
        /// </summary>
        private sealed class ResXReader : XmlTextReader
        {
            internal ResXReader(Stream stream): base(stream, InitNameTable())
            {
                WhitespaceHandling = WhitespaceHandling.Significant;
                //var settings = new XmlReaderSettings
                //{
                //    IgnoreWhitespace = true,
                //    IgnoreComments = true,
                //    CloseInput = true,
                //    CheckCharacters = false,
                //};
            }

            internal ResXReader(string fileName): this(File.OpenRead(fileName))
            {
            }

            internal ResXReader(TextReader reader)
                : base(reader, InitNameTable())
            {
                WhitespaceHandling = WhitespaceHandling.Significant;
            }

            private static XmlNameTable InitNameTable()
            {
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
                // delete: mime types are not compared by reference
                //nameTable.Add(ResXCommon.BinSerializedObjectMimeType);
                //nameTable.Add(ResXCommon.SoapSerializedObjectMimeType);
                nameTable.Add(ResXCommon.AssemblyStr);
                nameTable.Add(ResXCommon.AliasStr);
                return nameTable;
            }
        }

        private enum States { Created, Reading, Read, Disposed };
        //static readonly char[] SpecialChars = new char[]{' ', '\r', '\n'};

        //IFormatter binaryFormatter = null;
        //private string fileName;        

        /// <summary>
        /// The internally created reader. Will be closed automatically when stream ends or on Dispose
        /// </summary>
        private XmlReader reader;

        //private Stream stream;
        //string fileContents = null;
        //AssemblyName[] assemblyNames;
        private string basePath;
        //bool isReaderDirty;
        private States state = States.Created;

        ITypeResolutionService typeResolver;

        /// <summary>
        /// The currently active aliases. Same as <see cref="aliases"/> if duplication is disabled.
        /// </summary>
        private Dictionary<string, string> activeAliases;

        private ICollection<KeyValuePair<string, string>> aliases;
        private ICollection<KeyValuePair<string, ResXDataNode>> resources;
        private ICollection<KeyValuePair<string, ResXDataNode>> metadata;
        private LazyEnumerator enumerator;

        //ReaderAliasResolver aliasResolver =null;
        //ListDictionary resData = null;
        //ListDictionary resMetadata = null;
        //string resHeaderVersion = null;

        //private string resHeaderMimeType;
        //private string resHeaderReaderType;
        //private string resHeaderWriterType;
        private bool safeMode;
        private readonly object syncRoot = new object();
        private bool checkHeader;
        private bool allowDuplicatedKeys = true;

        //private ResXResourceReader(ITypeResolutionService typeResolver) {
        //    this.typeResolver = typeResolver;
        //    this.aliasResolver = new ReaderAliasResolver();
        //}

        //private ResXResourceReader(AssemblyName[] assemblyNames) {
        //    this.assemblyNames = assemblyNames;
        //    this.aliasResolver = new ReaderAliasResolver();
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceReader"/> class for the specified resource file.
        /// </summary>
        /// <param name="fileName">The name of an XML resource file that contains resources. </param>
        /// <param name="typeResolver">An object that resolves type names specified in a resource.</param>
        public ResXResourceReader(string fileName, ITypeResolutionService typeResolver = null)
            //: this(fileName, typeResolver, (ReaderAliasResolver)null)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName", Res.Get(Res.ArgumentNull));

            //var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            //reader = XmlReader.Create(fileName, GetReaderSettings());
            reader = new ResXReader(fileName);
            this.typeResolver = typeResolver;
        }

        //private ResXResourceReader(string fileName, ITypeResolutionService typeResolver, ReaderAliasResolver aliasResolver) {
        //    this.fileName = fileName;
        //    this.typeResolver = typeResolver;
        //    this.aliasResolver = aliasResolver;
        //    if(this.aliasResolver == null) {
        //         this.aliasResolver = new ReaderAliasResolver();
        //    }
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceReader"/> class for the specified <see cref="TextReader"/>.
        /// </summary>
        /// <param name="reader">A text stream reader that contains resources. </param>
        /// <param name="typeResolver">An object that resolves type names specified in a resource.</param>
        public ResXResourceReader(TextReader reader, ITypeResolutionService typeResolver = null)
            //: this(reader, typeResolver, (ReaderAliasResolver)null)
        {
            if (reader == null)
                throw new ArgumentNullException("reader", Res.Get(Res.ArgumentNull));

            //this.reader = XmlReader.Create(reader, InitNameTable());
            this.reader = new ResXReader(reader);
            this.typeResolver = typeResolver;
        }

        //private ResXResourceReader(TextReader reader, ITypeResolutionService typeResolver, ReaderAliasResolver aliasResolver)
        //{
        //    this.reader = reader;
        //    this.typeResolver = typeResolver;
        //    this.aliasResolver = aliasResolver;
        //    if (this.aliasResolver == null)
        //    {
        //        this.aliasResolver = new ReaderAliasResolver();
        //    }
        //}

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceReader"/> class for the specified stream.
        /// </summary>
        /// <param name="stream">An input stream that contains resources. </param>
        /// <param name="typeResolver">An object that resolves type names specified in a resource.</param>
        public ResXResourceReader(Stream stream, ITypeResolutionService typeResolver = null)
            //: this(stream, typeResolver, (ReaderAliasResolver)null)
        {
            if (stream == null)
                throw new ArgumentNullException("stream", Res.Get(Res.ArgumentNull));

            //reader = XmlReader.Create(stream, InitNameTable());
            reader = new ResXReader(stream);
            this.typeResolver = typeResolver;
        }

        //private ResXResourceReader(Stream stream, ITypeResolutionService typeResolver, ReaderAliasResolver aliasResolver)
        //{
        //    this.stream = stream;
        //    this.typeResolver = typeResolver;
        //    this.aliasResolver = aliasResolver;
        //    if (this.aliasResolver == null)
        //    {
        //        this.aliasResolver = new ReaderAliasResolver();
        //    }
        //}

        ///// <summary>
        ///// Initializes a new instance of the <see cref="T:System.Resources.ResXResourceReader"/> class using a stream and an array of assembly names.
        ///// </summary>
        ///// <param name="stream">An input stream that contains resources. </param><param name="assemblyNames">An array of <see cref="T:System.Reflection.AssemblyName"/> objects that specifies one or more assemblies. The assemblies are used to resolve a type name in the resource to an actual type. </param>
        //public ResXResourceReader(Stream stream, AssemblyName[] assemblyNames) : this(stream, assemblyNames, (ReaderAliasResolver)null){
        //}
        //private ResXResourceReader(Stream stream, AssemblyName[] assemblyNames, ReaderAliasResolver aliasResolver) {
        //    this.stream = stream;
        //    this.assemblyNames = assemblyNames;
        //    this.aliasResolver = aliasResolver;
        //    if(this.aliasResolver == null) {
        //         this.aliasResolver = new ReaderAliasResolver();
        //    }
        //}

        ///// <summary>
        ///// Initializes a new instance of the <see cref="T:System.Resources.ResXResourceReader"/> class using a <see cref="T:System.IO.TextReader"/> object and an array of assembly names.
        ///// </summary>
        ///// <param name="reader">An object used to read resources from a stream of text. </param><param name="assemblyNames">An array of <see cref="T:System.Reflection.AssemblyName"/> objects that specifies one or more assemblies. The assemblies are used to resolve a type name in the resource to an actual type. </param>
        //public ResXResourceReader(TextReader reader, AssemblyName[] assemblyNames) : this(reader, assemblyNames, (ReaderAliasResolver)null){
        //}
        //private ResXResourceReader(TextReader reader, AssemblyName[] assemblyNames, ReaderAliasResolver aliasResolver) {
        //    this.reader = reader;
        //    this.assemblyNames = assemblyNames;
        //    this.aliasResolver = aliasResolver;
        //    if(this.aliasResolver == null) {
        //         this.aliasResolver = new ReaderAliasResolver();
        //    }
        //}

        ///// <summary>
        ///// Initializes a new instance of the <see cref="T:System.Resources.ResXResourceReader"/> class using an XML resource file name and an array of assembly names.
        ///// </summary>
        ///// <param name="fileName">The name of an XML resource file that contains resources. </param><param name="assemblyNames">An array of <see cref="T:System.Reflection.AssemblyName"/> objects that specifies one or more assemblies. The assemblies are used to resolve a type name in the resource to an actual type. </param>
        //public ResXResourceReader(string fileName, AssemblyName[] assemblyNames) : this(fileName, assemblyNames, (ReaderAliasResolver)null){
        //}
        //private ResXResourceReader(string fileName, AssemblyName[] assemblyNames, ReaderAliasResolver aliasResolver) {
        //    this.fileName = fileName;
        //    this.assemblyNames = assemblyNames;
        //    this.aliasResolver = aliasResolver;
        //    if(this.aliasResolver == null) {
        //         this.aliasResolver = new ReaderAliasResolver();
        //    }
        //}

        /// <summary>
        /// Creates a new <see cref="ResXResourceReader"/> object and initializes it to read a string whose contents are in the form of an XML resource file.
        /// </summary>
        /// 
        /// <returns>
        /// An object that reads resources from the <paramref name="fileContents"/> string.
        /// </returns>
        /// <param name="fileContents">A string containing XML resource-formatted information. </param>
        /// <param name="typeResolver">An object that resolves type names specified in a resource.</param>
        /// <internalonly/>
        /// <devdoc>
        ///     Creates a reader with the specified file contents.
        /// </devdoc>
        public static ResXResourceReader FromFileContents(string fileContents, ITypeResolutionService typeResolver = null)
        {
            ResXResourceReader result = new ResXResourceReader(new StringReader(fileContents), typeResolver);
            //result.fileContents = fileContents;
            return result;
        }

        ///// <summary>
        ///// Creates a new <see cref="T:System.Resources.ResXResourceReader"/> object and initializes it to read a string whose contents are in the form of an XML resource file, and to use an array of <see cref="T:System.Reflection.AssemblyName"/> objects to resolve type names specified in a resource.
        ///// </summary>
        ///// 
        ///// <returns>
        ///// An object that reads resources from the <paramref name="fileContents"/> string.
        ///// </returns>
        ///// <param name="fileContents">A string whose contents are in the form of an XML resource file.</param>
        ///// <param name="assemblyNames">An array of <see cref="AssemblyName"/> objects that specifies one or more assemblies. The assemblies are used to resolve a type name in the resource to an actual type.</param>
        ///// <internalonly/>
        ///// <devdoc>
        /////     Creates a reader with the specified file contents.
        ///// </devdoc>
        //public static ResXResourceReader FromFileContents(string fileContents, AssemblyName[] assemblyNames)
        //{
        //    ResXResourceReader result = new ResXResourceReader(assemblyNames);
        //    result.fileContents = fileContents;
        //    return result;
        //}

        /// <summary>
        /// This member overrides the <see cref="M:System.Object.Finalize"/> method.
        /// </summary>
        ~ResXResourceReader()
        {
            Dispose(false);
        }

        /// <summary>
        /// Gets or sets the base path for the relative file path specified in a <see cref="ResXFileRef"/> object.
        /// </summary>
        /// <returns>
        /// A path that, if prepended to the relative file path specified in a <see cref="ResXFileRef"/> object, yields an absolute path to a resource file.
        /// </returns>
        public string BasePath
        {
            get { return basePath; }
            set
            {
                switch (state)
                {
                    case States.Disposed:
                        throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                    default:
                        basePath = value;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether <see cref="ResXDataNode"/> objects are returned when reading the current XML resource file or stream.
        /// </summary>
        /// <remarks>
        /// <note>This property is maintained due to compatibility reasons with the <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a> class.
        /// Use <see cref="SafeMode"/> property instead.</note>
        /// </remarks>
        /// <seealso cref="SafeMode"/>
        /// <seealso cref="ResXResourceSet.SafeMode"/>
        /// <seealso cref="ResXResourceManager.SafeMode"/>
        [Obsolete("This property is maintained due to compatibility reasons with the System.Windows.Forms.ResXResourceReader class. Use SafeMode property instead.")]
        public bool UseResXDataNodes
        {
            get { return safeMode; }
            set { SafeMode = value; }
        }

        /// <summary>
        /// Gets or sets whether <see cref="ResXDataNode"/> objects are returned when reading the current XML resource file or stream.
        /// </summary>
        /// <remarks>
        /// <para>When <c>SafeMode</c> is <c>true</c>, the objects returned by the <see cref="GetEnumerator"/> and <see cref="GetMetadataEnumerator"/> methods
        /// return <see cref="ResXDataNode"/> instances instead of deserialized objects. You can retrieve the deserialized
        /// objects on demand by calling the <see cref="ResXDataNode.GetValue"/> method on the <see cref="ResXDataNode"/> instance.</para>
        /// </remarks>
        /// <seealso cref="ResXResourceSet.SafeMode"/>
        /// <seealso cref="ResXResourceManager.SafeMode"/>
        public bool SafeMode
        {
            get { return safeMode; }
            set
            {
                if (state == States.Disposed)
                    throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                safeMode = value;
            }
        }

        /// <summary>
        /// Gets or sets whether "resheader" entries are checked. When <c>true</c>, a <see cref="NotSupportedException"/>
        /// can be thrown during the enumeration when "resheader" entries contain invalid values. When header entries are
        /// missing, no exception is thrown.
        /// <br/>Default value: <c>false</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">In a set operation, a value cannot be specified because the XML resource file has already been accessed and is in use.</exception>
        public bool CheckHeader
        {
            get { return checkHeader; }
            set
            {
                switch (state)
                {
                    case States.Created:
                        checkHeader = value;
                        break;
                    case States.Disposed:
                        throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                    default:
                        throw new InvalidOperationException(Res.Get(Res.InvalidResXReaderPropertyChange));
                }
            }
        }

        ///// <summary>
        ///// Gets or sets whether the first enumeration should be lazy.
        ///// <br/>Default value: <c>true</c>.
        ///// </summary>
        ///// <remarks>
        ///// <para>A lazy enumeration means that the underlying .resx file should be read only on demand. It is possible that
        ///// not the whole .resx is read, if enumeration stops. After the first enumeration elements are cached.</para>
        ///// <para>If an element is defined more than once, and <see cref="LazyEnumeration"/> is <c>true</c>, then the first enumeration returns every occurance,
        ///// while the further ones only the last occurance.</para>
        ///// </remarks>
        ///// <exception cref="InvalidOperationException">In a set operation, a value cannot be specified because the XML resource file has already been accessed and is in use.</exception>
        //public bool LazyEnumeration
        //{
        //    get { return lazyEnumeration; }
        //    set
        //    {
        //        switch (state)
        //        {
        //            case States.Created:
        //                lazyEnumeration = value;
        //                break;
        //            case States.Disposed:
        //                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
        //            default:
        //                throw new InvalidOperationException(Res.Get(Res.InvalidResXReaderPropertyChange));
        //        }
        //    }
        //}

        /// <summary>
        /// Gets or sets whether all entries of same name of the .resx file should be returned.
        /// <br/>Default value: <c>true</c>.
        /// </summary>
        /// <remarks>
        /// <para>If an element is defined more than once, and <see cref="AllowDuplicatedKeys"/> is <c>true</c>,
        /// then the enumeration returns every occurrence of the entries with identical names.
        /// If <see cref="AllowDuplicatedKeys"/> is <c>false</c> the enumeration returns always the last occurrence of the entries with identical names.</para>
        /// <para>If duplicated keys are allowed, the enumeration of the .resx file is lazy for the first time.
        /// A lazy enumeration means that the underlying .resx file is read only on demand. It is possible that
        /// not the whole .resx is read if enumeration is canceled. After the first enumeration elements are cached.</para>
        /// <note>To be compatible with the <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a>
        /// class set the value of this property <c>false</c>.</note>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceReader"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException">In a set operation, a value cannot be specified because the XML resource file has already been accessed and is in use.</exception>
        public bool AllowDuplicatedKeys
        {
            get { return allowDuplicatedKeys; }
            set
            {
                switch (state)
                {
                    case States.Created:
                        allowDuplicatedKeys = value;
                        break;
                    case States.Disposed:
                        throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                    default:
                        throw new InvalidOperationException(Res.Get(Res.InvalidResXReaderPropertyChange));
                }
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="T:System.Resources.ResXResourceReader"/>.
        /// </summary>
        /// <devdoc>
        ///     Closes and files or streams being used by the reader.
        /// </devdoc>
        // NOTE: Part of IResourceReader - not protected by class level LinkDemand.
        public void Close() {
            ((IDisposable)this).Dispose();
        }

        /// <include file='doc\ResXResourceReader.uex' path='docs/doc[@for="ResXResourceReader.IDisposable.Dispose"]/*' />
        /// <internalonly/>
        // NOTE: Part of IDisposable - not protected by class level LinkDemand.
        void IDisposable.Dispose() {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Resources.ResXResourceReader"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources. </param>
        private void Dispose(bool disposing)
        {
            if (state == States.Disposed)
                return;

            if (disposing)
                reader?.Close();

            reader = null;
            aliases = null;
            resources = null;
            metadata = null;
            enumerator = null;
            state = States.Disposed;
        }

        /// <devdoc>
        ///     Demand loads the resource data.
        /// </devdoc>
        //private void EnsureResData() {
        //    if (resData == null) {
        //        resData = new ListDictionary();
        //        resMetadata = new ListDictionary();

        //        XmlTextReader contentReader = null;

        //        try {
        //            // Read data in any which way
        //            if (fileContents != null) {
        //                contentReader = new XmlTextReader(new StringReader(fileContents));
        //            }
        //            else if (reader != null) {
        //                contentReader = new XmlTextReader(reader);
        //            }
        //            else if (fileName != null || stream != null) {
        //                if (stream == null) {
        //                    stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        //                }

        //                contentReader = new XmlTextReader(stream);
        //            }

        //            SetupNameTable(contentReader);
        //            contentReader.WhitespaceHandling = WhitespaceHandling.None;
        //            ParseXml(contentReader);
        //        }
        //        finally {
        //            if (fileName != null && stream != null) {
        //                stream.Close();
        //                stream = null;
        //            }
        //        }
        //    }
        //}                                

        // NOTE: Part of IEnumerable - not protected by class level LinkDemand.
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumeratorInternal(ResXEnumeratorModes.Resources);
        }

        /// <summary>
        /// Returns an enumerator for the current <see cref="ResXResourceReader"/> object that enumerates the resources
        /// in the source XML resource file.
        /// </summary>
        /// TODO: Remarks:
        /// - mindig csak a resource-okat, winformssal ellentétben ez sosem keveri a metákkal
        /// - elemek típusa függ attól, hogy safe mód van-e (UseResXDataNodes)
        /// - elsőre lazy enumerálás van, hacsak nincs a LazyEnumeration kikapcsolva
        /// - nem garantált, hogy a második enumerálás sorrendje egyezik az elsőével
        public IDictionaryEnumerator GetEnumerator()
        {
            return GetEnumeratorInternal(ResXEnumeratorModes.Resources);
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
                            activeAliases = new Dictionary<string, string>();
                            state = States.Reading;
                            enumerator = new LazyEnumerator(this, mode);
                            return enumerator;                            
                        }

                        // no duplication (non-lazy mode): allocating a dictionary and caching for the first time, too.
                        resources = new Dictionary<string, ResXDataNode>();
                        metadata = new Dictionary<string, ResXDataNode>();
                        aliases = activeAliases = new Dictionary<string, string>();
                        ReadAll();
                        state = States.Read;
                        return new ResXResourceEnumerator(this, mode, 0);
                        
                    // getting an enumerator while the first lazy enumeration has not finished: buffering the items
                    // for the first enumeration and returning a cached enumerator
                    case States.Reading:
                        enumerator.ReadToEnd();
                        state = States.Read;
                        enumerator = null;
                        return new ResXResourceEnumerator(this, mode, 0);

                    // .resx contents are already cached
                    case States.Read:
                        return new ResXResourceEnumerator(this, mode, 0);

                    default:
                        throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                }
             }
        }

        /// <summary>
        /// Provides a dictionary enumerator that can retrieve the design-time properties (&lt;metadata&gt; elements)
        /// from the current XML resource file or stream.
        /// </summary>
        // TODO: lásd GetEnumerator kommentek
        public IDictionaryEnumerator GetMetadataEnumerator()
        {
            return GetEnumeratorInternal(ResXEnumeratorModes.Metadata);
        }

        /// <summary>
        /// Provides a dictionary enumerator that can retrieve the aliases from the current XML resource file or stream.
        /// </summary>
        public IDictionaryEnumerator GetAliasEnumerator()
        {
            return GetEnumeratorInternal(ResXEnumeratorModes.Aliases);
        }


        // ReSharper disable once ParameterHidesMember
        private int GetLineNumber(XmlReader reader)
        {
            IXmlLineInfo xmlLineInfo = reader as IXmlLineInfo;
            return xmlLineInfo != null ? xmlLineInfo.LineNumber : 0;
        }

        // ReSharper disable once ParameterHidesMember
        private int GetLinePosition(XmlReader reader)
        {
            IXmlLineInfo xmlLineInfo = reader as IXmlLineInfo;
            return xmlLineInfo != null ? xmlLineInfo.LinePosition : 0;
        }

//        private void ParseXml(XmlTextReader reader) {
//            bool success = false;
//            try {
//                try {
//                    while (reader.Read()) {
//                        if (reader.NodeType == XmlNodeType.Element) {
//                            string s = reader.LocalName;
                            
//                            if (reader.LocalName.Equals(ResXResourceWriter.AssemblyStr)) {
//                                ParseAssemblyNode(reader, false);
//                            }
//                            else if (reader.LocalName.Equals(ResXResourceWriter.DataStr)) {
//                                ParseDataNode(reader, false);
//                            }
//                            else if (reader.LocalName.Equals(ResXResourceWriter.ResHeaderStr)) {
//                                ParseResHeaderNode(reader);
//                            }
//                            else if (reader.LocalName.Equals(ResXResourceWriter.MetadataStr)) {
//                                ParseDataNode(reader, true);
//                            }
//                        }
//                    }

//                    success = true;
//                }
//                catch (SerializationException se) {
//                int line = this.GetLineNumber((XmlReader)reader);
//                int col = this.GetLinePosition((XmlReader)reader);
//                    string newMessage = String.Empty; //TODO: SR.GetString(SR.SerializationException, reader[ResXResourceWriter.TypeStr], pt.Y, pt.X, se.Message);
//                    XmlException xml = new XmlException(newMessage, se, line, col);
//                    SerializationException newSe = new SerializationException(newMessage, xml);

//                    throw newSe;
//                }
//                catch (TargetInvocationException tie) {
//                int line = this.GetLineNumber((XmlReader)reader);
//                int col = this.GetLinePosition((XmlReader)reader);
//                    string newMessage = String.Empty; //TODO: SR.GetString(SR.InvocationException, reader[ResXResourceWriter.TypeStr], pt.Y, pt.X, tie.InnerException.Message);
//                    XmlException xml = new XmlException(newMessage, tie.InnerException, line, col);
//                    TargetInvocationException newTie = new TargetInvocationException(newMessage, xml);

//                    throw newTie;
//                }
//                catch (XmlException e) {
//                    throw new ArgumentException(/*TODO: SR.GetString(SR.InvalidResXFile, e.Message), e*/);
//                }
//                catch (Exception e) {
//                    if (ClientUtils.IsSecurityOrCriticalException(e)) {
//                        throw;
//                    } else {
//                    int line = this.GetLineNumber((XmlReader)reader);
//                    int col = this.GetLinePosition((XmlReader)reader);
//                        XmlException xmlEx = new XmlException(e.Message, e, line, col);
//                        throw new ArgumentException(/*TODO SR.GetString(SR.InvalidResXFile, xmlEx.Message), xmlEx*/);
//                    }
//                }
//            }
//            finally {
//                if (!success) {
//                    resData = null;
//                    resMetadata = null;
//                }
//            }

//            bool validFile = false;

//            if (object.Equals(resHeaderMimeType, ResXResourceWriter.ResMimeType)) {

//                Type readerType = typeof(ResXResourceReader);
//                Type writerType = typeof(ResXResourceWriter);

//                string readerTypeName = resHeaderReaderType;
//                string writerTypeName = resHeaderWriterType;
//                if (readerTypeName != null &&readerTypeName.IndexOf(',') != -1) {
//                    readerTypeName = readerTypeName.Split(new char[] {','})[0].Trim();
//                }
//                if (writerTypeName != null && writerTypeName.IndexOf(',') != -1) {
//                    writerTypeName = writerTypeName.Split(new char[] {','})[0].Trim();
//                }

//// Don't check validity, since our reader/writer classes are in KGySoft.Libraries.Resources,
//// while the file format has them in System.Resources.  
//                validFile = true;
//                //if (readerTypeName != null && 
//                //    writerTypeName != null && 
//                //    readerTypeName.Equals(readerType.FullName) && 
//                //    writerTypeName.Equals(writerType.FullName)) {
//                //    validFile = true;
//                //}
//            }

//            if (!validFile) {
//                resData = null;
//                resMetadata = null;
//                throw new ArgumentException(/*TODO SR.GetString(SR.InvalidResXFileReaderWriterTypes)*/);
//            }
//        }

        // ReSharper disable once ParameterHidesMember        
        /// <summary>
        /// Parses the resource header node. Header can be completely missing; however, it is checked when required and exists.
        /// </summary>
        private void ParseResHeaderNode(XmlReader reader)
        {
            object name = reader[ResXCommon.NameStr];
            if (name == null)
                return;

            reader.ReadStartElement();

#pragma warning disable 252,253 // reference equality is intended because names are added to NameTable
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
                    throw new NotSupportedException(Res.Get(Res.ResXFileMimeTypeNotSupported, resHeaderMimeType, GetLineNumber(reader), GetLinePosition(reader)));
            }
            else if (name == ResXCommon.ReaderStr
                || name == ResXCommon.WriterStr)
            {
                string typeName = reader.NodeType == XmlNodeType.Element
                    ? reader.ReadElementString()
                    : reader.Value.Trim();

                if (typeName != null
                    && typeName.IndexOf(',') != -1)
                    typeName = typeName.Split(new char[] { ',' })[0].Trim();

                if (name == ResXCommon.ReaderStr)
                {
                    if (typeName == null || (!ResXCommon.ResXResourceReaderNameWinForms.StartsWith(typeName, StringComparison.Ordinal)
                            && typeName != typeof(ResXResourceReader).FullName))
                        throw new NotSupportedException(Res.Get(Res.ResXReaderNotSupported, typeName, GetLineNumber(reader), GetLinePosition(reader)));
                }
                else
                {
                    if (typeName == null || (!ResXCommon.ResXResourceWriterNameWinForms.StartsWith(typeName, StringComparison.Ordinal)
                            && typeName != typeof(ResXResourceReader).FullName))
                        throw new NotSupportedException(Res.Get(Res.ResXWriterNotSupported, typeName, GetLineNumber(reader), GetLinePosition(reader)));
                }
            }
#pragma warning restore 252,253
        }

        // ReSharper disable once ParameterHidesMember
        private void ParseAssemblyNode(XmlReader reader, out string key, out string value)
        {
            key = reader[ResXCommon.AliasStr];
            value = reader[ResXCommon.NameStr];
            if (value == null)
            {
                int line = GetLineNumber(reader);
                int col = GetLinePosition(reader);
                throw ResXCommon.CreateXmlException(Res.Get(Res.XmlMissingAttribute, ResXCommon.NameStr, line, col), line, col);
            }
        }

        private string GetAliasValueFromTypeName(string typeName)
        {
            // value is string
            if (String.IsNullOrEmpty(typeName))
                return null;

            // full name only
            int posComma = typeName.IndexOf(',');
            if (posComma < 0)
                return null;

            // there is an assembly or alias name after the full name
            string alias = typeName.Substring(posComma + 1).Trim();

            // no, sorry
            if (alias.Length == 0)
                return null;

            // alias value found
            string asmName;
            if (activeAliases.TryGetValue(alias, out asmName))
                return asmName;

            // type name is with assembly name
            return null;
        }

        //private sealed class ReaderAliasResolver {
        //    private Hashtable cachedAliases;

        //    internal ReaderAliasResolver() {
        //        this.cachedAliases = new Hashtable();
        //    }

        //    public AssemblyName ResolveAlias(string alias) {

        //        AssemblyName result = null;
        //        if(cachedAliases != null) {
        //            result = (AssemblyName)cachedAliases[alias];
        //        } 
        //        return result;
        //    }

        //    public void PushAlias(string alias, AssemblyName name) {
        //        if (this.cachedAliases != null && !string.IsNullOrEmpty(alias)) {
        //            cachedAliases[alias] = name;
        //        }
        //    }
            
        //}

        /// <summary>
        /// Reads next element (depending on mode) from the XML. Skipped elements (on mode mismatch) are stored into the appropriate caches.
        /// Callers must be in a lock.
        /// </summary>
        private bool ReadNext(ResXEnumeratorModes mode, out string key, out ResXDataNode value)
        {
            key = null;
            value = null;
            switch (state)
            {
                case States.Created:
                    // internal error, no resource is needed
                    throw new InvalidOperationException("State should not be in Created in ReadNext");
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
                    throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
            }
        }

        /// <summary>
        /// Reads the rest of the elements and returns the passed read elements.
        /// Must not be implemented as an iterator because it must read all of the remaining elements immediately.
        /// </summary>
        private IEnumerable<KeyValuePair<string, ResXDataNode>> ReadToEnd(ResXEnumeratorModes mode)
        {
            Dictionary<string, ResXDataNode> result = new Dictionary<string, ResXDataNode>();
            string key;
            ResXDataNode value;
            while (ReadNext(mode, out  key, out value))
            {
                result[key] = value;
            }

            return result;
        }

        private void ReadAll()
        {
            string key;
            ResXDataNode value;
            while (Advance(null, out key, out value))
            {
            }
        }

        /// <summary>
        /// Advances in the XML file based on the specified mode or the whole file if mode is null.
        /// Calls must be in a lock or from a ctor
        /// </summary>
        private bool Advance(ResXEnumeratorModes? mode, out string key, out ResXDataNode value)
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

#pragma warning disable 252,253 // reference equality is intended because names are added to NameTable
                object name = reader.LocalName;
                if (name == ResXCommon.DataStr)
                {
                    ParseDataNode(reader, out key, out value);
                    AddNode(resources, key, value);
                    if (mode == ResXEnumeratorModes.Resources)
                        return true;
                }
                else if (name == ResXCommon.MetadataStr)
                {
                    ParseDataNode(reader, out key, out value);
                    AddNode(metadata, key, value);
                    if (mode == ResXEnumeratorModes.Metadata)
                        return true;
                }
                else if (name == ResXCommon.AssemblyStr)
                {
                    string assemblyName;
                    ParseAssemblyNode(reader, out key, out assemblyName);
                    AddAlias(key, assemblyName);
                    if (mode == ResXEnumeratorModes.Aliases)
                    {
                        value = new ResXDataNode(key, assemblyName);
                        return true;
                    }
                }
                else if (name == ResXCommon.ResHeaderStr && checkHeader)
                {
                    ParseResHeaderNode(reader);
                }
#pragma warning restore 252,253
            }

            key = null;
            value = null;
            reader.Close();
            reader = null;

            return false;
        }

        private void AddAlias(string key, string assemblyName)
        {
            var dict = aliases as Dictionary<string, string>;
            if (dict != null)
            {
                dict[key] = assemblyName;
                Debug.Assert(ReferenceEquals(aliases, activeAliases), "activeAliases should be the same as aliases");
                return;
            }

            activeAliases[key] = assemblyName;
            aliases.Add(new KeyValuePair<string, string>(key, assemblyName));
        }

        private void AddNode(ICollection<KeyValuePair<string, ResXDataNode>> collection, string key, ResXDataNode value)
        {
            var dict = collection as Dictionary<string, ResXDataNode>;
            if (dict != null)
                dict[key] = value;
            else
                collection.Add(new KeyValuePair<string, ResXDataNode>(key, value));
        }

        // ReSharper disable once ParameterHidesMember
        /// <summary>
        /// Parses a data or metadata node.
        /// Calls must be in a lock or from a ctor
        /// </summary>
        private void ParseDataNode(XmlReader reader, out string key, out ResXDataNode value)
        {
            key = reader[ResXCommon.NameStr];
            int line = GetLineNumber(reader);
            int col = GetLinePosition(reader);
            if (key == null)
                throw ResXCommon.CreateXmlException(Res.Get(Res.InvalidResXResourceNoName, line, col), line, col);

            DataNodeInfo nodeInfo = new DataNodeInfo
                {
                    Name = key,
                    TypeName = reader[ResXCommon.TypeStr],
                    MimeType = reader[ResXCommon.MimeTypeStr],
                    BasePath = basePath,
                    Line = line,
                    Column = col
                };

            nodeInfo.AssemblyAliasValue = GetAliasValueFromTypeName(nodeInfo.TypeName);
            nodeInfo.DetectCompatibleFormat();

            bool finishedReadingDataNode = false;
            while (!finishedReadingDataNode && reader.Read())
            {
#pragma warning disable 252,253 // reference equality is intended because names are added to NameTable
                object name = reader.LocalName;
                if (reader.NodeType == XmlNodeType.EndElement
                    && (name == ResXCommon.DataStr || name == ResXCommon.MetadataStr))
                {
                    // we just found </data> or </metadata>
                    finishedReadingDataNode = true;
                }
                else
                {
                    // could be a <value> or a <comment>
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (name == ResXCommon.ValueStr)
                            nodeInfo.ValueData = reader.ReadString();
                        else if (name == ResXCommon.CommentStr)
                            nodeInfo.Comment = reader.ReadString();
                        else
                        {
                            line = GetLineNumber(reader);
                            col = GetLinePosition(reader);
                            throw ResXCommon.CreateXmlException(Res.Get(Res.XmlUnexpectedElement, name, line, col), line, col);
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Text)
                    {
                        // or there is no <value> tag, just the inside of <data> as text
                        nodeInfo.ValueData = reader.Value.Trim();
                    }
                }
#pragma warning restore 252,253
            }

            value = new ResXDataNode(nodeInfo);
        }

        ///// <summary>
        ///// Parses a data or metadata node.
        ///// </summary>
        //private void ParseDataNode(XmlTextReader reader, bool isMetaData)
        //{
        //    DataNodeInfo nodeInfo = new DataNodeInfo();

        //    nodeInfo.Name = reader[ResXResourceWriter.NameStr];
        //    string typeName = reader[ResXResourceWriter.TypeStr];

        //    string alias = null;
        //    AssemblyName assemblyName = null;

        //    if (!string.IsNullOrEmpty(typeName))
        //    {
        //        alias = GetAliasFromTypeName(typeName);
        //    }
        //    if (!string.IsNullOrEmpty(alias))
        //    {
        //        assemblyName = aliasResolver.ResolveAlias(alias);
        //    }
        //    if (assemblyName != null)
        //    {
        //        nodeInfo.TypeName = GetTypeFromTypeName(typeName) + ", " + assemblyName.FullName;
        //    }
        //    else
        //    {
        //        nodeInfo.TypeName = reader[ResXResourceWriter.TypeStr];
        //    }

        //    nodeInfo.MimeType = reader[ResXResourceWriter.MimeTypeStr];

        //    bool finishedReadingDataNode = false;
        //    nodeInfo.Line = this.GetLineNumber(reader);
        //    nodeInfo.Column = this.GetLinePosition(reader);
        //    while (!finishedReadingDataNode && reader.Read())
        //    {
        //        if (reader.NodeType == XmlNodeType.EndElement && (reader.LocalName.Equals(ResXResourceWriter.DataStr) || reader.LocalName.Equals(ResXResourceWriter.MetadataStr)))
        //        {
        //            // we just found </data>, quit or </metadata>
        //            finishedReadingDataNode = true;
        //        }
        //        else
        //        {
        //            // could be a <value> or a <comment>
        //            if (reader.NodeType == XmlNodeType.Element)
        //            {
        //                if (reader.Name.Equals(ResXResourceWriter.ValueStr))
        //                {
        //                    WhitespaceHandling oldValue = reader.WhitespaceHandling;
        //                    try
        //                    {
        //                        // based on the documentation at http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpref/html/frlrfsystemxmlxmltextreaderclasswhitespacehandlingtopic.asp 
        //                        // this is ok because:
        //                        // "Because the XmlTextReader does not have DTD information available to it,
        //                        // SignificantWhitepsace nodes are only returned within the an xml:space='preserve' scope." 
        //                        // the xml:space would not be present for anything else than string and char (see ResXResourceWriter)
        //                        // so this would not cause any breaking change while reading data from Everett (we never outputed
        //                        // xml:space then) or from whidbey that is not specifically either a string or a char.
        //                        // However please note that manually editing a resx file in Everett and in Whidbey because of the addition
        //                        // of xml:space=preserve might have different consequences...
        //                        reader.WhitespaceHandling = WhitespaceHandling.Significant;
        //                        nodeInfo.ValueData = reader.ReadString();
        //                    }
        //                    finally
        //                    {
        //                        reader.WhitespaceHandling = oldValue;
        //                    }
        //                }
        //                else if (reader.Name.Equals(ResXResourceWriter.CommentStr))
        //                {
        //                    nodeInfo.Comment = reader.ReadString();
        //                }
        //            }
        //            else
        //            {
        //                // weird, no <xxxx> tag, just the inside of <data> as text
        //                nodeInfo.ValueData = reader.Value.Trim();
        //            }
        //        }
        //    }

        //    if (nodeInfo.Name == null)
        //    {
        //        throw new ArgumentException(/*TODO SR.GetString(SR.InvalidResXResourceNoName, nodeInfo.ValueData)*/);
        //    }

        //    ResXDataNode dataNode = new ResXDataNode(nodeInfo, BasePath);

        //    if (UseResXDataNodes)
        //    {
        //        resData[nodeInfo.Name] = dataNode;
        //    }
        //    else
        //    {
        //        IDictionary data = (isMetaData ? resMetadata : resData);
        //        if (assemblyNames == null)
        //        {
        //            data[nodeInfo.Name] = dataNode.GetValue(typeResolver);
        //        }
        //        else
        //        {
        //            data[nodeInfo.Name] = dataNode.GetValue(assemblyNames);
        //        }
        //    }
        //}

        /// <summary>
        /// Special initialization for ResXResourceSet. No lock is needed because called from ctor. Reads raw xml content only.
        /// </summary>
        internal void ReadAllInternal(Dictionary<string, ResXDataNode> resources, Dictionary<string, ResXDataNode> metadata, Dictionary<string, string> aliases)
        {
            Debug.Assert(state == States.Created);
            this.resources = resources;
            this.metadata = metadata;
            this.aliases = activeAliases = aliases;
            ReadAll();
        }

        #region IResXResourceContainer Members

        ICollection<KeyValuePair<string, ResXDataNode>> IResXResourceContainer.Resources
        {
            get { return resources; }
        }

        ICollection<KeyValuePair<string, ResXDataNode>> IResXResourceContainer.Metadata
        {
            get { return metadata; }
        }

        ICollection<KeyValuePair<string, string>> IResXResourceContainer.Aliases
        {
            get { return aliases; }
        }

        ITypeResolutionService IResXResourceContainer.TypeResolver
        {
            get { return typeResolver; }
        }

        bool IResXResourceContainer.AutoFreeXmlData
        {
            get { return false; }
        }

        int IResXResourceContainer.Version => 0;

        #endregion
    }

    [Serializable]
    internal sealed class ResXNullRef: IObjectReference
    {
        [NonSerialized]
        private static ResXNullRef value;

        /// <summary>
        /// Represents the sole instance of <see cref="ResXNullRef"/> class.
        /// </summary>
        internal static ResXNullRef Value
        {
            get
            {
                if (value == null)
                    Interlocked.CompareExchange(ref value, new ResXNullRef(), null);

                return value;
            }
        }

        #region IObjectReference Members

        object IObjectReference.GetRealObject(StreamingContext context)
        {
            return Value;
        }

        #endregion
    }

//    // Miscellaneous utilities
//    static internal class ClientUtils {

//        // ExecutionEngineException is obsolete and shouldn't be used (to catch, throw or reference) anymore. 
//        // Pragma added to prevent converting the "type is obsolete" warning into build error.
//        // File owner should fix this. 
//#pragma warning disable 618
//        public static bool IsCriticalException(Exception ex)
//        {
//            return ex is NullReferenceException
//                    || ex is StackOverflowException
//                    || ex is OutOfMemoryException
//                    || ex is System.Threading.ThreadAbortException
//                    || ex is ExecutionEngineException
//                    || ex is IndexOutOfRangeException
//                    || ex is AccessViolationException;
//        }
//#pragma warning restore 618

//        public static bool IsSecurityOrCriticalException(Exception ex)
//        {
//            return (ex is System.Security.SecurityException) || IsCriticalException(ex);
//        }
 
////        public static int GetBitCount(uint x) {
////          int count = 0; 
////          while (x > 0){
////              x &= x - 1;
////              count++;
////          } 
////          return count;
////        } 
 

////        // Sequential version 
////        // assumes sequential enum members 0,1,2,3,4 -etc.
////        //
////        public static bool IsEnumValid(Enum enumValue, int value, int minValue, int maxValue) 
////        {		
////            bool valid = (value >= minValue) && (value <= maxValue); 
////#if DEBUG 
////            Debug_SequentialEnumIsDefinedCheck(enumValue, minValue, maxValue);
////#endif 
////            return valid;

////        }
 
////        // Useful for sequential enum values which only use powers of two 0,1,2,4,8 etc: IsEnumValid(val, min, max, 1)
////        // Valid example: TextImageRelation 0,1,2,4,8 - only one bit can ever be on, and the value is between 0 and 8. 
////        // 
////        //   ClientUtils.IsEnumValid((int)(relation), /*min*/(int)TextImageRelation.None, (int)TextImageRelation.TextBeforeImage,1);
////        // 
////        public static bool IsEnumValid(Enum enumValue, int value, int minValue, int maxValue, int maxNumberOfBitsOn) {
////            System.Diagnostics.Debug.Assert(maxNumberOfBitsOn >=0 && maxNumberOfBitsOn<32, "expect this to be greater than zero and less than 32"); 

////            bool valid = (value >= minValue) && (value <= maxValue); 
////            //Note: if it's 0, it'll have no bits on.  If it's a power of 2, it'll have 1. 
////            valid =  (valid && GetBitCount((uint)value) <= maxNumberOfBitsOn);
////#if DEBUG 
////            Debug_NonSequentialEnumIsDefinedCheck(enumValue, minValue, maxValue, maxNumberOfBitsOn, valid);
////#endif
////            return valid;
////        } 

////        // Useful for enums that are a subset of a bitmask 
////        // Valid example: EdgeEffects  0, 0x800 (FillInterior), 0x1000 (Flat), 0x4000(Soft), 0x8000(Mono) 
////        //
////        //   ClientUtils.IsEnumValid((int)(effects), /*mask*/ FillInterior | Flat | Soft | Mono, 
////        //          ,2);
////        //
////        public static bool IsEnumValid_Masked(Enum enumValue, int value, UInt32 mask) {
////            bool valid = ((value & mask) == value); 
 
////#if DEBUG
////            Debug_ValidateMask(enumValue, mask); 
////#endif

////            return valid;
////        } 

 
 

 
////        // Useful for cases where you have discontiguous members of the enum.
////        // Valid example: AutoComplete source.
////        // if (!ClientUtils.IsEnumValid(value, AutoCompleteSource.None,
////        //                                            AutoCompleteSource.AllSystemSources 
////        //                                            AutoCompleteSource.AllUrl,
////        //                                            AutoCompleteSource.CustomSource, 
////        //                                            AutoCompleteSource.FileSystem, 
////        //                                            AutoCompleteSource.FileSystemDirectories,
////        //                                            AutoCompleteSource.HistoryList, 
////        //                                            AutoCompleteSource.ListItems,
////        //                                            AutoCompleteSource.RecentlyUsedList))
////        //
////        // PERF tip: put the default value in the enum towards the front of the argument list. 
////        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
////        public static bool IsEnumValid_NotSequential(System.Enum enumValue, int value, params int[] enumValues) { 
////             System.Diagnostics.Debug.Assert(Enum.GetValues(enumValue.GetType()).Length == enumValues.Length, "Not all the enum members were passed in."); 
////             for (int i = 0; i < enumValues.Length; i++){
////                 if (enumValues[i] == value){ 
////                     return true;
////                 }
////             }
////             return false; 
////        }
 
////#if DEBUG 
////        [ThreadStatic]
////        private static Hashtable enumValueInfo; 
////        public const int MAXCACHE = 300;  // we think we're going to get O(100) of these, put in a tripwire if it gets larger.

////        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
////        private class SequentialEnumInfo { 
////            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
////            public SequentialEnumInfo(Type t) { 
////                int actualMinimum = Int32.MaxValue; 
////                int actualMaximum = Int32.MinValue;
////                int countEnumVals = 0; 

////                foreach (int iVal in Enum.GetValues(t)){
////                    actualMinimum = Math.Min(actualMinimum, iVal);
////                    actualMaximum = Math.Max(actualMaximum, iVal); 
////                    countEnumVals++;
////                } 
 
////                if (countEnumVals -1 != (actualMaximum - actualMinimum)) {
////                    Debug.Fail("this enum cannot be sequential."); 
////                }
////                MinValue = actualMinimum;
////                MaxValue = actualMaximum;
 
////            }
////            public int MinValue; 
////            public int MaxValue; 
////        }
 

////        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider")]
////        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
////        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")] 
////        private static void Debug_SequentialEnumIsDefinedCheck(System.Enum value, int minVal, int maxVal) {
////            Type t = value.GetType(); 
 
////            if (enumValueInfo == null) {
////                enumValueInfo = new Hashtable(); 
////            }

////            SequentialEnumInfo sequentialEnumInfo = null;
 
////            if (enumValueInfo.ContainsKey(t)) {
////                sequentialEnumInfo = enumValueInfo[t] as SequentialEnumInfo; 
////            } 
////            if (sequentialEnumInfo == null) {
////                sequentialEnumInfo = new SequentialEnumInfo(t); 

////                if (enumValueInfo.Count > MAXCACHE) {
////                    // see comment next to MAXCACHE declaration.
////                    Debug.Fail("cache is too bloated, clearing out, we need to revisit this."); 
////                    enumValueInfo.Clear();
////                } 
////                enumValueInfo[t] = sequentialEnumInfo; 

////            } 
////            if (minVal != sequentialEnumInfo.MinValue) {
////                // put string allocation in the IF block so the common case doesnt build up the string.
////                System.Diagnostics.Debug.Fail("Minimum passed in is not the actual minimum for the enum.  Consider changing the parameters or using a different function.");
////            } 
////            if (maxVal != sequentialEnumInfo.MaxValue) {
////                // put string allocation in the IF block so the common case doesnt build up the string. 
////                Debug.Fail("Maximum passed in is not the actual maximum for the enum.  Consider changing the parameters or using a different function."); 
////            }
 
////        }


 
////        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider")]
////        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
////        private static void Debug_ValidateMask(System.Enum value, UInt32 mask) { 
////            Type t = value.GetType();
////            UInt32 newmask = 0; 
////            foreach (int iVal in Enum.GetValues(t)){
////                newmask = newmask | (UInt32)iVal;
////            }
////            System.Diagnostics.Debug.Assert(newmask == mask, "Mask not valid in IsEnumValid!"); 
////        }
 
////        [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider")] 
////        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
////        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")] 
////        private static void Debug_NonSequentialEnumIsDefinedCheck(System.Enum value, int minVal, int maxVal, int maxBitsOn, bool isValid) {
////               Type t = value.GetType();
////               int actualMinimum = Int32.MaxValue;
////               int actualMaximum = Int32.MinValue; 
////               int checkedValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
////               int maxBitsFound = 0; 
////               bool foundValue = false; 
////               foreach (int iVal in Enum.GetValues(t)){
////                   actualMinimum = Math.Min(actualMinimum, iVal); 
////                   actualMaximum = Math.Max(actualMaximum, iVal);
////                   maxBitsFound = Math.Max(maxBitsFound, GetBitCount((uint)iVal));
////                   if (checkedValue == iVal) {
////                       foundValue = true; 
////                   }
////               } 
////               if (minVal != actualMinimum) { 
////                    // put string allocation in the IF block so the common case doesnt build up the string.
////                   System.Diagnostics.Debug.Fail( "Minimum passed in is not the actual minimum for the enum.  Consider changing the parameters or using a different function."); 
////               }
////               if (maxVal != actualMaximum) {
////                    // put string allocation in the IF block so the common case doesnt build up the string.
////                   System.Diagnostics.Debug.Fail("Maximum passed in is not the actual maximum for the enum.  Consider changing the parameters or using a different function."); 
////               }
 
////               if (maxBitsFound != maxBitsOn) { 
////                   System.Diagnostics.Debug.Fail("Incorrect usage of IsEnumValid function. The bits set to 1 in this enum was found to be: " + maxBitsFound.ToString(CultureInfo.InvariantCulture) + "this does not match what's passed in: " + maxBitsOn.ToString(CultureInfo.InvariantCulture));
////               } 
////               if (foundValue != isValid) {
////                    System.Diagnostics.Debug.Fail(String.Format(CultureInfo.InvariantCulture, "Returning {0} but we actually {1} found the value in the enum! Consider using a different overload to IsValidEnum.", isValid, ((foundValue) ? "have" : "have not")));
////               }
 
////           }
////        #endif 
 
////        /// <devdoc>
////        ///   WeakRefCollection - a collection that holds onto weak references 
////        ///
////        ///   Essentially you pass in the object as it is, and under the covers
////        ///   we only hold a weak reference to the object.
////        /// 
////        ///   -----------------------------------------------------------------
////        ///   !!!IMPORTANT USAGE NOTE!!! 
////        ///   Users of this class should set the RefCheckThreshold property 
////        ///   explicitly or call ScavengeReferences every once in a while to
////        ///   remove dead references. 
////        ///   Also avoid calling Remove(item).  Instead call RemoveByHashCode(item)
////        ///   to make sure dead refs are removed.
////        ///   -----------------------------------------------------------------
////        /// 
////        /// </devdoc>
////#if [....]_NAMESPACE || [....]_PUBLIC_GRAPHICS_LIBRARY || DRAWING_NAMESPACE 
////        internal class WeakRefCollection : IList { 
////            private int refCheckThreshold = Int32.MaxValue; // this means this is disabled by default.
////            private ArrayList _innerList; 

////            internal WeakRefCollection() {
////                _innerList = new ArrayList(4);
////            } 

////            internal WeakRefCollection(int size) { 
////                _innerList = new ArrayList(size); 
////            }
 
////            internal ArrayList InnerList {
////                get { return _innerList; }
////            }
 
////            /// <summary>
////            ///     Indicates the value where the collection should check its items to remove dead weakref left over. 
////            ///     Note: When GC collects weak refs from this collection the WeakRefObject identity changes since its 
////            ///           Target becomes null.  This makes the item unrecognizable by the collection and cannot be
////            ///           removed - Remove(item) and Contains(item) will not find it anymore. 
////            ///
////            /// </summary>
////            public int RefCheckThreshold {
////                get{ 
////                    return this.refCheckThreshold;
////                } 
////                set { 
////                    this.refCheckThreshold = value;
////                } 
////            }

////            public object this[int index] {
////                get { 
////                    WeakRefObject weakRef = InnerList[index] as WeakRefObject;
 
////                    if ((weakRef != null) && (weakRef.IsAlive)) { 
////                        return weakRef.Target;
////                    } 

////                    return null;
////                }
////                set { 
////                    InnerList[index] = CreateWeakRefObject(value);
////                } 
////            } 

////            public void ScavengeReferences() { 
////                int currentIndex = 0;
////                int currentCount = Count;
////                for (int i = 0; i < currentCount; i++) {
////                    object item = this[currentIndex]; 

////                    if (item == null) { 
////                        InnerList.RemoveAt(currentIndex); 
////                    }
////                    else {   // only incriment if we have not removed the item 
////                        currentIndex++;
////                    }
////                }
////            } 

////            public override bool Equals(object obj) { 
////                WeakRefCollection other = obj as WeakRefCollection; 

////                if (other == this) { 
////                    return true;
////                }

////                if (other == null || Count != other.Count) { 
////                    return false;
////                } 
 
////                for (int i = 0; i < Count; i++) {
////                    if( this.InnerList[i] != other.InnerList[i] ) { 
////                        if( this.InnerList[i] == null || !this.InnerList[i].Equals(other.InnerList[i])) {
////                            return false;
////                        }
////                    } 
////                }
 
////                return true; 
////            }
 
////            public override int GetHashCode() {
////                return base.GetHashCode();
////            }
 
////            private WeakRefObject CreateWeakRefObject(object value) {
////                if (value == null) { 
////                    return null; 
////                }
////                return new WeakRefObject(value); 
////            }

////            private static void Copy(WeakRefCollection sourceList, int sourceIndex, WeakRefCollection destinationList, int destinationIndex, int length) {
////                if (sourceIndex < destinationIndex) { 
////                    // We need to copy from the back forward to prevent overwrite if source and
////                    // destination lists are the same, so we need to flip the source/dest indices 
////                    // to point at the end of the spans to be copied. 
////                    sourceIndex = sourceIndex + length;
////                    destinationIndex = destinationIndex + length; 
////                    for (; length > 0; length--) {
////                        destinationList.InnerList[--destinationIndex] = sourceList.InnerList[--sourceIndex];
////                    }
////                } 
////                else {
////                    for (; length > 0; length--) { 
////                        destinationList.InnerList[destinationIndex++] = sourceList.InnerList[sourceIndex++]; 
////                    }
////                } 
////            }

////            /// <summary>
////            ///     Removes the value using its hash code as its identity. 
////            ///     This is needed because the underlying item in the collection may have already been collected
////            ///     changing the identity of the WeakRefObject making it impossible for the collection to identify 
////            ///     it.  See WeakRefObject for more info. 
////            /// </summary>
////            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")] 
////            public void RemoveByHashCode(object value) {
////                if( value == null ) {
////                    return;
////                } 

////                int hash = value.GetHashCode(); 
 
////                for( int idx = 0; idx < this.InnerList.Count; idx++ ) {
////                    if(this.InnerList[idx] != null && this.InnerList[idx].GetHashCode() == hash ) { 
////                        this.RemoveAt(idx);
////                        return;
////                    }
////                } 
////            }
 
////            #region IList Members 
////            public void Clear() { InnerList.Clear(); }
////            public bool IsFixedSize { get { return InnerList.IsFixedSize; } } 
////            public bool Contains(object value) { return InnerList.Contains(CreateWeakRefObject(value)); }
////            public void RemoveAt(int index) { InnerList.RemoveAt(index); }
////            public void Remove(object value) { InnerList.Remove(CreateWeakRefObject(value)); }
////            public int IndexOf(object value) { return InnerList.IndexOf(CreateWeakRefObject(value)); } 
////            public void Insert(int index, object value) { InnerList.Insert(index, CreateWeakRefObject(value)); }
////            public int Add(object value) { 
////                if (this.Count > RefCheckThreshold) { 
////                    ScavengeReferences();
////                } 
////                return InnerList.Add(CreateWeakRefObject(value));
////            }
////        #endregion
 
////        #region ICollection Members
////            /// <include file='doc\ArrangedElementCollection.uex' path='docs/doc[@for="ArrangedElementCollection.Count"]/*' /> 
////            public int Count { get { return InnerList.Count; } } 
////            object ICollection.SyncRoot { get { return InnerList.SyncRoot; } }
////            public bool IsReadOnly { get { return InnerList.IsReadOnly; } } 
////            public void CopyTo(Array array, int index) { InnerList.CopyTo(array, index); }
////            bool ICollection.IsSynchronized { get { return InnerList.IsSynchronized; } }
////        #endregion
 
////        #region IEnumerable Members
////            public IEnumerator GetEnumerator() { 
////                return InnerList.GetEnumerator(); 
////            }
////        #endregion 

////            /// <summary>
////            ///     Wraps a weak ref object.
////            ///     WARNING: Use this class carefully! 
////            ///     When the weak ref is collected, this object looses its identity. This is bad when the object
////            ///     has been added to a collection since Contains(WeakRef(item)) and Remove(WeakRef(item)) would 
////            ///     not be able to identify the item. 
////            /// </summary>
////            internal class WeakRefObject { 
////                int hash;
////                WeakReference weakHolder;

////                internal WeakRefObject(object obj) { 
////                    Debug.Assert(obj != null, "Unexpected null object!");
////                    weakHolder = new WeakReference(obj); 
////                    hash = obj.GetHashCode(); 
////                }
 
////                internal bool IsAlive {
////                    get { return weakHolder.IsAlive; }
////                }
 
////                internal object Target {
////                    get { 
////                        return weakHolder.Target; 
////                    }
////                } 

////                public override int GetHashCode() {
////                    return hash;
////                } 

////                public override bool Equals(object obj) { 
////                    WeakRefObject other = obj as WeakRefObject; 

////                    if( other == this ) { 
////                        return true;
////                    }

////                    if (other == null ){ 
////                        return false;
////                    } 
 
////                    if( other.Target != this.Target ) {
////                        if( this.Target == null || !this.Target.Equals(other.Target) ) { 
////                            return false;
////                        }
////                    }
 
////                    return true;
////                } 
////            } 
////        }
////#endif 

//    }

//    /// <devdoc>
//    ///     Helper class supporting Multitarget type assembly qualified name resolution for ResX API.
//    ///     Note: this file is compiled into different assemblies (runtime and VSIP assemblies ...)
//    /// </devdoc>
//    internal static class MultitargetUtil
//    {
//        /// <devdoc>
//        ///     This method gets assembly info for the corresponding type. If the delegate
//        ///     is provided it is used to get this information.
//        /// </devdoc>
//        public static string GetAssemblyQualifiedName(Type type, Func<Type, string> typeNameConverter)
//        {
//            string assemblyQualifiedName = null;

//            if (type != null)
//            {
//                if (typeNameConverter != null)
//                {
//                    try
//                    {
//                        assemblyQualifiedName = typeNameConverter(type);
//                    }
//                    catch (Exception e)
//                    {
//                        if (IsSecurityOrCriticalException(e))
//                        {
//                            throw;
//                        }
//                    }
//                }

//                if (string.IsNullOrEmpty(assemblyQualifiedName))
//                {
//                    assemblyQualifiedName = type.AssemblyQualifiedName;
//                }
//            }

//            return assemblyQualifiedName;
//        }

//        // ExecutionEngineException is obsolete and shouldn't be used (to catch, throw or reference) anymore.
//        // Pragma added to prevent converting the "type is obsolete" warning into build error.
//#pragma warning disable 618
//        private static bool IsSecurityOrCriticalException(Exception ex)
//        {
//            return ex is NullReferenceException
//                    || ex is StackOverflowException
//                    || ex is OutOfMemoryException
//                    || ex is System.Threading.ThreadAbortException
//                    || ex is ExecutionEngineException
//                    || ex is IndexOutOfRangeException
//                    || ex is AccessViolationException
//                    || ex is System.Security.SecurityException;
//        }
//#pragma warning restore 618
//    }

}