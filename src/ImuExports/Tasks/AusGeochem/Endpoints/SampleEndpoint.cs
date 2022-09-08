using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem.Endpoints;

public interface ISampleEndpoint
{
    Task<IList<SampleWithLocationDto>> GetSamplesByPackageId(int dataPackageId, CancellationToken stoppingToken);

    Task<SampleWithLocationDto> GetSampleBySourceId(string sourceId, CancellationToken stoppingToken);

    Task SendSample(SampleWithLocationDto dto, Method method, CancellationToken stoppingToken);

    Task DeleteSample(SampleWithLocationDto dto, CancellationToken stoppingToken);
}

public class SampleEndpoint : EndpointBase, ISampleEndpoint
{
    private readonly RestClient _client;

    public SampleEndpoint(
        RestClient client) : base(client)
    {
        _client = client;
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
            Log.Logger.Fatal(
                "Error occured fetching {Name} by SourceId {SourceId} at endpoint {Resource} via {Method} exiting, {ErrorMessage}",
                nameof(SampleWithLocationDto), sourceId, request.Resource, request.Method,
                response.ErrorMessage ?? response.Content);
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
            Log.Logger.Fatal(
                "Error occured sending sample {ShortName} at resource {Resource} via {Method} exiting, {ErrorMessage}",
                dto.ShortName, request.Resource, request.Method, response.ErrorMessage ?? response.Content);
            Environment.Exit(Constants.ExitCodeError);
        }
        else
        {
            Log.Logger.Debug("Sent sample {ShortName} via {Method}, status {ResponseStatus}",
                dto.ShortName, request.Method, response.ResponseStatus);
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
            Log.Logger.Fatal(
                "Error occured deleting sample {ShortName} at resource {Resource} via {Method} exiting, {ErrorMessage}",
                dto.ShortName, request.Resource, request.Method, response.ErrorMessage ?? response.Content);
            Environment.Exit(Constants.ExitCodeError);
        }
        else
        {
            Log.Logger.Debug("Deleted sample {ShortName}, status {ResponseStatus}",
                dto.ShortName, response.ResponseStatus);
        }
    }
}