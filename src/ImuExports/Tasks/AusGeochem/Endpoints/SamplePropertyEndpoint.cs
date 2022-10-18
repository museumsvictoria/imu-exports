using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem.Endpoints;

public interface ISamplePropertyEndpoint
{
    Task SendSampleProperty(SamplePropertyDto dto, Method method, CancellationToken stoppingToken);
    
    Task DeleteSampleProperty(SamplePropertyDto dto, CancellationToken stoppingToken);

    Task<IList<SamplePropertyDto>> GetSamplePropertiesBySampleId(int sampleId, CancellationToken stoppingToken);
}

public class SamplePropertyEndpoint : EndpointBase, ISamplePropertyEndpoint
{
    private readonly RestClient _client;

    public SamplePropertyEndpoint(
        RestClient client) : base(client)
    {
        _client = client;
    }
    
    public async Task SendSampleProperty(SamplePropertyDto dto, Method method, CancellationToken stoppingToken)
    {
        // Build request
        var request = new RestRequest("core/SampleProperty")
        {
            Method = method
        };

        // Add dto
        request.AddJsonBody(dto);

        // Send sample
        Log.Logger.Debug("Sending Request for {Url} via {Method}", _client.BuildUri(request), request.Method);
        var response = await ExecuteWithPolicyAsync(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Error occured sending sample property {PropName}", dto.PropName);
            throw RestException.CreateException(response);
        }
        else
        {
            Log.Logger.Debug("Sent sample property {PropName} via {Method}, status {ResponseStatus}",
                dto.PropName, request.Method, response.ResponseStatus);
        }
    }

    public async Task DeleteSampleProperty(SamplePropertyDto dto, CancellationToken stoppingToken)
    {
        // Build request
        var request = new RestRequest($"core/SampleProperty/{dto.Id}")
        {
            Method = Method.Delete
        };

        // Delete sample
        Log.Logger.Debug("Sending Request for {Url} via {Method}", _client.BuildUri(request), request.Method);
        var response = await ExecuteWithPolicyAsync(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Error occured deleting sample property {PropName}", dto.PropName);
            throw RestException.CreateException(response);
        }
        else
        {
            Log.Logger.Debug("Deleted sample property {ShortName}, status {ResponseStatus}",
                dto.PropName, response.ResponseStatus);
        }
    }

    public async Task<IList<SamplePropertyDto>> GetSamplePropertiesBySampleId(int sampleId, CancellationToken stoppingToken)
    {
        // Build parameters
        var parameters = new ParametersCollection();
        parameters.AddParameter(new QueryParameter("sampleId.equals", sampleId.ToString()));

        // GetAll SamplePropertyDtos
        return await GetAll<SamplePropertyDto>("core/SampleProperty", stoppingToken, parameters);
    }
}