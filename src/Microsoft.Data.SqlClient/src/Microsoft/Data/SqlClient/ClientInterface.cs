using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Data.SqlClient
{
    // ========================================================================
    // This class uses runtime environment information to produce a value
    // suitable for use in the TDS LOGIN7 Client Interface Name field.
    //
    internal static class ClientInterface
    {
        // ====================================================================
        // Properties

        // The client interface name, never null, never empty, and never larger
        // than TdsEnum.MAXLEN_CLIENTINTERFACE characters.
        //
        // Format:
        //
        //   Microsoft SqlClient - {OS Name} {OS Version}, {Runtime} - {Arch}
        //
        // The {OS Name} will be one of the following strings:
        //
        //   Windows
        //   Linux
        //   macOS
        //   FreeBSD
        //   Unknown
        //
        // The {OS Version} will be whatever the value of
        // System.Environment.OSVersion.Version is.
        //
        // The {Runtime} will be the value of
        // System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription.
        //
        // The {Arch} will be the value of
        // System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.
        //
        // This adheres to the TDS v37.0 spec, which specifies that the client
        // interface name has a maximum length of 128 unicode characters.  If
        // the format above expands beyond that limit, it will be truncated.
        //
        public static string Name => _name;

        // ====================================================================
        // Private Helpers

        // Static construction builds the client interface name.
        //
        // All known exceptions are caught and handled by providing a fallback
        // value.  However, no effort is made to catch all exceptions.
        //
        static ClientInterface()
        {
            try
            {
                // Expect to build a string whose length is up to our max
                // length.
                //
                // This isn't a max capacity, but a hint for initial buffer
                // allocation.  We will truncate to our max length after all of
                // the pieces have been appended.
                StringBuilder name = new(TdsEnums.MAXLEN_CLIENTINTERFACE);

                // Start with the well-known name of this driver.
                name.Append(Common.DbConnectionStringDefaults.ApplicationName);
                name.Append(" - ");

                // Add the OS name, in order of likelihood.
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    name.Append("Windows");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    name.Append("Linux");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    name.Append("macOS");
                }
#if NET
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                {
                    name.Append("FreeBSD");
                }
#endif // NET
                else
                {
                    name.Append(Unknown);
                }
                name.Append(' ');

                try
                {
                    // Add the OS version.
                    //
                    // Per the System.Version.ToString() documentation, this
                    // takes the form:
                    //
                    //   {Major}.{Minor}[.{Build}[.{Revision}]]
                    //
                    // Where [] indicate an optional part.
                    //
                    // All parts are decimal integers.
                    //
                    name.Append(
#if NET
                        // TODO: Why the special case for FreeBSD here?
                        // GetSystemVersion() returns the .NET runtime version,
                        // not the OS version.
                        RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)
                        ? RuntimeEnvironment.GetSystemVersion()
                        : Environment.OSVersion.Version);
#else
                        Environment.OSVersion.Version);
#endif // NET
                }
                catch (InvalidOperationException)
                {
                    // Environment.OSVersion failed in an unexpected way, so use
                    // a fallback value.
                    name.Append(Unknown);
                }
                name.Append(", ");

                // Add the .NET runtime info.
                //
                // The documentation for FrameworkDescription doesn't specify
                // that it will always return a non-null value, so apply our
                // unknown value in that unlikely case.
                name.Append(
                    RuntimeInformation.FrameworkDescription ?? Unknown);
                name.Append(" - ");

                // Add the architecture.
#if TARGET_X86 || TARGET_AMD64 || TARGET_ARM || TARGET_ARM64 || TARGET_WASM || TARGET_S390X || TARGET_LOONGARCH64 || TARGET_POWERPC64 || TARGET_RISCV64
                // TODO: Why the special case for some target architectures
                // here?  Architecture is an enum, so there is no such thing as
                // an unknown value.
                name.Append(RuntimeInformation.ProcessArchitecture);
#else
                name.Append(Unknown);
#endif
                
                // Remember it!
                _name = name.ToString();

                // Truncate to our max length.
                if (_name.Length > TdsEnums.MAXLEN_CLIENTINTERFACE)
                {
                    _name = _name.Substring(0, TdsEnums.MAXLEN_CLIENTINTERFACE);
                }

                Debug.Assert(_name.Length <= TdsEnums.MAXLEN_CLIENTINTERFACE);
            }
            catch (ArgumentOutOfRangeException)
            {
                // StringBuilder failed in an unexpected way, so use a fallback
                // value.
                _name =
                  Common.DbConnectionStringDefaults.ApplicationName +
                  " - " +
                  Unknown;
            }
        }

        // ====================================================================
        // Private Fields

        // The client interface name.
        private static readonly string _name;

        // A placeholder string for parts of the client interface name that are
        // unknown.
        private const string Unknown = "Unknown";
    }
}
