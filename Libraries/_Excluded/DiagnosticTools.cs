/*
  <data name="ExceptionMessage" xml:space="preserve">
    <value>Exception Message:</value>
  </data>
  <data name="ExceptionMessageNotAvailable" xml:space="preserve">
    <value>Message is not available: {0}</value>
  </data>
   <data name="InnerException" xml:space="preserve">
    <value>&gt;&gt;&gt;&gt; Inner Exception &gt;&gt;&gt;&gt;</value>
  </data>
  <data name="InnerExceptionEnd" xml:space="preserve">
    <value>&lt;&lt;&lt;&lt; End of Inner Exception &lt;&lt;&lt;&lt;</value>
  </data>
  <data name="Win32ErrorCode" xml:space="preserve">
    <value>Win32 native error code: {0}</value>
  </data>
  <data name="SystemInformation" xml:space="preserve">
    <value>System information:</value>
  </data>
  <data name="UserInformation" xml:space="preserve">
    <value>User information:</value>
  </data>
  <data name="ExceptionSource" xml:space="preserve">
    <value>Exception Source:</value>
  </data>
  <data name="ExceptionSourceNotAvailable" xml:space="preserve">
    <value>Source is not available: {0}</value>
  </data>
  <data name="ExceptionTargetSite" xml:space="preserve">
    <value>Exception Target Site:</value>
  </data>
  <data name="ExceptionTargetSiteNotAccessible" xml:space="preserve">
    <value>Cannot not access target site infos: {0}</value>
  </data>
  <data name="ExceptionTargetSiteNotAvailable" xml:space="preserve">
    <value>Target site is not available.</value>
  </data>
  <data name="ExceptionType" xml:space="preserve">
    <value>Exception Type:</value>
  </data>
  <data name="ExceptionTypeNotAvailable" xml:space="preserve">
    <value>Type info is not available: {0}</value>
  </data>
  <data name="RemoteStackTrace" xml:space="preserve">
    <value>---- Remote Stack Trace ----</value>
  </data>
  <data name="SourceOffset" xml:space="preserve">
    <value>{0}: line {1}, col {2}</value>
  </data>
  <data name="StackTrace" xml:space="preserve">
    <value>---- Stack Trace ----</value>
  </data>
  <data name="DateAndTime" xml:space="preserve">
    <value>Date and Time: </value>
  </data>
  <data name="ILOffset" xml:space="preserve">
    <value>, IL {0:#0000}</value>
  </data>
  <data name="LocalStackTrace" xml:space="preserve">
    <value>---- Local Stack Trace ----</value>
  </data>
  <data name="NativeOffset" xml:space="preserve">
    <value>{0}: N {1:#00000}</value>
  </data>
  <data name="OperatingSystem" xml:space="preserve">
    <value>Operating System: </value>
  </data>
  <data name="Environment" xml:space="preserve">
    <value>Environment: {0}-bit</value>
  </data>
  <data name="ProcessorCount" xml:space="preserve">
    <value>Processor Count: </value>
  </data>
  <data name="ClrVersion" xml:space="preserve">
    <value>CLR version: </value>
  </data>
  <data name="WorkingSet" xml:space="preserve">
    <value>Working Set: {0} bytes</value>
  </data>
  <data name="CommandLine" xml:space="preserve">
    <value>Command Line: </value>
  </data>
  <data name="ApplicationDomain" xml:space="preserve">
    <value>Application Domain: </value>
  </data>
  <data name="MachineName" xml:space="preserve">
    <value>Machine Name: </value>
  </data>
  <data name="UserName" xml:space="preserve">
    <value>User name: </value>
  </data>
  <data name="CurrentUser" xml:space="preserve">
    <value>Current User: </value>
  </data>
  <data name="CannotGetDomain" xml:space="preserve">
    <value>&lt;Domain is not available&gt;</value>
  </data>
  <data name="AssemblyBuildDate" xml:space="preserve">
    <value>Assembly Build Date: </value>
  </data>
  <data name="AssemblyCodebase" xml:space="preserve">
    <value>Assembly Codebase: </value>
  </data>
  <data name="AssemblyFullName" xml:space="preserve">
    <value>Assembly Full Name: </value>
  </data>
  <data name="AssemblyVersion" xml:space="preserve">
    <value>Assembly Version: </value>
  </data>
*/
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Diagnostic tools and <see cref="Exception"/> utilities that can be used in logs, error reports,
    /// user feedbacks, etc.
    /// </summary>
    public static class DiagnosticTools
    {
        private const string indent = "  ";

        /// <summary>
        /// Gets a detailed <see cref="string"/> of exception information.
        /// </summary>
        public static string ExceptionToString(Exception e, bool includeSystemAndUserInfo)
        {
            return ExceptionToString(e, includeSystemAndUserInfo, 0);
        }

        /// <summary>
        /// Gets a detailed <see cref="string"/> of exception information.
        /// </summary>
        public static string ExceptionToString(Exception e)
        {
            return ExceptionToString(e, true, 0);
        }

        /// <summary>
        /// Gets a detailed <see cref="string"/> of exception information.
        /// </summary>
        private static string ExceptionToString(Exception e, bool includeSystemAndUserInfo, int depth)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(indent.Repeat(depth) + Res.Get(Res.ExceptionMessage));
            try
            {
                sb.AppendLine(indent.Repeat(depth + 1) + e.Message);
            }
            catch (Exception ex)
            {
                sb.AppendLine(indent.Repeat(depth + 1) + Res.Get(Res.ExceptionMessageNotAvailable, ex.Message));
            }
            if (e.InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine(indent.Repeat(depth) + Res.Get(Res.InnerException));
                sb.AppendLine(ExceptionToString(e.InnerException, false, depth + 1));
                sb.AppendLine(indent.Repeat(depth) + Res.Get(Res.InnerExceptionEnd));
                sb.AppendLine();
            }
            Win32Exception win32Exception = e as Win32Exception;
            if (win32Exception != null)
            {
                sb.AppendLine(indent.Repeat(depth) + Res.Get(Res.Win32ErrorCode, win32Exception.NativeErrorCode));
            }

            sb.AppendLine();
            if (includeSystemAndUserInfo)
            {
                //'-- get general system and app information
                sb.AppendLine(Res.Get(Res.SystemInformation));
                sb.AppendLine(SysInfoToString());
                sb.AppendLine(Res.Get(Res.UserInformation));
                sb.AppendLine(UserInfoToString());
            }

            //'-- get exception-specific information
            sb.AppendLine(indent.Repeat(depth) + Res.Get(Res.ExceptionSource));
            try
            {
                sb.AppendLine(indent.Repeat(depth + 1) + e.Source);
            }
            catch (Exception ex)
            {
                sb.AppendLine(indent.Repeat(depth + 1) + Res.Get(Res.ExceptionSourceNotAvailable, ex.Message));
            }

            sb.AppendLine();
            sb.AppendLine(indent.Repeat(depth) + Res.Get(Res.ExceptionType));
            try
            {
                sb.AppendLine(indent.Repeat(depth + 1) + e.GetType());
            }
            catch (Exception ex)
            {
                sb.AppendLine(indent.Repeat(depth + 1) + Res.Get(Res.ExceptionTypeNotAvailable, ex.Message));
            }

            sb.AppendLine();
            sb.AppendLine(indent.Repeat(depth) + Res.Get(Res.ExceptionTargetSite));
            try
            {
                MethodBase targetSite = e.TargetSite;
                if (targetSite != null)
                {
                    sb.AppendLine(indent.Repeat(depth + 1) + TypeToString(targetSite.DeclaringType) + "." + targetSite.Name);
                    sb.AppendLine(AssemblyInfoToString(targetSite.DeclaringType.Assembly, depth));
                }
                else
                {
                    sb.AppendLine(indent.Repeat(depth + 1) + Res.Get(Res.ExceptionTargetSiteNotAvailable));
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine(indent.Repeat(depth + 1) + Res.Get(Res.ExceptionTargetSiteNotAccessible, ex.Message));
            }

            try
            {
                sb.Append(EnhancedStackTrace(e, depth));
            }
            catch (Exception ex)
            {
                sb.Append(ex.Message);
            }

            sb.AppendLine();
            return sb.ToString();
        }

        private static string TypeToString(Type type)
        {
            return type.ToString().Replace('+', '.');
        }

        /// <summary>
        /// Gets an enhanced string trace of an exception.
        /// </summary>
        private static string EnhancedStackTrace(Exception e, int depth)
        {
            StackTrace st = new StackTrace(e, true);
            StringBuilder sb = new StringBuilder();
            string remoteTrace = (string)Accessors.Exception_remoteStackTraceString.Get(e);
            if (remoteTrace != null)
            {
                sb.AppendLine();
                sb.AppendLine(indent.Repeat(depth) + Res.Get(Res.RemoteStackTrace));
                sb.AppendLine(remoteTrace);
            }

            sb.AppendLine();
            sb.AppendLine(indent.Repeat(depth) + (remoteTrace == null ? Res.Get(Res.StackTrace) : Res.Get(Res.LocalStackTrace)));
            EnhancedStackTrace(st, sb, depth);
            return sb.ToString();
        }

        private static void EnhancedStackTrace(StackTrace st, StringBuilder sb, int depth)
        {
            for (int i = 0; i < st.FrameCount; i++)
            {
                StackFrame sf = st.GetFrame(i);
                sb.Append(StackFrameToString(sf, depth));
            }

            sb.AppendLine();
        }

        private static string StackFrameToString(StackFrame sf, int depth)
        {
            StringBuilder sb = new StringBuilder();
            MemberInfo mi = sf.GetMethod();

            //'-- build method name
            sb.Append(indent.Repeat(depth));
            if (mi.DeclaringType != null)
            {
                sb.Append(TypeToString(mi.DeclaringType));
                sb.Append(".");
            }
            sb.Append(mi.Name);

            //'-- build method params
            ParameterInfo[] parameters = sf.GetMethod().GetParameters();
            int i = 0;
            sb.Append("(");
            foreach (ParameterInfo pi in parameters)
            {
                i++;
                if (i > 1)
                    sb.Append(", ");
                sb.Append(pi.ParameterType.Name);
                sb.Append(" ");
                sb.Append(pi.Name);
            }
            sb.Append(")");
            sb.AppendLine();

            //'-- if source code is available, append location info
            sb.Append(indent.Repeat(depth + 2));
            string fileName = sf.GetFileName();
            if (String.IsNullOrEmpty(fileName))
            {
                //'-- native code offset is always available
                sb.AppendFormat(Res.Get(Res.NativeOffset), Assembly.GetEntryAssembly().CodeBase, sf.GetNativeOffset());
            }
            else
            {
                sb.AppendFormat(Res.Get(Res.SourceOffset), fileName, sf.GetFileLineNumber(), sf.GetFileColumnNumber());
                //'-- if IL is available, append IL location info
                if (sf.GetILOffset() != StackFrame.OFFSET_UNKNOWN)
                {
                    sb.AppendFormat(Res.Get(Res.ILOffset), sf.GetILOffset());
                }
            }

            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Gets a detailed system information as string.
        /// </summary>
        public static string SysInfoToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Res.Get(Res.DateAndTime) + DateTime.Now.ToString(LanguageSettings.FormattingLanguage));
            sb.Append(Res.Get(Res.OperatingSystem));
            try
            {
                sb.Append(Environment.OSVersion.VersionString);
            }
            catch (Exception e)
            {
                sb.Append(e.Message);
            }
            sb.AppendLine();
            sb.AppendFormat(Res.Get(Res.Environment), IntPtr.Size * 8);
            sb.AppendLine();
            sb.AppendLine(Res.Get(Res.ProcessorCount) + Environment.ProcessorCount);
            sb.AppendLine(Res.Get(Res.ClrVersion) + Environment.Version);
            sb.AppendFormat(Res.Get(Res.WorkingSet, Environment.WorkingSet.ToString("N0", LanguageSettings.FormattingLanguage)));
            sb.AppendLine();
            sb.AppendLine(Res.Get(Res.CommandLine) + Environment.CommandLine);
            sb.Append(Res.Get(Res.ApplicationDomain));
            try
            {
                sb.Append(AppDomain.CurrentDomain.FriendlyName);
            }
            catch (Exception e)
            {
                sb.Append(e.Message);
            }
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Gets a detailed user information as string.
        /// </summary>
        public static string UserInfoToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(Res.Get(Res.MachineName));
            try
            {
                sb.Append(Environment.MachineName);
            }
            catch (Exception e)
            {
                sb.Append(e.Message);
            }
            sb.AppendLine();
            sb.Append(Res.Get(Res.UserName));
            sb.Append(Environment.UserName);
            sb.AppendLine();
            sb.Append(Res.Get(Res.CurrentUser));
            try
            {
                sb.Append(WindowsIdentity.GetCurrent().Name);
            }
            catch
            {
                try
                {
                    sb.Append(Environment.UserDomainName + "\\");
                }
                catch
                {
                    sb.Append(Res.Get(Res.CannotGetDomain) + "\\");
                }
                sb.Append(Environment.UserName);
            }
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Gets detailed assembly information as string.
        /// </summary>
        public static string AssemblyInfoToString(Assembly a, int depth)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(indent.Repeat(depth) + Res.Get(Res.AssemblyCodebase));
            try
            {
                sb.Append(a.CodeBase);
            }
            catch (Exception e)
            {
                sb.Append(e.Message);
            }
            sb.AppendLine();
            sb.Append(indent.Repeat(depth) + Res.Get(Res.AssemblyFullName));
            try
            {
                sb.Append(a.FullName);
            }
            catch (Exception e)
            {
                sb.Append(e.Message);
            }
            sb.AppendLine();
            sb.Append(indent.Repeat(depth) + Res.Get(Res.AssemblyVersion));
            try
            {
                sb.Append(a.GetName().Version);
            }
            catch (Exception e)
            {
                sb.Append(e.Message);
            }
            sb.AppendLine();
            sb.Append(indent.Repeat(depth) + Res.Get(Res.AssemblyBuildDate));
            try
            {
                sb.Append(AssemblyBuildDate(a));
            }
            catch (Exception e)
            {
                sb.Append(e.Message);
            }
            sb.AppendLine();
            return sb.ToString();
        }

        /// <summary>
        /// Gets assembly build date
        /// </summary>
        private static DateTime AssemblyBuildDate(Assembly a)
        {
            Version v = a.GetName().Version;
            DateTime dt = new DateTime(2000, 1, 1).AddDays(v.Build).AddSeconds(v.Revision * 2);
            DateTime localTime = DateTime.Now;
            if (TimeZone.IsDaylightSavingTime(localTime, TimeZone.CurrentTimeZone.GetDaylightChanges(localTime.Year)))
            {
                dt = dt.AddHours(1);
            }
            if (dt > localTime || v.Build < 730 || v.Revision == 0)
            {
                try
                {
                    dt = File.GetLastWriteTime(a.Location);
                }
                catch
                {
                    return DateTime.MaxValue;
                }
            }

            return dt;
        }
    }
}
