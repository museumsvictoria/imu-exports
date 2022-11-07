using ImuExports.Tasks.AusGeochem.Endpoints;
using ImuExports.Tasks.AusGeochem.Mappers;
using ImuExports.Tasks.AusGeochem.Models;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem.Handlers;

public interface ISamplePropertyApiHandler
{
    Task Update(int sampleId, IList<SampleProperty> sampleProperties, CancellationToken stoppingToken);
    
    Task Create(int sampleId, IList<SampleProperty> sampleProperties, CancellationToken stoppingToken);
}

public class SamplePropertyApiHandler : ISamplePropertyApiHandler
{
    private readonly ISamplePropertyEndpoint _samplePropertyEndpoint;
    
    public SamplePropertyApiHandler(
        ISamplePropertyEndpoint samplePropertyEndpoint)
    {
        _samplePropertyEndpoint = samplePropertyEndpoint;
    }
    
    public async Task Update(int sampleId, IList<SampleProperty> sampleProperties,  CancellationToken stoppingToken)
    {
        // Fetch all sample properties associated with sample
        var dtos = await _samplePropertyEndpoint.GetSamplePropertiesBySampleId(sampleId, stoppingToken);
                    
        // Update all sample properties
        foreach (var sampleProperty in sampleProperties)
        {
            var existingDto = dtos.SingleOrDefault(x =>
                string.Equals(x.PropName, sampleProperty.Name, StringComparison.OrdinalIgnoreCase));

            if (existingDto != null)
            {
                // Update sample property
                var updatedDto = sampleProperty.ToSamplePropertyDto(existingDto);

                await _samplePropertyEndpoint.SendSampleProperty(updatedDto, Method.Put, stoppingToken);
            }
            else
            {
                // Create sample property
                var createDto = sampleProperty.ToSamplePropertyDto(sampleId);
                await _samplePropertyEndpoint.SendSampleProperty(createDto, Method.Post, stoppingToken);
            }
        }

        // Delete sample properties that dont exist in properties but do in AusGeochem
        foreach (var samplePropertyDto in dtos.Where(x => sampleProperties.All(y => y.Name != x.PropName)))
            await _samplePropertyEndpoint.DeleteSampleProperty(samplePropertyDto, stoppingToken);
    }

    public async Task Create(int sampleId, IList<SampleProperty> sampleProperties, CancellationToken stoppingToken)
    {
        // Create all sample properties
        foreach (var sampleProperty in sampleProperties)
        {
            var dto = sampleProperty.ToSamplePropertyDto(sampleId);
            
            await _samplePropertyEndpoint.SendSampleProperty(dto, Method.Post, stoppingToken);
        }
    }
}

