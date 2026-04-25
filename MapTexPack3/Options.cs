using System.Text.RegularExpressions;

namespace MapTexPack3;

public static class Options
{ 
    public static int MapId { get; private set; }
    public static int BlockId { get; private set; }
    public static string WorkingDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory;
    public static string ModMapDirectory { get; private set; }
    public static string ModObjectDirectory { get; private set; }
    public static string GameMapDirectory { get; private set; }
    public static string GameObjectDirectory { get; private set; }
    public static string YarrTextureDirectory { get; private set; }
    public static bool PatchRegular { get; private set; }
    public static bool IncludeRegularTextures { get; private set; }
    public static bool CreateBackups { get; private set; }

    public static bool ProcessOptions()
    {
        string configPath = WorkingDirectory + @"\config.ini";
        if (!File.Exists(configPath))
        {
            Console.WriteLine("CRITICAL: Couldn't find config.ini file in exe folder. Aborting.");
            return false;
        }
        
        using (TextReader reader = new StringReader(File.ReadAllText(configPath)))
        {
            reader.ReadLine();
            string gameDirectory = reader.ReadLine();
            gameDirectory = gameDirectory[(gameDirectory.IndexOf('=') + 1)..];
            GameMapDirectory = $"{gameDirectory}\\map";
            GameObjectDirectory = $"{gameDirectory}\\obj";
            reader.ReadLine();
            string modDirectory = reader.ReadLine();
            modDirectory = modDirectory[(modDirectory.IndexOf('=') + 1)..];
            ModMapDirectory = $"{modDirectory}\\map";
            ModObjectDirectory = $"{modDirectory}\\obj";
            reader.ReadLine();
            YarrTextureDirectory = reader.ReadLine();
            YarrTextureDirectory = YarrTextureDirectory[(YarrTextureDirectory.IndexOf('=') + 1)..];
        }

        Console.WriteLine("Enter the map to transfer textures to (ex. 30_0):");
        string? input = Console.ReadLine();
        if (input == null)
        {
            Console.WriteLine("CRITICAL: No map and block id given. Aborting.");
            return false;
        }
        
        Match m = Regex.Match(input, @"([0-9]{2})_([0-9])");
        if (m.Success)
        {
            MapId = int.Parse(m.Groups[1].Value);
            BlockId = int.Parse(m.Groups[2].Value);
        }
        else
        {
            Console.WriteLine("CRITICAL: Map and block Id were entered incorrectly. Aborting.");
            return false;
        }

        PatchRegular = YesNoQuestion(
            $"Do you want to use the .patch extension for textures? (use if this map relies heavily on the vanilla m{MapId} bdt/bhd textures)",
            $"The .patch extension will be used for m{MapId}_000Xs.",
            $"The .patch extension will not be used for m{MapId}_000Xs.");
        if (!PatchRegular)
        {
            IncludeRegularTextures = YesNoQuestion(
                $"Do you want used textures to be included from the vanilla m{MapId} texture binders?",
                $"Used vanilla textures will be included in m{MapId}_000Xs.",
                $"Used vanilla textures will not be included in m{MapId}_000Xs.");
        }

        CreateBackups = YesNoQuestion(
            "Should backups be created?",
            "Backups will be created.",
            "Backups will not be created.");

        return true;
    }

    private static bool YesNoQuestion(string question, string resultYes, string resultNo)
    {
        Console.WriteLine(question);
        Console.Write("(y/n): ");
        ConsoleKeyInfo keyInfo = Console.ReadKey();
        while (keyInfo.KeyChar != 'y' && keyInfo.KeyChar != 'n')
        {
            Console.WriteLine("\nInvalid key entered. Try again.");
            Console.Write("(y/n): ");
            keyInfo = Console.ReadKey();
        }
        bool result = keyInfo.KeyChar == 'y';
        Console.WriteLine();
        //Console.WriteLine(result ? resultYes : resultNo);
        return result;
    }
}