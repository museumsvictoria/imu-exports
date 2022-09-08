using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem.Endpoints;

public interface ILocationKindEndpoint
{
    Task<IList<LocationKindDto>> GetAll(CancellationToken stoppingToken);
}

public class LocationKindEndpoint : EndpointBase, ILocationKindEndpoint
{
    public LocationKindEndpoint(
        RestClient client) : base(client)
    {
    }

    public async Task<IList<LocationKindDto>> GetAll(CancellationToken stoppingToken)
    {
        return await GetAll<LocationKindDto>("core/l-location-kinds", stoppingToken, null, Constants.RestClientLargePageSize);
    }
}