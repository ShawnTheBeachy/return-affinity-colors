using System.Collections;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Resources;
using dnlib.DotNet;
using dnlib.DotNet.Writer;

namespace ReturnColors.Commands;

internal static class ImportIconsCommand
{
    [field: AllowNull, MaybeNull]
    public static Command Command
    {
        get
        {
            if (field is not null)
                return field;

            field = new Command("icons", "Import Affinity icons from a folder.");
            field.Arguments.Add(Global.DirectoryArgument);
            field.SetAction(Execute);
            return field;
        }
    }

    private static async Task Execute(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var installDirectory = parseResult.GetValue(Global.DirectoryArgument)!;

        if (!CheckCommand.Execute(installDirectory))
            return;

        var dllPath = Path.Combine(installDirectory.FullName, "Serif.Affinity.dll");
        var dllBytes = await File.ReadAllBytesAsync(dllPath, cancellationToken);
        using var module = ModuleDefMD.Load(
            dllBytes,
            new ModuleCreationOptions(ModuleDef.CreateModuleContext())
        );
        var inputDirectory = parseResult.GetRequiredValue(
            ImportCommand.Options.ResourcesDirectoryOption
        );
        var mergedResourcesFile = MergeResources(module, inputDirectory);
        var resourceIndex = module.Resources.IndexOf("Serif.Affinity.g.resources");
        var mergedResourcesFs = new FileStream(
            mergedResourcesFile.FullName,
            FileMode.Open,
            FileAccess.Read
        );
        var buffer = new Memory<byte>(new byte[mergedResourcesFs.Length]);
        await mergedResourcesFs.ReadExactlyAsync(buffer, cancellationToken);
        await mergedResourcesFs.DisposeAsync();
        var newResource = new EmbeddedResource(
            "Serif.Affinity.g.resources",
            buffer.ToArray(),
            ManifestResourceAttributes.Public
        );
        module.Resources[resourceIndex] = newResource;
        SaveDll(module, dllPath);
        mergedResourcesFile.Delete();
    }

    private static FileStream? FindCustomResource(string resourceKey, DirectoryInfo inputDirectory)
    {
        var splits = resourceKey.Split('/')[2..];
        var fileName = Path.Combine(inputDirectory.FullName, Path.Combine(splits));
        var file = new FileInfo(fileName);
        return !file.Exists ? null : file.OpenRead();
    }

    private static FileInfo MergeResources(ModuleDefMD module, DirectoryInfo inputDirectory)
    {
        var disposables = new Disposables();
        using var affinityResourceReader = new ResourceReader(
            module
                .Resources.FindEmbeddedResource("Serif.Affinity.g.resources")
                .CreateReader()
                .AsStream()
                .DisposeWith(disposables)
        );
        var resourcesTempFile = Path.Combine(AppContext.BaseDirectory, Path.GetRandomFileName());
        var mergedResourcesWriter = new ResourceWriter(resourcesTempFile);

        foreach (DictionaryEntry entry in affinityResourceReader)
        {
            var key = entry.Key.ToString() ?? "";

            if (!key.EndsWith(".png") || !key.StartsWith("resources/icons/"))
            {
                mergedResourcesWriter.AddResource(key, entry.Value);
                continue;
            }

            var customResource = FindCustomResource(key, inputDirectory);

            if (customResource is null)
            {
                mergedResourcesWriter.AddResource(key, entry.Value);
                continue;
            }

            mergedResourcesWriter.AddResource(key, customResource, true);
        }

        mergedResourcesWriter.Dispose();
        disposables.Dispose();
        return new FileInfo(resourcesTempFile);
    }

    private static void SaveDll(ModuleDefMD module, string path)
    {
        File.Delete(path);

        if (module.IsILOnly)
            module.Write(path);
        else
        {
            var writerOptions = new NativeModuleWriterOptions(module, false);
            module.NativeWrite(path, writerOptions);
        }

        Console.WriteLine($"Updated \"{path}\", importing custom icons.");
    }
}
