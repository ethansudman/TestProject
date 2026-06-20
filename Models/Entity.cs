/// <summary>
/// Whether an <see cref="Entity"/> represents a file or a folder. This is used to determine how to interpret the Path property and whether the Subentities property will contain any child entities.
/// </summary>
public enum EntityType
{
    /// <summary>
    /// <see cref="Entity"/> represents a folder
    /// </summary>
    Folder,

    /// <summary>
    /// <see cref="Entity"/> represents a file
    /// </summary>
    File
}

/// <summary>
/// Represents either a file or a folder in the filesystem. If it's a folder, it can contain subentities which can be either files or folders. This class is used to represent the directory structure of the browsable directory and its contents in a way that can be easily serialized and sent to the client for display in the UI.
/// </summary>
public class Entity
{
    /// <summary>
    /// The type of this entity, either a file or a folder. This will determine how the Path property should be interpreted and whether the Subentities property will contain any child entities.
    /// </summary>
    public EntityType EntityType { get; set; }

    /// <summary>
    /// Full path to the file or folder represented by this entity. For example, "C:\MyFolder\Subfolder\File.txt" for a file or "C:\MyFolder\Subfolder" for a folder. This should be an absolute path that can be used to access the file or folder on the server's filesystem.
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// Get all files and subfolders contained within this entity. If this entity is a file, this will be an empty list.
    /// </summary>
    public List<Entity> Subentities { get; set; }

    /// <summary>
    /// Get the number of elements contained within this entity, including itself and all nested subentities
    /// </summary>
    public int ChildCount
    {
        get
        {
            return 1 + (Subentities?.Sum(e => e.ChildCount) ?? 0);
        }
    }

    /// <summary>
    /// Get the size in bytes (if it's a file) or the total size of all contained files (if it's a folder)
    /// </summary>
    public long Size
    {
        get
        {
            return EntityType == EntityType.File ? new FileInfo(Path).Length : Subentities.Sum(e => e.Size);
        }
    }
}