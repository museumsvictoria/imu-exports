using IMu;
using ImuExports.Tasks.AusGeochem.Models;

namespace ImuExports.Tasks.AusGeochem.Factories;

public class ImageFactory : IImuFactory<Image>
{
    public Image Make(Map map, CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        
        if (map != null &&
            string.Equals(map.GetTrimString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase))
        {
            var image = new Image()
            {
                Irn = map.GetLong("irn"),
                Name = map.GetTrimString("MulTitle"),
            };

            var captionMap = map
                .GetMaps("metadata")
                .FirstOrDefault(x =>
                    string.Equals(x.GetTrimString("MdaElement_tab"), "dcTitle", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.GetTrimString("MdaQualifier_tab"), "Caption.COL"));

            if (captionMap != null)
                image.Description = HtmlConverter.HtmlToText(captionMap.GetTrimString("MdaFreeText_tab"));
            
            return image;
        }

        return null;
    }

    public IEnumerable<Image> Make(IEnumerable<Map> maps, CancellationToken stoppingToken)
    {
        var images = new List<Image>();

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

        images.AddRange(distinctMediaMaps.Select(map => Make(map, stoppingToken)).Where(x => x != null));

        return images;
    }
}