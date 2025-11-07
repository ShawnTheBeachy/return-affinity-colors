using System.CommandLine;

namespace ReturnColors;

internal static class Global
{
    public static readonly Argument<DirectoryInfo> DirectoryArgument = new("dir")
    {
        Description = "The directory where Affinity is installed.",
        Validators =
        {
            result =>
            {
                var directory = result.GetValueOrDefault<DirectoryInfo>();

                if (!directory.Exists)
                    result.AddError($"\"{directory.FullName}\" does not exist.");
            },
        },
    };
}
