namespace ImuExports.Configuration
{
    public class AppSettings
    {
        public const string SectionName = "AppSettings";

        public string LiteDbConnectionString { get; set; }
        
        public Emu Emu { get; set; }
        
        public AtlasOfLivingAustralia AtlasOfLivingAustralia { get; set; }
    }

    public class Emu
    {
        public string Host { get; set; }
        
        public string Port { get; set; }
    }

    public class AtlasOfLivingAustralia
    {
        public string SFTPUsername { get; set; }
        
        public string SFTPPassword { get; set; }
    }
}