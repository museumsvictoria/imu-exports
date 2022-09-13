using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using ImuExports.Tasks.AusGeochem.Contracts.Requests;
using ImuExports.Tasks.AusGeochem.Models;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem.Endpoints;

public interface ISamplePropertyEndpoint
{
    Task SendSampleProperty(SamplePropertyDto dto, Method method, CancellationToken stoppingToken);
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
        var response = await _client.ExecuteAsync(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal(
                "Error occured sending sample property {PropName} at resource {Resource} via {Method} exiting, {ErrorMessage}",
                dto.PropName, request.Resource, request.Method, response.ErrorMessage ?? response.Content);
            Environment.Exit(Constants.ExitCodeError);
        }
        else
        {
            Log.Logger.Debug("Sent sample property {PropName} via {Method}, status {ResponseStatus}",
                dto.PropName, request.Method, response.ResponseStatus);
        }
    }
}