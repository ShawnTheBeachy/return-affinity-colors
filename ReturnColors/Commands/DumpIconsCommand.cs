using System.Collections;
using System.CommandLine;
using System.Resources;
using dnlib.DotNet;

namespace ReturnColors.Commands;

internal static class DumpIconsCommand
{
    private static Command? _command;
    public static Command Command
    {
        get
        {
            if (_command is not null)
                return _command;

            _command = new Command("icons", "Dump the Affinity icon resources to a folder.");
            _command.Arguments.Add(Global.DirectoryArgument);
            _command.Options.Add(DumpCommand.Options.OutputDirectoryOption);
            _command.SetAction(Execute);
            return _command;
        }
    }

    private static async Task Execute(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var outputDirectory = parseResult.GetRequiredValue(
            DumpCommand.Options.OutputDirectoryOption
        );

        if (!outputDirectory.Exists)
            outputDirectory.Create();

        if (!CheckCommand.Execute(outputDirectory))
            return;

        var installDirectory = parseResult.GetValue(Global.DirectoryArgument)!;
        var dllPath = Path.Combine(installDirectory.FullName, "Serif.Affinity.dll");
        var dllBytes = await File.ReadAllBytesAsync(dllPath, cancellationToken);
        using var module = ModuleDefMD.Load(
            dllBytes,
            new ModuleCreationOptions(ModuleDef.CreateModuleContext())
        );
        var disposables = new Disposables();
        using var resourceReader = new ResourceReader(
            module
                .Resources.FindEmbeddedResource("Serif.Affinity.g.resources")
                .CreateReader()
                .AsStream()
                .DisposeWith(disposables)
        );

        foreach (DictionaryEntry entry in resourceReader)
        {
            var key = entry.Key.ToString() ?? "";

            if (!key.EndsWith(".png") || !key.StartsWith("resources/icons/"))
                continue;

            if (entry.Value is not Stream resourceStream)
                continue;

            var splits = key.Split('/')[2..];
            var directorySplits = splits.Length < 2 ? [] : splits[..^1];
            var subDirectoryPath = Path.Combine(directorySplits);
            var subDirectory = outputDirectory.CreateSubdirectory(subDirectoryPath);
            var fileName = Path.Combine(subDirectory.FullName, splits[^1]);
            await using var fs = File.Create(fileName, (int)resourceStream.Length);
            using var ms = new MemoryStream(new byte[resourceStream.Length], true);
            await resourceStream.CopyToAsync(ms, cancellationToken);
            ms.Seek(0, SeekOrigin.Begin);
            await fs.WriteAsync(ms.ToArray(), cancellationToken);
        }

        disposables.Dispose();
    }
}
