
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0034:Simplify 'default' expression", Justification = "Decided individually")]
[assembly: SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "Decided individually")]
[assembly: SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "Decided individually")]
[assembly: SuppressMessage("Style", "IDE0057:Use range operator", Justification = "Cannot be used because it is not supported in every targeted platform")]
[assembly: SuppressMessage("Style", "IDE0056:Use index operator", Justification = "Cannot be used because it is not supported in every targeted platform")]
[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "Decided individually")]
[assembly: SuppressMessage("Design", "CA1041:Provide ObsoleteAttribute message", Justification = "In UnitTests applying [Obsolete] just to suppress CS0618 but the tests are needed")]
[assembly: SuppressMessage("Style", "IDE0028:Simplify collection initialization", Justification = "Decided individually")]
[assembly: SuppressMessage("Performance", "CA1846:Prefer 'AsSpan' over 'Substring'", Justification = "Spans are not supported on every targeted platform")]
