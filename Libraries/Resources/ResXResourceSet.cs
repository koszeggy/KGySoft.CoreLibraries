#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXResourceSet.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Xml;

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents the complete content of an XML resource (.resx) file including resources, metadata and aliases.
    /// <br/>See the <strong>Remarks</strong> section to see the differences compared to <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourceset.aspx" target="_blank">System.Resources.ResXResourceSet</a> class.
    /// </summary>
    /// <remarks>
    /// <note>This class is similar to <a href="https://msdn.microsoft.com/en-us/library/System.Resources.ResXResourceSet.aspx" target="_blank">System.Resources.ResXResourceSet</a>
    /// in <c>System.Windows.Forms.dll</c>. See the <a href="#comparison">Comparison with System.Resources.ResXResourceSet</a> section to see the differences.</note>
    /// <note type="tip">To see when to use the <see cref="ResXResourceReader"/>, <see cref="ResXResourceWriter"/>, <see cref="ResXResourceSet"/>, <see cref="ResXResourceManager"/>, <see cref="HybridResourceManager"/> and <see cref="DynamicResourceManager"/>
    /// classes see the documentation of the <see cref="N:KGySoft.Resources">KGySoft.Libraries.Resources</see> namespace.</note>
    /// <para>The <see cref="ResXResourceSet"/> class represents a single XML resource file (.resx file) in memory. It uses <see cref="ResXResourceReader"/> internally to read the .resx content and <see cref="ResXResourceWriter"/> to save it.</para>
    /// <para>A <see cref="ResXResourceSet"/> instance can contain resources, metadata and aliases (unlike the <a href="https://msdn.microsoft.com/en-us/library/System.Resources.ResXResourceSet.aspx" target="_blank">System.Resources.ResXResourceSet</a> class, which contains only the resources).
    /// These contents are available either by enumerators (<see cref="GetEnumerator">GetEnumerator</see>, <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> and <see cref="GetAliasEnumerator">GetAliasEnumerator</see> methods) or directly by key
    /// (<see cref="GetString(string)">GetString</see> and <see cref="GetObject(string)">GetObject</see> methods for resources, <see cref="GetMetaString">GetMetaString</see> and <see cref="GetMetaObject">GetMetaObject</see>
    /// for metadata, and <see cref="GetAliasValue">GetAliasValue</see> for aliases).</para>
    /// <example>
    /// The following example demonstrates how to access the content of a .resx file by the <see cref="ResXResourceSet"/> class using the enumerators.
    /// This is very similar to the first example of <see cref="ResXResourceReader"/>.
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
    ///        var set = new ResXResourceSet(new StringReader(resx));
    ///        Console.WriteLine("____Resources in .resx:____");
    ///        Dump(set, set.GetEnumerator);
    ///        Console.WriteLine("____Metadata in .resx:____");
    ///        Dump(set, set.GetMetadataEnumerator);
    ///        Console.WriteLine("____Aliases in .resx:____");
    ///        Dump(set, set.GetAliasEnumerator);
    ///    }
    /// 
    ///    private static void Dump(ResXResourceSet set, Func<IDictionaryEnumerator> getEnumeratorFunction)
    ///    {
    ///        var enumerator = getEnumeratorFunction();
    ///        while (enumerator.MoveNext())
    ///        {
    ///            Console.WriteLine($"Name: {enumerator.Key}");
    ///            set.SafeMode = true;
    ///            Console.WriteLine($"  Value in SafeMode:     {enumerator.Value} ({enumerator.Value.GetType()})");
    ///            try
    ///            {
    ///                set.SafeMode = false;
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
    /// <para>The <see cref="ResXResourceSet"/> class supports adding new resources (<see cref="SetObject">SetObject</see>), metadata (<see cref="SetMetaObject">SetMetaObject</see>) and aliases (<see cref="SetAliasValue">SetAliasValue</see>).
    /// Existing entries can be removed by <see cref="RemoveObject">RemoveObject</see>, <see cref="RemoveMetaObject">RemoveMetaObject</see> and <see cref="RemoveAliasValue">RemoveAliasValue</see> methods.
    /// The changed set can be saved by the <see cref="O:KGySoft.Resources.ResXResourceSet.Save">Save</see> overloads.</para>
    /// <example>
    /// The following example shows how to create a new resource set, add a new resource and save the content. It demonstrates the usage of the key-based resource access, too.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.IO;
    /// using System.Text;
    /// using KGySoft.Libraries.Resources;
    /// 
    /// public class Example
    /// {
    ///     public static void Main()
    ///     {
    ///         const string key = "myKey";
    ///         var set = new ResXResourceSet();
    /// 
    ///         // GetString/GetObject: reads a resource by key (GetMetaString/GetMetaObject for metadata, GetAliasValue for alias)
    ///         Console.WriteLine($"Getting a non-existing key: {set.GetString(key) ?? "<null>"}");
    /// 
    ///         // SetObject: adds a new resource or replaces an existing one (SetMetaObject for metadata, SetAliasValue for assembly alias)
    ///         // you can even remove entries by RemoveObject/RemoveMetaObject/RemoveAliasValue)
    ///         set.SetObject(key, "a string value");
    ///         Console.WriteLine($"Getting an existing key: {set.GetString(key) ?? "<null>"}");
    /// 
    ///         var savedContent = new StringBuilder();
    ///         set.Save(new StringWriter(savedContent), compatibleFormat: false); // try compatibleFormat: true as well
    ///         Console.WriteLine("Saved .resx content:");
    ///         Console.WriteLine(savedContent);
    ///     }
    ///}
    ///
    /// // The example displays the following output:
    /// // Getting a non-existing key: <null>
    /// // Getting an existing key: a string value
    /// // Saved .resx content:
    /// // <?xml version="1.0" encoding="utf-8"?>
    /// // <root>
    /// //   <data name="myKey">
    /// //     <value>a string value</value>
    /// //   </data>
    /// // </root>]]></code>
    /// </example>
    /// <para>If a .resx content contains the same resource name multiple times, <see cref="ResXResourceSet"/> will contain the lastly defined key. To obtain redefined values use <see cref="ResXResourceReader"/> explicitly
    /// and set <see cref="ResXResourceReader.AllowDuplicatedKeys"/> to <see langword="true"/>.</para>
    /// <para>If the <see cref="SafeMode"/> property is <see langword="true"/> the value of the <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> property returned by the enumerator methods is a <see cref="ResXDataNode"/>
    /// instance rather than the resource value. The same applies for the return value of <see cref="O:KGySoft.Resources.ResXResourceSet.GetObject">GetObject</see> and <see cref="GetMetaObject">GetMetaObject</see> methods. This makes possible to check the raw .resx content before deserialization if the .resx file is from an untrusted source. See also the example at <see cref="ResXDataNode"/>.
    /// <note type="security">Even if <see cref="SafeMode"/> is <see langword="false"/>, loading a .resx content with corrupt or malicious entry will have no effect until we try to obtain the corresponding value. See the example below for the demonstration.</note>
    /// </para>
    /// <para>If <see cref="SafeMode"/> property is <see langword="true"/> the <see cref="O:KGySoft.Resources.ResXResourceSet.GetString">GetString</see> and <see cref="GetMetaString">GetMetaString</see> methods will not throw an
    /// <see cref="InvalidOperationException"/> even for non-string entries; they return the raw XML value instead.</para>
    /// <example>
    /// The following example demonstrates the behavior of <see cref="SafeMode"/> property (see the first example as well, where the entries are accessed by the enumerators).
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using KGySoft.Libraries.Resources;
    /// 
    /// public class Example
    /// {
    ///     private const string resx = @"<?xml version='1.0' encoding='utf-8'?>
    /// <root>
    ///   <data name='string'>
    ///     <value>Test string</value>
    ///     <comment>Default data type is string (when there is neither 'type' nor 'mimetype' attribute).</comment>
    ///   </data>
    /// 
    ///   <data name='binary' type='System.Byte[]'>
    ///     <value>VGVzdCBieXRlcw==</value>
    ///   </data>
    /// 
    ///   <data name='dangerous' mimetype='application/x-microsoft.net.object.binary.base64'>
    ///     <value>boo!</value>
    ///     <comment>BinaryFormatter will throw an exception for this invalid content.</comment>
    ///   </data>
    /// </root>";
    /// 
    ///     public static void Main()
    ///     {
    ///         // please note that default value of SafeMode is false. Nevertheless, reading the .resx containing an invalid node (dangerous)
    ///         // will not cause any problem because nothing is deserialized yet.
    ///         var set = ResXResourceSet.FromFileContents(resx); // same as "new ResXResourceSet(new StringReader(resx));"
    /// 
    ///         // enabling SafeMode changes the GetObject/GetString behavior
    ///         set.SafeMode = true;
    /// 
    ///         Console.WriteLine($"Return type of GetObject in safe mode: {set.GetObject("string").GetType()}");
    /// 
    ///         Console.WriteLine();
    ///         Console.WriteLine("*** Demonstrating SafeMode=true ***");
    ///         TreatSafely(set, "unknown");
    ///         TreatSafely(set, "string");
    ///         TreatSafely(set, "binary");
    ///         TreatSafely(set, "dangerous");
    /// 
    ///         set.SafeMode = false;
    ///         Console.WriteLine();
    ///         Console.WriteLine("*** Demonstrating SafeMode=false ***");
    ///         TreatUnsafely(set, "unknown");
    ///         TreatUnsafely(set, "string");
    ///         TreatUnsafely(set, "binary");
    ///         TreatUnsafely(set, "dangerous");
    ///     }
    /// 
    ///     private static void TreatSafely(ResXResourceSet set, string resourceName)
    ///     {
    ///         // in SafeMode GetObject returns a ResXDataNode
    ///         var resource = set.GetObject(resourceName) as ResXDataNode;
    ///         if (resource == null)
    ///         {
    ///             Console.WriteLine($"Resource name '{resourceName}' does not exist in resource set or SafeMode is off.");
    ///             return;
    ///         }
    /// 
    ///         if (resource.TypeName == null && resource.MimeType == null)
    ///         {
    ///             // to deserialize a node considered safe call GetValue
    ///             Console.WriteLine($"Resource with name '{resourceName}' is a string so it is safe. Its value is '{resource.GetValue()}'");
    ///             return;
    ///         }
    /// 
    ///         if (resource.TypeName != null)
    ///         {
    ///             Console.WriteLine($"Resource with name '{resourceName}' is a '{resource.TypeName}'. If we trust this type we can call GetValue to deserialize it.");
    ///         }
    ///         else
    ///         {
    ///             Console.WriteLine($"Resource with name '{resourceName}' has only mime type: '{resource.MimeType}'.");
    ///             Console.WriteLine("  We cannot tell its type before we deserialize it. We can consider this entry potentially dangerous.");
    ///         }
    /// 
    ///         // In SafeMode GetString(resourceName) never fails.
    ///         // resource.ValueData is similar but ValueData can be null if we allow cleanup .resx content after deserialization.
    ///         Console.WriteLine($"  Raw string value: {set.GetString(resourceName)}");
    ///     }
    /// 
    ///     private static void TreatUnsafely(ResXResourceSet set, string resourceName)
    ///     {
    ///         // If SafeMode is false, GetObject returns null for existing resources containing null value, too.
    ///         // Use ContainsResource to distinct non-existing and null values.
    ///         if (!set.ContainsResource(resourceName))
    ///         {
    ///             Console.WriteLine($"The resource set does not contain a resource named '{resourceName}'.");
    ///             return;
    ///         }
    ///         try
    ///         {
    ///             // If SafeMode is false, GetObject tries to deserialize the resource.
    ///             // GetString would throw an InvalidOperationException on non-string values.
    ///             var value = set.GetObject(resourceName);
    ///             Console.WriteLine($"Type of resource with name '{resourceName}' is {value?.GetType().ToString() ?? "<none>"}. String representation: {value ?? "<null>"}");
    ///         }
    ///         catch (Exception e)
    ///         {
    ///             Console.WriteLine($"Obtaining '{resourceName}' failed with an error: {e.Message}");
    ///         }
    ///     }
    /// }]]>
    /// 
    /// // The example displays the following output:
    /// // Return type of GetObject in safe mode: KGySoft.Libraries.Resources.ResXDataNode
    /// // 
    /// // *** Demonstrating SafeMode=true ***
    /// // Resource name 'unknown' does not exist in resource set or SafeMode is off.
    /// // Resource with name 'string' is a string so it is safe. Its value is 'Test string'
    /// // Resource with name 'binary' is a 'System.Byte[]'. If we trust this type we can call GetValue to deserialize it.
    /// //   Raw string value: VGVzdCBieXRlcw==
    /// // Resource with name 'dangerous' has only mime type: 'application/x-microsoft.net.object.binary.base64'.
    /// //   We cannot tell its type before we deserialize it. We can consider this entry potentially dangerous.
    /// //   Raw string value: boo!
    /// // 
    /// // *** Demonstrating SafeMode=false ***
    /// // The resource set does not contain a resource named 'unknown'.
    /// // Type of resource with name 'string' is System.String. String representation: Test string
    /// // Type of resource with name 'binary' is System.Byte[]. String representation: System.Byte[]
    /// // Obtaining 'dangerous' failed with an error: The input is not a valid Base-64 string as it contains a non-base 64 character,
    /// // more than two padding characters, or an illegal character among the padding characters.</code>
    /// </example>
    /// <h1 class="heading">Comparison with System.Resources.ResXResourceSet<a name="comparison">&#160;</a></h1>
    /// <para><see cref="ResXResourceSet"/> can load .resx files produced both by <see cref="ResXResourceWriter"/> and <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcewriter.aspx" target="_blank">System.Resources.ResXResourceWriter</a>.
    /// <note>When reading a .resx file written by the <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcewriter.aspx" target="_blank">System.Resources.ResXResourceWriter</a> class,
    /// the <c>System.Windows.Forms.dll</c> is not loaded during resolving <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxfileref.aspx" target="_blank">System.Resources.ResXFileRef</a>
    /// and <strong>System.Resources.ResXNullRef</strong> types.</note>
    /// </para>
    /// <para><strong>Incompatibility</strong> with <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourceset.aspx" target="_blank">System.Resources.ResXResourceSet</a>:
    /// <list type="bullet">
    /// <item>There are no constructors with single <see cref="string"/> and <see cref="Stream"/> arguments; though if using pure C# (without reflection) this is a compatible change as the second parameter of the constructors is optional.</item>
    /// <item>The constructors of <a href="https://msdn.microsoft.com/en-us/library/System.Resources.ResXResourceSet.aspx" target="_blank">System.Resources.ResXResourceSet</a> throw an <see cref="ArgumentException"/> on any
    /// kind of error, including when an object cannot be deserialized. This <see cref="ResXResourceSet"/> implementation does not deserialize anything on construction just parses the raw XML content. If there is a syntax error in the .resx
    /// content an <see cref="XmlException"/> will be thrown from the constructors. If an entry cannot be deserialized, the <see cref="GetObject(string)">GetObject</see>, <see cref="GetMetaObject">GetMetaObject</see> or
    /// <see cref="ResXDataNode.GetValue">ResXDataNode.GetValue</see> methods will throw an <see cref="XmlException"/>, <see cref="TypeLoadException"/> or <see cref="NotSupportedException"/> based on the nature of the error.</item>
    /// <item>This <see cref="ResXResourceSet"/> is a sealed class.</item>
    /// </list>
    /// </para>
    /// <para><strong>New features and improvements</strong> compared to <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourceset.aspx" target="_blank">System.Resources.ResXResourceSet</a>:
    /// <list type="bullet">
    /// <item><term>Supporting file references</term>
    /// <description>If the .resx file contains file references with relative paths, then a base path can be defined in the constructors so the file references can be resolved successfully. See also the <see cref="ResXFileRef"/> class.</description></item>
    /// <item><term>Full .resx support</term>
    /// <description>A .resx file can contain also metadata and assembly alias entries in top of resources and this <see cref="ResXResourceSet"/> implementation handles them.</description></item>
    /// <item><term>Performance</term>
    /// <description>Load time is much faster because the constructors just simply parse the raw XML content. The actual deserialization occurs on demand only for the really accessed resources and metadata.
    /// Memory footprint is tried to be kept minimal as well. If <see cref="AutoFreeXmlData"/> is <see langword="true"/>, then raw XML data is freed after deserializing and caching an entry.</description></item>
    /// <item><term>Security</term>
    /// <description>This <see cref="ResXResourceSet"/> is much more safe, even if <see cref="SafeMode"/> is <see langword="false"/>, because no object is deserialized at load time.
    /// If <see cref="SafeMode"/> is <see langword="true"/>, then security is even more increased as <see cref="GetObject(string,bool)">GetObject</see> and <see cref="GetMetaObject">GetMetaObject</see> methods, and the <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see>
    /// property of the enumerators returned by <see cref="GetEnumerator">GetEnumerator</see> and <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> methods return a <see cref="ResXDataNode"/> instance instead of a deserialized object
    /// so you can check whether the resource or metadata can be treat as a safe object before actually deserializing it. See the example above for more details.</description></item>
    /// <item><term>Write support</term>
    /// <description>The .resx file content can be expanded, existing entries can be replaced or removed and the new content can be saved by the <see cref="O:KGySoft.Resources.ResXResourceSet.Save">Save</see> methods.
    /// You can start even with a completely empty set, add content dynamically and save the new resource set.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <seealso cref="ResXDataNode"/>
    /// <seealso cref="ResXFileRef"/>
    /// <seealso cref="ResXResourceReader"/>
    /// <seealso cref="ResXResourceWriter"/>
    /// <seealso cref="ResXResourceManager"/>
    /// <seealso cref="HybridResourceManager"/>
    /// <seealso cref="DynamicResourceManager"/>
    [Serializable]
    public sealed class ResXResourceSet : ResourceSet, IExpandoResourceSet, IResXResourceContainer, IExpandoResourceSetInternal, IEnumerable
    {
        #region Fields

        private readonly string fileName;

        private Dictionary<string, ResXDataNode> resources;
        [NonSerialized] private Dictionary<string, ResXDataNode> resourcesIgnoreCase;
        private Dictionary<string, ResXDataNode> metadata;
        [NonSerialized] private Dictionary<string, ResXDataNode> metadataIgnoreCase;
        private Dictionary<string, string> aliases;
        private bool safeMode;
        private bool autoFreeXmlData = true;
        private string basePath;
        private bool isModified;
        private int version;

        #endregion

        #region Properties

        /// <summary>
        /// If this <see cref="ResXResourceSet"/> has been created from a file, returns the name of the original file.
        /// This property will not change if the <see cref="ResXResourceSet"/> is saved into another file.
        /// </summary>
        public string FileName => fileName;

        /// <summary>
        /// Gets or sets whether the <see cref="ResXResourceSet"/> works in safe mode. In safe mode the retrieved
        /// objects are not deserialized automatically. See Remarks section for details.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>When <c>SafeMode</c> is <see langword="true"/>, the <see cref="GetObject(string,bool)">GetObject</see> and <see cref="GetMetaObject">GetMetaObject</see> methods
        /// return <see cref="ResXDataNode"/> instances instead of deserialized objects. You can retrieve the deserialized
        /// objects on demand by calling the <see cref="ResXDataNode.GetValue">ResXDataNode.GetValue</see> method.</para>
        /// <para>When <c>SafeMode</c> is <see langword="true"/>, the <see cref="GetString(string,bool)">GetString</see> and <see cref="GetMetaString">GetMetaString</see> methods
        /// work for every defined item in the resource set. For non-string elements the raw XML string value will be returned.</para>
        /// <para>If <c>SafeMode</c> is <see langword="true"/>, the <see cref="AutoFreeXmlData"/> property is ignored. The raw XML data of a node
        /// can be freed when calling the <see cref="ResXDataNode.GetValue">ResXDataNode.GetValue</see> method.</para>
        /// <para>For examples see the documentation of the <see cref="ResXResourceSet"/> class.</para>
        /// </remarks>
        /// <seealso cref="ResXResourceReader.SafeMode"/>
        /// <seealso cref="ResXResourceManager.SafeMode"/>
        /// <seealso cref="HybridResourceManager.SafeMode"/>
        /// <seealso cref="AutoFreeXmlData"/>
        public bool SafeMode
        {
            get { return safeMode; }
            set
            {
                if (resources == null)
                    throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                safeMode = value;
            }
        }

        /// <summary>
        /// Gets the base path for the relative file paths specified in a <see cref="ResXFileRef"/> object.
        /// </summary>
        /// <returns>
        /// A path that, if prepended to the relative file path specified in a <see cref="ResXFileRef"/> object, yields an absolute path to a resource file.
        /// </returns>
        /// <remarks>This property is read-only. To define a base path specify it in the constructors. When a <see cref="ResXResourceSet"/> is saved by
        /// one of the <see cref="O:KGySoft.Resources.ResXResourceSet.Save">Save</see> methods you can define an alternative path, which will not overwrite the value of this property.</remarks>
        public string BasePath => basePath;

        /// <summary>
        /// Gets or sets whether the raw XML data of the stored elements should be freed once their value has been deserialized.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to free the stored raw XML data automatically; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// <para>If the value of the property is <see langword="true"/>, then the stored raw XML data will be automatically freed when
        /// a resource or metadata item is obtained by <see cref="O:KGySoft.Resources.ResXResourceSet.GetObject">GetObject</see>, <see cref="GetMetaObject">GetMetaObject</see>,
        /// <see cref="O:KGySoft.Resources.ResXResourceSet.GetString">GetString</see> or <see cref="GetMetaString">GetMetaString</see> methods.
        /// The raw XML data is re-generated on demand if needed, it is transparent to the user.</para>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>, this property has no effect and the clean-up can be controlled by the <see cref="ResXDataNode.GetValue">ResXDataNode.GetValue</see> method.</para>
        /// </remarks>
        public bool AutoFreeXmlData
        {
            get { return autoFreeXmlData; }
            set { autoFreeXmlData = value; }
        }

        /// <summary>
        /// Gets whether this <see cref="ResXResourceSet" /> instance is modified (contains unsaved data).
        /// </summary>
        /// <value>
        /// <see langword="true"/> if this instance is modified; otherwise, <see langword="false"/>.
        /// </value>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        public bool IsModified
        {
            get
            {
                if (resources == null)
                    throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));
                return isModified;
            }
        }

        #endregion

        #region Constructors

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of a <see cref="ResXResourceSet"/> class using the <see cref="ResXResourceReader"/> that opens and reads resources from the specified file.
        /// </summary>
        /// <param name="fileName">The name of the file to read resources from. If <see langword="null"/>, just an empty <see cref="ResXResourceSet"/> will be created.</param>
        /// <param name="basePath">The base path for the relative file paths specified in a <see cref="ResXFileRef"/> object. If <see langword="null"/> and <paramref name="fileName"/> is not <see langword="null"/>, the directory part of <paramref name="fileName"/> will be used.</param>
        public ResXResourceSet(string fileName = null, string basePath = null)
            : this(basePath)
        {
            this.fileName = fileName;
            if (fileName != null)
            {
                Initialize(new ResXResourceReader(fileName) { BasePath = basePath ?? Path.GetDirectoryName(fileName) });
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceSet"/> class using the <see cref="ResXResourceReader"/> to read resources from the specified <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> of resources to be read. The stream should refer to a valid resource file content.</param>
        /// <param name="basePath">The base path for the relative file paths specified in a <see cref="ResXFileRef"/> object. If <see langword="null"/>, the current directory will be used.</param>
        public ResXResourceSet(Stream stream, string basePath = null)
            : this(basePath)
        {
            Initialize(new ResXResourceReader(stream) { BasePath = basePath });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceSet"/> class using the <see cref="ResXResourceReader"/> to read resources from the specified <paramref name="textReader"/>.
        /// </summary>
        /// <param name="textReader">The <see cref="TextReader"/> of resources to be read. The reader should refer to a valid resource file content.</param>
        /// <param name="basePath">The base path for the relative file paths specified in a <see cref="ResXFileRef"/> object. If <see langword="null"/>, the current directory will be used.</param>
        public ResXResourceSet(TextReader textReader, string basePath = null)
            : this(basePath)
        {
            Initialize(new ResXResourceReader(textReader) { BasePath = basePath });
        }

        #endregion

        #region Private Constructors

        /// <summary>
        /// Initializes a new, empty instance of the <see cref="ResXResourceSet"/> class.
        /// </summary>
        /// <param name="basePath">The base path for the relative file paths specified in the <see cref="ResXFileRef"/> objects,
        /// which will be added to this empty <see cref="ResXResourceSet"/> instance.</param>
        /// <remarks>This constructor is private so the single string parameter in the public constructors means file name, which is compatible with the system version.</remarks>
        private ResXResourceSet(string basePath)
        {
            Table = null; // base ctor initializes that; however, we don't need it.
            this.basePath = basePath;
            resources = new Dictionary<string, ResXDataNode>();
            metadata = new Dictionary<string, ResXDataNode>(0);
            aliases = new Dictionary<string, string>(0);
        }

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        /// <summary>
        /// Creates a new <see cref="ResXResourceSet"/> object and initializes it to read a string whose contents are in the form of an XML resource file.
        /// </summary>
        /// <param name="fileContents">A string containing XML resource-formatted information.</param>
        /// <param name="basePath">The base path for the relative file paths specified in a <see cref="ResXFileRef"/> object.</param>
        /// <returns></returns>
        public static ResXResourceSet FromFileContents(string fileContents, string basePath = null)
        {
            return new ResXResourceSet(new StringReader(fileContents), basePath);
        }

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Returns the type of <see cref="ResXResourceReader"/>, which is the preferred resource reader class for <see cref="ResXResourceSet"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="ResXResourceReader"/>, which is the preferred resource reader for <see cref="ResXResourceSet"/>.
        /// </returns>
        public override Type GetDefaultReader()
        {
            return typeof(ResXResourceReader);
        }

        /// <summary>
        /// Returns the type of <see cref="ResXResourceWriter"/>, which is the preferred resource writer class for <see cref="ResXResourceSet"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/> of <see cref="ResXResourceWriter"/>, which is the preferred resource writer for <see cref="ResXResourceSet"/>.
        /// </returns>
        public override Type GetDefaultWriter()
        {
            return typeof(ResXResourceWriter);
        }

        /// <summary>
        /// Returns an <see cref="IDictionaryEnumerator" /> that can iterate through the resources of the <see cref="ResXResourceSet" />.
        /// </summary>
        /// <returns>
        /// An <see cref="IDictionaryEnumerator" /> for the resources of this <see cref="ResXResourceSet" />.
        /// </returns>
        /// <remarks>
        /// <para>The returned enumerator iterates through the resources of the <see cref="ResXResourceSet"/>.
        /// To obtain a specific resource by name, use the <see cref="O:KGySoft.Resources.ResXResourceSet.GetObject">GetObject</see> or <see cref="O:KGySoft.Resources.ResXResourceSet.GetString">GetString</see> methods.
        /// To obtain an enumerator for the metadata entries instead, use the <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> method instead.</para>
        /// <para>If the <see cref="SafeMode"/> property is <see langword="true"/>, the <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> property of the returned enumerator is a <see cref="ResXDataNode"/>
        /// instance rather than the resource value. This makes possible to check the raw .resx content before deserialization if the .resx file is from an untrusted source. See also the examples at <see cref="ResXDataNode"/> and <see cref="ResXResourceSet"/> classes.</para>
        /// <para>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</para>
        /// </remarks>
        /// <seealso cref="O:KGySoft.Resources.ResXResourceSet.GetObject"/>
        /// <seealso cref="O:KGySoft.Resources.ResXResourceSet.GetString"/>
        /// <seealso cref="GetMetadataEnumerator"/>
        /// <seealso cref="GetAliasEnumerator"/>
        public override IDictionaryEnumerator GetEnumerator()
        {
            return GetEnumeratorInternal(ResXEnumeratorModes.Resources);
        }

        /// <summary>
        /// Returns an <see cref="IDictionaryEnumerator" /> that can iterate through the metadata of the <see cref="ResXResourceSet" />.
        /// </summary>
        /// <returns>
        /// An <see cref="IDictionaryEnumerator" /> for the metadata of this <see cref="ResXResourceSet" />.
        /// </returns>
        /// <remarks>
        /// <para>The returned enumerator iterates through the metadata entries of the <see cref="ResXResourceSet"/>.
        /// To obtain a specific metadata by name, use the <see cref="GetMetaObject">GetMetaObject</see> or <see cref="GetMetaString">GetMetaString</see> methods.
        /// To obtain an enumerator for the resources instead, use the <see cref="GetEnumerator">GetEnumerator</see> method instead.</para>
        /// <para>If the <see cref="SafeMode"/> property is <see langword="true"/>, the <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> property of the returned enumerator is a <see cref="ResXDataNode"/>
        /// instance rather than the resource value. This makes possible to check the raw .resx content before deserialization if the .resx file is from an untrusted source. See also the examples at <see cref="ResXDataNode"/> and <see cref="ResXResourceSet"/> classes.</para>
        /// <para>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</para>
        /// </remarks>
        /// <seealso cref="GetMetaObject"/>
        /// <seealso cref="GetMetaString"/>
        /// <seealso cref="GetEnumerator"/>
        /// <seealso cref="GetAliasEnumerator"/>
        public IDictionaryEnumerator GetMetadataEnumerator()
        {
            return GetEnumeratorInternal(ResXEnumeratorModes.Metadata);
        }

        /// <summary>
        /// Returns an <see cref="IDictionaryEnumerator" /> that can iterate through the aliases of the <see cref="ResXResourceSet" />.
        /// </summary>
        /// <returns>
        /// An <see cref="IDictionaryEnumerator" /> for the aliases of this <see cref="ResXResourceSet" />.
        /// </returns>
        /// <remarks>
        /// <para>The returned enumerator iterates through the assembly aliases of the <see cref="ResXResourceSet"/>.
        /// To obtain a specific alias value by assembly name, use the <see cref="GetAliasValue">GetAliasValue</see> method.
        /// To obtain an enumerator for the resources instead, use the <see cref="GetEnumerator">GetEnumerator</see> method instead.</para>
        /// <para>The <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> property of the returned enumerator is always a <see cref="string"/> regardless of the value of the <see cref="SafeMode"/> property.</para>
        /// <para>The <see cref="IDictionaryEnumerator.Key">IDictionaryEnumerator.Key</see> property of the returned enumerator is the alias name, whereas <see cref="IDictionaryEnumerator.Value">IDictionaryEnumerator.Value</see> is the corresponding assembly name.</para>
        /// <para>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</para>
        /// </remarks>
        /// <seealso cref="GetAliasValue"/>
        /// <seealso cref="GetEnumerator"/>
        /// <seealso cref="GetMetadataEnumerator"/>
        public IDictionaryEnumerator GetAliasEnumerator()
        {
            return GetEnumeratorInternal(ResXEnumeratorModes.Aliases);
        }

        /// <summary>
        /// Searches for a resource object with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Case-sensitive name of the resource to search for.</param>
        /// <returns>
        /// The requested resource, or when <see cref="SafeMode"/> is <see langword="true"/>, a <see cref="ResXDataNode"/> instance
        /// from which the resource can be obtained. If the requested <paramref name="name"/> cannot be found, <see langword="null"/> is returned.
        /// </returns>
        /// <remarks>
        /// <para>When <see cref="SafeMode"/> is <see langword="true"/>, the returned object is a <see cref="ResXDataNode"/> instance from which the resource can be obtained.</para>
        /// <para>For examples, see the description of the <see cref="ResXResourceSet"/> class</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        public override object GetObject(string name)
        {
            return GetValueInternal(name, false, false, safeMode, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Searches for a resource object with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the resource to search for.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored.</param>
        /// <returns>
        /// The requested resource, or when <see cref="SafeMode"/> is <see langword="true"/>, a <see cref="ResXDataNode"/> instance
        /// from which the resource can be obtained. If the requested <paramref name="name"/> cannot be found, <see langword="null"/> is returned.
        /// </returns>
        /// <remarks> 
        /// <para>When <see cref="SafeMode"/> is <see langword="true"/>, the returned object is a <see cref="ResXDataNode"/> instance from which the resource can be obtained.</para>
        /// <para>For examples, see the description of the <see cref="ResXResourceSet"/> and <see cref="ResXDataNode"/> classes.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        public override object GetObject(string name, bool ignoreCase)
        {
            return GetValueInternal(name, ignoreCase, false, safeMode, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Searches for a <see cref="string" /> resource with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the resource to search for.</param>
        /// <returns>
        /// The <see cref="string"/> value of a resource.
        /// If <see cref="SafeMode"/> is <see langword="false"/>, an <see cref="InvalidOperationException"/> will be thrown for
        /// non-string resources. If <see cref="SafeMode"/> is <see langword="true"/>, the raw XML value will be returned for non-string resources.
        /// </returns>
        /// <remarks>
        /// <para>For examples, see the description of the <see cref="ResXResourceSet"/> class.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/> and the type of the resource is not <see cref="string"/>.</exception>
        public override string GetString(string name)
        {
            return (string)GetValueInternal(name, false, true, safeMode, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Searches for a <see cref="string" /> resource with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the resource to search for.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored.</param>
        /// <returns>
        /// The <see cref="string"/> value of a resource.
        /// If <see cref="SafeMode"/> is <see langword="false"/>, an <see cref="InvalidOperationException"/> will be thrown for
        /// non-string resources. If <see cref="SafeMode"/> is <see langword="true"/>, the raw XML value will be returned for non-string resources.
        /// </returns>
        /// <remarks>
        /// <para>For examples, see the description of the <see cref="ResXResourceSet"/> class.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/> and the type of the resource is not <see cref="string"/>.</exception>
        public override string GetString(string name, bool ignoreCase)
        {
            return (string)GetValueInternal(name, ignoreCase, true, safeMode, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Searches for a metadata object with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the metadata to search for.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored. This parameter is optional
        /// <br/>Default value: <see langword="false"/></param>
        /// <returns>
        /// The requested metadata, or when <see cref="SafeMode"/> is <see langword="true"/>, a <see cref="ResXDataNode"/> instance
        /// from which the metadata can be obtained. If the requested <paramref name="name"/> cannot be found, <see langword="null"/> is returned.
        /// </returns>
        /// <remarks>
        /// When <see cref="SafeMode"/> is <see langword="true"/>, the returned object is a <see cref="ResXDataNode"/> instance
        /// from which the metadata can be obtained.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        public object GetMetaObject(string name, bool ignoreCase = false)
        {
            return GetValueInternal(name, ignoreCase, false, safeMode, metadata, ref metadataIgnoreCase);
        }

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
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/> and the type of the metadata is not <see cref="string"/>.</exception>
        public string GetMetaString(string name, bool ignoreCase = false)
        {
            return (string)GetValueInternal(name, ignoreCase, true, safeMode, metadata, ref metadataIgnoreCase);
        }

        /// <summary>
        /// Gets the assembly name for the specified <paramref name="alias"/>.
        /// </summary>
        /// <param name="alias">The alias of the assembly name, which should be retrieved.</param>
        /// <returns>The assembly name of the <paramref name="alias"/>, or <see langword="null"/> if there is no such alias defined.</returns>
        /// <remarks>If an alias is redefined in the .resx file, then this method returns the last occurrence of the alias value.</remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="IExpandoResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="alias"/> is <see langword="null"/>.</exception>
        public string GetAliasValue(string alias)
        {
            var dict = aliases;
            if (dict == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            string result;
            lock (dict)
            {
                if (!dict.TryGetValue(alias, out result))
                    return null;
            }

            return result;
        }

        /// <summary>
        /// Adds or replaces a resource object in the current <see cref="ResXResourceSet" /> with the specified <paramref name="name" />.
        /// </summary>
        /// <param name="name">Name of the resource to set. Casing is not ignored.</param>
        /// <param name="value">The resource value to set.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet" /> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>If <paramref name="value"/> is <see langword="null"/>, a null reference will be explicitly stored.
        /// Its effect is similar to the <see cref="RemoveObject">RemoveObject</see> method (<see cref="O:KGySoft.Resources.ResXResourceSet.GetObject">GetObject</see> will return <see langword="null"/> in both cases),
        /// but if <see langword="null"/> has been set, it will returned among the results of the <see cref="GetEnumerator">GetEnumerator</see> method.</para>
        /// <para><paramref name="value"/> can be a <see cref="ResXDataNode"/> as well, its value will be interpreted correctly and added to the <see cref="ResXResourceSet"/> with the specified <paramref name="name"/>.</para>
        /// <para>If <paramref name="value"/> is a <see cref="ResXFileRef"/>, then a file reference will be added to the <see cref="ResXResourceSet"/>.
        /// On saving its path will be made relative to the specified <c>basePath</c> argument of the <see cref="O:KGySoft.Resources.ResXResourceSet.Save">Save</see> methods.
        /// If <c>forceEmbeddedResources</c> is <see langword="true"/> on saving, the file references will be converted to embedded ones.</para>
        /// <note>Not just <see cref="ResXDataNode"/> and <see cref="ResXFileRef"/> are handled but <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxdatanode.aspx" target="_blank">System.Resources.ResXDataNode</a>
        /// and <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxfileref.aspx" target="_blank">System.Resources.ResXFileRef</a> as well. The compatibility with the system versions
        /// is provided without any reference to <c>System.Windows.Forms.dll</c>, where those types are located.</note>
        /// </remarks>
        public void SetObject(string name, object value)
        {
            SetValueInternal(name, value, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Adds or replaces a metadata object in the current <see cref="ResXResourceSet" /> with the specified <paramref name="name" />.
        /// </summary>
        /// <param name="name">Name of the metadata value to set.</param>
        /// <param name="value">The metadata value to set. If <see langword="null" />, the value will be removed.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <remarks>
        /// <para>If <paramref name="value"/> is <see langword="null"/>, a null reference will be explicitly stored.
        /// Its effect is similar to the <see cref="RemoveMetaObject">RemoveMetaObject</see> method (<see cref="GetMetaObject">GetMetaObject</see> will return <see langword="null"/> in both cases),
        /// but if <see langword="null"/> has been set, it will returned among the results of the <see cref="GetMetadataEnumerator">GetMetadataEnumerator</see> method.</para>
        /// <para><paramref name="value"/> can be a <see cref="ResXDataNode"/> as well, its value will be interpreted correctly and added to the <see cref="ResXResourceSet"/> with the specified <paramref name="name"/>.</para>
        /// <para>If <paramref name="value"/> is a <see cref="ResXFileRef"/>, then a file reference will be added to the <see cref="ResXResourceSet"/>.
        /// On saving its path will be made relative to the specified <c>basePath</c> argument of the <see cref="O:KGySoft.Resources.ResXResourceSet.Save">Save</see> methods.
        /// If <c>forceEmbeddedResources</c> is <see langword="true"/> on saving, the file references will be converted to embedded ones.</para>
        /// <note>Not just <see cref="ResXDataNode"/> and <see cref="ResXFileRef"/> are handled but <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxdatanode.aspx" target="_blank">System.Resources.ResXDataNode</a>
        /// and <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxfileref.aspx" target="_blank">System.Resources.ResXFileRef</a> as well. The compatibility with the system versions
        /// is provided without any reference to <c>System.Windows.Forms.dll</c>, where those types are located.</note>
        /// </remarks>
        public void SetMetaObject(string name, object value)
        {
            SetValueInternal(name, value, metadata, ref metadataIgnoreCase);
        }

        /// <summary>
        /// Adds or replaces an assembly alias value in the current <see cref="ResXResourceSet"/>.
        /// </summary>
        /// <param name="alias">The alias name to use instead of <paramref name="assemblyName"/> in the saved .resx file.</param>
        /// <param name="assemblyName">The fully or partially qualified name of the assembly.</param>
        /// <remarks>
        /// <note>The added alias values are dumped on saving on demand: only when a resource type is defined in the <see cref="Assembly"/>, whose name is the <paramref name="assemblyName"/>.
        /// Other alias names will be auto generated for non-specified assemblies.</note>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="assemblyName"/> or <paramref name="alias"/> is <see langword="null"/>.</exception>
        public void SetAliasValue(string alias, string assemblyName)
        {
            var dict = aliases;
            if (dict == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (alias == null)
                throw new ArgumentNullException(nameof(alias), Res.ArgumentNull);

            if (assemblyName == null)
                throw new ArgumentNullException(nameof(assemblyName), Res.ArgumentNull);

            lock (dict)
            {
                string asmName;
                if (dict.TryGetValue(alias, out asmName) && asmName == assemblyName)
                    return;

                dict[alias] = assemblyName;
                isModified = true;
                version++;
            }
        }

        /// <summary>
        /// Removes a resource object from the current <see cref="ResXResourceSet"/> with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the resource value to remove. Name is treated case sensitive.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        public void RemoveObject(string name)
        {
            RemoveValueInternal(name, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Removes a metadata object from the current <see cref="ResXResourceSet"/> with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the metadata value to remove. Name is treated case sensitive.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        public void RemoveMetaObject(string name)
        {
            RemoveValueInternal(name, metadata, ref metadataIgnoreCase);
        }

        /// <summary>
        /// Removes an assembly alias value from the current <see cref="ResXResourceSet"/>.
        /// </summary>
        /// <param name="alias">The alias, which should be removed.</param>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceSet"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="alias"/> is <see langword="null"/>.</exception>
        public void RemoveAliasValue(string alias)
        {
            var dict = aliases;
            if (dict == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (alias == null)
                throw new ArgumentNullException(nameof(alias), Res.ArgumentNull);

            lock (dict)
            {
                if (!dict.ContainsKey(alias))
                    return;

                dict.Remove(alias);
                isModified = true;
            }
        }

        /// <summary>
        /// Saves the <see cref="ResXResourceSet" /> to the specified file.</summary>
        /// <param name="fileName">The location of the file where you want to save the resources.</param>
        /// <param name="compatibleFormat">If set to <see langword="true"/>, the result .resx file can be read by the <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a> class
        /// and the Visual Studio Resource Editor. If set to <see langword="false"/>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />), but the result can be read only by <see cref="ResXResourceReader" />
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <param name="forceEmbeddedResources">If set to <see langword="true"/> the resources using a file reference (<see cref="ResXFileRef" />) will be replaced to embedded resources.
        /// <br/>Default value: <see langword="false"/></param>
        /// <param name="basePath">A new base path for the file paths specified in the <see cref="ResXFileRef"/> objects. If <see langword="null"/>,
        /// the original <see cref="BasePath"/> will be used. The file paths in the saved .resx file will be relative to the <paramref name="basePath"/>.
        /// Applicable if <paramref name="forceEmbeddedResources"/> is <see langword="false"/>.
        /// <br/>Default value: <c><see langword="null"/>.</c></param>
        /// <seealso cref="ResXResourceWriter"/>
        /// <seealso cref="ResXResourceWriter.CompatibleFormat"/>
        public void Save(string fileName, bool compatibleFormat = false, bool forceEmbeddedResources = false, string basePath = null)
        {
            using (var writer = new ResXResourceWriter(fileName) { BasePath = basePath ?? this.basePath, CompatibleFormat = compatibleFormat })
            {
                Save(writer, forceEmbeddedResources);
            }
        }

        /// <summary>
        /// Saves the <see cref="ResXResourceSet" /> to the specified <paramref name="stream"/>.</summary>
        /// <param name="stream">The stream to which you want to save.</param>
        /// <param name="compatibleFormat">If set to <see langword="true"/>, the result .resx file can be read by the <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a> class
        /// and the Visual Studio Resource Editor. If set to <see langword="false"/>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />), but the result can be read only by <see cref="ResXResourceReader" />
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <param name="forceEmbeddedResources">If set to <see langword="true"/> the resources using a file reference (<see cref="ResXFileRef" />) will be replaced to embedded resources.
        /// <br/>Default value: <see langword="false"/></param>
        /// <param name="basePath">A new base path for the file paths specified in the <see cref="ResXFileRef"/> objects. If <see langword="null"/>,
        /// the original <see cref="BasePath"/> will be used. The file paths in the saved .resx file will be relative to the <paramref name="basePath"/>.
        /// Applicable if <paramref name="forceEmbeddedResources"/> is <see langword="false"/>.
        /// <br/>Default value: <c><see langword="null"/>.</c></param>
        /// <seealso cref="ResXResourceWriter"/>
        /// <seealso cref="ResXResourceWriter.CompatibleFormat"/>
        public void Save(Stream stream, bool compatibleFormat = false, bool forceEmbeddedResources = false, string basePath = null)
        {
            using (var writer = new ResXResourceWriter(stream) { BasePath = basePath ?? this.basePath, CompatibleFormat = compatibleFormat })
            {
                Save(writer, forceEmbeddedResources);
            }
        }

        /// <summary>
        /// Saves the <see cref="ResXResourceSet" /> to the specified <paramref name="textWriter"/>.</summary>
        /// <param name="textWriter">The text writer to which you want to save.</param>
        /// <param name="compatibleFormat">If set to <see langword="true"/>, the result .resx file can be read by the <a href="https://msdn.microsoft.com/en-us/library/system.resources.resxresourcereader.aspx" target="_blank">System.Resources.ResXResourceReader</a> class
        /// and the Visual Studio Resource Editor. If set to <see langword="false"/>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />), but the result can be read only by <see cref="ResXResourceReader" /><br />Default value: <see langword="false"/>.</param>
        /// <param name="forceEmbeddedResources">If set to <see langword="true"/> the resources using a file reference (<see cref="ResXFileRef" />) will be replaced to embedded resources.
        /// <br/>Default value: <see langword="false"/></param>
        /// <param name="basePath">A new base path for the file paths specified in the <see cref="ResXFileRef"/> objects. If <see langword="null"/>,
        /// the original <see cref="BasePath"/> will be used. The file paths in the saved .resx file will be relative to the <paramref name="basePath"/>.
        /// Applicable if <paramref name="forceEmbeddedResources"/> is <see langword="false"/>.
        /// <br/>Default value: <c><see langword="null"/>.</c></param>
        /// <seealso cref="ResXResourceWriter"/>
        /// <seealso cref="ResXResourceWriter.CompatibleFormat"/>
        public void Save(TextWriter textWriter, bool compatibleFormat = false, bool forceEmbeddedResources = false, string basePath = null)
        {
            using (var writer = new ResXResourceWriter(textWriter) { BasePath = basePath ?? this.basePath, CompatibleFormat = compatibleFormat })
            {
                Save(writer, forceEmbeddedResources);
            }
        }

        /// <summary>
        /// Gets whether the current <see cref="ResXResourceSet"/> contains a resource with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the resource to check.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored. This parameter is optional.
        /// <br/>Default value: <see langword="false"/></param>
        /// <returns><see langword="true"/>, if the current <see cref="ResXResourceSet"/> contains a resource with name <paramref name="name"/>; otherwise, <see langword="false"/>.</returns>
        public bool ContainsResource(string name, bool ignoreCase = false)
        {
            return ContainsInternal(name, ignoreCase, resources, ref resourcesIgnoreCase);
        }

        /// <summary>
        /// Gets whether the current <see cref="ResXResourceSet"/> contains a metadata with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the metadata to check.</param>
        /// <param name="ignoreCase">Indicates whether the case of the specified <paramref name="name"/> should be ignored. This parameter is optional.
        /// <br/>Default value: <see langword="false"/></param>
        /// <returns><see langword="true"/>, if the current <see cref="ResXResourceSet"/> contains a metadata with name <paramref name="name"/>; otherwise, <see langword="false"/>.</returns>
        public bool ContainsMeta(string name, bool ignoreCase = false)
        {
            return ContainsInternal(name, ignoreCase, metadata, ref metadataIgnoreCase);
        }

        #endregion

        #region Internal Methods

        internal object GetResourceInternal(string name, bool ignoreCase, bool isString, bool asSafe)
        {
            return GetValueInternal(name, ignoreCase, isString, asSafe, resources, ref resourcesIgnoreCase);
        }

        internal object GetMetaInternal(string name, bool ignoreCase, bool isString, bool asSafe)
        {
            return GetValueInternal(name, ignoreCase, isString, asSafe, metadata, ref metadataIgnoreCase);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Releases the resources of the current <see cref="ResXResourceSet"/> instance.
        /// </summary>
        /// <param name="disposing">Indicates whether the objects contained in the current instance should be explicitly closed.</param>
        protected override void Dispose(bool disposing)
        {
            // base.Dispose is not called because Table is nullified in ctor and Reader is never set
            resources = null;
            metadata = null;
            aliases = null;
            resourcesIgnoreCase = null;
            metadataIgnoreCase = null;
            basePath = null;
        }

        #endregion

        #region Private Methods

        private void Initialize(ResXResourceReader reader)
        {
            using (reader)
            {
                // this will not deserialize anything just quickly parses the .resx and stores the raw nodes
                reader.ReadAllInternal(resources, metadata, aliases);
            }
        }

        private IDictionaryEnumerator GetEnumeratorInternal(ResXEnumeratorModes mode)
        {
            var syncObj = resources;
            if (syncObj == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            lock (syncObj)
            {
                return new ResXResourceEnumerator(this, mode, version);
            }
        }

        private void Save(ResXResourceWriter writer, bool forceEmbeddedResources)
        {
            var resources = this.resources;
            var metadata = this.metadata;
            var aliases = this.aliases;

            if ((resources ?? metadata ?? (object)aliases) == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            // 1. Adding existing aliases (writing them on-demand) - non existing ones will be auto-generated
            lock (aliases)
            {
                foreach (var alias in aliases)
                {
                    writer.AddAlias(alias.Key, alias.Value);
                }
            }

            // ReSharper disable PossibleNullReferenceException - false alarm, ReSharper does not recognize the effect of ?? operator
            // 2. Adding resources (not freeing xml data during saving)
            bool adjustPath = basePath != null && basePath != writer.BasePath;
            lock (resources)
            {
                foreach (var resource in resources)
                {
                    writer.AddResource(GetNodeToSave(resource.Value, forceEmbeddedResources, adjustPath));
                }
            }

            // 3. Adding metadata
            lock (metadata)
            {
                foreach (var meta in metadata)
                {
                    writer.AddMetadata(GetNodeToSave(meta.Value, forceEmbeddedResources, adjustPath));
                }
            }
            // ReSharper restore PossibleNullReferenceException

            writer.Generate();
            isModified = false;
        }

        private ResXDataNode GetNodeToSave(ResXDataNode node, bool forceEmbeddedResources, bool adjustPath)
        {
            ResXFileRef fileRef = node.FileRef;
            if (fileRef == null)
                return node;

            if (forceEmbeddedResources)
                node = new ResXDataNode(node.Name, node.GetValue(null, basePath, !safeMode && autoFreeXmlData));
            else if (adjustPath && !Path.IsPathRooted(fileRef.FileName))
            {
                // Restoring the original full path so the ResXResourceWriter can create a new relative path to the new basePath
                string origPath = Path.GetFullPath(Path.Combine(basePath, fileRef.FileName));
                node = new ResXDataNode(node.Name, new ResXFileRef(origPath, fileRef.TypeName, fileRef.EncodingName));
            }

            return node;
        }

        private bool ContainsInternal(string name, bool ignoreCase, Dictionary<string, ResXDataNode> data, ref Dictionary<string, ResXDataNode> dataCaseInsensitive)
        {
            if (data == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            lock (data)
            {
                if (data.ContainsKey(name))
                    return true;

                if (!ignoreCase)
                    return false;

                if (dataCaseInsensitive == null)
                {
                    dataCaseInsensitive = InitCaseInsensitive(data);
                }

                return dataCaseInsensitive.ContainsKey(name);
            }
        }

        private object GetValueInternal(string name, bool ignoreCase, bool isString, bool asSafe, Dictionary<string, ResXDataNode> data, ref Dictionary<string, ResXDataNode> dataCaseInsensitive)
        {
            if (data == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (name == null)
                throw new ArgumentNullException(nameof(name), Res.ArgumentNull);

            lock (data)
            {
                ResXDataNode result;
                if (data.TryGetValue(name, out result))
                    return result.GetValueInternal(asSafe, isString, autoFreeXmlData, basePath);

                if (!ignoreCase)
                    return null;

                if (dataCaseInsensitive == null)
                {
                    dataCaseInsensitive = InitCaseInsensitive(data);
                }

                if (dataCaseInsensitive.TryGetValue(name, out result))
                    return result.GetValueInternal(asSafe, isString, autoFreeXmlData, basePath);
            }

            return null;
        }

        private Dictionary<string, ResXDataNode> InitCaseInsensitive(Dictionary<string, ResXDataNode> data)
        {
            var result = new Dictionary<string, ResXDataNode>(data.Count, StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, ResXDataNode> item in data)
            {
                result[item.Key] = item.Value;
            }

            return result;
        }

        private void SetValueInternal(string name, object value, Dictionary<string, ResXDataNode> data, ref Dictionary<string, ResXDataNode> dataIgnoreCase)
        {
            if (data == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (name == null)
                throw new ArgumentNullException(nameof(name), Res.ArgumentNull);

            lock (data)
            {
                ResXDataNode valueNode;

                // optimization: if the deserialized value is the same reference, which is about to be added, returning
                if (data.TryGetValue(name, out valueNode) && valueNode.ValueInternal == (value ?? ResXNullRef.Value))
                    return;

                valueNode = new ResXDataNode(name, value);
                data[name] = valueNode;
                if (dataIgnoreCase != null)
                    dataIgnoreCase[name] = valueNode;

                isModified = true;
                version++;
            }
        }

        private void RemoveValueInternal(string name, Dictionary<string, ResXDataNode> data, ref Dictionary<string, ResXDataNode> dataIgnoreCase)
        {
            if (data == null)
                throw new ObjectDisposedException(null, Res.Get(Res.ObjectDisposed));

            if (name == null)
                throw new ArgumentNullException(nameof(name), Res.ArgumentNull);

            lock (data)
            {
                if (!data.ContainsKey(name))
                    return;

                // clearing the whole ignoreCase dictionary, because cannot tell whether the element should be removed.
                data.Remove(name);
                dataIgnoreCase = null;
                isModified = true;
                version++;
            }
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumeratorInternal(ResXEnumeratorModes.Resources);
        }

        #endregion

        #endregion

        #endregion

        #region IResXResourceContainer Members

        ICollection<KeyValuePair<string, ResXDataNode>> IResXResourceContainer.Resources => resources;

        ICollection<KeyValuePair<string, ResXDataNode>> IResXResourceContainer.Metadata => metadata;

        ICollection<KeyValuePair<string, string>> IResXResourceContainer.Aliases => aliases;

        bool IResXResourceContainer.SafeMode => safeMode;

        ITypeResolutionService IResXResourceContainer.TypeResolver => null;

        int IResXResourceContainer.Version => version;

        #endregion

        #region IExpandoResourceSetInternal Members

        object IExpandoResourceSetInternal.GetResource(string name, bool ignoreCase, bool isString, bool asSafe)
            => GetResourceInternal(name, ignoreCase, isString, asSafe);

        object IExpandoResourceSetInternal.GetMeta(string name, bool ignoreCase, bool isString, bool asSafe)
            => GetMetaInternal(name, ignoreCase, isString, asSafe);

        #endregion
    }
}