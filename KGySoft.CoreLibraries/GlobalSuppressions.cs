// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Code Analysis results, point to "Suppress Message", and click 
// "In Suppression File".
// You do not need to add suppressions to this file manually.

using System.Diagnostics.CodeAnalysis;

// General
[assembly: SuppressMessage("Microsoft.Usage", "CA2243:AttributeStringLiteralsShouldParseCorrectly",
    Justification = "AssemblyInformationalVersion reflects the nuget versioning convention.")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Gy",
    Justification = "KGy stands for initials")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Gy",
    Justification = "KGy stands for initials")]
[assembly: SuppressMessage("Style", "IDE0034:Simplify 'default' expression", Justification = "Should remain if helps understanding the code")]
[assembly: SuppressMessage("Style", "IDE0017:Object initialization can be simplified", Justification = "Decided individually")]
[assembly: SuppressMessage("Style", "IDE0057:Use range operator", Justification = "Cannot be used because it is not supported in every targeted platform")]
[assembly: SuppressMessage("Style", "IDE0063:'using' statement can be simplified", Justification = "Decided individually")]
[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "Decided individually")]
[assembly: SuppressMessage("Compiler", "CS9258:'value' is a contextual keyword", Justification = "Not used in a breaking way")]

// Static constructors (actually static field initializers)
[assembly: SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Scope = "member", Target = "~M:KGySoft.Serialization.Binary.BinarySerializationFormatter.#cctor",
    Justification = "BinarySerializationFormatter supports many types natively, this is intended. See also its DataTypes enum.")]
