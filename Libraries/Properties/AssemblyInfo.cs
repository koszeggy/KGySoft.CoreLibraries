using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Common Libraries")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("KGy SOFT")]
[assembly: AssemblyProduct("KGy SOFT Common Libraries")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("4d2e18aa-1e79-4f90-a2a0-fb68a6901de5")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("3.6.3.1")]
[assembly: AssemblyFileVersion("3.6.3.1")]

[assembly: AllowPartiallyTrustedCallers]
#if NET40 || NET45
[assembly: SecurityRules(SecurityRuleSet.Level1/*, SkipVerificationInFullTrust = true*/)]
//[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#elif !NET35
#error .NET version is not set or not supported!
#endif

[assembly: NeutralResourcesLanguage("en-US")]