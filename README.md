# MapTexPack3
A console app that primarily automates the process of packing dds textures into a Dark Souls 3 TPFBDT/BHD.
It also prunes any unused textures from these binders and keeps them all about the same size.
# Supported Platforms
- Windows
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
  - It's also recommended that you enable backups incase there are any unforseen issues with this program,
    because **pruned textures are deleted permanently**.
    Use of a version control system like git for your mod files will also reduce the risk of losing stuff.
# Libraries Used
- **SoulsFormatsNEXT**: All contributors to SoulsFormatsNEXT at soulsmods. https://github.com/soulsmods/SoulsFormatsNEXT/
# License
Both MapTexPack3 and SoulsFormatsNEXT are licensed under GPL v3. In other words, all files in this solution
are licensed under GPL v3. A copy of the license is provided with the program.
# Changelog
### 1.0.1
- Fix- change TryGetMsb call to use correct map dir when getting game msb (oops)
### 1.0.0
- Initial release