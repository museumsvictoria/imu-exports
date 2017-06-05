using System;
using System.Collections.Generic;
using System.Linq;
using IMu;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.FieldGuideGippsland.Models;

namespace ImuExports.Tasks.FieldGuideGippsland.Factories
{
    public class GippslandSpeciesFactory : IFactory<GippslandSpecies>
    {        
        private readonly IFactory<GippslandImage> gippslandImageFactory;
        private readonly IFactory<GippslandAudio> gippslandAudioFactory;

        public GippslandSpeciesFactory(IFactory<GippslandImage> gippslandImageFactory,
            IFactory<GippslandAudio> gippslandAudioFactory)
        {
            this.gippslandImageFactory = gippslandImageFactory;
            this.gippslandAudioFactory = gippslandAudioFactory;
        }

        public GippslandSpecies Make(Map map)
        {
            var species = new GippslandSpecies();

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

            species.AnimalType = map.GetTrimString("SpeTaxonGroup");
            species.AnimalSubType = map.GetTrimString("SpeTaxonSubGroup");
            species.BriefDescription = map.GetTrimString("SpeBriefID");
            species.IdentifyingCharacteristics = map.GetTrimString("SpeIdentifyingCharacters");
            species.Habitat = map.GetTrimString("SpeHabitatNotes");
            species.Distribution = map.GetTrimString("SpeDistribution");
            species.Biology = map.GetTrimString("SpeBiology");
            species.Diet = map.GetTrimString("SpeDiet");
            species.LocalKnowledge = map.GetTrimString("SpeFastFact");
            species.HazardousToHumans = map.GetTrimString("SpeHazards");
            species.FlightMonthFrom = map.GetTrimString("SpeFlightStart");
            species.FlightMonthTo = map.GetTrimString("SpeFlightEnd");
            species.Depths = map.GetTrimStrings("SpeDepth_tab").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            species.WaterColumnLocations = map.GetTrimStrings("SpeWaterColumnLocation_tab").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            species.Colours = map.GetTrimStrings("SpeColour_tab").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            species.Habitats = map.GetTrimStrings("SpeHabitat_tab").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
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

            species.Images = gippslandImageFactory.Make(map.GetMaps("media")).ToList();            
            species.Audios = gippslandAudioFactory.Make(map.GetMaps("media")).ToList();

            return species;
        }

        public IEnumerable<GippslandSpecies> Make(IEnumerable<Map> maps)
        {
            return maps.Select(Make);
        }
    }
}