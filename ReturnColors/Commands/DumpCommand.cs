using System.CommandLine;

namespace ReturnColors.Commands;

internal static class DumpCommand
{
    private static Command? _command;
    public static Command Command
    {
        get
        {
            if (_command is not null)
                return _command;

            _command = new Command("dump", "Dump Affinity resources to a folder.");
            _command.Arguments.Add(Global.DirectoryArgument);
            _command.Options.Add(Options.OutputDirectoryOption);
            _command.Subcommands.Add(DumpIconsCommand.Command);
            return _command;
        }
    }

    public static class Options
    {
        public static readonly Option<DirectoryInfo> OutputDirectoryOption = new("--output", "-o")
        {
            DefaultValueFactory = _ => new DirectoryInfo(
                Path.Combine(AppContext.BaseDirectory, "icons")
            ),
            Description = "The directory into which to dump the icons.",
            Recursive = true,
            Required = true,
        };
    }
}
