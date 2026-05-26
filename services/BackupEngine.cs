using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using SaveScribe.Models;

namespace SaveScribe.Services;

public class BackupEngine
{
    /// <summary>
    /// Checks if a directory has actually changed by comparing file sizes and timestamps,
    /// completely avoiding heavy MD5 disk-reading bottlenecks.
    /// </summary>
    public bool HasDirectoryChanged(string sourceDir, string lastKnownSignature)
    {
        if (!Directory.Exists(sourceDir)) return false;

        string currentSignature = GenerateDirectorySignature(sourceDir);
        return currentSignature != lastKnownSignature;
    }

    /// <summary>
    /// Generates an ultra-lightweight string fingerprint of a folder's state.
    /// </summary>
    public string GenerateDirectorySignature(string sourceDir)
    {
        var sb = new StringBuilder();
        var dirInfo = new DirectoryInfo(sourceDir);

        foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            // Fingerprint combines relative path + file size + last write time
            string relativePath = Path.GetRelativePath(sourceDir, file.FullName);
            sb.Append($"{relativePath}_{file.Length}_{file.LastWriteTimeUtc.Ticks}|");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Compresses the save directory into a clean, timestamped ZIP archive.
    /// </summary>
    public string CreateBackup(GameConfig game)
    {
        if (!Directory.Exists(game.SaveDirectory))
            throw new DirectoryNotFoundException($"Save directory not found: {game.SaveDirectory}");

        // Create the backup directory structure if it doesn't exist yet
        Directory.CreateDirectory(game.BackupDirectory);

        // Generate a clean, safe timestamp file name (e.g., Skyrim_2026-05-26_20-15-30.zip)
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string safeGameName = string.Join("_", game.Name.Split(Path.GetInvalidFileNameChars()));
        string zipPath = Path.Combine(game.BackupDirectory, $"{safeGameName}_{timestamp}.zip");

        // Execute fast .NET 10 hardware-optimized compression
        ZipFile.CreateFromDirectory(game.SaveDirectory, zipPath, CompressionLevel.Optimal, false);

        // Enforce the maximum backup rotation limit
        EnforceBackupLimit(game);

        return zipPath;
    }

    /// <summary>
    /// Automatically deletes the oldest backups if they exceed the user's allowed count.
    /// </summary>
    private void EnforceBackupLimit(GameConfig game)
    {
        if (!Directory.Exists(game.BackupDirectory)) return;

        var directory = new DirectoryInfo(game.BackupDirectory);
        
        // Find all zip files matching this specific game's naming convention
        var safeGameName = string.Join("_", game.Name.Split(Path.GetInvalidFileNameChars()));
        var backupFiles = directory.GetFiles($"{safeGameName}_*.zip")
                                   .OrderByDescending(f => f.CreationTime)
                                   .ToList();

        // If we have more files than allowed, delete the oldest ones at the bottom of the list
        if (backupFiles.Count > game.MaxBackupCount)
        {
            for (int i = game.MaxBackupCount; i < backupFiles.Count; i++)
            {
                try
                {
                    backupFiles[i].Delete();
                }
                catch
                {
                    // Fail silently if a file is temporarily locked by the OS
                }
            }
        }
    }
}
