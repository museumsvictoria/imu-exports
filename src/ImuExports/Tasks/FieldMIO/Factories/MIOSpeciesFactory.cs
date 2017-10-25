using System;
using System.Collections.Generic;
using System.Linq;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.FieldMIO.Models;
using IMu;

namespace ImuExports.Tasks.FieldMIO.Factories
{
    public class MIOSpeciesFactory : IFactory<MIOSpecies>
    {        
        private readonly IFactory<MIOImage> MIOImageFactory;
        private readonly IFactory<MIOAudio> MIOAudioFactory;
        private readonly IFactory<MIOThumbnail> MIOThumbnailFactory;

        public MIOSpeciesFactory(IFactory<MIOImage> MIOImageFactory,
            IFactory<MIOAudio> MIOAudioFactory,
            IFactory<MIOThumbnail> MIOThumbnailFactory
            )
        {
            this.MIOImageFactory = MIOImageFactory;
            this.MIOAudioFactory = MIOAudioFactory;
            this.MIOThumbnailFactory = MIOThumbnailFactory;
        }

        public MIOSpecies Make(Map map)
        {
            var species = new MIOSpecies();

            species.irn = map.GetLong("irn");
            species.scene = map.GetTrimString("DetNarrativeIdentifier");
            species.priority = map.GetInt("DetVersion");
            species.title = map.GetTrimString("NarTitle");
            species.description = map.GetTrimString("NarNarrative");
            if (map.GetMaps("emv").Count() != 0)
            {
                if (string.Equals(map.GetMaps("emv").First().GetTrimString("ColCategory"), 
                    "Natural Sciences", StringComparison.OrdinalIgnoreCase))
                {
                    species.externalUrl = "https://collections.museumvictoria.com.au/specimens/" +
                        map.GetMaps("emv").First().GetLong("irn").ToString();
                }
                else if 
                    (string.Equals(map.GetMaps("emv").First().GetTrimString("ColCategory"),
                    "Indigenous Collections", StringComparison.OrdinalIgnoreCase))
                {
                    species.externalUrl = "https://collections.museumvictoria.com.au/items/" +
                        map.GetMaps("emv").First().GetLong("irn").ToString();
                }
                else
                {
                    species.externalUrl = "https://collections.museumvictoria.com.au/items/" +
                        map.GetMaps("emv").First().GetLong("irn").ToString();
                }
            }
            else
            {
                species.externalUrl = null;
            }
            if (map.GetTrimStrings("IntInterviewNotes_tab").Count() != 0)
            {
                species.audioTranscript = string.Join(" ",map.GetTrimStrings("IntInterviewNotes_tab"));
            }
            else
            {
                species.audioTranscript = null;
            }
                
            var images = MIOImageFactory.Make(map.GetMaps("media")).ToList();
            var thumbnails = MIOThumbnailFactory.Make(map.GetMaps("media")).ToList();
            var audios = MIOAudioFactory.Make(map.GetMaps("media")).ToList();

            if (audios.Count() != 0)
            {
                species.audioFilename = audios.First().Filename;
            }
            else
            {
                species.audioFilename = null;
            } 

            if (thumbnails.Count() != 0)
            {
                species.thumbnailClass = thumbnails.First().ThumbnailClass;
                species.thumbnailFilename = thumbnails.First().Filename;
            }
            else
            {
                species.thumbnailClass = null;
                species.thumbnailFilename = null;
            }

            if (images.Count() != 0)
            {
                species.imageFilename = images.First().Filename;
            }
            else
            {
                species.imageFilename = null;
            }
            return species;
        }

        public IEnumerable<MIOSpecies> Make(IEnumerable<Map> maps)
        {
            return maps.Select(Make);
        }
    }
}