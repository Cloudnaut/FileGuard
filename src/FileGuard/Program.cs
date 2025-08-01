using System.CommandLine;
using Serilog;

namespace FileGuard;

class Program
{
    static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        RootCommand rootCommand = new RootCommand();
        rootCommand.AddCommand(new InitCommand());
        rootCommand.AddCommand(new IndexCommand());
        rootCommand.AddCommand(new VerifyCommand());

        rootCommand.Invoke(args);
    }
    
    private static void PrepareTargetDirectoryAndFilePaths(string path, out string targetDirectoryPath, out string[] filePaths)
    {
        FileAttributes pathFileAttributes = File.GetAttributes(path);

        if (pathFileAttributes.HasFlag(FileAttributes.Directory))
        {
            targetDirectoryPath = path;
            filePaths = FileGuard.RetrieveFilePaths(path);
            return;
        }

        targetDirectoryPath = Path.GetDirectoryName(path);
        filePaths = [path];
    }
    
    private class InitCommand : Command
    {
        public InitCommand() : base("init", "Initialize FileGuard in a directory")
        {
            Option<string> pathOption = new Option<string>("--path", "Path to initialize FileGuard in")
            {
                IsRequired = true
            };
            pathOption.AddAlias("-p");
            
            AddOption(pathOption);
            
            this.SetHandler(path =>
            {
                try
                {
                    new FileGuard().Initialize(path);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to initialize FileGuard");
                    Environment.Exit(0xDEAD);
                }
                
                Environment.Exit(0);
            }, pathOption);
        }
    }
    
    private class IndexCommand : Command
    {
        public IndexCommand() : base("index", "Index a file or directory")
        {
            Option<string> pathOption = new Option<string>("--path", "Path to file or directory to index")
            {
                IsRequired = true
            };
            pathOption.AddAlias("-p");
            
            AddOption(pathOption);
            
            this.SetHandler(path =>
            {
                PrepareTargetDirectoryAndFilePaths(path, out string targetDirectoryPath, out string[] filePaths);
                try
                {
                    new FileGuard().IndexFiles(targetDirectoryPath, filePaths);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to index files");
                    Environment.Exit(0xDEAD);
                }
                
                Environment.Exit(0);
            }, pathOption);
        }
    }
    
    private class VerifyCommand : Command
    {
        public VerifyCommand() : base("verify", "Verify a file or directory")
        {
            Option<string> pathOption = new Option<string>("--path", "Path to file or directory to verify")
            {
                IsRequired = true
            };
            pathOption.AddAlias("-p");
            
            Option<FileGuard.Mode> modeOption = new Option<FileGuard.Mode>("--mode")
            {
                IsRequired = false
            };
            modeOption.Description = "Verification mode: lenient (error on altered files), moderate(error on altered files or failed verifications), strict (error on altered files, failed verifications, unindexed files and modified files)";
            modeOption.SetDefaultValue(FileGuard.Mode.Moderate);
            modeOption.AddAlias("-m");
            
            Option<int?> progressIntervalOption = new Option<int?>("--progress-interval", "Interval for progress updates (in files)")
            {
                IsRequired = false
            };
            progressIntervalOption.SetDefaultValue(null);
            progressIntervalOption.AddAlias("-pi");
            
            AddOption(pathOption);
            AddOption(modeOption);
            AddOption(progressIntervalOption);
            
            this.SetHandler((path, mode, progressInterval) =>
            {
                PrepareTargetDirectoryAndFilePaths(path, out string targetDirectoryPath, out string[] filePaths);
                try
                {
                    bool succeeded = new FileGuard().VerifyFiles(targetDirectoryPath, filePaths, mode, progressInterval);
                    if (!succeeded)
                    {
                        Log.Error("File verification failed. Please check the logs for more details.");
                        Environment.Exit(1);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to verify files");
                    Environment.Exit(0xDEAD);
                }
                
                Environment.Exit(0);
            }, pathOption, modeOption, progressIntervalOption);
        }
    }

}