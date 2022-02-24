#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DynamicResourceManager.cs
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
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;
using System.Security;

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.Resources
{
    /// <summary>
    /// Represents a resource manager that provides convenient access to culture-specific resources at run time.
    /// As it is derived from <see cref="HybridResourceManager"/>, it can handle both compiled resources from <c>.dll</c> and
    /// <c>.exe</c> files, and XML resources from <c>.resx</c> files at the same time. Based on the selected strategies when a resource
    /// is not found in a language it can automatically add the missing resource from a base culture or even create completely new resource sets
    /// and save them into <c>.resx</c> files. For text entries the untranslated elements will be marked so they can be found easily for translation.
    /// <br/>See the <strong>Remarks</strong> section for examples and for the differences compared to <see cref="HybridResourceManager"/> class.
    /// </summary>
    /// <remarks>
    /// <para><see cref="DynamicResourceManager"/> class is derived from <see cref="HybridResourceManager"/> and adds the functionality
    /// of automatic appending the resources with the non-translated and/or unknown entries as well as
    /// auto-saving the changes. This makes possible to automatically create the .resx files if the language
    /// of the application is changed to a language, which has no translation yet. See also the static <see cref="LanguageSettings"/> class.
    /// The strategy of auto appending and saving can be chosen by the <see cref="AutoAppend"/>
    /// and <see cref="AutoSave"/> properties (see <see cref="AutoAppendOptions"/> and <see cref="AutoSaveOptions"/> enumerations).</para>
    /// <note type="tip">To see when to use the <see cref="ResXResourceReader"/>, <see cref="ResXResourceWriter"/>, <see cref="ResXResourceSet"/>,
    /// <see cref="ResXResourceManager"/>, <see cref="HybridResourceManager"/> and <see cref="DynamicResourceManager"/>
    /// classes see the documentation of the <see cref="N:KGySoft.Resources">KGySoft.Resources</see> namespace.</note>
    /// <para><see cref="DynamicResourceManager"/> combines the functionality of <see cref="ResourceManager"/>, <see cref="ResXResourceManager"/>, <see cref="HybridResourceManager"/>
    /// and extends these with the feature of auto expansion. It can be an ideal choice to use it as a resource manager of an application or a class library
    /// because it gives you freedom (or to the consumer of your library) to choose the strategy. If <see cref="AutoAppend"/> and <see cref="AutoSave"/> functionalities
    /// are completely disabled, then it is equivalent to a <see cref="HybridResourceManager"/>, which can handle resources both from compiled and XML sources but you must
    /// explicitly add new content and save it (see the example of the <see cref="HybridResourceManager"/> base class).
    /// If you restrict even the source of the resources, then you can get the functionality of the <see cref="ResXResourceManager"/> class (<see cref="Source"/> is <see cref="ResourceManagerSources.ResXOnly"/>),
    /// or the <see cref="ResourceManager"/> class (<see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>).</para>
    /// <h1 class="heading">Additional features compared to <see cref="HybridResourceManager"/></h1>
    /// <para><strong>Centralized vs. individual settings</strong>:
    /// <br/>The behavior of <see cref="DynamicResourceManager"/> instances can be controlled in two ways, which can be configured by the <see cref="UseLanguageSettings"/> property.
    /// <list type="bullet">
    /// <item><term>Individual control</term>
    /// <description>If <see cref="UseLanguageSettings"/> is <see langword="false"/>, which is the default value, then the <see cref="DynamicResourceManager"/> behavior is simply determined by its own properties.
    /// This can be alright for short-living <see cref="DynamicResourceManager"/> instances (for example, in a <see langword="using"/>&#160;block), or when you are sure you don't want let the
    /// consumers of your library to customize the settings of resource managers.</description></item>
    /// <item><term>Centralized control</term>
    /// <description>If you use the <see cref="DynamicResourceManager"/> class as the resource manager of a class library, you might want to let the consumers of your library to control
    /// how <see cref="DynamicResourceManager"/> instances should behave. For example, maybe one consumer wants to allow generating new .resx language files by using custom <see cref="AutoAppendOptions"/>,
    /// and another one does not want to let generating .resx files at all. If <see cref="UseLanguageSettings"/> is <see langword="true"/>, then <see cref="AutoAppend"/>, <see cref="AutoSave"/> and <see cref="Source"/>
    /// properties will be taken from the static <see cref="LanguageSettings"/> class (see <see cref="LanguageSettings.DynamicResourceManagersAutoAppend">LanguageSettings.DynamicResourceManagersAutoAppend</see>,
    /// <see cref="LanguageSettings.DynamicResourceManagersAutoSave">LanguageSettings.DynamicResourceManagersAutoSave</see> and <see cref="LanguageSettings.DynamicResourceManagersSource">LanguageSettings.DynamicResourceManagersSource</see>
    /// properties, respectively). This makes possible to control the behavior of <see cref="DynamicResourceManager"/> instances, which enable centralized settings,
    /// without exposing these manager instances of our class library to the public. See also the example at the <a href="#recommendation">Recommended usage for string resources in a class library</a> section.</description></item>
    /// </list>
    /// <note>
    /// <list type="bullet">
    /// <item>Unlike the <see cref="Source"/> property, <see cref="LanguageSettings.DynamicResourceManagersSource">LanguageSettings.DynamicResourceManagersSource</see> property is
    /// <see cref="ResourceManagerSources.CompiledOnly"/> by default, ensuring that centralized <see cref="DynamicResourceManager"/> instances work the same way as regular <see cref="ResourceManager"/>
    /// classes by default, so the application can opt-in dynamic creation of .resx files, for example in its <c>Main</c> method.</item>
    /// <item>Turning on the <see cref="UseLanguageSettings"/> property makes the <see cref="DynamicResourceManager"/> to subscribe multiple events. If such a <see cref="DynamicResourceManager"/>
    /// is used in a non-static or short-living context make sure to dispose it to prevent leaking resources.</item>
    /// </list></note></para>
    /// <para><strong>Auto Appending</strong>:
    /// <br/>The automatic expansion of the resources can be controlled by the <see cref="AutoAppend"/> property (or by <see cref="LanguageSettings.DynamicResourceManagersAutoAppend">LanguageSettings.DynamicResourceManagersAutoAppend</see>,
    /// property, if <see cref="UseLanguageSettings"/> is <see langword="true"/>), and it covers three different strategies, which can be combined:
    /// <list type="number">
    /// <item><term>Unknown resources</term>
    /// <description>If <see cref="AutoAppendOptions.AddUnknownToInvariantCulture"/> option is enabled and an unknown resource is requested, then the resource set
    /// of the invariant culture will be appended by the newly requested resource. It also means that <see cref="MissingManifestResourceException"/> will never be thrown,
    /// even if <see cref="HybridResourceManager.ThrowException"/> property is <see langword="true"/>. If the unknown resource is requested by <see cref="O:KGySoft.Resources.DynamicResourceManager.GetString">GetString</see>
    /// methods, then the value of the requested name will be the name itself prefixed by the <see cref="LanguageSettings.UnknownResourcePrefix">LanguageSettings.UnknownResourcePrefix</see> property.
    /// On the other hand, if the unknown resource is requested by the <see cref="O:KGySoft.Resources.DynamicResourceManager.GetObject">GetObject</see> methods, then a <see langword="null"/>&#160;value will be added.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using KGySoft.Resources;
    ///
    /// class Example
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         var manager = new DynamicResourceManager(typeof(Example));
    ///         manager.AutoAppend = AutoAppendOptions.AddUnknownToInvariantCulture;
    ///
    ///         // Without the option above the following line would throw a MissingManifestResourceException
    ///         Console.WriteLine(manager.GetString("UnknownString")); // prints [U]UnknownString
    ///         Console.WriteLine(manager.GetObject("UnknownObject")); // prints empty line
    ///
    ///         manager.SaveAllResources(compatibleFormat: false);
    ///     }
    /// }]]></code>
    /// The example above creates a <c>Resources\Example.resx</c> file under the binary output folder of the console application
    /// with the following content:
    /// <code lang="XML"><![CDATA[
    /// <?xml version="1.0"?>
    /// <root>
    ///   <data name="UnknownString">
    ///     <value>[U]UnknownString</value>
    ///   </data>
    ///   <assembly alias="KGySoft.CoreLibraries" name="KGySoft.CoreLibraries, Version=3.6.3.1, Culture=neutral, PublicKeyToken=b45eba277439ddfe" />
    ///   <data name="UnknownObject" type="KGySoft.Resources.ResXNullRef, KGySoft.Libraries">
    ///     <value />
    ///   </data>
    /// </root>]]></code></description></item>
    /// <item><term>Appending neutral and specific cultures</term>
    /// <description>The previous section was about expanding the resource file of the invariant culture represented by the <see cref="CultureInfo.InvariantCulture">CultureInfo.InvariantCulture</see> property.
    /// Every other <see cref="CultureInfo"/> instance can be classified as either a neutral or specific culture. Neutral cultures are region independent (eg. <c>en</c> is the English culture in general), whereas
    /// specific cultures are related to a specific region (eg. <c>en-US</c> is the American English culture). The parent of a specific culture can be another specific or a neutral one, and the parent
    /// of a neutral culture can be another neutral or the invariant culture. In most cases there is one specific and one neutral culture in a full chain, for example:
    /// <br/><c>en-US (specific) -> en (neutral) -> Invariant</c>
    /// <br/>But sometimes there can be more neutral cultures:
    /// <br/><c>ku-Arab-IR (specific) -> ku-Arab (neutral) -> ku (neutral) -> Invariant</c>
    /// <br/>Or more specific cultures:
    /// <br/><c>ca-ES-valencia (specific) -> ca-es (specific) -> ca (neutral) -> Invariant</c>
    /// <br/> There are two groups of options, which control where the untranslated resources should be merged from and to:
    /// <list type="number">
    /// <item><see cref="AutoAppendOptions.AppendFirstNeutralCulture"/>, <see cref="AutoAppendOptions.AppendLastNeutralCulture"/> and <see cref="AutoAppendOptions.AppendNeutralCultures"/> options
    /// will append the neutral cultures (eg. <c>en</c>) if a requested resource is found in the invariant culture.</item>
    /// <item><see cref="AutoAppendOptions.AppendFirstSpecificCulture"/> and <see cref="AutoAppendOptions.AppendSpecificCultures"/> options
    /// will append the specific cultures (eg. <c>en-US</c>) if a requested resource is found in any parent culture. <see cref="AutoAppendOptions.AppendLastSpecificCulture"/> does the same,
    /// except that the found resource must be in the resource set of a non-specific culture..</item>
    /// </list>
    /// If the merged resource is a <see cref="string"/>, then the value of the existing resource will be prefixed by the
    /// <see cref="LanguageSettings.UntranslatedResourcePrefix">LanguageSettings.UntranslatedResourcePrefix</see> property, and
    /// this prefixed string will be saved in the target resource; otherwise, the original value will be duplicated in the target resource.
    /// <note>"First" and "last" terms above refer the first and last neutral/specific cultures in the order from
    /// most specific to least specific one as in the examples above. See the descriptions of the referred <see cref="AutoAppendOptions"/> options for more details and for examples
    /// with a fully artificial culture hierarchy with multiple neutral and specific cultures.</note>
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Globalization;
    /// using KGySoft;
    /// using KGySoft.Resources;
    ///
    /// class Example
    /// {
    ///     private static CultureInfo enUS = CultureInfo.GetCultureInfo("en-US");
    ///     private static CultureInfo en = enUS.Parent;
    ///     private static CultureInfo inv = en.Parent;
    ///
    ///     static void Main(string[] args)
    ///     {
    ///         var manager = new DynamicResourceManager(typeof(Example));
    ///
    ///         // this will cause to copy the non-existing entries from invariant to "en"
    ///         manager.AutoAppend = AutoAppendOptions.AppendFirstNeutralCulture;
    ///
    ///         // preparation
    ///         manager.SetObject("TestString", "Test string in invariant culture", inv);
    ///         manager.SetObject("TestString", "Test string in English culture", en);
    ///         manager.SetObject("TestString2", "Another test string in invariant culture", inv);
    ///         manager.SetObject("TestObject", 42, inv);
    ///         manager.SetObject("DontCareObject", new byte[0], inv);
    ///
    ///         // setting the UI culture so we do not need to specify the culture in GetString/Object
    ///         LanguageSettings.DisplayLanguage = enUS;
    ///
    ///         Console.WriteLine(manager.GetString("TestString")); // already exists in en
    ///         Console.WriteLine(manager.GetString("TestString2")); // copied to en with prefix
    ///         Console.WriteLine(manager.GetObject("TestObject")); // copied to en
    ///         // Console.WriteLine(manager.GetObject("DontCareObject")); // not copied because not accessed
    ///
    ///         // saving the changes
    ///         manager.SaveAllResources(compatibleFormat: false);
    ///     }
    /// }]]></code>
    /// The example above creates the <c>Example.resx</c> and <c>Example.en.resx</c> files.
    /// No <c>Example.en-US.resx</c> is created because we chose appending the first neutral culture only. The content of <c>Example.en.resx</c> will be the following:
    /// <code lang="XML"><![CDATA[
    /// <?xml version="1.0"?>
    /// <root>
    ///   <data name="TestString">
    ///     <value>Test string in English culture</value>
    ///   </data>
    ///   <data name="TestString2">
    ///     <value>[T]Another test string in invariant culture</value>
    ///   </data>
    ///   <data name="TestObject" type="System.Int32">
    ///     <value>42</value>
    ///   </data>
    /// </root>]]></code>
    /// By looking for '<c>[T]</c>' occurrences we can easily find the merged strings to translate.</description></item>
    /// <item><term>Merging complete resource sets</term>
    /// <description>The example above demonstrates how the untranslated entries will be applied to the target language files. However, in that example only the
    /// actually requested entries will be copied on demand. It is possible that we want to generate a full language file in order to be able to make complete translations.
    /// If that is what we need we can use the <see cref="AutoAppendOptions.AppendOnLoad"/> option. This option should be used together with at least one of the options from
    /// the previous point to have any effect.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Globalization;
    /// using System.Resources;
    /// using KGySoft;
    /// using KGySoft.Resources;
    ///
    /// // we can tell what the language of the invariant resource is
    /// [assembly: NeutralResourcesLanguage("en")]
    ///
    /// class Example
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         var manager = new DynamicResourceManager(typeof(Example));
    ///
    ///         // actually this is the default option:
    ///         manager.AutoAppend = AutoAppendOptions.AppendFirstNeutralCulture | AutoAppendOptions.AppendOnLoad;
    ///
    ///         // we prepare only the invariant resource
    ///         manager.SetObject("TestString", "Test string", CultureInfo.InvariantCulture);
    ///         manager.SetObject("TestString2", "Another test string", CultureInfo.InvariantCulture);
    ///         manager.SetObject("TestObject", 42, CultureInfo.InvariantCulture);
    ///         manager.SetObject("AnotherObject", new byte[0], CultureInfo.InvariantCulture);
    ///
    ///         // Getting an English resource will not create the en.resx file because this is
    ///         // the default language of our application thanks to the NeutralResourcesLanguage attribute
    ///         LanguageSettings.DisplayLanguage = CultureInfo.GetCultureInfo("en-US");
    ///         Console.WriteLine(manager.GetString("TestString")); // Displays "Test string", no resource is created
    ///
    ///         LanguageSettings.DisplayLanguage = CultureInfo.GetCultureInfo("fr-FR");
    ///         Console.WriteLine(manager.GetString("TestString")); // Displays "[T]Test string", resource "fr" is created
    ///
    ///         // saving the changes
    ///         manager.SaveAllResources(compatibleFormat: false);
    ///     }
    /// }]]></code>
    /// The example above creates <c>Example.resx</c> and <c>Example.fr.resx</c> files. Please note that no <c>Example.en.resx</c> is created because
    /// the <see cref="NeutralResourcesLanguageAttribute"/> indicates that the language of the invariant resource is English. If we open the
    /// created <c>Example.fr.resx</c> file we can see that every resource was copied from the invariant resource even though we accessed a single item:
    /// <code lang="XML"><![CDATA[
    /// <?xml version="1.0"?>
    /// <root>
    ///   <data name="TestString">
    ///     <value>[T]Test string</value>
    ///   </data>
    ///   <data name="TestString2">
    ///     <value>[T]Another test string</value>
    ///   </data>
    ///   <data name="TestObject" type="System.Int32">
    ///     <value>42</value>
    ///   </data>
    ///   <data name="AnotherObject" type="System.Byte[]">
    ///     <value />
    ///   </data>
    /// </root>]]></code></description></item>
    /// </list>
    /// <note>Please note that auto appending affects resources only. Metadata are never merged.</note>
    /// </para>
    /// <para><strong>Auto Saving</strong>:
    /// <br/>By setting the <see cref="AutoSave"/> property (or <see cref="LanguageSettings.DynamicResourceManagersAutoSave">LanguageSettings.DynamicResourceManagersAutoSave</see>,
    /// if <see cref="UseLanguageSettings"/> is <see langword="true"/>), the <see cref="DynamicResourceManager"/> is able to save the dynamically created content automatically on specific events:
    /// <list type="bullet">
    /// <item><term>Changing the display language</term>
    /// <description>If <see cref="AutoSaveOptions.LanguageChange"/> option is enabled the changes are saved whenever the current UI culture is set via the
    /// <see cref="LanguageSettings.DisplayLanguage">LanguageSettings.DisplayLanguage</see> property.
    /// <note>Enabling this option makes the <see cref="DynamicResourceManager"/> to subscribe to the static <see cref="LanguageSettings.DisplayLanguageChanged">LanguageSettings.DisplayLanguageChanged</see>
    /// event. To prevent leaking resources make sure to dispose the <see cref="DynamicResourceManager"/> if it is used in a non-static or short-living context.</note>
    /// </description></item>
    /// <item><term>Application exit</term>
    /// <description>If <see cref="AutoSaveOptions.DomainUnload"/> option is enabled the changes are saved when current <see cref="AppDomain"/> is being unloaded, including the case when
    /// the application exits.
    /// <note>Enabling this option makes the <see cref="DynamicResourceManager"/> to subscribe to the <see cref="AppDomain.ProcessExit">AppDomain.ProcessExit</see>
    /// or <see cref="AppDomain.DomainUnload">AppDomain.DomainUnload</see> event. To prevent leaking resources make sure to dispose the <see cref="DynamicResourceManager"/> if it is used in a non-static or short-living context.
    /// However, to utilize saving changes on application exit or domain unload, <see cref="DynamicResourceManager"/> is best to be used in a static context.</note>
    /// </description></item>
    /// <item><term>Changing resource source</term>
    /// <description>If <see cref="Source"/> property changes it may cause data loss in terms of unsaved changes. To prevent this <see cref="AutoSaveOptions.SourceChange"/> option can be enabled
    /// so the changes will be saved before actualizing the new value of the <see cref="Source"/> property.</description></item>
    /// <item><term>Disposing</term>
    /// <description>Enabling the <see cref="AutoSaveOptions.Dispose"/> option makes possible to save changes automatically when the <see cref="DynamicResourceManager"/> is being disposed.
    /// <code lang="C#"><![CDATA[
    /// using System.Globalization;
    /// using KGySoft.Resources;
    ///
    /// class Example
    /// {
    ///     static void Main(string[] args)
    ///     {
    ///         // thanks to the AutoSave = Dispose the Example.resx will be created at the end of the using block
    ///         using (var manager = new DynamicResourceManager(typeof(Example)) { AutoSave = AutoSaveOptions.Dispose })
    ///         {
    ///             manager.SetObject("Test string", "Test value", CultureInfo.InvariantCulture);
    ///         }
    ///     }
    /// }]]></code></description></item></list></para>
    /// <para>Considering <see cref="HybridResourceManager.SaveAllResources">SaveAllResources</see> is indirectly called on auto save, you cannot set its parameters directly.
    /// However, by setting the <see cref="CompatibleFormat"/> property, you can tell whether the result .resx files should be able to be read by a
    /// <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a> instance
    /// and the Visual Studio Resource Editor. If it is <see langword="false"/>&#160;the result .resx files are often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />),
    /// but the result can be read only by the <see cref="ResXResourceReader" /> class.</para>
    /// <para>Normally, if you save the changes by the <see cref="HybridResourceManager.SaveAllResources">SaveAllResources</see> method, you can handle the possible exceptions locally.
    /// To handle errors occurred during auto save you can subscribe the <see cref="AutoSaveError"/> event.</para>
    /// <h1 class="heading">Recommended usage for string resources in a class library<a name="recommendation">&#160;</a></h1>
    /// <para>A class library can be used by any consumers who want to use the features of that library. If it contains resources it can be useful if we allow the consumer
    /// of our class library to create translations for it in any language. In an application it can be the decision of the consumer whether generating new XML resource (.resx) files
    /// should be allowed or not. If so, we must be prepared for invalid files or malicious content (for example, the .resx file can contain serialized data of any type, whose constructor
    /// can run any code when deserialized). The following example takes all of these aspects into consideration.
    /// <note>In the following example there is a single compiled resource created in a class library, without any satellite assemblies. The additional language files
    /// can be generated at run-time if the consumer application allows it. </note>
    /// <list type="number">
    /// <item>
    /// Create a new project (Class Library)
    /// <br/><img src="../Help/Images/NewClassLibrary.png" alt="New class library"/></item>
    /// <item>Delete the automatically created <c>Class1.cs</c></item>
    /// <item>Create a new class: <c>Res</c>. The class will be <see langword="static"/>&#160;and <see langword="internal"/>, and it will contain the resource manager for
    /// this class library. The initial content of the file will be the following:
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using KGySoft;
    /// using KGySoft.Resources;
    /// 
    /// namespace ClassLibrary1
    /// {
    ///     internal static class Res
    ///     {
    ///         // internal resources for errors
    ///         private const string unavailableResource = "Resource ID not found: {0}";
    ///         private const string invalidResource = "Resource text is not valid for {0} arguments: {1}";
    /// 
    ///         private static readonly DynamicResourceManager resourceManager =
    ///             // the name of the compiled resources must match (see also point 5)
    ///             new DynamicResourceManager("ClassLibrary1.Messages", typeof(Res).Assembly)
    ///             {
    ///                 SafeMode = true,
    ///                 UseLanguageSettings = true,
    ///                 ThrowException = false,
    ///                 // CompatibleFormat = true // use this if you want to edit the result files with VS resource editor
    ///             };
    /// 
    ///         // Here will be the properties for the resources. This one is private because used from this class.
    ///         private static string NullReference => Get(nameof(NullReference));
    /// 
    ///         // [...] Your resources can be added here (see point 8. and 9.)
    /// 
    ///         private static string Get(string id) =>
    ///             resourceManager.GetString(id, LanguageSettings.DisplayLanguage) ?? String.Format(unavailableResource, id);
    /// 
    ///         private static string Get(string id, params object[] args)
    ///         {
    ///             string format = Get(id);
    ///             return args == null || args.Length == 0 ? format : SafeFormat(format, args);
    ///         }
    /// 
    ///         private static string SafeFormat(string format, object[] args)
    ///         {
    ///             try
    ///             {
    ///                 int i = Array.IndexOf(args, null);
    ///                 if (i >= 0)
    ///                 {
    ///                     string nullRef = Get(NullReference);
    ///                     for (; i < args.Length; i++)
    ///                     {
    ///                         if (args[i] == null)
    ///                             args[i] = nullRef;
    ///                     }
    ///                 }
    /// 
    ///                 return String.Format(LanguageSettings.FormattingLanguage, format, args);
    ///             }
    ///             catch (FormatException)
    ///             {
    ///                 return String.Format(invalidResource, args.Length, format);
    ///             }
    ///         }
    ///     }
    /// }]]></code></item>
    /// <item>In Solution Explorer right click on <c>ClassLibrary1</c>, then select Add, New Item, Resources File.
    /// <br/><img src="../Help/Images/NewResourcesFile.png" alt="New Resources file"/></item>
    /// <item>To make sure the compiled name of the resource is what you want (it must match the name in the <see cref="DynamicResourceManager"/> constructor)
    /// you can edit the .csproj file as follows:
    /// <list type="bullet">
    /// <item>In Solution Explorer right click on <c>ClassLibrary1</c>, Unload Project</item>
    /// <item>In Solution Explorer right click on <c>ClassLibrary1 (unavailable)</c>, Edit ClassLibrary1.csproj</item>
    /// <item>Search for the <c>EmbeddedResource</c> element and edit it as follows (or in case of a .NET Core project add it if it does not exist):
    /// <code lang="XML"><![CDATA[
    /// <!-- .NET Framework project: -->
    /// <EmbeddedResource Include="Resource1.resx" >
    ///   <LogicalName>ClassLibrary1.Messages.resources</LogicalName>
    /// </EmbeddedResource>
    ///
    /// <!-- .NET Core project (note "Update" in place of "Include"): -->
    /// <EmbeddedResource Update="Resource1.resx" >
    ///   <LogicalName>ClassLibrary1.Messages.resources</LogicalName>
    /// </EmbeddedResource>]]></code></item>
    /// <item>In Solution Explorer right click on <c>ClassLibrary1 (unavailable)</c>, Reload ClassLibrary1.csproj</item>
    /// </list>
    /// </item>
    /// <item>In Solution Explorer right click on the new resource file (<c>Resource1.resx</c>) and select Properties</item>
    /// <item>Clear the default <c>Custom Tool</c> value because the generated file uses a <see cref="ResourceManager"/> class internally, which cannot handle the dynamic expansions.
    /// It means also, that instead of the generated <c>Resources</c> class we will use our <c>Res</c> class.
    /// Leave the <c>Build Action</c> so its value is <c>Embedded Resource</c>, which means that the resource will be compiled into the assembly.
    /// The <see cref="DynamicResourceManager"/> will use these compiled resources as default values. If the consumer application of our class library sets the
    /// <see cref="LanguageSettings.DynamicResourceManagersSource">LanguageSettings.DynamicResourceManagersSource</see> property to <see cref="ResourceManagerSources.CompiledAndResX"/>,
    /// then for the different languages the .resx files will be automatically created containing the resource set of our class library, ready to translate.
    /// <br/><img src="../Help/Images/ResourceFileProperties_DynamicResourceManager.png" alt="Resources1.resx properties"/></item>
    /// <item>In Solution Explorer double click on <c>Resource1.resx</c> and add any resource entries you want to use in your library.
    /// Add <c>NullReference</c> key as well as it is used by the default <c>Res</c> implementation.
    /// <br/><img src="../Help/Images/DynamicResourceManager_ExampleResources.png" alt="Example resources"/></item>
    /// <item>Define a property for all of your simple resources and a method for the format strings with placeholders in the <c>Res</c> class. For example:
    /// <code lang="C#"><![CDATA[
    /// // simple resource: can be a property
    /// internal static string MyResourceExample => Get(nameof(MyResourceExample));
    /// 
    /// // resource format string with placeholders: can be a method
    /// internal static string MyResourceFormatExample(int arg1, string arg2) => Get(nameof(MyResourceFormatExample), arg1, arg2);]]></code></item>
    /// <item>You can retrieve any resources in your library as it is shown in the example below:
    /// <code lang="C#"><![CDATA[
    /// using System;
    ///
    /// namespace ClassLibrary1
    /// {
    ///     public class Example
    ///     {
    ///         public void SomeMethod(int someParameter)
    ///         {
    ///             if (someParameter == 42)
    ///                 // simple resource
    ///                 throw new InvalidOperationException(Res.MyResourceExample);
    ///             if (someParameter == -42)
    ///                 // formatted resource - enough arguments must be specified for placeholders (though errors are handled in Res)
    ///                 throw new Throw.ArgumentException(Res.MyResourceFormatExample(123, "x"), nameof(someParameter));
    ///         }
    ///     }
    /// }]]></code></item>
    /// <item>To indicate the language of your default compiled resource open the <c>AssemblyInfo.cs</c> of your project and add the following line:
    /// <code lang="C#"><![CDATA[
    /// // this informs the resource manager about the language of the default culture
    /// [assembly:NeutralResourcesLanguage("en")]]]></code></item>
    /// <item>Now a consumer application can enable dynamic resource creation for new languages. Create a new console application, add reference to <c>ClassLibrary1</c> project
    /// and edit the <c>Program.cs</c> file as follows:
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Globalization;
    /// using ClassLibrary1;
    /// using KGySoft;
    /// using KGySoft.Resources;
    ///
    /// namespace ConsoleApp1
    /// {
    ///     class Program
    ///     {
    ///         static void Main(string[] args)
    ///         {
    ///             // Enabling dynamic .resx creation for all DynamicResourceManager instances in this application,
    ///             // which are configured to use centralized settings
    ///             LanguageSettings.DynamicResourceManagersSource = ResourceManagerSources.CompiledAndResX;
    ///
    ///             // Setting the language of the application
    ///             LanguageSettings.DisplayLanguage = CultureInfo.GetCultureInfo("fr-FR");
    ///
    ///             LaunchMyApplication();
    ///         }
    ///
    ///         private static void LaunchMyApplication()
    ///         {
    ///             // if we use anything that has a reference to the Res class in ClassLibrary1, then
    ///             // a .resx file with the language of the application will be used or created if not exists yet.
    ///             var example = new Example();
    ///             try
    ///             {
    ///                 example.SomeMethod(42);
    ///             }
    ///             catch (Exception e)
    ///             {
    ///                 Console.WriteLine($"{e.GetType().Name}: {e.Message}");
    ///             }
    ///         }
    ///     }
    /// }]]></code>
    /// When the console application above exits, it creates a <c>Resources\ClassLibrary1.Messages.fr.resx</c> file with the following content:
    /// <code lang="XML"><![CDATA[
    /// <?xml version="1.0"?>
    /// <root>
    ///   <data name="NullReference">
    ///     <value>[T]&lt;null&gt;</value>
    ///   </data>
    ///   <data name="MyResourceExample">
    ///     <value>[T]This is a resource value will be used in my library</value>
    ///   </data>
    ///   <data name="MyResourceFormatExample">
    ///     <value>[T]This is a resource format string with two placeholders: {0}, {1}</value>
    ///   </data>
    /// </root>]]></code>
    /// By looking for the '<c>[T]</c>' prefixes you can easily find the untranslated elements.</item></list></para>
    /// <note type="tip"><list type="bullet">
    /// <item>You can use the <see cref="EnsureResourcesGenerated">EnsureResourcesGenerated</see> method to create possible non-existing resources for a language.</item>
    /// <item>You can use the <see cref="EnsureInvariantResourcesMerged">EnsureInvariantResourcesMerged</see> method to forcibly merge all resource entries in the invariant
    /// resource set for a language. This can be useful if new resources have been introduced since a previous version and the newly introduced entries also have to be added to the localized resource sets.</item>
    /// <item>To see how to use dynamically created resources for any language in a live application with editing support see
    /// the <a href="https://github.com/koszeggy/KGySoft.Drawing.Tools" target="_blank">KGySoft.Drawing.Tools</a> GitHub repository.</item>
    /// </list></note>
    /// </remarks>
    [Serializable]
    public class DynamicResourceManager : HybridResourceManager
    {
        #region Nested Types

        #region GetObjectWithAppendContext Struct

        private struct GetObjectWithAppendContext
        {
            #region Fields

            internal string Name;
            internal CultureInfo Culture;
            internal bool IsString;
            internal bool CloneValue;
            internal bool SafeMode;
            internal AutoAppendOptions Options;

            internal object? Result;
            internal ResourceSet? CheckedResource;
            internal ResourceSet? ToCache;
            internal Stack<IExpandoResourceSet>? ToMerge;

            #endregion
        }

        #endregion

        #region EnsureLoadedWithMergeContext Struct

        private struct EnsureLoadedWithMergeContext
        {
            #region Fields

            internal CultureInfo Culture;
            internal AutoAppendOptions Options;

            internal Stack<KeyValuePair<CultureInfo, bool>>? ToMerge;
            internal CultureInfo? StopMerge;

            #endregion
        }

        #endregion

        #endregion

        #region Fields

        private bool useLanguageSettings;
        private volatile bool canAcceptProxy = true;
        private AutoSaveOptions autoSave;
        private AutoAppendOptions autoAppend = LanguageSettings.AutoAppendDefault;

        /// <summary>
        /// If <see cref="autoAppend"/> contains <see cref="AutoAppendOptions.AppendOnLoad"/> flag,
        /// contains the up-to-date cultures. Value is <see langword="true"/>&#160;if that culture is merged so it can be taken as a base for merge.
        /// </summary>
        private Dictionary<CultureInfo, bool>? mergedCultures;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when an exception is thrown on auto saving. If this event is not subscribed, the following exception types are automatically suppressed,
        /// as they can occur on save: <see cref="IOException"/>, <see cref="SecurityException"/>, <see cref="UnauthorizedAccessException"/>. If such an
        /// exception is suppressed some resources might remain unsaved. Though the event is a static one, the sender of the handler is the corresponding <see cref="DynamicResourceManager"/> instance.
        /// Thus the save failures of the non public <see cref="DynamicResourceManager"/> instances (eg. resource managers of an assembly) can be tracked, too.
        /// </summary>
        /// <seealso cref="AutoSave"/>
        public static event EventHandler<AutoSaveErrorEventArgs>? AutoSaveError;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets whether values of <see cref="AutoAppend"/>, <see cref="AutoSave"/> and <see cref="Source"/> properties
        /// are taken centrally from the <see cref="LanguageSettings"/> class.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <seealso cref="LanguageSettings.DynamicResourceManagersSource"/>
        /// <seealso cref="LanguageSettings.DynamicResourceManagersAutoAppend"/>
        /// <seealso cref="LanguageSettings.DynamicResourceManagersAutoSave"/>
        /// <remarks><note>For examples see the <em>Centralized vs. individual settings</em> and <em>Recommended usage for string resources in a class library</em>
        /// sections at the description of the <see cref="DynamicResourceManager"/> class.</note></remarks>
        public bool UseLanguageSettings
        {
            get => useLanguageSettings;
            set
            {
                if (value == useLanguageSettings)
                    return;

                lock (SyncRoot)
                {
                    useLanguageSettings = value;
                    UnhookEvents();
                    if (value)
                    {
                        if (base.Source != LanguageSettings.DynamicResourceManagersSource)
                            SetSource(LanguageSettings.DynamicResourceManagersSource);
                        if (LanguageSettings.DynamicResourceManagersAutoAppend.IsWidening(autoAppend))
                            mergedCultures = null;
                        string? dir = LanguageSettings.DynamicResourceManagersResXResourcesDir;
                        if (dir != null)
                            ResXResourcesDir = dir;
                    }
                    else if (autoAppend.IsWidening(LanguageSettings.DynamicResourceManagersAutoAppend))
                        mergedCultures = null;

                    HookEvents();
                }
            }
        }

        /// <summary>
        /// When <see cref="UseLanguageSettings"/> is <see langword="false"/>,
        /// gets or sets the auto saving options.
        /// When <see cref="UseLanguageSettings"/> is <see langword="true"/>,
        /// auto saving is controlled by <see cref="LanguageSettings.DynamicResourceManagersAutoSave">LanguageSettings.DynamicResourceManagersAutoSave</see> property.
        /// <br/>Default value: <see cref="AutoSaveOptions.None"/>
        /// </summary>
        /// <seealso cref="AutoSaveError"/>
        /// <exception cref="InvalidOperationException">The property is set and <see cref="UseLanguageSettings"/> is <see langword="true"/>.</exception>
        /// <remarks>The default value of this property is <see cref="AutoSaveOptions.None"/>. Though, if <see cref="UseLanguageSettings"/> is <see langword="true"/>,
        /// the <see cref="LanguageSettings.DynamicResourceManagersAutoSave">LanguageSettings.DynamicResourceManagersAutoSave</see> property is used, whose default value is
        /// <see cref="AutoSaveOptions.LanguageChange"/>, <see cref="AutoSaveOptions.DomainUnload"/>, <see cref="AutoSaveOptions.SourceChange"/>.
        /// <note>For more details see the <em>Auto Saving</em> section at the description of the <see cref="DynamicResourceManager"/> class.</note></remarks>
        public AutoSaveOptions AutoSave
        {
            get => useLanguageSettings ? LanguageSettings.DynamicResourceManagersAutoSave : autoSave;
            set
            {
                if (useLanguageSettings)
                    Throw.InvalidOperationException(Res.ResourcesInvalidDrmPropertyChange);

                if (autoSave == value)
                    return;

                if (!value.AllFlagsDefined())
                    Throw.FlagsEnumArgumentOutOfRange(Argument.value, value);

                lock (SyncRoot)
                {
                    UnhookEvents();
                    autoSave = value;
                    HookEvents();
                }
            }
        }

        /// <summary>
        /// When <see cref="UseLanguageSettings"/> is <see langword="false"/>,
        /// gets or sets the resource auto append options.
        /// When <see cref="UseLanguageSettings"/> is <see langword="true"/>,
        /// auto appending of resources is controlled by <see cref="LanguageSettings.DynamicResourceManagersAutoAppend">LanguageSettings.DynamicResourceManagersAutoAppend</see> property.
        /// <br/>
        /// Default value: <see cref="AutoAppendOptions.AppendFirstNeutralCulture"/>, <see cref="AutoAppendOptions.AppendOnLoad"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">The property is set and <see cref="UseLanguageSettings"/> is <see langword="true"/>.</exception>
        /// <remarks>
        /// <para>Auto appending affects the resources only. Metadata are never merged.</para>
        /// <para>Auto appending options are ignored if <see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/></para>
        /// <para><note>For more details see the <em>Auto Appending</em> section at the description of the <see cref="DynamicResourceManager"/> class.</note></para>
        /// </remarks>
        /// <seealso cref="AutoAppendOptions"/>
        public AutoAppendOptions AutoAppend
        {
            get => useLanguageSettings ? LanguageSettings.DynamicResourceManagersAutoAppend : autoAppend;
            set
            {
                if (useLanguageSettings)
                    Throw.InvalidOperationException(Res.ResourcesInvalidDrmPropertyChange);

                if (autoAppend == value)
                    return;

                value.CheckOptions();
                if (autoAppend.IsWidening(value))
                {
                    mergedCultures = null;
                    if (IsAnyProxyLoaded())
                        canAcceptProxy = false;
                }

                autoAppend = value;
            }
        }

        /// <summary>
        /// When <see cref="UseLanguageSettings"/> is <see langword="false"/>,
        /// gets or sets the source, from which the resources should be taken.
        /// When <see cref="UseLanguageSettings"/> is <see langword="true"/>,
        /// the source is controlled by <see cref="LanguageSettings.DynamicResourceManagersSource">LanguageSettings.DynamicResourceManagersSource</see> property.
        /// <br/>Default value: <see cref="ResourceManagerSources.CompiledAndResX"/>
        /// </summary>
        /// <seealso cref="UseLanguageSettings"/>
        /// <seealso cref="LanguageSettings.DynamicResourceManagersSource"/>
        /// <exception cref="InvalidOperationException">The property is set and <see cref="UseLanguageSettings"/> is <see langword="true"/>.</exception>
        /// <remarks>The default value of this property is <see cref="ResourceManagerSources.CompiledAndResX"/>. Though, if <see cref="UseLanguageSettings"/> is <see langword="true"/>,
        /// the <see cref="LanguageSettings.DynamicResourceManagersSource">LanguageSettings.DynamicResourceManagersSource</see> property is used, whose default value is
        /// <see cref="ResourceManagerSources.CompiledOnly"/>.</remarks>
        public override ResourceManagerSources Source
        {
            get => useLanguageSettings ? LanguageSettings.DynamicResourceManagersSource : base.Source;
            set
            {
                if (useLanguageSettings)
                    Throw.InvalidOperationException(Res.ResourcesInvalidDrmPropertyChange);

                base.Source = value;
            }
        }

        /// <summary>
        /// Gets or sets whether the .resx files should use a compatible format when the resources are automatically saved.
        /// <br/>Default value: <see langword="false"/>
        /// </summary>
        /// <remarks>
        /// <para>The changes can be always saved by the <see cref="HybridResourceManager.SaveAllResources">SaveAllResources</see> method, where compatible format
        /// can be explicitly requested. This property affects the result of auto saving controlled by the <see cref="AutoSave"/> property.</para>
        /// <para>If <see cref="CompatibleFormat"/> is <see langword="true"/>&#160;the result .resx files can be read by a <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxresourcereader" target="_blank">System.Resources.ResXResourceReader</a>
        /// instance and the Visual Studio Resource Editor. If it is <see langword="false"/>, the result .resx files are often shorter, and the values can be deserialized with better accuracy (see the remarks at <see cref="ResXResourceWriter" />),
        /// but the result can be read only by the <see cref="ResXResourceReader"/> class.</para>
        /// </remarks>
        public bool CompatibleFormat { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of <see cref="DynamicResourceManager"/> class that looks up resources in
        /// compiled assemblies and resource XML files based on information from the specified <paramref name="baseName"/>
        /// and <paramref name="assembly"/>.
        /// </summary>
        /// <param name="baseName">A root name that is the prefix of the resource files without the extension.</param>
        /// <param name="assembly">The main assembly for the resources.</param>
        /// <param name="explicitResXBaseFileName">When <see langword="null"/>&#160;the .resx file name will be constructed based on the
        /// <paramref name="baseName"/> parameter; otherwise, the given <see cref="string"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        public DynamicResourceManager(string baseName, Assembly assembly, string? explicitResXBaseFileName = null)
            : base(baseName, assembly, explicitResXBaseFileName)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="DynamicResourceManager"/> class that looks up resources in
        /// compiled assemblies and resource XML files based on information from the specified <see cref="Type"/> object.
        /// </summary>
        /// <param name="resourceSource">A type from which the resource manager derives all information for finding resource files.</param>
        /// <param name="explicitResXBaseFileName">When <see langword="null"/>&#160;the .resx file name will be constructed based on the
        /// <paramref name="resourceSource"/> parameter; otherwise, the given <see cref="string"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        public DynamicResourceManager(Type resourceSource, string? explicitResXBaseFileName = null)
            : base(resourceSource, explicitResXBaseFileName)
        {
        }

        #endregion

        #region Methods

        #region Static Methods

        private static void ToDictionary(ResourceSet source, StringKeyedDictionary<object?> target)
        {
            IDictionaryEnumerator enumerator = source.GetEnumerator();
            if (source is IExpandoResourceSetInternal expandoResourceSet)
            {
                // when merging resource sets, always cloning the values because they meant to be different (and when loading from file later they will be)
                while (enumerator.MoveNext())
                {
                    string key = (string)enumerator.Key;
                    target[key] = expandoResourceSet.GetResource(key, false, false, true, true);
                }

                return;
            }

            // fallback for non-expando source
            while (enumerator.MoveNext())
                target[(string)enumerator.Key] = enumerator.Value;
        }

        [SuppressMessage("ReSharper", "UsePatternMatching", Justification = "False alarm, cannot use pattern matching because type must be nullable and 'value is string ? result' is invalid")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "ReSharper issue")]
        [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "False alarm, cannot use pattern matching because type must be nullable and 'value is string ? result' is invalid")]
        private static object? AsString(object? value)
        {
            string? result = value as string;
            if (result != null)
                return result;

            // even if a FileRef contains a string, we cannot add prefix to the referenced file so returning null here
            if (value is not ResXDataNode node || node.FileRef != null)
                return null;

            // already deserialized: returning the string or null
            result = node.ValueInternal as string;
            if (node.ValueInternal != null)
                return result;

            // the default way of storing a string
            if (node.TypeName == null && node.MimeType == null && node.ValueData != null)
                return node.ValueData;

            // string type is explicitly defined
            if (node.TypeName != null)
            {
                var nodeTypeName = new StringSegmentInternal(node.TypeName);
                nodeTypeName.TryGetNextSegment(',', out StringSegmentInternal typeName);
                typeName.Trim();
                
                StringSegmentInternal assembly = default;
                if (node.AssemblyAliasValue != null)
                    new StringSegmentInternal(node.AssemblyAliasValue).TryGetNextSegment(',', out assembly);
                else if (nodeTypeName.Length != 0)
                    nodeTypeName.TryGetNextSegment(',', out assembly);
                assembly.Trim();

                if (typeName.Equals(TypeResolver.StringTypeFullName) && (assembly.Length == 0 || AssemblyResolver.IsCoreLibAssemblyName(assembly)))
                    return node.ValueData;
            }

            // otherwise: non string (theoretically it could be a serialized string but the writer never creates it so)
            return null;
        }

        private static void MergeResourceSet(StringKeyedDictionary<object?> source, IExpandoResourceSet target, bool rebuildSource)
        {
            string prefix = LanguageSettings.UntranslatedResourcePrefix;
            foreach (KeyValuePair<string, object?> resource in source)
            {
                if (target.ContainsResource(resource.Key))
                    continue;

                object? newValue = AsString(resource.Value) is string strValue
                    ? (strValue.StartsWith(prefix, StringComparison.Ordinal) ? strValue : prefix + strValue)
                    : resource.Value;

                target.SetObject(resource.Key, newValue);
            }

            if (rebuildSource)
            {
                source.Clear();
                ToDictionary((ResourceSet)target, source);
            }
        }

        #endregion

        #region Instance Methods

        #region Public Methods

        // ReSharper disable once RedundantOverriddenMember - overridden for the description
        /// <summary>
        /// Gets the value of the specified resource localized for the specified <paramref name="culture"/>.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <param name="culture">The culture for which the resource is localized. If the resource is not localized for this culture,
        /// the resource manager uses fallback rules to locate an appropriate resource. If this value is <see langword="null"/>,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="LanguageSettings.DisplayLanguage" /> property.</param>
        /// <returns>
        /// If <see cref="HybridResourceManager.SafeMode"/> is <see langword="true"/>, and the resource is from a .resx content, then the method returns a <see cref="ResXDataNode"/> instance instead of the actual deserialized value.
        /// Otherwise, returns the value of the resource localized for the specified <paramref name="culture"/>, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="HybridResourceManager.CloneValues"/> property, the <see cref="O:KGySoft.Resources.DynamicResourceManager.GetObject">GetObject</see> methods return either
        /// a full copy of the specified resource, or always the same instance. For memory streams and byte arrays none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="O:KGySoft.Resources.DynamicResourceManager.GetStream">GetStream</see> methods
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="HybridResourceManager.CloneValues"/> property.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="HybridResourceManager.CloneValues"/> property.</para>
        /// <para>Depending on the value of the <see cref="AutoAppend"/> property, dynamic expansion of the resource sets of different cultures may occur when calling this method.</para>
        /// <para><note>For more details see the <em>Auto Appending</em> section at the description of the <see cref="DynamicResourceManager"/> class.</note></para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="DynamicResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public override object? GetObject(string name, CultureInfo? culture) => base.GetObject(name, culture);

        /// <summary>
        /// Returns the value of the specified resource.
        /// </summary>
        /// <param name="name">The name of the resource to get.</param>
        /// <returns>
        /// If <see cref="HybridResourceManager.SafeMode"/> is <see langword="true"/>, and the resource is from a .resx content, then the method returns a <see cref="ResXDataNode"/> instance instead of the actual deserialized value.
        /// Otherwise, returns the value of the resource localized for the caller's current UI culture, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="HybridResourceManager.CloneValues"/> property, the <see cref="O:KGySoft.Resources.DynamicResourceManager.GetObject">GetObject</see> methods return either
        /// a full copy of the specified resource, or always the same instance. For memory streams and byte arrays none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="O:KGySoft.Resources.DynamicResourceManager.GetStream">GetStream</see> methods
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="HybridResourceManager.CloneValues"/> property.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="HybridResourceManager.CloneValues"/> property.</para>
        /// <para>Depending on the value of the <see cref="AutoAppend"/> property, dynamic expansion of the resource sets of different cultures may occur when calling this method.</para>
        /// <para><note>For more details see the <em>Auto Appending</em> section at the description of the <see cref="DynamicResourceManager"/> class.</note></para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="DynamicResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public override object? GetObject(string name) => GetObject(name, null);

        // ReSharper disable once RedundantOverriddenMember - overridden for the description
        /// <summary>
        /// Returns the value of the string resource localized for the specified culture.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <param name="culture">An object that represents the culture for which the resource is localized.</param>
        /// <returns>
        /// The value of the resource localized for the specified culture, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>If <see cref="HybridResourceManager.SafeMode"/> is <see langword="true"/>, then instead of throwing an <see cref="InvalidOperationException"/>
        /// either the raw XML value (for resources from a .resx source) or the string representation of the object (for resources from a compiled source) will be returned for non-string resources.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="HybridResourceManager.CloneValues"/> property.</para>
        /// <para>Depending on the value of the <see cref="AutoAppend"/> property, dynamic expansion of the resource sets of different cultures may occur when calling this method.
        /// In this case the result will be prefixed either by <see cref="LanguageSettings.UntranslatedResourcePrefix">LanguageSettings.UntranslatedResourcePrefix</see>
        /// or <see cref="LanguageSettings.UnknownResourcePrefix">LanguageSettings.UnknownResourcePrefix</see>.
        /// <note>For more details see the <em>Auto Appending</em> section at the description of the <see cref="DynamicResourceManager"/> class.</note>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="DynamicResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="HybridResourceManager.SafeMode"/> is <see langword="false"/>&#160; and the type of the resource is not <see cref="string"/>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public override string? GetString(string name, CultureInfo? culture) => base.GetString(name, culture);

        /// <summary>
        /// Returns the value of the specified string resource.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <returns>
        /// The value of the resource localized for the caller's current UI culture, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>If <see cref="HybridResourceManager.SafeMode"/> is <see langword="true"/>, then instead of throwing an <see cref="InvalidOperationException"/>
        /// either the raw XML value (for resources from a .resx source) or the string representation of the object (for resources from a compiled source) will be returned for non-string resources.</para>
        /// <para><see cref="string"/> values are not duplicated in memory, regardless the value of the <see cref="HybridResourceManager.CloneValues"/> property.</para>
        /// <para>Depending on the value of the <see cref="AutoAppend"/> property, dynamic expansion of the resource sets of different cultures may occur when calling this method.
        /// In this case the result will be prefixed either by <see cref="LanguageSettings.UntranslatedResourcePrefix">LanguageSettings.UntranslatedResourcePrefix</see>
        /// or <see cref="LanguageSettings.UnknownResourcePrefix">LanguageSettings.UnknownResourcePrefix</see>.
        /// <note>For more details see the <em>Auto Appending</em> section at the description of the <see cref="DynamicResourceManager"/> class.</note>
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="DynamicResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="HybridResourceManager.SafeMode"/> is <see langword="false"/>&#160; and the type of the resource is not <see cref="string"/>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public override string? GetString(string name) => GetString(name, null);

        // ReSharper disable once RedundantOverriddenMember - overridden for the description
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
        /// <para>Depending on the value of the <see cref="HybridResourceManager.CloneValues"/> property, the <see cref="O:KGySoft.Resources.DynamicResourceManager.GetObject">GetObject</see> methods return either
        /// a full copy of the specified resource, or always the same instance. For memory streams none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="O:KGySoft.Resources.DynamicResourceManager.GetStream">GetStream</see> methods
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="HybridResourceManager.CloneValues"/> property.</para>
        /// <para><see cref="O:KGySoft.Resources.DynamicResourceManager.GetStream">GetStream</see> can be used also for byte array resources. However, if the value is returned from compiled resources, then always a new copy of the byte array will be wrapped.</para>
        /// <para>If <see cref="HybridResourceManager.SafeMode"/> is <see langword="true"/>&#160;and <paramref name="name"/> is neither a <see cref="MemoryStream"/> nor a byte array resource, then
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns a stream wrapper for the same string value that is returned by the <see cref="O:KGySoft.Resources.DynamicResourceManager.GetString">GetString</see> method,
        /// which will be the raw XML content for non-string resources.</para>
        /// <note>The internal buffer is tried to be obtained by reflection in the first place. On platforms, which have possibly unknown non-public member names the public APIs are used, which may copy the content in memory.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="DynamicResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="HybridResourceManager.SafeMode"/> is <see langword="false"/>&#160;and the type of the resource is neither <see cref="MemoryStream"/> nor <see cref="Array">byte[]</see>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public override MemoryStream? GetStream(string name, CultureInfo? culture) => base.GetStream(name, culture);

        /// <summary>
        /// Returns a <see cref="MemoryStream"/> instance from the resource of the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the resource to retrieve.</param>
        /// <returns>
        /// A <see cref="MemoryStream"/> object from the specified resource localized for the caller's current UI culture, or <see langword="null"/>&#160;if <paramref name="name" /> cannot be found in a resource set.
        /// </returns>
        /// <remarks>
        /// <para>Depending on the value of the <see cref="HybridResourceManager.CloneValues"/> property, the <see cref="O:KGySoft.Resources.HybridResourceManager.GetObject">GetObject</see> methods return either
        /// a full copy of the specified resource, or always the same instance. For memory streams none of them are ideal because a full copy duplicates the inner buffer of a possibly large
        /// array of bytes, whereas returning the same stream instance can cause issues with conflicting positions or disposed state. Therefore the <see cref="O:KGySoft.Resources.HybridResourceManager.GetStream">GetStream</see> methods
        /// can be used to obtain a new read-only <see cref="MemoryStream"/> wrapper around the same internal buffer, regardless the current value of the <see cref="HybridResourceManager.CloneValues"/> property.</para>
        /// <para><see cref="O:KGySoft.Resources.HybridResourceManager.GetStream">GetStream</see> can be used also for byte array resources. However, if the value is returned from compiled resources, then always a new copy of the byte array will be wrapped.</para>
        /// <para>If <see cref="HybridResourceManager.SafeMode"/> is <see langword="true"/>&#160;and <paramref name="name"/> is neither a <see cref="MemoryStream"/> nor a byte array resource, then
        /// instead of throwing an <see cref="InvalidOperationException"/> the method returns a stream wrapper for the same string value that is returned by the <see cref="O:KGySoft.Resources.HybridResourceManager.GetString">GetString</see> method,
        /// which will be the raw XML content for non-string resources.</para>
        /// <para>Depending on the value of the <see cref="AutoAppend"/> property, dynamic expansion of the resource sets of different cultures may occur when calling this method.</para>
        /// <para><note>For more details see the <em>Auto Appending</em> section at the description of the <see cref="DynamicResourceManager"/> class.</note></para>
        /// <note>The internal buffer is tried to be obtained by reflection in the first place. On platforms, which have possibly unknown non-public member names the public APIs are used, which may copy the content in memory.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="DynamicResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="HybridResourceManager.SafeMode"/> is <see langword="false"/>&#160;and the type of the resource is neither <see cref="MemoryStream"/> nor <see cref="Array">byte[]</see>.</exception>
        /// <exception cref="MissingManifestResourceException">No usable set of localized resources has been found, and there are no default culture resources.
        /// For information about how to handle this exception, see the notes under <em>Instantiating a ResXResourceManager object</em> section of the description of the <see cref="ResXResourceManager"/> class.</exception>
        public override MemoryStream? GetStream(string name) => GetStream(name, null);

        /// <summary>
        /// Retrieves the resource set for a particular culture.
        /// </summary>
        /// <param name="culture">The culture whose resources are to be retrieved.</param>
        /// <param name="loadIfExists"><see langword="true"/>&#160;to load the resource set, if it has not been loaded yet and the corresponding resource file exists; otherwise, <see langword="false"/>.</param>
        /// <param name="tryParents"><see langword="true"/>&#160;to use resource fallback to load an appropriate resource if the resource set cannot be found; <see langword="false"/>&#160;to bypass the resource fallback process.</param>
        /// <returns>The resource set for the specified culture.</returns>
        /// <remarks>
        /// <para>If <see cref="AutoAppendOptions.AppendOnLoad"/> option is enabled in <see cref="AutoAppend"/> property, then dynamic expansion of the resource sets may occur when calling this method.
        /// <note>For more details see the <em>Auto Appending</em> section at the description of the <see cref="DynamicResourceManager"/> class.</note>
        /// </para>
        /// <para>Appending is applied only if both <paramref name="loadIfExists"/> and <paramref name="tryParents"/> are <see langword="true"/>, though no new resource set will be created.
        /// To make possible to create a completely new resource set call the <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> method with <see cref="ResourceSetRetrieval.CreateIfNotExists"/> behavior instead.</para>
        /// <para>If due to the current parameters no auto appending is performed during the call, then it will not happen also for successive calls for the same resource set until the <see cref="ReleaseAllResources">ReleaseAllResources</see>
        /// method is called.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="MissingManifestResourceException"><paramref name="tryParents"/> and <see cref="HybridResourceManager.ThrowException"/> are <see langword="true"/>&#160;and
        /// the resource of the neutral culture was not found.</exception>
        public override ResourceSet? GetResourceSet(CultureInfo culture, bool loadIfExists, bool tryParents)
        {
            if (culture == null!)
                Throw.ArgumentNullException(Argument.culture);

            AutoAppendOptions options;
            if (loadIfExists && tryParents && IsAppendPossible(culture, options = AutoAppend))
                EnsureLoadedWithMerge(culture, ResourceSetRetrieval.LoadIfExists, options);
            else
                ReviseCanAcceptProxy(culture, loadIfExists ? ResourceSetRetrieval.LoadIfExists : ResourceSetRetrieval.GetIfAlreadyLoaded, tryParents);

            return base.GetResourceSet(culture, loadIfExists, tryParents);
        }

        /// <summary>
        /// Retrieves the resource set for a particular culture, which can be dynamically modified.
        /// </summary>
        /// <param name="culture">The culture whose resources are to be retrieved.</param>
        /// <param name="behavior">Determines the retrieval behavior of the result <see cref="IExpandoResourceSet"/>. This parameter is optional.
        /// <br/>Default value: <see cref="ResourceSetRetrieval.LoadIfExists"/>.</param>
        /// <param name="tryParents"><see langword="true"/>&#160;to use resource fallback to load an appropriate resource if the resource set cannot be found; <see langword="false"/>&#160;to bypass the resource fallback process. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>
        /// The resource set for the specified culture, or <see langword="null"/>&#160;if the specified culture cannot be retrieved by the defined <paramref name="behavior"/>,
        /// or when <see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/> so it cannot return an <see cref="IExpandoResourceSet"/> instance.
        /// </returns>
        /// <remarks>
        /// <para>If <see cref="AutoAppendOptions.AppendOnLoad"/> option is enabled in <see cref="AutoAppend"/> property, then dynamic expansion of the resource sets may occur when calling this method.
        /// <note>For more details see the <em>Auto Appending</em> section at the description of the <see cref="DynamicResourceManager"/> class.</note>
        /// </para>
        /// <para>Appending is applied only if <paramref name="behavior"/> is not <see cref="ResourceSetRetrieval.GetIfAlreadyLoaded"/> and <paramref name="tryParents"/> is <see langword="true"/>.</para>
        /// <para>If due to the current parameters no auto appending is performed during the call, then it will not happen also for successive calls for the same resource set until the <see cref="ReleaseAllResources">ReleaseAllResources</see>
        /// method is called.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="DynamicResourceManager"/> is already disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="behavior"/> does not fall in the expected range.</exception>
        /// <exception cref="MissingManifestResourceException">Resource file of the neutral culture was not found, while <paramref name="tryParents"/> is <see langword="true"/>
        /// and <paramref name="behavior"/> is not <see cref="ResourceSetRetrieval.CreateIfNotExists"/>.</exception>
        public override IExpandoResourceSet? GetExpandoResourceSet(CultureInfo culture, ResourceSetRetrieval behavior = ResourceSetRetrieval.LoadIfExists, bool tryParents = false)
        {
            if (culture == null!)
                Throw.ArgumentNullException(Argument.culture);

            if (!Enum<ResourceSetRetrieval>.IsDefined(behavior))
                Throw.EnumArgumentOutOfRange(Argument.behavior, behavior);

            AutoAppendOptions options;
            if (behavior != ResourceSetRetrieval.GetIfAlreadyLoaded && tryParents && IsAppendPossible(culture, options = AutoAppend))
                EnsureLoadedWithMerge(culture, behavior, options);
            else
                ReviseCanAcceptProxy(culture, behavior, tryParents);

            return base.GetExpandoResourceSet(culture, behavior, tryParents);
        }

        /// <summary>
        /// Ensures that the resource sets are generated for the specified <paramref name="culture"/> respecting the merging rules
        /// specified by the <see cref="AutoAppend"/> parameter.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="culture">The culture to generate the resource sets for.</param>
        /// <remarks>
        /// <note>This method is similar to <see cref="EnsureInvariantResourcesMerged">EnsureInvariantResourcesMerged</see> but it skips
        /// merging of resources for already existing resource sets (either in memory or in a loadable file).
        /// Use the <see cref="EnsureInvariantResourcesMerged">EnsureInvariantResourcesMerged</see> method to force a new merge even for possibly existing resource sets.</note>
        /// <para>This method generates the possibly missing resource sets in memory.
        /// Call also the <see cref="HybridResourceManager.SaveAllResources">SaveAllResources</see> method to save the generated resource sets immediately.</para>
        /// <para>When generating resources, the value of the <see cref="AutoAppend"/> is be respected.</para>
        /// <note>This method has no effect if <see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>,
        /// or when there are no append options enabled in the <see cref="AutoAppend"/> property.</note>
        /// </remarks>
        public void EnsureResourcesGenerated(CultureInfo culture)
        {
            if (culture == null!)
                Throw.ArgumentNullException(Argument.culture);
            var options = AutoAppend | AutoAppendOptions.AppendOnLoad;
            if (IsAppendPossible(culture, options))
                EnsureLoadedWithMerge(culture, ResourceSetRetrieval.CreateIfNotExists, options);
        }

        /// <summary>
        /// Ensures that all invariant resource entries are merged for the specified <paramref name="culture"/>.
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="culture">The culture to merge the resource sets for.</param>
        /// <remarks>
        /// <note>This method is similar to <see cref="EnsureResourcesGenerated">EnsureResourcesGenerated</see> but it forces a new merge even
        /// for existing resource sets. It can be useful if we want to ensure that possibly newly introduced resources (due to a new version release, for example)
        /// are also merged into the optionally already existing resource set files.</note>
        /// <para>If there are no existing resources for the specified <paramref name="culture"/> yet, then this method is functionally equivalent with
        /// the <see cref="EnsureResourcesGenerated">EnsureResourcesGenerated</see> method, though it can be significantly slower than that.</para>
        /// <para>You can call also the <see cref="HybridResourceManager.SaveAllResources">SaveAllResources</see> method
        /// to save the generated or updated resource sets immediately.</para>
        /// <para>Merging is performed using the rules specified by the <see cref="AutoAppend"/> property.</para>
        /// <note>This method has no effect if <see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>,
        /// or when there are no append options enabled in the <see cref="AutoAppend"/> property.</note>
        /// </remarks>
        public void EnsureInvariantResourcesMerged(CultureInfo culture)
        {
            if (culture == null!)
                Throw.ArgumentNullException(Argument.culture);
            if (Equals(culture, CultureInfo.InvariantCulture))
                return;

            if (!IsAppendPossible(culture, AutoAppend))
                return;

            ResourceSet? invariantSet = InternalGetResourceSet(CultureInfo.InvariantCulture, ResourceSetRetrieval.LoadIfExists, false, false);
            if (invariantSet == null)
                return;

            IDictionaryEnumerator enumerator = invariantSet.GetEnumerator();
            while (enumerator.MoveNext())
                GetObjectInternal((string)enumerator.Key, culture, false, false, true);
        }

        /// <summary>
        /// Tells the resource manager to call the <see cref="ResourceSet.Close">ResourceSet.Close</see> method on all <see cref="ResourceSet"/> objects and release all resources.
        /// All unsaved resources will be lost.
        /// </summary>
        /// <remarks>
        /// <note type="caution">By calling this method all of the unsaved changes will be lost.</note>
        /// <para>By the <see cref="HybridResourceManager.IsModified"/> property you can check whether there are unsaved changes.</para>
        /// <para>To save the changes you can call the <see cref="HybridResourceManager.SaveAllResources">SaveAllResources</see> method.</para>
        /// </remarks>
        public override void ReleaseAllResources()
        {
            lock (SyncRoot)
            {
                base.ReleaseAllResources();
                mergedCultures = null;
                canAcceptProxy = true;
            }
        }

        /// <summary>
        /// Adds or replaces a resource object in the current <see cref="DynamicResourceManager" /> with the specified
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
        /// As a result, the subsequent <see cref="O:KGySoft.Resources.DynamicResourceManager.GetObject">GetObject</see> calls
        /// with the same <paramref name="culture" /> will fall back to the parent culture, or will return <see langword="null"/>&#160;if
        /// <paramref name="name" /> is not found in any parent cultures. However, enumerating the result set returned by
        /// <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> and <see cref="GetResourceSet">GetResourceSet</see> methods will return the resources with <see langword="null"/>&#160;value.</para>
        /// <para>If you want to remove the user-defined ResX content and reset the original resource defined in the binary resource set (if any), use the <see cref="RemoveObject">RemoveObject</see> method.</para>
        /// <note>If you use the <see cref="DynamicResourceManager"/> with some enabled options in the <see cref="AutoAppend"/> property, then do not need to call this method explicitly.
        /// For more details see the <em>Auto Appending</em> section at the description of the <see cref="DynamicResourceManager"/> class.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        public override void SetObject(string name, object? value, CultureInfo? culture = null)
        {
            base.SetObject(name, value, culture);

            // if load creates a rs and this removes the proxies we may reset accepting proxies
            if (!canAcceptProxy && !IsAnyProxyLoaded())
                canAcceptProxy = true;
        }

        /// <summary>
        /// Removes a resource object from the current <see cref="DynamicResourceManager"/> with the specified
        /// <paramref name="name" /> for the specified <paramref name="culture" />.
        /// </summary>
        /// <param name="name">The case-sensitive name of the resource to remove.</param>
        /// <param name="culture">The culture of the resource to remove. If this value is <see langword="null"/>,
        /// the <see cref="CultureInfo" /> object is obtained by using the <see cref="CultureInfo.CurrentUICulture">CultureInfo.CurrentUICulture</see> property. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <remarks>
        /// <para>If there is a binary resource defined for <paramref name="name" /> and <paramref name="culture" />,
        /// then after this call the originally defined value will be returned by <see cref="O:KGySoft.Resources.DynamicResourceManager.GetObject">GetObject</see> method from the binary resources.
        /// If you want to force hiding the binary resource and make <see cref="O:KGySoft.Resources.DynamicResourceManager.GetObject">GetObject</see> to default to the parent <see cref="CultureInfo" /> of the specified <paramref name="culture" />,
        /// then use the <see cref="SetObject">SetObject</see> method with a <see langword="null"/>&#160;value.</para>
        /// <para><paramref name="name"/> is considered as case-sensitive. If <paramref name="name"/> occurs multiple times
        /// in the resource set in case-insensitive manner, they can be removed one by one only.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is <see langword="null" />.</exception>
        /// <exception cref="ObjectDisposedException">The <see cref="HybridResourceManager"/> is already disposed.</exception>
        /// <exception cref="InvalidOperationException"><see cref="Source"/> is <see cref="ResourceManagerSources.CompiledOnly"/>.</exception>
        public override void RemoveObject(string name, CultureInfo? culture = null)
        {
            base.RemoveObject(name, culture);

            // if remove loads a rs and this removes the proxies we may reset accepting proxies
            if (!canAcceptProxy && !IsAnyProxyLoaded())
                canAcceptProxy = true;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Releases the resources used by this <see cref="DynamicResourceManager"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/>&#160;if this method is being called due to explicit disposing; otherwise, <see langword="false"/>.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (AutoSave & AutoSaveOptions.Dispose) == AutoSaveOptions.Dispose)
                DoAutoSave();
            mergedCultures = null;
            UnhookEvents();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Provides the implementation for finding a resource set.
        /// </summary>
        /// <param name="culture">The culture object to look for.</param>
        /// <param name="loadIfExists"><see langword="true"/>&#160;to load the resource set, if it has not been loaded yet; otherwise, <see langword="false"/>.</param>
        /// <param name="tryParents"><see langword="true"/>&#160;to check parent <see cref="CultureInfo" /> objects if the resource set cannot be loaded; otherwise, <see langword="false"/>.</param>
        /// <returns>
        /// The specified resource set.
        /// </returns>
        /// <remarks>Unlike in case of <see cref="GetResourceSet">GetResourceSet</see> and <see cref="GetExpandoResourceSet">GetExpandoResourceSet</see> methods,
        /// no auto appending occurs when this method is called.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
        /// <exception cref="MissingManifestResourceException">The .resx file of the neutral culture was not found, while <paramref name="tryParents"/> and <see cref="HybridResourceManager.ThrowException"/> are both <see langword="true"/>.</exception>
        protected override ResourceSet? InternalGetResourceSet(CultureInfo culture, bool loadIfExists, bool tryParents)
        {
            Debug.Assert(!ReferenceEquals(Assembly.GetCallingAssembly(), Assembly.GetExecutingAssembly()), "InternalGetResourceSet is called from Libraries assembly.");
            ReviseCanAcceptProxy(culture, loadIfExists ? ResourceSetRetrieval.LoadIfExists : ResourceSetRetrieval.GetIfAlreadyLoaded, tryParents);

            return Unwrap(InternalGetResourceSet(culture, loadIfExists ? ResourceSetRetrieval.LoadIfExists : ResourceSetRetrieval.GetIfAlreadyLoaded, tryParents, false));
        }

        #endregion

        #region Private Protected Methods

        private protected override void SetSource(ResourceManagerSources value)
        {
            lock (SyncRoot)
            {
                OnSourceChanging();
                mergedCultures = null;
                canAcceptProxy = true;
                base.SetSource(value);
            }
        }

        /// <summary>
        /// Called by GetFirstResourceSet if cache is a proxy.
        /// There is always a traversal if this is called (tryParents).
        /// Proxy is accepted if it is no problem if a result is found in the proxied resource set.
        /// </summary>
        /// <param name="proxy">The found proxy</param>
        /// <param name="culture">The requested culture</param>
        private protected override bool IsCachedProxyAccepted(ResourceSet proxy, CultureInfo culture)
        {
            // if invariant culture is requested, this method should not be reached
            Debug.Assert(!Equals(culture, CultureInfo.InvariantCulture), "There should be no proxy for the invariant culture");

            if (!base.IsCachedProxyAccepted(proxy, culture))
                return false;

            if (canAcceptProxy)
                return true;

            AutoAppendOptions autoAppendOptions = AutoAppend;

            // There is no append for existing entries (only AddUnknownToInvariantCulture).
            if ((autoAppendOptions & (AutoAppendOptions.AppendNeutralCultures | AutoAppendOptions.AppendSpecificCultures)) == AutoAppendOptions.None)
                return true;

            // traversing the cultures until wrapped culture in proxy is reached
            CultureInfo wrappedCulture = GetWrappedCulture(proxy);
            ResourceFallbackManager mgr = new ResourceFallbackManager(culture, NeutralResourcesCulture, true);
            bool isFirstNeutral = true;
            foreach (CultureInfo currentCulture in mgr)
            {
                // if we reached the wrapped culture, without merge need, we can accept the proxy
                if (wrappedCulture.Equals(currentCulture))
                    return true;

                // if merge is needed for a culture, which is a descendant of the wrapped culture, we cannot accept proxy
                if (IsMergeNeeded(culture, currentCulture, isFirstNeutral))
                    return false;

                if (isFirstNeutral && currentCulture.IsNeutralCulture)
                    isFirstNeutral = false;
            }

            return Throw.InternalError<bool>(Res.InternalError("Proxied culture not found in hierarchy"));
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
        private protected override object? GetObjectInternal(string name, CultureInfo? culture, bool isString, bool cloneValue, bool safeMode)
        {
            #region Local Methods to reduce complexity

            bool TryGetFromCache(ref GetObjectWithAppendContext ctx)
            {
                ctx.CheckedResource = TryGetFromCachedResourceSet(ctx.Name, ctx.Culture, ctx.IsString, ctx.CloneValue, ctx.SafeMode, out ctx.Result);

                // There is a result, or a stored null is returned from invariant resource: it is never merged even if requested as string
                return ctx.Result != null
                    || (ctx.CheckedResource != null
                        && (Equals(ctx.Culture, CultureInfo.InvariantCulture) || IsProxy(ctx.CheckedResource) && GetWrappedCulture(ctx.CheckedResource).Equals(CultureInfo.InvariantCulture))
                        && (Unwrap(ctx.CheckedResource) as IExpandoResourceSet)?.ContainsResource(ctx.Name, IgnoreCase) == true);
            }

            bool TryGetWhileTraverse(ref GetObjectWithAppendContext ctx)
            {
                ctx.ToMerge = new Stack<IExpandoResourceSet>();
                bool isFirstNeutral = true;
                var mgr = new ResourceFallbackManager(ctx.Culture, NeutralResourcesCulture, true);

                // Crawling from specific to neutral and collecting the cultures to merge
                foreach (CultureInfo currentCulture in mgr)
                {
                    bool isMergeNeeded = IsMergeNeeded(ctx.Culture, currentCulture, isFirstNeutral);
                    if (isFirstNeutral && currentCulture.IsNeutralCulture)
                        isFirstNeutral = false;

                    // can occur from the 2nd iteration: the proxy to cache will be invalidated so it should be re-created
                    if (isMergeNeeded && IsProxy(ctx.ToCache) && !IsExpandoExists(currentCulture))
                        ctx.ToCache = null;

                    // using tryParents only if invariant is requested without appending so the exception can come from the base
                    bool tryParents = ReferenceEquals(currentCulture, CultureInfo.InvariantCulture)
                        && (ctx.Options & AutoAppendOptions.AddUnknownToInvariantCulture) == AutoAppendOptions.None;
                    ResourceSet? rs = InternalGetResourceSet(currentCulture, isMergeNeeded ? ResourceSetRetrieval.CreateIfNotExists : ResourceSetRetrieval.LoadIfExists, tryParents, isMergeNeeded);
                    var rsExpando = rs as IExpandoResourceSet;

                    // Proxies are considered only at TryGetFromCachedResourceSet.
                    // When traversing, proxies are skipped because we must track the exact levels at merging and we don't know what is between the current and proxied levels.
                    if (rs != null && rs != ctx.CheckedResource && !IsProxy(rs))
                    {
                        ctx.Result = GetResourceFromAny(rs, ctx.Name, ctx.IsString, ctx.CloneValue || ctx.ToMerge.Count > 0 || isMergeNeeded && !currentCulture.Equals(ctx.Culture), ctx.SafeMode);

                        // there is a result
                        if (ctx.Result != null)
                        {
                            // returning the value immediately only if there is nothing to merge and to proxy
                            if (ctx.ToMerge.Count == 0 && currentCulture.Equals(ctx.Culture))
                            {
                                SetCache(ctx.Culture, rs);
                                return true;
                            }
                        }
                        // null from invariant can be returned. Stored null is never merged (even if requested as string) so ignoring toMerge if any.
                        else if (ReferenceEquals(currentCulture, CultureInfo.InvariantCulture) && rsExpando?.ContainsResource(ctx.Name, IgnoreCase) == true)
                        {
                            SetCache(ctx.Culture, ctx.ToCache
                                ?? (Equals(ctx.Culture, CultureInfo.InvariantCulture)
                                    ? rs
                                    : InternalGetResourceSet(ctx.Culture, ResourceSetRetrieval.LoadIfExists, true, false)));
                            ctx.Result = null;
                            return true;
                        }

                        // Unlike in base, no unwrapping for this check because proxies are skipped here
                        ctx.CheckedResource = rs;
                    }

                    // The resource to cache normally should be the resource to which the requested culture belongs.
                    // Since DRM does not use tryParents = true during the traversal, the cached proxies are created on caching.
                    if (Equals(ctx.Culture, currentCulture))
                        ctx.ToCache = rs;

                    // returning false just means that the result is not returned immediately but there will be a merge
                    if (ctx.Result != null)
                        return false;

                    if (isMergeNeeded)
                        ctx.ToMerge.Push(rsExpando!);
                }

                return false;
            }

            #endregion

            if (name == null!)
                Throw.ArgumentNullException(Argument.name);

            AdjustCulture(ref culture);
            AutoAppendOptions options = AutoAppend;
            if (!IsAppendPossible(culture, options))
                return base.GetObjectInternal(name, culture, isString, cloneValue, safeMode);

            var context = new GetObjectWithAppendContext
            {
                Name = name,
                Culture = culture,
                IsString = isString,
                CloneValue = cloneValue,
                SafeMode = safeMode,
                Options = options
            };

            if (TryGetFromCache(ref context))
                return context.Result;

            EnsureLoadedWithMerge(culture, ResourceSetRetrieval.CreateIfNotExists, context.Options);

            // Phase 1: finding the resource and meanwhile collecting cultures to merge
            if (TryGetWhileTraverse(ref context))
                return context.Result;

            // Phase 2: handling null
            if (context.Result == null)
            {
                // no append of invariant: exit
                if ((context.Options & AutoAppendOptions.AddUnknownToInvariantCulture) == AutoAppendOptions.None)
                    return null;

                IExpandoResourceSet inv = (IExpandoResourceSet)InternalGetResourceSet(CultureInfo.InvariantCulture, ResourceSetRetrieval.CreateIfNotExists, false, true)!;

                // as string: adding unknown to invariant; otherwise, adding null explicitly
                if (isString)
                    context.Result = LanguageSettings.UnknownResourcePrefix + name;

                inv.SetObject(name, context.Result);

                // if there is nothing to merge, returning (as object, null is never merged)
                if (!isString || context.ToMerge!.Count == 0)
                {
                    SetCache(culture, context.ToCache
                        ?? (Equals(culture, CultureInfo.InvariantCulture)
                            ? (ResourceSet)inv
                            : InternalGetResourceSet(culture, ResourceSetRetrieval.LoadIfExists, true, false)));
                    return context.Result;
                }
            }

            // Phase 3: processing the merge
            // Unlike in EnsureLoadedWithMerge, not merged levels are missing here because we can be sure that the resource does not exist at mid-level.
            string prefix = LanguageSettings.UntranslatedResourcePrefix;
            foreach (IExpandoResourceSet rsToMerge in context.ToMerge!)
            {
                if (AsString(context.Result) is string strValue)
                    context.Result = strValue.StartsWith(prefix, StringComparison.Ordinal) ? strValue : prefix + strValue;
                rsToMerge.SetObject(name, context.Result);
            }

            // If there is no loaded proxy at the moment (because rs creation above deleted all of them) resetting trust in proxies
            if (!canAcceptProxy && !IsAnyProxyLoaded())
                canAcceptProxy = true;

            // If the resource set of the requested level does not exist, it can be created (as a proxy) by the base class by using tryParent=true in all levels.
            SetCache(culture, context.ToCache ?? InternalGetResourceSet(culture, ResourceSetRetrieval.LoadIfExists, true, false));
            return context.Result;
        }

        #endregion

        #region Private Methods

        private void OnSourceChanging()
        {
            if ((AutoSave & AutoSaveOptions.SourceChange) != AutoSaveOptions.None)
                DoAutoSave();
        }

        private void OnDomainUnload()
        {
            if ((AutoSave & AutoSaveOptions.DomainUnload) != AutoSaveOptions.None)
                DoAutoSave();
        }

        private void OnLanguageChanged()
        {
            if ((AutoSave & AutoSaveOptions.LanguageChange) != AutoSaveOptions.None)
                DoAutoSave();
        }

        private void DoAutoSave()
        {
            try
            {
                SaveAllResources(compatibleFormat: CompatibleFormat);
            }
            catch (Exception e)
            {
                if (!OnAutoSaveError(new AutoSaveErrorEventArgs(e)))
                    throw;
            }
        }

        /// <summary>
        /// Raises the <see cref="AutoSaveError" /> event.
        /// </summary>
        /// <returns><see langword="true"/>&#160;if the error is handled and should not be re-thrown.</returns>
        private bool OnAutoSaveError(AutoSaveErrorEventArgs e)
        {
            EventHandler<AutoSaveErrorEventArgs>? handler = AutoSaveError;
            if (handler != null)
            {
                handler.Invoke(this, e);
                return e.Handled;
            }

            return e.Exception is IOException || e.Exception is SecurityException || e.Exception is UnauthorizedAccessException;
        }

        private void HookEvents()
        {
            if (useLanguageSettings)
            {
                LanguageSettings.DynamicResourceManagersSourceChanged += LanguageSettings_DynamicResourceManagersSourceChanged;
                LanguageSettings.DynamicResourceManagersAutoSaveChanged += LanguageSettings_DynamicResourceManagersAutoSaveChanged;
                LanguageSettings.DynamicResourceManagersCommonSignal += LanguageSettings_DynamicResourceManagersCommonSignal;
                LanguageSettings.DynamicResourceManagersResXResourcesDirChanged += LanguageSettings_DynamicResourceManagersResXResourcesDirChanged;
            }

            if ((AutoSave & AutoSaveOptions.LanguageChange) != AutoSaveOptions.None)
                LanguageSettings.DisplayLanguageChanged += LanguageSettings_DisplayLanguageChanged;
            if ((AutoSave & AutoSaveOptions.DomainUnload) != AutoSaveOptions.None)
            {
                if (AppDomain.CurrentDomain.IsDefaultAppDomain())
                    AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                else
                    AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;
            }
        }

        private void UnhookEvents()
        {
            LanguageSettings.DynamicResourceManagersSourceChanged -= LanguageSettings_DynamicResourceManagersSourceChanged;
            LanguageSettings.DynamicResourceManagersAutoSaveChanged -= LanguageSettings_DynamicResourceManagersAutoSaveChanged;
            LanguageSettings.DynamicResourceManagersCommonSignal -= LanguageSettings_DynamicResourceManagersCommonSignal;
            LanguageSettings.DisplayLanguageChanged -= LanguageSettings_DisplayLanguageChanged;
            LanguageSettings.DynamicResourceManagersResXResourcesDirChanged -= LanguageSettings_DynamicResourceManagersResXResourcesDirChanged;
            if (AppDomain.CurrentDomain.IsDefaultAppDomain())
                AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
            else
                AppDomain.CurrentDomain.DomainUnload -= CurrentDomain_DomainUnload;
        }

        private void AdjustCulture([AllowNull]ref CultureInfo culture)
        {
            culture ??= CultureInfo.CurrentUICulture;

            // Logic from ResourceFallbackManager.GetEnumerator() - so reference equality will work for invariant
            if (culture.Name == NeutralResourcesCulture.Name)
                culture = CultureInfo.InvariantCulture;
        }

        private void ReviseCanAcceptProxy(CultureInfo culture, ResourceSetRetrieval behavior, bool tryParents)
        {
            if (!canAcceptProxy)
                return;

            // accepting proxy breaks (full check must be performed in IsCachedProxyAccepted) if hierarchy is not about to be loaded...
            if (behavior == ResourceSetRetrieval.GetIfAlreadyLoaded
                // ...while parents are requested...
                && tryParents
                // ...and there is no appending on loading a new resource...
                && (AutoAppend & AutoAppendOptions.AppendOnLoad) == AutoAppendOptions.None
                // ...and non-invariant culture is requested...
                && !CultureInfo.InvariantCulture.Equals(culture)
                // ...which is either not loaded yet or a proxy is loaded for it (= non-proxy is not loaded for it yet)
                && !IsNonProxyLoaded(culture))
            {
                canAcceptProxy = false;
            }
        }

        /// <summary>
        /// Checks whether append is possible
        /// </summary>
        private bool IsAppendPossible(CultureInfo culture, AutoAppendOptions options)
        {
            // append is not possible if only compiled resources are used...
            return !(base.Source == ResourceManagerSources.CompiledOnly
                // ...or there is no appending at all...
                || options == AutoAppendOptions.None
                // ...invariant culture is requested but invariant is not appended...
                || (options & AutoAppendOptions.AddUnknownToInvariantCulture) == AutoAppendOptions.None && Equals(culture, CultureInfo.InvariantCulture)
                // ...a neutral culture is requested but only specific culture is appended
                || (options & (AutoAppendOptions.AddUnknownToInvariantCulture | AutoAppendOptions.AppendNeutralCultures | AutoAppendOptions.AppendOnLoad)) == AutoAppendOptions.None && culture.IsNeutralCulture);
        }

        /// <summary>
        /// Applies the AppendOnLoad rule.
        /// </summary>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "False alarm, the new analyzer includes the complexity of local methods.")]
        private void EnsureLoadedWithMerge(CultureInfo culture, ResourceSetRetrieval behavior, AutoAppendOptions options)
        {
            #region Local Methods to reduce complexity

            bool CanSkipMerge(ref EnsureLoadedWithMergeContext ctx) =>
                // can skip if there is no append on load...
                ((ctx.Options & AutoAppendOptions.AppendOnLoad) == AutoAppendOptions.None
                    // ...or append flags are not set...
                    || (ctx.Options & (AutoAppendOptions.AppendNeutralCultures | AutoAppendOptions.AppendSpecificCultures)) == AutoAppendOptions.None
                    // ...or the requested culture is invariant...
                    || ctx.Culture.Equals(CultureInfo.InvariantCulture)
                    // ...or only AppendSpecific is on but the requested culture is neutral...
                    || (ctx.Options & AutoAppendOptions.AppendNeutralCultures) == AutoAppendOptions.None && ctx.Culture.IsNeutralCulture
                    // ...or merge status is already up-to-date
                    || mergedCultures?.ContainsKey(ctx.Culture) == true)
                    //// ...or culture is already loaded/created
                    //|| IsNonProxyLoaded(culture))
            ;

            void CollectCulturesToMerge(ref EnsureLoadedWithMergeContext ctx)
            {
                ResourceFallbackManager mgr = new ResourceFallbackManager(ctx.Culture, NeutralResourcesCulture, true);
                ctx.ToMerge = new Stack<KeyValuePair<CultureInfo, bool>>();
                bool isFirstNeutral = true;

                // crawling from specific to neutral...
                foreach (CultureInfo currentCulture in mgr)
                {
                    // if culture is not up-to-date
                    if (!mergedCultures.TryGetValue(currentCulture, out bool merged))
                    {
                        bool isMergeNeeded = IsMergeNeeded(ctx.Culture, currentCulture, isFirstNeutral);
                        if (isFirstNeutral && currentCulture.IsNeutralCulture)
                            isFirstNeutral = false;

                        ctx.ToMerge.Push(new KeyValuePair<CultureInfo, bool>(currentCulture, isMergeNeeded));

                        // storing the last culture to be merged in this hierarchy
                        if (isMergeNeeded && ctx.StopMerge == null)
                            ctx.StopMerge = currentCulture;
                    }
                    // we reached an up-to-date culture: searching for a merged one to take a base or invariant
                    else
                    {
                        ctx.ToMerge.Push(new KeyValuePair<CultureInfo, bool>(currentCulture, merged));
                        if (merged)
                            break;
                    }
                }
            }

            #endregion

            Debug.Assert(behavior is ResourceSetRetrieval.LoadIfExists or ResourceSetRetrieval.CreateIfNotExists);

            var context = new EnsureLoadedWithMergeContext { Culture = culture, Options = options };
            lock (SyncRoot)
            {
                if (CanSkipMerge(ref context))
                    return;

                // Phase 1: collecting cultures to merge
                Debug.Assert(Source != ResourceManagerSources.CompiledOnly);
                mergedCultures ??= new Dictionary<CultureInfo, bool> { { CultureInfo.InvariantCulture, true } };
                CollectCulturesToMerge(ref context);

                // Phase 2: Performing the merge
                Debug.Assert(context.ToMerge!.Count > 0 && context.ToMerge.Peek().Value, "A merged culture is expected as top element on the stack ");

                // actually there is nothing to merge: updating mergedCultures and exit
                if (context.StopMerge == null)
                {
                    foreach (KeyValuePair<CultureInfo, bool> item in context.ToMerge)
                    {
                        if (!mergedCultures.ContainsKey(item.Key))
                        {
                            Debug.Assert(!item.Value, "No merging is expected here");
                            mergedCultures.Add(item.Key, false);
                        }
                    }

                    return;
                }

                // taking the first element of the stack, which is merged (or is the invariant culture) and doing the merges
                // tryParents is always true in callers; otherwise, there would be no merge.
                bool tryParents = ThrowException && ReferenceEquals(context.ToMerge.Peek().Key, CultureInfo.InvariantCulture)
                    && (context.Options & AutoAppendOptions.AddUnknownToInvariantCulture) == AutoAppendOptions.None;
                ResourceSet? rs = InternalGetResourceSet(context.ToMerge.Peek().Key, ResourceSetRetrieval.LoadIfExists, tryParents, false);
                Debug.Assert(rs != null || ReferenceEquals(context.ToMerge.Peek().Key, CultureInfo.InvariantCulture), "A merged culture is expected to be exist. Only invariant can be missing.");
                Debug.Assert(rs == null || IsNonProxyLoaded(context.ToMerge.Peek().Key), "The base culture for merge should not be a proxy");

                // populating an accumulator with the first, fully merged resource
                context.ToMerge.Pop();
                Debug.Assert(context.ToMerge.Count > 0, "Cultures to be merged are expected on the stack");
                KeyValuePair<CultureInfo, bool> current;
                var acc = new StringKeyedDictionary<object?>();
                if (rs != null)
                    ToDictionary(rs, acc);

                do
                {
                    current = context.ToMerge.Pop();
                    ResourceSet? rsTarget = InternalGetResourceSet(current.Key,
                        behavior == ResourceSetRetrieval.CreateIfNotExists && current.Value
                            ? ResourceSetRetrieval.CreateIfNotExists
                            : ResourceSetRetrieval.LoadIfExists, false, current.Value);

                    // cannot be loaded
                    if (rsTarget == null || IsProxy(rsTarget))
                    {
                        // not storing anything to cache if behavior is only LoadIfExist (from Get[Expando]ResourceSet) while culture should be merged
                        // so next time this culture can be created from GetObjectWithAppend
                        if (!(current.Value && behavior == ResourceSetRetrieval.LoadIfExists))
                            mergedCultures[current.Key] = false;

                        continue;
                    }

                    // merging accumulator to target resource set
                    if (current.Value)
                    {
                        MergeResourceSet(acc, (IExpandoResourceSet)rsTarget, !ReferenceEquals(current.Key, context.StopMerge));
                    }
                    // updating accumulator
                    else if (!ReferenceEquals(current.Key, context.StopMerge))
                    {
                        ToDictionary(rsTarget, acc);
                    }

                    mergedCultures[current.Key] = current.Value;
                } while (!ReferenceEquals(current.Key, context.StopMerge));

                // updating mergedCultures with the rest of the cultures (they are not merged)
                foreach (KeyValuePair<CultureInfo, bool> item in context.ToMerge)
                {
                    mergedCultures[item.Key] = false;
                }
            }
        }

        private bool IsMergeNeeded(CultureInfo requestedCulture, CultureInfo currentCulture, bool isFirstNeutral)
        {
            var append = AutoAppend;
            return
                // append neutral cultures is on and current culture is a neutral one
                ((append & AutoAppendOptions.AppendNeutralCultures) == AutoAppendOptions.AppendNeutralCultures && currentCulture.IsNeutralCulture)
                // append first neutral is on and current culture is the first neutral one
                || ((append & AutoAppendOptions.AppendFirstNeutralCulture) != AutoAppendOptions.None && isFirstNeutral && currentCulture.IsNeutralCulture)
                // append last neutral is on and parent of current neutral culture is invariant
                || ((append & AutoAppendOptions.AppendLastNeutralCulture) != AutoAppendOptions.None && currentCulture.IsNeutralCulture && ReferenceEquals(currentCulture.Parent, CultureInfo.InvariantCulture))
                // append specific cultures is on and current culture is a specific one
                || ((append & AutoAppendOptions.AppendSpecificCultures) == AutoAppendOptions.AppendSpecificCultures && !ReferenceEquals(currentCulture, CultureInfo.InvariantCulture) && !currentCulture.IsNeutralCulture)
                // append first specific is on and requested culture is the current culture, which is a specific one
                || ((append & AutoAppendOptions.AppendFirstSpecificCulture) != AutoAppendOptions.None && !ReferenceEquals(currentCulture, CultureInfo.InvariantCulture) && currentCulture.Equals(requestedCulture) && !currentCulture.IsNeutralCulture)
                // append last specific is on and parent of current specific is neutral or invariant
                || ((append & AutoAppendOptions.AppendLastSpecificCulture) != AutoAppendOptions.None && !ReferenceEquals(currentCulture, CultureInfo.InvariantCulture) && !currentCulture.IsNeutralCulture && (currentCulture.Parent.IsNeutralCulture || ReferenceEquals(currentCulture.Parent, CultureInfo.InvariantCulture)))
                ;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx) => HookEvents();

        #endregion

        #region Event handlers

        private void LanguageSettings_DynamicResourceManagersCommonSignal(object? sender, EventArgs<LanguageSettingsSignal> e)
        {
            switch (e.EventData)
            {
                case LanguageSettingsSignal.EnsureResourcesGenerated:
                    EnsureResourcesGenerated(LanguageSettings.DisplayLanguage);
                    break;
                case LanguageSettingsSignal.SavePendingResources:
                    SaveAllResources();
                    break;
                case LanguageSettingsSignal.SavePendingResourcesCompatible:
                    SaveAllResources(compatibleFormat: true);
                    break;
                case LanguageSettingsSignal.ReleaseAllResourceSets:
                    ReleaseAllResources();
                    break;
                case LanguageSettingsSignal.EnsureInvariantResourcesMerged:
                    EnsureInvariantResourcesMerged(LanguageSettings.DisplayLanguage);
                    break;
                default:
                    Throw.InternalError(Res.EnumOutOfRange(e.EventData));
                    break;
            }
        }

        private void LanguageSettings_DynamicResourceManagersSourceChanged(object? sender, EventArgs e)
            => SetSource(LanguageSettings.DynamicResourceManagersSource);

        private void LanguageSettings_DynamicResourceManagersAutoSaveChanged(object? sender, EventArgs e)
        {
            lock (SyncRoot)
            {
                UnhookEvents();
                HookEvents();
            }
        }

        private void LanguageSettings_DisplayLanguageChanged(object? sender, EventArgs e) => OnLanguageChanged();

        private void CurrentDomain_DomainUnload(object? sender, EventArgs e) => OnDomainUnload();

        private void CurrentDomain_ProcessExit(object? sender, EventArgs e) => OnDomainUnload();

        private void LanguageSettings_DynamicResourceManagersResXResourcesDirChanged(object? sender, EventArgs e)
            => ResXResourcesDir = LanguageSettings.DynamicResourceManagersResXResourcesDir;

        #endregion

        #endregion

        #endregion
    }
}
