using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageProcessor.Imaging.Formats;
using IMu;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using Serilog;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Factories
{
    public class ImageFactory : IFactory<Image>
    {
        private readonly IImuSessionProvider imuSessionProvider;

        public ImageFactory(IImuSessionProvider imuSessionProvider)
        {
            this.imuSessionProvider = imuSessionProvider;
        }
       
        public Image Make(Map map)
        {
            if (map != null &&
                string.Equals(map.GetEncodedString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                map.GetEncodedStrings("MdaDataSets_tab").Any(x => x.Contains("Atlas of Living Australia")) &&
                string.Equals(map.GetEncodedString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase))
            {
                var irn = long.Parse(map.GetString("irn"));
                var fileName = string.Format("{0}.jpg", irn);
                var title = map.GetString("MulTitle");
                var description = map.GetString("MulDescription");
                var creator = map.GetStrings("MulCreator_tab").Concatenate(";");
                var rigAcknowledgement = string.Empty;
                var rigType = string.Empty;

                var credit = map.GetMaps("credit").FirstOrDefault();
                if (credit != null)
                {
                    rigAcknowledgement = credit.GetString("RigAcknowledgement");
                    rigType = credit.GetString("RigType");
                }

                if (TrySaveImage(irn))
                {
                    return new Image
                    {
                        Identifier = fileName,
                        Title = title,
                        Description = description,
                        Format = "jpeg",
                        Creator = creator,
                        License = rigType,
                        RightsHolder = rigAcknowledgement
                    };
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
                using (var imuSession = imuSessionProvider.CreateInstance("emultimedia"))
                {
                    imuSession.FindKey(irn);
                    var resource = imuSession.Fetch("start", 0, -1, new[] { "resource" }).Rows[0].GetMap("resource");

                    if (resource == null)
                        throw new IMuException("MultimediaResourceNotFound");

                    var fileStream = resource["file"] as FileStream;
                    var mimeFormat = resource["mimeFormat"] as string;

                    using (var imageFactory = new ImageProcessor.ImageFactory())
                    using (var file = File.OpenWrite(string.Format("{0}{1}.jpg", CommandLineConfig.Options.Ala.Destination, irn)))
                    {
                        if (mimeFormat != null && mimeFormat.ToLower() == "jpeg")
                            fileStream.CopyTo(file);
                        else
                            imageFactory
                                .Load(fileStream)
                                .Format(new JpegFormat())
                                .Quality(90)
                                .Save(file);
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
