﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SonicOrca.Resources;
using SonicOrca.HelperLibraries.Png;
using SonicOrca.Graphics;

class Program
{
    private static ResourceTree ResourceTree { get; } = new ResourceTree();

    static void Main(string[] args)
    {
        InitializeResourceTypes();

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
                    {
                        if (!Directory.Exists(filePath))
                        {
                            Console.WriteLine($"Directory {filePath} does not exist!");
                            return;
                        }

                        string outputFile = Path.Combine(
                            Path.GetDirectoryName(filePath),
                            Path.GetFileName(filePath.TrimEnd('_', 'u', 'n', 'p', 'a', 'c', 'k', 'e', 'd')) + ".dat"
                        );

                        Console.WriteLine($"Repacking {filePath} to {outputFile}...");
                        RepackDirectory(filePath, outputFile);
                        break;
                    }
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

    private static void RepackDirectory(string inputDirectory, string outputDatFile)
    {
        var tree = new ResourceTree();

        var files = Directory.GetFiles(inputDirectory, "*.*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            try
            {
                string relativePath = Path.GetRelativePath(inputDirectory, file)
                                        .Replace('\\', '/');

                string resourceKey = Path.Combine(
                    Path.GetDirectoryName(relativePath),
                    Path.GetFileNameWithoutExtension(relativePath)
                ).Replace('\\', '/');

                var resourceType = GetResourceTypeFromExtension(Path.GetExtension(file));
                if (resourceType == ResourceTypeIdentifier.Unknown)
                {
                    Console.WriteLine($"Skipping {file}: Unknown file type");
                    continue;
                }

                var source = new FileResourceSource(file, 0, new FileInfo(file).Length);
                var resource = new Resource(resourceKey, resourceType, source);

                tree.SetOrAdd(resourceKey, resource);

                Console.WriteLine($"Added: {resourceKey} as {resourceType}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to add {file}: {ex.Message}");
            }
        }

        new ResourceFile(outputDatFile).Write(tree);
        Console.WriteLine($"Successfully created {outputDatFile}");
    }

    private static ResourceTypeIdentifier GetResourceTypeFromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".xml" => ResourceTypeIdentifier.Xml,
            ".bmp" => ResourceTypeIdentifier.TextureBMP,
            ".gif" => ResourceTypeIdentifier.TextureGIF,
            ".jpg" or ".jpeg" => ResourceTypeIdentifier.TextureJPG,
            ".png" => ResourceTypeIdentifier.TexturePNG,
            ".sinfo" => ResourceTypeIdentifier.SampleInfo,
            ".wav" => ResourceTypeIdentifier.SampleWAV,
            ".mp3" => ResourceTypeIdentifier.SampleMP3,
            ".ogg" => ResourceTypeIdentifier.SampleOGG,
            ".font" => ResourceTypeIdentifier.Font,
            ".mp4" or ".h264" => ResourceTypeIdentifier.VideoH264,
            ".anim" => ResourceTypeIdentifier.AnimationGroup,
            ".comp" => ResourceTypeIdentifier.CompositionGroup,
            ".film" => ResourceTypeIdentifier.FilmGroup,
            ".area" => ResourceTypeIdentifier.Area,
            ".tiles" => ResourceTypeIdentifier.TileSet,
            ".map" => ResourceTypeIdentifier.Map,
            ".obj" => ResourceTypeIdentifier.Object,
            ".bind" => ResourceTypeIdentifier.Binding,
            ".lvldep" => ResourceTypeIdentifier.LevelDependencies,
            ".input" => ResourceTypeIdentifier.InputRecording,
            _ => ResourceTypeIdentifier.Unknown
        };
    }

    private static void InitializeResourceTypes()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Console.WriteLine($"- {assembly.FullName}");
        }
        Console.WriteLine();

        try
        {
            var sonicOrcaAssembly = Assembly.Load("SonicOrca");
            Console.WriteLine($"Successfully loaded SonicOrca assembly: {sonicOrcaAssembly.FullName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load SonicOrca assembly: {ex.Message}");
        }

        var registeredTypes = ResourceType.RegisteredResourceTypes.ToList();
        Console.WriteLine("\nRegistered resource types:");
        foreach (var type in registeredTypes)
        {
            Console.WriteLine($"- {type.Name} ({type.DefaultExtension}) => {type.Identifier}");
        }

        var pngType = registeredTypes.FirstOrDefault(t => t.Identifier == ResourceTypeIdentifier.TexturePNG);
        if (pngType == null)
        {
            throw new InvalidOperationException("PNG resource type is not registered. Make sure SonicOrca.dll is properly referenced and copied to the output directory.");
        }
        Console.WriteLine($"\nPNG Resource Type is registered: {pngType.Name} ({pngType.DefaultExtension})\n");
    }
}