#if NETCOREAPP3_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;

namespace KGySoft.CoreLibraries
{
    internal sealed partial class AotTestRunner
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "NUnit.Framework.Internal.TestExecutionContext", "nunit.framework")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(GlobalInitialization))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.ResTests))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.AllowNullDictionaryTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.Array2DTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.Array3DTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.ArraySectionTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.CacheTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.CastArrayTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.CircularListTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.CircularSortedListTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.StringKeyedDictionaryTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.ThreadSafeCacheTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.ThreadSafeDictionaryTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.ThreadSafeHashSetTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Collections.ObjectModel.FastLookupCollectionTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.ComponentModel.CommandsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.ComponentModel.ObservableObjectTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.ComponentModel.PersistableObjectTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.ComponentModel.TypeConvertersTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.ComponentModel.UndoableObjectTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.ComponentModel.ValidatingObjectTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.ComponentModel.Collections.FastBindingListTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.ComponentModel.Collections.ObservableBindingListTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.ComponentModel.Collections.SortableBindingListTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.EnumComparerTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.EnumTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.FastRandomTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.FilesTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.StringSegmentTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.ByteArrayExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.DecimalExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.DictionaryExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.DoubleExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.EnumerableExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.FloatExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.ObjectExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.RandomExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.SpanExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.StreamExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.StringExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.StringSegmentExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.CoreLibraries.Extensions.TypeExtensionsTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Diagnostics.ProfilerTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Reflection.ReflectorTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Reflection.TypeResolverTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Resources.DynamicResourceManagerTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Resources.HybridResourceManagerTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Resources.ResXDataNodeTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Resources.ResXResourceManagerTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Resources.ResXResourceReaderTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Resources.ResXResourceSetTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Resources.ResXResourceWriterTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Serialization.Binary.BinarySerializerTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Serialization.Binary.CustomSerializerSurrogateSelectorTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Serialization.Binary.ForwardedTypesSerializationBinderTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Serialization.Xml.XmlSerializerTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Threading.AsyncHelperTest))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(UnitTests.Threading.ParallelHelperTest))]
        private AotTestRunner()
        {
        }
    }
}

#endif
