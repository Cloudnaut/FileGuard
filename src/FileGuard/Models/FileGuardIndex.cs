using System.Text.Json;
using Serilog;

namespace FileGuard.Models;

public class FileGuardIndex
{
    public class FileEntry
    {
        public string FileRef { get; set; }
        public DateTime LastModified { get; set; }
        public string Sha265 { get; set; }
    }

    public IDictionary<string, FileEntry> Files { get; set; } = new Dictionary<string, FileEntry>();

    public void SetFileIndex(FileEntry fileEntry)
    {
        if (!Files.ContainsKey(fileEntry.FileRef))
        {
            Files.Add(fileEntry.FileRef, fileEntry);
            Log.Verbose($"Indexed new file: '{fileEntry.FileRef}'");
            return;
        }

        Files[fileEntry.FileRef] = fileEntry;
        Log.Verbose($"Updated file entry: '{fileEntry.FileRef}'");
    }

    public FileEntry? GetFileIndex(string fileRef) => Files.TryGetValue(fileRef, out FileEntry? entry) ? entry : null;
    
    public void WriteToFile(string filePath)
    {
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            WriteIndented = true
        });
        File.WriteAllText(filePath, json);
    }
    
    public static FileGuardIndex ReadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The index file '{filePath}' does not exist.");
        }

        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<FileGuardIndex>(json) ?? new FileGuardIndex();
    }
    
}