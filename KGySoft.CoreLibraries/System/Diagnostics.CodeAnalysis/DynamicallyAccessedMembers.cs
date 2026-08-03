// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Contains constants that have been added later to <see cref="DynamicallyAccessedMemberTypes"/>, and other meaningful combinations.
    /// </summary>
    internal static class DynamicallyAccessedMembers
    {
        #region Constants

        internal const DynamicallyAccessedMemberTypes NonPublicConstructorsWithInherited = DynamicallyAccessedMemberTypes.NonPublicConstructors | (DynamicallyAccessedMemberTypes)0x4000;
        internal const DynamicallyAccessedMemberTypes PublicConstructorsWithInherited = DynamicallyAccessedMemberTypes.PublicConstructors | (DynamicallyAccessedMemberTypes)0x_0010_0000;
        internal const DynamicallyAccessedMemberTypes AllConstructors = PublicConstructorsWithInherited | NonPublicConstructorsWithInherited;

        internal const DynamicallyAccessedMemberTypes NonPublicMethodsWithInherited = DynamicallyAccessedMemberTypes.NonPublicMethods | (DynamicallyAccessedMemberTypes)0x8000;
        internal const DynamicallyAccessedMemberTypes AllMethods = DynamicallyAccessedMemberTypes.PublicMethods | NonPublicMethodsWithInherited;

        internal const DynamicallyAccessedMemberTypes NonPublicFieldsWithInherited = DynamicallyAccessedMemberTypes.NonPublicFields | (DynamicallyAccessedMemberTypes)0x_0001_0000;
        internal const DynamicallyAccessedMemberTypes AllFields = DynamicallyAccessedMemberTypes.PublicFields | NonPublicFieldsWithInherited;

        internal const DynamicallyAccessedMemberTypes NonPublicNestedTypesWithInherited = DynamicallyAccessedMemberTypes.NonPublicNestedTypes | (DynamicallyAccessedMemberTypes)0x_0002_0000;
        internal const DynamicallyAccessedMemberTypes PublicNestedTypesWithInherited = DynamicallyAccessedMemberTypes.PublicNestedTypes | (DynamicallyAccessedMemberTypes)0x_0020_0000;
        internal const DynamicallyAccessedMemberTypes AllNestedTypes = PublicNestedTypesWithInherited | NonPublicNestedTypesWithInherited;

        internal const DynamicallyAccessedMemberTypes NonPublicPropertiesWithInherited = DynamicallyAccessedMemberTypes.NonPublicProperties | (DynamicallyAccessedMemberTypes)0x_0004_0000;
        internal const DynamicallyAccessedMemberTypes AllProperties = DynamicallyAccessedMemberTypes.PublicProperties | NonPublicPropertiesWithInherited;

        internal const DynamicallyAccessedMemberTypes NonPublicEventsWithInherited = DynamicallyAccessedMemberTypes.NonPublicEvents | (DynamicallyAccessedMemberTypes)0x_0008_0000;
        internal const DynamicallyAccessedMemberTypes AllEvents = DynamicallyAccessedMemberTypes.PublicEvents | NonPublicEventsWithInherited;

        internal const DynamicallyAccessedMemberTypes AllMembersAndInterfaces =
            AllConstructors |
            AllEvents |
            AllFields |
            AllMethods |
            AllNestedTypes |
            AllProperties |
            DynamicallyAccessedMemberTypes.Interfaces;

        #endregion
    }
}