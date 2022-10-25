using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Contracts.Dtos;

public class SamplePropertyDto
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("propName")]
    public string PropName  { get; set; }

    [JsonPropertyName("propValue")]
    public string PropValue { get; set; }
        
    [JsonPropertyName("sampleId")]
    public int? SampleId { get; set; }
        
    [JsonPropertyName("sampleName")]
    public string SampleName { get; set; }
}