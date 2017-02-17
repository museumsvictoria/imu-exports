using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageMagick;
using ImuExports.Config;
using IMu;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.FieldGuideGippsland.Models;
using Serilog;

namespace ImuExports.Tasks.FieldGuideGippsland.Factories
{
    public class ImageFactory : IFactory<Image>
    {
        public Image Make(Map map)
        {
            if (map != null &&
                string.Equals(map.GetEncodedString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                map.GetEncodedStrings("MdaDataSets_tab").Any(x => x.Contains("App: Gippsland")) &&
                string.Equals(map.GetEncodedString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase))
            {
                var irn = long.Parse(map.GetString("irn"));

                var image = new Image();

                var captionMap = map.GetMaps("metadata").FirstOrDefault(x => string.Equals(x.GetEncodedString("MdaElement_tab"), "dcDescription", StringComparison.OrdinalIgnoreCase) && string.Equals(x.GetEncodedString("MdaQualifier_tab"), "Caption.AppGippsland"));
                if (captionMap != null)
                    image.Caption = captionMap.GetEncodedString("MdaFreeText_tab");

                image.AlternateText = map.GetEncodedString("DetAlternateText");
                image.Creators = map.GetEncodedStrings("RigCreator_tab");
                image.Sources = map.GetEncodedStrings("RigSource_tab");
                image.Acknowledgment = map.GetEncodedString("RigAcknowledgementCredit");
                image.CopyrightStatus = map.GetEncodedString("RigCopyrightStatus");
                image.CopyrightStatement = map.GetEncodedString("RigCopyrightStatement");
                image.Licence = map.GetEncodedString("RigLicence");
                image.LicenceDetails = map.GetEncodedString("RigLicenceDetails");
                image.Filename = string.Format("{0}.jpg", irn);

                var repositories = map.GetEncodedStrings("ChaRepository_tab");
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

        public IEnumerable<Image> Make(IEnumerable<Map> maps)
        {
            var images = new List<Image>();

            var groupedMediaMaps = maps
                .Where(x => x != null)
                .GroupBy(x => x.GetEncodedString("irn"))
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
                    using (var file = File.Open(string.Format("{0}{1}.jpg", GlobalOptions.Options.Fgg.Destination, irn), FileMode.Create, FileAccess.Write))
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
