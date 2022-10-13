using System.Text;
using RestSharp;

namespace ImuExports.Infrastructure;

public class RestException : Exception
{
    public RestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static RestException CreateException(RestResponse response)
    {
        Exception innerException = null;
        var builder = new StringBuilder();

        builder.AppendLine($"Error encountered while processing request for uri '{response.ResponseUri}', the following occured");
        builder.AppendLine($"   Server responded with status code {response.StatusDescription}");

        if (response.ErrorException != null)
        {
            builder.AppendLine($"    An exception occurred while processing request: {response.ErrorMessage}");

            innerException = response.ErrorException;
        }

        builder.AppendLine($"   Server responded with: {response.Content}");

        return new RestException(builder.ToString(), innerException);
    }
}