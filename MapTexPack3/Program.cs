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

namespace MapTexPack3;

static class Program
{
    private const string texNameNoTypeRegex = @".+(?=_[a-km-z](|_l)\.tpf\.dcx)";
    
    public static void Main()
    {
        if (Options.ProcessOptions())
        {
            Run();
        }
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    public static void Run()
    {
        long totalTexByteCount = 0;

        Console.WriteLine(": Searching the game files for textures used by this msb... (this may take a minute)");

        if (!TryGetMsb(Options.ModMapDirectory, out MSB3? modMsb))
        {
            Console.WriteLine("CRITICAL: No mod msb found. Aborting.");
            return;
        }
        TextureManager textureManager = new TextureManager(modMsb);

        Console.WriteLine($": Found {textureManager.UsedTextures.Count} unique textures used by this msb.\n");

        Console.WriteLine(": Aggregating used mod map textures into pool...");
        textureManager.FillTexturePool(Options.ModMapDirectory, Options.CreateBackups, 
            out long usedTexByteCount, out long unusedTexByteCount, out int texTransferCount);
        totalTexByteCount += usedTexByteCount;
        Console.WriteLine($": Found {texTransferCount} textures, totaling {usedTexByteCount / 1000} KB.");
        Console.WriteLine($": Excluded approximately {unusedTexByteCount / 1000} KB of unused textures.\n");

        Console.WriteLine(": Copying used local dds textures into pool...");
        
        IEnumerable<string> files = Directory.EnumerateFiles(Options.YarrTextureDirectory, "*.dds");
        int transferCount = textureManager.TransferYarrTextures(files);

        Console.WriteLine($": Copied {transferCount} local dds textures to this map.\n");

        if (Options.IncludeRegularTextures)
        {
            Console.WriteLine(": Aggregating used vanilla map textures into pool...");
            textureManager.FillTexturePool(Options.GameMapDirectory, false,
                out usedTexByteCount, out unusedTexByteCount, out texTransferCount);
            Console.WriteLine($": Found {texTransferCount}, totaling {usedTexByteCount / 1000} KB.");
            Console.WriteLine($": Excluded approximately {unusedTexByteCount / 1000} KB of unused textures.\n");
            totalTexByteCount += usedTexByteCount;
        }

        long targetBinderCapacity = totalTexByteCount / 4;
        BXF4 currentOutputBinder = new BXF4();
        long currentByteCount = 0;
        int fileIndex = 0;
        int binderIndex = 0;
        bool isIgnoreSizeCap = false;
        while (textureManager.BinderTexPool.Count >= 0 && binderIndex < 4)
        {
            if (textureManager.BinderTexPool.Count > 0 &&
                (binderIndex == 3 || isIgnoreSizeCap || 
                 currentByteCount + textureManager.BinderTexPool[0].Bytes.Length <= targetBinderCapacity))
            {
                BinderFile nextTex = textureManager.BinderTexPool[0];
                nextTex.ID = fileIndex++;
                currentByteCount += nextTex.Bytes.Length;
                currentOutputBinder.Files.Add(nextTex);
                textureManager.BinderTexPool.RemoveAt(0);
                
                if (binderIndex < 3 && textureManager.BinderTexPool.Count > 1)
                {
                    // keep textures in the same family together even if it goes over the size limit
                    Match nextTexMatch = Regex.Match(nextTex.Name, texNameNoTypeRegex, RegexOptions.IgnoreCase);
                    Match followingTexMatch = Regex.Match(textureManager.BinderTexPool[0].Name, texNameNoTypeRegex, RegexOptions.IgnoreCase);
                    isIgnoreSizeCap = nextTexMatch.Value == followingTexMatch.Value;
                }
            }
            else
            {
                string pathBase = $@"{Options.ModMapDirectory}\m{Options.MapId}\m{Options.MapId}_000{binderIndex}";
                string bhdPath = $"{pathBase}.tpfbhd";
                string bdtPath = $"{pathBase}.tpfbdt";
                if (Options.PatchRegular)
                {
                    bhdPath += ".patch";
                    bdtPath += ".patch";
                }

                long bhdSizeDiff = 0;
                long bdtSizeDiff = 0;
                if (File.Exists(bhdPath)) bhdSizeDiff = new FileInfo(bhdPath).Length;
                if (File.Exists(bdtPath)) bdtSizeDiff = new FileInfo(bdtPath).Length;
                
                currentOutputBinder.Write(bhdPath, bdtPath);
                
                if (bhdSizeDiff > 0) bhdSizeDiff = new FileInfo(bhdPath).Length - bhdSizeDiff;
                if (bdtSizeDiff > 0) bdtSizeDiff = new FileInfo(bdtPath).Length - bdtSizeDiff;
                
                Console.WriteLine($": BXF4 header written to \"{bhdPath}\", Diff: {(bhdSizeDiff > 0 ? "+" : "")}{bhdSizeDiff} Bytes");
                Console.WriteLine($": BXF4 data written to \"{bdtPath}\", Diff: {(bdtSizeDiff > 0 ? "+" : "")}{bdtSizeDiff} Bytes");
                
                currentOutputBinder = new BXF4();
                currentByteCount = 0;
                fileIndex = 0;
                isIgnoreSizeCap = false;
                binderIndex++;
            }
        }

        Console.WriteLine();
        Console.WriteLine(": Texture transfer complete!");
    }
    
    public static bool TryGetMsb(string mapDirectory, out MSB3? msb)
    {
        msb = null;
        
        if (!Directory.Exists(Options.ModMapDirectory)) return false;
        
        string? mapStudioDirectory =
            Directory.GetDirectories(mapDirectory).FirstOrDefault(x => x.EndsWith("MapStudio", StringComparison.OrdinalIgnoreCase));
        if (mapStudioDirectory == null) return false;
        
        string? mapStudioDcx = Directory.GetFiles(mapStudioDirectory)
            .FirstOrDefault(x =>
                Path.GetFileName(x).Equals($"m{Options.MapId}_{Options.BlockId:D2}_00_00.msb.dcx"));
        if (mapStudioDcx == null) return false;

        return MSB3.IsRead(mapStudioDcx, out msb);
    }
}