using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageMagick;
using ImuExports.Config;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.FieldMIO.Models;
using IMu;
using Serilog;

namespace ImuExports.Tasks.FieldMIO.Factories
{
    public class MIOThumbnailFactory : IFactory<MIOThumbnail>
    {
        public MIOThumbnail Make(Map map)
        {
            if (map != null)
            {
                var CropMap = map.GetMaps("metadata").FirstOrDefault(x => string.Equals(x.GetTrimString("MdaQualifier_tab"), "MIOAct1Crop"));
                var FullMap = map.GetMaps("metadata").FirstOrDefault(x => string.Equals(x.GetTrimString("MdaQualifier_tab"), "MIOAct1Full"));
                if (CropMap != null &&
                        string.Equals(map.GetTrimString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase)
                    )
                {
                    var irn = map.GetLong("irn");

                    var image = new MIOThumbnail();

                    image.Filename = $"{irn}.jpg";

                    var thumbnailClassMap = map.GetMaps("metadata").FirstOrDefault(x => string.Equals(x.GetTrimString("MdaQualifer_tab"), "MIOAct1Crop", StringComparison.OrdinalIgnoreCase));
                    if (thumbnailClassMap != null)
                        image.ThumbnailClass = thumbnailClassMap.GetTrimString("MdaFreeText_tab");


                    if (TrySaveImage(irn))
                    {
                        return image;
                    }
                }
            }
            return null;
        }
        public IEnumerable<MIOThumbnail> Make(IEnumerable<Map> maps)
        {
            var images = new List<MIOThumbnail>();

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
                    using (var file = File.Open($"{GlobalOptions.Options.Mio.Destination}{irn}.jpg", FileMode.Create, FileAccess.ReadWrite))
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
