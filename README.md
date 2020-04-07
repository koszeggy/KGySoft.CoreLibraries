[![KGy SOFT .net](https://docs.kgysoft.net/corelibraries/icons/logo.png)](https://kgysoft.net)

# KGy SOFT Core Libraries

KGy SOFT Core Libraries offer high-performance and handy general libraries.
Many of them aim to replace existing APIs of the original .NET framework with more efficient versions.
Multiple versions of .NET Framework, .NET Core and .NET Standard are supported.

[![Website](https://img.shields.io/website/https/kgysoft.net/corelibraries.svg)](https://kgysoft.net/corelibraries)
[![Online Help](https://img.shields.io/website/https/docs.kgysoft.net/corelibraries.svg?label=online%20help&up_message=available)](https://docs.kgysoft.net/corelibraries)
[![GitHub Repo](https://img.shields.io/github/repo-size/koszeggy/KGySoft.CoreLibraries.svg?label=github)](https://github.com/koszeggy/KGySoft.CoreLibraries)
[![Nuget](https://img.shields.io/nuget/vpre/KGySoft.CoreLibraries.svg)](https://www.nuget.org/packages/KGySoft.CoreLibraries)
[![.NET Fiddle](https://img.shields.io/website/https/dotnetfiddle.net/Authors/84474/koszeggy.svg?label=.NET%20Fiddle)](https://dotnetfiddle.net/Authors/84474/koszeggy)

## Table of Contents:
1. [Download](#download)
   - [Download Binaries](#download-binaries)
   - [Download Demo App](#download-demo-app)
2. [Project Site](#project-site)
3. [Documentation](#documentation)
4. [Release Notes](#release-notes)
5. [Examples](#examples)
   - [Extensions Methods](#useful-extensions)
   - [Collections](#high-performance-collections)
   - [Reflection](#alternative-reflection-api)
   - [Binary Serialization](#binary-serialization)
   - [XML Serialization](#xml-serialization)
   - [Dynamic Resource Management](#dynamic-resource-management)
   - [Business Objects](#business-objects)
   - [Command Binding](#command-binding)
   - [Performance Measurement](#performance-measurement)
6. [License](#license)

## Download:

### Download Binaries:

The binaries can be downloaded as a NuGet package directly from [nuget.org](https://www.nuget.org/packages/KGySoft.CoreLibraries)

However, the preferred way is to install the package in VisualStudio either by looking for the `KGySoft.CoreLibraries` package in the Nuget Package Manager GUI, or by sending the following command at the Package Manager Console prompt:

    PM> Install-Package KGySoft.CoreLibraries

Alternatively, you can download the binaries as a .zip file attached to the [releases](https://github.com/koszeggy/KGySoft.CoreLibraries/releases).

### Download Demo App:

[KGySoft.ComponentModelDemo](https://github.com/koszeggy/KGySoft.ComponentModelDemo) is a desktop application, which focuses mainly on the features of the [KGySoft.ComponentModel](http://docs.kgysoft.net/corelibraries/?topic=html/N_KGySoft_ComponentModel.htm) namespace of KGy SOFT Core Libraries (see also the [business objects](#business-objects) and [command binding](#command-binding) examples below). Furthermore, it also provides some useful code samples for using the KGy SOFT Core Libraries in WPF and Windows Forms applications.

[![KGySoft.ComponentModelDemo](https://kgysoft.net/images/KGySoft.ComponentModelDemo.jpg)](https://github.com/koszeggy/KGySoft.ComponentModelDemo/releases)

> _Tip:_ Some simple console application live examples are also available at [.NET Fiddle](https://dotnetfiddle.net/Authors/84474/koszeggy).

## Project Site

Find the project site at [kgysoft.net](https://kgysoft.net/corelibraries/).

## Documentation

* [Online documentation](https://docs.kgysoft.net/corelibraries)
* [Offline .chm documentation](https://github.com/koszeggy/KGySoft.CoreLibraries/raw/master/KGySoft.CoreLibraries/Help/KGySoft.CoreLibraries.chm)

## Release Notes

See the [change log](https://github.com/koszeggy/KGySoft.CoreLibraries/blob/master/KGySoft.CoreLibraries/changelog.txt).

## Examples

### Useful Extensions:

- #### [`IDictionary<TKey, TValue>.GetValueOrDefault`](https://docs.kgysoft.net/corelibraries/?topic=html/Overload_KGySoft_CoreLibraries_DictionaryExtensions_GetValueOrDefault.htm) extension methods:

> _Tip:_ Try also [online](https://dotnetfiddle.net/GKSif4).
```cs
// old way:
object obj;
int intValue;
if (dict.TryGetValue("Int", out obj) && obj is int)
    intValue = (int)obj;

// C# 7.0 way:
if (dict.TryGetValue("Int", out object o) && o is int i)
    intValue = i;

// GetValueOrDefault ways:
intValue = (int)dict.GetValueOrDefault("Int");
intValue = dict.GetValueOrDefault("Int", 0);
intValue = dict.GetValueOrDefault<int>("Int");
```

- #### Collection range operations:

The [`AddRange`](https://docs.kgysoft.net/corelibraries/?topic=html/M_KGySoft_CoreLibraries_CollectionExtensions_AddRange__1.htm) extension method allows you to add multiple elements to any `ICollection<T>` instance. Similarly, [`InsertRange`](https://docs.kgysoft.net/corelibraries/?topic=html/M_KGySoft_CoreLibraries_ListExtensions_InsertRange__1.htm), [`RemoveRange`](https://docs.kgysoft.net/corelibraries/?topic=html/M_KGySoft_CoreLibraries_ListExtensions_RemoveRange__1.htm) and [`ReplaceRange`](https://docs.kgysoft.net/corelibraries/?topic=html/M_KGySoft_CoreLibraries_ListExtensions_ReplaceRange__1.htm) are available for `IList<T>` implementations. You might need to check the `ICollection<T>.IsReadOnly` property before using these methods.

- #### Manipulating `IEnumerable` and `IEnumerable<T>` types by `Try...` methods:

Depending on the actual implementation inserting/removing/setting elements in an `IEnumerable` type might be possible. See the `Try...` methods of the [`EnumerableExtensions`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_CoreLibraries_EnumerableExtensions.htm) class. All of these methods have a **Remarks** section in the documentation that precisely describes the conditions when the corresponding method can be used successfully.

- #### [`AsThreadSafe`](https://docs.kgysoft.net/corelibraries/?topic=html/M_KGySoft_CoreLibraries_CollectionExtensions_AsThreadSafe__1.htm) extension methods:

Starting with .NET 4 a sort of concurrent collections appeared. While they provide good scalability for multiple concurrent readers by using separate locks for entries or for a set of entries, in many situations they perform worse than a simple locking collection, especially if the collection to lock uses a fast accessible storage (eg. an array) internally. It also may worth to mention that some members (such as the `Count` property) are surprisingly expensive operations on most concurrent collections as they traverse the inner storage and in the meantime they lock all entries while counting the elements. So it always depends on the concrete scenario whether a simple locking collection or a concurrent collection is more beneficial to use.

Therefore, an `AsThreadSafe` method is available for the `ICollection<T>`, `IList<T>` and `IDictionary<TKey, TValue>` types, which simply wrap the collection into a [`LockingCollection<T>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Collections_LockingCollection_1.htm), [`LockingList<T>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Collections_LockingCollection_1.htm) or [`LockingDictionary<TKey, TValue>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Collections_LockingDictionary_2.htm) instance, respectively.

> _Note:_ Using a locking collection does not solve all concurrent issues magically. See the **Remarks** section in the descriptions of these classes for more details.

- #### [`Object.Convert<T>`](https://docs.kgysoft.net/corelibraries/?topic=html/Overload_KGySoft_CoreLibraries_ObjectExtensions_Convert.htm) extension method:

> _Tip:_ Try also [online](https://dotnetfiddle.net/rzg8If).
```cs
// between convertible types: like the Convert class but supports also enums in both ways
result = "123".Convert<int>(); // culture can be specified, default is InvariantCulture
result = ConsoleColor.Blue.Convert<float>();
result = 13.Convert<ConsoleColor>(); // this would fail by Convert.ChangeType
        
// TypeConverters are used if possible:
result = "AADC78003DAB4906826EFD8B2D5CF33D".Convert<Guid>();
        
// New conversions can be registered:
result = 42L.Convert<IntPtr>(); // fail
typeof(long).RegisterConversion(typeof(IntPtr), (obj, type, culture) => new IntPtr((long)obj));
result = 42L.Convert<IntPtr>(); // success
        
// Registered conversions can be used as intermediate steps:
result = 'x'.Convert<IntPtr>(); // char => long => IntPtr
        
// Collection conversion is also supported:
result = new List<int> { 1, 0, 0, 1 }.Convert<bool[]>();
result = "Blah".Convert<List<int>>(); // works because string is an IEnumerable<char>
result = new[] { 'h', 'e', 'l', 'l', 'o' }.Convert<string>(); // because string has a char[] constructor
result = new[] { 1.0m, 2, -1 }.Convert<ReadOnlyCollection<string>>(); // via the IList<T> constructor
        
// even between non-generic collections:
result = new HashSet<int> { 1, 2, 3 }.Convert<ArrayList>();
result = new Hashtable { { 1, "One" }, { "Black", 'x' } }.Convert<Dictionary<ConsoleColor, string>>();
```

- #### [`Object.In`](https://docs.kgysoft.net/corelibraries/?topic=html/Overload_KGySoft_CoreLibraries_ObjectExtensions_In.htm) extension method:

```cs
// old way:
if (stringValue == "something" || stringValue == "something else" || stringValue == "maybe some other value" || stringValue == "or...")
    DoSomething();

// In method:
if (stringValue.In("something", "something else", "maybe some other value", "or..."))
    DoSomething();
```

- #### [`Random` extensions methods](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_CoreLibraries_RandomExtensions.htm):

> _Tip:_ Try also [online](https://dotnetfiddle.net/EPHRIx).

```cs
var rnd = new Random();

// Next... for all simple types:
rnd.NextBoolean();
rnd.NextDouble(Double.PositiveInfinity); // see also the overloads
rnd.NextString(); // see also the overloads
rnd.NextDateTime(); // also NextDate, NextDateTimeOffset, NextTimeSpan
rnd.NextEnum<ConsoleColor>();
// and NextByte, NextSByte, NextInt16, NextDecimal, etc.

// NextObject: for practically anything. See also GenerateObjectSettings.
rnd.NextObject<Person>().Dump(); // custom type
rnd.NextObject<(int, string)>(); // tuple
rnd.NextObject<IConvertible>(); // interface implementation
rnd.NextObject<MarshalByRefObject>(); // abstract type implementation
rnd.NextObject<int[]>(); // array
rnd.NextObject<IList<IConvertible>>(); // some collection of an interface
rnd.NextObject<Func<DateTime>>(); // delegate with random result

// specific type for object (useful for non-generic collections)
rnd.NextObject<ArrayList>(new GenerateObjectSettings { SubstitutionForObjectType = typeof(ConsoleColor) };

// literally any random object
rnd.NextObject<object>(new GenerateObjectSettings { AllowDerivedTypesForNonSealedClasses = true });
```

> _Tip:_ Find more extensions in the [online documentation](https://docs.kgysoft.net/corelibraries/?topic=html/N_KGySoft_CoreLibraries.htm).

### High Performance Collections:

- #### [`Cache<TKey, TValue>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Collections_Cache_2.htm):

A `Dictionary`-like type with a specified capacity. If the cache is full and new items have to be stored, then the oldest element (or the least recent used one, depending on `Behavior`) is dropped from the cache.

If an item loader is passed to the constructor, then it is enough only to read the cache via the indexer and the corresponding item will be transparently loaded when necessary.

> _Tip:_ Try also [online](https://dotnetfiddle.net/wTqCoa).

```cs
// instantiating the cache by a loader method and a capacity of 1000 possible items
var personCache = new Cache<int, Person>(LoadPersonById, 1000);

// you only need to read the cache:
var person = personCache[id];

// If a cache instance is accessed from multiple threads use it from a thread safe accessor.
// The item loader can be protected from being called concurrently.
// Similarly to ConcurrentDictionary, this is false by default.
var threadSafeCache = personCache.GetThreadSafeAccessor(protectItemLoader: false);

person = threadSafeCache[id];
```

- #### [`CircularList<T>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Collections_CircularList_1.htm):

Fully compatible with `List<T>` but maintains a dynamic start/end position of the stored elements internally, which makes it very fast when elements are added/removed at the first position. It has also optimized range operations and can return both value type and reference type enumerators depending on the used context.

```cs
var clist = new CircularList<int>(Enumerable.Range(0, 1000));

// or by ToCircularList:
clist = Enumerable.Range(0, 1000).ToCircularList();

// AddFirst/AddLast/RemoveFirst/RemoveLast
clist.AddFirst(-1); // same as clist.Insert(0, -1); (much faster than List<T>)
clist.RemoveFirst(); // same as clist.RemoveAt(0); (much faster than List<T>)

// if the inserted collection is not ICollection<T>, then List<T> is especially slow here
// because it inserts the items one by one and shifts the elements in every iteration
clist.InsertRange(0, Enumerable.Range(-500, 500));

// When enumerated by LINQ expressions, List<T> is not so effective because of its boxed
// value type enumerator. In these cases CircularList returns a reference type enumerator.
Console.WriteLine(clist.SkipWhile(i => i < 0).Count());
```

- #### [`ObservableBindingList<T>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_ObservableBindingList_1.htm):

Combines the features of `IBindingList` implementations (such as `BindingList<T>`) and `INotifyCollectionChanged` implementations (such as `ObservableCollection<T>`). It makes it an ideal collection type in many cases (such as in a technology-agnostic View-Model layer) because it can used in practically any UI environments. By default it is initialized by a [`SortableBindingList<T>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_SortableBindingList_1.htm) but can wrap any `IList<T>` implementation.

> _Tip:_ See more collections in the [`KGySoft.Collections`](https://docs.kgysoft.net/corelibraries/?topic=html/N_KGySoft_Collections.htm), [`KGySoft.Collections.ObjectModel`](https://docs.kgysoft.net/corelibraries/?topic=html/N_KGySoft_Collections_ObjectModel.htm) and [`KGySoft.ComponentModel`](https://docs.kgysoft.net/corelibraries/?topic=html/N_KGySoft_ComponentModel.htm) namespaces.

### Alternative Reflection API

- #### Accessors - the performant way:

There are four public classes derived from [`MemberAccessor`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Reflection_MemberAccessor.htm), which can be used where you would use `MemberInfo` instances. The following table summarizes the relations among them:

| System Type                    | KGy SOFT Type                 |
|--------------------------------|-------------------------------|
| `FieldInfo`                    | [`FieldAccessor`][fa]         |
| `PropertyInfo`                 | [`PropertyAccessor`][pa]      |
| `MethodInfo`                   | [`MethodAccessor`][ma]        |
| `ConstructorInfo`, `Activator` | [`CreateIstanceAccessor`][ca] |

[fa]: https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Reflection_FieldAccessor.htm
[pa]: https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Reflection_PropertyAccessor.htm
[ma]: https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Reflection_MethodAccessor.htm
[ca]: https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Reflection_CreateInstanceAccessor.htm

> _Tip:_ See the links in the table above for performance comparison examples.

- #### [`Reflector`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Reflection_Reflector.htm) class - the convenient way:

If convenience is priority, then the [`Reflector`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Reflection_Reflector.htm) class offers every functionality you need to use for reflection. While the accessors above can to be obtained by a `MemberInfo` instance, the `Reflector` can be used even by name. The following example demonstrates this for methods:

```cs
// Any method by MethodInfo:
MethodInfo method = typeof(MyType).GetMethod("MyMethod");

result = Reflector.InvokeMethod(instance, method, param1, param2); // by Reflector
result = method.Invoke(instance, new object[] { param1, param2 }); // the old (slow) way
result = MethodAccessor.GetAccessor(method).Invoke(instance, param1, param2); // by accessor (fast)

// Instance method by name (can be non-public, even in base classes):
result = Reflector.InvokeMethod(instance, "MethodName", param1, param2);

// Static method by name (can be non-public, even in base classes):
result = Reflector.InvokeMethod(typeof(MyType), "MethodName", param1, param2);

// Even generic methods are supported:
result = Reflector.InvokeMethod(instance, "MethodName", new[] { typeof(GenericArg) }, param1, param2);

// If you are not sure whether a method by the specified name exists use TryInvokeMethod:
bool invoked = Reflector.TryInvokeMethod(instance, "MethodMaybeExists", out result, param1, param2);
```

> _Note:_ `Try...` methods return false if a matching member with the given name/parameters cannot be found. However, if a member could be successfully invoked, which threw an exception, then this exception will be thrown further.

### Serialization

- #### Binary Serialization

[`BinarySerializationFormatter`][bsf] is functionally compatible with `BinaryFormatter` but in most cases produces much compact serialized data with a better performance. Basically it has the same nature as `BinaryFormatter`: apart from some natively supported types it is based on field serialization by default and is sensitive to assembly version. While this can be desired in some cases such as saving/restoring bitwise state or [deep cloning](https://docs.kgysoft.net/corelibraries/?topic=html/M_KGySoft_CoreLibraries_ObjectExtensions_DeepClone__1.htm), for serializing public members of data contracts and components it is suggested to use the text based `XmlSerializer` instead.

Binary serialization functions are available via the static [`BinarySerializer`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Serialization_Binary_BinarySerializer.htm) class and by the [`BinarySerializationFormatter`][bsf] type.

> _Tip:_ Try also [online](https://dotnetfiddle.net/T7BUyB).

```cs
// Simple way: by the static BinarySerializer class
byte[] rawData = BinarySerializer.Serialize(instance); // to byte[]
BinarySerializer.SerializeToStream(stream, instance); // to Stream
BinarySerializer.SerializeByWriter(writer, instance); // by BinaryWriter

// or explicitly by a BinarySerializationFormatter instance:
rawData = new BinarySerializationFormatter().Serialize(instance);

// supports even non-serializable types (default options actually contain this flag):
data = BinarySerializer.Serialize(instance, BinarySerializationOptions.RecursiveSerializationAsFallback);

// Deserialization:
obj = (MyClass)BinarySerializer.Deserialize(rawData); // from byte[]
obj = (MyClass)BinarySerializer.DeserializeFromStream(stream); // from Stream
obj = (MyClass)BinarySerializer.DeserializeByReader(reader); // by BinaryReader
```

Sensitivity of binary serializers to assembly version can be considered either a useful security protection or an annoying thing. The [`BinarySerializationFormatter`][bsf] supports many types and collections natively (see the link), which has two benefits: these types are serialized without any assembly information and the result is very compact as well. Additionally, you can use the `BinarySerializationOptions.OmitAssemblyQualifiedNames` flag to omit assembly information on serialization.

In fact, KGy SOFT Core Library contains also a [`WeakAssemblySerializationBinder`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Serialization_Binary_WeakAssemblySerializationBinder.htm) class, which can be used with any `IFormatter` serializers (even with `BinaryFormatter`) to allow deserialization from different assemblies:

```cs
IFormatter formatter = new BinarySerializationFormatter();
formatter.Binder = new WeakAssemblySerializationBinder(); // works also for BinaryFormatter!

result = (MyClass)formatter.Deserialize(streamSerializedByAnOldAssembly);
```

> _Solving compatibility issues between different platforms:_ In .NET Core there are many types that used to be serializable in .NET Framework but the `[Serializable]` attribute is not applied to them in .NET Core/Standard. Though the binary serialization of such types is not recommended anymore, their support could be required for compatibility reasons. In this case the [`CustomSerializerSurrogateSelector`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Serialization_Binary_CustomSerializerSurrogateSelector.htm) can be a solution, which can be used both with `BinaryFormatter` and [`BinarySerializationFormatter`][bsf]. See the **Remarks** section of the [`CustomSerializerSurrogateSelector`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Serialization_Binary_CustomSerializerSurrogateSelector.htm) class for various use cases and their solutions.

[bsf]: https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Serialization_Binary_BinarySerializationFormatter.htm

- #### XML Serialization

> _Note:_ KGy SOFT's [`XmlSerializer`][xml] does not attempt to replace the system `XmlSerializer`. The latter can be used (more or less) to produce a customized XML, whereas the KGy SOFT version focuses to be able to serialize any object (without any customization though). There are many cases where the system version cannot be used. For more details see the **Remarks** section in the description of the [`XmlSerializer`][xml] class.

Unlike binary serialization, which is meant to save the bitwise content of an object, the [`XmlSerializer`][xml] can save and restore the public properties and fields. Meaning, it cannot guarantee that the original state of an object can be fully restored unless it is completely exposed by public members. The [`XmlSerializer`][xml] can be a good choice for saving configurations or components whose state can be edited in a property grid, for example.

Therefore [`XmlSerializer`][xml] supports several `System.ComponentModel` attributes and techniques such as `TypeConverterAttribute`, `DefaultValueAttribute`, `DesignerSerializationVisibilityAttribute` and even the `ShouldSerialize...` methods.

```cs
// A good candidate for XML serialization:
public class Person
{
    public string FirstName { get; set; }

    [DefaultValue(null)] // will not be serialized if null
    public string MiddleName { get; set; }

    public string LastName { get; set; }

    public DateTime BirthDate { get; set; }

    // System serializer fails here: the property has no setter and its type cannot be instantiated.
    public IList<string> PhoneNumbers { get; } = new Collection<string>();
}
```

And the serialization:

> _Tip:_ Try also [online](https://dotnetfiddle.net/M2dfrx).

```cs
var person = ThreadSafeRandom.Instance.NextObject<Person>();
var options = XmlSerializationOptions.RecursiveSerializationAsFallback;

// serializing into XElement
XElement element = XmlSerializer.Serialize(person, options);
var clone = (Person)XmlSerializer.Deserialize(element);

// serializing into file/Stream/TextWriter/XmlWriter are also supported: An XmlWriter will be used
var sb = new StringBuilder();
XmlSerializer.Serialize(new StringWriter(sb), person, options);
clone = (Person)XmlSerializer.Deserialize(new StringReader(sb.ToString()));

Console.WriteLine(sb);
```

If a type has a non-default constructor it still can be deserialized after manually
creating an empty instance:

```cs
public class MyComponent
{
    // there is no default constructor
    public MyComponent(Guid id) => Id = id;

    // read-only property: will not be serialized unless forced by the
    // ForcedSerializationOfReadOnlyMembersAndCollections option
    public Guid Id { get; }

    // this tells the serializer to allow recursive serialization for this non-common type
    // without using the RecursiveSerializationAsFallback option
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public Person Person { get; set; }
}
```

When serializing such a type we need to emit a root element explicitly and on deserialization we need to create an empty `MyComponent` instance manually:

```cs
var instance = new MyComponent(Guid.NewGuid()) { Person = person };

// serialization (now into XElement but XmlWriter is also supported):
var root = new XElement("SomeRootElement");
XmlSerializer.SerializeContent(root, instance);

// deserialization (now from XElement but XmlReader is also supported):
var cloneWithNewId = new MyComponent(Guid.NewGuid());
XmlSerializer.DeserializeContent(root, cloneWithNewId);
```

[xml]: https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Serialization_Xml_XmlSerializer.htm

### Dynamic Resource Management

The KGy SOFT Core Libraries contain numerous classes for working with resources directly from .resx files. Some classes can be familiar from the .NET Framework. For example, [`ResXResourceReader`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Resources_ResXResourceReader.htm), [`ResXResourceWriter`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Resources_ResXResourceWriter.htm) and [`ResXResourceSet`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Resources_ResXResourceSet.htm) are reimplemented by referencing only the core system assemblies (the original versions of these reside in `System.Windows.Forms.dll`, which cannot be used in all circumstances) and they got a bunch of improvements at the same time. For example, `ResXResourceSet` is now a read-write collection and the changes can be saved in a new .resx file (see the links above for details and comparisons and examples).

On top of those, KGy SOFT Core Libraries introduce a sort of new types that can be used the same way as a standard `ResourceManager` class:
- [`ResXResourceManager`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Resources_ResXResourceManager.htm) works the same way as the regular `ResourceManager` but works on .resx files instead of compiled resources and supports adding and saving new resources, .resx metadata and assembly aliases.
- The [`HybridResourceManager`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Resources_HybridResourceManager.htm) is able to work both with compiled and .resx resources even at the same time: it can be used to override the compiled resources with .resx content.
- The [`DynamicResourceManager`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Resources_DynamicResourceManager.htm) can be used to generate new .resx files automatically for languages without a localization. The KGy SOFT Libraries also use `DynamicResourceManager` instances to maintain their resources. The library assemblies are compiled only with the English resources but any consumer library or application can enable the .resx expansion for any language.

> _Tip:_ See the **Remarks** section of the [`KGySoft.Resources`](https://docs.kgysoft.net/corelibraries/?topic=html/N_KGySoft_Resources.htm) namespace description, which may help you to choose the most appropriate class for your needs.

- #### Enabling the localization of the Core Libraries resources:

```cs
// Just pick a language for your application
LanguageSettings.DisplayLanguage = CultureInfo.GetCultureInfo("de-DE");

// Opt-in using .resx files (for all `DynamicResourceManager` instances, which are configured to obtain
// their configuration from LanguageSettings):
LanguageSettings.DynamicResourceManagersSource = ResourceManagerSources.CompiledAndResX;

// When you access a resource for the first time for a new language, a new resource set will be generated.
// This is saved automatically when you exit the application
Console.WriteLine(PublicResources.ArgumentNull);
```

The example above will print a prefixed English message for the first time: `[T]Value cannot be null.`. Find the newly saved .resx file and look for the untranslated resources with the `[T]` prefix. After saving an edited resource file the example will print the localized message.

> See a complete example at the [`LanguageSettins`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_LanguageSettings.htm) class.

- #### Create dynamically localizable resources for an application or class library:

See the [step-by step description](https://docs.kgysoft.net/corelibraries/html/T_KGySoft_Resources_DynamicResourceManager.htm#recommendation) at the [`DynamicResourceManager`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Resources_DynamicResourceManager.htm) class.

### Business Objects

The [`KGySoft.ComponentModel`](https://docs.kgysoft.net/corelibraries/?topic=html/N_KGySoft_ComponentModel.htm) namespace contains several types that can be used as base type for model classes, view-model objects or other kind of business objects:

![Base classes for business objects](https://docs.kgysoft.net/corelibraries/Help/Images/ComponentModel_BusinessObjects.png)

- [`ObservableObjectBase`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_ObservableObjectBase.htm): The simplest class, supports change notification via the `INotifyPropertyChanged` interface and can tell whether any of the properties have been modified. Provides protected members for maintaining properties.
- [`PersistableObjectBase`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_PersistableObjectBase.htm): Extends the `ObservableObjectBase` class by implementing the [`IPersistableObject`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_IPersistableObject.htm) interface, which makes possible to access and manipulate the internal property storage.
- [`UndoableObjectBase`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_UndoableObjectBase.htm): Adds step-by-step undo/redo functionality to the `PersistableObjectBase` type. This is achieved by implementing a flexible [`ICanUndoRedo`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_ICanUndoRedo.htm) interface. Implements also the standard `System.ComponentModel.IRevertibleChangeTracking` interface.
- [`EditableObjectBase`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_EditableObjectBase.htm): Adds committable and revertible editing functionality to the `PersistableObjectBase` type. The editing sessions can be nested. This is achieved by implementing a flexible [`ICanEdit`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_ICanEdit.htm) interface but implements also the standard `System.ComponentModel.IEditableObject` interface, which is already supported by multiple already existing controls in the various graphical user environments.
- [`ValidatingObjectBase`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_ValidatingObjectBase.htm): Adds business validation features to the `PersistableObjectBase` type. This is achieved by implementing a flexible [`IValidatingObject`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_IValidatingObject.htm) interface, which provides multiple validation levels for each properties. Implements also the standard `System.ComponentModel.IDataErrorInfo` interface, which is the oldest and thus the most widely supported standard validation technique in the various GUI frameworks.
- [`ModelBase`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_ModelBase.htm): Unifies the features of all of the classes above.

The following example demonstrates a possible model class with validation:

```cs
public class MyModel : ValidatingObjectBase
{
    // A simple integer property (with zero default value).
    // Until the property is set no value is stored internally.
    public int IntProperty { get => Get<int>(); set => Set(value); }

    // An int property with default value. Until the property is set the default will be returned.
    public int IntPropertyCustomDefault { get => Get(-1); set => Set(value); }

    // If the default value is a complex one, which should not be evaluated each time
    // you can provide a factory for it.
    // When this property is read for the first time without setting it before
    // the provided delegate will be invoked and the returned default value is stored without triggering
    // the PropertyChanged event.
    public MyComplexType ComplexProperty { get => Get(() => new MyComplexType()); set => Set(value); }

    // You can use regular properties to prevent raising the events
    // and not to store the value in the internal storage.
    // The OnPropertyChanged method still can be called explicitly to raise the PropertyChanged event.
    public int UntrackedProperty { get; set; }

    public int Id { get => Get<int>(); set => Set(value); }
    public string Name { get => Get<string>(); set => Set(value); }

    protected override ValidationResultsCollection DoValidation()
    {
        var result = new ValidationResultsCollection();

        // info
        if (Id == 0)
            result.AddInfo(nameof(Id), "This will be considered as a new object when saved");

        // warning
        if (Id < 0)
            result.AddWarning(nameof(Id), $"{nameof(Id)} is recommended to be greater or equal to 0.");

        // error
        if (String.IsNullOrEmpty(Name))
            result.AddError(nameof(Name), $"{nameof(Name)} must not be null or empty.");

        return result;
    }
}
```

### Command Binding

KGy SOFT Core Libraries contain a simple, technology-agnostic implementation of the Command pattern. Commands are actually event handlers with a static logic on possibly dynamic parameters.

A command is represented by the [`ICommand`][ICommand] interface (see some examples also in the link). There are four pairs of predefined `ICommand` implementations that can accept delegate handlers: [`SimpleCommand`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_SimpleCommand.htm)/[`SimpleCommand<TParam>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_SimpleCommand_1.htm), [`TargetedCommand<TTarget>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_TargetedCommand_1.htm)/[`TargetedCommand<TTarget, TParam>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_TargetedCommand_2.htm), [`SourceAwareCommand<TEventArgs>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_SourceAwareCommand_1.htm)/[`SourceAwareCommand<TEventArgs, TParam>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_SourceAwareCommand_2.htm) and [`SourceAwareTargetedCommand<TEventArgs, TTarget>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_SourceAwareTargetedCommand_2.htm)/[`SourceAwareTargetedCommand<TEventArgs, TTarget, TParam>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_SourceAwareTargetedCommand_3.htm).

- #### [`ICommand`][ICommand] and [`ICommandBinding`][ICommandBinding]:

```cs
public static class MyCommands
{
    public static readonly ICommand PasteCommand = new TargetedCommand<TextBoxBase>(tb => tb.Paste());

    public static readonly ICommand ReplaceTextCommand = new TargetedCommand<Control, string>((target, value) => target.Text = value);
}
```

To use a command it has to be bound to one or more sources (and to some targets if the command is targeted). To create a binding the `CreateBinding` extension method can be used:

> _Tip:_ Try also [online](https://dotnetfiddle.net/7b0lFq).

```cs
var binding = MyCommands.PasteCommand.CreateBinding(menuItemPaste, "Click", textBox);

// Alternative way by fluent syntax: (also allows to add multiple sources)
binding = MyCommands.PasteCommand.CreateBinding()
    .AddSource(menuItemPaste, nameof(menuItemPaste.Click))
    .AddSource(buttonPaste, nameof(buttonPaste.Click))
    .AddTarget(textBox);

// by disposing the binding every event subscription will be removed
binding.Dispose();
```

- #### [`CommandBindingsCollection`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_CommandBindingsCollection.htm):

If you create your bindings by a `CommandBindingsCollection` (or add the created bindings to it), then all of the event subscriptions of every added binding can be removed at once when the collection is disposed.

```cs
public class MyView : ViewBase
{
    private CommandBindingsCollection bindings = new CommandBindingsCollection();

    private void InitializeView()
    {
        bindings.Add(MyCommands.PasteCommand)
            .AddSource(menuItemPaste, nameof(menuItemPaste.Click))
            .AddSource(buttonPaste, nameof(buttonPaste.Click))
            .AddTarget(textBox);
        // [...] more bindings
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            bindings.Dispose(); // releases all of the event subscriptions of every added binding
        base.Dispose(disposing);
    }
}
```

- #### [`ICommandState`][ICommandState]:

An [`ICommand`][ICommand] instance is stateless by itself. However, the created [`ICommandBinding`][ICommandBinding] has a `State` property, which is an [`ICommandState`][ICommandState] instance containing any arbitrary dynamic properties of the binding. Actually you can treat this object as a `dynamic` instance and add any properties you want. It has one predefined property, `Enabled`, which can be used to enable or disable the execution of the command.

```cs
// The command state can be pre-created and passed to the binding creation
var pasteCommandState = new CommandState { Enabled = false };

// passing the state object when creating the binding
var pasteBinding = bindings.Add(MyCommands.PasteCommand, pasteCommandState)
    .AddSource(menuItemPaste, nameof(menuItemPaste.Click))
    .AddSource(buttonPaste, nameof(buttonPaste.Click))
    .AddTarget(textBox);

// ...

// enabling the command
pasteCommandState.Enabled = true;
// or:
pasteBinding.State.Enabled = true;
```

- #### Command Enabled State - poll vs. push ways:

As you could see in the previous example the `Enabled` state of the command can set explicitly (push) any time via the [`ICommandState`][ICommandState] object.

On the other hand, it is possible to subscribe the `ICommandBinding.Executing` event, which is raised when a command is about to be executed. By this event the binding instance checks the enabled status (poll) and allows the subscriber to change it.

```cs
// Handling the ICommandBinding.Executing event. Of course, it can be a command, too... how fancy :)

public static class MyCommands
{
    // [...]

    // this time we define a SourceAwareCommand because we want to get the event data
    public static readonly ICommand SetPasteEnabledCommand =
        new SourceAwareCommand<ExecuteCommandEventArgs>(OnSetPasteEnabledCommand);

    private static void OnSetPasteEnabledCommand(ICommandSource<ExecuteCommandEventArgs> sourceData)
    {
        // we set the enabled state based on the clipboard
        sourceData.EventArgs.State.Enabled = Clipboard.ContainsText();
    }
}

// ...

// and the creation of the bindings:

// the same as previously, except that we don't pass a pre-created state this time.
var pasteBinding = bindings.Add(MyCommands.PasteCommand)
    .AddSource(menuItemPaste, nameof(menuItemPaste.Click))
    .AddSource(buttonPaste, nameof(buttonPaste.Click))
    .AddTarget(textBox);

// A command binding to set the Enabled state of the Paste command on demand (poll). No targets this time.
bindings.Add(MyCommands.SetPasteEnabledCommand)
    .AddSource(pasteBinding, nameof(pasteBinding.Executing));
```

The possible drawback of the polling way is that `Enabled` is set only in the moment when the command is executed. But if the sources can represent the disabled state (eg. visual elements may turn gray), then the explicit way may provide a better visual feedback. Just go on with reading...

- #### [ICommandStateUpdater](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_ICommandStateUpdater.htm):

An [`ICommandState`][ICommandState] can store not just the predefined `Enabled` state but also any other data. If these states can be rendered meaningfully by the command sources (for example, when `Enabled` is false, then a source button or menu item can be disabled), then an `ICommandStateUpdater` can be used to apply the states to the sources. If the states are properties on the source, then the [`PropertyCommandStateUpdater`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_PropertyCommandStateUpdater.htm) can be added to the binding:

```cs
// we can pass a string-object dictionary to the constructor, or we can treat it as a dynamic object.
var pasteCommandState = new CommandState(new Dictionary<string, object>
{
    { "Enabled", false }, // can be set also this way - must have a bool value
    { "Text", "Paste" },
    { "HotKey", Key.Control | Key.V },
    { "Icon", Icons.PasteIcon },
});

// as now we add a state updater, the states will be immediately applied to the sources
bindings.Add(MyCommands.PasteCommand, pasteCommandState)
    .AddStateUpdater(PropertyCommandStateUpdater.Updater) // to sync back state properties to sources
    .AddSource(menuItemPaste, nameof(menuItemPaste.Click))
    .AddSource(buttonPaste, nameof(buttonPaste.Click))
    .AddTarget(textBox);

// This will enable all sources now (if they have an Enabled property):
pasteCommandState.Enabled = true;

// We can set anything by casting the state to dynamic or via the AsDynamic property.
// It is not a problem if a source does not have such a property. You can chain multiple updaters to
// handle special cases. If an updater fails, the next one is tried (if any).
pasteCommandState.AsDynamic.ToolTip = "Paste text from the Clipboard";
```

- #### Parameterized Commands:

In WPF you can pass a parameter to a command, whose value is determined when the command is executed. KGy SOFT Libraries also have parameterized command support:

```cs
bindings.Add(MyCommands.ReplaceTextCommand)
    .AddSource(menuItemPaste, nameof(menuItemPaste.Click))
    .AddSource(buttonPaste, nameof(buttonPaste.Click))
    .AddTarget(textBox)
    .WithParameter(() => GetNewText()); // the delegate will be called when the command is executed
```

- #### Command Targets vs. Parameter:

Actually also the `AddTarget` method can accept a delegate, which is invoked just before executing the command.  The difference between targets and parameters is that whenever triggering the command the parameter value is evaluated only once but the `ICommand.Execute` method is invoked as many times as many targets are added to the binding (but at least once if there are no targets) using the same parameter value.

But if there are no multiple targets, then either a target or a parameter can be used interchangeably. Use whatever is more correct semantically. If the parameter/target can be determined when creating the binding (no callback is needed to determine its value), then it is probably rather a target than a parameter.

- #### Property Binding:

Most UI frameworks have some advanced property binding, supporting fancy things such as collections and paths. Though they can be perfectly used in most cases they can have also some drawbacks. For example, WPF data binding (similarly to other XAML based frameworks) can be used with `DependencyProperty` targets of `DependencyObject` instances only; and Windows Forms data binding works only for `IBindableComponent` implementations.

For environments without any binding support or for the aforementioned exceptional cases KGy SOFT's command binding offers a very simple one-way property binding by an internally predefined command exposed by the [`Command.CreatePropertyBinding`](https://docs.kgysoft.net/corelibraries/?topic=html/Overload_KGySoft_ComponentModel_Command_CreatePropertyBinding.htm) and [`CommandBindingsCollection.AddPropertyBinding`](https://docs.kgysoft.net/corelibraries/?topic=html/Overload_KGySoft_ComponentModel_CommandBindingsCollection_AddPropertyBinding.htm) methods. The binding works for any sources, which implement the `INotifyPropertyChanged` interface, or, if they have a `<PropertyName>Changed` event for the property to bind. The target object can be anything as long as the target property can be set.

In the following example our view-model is a [`ModelBase`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_ModelBase.htm) (see also [above](#business-objects)), which implements `INotifyPropertyChanged`.

```cs
// ViewModel:
public class MyViewModel : ModelBase // ModelBase implements INotifyPropertyChanged
{
    public string Text { get => Get<string>(); set => Set(value); }
}

// View: assuming we have a ViewBase<TDataContext> class with DataContext and CommandBindings properties
public class MyView : ViewBase<MyViewModel>
{
    private void InitializeView()
    {
        CommandBindingsCollection bindings = base.CommandBindings;
        MyViewModel viewModel = base.DataContext;

        // [...] the usual bindings.Add(...) lines here

        // Adding a simple property binding (uses a predefined command internally):
        bindings.AddPropertyBinding(
            viewModel, "Text", // source object and property name
            "Text", textBox, labelTextBox); // target property name and target object(s)

        // a formatting can be added if types (or just the values) of the properties should be different:
        bindings.AddPropertyBinding(
            viewModel, "Text", // source object and property name
            "BackColor", // target property name
            value => value == null ? Colors.Yellow : SystemColors.WindowColor, // string -> Color conversion
            textBox); // target object(s)
    }
}
```

[ICommand]: https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_ICommand.htm
[ICommandBinding]: https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_ICommandBinding.htm
[ICommandState]: https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_ComponentModel_ICommandState.htm

### Performance Measurement

KGy SOFT CoreLibraries offers two ways for performance measurement, which can be found in the [`KGySoft.Diagnostics`](https://docs.kgysoft.net/corelibraries/?topic=html/N_KGySoft_Diagnostics.htm) namespace.

- #### The [`Profiler`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Diagnostics_Profiler.htm) class:

You can use the `Profiler` class to inject measurement sections as `using` blocks into your code base:

> _Tip:_ Try also [online](https://dotnetfiddle.net/4nTM98).
```cs
const string category = "Example";

using (Profiler.Measure(category, "DoBigTask"))
{
    // ... code ...

	// measurement blocks can be nested
    using (Profiler.Measure(category, "DoSmallTask"))
    {
        // ... more code ...
    }
}
```

The number of hits, execution times (first, total, average) are tracked and can be obtained explicitly or you can let them to be dumped automatically into an .xml file.

```xml
<?xml version="1.0" encoding="utf-8"?>
<ProfilerResult>
  <item Category = "Example" Operation="Main total" NumberOfCalls="1" FirstCall="00:00:00.5500736" TotalTime="00:00:00.5500736" AverageCallTime="00:00:00.5500736" />
  <item Category = "Example" Operation="Main/1 iteration" NumberOfCalls="10" FirstCall="00:00:00.0555439" TotalTime="00:00:00.5500554" AverageCallTime="00:00:00.0550055" />
  <item Category = "Example" Operation="DoSmallTask" NumberOfCalls="60" FirstCall="00:00:00.0005378" TotalTime="00:00:00.0124114" AverageCallTime="00:00:00.0002068" />
  <item Category = "Example" Operation="DoBigTask" NumberOfCalls="10" FirstCall="00:00:00.0546513" TotalTime="00:00:00.5455339" AverageCallTime="00:00:00.0545533" />
</ProfilerResult>
```

The result .xml can be imported easily into Microsoft Excel:

![Profiler result](https://docs.kgysoft.net/corelibraries/Help/Images/ProfilerResults.png)

- #### `PerformanceTest` classes:

For more direct operations you can use the [`PerformanceTest`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Diagnostics_PerformanceTest.htm) and [`PerformanceTest<TResult>`](https://docs.kgysoft.net/corelibraries/?topic=html/T_KGySoft_Diagnostics_PerformanceTest_1.htm) classes to measure operations with `void` and non-`void` return values, respectively.

> _Tip:_ Try also [online](https://dotnetfiddle.net/PCcVuD).
```cs
new PerformanceTest
    {
        TestName = "System.Enum vs. KGySoft.CoreLibraries.Enum<TEnum>",
        Iterations = 1_000_000,
        Repeat = 2
    }
    .AddCase(() => ConsoleColor.Black.ToString(), "Enum.ToString")
    .AddCase(() => Enum<ConsoleColor>.ToString(ConsoleColor.Black), "Enum<TEnum>.ToString")
    .DoTest()
    .DumpResults(Console.Out);
```

The result of the `DoTest` method can be processed either manually or can be dumped in any `TextWriter`. The example above dumps it on the console and produces a result similar to this one:

```
==[System.Enum vs. KGySoft.CoreLibraries.Enum<TEnum> Results]================================================
Iterations: 1 000 000
Warming up: Yes
Test cases: 2
Repeats: 2
Calling GC.Collect: Yes
Forced CPU Affinity: 2
Cases are sorted by time (quickest first)
--------------------------------------------------
1. Enum<TEnum>.ToString: average time: 26,60 ms
  #1          29,40 ms   <---- Worst
  #2          23,80 ms   <---- Best
  Worst-Best difference: 5,60 ms (23,55%)
2. Enum.ToString: average time: 460,78 ms (+434,18 ms / 1 732,36%)
  #1         456,18 ms   <---- Best
  #2         465,37 ms   <---- Worst
  Worst-Best difference: 9,19 ms (2,01%)
```

If you need to use parameterized tests you can simply derive the `PerformanceTestBase<TDelegate, TResult>` class. Override the `OnBeforeCase` method to reset the parameter for each test cases. For example, this is how you can use a prepared `Random` instance in a performance test:

> _Tip:_ Try also [online](https://dotnetfiddle.net/GSeBq6).

```cs
public class RandomizedPerformanceTest<T> : PerformanceTestBase<Func<Random, T>, T>
{
    private Random random;

    protected override T Invoke(Func<Random, T> del) => del.Invoke(random);

    protected override void OnBeforeCase() => random = new Random(0); // resetting with a fix seed
}
```

And then a properly prepared `Random` instance will be an argument of your test cases:

```cs
new RandomizedPerformanceTest<string> { Iterations = 1_000_000 }
    .AddCase(rnd => rnd.NextEnum<ConsoleColor>().ToString(), "Enum.ToString")
    .AddCase(rnd => Enum<ConsoleColor>.ToString(rnd.NextEnum<ConsoleColor>()), "Enum<TEnum>.ToString")
    .DoTest()
    .DumpResults(Console.Out);
```

## License
KGy SOFT Core Libraries are under the [CC BY-ND 4.0](https://creativecommons.org/licenses/by-nd/4.0/legalcode) license (see a short summary [here](https://creativecommons.org/licenses/by-nd/4.0)). It allows you to copy and redistribute the material in any medium or format for any purpose, even commercially. The only thing is not allowed is to distribute a modified material as yours.

---

See the complete KGy SOFT Core Libraries documentation with even more examples at [docs.kgysoft.net](https://docs.kgysoft.net/corelibraries).

[![KGy SOFT .net](https://docs.kgysoft.net/corelibraries/icons/logo.png)](https://kgysoft.net)
