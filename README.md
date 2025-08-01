# FileGuard

**Disclaimer: This README is entirely written by AI, might contain errors, use with caution!**

[**⚠️ Important Legal Notice and Disclaimer (click)**](#️-important-legal-notice-and-disclaimer)

FileGuard is a command-line tool for file and directory integrity verification. It creates indexes with SHA-256 checksums of files and enables detection of changes to these files. The tool is designed for monitoring data integrity, compliance auditing, and forensic analysis.

## ✨ Features

- **SHA-256 Hash Verification**: Uses cryptographically secure SHA-256 hashing for file integrity verification
- **Recursive Directory Processing**: Automatically processes all files in directory trees
- **Smart Indexing**: Skips unchanged files for performance optimization based on modification timestamps
- **Progress Reporting**: Optional progress updates during verification of large file sets
- **Structured Logging**: Comprehensive logging with Serilog for all operations
- **Cross-Platform**: Runs on Windows, Linux, and macOS
- **Multi-Platform Binaries**: Self-contained executables for all major platforms
- **Docker Support**: Containerized execution for easy deployment
- **Hidden Index Storage**: Uses `.guard` directories for non-intrusive index storage
- **JSON Index Format**: Human-readable index files for transparency

## 🎯 Use Cases

FileGuard is suitable for various use cases:

- **Data Integrity Monitoring**: Detection of unauthorized changes to important files
- **Backup Verification**: Checking whether backup files have remained unchanged
- **Compliance and Auditing**: Proof of document immutability with cryptographic evidence
- **Forensic Analysis**: Detecting changes in file systems for security investigations
- **Software Distribution**: Ensuring integrity of delivered files and installations
- **Configuration Management**: Monitoring system and application configurations
- **Archive Verification**: Long-term integrity verification of archived data
- **Incident Response**: Baseline creation and change detection for security incidents

## 🚀 Getting Started

### Prerequisites

- **No .NET Runtime required** for pre-built binaries (self-contained)
- **OR** .NET 8.0 Runtime for source compilation
- Windows, Linux or macOS

### Installation

#### Option 1: Download Pre-built Binaries (Recommended)
Download the latest version from [GitHub Releases](https://github.com/Cloudnaut/FileGuard/releases):

**Linux x64:**
```bash
# Download and extract
wget https://github.com/Cloudnaut/FileGuard/releases/latest/download/fileguard-VERSION-linux-x64.tar.gz
tar -xzf fileguard-VERSION-linux-x64.tar.gz
chmod +x FileGuard
./FileGuard --help
```

**Windows x64:**
```powershell
# Download from releases page and extract ZIP file
# Run FileGuard.exe from extracted folder
.\FileGuard.exe --help
```

**macOS x64:**
```bash
# Download and extract
curl -L -o fileguard-macos.tar.gz https://github.com/Cloudnaut/FileGuard/releases/latest/download/fileguard-VERSION-osx-x64.tar.gz
tar -xzf fileguard-macos.tar.gz
chmod +x FileGuard
./FileGuard --help
```

#### Option 2: Docker (Recommended for Containers)
```bash
# Pull latest version
docker pull coderholic/fileguard:latest

# Or pull specific version
docker pull coderholic/fileguard:1.0.X
```

#### Option 3: Compile from Source
```bash
git clone https://github.com/Cloudnaut/FileGuard.git
cd FileGuard/src/FileGuard
dotnet build -c Release
```

### First Steps

1. **Initialize FileGuard in a directory:**
   ```bash
   # Using downloaded binary
   ./FileGuard init --path /path/to/directory
   
   # With Docker
   docker run --rm -v /path/to/directory:/data coderholic/fileguard:latest init --path /data
   ```

2. **Index files:**
   ```bash
   # Single file
   ./FileGuard index --path /path/to/file.txt
   
   # Entire directory (recursive)
   ./FileGuard index --path /path/to/directory
   
   # With Docker
   docker run --rm -v /path/to/directory:/data coderholic/fileguard:latest index --path /data
   ```

3. **Verify integrity:**
   ```bash
   # Verify single file
   ./FileGuard verify --path /path/to/file.txt
   
   # Verify entire directory with progress updates every 100 files
   ./FileGuard verify --path /path/to/directory --progress-interval 100
   
   # With Docker
   docker run --rm -v /path/to/directory:/data coderholic/fileguard:latest verify --path /data --progress-interval 50
   ```

## 📖 Detailed Usage

### Commands

FileGuard provides three main commands with comprehensive options:

#### `init` - Initialization
Prepares a directory for use with FileGuard by creating the `.guard` directory and index file.

```bash
FileGuard init --path <path>
FileGuard init -p <path>
```

**Parameters:**
- `--path` / `-p`: Path to the directory to be initialized (required)

**Behavior:**
- Creates a hidden `.guard` directory in the specified path
- Initializes an empty JSON index file
- Safe to run multiple times (will not overwrite existing initialization)

#### `index` - Indexing
Creates or updates indexes for files or directories with intelligent change detection.

```bash
FileGuard index --path <path>
FileGuard index -p <path>
```

**Parameters:**
- `--path` / `-p`: Path to the file or directory to be indexed (required)

**Behavior:**
- **New files**: Automatically indexed with SHA-256 hash
- **Modified files**: Re-indexed if modification timestamp is newer than stored timestamp
- **Unchanged files**: Skipped for performance optimization
- **Recursive processing**: Automatically processes all subdirectories
- **Multiple guard directories**: Supports nested `.guard` directories for complex hierarchies

#### `verify` - Verification
Checks the integrity of files based on stored indexes with detailed reporting and configurable verification modes.

```bash
FileGuard verify --path <path> [--mode <mode>] [--progress-interval <number>]
FileGuard verify -p <path> [-m <mode>] [-pi <number>]
```

**Parameters:**
- `--path` / `-p`: Path to the file or directory to be verified (required)
- `--mode` / `-m`: Verification mode that controls error handling behavior (optional, default: Moderate)
- `--progress-interval` / `-pi`: Interval for progress updates (in files) (optional)

**Verification Modes:**
- **Lenient**: Only report errors on altered files (hash mismatches)
- **Moderate** (default): Report errors on altered files and failed verifications
- **Strict**: Report errors on altered files, failed verifications, unindexed files, and modified files

**Mode Examples:**
```bash
# Lenient mode - only fail on hash mismatches
./FileGuard verify --path /data --mode lenient

# Moderate mode (default) - fail on alterations and verification failures
./FileGuard verify --path /data --mode moderate

# Strict mode - fail on any detected changes or issues
./FileGuard verify --path /data --mode strict
```

**Behavior:**
- **Hash verification**: Compares current SHA-256 hash with stored hash
- **Timestamp check**: Reports files modified since last indexing
- **Missing files**: Reports files that are not indexed
- **Detailed reporting**: Provides comprehensive statistics at completion
- **Progress updates**: Optional progress reporting for large file sets
- **Mode-based error handling**: Different exit codes based on verification mode and findings

### Verification Results

FileGuard provides detailed verification results:

- ✅ **Verified**: File hash matches stored hash and timestamp is unchanged
- ⚠️ **Modified**: File timestamp is newer than stored timestamp (needs re-indexing)
- ❌ **Altered**: File hash differs from stored hash (potential integrity violation)
- 🔍 **Not Indexed**: File exists but has no stored index entry
- ⚠️ **Verification Failed**: Error occurred during verification process

### How It Works

1. **Initialization**: FileGuard creates hidden `.guard` directories in monitored folders
2. **Indexing**: 
   - Calculates SHA-256 hash for each file
   - Stores hash, file path (relative), and last modification timestamp in JSON format
   - Uses file modification time for optimization
3. **Verification**: 
   - Recalculates SHA-256 hash for each file
   - Compares with stored hash and timestamp
   - Reports any discrepancies or changes

### Index File Structure

FileGuard stores indexes in JSON format for transparency:

```json
{
  "Files": {
    "./document.pdf": {
      "FileRef": "./document.pdf",
      "LastModified": "2025-07-31T10:30:00.000Z",
      "Sha265": "A1B2C3D4E5F6..."
    }
  }
}
```

### Docker Usage

FileGuard is available as a Docker image for easy deployment and cross-platform usage:

```bash
# Basic usage pattern
docker run --rm -v /host/path:/container/path coderholic/fileguard:latest <command> --path /container/path

# Example: Initialize a directory
docker run --rm -v /home/user/documents:/data coderholic/fileguard:latest init --path /data

# Example: Index files recursively
docker run --rm -v /home/user/documents:/data coderholic/fileguard:latest index --path /data

# Example: Verify with progress updates
docker run --rm -v /home/user/documents:/data coderholic/fileguard:latest verify --path /data --progress-interval 100

# Example: Using specific version
docker run --rm -v /home/user/documents:/data coderholic/fileguard:1.0.50 verify --path /data

# Example: Process single file
docker run --rm -v /home/user/important.pdf:/data/important.pdf coderholic/fileguard:latest verify --path /data/important.pdf
```

**Docker Best Practices:**
- Always use `--rm` to automatically remove containers after execution
- Mount directories with appropriate permissions
- Use specific version tags for production environments
- Ensure the container path matches the path used in FileGuard commands

### Advanced Usage Examples

#### Large Directory Processing
```bash
# Index large directory with verbose logging
./FileGuard index --path /large/dataset

# Verify with progress updates every 1000 files
./FileGuard verify --path /large/dataset --progress-interval 1000
```

#### Multiple Directory Hierarchies
```bash
# Initialize parent directory
./FileGuard init --path /projects

# Initialize subdirectories for granular control
./FileGuard init --path /projects/project-a
./FileGuard init --path /projects/project-b

# Index specific projects
./FileGuard index --path /projects/project-a
./FileGuard verify --path /projects/project-a
```

#### Batch Operations (Linux/macOS)
```bash
#!/bin/bash
# Script to verify multiple directories
for dir in /backups/*/; do
    echo "Verifying $dir"
    ./FileGuard verify --path "$dir" --progress-interval 50
done
```

#### PowerShell Batch Operations (Windows)
```powershell
# PowerShell script to verify multiple directories
Get-ChildItem "C:\Backups" -Directory | ForEach-Object {
    Write-Host "Verifying $($_.FullName)"
    .\FileGuard.exe verify --path $_.FullName --progress-interval 50
}
```

## 🔧 Development

### Clone and Build
```bash
git clone https://github.com/Cloudnaut/FileGuard.git
cd FileGuard/src/FileGuard
dotnet restore
dotnet build -c Release
```

### Create Self-Contained Executable
```bash
# Linux
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# Windows
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# macOS
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

### Run Tests
```bash
dotnet test
```

### Build Docker Image Locally
```bash
docker build -t fileguard:local ./src/FileGuard
```

## 📋 System Requirements

### For Pre-built Binaries
- **Operating System**: Windows 10/11, Linux (most distributions), macOS 10.15+
- **.NET Runtime**: Not required (self-contained)
- **Storage Space**: Minimal for index files (typically < 1% of monitored data)
- **Permissions**: Read/write access to monitored directories

### For Docker
- **Docker Engine**: Version 20.10 or higher
- **Storage**: Space for Docker image (~100MB) plus index files
- **Permissions**: Docker execution permissions

### For Source Compilation
- **.NET SDK**: Version 8.0 or higher
- **Operating System**: Windows, Linux, macOS

## 🔍 Logging and Output

FileGuard uses Serilog for structured logging with different log levels:

### Log Levels
- **Information**: General operational messages, progress updates
- **Verbose**: Detailed file-by-file operations (individual file indexing/verification)
- **Warning**: Non-critical issues (files not indexed, files modified since indexing)
- **Error**: Critical issues (file access errors, hash mismatches, verification failures)

### Output Examples

**Initialization:**
```
[INFO] FileGuard initialized successfully in: '/path/to/.guard'
```

**Indexing:**
```
[INFO] Indexing files in directory: '/path/to/data'
[INFO] Found guard directories: /path/to/data/.guard
[INFO] Indexing file: '/path/to/data/document.pdf'
[INFO] Indexing completed
```

**Verification:**
```
[INFO] Verifying files in directory: '/path/to/data'
[INFO] Verifying file 1000/5000
[WARN] File '/path/to/data/modified.txt' has been modified since last index
[ERROR] File '/path/to/data/corrupted.pdf' has been altered. Expected hash: A1B2C3..., Current hash: X7Y8Z9...
[INFO] Verification completed: 4998 files verified, 0 files not indexed, 1 files modified, 1 files altered, 0 failed file verifications
```

## 🚀 Performance Considerations

### Optimization Features
- **Timestamp-based skipping**: Unchanged files are skipped during indexing
- **Efficient hashing**: Uses streaming SHA-256 computation for large files
- **Progress reporting**: Optional progress updates prevent timeout concerns
- **Parallel-safe**: Multiple FileGuard instances can operate on different directories

### Performance Tips
- **Use progress intervals** for large datasets (e.g., `--progress-interval 1000`)
- **Initialize at appropriate levels** to avoid deep recursion in large hierarchies
- **Regular indexing** keeps verification fast by minimizing file changes
- **Separate guard directories** for logically distinct areas

### Benchmarks (Approximate)
- **Small files (< 1MB)**: ~1000 files/second verification
- **Large files (> 100MB)**: Limited by disk I/O for hash calculation
- **Index overhead**: Typically < 1KB per file in index

## 🛡️ Security Considerations

### Cryptographic Security
- **SHA-256 hashing**: Industry-standard cryptographic hash function
- **Collision resistance**: SHA-256 provides strong protection against hash collisions
- **Integrity detection**: Can detect both accidental corruption and intentional tampering

### Limitations
- **Time-of-check vs. time-of-use**: Files can be modified between verification runs
- **Index integrity**: The `.guard` directories themselves should be protected
- **Key management**: FileGuard does not use encryption or digital signatures
- **Access controls**: Relies on file system permissions for security

### Best Practices
- **Protect guard directories**: Ensure `.guard` directories have appropriate access controls
- **Regular verification**: Run verification frequently to detect changes quickly
- **Backup indexes**: Include `.guard` directories in backup strategies
- **Combine with other tools**: Use alongside proper access controls and monitoring
- **Test integrity**: Verify the integrity verification process itself periodically

## 🔄 Integration Examples

### CI/CD Pipeline Integration
```yaml
# GitHub Actions example
- name: Verify File Integrity
  run: |
    docker run --rm -v ${{ github.workspace }}:/data coderholic/fileguard:latest verify --path /data --progress-interval 100
```

### Cron Job Example (Linux)
```bash
# Add to crontab for daily verification
0 2 * * * /usr/local/bin/FileGuard verify --path /important/data --progress-interval 500 >> /var/log/fileguard.log 2>&1
```

### PowerShell Scheduled Task (Windows)
```powershell
# Weekly verification script
$logFile = "C:\Logs\FileGuard-$(Get-Date -Format 'yyyyMMdd').log"
& "C:\Tools\FileGuard.exe" verify --path "C:\ImportantData" --progress-interval 100 *> $logFile
```

### Monitoring Integration
```bash
# Example with exit code checking
./FileGuard verify --path /data --progress-interval 100
if [ $? -eq 0 ]; then
    echo "Verification successful" | logger -t fileguard
else
    echo "Verification failed - check logs" | logger -t fileguard -p user.error
fi
```

## ⚠️ Important Legal Notice and Disclaimer

**USE AT YOUR OWN RISK**

This tool is provided "as is" and without any warranty. The developers and contributors assume no liability for:

- **Data loss or corruption** of any kind
- **Security breaches** or undetected manipulations
- **Business losses** or downtime
- **False alarms** or undetected changes
- **Incompatibilities** with your system or other tools

**Important Security Notes:**

1. **Not a replacement for professional security solutions**: FileGuard is a development tool and not a replacement for professional intrusion detection systems or security solutions.

2. **Regular backups**: Always perform regular backups of your important data.

3. **Test before production use**: Test the tool thoroughly in a test environment before using it in production systems.

4. **No guarantee of completeness**: There is no guarantee that all changes or manipulations will be detected.

5. **System-dependent functionality**: Functionality may vary depending on operating system, file system, and hardware.

By using this tool, you expressly accept that you bear full responsibility for all consequences that may arise from its use.

**For critical applications, consult professional security experts and use certified security solutions.**

## 📄 License

This project is released under an open-source license. See the LICENSE file for details.

---

**FileGuard** - Ensuring data integrity through cryptographic verification  
*Last Updated: July 2025*
