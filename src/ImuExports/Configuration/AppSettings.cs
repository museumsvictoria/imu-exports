namespace ImuExports.Configuration;

public class AppSettings
{
    public const string SECTION_NAME = "AppSettings";

    public string LiteDbFilename { get; set; } = string.Empty;

    public Emu Emu { get; set; }

    public AtlasOfLivingAustralia AtlasOfLivingAustralia { get; set; }
    
    public AusGeochem AusGeochem { get; set; }
}

public class Emu
{
    public string Host { get; set; } = string.Empty;

    public string Port { get; set; } = string.Empty;
}

public class AtlasOfLivingAustralia
{
    public string Host { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
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