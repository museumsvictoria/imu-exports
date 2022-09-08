using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem.Endpoints;

public interface IMaterialEndpoint
{
    Task<IList<MaterialDto>> GetAll(CancellationToken stoppingToken);
}

public class MaterialEndpoint : EndpointBase, IMaterialEndpoint
{
    public MaterialEndpoint(
        RestClient client) : base(client)
    {
    }

    public async Task<IList<MaterialDto>> GetAll(CancellationToken stoppingToken)
    {
        return await GetAll<MaterialDto>("core/materials", stoppingToken, null, Constants.RestClientLargePageSize);
    }
}