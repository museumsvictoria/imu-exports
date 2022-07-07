using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Models.Api;

public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; }
    
    [JsonPropertyName("password")]
    public string Password { get; set; }
}