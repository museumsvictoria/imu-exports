using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Contracts.Dtos;

public class ImageDto
{
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("mediumSizeContentType")]
        public string MediumSizeContentType  { get; set; }

        [JsonPropertyName("mediumSizeUrl")]
        public string MediumSizeUrl { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("originalSizeContentType")]
        public string OriginalSizeContentType { get; set; }

        [JsonPropertyName("originalSizeUrl")]
        public string OriginalSizeUrl { get; set; }
        
        [JsonPropertyName("sampleId")]
        public int? SampleId { get; set; }
        
        [JsonPropertyName("sampleName")]
        public string SampleName { get; set; }
        
        [JsonPropertyName("smallSizeContentType")]
        public string SmallSizeContentType { get; set; }
        
        [JsonPropertyName("smallSizeUrl")]
        public string SmallSizeUrl { get; set; }
}