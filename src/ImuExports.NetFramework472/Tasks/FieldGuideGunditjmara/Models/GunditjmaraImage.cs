namespace ImuExports.NetFramework472.Tasks.FieldGuideGunditjmara.Models
{
    public class GunditjmaraImage : GunditjmaraMedia
    {
        public ImageType Type { get; set; }
    }

    public enum ImageType
    {
        General,
        Hero,
        Thumbnail
    }
}