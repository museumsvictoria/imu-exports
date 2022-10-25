using ImuExports.Tasks.AusGeochem.Endpoints;
using ImuExports.Tasks.AusGeochem.Factories;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.Handlers;

public interface IImageApiHandler
{
    Task Update(int sampleId, IList<Image> images, CancellationToken stoppingToken);

    Task Create(int sampleId, IList<Image> images, CancellationToken stoppingToken);
}

public class ImageApiHandler : IImageApiHandler
{
    private readonly IImageEndpoint _imageEndpoint;
    private readonly IBase64ImageFactory _base64ImageFactory;
    
    public ImageApiHandler(
        IImageEndpoint imageEndpoint,
        IBase64ImageFactory base64ImageFactory)
    {
        _imageEndpoint = imageEndpoint;
        _base64ImageFactory = base64ImageFactory;
    }

    public async Task Update(int sampleId, IList<Image> images, CancellationToken stoppingToken)
    {
        // Fetch all images associated with sample
        var dtos = await _imageEndpoint.GetImagesBySampleId(sampleId, stoppingToken);

        // Delete all images
        foreach (var dto in dtos)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            await _imageEndpoint.DeleteImage(dto, stoppingToken);
        }

        // Re-Send all images
        foreach (var image in images)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            // Fetch image as base64 string from IMu
            var base64Image = await _base64ImageFactory.Make(image.Irn, stoppingToken);

            if (!string.IsNullOrEmpty(base64Image))
                await _imageEndpoint.CreateImage(image, base64Image, sampleId, stoppingToken);
        }
    }
    
    public async Task Create(int sampleId, IList<Image> images, CancellationToken stoppingToken)
    {
        foreach (var image in images)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            // Fetch image as base64 string from IMu
            var base64Image = await _base64ImageFactory.Make(image.Irn, stoppingToken);

            if (!string.IsNullOrEmpty(base64Image))
                await _imageEndpoint.CreateImage(image, base64Image, sampleId, stoppingToken);
        }
    }
}

