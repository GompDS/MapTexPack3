# MapTexPack3
A console app that primarily automates the process of packing dds textures into a Dark Souls 3 TPFBDT.
It also prunes (permanently deletes) any unused textures from these texture binders.
# Requirements
- Install .NET Runtime 9.0.X
- Unpack your Dark Souls 3 game using UXM
  - Find UXM here: https://github.com/Nordgaren/UXM-Selective-Unpack
# Usage
- Ensure that the config.ini file paths reflect the layout of your system
  - game_path: the folder containing DarkSoulsIII.exe and all the unpacked game files
  - mod_path: the folder where all your mod files are stored
  - tex_path: the folder where you put dds files that you want to be packed into TPFBDTs
- Run the exe and follow the prompts in the interactive dialog
  - Enabling .patch means that your mod tpfbdt/bhds will be loaded IN ADDITION to the vanilla ones. 
    This is useful for saving space if your map primarily relies on textures from the vanilla binders.
  - If you do not enable .patch, the next prompt will ask if instead you would like textures to be
    included from the vanilla binders. This is will grab all the used textures from the vanilla
    binders and add them to the mod binders. This is useful if your map uses very few vanilla textures
    because then you aren't wasting memory loading excess textures.
  - It's also recommended that you enable backups incase there are any unforseen issues with this program.
    Use of a version control system like git for your mod files will also reduce the risk of losing stuff.
# License
Both MapTexPack3 and SoulsFormatsNEXT are licensed under GPL v3. In other words, all files in this solution
are licensed under GPL v3. A copy of the license is provided with the program.
# Links
The SoulsFormatsNEXT repository can be found here: https://github.com/soulsmods/SoulsFormatsNEXT/
