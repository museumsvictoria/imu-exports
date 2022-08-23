using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using ImuExports.Tasks.AusGeochem.ClassMaps;
using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.Factories;

public class LookupsFactory : IFactory<Lookups>
{
    private readonly IAusGeochemClient _ausGeochemClient;

    public LookupsFactory(IAusGeochemClient ausGeochemClient)
    {
        _ausGeochemClient = ausGeochemClient;
    }
    
    public async Task<Lookups> Make(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        // Lookups fetched from API
        var lookups = new Lookups
        {
            LocationKindDtos = await _ausGeochemClient.GetAll<LocationKindDto>("core/l-location-kinds", stoppingToken, null, Constants.RestClientLargePageSize),
            MaterialDtos = await _ausGeochemClient.GetAll<MaterialDto>("core/materials", stoppingToken, null, Constants.RestClientLargePageSize),
            SampleKindDtos = await _ausGeochemClient.GetAll<SampleKindDto>("core/l-sample-kinds", stoppingToken, null, Constants.RestClientLargePageSize),
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