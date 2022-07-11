using ImuExports.Tasks.AusGeochem.Models;
using ImuExports.Tasks.AusGeochem.Models.Api;

namespace ImuExports.Tasks.AusGeochem.Extensions;

public static class SampleExtensions
{
    public static SampleWithLocationDto ToSampleWithLocationDto(this Sample sample)
    {
        var dto = new SampleWithLocationDto()
        {
            SampleDto = new SampleDto(),
            LocationDto = new LocationDto()
        };

        dto.ShortName = sample.SampleId;
        dto.SampleDto.Name = sample.SampleId;
        dto.SampleDto.SampleId = sample.SampleId;
        dto.SampleDto.ArchiveNote = sample.ArchiveNotes;
        
        if(long.TryParse(sample.Latitude, out var latitude))
            dto.LocationDto.Lat = latitude;
        if(long.TryParse(sample.Longitude, out var longitude))
            dto.LocationDto.Lon = longitude;
        if(int.TryParse(sample.LatLongPrecision, out var latLongPrecision))
            dto.LocationDto.LatLonPrecision = latLongPrecision;

        dto.LocationDto.Description = sample.LocationNotes;

        return dto;
    }
    
}