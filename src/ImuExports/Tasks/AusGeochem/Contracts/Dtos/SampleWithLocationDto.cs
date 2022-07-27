using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Contracts.Dtos;

public class SampleWithLocationDto
{
    [JsonPropertyName("autoSetElevationWriteConfig")]
    public bool AutoSetElevationWriteConfig { get; set; }

    [JsonPropertyName("createdByName")]
    public string CreatedByName { get; set; }

    [JsonPropertyName("id")]
    public int? Id { get; set; }

    [JsonPropertyName("lastEditedByName")]
    public string LastEditedByName { get; set; }

    [JsonPropertyName("locationDTO")]
    public LocationDto LocationDto { get; set; }

    [JsonPropertyName("sampleDTO")]
    public SampleDto SampleDto { get; set; }

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; }
}