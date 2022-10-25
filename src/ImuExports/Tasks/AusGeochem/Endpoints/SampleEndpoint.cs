using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem.Endpoints;

public interface ISampleEndpoint
{
    Task<IList<SampleWithLocationDto>> GetSamplesByPackageId(int dataPackageId, CancellationToken stoppingToken);

    Task<SampleWithLocationDto> SendSample(SampleWithLocationDto dto, Method method, CancellationToken stoppingToken);

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
    
    public async Task<SampleWithLocationDto> SendSample(SampleWithLocationDto dto, Method method,
        CancellationToken stoppingToken)
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
        var response = await ExecuteWithPolicyAsync<SampleWithLocationDto>(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Error occured sending sample {ShortName}", dto.ShortName);
            throw RestException.CreateException(response);
        }
        else
        {
            Log.Logger.Debug("Sent sample {ShortName} via {Method}, status {ResponseStatus}",
                dto.ShortName, request.Method, response.ResponseStatus);
        }

        return response.Data;
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
        var response = await ExecuteWithPolicyAsync(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal(
                "Error occured deleting sample {ShortName}", dto.ShortName);
            throw RestException.CreateException(response);
        }
        else
        {
            Log.Logger.Debug("Deleted sample {ShortName}, status {ResponseStatus}",
                dto.ShortName, response.ResponseStatus);
        }
    }
}