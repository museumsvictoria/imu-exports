using ImuExports.Tasks.AusGeochem.Endpoints;
using ImuExports.Tasks.AusGeochem.Handlers;
using ImuExports.Tasks.AusGeochem.Mappers;
using ImuExports.Tasks.AusGeochem.Models;
using Microsoft.Extensions.Options;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem;

public interface IAusGeochemApiClient
{
    Task Authenticate(CancellationToken stoppingToken);

    Task DeleteAllByDataPackageId(DataPackage package, CancellationToken stoppingToken);

    Task SendSamples(Lookups lookups, IList<Sample> samples, DataPackage package, CancellationToken stoppingToken);
}

public class AusGeochemApiApiClient : IAusGeochemApiClient
{
    private readonly AppSettings _appSettings;
    private readonly IAuthenticateEndpoint _authenticateEndpoint;
    private readonly ISampleEndpoint _sampleEndpoint;
    private readonly IImageApiHandler _imageApiHandler;
    private readonly ISamplePropertyApiHandler _samplePropertyApiHandler;
    private readonly IHostEnvironment _hostEnvironment;

    public AusGeochemApiApiClient(
        IOptions<AppSettings> appSettings,
        IAuthenticateEndpoint authenticateEndpoint,
        ISampleEndpoint sampleEndpoint,
        IImageApiHandler imageApiHandler,
        ISamplePropertyApiHandler samplePropertyApiHandler,
        IHostEnvironment hostEnvironment)
    {
        _appSettings = appSettings.Value;
        _authenticateEndpoint = authenticateEndpoint;
        _sampleEndpoint = sampleEndpoint;
        _imageApiHandler = imageApiHandler;
        _samplePropertyApiHandler = samplePropertyApiHandler;
        _hostEnvironment = hostEnvironment;
    }

    public async Task Authenticate(CancellationToken stoppingToken)
    {
        await _authenticateEndpoint.Authenticate(stoppingToken);
    }

    public async Task DeleteAllByDataPackageId(DataPackage package, CancellationToken stoppingToken)
    {
        // Throw if DataPackageId not known
        if (package.Id == null)
        {
            Log.Logger.Fatal("DataPackage Id is null, cannot continue without one, exiting");
            ArgumentNullException.ThrowIfNull(package.Id);
        }
        
        // Fetch all current SampleWithLocationDtos
        Log.Logger.Information("Fetching all current SampleWithLocationDtos within AusGeochem for Data Package {Discipline} ({DataPackageId})", package.Discipline, package.Id);
        var currentSampleDtos = await _sampleEndpoint.GetSamplesByPackageId(package.Id.Value, stoppingToken);

        Log.Logger.Information("Deleting all samples for Data Package {Discipline} ({DataPackageId})", package.Discipline, package.Id);
        var offset = 0;
        foreach (var sampleDto in currentSampleDtos)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            ArgumentNullException.ThrowIfNull(sampleDto.Id);
            
            // Delete sample
            await _sampleEndpoint.DeleteSample(sampleDto, stoppingToken);
            
            offset++;
            Log.Logger.Information("Delete all samples progress for Data Package {Discipline} ({DataPackageId})... {Offset}/{TotalResults}", package.Discipline, package.Id, offset, currentSampleDtos.Count);
        }
    }

    public async Task SendSamples(Lookups lookups, IList<Sample> samples, DataPackage package, CancellationToken stoppingToken)
    {
        if(!samples.Any())
            return;
        
        // Throw if DataPackageId not known
        if (package.Id == null)
        {
            Log.Logger.Fatal("DataPackage Id is null, cannot continue without one, exiting");
            ArgumentNullException.ThrowIfNull(package.Id);
        }

        // Fetch all current SampleWithLocationDtos
        Log.Logger.Information("Fetching all current SampleWithLocationDtos within AusGeochem for Data Package {Discipline} ({DataPackageId})", package.Discipline, package.Id);
        var currentSampleDtos = await _sampleEndpoint.GetSamplesByPackageId(package.Id.Value, stoppingToken);

        // Send/Delete samples
        Log.Logger.Information("Sending samples for Data Package {Discipline} ({DataPackageId})", package.Discipline, package.Id);
        var offset = 0;
        foreach (var sample in samples)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            var existingSampleDto = currentSampleDtos.SingleOrDefault(x =>
                string.Equals(x.SampleDto.SourceId, sample.Irn, StringComparison.OrdinalIgnoreCase));

            if (existingSampleDto != null)
            {
                var updatedSampleDto = sample.ToSampleWithLocationDto(lookups, _hostEnvironment.IsProduction(), existingSampleDto);
                ArgumentNullException.ThrowIfNull(updatedSampleDto.Id);

                if (sample.Deleted)
                {
                    // Delete sample
                    await _sampleEndpoint.DeleteSample(updatedSampleDto, stoppingToken);
                }
                else
                {
                    // Update sample
                    _ = await _sampleEndpoint.SendSample(updatedSampleDto, Method.Put, stoppingToken);
                    
                    // Update images
                    await _imageApiHandler.Update(updatedSampleDto.Id.Value, sample.Images, stoppingToken);
                    
                    // Update sample properties
                    await _samplePropertyApiHandler.Update(updatedSampleDto.Id.Value, sample.Properties, stoppingToken);
                }
            }
            else
            {
                var createSampleDto = sample.ToSampleWithLocationDto(lookups, _hostEnvironment.IsProduction(), package.Id, _appSettings.AusGeochem.ArchiveId);

                if (!sample.Deleted)
                {
                    // Create sample
                    createSampleDto = await _sampleEndpoint.SendSample(createSampleDto, Method.Post, stoppingToken);
                    
                    ArgumentNullException.ThrowIfNull(createSampleDto.Id);
                
                    // Create images
                    await _imageApiHandler.Create(createSampleDto.Id.Value, sample.Images, stoppingToken);
                    
                    // Create sample properties
                    await _samplePropertyApiHandler.Create(createSampleDto.Id.Value, sample.Properties, stoppingToken);
                }
                else
                    Log.Logger.Debug("Nothing to do with Sample {ShortName} as it is marked for deletion but doesnt exist in AusGeochem", createSampleDto.ShortName);
            }

            offset++;
            Log.Logger.Information("Send samples progress for Data Package {Discipline} ({DataPackageId})... {Offset}/{TotalResults}", package.Discipline, package.Id, offset, samples.Count);
        }

        var samplesByIrn = samples.Select(x => new { x.Irn, x.Deleted }).ToList();
        var currentSamplesByIrn = currentSampleDtos.Select(x => x.SampleDto.SourceId).ToList();

        Log.Logger.Information(
            "Created {CountCreated}, Updated {CountUpdated}, Deleted {CountDeleted}, Did nothing with {CountNothingToDo} out of a total of {TotalSamples} Samples", 
            samplesByIrn.ExceptBy(currentSamplesByIrn, x => x.Irn).Count(),
            samplesByIrn.IntersectBy(currentSamplesByIrn, x => x.Irn).Count(),
            samplesByIrn.IntersectBy(currentSamplesByIrn, x => x.Irn).Count(x => x.Deleted),
            samplesByIrn.ExceptBy(currentSamplesByIrn, x => x.Irn).Count(x => x.Deleted),
            samples.Count);
    }
}