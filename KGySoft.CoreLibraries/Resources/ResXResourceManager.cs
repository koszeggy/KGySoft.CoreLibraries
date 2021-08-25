#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXResourceManager.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
#if !NET35
using System.Runtime.CompilerServices;

using KGySoft.Collections;
#endif
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents a resource manager that provides convenient access to culture-specific XML resources (.resx files) at run time.
    /// New elements can be added as well, which can be saved into the <c>.resx</c> files.
    /// <br/>See the <strong>Remarks</strong> section for examples and for the differences compared to <see cref="ResourceManager"/> class.
    /// </summary>
    /// <remarks>
    /// <para><see cref="ResXResourceManager"/> class is derived from <see cref="ResourceManager"/> so it can be used the same way.
    /// The main difference is that instead of working with binary compiled resources the <see cref="ResXResourceManager"/> class uses XML resources (.resx files) directly.
    /// As an <see cref="IExpandoResourceManager"/> implementation it is able to add/replace/remove entries in the resource sets belonging to specified cultures and it can save the changed contents.</para>
    /// <para>See the <a href="#comparison">Comparison with ResourceManager</a> section to see all of the differences.</para>
    /// <note type="tip">To see when to use the <see cref="ResXResourceReader"/>, <see cref="ResXResourceWriter"/>, <see cref="ResXResourceSet"/>, <see cref="ResXResourceManager"/>, <see cref="HybridResourceManager"/> and <see cref="DynamicResourceManager"/>
    /// classes see the documentation of the <see cref="N:KGySoft.Resources">KGySoft.Resources</see> namespace.</note>
    /// <h1 class="heading">Example: Using XML resources created by Visual Studio</h1>
    /// <para>You can create XML resource files by Visual Studio and you can use them by <see cref="ResXResourceManager"/>. See the following example for a step-by-step guide.
    /// <list type="number">
    /// <item>Create a new project (Console Application)
    /// <br/><img src="../Help/Images/NewConsoleApp.png" alt="New console application"/></item>
    /// <item>In Solution Explorer right click on <c>ConsoleApp1</c>, Add, New Folder, name it <c>Resources</c>.</item>
    /// <item>In Solution Explorer right click on <c>Resources</c>, Add, New Item, Resources File.
    /// <br/><img src="../Help/Images/NewResourcesFile.png" alt="New Resources file"/></item>
    /// <item>In Solution Explorer right click on the new resource file (<c>Resource1.resx</c> if not named otherwise) and select Properties</item>
    /// <item>The default value of <c>Build Action</c> is <c>Embedded Resource</c>, which means that the resource will be compiled into the assembly and will be able to be read by the <see cref="ResourceManager"/> class.
    /// To be able to handle it by the <see cref="ResXResourceManager"/> we might want to deploy the .resx file with the application. To do so, select <c>Copy if newer</c> at <c>Copy to Output directory</c>.
    /// If we want to use purely the .resx file, then we can change the <c>Build Action</c> to <c>None</c> and we can clear the default <c>Custom Tool</c> value because we do not need the generated file.
    /// <br/><img src="../Help/Images/ResourceFileProperties_ResXResourceManager.png" alt="Resources1.resx properties"/>
    /// <note>To use both the compiled binary resources and the .resx file you can use the <see cref="HybridResourceManager"/> and <see cref="DynamicResourceManager"/> classes.</note></item>
    /// <item>Now we can either use the built-on resource editor of Visual Studio or just edit the .resx file by the XML Editor. If we add new or existing files to the resources, they will be automatically added to the project's Resources folder.
    /// Do not forget to set <c>Copy if newer</c> for the linked resources as well so they will be copied to the output directory along with the .resx file. Now add some string resources and files if you wish.</item>
    /// <item>To add culture-specific resources you can add further resource files with the same base name, extended by culture names. For example, if the invariant resource is called <c>Resource1.resx</c>, then a
    /// region neutral English resource can be called <c>Resource1.en.resx</c> and the American English resource can be called <c>Resource1.en-US.resx</c>.</item>
    /// <item>Reference <c>KGySoft.CoreLibraries.dll</c> and paste the following code in <c>Program.cs</c>:</item>
    /// </list></para>
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Globalization;
    /// using KGySoft;
    /// using KGySoft.Resources;
    /// 
    /// public class Program
    /// {
    ///     public static void Main()
    ///     {
    ///         var enUS = CultureInfo.GetCultureInfo("en-US");
    ///         var en = enUS.Parent;
    /// 
    ///         // The base name parameter is the name of the resource file without extension and culture specifier.
    ///         // The ResXResourcesDir property denotes the relative path to the resource files.
    ///         // Actually "Resources" is the default value.
    ///         var resourceManager = new ResXResourceManager(baseName: "Resource1") { ResXResourcesDir = "Resources" };
    /// 
    ///         // Tries to get the resource from Resource1.en-US.resx, then Resource1.en.resx, then Resource1.resx
    ///         // and writes the result to the console.
    ///         Console.WriteLine(resourceManager.GetString("String1", enUS));
    /// 
    ///         // Sets the UI culture (similarly to Thread.CurrentThread.CurrentUICulture) so now this is the default
    ///         // culture for looking up resources.
    ///         LanguageSettings.DisplayLanguage = en;
    /// 
    ///         // The Current UI Culture is now en so tries to get the resource from Resource1.en.resx, then Resource1.resx
    ///         // and writes the result to the console.
    ///         Console.WriteLine(resourceManager.GetString("String1"));
    ///     }
    /// }]]>
    /// 
    /// // A possible result of the example above (depending on the created resource files and the added content)
    /// // 
    /// // Test string in en-US resource set.
    /// // Test string in en resource set.</code>
    /// <para>Considering there are .resx files in the background not just <see cref="string"/> and other <see cref="object"/> resources
    /// can be obtained by <see cref="O:KGySoft.Resources.ResXResourceManager.GetString">GetString</see> and <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see> methods
    /// but metadata as well by <see cref="GetMetaString">GetMetaString</see> and <see cref="GetMetaObject">GetMetaObject</see> methods. Please note that accessing aliases are not exposed
    /// by the <see cref="ResXResourceManager"/> class, but you can still access them via the <see cref="IExpandoResourceSet"/> type returned by the <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> method.
    /// <note>Please note that unlike in case of <see cref="O:KGySoft.Resources.ResXResourceManager.GetString">GetString</see> and <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see> methods,
    /// there is no falling back to the parent cultures (as seen in the example above) for metadata accessed by the <see cref="GetMetaString">GetMetaString</see> and <see cref="GetMetaObject">GetMetaObject</see> methods.</note></para>
    /// <h1 class="heading">Instantiating a <see cref="ResXResourceManager"/> object</h1>
    /// <para>You instantiate a <see cref="ResXResourceManager"/> object that retrieves resources from .resx files by calling one of its class constructor overloads.
    /// This tightly couples a <see cref="ResXResourceManager"/> object with a particular set of .resx files (see the previous example as well).</para>
    /// <para>There are three possible constructors to use:
    /// <list type="bullet">
    /// <item><see cref="ResXResourceManager(string,CultureInfo)">ResXResourceManager(baseName string, CultureInfo neutralResourcesLanguage = null)</see>
    /// looks up resources in <c>baseName.cultureName.resx</c> files, where <c>baseName.resx</c> contains the resource set of the ultimate fallback culture (also known as default or invariant or neutral resources culture).
    /// If <c>neutralResourcesLanguage</c> is specified, then <see cref="ResXResourceManager"/> will use the <c>baseName.resx</c> file when the culture to be used equals to the <c>neutralResourcesLanguage</c>.
    /// If <c>neutralResourcesLanguage</c> is not specified, then the default culture is auto detected by the current application's <see cref="NeutralResourcesLanguageAttribute"/>.
    /// If it is not defined, then <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> will be used as default culture.
    /// <code lang="C#">var manager = new ResXResourceManager("MyResources", CultureInfo.GetCultureInfo("en-US"));</code></item>
    /// <item><see cref="ResXResourceManager(string,Assembly)">ResXResourceManager(baseName string, Assembly assembly)</see> is similar to the previous one, except that
    /// it does not set the default culture explicitly but tries to detect it from the provided <see cref="Assembly"/>. If it has a <see cref="NeutralResourcesLanguageAttribute"/> defined,
    /// then it will be used; otherwise, the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> will be used as the default culture.
    /// <code lang="C#">var manager = new ResXResourceManager("MyResources", typeof(Example).Assembly);</code></item>
    /// <item><see cref="ResXResourceManager(Type)">ResXResourceManager(Type resourceSource)</see> will use the name of the provided <see cref="Type"/> as base name, and its <see cref="Assembly"/> to detect the default culture.
    /// <code lang="C#">var manager = new ResXResourceManager(typeof(Example));</code></item></list></para>
    /// <para><note>If a <see cref="ResXResourceManager"/> instance is created with a <c>baseName</c> without corresponding .resx file for the default culture, then accessing a non-existing
    /// resource will throw a <see cref="MissingManifestResourceException"/> unless <see cref="ThrowException"/> property is <see langword="false"/>, in which case only a <see langword="null"/>&#160;value will be
    /// returned in such case. The exception can be avoided, if a resource set is created for the default culture either by adding a new resource (see next section) or by creating the resource set
    /// explicitly by calling the <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> method with <see cref="ResourceSetRetrieval.CreateIfNotExists"/> behavior.</note></para>
    /// <h1 class="heading">Example: Adding and saving new resources at runtime</h1>
    /// <para>As <see cref="ResXResourceManager"/> maintains <see cref="ResXResourceSet"/> instances for each culture, it also supports adding new resources at runtime.
    /// By <see cref="SetObject">SetObject</see> method you can add a resource to a specific culture. You can add metadata as well by <see cref="SetMetaObject">SetMetaObject</see> method.
    /// The resources and metadata can be removed, too (see <see cref="RemoveObject">RemoveObject</see> and <see cref="RemoveMetaObject">RemoveMetaObject</see> methods).</para>
    /// <para>The changes in the resource sets can be saved by calling the <see cref="SaveAllResources">SaveAllResources</see> method. A single resource set can be saved
    /// by calling the <see cref="SaveResourceSet">SaveResourceSet</see> method.
    /// <note>The <see cref="ResXResourceManager"/> always saves the resources into files and never embeds the resources if they are file references (see <see cref="ResXFileRef"/>). If you need more control
    /// over saving you can call the <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> method to access the various <see cref="O:KGySoft.Resources.IExpandoResourceSet.Save">Save</see> overloads)</note></para>
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Globalization;
    /// using System.Resources;
    /// using KGySoft;
    /// using KGySoft.Resources;
    /// 
    /// // You can put this into AssemblyInfo.cs. Indicates that the invariant (default) resource set uses the American English culture.
    /// // Try commenting out next line and see the differences.
    /// [assembly:NeutralResourcesLanguage("en-US")]
    /// 
    /// public static class Example
    /// {
    ///     private static CultureInfo enUS = CultureInfo.GetCultureInfo("en-US");
    ///     private static CultureInfo en = enUS.Parent;
    ///     private static CultureInfo invariant = en.Parent;
    /// 
    ///     // Now that we don't specify the neutralResourcesLanguage optional parameter it will be auto detected
    ///     private static ResXResourceManager manager = new ResXResourceManager("NewResource");
    /// 
    ///     public static void Main()
    ///     {
    ///         // If NewResource.resx does not exist yet a MissingManifestResourceException will be thrown here
    ///         DumpValue("unknown");
    /// 
    ///         // This now creates the resource set for the default culture
    ///         manager.SetObject("StringValue", "This is a string in the default resource", invariant);
    /// 
    ///         // No exception is thrown any more because the default resource set exists now.
    ///         DumpValue("unknown");
    /// 
    ///         // If NeutralResourcesLanguage attribute above is active, invariant == enUS now. 
    ///         manager.SetObject("StringValue", "This is a string in the English resource", en);
    ///         manager.SetObject("StringValue", "This is a string in the American English resource", enUS);
    /// 
    ///         manager.SetObject("IntValue", 42, invariant);
    ///         manager.SetObject("IntValue", 52, en);
    ///         manager.SetObject("IntValue", 62, enUS);
    /// 
    ///         manager.SetObject("DefaultOnly", "This resource is the same everywhere", invariant);
    /// 
    ///         DumpValue("StringValue", invariant);
    ///         DumpValue("StringValue", en);
    ///         DumpValue("StringValue", enUS);
    /// 
    ///         DumpValue("IntValue", invariant);
    ///         DumpValue("IntValue", en);
    ///         DumpValue("IntValue", enUS);
    /// 
    ///         DumpValue("DefaultOnly", invariant);
    ///         DumpValue("DefaultOnly", en);
    ///         DumpValue("DefaultOnly", enUS);
    /// 
    ///         // This now creates NewResource.resx and NewResource.en.resx files
    ///         manager.SaveAllResources(compatibleFormat: true); // so the saved files can be edited by VisualStudio
    ///     }
    /// 
    ///     private static void DumpValue(string name, CultureInfo culture = null)
    ///     {
    ///         try
    ///         {
    ///             Console.WriteLine($"Value of resource '{name}' for culture '{culture ?? LanguageSettings.DisplayLanguage}': " +
    ///                 $"{manager.GetObject(name, culture) ?? "<null>"}");
    ///         }
    ///         catch (Exception e)
    ///         {
    ///             Console.WriteLine($"Accessing resource '{name}' caused an exception: {e.Message}");
    ///         }
    ///     }
    /// }
    /// 
    /// // If NeutralLanguagesResource is en-US, the example above produces the following output:
    /// // 
    /// // Accessing resource 'unknown' caused an exception: Resource file not found: D:\ConsoleApp1\bin\Debug\Resources\NewResource.resx
    /// // Value of resource 'unknown' for culture 'en-US': <null>
    /// // Value of resource 'StringValue' for culture '': This is a string in the American English resource
    /// // Value of resource 'StringValue' for culture 'en': This is a string in the English resource
    /// // Value of resource 'StringValue' for culture 'en-US': This is a string in the American English resource
    /// // Value of resource 'IntValue' for culture '': 62
    /// // Value of resource 'IntValue' for culture 'en': 52
    /// // Value of resource 'IntValue' for culture 'en-US': 62
    /// // Value of resource 'DefaultOnly' for culture '': This resource is the same everywhere
    /// // Value of resource 'DefaultOnly' for culture 'en': This resource is the same everywhere
    /// // Value of resource 'DefaultOnly' for culture 'en-US': This resource is the same everywhere]]></code>
    /// <h1 class="heading">Safety<a name="safety">&#160;</a></h1>
    /// <para>Similarly to <see cref="ResXResourceSet"/> and <see cref="ResXResourceReader"/>, the <see cref="ResXResourceManager"/>
    /// class also has a <see cref="SafeMode"/> which changes the behavior of <see cref="O:KGySoft.Resources.ResXResourceManager.GetString">GetString</see>/<see cref="GetMetaString">GetMetaString</see>
    /// and <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see>/<see cref="GetMetaObject">GetMetaObject</see>
    /// methods:
    /// <list type="bullet">
    /// <item>If the <see cref="SafeMode"/> property is <see langword="true"/>&#160;the return value of <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see>
    /// and <see cref="GetMetaObject">GetMetaObject</see> methods is a <see cref="ResXDataNode"/> rather than the resource or metadata value.
    /// This makes possible to check the raw .resx content before deserialization if the .resx file is from an untrusted source.
    /// The actual value can be obtained by the <see cref="ResXDataNode.GetValue">ResXDataNode.GetValue</see> or <see cref="ResXDataNode.GetValueSafe">ResXDataNode.GetValueSafe</see> method.
    /// See also the third example at the <see cref="ResXResourceSet"/> class.</item>
    /// <item>If the <see cref="SafeMode"/> property is <see langword="true"/>, then <see cref="O:KGySoft.Resources.ResXResourceManager.GetString">GetString</see>
    /// and <see cref="GetMetaString">GetMetaString</see> methods will not throw an <see cref="InvalidOperationException"/>
    /// even for non-string entries; they return the raw XML value instead.</item>
    /// <item>If the <see cref="SafeMode"/> property is <see langword="true"/>, then <see cref="O:KGySoft.Resources.ResXResourceManager.GetStream">GetStream</see>
    /// and <see cref="GetMetaStream">GetMetaStream</see> methods will not throw an <see cref="InvalidOperationException"/>.
    /// For values, which are neither <see cref="MemoryStream"/> nor <see cref="Array">byte[]</see> instances, they return a stream wrapper for the same string value
    /// that is returned by the <see cref="O:KGySoft.Resources.ResXResourceManager.GetString">GetString</see>/<see cref="GetMetaString">GetMetaString</see> methods.</item>
    /// </list>
    /// <note type="security">Even if <see cref="SafeMode"/> is <see langword="false"/>, loading a .resx content with corrupt or malicious entry
    /// will have no effect until we try to obtain the corresponding value. See the last example at <see cref="ResXResourceSet"/> for the demonstration
    /// and the example at <see cref="ResXDataNode"/> to see what members can be checked in safe mode.
    /// </note>
    /// </para>
    /// <h1 class="heading">Comparison with ResourceManager<a name="comparison">&#160;</a></h1>
    /// <para>While <see cref="ResourceManager"/> is read-only and works on binary resources, <see cref="ResXResourceManager"/> supports expansion (see <see cref="IExpandoResourceManager"/>) and works on XML resource (.resx) files.</para>
    /// <para><strong>Incompatibility</strong> with <see cref="ResourceManager"/>:
    /// <list type="bullet">
    /// <item>There is no constructor where the type of the resource sets can be specified. The <see cref="ResourceManager.ResourceSetType"/> property
    /// returns always the type of <see cref="ResXResourceSet"/>.</item>
    /// <item>If <see cref="ResourceManager.GetResourceSet">ResourceManager.GetResourceSet</see> method is called with <c>createIfNotExists = false</c> for a culture,
    /// which has a corresponding but not loaded resource file, then a resource set for a parent culture might be cached and on successive calls that cached parent set will be
    /// returned even if the <c>createIfNotExists</c> argument is <see langword="true"/>. In <see cref="ResXResourceManager"/> the corresponding argument of
    /// the <see cref="GetResourceSet">GetResourceSet</see> method has been renamed to <c>loadIfExists</c> and works as expected.</item>
    /// <item>The <see cref="O:KGySoft.Resources.ResXResourceManager.GetStream">GetStream</see> methods have <see cref="MemoryStream"/> return type instead of <see cref="UnmanagedMemoryStream"/> and they can be used also for <see cref="Array">byte[]</see> values.</item>
    /// </list></para>
    /// <para><strong>New features and improvements</strong> compared to <see cref="ResourceManager"/>:
    /// <list type="bullet">
    /// <item><term>Write support</term>
    /// <description>The stored content can be expanded or existing entries can be replaced (see <see cref="SetObject">SetObject</see>/<see cref="SetMetaObject">SetMetaObject</see>),
    /// the entries can be removed (see <see cref="RemoveObject">RemoveObject</see>/<see cref="RemoveMetaObject">RemoveMetaObject</see>),
    /// and the new content can be saved (see <see cref="SaveAllResources">SaveAllResources</see>/<see cref="SaveResourceSet">SaveResourceSet</see>).
    /// You can start even with a completely empty manager, add content dynamically and save the new resources (see the example above).</description></item>
    /// <item><term>Security</term>
    /// <description>During the initialization of <see cref="ResXResourceManager"/> and loading of a resource set no object is deserialized even if <see cref="SafeMode"/>
    /// property is <see langword="false"/>. Objects are deserialized only when they are accessed (see <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see>/<see cref="GetMetaObject">GetMetaObject</see>).
    /// If <see cref="SafeMode"/> is <see langword="true"/>, then security is even more increased because <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see> and <see cref="GetMetaObject">GetMetaObject</see> methods
    /// return a <see cref="ResXDataNode"/> instance instead of a deserialized object so you can check whether the resource or metadata
    /// can be treat as a safe object before actually deserializing it. See the <a href="#safety">Safety</a> section above for more details.</description></item>
    /// <item><term>Disposal</term>
    /// <description>As <see cref="ResourceSet"/> implementations are disposable objects, <see cref="ResXResourceManager"/> itself implements
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
    /// <seealso cref="HybridResourceManager"/>
    /// <seealso cref="DynamicResourceManager"/>
    [Serializable]
    public class ResXResourceManager : ResourceManager, IExpandoResourceManager
    {
        #region Nested Types

        #region ProxyResourceSet class

        /// <summary>
        /// Represents a cached resource set for a child culture, which might be replaced later.
        /// </summary>
        private sealed class ProxyResourceSet : ResourceSet
        {
            #region Fields

            private bool canHaveLoadableParent;

            #endregion

            #region Properties

            /// <summary>
            /// Gets the wrapped resource set. This is always a parent of <see cref="WrappedCulture"/>.
            /// </summary>
            internal ResXResourceSet? ResXResourceSet { get; private set; }

            /// <summary>
            /// Gets the culture of the wrapped resource set
            /// </summary>
            internal CultureInfo WrappedCulture { get; }


            /// <summary>
            /// Gets whether this proxy has been loaded by <see cref="ResourceSetRetrieval.GetIfAlreadyLoaded"/> and trying parents.
            /// In this case there might be unloaded parents for this resource set.
            /// </summary>
            internal bool CanHaveLoadableParent
            {
                get
                {
                    lock (this)
                        return canHaveLoadableParent;
                }
                set
                {
                    lock (this)
                        canHaveLoadableParent = value;
                }
            }

            /// <summary>
            /// Gets whether this proxy has been loaded by <see cref="ResourceSetRetrieval.GetIfAlreadyLoaded"/> and trying parents,
            /// and there is an existing file for this resource. File name itself is not stored so it will be re-groveled next time
            /// handling the case if it has been deleted.
            /// </summary>
            internal bool FileExists { get; }

            internal bool HierarchyLoaded => !CanHaveLoadableParent && !FileExists;

            #endregion

            #region Constructors

            internal ProxyResourceSet(ResXResourceSet toWrap, CultureInfo wrappedCulture, bool fileExists, bool canHaveLoadableParent)
            {
                ResXResourceSet = toWrap;
                WrappedCulture = wrappedCulture;
                FileExists = fileExists;
                this.canHaveLoadableParent = canHaveLoadableParent;
            }

            #endregion

            #region Methods

            protected override void Dispose(bool disposing)
            {
                ResXResourceSet = null;
                base.Dispose(disposing);
            }

            #endregion
        }

        #endregion

        #region GetResXResourceSetContext struct

        private struct GetResXResourceSetContext
        {
            #region Fields

            internal CultureInfo Culture;
            internal ResourceSetRetrieval Behavior;
            internal bool TryParents;

#if NET35
            internal Hashtable ResourceSets;
#else
            internal StringKeyedDictionary<ResourceSet> ResourceSets;
#endif
            internal ResourceSet? Result;
            internal ProxyResourceSet? Proxy;
            internal CultureInfo? FoundCultureToAdd;
            internal CultureInfo? FoundProxyCulture;
            internal ResourceFallbackManager? FallbackManager;

            #endregion
        }

        #endregion

        #endregion

        #region Constants

        private const string resXFileExtension = ".resx";

        #endregion

        #region Fields

        private readonly CultureInfo neutralResourcesCulture;

        private string resxResourcesDir = "Resources";
        [NonSerialized]private string? resxDirFullPath;
        [NonSerialized]private object? syncRoot;

        /// <summary>
        /// The lastly used resource set. Unlike in base, this is not necessarily the resource set in which a result
        /// has been found but the resource set was requested last time. In cases it means difference this way performs usually better (no unneeded traversal again and again).
        /// </summary>
        [NonSerialized]private KeyValuePair<string, ResXResourceSet> lastUsedResourceSet;

#if !NET35
        /// <summary>
        /// Local cache of the resource sets.
        /// Before serializing we remove proxies and unmodified sets.
        /// </summary>
        private StringKeyedDictionary<ResourceSet>? resourceSets;
#endif

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets the relative path to .resx resource files.
        /// <br/>Default value: <c>Resources</c>
        /// </summary>
        [AllowNull]
        public string ResXResourcesDir
        {
            get => resxResourcesDir;
            set
            {
                if (value == resxResourcesDir)
                    return;

                resxDirFullPath = null;
                if (value == null!)
                {
                    resxResourcesDir = String.Empty;
                    return;
                }

                if (value.IndexOfAny(Files.IllegalPathChars) >= 0)
                    Throw.ArgumentException(Argument.value, Res.ValueContainsIllegalPathCharacters(value));

                if (Path.IsPathRooted(value))
                {
                    string baseDir = Files.GetExecutingPath();
                    string relPath = Files.GetRelativePath(value, baseDir);
                    if (!Path.IsPathRooted(relPath))
                    {
                        resxResourcesDir = relPath;
                        return;
                    }
                }

                resxResourcesDir = value;
            }
        }

        /// <summary>
        /// Gets or sets whether a <see cref="MissingManifestResourceException"/> should be thrown when a resource
        /// .resx file is not found even for the neutral culture.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public bool ThrowException { get; set; } = true;

        /// <summary>
        /// Gets or sets whether .resx file errors should be ignored when attempting to load a resource set. If <see langword="true"/>,
        /// then non-loadable resource sets are considered as missing ones; otherwise, an exception is thrown.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        public bool IgnoreResXParseErrors { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the <see cref="ResXResourceManager"/> works in safe mode. In safe mode the retrieved
        /// objects are not deserialized automatically.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>When <see cref="SafeMode"/> is <see langword="true"/>, then <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see> and <see cref="GetMetaObject">GetMetaObject</see> methods
        /// return <see cref="ResXDataNode"/> instances instead of deserialized objects. You can retrieve the deserialized objects by calling
        /// the <see cref="ResXDataNode.GetValue">ResXDataNode.GetValue</see> or <see cref="ResXDataNode.GetValueSafe">ResXDataNode.GetValueSafe</see> method.</para>
        /// <para>When <see cref="SafeMode"/> is <see langword="true"/>, then <see cref="O:KGySoft.Resources.ResXResourceManager.GetString">GetString</see> and <see cref="GetMetaString">GetMetaString</see> methods
        /// will return a <see cref="string"/> also for non-string objects. For non-string values the raw XML string value will be returned.</para>
        /// <para>When <see cref="SafeMode"/> is <see langword="true"/>, then <see cref="O:KGySoft.Resources.ResXResourceManager.GetStream">GetStream</see> and <see cref="GetMetaStream">GetMetaStream</see> methods
        /// will return a <see cref="MemoryStream"/> for any object.
        /// For values, which are neither <see cref="MemoryStream"/>, nor <see cref="Array">byte[]</see> instances these methods return a stream wrapper for the same string value
        /// that is returned by the <see cref="O:KGySoft.Resources.ResXResourceManager.GetString">GetString</see>/<see cref="GetMetaString">GetMetaString</see> methods.</para>
        /// </remarks>
        /// <seealso cref="ResXResourceReader.SafeMode"/>
        /// <seealso cref="ResXResourceSet.SafeMode"/>
        public bool SafeMode { get; set; }

        /// <summary>
        /// Gets or sets whether <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see> and <see cref="GetMetaObject">GetMetaObject</see> methods return always a new copy of the stored values.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>To be compatible with <a href="https://docs.microsoft.com/en-us/dotnet/api/System.Resources.ResourceManager" target="_blank">System.Resources.ResourceManager</a> this
        /// property is <see langword="true"/>&#160;by default. If this <see cref="ResXResourceManager"/> contains no mutable values or it is known that modifying values is not
        /// an issue, then this property can be set to <see langword="false"/>&#160;for better performance.</para>
        /// <para>String values are not cloned.</para>
        /// </remarks>
        public bool CloneValues { get; set; } = true;

        /// <summary>
        /// Gets whether this <see cref="ResXResourceManager"/> instance is disposed.
        /// </summary>
        public bool IsDisposed => InternalResourceSets == null;

        /// <summary>
        /// Gets whether this <see cref="ResXResourceManager"/> instance has modified and unsaved data.
        /// </summary>
        public bool IsModified
        {
            get
            {
                lock (SyncRoot)
                {
                    // skipping proxies for this check
                    return ResourceSets.Values.OfType<ResXResourceSet>().Any(rs => rs.IsModified);
                }
            }
        }

        #endregion

        #region Internal Properties

        internal string ResourceFileName => GetResourceFileName(CultureInfo.InvariantCulture);

        #endregion

        #region Private Properties


#if NET35
        private Hashtable? InternalResourceSets => base.ResourceSets;

        [AllowNull]
        private new Hashtable ResourceSets
        {
            get
            {
                Hashtable? result = base.ResourceSets;
                if (result == null)
                    Throw.ObjectDisposedException();
                return result;
            }
            set => base.ResourceSets = value;
        }
#else
        private StringKeyedDictionary<ResourceSet>? InternalResourceSets => resourceSets;

        [AllowNull]
#if NETFRAMEWORK
        new
#endif
        private StringKeyedDictionary<ResourceSet> ResourceSets
        {
            get
            {
                StringKeyedDictionary<ResourceSet>? result = resourceSets;
                if (result == null)
                    Throw.ObjectDisposedException();
                return result;
            }
            set => resourceSets = value;
        }
#endif

        private object SyncRoot
        {
            get
            {
                if (syncRoot == null)
                    Interlocked.CompareExchange(ref syncRoot, new object(), null);
                return syncRoot;
            }
        }

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceManager"/> class that looks up resources in
        /// resource XML files based on the provided <paramref name="baseName"/>.
        /// </summary>
        /// <param name="baseName">A base name that is the prefix of the resource files.
        /// For example, the prefix for the resource file named <c>Resource1.en-US.resx</c> is <c>Resource1</c>.</param>
        /// <param name="assembly">The assembly, from which the language of the neutral resources is tried to be auto detected. See the <strong>Remarks</strong> section for details.</param>
        /// <remarks>
        /// <para>The <see cref="ResXResourceManager"/> looks up resources in <c>baseName.cultureName.resx</c> files, where <c>baseName.resx</c> contains the resource set of the
        /// ultimate fallback culture (also known as default or invariant or neutral resources culture).</para>
        /// <para>If the provided <paramref name="assembly"/> has a <see cref="NeutralResourcesLanguageAttribute"/> defined, then it will be used to determine the language
        /// of the fallback culture. If the provided <paramref name="assembly"/> does not define this attribute, then <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> will be used as the default culture.</para>
        /// </remarks>
        public ResXResourceManager(string baseName, Assembly assembly)
            : base(baseName, assembly, typeof(ResXResourceSet))
        {
            // Effects of calling the base constructor:
            // - Sets MainAssembly and BaseNameField directly
            // - .resx files will be searched in resxResourcesDir\baseName[.Culture].resx
            // - _userResourceSet is set to ResXResourceSet and thus is returned by ResourceSetType; however, it will never be used by base because InternalGetResourceSet is overridden
            // - _neutralResourcesCulture is initialized from assembly (> .NET4 only)
            neutralResourcesCulture = GetNeutralResourcesLanguage(assembly);
#if !NET35
            ResetResourceSets();
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXResourceManager"/> class that looks up resources in
        /// resource XML files based on the provided <paramref name="baseName"/>.
        /// </summary>
        /// <param name="baseName">A base name that is the prefix of the resource files.
        /// For example, the prefix for the resource file named <c>Resource1.en-US.resx</c> is <c>Resource1</c>.</param>
        /// <param name="neutralResourcesLanguage">Determines the language of the neutral resources. When <see langword="null"/>,
        /// it will be determined by the entry assembly, or if that is not available, then by the assembly of the caller's method. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>The <see cref="ResXResourceManager"/> looks up resources in <c>baseName.cultureName.resx</c> files, where <c>baseName.resx</c> contains the resource set of the
        /// ultimate fallback culture (also known as default or invariant or neutral resources culture).</para>
        /// <para>If <paramref name="neutralResourcesLanguage"/> is <see langword="null"/>, then the default culture is auto detected by the current application's <see cref="NeutralResourcesLanguageAttribute"/>.
        /// If it is not defined, then <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> will be used as default culture.</para>
        /// </remarks>
        public ResXResourceManager(string baseName, CultureInfo? neutralResourcesLanguage = null)
            : this(baseName, Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly())
        {
            if (neutralResourcesLanguage != null)
                neutralResourcesCulture = neutralResourcesLanguage;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ResXResourceManager"/> class that looks up resources in
        /// resource XML files based on information from the specified type object.
        /// </summary>
        /// <param name="resourceSource">A type from which the resource manager derives all information for finding resource files.</param>
        /// <remarks>
        /// <para>The <see cref="ResXResourceManager"/> looks up resources in <c>resourceSourceTypeName.cultureName.resx</c> files, where <c>resourceSourceTypeName.resx</c> contains the resource set of the
        /// ultimate fallback culture (also known as default or invariant or neutral resources culture).</para>
        /// <para>If the <see cref="Assembly"/> of <paramref name="resourceSource"/> has a <see cref="NeutralResourcesLanguageAttribute"/> defined, then it will be used to determine the language
        /// of the fallback culture. If the <see cref="Assembly"/> of <paramref name="resourceSource"/> does not define this attribute, then <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> will be used as the default culture.</para>
        /// </remarks>
        public ResXResourceManager(Type resourceSource)
            // ReSharper disable once ConstantConditionalAccessQualifier - it CAN be null even if it is not allowed. Exception is thrown from the base via the overloaded ctor.
            : this(resourceSource?.Name!, resourceSource?.Assembly!)
        {
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Internal Methods

        internal static bool IsParentCulture(CultureInfo parent, string childName)
        {
            for (CultureInfo ci = CultureInfo.GetCultureInfo(childName).Parent; !Equals(ci, CultureInfo.InvariantCulture); ci = ci.Parent)
            {
                if (Equals(ci, parent))
                    return true;
            }

            return false;
        }

        #endregion

        #region Private Methods

#if NET35
        private static bool TryGetResource(Hashtable localResourceSets, string cultureName, [MaybeNullWhen(false)]out ResourceSet rs)
        {
            rs = (ResourceSet?)localResourceSets[cultureName];
            return rs != null;
        }

        private static void AddResourceSet(Hashtable localResourceSets, string cultureName, ref ResourceSet rs)
        {
            // GetResXResourceSet is both recursive and reentrant -
            // assembly load callbacks in particular are a way we can call
            // back into the ResourceManager in unexpectedly on the same thread.
            lock (localResourceSets)
            {
                // If another thread added this culture, return that.
                ResourceSet? lostRace = (ResourceSet?)localResourceSets[cultureName];
                if (lostRace != null)
                {
                    if (!ReferenceEquals(lostRace, rs))
                    {
                        // Note: In certain cases, we can be trying to add a ResourceSet for multiple
                        // cultures on one thread, while a second thread added another ResourceSet for one
                        // of those cultures.  So when we lose the race, we must make sure our ResourceSet
                        // isn't in our dictionary before closing it.
                        // But if a proxy is already in the cache, we replace that.
                        if (!(lostRace is ProxyResourceSet && rs is ResXResourceSet))
                        {
                            if (!localResourceSets.ContainsValue(rs))
                                rs.Dispose();
                            rs = lostRace;
                        }
                        else
                            localResourceSets[cultureName] = rs;
                    }
                }
                else
                {
                    localResourceSets.Add(cultureName, rs);
                }
            }
        }
#else
        [MethodImpl(MethodImpl.AggressiveInlining)]
        private static bool TryGetResource(StringKeyedDictionary<ResourceSet> localResourceSets, string cultureName, [MaybeNullWhen(false)]out ResourceSet rs)
            => localResourceSets.TryGetValue(cultureName, out rs);

        private static void AddResourceSet(StringKeyedDictionary<ResourceSet> localResourceSets, string cultureName, ref ResourceSet rs)
        {
            // GetResXResourceSet is both recursive and reentrant -
            // assembly load callbacks in particular are a way we can call
            // back into the ResourceManager in unexpectedly on the same thread.
            lock (localResourceSets)
            {
                // If another thread added this culture, return that.
                if (TryGetResource(localResourceSets, cultureName, out ResourceSet? lostRace))
                {
                    if (!ReferenceEquals(lostRace, rs))
                    {
                        // Note: In certain cases, we can be trying to add a ResourceSet for multiple
                        // cultures on one thread, while a second thread added another ResourceSet for one
                        // of those cultures.  So when we lose the race, we must make sure our ResourceSet
                        // isn't in our dictionary before closing it.
                        // But if a proxy is already in the cache, we replace that.
                        if (lostRace is ProxyResourceSet && rs is ResXResourceSet)
                            localResourceSets[cultureName] = rs;
                        else
                        {
                            if (!localResourceSets.ContainsValue(rs))
                                rs.Dispose();
                            rs = lostRace;
                        }
                    }
                }
                else
                {
                    localResourceSets.Add(cultureName, rs);
                }
            }
        }
#endif

        [return:NotNullIfNotNull("rs")]
        private static ResXResourceSet? Unwrap(ResourceSet? rs)
            => rs == null ? null
                : rs is ResXResourceSet resx ? resx
                : ((ProxyResourceSet)rs).ResXResourceSet;

        private static void ReleaseResourceSets(IDictionary resourceSets)
        {
            // this enumerates both Hashtable and StringKeyedDictionary the same way.
            // The non-generic enumerator is not a problem, values must be cast anyway.
            IDictionaryEnumerator enumerator = resourceSets.GetEnumerator();
            while (enumerator.MoveNext())
                ((ResourceSet)enumerator.Value!).Dispose();
        }

        #endregion

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
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>&#160;and <paramref name="name"/> is a non-<see langword="string"/> resource, then
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns the underlying raw XML content of the resource.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="CloneValues"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160;and the type of the resource is not <see cref="string"/>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        /// <remarks>For examples, see the description of the <see cref="ResXResourceManager"/> class.</remarks>
        public override string? GetString(string name) => (string?)GetObjectInternal(name, null, true, false);

        /// <summary>
        /// Returns the value of the string resource localized for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the resource is localized. If the resource is not localized for
        /// this culture, the resource manager uses fallback rules to locate an appropriate resource. If this value is
        /// <see langword="null"/>, the <see cref="CultureInfo"/> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property.</param>
        /// <returns>
        /// The value of the resource localized for the specified <paramref name="culture"/>, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>&#160;and <paramref name="name"/> is a non-<see langword="string"/> resource, then
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns the underlying raw XML content of the resource.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="CloneValues"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160;and the type of the resource is not <see cref="string"/>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        /// <remarks>For examples, see the description of the <see cref="ResXResourceManager"/> class.</remarks>
        public override string? GetString(string name, CultureInfo? culture) => (string?)GetObjectInternal(name, culture, true, false);

        /// <summary>
        /// Returns a <see cref="MemoryStream"/> instance from the resource of the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <returns>
        /// A <see cref="MemoryStream"/> object from the specified resource localized for the caller's current UI culture, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="CloneValues"/> property, the <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see> methods return either
        /// a full copy of the specified resource, or always the same instance. For memory streams none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="O:KGySoft.Resources.ResXResourceManager.GetStream">GetStream</see> methods
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="CloneValues"/> property.</para>
        /// <para><see cref="O:KGySoft.Resources.ResXResourceManager.GetStream">GetStream</see> can be used also for byte array resources. However, if the value is returned from compiled resources, then always a new copy of the byte array will be wrapped.</para>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>&#160;and <paramref name="name"/> is neither a <see cref="MemoryStream"/> nor a byte array resource, then
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns a stream wrapper for the same string value that is returned by the <see cref="O:KGySoft.Resources.ResXResourceManager.GetString">GetString</see> method,
        /// which will be the raw XML content for non-string resources.</para>
        /// <note>The internal buffer is tried to be obtained by reflection in the first place. On platforms, which have possibly unknown non-public member names the public APIs are used, which may copy the content in memory.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
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
        /// <para>Depending on the value of the <see cref="CloneValues"/> property, the <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see> methods return either
        /// a full copy of the specified resource, or always the same instance. For memory streams none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="O:KGySoft.Resources.ResXResourceManager.GetStream">GetStream</see> methods
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="CloneValues"/> property.</para>
        /// <para><see cref="O:KGySoft.Resources.ResXResourceManager.GetStream">GetStream</see> can be used also for byte array resources. However, if the value is returned from compiled resources, then always a new copy of the byte array will be wrapped.</para>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>&#160;and <paramref name="name"/> is neither a <see cref="MemoryStream"/> nor a byte array resource, then
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns a stream wrapper for the same string value that is returned by the <see cref="O:KGySoft.Resources.ResXResourceManager.GetString">GetString</see> method,
        /// which will be the raw XML content for non-string resources.</para>
        /// <note>The internal buffer is tried to be obtained by reflection in the first place. On platforms, which have possibly unknown non-public member names the public APIs are used, which may copy the content in memory.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160;and the type of the resource is neither <see cref="MemoryStream"/> nor <see cref="Array">byte[]</see>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public new virtual MemoryStream? GetStream(string name, CultureInfo? culture)
        {
            object? value = GetObjectInternal(name, culture, false, false);
            return ResXCommon.ToMemoryStream(name, value, SafeMode);
        }

        /// <summary>
        /// Returns the value of the specified resource.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="ResXResourceManager"/> class for examples.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <returns>
        /// If <see cref="SafeMode"/> is <see langword="true"/>, then the method returns a <see cref="ResXDataNode"/> instance instead of the actual deserialized value.
        /// Otherwise, returns the value of the resource localized for the caller's current UI culture, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="CloneValues"/> property, the <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see> methods return either
        /// a full copy of the specified resource, or always the same instance. For memory streams and byte arrays none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="O:KGySoft.Resources.ResXResourceManager.GetStream">GetStream</see> methods
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="CloneValues"/> property.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="CloneValues"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public override object? GetObject(string name) => GetObjectInternal(name, null, false, CloneValues);

        /// <summary>
        /// Gets the value of the specified resource localized for the specified <paramref name="culture"/>.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="ResXResourceManager"/> class for examples.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <param name="culture">The culture for which the resource is localized. If the resource is not localized for
        /// this culture, the resource manager uses fallback rules to locate an appropriate resource. If this value is
        /// <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture" /> property.</param>
        /// <returns>
        /// If <see cref="SafeMode"/> is <see langword="true"/>, then the method returns a <see cref="ResXDataNode"/> instance instead of the actual deserialized value.
        /// Otherwise, returns the value of the resource localized for the specified <paramref name="culture"/>, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="CloneValues"/> property, the <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see> methods return either
        /// a full copy of the specified resource, or always the same instance. For memory streams and byte arrays none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="O:KGySoft.Resources.ResXResourceManager.GetStream">GetStream</see> methods
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="CloneValues"/> property.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="CloneValues"/> property.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public override object? GetObject(string name, CultureInfo? culture) => GetObjectInternal(name, culture, false, CloneValues);

        /// <summary>
        /// Returns the value of the string metadata for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the metadata to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the metadata should be returned.
        /// If this value is <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property.
        /// Unlike in case of <see cref="O:KGySoft.Resources.ResXResourceManager.GetString">GetString</see> method, no fallback is used if the metadata is not found in the specified culture. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.
        /// </param>
        /// <returns>
        /// The value of the metadata of the specified culture, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>If <see cref="SafeMode"/> is <see langword="true"/>&#160;and <paramref name="name"/> is a non-<see langword="string"/> metadata, then
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns the underlying raw XML content of the metadata.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="SafeMode"/> is <see langword="false"/>&#160;and the type of the metadata is not <see cref="string"/>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public string? GetMetaString(string name, CultureInfo? culture = null) => (string?)GetMetaInternal(name, culture, true, CloneValues);

        /// <summary>
        /// Returns a <see cref="MemoryStream"/> instance from the metadata of the specified <paramref name="name"/> and <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the metadata to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the metadata should be returned.
        /// If this value is <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property.
        /// Unlike in case of <see cref="O:KGySoft.Resources.ResXResourceManager.GetStream">GetStream</see> methods, no fallback is used if the metadata is not found in the specified culture. This parameter is optional.
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
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns a stream wrapper for the same string value that is returned by the <see cref="O:KGySoft.Resources.ResXResourceManager.GetString">GetString</see> methods,
        /// which will be the raw XML content for non-string metadata.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
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
        /// If this value is <see langword="null"/>, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property.
        /// Unlike in case of <see cref="O:KGySoft.Resources.ResXResourceManager.GetObject">GetObject</see> method, no fallback is used if the metadata is not found in the specified culture. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.
        /// </param>
        /// <returns>
        /// If <see cref="SafeMode"/> is <see langword="true"/>, then the method returns a <see cref="ResXDataNode"/> instance instead of the actual deserialized value.
        /// Otherwise, returns the value of the metadata localized for the specified <paramref name="culture"/>, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public object? GetMetaObject(string name, CultureInfo? culture = null) => GetMetaInternal(name, culture, false, CloneValues);

        /// <summary>
        /// Adds or replaces a resource object in the current <see cref="ResXResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The name of the resource to set.</param>
        /// <param name="culture">The culture of the resource to set. If this value is <see langword="null"/>,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property.</param>
        /// <param name="value">The value of the resource to set. If <see langword="null" />, then a null reference will be explicitly
        /// stored for the specified <paramref name="culture"/>. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>If <paramref name="value" /> is <see langword="null" />, a null reference will be explicitly stored.
        /// Its effect is similar to the <see cref="RemoveObject">RemoveObject</see> method: the subsequent <see cref="GetObject(string, CultureInfo)">GetObject</see> calls
        /// with the same <paramref name="culture" /> will fall back to the parent culture, or will return <see langword="null"/>&#160;if
        /// <paramref name="name" /> is not found in any parent cultures. However, enumerating the result set returned by
        /// <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> and <see cref="GetResourceSet">GetResourceSet</see> methods will return the resources with
        /// <see langword="null"/>&#160;value.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        public void SetObject(string name, object? value, CultureInfo? culture = null)
        {
            ResXResourceSet rs = GetResXResourceSet(culture ?? CultureInfo.CurrentUICulture, ResourceSetRetrieval.CreateIfNotExists, false)!;
            rs.SetObject(name, value);
        }

        /// <summary>
        /// Removes a resource object from the current <see cref="ResXResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The case-sensitive name of the resource to remove.</param>
        /// <param name="culture">The culture of the resource to remove. If this value is <see langword="null"/>,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para><paramref name="name"/> is considered as case-sensitive. If <paramref name="name"/> occurs multiple times
        /// in the resource set in case-insensitive manner, they can be removed one by one only.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        public void RemoveObject(string name, CultureInfo? culture = null)
        {
            ResXResourceSet? rs = GetResXResourceSet(culture ?? CultureInfo.CurrentUICulture, ResourceSetRetrieval.LoadIfExists, false);
            rs?.RemoveObject(name);
        }

        /// <summary>
        /// Adds or replaces a metadata object in the current <see cref="ResXResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The name of the metadata to set.</param>
        /// <param name="culture">The culture of the metadata to set.
        /// If this value is <see langword="null" />, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="value">The value of the metadata to set. If <see langword="null" />,  then a null reference will be explicitly
        /// stored for the specified <paramref name="culture" />.</param>
        /// <remarks>
        /// If <paramref name="value" /> is <see langword="null" />, a null reference will be explicitly stored.
        /// Its effect is similar to the <see cref="RemoveMetaObject">RemoveMetaObject</see> method: the subsequent <see cref="GetMetaObject">GetMetaObject</see> calls
        /// with the same <paramref name="culture" /> will return <see langword="null" />.
        /// However, enumerating the result set returned by <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> method will return the meta objects with <see langword="null"/>&#160;value.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        public void SetMetaObject(string name, object? value, CultureInfo? culture = null)
        {
            ResXResourceSet rs = GetResXResourceSet(culture ?? CultureInfo.InvariantCulture, ResourceSetRetrieval.CreateIfNotExists, false)!;
            rs.SetMetaObject(name, value);
        }

        /// <summary>
        /// Removes a metadata object from the current <see cref="ResXResourceManager" /> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The case-sensitive name of the metadata to remove.</param>
        /// <param name="culture">The culture of the metadata to remove.
        /// If this value is <see langword="null" />, the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <paramref name="name" /> is considered as case-sensitive. If <paramref name="name" /> occurs multiple times
        /// in the resource set in case-insensitive manner, they can be removed one by one only.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        public void RemoveMetaObject(string name, CultureInfo? culture = null)
        {
            ResXResourceSet? rs = GetResXResourceSet(culture ?? CultureInfo.InvariantCulture, ResourceSetRetrieval.LoadIfExists, false);
            rs?.RemoveMetaObject(name);
        }

        /// <summary>
        /// Saves the resource set of a particular <paramref name="culture" /> if it has been already loaded.
        /// </summary>
        /// <param name="culture">The culture of the resource set to save.</param>
        /// <param name="force"><see langword="true"/>&#160;to save the resource set even if it has not been modified; <see langword="false"/>&#160;to save it only if it has been modified.
        /// <br />Default value: <see langword="false"/>.</param>
        /// <param name="compatibleFormat">If set to <see langword="true"/>, the result .resx file can be read by a <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> instance
        /// and the Visual Studio Resource Editor. If set to <see langword="false"/>, the result .resx is often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />),
        /// but the result can be read only by the <see cref="ResXResourceReader" /> class.
        /// <br />Default value: <see langword="false"/>.</param>
        /// <returns>
        /// <see langword="true"/>&#160;if the resource set of the specified <paramref name="culture" /> has been saved;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="IOException">The resource set could not be saved.</exception>
        public bool SaveResourceSet(CultureInfo culture, bool force = false, bool compatibleFormat = false)
        {
            if (culture == null!)
                Throw.ArgumentNullException(Argument.culture);
            var localResourceSets = ResourceSets; // var is Hashtable in .NET 3.5 and is StringKeyedDictionary above
            ResourceSet? rs;
            lock (SyncRoot)
            {
                if (!TryGetResource(localResourceSets, culture.Name, out rs))
                    return false;
            }

            if (rs is not ResXResourceSet resx || !(force || resx.IsModified))
                return false;

            resx.Save(GetResourceFileName(culture), compatibleFormat);
            return true;
        }

        /// <summary>
        /// Saves all already loaded resources.
        /// </summary>
        /// <param name="force"><see langword="true"/>&#160;to save all of the already loaded resource sets regardless if they have been modified; <see langword="false"/>&#160;to save only the modified resource sets. This parameter is optional.
        /// <br />Default value: <see langword="false"/>.</param>
        /// <param name="compatibleFormat">If set to <see langword="true"/>, the result .resx files can be read by a <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> instance
        /// and the Visual Studio Resource Editor. If set to <see langword="false"/>, the result .resx files are often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />),
        /// but the result can be read only by the <see cref="ResXResourceReader" /> class. This parameter is optional.
        /// <br />Default value: <see langword="false"/>.</param>
        /// <returns>
        ///   <see langword="true"/>&#160;if at least one resource set has been saved; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        /// <exception cref="IOException">A resource set could not be saved.</exception>
        public bool SaveAllResources(bool force = false, bool compatibleFormat = false)
        {
            IDictionary localResourceSets = ResourceSets; // type is Hashtable in .NET 3.5 and is StringKeyedDictionary above
            bool result = false;
            lock (SyncRoot)
            {
                // this enumerates both Hashtable and Dictionary the same way.
                // The non-generic enumerator is not a problem, values must be cast anyway.
                IDictionaryEnumerator enumerator = localResourceSets.GetEnumerator();
                bool first = true;
                while (enumerator.MoveNext())
                {
                    if (enumerator.Value is not ResXResourceSet rs || (!rs.IsModified && !force))
                        continue;

                    if (first)
                    {
                        string dir = GetResourceDirName();
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        first = false;
                    }

                    rs.Save(GetResourceFileName((string)enumerator.Key!), compatibleFormat);
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Disposes all of the cached <see cref="ResXResourceSet"/> instances and releases all resources.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        /// <remarks>
        /// <note type="caution">By calling this method all of the unsaved changes will be lost.</note>
        /// <para>By the <see cref="IsModified"/> property you can check whether there are unsaved changes.</para>
        /// <para>To save the changes you can call the <see cref="SaveAllResources">SaveAllResources</see> method.</para>
        /// </remarks>
        public override void ReleaseAllResources()
        {
            ReleaseResourceSets(ResourceSets);
            base.ReleaseAllResources();
            ResetResourceSets();
            lastUsedResourceSet = default;
        }

        /// <summary>
        /// Retrieves the resource set for a particular culture.
        /// </summary>
        /// <param name="culture">The culture whose resources are to be retrieved.</param>
        /// <param name="loadIfExists"><see langword="true"/>&#160;to load the resource set, if it has not been loaded yet and the corresponding resource file exists; otherwise, <see langword="false"/>.</param>
        /// <param name="tryParents"><see langword="true"/>&#160;to use resource fallback to load an appropriate resource if the resource set cannot be found; <see langword="false"/>&#160;to bypass the resource fallback process.</param>
        /// <returns>
        /// The resource set for the specified <paramref name="culture"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The <see cref="ResXResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException"><paramref name="tryParents"/> and <see cref="ThrowException"/> are <see langword="true"/>&#160;and the .resx file of the neutral culture was not found.</exception>
        public override ResourceSet? GetResourceSet(CultureInfo culture, bool loadIfExists, bool tryParents)
            => (ResourceSet?)GetExpandoResourceSet(culture, loadIfExists ? ResourceSetRetrieval.LoadIfExists : ResourceSetRetrieval.GetIfAlreadyLoaded, tryParents);

        /// <summary>
        /// Retrieves the resource set for a particular culture, which can be dynamically modified.
        /// </summary>
        /// <param name="culture">The culture whose resources are to be retrieved.</param>
        /// <param name="behavior">Determines the retrieval behavior of the result <see cref="IExpandoResourceSet"/>. This parameter is optional.
        /// <br/>Default value: <see cref="ResourceSetRetrieval.LoadIfExists"/>.</param>
        /// <param name="tryParents"><see langword="true"/>&#160;to use resource fallback to load an appropriate resource if the resource set cannot be found; <see langword="false"/>&#160;to bypass the resource fallback process. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>The resource set for the specified culture, or <see langword="null"/>&#160;if the specified culture cannot be retrieved by the defined <paramref name="behavior"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="behavior"/> does not fall in the expected range.</exception>
        /// <exception cref="MissingManifestResourceException">Resource file of the neutral culture was not found, while <paramref name="tryParents"/> is <see langword="true"/>
        /// and <paramref name="behavior"/> is not <see cref="ResourceSetRetrieval.CreateIfNotExists"/>.</exception>
        public IExpandoResourceSet? GetExpandoResourceSet(CultureInfo culture, ResourceSetRetrieval behavior = ResourceSetRetrieval.LoadIfExists, bool tryParents = false)
        {
            if (!Enum<ResourceSetRetrieval>.IsDefined(behavior))
                Throw.EnumArgumentOutOfRange(Argument.behavior, behavior);

            ResXResourceSet? result = GetResXResourceSet(culture, behavior, tryParents);

            // These properties are never taken from the stored sets so setting them only when the user obtains a ResXResourceSet instance.
            // It does not matter if they are changed by the user.
            if (result != null)
            {
                result.SafeMode = SafeMode;
                result.CloneValues = CloneValues;
            }

            return result;
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

        #region Internal Methods

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
        internal ResXResourceSet? GetResXResourceSet(CultureInfo culture, ResourceSetRetrieval behavior, bool tryParents)
        {
            #region Local Methods to reduce complexity

            bool TryGetCachedResourceSet(ref GetResXResourceSetContext ctx)
            {
                lock (SyncRoot)
                {
                    if (!TryGetResource(ctx.ResourceSets, ctx.Culture.Name, out ctx.Result))
                        return false;
                }

                // returning whether the cached resource is not a proxy or if the proxy does not have to be (possibly) replaced
                return ctx.Result is ResXResourceSet // result is not a proxy but an actual resource
                    || ctx.Behavior == ResourceSetRetrieval.GetIfAlreadyLoaded // nothing new should be loaded
                    || (ctx.Behavior == ResourceSetRetrieval.LoadIfExists
                        && ((ctx.Proxy = (ProxyResourceSet)ctx.Result).HierarchyLoaded // nothing new can be loaded in the hierarchy
                            || !ctx.TryParents && !ctx.Proxy.FileExists));
            }

            bool TryGetResourceWhileTraverse(ref GetResXResourceSetContext ctx)
            {
                ctx.FallbackManager = new ResourceFallbackManager(ctx.Culture, neutralResourcesCulture, ctx.TryParents);
                foreach (CultureInfo currentCultureInfo in ctx.FallbackManager)
                {
                    bool resourceFound;
                    lock (syncRoot!)
                        resourceFound = TryGetResource(ctx.ResourceSets, currentCultureInfo.Name, out ctx.Result);

                    if (resourceFound)
                    {
                        // a final result is found in the local cache
                        if (ctx.Result is ResXResourceSet)
                        {
                            // since the first try above we have a result from another thread for the searched culture
                            if (Equals(ctx.Culture, currentCultureInfo))
                                return true;

                            // after some proxies, a parent culture has been found: simply return if this was the proxied culture in the children
                            if (Equals(currentCultureInfo, ctx.FoundProxyCulture))
                                return true;

                            // otherwise, we found a parent: we need to re-create the proxies in the cache to the children
                            Debug.Assert(ctx.FoundProxyCulture == null, "There is a proxy with an inconsistent parent in the hierarchy.");
                            ctx.FoundCultureToAdd = currentCultureInfo;
                            return false;
                        }

                        // proxy is found
                        ctx.Proxy = (ProxyResourceSet)ctx.Result!;

                        // ReSharper disable once PossibleNullReferenceException - if not ResXResourceSet, then proxy
                        Debug.Assert(ctx.FoundProxyCulture == null || Equals(ctx.FoundProxyCulture, ctx.Proxy.WrappedCulture), "Proxied cultures are different in the hierarchy.");
                        ctx.FoundProxyCulture ??= ctx.Proxy.WrappedCulture;

                        // if we traversing here because last time the proxy has been loaded by
                        // ResourceSetRetrieval.GetIfAlreadyLoaded, but now we load the possible parents, we clear the
                        // CanHaveLoadableParent flag in the hierarchy. Unless no new proxy is created (and thus the descendant proxies are deleted),
                        // this will prevent the redundant traversal next time.
                        if (ctx.TryParents && ctx.Behavior == ResourceSetRetrieval.LoadIfExists)
                            ctx.Proxy.CanHaveLoadableParent = false;
                    }

                    Debug.Assert(ctx.FoundProxyCulture == null || ctx.Result != null, "There is a proxy without parent in the hierarchy.");
                    ctx.Result = GrovelForResourceSet(currentCultureInfo, ctx.Behavior != ResourceSetRetrieval.GetIfAlreadyLoaded, out bool exists);

                    if (ThrowException && ctx.TryParents && !exists && ctx.Behavior != ResourceSetRetrieval.CreateIfNotExists
                        && Equals(currentCultureInfo, CultureInfo.InvariantCulture))
                    {
                        Throw.MissingManifestResourceException(Res.ResourcesNeutralResourceFileNotFoundResX(GetResourceFileName(currentCultureInfo)));
                    }

                    // a new ResourceSet has been loaded; we're done
                    if (ctx.Result != null)
                    {
                        ctx.FoundCultureToAdd = currentCultureInfo;
                        return false;
                    }

                    // no resource is found but we need to create one
                    if (ctx.Behavior == ResourceSetRetrieval.CreateIfNotExists)
                    {
                        ctx.FoundCultureToAdd = currentCultureInfo;
                        ctx.Result = new ResXResourceSet(basePath: GetResourceDirName());
                        return false;
                    }
                }

                return false;
            }

            #endregion

            if (culture == null!)
                Throw.ArgumentNullException(Argument.culture);
            var context = new GetResXResourceSetContext { Culture = culture, Behavior = behavior, TryParents = tryParents, ResourceSets = ResourceSets };
            if (TryGetCachedResourceSet(ref context))
                return Unwrap(context.Result);

            if (TryGetResourceWhileTraverse(ref context))
                return Unwrap(context.Result);

            // there is a culture to be added to the cache
            if (context.FoundCultureToAdd != null)
            {
                lock (syncRoot!)
                {
                    // we replace a proxy: we must delete proxies, which are children of the found resource.
                    if (context.FoundProxyCulture != null)
                    {
                        Debug.Assert(!Equals(context.FoundProxyCulture, context.FoundCultureToAdd), "The culture to add is the same as the existing proxies.");
#if NET35
                        List<string> keysToRemove = context.ResourceSets.Cast<DictionaryEntry>().Where(item => item.Value is ProxyResourceSet && IsParentCulture(context.FoundCultureToAdd, item.Key.ToString())).Select(item => item.Key.ToString()).ToList();
#else
                        List<string> keysToRemove = context.ResourceSets.Where(item => item.Value is ProxyResourceSet && IsParentCulture(context.FoundCultureToAdd, item.Key)).Select(item => item.Key).ToList();
#endif

                        foreach (string key in keysToRemove)
                            context.ResourceSets.Remove(key);
                    }

                    // Add entries to the cache for the cultures we have gone through.
                    // context.FoundCultureToAdd now refers to the culture that had resources.
                    // Update cultures starting from requested culture up to the culture
                    // that had resources, but in place of non-found resources we will place a proxy.
                    foreach (CultureInfo updateCultureInfo in context.FallbackManager!)
                    {
                        // stop when we've added current or reached invariant (top of chain)
                        if (ReferenceEquals(updateCultureInfo, context.FoundCultureToAdd))
                        {
                            AddResourceSet(context.ResourceSets, updateCultureInfo.Name, ref context.Result!);
                            break;
                        }

                        ResourceSet newProxy = new ProxyResourceSet(Unwrap(context.Result)!, context.FoundCultureToAdd,
                            GetExistingResourceFileName(updateCultureInfo) != null, behavior == ResourceSetRetrieval.GetIfAlreadyLoaded);
                        AddResourceSet(context.ResourceSets, updateCultureInfo.Name, ref newProxy);
                    }

                    lastUsedResourceSet = default;
                }
            }

            return Unwrap(context.Result);
        }

        /// <summary>
        /// Creates an empty resource set for the given culture so it can be expanded.
        /// Does not make the resource set dirty until it is actually edited.
        /// </summary>
        internal ResXResourceSet CreateResourceSet(CultureInfo culture)
        {
            ResourceSet result = new ResXResourceSet(basePath: GetResourceDirName());
            lock (SyncRoot)
            {
                AddResourceSet(ResourceSets, culture.Name, ref result);
                lastUsedResourceSet = default;
            }

            Debug.Assert(result is ResXResourceSet, "AddResourceSet has replaced the ResXResourceSet to a proxy.");
            return (ResXResourceSet)result;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Generates the name of the resource file for the given <see cref="CultureInfo"/> object.
        /// </summary>
        /// <param name="culture">The culture object for which a resource file name is constructed.</param>
        /// <returns>
        /// The name that can be used for a resource file for the given <see cref="CultureInfo" /> object.
        /// </returns>
        protected override string GetResourceFileName(CultureInfo culture)
        {
            if (culture == null!)
                Throw.ArgumentNullException(Argument.culture);
            return GetResourceFileName(culture.Name);
        }

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
            // Internally just call the internal GetResXResourceSet instead. Via public methods GetExpandoResourceSet is called, which adjusts safe mode of the result accordingly to this instance.
            Debug.Assert(!ReferenceEquals(Assembly.GetCallingAssembly(), Assembly.GetExecutingAssembly()), "InternalGetResourceSet is called from CoreLibraries assembly.");

            // the base tries to parse the stream as binary. It would be better if GrovelForResourceSet
            // would be protected in base, so it would be enough to override only that one (at least in .NET 4 and above).
            // But actually that would not be enough because we cache the non-found cultures differently via a proxy.
            return GetResXResourceSet(culture, loadIfExists ? ResourceSetRetrieval.LoadIfExists : ResourceSetRetrieval.GetIfAlreadyLoaded, tryParents);
        }

        /// <summary>
        /// Releases the resources used by this <see cref="ResXResourceManager"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/>&#160;if this method is being called due to a call to <see cref="Dispose()"/>; otherwise, <see langword="false"/>.</param>
        protected virtual void Dispose(bool disposing)
        {
            var localResourceSets = InternalResourceSets;
            if (localResourceSets == null)
                return;

            if (disposing)
                ReleaseResourceSets(localResourceSets);

            ResourceSets = null;
            resxDirFullPath = null;
            syncRoot = null;
            lastUsedResourceSet = default;
        }

        #endregion

        #region Private Methods

#if NET35
        private void ResetResourceSets() => base.ResourceSets = new Hashtable();
#else
        private void ResetResourceSets() => resourceSets = new StringKeyedDictionary<ResourceSet>();
#endif

        private object? GetObjectInternal(string name, CultureInfo? culture, bool isString, bool cloneValue)
        {
            if (name == null!)
                Throw.ArgumentNullException(Argument.name);

            culture ??= CultureInfo.CurrentUICulture;
            ResXResourceSet? first = GetFirstResourceSet(culture);
            object? value;
            if (first != null)
            {
                value = first.GetResourceInternal(name, IgnoreCase, isString, SafeMode, cloneValue);
                if (value != null)
                    return value;
            }

            // The GetResXResourceSet has also a hierarchy traversal. This outer traversal is required as well because
            // the inner one can return an existing resource set without the searched resource, in which case here is
            // the fallback to the parent resource.
            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, neutralResourcesCulture, true);
            ResXResourceSet? toCache = null;
            foreach (CultureInfo currentCultureInfo in mgr)
            {
                ResXResourceSet? rs = GetResXResourceSet(currentCultureInfo, ResourceSetRetrieval.LoadIfExists, true);
                if (rs == null)
                    return null;

                if (rs == first)
                    continue;

                toCache ??= rs;
                value = rs.GetResourceInternal(name, IgnoreCase, isString, SafeMode, cloneValue);
                if (value != null)
                {
                    lock (syncRoot!)
                        lastUsedResourceSet = new KeyValuePair<string, ResXResourceSet>(culture.Name, toCache);

                    return value;
                }

                first = rs;
            }

            return null;
        }

        private object? GetMetaInternal(string name, CultureInfo? culture, bool isString, bool cloneValue)
        {
            if (name == null!)
                Throw.ArgumentNullException(Argument.name);

            // in case of metadata there is no hierarchy traversal so if there is no result trying to provoke the missing manifest exception
            ResXResourceSet? rs = GetResXResourceSet(culture ?? CultureInfo.InvariantCulture, ResourceSetRetrieval.LoadIfExists, false);
            if (rs == null && ThrowException)
                GetResXResourceSet(CultureInfo.InvariantCulture, ResourceSetRetrieval.GetIfAlreadyLoaded, true);
            return rs?.GetMetaInternal(name, IgnoreCase, isString, SafeMode, cloneValue);
        }

        /// <summary>
        /// Tries to get the first resource set in the traversal path from the caches.
        /// </summary>
        private ResXResourceSet? GetFirstResourceSet(CultureInfo culture)
        {
            // Logic from ResourceFallbackManager.GetEnumerator()
            if (!ReferenceEquals(culture, CultureInfo.InvariantCulture) && culture.Name == neutralResourcesCulture.Name)
                culture = CultureInfo.InvariantCulture;

            lock (SyncRoot)
            {
                if (culture.Name == lastUsedResourceSet.Key)
                    return lastUsedResourceSet.Value;

                // Look in the ResourceSet table
                var localResourceSets = ResourceSets; // this is Hashtable in .NET 3.5, Dictionary above
                if (!TryGetResource(localResourceSets, culture.Name, out ResourceSet? rs))
                    return null;

                // update the cache with the most recent ResourceSet
                ResXResourceSet result = Unwrap(rs)!;
                lastUsedResourceSet = new KeyValuePair<string, ResXResourceSet>(culture.Name, result);
                return result;
            }
        }

        private string GetResourceFileName(string cultureName)
        {
            StringBuilder result = new StringBuilder(BaseName, BaseName.Length + 32);
            if (CultureInfo.InvariantCulture.Name != cultureName)
            {
                result.Append('.');
                result.Append(cultureName);
            }

            result.Append(resXFileExtension);
            return Path.Combine(GetResourceDirName(), result.ToString());
        }

        private ResXResourceSet? GrovelForResourceSet(CultureInfo culture, bool loadIfExists, out bool exists)
        {
            string? fileName = GetExistingResourceFileName(culture);
            exists = fileName != null;
            try
            {
                return exists && loadIfExists ? new ResXResourceSet(fileName, GetResourceDirName()) : null;
            }
            catch (Exception e) when (!e.IsCritical())
            {
                // it still can throw MissingManifestResourceException from the caller
                if (IgnoreResXParseErrors)
                    return null;
                throw;
            }
        }

        private string? GetExistingResourceFileName(CultureInfo culture)
        {
            string fileName = GetResourceFileName(culture);
            bool exists = File.Exists(fileName);

            // fallback for neutral culture: if neutral file does not exist but specific does, using that file.
            if (!exists && Equals(CultureInfo.InvariantCulture, culture))
            {
                CultureInfo neutralResources = neutralResourcesCulture;
                if (!CultureInfo.InvariantCulture.Equals(neutralResources))
                {
                    string neutralFileName = GetResourceFileName(neutralResources);
                    if (File.Exists(neutralFileName))
                    {
                        fileName = neutralFileName;
                        exists = true;
                    }
                }
            }

            return exists ? fileName : null;
        }

        private string GetResourceDirName()
        {
            if (resxDirFullPath != null)
                return resxDirFullPath;

            if (String.IsNullOrEmpty(resxResourcesDir))
                resxDirFullPath = Files.GetExecutingPath();
            else if (Path.IsPathRooted(resxResourcesDir))
                resxDirFullPath = resxResourcesDir;
            else
                resxDirFullPath = Path.Combine(Files.GetExecutingPath(), resxResourcesDir);

            return resxDirFullPath;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {
            // Removing unmodified sets and proxies before serializing
            var resources = ResourceSets; // var is Hashtable in .NET 3.5, and is Dictionary above
            if (resources.Count == 0)
                return;

            lock (SyncRoot)
            {
                var keys = from res in resources // res.Key is object in .NET 3.5, and is string above
#if NET35
                    .Cast<DictionaryEntry>()
#endif

                    where res.Value is not ResXResourceSet resx || !resx.IsModified
                    select res.Key;

                foreach (var key in keys.ToList()) // key is object in .NET 3.5, and is string above
                    resources.Remove(key);
            }
        }

        #endregion

        #endregion

        #endregion
    }
}
