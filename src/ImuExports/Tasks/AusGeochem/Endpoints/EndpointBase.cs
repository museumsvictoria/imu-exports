using Microsoft.AspNetCore.WebUtilities;
using RestSharp;

namespace ImuExports.Tasks.AusGeochem.Endpoints;

public abstract class EndpointBase
{
    private readonly RestClient _client;

    protected EndpointBase(
        RestClient client)
    {
        _client = client;
    }

    protected async Task<IList<T>> GetAll<T>(string resource, CancellationToken stoppingToken,
        ParametersCollection parameters = null, int pageSize = Constants.RestClientSmallPageSize)
    {
        var dtos = new List<T>();
        
        // Build request
        var request = new RestRequest(resource);

        // Add size parameter for pagination size
        request.AddQueryParameter("size", pageSize);

        // Add any passed in parameters
        if (parameters != null)
            foreach (var parameter in parameters)
                request.AddParameter(parameter);
        
        // Ensure there is a sort parameter attached otherwise we may get inconsistent results 
        if(request.Parameters.All(x => x.Name != "sort"))
            request.AddParameter(new QueryParameter("sort", "id,DESC", false));

        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();

            Log.Logger.Debug("Sending Request for {Url} via {Method}", _client.BuildUri(request), request.Method);
            var response = await _client.ExecuteAsync<IList<T>>(request, stoppingToken);

            if (!response.IsSuccessful)
            {
                Log.Logger.Fatal("Error occured fetching {Name}", typeof(T).Name);
                throw RestException.CreateException(response);
            }

            if (response.Data != null)
                dtos.AddRange(response.Data);

            // Parse response link header in order to extract the next page
            var linkHeaderParameter = response.Headers?.FirstOrDefault(x =>
                string.Equals(x.Name, "link", StringComparison.OrdinalIgnoreCase))
                ?.Value?.ToString();
            
            // Return what we have if no link header parameter
            if (string.IsNullOrEmpty(linkHeaderParameter))
            {
                Log.Logger.Information("Fetch all completed, fetched {Count} {Name}", dtos.Count, typeof(T).Name);
                return dtos;
            }

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
}