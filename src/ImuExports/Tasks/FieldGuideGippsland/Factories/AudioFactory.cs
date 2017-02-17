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
                string.Equals(map.GetTrimString("AdmPublishWebNoPassword"), "yes", StringComparison.OrdinalIgnoreCase) &&
                map.GetTrimStrings("MdaDataSets_tab").Any(x => x.Contains("App: Gippsland")) &&
                string.Equals(map.GetTrimString("MulMimeType"), "audio", StringComparison.OrdinalIgnoreCase))
            {
                var irn = map.GetLong("irn");

                var audio = new Audio();

                var captionMap = map.GetMaps("metadata").FirstOrDefault(x => string.Equals(x.GetTrimString("MdaElement_tab"), "dcDescription", StringComparison.OrdinalIgnoreCase) && string.Equals(x.GetTrimString("MdaQualifier_tab"), "Caption.AppGippsland"));
                if (captionMap != null)
                    audio.Caption = captionMap.GetTrimString("MdaFreeText_tab");

                audio.AlternateText = map.GetTrimString("DetAlternateText");
                audio.Creators = map.GetTrimStrings("RigCreator_tab");
                audio.Sources = map.GetTrimStrings("RigSource_tab");
                audio.Acknowledgment = map.GetTrimString("RigAcknowledgementCredit");
                audio.CopyrightStatus = map.GetTrimString("RigCopyrightStatus");
                audio.CopyrightStatement = map.GetTrimString("RigCopyrightStatement");
                audio.Licence = map.GetTrimString("RigLicence");
                audio.LicenceDetails = map.GetTrimString("RigLicenceDetails");
                audio.Filename = string.Format("{0}{1}", irn, Path.GetExtension(map.GetTrimString("MulIdentifier")));

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
