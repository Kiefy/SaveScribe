namespace SaveScribe.Models;

public class GameConfig
{
    public string Name { get; set; } = string.Empty;
    public string SaveDirectory { get; set; } = string.Empty;
    public string BackupDirectory { get; set; } = string.Empty;
    public int MaxBackupCount { get; set; } = 5;
    public bool IsAutobackupEnabled { get; set; } = true;
}