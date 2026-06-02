using System.CommandLine;
using System.CommandLine.Help;

namespace Taiwu.Mods.Cli;

internal sealed class CommandLineOptions
{
    private CommandLineOptions()
    {
    }

    public required string ModName { get; init; }

    public CliOperation Operation { get; init; } = CliOperation.Create;

    public string RepoRoot { get; init; } = Directory.GetCurrentDirectory();

    public string? ModsRoot { get; init; }

    public string? ArtifactsRoot { get; init; }

    public string Configuration { get; init; } = "Release";

    public bool Force { get; init; }

    public bool SkipSolution { get; init; }

    public static Command CreateCommand(Action<CommandLineOptions> run)
    {
        Command command = new("Taiwu.Mods.Cli", "太吾 mod 仓库维护命令。");

        command.Options.Add(new HelpOption { Recursive = true });
        command.Subcommands.Add(CreateCreateCommand(run));
        command.Subcommands.Add(CreateRemoveCommand(run));
        command.Subcommands.Add(CreatePackCommand(run));

        return command;
    }

    private static Command CreateCreateCommand(Action<CommandLineOptions> run)
    {
        Option<string> nameOption = CreateNameOption();
        Option<string> repoRootOption = CreateRepoRootOption();
        Option<string?> modsRootOption = CreateModsRootOption();
        Option<bool> forceOption = CreateForceOption();
        Option<bool> skipSolutionOption = CreateSkipSolutionOption();
        Command command = new("create", "复制 mod 模板并把项目注册到解决方案。");

        command.Options.Add(nameOption);
        command.Options.Add(repoRootOption);
        command.Options.Add(modsRootOption);
        command.Options.Add(forceOption);
        command.Options.Add(skipSolutionOption);
        command.SetAction(parseResult =>
            run(
                new CommandLineOptions
                {
                    ModName = parseResult.GetRequiredValue(nameOption),
                    Operation = CliOperation.Create,
                    RepoRoot = parseResult.GetRequiredValue(repoRootOption),
                    ModsRoot = parseResult.GetValue(modsRootOption),
                    Force = parseResult.GetValue(forceOption),
                    SkipSolution = parseResult.GetValue(skipSolutionOption),
                }));

        return command;
    }

    private static Command CreateRemoveCommand(Action<CommandLineOptions> run)
    {
        Option<string> nameOption = CreateNameOption();
        Option<string> repoRootOption = CreateRepoRootOption();
        Command command = new("remove", "从解决方案取消注册指定 mod，保留文件。");

        command.Options.Add(nameOption);
        command.Options.Add(repoRootOption);
        command.SetAction(parseResult =>
            run(
                new CommandLineOptions
                {
                    ModName = parseResult.GetRequiredValue(nameOption),
                    Operation = CliOperation.Remove,
                    RepoRoot = parseResult.GetRequiredValue(repoRootOption),
                }));

        return command;
    }

    private static Command CreatePackCommand(Action<CommandLineOptions> run)
    {
        Option<string> nameOption = CreateNameOption();
        Option<string> repoRootOption = CreateRepoRootOption();
        Option<string?> modsRootOption = CreateModsRootOption();
        Option<string?> artifactsRootOption = CreateArtifactsRootOption();
        Option<string> configurationOption = CreateConfigurationOption();
        Command command = new("pack", "构建并组装指定 mod 的可部署目录。");

        command.Options.Add(nameOption);
        command.Options.Add(repoRootOption);
        command.Options.Add(modsRootOption);
        command.Options.Add(artifactsRootOption);
        command.Options.Add(configurationOption);
        command.SetAction(parseResult =>
            run(
                new CommandLineOptions
                {
                    ModName = parseResult.GetRequiredValue(nameOption),
                    Operation = CliOperation.Pack,
                    RepoRoot = parseResult.GetRequiredValue(repoRootOption),
                    ModsRoot = parseResult.GetValue(modsRootOption),
                    ArtifactsRoot = parseResult.GetValue(artifactsRootOption),
                    Configuration = parseResult.GetRequiredValue(configurationOption),
                }));

        return command;
    }

    private static Option<string> CreateNameOption()
    {
        return new Option<string>("--name")
        {
            Description = "Mod 名称。",
            HelpName = "ModName",
            Required = true,
        };
    }

    private static Option<string> CreateRepoRootOption()
    {
        return new Option<string>("--repo-root")
        {
            Description = "仓库根目录。",
            HelpName = "path",
            DefaultValueFactory = _ => Directory.GetCurrentDirectory(),
        };
    }

    private static Option<string?> CreateModsRootOption()
    {
        return new Option<string?>("--mods-root")
        {
            Description = "mods 目录；不传时使用仓库根目录下的 mods。",
            HelpName = "path",
        };
    }

    private static Option<string?> CreateArtifactsRootOption()
    {
        return new Option<string?>("--artifacts-root")
        {
            Description = "打包输出根目录；不传时使用仓库根目录下的 artifacts/mods。",
            HelpName = "path",
        };
    }

    private static Option<string> CreateConfigurationOption()
    {
        return new Option<string>("--configuration")
        {
            Description = "打包时使用的构建配置。",
            HelpName = "configuration",
            DefaultValueFactory = _ => "Release",
        };
    }

    private static Option<bool> CreateForceOption()
    {
        return new Option<bool>("--force")
        {
            Description = "覆盖已存在的模板文件。",
        };
    }

    private static Option<bool> CreateSkipSolutionOption()
    {
        return new Option<bool>("--skip-solution")
        {
            Description = "不把新建 mod 项目加入解决方案。",
        };
    }
}

internal enum CliOperation
{
    Create = 0,
    Remove = 1,
    Pack = 2,
}
