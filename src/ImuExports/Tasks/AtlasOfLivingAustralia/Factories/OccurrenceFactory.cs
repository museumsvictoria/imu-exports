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
            var irn = long.Parse(map.GetEncodedString("irn"));

            var occurrence = new Occurrence
            {
                OccurrenceID = (string.IsNullOrWhiteSpace(map.GetEncodedString("ColRegPart")))
                    ? string.Format(
                        "urn:lsid:ozcam.taxonomy.org.au:NMV:{0}:PreservedSpecimen:{1}{2}",
                        map.GetEncodedString("ColDiscipline"), map.GetEncodedString("ColRegPrefix"),
                        map.GetEncodedString("ColRegNumber"))
                    : string.Format(
                        "urn:lsid:ozcam.taxonomy.org.au:NMV:{0}:PreservedSpecimen:{1}{2}-{3}",
                        map.GetEncodedString("ColDiscipline"), map.GetEncodedString("ColRegPrefix"),
                        map.GetEncodedString("ColRegNumber"), map.GetEncodedString("ColRegPart"))
            };

            if (map.GetEncodedString("ColTypeOfItem") == "Specimen")
                occurrence.DctermsType = "PhysicalObject";

            DateTime dctermsModified;
            if (DateTime.TryParseExact(
                string.Format("{0} {1}", map.GetEncodedString("AdmDateModified"), map.GetEncodedString("AdmTimeModified")),
                "dd/MM/yyyy HH:mm",
                new CultureInfo("en-AU"),
                DateTimeStyles.None,
                out dctermsModified))
            {
                occurrence.DctermsModified = dctermsModified.ToString("s");
            }

            occurrence.DctermsLanguage = "en";
            occurrence.DctermsLicense = "https://creativecommons.org/publicdomain/zero/1.0/legalcode";
            occurrence.DctermsRightsHolder = "Museum Victoria";
            occurrence.InstitutionId = occurrence.InstitutionCode = occurrence.OwnerInstitutionCode = "NMV";
            occurrence.CollectionId = "urn:lsid:biocol.org:col:34978";
            occurrence.DatasetId = occurrence.CollectionCode = map.GetEncodedString("ColDiscipline");
            occurrence.OccurrenceStatus = "present";

            var colevent = map.GetMap("colevent");
            if (colevent != null)
                occurrence.DatasetName = colevent.GetEncodedString("ExpExpeditionName");

            if (map.GetEncodedString("ColTypeOfItem") == "Specimen")
                occurrence.BasisOfRecord = "PreservedSpecimen";
            
            occurrence.CatalogNumber = (string.IsNullOrWhiteSpace(map.GetEncodedString("ColRegPart")))
                                      ? string.Format("{0}{1}", map.GetEncodedString("ColRegPrefix"), map.GetEncodedString("ColRegNumber"))
                                      : string.Format("{0}{1}-{2}", map.GetEncodedString("ColRegPrefix"), map.GetEncodedString("ColRegNumber"), map.GetEncodedString("ColRegPart"));

            var individualCount = 0;
            if (!string.IsNullOrWhiteSpace(map.GetEncodedString("SpeNoSpecimens")))
                individualCount += int.Parse(map.GetEncodedString("SpeNoSpecimens"));
            if (!string.IsNullOrWhiteSpace(map.GetEncodedString("BirTotalClutchSize")))
                individualCount += int.Parse(map.GetEncodedString("BirTotalClutchSize"));
            if (individualCount > 0)
                occurrence.IndividualCount = individualCount.ToString();

            occurrence.Sex = map.GetEncodedStrings("SpeSex_tab").Concatenate(";");
            occurrence.LifeStage = map.GetEncodedStrings("SpeStageAge_tab").Concatenate(";");

            foreach (var preparationMap in map.GetMaps("preparations"))
            {
                var preparation = new[]
                            {
                                new KeyValuePair<string, string>("specimenNature", preparationMap.GetEncodedString("StrSpecimenNature_tab")),
                                new KeyValuePair<string, string>("specimenForm", preparationMap.GetEncodedString("StrSpecimenForm_tab")),
                                new KeyValuePair<string, string>("fixativeTreatment", preparationMap.GetEncodedString("StrFixativeTreatment_tab")),
                                new KeyValuePair<string, string>("storageMedium", preparationMap.GetEncodedString("StrStorageMedium_tab"))
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
                occurrence.EventID = colevent.GetEncodedString("ColCollectionEventCode");
                occurrence.SamplingProtocol = colevent.GetEncodedString("ColCollectionMethod");

                DateTime eventDateTo, eventDateFrom;
                TimeSpan eventTimeTo, eventTimeFrom;
                IList<DateTime> eventDates = new List<DateTime>();
                IList<TimeSpan> eventTimes = new List<TimeSpan>();
                var culture = new CultureInfo("en-AU");

                if (DateTime.TryParseExact(colevent.GetEncodedString("ColDateVisitedFrom"), new[] { "dd/MM/yyyy", "dd/MM/yy" }, culture, DateTimeStyles.None, out eventDateFrom))
                {
                    if (TimeSpan.TryParseExact(colevent.GetEncodedString("ColTimeVisitedFrom"), @"hh\:mm", culture, out eventTimeFrom))
                    {
                        eventDateFrom += eventTimeFrom;
                        eventTimes.Add(eventTimeFrom);
                    }

                    eventDates.Add(eventDateFrom);
                }

                if (DateTime.TryParseExact(colevent.GetEncodedString("ColDateVisitedTo"), new[] { "dd/MM/yyyy", "dd/MM/yy" }, culture, DateTimeStyles.None, out eventDateTo))
                {
                    if (TimeSpan.TryParseExact(colevent.GetEncodedString("ColTimeVisitedTo"), @"hh\:mm", culture, out eventTimeTo))
                    {
                        eventDateTo += eventTimeTo;
                        eventTimes.Add(eventTimeTo);
                    }

                    eventDates.Add(eventDateTo);
                }

                occurrence.EventDate = eventDates.Select(x => x.ToString("s")).Concatenate(";");
                occurrence.EventTime = eventTimes.Select(x => x.ToString("c")).Concatenate(";");

                occurrence.FieldNumber = colevent.GetEncodedString("ColCollectionEventCode");
                occurrence.MinimumDepthInMeters = colevent.GetEncodedString("AquDepthFromMet");
                occurrence.MaximumDepthInMeters = colevent.GetEncodedString("AquDepthToMet");

                if (colevent.GetMaps("collectors") != null)
                    occurrence.RecordedBy = colevent.GetMaps("collectors").Where(x => x != null).Select(x => partyFactory.Make(x).Name).Concatenate("; ");
            }

            occurrence.Year = map.GetEncodedString("DarYearCollected");
            occurrence.Month = map.GetEncodedString("DarMonthCollected");
            occurrence.Day = map.GetEncodedString("DarDayCollected");

            var site = map.GetMap("site");

            if (site == null && colevent != null)
                site = colevent.GetMap("site");

            if (site != null)
            {
                if (!string.IsNullOrWhiteSpace(site.GetEncodedString("SitSiteCode")) || !string.IsNullOrWhiteSpace(site.GetEncodedString("SitSiteNumber")))
                    occurrence.LocationID = string.Format("{0}{1}", site.GetEncodedString("SitSiteCode"), site.GetEncodedString("SitSiteNumber"));

                occurrence.Locality = site.GetEncodedString("LocPreciseLocation").ReplaceLineBreaks();
                occurrence.VerbatimLocality = site.GetEncodedString("LocPreciseLocation").ReplaceLineBreaks();
                occurrence.MinimumElevationInMeters = site.GetEncodedString("LocElevationASLFromMt");
                occurrence.MaximumElevationInMeters = site.GetEncodedString("LocElevationASLToMt");

                var geo = site.GetMaps("geo").FirstOrDefault();
                if (geo != null)
                {
                    occurrence.HigherGeography = new[]
                                {
                                    geo.GetEncodedString("LocOcean_tab"),
                                    geo.GetEncodedString("LocContinent_tab"),
                                    geo.GetEncodedString("LocCountry_tab"),
                                    geo.GetEncodedString("LocProvinceStateTerritory_tab")
                                }.Concatenate(", ");

                    occurrence.Continent = geo.GetEncodedString("LocContinent_tab");
                    occurrence.WaterBody = geo.GetEncodedString("LocOcean_tab");
                    occurrence.Country = geo.GetEncodedString("LocCountry_tab");
                    occurrence.StateProvince = geo.GetEncodedString("LocProvinceStateTerritory_tab");
                    occurrence.County = geo.GetEncodedString("LocDistrictCountyShire_tab");
                    occurrence.Municipality = geo.GetEncodedString("LocTownship_tab");
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

                    occurrence.CoordinateUncertaintyInMeters = latlong.GetEncodedString("LatRadiusNumeric_tab");
                    occurrence.GeodeticDatum = (string.IsNullOrWhiteSpace(latlong.GetEncodedString("LatDatum_tab"))) ? "WGS84" : latlong.GetEncodedString("LatDatum_tab");

                    occurrence.GeoreferencedBy = partyFactory.Make(latlong.GetMap("determinedBy")).Name;

                    DateTime georeferencedDate;
                    if (DateTime.TryParseExact(latlong.GetEncodedString("LatDetDate0"), "dd/MM/yyyy", new CultureInfo("en-AU"), DateTimeStyles.None, out georeferencedDate))
                        occurrence.GeoreferencedDate = georeferencedDate.ToString("s");

                    occurrence.GeoreferenceProtocol = latlong.GetEncodedString("LatLatLongDetermination_tab");
                    occurrence.GeoreferenceSources = latlong.GetEncodedString("LatDetSource_tab");
                }
            }

            var types = new[] { "holotype", "lectotype", "neotype", "paralectotype", "paratype", "syntype", "type" };
            var identification =
                (map.GetMaps("identifications")
                    .FirstOrDefault(x => (x.GetEncodedString("IdeTypeStatus_tab") != null && types.Contains(x.GetEncodedString("IdeTypeStatus_tab").Trim().ToLower()))) ??
                 map.GetMaps("identifications")
                     .FirstOrDefault(x => (x.GetEncodedString("IdeCurrentNameLocal_tab") != null && x.GetEncodedString("IdeCurrentNameLocal_tab").Trim().ToLower() == "yes"))) ??
                map.GetMaps("identifications").FirstOrDefault(x => x != null);

            if (identification != null)
            {
                occurrence.TypeStatus = identification.GetEncodedString("IdeTypeStatus_tab");
                occurrence.DateIdentified = identification.GetEncodedString("IdeDateIdentified0");
                
                if (identification.GetMaps("identifiers") != null)
                    occurrence.IdentifiedBy = identification.GetMaps("identifiers").Where(x => x != null).Select(x => partyFactory.Make(x).Name).Concatenate("; ");

                var taxonomy = identification.GetMap("taxa");
                if (taxonomy != null)
                {
                    occurrence.ScientificName = new[]
                            {
                                taxonomy.GetCleanEncodedString("ClaGenus"),
                                string.IsNullOrWhiteSpace(taxonomy.GetCleanEncodedString("ClaSubgenus"))
                                    ? null
                                    : string.Format("({0})", taxonomy.GetCleanEncodedString("ClaSubgenus")),
                                taxonomy.GetCleanEncodedString("ClaSpecies"),
                                taxonomy.GetCleanEncodedString("ClaSubspecies"),
                                taxonomy.GetEncodedString("AutAuthorString")
                            }.Concatenate(" ");

                    occurrence.Kingdom = taxonomy.GetCleanEncodedString("ClaKingdom");
                    occurrence.Phylum = taxonomy.GetCleanEncodedString("ClaPhylum");
                    occurrence.Class = taxonomy.GetCleanEncodedString("ClaClass");
                    occurrence.Order = taxonomy.GetCleanEncodedString("ClaOrder");
                    occurrence.Family = taxonomy.GetCleanEncodedString("ClaFamily");
                    occurrence.Genus = taxonomy.GetCleanEncodedString("ClaGenus");
                    occurrence.Subgenus = taxonomy.GetCleanEncodedString("ClaSubgenus");
                    occurrence.SpecificEpithet = taxonomy.GetCleanEncodedString("ClaSpecies");
                    occurrence.InfraspecificEpithet = taxonomy.GetCleanEncodedString("ClaSubspecies");
                    occurrence.HigherClassification = new[]
                                {
                                    taxonomy.GetCleanEncodedString("ClaKingdom"), 
                                    taxonomy.GetCleanEncodedString("ClaPhylum"),
                                    taxonomy.GetCleanEncodedString("ClaSubphylum"),
                                    taxonomy.GetCleanEncodedString("ClaSuperclass"),
                                    taxonomy.GetCleanEncodedString("ClaClass"),
                                    taxonomy.GetCleanEncodedString("ClaSubclass"),
                                    taxonomy.GetCleanEncodedString("ClaSuperorder"),
                                    taxonomy.GetCleanEncodedString("ClaOrder"),
                                    taxonomy.GetCleanEncodedString("ClaSuborder"),
                                    taxonomy.GetCleanEncodedString("ClaInfraorder"),
                                    taxonomy.GetCleanEncodedString("ClaSuperfamily"),
                                    taxonomy.GetCleanEncodedString("ClaFamily"),
                                    taxonomy.GetCleanEncodedString("ClaSubfamily")
                                }.Concatenate(";");

                    occurrence.TaxonRank = new Dictionary<string, string>
                            {
                                {"Kingdom", taxonomy.GetCleanEncodedString("ClaKingdom")},
                                {"Phylum", taxonomy.GetCleanEncodedString("ClaPhylum")},
                                {"Subphylum", taxonomy.GetCleanEncodedString("ClaSubphylum")},
                                {"Superclass", taxonomy.GetCleanEncodedString("ClaSuperclass")},
                                {"Class", taxonomy.GetCleanEncodedString("ClaClass")},
                                {"Subclass", taxonomy.GetCleanEncodedString("ClaSubclass")},
                                {"Superorder", taxonomy.GetCleanEncodedString("ClaSuperorder")},
                                {"Order", taxonomy.GetCleanEncodedString("ClaOrder")},
                                {"Suborder", taxonomy.GetCleanEncodedString("ClaSuborder")},
                                {"Infraorder", taxonomy.GetCleanEncodedString("ClaInfraorder")},
                                {"Superfamily", taxonomy.GetCleanEncodedString("ClaSuperfamily")},
                                {"Family", taxonomy.GetCleanEncodedString("ClaFamily")},
                                {"Subfamily", taxonomy.GetCleanEncodedString("ClaSubfamily")},
                                {"Genus", taxonomy.GetCleanEncodedString("ClaGenus")},
                                {"Subgenus", taxonomy.GetCleanEncodedString("ClaSubgenus")},
                                {"Species", taxonomy.GetCleanEncodedString("ClaSpecies")},
                                {"Subspecies", taxonomy.GetCleanEncodedString("ClaSubspecies")}
                            }.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Key).LastOrDefault();

                    occurrence.ScientificNameAuthorship = taxonomy.GetEncodedString("AutAuthorString");
                    occurrence.NomenclaturalCode = taxonomy.GetEncodedString("ClaApplicableCode");

                    var vernacularName = taxonomy.GetMaps("comname").FirstOrDefault(x => x.GetEncodedString("ComStatus_tab") != null && x.GetEncodedString("ComStatus_tab").Trim().ToLower() == "preferred");
                    if (vernacularName != null)
                        occurrence.VernacularName = vernacularName.GetEncodedString("ComName_tab");
                }

                var identificationQualifier = identification.GetEncodedString("IdeQualifier_tab");
                if (!string.IsNullOrWhiteSpace(identificationQualifier))
                {
                    if (string.Equals(identification.GetEncodedString("IdeQualifierRank_tab"), "Genus", StringComparison.OrdinalIgnoreCase))
                        occurrence.IdentificationQualifier = string.Format("{0} {1}", identificationQualifier, occurrence.Genus);
                    else if (string.Equals(identification.GetEncodedString("IdeQualifierRank_tab"), "species", StringComparison.OrdinalIgnoreCase))
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