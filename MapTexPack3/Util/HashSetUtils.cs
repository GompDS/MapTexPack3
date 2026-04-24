//    MapTexPack3
//    A console app that primarily automates the process of packing dds
//    textures into a Dark Souls 3 TPFBDT.
//
//    Copyright (C) 2026  GompDS
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License,
//    any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program. If not, see <https://www.gnu.org/licenses/>.

using System.Text.RegularExpressions;
using SoulsFormats;

namespace MapTexPack3.Util;

public static class HashSetUtils
{
    public static void AddTexturesFromBnd(this HashSet<string> currentTextureSet, string dcx)
    {
        BND4 bnd = BND4.Read(dcx);

        HashSet<string> localTextures = new HashSet<string>();
        BinderFile? tpfFile = bnd.Files.FirstOrDefault(x => TPF.Is(x.Bytes));
        if (tpfFile != null)
        {
            TPF tpf = TPF.Read(tpfFile.Bytes);
            foreach (TPF.Texture tex in tpf.Textures)
            {
                string texName = Path.GetFileNameWithoutExtension(tex.Name).ToLower();
                localTextures.Add(texName);
                localTextures.Add(texName + "_l");
            }
        }

        BinderFile? flverFile = bnd.Files.FirstOrDefault(x => x.Name.EndsWith(".flver"));
        if (flverFile != null)
        {
            FLVER2 model = FLVER2.Read(flverFile.Bytes);
            foreach (FLVER2.Material mat in model.Materials)
            {
                foreach (FLVER2.Texture tex in mat.Textures.Where(x => x.Path.Length > 0))
                {
                    string texPath = Path.GetFileNameWithoutExtension(tex.Path).ToLower();
                    if (localTextures.Contains(texPath)) continue;
                    if (Regex.IsMatch(texPath, "[0-9]{8}_m[0-9]{6}_gi_[0-9]{4}_[0-9]{2}_dol_[0-9]{2}"))
                    {
                        // skip lightmap textures
                        continue;
                    }
                    currentTextureSet.Add(texPath);
                    currentTextureSet.Add(texPath + "_l");
                }
            }
        }
    }
}