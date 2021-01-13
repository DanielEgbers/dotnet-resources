using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Dotnet.Resources
{
    public static class Program
    {
#pragma warning disable IDE1006 // Naming Styles
        public static async Task<int> Main(string[] args)
#pragma warning restore IDE1006 // Naming Styles
        {
            var versionInfoCopy = new Command("copy")
            {
                new Argument<FileInfo>("source-file"),
                new Argument<FileInfo>("target-file"),
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
            {
                Console.WriteLine("ERROR: source file not found");
                return 2;
            }

            if (!File.Exists(targetFile))
            {
                Console.WriteLine("ERROR: target file not found");
                return 3;
            }

            try
            {
                var rcFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".rc");
                await ExecAsync($@"-open ""{sourceFile}"" -save ""{rcFile}"" -action extract -mask VersionInfo");
                try
                {
                    var resFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".res");
                    await ExecAsync($@"-open ""{rcFile}"" -save ""{resFile}"" -action compile");
                    try
                    {
                        await ExecAsync($@"-open ""{targetFile}"" -save ""{targetFile}"" -res ""{resFile}"" -action addoverwrite -mask VersionInfo");
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
                Console.WriteLine("ERROR: unknown");
                return 1;
            }
        }

        private static async Task ExecAsync(string command)
        {
            await SimpleExec.Command.ReadAsync(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ResourceHacker.exe"), command, noEcho: true);
        }
    }
}
