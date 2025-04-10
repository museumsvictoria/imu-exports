namespace ImuExports.Configuration;

public class AppSettings
{
    public const string SECTION_NAME = "AppSettings";

    public string LiteDbFilename { get; set; } = string.Empty;

    public Imu Imu { get; set; }

    public AtlasOfLivingAustralia AtlasOfLivingAustralia { get; set; }
    
    public AusGeochem AusGeochem { get; set; }
}

public class Imu
{
    public string Host { get; set; } = string.Empty;

    public int? Port { get; set; }
}

public class AtlasOfLivingAustralia
{
    public string FtpHost { get; set; } = string.Empty;

    public string FtpUsername { get; set; } = string.Empty;

    public string FtpPassword { get; set; } = string.Empty;
    
    public string WebSitePath { get; set; } = string.Empty;
        
    public string WebSiteUser { get; set; } = string.Empty;
        
    public string WebSitePassword { get; set; } = string.Empty;
        
    public string WebSiteDomain { get; set; } = string.Empty;
}

public class AusGeochem
{
    public string Username { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;
    
    public string BaseUrl { get; set; } = string.Empty;
    
    public IList<DataPackage> DataPackages { get; set; }

    public int? ArchiveId { get; set; }
}

public class DataPackage
{
    public int? Id { get; set; }
    
    public string Discipline { get; set; }
}