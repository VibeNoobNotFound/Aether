namespace Aether.Backend.Data;

public class MetadataConfig
{
    public int Id { get; set; }
    public List<string> ProviderPriority { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}
