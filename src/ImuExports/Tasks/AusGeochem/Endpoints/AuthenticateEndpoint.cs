using ImuExports.Tasks.AusGeochem.Contracts.Requests;
using ImuExports.Tasks.AusGeochem.Contracts.Responses;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;

namespace ImuExports.Tasks.AusGeochem.Endpoints;

public interface IAuthenticateEndpoint
{
    Task Authenticate(CancellationToken stoppingToken);
}

public class AuthenticateEndpoint : EndpointBase, IAuthenticateEndpoint
{
    private readonly AppSettings _appSettings;
    private readonly RestClient _client;

    public AuthenticateEndpoint(
        IOptions<AppSettings> appSettings,
        RestClient client) : base(client)
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
        var response = await ExecuteWithPolicyAsync<AuthenticateResponse>(request, stoppingToken);

        if (!response.IsSuccessful)
        {
            Log.Logger.Fatal("Could not authenticate successfully");
            throw RestException.CreateException(response);
        }
        
        if (!string.IsNullOrWhiteSpace(response.Data?.Token))
        {
            // Set authenticator if successful
            _client.Authenticator = new JwtAuthenticator(response.Data.Token);
        }
        else
        {
            throw new InvalidOperationException("JWT Token cannot be null or empty");
        }

        Log.Logger.Debug("Api authentication successful");
    }
}