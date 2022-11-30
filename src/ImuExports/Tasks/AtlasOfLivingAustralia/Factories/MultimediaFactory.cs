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
    private readonly IPathFactory _pathFactory;
    
    public MultimediaFactory(IOptions<AppSettings> appSettings,
        IPathFactory pathFactory)
    {
        _appSettings = appSettings.Value;
        _pathFactory = pathFactory;
    }

    public Multimedia Make(Map map, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        
        if (Assertions.IsAtlasOfLivingAustraliaImage(map))
        {
            var irn = map.GetLong("irn");

            var multimedia = new Multimedia
            {
                Type = "StillImage",
                Format = "image/jpeg",
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

            var (isSuccess, identifier) = MakeIdentifier(map);

            if (isSuccess)
            {
                multimedia.Identifier = identifier;
                
                return multimedia;
            }
            
            Log.Logger.Warning("Could not construct identifier, omitting image from occurence record (MMR Irn: {Irn})", irn);
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

    private (bool, string) MakeIdentifier(Map map)
    {
        var irn = map.GetLong("irn");
        var destinationPath = _pathFactory.MakeImageDestinationPath(irn);
        
        // First check to see if image is collections online image
        if (Assertions.IsCollectionsOnlineImage(map))
        {
            // Check that file exists on collections online website path
            if (File.Exists(destinationPath))
            {
                return (true, _pathFactory.MakeImageUriPath(irn));
            }
            else
            {
                Log.Logger.Warning("Could not find occurence record image file on Collections Online website path {DestinationPath} (MMR Irn: {Irn})", destinationPath, irn);
            }
        }

        // If not create large derivative and save it to collections online
        if (File.Exists(destinationPath))
        {
            // File exists already so return uri path
            return (true, _pathFactory.MakeImageUriPath(irn));
        }
        else
        {
            // Save image to collections online
            try
            {
                var stopwatch = Stopwatch.StartNew();
            
                using var imuSession = new ImuSession("emultimedia", _appSettings.Imu.Host, _appSettings.Imu.Port);
                imuSession.FindKey(irn);
                var resource = imuSession.Fetch("start", 0, -1, new[] { "resource" }).Rows[0].GetMap("resource");
            
                if (resource == null)
                    throw new IMuException("MultimediaResourceNotFound");

                using var fileStream = resource["file"] as FileStream;
                using var image = new MagickImage(fileStream);
                
                image.Format = MagickFormat.Jpg;
                image.Quality = 86;
                image.FilterType = FilterType.Lanczos;
                image.ColorSpace = ColorSpace.sRGB;
                image.Resize(new MagickGeometry(3000) { Greater = true });
                image.UnsharpMask(0.5, 0.5, 0.6, 0.025);
                image.Write(destinationPath!);

                stopwatch.Stop();
                Log.Logger.Debug("Completed image {Irn} creation in {ElapsedMilliseconds}ms", irn,
                    stopwatch.ElapsedMilliseconds);
            
                return (true, _pathFactory.MakeImageUriPath(irn));
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex, "Error saving image {Irn}", irn);
            }
        }

        return (false, null);
    }
}