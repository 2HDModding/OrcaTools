using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SonicOrca.Resources;

class Program {
    private static ResourceTree ResourceTree { get; } = new ResourceTree();

    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: OrcaTools <command> <sonicorca.dat>\nAvailable Commands:\nunpack - unpacks the sonicorca.dat file\nrepack - repacks the sonicorca.dat file\nload -  loads the sonicorca.dat file (MAINLY FOR TESTING LOL)");
            return;
        }

        string command = args[0];
        string filePath = args[1];

        try
        {
            switch (command.ToLower())
            {
                case "unpack":
                {
                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine($"File {filePath} does not exist!");
                        return;
                    }
                    
                    string outputDir = Path.Combine(
                        Path.GetDirectoryName(filePath),
                        Path.GetFileNameWithoutExtension(filePath) + "_unpacked"
                    );
                    
                    Console.WriteLine($"Unpacking {filePath} to {outputDir}...");
                    UnpackResourceFile(filePath, outputDir);
                    break;
                }
                case "repack":
                    Console.WriteLine("repacking shit not done");
                    break;
                case "load":
                {
                    string directoryName = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    LoadResourceFiles(Path.Combine(directoryName, "data"));
                    TraceResourceTree(ResourceTree);
                    break;
                }    
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private static void LoadResources(string path)
    {
        foreach (string file in Directory.GetFiles(path, "*.dat", SearchOption.AllDirectories))
            ResourceTree.MergeWith(new ResourceFile(file).Scan());
    }

    private static void LoadResourceFiles(string inputDirectory)
    {
        if (!Directory.Exists(inputDirectory))
            return;
        foreach (string file in Directory.GetFiles(inputDirectory))
            ResourceTree.MergeWith(new ResourceFile(file).Scan());
    }

    private static void CreateDataResourceFiles(string inputDirectory, string outputDirectory)
    {
        if (!Directory.Exists(inputDirectory))
            return;
        
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);
        
        foreach (string directory in Directory.GetDirectories(inputDirectory))
        {
            string path2 = Path.GetFileName(directory) + ".dat";
            string path = Path.Combine(directory, "sonicorca");
            ResourceTree tree = new ResourceTree();
            ResourceFile.GetResourcesFromDirectory(tree, path);
            new ResourceFile(Path.Combine(outputDirectory, path2)).Write(tree);
        }
    }

    private static void TraceResourceTree(ResourceTree tree, string indent = "")
    {
        var resources = tree.GetResourceListing();
        var nodes = tree.GetNodeListing();

        foreach (var resource in resources)
        {
            Console.WriteLine($"{indent}Resource: {resource.Key} => {resource.Value}");
        }

        foreach (var node in nodes)
        {
            if (node.Key != string.Empty)
            {
                if (node.Value.Resource == null)
                {
                    Console.WriteLine($"{indent}Directory: {node.Key}");
                }
            }
        }
    }

    private static void UnpackResourceFile(string datFile, string outputDirectory)
    {
        var resourceFile = new ResourceFile(datFile);
        var tree = resourceFile.Scan();
        
        Directory.CreateDirectory(outputDirectory);
        
        var resources = tree.GetResourceListing();
        
        foreach (var resource in resources)
        {
            try
            {
                string resourcePath = Path.Combine(outputDirectory, resource.Key);
                string resourceDir = Path.GetDirectoryName(resourcePath);
                
                if (!Directory.Exists(resourceDir))
                {
                    Directory.CreateDirectory(resourceDir);
                }

                string extension = GetResourceExtension(resource.Value.Identifier);
                resourcePath = Path.ChangeExtension(resourcePath, extension);

                resource.Value.Export(resourcePath);

                Console.WriteLine($"Extracted: {resource.Key} -> {resourcePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to extract {resource.Key}: {ex.Message}");
            }
        }
    }

    private static string GetResourceExtension(ResourceTypeIdentifier identifier)
    {
        return identifier switch
        {
            ResourceTypeIdentifier.Xml => ".xml",
            ResourceTypeIdentifier.TextureBMP => ".bmp",
            ResourceTypeIdentifier.TextureGIF => ".gif",
            ResourceTypeIdentifier.TextureJPG => ".jpg",
            ResourceTypeIdentifier.TexturePNG => ".png",
            ResourceTypeIdentifier.SampleInfo => ".sinfo",
            ResourceTypeIdentifier.SampleWAV => ".wav",
            ResourceTypeIdentifier.SampleMP3 => ".mp3",
            ResourceTypeIdentifier.SampleOGG => ".ogg",
            ResourceTypeIdentifier.Font => ".font",
            ResourceTypeIdentifier.VideoH264 => ".mp4",
            ResourceTypeIdentifier.AnimationGroup => ".anim",
            ResourceTypeIdentifier.CompositionGroup => ".comp",
            ResourceTypeIdentifier.FilmGroup => ".film",
            ResourceTypeIdentifier.Area => ".area",
            ResourceTypeIdentifier.TileSet => ".tiles",
            ResourceTypeIdentifier.Map => ".map",
            ResourceTypeIdentifier.Object => ".obj",
            ResourceTypeIdentifier.Binding => ".bind",
            ResourceTypeIdentifier.LevelDependencies => ".lvldep",
            ResourceTypeIdentifier.InputRecording => ".input",
            ResourceTypeIdentifier.Null => ".null",
            ResourceTypeIdentifier.Unknown => ".unknown",
            _ => ".bin"  // uhh in case of unknown files
        };
    }
}       