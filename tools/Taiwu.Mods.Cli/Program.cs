using System.ComponentModel;
using System.CommandLine;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Taiwu.Mods.Cli;

internal static partial class Program
{
    private const string ModNameToken = "__ModName__";
    private const string MarkdownModNameToken = "{{ModName}}";
    private const string DefaultTemplateRelativePath = "templates/mod";
    private const string DefaultModsRelativePath = "mods";
    private const string PluginsDirectoryName = "Plugins";
    private const string SolutionFileName = "Taiwu.Mods.slnx";
    private const string ModsSolutionFolderName = "/mods/";

    public static int Main(string[] args)
    {
        try
        {
            Command command = CommandLineOptions.CreateCommand(Run);
            return command.Parse(args).Invoke(CreateInvocationConfiguration());
        }
        catch (ArgumentException ex)
        {
            return ReportError(ex);
        }
        catch (IOException ex)
        {
            return ReportError(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ReportError(ex);
        }
        catch (InvalidOperationException ex)
        {
            return ReportError(ex);
        }
        catch (Win32Exception ex)
        {
            return ReportError(ex);
        }
    }

    private static InvocationConfiguration CreateInvocationConfiguration()
    {
        return new InvocationConfiguration
        {
            EnableDefaultExceptionHandler = false,
        };
    }

    private static int ReportError(Exception ex)
    {
        Console.Error.WriteLine($"error: {ex.Message}");
        return 1;
    }

    private static void Run(CommandLineOptions options)
    {
        switch (options.Operation)
        {
            case CliOperation.Create:
                Create(options);
                break;
            case CliOperation.Remove:
                Remove(options);
                break;
            case CliOperation.Pack:
                Pack(options);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options));
        }
    }

    private static void Create(CommandLineOptions options)
    {
        ValidateModName(options.ModName);

        string repoRoot = Path.GetFullPath(options.RepoRoot);
        string templateRoot = Path.Combine(repoRoot, DefaultTemplateRelativePath);
        string modsRoot = Path.GetFullPath(options.ModsRoot ?? Path.Combine(repoRoot, DefaultModsRelativePath));
        string modRoot = Path.Combine(modsRoot, options.ModName);

        if (!Directory.Exists(templateRoot))
        {
            throw new DirectoryNotFoundException($"Template directory does not exist: {templateRoot}");
        }

        if (Directory.Exists(modRoot) && !options.Force)
        {
            throw new InvalidOperationException($"Mod directory already exists: {modRoot}. Pass --force to overwrite template files.");
        }

        CopyTemplate(templateRoot, modRoot, options.ModName, options.Force);

        if (!options.SkipSolution && IsUnderDirectory(modRoot, repoRoot))
        {
            AddProjectsToSolution(repoRoot, options.ModName);
        }

        Console.WriteLine($"Created mod '{options.ModName}' at {modRoot}");
    }

    private static void CopyTemplate(string templateRoot, string modRoot, string modName, bool force)
    {
        foreach (string templateFile in Directory.EnumerateFiles(templateRoot, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(templateRoot, templateFile);
            string destinationRelativePath = ReplaceTokens(relativePath, modName);
            string destinationPath = Path.Combine(modRoot, destinationRelativePath);

            if (File.Exists(destinationPath) && !force)
            {
                throw new IOException($"Destination file already exists: {destinationPath}");
            }

            string? destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                _ = Directory.CreateDirectory(destinationDirectory);
            }

            if (ShouldReplaceTokens(templateFile))
            {
                string content = File.ReadAllText(templateFile, Encoding.UTF8);
                File.WriteAllText(destinationPath, ReplaceTokens(content, modName), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                continue;
            }

            File.Copy(templateFile, destinationPath, overwrite: force);
        }
    }

    private static void AddProjectsToSolution(string repoRoot, string modName)
    {
        string solutionPath = Path.Combine(repoRoot, SolutionFileName);
        if (!File.Exists(solutionPath))
        {
            throw new FileNotFoundException($"Solution file does not exist: {solutionPath}");
        }

        string[] projectPaths = GetModProjectPaths(modName);
        RunDotnet(repoRoot, "sln", SolutionFileName, "add", projectPaths[0], projectPaths[1]);
    }

    private static void Remove(CommandLineOptions options)
    {
        ValidateModName(options.ModName);

        string repoRoot = Path.GetFullPath(options.RepoRoot);
        string solutionPath = Path.Combine(repoRoot, SolutionFileName);
        if (!File.Exists(solutionPath))
        {
            throw new FileNotFoundException($"Solution file does not exist: {solutionPath}");
        }

        string[] projectPaths = GetModProjectPaths(options.ModName);
        List<string> existingProjectPaths = [];
        foreach (string projectPath in projectPaths)
        {
            if (File.Exists(Path.Combine(repoRoot, projectPath)))
            {
                existingProjectPaths.Add(projectPath);
            }
            else
            {
                Console.WriteLine($"Skipped missing project file: {projectPath}");
            }
        }

        if (existingProjectPaths.Count == 0)
        {
            Console.WriteLine($"No solution projects found for mod '{options.ModName}'.");
            return;
        }

        RunDotnet(repoRoot, ["sln", SolutionFileName, "remove", .. existingProjectPaths]);
        EnsureSolutionFolder(repoRoot, ModsSolutionFolderName);
        Console.WriteLine($"Removed mod '{options.ModName}' projects from {SolutionFileName}. Files were not deleted.");
    }

    private static void Pack(CommandLineOptions options)
    {
        ValidateModName(options.ModName);

        string repoRoot = Path.GetFullPath(options.RepoRoot);
        string modsRoot = Path.GetFullPath(options.ModsRoot ?? Path.Combine(repoRoot, DefaultModsRelativePath));
        string artifactsRoot = Path.GetFullPath(options.ArtifactsRoot ?? Path.Combine(repoRoot, "artifacts", "mods"));
        string modRoot = Path.Combine(modsRoot, options.ModName);
        string packageRoot = Path.Combine(artifactsRoot, options.ModName);

        if (!Directory.Exists(modRoot))
        {
            throw new DirectoryNotFoundException($"Mod directory does not exist: {modRoot}");
        }

        string[] fullProjectPaths = GetModProjectFullPaths(modsRoot, options.ModName);
        foreach (string fullProjectPath in fullProjectPaths)
        {
            if (!File.Exists(fullProjectPath))
            {
                throw new FileNotFoundException($"Mod project does not exist: {fullProjectPath}");
            }
        }

        foreach (string fullProjectPath in fullProjectPaths)
        {
            RunDotnet(repoRoot, "build", fullProjectPath, "--configuration", options.Configuration);
        }

        if (Directory.Exists(packageRoot))
        {
            Directory.Delete(packageRoot, recursive: true);
        }

        CopyPackageFiles(modRoot, packageRoot);
        CopyPluginOutputs(repoRoot, fullProjectPaths, options.Configuration, packageRoot);
        Console.WriteLine($"Packed mod '{options.ModName}' to {packageRoot}");
    }

    private static void CopyPackageFiles(string modRoot, string packageRoot)
    {
        foreach (string sourcePath in Directory.EnumerateFiles(modRoot, "*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(modRoot, sourcePath);
            if (ShouldExcludeFromPackage(relativePath))
            {
                continue;
            }

            string destinationPath = Path.Combine(packageRoot, relativePath);
            string? destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                _ = Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourcePath, destinationPath, overwrite: true);
        }
    }

    private static bool ShouldExcludeFromPackage(string relativePath)
    {
        string normalizedPath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
        string fileName = Path.GetFileName(normalizedPath);
        return normalizedPath.StartsWith("src/", StringComparison.Ordinal)
            || normalizedPath.StartsWith($"{PluginsDirectoryName}/", StringComparison.Ordinal)
            || normalizedPath.StartsWith("bin/", StringComparison.Ordinal)
            || normalizedPath.StartsWith("obj/", StringComparison.Ordinal)
            || normalizedPath.Contains("/bin/", StringComparison.Ordinal)
            || normalizedPath.Contains("/obj/", StringComparison.Ordinal)
            || fileName is ".gitignore" or ".gitkeep" or "README.md";
    }

    private static void CopyPluginOutputs(string repoRoot, IEnumerable<string> projectPaths, string configuration, string packageRoot)
    {
        foreach (string projectPath in projectPaths)
        {
            string outputPath = GetProjectTargetDirectory(repoRoot, projectPath, configuration);
            if (!Directory.Exists(outputPath))
            {
                throw new DirectoryNotFoundException($"Project output directory does not exist: {outputPath}");
            }

            foreach (string sourcePath in Directory.EnumerateFiles(outputPath))
            {
                string extension = Path.GetExtension(sourcePath);
                if (!PackagePluginOutputExtensions.Contains(extension))
                {
                    continue;
                }

                string destinationPath = Path.Combine(packageRoot, PluginsDirectoryName, Path.GetFileName(sourcePath));
                string? destinationDirectory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    _ = Directory.CreateDirectory(destinationDirectory);
                }

                File.Copy(sourcePath, destinationPath, overwrite: true);
            }
        }
    }

    private static string GetProjectTargetDirectory(string repoRoot, string projectPath, string configuration)
    {
        string targetDirectory = RunDotnetForOutput(
            repoRoot,
            "msbuild",
            projectPath,
            "-getProperty:TargetDir",
            $"-p:Configuration={configuration}");

        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new InvalidOperationException($"Project TargetDir is empty: {projectPath}");
        }

        string projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Project path has no directory: {projectPath}");

        return Path.GetFullPath(targetDirectory, projectDirectory);
    }

    private static void EnsureSolutionFolder(string repoRoot, string folderName)
    {
        string solutionPath = Path.Combine(repoRoot, SolutionFileName);
        XDocument document = XDocument.Load(solutionPath);
        XElement root = document.Root ?? throw new InvalidOperationException($"Solution file has no root element: {solutionPath}");

        if (root.Elements("Folder").Any(element => string.Equals((string?)element.Attribute("Name"), folderName, StringComparison.Ordinal)))
        {
            return;
        }

        XElement folder = new("Folder", new XAttribute("Name", folderName));
        XElement? insertBefore = root.Elements("Folder").FirstOrDefault(element =>
            string.CompareOrdinal((string?)element.Attribute("Name"), folderName) > 0);

        if (insertBefore is null)
        {
            root.Add(folder);
        }
        else
        {
            insertBefore.AddBeforeSelf(folder);
        }

        File.WriteAllText(solutionPath, $"{document}{Environment.NewLine}", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static bool ShouldReplaceTokens(string templateFile)
    {
        return TextTemplateExtensions.Contains(Path.GetExtension(templateFile));
    }

    private static string[] GetModProjectPaths(string modName)
    {
        return
        [
            $"{DefaultModsRelativePath}/{modName}/src/Frontend/{modName}.Frontend.csproj",
            $"{DefaultModsRelativePath}/{modName}/src/Backend/{modName}.Backend.csproj",
        ];
    }

    private static string[] GetModProjectFullPaths(string modsRoot, string modName)
    {
        return
        [
            Path.Combine(modsRoot, modName, "src", "Frontend", $"{modName}.Frontend.csproj"),
            Path.Combine(modsRoot, modName, "src", "Backend", $"{modName}.Backend.csproj"),
        ];
    }

    private static void RunDotnet(string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet.");

        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(standardOutput))
        {
            Console.Write(standardOutput);
        }

        if (!string.IsNullOrWhiteSpace(standardError))
        {
            Console.Error.Write(standardError);
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet {string.Join(' ', arguments)} failed with exit code {process.ExitCode}.");
        }
    }

    private static string RunDotnetForOutput(string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet.");

        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(standardError))
        {
            Console.Error.Write(standardError);
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet {string.Join(' ', arguments)} failed with exit code {process.ExitCode}.");
        }

        return standardOutput.Trim();
    }

    private static string ReplaceTokens(string value, string modName)
    {
        return value
            .Replace(ModNameToken, modName, StringComparison.Ordinal)
            .Replace(MarkdownModNameToken, modName, StringComparison.Ordinal);
    }

    private static void ValidateModName(string modName)
    {
        if (string.IsNullOrWhiteSpace(modName))
        {
            throw new ArgumentException("Mod name cannot be empty.");
        }

        if (!ModNameRegex().IsMatch(modName))
        {
            throw new ArgumentException("ModName must be a C# namespace-style identifier, for example MyMod or MyCompany.MyMod.");
        }

        foreach (string segment in modName.Split('.'))
        {
            if (CSharpKeywords.Contains(segment))
            {
                throw new ArgumentException($"ModName segment '{segment}' is a C# keyword.");
            }
        }
    }

    private static bool IsUnderDirectory(string path, string directory)
    {
        string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$")]
    private static partial Regex ModNameRegex();

    private static readonly HashSet<string> TextTemplateExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".config",
        ".cs",
        ".csproj",
        ".json",
        ".lua",
        ".md",
        ".proj",
        ".props",
        ".slnx",
        ".toml",
        ".txt",
        ".xml",
        ".yaml",
        ".yml",
    };

    private static readonly HashSet<string> PackagePluginOutputExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dll",
        ".json",
    };

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract",
        "as",
        "base",
        "bool",
        "break",
        "byte",
        "case",
        "catch",
        "char",
        "checked",
        "class",
        "const",
        "continue",
        "decimal",
        "default",
        "delegate",
        "do",
        "double",
        "else",
        "enum",
        "event",
        "explicit",
        "extern",
        "false",
        "finally",
        "fixed",
        "float",
        "for",
        "foreach",
        "goto",
        "if",
        "implicit",
        "in",
        "int",
        "interface",
        "internal",
        "is",
        "lock",
        "long",
        "namespace",
        "new",
        "null",
        "object",
        "operator",
        "out",
        "override",
        "params",
        "private",
        "protected",
        "public",
        "readonly",
        "ref",
        "return",
        "sbyte",
        "sealed",
        "short",
        "sizeof",
        "stackalloc",
        "static",
        "string",
        "struct",
        "switch",
        "this",
        "throw",
        "true",
        "try",
        "typeof",
        "uint",
        "ulong",
        "unchecked",
        "unsafe",
        "ushort",
        "using",
        "virtual",
        "void",
        "volatile",
        "while",
    };
}
