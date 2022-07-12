using System.Text.Json;
using System.Text.Json.Serialization;
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

    Task<IList<MaterialDto>> GetMaterials(CancellationToken stoppingToken);
}

public class AusGeochemClient : IAusGeochemClient, IDisposable
{
    private readonly AppSettings _appSettings;
    private readonly RestClient _client;

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

    public async Task<IList<MaterialDto>> GetMaterials(CancellationToken stoppingToken)
    {
        var materials = new List<MaterialDto>();
        
        var materialsRequest = new RestRequest("core/materials");
        materialsRequest.AddQueryParameter("size", "1000");
        
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();
            
            var materialsResponse = await _client.ExecuteAsync<List<MaterialDto>>(materialsRequest, stoppingToken);
            
            if(materialsResponse.Data != null)
                materials.AddRange(materialsResponse.Data);
        
            var linkHeaderParameter = materialsResponse.Headers?.First(x =>
                string.Equals(x.Name, "link", StringComparison.OrdinalIgnoreCase)).Value?.ToString();
        
            var linkHeader = LinkHeader.LinksFromHeader(linkHeaderParameter);
        
            if (linkHeader?.NextLink != null)
            {
                var nextLinkQueryString = QueryHelpers.ParseQuery(linkHeader.NextLink.Query);
        
                var newPageParameter = nextLinkQueryString.FirstOrDefault(x =>
                    string.Equals(x.Key, "page", StringComparison.OrdinalIgnoreCase));
                var oldPageParameter = materialsRequest.Parameters.TryFind("page");
                
                if (oldPageParameter != null)
                {
                    materialsRequest.RemoveParameter(oldPageParameter);
                }
                
                materialsRequest.AddQueryParameter(newPageParameter.Key, newPageParameter.Value);
                
                var totalCount = materialsResponse.Headers?.First(x => string.Equals(x.Name, "x-total-count", StringComparison.OrdinalIgnoreCase)).Value;
                
                Log.Logger.Information("Materials progress... {materialsCount}/{totalCount}", materials.Count, totalCount);
            }
            else
                break;
        }

        return materials;
    }
    

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}