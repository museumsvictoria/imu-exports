using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImuExports.Config;
using IMu;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.FieldGuideGippsland.Models;
using Serilog;

namespace ImuExports.Tasks.FieldGuideGippsland.Factories
{
    public class AudioFactory : IFactory<Audio>
    {
        public Audio Make(Map map)
        {
            if (map != null &&
                string.Equals(map.GetEncodedString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                map.GetEncodedStrings("MdaDataSets_tab").Any(x => x.Contains("App: Gippsland")) &&
                string.Equals(map.GetEncodedString("MulMimeType"), "audio", StringComparison.OrdinalIgnoreCase))
            {
                var irn = long.Parse(map.GetString("irn"));

                var audio = new Audio();

                var captionMap = map.GetMaps("metadata").FirstOrDefault(x => string.Equals(x.GetEncodedString("MdaElement_tab"), "dcDescription", StringComparison.OrdinalIgnoreCase) && string.Equals(x.GetEncodedString("MdaQualifier_tab"), "Caption.AppGippsland"));
                if (captionMap != null)
                    audio.Caption = captionMap.GetEncodedString("MdaFreeText_tab");

                audio.AlternateText = map.GetEncodedString("DetAlternateText");
                audio.Creators = map.GetEncodedStrings("RigCreator_tab");
                audio.Sources = map.GetEncodedStrings("RigSource_tab");
                audio.Acknowledgment = map.GetEncodedString("RigAcknowledgementCredit");
                audio.CopyrightStatus = map.GetEncodedString("RigCopyrightStatus");
                audio.CopyrightStatement = map.GetEncodedString("RigCopyrightStatement");
                audio.Licence = map.GetEncodedString("RigLicence");
                audio.LicenceDetails = map.GetEncodedString("RigLicenceDetails");
                audio.Filename = string.Format("{0}{1}", irn, Path.GetExtension(map.GetEncodedString("MulIdentifier")));

                if (TrySaveAudio(irn, audio.Filename))
                {
                    return audio;
                }
            }
            
            return null;
        }

        public IEnumerable<Audio> Make(IEnumerable<Map> maps)
        {
            var audios = new List<Audio>();

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

            audios.AddRange(distinctMediaMaps.Select(Make).Where(x => x != null));

            return audios;
        }

        private bool TrySaveAudio(long irn, string filename)
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
                    using (var file = File.OpenWrite(string.Format("{0}{1}", GlobalOptions.Options.Fgg.Destination, filename)))
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
