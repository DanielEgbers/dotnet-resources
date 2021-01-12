﻿using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Dotnet.Resources
{
    public static class Program
    {
        private const string ResourceHacker = "ResourceHacker.exe";

#pragma warning disable IDE1006 // Naming Styles
        public static async Task<int> Main(string[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            var versionInfoCopy = new Command("copy")
            {
                new Option<FileInfo>(new [] { "--source-file", "-s" }) { IsRequired = true },
                new Option<FileInfo>(new [] { "--target-file", "-t" }) { IsRequired = true },
            };
            versionInfoCopy.Handler = CommandHandler.Create
            (
                async (FileInfo sourceFile, FileInfo targetFile) =>
                {
                    return await ExecVersionInfoCopyAsync(sourceFile.FullName, targetFile.FullName);
                }
            );

            var versionInfo = new Command("versioninfo")
            {
                versionInfoCopy
            };

            var root = new RootCommand()
            {
                versionInfo
            };

            var exitCode = await root.InvokeAsync(args);

            return exitCode;
        }

        private static async Task<int> ExecVersionInfoCopyAsync(string sourceFile, string targetFile)
        {
            if (!File.Exists(sourceFile))
                return 2;

            if (!File.Exists(targetFile))
                return 3;

            try
            {
                var rcFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".rc");
                await RunAsync(ResourceHacker, $@"-open ""{sourceFile}"" -save ""{rcFile}"" -action extract -mask VersionInfo");
                try
                {
                    var resFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".res");
                    await RunAsync(ResourceHacker, $@"-open ""{rcFile}"" -save ""{resFile}"" -action compile");
                    try
                    {
                        await RunAsync(ResourceHacker, $@"-open ""{targetFile}"" -save ""{targetFile}"" -res ""{resFile}"" -action addoverwrite -mask VersionInfo");
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
            catch (SimpleExec.NonZeroExitCodeException)
            {
                Console.WriteLine("unknown error");
                return 1;
            }
        }

        private static async Task RunAsync(string name, string args)
        {
            var consoleOut = Console.Out;
            try
            {
                Console.SetOut(TextWriter.Null);
                await SimpleExec.Command.ReadAsync(name, args, noEcho: true);
            }
            finally
            {
                Console.SetOut(consoleOut);
            }
        }
    }
}