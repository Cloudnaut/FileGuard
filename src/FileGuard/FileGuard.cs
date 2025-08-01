using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FileGuard.Models;
using Serilog;

namespace FileGuard;

public class FileGuard
{
    private const string GuardDirectoryName = ".guard";
    private const string IndexFileName = "index";

    public enum Mode
    {
        Lenient,
        Moderate,
        Strict
    }
    
    public void Initialize(string path)
    {
        string guardDirectory = Path.Combine(path, GuardDirectoryName);

        if (Directory.Exists(guardDirectory))
        {
            Log.Information("FileGuard is already initialized in this directory.");
            return;
        }

        Directory.CreateDirectory(guardDirectory);
        new FileGuardIndex().WriteToFile(Path.Combine(guardDirectory, IndexFileName));

        Log.Information($"FileGuard initialized successfully in: '{guardDirectory}'");
    }

    private string ComputeSha256Hash(FileInfo fileInfo)
    {
        using SHA256 algorithm = SHA256.Create();
        using FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);

        StringBuilder sb = new StringBuilder();
        try
        {
            fileStream.Position = 0;
            byte[] hashValue = algorithm.ComputeHash(fileStream);

            for (int i = 0; i < hashValue.Length; i++)
            {
                sb.Append($"{hashValue[i]:X2}");
            }
        }
        catch (IOException e)
        {
            Log.Error($"I/O Exception: {e.Message}", e);
        }
        catch (UnauthorizedAccessException e)
        {
            Log.Error($"Access Exception: {e.Message}", e);
        }

        return sb.ToString();
    }

    private string GetFileRef(string filePath, string absoluteGuardDirectoryPath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{filePath}' does not exist.");
        }

        FileInfo fileInfo = new FileInfo(filePath);

        string fileRef = Regex.Replace(fileInfo.FullName, $"^{Regex.Escape(Path.GetDirectoryName(absoluteGuardDirectoryPath))}", ".");
        fileRef = fileRef.Replace(Path.DirectorySeparatorChar, '/');

        return fileRef;
    }

    private FileGuardIndex.FileEntry CreateFileIndex(string filePath, string absoluteGuardDirectoryPath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The file '{filePath}' does not exist.");
        }

        FileInfo fileInfo = new FileInfo(filePath);

        var fileEntry = new FileGuardIndex.FileEntry
        {
            FileRef = GetFileRef(filePath, absoluteGuardDirectoryPath),
            LastModified = fileInfo.LastWriteTimeUtc,
            Sha265 = ComputeSha256Hash(fileInfo)
        };

        return fileEntry;
    }

    public static string[] RetrieveFilePaths(string directoryPath)
    {
        Log.Information("Retrieving file list...");
        DateTime startTime = DateTime.UtcNow;
        string[] filePaths = Directory.GetFiles(directoryPath, "*", new EnumerationOptions()
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.System
        });
        Log.Information($"Retrieved {filePaths.Length} files in {DateTime.UtcNow - startTime}");
        return filePaths;
    }

    private bool IsGuardDirectory(string directoryPath) => directoryPath.EndsWith(GuardDirectoryName);
    
    private string? MapFilePathToGuardDirectory(string filePath, HashSet<string> guardedDirectoryPaths)
    {
        string currentDirectory = Path.GetDirectoryName(filePath);

        while (currentDirectory != null)
        {
            if (guardedDirectoryPaths.Contains(currentDirectory))
            {
                return Path.Combine(currentDirectory, GuardDirectoryName);
            }
                
            currentDirectory = Path.GetDirectoryName(currentDirectory);
        }
            
        return currentDirectory != null ? Path.Combine(currentDirectory, GuardDirectoryName) : null;
    }
    
    private HashSet<string> GetGuardDirectoryPaths(string targetDirectoryPath, string[] filePaths)
    {
        HashSet<string> guardDirectoryPaths = filePaths
            .Select(Path.GetDirectoryName)
            .Where(IsGuardDirectory)
            .Distinct()
            .ToHashSet();

        string? GetParentGuardDirectory(string directoryPath)
        {
            string? currentDirectory = directoryPath;

            while (currentDirectory != null)
            {
                if (Directory.Exists(Path.Combine(currentDirectory, GuardDirectoryName)))
                {
                    break;
                }

                currentDirectory = Path.GetDirectoryName(currentDirectory);
            }

            return currentDirectory != null ? Path.Combine(currentDirectory, GuardDirectoryName) : null;
        }
        
        string parentGuardDirectory = GetParentGuardDirectory(targetDirectoryPath);
        if (parentGuardDirectory != null)
        {
            guardDirectoryPaths.Add(parentGuardDirectory);
        }
        
        HashSet<string> guardedDirectories = guardDirectoryPaths
            .Select(guardDirectoryPath => Path.GetDirectoryName(guardDirectoryPath))
            .ToHashSet();
        
        IDictionary<string, string?> fileToGuardDirectoryMap = filePaths
            .Where(filePath => !Path.GetDirectoryName(filePath).EndsWith(GuardDirectoryName))
            .ToDictionary(filePath => filePath, filePath => MapFilePathToGuardDirectory(filePath, guardedDirectories));

        string[] unguardedFilePaths = fileToGuardDirectoryMap
            .Where(kvp => kvp.Value == null)
            .Select(kvp => kvp.Key)
            .Order()
            .ToArray();

        if (unguardedFilePaths.Any())
        {
            Log.Error($"The following files cannot be guarded:{Environment.NewLine}{string.Join(Environment.NewLine, unguardedFilePaths)}");
            Log.Error("Please initialize FileGuard in the parent directory or ensure the files are within a guarded directory structure");
            throw new InvalidOperationException("Some files are not within a guarded directory structure.");
        }

        return guardDirectoryPaths;
    }
    
    public void IndexFiles(string targetDirectoryPath, string[] filePaths)
    {
        Log.Information($"Indexing files in directory: '{targetDirectoryPath}'");
        Log.Information("Detecting initialized directories...");
        HashSet<string> guardDirectoryPaths = GetGuardDirectoryPaths(targetDirectoryPath, filePaths);
        Log.Information($"Found guard directories:{Environment.NewLine}{string.Join(Environment.NewLine, guardDirectoryPaths)}");
        
        IDictionary<string, FileGuardIndex> fileGuardIndices = guardDirectoryPaths
            .ToDictionary(path => path, path => FileGuardIndex.ReadFromFile(Path.Combine(path, IndexFileName)));

        Log.Information("Associating files with their guard directories...");
        HashSet<string> guardedDirectoryPaths = guardDirectoryPaths
            .Select(guardDirectoryPath => Path.GetDirectoryName(guardDirectoryPath))
            .ToHashSet();
        
        IDictionary<string, string> filePathGuardDirectoryPathMapping = filePaths
            .Where(filePath => !Path.GetDirectoryName(filePath).EndsWith(GuardDirectoryName))
            .ToDictionary(filePath => filePath, filePath => MapFilePathToGuardDirectory(filePath, guardedDirectoryPaths));
        
        IDictionary<string, FileGuardIndex> filePathIndexMapping = filePathGuardDirectoryPathMapping
            .Select(mapping => (filePath: mapping.Key, index: fileGuardIndices[mapping.Value]))
            .ToDictionary(mapping => mapping.filePath, mapping => mapping.index);
        
        Log.Information("Indexing files...");
        filePaths
            .Where(filePath => !Path.GetDirectoryName(filePath).EndsWith(GuardDirectoryName))
            .Where(filePath =>
            {
                string fileRef = GetFileRef(filePath, filePathGuardDirectoryPathMapping[filePath]);
                
                FileGuardIndex.FileEntry? fileEntry = filePathIndexMapping[filePath].GetFileIndex(fileRef);
                
                if (fileEntry == null)
                {
                    return true; // File is not indexed, so we should index it
                }
                
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.LastWriteTimeUtc > fileEntry.LastModified)
                {
                    Log.Information($"File '{filePath}' has been modified since last index, will be updated");
                    return true;
                }
                
                return false; // File is already indexed and not modified, so we skip it
            })
            .Order()
            .ToList()
            .ForEach(filePath =>
            {
                Log.Information($"Indexing file: '{filePath}'");
                try
                {
                    filePathIndexMapping[filePath].SetFileIndex(CreateFileIndex(filePath, filePathGuardDirectoryPathMapping[filePath]));
                }
                catch (Exception e)
                {
                    Log.Error($"Error indexing file '{filePath}': {e.Message}", e);
                }
            });
        
        Log.Information("Writing updated indices to files...");
        fileGuardIndices
            .Select(kvp => (indexFilePath: Path.Combine(kvp.Key, IndexFileName), index: kvp.Value))
            .ToList()
            .ForEach(x => x.index.WriteToFile(x.indexFilePath));
        
        Log.Information("Indexing completed");
    }


    public bool VerifyFiles(string targetDirectoryPath, string[] filePaths, Mode mode, int? progressInterval)
    {
        Log.Information($"Verifying files in directory: '{targetDirectoryPath}'");
        Log.Information("Detecting initialized directories...");
        HashSet<string> guardDirectoryPaths = GetGuardDirectoryPaths(targetDirectoryPath, filePaths);
        Log.Information($"Found guard directories:{Environment.NewLine}{string.Join(Environment.NewLine, guardDirectoryPaths)}");
        
        IDictionary<string, FileGuardIndex> fileGuardIndices = guardDirectoryPaths
            .ToDictionary(path => path, path => FileGuardIndex.ReadFromFile(Path.Combine(path, IndexFileName)));

        Log.Information("Associating files with their guard directories...");
        HashSet<string> guardedDirectoryPaths = guardDirectoryPaths
            .Select(guardDirectoryPath => Path.GetDirectoryName(guardDirectoryPath))
            .ToHashSet();
        
        IDictionary<string, string> filePathGuardDirectoryPathMapping = filePaths
            .Where(filePath => !Path.GetDirectoryName(filePath).EndsWith(GuardDirectoryName))
            .ToDictionary(filePath => filePath, filePath => MapFilePathToGuardDirectory(filePath, guardedDirectoryPaths));
        
        IDictionary<string, FileGuardIndex> filePathIndexMapping = filePathGuardDirectoryPathMapping
            .Select(mapping => (filePath: mapping.Key, index: fileGuardIndices[mapping.Value]))
            .ToDictionary(mapping => mapping.filePath, mapping => mapping.index);
        
        Log.Information("Verifying file integrity...");
        int currentFileCount = 0;
        int fileNotIndexedCount = 0;
        int fileModifiedCount = 0;
        int fileAlteredCount = 0;
        int fileVerificationFailedCount = 0;
        int fileVerifiedCount = 0;

        foreach (string filePath in filePaths)
        {
            currentFileCount++;
            if(progressInterval is > 0 && currentFileCount % progressInterval == 0)
            {
                Log.Information($"Verifying file {currentFileCount}/{filePaths.Length}");
            }
            
            if (Path.GetDirectoryName(filePath).EndsWith(GuardDirectoryName))
            {
                continue;
            }
            
            try
            {
                string fileRef = GetFileRef(filePath, filePathGuardDirectoryPathMapping[filePath]);
                FileGuardIndex.FileEntry? fileEntry = filePathIndexMapping[filePath].GetFileIndex(fileRef);

                if (fileEntry == null)
                {
                    Log.Warning($"File '{filePath}' is not indexed.");
                    fileNotIndexedCount++;
                    continue;
                }

                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.LastWriteTimeUtc > fileEntry.LastModified)
                {
                    Log.Warning($"File '{filePath}' has been modified since last index.");
                    fileModifiedCount++;
                    continue;
                }

                string currentHash = ComputeSha256Hash(fileInfo);
                if (currentHash != fileEntry.Sha265)
                {
                    Log.Error($"File '{filePath}' has been altered. Expected hash: {fileEntry.Sha265}, Current hash: {currentHash}");
                    fileAlteredCount++;
                    continue;
                }

                Log.Verbose($"File '{filePath}' is verified successfully.");
                fileVerifiedCount++;
            }
            catch (Exception e)
            {
                Log.Error($"Error verifying file '{filePath}': {e.Message}", e);
                fileVerificationFailedCount++;
            }
        }

        Log.Information($"Verification completed: {fileVerifiedCount} files verified, {fileNotIndexedCount} files not indexed, {fileModifiedCount} files modified, {fileAlteredCount} files altered, {fileVerificationFailedCount} failed file verifications.");
        
        switch (mode)
        {
            case Mode.Lenient:
                return fileAlteredCount == 0;
            case Mode.Moderate:
                return fileAlteredCount == 0 && fileVerificationFailedCount == 0;
            case Mode.Strict:
                return fileAlteredCount == 0 && fileVerificationFailedCount == 0 && fileModifiedCount == 0 && fileNotIndexedCount == 0;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown verification mode");
        }
    }
}