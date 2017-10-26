using System;
using System.Collections.Generic;
using System.Linq;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.InsideOut.Models;
using IMu;

namespace ImuExports.Tasks.InsideOut.Factories
{
    public class SpeciesFactory : IFactory<Species>
    {        
        private readonly IFactory<Image> imageFactory;
        private readonly IFactory<Audio> audioFactory;
        private readonly IFactory<Thumbnail> thumbnailFactory;

        public SpeciesFactory(            IFactory<Image> imageFactory,
            IFactory<Audio> audioFactory,
            IFactory<Thumbnail> thumbnailFactory)
        {
            this.imageFactory = imageFactory;
            this.audioFactory = audioFactory;
            this.thumbnailFactory = thumbnailFactory;
        }

        public Species Make(Map map)
        {
            var species = new Species();

            species.Irn = map.GetLong("irn");
            species.Scene = map.GetTrimString("DetNarrativeIdentifier");
            species.Priority = map.GetInt("DetVersion");
            species.Title = map.GetTrimString("NarTitle");
            species.Description = map.GetTrimString("NarNarrative");
            if (map.GetMaps("emv").Count() != 0)
            {
                if (string.Equals(map.GetMaps("emv").First().GetTrimString("ColCategory"), 
                    "Natural Sciences", StringComparison.OrdinalIgnoreCase))
                {
                    species.ExternalUrl = "https://collections.museumvictoria.com.au/specimens/" +
                        map.GetMaps("emv").First().GetLong("irn").ToString();
                }
                else if 
                    (string.Equals(map.GetMaps("emv").First().GetTrimString("ColCategory"),
                    "Indigenous Collections", StringComparison.OrdinalIgnoreCase))
                {
                    species.ExternalUrl = "https://collections.museumvictoria.com.au/items/" +
                        map.GetMaps("emv").First().GetLong("irn").ToString();
                }
                else
                {
                    species.ExternalUrl = "https://collections.museumvictoria.com.au/items/" +
                        map.GetMaps("emv").First().GetLong("irn").ToString();
                }
            }
            else
            {
                species.ExternalUrl = null;
            }
            if (map.GetTrimStrings("IntInterviewNotes_tab").Count() != 0)
            {
                species.AudioTranscript = string.Join(" ",map.GetTrimStrings("IntInterviewNotes_tab"));
            }
            else
            {
                species.AudioTranscript = null;
            }
                
            var images = imageFactory.Make(map.GetMaps("media")).ToList();
            var thumbnails = thumbnailFactory.Make(map.GetMaps("media")).ToList();
            var audios = audioFactory.Make(map.GetMaps("media")).ToList();

            if (audios.Count() != 0)
            {
                species.AudioFilename = audios.First().Filename;
            }
            else
            {
                species.AudioFilename = null;
            } 

            if (thumbnails.Count() != 0)
            {
                species.ThumbnailClass = thumbnails.First().ThumbnailClass;
                species.ThumbnailFilename = thumbnails.First().Filename;
            }
            else
            {
                species.ThumbnailClass = null;
                species.ThumbnailFilename = null;
            }

            if (images.Count() != 0)
            {
                species.ImageFilename = images.First().Filename;
            }
            else
            {
                species.ImageFilename = null;
            }
            return species;
        }

        public IEnumerable<Species> Make(IEnumerable<Map> maps)
        {
            return maps.Select(Make);
        }
    }
}