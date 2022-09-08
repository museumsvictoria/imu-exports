using System.Text.Json;
using System.Text.Json.Serialization;
using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using ImuExports.Tasks.AusGeochem.Contracts.Requests;
using ImuExports.Tasks.AusGeochem.Contracts.Responses;
using ImuExports.Tasks.AusGeochem.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.Json;

namespace ImuExports.Tasks.AusGeochem;

public interface IAusGeochemClient
{
    Task Authenticate(CancellationToken stoppingToken);

    Task<IList<SampleWithLocationDto>> GetSamplesByPackageId(int dataPackageId, CancellationToken stoppingToken);
    
    Task<SampleWithLocationDto> GetSampleBySourceId(string sourceId, CancellationToken stoppingToken);

    Task SendSample(SampleWithLocationDto dto, Method method, CancellationToken stoppingToken);

    Task DeleteSample(SampleWithLocationDto dto, CancellationToken stoppingToken);

    Task SendImage(Image image, string base64Image, int sampleId, CancellationToken stoppingToken);
    
    Task DeleteImage(ImageDto dto, CancellationToken stoppingToken);

    Task<IList<ImageDto>> GetImagesBySampleId(int sampleId, CancellationToken stoppingToken);

    Task<IList<T>> GetAll<T>(string resource, CancellationToken stoppingToken,
        ParametersCollection parameters = null, int pageSize = Constants.RestClientSmallPageSize);
}

public class AusGeochemClient : IAusGeochemClient, IDisposable
{
    private readonly AppSettings _appSettings;
    private readonly RestClient _client;

    public AusGeochemClient(
        IOptions<AppSettings> appSettings,
        RestClient client)
    {
        _appSettings = appSettings.Value;
        _client = client;
    }

    public async Task Authenticate(CancellationToken stoppingToken)
    {
        // Build request
        var request = new RestRequest("authenticate", Method.Post).AddJsonBody(new AuthenticateRequest
        {
            Password = _appSettings.AusGeochem.Password,
            Username = _appSettings.AusGeochem.Username
        });

        // Request JWT
        Log.Logger.Debug("Sending Request for {Url} via {Method}", _client.BuildUri(request), request.Method);
        var response = await _client.ExecuteAsync<AuthenticateResponse>(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Could not successfully authenticate, exiting, {ErrorMessage}", response.ErrorMessage ?? response.Content);
            Environment.Exit(Constants.ExitCodeError);
        }
        else if (!string.IsNullOrWhiteSpace(response.Data?.Token))
        {
            // Set authenticator if successful
            _client.Authenticator = new JwtAuthenticator(response.Data.Token);
        }

        Log.Logger.Debug("Api authentication successful");
    }

    public async Task<IList<SampleWithLocationDto>> GetSamplesByPackageId(int dataPackageId,
        CancellationToken stoppingToken)
    {
        // Build parameters
        var parameters = new ParametersCollection();
        parameters.AddParameter(new QueryParameter("dataPackageId.equals", dataPackageId.ToString()));

        // GetAll SampleWithLocationDto
        return await GetAll<SampleWithLocationDto>("core/sample-with-locations", stoppingToken, parameters);
    }

    public async Task<SampleWithLocationDto> GetSampleBySourceId(string sourceId, CancellationToken stoppingToken)
    {
        // Build request
        var request = new RestRequest("core/sample-with-locations")
        {
            Method = Method.Get
        };
        
        request.AddQueryParameter("sourceId.equals", sourceId);
        
        // Get record based on RegNumber (sourceId) in AusGeochem
        Log.Logger.Debug("Sending Request for {Url} via {Method}", _client.BuildUri(request), request.Method);
        var response = await _client.ExecuteAsync<IList<SampleWithLocationDto>>(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Error occured fetching {Name} by SourceId {SourceId} at endpoint {Resource} via {Method} exiting, {ErrorMessage}",
                nameof(SampleWithLocationDto), sourceId, request.Resource, request.Method, response.ErrorMessage ?? response.Content);
            Environment.Exit(Constants.ExitCodeError);
        }

        return response.Data?.FirstOrDefault();
    }

    public async Task SendSample(SampleWithLocationDto dto, Method method, CancellationToken stoppingToken)
    {
        // Build request
        var request = new RestRequest("core/sample-with-locations")
        {
            Method = method
        };

        // Add dto
        request.AddJsonBody(dto);

        // Send sample
        Log.Logger.Debug("Sending Request for {Url} via {Method}", _client.BuildUri(request), request.Method);
        var response = await _client.ExecuteAsync(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Error occured sending sample {ShortName} at resource {Resource} via {Method} exiting, {ErrorMessage}", 
                dto.ShortName, request.Resource, request.Method, response.ErrorMessage ?? response.Content);
            Environment.Exit(Constants.ExitCodeError);
        }
        else
        {
            Log.Logger.Debug("Sent sample {ShortName} via {Method}, status {ResponseStatus}",
                dto.ShortName, request.Method, response.ResponseStatus);
        }
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
        var response = await _client.ExecuteAsync(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Error occured deleting image {Id} at resource {Resource} via {Method} exiting, {ErrorMessage}",
                dto.Id, request.Resource, request.Method, response.ErrorMessage ?? response.Content);
            Environment.Exit(Constants.ExitCodeError);
        }
        else
        {
            Log.Logger.Debug("Deleted image {Name}, status {ResponseStatus}",
                dto.Name, response.ResponseStatus);
        }
    }

    public async Task SendImage(Image image, string base64Image, int sampleId, CancellationToken stoppingToken)
    {
        // Build request
        var request = new RestRequest("core/images/add-to-sample", Method.Post).AddJsonBody(new AddImageToSampleRequest
        {
            Content = base64Image,
            Description = image.Description,
            Name = image.Name,
        });

        // Add sampleId we want to attach image to
        request.AddQueryParameter("sampleId", sampleId);

        // Send image
        Log.Logger.Debug("Sending Request for {Url} via {Method}", _client.BuildUri(request), request.Method);
        var response = await _client.ExecuteAsync(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Error occured sending image {Irn} via {Method} attached to sample {SampleId}, exiting, {ErrorMessage}", 
                image.Irn, request.Method, sampleId, response.ErrorMessage ?? response.Content);
            Environment.Exit(Constants.ExitCodeError);
        }
        else
        {
            Log.Logger.Debug("Sent image {Irn} via {Method} attached to sample {SampleId}, status {ResponseStatus}",
                image.Irn, request.Method, sampleId, response.ResponseStatus);
        }
    }
    
    public async Task DeleteSample(SampleWithLocationDto dto, CancellationToken stoppingToken)
    {
        // Build request
        var request = new RestRequest($"core/sample-with-locations/{dto.Id}")
        {
            Method = Method.Delete
        };

        // Delete sample
        Log.Logger.Debug("Sending Request for {Url} via {Method}", _client.BuildUri(request), request.Method);
        var response = await _client.ExecuteAsync(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Error occured deleting sample {ShortName} at resource {Resource} via {Method} exiting, {ErrorMessage}",
                dto.ShortName, request.Resource, request.Method, response.ErrorMessage ?? response.Content);
            Environment.Exit(Constants.ExitCodeError);
        }
        else
        {
            Log.Logger.Debug("Deleted sample {ShortName}, status {ResponseStatus}",
                dto.ShortName, response.ResponseStatus);
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

    public async Task<IList<T>> GetAll<T>(string resource, CancellationToken stoppingToken,
        ParametersCollection parameters = null, int pageSize = Constants.RestClientSmallPageSize)
    {
        var dtos = new List<T>();
        
        // Build request
        var request = new RestRequest(resource);

        // Add size parameter for pagination size
        request.AddQueryParameter("size", pageSize);

        // Add any passed in parameters
        if (parameters != null)
            foreach (var parameter in parameters)
                request.AddParameter(parameter);
        
        // Ensure there is a sort parameter attached otherwise we may get inconsistent results 
        if(request.Parameters.All(x => x.Name != "sort"))
            request.AddParameter(new QueryParameter("sort", "id,DESC", false));

        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            Log.Logger.Debug("Sending Request for {Url} via {Method}", _client.BuildUri(request), request.Method);
            var response = await _client.ExecuteAsync<IList<T>>(request, stoppingToken);

            if (!response.IsSuccessful)
            {
                Log.Logger.Fatal("Error occured fetching {Name} at endpoint {Resource} via {Method} exiting, {ErrorMessage}",
                    typeof(T).Name, request.Resource, request.Method, response.ErrorMessage ?? response.Content);
                Environment.Exit(Constants.ExitCodeError);
            }

            if (response.Data != null)
                dtos.AddRange(response.Data);

            // Parse response link header in order to extract the next page
            var linkHeaderParameter = response.Headers?.FirstOrDefault(x =>
                string.Equals(x.Name, "link", StringComparison.OrdinalIgnoreCase))
                ?.Value?.ToString();
            
            // Return what we have if no link header parameter
            if (string.IsNullOrEmpty(linkHeaderParameter))
            {
                Log.Logger.Information("Fetch all completed, fetched {Count} {Name}", dtos.Count, typeof(T).Name);
                return dtos;
            }

            // Create link header
            var linkHeader = LinkHeader.LinksFromHeader(linkHeaderParameter);

            // Parse total count for logging purposes
            var totalCount = response.Headers
                ?.First(x => string.Equals(x.Name, "x-total-count", StringComparison.OrdinalIgnoreCase)).Value;

            Log.Logger.Information("Fetch all {Name} progress... {Count}/{TotalCount}", typeof(T).Name, dtos.Count,
                totalCount);

            // Continue fetching more data if the next link is present
            if (linkHeader?.NextLink != null)
            {
                // Build dictionary of querystring parameters
                var nextLinkQueryString = QueryHelpers.ParseQuery(linkHeader.NextLink.Query);

                // Construct the new page parameter and replace the one in the previous request
                var newPageParameter = nextLinkQueryString.FirstOrDefault(x =>
                    string.Equals(x.Key, "page", StringComparison.OrdinalIgnoreCase));
                var oldPageParameter = request.Parameters.TryFind("page");

                if (oldPageParameter != null) request.RemoveParameter(oldPageParameter);

                request.AddQueryParameter(newPageParameter.Key, newPageParameter.Value);
            }
            else
            {
                break;
            }
        }

        return dtos;
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}