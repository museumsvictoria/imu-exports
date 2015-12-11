using System;
using System.Collections.Generic;
using System.Linq;
using IMu;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.FieldGuideGippsland.Models;

namespace ImuExports.Tasks.FieldGuideGippsland.Factories
{
    public class SpeciesFactory : IFactory<Species>
    {        
        private readonly IFactory<Image> imageFactory;
        private readonly IFactory<Audio> audioFactory;

        public SpeciesFactory(IFactory<Image> imageFactory,
            IFactory<Audio> audioFactory)
        {
            this.imageFactory = imageFactory;
            this.audioFactory = audioFactory;
        }

        public Species Make(Map map)
        {
            var species = new Species();

            species.Irn = long.Parse(map.GetString("irn"));
            
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
                        var status = name.GetEncodedString("ComStatus_tab");
                        var vernacularName = name.GetEncodedString("ComName_tab");

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

                species.Lsid = taxonomyInformationMap.GetEncodedString("TaxTaxaNotes_tab");
            }

            species.AnimalType = map.GetEncodedString("SpeTaxonGroup");
            species.AnimalSubType = map.GetEncodedString("SpeTaxonSubGroup");
            species.BriefDescription = map.GetEncodedString("SpeBriefID");
            species.IdentifyingCharacteristics = map.GetEncodedString("SpeIdentifyingCharacters");
            species.Habitat = map.GetEncodedString("SpeHabitatNotes");
            species.Distribution = map.GetEncodedString("SpeDistribution");
            species.Biology = map.GetEncodedString("SpeBiology");
            species.Diet = map.GetEncodedString("SpeDiet");
            species.LocalKnowledge = map.GetEncodedString("SpeFastFact");
            species.HazardousToHumans = map.GetEncodedString("SpeHazards");
            species.FlightMonthFrom = map.GetEncodedString("SpeFlightStart");
            species.FlightMonthTo = map.GetEncodedString("SpeFlightEnd");
            species.Depths = map.GetEncodedStrings("SpeDepth_tab").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            species.WaterColumnLocations = map.GetEncodedStrings("SpeWaterColumnLocation_tab").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            species.Colours = map.GetEncodedStrings("SpeColour_tab").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            species.Habitats = map.GetEncodedStrings("SpeHabitat_tab").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            species.MaximumSize = map.GetEncodedString("SpeMaximumSize");

            var authorsMap = map.GetMaps("authors");
            
            foreach (var authorMap in authorsMap)
            {
                string author = null;

                switch (authorMap.GetEncodedString("NamPartyType"))
                {
                    case "Person":
                        author = new[]
                        {
                            authorMap.GetEncodedString("NamFullName"),
                            authorMap.GetEncodedString("NamOrganisation")
                        }.Concatenate(" / ");
                        break;
                    case "Organisation":
                        author =  authorMap.GetEncodedString("NamOrganisation");
                        break;
                    case "Collaboration":
                        author = authorMap.GetEncodedString("ColCollaborationName");
                        break;
                }

                if (!string.IsNullOrWhiteSpace(author))
                    species.Authors.Add(author);
            }

            species.Images = imageFactory.Make(map.GetMaps("media")).ToList();            
            species.Audios = audioFactory.Make(map.GetMaps("media")).ToList();

            return species;
        }

        public IEnumerable<Species> Make(IEnumerable<Map> maps)
        {
            return maps.Select(Make);
        }
    }
}