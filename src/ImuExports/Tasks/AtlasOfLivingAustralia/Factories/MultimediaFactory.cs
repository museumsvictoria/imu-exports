using System.Diagnostics;
using System.Text.RegularExpressions;
using ImageMagick;
using IMu;
using ImuExports.Tasks.AtlasOfLivingAustralia.Helpers;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using Microsoft.Extensions.Options;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Factories;

public class MultimediaFactory : IImuFactory<Multimedia>
{
    private readonly AppSettings _appSettings;
    private readonly AtlasOfLivingAustraliaOptions _options = (AtlasOfLivingAustraliaOptions)CommandOptions.TaskOptions;
    
    public MultimediaFactory(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;
    }

    public Multimedia Make(Map map, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        
        if (Assertions.IsMultimedia(map))
        {
            var irn = map.GetLong("irn");

            var multimedia = new Multimedia
            {
                Type = "StillImage",
                Format = "image/jpeg",
                Identifier = $"{irn}.jpg",
                Title = map.GetTrimString("MulTitle"),
                Creator = map.GetTrimStrings("MulCreator_tab").Concatenate(" | "),
                Publisher = "Museums Victoria",
                Source = map.GetTrimStrings("RigSource_tab").Concatenate(" | "),
                AltText = map.GetTrimString("DetAlternateText")
            };

            var captionMap = map
                .GetMaps("metadata")
                .FirstOrDefault(x =>
                    string.Equals(x.GetTrimString("MdaElement_tab"), "dcTitle", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.GetTrimString("MdaQualifier_tab"), "Caption.COL"));

            if (captionMap != null)
                multimedia.Description = HtmlConverter.HtmlToText(captionMap.GetTrimString("MdaFreeText_tab"));

            if (map.GetTrimString("RigLicence")?.Equals("CC BY", StringComparison.OrdinalIgnoreCase) == true)
                multimedia.License = "https://creativecommons.org/licenses/by/4.0/";
            else if (map.GetTrimString("RigLicence")?.Equals("CC BY-NC", StringComparison.OrdinalIgnoreCase) == true)
                multimedia.License = "https://creativecommons.org/licenses/by-nc/4.0/";

            if (map.GetTrimString("RigCopyrightStatus")
                    ?.Equals("In Copyright: MV Copyright", StringComparison.OrdinalIgnoreCase) == true)
            {
                multimedia.RightsHolder = "Museums Victoria";
            }
            else if (map.GetTrimString("RigCopyrightStatus")?.Equals("In Copyright: Third Party Copyright",
                         StringComparison.OrdinalIgnoreCase) == true)
            {
                var rightsHolderRegex = Regex.Match(map.GetTrimString("RigCopyrightStatement"),
                    @"Copyright (?<rightsholder>.*) \/ ");

                if (rightsHolderRegex.Groups["rightsholder"].Success)
                    multimedia.RightsHolder = rightsHolderRegex.Groups["rightsholder"].Value;
            }

            if (TrySaveMultimedia(irn))
                return multimedia;
        }

        return null;
    }

    public IEnumerable<Multimedia> Make(IEnumerable<Map> maps, CancellationToken stoppingToken)
    {
        var multimedias = new List<Multimedia>();

        var groupedMediaMaps = maps
            .Where(x => x != null)
            .GroupBy(x => x.GetLong("irn"))
            .ToList();

        // Find and log duplicate mmr irns
        var duplicateMediaIrns = groupedMediaMaps
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicateMediaIrns.Any())
            Log.Logger.Warning("Duplicate MMR Irns detected {@DuplicateMediaIrns}", duplicateMediaIrns);

        // Select only distinct mmr maps
        var distinctMediaMaps = groupedMediaMaps.Select(x => x.First());

        multimedias.AddRange(distinctMediaMaps.Select(map => Make(map, stoppingToken)).Where(x => x != null));

        return multimedias;
    }

    private bool TrySaveMultimedia(long irn)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            using var imuSession = new ImuSession("emultimedia", _appSettings.Imu.Host, _appSettings.Imu.Port);
            imuSession.FindKey(irn);
            var resource = imuSession.Fetch("start", 0, -1, new[] { "resource" }).Rows[0].GetMap("resource");

            if (resource == null)
                throw new IMuException("MultimediaResourceNotFound");

            var mimeFormat = resource["mimeFormat"] as string;

            using var fileStream = resource["file"] as FileStream;
            using var file = File.Open($"{_options.Destination}{irn}.jpg", FileMode.Create,
                FileAccess.Write);
            if (mimeFormat != null && mimeFormat.ToLower() == "jpeg")
                fileStream.CopyTo(file);
            else
            {
                using var image = new MagickImage(fileStream);
                image.Format = MagickFormat.Jpg;

                image.Format = MagickFormat.Jpg;
                image.Quality = 90;
                image.FilterType = FilterType.Lanczos;
                image.ColorSpace = ColorSpace.sRGB;
                image.Resize(new MagickGeometry(3000) { Greater = true });
                image.UnsharpMask(0.5, 0.5, 0.6, 0.025);

                image.Write(file);
            }

            stopwatch.Stop();
            Log.Logger.Debug("Completed image {irn} creation in {ElapsedMilliseconds}ms", irn,
                stopwatch.ElapsedMilliseconds);

            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error saving multimedia {irn}", irn);
        }

        return false;
    }
}