using System.Collections;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Resources;
using dnlib.DotNet;

namespace ReturnColors.Commands;

internal static class ReplaceSplashImageCommand
{
    [field: AllowNull, MaybeNull]
    public static Command Command
    {
        get
        {
            if (field is not null)
                return field;

            field = new Command("splash", "Replace the Affinity startup splash image.");
            field.Arguments.Add(Global.DirectoryArgument);
            field.Options.Add(Global.TerminateOption);
            field.Options.Add(Options.BackupOption);
            field.Options.Add(Options.SplashImageOption);
            field.SetAction(Execute);
            return field;
        }
    }

    private static async Task Execute(ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!CheckCommand.Execute(parseResult))
            return;

        if (parseResult.GetValue(Global.TerminateOption))
            Global.TerminateAffinity();

        var directory = parseResult.GetValue(Global.DirectoryArgument)!;
        var exePath = Path.Combine(directory.FullName, "Affinity.exe");
        var backup = parseResult.GetValue(Options.BackupOption);

        if (backup is not null && !new FileInfo(exePath).BackUp(backup))
        {
            Console.RedLine("Failed to back up current Affinity.exe.");
            return;
        }

        var splashImage = parseResult.GetRequiredValue(Options.SplashImageOption);
        using var module = ModuleDefMD.Load(
            exePath,
            new ModuleCreationOptions(ModuleDef.CreateModuleContext())
        );
        using var resourceReader = new ResourceReader(
            module.Resources.FindEmbeddedResource("Affinity.g.resources").CreateReader().AsStream()
        );
        var resourcesTempFile = Path.Combine(AppContext.BaseDirectory, Path.GetRandomFileName());
        var resourceWriter = new ResourceWriter(resourcesTempFile);

        foreach (DictionaryEntry resource in resourceReader)
        {
            var key = resource.Key.ToString() ?? "";

            if (key != "resources/images/splash.imageset/studioprosplash.png")
            {
                resourceWriter.AddResource(key, resource.Value);
                continue;
            }

            await using var fs = new FileStream(splashImage.FullName, FileMode.Open);
            var buffer = new byte[fs.Length];
            await fs.ReadExactlyAsync(buffer, 0, (int)fs.Length, cancellationToken);
            var ms = new MemoryStream(buffer, false);
            resourceWriter.AddResource(key, ms, true);
        }

        resourceWriter.Dispose();
        var resourceIndex = module.Resources.IndexOf("Affinity.g.resources");
        var mergedResourcesFs = new FileStream(resourcesTempFile, FileMode.Open);
        var resBuffer = new Memory<byte>(new byte[mergedResourcesFs.Length]);
        _ = await mergedResourcesFs.ReadAsync(resBuffer, cancellationToken);
        var newResource = new EmbeddedResource(
            "Affinity.g.resources",
            resBuffer.ToArray(),
            ManifestResourceAttributes.Public
        );
        module.Resources[resourceIndex] = newResource;
        File.Delete(exePath);
        module.Write(exePath);
        await mergedResourcesFs.DisposeAsync();
        File.Delete(resourcesTempFile);
        Console.WriteLine(
            $"Updated \"{exePath}\", replacing the startup splash image with \"{splashImage.FullName}\"."
        );
    }

    private static class Options
    {
        public static readonly Option<DirectoryInfo> BackupOption = new("--backup", "-b")
        {
            DefaultValueFactory = _ => new DirectoryInfo(AppContext.BaseDirectory),
            Description =
                "The directory into which to back up the current Affinity.exe file before modifying it.",
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
        public static readonly Option<FileInfo> SplashImageOption = new("--img", "-i")
        {
            Description = "The image to use as the Affinity startup splash image.",
            Required = true,
            Validators =
            {
                result =>
                {
                    var file = result.GetValueOrDefault<FileInfo>();

                    if (!file.Exists)
                        result.AddError($"\"{file.FullName}\" does not exist.");
                },
            },
        };
    }
}
