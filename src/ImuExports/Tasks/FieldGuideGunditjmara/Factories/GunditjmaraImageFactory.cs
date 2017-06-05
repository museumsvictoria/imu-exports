using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageMagick;
using ImuExports.Config;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.FieldGuideGunditjmara.Models;
using IMu;
using Serilog;

namespace ImuExports.Tasks.FieldGuideGunditjmara.Factories
{
    public class GunditjmaraImageFactory : IFactory<GunditjmaraImage>
    {
        public GunditjmaraImage Make(Map map)
        {
            if (map != null &&
                string.Equals(map.GetTrimString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                map.GetTrimStrings("MdaDataSets_tab").Any(x => x.Contains("App: Gunditjmara")) &&
                string.Equals(map.GetTrimString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase))
            {
                var irn = map.GetLong("irn");

                var image = new GunditjmaraImage();

                var captionMap = map.GetMaps("metadata").FirstOrDefault(x => string.Equals(x.GetTrimString("MdaElement_tab"), "dcDescription", StringComparison.OrdinalIgnoreCase) && string.Equals(x.GetTrimString("MdaQualifier_tab"), "Caption.AppGunditjmara"));
                if (captionMap != null)
                    image.Caption = captionMap.GetTrimString("MdaFreeText_tab");

                image.AlternateText = map.GetTrimString("DetAlternateText");
                image.Creators = map.GetTrimStrings("RigCreator_tab");
                image.Sources = map.GetTrimStrings("RigSource_tab");
                image.Acknowledgment = map.GetTrimString("RigAcknowledgementCredit");
                image.CopyrightStatus = map.GetTrimString("RigCopyrightStatus");
                image.CopyrightStatement = map.GetTrimString("RigCopyrightStatement");
                image.Licence = map.GetTrimString("RigLicence");
                image.LicenceDetails = map.GetTrimString("RigLicenceDetails");
                image.Filename = $"{irn}.jpg";

                var repositories = map.GetTrimStrings("ChaRepository_tab");
                if (repositories.Any(x => string.Equals(x, "NS Online Images Live Hero", StringComparison.OrdinalIgnoreCase)))
                    image.Type = ImageType.Hero;
                else if (repositories.Any(x => string.Equals(x, "NS Online Images Square", StringComparison.OrdinalIgnoreCase)))
                    image.Type = ImageType.Thumbnail;
                else
                    image.Type = ImageType.General;

                if (TrySaveImage(irn))
                {
                    return image;
                }
            }

            return null;
        }

        public IEnumerable<GunditjmaraImage> Make(IEnumerable<Map> maps)
        {
            var images = new List<GunditjmaraImage>();

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

            images.AddRange(distinctMediaMaps.Select(Make).Where(x => x != null));

            return images;
        }

        private bool TrySaveImage(long irn)
        {
            try
            {
                using (var imuSession = ImuSessionProvider.CreateInstance("emultimedia"))
                {
                    imuSession.FindKey(irn);
                    var resource = imuSession.Fetch("start", 0, -1, new[] { "resource" }).Rows[0].GetMap("resource");

                    if (resource == null)
                        throw new IMuException("MultimediaResourceNotFound");

                    using (var fileStream = resource["file"] as FileStream)
                    using (var file = File.Open($"{GlobalOptions.Options.Gun.Destination}{irn}.jpg", FileMode.Create, FileAccess.ReadWrite))
                    {
                        if (string.Equals(resource["mimeFormat"] as string, "jpeg", StringComparison.OrdinalIgnoreCase))
                            fileStream.CopyTo(file);
                        else
                        {
                            using (var image = new MagickImage(fileStream))
                            {
                                image.Format = MagickFormat.Jpg;
                                image.Quality = 95;
                                image.Write(file);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Error saving image {irn}", irn);
            }

            return false;
        }
    }
}
