using ImuExports.Tasks.AusGeochem.Contracts.Dtos;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem.Endpoints;

public interface ISampleKindEndpoint
{
    Task<IList<SampleKindDto>> GetAll(CancellationToken stoppingToken);
}

public class SampleKindEndpoint : EndpointBase, ISampleKindEndpoint
{
    public SampleKindEndpoint(
        RestClient client) : base(client)
    {
    }

    public async Task<IList<SampleKindDto>> GetAll(CancellationToken stoppingToken)
    {
        return await GetAll<SampleKindDto>("core/l-sample-kinds", stoppingToken, null, Constants.RestClientLargePageSize);
    }
}