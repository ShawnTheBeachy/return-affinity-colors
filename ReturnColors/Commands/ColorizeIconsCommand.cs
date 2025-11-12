using System.Collections;
using System.CommandLine;
using System.Resources;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Microsoft.Extensions.FileProviders;

namespace ReturnColors.Commands;

internal static class ColorizeIconsCommand
{
    private static Command? _command;
    public static Command Command
    {
        get
        {
            if (_command is not null)
                return _command;

            _command = new Command(
                "colorize",
                "Replace the Affinity monochrome icons with the v2 colored icons."
            );
            _command.Arguments.Add(Global.DirectoryArgument);
            _command.Options.Add(Global.TerminateOption);
            _command.Options.Add(Options.BackupOption);
            _command.SetAction(Execute);
            return _command;
        }
    }

    private static bool BackUpDll(FileInfo dll, DirectoryInfo directory)
    {
        if (!CheckCommand.Execute(directory))
            return false;

        var dllBackup = dll.CopyTo(
            Path.Combine(directory.FullName, "Serif.Affinity.dll.bak"),
            true
        );
        Console.WriteLine($"Backed up \"{dll.FullName}\" to \"{dllBackup.FullName}\".");
        return true;
    }

    private static async Task Execute(ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!CheckCommand.Execute(parseResult))
            return;

        if (parseResult.GetValue(Global.TerminateOption))
            Global.TerminateAffinity();

        var directory = parseResult.GetValue(Global.DirectoryArgument)!;
        var dllPath = Path.Combine(directory.FullName, "Serif.Affinity.dll");
        var backup = parseResult.GetValue(Options.BackupOption);

        if (backup is not null && !BackUpDll(new FileInfo(dllPath), backup))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed to back up current Serif.Affinity.dll.");
            Console.ResetColor();
            return;
        }

        byte[] dllBytes = System.IO.File.ReadAllBytes(dllPath);
        using var module = ModuleDefMD.Load(
            dllBytes,
            new ModuleCreationOptions(ModuleDef.CreateModuleContext())
        );
        var resourcesTempFile = Path.Combine(AppContext.BaseDirectory, Path.GetRandomFileName());
        MergeResources(module, resourcesTempFile);
        await ReplaceResources(module, resourcesTempFile);
        SaveDll(module, dllPath);
        File.Delete(resourcesTempFile);
    }

    private static object? FindV2Resource(string key, ResourceReader v2ResourceReader)
    {
        var v2Key = GetV2ResourceKey(key);

        foreach (DictionaryEntry v2Entry in v2ResourceReader)
            if (v2Entry.Key.ToString() == v2Key)
                return v2Entry.Value;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Failed to resolve v2 resource \"{key}\".");
        Console.ResetColor();
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
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Merged v2 resource \"{key}\".");
                Console.ResetColor();
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to merge v2 resource \"{key}\".");
                Console.WriteLine(exception.Message);
                Console.ResetColor();
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
    }
}
