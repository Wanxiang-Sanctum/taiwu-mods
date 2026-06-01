namespace Taiwu.Mods.ModCreator;

internal sealed class ModCreatorOptions
{
    private ModCreatorOptions()
    {
    }

    public required string ModName { get; init; }

    public ModCreatorOperation Operation { get; init; } = ModCreatorOperation.Create;

    public string RepoRoot { get; init; } = Directory.GetCurrentDirectory();

    public string? ModsRoot { get; init; }

    public string? ArtifactsRoot { get; init; }

    public string Configuration { get; init; } = "Release";

    public bool Force { get; init; }

    public bool SkipSolution { get; init; }

    public static ModCreatorOptions Parse(IReadOnlyList<string> args)
    {
        string? modName = null;
        string repoRoot = Directory.GetCurrentDirectory();
        string? modsRoot = null;
        string? artifactsRoot = null;
        string configuration = "Release";
        ModCreatorOperation operation = ModCreatorOperation.Create;
        bool force = false;
        bool skipSolution = false;

        for (int index = 0; index < args.Count; index++)
        {
            string arg = args[index];
            switch (arg)
            {
                case "--name":
                    modName = ReadValue(args, ref index, arg);
                    break;
                case "--operation":
                    operation = ReadOperation(args, ref index, arg);
                    break;
                case "--repo-root":
                    repoRoot = ReadValue(args, ref index, arg);
                    break;
                case "--mods-root":
                    modsRoot = ReadValue(args, ref index, arg);
                    break;
                case "--artifacts-root":
                    artifactsRoot = ReadValue(args, ref index, arg);
                    break;
                case "--configuration":
                    configuration = ReadValue(args, ref index, arg);
                    break;
                case "--force":
                    force = ReadOptionalBooleanValue(args, ref index, arg);
                    break;
                case "--skip-solution":
                    skipSolution = ReadOptionalBooleanValue(args, ref index, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        if (string.IsNullOrWhiteSpace(modName))
        {
            throw new ArgumentException("Missing required argument: --name <ModName>.");
        }

        return new ModCreatorOptions
        {
            ModName = modName,
            Operation = operation,
            RepoRoot = repoRoot,
            ModsRoot = modsRoot,
            ArtifactsRoot = artifactsRoot,
            Configuration = configuration,
            Force = force,
            SkipSolution = skipSolution,
        };
    }

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string optionName)
    {
        if (index + 1 >= args.Count)
        {
            throw new ArgumentException($"Missing value for {optionName}.");
        }

        index++;
        string value = args[index];
        if (value.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Missing value for {optionName}.");
        }

        return value;
    }

    private static ModCreatorOperation ReadOperation(IReadOnlyList<string> args, ref int index, string optionName)
    {
        string value = ReadValue(args, ref index, optionName);
        return value.ToUpperInvariant() switch
        {
            "CREATE" => ModCreatorOperation.Create,
            "REMOVE" => ModCreatorOperation.Remove,
            "PACK" => ModCreatorOperation.Pack,
            _ => throw new ArgumentException($"Invalid operation for {optionName}: {value}"),
        };
    }

    private static bool ReadOptionalBooleanValue(IReadOnlyList<string> args, ref int index, string optionName)
    {
        if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            return true;
        }

        index++;
        string value = args[index];
        if (bool.TryParse(value, out bool result))
        {
            return result;
        }

        throw new ArgumentException($"Invalid boolean value for {optionName}: {value}");
    }
}

internal enum ModCreatorOperation
{
    Create = 0,
    Remove = 1,
    Pack = 2,
}
