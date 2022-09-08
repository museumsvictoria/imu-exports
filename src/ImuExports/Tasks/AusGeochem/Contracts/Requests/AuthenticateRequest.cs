using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Contracts.Requests;

public class AuthenticateRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; }
    
    [JsonPropertyName("password")]
    public string Password { get; set; }
}