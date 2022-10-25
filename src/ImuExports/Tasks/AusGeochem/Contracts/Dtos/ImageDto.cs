using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Contracts.Dtos;

public class ImageDto
{
    [JsonPropertyName("altText")]
    public string AltText { get; set; }
        
    [JsonPropertyName("creator")]
    public string Creator { get; set; }
        
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("id")]
    public int? Id { get; set; }
        
    [JsonPropertyName("license")]
    public string License { get; set; }

    [JsonPropertyName("mediumSizeContentType")]
    public string MediumSizeContentType { get; set; }

    [JsonPropertyName("mediumSize")]
    public string MediumSize { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("originalSizeContentType")]
    public string OriginalSizeContentType { get; set; }

    [JsonPropertyName("originalSize")]
    public string OriginalSize { get; set; }
        
    [JsonPropertyName("rightsHolder")]
    public string RightsHolder { get; set; }
        
    [JsonPropertyName("sampleId")]
    public int? SampleId { get; set; }
        
    [JsonPropertyName("sampleName")]
    public string SampleName { get; set; }
        
    [JsonPropertyName("smallSizeContentType")]
    public string SmallSizeContentType { get; set; }
        
    [JsonPropertyName("smallSize")]
    public string SmallSize { get; set; }
}