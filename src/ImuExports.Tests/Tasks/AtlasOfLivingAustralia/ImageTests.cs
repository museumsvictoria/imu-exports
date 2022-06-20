using ImageMagick;

namespace ImuExports.Tests.Tasks.AtlasOfLivingAustralia
{
    public class ImageTests : FileBasedTest
    {
        [Fact]
        public void ShouldApplyUnsharpMask()
        {
            using (var image = new MagickImage())
            {
                image.Read(Files.Crab);
                
                image.Format = MagickFormat.Jpg;
                image.Quality = 90;
                image.FilterType = FilterType.Lanczos;
                image.ColorSpace = ColorSpace.sRGB;
                image.Resize(new MagickGeometry(3000) { Greater = true });
                image.UnsharpMask(0.5, 0.5, 0.6, 0.025);
                image.Write($"{Files.OutputFolder}{Path.GetFileNameWithoutExtension(Files.Crab)}.jpg");
            }
            
            using (var image = new MagickImage())
            {
                image.Read(Files.Fish);
                
                image.Format = MagickFormat.Jpg;
                image.Quality = 90;
                image.FilterType = FilterType.Lanczos;
                image.ColorSpace = ColorSpace.sRGB;
                image.Resize(new MagickGeometry(3000) { Greater = true });
                image.UnsharpMask(0.5, 0.5, 0.6, 0.025);
                
                image.Write($"{Files.OutputFolder}{Path.GetFileNameWithoutExtension(Files.Fish)}.jpg");
            }
            
            using (var image = new MagickImage())
            {
                image.Read(Files.Beetle);
                
                image.Format = MagickFormat.Jpg;
                image.Quality = 90;
                image.FilterType = FilterType.Lanczos;
                image.ColorSpace = ColorSpace.sRGB;
                image.Resize(new MagickGeometry(3000) { Greater = true });
                image.UnsharpMask(0.5, 0.5, 0.6, 0.025);
                
                image.Write($"{Files.OutputFolder}{Path.GetFileNameWithoutExtension(Files.Beetle)}.jpg");
            }
        }
    }
}