using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageProcessor.Imaging.Formats;
using IMu;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using ImuExports.Utilities;
using Serilog;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Factories
{
    public class MultimediaFactory : IFactory<Multimedia>
    {
        public Multimedia Make(Map map)
        {
            if (map != null &&
                string.Equals(map.GetEncodedString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                map.GetEncodedStrings("MdaDataSets_tab").Any(x => x.Contains("Atlas of Living Australia")) &&
                string.Equals(map.GetEncodedString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase))
            {
                var irn = long.Parse(map.GetEncodedString("irn"));

                var multimedia = new Multimedia
                {
                    Type = "StillImage",
                    Format = "image/jpeg",
                    Identifier = string.Format("{0}.jpg", irn),
                    Title = map.GetEncodedString("MulTitle"),
                    Creator = map.GetEncodedStrings("MulCreator_tab").Concatenate(";"),
                    Publisher = "Museum Victoria",
                    Source = map.GetEncodedStrings("RigSource_tab").Concatenate(";"),
                    RightsHolder = "Museum Victoria",
                    AltText = map.GetEncodedString("DetAlternateText")
                };
                
                var captionMap = map
                    .GetMaps("metadata")
                    .FirstOrDefault(x => string.Equals(x.GetEncodedString("MdaElement_tab"), "dcTitle", StringComparison.OrdinalIgnoreCase) && 
                        string.Equals(x.GetEncodedString("MdaQualifier_tab"), "Caption.COL"));

                if(captionMap != null)
                    multimedia.Description = HtmlConverter.HtmlToText(captionMap.GetEncodedString("MdaFreeText_tab"));

                if (map.GetEncodedString("RigLicence").Equals("CC BY", StringComparison.OrdinalIgnoreCase))
                    multimedia.License = "https://creativecommons.org/licenses/by/4.0/";
                else if (map.GetEncodedString("RigLicence").Equals("CC BY-NC", StringComparison.OrdinalIgnoreCase))
                    multimedia.License = "https://creativecommons.org/licenses/by-nc/4.0/";

                if (TrySaveMultimedia(irn))
                    return multimedia;
            }

            return null;
        }

        public IEnumerable<Multimedia> Make(IEnumerable<Map> maps)
        {
            var multimedias = new List<Multimedia>();

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

            multimedias.AddRange(distinctMediaMaps.Select(Make).Where(x => x != null));

            return multimedias;
        }

        private bool TrySaveMultimedia(long irn)
        {
            try
            {
                using (var imuSession = ImuSessionProvider.CreateInstance("emultimedia"))
                {
                    imuSession.FindKey(irn);
                    var resource = imuSession.Fetch("start", 0, -1, new[] { "resource" }).Rows[0].GetMap("resource");

                    if (resource == null)
                        throw new IMuException("MultimediaResourceNotFound");
                    
                    var mimeFormat = resource["mimeFormat"] as string;

                    using (var fileStream = resource["file"] as FileStream)
                    using (var imageFactory = new ImageProcessor.ImageFactory())
                    using (var file = File.OpenWrite(string.Format("{0}{1}.jpg", Config.Config.Options.Ala.Destination, irn)))
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
                Log.Logger.Error(ex, "Error saving multimedia {irn}", irn);
            }

            return false;
        }
    }
}
