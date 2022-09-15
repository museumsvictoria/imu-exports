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
    private readonly ISamplePropertyEndpoint _samplePropertyEndpoint;

    public AusGeochemClient(
        IOptions<AppSettings> appSettings,
        IAuthenticateEndpoint authenticateEndpoint,
        IImageEndpoint imageEndpoint,
        ISampleEndpoint sampleEndpoint,
        ISamplePropertyEndpoint samplePropertyEndpoint)
    {
        _appSettings = appSettings.Value;
        _authenticateEndpoint = authenticateEndpoint;
        _imageEndpoint = imageEndpoint;
        _sampleEndpoint = sampleEndpoint;
        _samplePropertyEndpoint = samplePropertyEndpoint;
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
            
            // Delete images
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
            
            // Delete sample properties
            IList<SamplePropertyDto> currentSamplePropertiesDtos = new List<SamplePropertyDto>();
                    
            // Fetch all sample properties associated with sample
            if (sampleDto.Id != null)
                currentSamplePropertiesDtos = await _samplePropertyEndpoint.GetSamplePropertiesBySampleId(sampleDto.Id.Value,
                    stoppingToken);

            // Delete all sample properties
            foreach (var samplePropertyDto in currentSamplePropertiesDtos)
            {
                await _samplePropertyEndpoint.DeleteSampleProperty(samplePropertyDto, stoppingToken);
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
                    
                    // Delete sample properties
                    IList<SamplePropertyDto> currentSamplePropertiesDtos = new List<SamplePropertyDto>();
                    
                    // Fetch all sample properties associated with sample
                    if (updatedSampleDto.Id != null)
                        currentSamplePropertiesDtos = await _samplePropertyEndpoint.GetSamplePropertiesBySampleId(updatedSampleDto.Id.Value,
                            stoppingToken);

                    // Delete all sample properties
                    foreach (var samplePropertyDto in currentSamplePropertiesDtos)
                    {
                        await _samplePropertyEndpoint.DeleteSampleProperty(samplePropertyDto, stoppingToken);
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
                            await _imageEndpoint.CreateImage(image, base64Image, updatedSampleDto.Id.Value, stoppingToken);
                    }
                    
                    // Fetch all current SamplePropertyDtos
                    IList<SamplePropertyDto> currentSamplePropertiesDtos = new List<SamplePropertyDto>();
                    if (updatedSampleDto.Id != null)
                        currentSamplePropertiesDtos = await _samplePropertyEndpoint.GetSamplePropertiesBySampleId(updatedSampleDto.Id.Value,
                            stoppingToken);
                    
                    // Update all sample properties
                    foreach (var sampleProperty in sample.Properties)
                    {
                        var existingSamplePropertyDto = currentSamplePropertiesDtos.SingleOrDefault(x =>
                            string.Equals(x.PropName, sampleProperty.Property.Key, StringComparison.OrdinalIgnoreCase));

                        if (existingSamplePropertyDto != null)
                        {
                            // Update sample property
                            var updatedSamplePropertyDto = sampleProperty.ToSamplePropertyDto(existingSamplePropertyDto);

                            await _samplePropertyEndpoint.SendSampleProperty(updatedSamplePropertyDto, Method.Put,
                                stoppingToken);
                        }
                        else
                        {
                            // Create sample property
                            var createSamplePropertyDto = sampleProperty.ToSamplePropertyDto(updatedSampleDto.Id.Value);
                            await _samplePropertyEndpoint.SendSampleProperty(createSamplePropertyDto, Method.Post, stoppingToken);
                        }
                    }

                    // Delete sample properties that dont exist in properties but do in AusGeochem
                    foreach (var samplePropertyDto in currentSamplePropertiesDtos.Where(x =>
                                 sample.Properties.All(y => y.Property.Key != x.PropName)))
                        await _samplePropertyEndpoint.DeleteSampleProperty(samplePropertyDto, stoppingToken);
                }
            }
            else
            {
                var createSampleDto = sample.ToSampleWithLocationDto(lookups, dataPackageId, _appSettings.AusGeochem.ArchiveId);

                if (!sample.Deleted)
                {
                    // Create sample
                    await _sampleEndpoint.SendSample(createSampleDto, Method.Post, stoppingToken);

                    if (sample.Images.Any() || sample.Properties.Any())
                    {
                        // Get created sample so we can link sample to images and sample properties
                        createSampleDto = await _sampleEndpoint.GetSampleBySourceId(sample.Irn, stoppingToken);
                    }
                    
                    // Create Images
                    foreach (var image in sample.Images)
                    {
                        // Fetch image as base64 string from IMu
                        var base64Image = await FetchImageAsBase64(stoppingToken, image);

                        if (createSampleDto.Id != null)
                            await _imageEndpoint.CreateImage(image, base64Image, createSampleDto.Id.Value, stoppingToken);
                    }

                    // Create all sample properties
                    foreach (var sampleProperty in sample.Properties)
                        if (createSampleDto.Id != null)
                        {
                            var createSamplePropertyDto = sampleProperty.ToSamplePropertyDto(createSampleDto.Id.Value);
                            await _samplePropertyEndpoint.SendSampleProperty(createSamplePropertyDto, Method.Post,
                                stoppingToken);
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