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
            Console.WriteLine("Usage: OrcaTools <command> <sonicorca.dat>\nAvailable Commands:\nunpack - unpacks the sonicorca.dat file\nrepack - repacks the sonicorca.dat file\nload -  loads the sonicorca.dat file (MAINLY FOR TESTING LOL)\ncreate - creates .dat files from directories");
            return;
        }

        string command = args[0];
        string filePath = args[1];

        try
        {
            switch (command.ToLower())
            {
                case "unpack":
                    Console.WriteLine("unpacking shit not done");
                    break;
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
                case "create":
                {
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Usage: OrcaTools create <input_directory> <output_directory>");
                        return;
                    }
                    CreateDataResourceFiles(args[1], args[2]);
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
}
