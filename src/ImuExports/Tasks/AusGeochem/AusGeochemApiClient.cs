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

    public AusGeochemApiApiClient(
        IOptions<AppSettings> appSettings,
        IAuthenticateEndpoint authenticateEndpoint,
        ISampleEndpoint sampleEndpoint,
        IImageApiHandler imageApiHandler,
        ISamplePropertyApiHandler samplePropertyApiHandler)
    {
        _appSettings = appSettings.Value;
        _authenticateEndpoint = authenticateEndpoint;
        _sampleEndpoint = sampleEndpoint;
        _imageApiHandler = imageApiHandler;
        _samplePropertyApiHandler = samplePropertyApiHandler;
    }

    public async Task Authenticate(CancellationToken stoppingToken)
    {
        await _authenticateEndpoint.Authenticate(stoppingToken);
    }

    public async Task DeleteAllByDataPackageId(DataPackage package, CancellationToken stoppingToken)
    {
        // Exit if DataPackageId not known
        if (package.Id == null)
        {
            Log.Logger.Fatal("DataPackage Id is null, cannot continue without one, exiting");
            Environment.Exit(Constants.ExitCodeError);
        }
        
        // Fetch all current SampleWithLocationDtos
        Log.Logger.Information("Fetching all current SampleWithLocationDtos within AusGeochem for Data Package {Discipline} ({DataPackageId})", package.Discipline, package.Id);
        var currentSampleDtos = await _sampleEndpoint.GetSamplesByPackageId(package.Id.Value, stoppingToken);

        Log.Logger.Information("Deleting all entities for Data Package {Discipline} ({DataPackageId})", package.Discipline, package.Id);
        foreach (var sampleDto in currentSampleDtos)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            ArgumentNullException.ThrowIfNull(sampleDto.Id);
            
            // Delete images
            await _imageApiHandler.Delete(sampleDto.Id.Value, stoppingToken);
            
            // Delete sample properties
            await _samplePropertyApiHandler.Delete(sampleDto.Id.Value, stoppingToken);

            // Delete sample
            await _sampleEndpoint.DeleteSample(sampleDto, stoppingToken);
        }
    }

    public async Task SendSamples(Lookups lookups, IList<Sample> samples, DataPackage package, CancellationToken stoppingToken)
    {
        if(!samples.Any())
            return;
        
        // Exit if DataPackageId not known
        if (package.Id == null)
        {
            Log.Logger.Fatal("DataPackageId is null, cannot continue without one, exiting");
            Environment.Exit(Constants.ExitCodeError);
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
                var updatedSampleDto = sample.ToSampleWithLocationDto(lookups, existingSampleDto);
                ArgumentNullException.ThrowIfNull(updatedSampleDto.Id);

                if (sample.Deleted)
                {
                    // Delete images
                    await _imageApiHandler.Delete(updatedSampleDto.Id.Value, stoppingToken);
                    
                    // Delete sample properties
                    await _samplePropertyApiHandler.Delete(updatedSampleDto.Id.Value, stoppingToken);

                    // Delete sample
                    await _sampleEndpoint.DeleteSample(updatedSampleDto, stoppingToken);
                }
                else
                {
                    // Update sample
                    await _sampleEndpoint.SendSample(updatedSampleDto, Method.Put, stoppingToken);
                    
                    // Update images
                    await _imageApiHandler.Update(updatedSampleDto.Id.Value, sample.Images, stoppingToken);
                    
                    // Update sample properties
                    await _samplePropertyApiHandler.Update(updatedSampleDto.Id.Value, sample.Properties, stoppingToken);
                }
            }
            else
            {
                var createSampleDto = sample.ToSampleWithLocationDto(lookups, package.Id, _appSettings.AusGeochem.ArchiveId);

                if (!sample.Deleted)
                {
                    // Create sample
                    await _sampleEndpoint.SendSample(createSampleDto, Method.Post, stoppingToken);

                    if (sample.Images.Any() || sample.Properties.Any())
                    {
                        // Get created sample so we can link sample to images and sample properties
                        createSampleDto = await _sampleEndpoint.GetSampleBySourceId(sample.Irn, stoppingToken);
                    }
                    
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
    }
}