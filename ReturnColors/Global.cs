using System.CommandLine;
using System.Diagnostics;
using Microsoft.Win32;

namespace ReturnColors;

internal static class Global
{
    public static readonly Argument<DirectoryInfo> DirectoryArgument = new("dir")
    {
        DefaultValueFactory = result =>
        {
            var affinityPath = GetAffinityInstallationPath();

            if (affinityPath is not null)
                return new DirectoryInfo(affinityPath);

            result.AddError(
                "No existing Affinity installation path was found.\nYou must manually specify the installation path."
            );
            return null!;
        },
        Description = "The directory where Affinity is installed.",
        Validators =
        {
            result =>
            {
                try
                {
                    var directory = result.GetValueOrDefault<DirectoryInfo>();

                    if (!directory.Exists)
                        result.AddError($"\"{directory.FullName}\" does not exist.");
                }
                catch (InvalidOperationException) { }
            },
        },
    };

    private static string? GetAffinityInstallationPath()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        var affinityKey = Registry.LocalMachine.OpenSubKey(@"Software\Serif\Affinity\Affinity");
        return (string?)affinityKey?.GetValue("Affinity Install Path");
    }

    public static void TerminateAffinity()
    {
        var affinityProcesses = Process.GetProcessesByName("Affinity");

        foreach (var process in affinityProcesses)
            process.Kill();
    }

    public static readonly Option<bool> TerminateOption = new("--terminate", "-t")
    {
        Arity = ArgumentArity.ZeroOrOne,
        DefaultValueFactory = _ => false,
        Description = "Checks if Affinity is running, force closes the application if used.",
    };
}
