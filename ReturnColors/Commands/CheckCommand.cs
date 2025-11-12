using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace ReturnColors.Commands;

internal static class CheckCommand
{
    [field: AllowNull, MaybeNull]
    public static Command Command
    {
        get
        {
            if (field is not null)
                return field;

            field = new Command(
                "check",
                "Check whether this tool has access to the Affinity installation directory."
            );
            field.Arguments.Add(Global.DirectoryArgument);
            field.SetAction(x => Execute(x));
            return field;
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
            Console.GreenLine($"You have write access to \"{directory.FullName}\".");
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            Console.RedLine($"You do not have write access to \"{directory.FullName}\".");
        }
        catch (IOException)
        {
            Console.RedLine($"Failed to write to \"{directory.FullName}\"");
        }

        return false;
    }
}
