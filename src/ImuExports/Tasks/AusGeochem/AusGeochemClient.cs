﻿using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CsvHelper;
using CsvHelper.Configuration;
using ImuExports.Tasks.AusGeochem.ClassMaps;
using ImuExports.Tasks.AusGeochem.Models;
using ImuExports.Tasks.AusGeochem.Models.Api;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.Json;

namespace ImuExports.Tasks.AusGeochem;

public interface IAusGeochemClient
{
    Task Authenticate(CancellationToken stoppingToken);

    Task<IList<SampleWithLocationDto>> FetchCurrentSamples(int dataPackageId, CancellationToken stoppingToken);

    Task<(IList<LocationKindDto>, IList<MaterialDto>, IList<SampleKindDto>, IList<MaterialLookup>)> FetchLookups(CancellationToken stoppingToken);

    Task SendSample(SampleWithLocationDto dto, Method method, CancellationToken stoppingToken);
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

    public async Task<IList<SampleWithLocationDto>> FetchCurrentSamples(int dataPackageId, CancellationToken stoppingToken)
    {
        // Fetch current MV records in AusGeochem
        var parameters = new ParametersCollection();
        
        parameters.AddParameter(new QueryParameter("dataPackageId.equals", dataPackageId.ToString()));
        
        return await FetchAll<SampleWithLocationDto>("core/sample-with-locations", stoppingToken, parameters);
    }

    public async Task<(IList<LocationKindDto>, IList<MaterialDto>, IList<SampleKindDto>, IList<MaterialLookup>)> FetchLookups(CancellationToken stoppingToken)
    {
        // Lookups fetched from API
        var locationKinds = await FetchAll<LocationKindDto>("core/l-location-kinds", stoppingToken);
        var materials = await FetchAll<MaterialDto>("core/materials", stoppingToken);
        var sampleKinds = await FetchAll<SampleKindDto>("core/l-sample-kinds", stoppingToken);
        
        // CSV lookup
        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };
        using var reader = new StreamReader($"{AppContext.BaseDirectory}Resources\\materials-lookup.csv");
        using var csv = new CsvReader(reader, csvConfig);
        csv.Context.RegisterClassMap<MaterialsLookupClassMap>();
        var materialsLookup = csv.GetRecords<MaterialLookup>().ToList();

        return (locationKinds, materials, sampleKinds, materialsLookup);
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
        var response = await _client.ExecuteAsync(request, stoppingToken);

        if (!response.IsSuccessful)
            Log.Logger.Error("Error occured sending sample via {Method}, {ErrorMessage}", request.Method,
                response.ErrorMessage);
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