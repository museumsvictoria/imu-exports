using System.IO;
using ImageMagick;
using ImuExports.Tests.Resources;
using Xunit;

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
        }
    }
}