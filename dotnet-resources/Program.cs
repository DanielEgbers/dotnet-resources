using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using CliWrap.Exceptions;

namespace Dotnet.Resources
{
    public static class Program
    {
#pragma warning disable IDE1006 // Naming Styles
        public static async Task<int> Main(string[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            var versionInfoCopySourceFileArgument = new Argument<FileInfo>("source-file");
            var versionInfoCopyTargetFileArgument = new Argument<FileInfo>("target-file");
            var versionInfoCopyCommand = new Command("copy")
            {
                versionInfoCopySourceFileArgument,
                versionInfoCopyTargetFileArgument,
            };
            versionInfoCopyCommand.SetHandler
            (
                (FileInfo sourceFile, FileInfo targetFile) =>
                {
                    return ExecVersionInfoCopyAsync(sourceFile.FullName, targetFile.FullName);
                },
                versionInfoCopySourceFileArgument,
                versionInfoCopyTargetFileArgument

            );

            var versionInfoCommand = new Command("versioninfo")
            {
                versionInfoCopyCommand
            };

            var rootCommand = new RootCommand()
            {
                versionInfoCommand
            };

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> ExecVersionInfoCopyAsync(string sourceFile, string targetFile)
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
                var resourceHacker = CliWrap.Cli.Wrap(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ResourceHacker.exe"));

                var rcFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".rc");

                await resourceHacker
                    .WithArguments(args => args
                        .Add("-open").Add(sourceFile)
                        .Add("-save").Add(rcFile)
                        .Add("-action").Add("extract")
                        .Add("-mask").Add("VersionInfo")
                    ).ExecuteAsync();
                try
                {
                    var resFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".res");

                    await resourceHacker
                        .WithArguments(args => args
                            .Add("-open").Add(rcFile)
                            .Add("-save").Add(resFile)
                            .Add("-action").Add("compile")
                        ).ExecuteAsync();
                    try
                    {
                        await resourceHacker
                            .WithArguments(args => args
                                .Add("-open").Add(targetFile)
                                .Add("-save").Add(targetFile)
                                .Add("-res").Add(resFile)
                                .Add("-action").Add("delete")
                                .Add("-mask").Add("VersionInfo")
                            ).ExecuteAsync();

                        await resourceHacker
                            .WithArguments(args => args
                                .Add("-open").Add(targetFile)
                                .Add("-save").Add(targetFile)
                                .Add("-res").Add(resFile)
                                .Add("-action").Add("addoverwrite")
                                .Add("-mask").Add("VersionInfo")
                            ).ExecuteAsync();
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
    }
}
