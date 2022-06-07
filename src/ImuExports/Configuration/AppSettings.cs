namespace ImuExports.Configuration
{
    public class AppSettings
    {
        public const string SECTION_NAME = "AppSettings";

        public string LiteDbFilename { get; set; } = string.Empty;
        
        public Emu Emu { get; set; }
        
        public AtlasOfLivingAustralia AtlasOfLivingAustralia { get; set; }
    }

    public class Emu
    {
        public string Host { get; set; } = string.Empty;
        
        public string Port { get; set; } = string.Empty;
    }

    public class AtlasOfLivingAustralia
    {
        public string Username { get; set; } = string.Empty;
        
        public string Password { get; set; } = string.Empty;
    }
}