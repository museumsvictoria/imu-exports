namespace ImuExports.Tasks.FieldGuideGunditjmara.Models
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