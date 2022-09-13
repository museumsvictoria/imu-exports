using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ImuExports.Tasks.AusGeochem.ClassMaps;
using ImuExports.Tasks.AusGeochem.Endpoints;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.Factories;

public class LookupsFactory : IFactory<Lookups>
{
    private readonly ILocationKindEndpoint _locationKindEndpoint;
    private readonly IMaterialEndpoint _materialEndpoint;
    private readonly ISampleKindEndpoint _sampleKindEndpoint;

    public LookupsFactory(
        ILocationKindEndpoint locationKindEndpoint,
        IMaterialEndpoint materialEndpoint,
        ISampleKindEndpoint sampleKindEndpoint)
    {
        _locationKindEndpoint = locationKindEndpoint;
        _materialEndpoint = materialEndpoint;
        _sampleKindEndpoint = sampleKindEndpoint;
    }
    
    public async Task<Lookups> Make(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        // Lookups fetched from API
        var lookups = new Lookups
        {
            LocationKindDtos = await _locationKindEndpoint.GetAll(stoppingToken),
            MaterialDtos = await _materialEndpoint.GetAll(stoppingToken),
            SampleKindDtos = await _sampleKindEndpoint.GetAll(stoppingToken),
        };

        // CSV based material name pairs for matching MV material name to AusGeochem material name
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
        
        using var reader = new StreamReader($"{AppContext.BaseDirectory}material-name-pairs.csv");
        using var csv = new CsvReader(reader, csvConfig);
        csv.Context.RegisterClassMap<MaterialNamePairClassMap>();
        
        lookups.MaterialNamePairs = csv.GetRecords<MaterialNamePair>().ToList();

        return lookups;
    }
}