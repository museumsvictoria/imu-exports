using System;
using System.Collections.Generic;
using System.Linq;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.FieldGuideGunditjmara.Models;
using IMu;

namespace ImuExports.Tasks.FieldGuideGunditjmara.Factories
{
    public class GunditjmaraSpeciesFactory : IFactory<GunditjmaraSpecies>
    {        
        private readonly IFactory<GunditjmaraImage> gunditjmaraImageFactory;
        private readonly IFactory<GunditjmaraAudio> gunditjmaraAudioFactory;

        public GunditjmaraSpeciesFactory(IFactory<GunditjmaraImage> gunditjmaraImageFactory,
            IFactory<GunditjmaraAudio> gunditjmaraAudioFactory)
        {
            this.gunditjmaraImageFactory = gunditjmaraImageFactory;
            this.gunditjmaraAudioFactory = gunditjmaraAudioFactory;
        }

        public GunditjmaraSpecies Make(Map map)
        {
            var species = new GunditjmaraSpecies();

            species.Irn = map.GetLong("irn");
            
            var taxonomyInformationMap = map.GetMaps("taxa").FirstOrDefault();
            if (taxonomyInformationMap != null)
            {
                var taxonomyMap = taxonomyInformationMap.GetMap("TaxTaxaRef_tab");
                if (taxonomyMap != null)
                {
                    var otherCommonNames = new List<string>();
                    var names = taxonomyMap.GetMaps("comname");

                    foreach (var name in names)
                    {
                        var status = name.GetTrimString("ComStatus_tab");
                        var vernacularName = name.GetTrimString("ComName_tab");

                        if (string.Equals(status, "preferred", StringComparison.OrdinalIgnoreCase))
                        {
                            species.CommonName = vernacularName;
                        }
                        else if (string.Equals(status, "other", StringComparison.OrdinalIgnoreCase))
                        {
                            otherCommonNames.Add(vernacularName);
                        }
                    }

                    species.OtherCommonNames = otherCommonNames.Concatenate(", ");
                }

                species.Lsid = taxonomyInformationMap.GetTrimString("TaxTaxaNotes_tab");
            }

            species.Group = map.GetTrimString("SpeGroup");
            species.AnimalType = map.GetTrimString("SpeTaxonGroup");
            species.AnimalSubType = map.GetTrimString("SpeTaxonSubGroup");
            species.BriefDescription = map.GetTrimString("SpeBriefID");
            species.IdentifyingCharacteristics = map.GetTrimString("SpeIdentifyingCharacters");
            species.Habitat = map.GetTrimString("SpeHabitatNotes");
            species.Distribution = map.GetTrimString("SpeDistribution");
            species.Biology = map.GetTrimString("SpeBiology");
            species.Diet = map.GetTrimString("SpeDiet");
            species.TraditionalKnowledge = map.GetTrimString("SpeFastFact");
            species.HazardousToHumans = map.GetTrimString("SpeHazards");
            species.CallingMonths = map.GetTrimStrings("SpeCallTimeFromTo_tab");
            species.FlightMonthFrom = map.GetTrimString("SpeFlightStart");
            species.FlightMonthTo = map.GetTrimString("SpeFlightEnd");
            species.Depths = map.GetTrimStrings("SpeDepth_tab");
            species.WaterColumnLocations = map.GetTrimStrings("SpeWaterColumnLocation_tab");
            species.Colours = map.GetTrimStrings("SpeColour_tab");
            species.Habitats = map.GetTrimStrings("SpeHabitat_tab");
            species.MaximumSize = map.GetTrimString("SpeMaximumSize");

            var authorsMap = map.GetMaps("authors");
            
            foreach (var authorMap in authorsMap)
            {
                string author = null;

                switch (authorMap.GetTrimString("NamPartyType"))
                {
                    case "Person":
                        author = new[]
                        {
                            authorMap.GetTrimString("NamFullName"),
                            authorMap.GetTrimString("NamOrganisation")
                        }.Concatenate(" / ");
                        break;
                    case "Organisation":
                        author =  authorMap.GetTrimString("NamOrganisation");
                        break;
                    case "Collaboration":
                        author = authorMap.GetTrimString("ColCollaborationName");
                        break;
                }

                if (!string.IsNullOrWhiteSpace(author))
                    species.Authors.Add(author);
            }

            species.Images = gunditjmaraImageFactory.Make(map.GetMaps("media")).ToList();
            species.Audios = gunditjmaraAudioFactory.Make(map.GetMaps("media")).ToList();

            return species;
        }

        public IEnumerable<GunditjmaraSpecies> Make(IEnumerable<Map> maps)
        {
            return maps.Select(Make);
        }
    }
}