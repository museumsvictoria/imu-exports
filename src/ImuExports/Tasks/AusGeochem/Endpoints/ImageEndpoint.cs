using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using ImuExports.Tasks.AusGeochem.Contracts.Requests;
using ImuExports.Tasks.AusGeochem.Models;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem.Endpoints;

public interface IImageEndpoint
{
    Task CreateImage(Image image, string base64Image, int sampleId, CancellationToken stoppingToken);
    
    Task<IList<ImageDto>> GetImagesBySampleId(int sampleId, CancellationToken stoppingToken);
    
    Task DeleteImage(ImageDto dto, CancellationToken stoppingToken);
}

public class ImageEndpoint : EndpointBase, IImageEndpoint
{
    private readonly RestClient _client;

    public ImageEndpoint(
        RestClient client) : base(client)
    {
        _client = client;
    }
    
    public async Task CreateImage(Image image, string base64Image, int sampleId, CancellationToken stoppingToken)
    {
        // Build request
        var request = new RestRequest("core/images/add-to-sample", Method.Post).AddJsonBody(new AddImageToSampleRequest
        {
            AltText = image.AltText,
            Content = base64Image,
            ContentType = "image/jpeg",
            Creator = image.Creator,
            Description = image.Description,
            License = image.License,
            Name = image.Name,
            RightsHolder = image.RightsHolder
        });

        // Add sampleId we want to attach image to
        request.AddQueryParameter("sampleId", sampleId);

        // Send image
        Log.Logger.Debug("Sending Request for {Url} via {Method}", _client.BuildUri(request), request.Method);
        var response = await ExecuteWithPolicyAsync(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Error occured sending image {Irn} attached to sample {SampleId}", image.Irn, sampleId);
            throw RestException.CreateException(response);
        }
        else
        {
            Log.Logger.Debug("Sent image {Irn} via {Method} attached to sample {SampleId}, status {ResponseStatus}",
                image.Irn, request.Method, sampleId, response.ResponseStatus);
        }
    }
    
    public async Task<IList<ImageDto>> GetImagesBySampleId(int sampleId, CancellationToken stoppingToken)
    {
        // Build parameters
        var parameters = new ParametersCollection();
        parameters.AddParameter(new QueryParameter("sampleId", sampleId.ToString()));

        // GetAll ImageReadDtos
        return await GetAll<ImageDto>("core/images/of-sample", stoppingToken, parameters);
    }
    
    public async Task DeleteImage(ImageDto dto, CancellationToken stoppingToken)
    {
        // Build request
        var request = new RestRequest($"core/images/{dto.Id}")
        {
            Method = Method.Delete
        };

        // Delete image
        Log.Logger.Debug("Sending Request for {Url} via {Method}", _client.BuildUri(request), request.Method);
        var response = await ExecuteWithPolicyAsync(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Error occured deleting image {Id}", dto.Id);
            throw RestException.CreateException(response);
        }
        else
        {
            Log.Logger.Debug("Deleted image {Name}, status {ResponseStatus}",
                dto.Name, response.ResponseStatus);
        }
    }
}