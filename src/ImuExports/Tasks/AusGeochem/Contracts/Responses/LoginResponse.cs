using System.Text.Json.Serialization;

namespace ImuExports.Tasks.AusGeochem.Contracts.Responses;

public class LoginResponse
{
    [JsonPropertyName("id_token")]
    public string Token { get; set; }
}