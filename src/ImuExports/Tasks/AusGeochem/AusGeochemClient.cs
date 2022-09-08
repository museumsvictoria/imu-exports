using ImageMagick;
using IMu;
using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using ImuExports.Tasks.AusGeochem.Endpoints;
using ImuExports.Tasks.AusGeochem.Mapping;
using ImuExports.Tasks.AusGeochem.Models;
using Microsoft.Extensions.Options;
using RestSharp;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace ImuExports.Tasks.AusGeochem;

public interface IAusGeochemClient
{
    Task Authenticate(CancellationToken stoppingToken);

    Task DeleteAllByDataPackageId(int? dataPackageId, CancellationToken stoppingToken);

    Task SendSamples(Lookups lookups, IList<Sample> samples, int? dataPackageId, CancellationToken stoppingToken);
}

public class AusGeochemClient : IAusGeochemClient
{
    private readonly AppSettings _appSettings;
    private readonly IAuthenticateEndpoint _authenticateEndpoint;
    private readonly IImageEndpoint _imageEndpoint;
    private readonly ISampleEndpoint _sampleEndpoint;

    public AusGeochemClient(
        IOptions<AppSettings> appSettings,
        IAuthenticateEndpoint authenticateEndpoint,
        IImageEndpoint imageEndpoint,
        ISampleEndpoint sampleEndpoint)
    {
        _appSettings = appSettings.Value;
        _authenticateEndpoint = authenticateEndpoint;
        _imageEndpoint = imageEndpoint;
        _sampleEndpoint = sampleEndpoint;
    }

    public async Task Authenticate(CancellationToken stoppingToken)
    {
        await _authenticateEndpoint.Authenticate(stoppingToken);
    }

    public async Task DeleteAllByDataPackageId(int? dataPackageId, CancellationToken stoppingToken)
    {
        // Exit if DataPackageId not known
        if (dataPackageId == null)
        {
            Log.Logger.Fatal("DataPackageId is null, cannot continue without one, exiting");
            Environment.Exit(Constants.ExitCodeError);
        }
        
        // Fetch all current SampleWithLocationDtos
        Log.Logger.Information("Fetching all current SampleWithLocationDtos within AusGeochem for DataPackageId {DataPackageId}", dataPackageId);
        var currentSampleDtos = await _sampleEndpoint.GetSamplesByPackageId(dataPackageId.Value, stoppingToken);

        Log.Logger.Information("Deleting all entities for DataPackageId {DataPackageId}", dataPackageId);
        foreach (var sampleDto in currentSampleDtos)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            IList<ImageDto> imageDtos = new List<ImageDto>();
            // Fetch all images associated with sample
            if (sampleDto.Id != null)
                imageDtos = await _imageEndpoint.GetImagesBySampleId(sampleDto.Id.Value, stoppingToken);

            // Delete all images
            foreach (var imageDto in imageDtos)
            {
                stoppingToken.ThrowIfCancellationRequested();
                
                await _imageEndpoint.DeleteImage(imageDto, stoppingToken);
            }
            
            // Delete sample
            await _sampleEndpoint.DeleteSample(sampleDto, stoppingToken);
        }
    }

    public async Task SendSamples(Lookups lookups, IList<Sample> samples, int? dataPackageId, CancellationToken stoppingToken)
    {
        if(!samples.Any())
            return;
        
        // Exit if DataPackageId not known
        if (dataPackageId == null)
        {
            Log.Logger.Fatal("DataPackageId is null, cannot continue without one, exiting");
            Environment.Exit(Constants.ExitCodeError);
        }

        // Fetch all current SampleWithLocationDtos
        Log.Logger.Information("Fetching all current SampleWithLocationDtos within AusGeochem for DataPackageId {DataPackageId}", dataPackageId);
        var currentSampleDtos = await _sampleEndpoint.GetSamplesByPackageId(dataPackageId.Value, stoppingToken);

        // Send/Delete samples
        Log.Logger.Information("Sending samples for DataPackageId {DataPackageId}", dataPackageId);
        var offset = 0;
        foreach (var sample in samples)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            var existingSampleDto = currentSampleDtos.SingleOrDefault(x =>
                string.Equals(x.SampleDto.SourceId, sample.Irn, StringComparison.OrdinalIgnoreCase));

            if (existingSampleDto != null)
            {
                var updatedSampleDto = sample.ToSampleWithLocationDto(lookups, existingSampleDto);

                if (sample.Deleted)
                {
                    // Delete sample
                    await _sampleEndpoint.DeleteSample(updatedSampleDto, stoppingToken);
                    
                    // Delete images
                    IList<ImageDto> imageDtos = new List<ImageDto>();
                    
                    // Fetch all images associated with sample
                    if (updatedSampleDto.Id != null)
                        imageDtos = await _imageEndpoint.GetImagesBySampleId(updatedSampleDto.Id.Value, stoppingToken);

                    // Delete all images
                    foreach (var imageDto in imageDtos)
                    {
                        await _imageEndpoint.DeleteImage(imageDto, stoppingToken);
                    }
                }
                else
                {
                    // Update sample
                    await _sampleEndpoint.SendSample(updatedSampleDto, Method.Put, stoppingToken);

                    // Update images
                    IList<ImageDto> imageDtos = new List<ImageDto>();
                    
                    // Fetch all images associated with sample
                    if (updatedSampleDto.Id != null)
                        imageDtos = await _imageEndpoint.GetImagesBySampleId(updatedSampleDto.Id.Value, stoppingToken);

                    // Delete all images
                    foreach (var imageDto in imageDtos)
                    {
                        await _imageEndpoint.DeleteImage(imageDto, stoppingToken);
                    }

                    // Re-Send all images
                    foreach (var image in sample.Images)
                    {
                        // Fetch image as base64 string from IMu
                        var base64Image = await FetchImageAsBase64(stoppingToken, image);

                        if (updatedSampleDto.Id != null)
                            await _imageEndpoint.SendImage(image, base64Image, updatedSampleDto.Id.Value, stoppingToken);
                    }
                }
            }
            else
            {
                var createSampleDto = sample.ToSampleWithLocationDto(lookups, dataPackageId, _appSettings.AusGeochem.ArchiveId);

                if (!sample.Deleted)
                {
                    // Create sample
                    await _sampleEndpoint.SendSample(createSampleDto, Method.Post, stoppingToken);

                    // Create Images
                    if (sample.Images.Any())
                    {
                        // Get created sample so we can link sample to image via id
                        createSampleDto = await _sampleEndpoint.GetSampleBySourceId(sample.Irn, stoppingToken);

                        foreach (var image in sample.Images)
                        {
                            // Fetch image as base64 string from IMu
                            var base64Image = await FetchImageAsBase64(stoppingToken, image);

                            if (createSampleDto.Id != null)
                                await _imageEndpoint.SendImage(image, base64Image, createSampleDto.Id.Value, stoppingToken);
                        }
                    }
                }
                else
                    Log.Logger.Debug("Nothing to do with Sample {ShortName} as it is marked for deletion but doesnt exist in AusGeochem", createSampleDto.ShortName);
            }

            offset++;
            Log.Logger.Information("Send samples progress... {Offset}/{TotalResults}", offset, samples.Count);
        }
    }
    
    //TODO: move this somewhere sensible
    private async Task<string> FetchImageAsBase64(CancellationToken stoppingToken, Image image)
    {
        try
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            var stopwatch = Stopwatch.StartNew();
            
            using var imuSession = new ImuSession("emultimedia", _appSettings.Imu.Host, _appSettings.Imu.Port);
            imuSession.FindKey(image.Irn);
            var resource = imuSession.Fetch("start", 0, -1, new[] { "resource" }).Rows[0].GetMap("resource");

            if (resource == null)
                throw new IMuException("MultimediaResourceNotFound");

            await using var sourceFileStream = resource["file"] as FileStream;

            using var imageResource = new MagickImage(sourceFileStream);

            imageResource.Format = MagickFormat.Jpg;
            imageResource.Quality = 90;
            imageResource.FilterType = FilterType.Lanczos;
            imageResource.ColorSpace = ColorSpace.sRGB;
            imageResource.Resize(new MagickGeometry(3000) { Greater = true });
            imageResource.UnsharpMask(0.5, 0.5, 0.6, 0.025);

            var base64Image = imageResource.ToBase64();

            stopwatch.Stop();
            
            Log.Logger.Debug("Completed fetching image {Irn} in {ElapsedMilliseconds}ms", image.Irn,
                stopwatch.ElapsedMilliseconds);

            return base64Image;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error fetching image {Irn}, exiting", image.Irn);
            Environment.Exit(Constants.ExitCodeError);
        }
        
        return null;
    }
}