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

    Task SendSamples(IList<Sample> samples, string dataPackageId, CancellationToken stoppingToken);
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
        // Request JWT
        var request = new RestRequest("authenticate", Method.Post).AddJsonBody(new LoginRequest()
        {
            Password = _appSettings.AusGeochem.Password,
            Username = _appSettings.AusGeochem.Username
        });

        var response = await _client.ExecuteAsync<LoginResponse>(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Error(response.ErrorException, "Could not successfully authenticate, exiting");
            Environment.Exit(Constants.ExitCodeError);
        }
        else if (!string.IsNullOrWhiteSpace(response.Data?.Token))
        {
            _client.Authenticator = new JwtAuthenticator(response.Data.Token);                
        }
    }

    public async Task FetchLookups(CancellationToken stoppingToken)
    {
        _sampleKinds = await FetchAll<SampleKindDto>("core/l-sample-kinds", stoppingToken);
        _locationKinds = await FetchAll<LocationKindDto>("core/l-location-kinds", stoppingToken);
        _materials = await this.FetchAll<MaterialDto>("core/materials", stoppingToken);
    }

    public async Task SendSamples(IList<Sample> samples, string dataPackageId, CancellationToken stoppingToken)
    {
        // Fetch current MV records in AusGeochem
        var parameters = new ParametersCollection();
        parameters.AddParameter(new QueryParameter("dataPackageId.equals", dataPackageId));
        var currentDtos = await FetchAll<SampleWithLocationDto>("core/sample-with-locations", stoppingToken, parameters);

        Log.Logger.Information("Sending sample data to AusGeochem API where DataPackageId {DataPackageId}", dataPackageId);
        
        var offset = 0;
        foreach (var sample in samples)
        {
            var existingDto = currentDtos.SingleOrDefault(x => string.Equals(x.SampleDto.SourceId, sample.SampleId, StringComparison.OrdinalIgnoreCase));
            
            var request = new RestRequest("core/sample-with-locations");

            if (existingDto != null)
            {
                var dto = existingDto.UpdateFromSample(sample, _locationKinds, _materials, _sampleKinds);
                request.Method = Method.Put;
                request.AddJsonBody(dto);
            }
            else
            {
                var dto = sample.CreateSampleWithLocationDto(_locationKinds, _materials, _sampleKinds, dataPackageId);
                request.Method = Method.Post;
                request.AddJsonBody(dto);
            }

            var response = await _client.ExecuteAsync(request, stoppingToken);
            
            if(!response.IsSuccessful)
                Log.Logger.Error("Error occured uploading {ErrorMessage}", response.ErrorMessage);

            offset++;
            Log.Logger.Information("Api upload progress... {Offset}/{TotalResults}", offset, samples.Count);
        }
    }

    private async Task<IList<T>> FetchAll<T>(string resource, CancellationToken stoppingToken, ParametersCollection parameters = null)
    {
        var dtos = new List<T>();
        var request = new RestRequest(resource);
        
        request.AddQueryParameter("size", Constants.RestClientPageSize);

        if (parameters != null)
        {
            foreach (var parameter in parameters)
            {
                request.AddParameter(parameter);
            }
        }
        
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            var response = await _client.ExecuteAsync<IList<T>>(request, stoppingToken);
            
            if(response.Data != null)
                dtos.AddRange(response.Data);
        
            var linkHeaderParameter = response.Headers?.First(x =>
                string.Equals(x.Name, "link", StringComparison.OrdinalIgnoreCase)).Value?.ToString();
        
            var linkHeader = LinkHeader.LinksFromHeader(linkHeaderParameter);
            
            var totalCount = response.Headers?.First(x => string.Equals(x.Name, "x-total-count", StringComparison.OrdinalIgnoreCase)).Value;
            
            Log.Logger.Information("FetchAllDtos ({Name}) progress... {Count}/{TotalCount}", typeof(T).Name, dtos.Count, totalCount);
        
            if (linkHeader?.NextLink != null)
            {
                var nextLinkQueryString = QueryHelpers.ParseQuery(linkHeader.NextLink.Query);
        
                var newPageParameter = nextLinkQueryString.FirstOrDefault(x =>
                    string.Equals(x.Key, "page", StringComparison.OrdinalIgnoreCase));
                var oldPageParameter = request.Parameters.TryFind("page");
                
                if (oldPageParameter != null)
                {
                    request.RemoveParameter(oldPageParameter);
                }
                
                request.AddQueryParameter(newPageParameter.Key, newPageParameter.Value);
            }
            else
                break;
        }

        return dtos;
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}