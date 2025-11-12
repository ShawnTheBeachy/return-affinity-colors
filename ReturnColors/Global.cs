using Microsoft.Win32;
using System.CommandLine;
using System.Diagnostics;
namespace ReturnColors.Commands;

internal static class Global
{
    public static string? ParseAffinityKey() {
        if (!OperatingSystem.IsWindows()) {
            return null;
        }
        RegistryKey? RegKeyMaybe = Registry.LocalMachine.OpenSubKey("Software\\Serif\\Affinity\\Affinity");
        if (RegKeyMaybe == null)
        {
            return null;
        }
        RegistryKey RegKey = RegKeyMaybe;
        return (string?)RegKey.GetValue("Affinity Install Path");
    }

    public static readonly Argument<DirectoryInfo> DirectoryArgument = new("dir")
    {
        DefaultValueFactory = result => {
            var affinityPath = ParseAffinityKey();
            if (affinityPath == null)
            {
                result.AddError("No existing Affinity installation path was found.\nYou must manually specify the installation path.");
                return null;
            }
            return new DirectoryInfo(affinityPath);
        },
        Description = "The directory where Affinity is installed.",
        Validators =
        {
            result =>
            {
                DirectoryInfo directory;

                try {
                    directory = result.GetValueOrDefault<DirectoryInfo>();

                    if (!directory.Exists)
                        result.AddError($"\"{directory.FullName}\" does not exist.");
                } catch (InvalidOperationException e) {
                }
            },
        },
    };
    public static void TerminateAffinity()
    {
        Process[] pname = Process.GetProcessesByName("Affinity");
        foreach (var process in pname)
        {
            process.Kill();
        }
    }
    public static readonly Option<bool> TerminateOption = new("--terminate", "-t")
    {
        Arity = ArgumentArity.ZeroOrOne,
        DefaultValueFactory = _ => false,
        Description = "Checks if Affinity is running, force closes the application if used.",
    };
}
