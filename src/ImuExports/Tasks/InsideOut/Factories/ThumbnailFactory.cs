using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImuExports.Config;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.InsideOut.Models;
using IMu;
using Serilog;

namespace ImuExports.Tasks.InsideOut.Factories
{
    public class ThumbnailFactory : IFactory<Thumbnail>
    {
        public Thumbnail Make(Map map)
        {
            if (map != null && 
                map.GetMaps("metadata").Any(metadata => string.Equals(metadata.GetTrimString("MdaQualifier_tab"), "MIOAct1Crop", StringComparison.OrdinalIgnoreCase)) && 
                string.Equals(map.GetTrimString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase))
            {
                var irn = map.GetLong("irn");
                var image = new Thumbnail
                {
                    Filename = $"{irn}.jpg",
                    AlternateText = map.GetTrimString("DetAlternateText"),
                    ClassName = map.GetMaps("metadata").FirstOrDefault(x => string.Equals(x.GetTrimString("MdaQualifier_tab"), "MIOAct1Crop", StringComparison.OrdinalIgnoreCase))?.GetTrimString("MdaFreeText_tab")
                };

                if (TrySaveImage(irn))
                {
                    return image;
                }
            }

            return null;
        }
        public IEnumerable<Thumbnail> Make(IEnumerable<Map> maps)
        {
            var images = new List<Thumbnail>();

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
                    using (var file = File.Open($"{GlobalOptions.Options.Io.Destination}{irn}.jpg", FileMode.Create, FileAccess.ReadWrite))
                    {
                        fileStream.CopyTo(file);
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
