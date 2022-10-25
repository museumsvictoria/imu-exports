using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Contracts.Requests;

public class AddImageToSampleRequest
{
    [JsonPropertyName("altText")]
    public string AltText { get; set; }
        
    [JsonPropertyName("content")]
    public string Content { get; set; }
        
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; }
        
    [JsonPropertyName("creator")]
    public string Creator { get; set; }
        
    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("id")]
    public int? Id { get; set; }
        
    [JsonPropertyName("license")]
    public string License { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
        
    [JsonPropertyName("rightsHolder")]
    public string RightsHolder { get; set; }
}