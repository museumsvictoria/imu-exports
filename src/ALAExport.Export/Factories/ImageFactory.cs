using System;
using System.Collections.Generic;
using System.Linq;
using ALAExport.Export.Extensions;
using ALAExport.Export.Models;
using IMu;
using Serilog;

namespace ALAExport.Export.Factories
{
    public class ImageFactory : IFactory<Image>
    {
        public Image Make(Map map)
        {
            if (map != null &&
                string.Equals(map.GetEncodedString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                map.GetEncodedStrings("MdaDataSets_tab").Contains(Constants.ImuMultimediaQueryString) &&
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
            // TODO: ADD CODE TO SAVE IMAGE

            return false;
        }
    }
}
