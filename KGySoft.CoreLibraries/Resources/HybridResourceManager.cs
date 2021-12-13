#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: HybridResourceManager.cs
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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.IO;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents a resource manager that provides convenient access to culture-specific resources at run time.
    /// It can handle both compiled resources from <c>.dll</c> and <c>.exe</c> files, and <c>.resx</c> files at
    /// the same time. New elements can be added as well, which can be saved into <c>.resx</c> files.
    /// <br/>See the <strong>Remarks</strong> section for an example and for the differences compared to <see cref="ResourceManager"/> class.
    /// </summary>
    /// <remarks>
    /// <para><see cref="HybridResourceManager"/> class is derived from <see cref="ResourceManager"/> and uses a <see cref="ResXResourceManager"/> internally.
    /// The <see cref="HybridResourceManager"/> combines the functionality of the regular <see cref="ResourceManager"/> and the <see cref="ResXResourceManager"/> classes.
    /// The source of the resources can be chosen by the <see cref="Source"/> property (see <see cref="ResourceManagerSources"/> enumeration).
    /// Enabling both binary and .resx resources makes possible to expand or override the resources originally come from binary resources.
    /// Just like the <see cref="ResXResourceManager"/> it is an <see cref="IExpandoResourceManager"/> implementation. The replacement and newly added content can be saved into .resx files.</para>
    /// <para>See the <a href="#comparison">Comparison with ResourceManager</a> section to see all of the differences.</para>
    /// <note type="tip">To see when to use the <see cref="ResXResourceReader"/>, <see cref="ResXResourceWriter"/>, <see cref="ResXResourceSet"/>, <see cref="ResXResourceManager"/>, <see cref="HybridResourceManager"/> and <see cref="DynamicResourceManager"/>
    /// classes see the documentation of the <see cref="N:KGySoft.Resources">KGySoft.Resources</see> namespace.</note>
    /// <h1 class="heading">Example: Expanding compiled resources created by Visual Studio</h1>
    /// <para>You can create compiled resources by Visual Studio and you can dynamically expand them by <see cref="HybridResourceManager"/>. The new and overridden content
    /// will be saved as .resx files. See the following example for a step-by-step guide.
    /// <list type="number">
    /// <item>Create a new project (Console Application)
    /// <br/><img src="../Help/Images/NewConsoleApp.png" alt="New console application"/></item>
    /// <item>In Solution Explorer right click on <c>ConsoleApp1</c>, Add, New Folder, name it <c>Resources</c>.</item>
    /// <item>In Solution Explorer right click on <c>Resources</c>, Add, New Item, Resources File.
    /// <br/><img src="../Help/Images/NewResourcesFile.png" alt="New Resources file"/></item>
    /// <item>In Solution Explorer right click on the new resource file (<c>Resource1.resx</c> if not named otherwise) and select Properties</item>
    /// <item>The default value of <c>Build Action</c> is <c>Embedded Resource</c>, which means that the resource will be compiled into the assembly.
    /// The <see cref="HybridResourceManager"/> will be able to read these compiled resources if its <see cref="Source"/> property is <see cref="ResourceManagerSources.CompiledOnly"/> or <see cref="ResourceManagerSources.CompiledAndResX"/>.
    /// We can clear the default <c>Custom Tool</c> value because the generated file uses a <see cref="ResourceManager"/> class internally, which cannot handle the dynamic expansions.
    /// <br/><img src="../Help/Images/ResourceFileProperties_HybridResourceManager.png" alt="Resources1.resx properties"/>
    /// <note type="tip">To use purely the .resx files you can use the <see cref="ResXResourceManager"/> class. To provide dynamic creation and expansion of new .resx files with the untranslated items for any language use the <see cref="DynamicResourceManager"/> class.</note></item>
    /// <item>Now we can either use the built-on resource editor of Visual Studio or just edit the .resx file by the XML Editor. If we add new or existing files to the resources, they will be automatically added to the project's Resources folder.</item>
    /// <item>To add culture-specific resources you can add further resource files with the same base name, extended by culture names. For example, if the invariant resource is called <c>Resource1.resx</c>, then a
    /// region neutral English resource can be called <c>Resource1.en.resx</c> and the American English resource can be called <c>Resource1.en-US.resx</c>.</item>
    /// <item>If we now compile the project, the <c>ConsoleApp1.exe</c> will contain an embedded resource named <c>ConsoleApp1.Resources.Resource1.resources</c>. If we created
    /// language-specific resources they will be compiled into so-called satellite assemblies. For example, if we have a resource file named <c>Resource1.en.resx</c>, then
    /// building the project will create an <c>en</c> folder containing a <c>ConsoleApp1.resources.dll</c>, which will contain an embedded resource named <c>ConsoleApp1.Resources.Resource1.en.resources</c>.
    /// In both cases the base name is <c>ConsoleApp1.Resources.Resource1</c>, this must be the <c>baseName</c> parameter in the constructors of the <see cref="HybridResourceManager"/>.
    /// <note type="tip">The automatically generated base name can be changed in the .csproj file. If you want to change the base name to <c>MyResources</c>, then
    /// follow the steps below:
    /// <list type="number">
    /// <item>In Solution Explorer right click on <c>ConsoleApp1</c>, Unload Project</item>
    /// <item>In Solution Explorer right click on <c>ConsoleApp1 (unavailable)</c>, Edit ConsoleApp1.csproj</item>
    /// <item>Search for the <c>EmbeddedResource</c> nodes and edit them as follows:
    /// <code lang="XML"><![CDATA[
    /// <EmbeddedResource Include="Resources\Resource1.resx" >
    ///   <LogicalName>MyResources.resources</LogicalName>
    /// </EmbeddedResource>
    /// <EmbeddedResource Include="Resources\Resource1.en.resx" >
    ///   <LogicalName>MyResources.en.resources</LogicalName>
    /// </EmbeddedResource>]]></code></item>
    /// <item>In Solution Explorer right click on <c>ConsoleApp1 (unavailable)</c>, Reload ConsoleApp1.csproj</item>
    /// </list></note></item>
    /// <item>Reference <c>KGySoft.CoreLibraries.dll</c> and paste the following code in <c>Program.cs</c>:</item>
    /// </list></para>
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Globalization;
    /// using KGySoft.Resources;
    /// 
    /// public class Program
    /// {
    ///     public static void Main()
    ///     {
    ///         var enUS = CultureInfo.GetCultureInfo("en-US");
    ///         var en = enUS.Parent;
    ///         var inv = en.Parent; // same as CultureInfo.InvariantCulture
    /// 
    ///         var resourceManager = new HybridResourceManager(
    ///                 baseName: "ConsoleApp1.Resources.Resource1", // Or "MyResources" if you followed the tip above.
    ///                 assembly: typeof(Program).Assembly, // This is the assembly contains the compiled resources
    ///                 explicitResXBaseFileName: null) // (optional) if not null, a different name can be specified from baseName
    ///             {
    ///                 ResXResourcesDir = "Resources", // The subfolder of .resx resources. Default is "Resources"
    ///                 Source = ResourceManagerSources.CompiledAndResX // Both types of resources are used. Default is CompiledAndResX
    ///             };
    /// 
    ///         // If no .resx file exists yet the results will come purely from compiled resources
    ///         Console.WriteLine("Results before adding some content:");
    ///         Console.WriteLine(resourceManager.GetString("String1", enUS)); // from en because en-US does not exist
    ///         Console.WriteLine(resourceManager.GetString("String1", en)); // from en
    ///         Console.WriteLine(resourceManager.GetString("String1", inv)); // from invariant
    /// 
    ///         // Adding some new content
    ///         resourceManager.SetObject("String1", "Replaced content in invariant culture", inv); // overriding existing compiled entry
    ///         resourceManager.SetObject("NewObject", 42, inv); // completely newly defined entry
    ///         resourceManager.SetObject("String1", "Test string in lately added American English resource file", enUS);
    /// 
    ///         Console.WriteLine();
    ///         Console.WriteLine("Results after adding some content:");
    ///         Console.WriteLine(resourceManager.GetString("String1", enUS)); // from en-US resx
    ///         Console.WriteLine(resourceManager.GetString("String1", en)); // from compiled en
    ///         Console.WriteLine(resourceManager.GetString("String1", inv)); // from invariant .resx
    /// 
    ///         // Removing works for .resx content. Removing an overridden entry resets the original compiled content.
    ///         resourceManager.RemoveObject("String1", inv);
    /// 
    ///         // But by setting null explicitly even a compiled value can be suppressed.
    ///         // By removing the null entry the compiled content will re-appear
    ///         resourceManager.SetObject("String1", null, en);
    /// 
    ///         Console.WriteLine();
    ///         Console.WriteLine("Results after deleting some content:");
    ///         Console.WriteLine(resourceManager.GetString("String1", enUS)); // from en-US .resx
    ///         Console.WriteLine(resourceManager.GetString("String1", en)); // from invariant because en is overridden by null
    ///         Console.WriteLine(resourceManager.GetString("String1", inv)); // from original invariant because .resx is removed
    /// 
    ///         // By saving the resources the changes above can be persisted
    ///         // resourceManager.SaveAllResources();
    ///     }
    /// }
    /// 
    /// // A possible result of the example above (depending on the created resource files and the added content)
    /// // Results before adding some content:
    /// // Test string in compiled en resource
    /// // Test string in compiled en resource
    /// // Test string in compiled invariant resource
    /// //
    /// // Results after adding some content:
    /// // Test string in lately added American English resource file
    /// // Test string in compiled en resource
    /// // Replaced content in invariant culture
    /// // 
    /// // Results after deleting some content:
    /// // Test string in lately added American English resource file
    /// // Test string in compiled invariant resource
    /// // Test string in compiled invariant resource]]></code>
    /// <para>Considering there are .resx files in the background not just <see cref="string"/> and other <see cref="object"/> resources
    /// can be obtained by <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see> and <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> methods
    /// but metadata as well by <see cref="GetMetaString">GetMetaString</see> and <see cref="GetMetaObject">GetMetaObject</see> methods. Please note that accessing aliases are not exposed
    /// by the <see cref="HybridResourceManager"/> class, but you can still access them via the <see cref="IExpandoResourceSet"/> type returned by the <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> method.
    /// <note>Please note that unlike in case of <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see> and <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> methods,
    /// there is no falling back to the parent cultures (as seen in the example above) for metadata accessed by the <see cref="GetMetaString">GetMetaString</see> and <see cref="GetMetaObject">GetMetaObject</see> methods.</note></para>
    /// <h1 class="heading">Safety<a name="safety">&#160;</a></h1>
    /// <para>Similarly to <see cref="ResXResourceSet"/>, <see cref="ResXResourceReader"/> and <see cref="ResXResourceManager"/>, the <see cref="HybridResourceManager"/>
    /// class also has a <see cref="SafeMode"/> property, which changes the behavior of <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see>/<see cref="GetMetaString">GetMetaString</see>
    /// and <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see>/<see cref="GetMetaObject">GetMetaObject</see>
    /// methods:
    /// <list type="bullet">
    /// <item>If the <see cref="SafeMode"/> property is <see langword="true"/>, and the result is from a .resx resource, then the return value
    /// of <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> and <see cref="GetMetaObject">GetMetaObject</see>
    /// methods is a <see cref="ResXDataNode"/> rather than the resource or metadata value.
    /// This makes possible to check the raw .resx content before deserialization if the .resx file is from an untrusted source.
    /// The actual value can be obtained by the <see cref="ResXDataNode.GetValue">ResXDataNode.GetValue</see> or <see cref="ResXDataNode.GetValueSafe">ResXDataNode.GetValueSafe</see> method.
    /// See also the third example at the <see cref="ResXResourceSet"/> class.</item>
    /// <item>If the <see cref="SafeMode"/> property is <see langword="true"/>, then <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see>
    /// and <see cref="GetMetaString">GetMetaString</see> methods will not throw an <see cref="InvalidOperationException"/> even for non-string entries.
    /// For non-string values the raw XML string value will be returned for resources from a .resx source and the result of the <see cref="Object.ToString">ToString</see> method
    /// for resources from a compiled source.</item>
    /// <item>If the <see cref="SafeMode"/> property is <see langword="true"/>, then <see cref="O:KGySoft.Resources.HybridResourceManager.GetStream">GetStream</see>
    /// and <see cref="GetMetaStream">GetMetaStream</see> methods will not throw an <see cref="InvalidOperationException"/>.
    /// For values, which are neither <see cref="MemoryStream"/> nor <see cref="Array">byte[]</see> instances, they return a stream wrapper for the same string value
    /// that is returned by the <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see>/<see cref="GetMetaString">GetMetaString</see> methods.</item>
    /// </list>
    /// <note type="security">Even if <see cref="SafeMode"/> is <see langword="false"/>, loading a .resx content with corrupt or malicious entry
    /// will have no effect until we try to obtain the corresponding value. See the last example at <see cref="ResXResourceSet"/> for the demonstration
    /// and the example at <see cref="ResXDataNode"/> to see what members can be checked in safe mode.
    /// </note></para>
    /// <h1 class="heading">Comparison with ResourceManager<a name="comparison">&#160;</a></h1>
    /// <para>While <see cref="ResourceManager"/> is read-only and works on binary resources, <see cref="HybridResourceManager"/> supports expansion (see <see cref="IExpandoResourceManager"/>)
    /// and works both on binary and XML resources (.resx files).</para>
    /// <para><strong>Incompatibility</strong> with <see cref="ResourceManager"/>:
    /// <list type="bullet">
    /// <item>There is no constructor where the type of the resource sets can be specified. As <see cref="HybridResourceManager"/> works with multiple resource types
    /// you must not rely on the value of the <see cref="ResourceSetType"/> property.</item>
    /// <item>If <see cref="ResourceManager.GetResourceSet">ResourceManager.GetResourceSet</see> method is called with <c>createIfNotExists = false</c> for a culture,
    /// which has a corresponding but not loaded resource file, then a resource set for a parent culture might be cached and on successive calls that cached parent set will be
    /// returned even if the <c>createIfNotExists</c> argument is <see langword="true"/>. In <see cref="HybridResourceManager"/> the corresponding argument of
    /// the <see cref="GetResourceSet">GetResourceSet</see> method has been renamed to <c>loadIfExists</c> and works as expected.</item>
    /// <item>The <see cref="O:KGySoft.Resources.HybridResourceManager.GetStream">GetStream</see> methods have <see cref="MemoryStream"/> return type instead of <see cref="UnmanagedMemoryStream"/> and they can be used also for <see cref="Array">byte[]</see> values.</item>
    /// </list></para>
    /// <para><strong>New features and improvements</strong> compared to <see cref="ResourceManager"/>:
    /// <list type="bullet">
    /// <item><term>Write support</term>
    /// <description>The stored content can be expanded or existing entries can be replaced (see <see cref="SetObject">SetObject</see>/<see cref="SetMetaObject">SetMetaObject</see>),
    /// the entries can be removed (see <see cref="RemoveObject">RemoveObject</see>/<see cref="RemoveMetaObject">RemoveMetaObject</see>),
    /// and the new content can be saved (see <see cref="SaveAllResources">SaveAllResources</see>/<see cref="SaveResourceSet">SaveResourceSet</see>).
    /// You can start even with pure compiled resources, add new content during runtime and save the changes (see the example above).</description></item>
    /// <item><term>Security</term>
    /// <description>During the initialization of <see cref="HybridResourceManager"/> and loading of a resource set from a .resx file no object is deserialized even if <see cref="SafeMode"/>
    /// property is <see langword="false"/>. Objects are deserialized only when they are accessed (see <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see>/<see cref="GetMetaObject">GetMetaObject</see>).
    /// If <see cref="SafeMode"/> is <see langword="true"/>, then security is even more increased because the <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> and <see cref="GetMetaObject">GetMetaObject</see> methods
    /// return a <see cref="ResXDataNode"/> instance instead of a deserialized object so you can check whether the resource or metadata
    /// can be treat as a safe object before actually deserializing it. See the <a href="#safety">Safety</a> section above for more details.</description></item>
    /// <item><term>Disposal</term>
    /// <description>As <see cref="ResourceSet"/> implementations are disposable objects, <see cref="HybridResourceManager"/> itself implements
    /// the <see cref="IDisposable"/> interface as well.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <seealso cref="LanguageSettings"/>
    /// <seealso cref="ResXDataNode"/>
    /// <seealso cref="ResXFileRef"/>
    /// <seealso cref="ResXResourceReader"/>
    /// <seealso cref="ResXResourceWriter"/>
    /// <seealso cref="ResXResourceSet"/>
    /// <seealso cref="ResXResourceManager"/>
    /// <seealso cref="DynamicResourceManager"/>
    [Serializable]
    public class HybridResourceManager : ResourceManager, IExpandoResourceManager
    {
        #region Nested Types

        #region ProxyResourceSet class

        /// <summary>
        /// Represents a cached resource set for a child culture, which might be replaced later.
        /// </summary>
        private sealed class ProxyResourceSet : ResourceSet
        {
            #region Constants

            private const string errorMessage = "This operation is invalid on a ProxyResourceSet";

            #endregion

            #region Fields

            private bool hierarchyLoaded;

            #endregion

            #region Properties

            /// <summary>
            /// Gets the wrapped resource set. This is always a parent of the represented resource set.
            /// </summary>
            internal ResourceSet WrappedResourceSet { get; }

            /// <summary>
            /// Gets the culture of the wrapped resource set
            /// </summary>
            internal CultureInfo WrappedCulture { get; }

            internal bool HierarchyLoaded
            {
                get
                {
                    lock (this)
                        return hierarchyLoaded;
                }
                set
                {
                    lock (this)
                        hierarchyLoaded = value;
                }
            }

            #endregion

            #region Constructors

            internal ProxyResourceSet(ResourceSet toWrap, CultureInfo wrappedCulture, bool hierarchyLoaded)
            {
                WrappedResourceSet = toWrap;
                WrappedCulture = wrappedCulture;
                this.hierarchyLoaded = hierarchyLoaded;
            }

            #endregion

            #region Methods

            public override object GetObject(string name, bool ignoreCase) => Throw.InternalError<object>(errorMessage);
            public override object GetObject(string name) => Throw.InternalError<object>(errorMessage);
            public override string GetString(string name) => Throw.InternalError<string>(errorMessage);
            public override string GetString(string name, bool ignoreCase) => Throw.InternalError<string>(errorMessage);
            public override IDictionaryEnumerator GetEnumerator() => Throw.InternalError<IDictionaryEnumerator>(errorMessage);

            #endregion
        }

        #endregion

        #region InternalGetResourceSetContext struct

        private struct InternalGetResourceSetContext
        {
            #region Fields

            internal CultureInfo Culture;
            internal ResourceSetRetrieval Behavior;
            internal bool TryParents;
            internal bool ForceExpandoResult;

            internal ResourceSet? Result;
            internal bool ResourceFound;
            internal ProxyResourceSet? Proxy;
            internal CultureInfo? FoundCultureToAdd;
            internal CultureInfo? FoundProxyCulture;
            internal ResourceFallbackManager? FallbackManager;

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        private readonly ResXResourceManager resxResources; // used as sync obj as well because this reference lives along with parent lifetime and is invisible from outside
        private readonly CultureInfo neutralResourcesCulture;

        private ResourceManagerSources source = ResourceManagerSources.CompiledAndResX;
        private bool throwException = true;

        [NonSerialized]private StringKeyedDictionary<ResourceSet>? resourceSets;

        /// <summary>
        /// The lastly used resource set. Unlike in base, this is not necessarily the resource set in which a result
        /// has been found but the resource set was requested last time. In cases there are different this method performs usually better.
        /// </summary>
        [NonSerialized]private KeyValuePair<string, ResourceSet?> lastUsedResourceSet;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets whether a <see cref="MissingManifestResourceException"/> should be thrown when a resource
        /// .resx file or compiled manifest is not found even for the neutral culture.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public bool ThrowException
        {
            get => throwException;
            set
            {
                if (value == throwException)
                    return;

                lock (SyncRoot)
                {
                    resxResources.ThrowException = value;
                    throwException = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether .resx file errors should be ignored when attempting to load a resource set. If <see langword="true"/>,
        /// then non-loadable resource sets are considered as missing ones; otherwise, an exception is thrown.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public bool IgnoreResXParseErrors
        {
            get => resxResources.IgnoreResXParseErrors;
            set => resxResources.IgnoreResXParseErrors = value;
        }

        /// <summary>
        /// Gets or sets the relative path to .resx resource files.
        /// <br/>Default value: <c>Resources</c>
        /// </summary>
        [AllowNull]
        public string ResXResourcesDir
        {
            get => resxResources.ResXResourcesDir;
            set => resxResources.ResXResourcesDir = value;
        }

        /// <summary>
        /// Gets the type of the resource set object that the <see cref="HybridResourceManager"/> uses to create a resource set for the binary resources.
        /// Please note that for .resx based resources other types can be created as well.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Type ResourceSetType => base.ResourceSetType;

        /// <summary>
        /// Gets or sets the source, from which the resources should be taken.
        /// <br/>Default value: <see cref="ResourceManagerSources.CompiledAndResX"/>
        /// </summary>
        public virtual ResourceManagerSources Source
        {
            get => source;
            set
            {
                if (value == source)
                    return;

                if (!value.IsDefined())
                    Throw.EnumArgumentOutOfRange(Argument.value, value);

                SetSource(value);
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the resource manager allows case-insensitive resource lookups in the
        /// <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see>/<see cref="GetMetaString">GetMetaString</see>
        /// and <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see>/<see cref="GetMetaObject">GetMetaObject</see> methods.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        public override bool IgnoreCase
        {
            get => base.IgnoreCase;
            set
            {
                if (value == base.IgnoreCase)
                    return;

                lock (SyncRoot)
                {
                    resxResources.IgnoreCase = value;
                    base.IgnoreCase = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the <see cref="HybridResourceManager"/> works in safe mode. In safe mode the retrieved
        /// objects returned from .resx sources are not deserialized automatically.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>When <see cref="SafeMode"/> is <see langword="true"/>, then <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> and <see cref="GetMetaObject">GetMetaObject</see> methods
        /// return <see cref="ResXDataNode"/> instances instead of deserialized objects, if they are returned from .resx resource. You can retrieve the deserialized
        /// objects by calling the <see cref="ResXDataNode.GetValue">ResXDataNode.GetValue</see> or <see cref="ResXDataNode.GetValueSafe">ResXDataNode.GetValueSafe</see> method.</para>
        /// <para>When <see cref="SafeMode"/> is <see langword="true"/>, then <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see> and <see cref="GetMetaString">GetMetaString</see> methods
        /// will return a <see cref="string"/> also for non-string objects.
        /// For non-string values the raw XML string value will be returned for resources from a .resx source and the result of the <see cref="Object.ToString">ToString</see> method
        /// for resources from a compiled source.</para>
        /// <para>When <see cref="SafeMode"/> is <see langword="true"/>, then <see cref="O:KGySoft.Resources.HybridResourceManager.GetStream">GetStream</see> and <see cref="GetMetaStream">GetMetaStream</see> methods
        /// will return a <see cref="MemoryStream"/> for any object.
        /// For values, which are neither <see cref="MemoryStream"/>, nor <see cref="Array">byte[]</see> instances these methods return a stream wrapper for the same string value
        /// that is returned by the <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see>/<see cref="GetMetaString">GetMetaString</see> methods.</para>
        /// </remarks>
        /// <seealso cref="ResXResourceReader.SafeMode"/>
        /// <seealso cref="ResXResourceManager.SafeMode"/>
        /// <seealso cref="ResXResourceSet.SafeMode"/>
        public bool SafeMode
        {
            get => resxResources.SafeMode;
            set => resxResources.SafeMode = value;
        }

        /// <summary>
        /// Gets or sets whether <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> and <see cref="GetMetaObject">GetMetaObject</see> methods return always a new copy of the stored values.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>To be compatible with <a href="https://docs.microsoft.com/en-us/dotnet/api/System.Resources.ResourceManager" target="_blank">System.Resources.ResourceManager</a> this
        /// property is <see langword="true"/>&#160;by default. If this <see cref="HybridResourceManager"/> contains no mutable values or it is known that modifying values is not
        /// an issue, then this property can be set to <see langword="false"/>&#160;for better performance.</para>
        /// <para>String values are not cloned.</para>
        /// <para>The value of this property affects only the objects returned from .resx sources. Non-string values from compiled sources are always cloned.</para>
        /// </remarks>
        public bool CloneValues
        {
            get => resxResources.CloneValues;
            set => resxResources.CloneValues = value;
        }

        /// <summary>
        /// Gets whether this <see cref="HybridResourceManager"/> instance has modified and unsaved data.
        /// </summary>
        public bool IsModified => source != ResourceManagerSources.CompiledOnly && resxResources.IsModified;

        /// <summary>
        /// Gets whether this <see cref="HybridResourceManager"/> instance is disposed.
        /// </summary>
        public bool IsDisposed => resxResources.IsDisposed;

        #endregion

        #region Internal Properties

        internal object SyncRoot => resxResources;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets the <see cref="CultureInfo"/> that is specified as neutral culture in the <see cref="Assembly"/>
        /// used to initialized this instance, or the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> if no such culture is defined.
        /// </summary>
        protected CultureInfo NeutralResourcesCulture => neutralResourcesCulture;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="HybridResourceManager"/> class that looks up resources in
        /// compiled assemblies and resource XML files based on information from the specified <paramref name="baseName"/>
        /// and <paramref name="assembly"/>.
        /// </summary>
        /// <param name="baseName">A root name that is the prefix of the resource files without the extension.</param>
        /// <param name="assembly">The main assembly for the resources. The compiled resources will be searched in this assembly.</param>
        /// <param name="explicitResXBaseFileName">When <see langword="null"/>&#160;the .resx file name will be constructed based on the
        /// <paramref name="baseName"/> parameter; otherwise, the given <see cref="string"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        public HybridResourceManager(string baseName, Assembly assembly, string? explicitResXBaseFileName = null)
            : base(baseName, assembly)
        {
            // base will set MainAssembly and BaseNameField directly
            resxResources = new ResXResourceManager(explicitResXBaseFileName ?? baseName, assembly);
            neutralResourcesCulture = GetNeutralResourcesLanguage(assembly);
        }

        /// <summary>
        /// Creates a new instance of <see cref="HybridResourceManager"/> class that looks up resources in
        /// compiled assemblies and resource XML files based on information from the specified type object.
        /// </summary>
        /// <param name="resourceSource">A type from which the resource manager derives all information for finding resource files.</param>
        /// <param name="explicitResXBaseFileName">When <see langword="null"/>&#160;the .resx file name will be constructed based on the
        /// <paramref name="resourceSource"/> parameter; otherwise, the given <see cref="string"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        public HybridResourceManager(Type resourceSource, string? explicitResXBaseFileName = null)
            : base(resourceSource)
        {
            // the base ctor will set MainAssembly and BaseNameField by the provided type
            // resx will be searched in dynamicResourcesDir\explicitResxBaseFileName[.Culture].resx
            resxResources = explicitResXBaseFileName == null
                ? new ResXResourceManager(resourceSource)
                : new ResXResourceManager(explicitResXBaseFileName, resourceSource.Assembly);
            neutralResourcesCulture = GetNeutralResourcesLanguage(resourceSource.Assembly);
        }

        #endregion

        #region Methods

        #region Static Methods

        [return:NotNullIfNotNull("rs")]
        private protected static ResourceSet? Unwrap(ResourceSet? rs)
            => rs == null ? null
                : rs is ProxyResourceSet proxy ? proxy.WrappedResourceSet
                : rs;

        private protected static bool IsProxy(ResourceSet? rs) => rs is ProxyResourceSet;

        private protected static CultureInfo GetWrappedCulture(ResourceSet proxy)
        {
            Debug.Assert(proxy is ProxyResourceSet);
            return ((ProxyResourceSet)proxy).WrappedCulture;
        }

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Returns the value of the specified string resource.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <returns>
        /// The value of the resource localized for the caller's current UI culture, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>, then instead of throwing an <see cref="InvalidOperationException"/>
        /// either the raw XML value (for resources from a .resx source) or the string representation of the object (for resources from a compiled source) will be returned for non-string resources.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="CloneValues"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160; and the type of the resource is not <see cref="string"/>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public override string? GetString(string name) => (string?)GetObjectInternal(name, null, true, CloneValues, SafeMode);

        /// <summary>
        /// Returns the value of the string resource localized for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the resource is localized. If the resource is not localized for
        /// this culture, the resource manager uses fallback rules to locate an appropriate resource. If this value is
        /// <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property.</param>
        /// <returns>
        /// The value of the resource localized for the specified <paramref name="culture"/>, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>, then instead of throwing an <see cref="InvalidOperationException"/>
        /// either the raw XML value (for resources from a .resx source) or the string representation of the object (for resources from a compiled source) will be returned for non-string resources.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="CloneValues"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160; and the type of the resource is not <see cref="string"/>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public override string? GetString(string name, CultureInfo? culture) => (string?)GetObjectInternal(name, culture, true, CloneValues, SafeMode);

        /// <summary>
        /// Returns a <see cref="MemoryStream"/> instance from the resource of the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <returns>
        /// A <see cref="MemoryStream"/> object from the specified resource localized for the caller's current UI culture, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="CloneValues"/> property, the <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> methods return either
        /// a full copy of the specified resource, or always the same instance. For memory streams none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="O:KGySoft.Resources.HybridResourceManager.GetStream">GetStream</see> methods
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="CloneValues"/> property.</para>
        /// <para><see cref="O:KGySoft.Resources.HybridResourceManager.GetStream">GetStream</see> can be used also for byte array resources. However, if the value is returned from compiled resources, then always a new copy of the byte array will be wrapped.</para>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>&#160;and <paramref name="name"/> is neither a <see cref="MemoryStream"/> nor a byte array resource, then
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns a stream wrapper for the same string value that is returned by the <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see> method,
        /// which will be the raw XML content for non-string resources.</para>
        /// <note>The internal buffer is tried to be obtained by reflection in the first place. On platforms, which have possibly unknown non-public member names the public APIs are used, which may copy the content in memory.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160;and the type of the resource is neither <see cref="MemoryStream"/> nor <see cref="Array">byte[]</see>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public new virtual MemoryStream? GetStream(string name) => GetStream(name, null);

        /// <summary>
        /// Returns a <see cref="MemoryStream"/> instance from the resource of the specified <paramref name="name"/> and <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the resource is localized. If the resource is not localized for
        /// this culture, the resource manager uses fallback rules to locate an appropriate resource. If this value is
        /// <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property.</param>
        /// <returns>
        /// A <see cref="MemoryStream"/> object from the specified resource localized for the specified <paramref name="culture"/>, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="CloneValues"/> property, the <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> methods return either
        /// a full copy of the specified resource, or always the same instance. For memory streams none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="O:KGySoft.Resources.HybridResourceManager.GetStream">GetStream</see> methods
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="CloneValues"/> property.</para>
        /// <para><see cref="O:KGySoft.Resources.HybridResourceManager.GetStream">GetStream</see> can be used also for byte array resources. However, if the value is returned from compiled resources, then always a new copy of the byte array will be wrapped.</para>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>&#160;and <paramref name="name"/> is neither a <see cref="MemoryStream"/> nor a byte array resource, then
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns a stream wrapper for the same string value that is returned by the <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see> method,
        /// which will be the raw XML content for non-string resources.</para>
        /// <note>The internal buffer is tried to be obtained by reflection in the first place. On platforms, which have possibly unknown non-public member names the public APIs are used, which may copy the content in memory.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160;and the type of the resource is neither <see cref="MemoryStream"/> nor <see cref="Array">byte[]</see>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public new virtual MemoryStream? GetStream(string name, CultureInfo? culture)
        {
            bool safeMode = SafeMode;
            object? value = GetObjectInternal(name, culture, false, false, safeMode);
            return ResXCommon.ToMemoryStream(name, value, safeMode);
        }

        /// <summary>
        /// Returns the value of the specified resource.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <returns>
        /// If <see cref="SafeMode"/> is <see langword="true"/>, and the resource is from a .resx content, then the method returns a <see cref="ResXDataNode"/> instance instead of the actual deserialized value.
        /// Otherwise, returns the value of the resource localized for the caller's current UI culture, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="CloneValues"/> property, the <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> methods return either
        /// a full copy of the specified resource, or always the same instance. For memory streams and byte arrays none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="O:KGySoft.Resources.HybridResourceManager.GetStream">GetStream</see> methods
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="CloneValues"/> property.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="CloneValues"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public override object? GetObject(string name) => GetObjectInternal(name, null, false, CloneValues, SafeMode);

        /// <summary>
        /// Gets the value of the specified resource localized for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <param name="culture">The culture for which the resource is localized. If the resource is not localized for
        /// this culture, the resource manager uses fallback rules to locate an appropriate resource. If this value is
        /// <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property.</param>
        /// <returns>
        /// If <see cref="SafeMode"/> is <see langword="true"/>, and the resource is from a .resx content, then the method returns a <see cref="ResXDataNode"/> instance instead of the actual deserialized value.
        /// Otherwise, returns the value of the resource localized for the specified <paramref name="culture"/>, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="CloneValues"/> property, the <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> methods return either
        /// a full copy of the specified resource, or always the same instance. For memory streams and byte arrays none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="O:KGySoft.Resources.HybridResourceManager.GetStream">GetStream</see> methods
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="CloneValues"/> property.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="CloneValues"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public override object? GetObject(string name, CultureInfo? culture) => GetObjectInternal(name, culture, false, CloneValues, SafeMode);

        /// <summary>
        /// Retrieves the resource set for a particular culture.
        /// </summary>
        /// <param name="culture">The culture whose resources are to be retrieved.</param>
        /// <param name="loadIfExists"><see langword="true"/>&#160;to load the resource set, if it has not been loaded yet and the corresponding resource file exists; otherwise, <see langword="false"/>.</param>
        /// <param name="tryParents"><see langword="true"/>&#160;to use resource fallback to load an appropriate resource if the resource set cannot be found; <see langword="false"/>&#160;to bypass the resource fallback process.</param>
        /// <returns>The resource set for the specified culture.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException"><paramref name="tryParents"/> and <see cref="HybridResourceManager.ThrowException"/> are <see langword="true"/>&#160;and
        /// the resource of the neutral culture was not found.</exception>
        public override ResourceSet? GetResourceSet(CultureInfo culture, bool loadIfExists, bool tryParents)
        {
            // base implementation must not be called because it wants to open main assembly in case of invariant culture
            ResourceSet? result = Unwrap(InternalGetResourceSet(culture, loadIfExists ? ResourceSetRetrieval.LoadIfExists : ResourceSetRetrieval.GetIfAlreadyLoaded, tryParents, false));

            // These properties are never taken from the stored sets so setting them only when an IExpandoResourceSet instance is returned.
            // It does not matter if they are changed by the user.
            if (result is IExpandoResourceSet expandoRs)
            {
                expandoRs.SafeMode = SafeMode;
                expandoRs.CloneValues = CloneValues;
            }

            return result;
        }

        /// <summary>
        /// Tells the resource manager to call the <see cref="ResourceSet.Close">ResourceSet.Close</see> method on all <see cref="ResourceSet"/> objects and release all resources.
        /// All unsaved resources will be lost.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <remarks>
        /// <note type="caution">By calling this method all of the unsaved changes will be lost.</note>
        /// <para>By the <see cref="IsModified"/> property you can check whether there are unsaved changes.</para>
        /// <para>To save the changes you can call the <see cref="SaveAllResources">SaveAllResources</see> method.</para>
        /// </remarks>
        public override void ReleaseAllResources()
        {
            // we are in lock; otherwise, the nullification could occur while we access the resourceSets anywhere else
            lock (SyncRoot)
            {
                resourceSets = null;
                lastUsedResourceSet = default;
                resxResources.ReleaseAllResources();
                base.ReleaseAllResources();
            }
        }

        /// <summary>
        /// Returns the value of the string metadata for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the metadata to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the metadata should be returned.
        /// If this value is <see langword="null" />, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture" /> property.
        /// Unlike in case of <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see> method, no fallback is used if the metadata is not found in the specified culture. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>
        /// The value of the metadata of the specified culture, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>&#160;and <paramref name="name"/> is a non-<see langword="string"/> metadata, then
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns the underlying raw XML content of the metadata.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160;and the type of the metadata is not <see cref="string"/>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public virtual string? GetMetaString(string name, CultureInfo? culture = null) => (string?)GetMetaInternal(name, culture, true, CloneValues);

        /// <summary>
        /// Returns a <see cref="MemoryStream"/> instance from the metadata of the specified <paramref name="name"/> and <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the metadata to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the metadata should be returned.
        /// If this value is <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property.
        /// Unlike in case of <see cref="O:KGySoft.Resources.HybridResourceManager.GetStream">GetStream</see> methods, no fallback is used if the metadata is not found in the specified culture. This parameter is optional.
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
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns a stream wrapper for the same string value that is returned by the <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see> methods,
        /// which will be the raw XML content for non-string metadata.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160;and the type of the metadata is neither <see cref="MemoryStream"/> nor <see cref="Array">byte[]</see>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public MemoryStream? GetMetaStream(string name, CultureInfo? culture = null)
        {
            object? value = GetMetaInternal(name, culture, false, false);
            return ResXCommon.ToMemoryStream(name, value, SafeMode);
        }

        /// <summary>
        /// Returns the value of the specified non-string metadata for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the metadata to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the metadata should be returned.
        /// If this value is <see langword="null" />, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property.
        /// Unlike in case of <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> method, no fallback is used if the metadata is not found in the specified culture. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>
        /// If <see cref="SafeMode"/> is <see langword="true"/>, then the method returns a <see cref="ResXDataNode"/> instance instead of the actual deserialized value.
        /// Otherwise, returns the value of the metadata localized for the specified <paramref name="culture"/>, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public virtual object? GetMetaObject(string name, CultureInfo? culture = null) => GetMetaInternal(name, culture, false, CloneValues);

        /// <summary>
        /// Retrieves the resource set for a particular culture, which can be dynamically modified.
        /// </summary>
        /// <param name="culture">The culture whose resources are to be retrieved.</param>
        /// <param name="behavior">Determines the retrieval behavior of the result <see cref="IExpandoResourceSet"/>. This parameter is optional.
        /// <br/>Default value: <see cref="ResourceSetRetrieval.LoadIfExists"/>.</param>
        /// <param name="tryParents"><see langword="true"/>&#160;to use resource fallback to load an appropriate resource if the resource set cannot be found; <see langword="false"/>&#160;to bypass the resource fallback process. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>The resource set for the specified culture, or <see langword="null"/>&#160;if the specified culture cannot be retrieved by the defined <paramref name="behavior"/>,
        /// or when <see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/> so it cannot return an <see cref="IExpandoResourceSet"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="behavior"/> does not fall in the expected range.</exception>
        /// <exception cref="MissingManifestResourceException">Resource file of the neutral culture was not found, while <paramref name="tryParents"/> is <see langword="true"/>
        /// and <paramref name="behavior"/> is not <see cref="ResourceSetRetrieval.CreateIfNotExists"/>.</exception>
        public virtual IExpandoResourceSet? GetExpandoResourceSet(CultureInfo culture, ResourceSetRetrieval behavior = ResourceSetRetrieval.LoadIfExists, bool tryParents = false)
        {
            if (!Enum<ResourceSetRetrieval>.IsDefined(behavior))
                Throw.EnumArgumentOutOfRange(Argument.behavior, behavior);

            IExpandoResourceSet? result = Unwrap(InternalGetResourceSet(culture, behavior, tryParents, true)) as IExpandoResourceSet;

            // These properties are never taken from the stored sets so setting them only when the user obtains an IExpandoResourceSet instance.
            // It does not matter if they are changed by the user.
            if (result != null)
            {
                result.SafeMode = SafeMode;
                result.CloneValues = CloneValues;
            }

            return result;
        }

        /// <summary>
        /// Adds or replaces a resource object in the current <see cref="HybridResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The name of the resource to set.</param>
        /// <param name="culture">The culture of the resource to set. If this value is <see langword="null"/>,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property.</param>
        /// <param name="value">The value of the resource to set. If <see langword="null"/>, then a null reference will be explicitly
        /// stored for the specified <paramref name="culture"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>If <paramref name="value" /> is <see langword="null" />, a null reference will be explicitly stored.
        /// As a result, the subsequent <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> calls
        /// with the same <paramref name="culture" /> will fall back to the parent culture, or will return <see langword="null"/>&#160;if
        /// <paramref name="name" /> is not found in any parent cultures. However, enumerating the result set returned by
        /// <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> and <see cref="GetResourceSet">GetResourceSet</see> methods will return the resources with <see langword="null"/>&#160;value.</para>
        /// <para>If you want to remove the user-defined ResX content and reset the original resource defined in the binary resource set (if any), use the <see cref="RemoveObject">RemoveObject</see> method.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        public virtual void SetObject(string name, object? value, CultureInfo? culture = null)
        {
            if (source == ResourceManagerSources.CompiledOnly)
                Throw.InvalidOperationException(Res.ResourcesHybridResSourceBinary);

            // because of create no proxy is returned
            IExpandoResourceSet rs = (IExpandoResourceSet)InternalGetResourceSet(culture ?? CultureInfo.CurrentUICulture, ResourceSetRetrieval.CreateIfNotExists, false, true)!;
            rs.SetObject(name, value);
        }

        /// <summary>
        /// Removes a resource object from the current <see cref="HybridResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The case-sensitive name of the resource to remove.</param>
        /// <param name="culture">The culture of the resource to remove. If this value is <see langword="null"/>,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>If there is a binary resource defined for <paramref name="name" /> and <paramref name="culture" />,
        /// then after this call the originally defined value will be returned by <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> method from the binary resources.
        /// If you want to force hiding the binary resource and make <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> to default to the parent <see cref="CultureInfo" /> of the specified <paramref name="culture" />,
        /// then use the <see cref="SetObject">SetObject</see> method with a <see langword="null"/>&#160;value.</para>
        /// <para><paramref name="name"/> is considered as case-sensitive. If <paramref name="name"/> occurs multiple times
        /// in the resource set in case-insensitive manner, they can be removed one by one only.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        public virtual void RemoveObject(string name, CultureInfo? culture = null)
        {
            if (source == ResourceManagerSources.CompiledOnly)
                Throw.InvalidOperationException(Res.ResourcesHybridResSourceBinary);

            // forcing expando result is not needed because there is nothing to remove from compiled resources
            IExpandoResourceSet? rs = Unwrap(InternalGetResourceSet(culture ?? CultureInfo.CurrentUICulture, ResourceSetRetrieval.LoadIfExists, false, false)) as IExpandoResourceSet;
            rs?.RemoveObject(name);
        }

        /// <summary>
        /// Adds or replaces a metadata object in the current <see cref="HybridResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The name of the metadata to set.</param>
        /// <param name="value">The value of the metadata to set. If <see langword="null" />,  then a null reference will be explicitly
        /// stored for the specified <paramref name="culture" />.</param>
        /// <param name="culture">The culture of the metadata to set.
        /// If this value is <see langword="null" />, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// If <paramref name="value" /> is <see langword="null" />, a null reference will be explicitly stored.
        /// Its effect is similar to the <see cref="RemoveMetaObject">RemoveMetaObject</see> method: the subsequent <see cref="GetMetaObject">GetMetaObject</see> calls
        /// with the same <paramref name="culture" /> will return <see langword="null" />.
        /// However, enumerating the result set returned by <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> method will return the meta objects with <see langword="null"/>&#160;value.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        public virtual void SetMetaObject(string name, object? value, CultureInfo? culture = null)
        {
            if (source == ResourceManagerSources.CompiledOnly)
                Throw.InvalidOperationException(Res.ResourcesHybridResSourceBinary);

            // because of create no proxy is returned
            IExpandoResourceSet rs = (IExpandoResourceSet)InternalGetResourceSet(culture ?? CultureInfo.InvariantCulture, ResourceSetRetrieval.CreateIfNotExists, false, true)!;
            rs.SetMetaObject(name, value);
        }

        /// <summary>
        /// Removes a metadata object from the current <see cref="HybridResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The case-sensitive name of the metadata to remove.</param>
        /// <param name="culture">The culture of the metadata to remove.
        /// If this value is <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <paramref name="name" /> is considered as case-sensitive. If <paramref name="name" /> occurs multiple times
        /// in the resource set in case-insensitive manner, they can be removed one by one only.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        public virtual void RemoveMetaObject(string name, CultureInfo? culture = null)
        {
            if (source == ResourceManagerSources.CompiledOnly)
                Throw.InvalidOperationException(Res.ResourcesHybridResSourceBinary);

            // forcing expando result is not needed because there is nothing to remove from compiled resources
            IExpandoResourceSet? rs = Unwrap(InternalGetResourceSet(culture ?? CultureInfo.InvariantCulture, ResourceSetRetrieval.LoadIfExists, false, false)) as IExpandoResourceSet;
            rs?.RemoveObject(name);
        }

        /// <summary>
        /// Saves the resource set of a particular <paramref name="culture" /> if it has been already loaded.
        /// </summary>
        /// <param name="culture">The culture of the resource set to save.</param>
        /// <param name="force"><see langword="true"/>&#160;to save the resource set even if it has not been modified; <see langword="false"/>&#160;to save it only if it has been modified. This parameter is optional.
        /// <br />Default value: <see langword="false"/>.</param>
        /// <param name="compatibleFormat">If set to <see langword="true"/>, the result .resx file can be read by a <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> instance
        /// and the Visual Studio Resource Editor. If set to <see langword="false"/>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />),
        /// but the result can be read only by the <see cref="ResXResourceReader" /> class. This parameter is optional.
        /// <br />Default value: <see langword="false"/>.</param>
        /// <returns>
        /// <see langword="true"/>&#160;if the resource set of the specified <paramref name="culture" /> has been saved;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="IOException">The resource set could not be saved.</exception>
        public virtual bool SaveResourceSet(CultureInfo culture, bool force = false, bool compatibleFormat = false)
        {
            if (source == ResourceManagerSources.CompiledOnly)
                return false;

            return resxResources.SaveResourceSet(culture, force, compatibleFormat);
        }

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
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="IOException">A resource set could not be saved.</exception>
        public virtual bool SaveAllResources(bool force = false, bool compatibleFormat = false)
        {
            if (source == ResourceManagerSources.CompiledOnly)
                return false;

            return resxResources.SaveAllResources(force, compatibleFormat);
        }

        /// <summary>
        /// Disposes the resources of the current instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Provides the implementation for finding a resource set.
        /// </summary>
        /// <param name="culture">The culture object to look for.</param>
        /// <param name="loadIfExists"><see langword="true"/>&#160;to load the resource set, if it has not been loaded yet; otherwise, <see langword="false"/>.</param>
        /// <param name="tryParents"><see langword="true"/>&#160;to check parent <see cref="CultureInfo" /> objects if the resource set cannot be loaded; otherwise, <see langword="false"/>.</param>
        /// <returns>The specified resource set.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="MissingManifestResourceException">The .resx file of the neutral culture was not found, while <paramref name="tryParents"/> and <see cref="ThrowException"/> are both <see langword="true"/>.</exception>
        protected override ResourceSet? InternalGetResourceSet(CultureInfo culture, bool loadIfExists, bool tryParents)
        {
            Debug.Assert(!ReferenceEquals(Assembly.GetCallingAssembly(), Assembly.GetExecutingAssembly()), "InternalGetResourceSet is called from CoreLibraries assembly.");
            return Unwrap(InternalGetResourceSet(culture, loadIfExists ? ResourceSetRetrieval.LoadIfExists : ResourceSetRetrieval.GetIfAlreadyLoaded, tryParents, false));
        }

        /// <summary>
        /// Releases the resources used by this <see cref="HybridResourceManager"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/>&#160;if this method is being called due to a call to <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (resxResources.IsDisposed)
                return;

            if (disposing)
            {
                resxResources.Dispose();
                base.ReleaseAllResources();
            }

            resourceSets = null;
            lastUsedResourceSet = default;
        }

        #endregion

        #region Private Protected Methods

        /// <summary>
        /// Sets the source of the resources.
        /// </summary>
        private protected virtual void SetSource(ResourceManagerSources value)
        {
            lock (SyncRoot)
            {
                // nullifying the local resourceSets cache does not mean we clear the resources.
                // they will be obtained and properly merged again on next Get...
                source = value;
                resourceSets = null;
                lastUsedResourceSet = default;
            }
        }

        /// <summary>
        /// Gets whether a non-proxy resource set is present for the specified culture.
        /// </summary>
        private protected bool IsNonProxyLoaded(CultureInfo culture)
        {
            lock (SyncRoot)
                return resourceSets != null && resourceSets.TryGetValue(culture.Name, out ResourceSet? rs) && rs is not ProxyResourceSet;
        }

        /// <summary>
        /// Gets whether a proxy resource set is present for any culture.
        /// </summary>
        private protected bool IsAnyProxyLoaded()
        {
            lock (SyncRoot)
                return resourceSets?.Values.Any(v => v is ProxyResourceSet) == true;
        }

        /// <summary>
        /// Gets whether an expando resource set is present for the specified culture.
        /// </summary>
        private protected bool IsExpandoExists(CultureInfo culture)
        {
            lock (SyncRoot)
                return resourceSets != null && resourceSets.TryGetValue(culture.Name, out ResourceSet? rs) && rs is IExpandoResourceSet;
        }

        private protected ResourceSet? TryGetFromCachedResourceSet(string name, CultureInfo culture, bool isString, bool cloneValue, bool safeMode, out object? value)
        {
            ResourceSet? cachedRs = GetFirstResourceSet(culture);
            if (cachedRs == null)
            {
                value = null;
                return null;
            }

            value = GetResourceFromAny(cachedRs, name, isString, cloneValue, safeMode);
            return cachedRs;
        }

        /// <summary>
        /// Updates last used ResourceSet
        /// </summary>
        private protected void SetCache(CultureInfo culture, ResourceSet? rs)
        {
            lock (SyncRoot)
                lastUsedResourceSet = new KeyValuePair<string, ResourceSet?>(culture.Name, rs);
        }

        private protected object? GetResourceFromAny(ResourceSet rs, string name, bool isString, bool cloneValue, bool safeMode)
        {
            ResourceSet realRs = Unwrap(rs);
            object? result = realRs is IExpandoResourceSetInternal expandoRs
                ? expandoRs.GetResource(name, IgnoreCase, isString, safeMode, cloneValue)
                : realRs.GetObject(name, IgnoreCase);

            if (result == null)
                return null;

            if (!isString)
                return result is UnmanagedMemoryStream ums ? new UnmanagedMemoryStreamWrapper(ums) : result;

            if (result is string)
                return result;
            if (safeMode)
                return result.ToString();

            Throw.InvalidOperationException(Res.ResourcesNonStringResourceWithType(name, result.GetType().GetName(TypeNameKind.LongName)));
            return null;
        }

        /// <summary>
        /// Gets whether a cached proxy can be accepted as a result.
        /// </summary>
        /// <param name="proxy">The found proxy</param>
        /// <param name="culture">The requested culture</param>
        private protected virtual bool IsCachedProxyAccepted(ResourceSet proxy, CultureInfo culture)
            // In HRM proxy is accepted only if hierarchy is loaded. This is ok because GetFirstResourceSet is called only from
            // methods, which call InternalGetResourceSet with LoadIfExists
            => ((ProxyResourceSet)proxy).HierarchyLoaded;

        /// <summary>
        /// Warning: It CAN return a proxy
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
        private protected ResourceSet? InternalGetResourceSet(CultureInfo culture, ResourceSetRetrieval behavior, bool tryParents, bool forceExpandoResult)
        {
            #region Local Methods to reduce complexity

            bool TryGetCachedResourceSet(ref InternalGetResourceSetContext ctx)
            {
                lock (SyncRoot)
                    ctx.ResourceFound = resourceSets?.TryGetValue(ctx.Culture.Name, out ctx.Result) == true;

                if (ctx.ResourceFound)
                {
                    // returning the cached resource if that is not a proxy or when the proxy does not have to be (possibly) replaced
                    // if result is not a proxy but an actual resource...
                    if ((((ctx.Proxy = ctx.Result as ProxyResourceSet) == null)
                            // ...or is a proxy but nothing new should be loaded...
                            || ctx.Behavior == ResourceSetRetrieval.GetIfAlreadyLoaded
                            // ...or is a proxy but nothing new can be loaded in the hierarchy
                            || (ctx.Behavior == ResourceSetRetrieval.LoadIfExists && ctx.Proxy.HierarchyLoaded))
                        // AND not expando is requested or result (wraps) an expando.
                        // Due to the conditions above it will not return proxied expando with Create behavior.
                        && (!ctx.ForceExpandoResult || Unwrap(ctx.Result) is IExpandoResourceSet))
                    {
                        return true;
                    }
                }

                return false;
            }

            bool TryGetResourceWhileTraverse(ref InternalGetResourceSetContext ctx)
            {
                ctx.FallbackManager = new ResourceFallbackManager(ctx.Culture, NeutralResourcesCulture, ctx.TryParents);
                foreach (CultureInfo currentCultureInfo in ctx.FallbackManager)
                {
                    ctx.Proxy = null;
                    lock (SyncRoot)
                        ctx.ResourceFound = resourceSets?.TryGetValue(currentCultureInfo.Name, out ctx.Result) == true;

                    if (ctx.ResourceFound)
                    {
                        // a returnable result (non-proxy) is found in the local cache
                        ctx.Proxy = ctx.Result as ProxyResourceSet;
                        if (ctx.Proxy == null && (!ctx.ForceExpandoResult || ctx.Result is IExpandoResourceSet))
                        {
                            // since the first try above we have a result from another thread for the searched context
                            if (Equals(ctx.Culture, currentCultureInfo))
                                return true;

                            // after some proxies, a parent culture has been found: returning a proxy for this if this was the proxied culture in the children
                            if (Equals(currentCultureInfo, ctx.FoundProxyCulture))
                            {
                                // The hierarchy is now up-to-date. Creating the possible missing proxies and returning the one for the requested culture
                                lock (SyncRoot)
                                {
                                    ResourceSet toWrap = ctx.Result!;
                                    ctx.Result = null;
                                    foreach (CultureInfo updateCultureInfo in ctx.FallbackManager)
                                    {
                                        // We have found again the first context.Proxy in the hierarchy. This is now up-to-date for sure so returning.
                                        if (resourceSets!.TryGetValue(updateCultureInfo.Name, out ResourceSet? rs))
                                        {
                                            Debug.Assert(rs is ProxyResourceSet, "A context.Proxy is expected to be found here.");
                                            ctx.Result ??= rs;
                                            return true;
                                        }

                                        // There is at least one non-existing key (most specific elements in the hierarchy): new context.Proxy creation is needed
                                        ResourceSet newProxy = new ProxyResourceSet(toWrap, ctx.FoundProxyCulture, ctx.Behavior == ResourceSetRetrieval.LoadIfExists);
                                        AddResourceSet(updateCultureInfo.Name, ref newProxy);
                                        ctx.Result ??= newProxy;
                                    }
                                }
                            }

                            // otherwise, we found a parent: we need to re-create the proxies in the cache to the children
                            Debug.Assert(ctx.FoundProxyCulture == null, "There is a context.Proxy with an inconsistent parent in the hierarchy.");
                            ctx.FoundCultureToAdd = currentCultureInfo;
                            break;
                        }

                        // context.Proxy is found
                        if (ctx.Proxy != null)
                        {
                            Debug.Assert(ctx.FoundProxyCulture == null || Equals(ctx.FoundProxyCulture, ctx.Proxy.WrappedCulture), "Proxied cultures are different in the hierarchy.");
                            ctx.FoundProxyCulture ??= ctx.Proxy.WrappedCulture;

                            // if we traversing here because last time the proxy has been loaded by
                            // ResourceSetRetrieval.GetIfAlreadyLoaded, but now we load the possible parents, we set the
                            // HierarchyLoaded flag in the hierarchy. Unless no new context.Proxy is created (and thus the descendant proxies are deleted),
                            // this will prevent the redundant traversal next time.
                            if (ctx.TryParents && ctx.Behavior == ResourceSetRetrieval.LoadIfExists)
                            {
                                ctx.Proxy.HierarchyLoaded = true;
                            }
                        }

                        // if none of above, we have a non-proxy result, which must be replaced by an expando result
                    }

                    ctx.Result = null;

                    // from resx
                    ResXResourceSet? resx = null;
                    if (source != ResourceManagerSources.CompiledOnly)
                        resx = resxResources.GetResXResourceSet(currentCultureInfo, ctx.Behavior, false);

                    // from assemblies
                    ResourceSet? compiled = null;
                    if (source != ResourceManagerSources.ResXOnly)
                    {
                        // otherwise, disposed state is checked by ResXResourceSet
                        if (source == ResourceManagerSources.CompiledOnly && resxResources.IsDisposed)
                            Throw.ObjectDisposedException();
                        compiled = base.InternalGetResourceSet(currentCultureInfo, ctx.Behavior != ResourceSetRetrieval.GetIfAlreadyLoaded, false);
                    }

                    // result found
                    if (resx != null || compiled != null)
                    {
                        ctx.FoundCultureToAdd = currentCultureInfo;

                        // single result - no merge is needed, unless expando is forced and context.Result is compiled
                        if (resx == null || compiled == null)
                        {
                            ctx.Result = resx ?? compiled;

                            if (!ctx.ForceExpandoResult)
                                break;

                            if (resx == null)
                            {
                                // there IS a result, which we cannot return because it is not expando, so returning null
                                // this is ok because neither fallback nor missing manifest is needed in this case - GetExpandoResourceSet returns null instead of ResourceSet
                                if (source == ResourceManagerSources.CompiledOnly)
                                {
                                    ctx.Result = null;
                                    return true;
                                }

                                resx = resxResources.CreateResourceSet(ctx.Culture);
                            }
                        }

                        // creating a merged resource set (merge is applied only when enumerated)
                        if (compiled != null)
                            ctx.Result = new HybridResourceSet(resx, compiled);

                        break;
                    }

                    // context.Behavior is LoadIfExists, context.TryParents = false, hierarchy is not loaded, but still no loadable content has been found
                    if (!ctx.TryParents && ctx.Proxy != null)
                    {
                        Debug.Assert(ctx.Behavior == ResourceSetRetrieval.LoadIfExists && !ctx.Proxy.HierarchyLoaded);
                        ctx.Result = ctx.Proxy.WrappedResourceSet;
                        return true;
                    }
                }

                return false;
            }

            void CheckNullResult(ref InternalGetResourceSetContext ctx)
            {
                bool raiseException = ctx.Behavior != ResourceSetRetrieval.GetIfAlreadyLoaded;

                // if behavior is not GetIfAlreadyLoaded, we cannot decide whether manifest is really missing so calling the methods
                // again and ignoring any context.Result just catching the exception if any. Calling with createIfNotExists=false,
                // so no new resources will be loaded. Because of tryParents=true, exception will be thrown if manifest is missing.
                if (!raiseException)
                {
                    try
                    {
                        resxResources.GetResXResourceSet(CultureInfo.InvariantCulture, ResourceSetRetrieval.GetIfAlreadyLoaded, true);
                        base.InternalGetResourceSet(CultureInfo.InvariantCulture, false, true);
                    }
                    catch (MissingManifestResourceException)
                    {
                        raiseException = true;
                    }
                }

                if (raiseException)
                {
                    switch (source)
                    {
                        case ResourceManagerSources.CompiledOnly:
                            Throw.MissingManifestResourceException(Res.ResourcesNeutralResourceNotFoundCompiled(BaseName, MainAssembly?.Location));
                            break;
                        case ResourceManagerSources.ResXOnly:
                            Throw.MissingManifestResourceException(Res.ResourcesNeutralResourceFileNotFoundResX(resxResources.ResourceFileName));
                            break;
                        default:
                            Throw.MissingManifestResourceException(Res.ResourcesNeutralResourceNotFoundHybrid(BaseName, MainAssembly?.Location, resxResources.ResourceFileName));
                            break;
                    }
                }
            }

            #endregion

            Debug.Assert(forceExpandoResult || behavior != ResourceSetRetrieval.CreateIfNotExists, "Behavior can be CreateIfNotExists only if expando is requested.");
            if (culture == null!)
                Throw.ArgumentNullException(Argument.culture);
            if (forceExpandoResult && source == ResourceManagerSources.CompiledOnly)
                return null;

            var context = new InternalGetResourceSetContext { Culture = culture, Behavior = behavior, TryParents = tryParents, ForceExpandoResult = forceExpandoResult };
            if (TryGetCachedResourceSet(ref context))
                return context.Result;

            if (TryGetResourceWhileTraverse(ref context))
                return context.Result;

            // The code above never throws MissingManifest exception because the wrapped managers are always called with tryParents = false.
            // This block ensures compatible behavior if ThrowException is enabled.
            if (throwException && context.Result == null && tryParents)
                CheckNullResult(ref context);

            if (context.FoundCultureToAdd == null)
                return null;

            lock (SyncRoot)
            {
                ResourceSet? toReturn = null;

                // we replace a context.Proxy: we must delete proxies, which are children of the found resource.
                if (context.FoundProxyCulture != null && resourceSets != null)
                {
                    Debug.Assert(forceExpandoResult || !Equals(context.FoundProxyCulture, context.FoundCultureToAdd), "The culture to add is the same as the existing proxies, while no expando context.Result is forced.");

                    List<string> keysToRemove = resourceSets.Where(item =>
                            item.Value is ProxyResourceSet && ResXResourceManager.IsParentCulture(context.FoundCultureToAdd, item.Key))
                        .Select(item => item.Key).ToList();

                    foreach (string key in keysToRemove)
                    {
                        resourceSets.Remove(key);
                    }
                }

                // add entries to the cache for the cultures we have gone through
                foreach (CultureInfo updateCultureInfo in context.FallbackManager!)
                {
                    // stop when we've added current or reached invariant (top of chain)
                    if (ReferenceEquals(updateCultureInfo, context.FoundCultureToAdd))
                    {
                        AddResourceSet(updateCultureInfo.Name, ref context.Result!);
                        toReturn ??= context.Result;
                        break;
                    }

                    ResourceSet newProxy = new ProxyResourceSet(context.Result!, context.FoundCultureToAdd, behavior == ResourceSetRetrieval.LoadIfExists);
                    AddResourceSet(updateCultureInfo.Name, ref newProxy);
                    toReturn ??= newProxy;
                }

                return toReturn;
            }
        }

        private protected virtual object? GetObjectInternal(string name, CultureInfo? culture, bool isString, bool cloneValue, bool safeMode)
        {
            if (name == null!)
                Throw.ArgumentNullException(Argument.name);

            culture ??= CultureInfo.CurrentUICulture;
            ResourceSet? seen = Unwrap(TryGetFromCachedResourceSet(name, culture, isString, cloneValue, safeMode, out object? value));

            // There is a result, or a stored null is returned from invariant
            if (value != null
                || (seen != null
                    && (Equals(culture, CultureInfo.InvariantCulture) || seen is ProxyResourceSet && GetWrappedCulture(seen).Equals(CultureInfo.InvariantCulture))
                    && (Unwrap(seen) as IExpandoResourceSet)?.ContainsResource(name, IgnoreCase) == true))
            {
                return value;
            }

            // The InternalGetResourceSet has also a hierarchy traversal. This outer traversal is required as well because
            // the inner one can return an existing resource set without the searched resource, in which case here is
            // the fallback to the parent resource.
            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, NeutralResourcesCulture, true);
            ResourceSet? toCache = null;
            foreach (CultureInfo currentCulture in mgr)
            {
                ResourceSet? rs = InternalGetResourceSet(currentCulture, ResourceSetRetrieval.LoadIfExists, true, false);
                if (rs == null)
                    return null;

                // we have already checked this resource
                ResourceSet unwrapped = Unwrap(rs);
                if (unwrapped == seen)
                    continue;

                toCache ??= rs;
                value = GetResourceFromAny(unwrapped, name, isString, cloneValue, safeMode);
                if (value != null)
                {
                    lock (SyncRoot)
                        lastUsedResourceSet = new KeyValuePair<string, ResourceSet?>(culture.Name, toCache);
                    return value;
                }

                seen = unwrapped;
            }

            return null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Tries to get the first resource set in the traversal hierarchy,
        /// so the resource set for the culture itself.
        /// Warning: it CAN return a proxy
        /// </summary>
        private ResourceSet? GetFirstResourceSet(CultureInfo culture)
        {
            lock (SyncRoot)
            {
                ResourceSet? rs;
                ProxyResourceSet? proxy;
                if (culture.Name == lastUsedResourceSet.Key)
                {
                    proxy = (rs = lastUsedResourceSet.Value) as ProxyResourceSet;
                    if (proxy == null)
                        return rs;

                    if (IsCachedProxyAccepted(proxy, culture))
                        return rs;

                    Debug.Assert(resourceSets!.TryGetValue(culture.Name, out rs) && rs == proxy, "Inconsistent value in cache");
                    return null;
                }

                // Look in the ResourceSet table
                var localResourceSets = resourceSets;

                if (localResourceSets == null || !localResourceSets.TryGetValue(culture.Name, out rs))
                    return null;

                if ((proxy = rs as ProxyResourceSet) != null && !IsCachedProxyAccepted(proxy, culture))
                    return null;

                // update the cache with the most recent ResourceSet
                lastUsedResourceSet = new KeyValuePair<string, ResourceSet?>(culture.Name, rs);
                return rs;
            }
        }

        private void AddResourceSet(string cultureName, ref ResourceSet rs)
        {
            // InternalGetResourceSet is both recursive and reentrant -
            // assembly load callbacks in particular are a way we can call
            // back into the ResourceManager in unexpectedly on the same thread.
            if (resourceSets == null)
                resourceSets = new StringKeyedDictionary<ResourceSet> { { cultureName, rs } };
            else if (resourceSets.TryGetValue(cultureName, out ResourceSet? lostRace))
            {
                if (!ReferenceEquals(lostRace, rs))
                {
                    // Note: In certain cases, we can try to add a ResourceSet for multiple
                    // cultures on one thread, while a second thread added another ResourceSet for one
                    // of those cultures.  So when we lose the race, we must make sure our ResourceSet
                    // isn't in our dictionary before closing it.
                    // But if a proxy or non-hybrid resource is already in the cache, we replace that.
                    if (lostRace is ProxyResourceSet && rs is not ProxyResourceSet
                        || lostRace is not HybridResourceSet && rs is HybridResourceSet)
                    {
                        resourceSets[cultureName] = rs;
                    }
                    else
                    {
                        // we do not dispose the replaced resource set because this manager is just a wrapper, such
                        // conflicting resources sets should be disposed by base or the resx manager.
                        rs = lostRace;
                    }
                }
            }
            else
                resourceSets.Add(cultureName, rs);

            lastUsedResourceSet = default;
        }

        private object? GetMetaInternal(string name, CultureInfo? culture, bool isString, bool cloneValue)
        {
            if (name == null!)
                Throw.ArgumentNullException(Argument.name);

            if (source == ResourceManagerSources.CompiledOnly)
                return null;

            // in case of metadata there is no hierarchy traversal so if there is no result trying to provoke the missing manifest exception
            IExpandoResourceSetInternal? rs = Unwrap(InternalGetResourceSet(culture ?? CultureInfo.InvariantCulture, ResourceSetRetrieval.LoadIfExists, false, false)) as IExpandoResourceSetInternal;
            if (rs == null && ThrowException)
                InternalGetResourceSet(CultureInfo.InvariantCulture, ResourceSetRetrieval.GetIfAlreadyLoaded, true, false);
            return rs?.GetMeta(name, IgnoreCase, isString, SafeMode, cloneValue);
        }

        #endregion

        #endregion

        #endregion
    }
}
