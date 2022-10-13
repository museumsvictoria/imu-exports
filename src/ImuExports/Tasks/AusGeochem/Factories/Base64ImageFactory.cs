using System.Diagnostics;
using ImageMagick;
using IMu;
using Microsoft.Extensions.Options;

namespace ImuExports.Tasks.AusGeochem.Factories;

public interface IBase64ImageFactory
{
    Task<string> Make(long irn, CancellationToken stoppingToken);
}

public class Base64ImageFactory : IBase64ImageFactory
{
    private readonly AppSettings _appSettings;
    
    public Base64ImageFactory(
        IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;        
    }
    
    public async Task<string> Make(long irn, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            using var imuSession = new ImuSession("emultimedia", _appSettings.Imu.Host, _appSettings.Imu.Port);
            imuSession.FindKey(irn);
            var resource = imuSession.Fetch("start", 0, -1, new[] { "resource" }).Rows[0].GetMap("resource");

            if (resource == null)
                throw new IMuException("MultimediaResourceNotFound");

            await using var sourceFileStream = resource["file"] as FileStream;

            using var imageResource = new MagickImage(sourceFileStream);

            imageResource.Format = MagickFormat.Jpg;
            imageResource.Quality = 90;
            imageResource.FilterType = FilterType.Lanczos;
            imageResource.ColorSpace = ColorSpace.sRGB;
            imageResource.Resize(new MagickGeometry(3000) { Greater = true });
            imageResource.UnsharpMask(0.5, 0.5, 0.6, 0.025);

            var base64Image = imageResource.ToBase64();

            stopwatch.Stop();
            
            Log.Logger.Debug("Completed fetching image {Irn} in {ElapsedMilliseconds}ms", irn,
                stopwatch.ElapsedMilliseconds);

            return base64Image;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error fetching image {Irn}, exiting", irn);
            throw;
        }
    }
}