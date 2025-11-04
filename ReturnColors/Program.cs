using System.Collections;
using System.Resources;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Microsoft.Extensions.FileProviders;
using ReturnColors;
using static Crayon.Output;

var v3Directory = args[0];
var v3DllPath = Path.Combine(v3Directory, "Serif.Affinity.dll");
BackUpV3Dll();
Disposables disposables = [];
var embeddedFileProvider = new ManifestEmbeddedFileProvider(typeof(Program).Assembly);
var v3Module = ModuleDefMD
    .Load(v3DllPath, new ModuleCreationOptions(ModuleDef.CreateModuleContext()))
    .DisposeWith(disposables);
var mergedResourcesFile = Path.Combine(AppContext.BaseDirectory, "merged.temp");
MergeResources();
ReplaceV3Resources();
SaveV3Dll();
disposables.Dispose();
File.Delete(mergedResourcesFile);
return;

void BackUpV3Dll()
{
    var backupFilePath = Path.Combine(v3Directory, "Serif.Affinity.bak");
    File.Delete(backupFilePath);
    File.Copy(v3DllPath, backupFilePath, true);

    if (!File.Exists(backupFilePath))
        throw new Exception("Failed to back up DLL.");

    Console.WriteLine(
        $"Backed up original DLL to \"{backupFilePath}\". To restore, delete \"{v3DllPath}\" and then change the .bak file extension to .dll."
    );
}

IReadOnlyList<ResourceReader> CollectV2ResourceReaders()
{
    var designerResReader = new ResourceReader(
        embeddedFileProvider
            .GetFileInfo("res/Serif.Affinity.Designer.g.resources")
            .CreateReadStream()
            .DisposeWith(disposables)
    ).DisposeWith(disposables);
    var photoResReader = new ResourceReader(
        embeddedFileProvider
            .GetFileInfo("res/Serif.Affinity.Photo.g.resources")
            .CreateReadStream()
            .DisposeWith(disposables)
    ).DisposeWith(disposables);
    var publisherResReader = new ResourceReader(
        embeddedFileProvider
            .GetFileInfo("res/Serif.Affinity.Publisher.g.resources")
            .CreateReadStream()
            .DisposeWith(disposables)
    ).DisposeWith(disposables);
    return [designerResReader, photoResReader, publisherResReader];
}

object? FindV2Resource(string key, IReadOnlyList<ResourceReader> v2ResourceReaders)
{
    var v2Key = GetV2ResourceKey(key);

    foreach (var resourceReader in v2ResourceReaders)
    foreach (DictionaryEntry v2Entry in resourceReader)
        if (v2Entry.Key.ToString() == v2Key)
            return v2Entry.Value;

    Console.WriteLine(Yellow($"Failed to resolve v2 resource \"{key}\"."));
    return null;
}

string GetV2ResourceKey(string v3ResourceKey) =>
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

ResourceReader GetV3ResourceReader()
{
    var v3ResourceStream = v3Module
        .Resources.FindEmbeddedResource("Serif.Affinity.g.resources")
        .CreateReader()
        .AsStream()
        .DisposeWith(disposables);
    return new ResourceReader(v3ResourceStream);
}

void MergeResources()
{
    File.Delete(mergedResourcesFile);
    var v2ResourceReaders = CollectV2ResourceReaders();
    var v3ResourceReader = GetV3ResourceReader();
    var mergedResourcesWriter = new ResourceWriter(mergedResourcesFile);

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
            var v2Resource = FindV2Resource(key, v2ResourceReaders);
            mergedResourcesWriter.AddResource(key, v2Resource ?? v3Entry.Value);
            Console.WriteLine(Green($"Merged v2 resource \"{key}\"."));
        }
        catch (Exception exception)
        {
            Console.WriteLine(Red($"Failed to merge v2 resource \"{key}\"."));
            Console.WriteLine(Red(exception.Message));
            mergedResourcesWriter.AddResource(key, v3Entry.Value);
        }
    }

    v3ResourceReader.Dispose();
    mergedResourcesWriter.Dispose();
}

void ReplaceV3Resources()
{
    var resourceIndex = v3Module.Resources.IndexOf("Serif.Affinity.g.resources");
    var mergedResourcesFs = new FileStream(mergedResourcesFile, FileMode.Open).DisposeWith(
        disposables
    );
    var buffer = new byte[mergedResourcesFs.Length];
    _ = mergedResourcesFs.Read(buffer, 0, (int)mergedResourcesFs.Length);
    var newResource = new EmbeddedResource(
        "Serif.Affinity.g.resources",
        buffer,
        ManifestResourceAttributes.Public
    );
    v3Module.Resources[resourceIndex] = newResource;
}

void SaveV3Dll()
{
    File.Delete(v3DllPath);

    if (v3Module.IsILOnly)
        v3Module.Write(v3DllPath);
    else
    {
        var writerOptions = new NativeModuleWriterOptions(v3Module, false);
        v3Module.NativeWrite(v3DllPath, writerOptions);
    }

    Console.WriteLine($"Saved modified DLL to \"{v3DllPath}\".");
}
