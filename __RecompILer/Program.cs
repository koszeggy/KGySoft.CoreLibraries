#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: Program.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

#endregion

namespace KGySoft.RecompILer
{
    internal static class Program
    {
        #region Constants

        private const bool decompileOnly = false;
        private const bool removeChangedSource = true;

        private const string patternEquals = "Equals(!TEnum x,";
        private const string equalsOrigSize = "20 (0x14)";
        private const string patternGetHashCode = "GetHashCode(!TEnum";
        private const string getHashCodeOrigSize = "14 (0xe)";
        private const string patternCompare = "Compare(!TEnum x,";
        private const string compareOrigSize = "23 (0x17)";
        private const string ilasm2 = @"..\Microsoft.NET\Framework\v2.0.50727\ilasm.exe";
        private const string ilasm4 = @"..\Microsoft.NET\Framework\v4.0.30319\ilasm.exe";

        private const string bodyEquals = @"
    .maxstack 8
    ldarg.1
    ldarg.2
    ceq
    ret";

        private const string bodyGetHashCode = @"
    .maxstack 1
    .locals init (int32 obj)
    ldarg.1
    conv.i4
    stloc.0
    ldloca.s obj
    call instance int32 [mscorlib]System.Int32::GetHashCode()
    ret";

        private const string bodyCompare = @"
    .maxstack 3
    .locals init (int64 signedX, uint64 unsignedX)
    ldsfld bool class KGySoft.CoreLibraries.EnumComparer`1/RecompiledEnumComparer<!TEnum>::isUnsignedCompare
    brtrue.s CompareAsUnsigned
    ldarg.1
    conv.i8
    stloc.0
    ldloca.s signedX
    ldarg.2
    conv.i8
    call instance int32 [mscorlib]System.Int64::CompareTo(int64)
    ret
    CompareAsUnsigned: ldarg.1
    conv.i8
    stloc.1
    ldloca.s unsignedX
    ldarg.2
    conv.i8
    call instance int32 [mscorlib]System.UInt64::CompareTo(uint64)
    ret";

        #endregion

        #region Fields

        private static readonly string[] ildasm35 =
            {
                @"Microsoft SDKs\Windows\v6.0A\bin\ildasm.exe",
                @"Microsoft SDKs\Windows\v7.0A\bin\ildasm.exe",
                @"Microsoft SDKs\Windows\v7\bin\ildasm.exe"
            };

        private static readonly string[] ildasm4 =
            {
                @"Microsoft SDKs\Windows\v7.0A\bin\NETFX 4.0 Tools\ildasm.exe",
                @"Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\ildasm.exe",
                @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools\ildasm.exe",
                @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\ildasm.exe",
            };

        #endregion

        #region Methods

        private static int Main(string[] args)
        {
            Console.WriteLine("RecompILer");
            if (args.Length != 3)
            {
                Console.WriteLine("Target Framework, assembly path and .snk path are expected");
                return 1;
            }

            Console.WriteLine("Used parameters:");
            string targetFramework = args[0];
            string fileName = args[1];
            string keyFile = args[2];
            Console.WriteLine($"Target Framework: {targetFramework}");
            Console.WriteLine($"Assembly Path: {fileName}");
            Console.WriteLine($"Key Path: {keyFile}");

            if (!File.Exists(fileName))
            {
                Console.WriteLine($"File does not exist: {fileName}");
                return 1;
            }

            if (!File.Exists(keyFile))
            {
                Console.WriteLine($"File does not exist: {keyFile}");
                return 1;
            }

            string ildasmExe = FindIldasm(targetFramework);
            if (ildasmExe == null)
            {
                // Error message has already been written
                return 1;
            }
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string ilasmExe = Path.Combine(windows, targetFramework.StartsWith("net4", StringComparison.Ordinal) ? ilasm4 : ilasm2);
            if (!File.Exists(ilasmExe))
            {
                Console.WriteLine("Can't find ilasm. Aborting. Expected it at: {0}", ilasmExe);
                return 1;
            }

            try
            {
                string ilFile = Decompile(ildasmExe, fileName);
                if (decompileOnly)
                    return 0;

                if (!ChangeIL(ilFile))
                {
                    return 1;
                }

                if (!Recompile(ilFile, ilasmExe, fileName, keyFile))
                {
                    return 1;
                }

                if (removeChangedSource)
                {
                    File.Delete(ilFile);
                    File.Delete(Path.ChangeExtension(ilFile, ".res"));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return 1;
            }
            return 0;
        }

        private static string FindIldasm(string frameworkVersion)
        {
            string programFiles = IntPtr.Size == 4 ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) : Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            foreach (string sdkPath in (frameworkVersion == "net40" || frameworkVersion == "net45") ? ildasm4 : ildasm35)
            {
                string ildasm = Path.Combine(programFiles, sdkPath);
                if (File.Exists(ildasm))
                {
                    return ildasm;
                }
            }
            Console.WriteLine("Unable to find SDK directory containing ildasm.exe. Aborting.");
            return null;
        }

        private static string Decompile(string ildasmExe, string fileName)
        {
            string ilFile = Path.GetTempFileName();
            Console.WriteLine("Decompiling to {0}", ilFile);
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = ildasmExe,
                Arguments = "\"/OUT=" + ilFile + "\" " + fileName,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            process.WaitForExit();
            return ilFile;
        }

        private static bool Recompile(string ilFile, string ilasmExe, string fileName, string keyFile)
        {
            var backupAssembly = fileName + ".bak";
            if (File.Exists(backupAssembly))
                File.Delete(backupAssembly);
            File.Move(fileName, backupAssembly);
            Console.WriteLine("Recompiling {0} to {1}", ilFile, fileName);
            Process process = Process.Start(new ProcessStartInfo
            {
                FileName = ilasmExe,
                Arguments = String.Format("/OUTPUT={0} /KEY={1} /DLL \"{2}\"", fileName, keyFile, ilFile), // "/OUTPUT=" + OutputAssembly + " /DLL " + "\"" + ilFile + "\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardError = true,
                //RedirectStandardOutput = true // just to hide it
            });
            process.WaitForExit();
            //process.StandardOutput.ReadToEnd(); // otherwise VisualStudio will never answer after build =|
            if (process.ExitCode != 0)
            {
                Console.WriteLine("Error: {0}", process.StandardError.ReadToEnd());
                return false;
            }

            return true;
        }

        private static bool ChangeIL(string ilFile)
        {
            StringBuilder content = new StringBuilder(File.ReadAllText(ilFile));
            // changing enum constraints
            content.Replace("([mscorlib]System.ValueType, [mscorlib]System.IConvertible)", "([mscorlib]System.Enum)");
            content.Replace("([mscorlib]System.IConvertible, [mscorlib]System.ValueType)", "([mscorlib]System.Enum)");

            // changing method bodies
            if (!ReplaceBody(content, patternEquals, equalsOrigSize, bodyEquals))
                return false;
            if (!ReplaceBody(content, patternGetHashCode, getHashCodeOrigSize, bodyGetHashCode))
                return false;
            if (!ReplaceBody(content, patternCompare, compareOrigSize, bodyCompare))
                return false;

            File.WriteAllText(ilFile, content.ToString());
            return true;
        }

        private static bool ReplaceBody(StringBuilder sb, string pattern, string origSize, string newBody)
        {

            string s = sb.ToString();
            int start = s.IndexOf(pattern, StringComparison.Ordinal);
            while (start >= 0)
            {
                start = s.IndexOf('{', start);
                int end = s.IndexOf('}', start);
                if (s.IndexOf(origSize, start, end - start, StringComparison.Ordinal) >= 0)
                {
                    sb.Remove(start + 1, end - start - 1);
                    sb.Insert(start + 1, Environment.NewLine);
                    sb.Insert(start + 1, newBody);
                    sb.Insert(start + 1, Environment.NewLine);
                    return true;
                }

                start = s.IndexOf(pattern, end + 1, StringComparison.Ordinal);
            }


            Console.WriteLine($"Error: \"{pattern}\" of size \"{origSize}\" not found in source code");
            return false;
        }

        #endregion
    }
}
