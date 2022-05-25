namespace ImuExports.NetFramework472.Tasks.FieldGuideGippsland.Models
{
    public class GippslandImage : GippslandMedia
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