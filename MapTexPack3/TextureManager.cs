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

using System.Globalization;
using System.Text.RegularExpressions;
using MapTexPack3.Util;
using SoulsFormats;

namespace MapTexPack3;

public class TextureManager
{
    public readonly HashSet<string> UsedTextures = new();
    private readonly HashSet<string> _usedMapPieces = new();
    private readonly HashSet<string> _usedObjects = new();
    private readonly HashSet<string> _objectsInMod = new();
    public readonly List<BinderFile> BinderTexPool = new List<BinderFile>();

    public TextureManager(MSB3 modMsb)
    {
        GetUsedMapPieces(modMsb);
        GetUsedMapPieceTextures(Options.ModMapDirectory);
        GetUsedObjects(modMsb);
        GetModObjects();
        GetUsedObjectTextures(Options.ModObjectDirectory);

        if (Program.TryGetMsb(Options.ModMapDirectory, out MSB3? gameMsb))
        {
            if (_usedMapPieces.Count == 0)
            {
                GetUsedMapPieces(gameMsb);
            }

            GetUsedMapPieceTextures(Options.GameMapDirectory);

            if (_usedObjects.Count == 0)
            {
                GetUsedObjects(gameMsb);
            }
            //else
            //{
            //    _usedObjects.ExceptWith(_objectsInMod);
            //}

            GetUsedObjectTextures(Options.GameObjectDirectory);
        }
    }

    private void GetUsedMapPieceTextures(string mapDirectory)
    {
        if (!Directory.Exists(mapDirectory)) return;
        
        string? mapPartsDirectory = Directory.GetDirectories(mapDirectory,
            $"m{Options.MapId:D2}_{Options.BlockId:D2}_00_00").FirstOrDefault();
        
        if (mapPartsDirectory == null) return;
        
        foreach (string dcx in Directory.EnumerateFiles(mapPartsDirectory, "*bnd.dcx")
                     .Where(x => _usedMapPieces.Contains(string.Concat("m", Path.GetFileName(x)
                         .AsSpan(13, 6)))))
        {
            UsedTextures.AddTexturesFromBnd(dcx);
        }
    }

    public void GetUsedObjectTextures(string objectDirectory)
    {
        if (Directory.Exists(objectDirectory))
        {
            foreach (string dcx in Directory.EnumerateFiles(objectDirectory, "*bnd.dcx")
                         .Where(x => _usedObjects.Contains(Path.GetFileName(x)[..7])))
            {
                UsedTextures.AddTexturesFromBnd(dcx);
            }
        }
    }

    private void GetUsedMapPieces(MSB3 msb)
    {
        foreach (MSB3.Part.MapPiece mapPiece in msb.Parts.MapPieces)
        {
            _usedMapPieces.Add(mapPiece.ModelName);
        }
    }

    private void GetUsedObjects(MSB3 msb)
    {
        foreach (MSB3.Part.Object obj in msb.Parts.Objects)
        {
            _usedObjects.Add(obj.ModelName);
        }
    }

    private void GetModObjects()
    {
        if (Directory.Exists(Options.ModObjectDirectory))
        {
            foreach (string path in Directory.EnumerateFiles(Options.ModObjectDirectory)
                         .Where(x => _usedObjects.Contains(Path.GetFileName(x)[..7])))
            {
                _objectsInMod.Add(Path.GetFileName(path)[..7]);
            }
        }
    }
    
    private bool CopyTextureToPool(BinderFile binderTex, out long unusedTextureByteCount)
    {
        unusedTextureByteCount = 0;
            
        // skip unused textures and add to unused byte tally
        if (!UsedTextures.Contains(binderTex.Name.ToLower()[..^8]))
        {
            unusedTextureByteCount += binderTex.Bytes.Length;
            return false;
        }
            
        // add used textures to pool in alphabetical order
        for (int j = 0; j < BinderTexPool.Count; j++)
        {
            BinderFile poolTex = BinderTexPool[j];
            int compareResult = string.Compare(binderTex.Name, poolTex.Name, true, CultureInfo.InvariantCulture);
            if (compareResult > 0) continue;
            if (compareResult == 0) return false; // since mod textures are added first, vanilla ones will not replace them
            
            BinderTexPool.Insert(j, binderTex);
            return true;
        }
            
        BinderTexPool.Add(binderTex);
        return true;
    }

    public void FillTexturePool(string mapDirectory, bool createBackups, 
        out long usedTexByteCount, out long unusedTexByteCount, out int texTransferCount)
    {
        usedTexByteCount = 0;
        unusedTexByteCount = 0;
        texTransferCount = 0;
        string mapTexturesDirectory = mapDirectory + $"//m{Options.MapId:D2}";
        if (Directory.Exists(mapTexturesDirectory))
        {
            for (int i = 0; i < 4; i++)
            {
                string? bhdPath = Directory.EnumerateFiles(mapTexturesDirectory).FirstOrDefault(x =>
                    x.ToLower().EndsWith($"m{Options.MapId:D2}_000{i}.tpfbhd") ||
                    x.ToLower().EndsWith($"m{Options.MapId:D2}_000{i}.tpfbhd.patch"));
                string? bdtPath = Directory.EnumerateFiles(mapTexturesDirectory).FirstOrDefault(x =>
                    x.ToLower().EndsWith($"m{Options.MapId:D2}_000{i}.tpfbdt") ||
                    x.ToLower().EndsWith($"m{Options.MapId:D2}_000{i}.tpfbdt.patch"));
                if (bhdPath == null || bdtPath == null) continue;
                if (File.ReadAllBytes(bdtPath).Length <= 0) continue;
                    
                BXF4 textureBinder = BXF4.Read(bhdPath, bdtPath);
                if (createBackups)
                {
                    textureBinder.Write(bhdPath + ".bak", bdtPath + ".bak");
                }

                foreach (BinderFile file in textureBinder.Files)
                {
                    if (CopyTextureToPool(file, out long tempUnusedTexByteCount))
                    {
                        texTransferCount++;
                        usedTexByteCount += file.Bytes.Length;
                    }
                    else
                    {
                        unusedTexByteCount += tempUnusedTexByteCount;
                    }
                }
            }
        }
    }

    public int TransferYarrTextures(IEnumerable<string> files)
    {
        int transferCount = 0;
        foreach (string filePath in files.Where(x => UsedTextures.Contains(Path.GetFileNameWithoutExtension(x).ToLower())))
        {
            BinderFile file = CreateTextureBinderFile(filePath);
            if (CopyTextureToPool(file, out _))
            {
                transferCount++;
            }
        }
        
        return transferCount;
    }

    private static BinderFile CreateTextureBinderFile(string filePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        TPF newTpf = new TPF();
        TPF.Texture newTexture = new TPF.Texture(fileName, 0, 0, File.ReadAllBytes(filePath), TPF.TPFPlatform.PC);
        if (fileName.EndsWith("_n") || fileName.EndsWith("_n_l"))
        {
            newTexture.Format = 0x6A;
        }
        else if (Regex.IsMatch(fileName, @"[0-9]{8}_m[0-9]{6}_gi_[0-9]{4}_[0-9]{2}_dol_[0-9]{2}"))
        {
            newTexture.Format = 0x66;
            newTpf.Flag2 = 0x03;
        }

        newTpf.Textures.Add(newTexture);

        BinderFile newFile = new BinderFile(Binder.FileFlags.Flag1, $"{fileName}.tpf.dcx",
            DCX.Compress(newTpf.Write(), new DCX.DcxDfltCompressionInfo(DCX.DfltCompressionPreset.DCX_DFLT_10000_44_9)));
        return newFile;
    }
}