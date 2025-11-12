using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace ReturnColors.Commands;

internal static class DumpCommand
{
    [field: AllowNull, MaybeNull]
    public static Command Command
    {
        get
        {
            if (field is not null)
                return field;

            field = new Command("dump", "Dump Affinity resources to a folder.");
            field.Arguments.Add(Global.DirectoryArgument);
            field.Options.Add(Options.OutputDirectoryOption);
            field.Subcommands.Add(DumpIconsCommand.Command);
            return field;
        }
    }

    public static class Options
    {
        public static readonly Option<DirectoryInfo> OutputDirectoryOption = new("--output", "-o")
        {
            DefaultValueFactory = _ => new DirectoryInfo(
                Path.Combine(AppContext.BaseDirectory, "icons")
            ),
            Description = "The directory into which to dump the resources.",
            Recursive = true,
            Required = true,
        };
    }
}
