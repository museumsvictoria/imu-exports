using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IMu;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Factories
{
    public class OccurrenceFactory : IFactory<Occurrence>
    {
        private readonly IFactory<Party> partyFactory;
        private readonly IFactory<Multimedia> imageFactory;

        public OccurrenceFactory(IFactory<Party> partyFactory,
            IFactory<Multimedia> imageFactory)
        {
            this.partyFactory = partyFactory;
            this.imageFactory = imageFactory;
        }

        public Occurrence Make(Map map)
        {
            var irn = map.GetLong("irn");

            var occurrence = new Occurrence
            {
                OccurrenceID = string.IsNullOrWhiteSpace(map.GetTrimString("ColRegPart"))
                    ? string.Format(
                        "urn:lsid:ozcam.taxonomy.org.au:NMV:{0}:PreservedSpecimen:{1}{2}",
                        map.GetTrimString("ColDiscipline"), map.GetTrimString("ColRegPrefix"),
                        map.GetTrimString("ColRegNumber"))
                    : string.Format(
                        "urn:lsid:ozcam.taxonomy.org.au:NMV:{0}:PreservedSpecimen:{1}{2}-{3}",
                        map.GetTrimString("ColDiscipline"), map.GetTrimString("ColRegPrefix"),
                        map.GetTrimString("ColRegNumber"), map.GetTrimString("ColRegPart"))
            };

            if (map.GetTrimString("ColTypeOfItem") == "Specimen")
                occurrence.DctermsType = "PhysicalObject";

            DateTime dctermsModified;
            if (DateTime.TryParseExact(
                string.Format("{0} {1}", map.GetTrimString("AdmDateModified"), map.GetTrimString("AdmTimeModified")),
                "dd/MM/yyyy HH:mm",
                new CultureInfo("en-AU"),
                DateTimeStyles.None,
                out dctermsModified))
            {
                occurrence.DctermsModified = dctermsModified.ToString("s");
            }
            
            occurrence.DctermsLanguage = "en";
            occurrence.DctermsLicense = "https://creativecommons.org/publicdomain/zero/1.0/legalcode";
            occurrence.DctermsRightsHolder = "Museums Victoria";
            occurrence.InstitutionId = occurrence.InstitutionCode = occurrence.OwnerInstitutionCode = "NMV";
            occurrence.CollectionId = "urn:lsid:biocol.org:col:34978";
            occurrence.DatasetId = occurrence.CollectionCode = map.GetTrimString("ColDiscipline");
            occurrence.OccurrenceStatus = "present";

            var colevent = map.GetMap("colevent");
            if (colevent != null)
                occurrence.DatasetName = colevent.GetTrimString("ExpExpeditionName");

            if (map.GetTrimString("ColTypeOfItem") == "Specimen")
                occurrence.BasisOfRecord = "PreservedSpecimen";
            if (map.GetTrimString("ColTypeOfItem") == "Audiovisual" || map.GetTrimString("ColTypeOfItem") == "Image")
                occurrence.BasisOfRecord = "HumanObservation";

            occurrence.CatalogNumber = (string.IsNullOrWhiteSpace(map.GetTrimString("ColRegPart")))
                                      ? string.Format("{0}{1}", map.GetTrimString("ColRegPrefix"), map.GetTrimString("ColRegNumber"))
                                      : string.Format("{0}{1}-{2}", map.GetTrimString("ColRegPrefix"), map.GetTrimString("ColRegNumber"), map.GetTrimString("ColRegPart"));

            var individualCount = 0;
            if (!string.IsNullOrWhiteSpace(map.GetTrimString("SpeNoSpecimens")))
                individualCount += int.Parse(map.GetTrimString("SpeNoSpecimens"));
            if (!string.IsNullOrWhiteSpace(map.GetTrimString("BirTotalClutchSize")))
                individualCount += int.Parse(map.GetTrimString("BirTotalClutchSize"));
            if (individualCount > 0)
                occurrence.IndividualCount = individualCount.ToString();

            occurrence.Sex = map.GetTrimStrings("SpeSex_tab").Concatenate(";");
            occurrence.LifeStage = map.GetTrimStrings("SpeStageAge_tab").Concatenate(";");

            foreach (var preparationMap in map.GetMaps("preparations"))
            {
                var preparation = new[]
                            {
                                new KeyValuePair<string, string>("specimenNature", preparationMap.GetTrimString("StrSpecimenNature_tab")),
                                new KeyValuePair<string, string>("specimenForm", preparationMap.GetTrimString("StrSpecimenForm_tab")),
                                new KeyValuePair<string, string>("fixativeTreatment", preparationMap.GetTrimString("StrFixativeTreatment_tab")),
                                new KeyValuePair<string, string>("storageMedium", preparationMap.GetTrimString("StrStorageMedium_tab"))
                            }
                    .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                    .Select(x => string.Format("{0}={1}", x.Key, x.Value))
                    .Concatenate(";");

                if (occurrence.Preparations != null)
                {
                    occurrence.Preparations += ";" + preparation;
                }
                else
                {
                    occurrence.Preparations = preparation;
                }
            }

            if (colevent != null)
            {
                occurrence.EventID = colevent.GetTrimString("ColCollectionEventCode");
                occurrence.SamplingProtocol = colevent.GetTrimString("ColCollectionMethod");

                DateTime eventDateTo, eventDateFrom;
                TimeSpan eventTimeTo, eventTimeFrom;
                IList<DateTime> eventDates = new List<DateTime>();
                IList<TimeSpan> eventTimes = new List<TimeSpan>();
                var culture = new CultureInfo("en-AU");

                if (DateTime.TryParseExact(colevent.GetTrimString("ColDateVisitedFrom"), new[] { "dd/MM/yyyy", "dd/MM/yy" }, culture, DateTimeStyles.None, out eventDateFrom))
                {
                    if (TimeSpan.TryParseExact(colevent.GetTrimString("ColTimeVisitedFrom"), @"hh\:mm", culture, out eventTimeFrom))
                    {
                        eventDateFrom += eventTimeFrom;
                        eventTimes.Add(eventTimeFrom);
                    }

                    eventDates.Add(eventDateFrom);
                }

                if (DateTime.TryParseExact(colevent.GetTrimString("ColDateVisitedTo"), new[] { "dd/MM/yyyy", "dd/MM/yy" }, culture, DateTimeStyles.None, out eventDateTo))
                {
                    if (TimeSpan.TryParseExact(colevent.GetTrimString("ColTimeVisitedTo"), @"hh\:mm", culture, out eventTimeTo))
                    {
                        eventDateTo += eventTimeTo;
                        eventTimes.Add(eventTimeTo);
                    }

                    eventDates.Add(eventDateTo);
                }

                occurrence.EventDate = eventDates.Select(x => x.ToString("s")).Concatenate(";");
                occurrence.EventTime = eventTimes.Select(x => x.ToString("c")).Concatenate(";");

                occurrence.FieldNumber = colevent.GetTrimString("ColCollectionEventCode");
                occurrence.MinimumDepthInMeters = colevent.GetTrimString("AquDepthFromMet");
                occurrence.MaximumDepthInMeters = colevent.GetTrimString("AquDepthToMet");

                if (colevent.GetMaps("collectors") != null)
                    occurrence.RecordedBy = colevent.GetMaps("collectors").Where(x => x != null).Select(x => partyFactory.Make(x).Name).Concatenate("; ");
            }

            occurrence.Year = map.GetTrimString("DarYearCollected");
            occurrence.Month = map.GetTrimString("DarMonthCollected");
            occurrence.Day = map.GetTrimString("DarDayCollected");

            var site = map.GetMap("site");

            if (site == null && colevent != null)
                site = colevent.GetMap("site");

            if (site != null)
            {
                if (!string.IsNullOrWhiteSpace(site.GetTrimString("SitSiteCode")) || !string.IsNullOrWhiteSpace(site.GetTrimString("SitSiteNumber")))
                    occurrence.LocationID = string.Format("{0}{1}", site.GetTrimString("SitSiteCode"), site.GetTrimString("SitSiteNumber"));

                occurrence.Locality = site.GetTrimString("LocPreciseLocation").ReplaceLineBreaks();
                occurrence.VerbatimLocality = site.GetTrimString("LocPreciseLocation").ReplaceLineBreaks();
                occurrence.MinimumElevationInMeters = site.GetTrimString("LocElevationASLFromMt");
                occurrence.MaximumElevationInMeters = site.GetTrimString("LocElevationASLToMt");

                var geo = site.GetMaps("geo").FirstOrDefault();
                if (geo != null)
                {
                    occurrence.HigherGeography = new[]
                                {
                                    geo.GetTrimString("LocOcean_tab"),
                                    geo.GetTrimString("LocContinent_tab"),
                                    geo.GetTrimString("LocCountry_tab"),
                                    geo.GetTrimString("LocProvinceStateTerritory_tab")
                                }.Concatenate(", ");

                    occurrence.Continent = geo.GetTrimString("LocContinent_tab");
                    occurrence.WaterBody = geo.GetTrimString("LocOcean_tab");
                    occurrence.Country = geo.GetTrimString("LocCountry_tab");
                    occurrence.StateProvince = geo.GetTrimString("LocProvinceStateTerritory_tab");
                    occurrence.IslandGroup = geo.GetTrimString("LocIslandGroup");
                    occurrence.Island = geo.GetTrimString("LocIsland");
                    occurrence.County = geo.GetTrimString("LocDistrictCountyShire_tab");
                    occurrence.Municipality = geo.GetTrimString("LocTownship_tab");
                }

                var latlong = site.GetMaps("latlong").FirstOrDefault();
                if (latlong != null)
                {
                    var decimalLatitude = (object[])latlong["LatLatitudeDecimal_nesttab"];
                    if (decimalLatitude != null && decimalLatitude.Any(x => x != null))
                        occurrence.DecimalLatitude = decimalLatitude.Where(x => x != null).FirstOrDefault().ToString();

                    var decimalLongitude = ((object[])latlong["LatLongitudeDecimal_nesttab"]);
                    if (decimalLongitude != null && decimalLongitude.Any(x => x != null))
                        occurrence.DecimalLongitude = decimalLongitude.Where(x => x != null).FirstOrDefault().ToString();

                    occurrence.CoordinateUncertaintyInMeters = latlong.GetTrimString("LatRadiusNumeric_tab");
                    occurrence.GeodeticDatum = (string.IsNullOrWhiteSpace(latlong.GetTrimString("LatDatum_tab"))) ? "WGS84" : latlong.GetTrimString("LatDatum_tab");

                    occurrence.GeoreferencedBy = partyFactory.Make(latlong.GetMap("determinedBy")).Name;

                    DateTime georeferencedDate;
                    if (DateTime.TryParseExact(latlong.GetTrimString("LatDetDate0"), "dd/MM/yyyy", new CultureInfo("en-AU"), DateTimeStyles.None, out georeferencedDate))
                        occurrence.GeoreferencedDate = georeferencedDate.ToString("s");

                    occurrence.GeoreferenceProtocol = latlong.GetTrimString("LatLatLongDetermination_tab");
                    occurrence.GeoreferenceSources = latlong.GetTrimString("LatDetSource_tab");
                }
            }

            var types = new[] { "holotype", "lectotype", "neotype", "paralectotype", "paratype", "syntype", "type" };
            var identification =
                (map.GetMaps("identifications")
                    .FirstOrDefault(x => (x.GetTrimString("IdeTypeStatus_tab") != null && types.Contains(x.GetTrimString("IdeTypeStatus_tab").Trim().ToLower()))) ??
                 map.GetMaps("identifications")
                     .FirstOrDefault(x => (x.GetTrimString("IdeCurrentNameLocal_tab") != null && x.GetTrimString("IdeCurrentNameLocal_tab").Trim().ToLower() == "yes"))) ??
                map.GetMaps("identifications").FirstOrDefault(x => x != null);

            if (identification != null)
            {
                occurrence.TypeStatus = identification.GetTrimString("IdeTypeStatus_tab");
                occurrence.DateIdentified = identification.GetTrimString("IdeDateIdentified0");
                
                if (identification.GetMaps("identifiers") != null)
                    occurrence.IdentifiedBy = identification.GetMaps("identifiers").Where(x => x != null).Select(x => partyFactory.Make(x).Name).Concatenate("; ");

                var taxonomy = identification.GetMap("taxa");
                if (taxonomy != null)
                {
                    occurrence.ScientificName = new[]
                            {
                                taxonomy.GetCleanString("ClaGenus"),
                                string.IsNullOrWhiteSpace(taxonomy.GetCleanString("ClaSubgenus"))
                                    ? null
                                    : string.Format("({0})", taxonomy.GetCleanString("ClaSubgenus")),
                                taxonomy.GetCleanString("ClaSpecies"),
                                taxonomy.GetCleanString("ClaSubspecies"),
                                taxonomy.GetTrimString("AutAuthorString")
                            }.Concatenate(" ");

                    occurrence.Kingdom = taxonomy.GetCleanString("ClaKingdom");
                    occurrence.Phylum = taxonomy.GetCleanString("ClaPhylum");
                    occurrence.Class = taxonomy.GetCleanString("ClaClass");
                    occurrence.Order = taxonomy.GetCleanString("ClaOrder");
                    occurrence.Family = taxonomy.GetCleanString("ClaFamily");
                    occurrence.Genus = taxonomy.GetCleanString("ClaGenus");
                    occurrence.Subgenus = taxonomy.GetCleanString("ClaSubgenus");
                    occurrence.SpecificEpithet = taxonomy.GetCleanString("ClaSpecies");
                    occurrence.InfraspecificEpithet = taxonomy.GetCleanString("ClaSubspecies");
                    occurrence.HigherClassification = new[]
                                {
                                    taxonomy.GetCleanString("ClaKingdom"), 
                                    taxonomy.GetCleanString("ClaPhylum"),
                                    taxonomy.GetCleanString("ClaSubphylum"),
                                    taxonomy.GetCleanString("ClaSuperclass"),
                                    taxonomy.GetCleanString("ClaClass"),
                                    taxonomy.GetCleanString("ClaSubclass"),
                                    taxonomy.GetCleanString("ClaSuperorder"),
                                    taxonomy.GetCleanString("ClaOrder"),
                                    taxonomy.GetCleanString("ClaSuborder"),
                                    taxonomy.GetCleanString("ClaInfraorder"),
                                    taxonomy.GetCleanString("ClaSuperfamily"),
                                    taxonomy.GetCleanString("ClaFamily"),
                                    taxonomy.GetCleanString("ClaSubfamily")
                                }.Concatenate(";");

                    occurrence.TaxonRank = new Dictionary<string, string>
                            {
                                {"Kingdom", taxonomy.GetCleanString("ClaKingdom")},
                                {"Phylum", taxonomy.GetCleanString("ClaPhylum")},
                                {"Subphylum", taxonomy.GetCleanString("ClaSubphylum")},
                                {"Superclass", taxonomy.GetCleanString("ClaSuperclass")},
                                {"Class", taxonomy.GetCleanString("ClaClass")},
                                {"Subclass", taxonomy.GetCleanString("ClaSubclass")},
                                {"Superorder", taxonomy.GetCleanString("ClaSuperorder")},
                                {"Order", taxonomy.GetCleanString("ClaOrder")},
                                {"Suborder", taxonomy.GetCleanString("ClaSuborder")},
                                {"Infraorder", taxonomy.GetCleanString("ClaInfraorder")},
                                {"Superfamily", taxonomy.GetCleanString("ClaSuperfamily")},
                                {"Family", taxonomy.GetCleanString("ClaFamily")},
                                {"Subfamily", taxonomy.GetCleanString("ClaSubfamily")},
                                {"Genus", taxonomy.GetCleanString("ClaGenus")},
                                {"Subgenus", taxonomy.GetCleanString("ClaSubgenus")},
                                {"Species", taxonomy.GetCleanString("ClaSpecies")},
                                {"Subspecies", taxonomy.GetCleanString("ClaSubspecies")}
                            }.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Key).LastOrDefault();

                    occurrence.ScientificNameAuthorship = taxonomy.GetTrimString("AutAuthorString");
                    occurrence.NomenclaturalCode = taxonomy.GetTrimString("ClaApplicableCode");

                    var vernacularName = taxonomy.GetMaps("comname").FirstOrDefault(x => x.GetTrimString("ComStatus_tab") != null && x.GetTrimString("ComStatus_tab").Trim().ToLower() == "preferred");
                    if (vernacularName != null)
                        occurrence.VernacularName = vernacularName.GetTrimString("ComName_tab");
                }

                var identificationQualifier = identification.GetTrimString("IdeQualifier_tab");
                if (!string.IsNullOrWhiteSpace(identificationQualifier))
                {
                    if (string.Equals(identification.GetTrimString("IdeQualifierRank_tab"), "Genus", StringComparison.OrdinalIgnoreCase))
                        occurrence.IdentificationQualifier = string.Format("{0} {1}", identificationQualifier, occurrence.Genus);
                    else if (string.Equals(identification.GetTrimString("IdeQualifierRank_tab"), "species", StringComparison.OrdinalIgnoreCase))
                        occurrence.IdentificationQualifier = string.Format("{0} {1}", identificationQualifier, occurrence.SpecificEpithet);
                }
            }

            occurrence.Images = imageFactory.Make(map.GetMaps("media")).ToList();
            
            foreach (var image in occurrence.Images)
            {
                image.CoreID = occurrence.OccurrenceID;
                image.References = string.Format("http://collections.museumvictoria.com.au/specimens/{0}", irn);
            }

            occurrence.AssociatedMedia = occurrence.Images.Select(x => x.Identifier).Concatenate(";");

            return occurrence;
        }

        public IEnumerable<Occurrence> Make(IEnumerable<Map> maps)
        {
            return maps.Select(Make);
        }
    }
}