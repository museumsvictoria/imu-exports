using Microsoft.Extensions.Options;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Factories;

public interface IPathFactory
{
    string MakeImageDestinationPath(long irn);
    
    string MakeImageUriPath(long irn);
}

public class PathFactory : IPathFactory
{
    private readonly AppSettings _appSettings;

    public PathFactory(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }
    
    public string MakeImageDestinationPath(long irn)
    {
        return $"{_appSettings.AtlasOfLivingAustralia.WebSitePath}\\content\\media\\{irn % 50}\\{irn}-large.jpg";
    }

    public string MakeImageUriPath(long irn)
    {
        return $"https://collections.museumsvictoria.com.au/content/media/{irn % 50}/{irn}-large.jpg";
    }
}