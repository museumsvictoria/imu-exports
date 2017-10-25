using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImuExports.Config;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.FieldMIO.Models;
using IMu;
using Serilog;

namespace ImuExports.Tasks.FieldMIO.Factories
{
    public class MIOAudioFactory : IFactory<MIOAudio>
    {
        public MIOAudio Make(Map map)
        {
            if (map != null &&
                string.Equals(map.GetTrimString("MulMimeType"), "audio", StringComparison.OrdinalIgnoreCase))
            {
                var irn = map.GetLong("irn");

                var audio = new MIOAudio();

                audio.Filename = $"{irn}{Path.GetExtension(map.GetTrimString("MulIdentifier"))}";

                if (TrySaveAudio(irn, audio.Filename))
                {
                    return audio;
                }
            }
            
            return null;
        }

        public IEnumerable<MIOAudio> Make(IEnumerable<Map> maps)
        {
            var audios = new List<MIOAudio>();

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
                    using (var file = File.OpenWrite($"{GlobalOptions.Options.Mio.Destination}{filename}"))
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
