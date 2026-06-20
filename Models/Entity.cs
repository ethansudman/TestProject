public enum EntityType
{
    Folder,
    File
}

public class Entity
{
    public EntityType EntityType { get; set; }

    public string Path { get; set; }

    public List<Entity> Subentities { get; set; }
}