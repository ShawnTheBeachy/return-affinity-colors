# Return Affinity Colors
This application modifies the `Serif.Affinity.dll` file to return colored tool icons for Affinity v3.

NOTE: This tool has only been tested on Windows. It runs on .NET 9, so it should be able to run on Linux and macOS. However, I'm not familiar with how Affinity is compiled on those platforms and I don't have either one to test on. You are free to compile it yourself and give it a try.

# How to use it
Download the [`latest release`](https://github.com/ShawnTheBeachy/return-affinity-colors/releases/latest) and run the program with an administrator terminal. The program will automatically look for an existing installation path for Affinity, if one is found it will automatically patch the library in the detected path by default.

From the command prompt, run `rafcol <command> "<installation-path>"`. If you installed Affinity using the EXE and the default settings, the installation path is probably `C:\Program Files\Affinity\Affinity`. If you used the MSIX, the installation path is probably something like `C:\Program Files\WindowsApps\Canva.Affinity_3.0.0.3791_x64__8a0j1tnjnt4a4\App`. So, for example, if you used the EXE your command would be `rafcol "C:\Program Files\Affinity\Affinity"`.

NOTE: If you installed Affinity using the MSIX you may need to take ownership of the `App` folder and give the `Administrators` group full control permissions before this tool can work.

## Check write access
To check if you have write access to the Affinity installation folder, run `rafcol check "<installation-path>"`.

## Colorize icons
To replace the monochrome icons with colored icons, run `rafcol colorize "<installation-path>"`. This will create a backup of your current `Serif.Affinity.dll` file before updating it. By default this backup will be placed in the folder from which you are running the command prompt. To change the backup location, pass the `--backup "<path>"` option.

The tool will run and output a list of all the resources it replaced. A few new tools, such as the adjustment brush and filter brush, will not be replaced since they did not exist in v2.

NOTE: When Affinity updates it is likely that `Serif.Affinity.dll` will get overwritten with an updated version. You should be able to simply run the tool again to return the colored icons.

## Replace the splash screen image
To replace the splash screen image, run `rafcol splash "<installation-path>" --img "<splash-image-path>"`. This will create a backup of your current `Affinity.exe` file before updating it. By default this backup will be placed in the folder from which you are running the command prompt. To change the backup location, pass the `--backup "<path>"` option. If anything goes wrong, you can revert to the default EXE by deleting the modified `Affinity.exe` and renaming `Affinity.exe.bak` to `Affinity.exe`.

NOTE: When Affinity updates it is likely that `Affinity.exe` will get overwritten with an updated version. You should be able to simply run the tool again to return the custom splash screen.

# How it works
The icons and splash screen image which Affinity uses are embedded as resources inside the `Serif.Affinity.dll` and `Affinity.exe` files. This tool loads the DLL or EXE file, reads those resources, and replaces them with the matching v2 resources or your custom splash screen.

# MSIX permissions
If you installed Affinity with the MSIX installer, you will need to get permissions for the Affinity installation folder.

1. Take ownership of the `WindowsApps` folder. Navigate to `C:\Program Files`. Make sure that your file explorer options are set to show hidden files and folders. Right-click on the `WindowsApps` folder and click "Properties". Navigate to the `Security` tab and click `Advanced`.
   
   <img width="714" height="918" alt="image" src="https://github.com/user-attachments/assets/5e724d37-ba21-425f-b98b-8bfb62b98cc3" />
   
   Click "Change" next to the "Owner".
   
   <img width="1528" height="992" alt="image" src="https://github.com/user-attachments/assets/6c046613-0fe2-42ce-93ae-f520c73aac0c" />

   Enter your username and then click "Check names". Click "OK", and then "OK" again.

2. Now you should be able to open the `WindowsApps` folder. Inside it, find the Affinity installation folder. This will probably be something like `Canva.Affinity_3.0.0.3791_x64__8a0j1tnjnt4a4`. Open the folder and right-click on the `App` folder. Follow the same steps as step 1 to take ownership of the `App` folder, but this time be sure to check the "Replace owner on subcontainers and objects" checkbox.
   
    <img width="1516" height="990" alt="image" src="https://github.com/user-attachments/assets/50a08be4-a644-46cc-8769-6a36d30c4074" />


3. Right-click on `App` again, choose properties, and navigate to Security > Advanced again. This time, check the "Replace all child object permission entries..." checkbox and then click "Disable inheritance". When prompted, choose to convert inherited permissions into explicit permissions.

   <img width="1520" height="996" alt="image" src="https://github.com/user-attachments/assets/177d0845-64c1-4f67-997e-c6210527296a" />


   Click "Apply", then choose "Yes".

4. Select the "Administrators" group permissions, then click "Edit".
   
   <img width="1518" height="990" alt="image" src="https://github.com/user-attachments/assets/0d8a840e-1ed1-426a-a14d-6ce4e6bd40c7" />

    Choose "Full control", then click "OK".

   <img width="1816" height="1134" alt="image" src="https://github.com/user-attachments/assets/17cc6e2b-98af-4136-8ca5-0fb9ba30e157" />

5. Click "OK", then "OK" again to close the "Properties" window.

   You should now be able to run the `rafcol` command to update your Affinity icons!

