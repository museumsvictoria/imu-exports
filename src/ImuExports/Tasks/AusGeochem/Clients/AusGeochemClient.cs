using System.Text.Json;
using System.Text.Json.Serialization;
using ImuExports.Tasks.AusGeochem.Extensions;
using ImuExports.Tasks.AusGeochem.Models;
using ImuExports.Tasks.AusGeochem.Models.Api;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.Json;

namespace ImuExports.Tasks.AusGeochem.Clients;

public interface IAusGeochemClient
{
    Task Authenticate(CancellationToken stoppingToken);

    Task FetchLookups(CancellationToken stoppingToken);

    Task SendSamples(IList<Sample> samples, int? dataPackageId, CancellationToken stoppingToken);
}

public class AusGeochemClient : IAusGeochemClient, IDisposable
{
    private readonly AppSettings _appSettings;
    private readonly RestClient _client;
    private IList<LocationKindDto> _locationKinds;
    private IList<MaterialDto> _materials;
    private IList<SampleKindDto> _sampleKinds;

    public AusGeochemClient(
        IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;

        _client = new RestClient(_appSettings.AusGeochem.BaseUrl);
        _client.UseSystemTextJson(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }

    public async Task Authenticate(CancellationToken stoppingToken)
    {
        var request = new RestRequest("authenticate", Method.Post).AddJsonBody(new LoginRequest()
        {
            Password = _appSettings.AusGeochem.Password,
            Username = _appSettings.AusGeochem.Username
        });
        
        // Request JWT
        var response = await _client.ExecuteAsync<LoginResponse>(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Could not successfully authenticate at endpoint {Resource} exiting, {ErrorMessage}", 
                request.Resource, response.ErrorMessage);
            Environment.Exit(Constants.ExitCodeError);
        }
        else if (!string.IsNullOrWhiteSpace(response.Data?.Token))
        {
            // Set authenticator if successful
            _client.Authenticator = new JwtAuthenticator(response.Data.Token);
        }

        Log.Logger.Debug("Api authentication successful");
    }

    public async Task FetchLookups(CancellationToken stoppingToken)
    {
        _sampleKinds = await FetchAll<SampleKindDto>("core/l-sample-kinds", stoppingToken);
        _locationKinds = await FetchAll<LocationKindDto>("core/l-location-kinds", stoppingToken);
        _materials = await FetchAll<MaterialDto>("core/materials", stoppingToken);
    }

    public async Task SendSamples(IList<Sample> samples, int? dataPackageId, CancellationToken stoppingToken)
    {
        // Fetch current MV records in AusGeochem
        var parameters = new ParametersCollection();
        
        // Add data package id as a query parameter, otherwise exit as theres no point doing anything if that's not there
        if (dataPackageId != null)
            parameters.AddParameter(new QueryParameter("dataPackageId.equals", dataPackageId.ToString()));
        else
        {
            Log.Logger.Fatal("DataPackageId is null, cannot continue without one, exiting");
            Environment.Exit(Constants.ExitCodeError);
        }

        var currentDtos =
            await FetchAll<SampleWithLocationDto>("core/sample-with-locations", stoppingToken, parameters);

        Log.Logger.Information("Sending sample data to AusGeochem API where DataPackageId {DataPackageId}",
            dataPackageId);

        var offset = 0;
        foreach (var sample in samples)
        {
            var existingDto = currentDtos.SingleOrDefault(x =>
                string.Equals(x.SampleDto.SourceId, sample.SampleId, StringComparison.OrdinalIgnoreCase));

            var request = new RestRequest("core/sample-with-locations");

            if (existingDto != null)
            {
                var dto = existingDto.UpdateFromSample(sample, _locationKinds, _materials, _sampleKinds);
                request.Method = Method.Put;
                request.AddJsonBody(dto);
            }
            else
            {
                var dto = sample.CreateSampleWithLocationDto(_locationKinds, _materials, _sampleKinds, dataPackageId,
                    _appSettings.AusGeochem.ArchiveId);
                request.Method = Method.Post;
                request.AddJsonBody(dto);
            }

            var response = await _client.ExecuteAsync(request, stoppingToken);

            if (!response.IsSuccessful)
                Log.Logger.Error("Error occured sending sample via {Method}, {ErrorMessage}", request.Method,
                    response.ErrorMessage);

            offset++;
            Log.Logger.Information("Api upload progress... {Offset}/{TotalResults}", offset, samples.Count);
        }
    }

    private async Task<IList<T>> FetchAll<T>(string resource, CancellationToken stoppingToken,
        ParametersCollection parameters = null)
    {
        var dtos = new List<T>();
        var request = new RestRequest(resource);

        // Add size parameter for pagination size
        request.AddQueryParameter("size", Constants.RestClientPageSize);

        // Add any passed in parameters
        if (parameters != null)
            foreach (var parameter in parameters)
                request.AddParameter(parameter);

        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var response = await _client.ExecuteAsync<IList<T>>(request, stoppingToken);

            if (!response.IsSuccessful)
                Log.Logger.Error("Error occured fetching {Name} at endpoint {Resource} via {Method}, {ErrorMessage}", 
                    typeof(T).Name, request.Resource, request.Method, response.ErrorMessage);

            if (response.Data != null)
                dtos.AddRange(response.Data);

            // Parse response link header in order to extract the next page
            var linkHeaderParameter = response.Headers?.First(x =>
                string.Equals(x.Name, "link", StringComparison.OrdinalIgnoreCase)).Value?.ToString();

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