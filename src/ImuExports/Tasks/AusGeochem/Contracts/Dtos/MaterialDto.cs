using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Contracts.Dtos;

public class MaterialDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}


