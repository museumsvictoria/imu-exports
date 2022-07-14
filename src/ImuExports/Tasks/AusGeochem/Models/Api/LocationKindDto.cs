using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Models.Api;

public class LocationKindDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}