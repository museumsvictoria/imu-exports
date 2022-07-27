using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.Mapping;

public static class SampleToDtoMapper
{
    public static SampleWithLocationDto UpdateFromSample(this SampleWithLocationDto dto,
        Sample sample, IList<LocationKindDto> locationKinds, IList<MaterialDto> materials,
        IList<SampleKindDto> sampleKinds, IList<MaterialLookup> materialsLookup)
    {
        return ToSampleWithLocationDto(sample, locationKinds, materials, sampleKinds, materialsLookup, null, null, dto);
    }

    public static SampleWithLocationDto CreateSampleWithLocationDto(this Sample sample,
        IList<LocationKindDto> locationKinds, IList<MaterialDto> materials, IList<SampleKindDto> sampleKinds,
        IList<MaterialLookup> materialsLookup, int? dataPackageId, int? archiveId = null)
    {
        return ToSampleWithLocationDto(sample, locationKinds, materials, sampleKinds, materialsLookup, dataPackageId,
            archiveId);
    }

    private static SampleWithLocationDto ToSampleWithLocationDto(Sample sample,
        IList<LocationKindDto> locationKinds, IList<MaterialDto> materials, IList<SampleKindDto> sampleKinds,
        IList<MaterialLookup> materialsLookup, int? dataPackageId = null, int? archiveId = null,
        SampleWithLocationDto dto = null)
    {
        // If there is a current DTO we are Updating otherwise we are Creating
        if (dto != null)
            dto.SampleDto.LastEditedTimestamp = DateTime.UtcNow;
        else
            dto = new SampleWithLocationDto
            {
                SampleDto = new SampleDto(),
                LocationDto = new LocationDto(),
                ShortName = sample.SampleId
            };

        if (dataPackageId != null)
            dto.SampleDto.DataPackageId = dataPackageId;

        if (archiveId != null)
            dto.SampleDto.ArchiveId = archiveId;

        // SampleId => SampleDto.ShortName, SampleDto.Name, SampleDto.SourceId
        dto.ShortName = dto.SampleDto.Name = dto.SampleDto.SourceId = sample.SampleId;

        // ArchiveNotes => SampleDto.ArchiveNote
        dto.SampleDto.ArchiveNote = sample.ArchiveNotes;

        // Latitude => LocationDto.Lat
        if (double.TryParse(sample.Latitude, out var latitude))
            dto.LocationDto.Lat = latitude;

        // Longitude => LocationDto.Lon
        if (double.TryParse(sample.Longitude, out var longitude))
            dto.LocationDto.Lon = longitude;

        // LatLongPrecision => LocationDto.LatLonPrecision
        if (double.TryParse(sample.LatLongPrecision, out var latLongPrecision))
            dto.LocationDto.LatLonPrecision = latLongPrecision;

        // LocationNotes => LocationDto.Description
        dto.LocationDto.Description = sample.LocationNotes;

        // UnitName => SampleDto.StratographicUnitName
        dto.SampleDto.StratographicUnitName = sample.UnitName;

        // LocationKind => SampleDto.LocationKindId, SampleDto.LocationKindName 
        var locationKind = locationKinds.FirstOrDefault(x =>
            string.Equals(x.Name, sample.LocationKind, StringComparison.OrdinalIgnoreCase));
        if (locationKind != null)
        {
            dto.SampleDto.LocationKindId = locationKind.Id;
            dto.SampleDto.LocationKindName = locationKind.Name;
        }

        // Material
        var materialsLookupMatch = materialsLookup.FirstOrDefault(x =>
            string.Equals(x.MvName, sample.MineralId, StringComparison.OrdinalIgnoreCase));

        if (materialsLookupMatch != null)
        {
            var material = materials.FirstOrDefault(x =>
                string.Equals(x.Name, materialsLookupMatch.AusGeochemName, StringComparison.OrdinalIgnoreCase));
            if (material != null)
            {
                dto.SampleDto.MaterialId = material.Id;
                dto.SampleDto.MaterialName = material.Name;
            }
        }

        // DepthMin => RelativeElevationMax
        if (int.TryParse(sample.DepthMin, out var depthMin))
            dto.SampleDto.RelativeElevationMax = depthMin;

        // DepthMin => RelativeElevationMax
        if (int.TryParse(sample.DepthMax, out var depthMax))
            dto.SampleDto.RelativeElevationMin = depthMax;

        // CollectDateMin => DateCollectedMin
        dto.SampleDto.CollectDateMin = sample.DateCollectedMin;

        // CollectDateMax => DateCollectedMax
        dto.SampleDto.CollectDateMax = sample.DateCollectedMax;

        // SampleKind => SampleDto.SampleKindId, SampleDto.SampleKindName
        var sampleKind = sampleKinds.FirstOrDefault(x =>
            string.Equals(x.Name, sample.SampleKind, StringComparison.OrdinalIgnoreCase));
        if (sampleKind != null)
        {
            dto.SampleDto.SampleKindId = sampleKind.Id;
            dto.SampleDto.SampleKindName = sampleKind.Name;
        }

        // Comment => SampleDto.Description
        dto.SampleDto.Description = sample.Comment;

        return dto;
    }
}