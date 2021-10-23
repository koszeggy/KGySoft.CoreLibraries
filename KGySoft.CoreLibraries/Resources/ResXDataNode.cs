#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ResXDataNode.cs
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
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;
using System.Xml;

using KGySoft.CoreLibraries;
using KGySoft.Reflection;
#if !NETFRAMEWORK
using KGySoft.Serialization;
#endif
using KGySoft.Serialization.Binary;

#endregion

#region Suppressions

#if NET
#if NET5_0 || NET6_0
#pragma warning disable SYSLIB0011 // Type or member is obsolete - this class uses IFormatter implementations for compatibility reasons
#pragma warning disable IDE0079 // Remove unnecessary suppression - CS0618 is emitted by ReSharper
#pragma warning disable CS0618 // Use of obsolete symbol - as above  
#else
#error Check whether IFormatter is still available in this .NET version
#endif
#endif

#endregion

namespace KGySoft.Resources
#pragma warning restore IDE0079 // Remove unnecessary suppression
{
    /// <summary>
    /// Represents a resource or metadata element in an XML resource (.resx) file.
    /// <br/>See the <strong>Remarks</strong> section for an example and for the differences compared to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a> class.
    /// </summary>
    /// <remarks>
    /// <note>This class is similar to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a>
    /// in <c>System.Windows.Forms.dll</c>. See the <a href="#comparison">Comparison with System.Resources.ResXDataNode</a> section for the differences.</note>
    /// <para>The <see cref="ResXDataNode"/> class supports the representation of rich data types within a resource file. It can support the storage of any object in a resource file.</para>
    /// <para>You can create a <see cref="ResXDataNode"/> object by calling one of its overloaded class constructors.
    /// You can then add the resource item or element to a resource file by one of the following options:
    /// <list type="bullet">
    /// <item>Call the <see cref="ResXResourceWriter.AddResource(ResXDataNode)">ResXResourceWriter.AddResource(ResXDataNode)</see> or <see cref="ResXResourceWriter.AddMetadata(ResXDataNode)">ResXResourceWriter.AddMetadata(ResXDataNode)</see> method.</item>
    /// <item>Call the <see cref="ResXResourceSet.SetObject(string, object)">ResXResourceSet.SetObject(string, object)</see> or <see cref="ResXResourceSet.SetMetaObject(string, object)">ResXResourceSet.SetMetaObject(string, object)</see> method and then
    /// call the <see cref="O:KGySoft.Resources.ResXResourceSet.Save">Save</see> method on it.</item>
    /// <item>Call the <see cref="IExpandoResourceManager.SetObject">SetObject</see> or <see cref="IExpandoResourceManager.SetMetaObject">SetMetaObject</see> method on any <see cref="IExpandoResourceManager"/> implementation
    /// (such as <see cref="ResXResourceManager"/>, <see cref="HybridResourceManager"/>, <see cref="DynamicResourceManager"/>) and then call the <see cref="IExpandoResourceManager.SaveResourceSet">SaveResourceSet</see>
    /// or <see cref="IExpandoResourceManager.SaveAllResources">SaveAllResources</see> methods on them.</item>
    /// </list>
    /// <note>If you call any of the <c>SetObject</c> methods of the list above by any <see cref="object"/>, then a <see cref="ResXDataNode"/> instance will be implicitly created.
    /// A <see cref="ResXDataNode"/> instance should be explicitly created only if you want to set the <see cref="Comment"/> property.</note>
    /// </para>
    /// <para>To retrieve a <see cref="ResXDataNode"/> object from a resource you have the following options:
    /// <list type="bullet">
    /// <item>Enumerate the <see cref="ResXDataNode"/> objects in an XML (.resx file) by instantiating a <see cref="ResXResourceReader"/> object,
    /// setting the <see cref="ResXResourceReader.SafeMode">ResXResourceReader.SafeMode</see> property to <see langword="true"/>, and calling the
    /// <see cref="ResXResourceReader.GetEnumerator">ResXResourceReader.GetEnumerator</see> or <see cref="ResXResourceReader.GetMetadataEnumerator">ResXResourceReader.GetMetadataEnumerator</see> method to get an enumerator.
    /// See also the example below.</item>
    /// <item>Instantiate a new <see cref="ResXResourceSet"/> from a .resx file, set <see cref="ResXResourceSet.SafeMode">ResXResourceSet.SafeMode</see> to <see langword="true"/>,
    /// and call the <see cref="ResXResourceSet.GetObject(string)">ResXResourceSet.GetObject</see> or <see cref="ResXResourceSet.GetMetaObject">ResXResourceSet.GetMetaObject</see>
    /// methods with a key, which exists in the .resx file. You can use the <see cref="ResXResourceSet.GetEnumerator">ResXResourceSet.GetEnumerator</see> and <see cref="ResXResourceSet.GetMetadataEnumerator">ResXResourceSet.GetMetadataEnumerator</see>
    /// methods in a similar way as in case of the <see cref="ResXResourceReader"/> class.</item>
    /// <item>Instantiate a new <see cref="ResXResourceManager"/>/<see cref="HybridResourceManager"/> or <see cref="DynamicResourceManager"/> class, set <see cref="IExpandoResourceManager.SafeMode"/> to <see langword="true"/>,
    /// and call the <see cref="IExpandoResourceManager.GetObject">GetObject</see> or <see cref="IExpandoResourceManager.GetMetaObject">GetMetaObject</see>
    /// methods with a key, which exists in the .resx file.</item>
    /// </list>
    /// </para>
    /// <example>
    /// The following example shows how to retrieve <see cref="ResXDataNode"/> instances from the <see cref="IDictionaryEnumerator"/> returned by <see cref="ResXResourceReader.GetEnumerator">ResXResourceReader.GetEnumerator</see>
    /// and <see cref="ResXResourceReader.GetMetadataEnumerator">ResXResourceReader.GetMetadataEnumerator</see> methods. For example, you can check the type information before deserialization if the .resx file is from an untrusted source.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Collections;
    /// using System.IO;
    /// 
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
    ///     <comment>When this entry is deserialized in an unsafe way, System.Drawing assembly will be loaded.</comment>
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
    ///     public static void Main()
    ///     {
    ///         // In SafeMode the enumerator values will be ResXDataNode instances instead of deserialized objects
    ///         var reader = new ResXResourceReader(new StringReader(resx)) { SafeMode = true };
    /// 
    ///         Console.WriteLine("____Resources in .resx:____");
    ///         Dump(reader.GetEnumerator());
    /// 
    ///         Console.WriteLine("____Metadata in .resx:____");
    ///         Dump(reader.GetMetadataEnumerator());
    ///     }
    /// 
    ///     private static void Dump(IDictionaryEnumerator enumerator)
    ///     {
    ///         while (enumerator.MoveNext())
    ///         {
    ///             var node = (ResXDataNode)enumerator.Value;
    ///             Console.WriteLine($"Name: {node.Name}");
    ///             Console.WriteLine($"  Type:        {node.TypeName}");
    ///             Console.WriteLine($"  Alias value: {node.AssemblyAliasValue}");
    ///             Console.WriteLine($"  MIME type:   {node.MimeType}");
    ///             Console.WriteLine($"  Comment:     {node.Comment}");
    ///             Console.WriteLine($"  Raw value:   {node.ValueData}");
    ///             try
    ///             {
    ///                 var value = node.GetValueSafe();
    ///                 Console.WriteLine($"  Real value:  {value} ({value.GetType()})");
    ///             }
    ///             catch (Exception e)
    ///             {
    ///                 Console.WriteLine($"  Safe deserialization of the node thrown an exception: {e.Message}");
    ///                 try
    ///                 {
    ///                     var value = node.GetValue();
    ///                     Console.WriteLine($"  Real value (unsafe):  {value} ({value.GetType()})");
    ///                 }
    ///                 catch (Exception)
    ///                 {
    ///                     Console.WriteLine($"  Unsafe deserialization of the node thrown an exception: {e.Message}");
    ///                 }
    ///             }
    ///             Console.WriteLine();
    ///         }
    ///     }
    /// }]]>
    /// 
    /// // The example displays the following output:
    /// //  ____Resources in .resx:____
    /// // Name: string
    /// //   Type:
    /// //   Alias value:
    /// //   MIME type:
    /// //   Comment:     Default data type is string.
    /// //   Raw value:   Test string
    /// //   Real value:  Test string (System.String)
    /// // 
    /// // Name: int
    /// //   Type:        System.Int32
    /// //   Alias value:
    /// //   MIME type:
    /// //   Comment:
    /// //   Raw value:   42
    /// //   Real value:  42 (System.Int32)
    /// // 
    /// // Name: color
    /// //   Type:        System.Drawing.Color, CustomAlias
    /// //   Alias value: System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
    /// //   MIME type:
    /// //   Comment:     When this entry is deserialized in an unsafe way, System.Drawing assembly will be loaded.
    /// //   Raw value:   Red
    /// //   Safe deserialization of the node thrown an exception: Type "System.Drawing.Color, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" in the data at line 18, position 4 cannot be resolved.
    /// // You may try to preload its assembly before deserialization or use the unsafe GetValue if the resource is from a trusted source.
    /// //   Real value (unsafe):  Color[Red] (System.Drawing.Color)
    /// // 
    /// // Name: bytes
    /// //  Type:        System.Byte[]
    /// //  Alias value:
    /// //   MIME type:
    /// //   Comment:
    /// //   Raw value:   VGVzdCBieXRlcw==
    /// //   Real value:  System.Byte[] (System.Byte[])
    /// // 
    /// // Name: dangerous
    /// //   Type:
    /// //   Alias value:
    /// //   MIME type:   application/x-microsoft.net.object.binary.base64
    /// //   Comment:     BinaryFormatter will throw an exception for this invalid content.
    /// //   Raw value:   YmluYXJ5
    /// //   Safe deserialization of the node thrown an exception: End of Stream encountered before parsing was completed.
    /// //   Unsafe deserialization of the node thrown an exception: End of Stream encountered before parsing was completed.
    /// // 
    /// // ____Metadata in .resx:____
    /// // Name: meta string
    /// //   Type:
    /// //   Alias value:
    /// //   MIME type:
    /// //   Comment:
    /// //   Raw value:   Meta String
    /// //   Real value:  Meta String (System.String) </code>
    /// </example>
    /// <h1 class="heading">Comparison with System.Resources.ResXDataNode<a name="comparison">&#160;</a></h1>
    /// <para>
    /// If instantiated from a <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a> or <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a>
    /// instance, an internal conversion into <see cref="ResXDataNode">KGySoft.Resources.ResXDataNode</see> and <see cref="ResXFileRef">KGySoft.Resources.ResXFileRef</see> automatically occurs.
    /// <note>The compatibility with <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a> is provided without any reference to <c>System.Windows.Forms.dll</c>, where that type is located.</note>
    /// </para>
    /// <para>Unlike <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a>, this <see cref="ResXDataNode"/> implementation
    /// really preserves the original information stored in the .resx file. No deserialization, assembly loading and type resolving occurs until a deserialization is explicitly
    /// requested by calling the <see cref="GetValue">GetValue</see> or <see cref="GetValueSafe">GetValueSafe</see> methods.</para>
    /// <note>When serialized in compatibility mode (see <see cref="ResXResourceWriter.CompatibleFormat">ResXResourceWriter.CompatibleFormat</see>, <see cref="O:KGySoft.Resources.ResXResourceSet.Save">ResXResourceSet.Save</see>, <see cref="ResXResourceManager.SaveResourceSet">ResXResourceManager.SaveResourceSet</see> and <see cref="ResXResourceManager.SaveAllResources">ResXResourceManager.SaveAllResources</see>),
    /// the result will be able to be parsed by the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a> type, too.</note>
    /// <para><strong>Incompatibility</strong> with <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a>:
    /// <list type="bullet">
    /// <item><see cref="Name"/> property is read-only. If you want to use a new name, instantiate a new <see cref="ResXDataNode"/> instance by the <see cref="ResXDataNode(string,object)">ResXDataNode(string, object)</see> constructor and pass the new name and the original <see cref="ResXDataNode"/> as parameters.</item>
    /// <item>There are no <strong>GetValueTypeName</strong> methods. The problem with <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode.getvaluetypename" target="_blank">System.Resources.ResXDataNode.GetValueTypeName</a>
    /// methods is that they are unsafe as they may deserialize the inner object, load assemblies and can throw various unexpected exceptions.
    /// Instead, you can read the original type information stored in the .resx file by <see cref="TypeName"/> and <see cref="AssemblyAliasValue"/> properties. Based on the
    /// retrieved information you can decide whether you really want to deserialize the object by the <see cref="GetValue">GetValue</see> method.
    /// </item>
    /// <item>There is no <strong>GetValue</strong> method with <see cref="AssemblyName">AssemblyName[]</see> argument. That overload ended up using the obsolete
    /// <see cref="Assembly.LoadWithPartialName(string)">Assembly.LoadWithPartialName</see> method. The weakly referenced assemblies however are handled automatically
    /// by using <see cref="Reflector.ResolveType(string,ResolveTypeOptions)">Reflector.ResolveType</see> method so this overload is actually not needed.</item>
    /// <item>The <see cref="GetValue">GetValue</see> method has three parameters instead of one. But all of them are optional so if called from a regular C# code, the method is compatible with
    /// the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode.getvalue" target="_blank">System.Resources.ResXDataNode.GetValue(ITypeResolutionService)</a> method.</item>
    /// <item>There are no public constructors with <see cref="Func{T,TResult}">Func&lt;Type, string&gt;</see> arguments. In the system version these <c>typeNameConverter</c> parameters are used exclusively by non-public methods, which are
    /// called by the <see cref="ResXResourceWriter"/> class. But you can pass such a custom <c>typeNameConverter</c> to the <see cref="ResXResourceWriter"/> constructors.</item>
    /// <item>There is no <strong>GetNodePosition</strong> method because it returned a <a href="https://docs.microsoft.com/en-us/dotnet/api/system.drawing.point" target="_blank">Point</a> structure
    /// from the <c>System.Drawing</c> assembly, which is not referenced by this library. Use <see cref="GetNodeLinePosition">GetNodeLinePosition</see> and <see cref="GetNodeColumnPosition">GetNodeColumnPosition</see> methods instead.</item>
    /// <item>The <see cref="FileRef"/> property returns the same reference during the lifetime of the <see cref="ResXDataNode"/> instance. This is alright as <see cref="ResXFileRef"/> is immutable.
    /// Unlike the system version, the <see cref="FileRef"/> property in this <see cref="ResXDataNode"/> contains exactly the same type information as the original .resx file.</item>
    /// <item>The <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode.getvalue" target="_blank">System.Resources.ResXDataNode.GetValue</a> method often throws <see cref="XmlException"/> if the node contains invalid data. In contrast,
    /// this <see cref="GetValue">GetValue</see> implementation may throw <see cref="XmlException"/>, <see cref="TypeLoadException"/>, <see cref="SerializationException"/> or <see cref="NotSupportedException"/> instead, depending on the actual issue.</item>
    /// </list></para>
    /// <para><strong>New features and improvements</strong> compared to <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a>:
    /// <list type="bullet">
    /// <item><term>Preserving original type information</term>
    /// <description>The originally stored type information, MIME type and the current assembly alias are preserved (see <see cref="TypeName"/>, <see cref="MimeType"/> and <see cref="AssemblyAliasValue"/> properties).
    /// The system version may replace type information with assembly qualified names when the .resx file is parsed. If the assembly qualified name is really needed, you can get it
    /// after explicit deserialization by calling <see cref="Type.AssemblyQualifiedName">GetType().AssemblyQualifiedName</see> on the <see cref="object"/> returned by the <see cref="GetValue">GetValue</see> method.</description></item>
    /// <item><term>Raw content</term><description>You can use the <see cref="ValueData"/> property to read the original raw <see cref="string"/> content stored in the .resx file for this element.</description></item>
    /// <item><term>Advanced string representation</term><description>The <see cref="ToString">ToString</see> method displays the string representation (either of the deserialized object if already cached, or the raw content) so can be used easily in a format argument and provides more debugging information.</description></item>
    /// <item><term>Security</term>
    /// <description>No deserialization, assembly loading and type resolving occurs until a deserialization is explicitly requested by calling the <see cref="GetValue">GetValue</see> or <see cref="GetValueSafe">GetValueSafe</see> methods.
    /// If you use the <see cref="GetValueSafe">GetValueSafe</see> method, then it is guaranteed that no new assembly is loaded during the deserialization, even if the resource was serialized by <see cref="BinaryFormatter"/>.
    /// Additionally, you can check the <see cref="TypeName"/>, <see cref="MimeType"/> and <see cref="AssemblyAliasValue"/> properties to get information
    /// about the type before obtaining the object. You can even check the raw string content by the <see cref="ValueData"/> property.
    /// </description></item>
    /// <item><term>Performance</term>
    /// <description>As there is no deserialization and assembly/type resolving during parsing a .resx file by the <see cref="ResXResourceReader"/> class, the parsing is
    /// much faster. This is true even if <see cref="ResXResourceReader.SafeMode">ResXResourceReader.SafeMode</see> is <see langword="false"/>, because there are always <see cref="ResXDataNode"/>
    /// instances stored internally and deserialization occurs only when a resource is actually accessed.</description></item>
    /// <item><term>Support of non-serializable types</term>
    /// <description>When serializing an object, the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a> type
    /// throws an <see cref="InvalidOperationException"/> for non-serializable types. This implementation can serialize also such types even if compatibility mode is used
    /// (see <see cref="ResXResourceWriter.CompatibleFormat">ResXResourceWriter.CompatibleFormat</see> property and the <see cref="O:KGySoft.Resources.ResXResourceSet.Save">ResXResourceSet.Save</see> methods).
    /// In compatibility mode this is achieved by wrapping the non-serializable types into an <see cref="AnyObjectSerializerWrapper"/> instance so the <see cref="BinaryFormatter"/> will
    /// able to handle them, too.</description></item>
    /// <item><term>Support of generics</term>
    /// <description>This <see cref="ResXDataNode"/> class uses a special <see cref="SerializationBinder"/> implementation, which supports generic types correctly.</description></item>
    /// </list></para>
    /// </remarks>
    /// <seealso cref="ResXFileRef"/>
    /// <seealso cref="ResXResourceReader"/>
    /// <seealso cref="ResXResourceWriter"/>
    /// <seealso cref="ResXResourceSet"/>
    /// <seealso cref="ResXResourceManager"/>
    /// <seealso cref="HybridResourceManager"/>
    /// <seealso cref="DynamicResourceManager"/>
    [Serializable]
    public sealed class ResXDataNode : ISerializable, ICloneable
    {
        #region ResXSerializationBinder class

        /// <summary>
        /// A partial type resolver for the formatters for a custom <see cref="ITypeResolutionService"/> type resolver (deserialization) or
        /// a type name converter (serialization). For deserialization if there is no type resolver a <see cref="WeakAssemblySerializationBinder"/> is used instead.
        /// </summary>
        private sealed class ResXSerializationBinder : SerializationBinder, ISerializationBinder
        {
            #region Fields

            private readonly ITypeResolutionService? typeResolver; // deserialization
            private readonly bool safeMode; // deserialization

            private readonly Func<Type, string?>? typeNameConverter; // serialization
            private readonly bool compatibleFormat; // serialization

            #endregion

            #region Constructors

            /// <summary>
            /// This is the constructor for deserialization
            /// </summary>
            internal ResXSerializationBinder(ITypeResolutionService typeResolver, bool safeMode)
            {
                this.typeResolver = typeResolver;
                this.safeMode = safeMode;
            }

            /// <summary>
            /// This is the constructor for serialization
            /// </summary>
            internal ResXSerializationBinder(Func<Type, string?> typeNameConverter, bool compatibleFormat)
            {
                this.typeNameConverter = typeNameConverter;
                this.compatibleFormat = compatibleFormat;
            }

            #endregion

            #region Methods

#if !NET35
            override
#endif
            public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
            {
                // Actually the same as in the WinForms implementation but fixed for generics.
                assemblyName = null;
                typeName = null;

                if (typeNameConverter == null)
                    return;

                // ReSharper disable once VariableCanBeNotNullable - can be null if serializedType is null, which is not expected just tolerated so it is not marked as nullable
                string? assemblyQualifiedTypeName = ResXCommon.GetAssemblyQualifiedName(serializedType, typeNameConverter, compatibleFormat);
                if (String.IsNullOrEmpty(assemblyQualifiedTypeName))
                    return;

                int genericEnd = assemblyQualifiedTypeName!.LastIndexOf(']');
                int asmNamePos = assemblyQualifiedTypeName.IndexOf(',', genericEnd + 1);
                if (asmNamePos > 0 && asmNamePos < assemblyQualifiedTypeName.Length - 1)
                {
                    assemblyName = assemblyQualifiedTypeName.Substring(asmNamePos + 1).TrimStart();
                    string newTypeName = assemblyQualifiedTypeName.Substring(0, asmNamePos);
                    if (newTypeName != serializedType.FullName)
                        typeName = newTypeName;
                }
            }

            public override Type? BindToType(string assemblyName, string typeName)
            {
                Debug.Assert(typeResolver != null, "typeResolver must be assigned on deserialization");
                string aqnOrFullName = String.IsNullOrEmpty(assemblyName) ? typeName : typeName + ", " + assemblyName;

                Type? result = typeResolver!.GetType(aqnOrFullName);
                if (result != null)
                    return result;

                // The original WinForms version fails for generic types. We do the same in a working way: we strip either the version
                // or full assembly part from the type
                string strippedName = TypeResolver.StripName(aqnOrFullName, true);
                if (strippedName != aqnOrFullName)
                    result = typeResolver.GetType(strippedName);

                if (result != null)
                    return result;

                strippedName = TypeResolver.StripName(aqnOrFullName, false);
                if (strippedName != aqnOrFullName)
                    result = typeResolver.GetType(strippedName);

                // If it is still null, then the binder couldn't handle it. If safe mode is disabled, then letting the formatter take over.
                if (result != null || !safeMode)
                    return result;

                // In safe mode we try to resolve the type without loading any assembly.
                // We do not allow returning a null result because if this binder is used by a BinaryFormatter it may try to load a potentially dangerous assembly.
                result = TypeResolver.ResolveType(aqnOrFullName, null, ResolveTypeOptions.AllowPartialAssemblyMatch);
                if (result == null)
                {
                    string message = String.IsNullOrEmpty(assemblyName)
                        ? Res.BinarySerializationCannotResolveType(typeName)
                        : Res.BinarySerializationCannotResolveTypeInAssemblySafe(typeName, assemblyName);
                    Throw.SerializationException(message);
                }

                return result;
            }

            #endregion
        }

        #endregion

        #region Fields

        #region Static Fields

        private static readonly char[] specialChars = { ' ', '\r', '\n' };
        private static readonly Type[] nonCompatibleModeNativeTypes =
        {
            Reflector.IntPtrType,
            Reflector.UIntPtrType,
            Reflector.RuntimeType,
#if !NET35
            Reflector.BigIntegerType,
#endif
#if NETCOREAPP3_0_OR_GREATER
            Reflector.RuneType,
#endif
#if NET5_0_OR_GREATER
            Reflector.HalfType,
#endif
#if NET6_0_OR_GREATER
            Reflector.DateOnlyType,
            Reflector.TimeOnlyType,
#endif
        };

        private static string? compatibleFileRefTypeName;

        #endregion

        #region Instance Fields

        private string? name;
        private string? comment;
        private string? fileRefBasePath;

        // In a valid ResXDataNode at least one of these must have a value:
        private object? cachedValue;
        private DataNodeInfo? nodeInfo;
        private ResXFileRef? fileRef;

        /// <summary>
        /// The cached assembly qualified name of the value. For FileRef it is initialized as FileRef and once the value
        /// is retrieved it returns the real type of the value.
        /// </summary>
        private string? assemblyQualifiedName;

        /// <summary>
        /// Gets whether the <see cref="assemblyQualifiedName"/> is from a real type. It is false if the <see cref="assemblyQualifiedName"/>
        /// is created from a string or is FileRef.
        /// </summary>
        private bool aqnValid;

        /// <summary>
        /// May contain a cached serialized value of <see cref="cachedValue"/> for cloning a bit faster. If null, can be restored.
        /// </summary>
        private byte[]? rawValue;

        #endregion

        #endregion

        #region Properties

        #region Static Properties

        private static string CompatibleFileRefTypeName => compatibleFileRefTypeName ??= ResXCommon.ResXFileRefNameWinForms + ResXCommon.WinFormsPostfix;

        #endregion

        #region Instance Properties

        #region Public Properties

        /// <summary>
        /// Gets or sets an arbitrary comment regarding this resource.
        /// </summary>
        public string? Comment
        {
            get => comment ?? nodeInfo?.Comment ?? String.Empty;
            set => comment = value;
        }

        /// <summary>
        /// Gets the name of this resource.
        /// </summary>
        public string Name => name ?? nodeInfo?.Name!;

        /// <summary>
        /// Gets the file reference for this resource, or <see langword="null"/>, if this resource does not have a file reference.
        /// </summary>
        public ResXFileRef? FileRef => fileRef;

        /// <summary>
        /// Gets the assembly name defined in the source .resx file if <see cref="TypeName"/> contains an assembly alias name,
        /// or <see langword="null"/>, if <see cref="TypeName"/> contains the assembly qualified name.
        /// If the resource does not contain the .resx information (that is, if the <see cref="ResXDataNode"/> was created from an object or the raw .resx data was removed on a <see cref="GetValue">GetValue</see> call), then this property returns <see langword="null"/>.
        /// </summary>
        public string? AssemblyAliasValue => nodeInfo?.AssemblyAliasValue;

        /// <summary>
        /// Gets the type information as <see cref="string"/> as it is stored in the source .resx file. It can be either an assembly qualified name,
        /// or a type name with or without an assembly alias name. If <see cref="AssemblyAliasValue"/> is not <see langword="null"/>, this property value
        /// contains an assembly alias name. The property returns <see langword="null"/>, if the <c>type</c> attribute is not defined in the .resx file.
        /// If the resource does not contain the .resx information (that is, if the <see cref="ResXDataNode"/> was created from an object or the raw .resx data was removed on a <see cref="GetValue">GetValue</see> call), then this property returns <see langword="null"/>.
        /// </summary>
        public string? TypeName => nodeInfo?.TypeName;

        /// <summary>
        /// Gets the MIME type as it is stored in the .resx file for this resource, or <see langword="null"/>, if the <c>mimetype</c> attribute was not defined in the .resx file.
        /// If the resource does not contain the .resx information (that is, if the <see cref="ResXDataNode"/> was created from an object or the raw .resx data was removed on a <see cref="GetValue">GetValue</see> call), then this property returns <see langword="null"/>.
        /// </summary>
        public string? MimeType => nodeInfo?.MimeType;

        /// <summary>
        /// Gets the raw value data as <see cref="string"/> as it was stored in the source .resx file.
        /// If the resource does not contain the .resx information (that is, if the <see cref="ResXDataNode"/> was created from an object or the raw .resx data was removed on a <see cref="GetValue">GetValue</see> call), then this property returns <see langword="null"/>.
        /// </summary>
        public string? ValueData => nodeInfo?.ValueData;

        #endregion

        #region Internal Properties

        internal object? ValueInternal => cachedValue;

        /// <summary>
        /// Gets the assembly qualified name of the node, or null, if type cannot be determined until deserialized.
        /// </summary>
        internal string? AssemblyQualifiedName
        {
            get
            {
                if (assemblyQualifiedName != null)
                    return assemblyQualifiedName;

                if (cachedValue != null)
                {
                    assemblyQualifiedName = cachedValue.GetType().AssemblyQualifiedName;
                    aqnValid = true;
                }
                else if (nodeInfo != null)
                {
                    // if FileRef is not null and there is a nodeInfo, this property returns the AQN of the FileRef type,
                    // which is alright and is required in InitFileRef
                    aqnValid = false;
                    if (nodeInfo.AssemblyAliasValue == null)
                        assemblyQualifiedName = GetTypeName(nodeInfo);
                    else
                    {
                        string? fullName = GetTypeName(nodeInfo);
                        if (fullName == null)
                            return null;
                        int genericEnd = fullName.LastIndexOf(']');
                        int aliasPos = fullName.IndexOf(',', genericEnd + 1);

                        if (aliasPos >= 0)
                            fullName = fullName.Substring(0, aliasPos);

                        assemblyQualifiedName = fullName + ", " + nodeInfo.AssemblyAliasValue;
                    }
                }
                else // if (fileRef != null)
                {
                    aqnValid = false;
                    assemblyQualifiedName = fileRef!.TypeName;
                }

                return assemblyQualifiedName;
            }
        }

        #endregion

        #endregion

        #endregion

        #region Constructors

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXDataNode"/> class.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        /// <param name="value">The resource to store.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is a string of zero length.</exception>
        /// <remarks>
        /// <para>Unlike <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a>,
        /// <see cref="ResXDataNode">KGySoft.Resources.ResXDataNode</see> supports non-serializable types, too. See the details in the <strong>Remarks</strong>
        /// section of the <see cref="ResXDataNode"/>.</para>
        /// <para>If <paramref name="value"/> is another <see cref="ResXDataNode"/> instance the new <see cref="ResXDataNode"/> instance will be a copy of it with a possibly new name specified in the <paramref name="name"/> parameter.
        /// A <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a> instance is also recognized.
        /// <note>The compatibility with <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxdatanode" target="_blank">System.Resources.ResXDataNode</a> is provided without any reference to <c>System.Windows.Forms.dll</c>, where that type is located.</note>
        /// </para>
        /// <para>If <paramref name="value"/> is a <see cref="ResXFileRef"/> instance, the new <see cref="ResXDataNode"/> will refer to a file reference.
        /// A <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a> instance is also recognized.
        /// For <see cref="ResXFileRef"/> a <paramref name="value"/> with relative path you might want to use the <see cref="ResXDataNode(string,ResXFileRef,string)">ResXDataNode(string, ResXFileRef, string)</see> constructor where you can specify a base path.
        /// <note>The compatibility with <a href="https://docs.microsoft.com/en-us/dotnet/api/system.resources.resxfileref" target="_blank">System.Resources.ResXFileRef</a> is provided without any reference to <c>System.Windows.Forms.dll</c>, where that type is located.</note>
        /// </para>
        /// </remarks>
        public ResXDataNode(string name, object? value)
        {
            if (name == null!)
                Throw.ArgumentNullException(Argument.name);
            if (name.Length == 0)
                Throw.ArgumentException(Argument.name, Res.ArgumentEmpty);

            this.name = name;

            // 1.) null
            if (value == null)
            {
                // unlike the WinForms version, we use ResXNullRef to indicate a null value; otherwise, in GetValue we would always try to deserialize the null value
                cachedValue = ResXNullRef.Value;
                return;
            }

            // 2.) ResXDataNode
            if (value is ResXDataNode other)
            {
                InitFrom(other);
                return;
            }

            // 3.) FileRef
            if (value is ResXFileRef fr)
            {
                fileRef = fr;
                return;
            }

#if !NETCOREAPP2_0
            string? typeName = value.GetType().AssemblyQualifiedName;
            if (typeName != null)
            {
                // 4.) System ResXDataNode
                if (typeName.StartsWith(ResXCommon.ResXDataNodeNameWinForms, StringComparison.Ordinal))
                {
                    InitFromWinForms(value);
                    return;
                }

                // 5.) System ResXFileRef
                if (typeName.StartsWith(ResXCommon.ResXFileRefNameWinForms, StringComparison.Ordinal))
                {
                    fileRef = ResXFileRef.InitFromWinForms(value);
                    return;
                }
            }
#endif

            // 6.) other value
            cachedValue = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResXDataNode"/> class with a reference to a resource file.
        /// </summary>
        /// <param name="name">The name of the resource.</param>
        /// <param name="fileRef">The file reference.</param>
        /// <param name="basePath">A default base path for the relative path defined in <paramref name="fileRef"/>. This can be overridden on calling the <see cref="GetValue">GetValue</see> method.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="name"/> or <paramref name="fileRef"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is a string of zero length.</exception>
        /// <exception cref="System.ArgumentException">name</exception>
        public ResXDataNode(string name, ResXFileRef fileRef, string? basePath = null)
        {
            if (name == null!)
                Throw.ArgumentNullException(Argument.name);
            if (name.Length == 0)
                Throw.ArgumentException(Argument.name, Res.ArgumentEmpty);
            if (fileRef == null!)
                Throw.ArgumentNullException(Argument.fileRef);

            this.fileRef = fileRef;
            this.name = name;
            fileRefBasePath = basePath;
        }

        #endregion

        #region Internal Constructors

        /// <summary>
        /// Called by <see cref="ResXResourceReader"/>.
        /// </summary>
        internal ResXDataNode(DataNodeInfo nodeInfo, string? fileRefBasePath)
        {
            // No need to set name and comment fields here. They will be set if nodeInfo is cleared.
            this.nodeInfo = nodeInfo;
            InitFileRef(fileRefBasePath);
        }

        #endregion

        #region Private Constructors

        private ResXDataNode(SerializationInfo info, StreamingContext context)
        {
            nodeInfo = new DataNodeInfo
            {
                Name = info.GetString(ResXCommon.NameStr)!,
                Comment = info.GetString(ResXCommon.CommentStr),
                TypeName = info.GetString(ResXCommon.TypeStr),
                MimeType = info.GetString(ResXCommon.MimeTypeStr),
                ValueData = info.GetString(ResXCommon.ValueStr),
                AssemblyAliasValue = info.GetString(ResXCommon.AliasStr),
                CompatibleFormat = info.GetBoolean(nameof(DataNodeInfo.CompatibleFormat))
            };

            InitFileRef(info.GetString(nameof(fileRefBasePath)));
        }

        private ResXDataNode(ResXDataNode other)
        {
            name = other.name;
            comment = other.Comment;
            fileRefBasePath = other.fileRefBasePath;
            fileRef = other.FileRef;

            // nodeInfo is regenerated only if also fileRef is null
            nodeInfo = other.nodeInfo?.Clone() ?? (fileRef == null ? other.GetDataNodeInfo(null, null) : null);
        }

        #endregion

        #endregion

        #region Methods

        #region Static Methods

        /// <summary>
        /// Gets the type name stored in the node or string, which is the default type.
        /// </summary>
        private static string? GetTypeName(DataNodeInfo nodeInfo)
        {
            if (nodeInfo.TypeName != null)
                return nodeInfo.TypeName;

            // occurs when there is no <value> element in the .resx
            if (nodeInfo.ValueData == null)
                return typeof(ResXNullRef).AssemblyQualifiedName;

            if (nodeInfo.MimeType == null)
                return Reflector.StringType.AssemblyQualifiedName;

            return null;
        }

        private static bool IsFileRef(string? assemblyQualifiedName)
        {
            if (assemblyQualifiedName == null)
                return false;

            // the common scenario as it is saved by system .resx
            return assemblyQualifiedName.StartsWith(ResXCommon.ResXFileRefNameWinForms, StringComparison.Ordinal)
                   || assemblyQualifiedName.StartsWith(ResXCommon.ResXFileRefNameKGySoft, StringComparison.Ordinal);
        }

        private static bool IsNullRef(string? assemblyQualifiedName)
        {
            if (assemblyQualifiedName == null)
                return false;

            // the common scenario as it is saved by system .resx
            return assemblyQualifiedName.StartsWith(ResXCommon.ResXNullRefNameWinForms, StringComparison.Ordinal)
                   || assemblyQualifiedName.StartsWith(ResXCommon.ResXNullRefNameKGySoft, StringComparison.Ordinal);
        }

        private static byte[] FromBase64WrappedString(string text)
        {
            if (text.IndexOfAny(specialChars) == -1)
                return Convert.FromBase64String(text);

            StringBuilder sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                switch (text[i])
                {
                    case ' ':
                    case '\r':
                    case '\n':
                        break;
                    default:
                        sb.Append(text[i]);
                        break;
                }
            }

            return Convert.FromBase64String(sb.ToString());
        }

        private static Type? ResolveType(string assemblyQualifiedName, ITypeResolutionService? typeResolver, bool safeMode)
        {
            // Mapping WinForms refs to KGySoft ones.
            if (IsFileRef(assemblyQualifiedName))
                return typeof(ResXFileRef);

            if (IsNullRef(assemblyQualifiedName))
                return typeof(ResXNullRef);

            Type? result = null;
            if (typeResolver != null)
                result = typeResolver.GetType(assemblyQualifiedName, false);

            if (result == null)
            {
                var options = ResolveTypeOptions.AllowPartialAssemblyMatch;
                if (!safeMode)
                    options |= ResolveTypeOptions.TryToLoadAssemblies;
                result = TypeResolver.ResolveType(assemblyQualifiedName, null, options);
            }

            if (result == null)
                return null;

            // Mapping WinForms refs to KGySoft ones (can happen in case of non-usual aliases)
            if (IsFileRef(result.AssemblyQualifiedName))
                return typeof(ResXFileRef);

            if (IsNullRef(result.AssemblyQualifiedName!))
                return typeof(ResXNullRef);

            return result;
        }

        private static bool TryDeserializeBySoapFormatter(DataNodeInfo dataNodeInfo, [MaybeNullWhen(false)]out object result)
        {
            string text = dataNodeInfo.ValueData ?? String.Empty;
            byte[] serializedData = FromBase64WrappedString(text);

            if (serializedData.Length > 0)
            {
                IFormatter? formatter = ResXCommon.GetSoapFormatter();
                if (formatter != null)
                {
                    using (var ms = new MemoryStream(serializedData))
                        result = formatter.Deserialize(ms);
                    if (result != ResXNullRef.Value && IsNullRef(result.GetType().AssemblyQualifiedName))
                        result = ResXNullRef.Value;
                    return true;
                }
            }

            result = null;
            return false;
        }

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Retrieves the line position of the resource in the resource file.
        /// </summary>
        /// <returns>
        /// An <see cref="int"/> that specifies the line position of this resource in the resource file.
        /// If the resource does not contain the .resx information (that is, if the <see cref="ResXDataNode"/> was created from an object or the raw .resx data was removed on a <see cref="GetValue">GetValue</see> call), then this method returns 0.
        /// </returns>
        public int GetNodeLinePosition() => nodeInfo?.Line ?? 0;

        /// <summary>
        /// Retrieves the column position of the resource in the resource file.
        /// </summary>
        /// <returns>
        /// An <see cref="int"/> that specifies the column position of this resource in the resource file.
        /// If the resource does not contain the .resx information (that is, if the <see cref="ResXDataNode"/> was created from an object or the raw .resx data was removed on a <see cref="GetValue">GetValue</see> call), then this method returns 0.
        /// </returns>
        public int GetNodeColumnPosition() => nodeInfo?.Column ?? 0;

        /// <summary>
        /// Retrieves the object that is stored by this node.
        /// </summary>
        /// <returns>The object that corresponds to the stored value.</returns>
        /// <param name="typeResolver">A custom type resolution service to use for resolving type names.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="basePath">Defines a base path for file reference values. Used when <see cref="FileRef"/> is not <see langword="null"/>.
        /// If this parameter is <see langword="null"/>, tries to use the original base path, if any.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="cleanupRawData"><see langword="true"/>&#160;to free the underlying XML data once the value is deserialized; otherwise, <see langword="false"/>.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <exception cref="TypeLoadException">The corresponding type or its container assembly could not be loaded.</exception>
        /// <exception cref="SerializationException">An error occurred during the binary deserialization of the resource.</exception>
        /// <exception cref="FileNotFoundException">The resource is a file reference and the referenced file cannot be found.</exception>
        /// <exception cref="NotSupportedException">Unsupported MIME type or an appropriate type converter is not available.</exception>
        /// <remarks>
        /// <note type="security">When using this method make sure that the .resx data to be deserialized is from a trusted source.
        /// This method might load assemblies during type resolve. To disallow loading assemblies use the <see cref="GetValueSafe">GetValueSafe</see> method instead.</note>
        /// <para>If the stored value currently exists in memory, it is returned directly.</para>
        /// <para>If the resource is a file reference, <see cref="GetValue">GetValue</see> tries to open the file and deserialize its content.</para>
        /// <para>If the resource is not a file reference, <see cref="GetValue">GetValue</see> tries to deserialize the value from the raw .resx string content.</para>
        /// </remarks>
        public object? GetValue(ITypeResolutionService? typeResolver = null, string? basePath = null, bool cleanupRawData = false)
            => DoGetValue(typeResolver, basePath, cleanupRawData, false);

        /// <summary>
        /// Retrieves the object that is stored by this node, not allowing loading assemblies during the possible deserialization.
        /// </summary>
        /// <returns>The object that corresponds to the stored value.</returns>
        /// <param name="typeResolver">A custom type resolution service to use for resolving type names.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="basePath">Defines a base path for file reference values. Used when <see cref="FileRef"/> is not <see langword="null"/>.
        /// If this parameter is <see langword="null"/>, tries to use the original base path, if any.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <param name="cleanupRawData"><see langword="true"/>&#160;to free the underlying XML data once the value is deserialized; otherwise, <see langword="false"/>.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <exception cref="TypeLoadException">The corresponding type or its container assembly could not be loaded.</exception>
        /// <exception cref="SerializationException">An error occurred during the binary deserialization of the resource.</exception>
        /// <exception cref="FileNotFoundException">The resource is a file reference and the referenced file cannot be found.</exception>
        /// <exception cref="NotSupportedException">Unsupported MIME type or an appropriate type converter is not available.</exception>
        /// <remarks>
        /// <note type="security">When using this method it is guaranteed that no new assembly is loaded during the deserialization, unless it is resolved by the specified <paramref name="typeResolver"/>.
        /// To allow loading assemblies use the <see cref="GetValue">GetValue</see> method instead.</note>
        /// <para>If the stored value currently exists in memory, it is returned directly.</para>
        /// <para>If the resource is a file reference, <see cref="GetValueSafe">GetValueSafe</see> tries to open the file and deserialize its content.
        /// The deserialization will fail if the assembly of the type to create type is not already loaded.</para>
        /// <para>If the resource is not a file reference, <see cref="GetValueSafe">GetValueSafe</see> tries to deserialize the value from the raw .resx string content.
        /// The deserialization will fail if the assembly of the type to create type is not already loaded.</para>
        /// </remarks>
        public object? GetValueSafe(ITypeResolutionService? typeResolver = null, string? basePath = null, bool cleanupRawData = false)
            => DoGetValue(typeResolver, basePath, cleanupRawData, true);

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>The string representation of this <see cref="ResXDataNode"/>.</returns>
        public override string ToString()
        {
            if (fileRef != null)
                return fileRef.ToString();
            object? valueCopy = cachedValue;
            if (valueCopy != null)
                return valueCopy.ToString() ?? String.Empty;
            DataNodeInfo? nodeInfoCopy = nodeInfo;
            if (nodeInfoCopy != null)
                return nodeInfoCopy.ValueData ?? String.Empty;
            return base.ToString()!;
        }

        /// <summary>
        /// Creates a new <see cref="ResXDataNode"/> that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new <see cref="ResXDataNode"/> instance that is a copy of this instance.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone() => new ResXDataNode(this);

        #endregion

        #region Internal Methods

        /// <summary>
        /// Called from <see cref="ResXResourceSet"/> when <see cref="ResXResourceSet.SafeMode"/> is <see langword="true"/>.
        /// </summary>
        internal object? GetSafeValueInternal(bool isString, bool cloneValue)
        {
            if (!isString)
                return cloneValue ? new ResXDataNode(this) : this;

            // returning a string for any type below
            object? result = cachedValue;
            if (result is string)
                return result;

            if (nodeInfo != null)
                return nodeInfo.ValueData;

            if (fileRef != null)
                return fileRef.ToString();

            // here there is no available string meta so generating nodeInfo
            Debug.Assert(result != null);
            nodeInfo = GetDataNodeInfo(null, null);
            return nodeInfo.ValueData;
        }

        /// <summary>
        /// Called from <see cref="ResXResourceSet"/> when <see cref="ResXResourceSet.SafeMode"/> is <see langword="false"/>.
        /// </summary>
        internal object? GetUnsafeValueInternal(ITypeResolutionService? typeResolver, bool isString, bool cloneValue, bool cleanup, string? basePath)
        {
            if (cachedValue is string)
                return cachedValue;
            if (cachedValue == ResXNullRef.Value)
                return null;

            if (!isString)
            {
                return cloneValue
                    ? CloneValue(typeResolver, basePath)
                    : GetValue(typeResolver, basePath, cleanup);
            }

            // string result required below

            if (cachedValue != null)
                Throw.InvalidOperationException(Res.ResourcesNonStringResourceWithType(Name, cachedValue.GetType().GetName(TypeNameKind.LongName)));

            // result is not deserialized here yet

            // Omitting assembly because of the version. If we know before the serialization that result is not a string,
            // we can already throw an exception. But type is checked once again at the end, after deserialization.
            string stringName = Reflector.StringType.FullName!;
            string? aqn = AssemblyQualifiedName;
            if (aqn != null && !IsNullRef(aqn) && !aqn.StartsWith(stringName, StringComparison.Ordinal) && (fileRef == null || !fileRef.TypeName.StartsWith(stringName, StringComparison.Ordinal)))
                Throw.InvalidOperationException(Res.ResourcesNonStringResourceWithType(Name, fileRef == null ? aqn : fileRef.TypeName));

            object? result = GetValue(typeResolver, basePath, !cloneValue && cleanup);
            if (result == null || result is string)
                return result;

            Throw.InvalidOperationException(Res.ResourcesNonStringResourceWithType(Name, result.GetType().GetName(TypeNameKind.LongName)));
            return null;
        }

        /// <summary>
        /// Gets or (re)generates the nodeInfo. Parameters are not null only if called from a <see cref="ResXResourceWriter"/>.
        /// <paramref name="safeMode"/> is relevant only when <paramref name="compatibleFormat"/> is changed to true and there is no cached value yet.
        /// </summary>
        internal DataNodeInfo GetDataNodeInfo(Func<Type, string?>? typeNameConverter, bool? compatibleFormat, bool safeMode = true)
        {
            // Regenerating existing node info only if switching to compatible format because the other way is supported,
            // and nodeInfo.CompatibleFormat is true for most types anyway.
            bool toGenerate = nodeInfo == null || compatibleFormat == true && !nodeInfo.CompatibleFormat;

            if (nodeInfo == null)
                nodeInfo = new DataNodeInfo { Name = name! };
            else if (nodeInfo.ValueData == null && nodeInfo.TypeName == null)
            {
                // handling that <value> element can be missing in .resx:
                // configuring a regular null; otherwise, an empty string would be written next time
                nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(typeof(ResXNullRef), typeNameConverter, compatibleFormat.GetValueOrDefault());
                nodeInfo.AssemblyAliasValue = null;
                nodeInfo.CompatibleFormat = compatibleFormat.GetValueOrDefault();
            }

            // comment is a mutable property so setting it in all cases
            nodeInfo.Comment = Comment;

            // Though FileRef is a public property, it is immutable so there is no need to always refresh NodeInfo from fileRef
            if (!toGenerate)
                return nodeInfo;

            // if we don't have a DataNodeInfo it could be either a direct object OR a FileRef
            if (fileRef != null)
            {
                // from fileRef
                nodeInfo.ValueData = fileRef.ToString();
                nodeInfo.MimeType = null;
                nodeInfo.CompatibleFormat = compatibleFormat.GetValueOrDefault();
                if (compatibleFormat.GetValueOrDefault())
                {
                    if (String.IsNullOrEmpty(nodeInfo.TypeName) || !nodeInfo.TypeName!.StartsWith(ResXCommon.ResXFileRefNameWinForms, StringComparison.Ordinal))
                    {
                        nodeInfo.TypeName = CompatibleFileRefTypeName;
                        nodeInfo.AssemblyAliasValue = null;
                        aqnValid = false;
                        assemblyQualifiedName = null;
                    }
                }
                else if (String.IsNullOrEmpty(nodeInfo.TypeName) || !nodeInfo.TypeName!.StartsWith(ResXCommon.ResXFileRefNameKGySoft, StringComparison.Ordinal))
                {
                    nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(typeof(ResXFileRef), typeNameConverter, compatibleFormat.GetValueOrDefault());
                    nodeInfo.AssemblyAliasValue = null;
                    aqnValid = false;
                    assemblyQualifiedName = null;
                }
            }
            // first initialization or switching to compatible format
            else
            {
                // Cached value can be null here when we are switching to compatible format and there was no deserialization yet.
                cachedValue ??= NodeInfoToObject(nodeInfo, null, safeMode);
                InitNodeInfo(typeNameConverter, compatibleFormat.GetValueOrDefault());
            }

            return nodeInfo;
        }

        #endregion

        #region Private Methods

#if !NETCOREAPP2_0
        private void InitFromWinForms(object other)
        {
            cachedValue = Accessors.ResXDataNode_GetValue(other);
            comment = Accessors.ResXDataNode_GetComment(other);
            object? fileRefWinForms = Accessors.ResXDataNode_GetFileRef(other);
            if (fileRefWinForms != null)
                fileRef = ResXFileRef.InitFromWinForms(fileRefWinForms);
            object? nodeInfoWinForms = Accessors.ResXDataNode_GetNodeInfo(other);
            if (nodeInfoWinForms != null)
                nodeInfo = DataNodeInfo.InitFromWinForms(nodeInfoWinForms);

            // the WinForms version uses simply null instead of ResXNullRef
            if (cachedValue == null && fileRef == null && nodeInfo == null)
                cachedValue = ResXNullRef.Value;
        }
#endif

        private void InitFrom(ResXDataNode other)
        {
            cachedValue = other.cachedValue;
            assemblyQualifiedName = other.assemblyQualifiedName;
            aqnValid = other.aqnValid;
            comment = other.comment;
            if (other.fileRef != null)
                fileRef = other.fileRef;
            if (other.nodeInfo != null)
                nodeInfo = other.nodeInfo.Clone();
        }

        private void InitFileRef(string? basePath)
        {
            if (!IsFileRef(AssemblyQualifiedName))
                return;

            if (ResXFileRef.TryParse(nodeInfo!.ValueData, out fileRef))
                fileRefBasePath = basePath;
        }

        /// <summary>
        /// (Re)generates the nodeInfo from a value.
        /// </summary>
        private void InitNodeInfo(Func<Type, string?>? typeNameConverter, bool compatibleFormat)
        {
            Debug.Assert(cachedValue != null, "value is null in FillDataNodeInfoFromObject");

            // 1.) natively supported type
            if (CanConvertNatively(compatibleFormat))
            {
                InitNodeInfoNative(typeNameConverter);
                return;
            }

            // 2.) byte[] (should not be checked by as cast because due to the CLR behavior that would allow sbyte[] as well)
            if (cachedValue!.GetType() == Reflector.ByteArrayType)
            {
                byte[] bytes = (byte[])cachedValue;
                nodeInfo!.ValueData = ResXCommon.ToBase64(bytes);
                nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(Reflector.ByteArrayType, typeNameConverter, compatibleFormat);
                nodeInfo.AssemblyAliasValue = null;
                nodeInfo.CompatibleFormat = true;
                return;
            }

            // 3.) CultureInfo - because CultureInfoConverter sets the CurrentUICulture in a finally block
            if (cachedValue is CultureInfo ci)
            {
                nodeInfo!.ValueData = ci.Name;
                nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(typeof(CultureInfo), typeNameConverter, compatibleFormat);
                nodeInfo.AssemblyAliasValue = null;
                nodeInfo.CompatibleFormat = true;
                return;
            }

            // 4.) null
            if (cachedValue == ResXNullRef.Value)
            {
                nodeInfo!.ValueData = null;
                nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(typeof(ResXNullRef), typeNameConverter, compatibleFormat);
                nodeInfo.AssemblyAliasValue = null;
                nodeInfo.CompatibleFormat = compatibleFormat;
                return;
            }

            // 5.) to string by TypeConverter
            Type type = cachedValue.GetType();
            TypeConverter tc = TypeDescriptor.GetConverter(type);
            bool toString = tc.CanConvertTo(Reflector.StringType);
            bool fromString = tc.CanConvertFrom(Reflector.StringType);
            try
            {
                if (toString && fromString)
                {
                    nodeInfo!.ValueData = tc.ConvertToInvariantString(cachedValue);
                    nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(type, typeNameConverter, compatibleFormat);
                    nodeInfo.AssemblyAliasValue = null;
                    nodeInfo.CompatibleFormat = true;
                    return;
                }
            }
            catch (NotSupportedException)
            {
                // Some custom type converters will throw this in ConvertTo(string)
                // to indicate that this object should be serialized through ISerializable
                // instead of as a string. This is semi-wrong, but something we will have to
                // live with to allow user created Cursors to be serializable.
            }

            // 6.) to byte[] by TypeConverter
            bool toByteArray = tc.CanConvertTo(Reflector.ByteArrayType);
            bool fromByteArray = tc.CanConvertFrom(Reflector.ByteArrayType);
            if (toByteArray && fromByteArray)
            {
                byte[] data = (byte[])tc.ConvertTo(cachedValue, Reflector.ByteArrayType)!;
                nodeInfo!.ValueData = ResXCommon.ToBase64(data);
                nodeInfo.MimeType = ResXCommon.ByteArraySerializedObjectMimeType;
                nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(type, typeNameConverter, compatibleFormat);
                nodeInfo.AssemblyAliasValue = null;
                nodeInfo.CompatibleFormat = true;
                return;
            }

            // 7.) to byte[] by system BinaryFormatter
            nodeInfo!.TypeName = null;
            nodeInfo.AssemblyAliasValue = null;
            if (compatibleFormat)
            {
                var binaryFormatter = new BinaryFormatter();
                if (typeNameConverter != null)
                    binaryFormatter.Binder = new ResXSerializationBinder(typeNameConverter, true);

                using (var ms = new MemoryStream())
                {
                    // When serializing, we allow unsafe handling. On deserialization safe mode can be specified.
                    // Known regression: AnyObjectSerializerWrapper used to be support non-zero based, non-primitive arrays in compatible mode
                    if (!type.IsSerializable)
                        binaryFormatter.SurrogateSelector = new CustomSerializerSurrogateSelector();
                    binaryFormatter.Serialize(ms, cachedValue);
                    nodeInfo.ValueData = ResXCommon.ToBase64(ms.ToArray());
                }

                nodeInfo.MimeType = ResXCommon.DefaultSerializedObjectMimeType;
                nodeInfo.CompatibleFormat = true;
                return;
            }

            // 8.) to byte[] by KGySoft BinarySerializationFormatter
            var serializer = new BinarySerializationFormatter();
            if (typeNameConverter != null)
                serializer.Binder = new ResXSerializationBinder(typeNameConverter, false);
            nodeInfo.ValueData = ResXCommon.ToBase64(serializer.Serialize(cachedValue));
            nodeInfo.MimeType = ResXCommon.KGySoftSerializedObjectMimeType;
            nodeInfo.CompatibleFormat = false;
        }

        private void InitNodeInfoNative(Func<Type, string?>? typeNameConverter)
        {
            Debug.Assert(cachedValue != null);
            nodeInfo!.AssemblyAliasValue = null;
            if (cachedValue is string str)
            {
                nodeInfo.CompatibleFormat = true;
                nodeInfo.ValueData = str;
                return;
            }

            Type type = cachedValue!.GetType();
            nodeInfo.TypeName = ResXCommon.GetAssemblyQualifiedName(type, typeNameConverter, false);
            nodeInfo.ValueData = cachedValue.ToStringInternal(CultureInfo.InvariantCulture);
            nodeInfo.CompatibleFormat = !type.In(nonCompatibleModeNativeTypes);
        }

        private object? DoGetValue(ITypeResolutionService? typeResolver, string? basePath, bool cleanupRawData, bool safeMode)
        {
            object? result;
            if (cachedValue != null)
                result = cachedValue;
            else if (fileRef != null)
            {
                // fileRef.TypeName is always an AQN, so there is no need to play with the alias name.
                Type? objectType = ResolveType(fileRef.TypeName, typeResolver, safeMode);
                if (objectType != null)
                    cachedValue = result = fileRef.GetValue(objectType, basePath ?? fileRefBasePath);
                else
                {
                    Throw.TypeLoadException(nodeInfo == null
                        ? safeMode
                            ? Res.ResourcesTypeLoadExceptionSafe(fileRef.TypeName)
                            : Res.ResourcesTypeLoadException(fileRef.TypeName)
                        : safeMode
                            ? Res.ResourcesTypeLoadExceptionSafeAt(fileRef.TypeName, nodeInfo.Line, nodeInfo.Column)
                            : Res.ResourcesTypeLoadExceptionAt(fileRef.TypeName, nodeInfo.Line, nodeInfo.Column));
                    return default;
                }
            }
            else
            {
                // it's embedded, we deserialize it
                Debug.Assert(nodeInfo != null);
                cachedValue = result = NodeInfoToObject(nodeInfo!, typeResolver, safeMode);
            }

            if (cleanupRawData && nodeInfo != null)
            {
                name ??= nodeInfo.Name;
                comment ??= nodeInfo.Comment;
                nodeInfo = null;
            }

            // if AQN is already set, but not from the resulting type, resetting it with the real type of the value
            if (!aqnValid && assemblyQualifiedName != null)
            {
                assemblyQualifiedName = result!.GetType().AssemblyQualifiedName;
                aqnValid = true;
            }

            return result == ResXNullRef.Value ? null : result;
        }

        private bool CanConvertNatively(bool compatibleFormat)
        {
            Type type = cachedValue!.GetType();
            return type.CanBeParsedNatively() && (!compatibleFormat || !type.In(nonCompatibleModeNativeTypes));
        }

        private object? NodeInfoToObject(DataNodeInfo dataNodeInfo, ITypeResolutionService? typeResolver, bool safeMode)
        {
            // Handling that <value> can be missing in .resx.
            // If TypeName is not null, then a TypeConverter may handle null
            string? valueData = dataNodeInfo.ValueData;
            if (valueData == null && dataNodeInfo.TypeName == null)
                return ResXNullRef.Value;

            // from MIME type
            if (!String.IsNullOrEmpty(dataNodeInfo.MimeType))
                return NodeInfoToObjectByMime(dataNodeInfo, typeResolver, safeMode);

            string typeName = AssemblyQualifiedName!;
            Debug.Assert(typeName != null!, "If there is no MIME type, typeName is expected to be string");

            // string: Even <value/> means an empty string. Null reference is encoded by ResXNullRef.
            if (typeName == null)
                return valueData ?? String.Empty;

            Type? type = ResolveType(typeName, typeResolver, safeMode);
            if (type == null)
            {
                string newMessage = safeMode
                    ? Res.ResourcesTypeLoadExceptionSafeAt(typeName, dataNodeInfo.Line, dataNodeInfo.Column)
                    : Res.ResourcesTypeLoadExceptionAt(typeName, dataNodeInfo.Line, dataNodeInfo.Column);
                XmlException xml = ResXCommon.CreateXmlException(newMessage, dataNodeInfo.Line, dataNodeInfo.Column);
                Throw.TypeLoadException(newMessage, xml);
            }

            // 1.) Native type - type converter is slower and will not convert negative zeros, for example.
            if (type.CanBeParsedNatively())
                return valueData.Parse(type, safeMode) ?? ResXNullRef.Value;

            // 2.) null
            if (type == typeof(ResXNullRef))
                return ResXNullRef.Value;

            // 3.) byte[]
            if (type == Reflector.ByteArrayType)
                return valueData == null ? ResXNullRef.Value : FromBase64WrappedString(valueData);

#if !NETFRAMEWORK
            // 4.) CultureInfo - There is no CultureInfoConverter in .NET Core but we handle it in InitNodeInfo
            if (type == typeof(CultureInfo))
                return valueData == null ? ResXNullRef.Value : new CultureInfo(valueData);
#endif

            // 5.) By TypeConverter from string. It may support also converting from null.
            TypeConverter tc = TypeDescriptor.GetConverter(type);
            if (!tc.CanConvertFrom(Reflector.StringType))
            {
                string message = Res.ResourcesConvertFromStringNotSupportedAt(typeName, dataNodeInfo.Line, dataNodeInfo.Column, Res.ResourcesConvertFromStringNotSupported(tc.GetType()));
                XmlException xml = ResXCommon.CreateXmlException(message, dataNodeInfo.Line, dataNodeInfo.Column);
                Throw.NotSupportedException(message, xml);
            }

            try
            {
                return tc.ConvertFromInvariantString(valueData!) ?? ResXNullRef.Value;
            }
            catch (NotSupportedException e)
            {
                string message = Res.ResourcesConvertFromStringNotSupportedAt(typeName, dataNodeInfo.Line, dataNodeInfo.Column, e.Message);
                XmlException xml = ResXCommon.CreateXmlException(message, dataNodeInfo.Line, dataNodeInfo.Column, e);
                Throw.NotSupportedException(message, xml);
                return default;
            }
            catch (Exception e) when (valueData == null && (e is ArgumentNullException || e is NullReferenceException))
            {
                // Handling that System serializer emits <value/> both for null and empty strings.
                return tc.ConvertFromInvariantString(String.Empty) ?? ResXNullRef.Value;
            }
        }

        private object? NodeInfoToObjectByMime(DataNodeInfo dataNodeInfo, ITypeResolutionService? typeResolver, bool safeMode)
        {
            string mimeType = dataNodeInfo.MimeType!;
            string text = dataNodeInfo.ValueData ?? String.Empty;

            // 1.) BinaryFormatter
            if (mimeType.In(ResXCommon.BinSerializedMimeTypes))
            {
                byte[] serializedData = FromBase64WrappedString(text);
                using var surrogate = new CustomSerializerSurrogateSelector { IgnoreNonExistingFields = true, SafeMode = safeMode };
#if !NETFRAMEWORK
                // Supporting MemoryStream even where it is not serializable anymore
                if (safeMode)
                    surrogate.IsTypeSupported = t => t == typeof(MemoryStream) || SerializationHelper.IsSafeType(t);
#endif
                var binaryFormatter = new BinaryFormatter
                {
                    SurrogateSelector = surrogate,
                    Binder = typeResolver != null
                        ? new ResXSerializationBinder(typeResolver, safeMode)
                        : new WeakAssemblySerializationBinder { SafeMode = safeMode }
                };

                object? result = null;
                if (serializedData.Length > 0)
                {
                    using (var ms = new MemoryStream(serializedData))
                        result = binaryFormatter.Deserialize(ms);
                    if (result != ResXNullRef.Value && IsNullRef(result.GetType().AssemblyQualifiedName))
                        result = ResXNullRef.Value;
                }

                return result;
            }

            // 2.) By TypeConverter from byte[]
            if (mimeType == ResXCommon.ByteArraySerializedObjectMimeType)
            {
                string? typeName = AssemblyQualifiedName;
                if (String.IsNullOrEmpty(typeName))
                    throw ResXCommon.CreateXmlException(Res.ResourcesMissingAttribute(ResXCommon.TypeStr, dataNodeInfo.Line, dataNodeInfo.Column), dataNodeInfo.Line, dataNodeInfo.Column);

                Type? type = ResolveType(typeName!, typeResolver, safeMode);
                if (type == null)
                {
                    string newMessage = safeMode
                        ? Res.ResourcesTypeLoadExceptionSafeAt(typeName!, dataNodeInfo.Line, dataNodeInfo.Column)
                        : Res.ResourcesTypeLoadExceptionAt(typeName!, dataNodeInfo.Line, dataNodeInfo.Column);
                    XmlException xml = ResXCommon.CreateXmlException(newMessage, dataNodeInfo.Line, dataNodeInfo.Column);
                    Throw.TypeLoadException(newMessage, xml);
                }

                TypeConverter byteArrayConverter = TypeDescriptor.GetConverter(type);
                if (!byteArrayConverter.CanConvertFrom(Reflector.ByteArrayType))
                {
                    string message = Res.ResourcesConvertFromByteArrayNotSupportedAt(typeName!, dataNodeInfo.Line, dataNodeInfo.Column, Res.ResourcesConvertFromByteArrayNotSupported(byteArrayConverter.GetType()));
                    XmlException xml = ResXCommon.CreateXmlException(message, dataNodeInfo.Line, dataNodeInfo.Column);
                    Throw.NotSupportedException(message, xml);
                }

                byte[] serializedData = FromBase64WrappedString(text);

                try
                {
                    return byteArrayConverter.ConvertFrom(serializedData);
                }
                catch (NotSupportedException e)
                {
                    string message = Res.ResourcesConvertFromByteArrayNotSupportedAt(typeName!, dataNodeInfo.Line, dataNodeInfo.Column, e.Message);
                    XmlException xml = ResXCommon.CreateXmlException(message, dataNodeInfo.Line, dataNodeInfo.Column, e);
                    Throw.NotSupportedException(message, xml);
                }
            }

            // 3.) BinarySerializationFormatter
            if (mimeType == ResXCommon.KGySoftSerializedObjectMimeType)
            {
                byte[] serializedData = FromBase64WrappedString(text);
                using var surrogate = new CustomSerializerSurrogateSelector { IgnoreNonExistingFields = true, SafeMode = safeMode };
#if !NETFRAMEWORK
                // Supporting MemoryStream even where it is not serializable anymore
                if (safeMode)
                    surrogate.IsTypeSupported = t => t == typeof(MemoryStream) || SerializationHelper.IsSafeType(t);
#endif
                var serializer = new BinarySerializationFormatter(safeMode ? BinarySerializationOptions.SafeMode : BinarySerializationOptions.None)
                {
                    SurrogateSelector = surrogate,
                    Binder = typeResolver != null
                        ? new ResXSerializationBinder(typeResolver, safeMode)
                        : new WeakAssemblySerializationBinder { SafeMode = safeMode }
                };

                object? result = null;
                if (serializedData.Length > 0)
                {
                    result = serializer.Deserialize(serializedData);
                    if (result != ResXNullRef.Value && IsNullRef(result!.GetType().AssemblyQualifiedName))
                        result = ResXNullRef.Value;
                }

                return result;
            }

            // 4.) SoapFormatter. We do not reference it explicitly. If cannot be loaded, NotSupportedException will be thrown.
            if (!safeMode && mimeType.In(ResXCommon.SoapSerializedMimeTypes) && TryDeserializeBySoapFormatter(dataNodeInfo, out object? value))
                return value;

            Throw.NotSupportedException(Res.ResourcesMimeTypeNotSupported(mimeType, dataNodeInfo.Line, dataNodeInfo.Column));
            return null;
        }

        private object? CloneValue(ITypeResolutionService? typeResolver, string? basePath)
        {
            Debug.Assert(!(cachedValue is string || cachedValue is ResXNullRef), "String or null value should never be cloned.");

            // we have no value yet: deserializing from FileRef or .resx data
            if (cachedValue == null)
                return GetValue(typeResolver, basePath); // not cleaning up if cloning

            // special handling for memory stream: we avoid cloning the underlying array if possible
            if (cachedValue is MemoryStream ms)
                return new MemoryStream(ms.InternalGetBuffer() ?? ms.ToArray(), false);

            var formatter = new BinarySerializationFormatter(BinarySerializationOptions.RecursiveSerializationAsFallback | BinarySerializationOptions.CompactSerializationOfStructures | BinarySerializationOptions.IgnoreTypeForwardedFromAttribute);

            // we have a cached value but it hasn't been cloned yet: creating a raw data of it
            rawValue ??= formatter.Serialize(cachedValue);
            return cachedValue = formatter.Deserialize(rawValue);
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        [SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null!)
                Throw.ArgumentNullException(Argument.info);
            DataNodeInfo dataNodeInfo = GetDataNodeInfo(null, null);
            info.AddValue(ResXCommon.NameStr, dataNodeInfo.Name);
            info.AddValue(ResXCommon.CommentStr, dataNodeInfo.Comment);
            info.AddValue(ResXCommon.TypeStr, dataNodeInfo.TypeName);
            info.AddValue(ResXCommon.MimeTypeStr, dataNodeInfo.MimeType);
            info.AddValue(ResXCommon.ValueStr, dataNodeInfo.ValueData);
            info.AddValue(ResXCommon.AliasStr, dataNodeInfo.AssemblyAliasValue);
            info.AddValue(nameof(fileRefBasePath), fileRefBasePath);
            info.AddValue(nameof(dataNodeInfo.CompatibleFormat), dataNodeInfo.CompatibleFormat);
            // no fileRef is needed, it is retrieved from nodeInfo
        }

        #endregion

        #endregion

        #endregion
    }
}
