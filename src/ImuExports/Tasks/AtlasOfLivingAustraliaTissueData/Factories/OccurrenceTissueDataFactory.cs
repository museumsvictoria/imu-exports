using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;
using ImuExports.Tasks.AtlasOfLivingAustraliaTissueData.Models;
using IMu;

namespace ImuExports.Tasks.AtlasOfLivingAustraliaTissueData.Factories
{
    public class OccurrenceTissueDataFactory : IFactory<OccurrenceTissueData>
    {
        private readonly IFactory<Party> partyFactory;

        public OccurrenceTissueDataFactory(IFactory<Party> partyFactory)
        {
            this.partyFactory = partyFactory;
        }

        public OccurrenceTissueData Make(Map map)
        {
            var occurrenceTissueData = new OccurrenceTissueData();

            if (map.GetString("ColTypeOfItem") == "Specimen")
                occurrenceTissueData.DctermsType = "PhysicalObject";

            DateTime dctermsModified;
            if (DateTime.TryParseExact(
                string.Format("{0} {1}", map.GetString("AdmDateModified"), map.GetString("AdmTimeModified")),
                "dd/MM/yyyy HH:mm",
                new CultureInfo("en-AU"),
                DateTimeStyles.None,
                out dctermsModified))
            {
                occurrenceTissueData.DctermsModified = dctermsModified.ToString("s");
            }

            occurrenceTissueData.DatasetId = map.GetString("ColDiscipline");
            occurrenceTissueData.CollectionCode = map.GetString("ColDiscipline");

            var colevent = map.GetMap("colevent");
            if (colevent != null)
                occurrenceTissueData.DatasetName = colevent.GetString("ExpExpeditionName");

            occurrenceTissueData.OccurrenceID = string.IsNullOrWhiteSpace(map.GetString("ColRegPart"))
                ? $"urn:lsid:ozcam.taxonomy.org.au:NMV:{map.GetString("ColDiscipline")}:materialSample:{map.GetString("ColRegPrefix")}{map.GetString("ColRegNumber")}"
                : $"urn:lsid:ozcam.taxonomy.org.au:NMV:{map.GetString("ColDiscipline")}:materialSample:{map.GetString("ColRegPrefix")}{map.GetString("ColRegNumber")}-{map.GetString("ColRegPart")}";

            occurrenceTissueData.CatalogNumber = string.IsNullOrWhiteSpace(map.GetString("ColRegPart"))
                ? $"{map.GetString("ColRegPrefix")}{map.GetString("ColRegNumber")}"
                : $"{map.GetString("ColRegPrefix")}{map.GetString("ColRegNumber")}-{map.GetString("ColRegPart")}";

            if (!string.IsNullOrWhiteSpace(map.GetString("SpeNoSpecimens")) && map.GetString("SpeNoSpecimens") != "0")
                occurrenceTissueData.IndividualCount = map.GetString("SpeNoSpecimens");
            else if (!string.IsNullOrWhiteSpace(map.GetString("BirTotalClutchSize")))
                occurrenceTissueData.IndividualCount = map.GetString("BirTotalClutchSize");

            var sex = map.GetStrings("SpeSex_tab");
            if (sex.Any())
            {
                occurrenceTissueData.Sex = sex.Concatenate(";");
            }

            var lifeStage = map.GetStrings("SpeStageAge_tab");
            if (lifeStage.Any())
            {
                occurrenceTissueData.LifeStage = lifeStage.Concatenate(";");
            }

            if (colevent != null)
            {
                occurrenceTissueData.EventID = colevent.GetString("ColCollectionEventCode");
                occurrenceTissueData.SamplingProtocol = colevent.GetString("ColCollectionMethod");

                DateTime eventDateTo, eventDateFrom;
                IList<DateTime> eventDates = new List<DateTime>();
                IList<TimeSpan> eventTimes = new List<TimeSpan>();
                var culture = new CultureInfo("en-AU");

                if (DateTime.TryParseExact(colevent.GetString("ColDateVisitedFrom"), new[] { "dd/MM/yyyy", "dd/MM/yy" }, culture, DateTimeStyles.None, out eventDateFrom))
                {
                    TimeSpan eventTimeFrom;
                    if (TimeSpan.TryParseExact(colevent.GetString("ColTimeVisitedFrom"), @"hh\:mm", culture, out eventTimeFrom))
                    {
                        eventDateFrom += eventTimeFrom;
                        eventTimes.Add(eventTimeFrom);
                    }

                    eventDates.Add(eventDateFrom);
                }

                if (DateTime.TryParseExact(colevent.GetString("ColDateVisitedTo"), new[] { "dd/MM/yyyy", "dd/MM/yy" }, culture, DateTimeStyles.None, out eventDateTo))
                {
                    TimeSpan eventTimeTo;
                    if (TimeSpan.TryParseExact(colevent.GetString("ColTimeVisitedTo"), @"hh\:mm", culture, out eventTimeTo))
                    {
                        eventDateTo += eventTimeTo;
                        eventTimes.Add(eventTimeTo);
                    }

                    eventDates.Add(eventDateTo);
                }

                occurrenceTissueData.EventDate = eventDates.Select(x => x.ToString("s")).Concatenate(";");
                occurrenceTissueData.EventTime = eventTimes.Select(x => x.ToString("c")).Concatenate(";");

                occurrenceTissueData.FieldNumber = colevent.GetString("ColCollectionEventCode");
                occurrenceTissueData.MinimumDepthInMeters = colevent.GetString("AquDepthFromMet");
                occurrenceTissueData.MaximumDepthInMeters = colevent.GetString("AquDepthToMet");

                if (colevent.GetMaps("collectors") != null)
                    occurrenceTissueData.RecordedBy = colevent.GetMaps("collectors").Where(x => x != null).Select(x => partyFactory.Make(x).Name).Concatenate("; ");
            }

            occurrenceTissueData.Year = map.GetString("DarYearCollected");
            occurrenceTissueData.Month = map.GetString("DarMonthCollected");
            occurrenceTissueData.Day = map.GetString("DarDayCollected");

            var site = map.GetMap("site");
            if (site == null && colevent != null)
                site = colevent.GetMap("site");
            if (site != null)
            {
                if (!string.IsNullOrWhiteSpace(site.GetString("SitSiteCode")) || !string.IsNullOrWhiteSpace(site.GetString("SitSiteNumber")))
                    occurrenceTissueData.LocationID = $"{site.GetString("SitSiteCode")}{site.GetString("SitSiteNumber")}";

                occurrenceTissueData.Locality = site.GetString("LocPreciseLocation");
                occurrenceTissueData.VerbatimLocality = site.GetString("LocPreciseLocation");
                occurrenceTissueData.MinimumElevationInMeters = site.GetString("LocElevationASLFromMt");
                occurrenceTissueData.MaximumElevationInMeters = site.GetString("LocElevationASLToMt");

                var geo = site.GetMaps("geo").FirstOrDefault();
                if (geo != null)
                {
                    occurrenceTissueData.HigherGeography = new[]
                        {
                                    geo.GetString("LocOcean_tab"),
                                    geo.GetString("LocContinent_tab"),
                                    geo.GetString("LocCountry_tab"),
                                    geo.GetString("LocProvinceStateTerritory_tab")
                                }.Concatenate(", ");

                    occurrenceTissueData.Continent = geo.GetString("LocContinent_tab");
                    occurrenceTissueData.WaterBody = geo.GetString("LocOcean_tab");
                    occurrenceTissueData.Country = geo.GetString("LocCountry_tab");
                    occurrenceTissueData.StateProvince = geo.GetString("LocProvinceStateTerritory_tab");
                    occurrenceTissueData.County = geo.GetString("LocDistrictCountyShire_tab");
                    occurrenceTissueData.Municipality = geo.GetString("LocTownship_tab");
                }

                var latlong = site.GetMaps("latlong").FirstOrDefault();
                if (latlong != null)
                {
                    var decimalLatitude = (object[])latlong["LatLatitudeDecimal_nesttab"];
                    if (decimalLatitude != null && decimalLatitude.Any(x => x != null))
                        occurrenceTissueData.DecimalLatitude = decimalLatitude.Where(x => x != null).FirstOrDefault().ToString();

                    var decimalLongitude = ((object[])latlong["LatLongitudeDecimal_nesttab"]);
                    if (decimalLongitude != null && decimalLongitude.Any(x => x != null))
                        occurrenceTissueData.DecimalLongitude = decimalLongitude.Where(x => x != null).FirstOrDefault().ToString();

                    occurrenceTissueData.CoordinateUncertaintyInMeters = latlong.GetString("LatRadiusNumeric_tab");
                    occurrenceTissueData.GeodeticDatum = (string.IsNullOrWhiteSpace(latlong.GetString("LatDatum_tab"))) ? "WGS84" : latlong.GetString("LatDatum_tab");

                    occurrenceTissueData.GeoreferencedBy = partyFactory.Make(latlong.GetMap("determinedBy")).Name;

                    DateTime georeferencedDate;
                    if (DateTime.TryParseExact(latlong.GetString("LatDetDate0"), "dd/MM/yyyy", new CultureInfo("en-AU"), DateTimeStyles.None, out georeferencedDate))
                        occurrenceTissueData.GeoreferencedDate = georeferencedDate.ToString("s");

                    occurrenceTissueData.GeoreferenceProtocol = latlong.GetString("LatLatLongDetermination_tab");
                    occurrenceTissueData.GeoreferenceSources = latlong.GetString("LatDetSource_tab");
                }
            }

            var types = new[] { "holotype", "lectotype", "neotype", "paralectotype", "paratype", "syntype", "type" };
            var identification = (map.GetMaps("identifications")
                    .FirstOrDefault(x => x.GetString("IdeTypeStatus_tab") != null && types.Contains(x.GetString("IdeTypeStatus_tab").Trim().ToLower())) ??
                 map.GetMaps("identifications")
                    .FirstOrDefault(x => x.GetString("IdeCurrentNameLocal_tab") != null && x.GetString("IdeCurrentNameLocal_tab").Trim().ToLower() == "yes")) ??
                 map.GetMaps("identifications")
                    .FirstOrDefault(x => x != null);

            if (identification != null)
            {
                occurrenceTissueData.TypeStatus = identification.GetString("IdeTypeStatus_tab");

                if (identification.GetMaps("identifiers") != null)
                    occurrenceTissueData.IdentifiedBy = identification.GetMaps("identifiers").Where(x => x != null).Select(x => partyFactory.Make(x).Name).Concatenate("; ");

                occurrenceTissueData.DateIdentified = identification.GetString("IdeDateIdentified0");

                var taxonomy = identification.GetMap("taxa");
                if (taxonomy != null)
                {
                    occurrenceTissueData.ScientificName = new[]
                    {
                                taxonomy.GetString("ClaGenus"),
                                string.IsNullOrWhiteSpace(taxonomy.GetString("ClaSubgenus")) ? null : $"({taxonomy.GetString("ClaSubgenus")})",
                                taxonomy.GetString("ClaSpecies"),
                                taxonomy.GetString("ClaSubspecies"),
                                taxonomy.GetString("AutAuthorString")
                            }.Concatenate(" ");

                    occurrenceTissueData.Kingdom = taxonomy.GetString("ClaKingdom");
                    occurrenceTissueData.Phylum = taxonomy.GetString("ClaPhylum");
                    occurrenceTissueData.Class = taxonomy.GetString("ClaClass");
                    occurrenceTissueData.Order = taxonomy.GetString("ClaOrder");
                    occurrenceTissueData.Family = taxonomy.GetString("ClaFamily");
                    occurrenceTissueData.Genus = taxonomy.GetString("ClaGenus");
                    occurrenceTissueData.Subgenus = taxonomy.GetString("ClaSubgenus");
                    occurrenceTissueData.SpecificEpithet = taxonomy.GetString("ClaSpecies");
                    occurrenceTissueData.InfraspecificEpithet = taxonomy.GetString("ClaSubspecies");

                    occurrenceTissueData.HigherClassification = new[]
                        {
                                    taxonomy.GetString("ClaKingdom"),
                                    taxonomy.GetString("ClaPhylum"),
                                    taxonomy.GetString("ClaSubphylum"),
                                    taxonomy.GetString("ClaSuperclass"),
                                    taxonomy.GetString("ClaClass"),
                                    taxonomy.GetString("ClaSubclass"),
                                    taxonomy.GetString("ClaSuperorder"),
                                    taxonomy.GetString("ClaOrder"),
                                    taxonomy.GetString("ClaSuborder"),
                                    taxonomy.GetString("ClaInfraorder"),
                                    taxonomy.GetString("ClaSuperfamily"),
                                    taxonomy.GetString("ClaFamily"),
                                    taxonomy.GetString("ClaSubfamily")
                                }.Concatenate(";");

                    occurrenceTissueData.TaxonRank = new Dictionary<string, string>
                            {
                                {"Kingdom", taxonomy.GetString("ClaKingdom")},
                                {"Phylum", taxonomy.GetString("ClaPhylum")},
                                {"Subphylum", taxonomy.GetString("ClaSubphylum")},
                                {"Superclass", taxonomy.GetString("ClaSuperclass")},
                                {"Class", taxonomy.GetString("ClaClass")},
                                {"Subclass", taxonomy.GetString("ClaSubclass")},
                                {"Superorder", taxonomy.GetString("ClaSuperorder")},
                                {"Order", taxonomy.GetString("ClaOrder")},
                                {"Suborder", taxonomy.GetString("ClaSuborder")},
                                {"Infraorder", taxonomy.GetString("ClaInfraorder")},
                                {"Superfamily", taxonomy.GetString("ClaSuperfamily")},
                                {"Family", taxonomy.GetString("ClaFamily")},
                                {"Subfamily", taxonomy.GetString("ClaSubfamily")},
                                {"Genus", taxonomy.GetString("ClaGenus")},
                                {"Subgenus", taxonomy.GetString("ClaSubgenus")},
                                {"Species", taxonomy.GetString("ClaSpecies")},
                                {"Subspecies", taxonomy.GetString("ClaSubspecies")}
                            }.Where(x => !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Key).LastOrDefault();

                    occurrenceTissueData.ScientificNameAuthorship = taxonomy.GetString("AutAuthorString");
                    occurrenceTissueData.NomenclaturalCode = taxonomy.GetString("ClaApplicableCode");

                    var vernacularName = taxonomy.GetMaps("comname").FirstOrDefault(x => x.GetString("ComStatus_tab") != null && x.GetString("ComStatus_tab").Trim().ToLower() == "preferred");
                    if (vernacularName != null)
                        occurrenceTissueData.VernacularName = vernacularName.GetString("ComName_tab");
                }

                var identificationQualifier = identification.GetString("IdeQualifier_tab");
                if (!string.IsNullOrWhiteSpace(identificationQualifier))
                {
                    if (string.Equals(identification.GetString("IdeQualifierRank_tab"), "Genus", StringComparison.OrdinalIgnoreCase))
                        occurrenceTissueData.IdentificationQualifier = $"{identificationQualifier} {occurrenceTissueData.Genus}";
                    else if (string.Equals(identification.GetString("IdeQualifierRank_tab"), "species", StringComparison.OrdinalIgnoreCase))
                        occurrenceTissueData.IdentificationQualifier = $"{identificationQualifier} {occurrenceTissueData.SpecificEpithet}";
                }
            }

            // Tissue data fields
            if (map.GetString("ColTypeOfItem") == "Specimen")
            {
                occurrenceTissueData.BasisOfRecord = "materialSample";
                occurrenceTissueData.MaterialSampleType = "Tissue";
            }

            occurrenceTissueData.Preparations = map.GetMaps("preparations")
                .Select(x => new[]
                {
                    new KeyValuePair<string, string>("specimenNature", x.GetString("StrSpecimenNature_tab")),
                    new KeyValuePair<string, string>("specimenForm", x.GetString("StrSpecimenForm_tab")),
                    new KeyValuePair<string, string>("fixativeTreatment", x.GetString("StrFixativeTreatment_tab")),
                    new KeyValuePair<string, string>("storageMedium", x.GetString("StrStorageMedium_tab"))
                })
                .Concat(map.GetMaps("tissue")
                    .Select(x => new[]
                    {
                        new KeyValuePair<string, string>("initialPreservation", x.GetString("TisInitialPreservation_tab")),
                        new KeyValuePair<string, string>("ltStorageMethod", x.GetString("TisLtStorageMethod_tab"))
                    })
                )
                .Select(
                    x =>
                        x.Where(y => !string.IsNullOrWhiteSpace(y.Value))
                            .Select(y => $"{y.Key}={y.Value}")
                            .Concatenate(","))
                .Concatenate(";");

            occurrenceTissueData.PreparationMaterials = map.GetMaps("preparations")
                .Select(x => new KeyValuePair<string, string>("fixativeTreatment", x.GetString("StrFixativeTreatment_tab")))
                .Concat(map.GetMaps("tissue")
                    .Select(x => new KeyValuePair<string, string>("initialPreservation", x.GetString("TisInitialPreservation_tab"))))
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => $"{x.Key}={x.Value}")
                .Concatenate(",");

            occurrenceTissueData.PreservationType = map.GetMaps("preparations")
                .SelectMany(x => new[]
                {
                    new KeyValuePair<string, string>("fixativeTreatment", x.GetString("StrFixativeTreatment_tab")),
                    new KeyValuePair<string, string>("storageMedium", x.GetString("StrStorageMedium_tab"))
                })
                .Concat(
                    map.GetMaps("tissue")
                        .Select(
                            x =>
                                new KeyValuePair<string, string>("initialPreservation",
                                    x.GetString("TisInitialPreservation_tab"))))
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => $"{x.Key}={x.Value}")
                .Concatenate(",");

            var locationMap = map.GetMap("location");
            if (map.GetString("ManOnLoan") == "Yes")
                occurrenceTissueData.Disposition = "On Loan";
            if (locationMap != null && (locationMap.GetLong("irn") == 294982 || locationMap.GetLong("irn") == 294981))
                occurrenceTissueData.Disposition = "Missing";
            else if (map.GetString("TisTissueUsedUp") == "Yes" || map.GetString("GneDnaUsedUp") == "Yes")
                occurrenceTissueData.Disposition = "Used";
            else
                occurrenceTissueData.Disposition = "In collection";

            occurrenceTissueData.Blocked = map.GetString("TisAvailableForLoan") == "No" ? "Not available for loan" : "Available for loan";
            occurrenceTissueData.PreservationTemperature = map.GetMaps("tissue").FirstOrDefault()?.GetString("TisLtStorageMethod_tab");

            DateTime preservationDateBegin;
            if (DateTime.TryParseExact(
                    string.IsNullOrWhiteSpace(map.GetMaps("tissue").FirstOrDefault()?.GetString("TisDatePrepared0")) ? map.GetMaps("preparations").FirstOrDefault()?.GetString("StrDatePrepared0") : map.GetMaps("tissue").FirstOrDefault()?.GetString("TisDatePrepared0"),
                    new[] { "dd/MM/yyyy", "dd/MM/yy" }, new CultureInfo("en-AU"), DateTimeStyles.None, out preservationDateBegin))
            {
                occurrenceTissueData.PreservationDateBegin = preservationDateBegin.ToString("s");
            }

            occurrenceTissueData.PreparationDate = occurrenceTissueData.PreservationDateBegin;

            occurrenceTissueData.PreparationType = map.GetMaps("tissue").FirstOrDefault()?.GetString("TisTissueType_tab");

            if (map.GetMaps("preparedBy") != null)
                occurrenceTissueData.PreparedBy = map.GetMaps("preparedBy").Where(x => x != null).Select(x => partyFactory.Make(x).Name).Concatenate("; ");

            occurrenceTissueData.SampleSize = map.GetMaps("tissue").FirstOrDefault()?.GetString("TisInitialQuantity_tab");

            var parentMap = map.GetMap("parent");
            if (parentMap != null && parentMap.GetTrimStrings("MdaDataSets_tab").Contains("Atlas of Living Australia"))
            {
                occurrenceTissueData.RelatedResourceCollectionCode = parentMap.GetString("ColDiscipline");
                occurrenceTissueData.RelatedResourceInstitutionCode = "NMV";
                occurrenceTissueData.RelatedResourceCatalogNumber = string.IsNullOrWhiteSpace(parentMap.GetString("ColRegPart"))
                    ? $"{parentMap.GetString("ColRegPrefix")}{parentMap.GetString("ColRegNumber")}"
                    : $"{parentMap.GetString("ColRegPrefix")}{parentMap.GetString("ColRegNumber")}-{parentMap.GetString("ColRegPart")}";
            }
            else
            {
                occurrenceTissueData.RelatedResourceCollectionCode = map.GetString("TisCollectionCode");
                occurrenceTissueData.RelatedResourceInstitutionCode = map.GetString("TisOtherInstitutionNo");
                occurrenceTissueData.RelatedResourceCatalogNumber = map.GetString("TisRegistrationNumber");
            }

            return occurrenceTissueData;
        }

        public IEnumerable<OccurrenceTissueData> Make(IEnumerable<Map> maps)
        {
            return maps.Select(Make);
        }
    }
}