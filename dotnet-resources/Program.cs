using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using CliWrap.Exceptions;

return await BuildRootCommand().Parse(args).InvokeAsync();

RootCommand BuildRootCommand()
{
    var rootCommand = new RootCommand();

    {
        var command = new Command("versioninfo");

        rootCommand.Add(command);

        {
            var sourceFileArgument = new Argument<FileInfo>("source-file");
            var targetFileArgument = new Argument<FileInfo>("target-file");

            var subCommand = new Command("copy")
            {
                sourceFileArgument,
                targetFileArgument,
            };

            subCommand.SetAction((ParseResult parseResult) =>
            {
                var sourceFile = parseResult.GetRequiredValue(sourceFileArgument);
                var targetFile = parseResult.GetRequiredValue(targetFileArgument);

                return ExecVersionInfoCopyAsync(sourceFile.FullName, targetFile.FullName);
            });

            command.Add(subCommand);
        }
    }

    return rootCommand;
}

async Task<int> ExecVersionInfoCopyAsync(string sourceFile, string targetFile)
{
    if (!File.Exists(sourceFile))
    {
        Console.Error.WriteLine("ERROR: source file not found");
        return 2;
    }

    if (!File.Exists(targetFile))
    {
        Console.Error.WriteLine("ERROR: target file not found");
        return 3;
    }

    try
    {
        const string ExeFileName = "ResourceHacker.exe";

        var exeFile = Path.Join(AppDomain.CurrentDomain.BaseDirectory, ExeFileName);
        if (!File.Exists(exeFile))
        {
            exeFile = Path.GetFullPath(Path.Join(AppDomain.CurrentDomain.BaseDirectory, "../../any", ExeFileName));
            if (!File.Exists(exeFile))
                throw new ApplicationException($"{ExeFileName} not found");
        }

        var rcFile = Path.Join(Path.GetTempPath(), Path.GetRandomFileName() + ".rc");

        await CliWrap.Cli.Wrap(exeFile)
            .WithArguments(args => args
                .Add("-open").Add(sourceFile)
                .Add("-save").Add(rcFile)
                .Add("-action").Add("extract")
                .Add("-mask").Add("VersionInfo")
            )
            .ExecuteAsync();
        try
        {
            var resFile = Path.Join(Path.GetTempPath(), Path.GetRandomFileName() + ".res");

            await CliWrap.Cli.Wrap(exeFile)
                .WithArguments(args => args
                    .Add("-open").Add(rcFile)
                    .Add("-save").Add(resFile)
                    .Add("-action").Add("compile")
                )
                .ExecuteAsync();
            try
            {
                await CliWrap.Cli.Wrap(exeFile)
                    .WithArguments(args => args
                        .Add("-open").Add(targetFile)
                        .Add("-save").Add(targetFile)
                        .Add("-res").Add(resFile)
                        .Add("-action").Add("delete")
                        .Add("-mask").Add("VersionInfo")
                    )
                    .ExecuteAsync();

                await CliWrap.Cli.Wrap(exeFile)
                    .WithArguments(args => args
                        .Add("-open").Add(targetFile)
                        .Add("-save").Add(targetFile)
                        .Add("-res").Add(resFile)
                        .Add("-action").Add("addoverwrite")
                        .Add("-mask").Add("VersionInfo")
                    )
                    .ExecuteAsync();
            }
            finally
            {
                File.Delete(resFile);
            }
        }
        finally
        {
            File.Delete(rcFile);
        }

        return 0;
    }
    catch (CommandExecutionException)
    {
        Console.Error.WriteLine("ERROR: unknown");
        return 1;
    }
}