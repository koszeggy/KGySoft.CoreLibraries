// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0057:Use range operator", Justification = "Cannot be used because it is not supported in every targeted platform")]
[assembly: SuppressMessage("Performance", "CA1806:Do not ignore method results", Justification = "In performance tests result is often ignored")]
