using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.Mapping;

public static class SamplePropertyToDtoMapper
{
    public static SamplePropertyDto ToSamplePropertyDto(this SampleProperty sampleProperty, SamplePropertyDto dto)
    {
        return ToSamplePropertyDto(sampleProperty, null, dto);
    }
    
    public static SamplePropertyDto ToSamplePropertyDto(this SampleProperty sampleProperty, int? sampleId = null, SamplePropertyDto dto = null)
    {
        dto ??= new SamplePropertyDto();

        if (sampleId != null)
            dto.SampleId = sampleId;

        dto.PropName = sampleProperty.Property.Key;
        dto.PropValue = sampleProperty.Property.Value;

        return dto;
    }
}