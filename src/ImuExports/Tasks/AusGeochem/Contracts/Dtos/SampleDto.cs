using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Contracts.Dtos;

public class SampleDto
{
        [JsonPropertyName("archiveId")]
        public int? ArchiveId { get; set; }

        [JsonPropertyName("archiveName")]
        public string ArchiveName { get; set; }

        [JsonPropertyName("archiveNote")]
        public string ArchiveNote { get; set; }

        [JsonPropertyName("collectDateMax")]
        public string CollectDateMax { get; set; }

        [JsonPropertyName("collectDateMin")]
        public string CollectDateMin { get; set; }

        [JsonPropertyName("createdById")]
        public int? CreatedById { get; set; }

        [JsonPropertyName("createdTimestamp")]
        public DateTime? CreatedTimestamp { get; set; }

        [JsonPropertyName("dataPackageId")]
        public int? DataPackageId { get; set; }

        [JsonPropertyName("dataPackageName")]
        public string DataPackageName { get; set; }

        [JsonPropertyName("deletedById")]
        public int? DeletedById { get; set; }

        [JsonPropertyName("deletedTimestamp")]
        public DateTime? DeletedTimestamp { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("igsn")]
        public string Igsn { get; set; }

        [JsonPropertyName("igsnHandleURL")]
        public string IgsnHandleUrl { get; set; }

        [JsonPropertyName("igsnMintingTimestamp")]
        public DateTime? IgsnMintingTimestamp { get; set; }

        [JsonPropertyName("lastEditedById")]
        public int? LastEditedById { get; set; }

        [JsonPropertyName("lastEditedTimestamp")]
        public DateTime? LastEditedTimestamp { get; set; }

        [JsonPropertyName("locationId")]
        public int? LocationId { get; set; }

        [JsonPropertyName("locationKindId")]
        public int? LocationKindId { get; set; }

        [JsonPropertyName("locationKindName")]
        public string LocationKindName { get; set; }

        [JsonPropertyName("locationName")]
        public string LocationName { get; set; }

        [JsonPropertyName("materialId")]
        public int? MaterialId { get; set; }

        [JsonPropertyName("materialName")]
        public string MaterialName { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("referenceElevation")]
        public double? ReferenceElevation { get; set; }

        [JsonPropertyName("referenceElevationKindId")]
        public int? ReferenceElevationKindId { get; set; }

        [JsonPropertyName("referenceElevationKindName")]
        public string ReferenceElevationKindName { get; set; }

        [JsonPropertyName("referenceElevationKindNote")]
        public string ReferenceElevationKindNote { get; set; }

        [JsonPropertyName("referenceElevationSource")]
        public string ReferenceElevationSource { get; set; }

        [JsonPropertyName("relativeElevationAccuracy")]
        public double? RelativeElevationAccuracy { get; set; }

        [JsonPropertyName("relativeElevationMax")]
        public double? RelativeElevationMax { get; set; }

        [JsonPropertyName("relativeElevationMin")]
        public double? RelativeElevationMin { get; set; }

        [JsonPropertyName("rockUnitAgeDescription")]
        public string RockUnitAgeDescription { get; set; }

        [JsonPropertyName("rockUnitAgeMax")]
        public double? RockUnitAgeMax { get; set; }

        [JsonPropertyName("rockUnitAgeMin")]
        public double? RockUnitAgeMin { get; set; }

        [JsonPropertyName("sampleID")]
        public string SampleId { get; set; }

        [JsonPropertyName("sampleKindId")]
        public int? SampleKindId { get; set; }

        [JsonPropertyName("sampleKindName")]
        public string SampleKindName { get; set; }

        [JsonPropertyName("sampleMethodId")]
        public int? SampleMethodId { get; set; }

        [JsonPropertyName("sampleMethodName")]
        public string SampleMethodName { get; set; }

        [JsonPropertyName("sourceId")]
        public string SourceId { get; set; }

        [JsonPropertyName("stratographicUnitId")]
        public int? StratographicUnitId { get; set; }

        [JsonPropertyName("stratographicUnitName")]
        public string StratographicUnitName { get; set; }
}