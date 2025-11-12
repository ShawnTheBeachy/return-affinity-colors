using System.CommandLine;
using ReturnColors.Commands;

var rootCommand = new RootCommand("Application for customizing Affinity.");
rootCommand.Subcommands.Add(CheckCommand.Command);
rootCommand.Subcommands.Add(ColorizeIconsCommand.Command);
rootCommand.Subcommands.Add(DumpCommand.Command);
rootCommand.Subcommands.Add(ReplaceSplashImageCommand.Command);
await rootCommand.Parse(args).InvokeAsync();
