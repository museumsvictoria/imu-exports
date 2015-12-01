namespace ImuExports.Tasks.FieldGuideGippsland.Models
{
    public class Image : Media
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