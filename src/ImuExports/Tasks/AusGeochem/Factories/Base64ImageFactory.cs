﻿using System.Diagnostics;
using ImageMagick;
using IMu;
using ImuExports.Tasks.AusGeochem.Models;
using Microsoft.Extensions.Options;

namespace ImuExports.Tasks.AusGeochem.Factories;

public interface IBase64ImageFactory
{
    Task<string> Make(Image image, CancellationToken stoppingToken);
}

public class Base64ImageFactory : IBase64ImageFactory
{
    private readonly AppSettings _appSettings;
    
    public Base64ImageFactory(
        IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;        
    }
    
    public async Task<string> Make(Image image, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            using var imuSession = new ImuSession("emultimedia", _appSettings.Imu.Host, _appSettings.Imu.Port);
            imuSession.FindKey(image.Irn);
            var resource = imuSession.Fetch("start", 0, -1, new[] { "resource" }).Rows[0].GetMap("resource");

            if (resource == null)
                throw new IMuException("MultimediaResourceNotFound");

            await using var sourceFileStream = resource["file"] as FileStream;

            using var imageResource = new MagickImage(sourceFileStream);

            imageResource.Format = MagickFormat.Jpeg;
            imageResource.Quality = 90;
            imageResource.FilterType = FilterType.Lanczos;
            imageResource.ColorSpace = ColorSpace.sRGB;
            imageResource.Resize(new MagickGeometry(3000) { Greater = true });
            imageResource.UnsharpMask(0.5, 0.5, 0.6, 0.025);
            
            // Save profiles if there are any
            var profile = imageResource.GetColorProfile();

            // Strip metadata and any profiles
            imageResource.Strip();

            // Add original profile back
            if (profile != null)
                imageResource.SetProfile(profile);
            
            // Set metadata
            var iptcProfile = new IptcProfile();

            iptcProfile.SetValue(IptcTag.CopyrightNotice, image.RightsHolder);

            imageResource.SetProfile(iptcProfile);

            var base64Image = imageResource.ToBase64(MagickFormat.Jpeg);

            stopwatch.Stop();
            
            Log.Logger.Debug("Completed fetching image {Irn} in {ElapsedMilliseconds}ms", image.Irn,
                stopwatch.ElapsedMilliseconds);

            return base64Image;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error fetching image {Irn}, exiting", image.Irn);
            throw;
        }
    }
}