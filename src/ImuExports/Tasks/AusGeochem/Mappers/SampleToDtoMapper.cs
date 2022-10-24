using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.Mappers;

public static class SampleToDtoMapper
{
    public static SampleWithLocationDto ToSampleWithLocationDto(this Sample sample, Lookups lookups,
        SampleWithLocationDto dto)
    {
        return SampleToDto(sample, lookups, null, null, dto);
    }

    public static SampleWithLocationDto ToSampleWithLocationDto(this Sample sample, Lookups lookups, int? dataPackageId,
        int? archiveId = null)
    {
        return SampleToDto(sample, lookups, dataPackageId, archiveId);
    }

    private static SampleWithLocationDto SampleToDto(Sample sample, Lookups lookups, int? dataPackageId = null,
        int? archiveId = null, SampleWithLocationDto dto = null)
    {
        // If there is a current DTO we are Updating otherwise we are Creating
        if (dto != null)
            dto.SampleDto.LastEditedTimestamp = DateTime.UtcNow;
        else
            dto = new SampleWithLocationDto
            {
                SampleDto = new SampleDto(),
                LocationDto = new LocationDto(),
            };

        if(sample.LocationKind == "Unknown" && string.IsNullOrWhiteSpace(sample.DepthMax) && string.IsNullOrWhiteSpace(sample.DepthMin))
            dto.AutoSetElevationWriteConfig = true;

        if (dataPackageId != null)
            dto.SampleDto.DataPackageId = dataPackageId;

        if (archiveId != null)
            dto.SampleDto.ArchiveId = archiveId;

        // Name => SampleDto.ShortName, SampleDto.Name
        dto.ShortName = dto.SampleDto.Name = sample.Name;

        // Irn => SampleDto.SourceId
        dto.SampleDto.SourceId = sample.Irn;

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

        // LocationName => LocationDto.Name
        dto.LocationDto.Name = sample.LocationName;

        // LocationDescription => LocationDto.Description
        dto.LocationDto.Description = sample.LocationDescription;

        // UnitName => SampleDto.StratographicUnitName
        dto.SampleDto.StratographicUnitName = sample.UnitName;

        // LocationKind => SampleDto.LocationKindId, SampleDto.LocationKindName 
        var locationKind = lookups.LocationKindDtos.FirstOrDefault(x =>
            string.Equals(x.Name, sample.LocationKind, StringComparison.OrdinalIgnoreCase));
        if (locationKind != null)
        {
            dto.SampleDto.LocationKindId = locationKind.Id;
            dto.SampleDto.LocationKindName = locationKind.Name;
        }

        // Find Material name based on external CSV
        MaterialDto material;
        var materialsLookupMatch = lookups.MaterialNamePairs.FirstOrDefault(x =>
            string.Equals(x.MvName, sample.MineralId, StringComparison.OrdinalIgnoreCase));

        if (materialsLookupMatch != null)
        {
            // Found match within material name pairs
            material = lookups.MaterialDtos.FirstOrDefault(x =>
                string.Equals(x.Name, materialsLookupMatch.AusGeochemName, StringComparison.OrdinalIgnoreCase));

            if (material != null)
                Log.Logger.Debug(
                    "Material name {MineralId} found via CSV match - MvName: {MvName}, AusGeochemName: {AusGeochemName}, Sample name: {SampleName}",
                    sample.MineralId, materialsLookupMatch.MvName, materialsLookupMatch.AusGeochemName, sample.Name);
        }
        else
        {
            // Look for match within materialDtos directly
            material = lookups.MaterialDtos.FirstOrDefault(x =>
                string.Equals(x.Name, sample.MineralId, StringComparison.OrdinalIgnoreCase));

            if (material != null)
                Log.Logger.Debug(
                    "Material name {MineralId} found via exact match in material list - Material name: {MaterialName}, Sample name: {SampleName}",
                    sample.MineralId, material.Name, sample.Name);

            
            // Remove diacritics then look for match within materialDtos directly
            var mineralIdNoDiacritics = sample.MineralId.RemoveDiacritics();
            material = lookups.MaterialDtos.FirstOrDefault(x =>
                string.Equals(x.Name, mineralIdNoDiacritics, StringComparison.OrdinalIgnoreCase));
            
            if (material != null)
                Log.Logger.Debug(
                    "Material name {MineralIdNoDiacritics} found via removing diacritics then exact match in material list - Material name: {MaterialName}, Sample name: {SampleName}",
                    mineralIdNoDiacritics, material.Name, sample.Name);
        }

        // Assign material if match found
        if (material != null)
        {
            dto.SampleDto.MaterialId = material.Id;
            dto.SampleDto.MaterialName = material.Name;
        }
        else
        {
            Log.Logger.Debug("Material name {MineralId} not matched - Sample name: {SampleName}", sample.MineralId,
                sample.Name);
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
        var sampleKind = lookups.SampleKindDtos.FirstOrDefault(x =>
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