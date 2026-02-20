using NodeModuleCleaner.Models;
using NodeModuleCleaner.Services;
using Spectre.Console;
using System.CommandLine;

namespace NodeModuleCleaner.Commands;

public static class ConfigCommand
{
    public static Command Create()
    {
        var command = new Command("config", "管理目標資料夾設定");
        command.Subcommands.Add(CreateListCommand());
        command.Subcommands.Add(CreateAddCommand());
        command.Subcommands.Add(CreateRemoveCommand());
        command.Subcommands.Add(CreateResetCommand());
        return command;
    }

    private static Command CreateListCommand()
    {
        var cmd = new Command("list", "顯示目前設定的目標資料夾");
        cmd.SetAction(_ =>
        {
            var service = new ConfigService();
            var config = service.Load();
            AnsiConsole.MarkupLine($"[dim]Config: {Markup.Escape(service.ConfigPath)}[/]");
            AnsiConsole.MarkupLine("[bold]目標資料夾：[/]");
            foreach (var t in config.Targets)
                AnsiConsole.MarkupLine($"  [cyan]{Markup.Escape(t)}[/]");
        });
        return cmd;
    }

    private static Command CreateAddCommand()
    {
        var cmd = new Command("add", "新增目標資料夾");
        var namesArg = new Argument<string[]>("names") { Description = "資料夾名稱（可多個）" };
        namesArg.Arity = ArgumentArity.OneOrMore;
        cmd.Arguments.Add(namesArg);
        cmd.SetAction(parseResult =>
        {
            var names = parseResult.GetValue(namesArg)!;
            var service = new ConfigService();
            var config = service.Load();
            var added = new List<string>();
            foreach (var name in names)
            {
                if (!config.Targets.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    config.Targets.Add(name);
                    added.Add(name);
                }
            }
            service.Save(config);
            if (added.Count > 0)
                AnsiConsole.MarkupLine($"[green]✓[/] 已新增: {Markup.Escape(string.Join(", ", added))}");
            else
                AnsiConsole.MarkupLine("[yellow]已存在，無需新增[/]");
        });
        return cmd;
    }

    private static Command CreateRemoveCommand()
    {
        var cmd = new Command("remove", "移除目標資料夾");
        var namesArg = new Argument<string[]>("names") { Description = "資料夾名稱（可多個）" };
        namesArg.Arity = ArgumentArity.OneOrMore;
        cmd.Arguments.Add(namesArg);
        cmd.SetAction(parseResult =>
        {
            var names = parseResult.GetValue(namesArg)!;
            var service = new ConfigService();
            var config = service.Load();
            var removed = config.Targets.RemoveAll(t =>
                names.Any(n => string.Equals(n, t, StringComparison.OrdinalIgnoreCase)));
            service.Save(config);
            AnsiConsole.MarkupLine($"[green]✓[/] 已移除 {removed} 個");
        });
        return cmd;
    }

    private static Command CreateResetCommand()
    {
        var cmd = new Command("reset", "重設為預設值 (node_modules)");
        cmd.SetAction(_ =>
        {
            var service = new ConfigService();
            service.Save(new AppConfig());
            AnsiConsole.MarkupLine("[green]✓[/] 已重設為預設值");
        });
        return cmd;
    }
}
