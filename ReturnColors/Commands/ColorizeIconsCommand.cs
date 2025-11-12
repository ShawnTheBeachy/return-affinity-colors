using System.Collections;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Resources;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Microsoft.Extensions.FileProviders;

namespace ReturnColors.Commands;

internal static class ColorizeIconsCommand
{
    [field: AllowNull, MaybeNull]
    public static Command Command
    {
        get
        {
            if (field is not null)
                return field;

            field = new Command(
                "colorize",
                "Replace the Affinity monochrome icons with the v2 colored icons."
            );
            field.Arguments.Add(Global.DirectoryArgument);
            field.Options.Add(Global.TerminateOption);
            field.Options.Add(Options.BackupOption);
            field.Options.Add(Options.PauseOption);
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

        var pause = parseResult.GetValue(Options.PauseOption);
        var directory = parseResult.GetValue(Global.DirectoryArgument)!;
        var dllPath = Path.Combine(directory.FullName, "Serif.Affinity.dll");
        var backup = parseResult.GetValue(Options.BackupOption);

        if (backup is not null && !new FileInfo(dllPath).BackUp(backup, pause))
        {
            Console.RedLine("Failed to back up current Serif.Affinity.dll.");
            return;
        }

        var dllBytes = await File.ReadAllBytesAsync(dllPath, cancellationToken);
        using var module = ModuleDefMD.Load(
            dllBytes,
            new ModuleCreationOptions(ModuleDef.CreateModuleContext())
        );
        var resourcesTempFile = Path.Combine(AppContext.BaseDirectory, Path.GetRandomFileName());
        MergeResources(module, resourcesTempFile);
        await ReplaceResources(module, resourcesTempFile);

        if (pause)
            Global.Pause();

        SaveDll(module, dllPath);
        File.Delete(resourcesTempFile);
    }

    private static object? FindV2Resource(string key, ResourceReader v2ResourceReader)
    {
        var v2Key = GetV2ResourceKey(key);

        foreach (DictionaryEntry v2Entry in v2ResourceReader)
            if (v2Entry.Key.ToString() == v2Key)
                return v2Entry.Value;

        Console.YellowLine($"Failed to resolve v2 resource \"{key}\".");
        return null;
    }

    private static string GetV2ResourceKey(string v3ResourceKey) =>
        v3ResourceKey switch
        {
            "resources/icons/tools/brushtool.imageset/paint%20brush%20tool_2.png" =>
                "resources/icons/tools/brushtool.imageset/paint%20brush%20tool.png",
            "resources/icons/tools/brushtool.imageset/paint%20brush%20tool@2x_2.png" =>
                "resources/icons/tools/brushtool.imageset/paint%20brush%20tool@2x.png",
            "resources/icons/tools/objectselectiontool.imageset/object%20selection%20tool.png" =>
                "resources/icons/tools/objectselectiontool.imageset/object_selection_tool.png",
            "resources/icons/tools/objectselectiontool.imageset/object%20selection%20tool@2x.png" =>
                "resources/icons/tools/objectselectiontool.imageset/object_selection_tool@2x.png",
            "resources/icons/tools/measuretool.imageset/measure%20tool.png" =>
                "resources/icons/tools/measuretool.imageset/measuretool.png",
            "resources/icons/tools/measuretool.imageset/measure%20tool@2x.png" =>
                "resources/icons/tools/measuretool.imageset/measuretool@2x.png",
            "resources/icons/tools/strokewidthtool.imageset/line%20width%20tool%20mono.png" =>
                "resources/icons/tools/strokewidthtool.imageset/line%20width%20tool.png",
            "resources/icons/tools/strokewidthtool.imageset/line%20width%20tool%20mono@2x.png" =>
                "resources/icons/tools/strokewidthtool.imageset/line%20width%20tool@2x.png",
            "resources/icons/tools/inpaintingbrushtool.imageset/inpainting%20tool.png" =>
                "resources/icons/tools/inpaintingbrushtool.imageset/inpainting%20brush%20tool.png",
            "resources/icons/tools/inpaintingbrushtool.imageset/inpainting%20tool@2x.png" =>
                "resources/icons/tools/inpaintingbrushtool.imageset/inpainting%20brush%20tool@2x.png",
            _ => v3ResourceKey,
        };

    private static ResourceReader GetV2ResourceReader(Disposables disposables)
    {
        var embeddedFileProvider = new ManifestEmbeddedFileProvider(
            typeof(ColorizeIconsCommand).Assembly
        );
        return new ResourceReader(
            embeddedFileProvider
                .GetFileInfo("res/Serif.Affinity.v2.g.resources")
                .CreateReadStream()
                .DisposeWith(disposables)
        );
    }

    private static ResourceReader GetV3ResourceReader(
        ModuleDefMD module,
        Disposables disposables
    ) =>
        new(
            module
                .Resources.FindEmbeddedResource("Serif.Affinity.g.resources")
                .CreateReader()
                .AsStream()
                .DisposeWith(disposables)
        );

    private static void MergeResources(ModuleDefMD module, string resourcesFile)
    {
        File.Delete(resourcesFile);
        Disposables disposables = [];
        using var v2ResourceReader = GetV2ResourceReader(disposables);
        using var v3ResourceReader = GetV3ResourceReader(module, disposables);
        var mergedResourcesWriter = new ResourceWriter(resourcesFile);

        foreach (DictionaryEntry v3Entry in v3ResourceReader)
        {
            var key = v3Entry.Key.ToString() ?? "";

            if (
                !key.EndsWith(".png")
                || (
                    !key.StartsWith("resources/icons/tools/")
                    && !key.StartsWith("resources/icons/colourpicker.imageset")
                    && !key.StartsWith("resources/icons/formatdropper.imageset")
                )
            )
            {
                mergedResourcesWriter.AddResource(key, v3Entry.Value);
                continue;
            }

            try
            {
                var v2Resource = FindV2Resource(key, v2ResourceReader);
                mergedResourcesWriter.AddResource(key, v2Resource ?? v3Entry.Value);
                Console.GreenLine($"Merged v2 resource \"{key}\".");
            }
            catch (Exception exception)
            {
                Console.RedLine($"Failed to merge v2 resource \"{key}\".");
                Console.RedLine(exception.Message);
                mergedResourcesWriter.AddResource(key, v3Entry.Value);
            }
        }

        mergedResourcesWriter.Dispose();
        disposables.Dispose();
    }

    private static async ValueTask ReplaceResources(ModuleDefMD module, string resourcesFile)
    {
        var resourceIndex = module.Resources.IndexOf("Serif.Affinity.g.resources");
        await using var mergedResourcesFs = new FileStream(resourcesFile, FileMode.Open);
        var buffer = new Memory<byte>(new byte[mergedResourcesFs.Length]);
        await mergedResourcesFs.ReadExactlyAsync(buffer);
        var newResource = new EmbeddedResource(
            "Serif.Affinity.g.resources",
            buffer.ToArray(),
            ManifestResourceAttributes.Public
        );
        module.Resources[resourceIndex] = newResource;
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

        Console.WriteLine($"Updated \"{path}\", replacing monochrome icons with colored icons.");
    }

    private static class Options
    {
        public static readonly Option<DirectoryInfo> BackupOption = new("--backup", "-b")
        {
            DefaultValueFactory = _ => new DirectoryInfo(AppContext.BaseDirectory),
            Description =
                "The directory into which to back up the current Serif.Affinity.dll file before modifying it.",
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
        public static readonly Option<bool> PauseOption = new("--pause", "-p")
        {
            DefaultValueFactory = _ => false,
            Description = "Pause between each step of the process and wait for user input.",
        };
    }
}
