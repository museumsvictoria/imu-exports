using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Contracts.Dtos;

public class ImageWriteDto
{
        [JsonPropertyName("content")]
        public string Content { get; set; }
        
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
}