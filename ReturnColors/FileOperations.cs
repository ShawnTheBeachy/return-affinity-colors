using ReturnColors.Commands;

namespace ReturnColors;

internal static class FileOperations
{
    public static bool BackUp(this FileInfo file, DirectoryInfo backupTo, bool pause = false)
    {
        if (!CheckCommand.Execute(backupTo))
            return false;

        var backupFile = file.CopyTo(Path.Combine(backupTo.FullName, $"{file.Name}.bak"), true);
        Console.WriteLine($"Backed up \"{file.FullName}\" to \"{backupFile.FullName}\".");

        if (pause)
            Global.Pause();

        return true;
    }
}
