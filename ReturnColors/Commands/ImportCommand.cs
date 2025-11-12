using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace ReturnColors.Commands;

internal static class ImportCommand
{
    [field: AllowNull, MaybeNull]
    public static Command Command
    {
        get
        {
            if (field is not null)
                return field;

            field = new Command("import", "Import Affinity resources from a folder.");
            field.Arguments.Add(Global.DirectoryArgument);
            field.Options.Add(Options.ResourcesDirectoryOption);
            field.Subcommands.Add(ImportIconsCommand.Command);
            return field;
        }
    }

    public static class Options
    {
        public static readonly Option<DirectoryInfo> ResourcesDirectoryOption = new("--input", "-i")
        {
            Description = "The directory from which to import resources",
            Recursive = true,
            Required = true,
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
}
