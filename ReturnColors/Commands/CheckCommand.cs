using System.CommandLine;

namespace ReturnColors.Commands;

internal static class CheckCommand
{
    private static Command? _command;
    public static Command Command
    {
        get
        {
            if (_command is not null)
                return _command;

            _command = new Command(
                "check",
                "Check whether this tool has access to the Affinity installation directory."
            );
            _command.Arguments.Add(Global.DirectoryArgument);
            _command.SetAction(x => Execute(x));
            return _command;
        }
    }

    public static bool Execute(ParseResult parseResult) =>
        Execute(parseResult.GetRequiredValue(Global.DirectoryArgument));

    public static bool Execute(DirectoryInfo directory)
    {
        try
        {
            var tempFile = Path.Combine(directory.FullName, Path.GetRandomFileName());
            using var _ = File.Create(tempFile, 1, FileOptions.DeleteOnClose);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"You have write access to \"{directory.FullName}\".");
            Console.ResetColor();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"You do not have write access to \"{directory.FullName}\".");
        }
        catch (IOException)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to write to \"{directory.FullName}\"");
        }

        Console.ResetColor();
        return false;
    }
}
