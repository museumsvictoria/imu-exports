﻿using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Models.Api;

public class LocationDto
{
        [JsonPropertyName("calcName")]
        public string CalcName { get; set; }

        [JsonPropertyName("captureMethodId")]
        public int? CaptureMethodId { get; set; }

        [JsonPropertyName("captureMethodName")]
        public string CaptureMethodName { get; set; }

        [JsonPropertyName("celestialId")]
        public int? CelestialId { get; set; }

        [JsonPropertyName("celestialName")]
        public string CelestialName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("lat")]
        public long? Lat { get; set; }

        [JsonPropertyName("latLonPrecision")]
        public int? LatLonPrecision { get; set; }

        [JsonPropertyName("lon")]
        public long? Lon { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
}