# Return Affinity Colors
This application modifies the `Serif.Affinity.dll` file to return colored tool icons for Affinity v3.

# How to use it
Simply download `rafcol.exe` and open an administrator command prompt at the download location. From the command prompt, run `rafcol "<path-to-folder-containing-Serif.Affinity.dll>"`. If you installed Affinity using the EXE and the default settings, the installation path is probably `C:\Program Files\Affinity\Affinity`. If you used the MSIX, the installation path is probably something like `C:\Program Files\WindowsApps\Canva.Affinity_3.0.0.3791_x64__8a0j1tnjnt4a4\App`. So, for example, if you used the EXE your command would be `rafcol "C:\Program Files\Affinity\Affinity"`.

NOTE: If you installed Affinity using the MSIX you may need to take ownership of the `App` folder and give the `Administrators` group full control permissions before this tool can work.

Before the tool runs, it will make a backup of your current `Serif.Affinity.dll` file. If anything goes wrong, you can revert to the default DLL by deleting the modified `Serif.Affinity.dll` and renaming `Serif.Affinity.bak` to `Serif.Affinity.dll`.

The tool will run and output a list of all the resources it replaced. A few new tools, such as the adjustment brush and filter brush, will not be replaced since they did not exist in v2.

# How it works
The icons which Affinity uses are embedded as resources inside the `Serif.Affinity.dll` file. This tool loads the DLL file, reads those resources, and replaces them with the matching v2 resources. Then it modifies the DLL file, replacing the v3 resources with the v2/v3 combined resources.
