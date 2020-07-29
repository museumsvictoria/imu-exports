using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IMu;
using ImuExports.Config;
using ImuExports.Extensions;
using ImuExports.Infrastructure;
using ImuExports.Tasks.AtlasOfLivingAustralia.Config;
using ImuExports.Tasks.AtlasOfLivingAustralia.Helpers;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Factories
{
    public class OccurrenceFactory : IFactory<Occurrence>
    {
        private readonly IFactory<Multimedia> multimediaFactory;

        public OccurrenceFactory(
            IFactory<Multimedia> multimediaFactory)
        {
            this.multimediaFactory = multimediaFactory;
        }

        public Occurrence Make(Map map)
        {
            var occurrence = new Occurrence();

            // Occurrence core fields
            occurrence.OccurrenceId = MakeOccurrenceId(map);

            switch (map.GetTrimString("ColTypeOfItem"))
            {
                case "Specimen":
                    occurrence.DctermsType = "PhysicalObject";
                    break;
                case "Audiovisual":
                case "Image":
                    occurrence.DctermsType = "Event";
                    break;
            }
            
            occurrence.DctermsModified = MakeDctermsModified(map);
            occurrence.DctermsLanguage = "en";
            occurrence.DctermsLicense = "https://creativecommons.org/publicdomain/zero/1.0/legalcode";
            occurrence.DctermsRightsHolder = "Museums Victoria";
            occurrence.InstitutionId = "urn:lsid:biocol.org:col:34978";
            occurrence.InstitutionCode = occurrence.OwnerInstitutionCode = "NMV";
            occurrence.CollectionCode = map.GetTrimString("ColDiscipline");
            occurrence.OccurrenceStatus = "present";

            var colevent = map.GetMap("colevent");
            if (colevent != null)
                occurrence.DatasetName = colevent.GetTrimString("ExpExpeditionName");

            occurrence.BasisOfRecord = MakeBasisOfRecord(map);
            occurrence.CatalogNumber = MakeCatalogNumber(map);

            var individualCount = 0;
            if (!string.IsNullOrWhiteSpace(map.GetTrimString("SpeNoSpecimens")))
                individualCount += int.Parse(map.GetTrimString("SpeNoSpecimens"));
            if (!string.IsNullOrWhiteSpace(map.GetTrimString("BirTotalClutchSize")))
                individualCount += int.Parse(map.GetTrimString("BirTotalClutchSize"));
            if (individualCount > 0)
                occurrence.IndividualCount = individualCount.ToString();

            occurrence.Sex = map.GetTrimStrings("SpeSex_tab").Concatenate(" | ");
            occurrence.LifeStage = map.GetTrimStrings("SpeStageAge_tab").Concatenate(" | ");

            occurrence.Preparations = map.GetMaps("preparations")
                .Select(x => new[]
                {
                    x.GetString("StrSpecimenNature_tab"),
                    x.GetString("StrSpecimenForm_tab"),
                    x.GetString("StrFixativeTreatment_tab"),
                    x.GetString("StrStorageMedium_tab")
                })
                .Concat(map.GetMaps("tissue")
                    .Select(x => new[]
                    {
                        x.GetString("TisTissueType_tab"),
                        x.GetString("TisInitialPreservation_tab"),
                        x.GetString("TisLtStorageMethod_tab")
                    }))
                .SelectMany(x => x)
                .Distinct()
                .Where(x => !string.Equals(x, "none", StringComparison.OrdinalIgnoreCase))
                .Concatenate(" | ");

            if (colevent != null)
            {
                occurrence.EventId = colevent.GetTrimString("ColCollectionEventCode");
                occurrence.SamplingProtocol = colevent.GetTrimString("ColCollectionMethod");

                IList<DateTime> eventDates = new List<DateTime>();
                IList<TimeSpan> eventTimes = new List<TimeSpan>();
                var culture = new CultureInfo("en-AU");

                if (DateTime.TryParseExact(colevent.GetTrimString("ColDateVisitedFrom"), new[] { "dd/MM/yyyy", "dd/MM/yy" }, culture, DateTimeStyles.None, out var eventDateFrom))
                {
                    if (TimeSpan.TryParseExact(colevent.GetTrimString("ColTimeVisitedFrom"), @"hh\:mm", culture, out var eventTimeFrom))
                    {
                        eventDateFrom += eventTimeFrom;
                        eventTimes.Add(eventTimeFrom);
                    }

                    eventDates.Add(eventDateFrom);
                }

                if (DateTime.TryParseExact(colevent.GetTrimString("ColDateVisitedTo"), new[] { "dd/MM/yyyy", "dd/MM/yy" }, culture, DateTimeStyles.None, out var eventDateTo))
                {
                    if (TimeSpan.TryParseExact(colevent.GetTrimString("ColTimeVisitedTo"), @"hh\:mm", culture, out var eventTimeTo))
                    {
                        eventDateTo += eventTimeTo;
                        eventTimes.Add(eventTimeTo);
                    }

                    eventDates.Add(eventDateTo);
                }

                occurrence.EventDate = eventDates.Select(x => x.ToString("s")).Concatenate("\\");
                occurrence.EventTime = eventTimes.Select(x => x.ToString("c")).Concatenate("\\");

                occurrence.FieldNumber = colevent.GetTrimString("ColCollectionEventCode");
                occurrence.MinimumDepthInMeters = colevent.GetTrimString("AquDepthFromMet");
                occurrence.MaximumDepthInMeters = colevent.GetTrimString("AquDepthToMet");

                if (colevent.GetMaps("collectors") != null)
                    occurrence.RecordedBy = colevent.GetMaps("collectors").Where(x => x != null).Select(MakePartyName).Concatenate(" | ");
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
                    occurrence.LocationId = $"{site.GetTrimString("SitSiteCode")}{site.GetTrimString("SitSiteNumber")}";

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
                                }.Concatenate(" | ");

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
                        occurrence.DecimalLatitude = decimalLatitude.FirstOrDefault(x => x != null)?.ToString();

                    var decimalLongitude = ((object[])latlong["LatLongitudeDecimal_nesttab"]);
                    if (decimalLongitude != null && decimalLongitude.Any(x => x != null))
                        occurrence.DecimalLongitude = decimalLongitude.FirstOrDefault(x => x != null)?.ToString();

                    occurrence.CoordinateUncertaintyInMeters = latlong.GetTrimString("LatRadiusNumeric_tab");
                    occurrence.GeodeticDatum = (string.IsNullOrWhiteSpace(latlong.GetTrimString("LatDatum_tab"))) ? "WGS84" : latlong.GetTrimString("LatDatum_tab");

                    occurrence.GeoreferencedBy = MakePartyName(latlong.GetMap("determinedBy"));

                    if (DateTime.TryParseExact(latlong.GetTrimString("LatDetDate0"), "dd/MM/yyyy", new CultureInfo("en-AU"), DateTimeStyles.None, out var georeferencedDate))
                        occurrence.GeoreferencedDate = georeferencedDate.ToString("s");

                    occurrence.GeoreferenceProtocol = latlong.GetTrimString("LatLatLongDetermination_tab");
                    occurrence.GeoreferenceSources = latlong.GetTrimString("LatDetSource_tab");
                }
            }

            var identification = MapSearches.GetIdentification(map);
            if (identification != null)
            {
                occurrence.TypeStatus = identification.GetTrimString("IdeTypeStatus_tab");
                occurrence.DateIdentified = identification.GetTrimString("IdeDateIdentified0");
                
                if (identification.GetMaps("identifiers") != null)
                    occurrence.IdentifiedBy = identification.GetMaps("identifiers").Where(x => x != null).Select(MakePartyName).Concatenate(" | ");

                var taxonomy = identification.GetMap("taxa");
                if (taxonomy != null)
                {
                    occurrence.ScientificName = new[]
                            {
                                taxonomy.GetCleanString("ClaGenus"),
                                string.IsNullOrWhiteSpace(taxonomy.GetCleanString("ClaSubgenus"))
                                    ? null
                                    : $"({taxonomy.GetCleanString("ClaSubgenus")})",
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
                                }.Concatenate(" | ");

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
                        occurrence.IdentificationQualifier = $"{identificationQualifier} {occurrence.Genus}";
                    else if (string.Equals(identification.GetTrimString("IdeQualifierRank_tab"), "species", StringComparison.OrdinalIgnoreCase))
                        occurrence.IdentificationQualifier = $"{identificationQualifier} {occurrence.SpecificEpithet}";
                }
            }

            // Tissue extension fields
            if (map.GetTrimString("ColTypeOfItem") == "Specimen" && map.GetTrimString("ColRegPrefix") == "Z")
            {
                // Loan
                var locationMap = map.GetMap("location");
                if (map.GetString("ManOnLoan") == "Yes")
                    occurrence.Disposition = "On Loan";
                else if (locationMap != null && (locationMap.GetLong("irn") == 294982 || locationMap.GetLong("irn") == 294981))
                    occurrence.Disposition = "Missing";
                else if (map.GetString("TisTissueUsedUp") == "Yes" || map.GetString("GneDnaUsedUp") == "Yes")
                    occurrence.Disposition = "Used";
                else
                    occurrence.Disposition = "In collection";

                if (map.GetString("TisAvailableForLoan") == "No")
                    occurrence.Blocked = "Not available for loan";
                else if (map.GetString("TisAvailableForLoan") == "Yes")
                    occurrence.Blocked = "Available for loan";

                // Material Sample
                occurrence.MaterialSampleType = "Tissue";

                // Preparation
                occurrence.PreparationType = map.GetMaps("tissue").Select(x => x.GetString("TisTissueType_tab")).Concatenate(" | ");
                occurrence.PreparationMaterials = map.GetMaps("preparations")
                    .Select(x => new[]
                    {
                        x.GetString("StrFixativeTreatment_tab"),
                    })
                    .Concat(map.GetMaps("tissue")
                        .Select(x => new[]
                        {
                            x.GetString("TisInitialPreservation_tab"),
                        }))
                    .SelectMany(x => x)
                    .Distinct()
                    .Where(x => !string.Equals(x, "none", StringComparison.OrdinalIgnoreCase))
                    .Concatenate(" | ");
                if (map.GetMaps("preparedby") != null)
                    occurrence.PreparedBy = map.GetMaps("preparedby").Select(MakePartyName).Concatenate(" | ");
                occurrence.PreparationDate = occurrence.PreservationDateBegin;

                // Preservation
                occurrence.PreservationType = map.GetMaps("preparations")
                    .Select(x => new[]
                    {
                        x.GetString("StrFixativeTreatment_tab"),
                        x.GetString("StrStorageMedium_tab")
                    })
                    .Concat(map.GetMaps("tissue")
                        .Select(x => new[]
                        {
                            x.GetString("TisInitialPreservation_tab"),
                        }))
                    .SelectMany(x => x)
                    .Distinct()
                    .Where(x => !string.Equals(x, "none", StringComparison.OrdinalIgnoreCase))
                    .Concatenate(" | ");
                occurrence.PreservationTemperature = map.GetMaps("tissue").Select(x => x.GetString("TisLtStorageMethod_tab")).Distinct().Concatenate(" | ");
                if (DateTime.TryParseExact(
                    string.IsNullOrWhiteSpace(map.GetMaps("tissue").FirstOrDefault()?.GetString("TisDatePrepared0")) ? map.GetMaps("preparations").FirstOrDefault()?.GetString("StrDatePrepared0") : map.GetMaps("tissue").FirstOrDefault()?.GetString("TisDatePrepared0"),
                    new[] { "dd/MM/yyyy", "dd/MM/yy" }, new CultureInfo("en-AU"), DateTimeStyles.None, out var preservationDateBegin))
                {
                    occurrence.PreservationDateBegin = preservationDateBegin.ToString("s");
                }

                // Resource Relationship
                var parentMap = map.GetMap("parent");
                if (parentMap != null && parentMap.GetTrimStrings("MdaDataSets_tab").Contains(AtlasOfLivingAustraliaConstants.QueryString))
                {
                    occurrence.RelatedResourceId = MakeOccurrenceId(parentMap);
                    occurrence.RelationshipOfResource = "same individual";
                }
                else if (!string.IsNullOrWhiteSpace(map.GetTrimString("TisCollectionCode")) ||
                         !string.IsNullOrWhiteSpace(map.GetTrimString("TisOtherInstitutionNo")) ||
                         !string.IsNullOrWhiteSpace(map.GetTrimString("TisRegistrationNumber")))
                {
                    occurrence.RelatedResourceId = new[]
                    {
                        map.GetTrimString("TisOtherInstitutionNo"),
                        map.GetTrimString("TisCollectionCode"),
                        map.GetTrimString("TisRegistrationNumber")
                    }.Concatenate(":");
                    occurrence.RelationshipOfResource = "same individual";
                }
            }

            // Multimedia extension fields
            occurrence.Multimedia = multimediaFactory.Make(map.GetMaps("media")).ToList();
            
            foreach (var multimedia in occurrence.Multimedia)
            {
                multimedia.CoreId = occurrence.OccurrenceId;
                multimedia.References = $"https://collections.museumsvictoria.com.au/specimens/{map.GetLong("irn")}";
            }

            occurrence.AssociatedMedia = MakeAssociatedMedia(map.GetMaps("media"));

            return occurrence;
        }

        public IEnumerable<Occurrence> Make(IEnumerable<Map> maps)
        {
            return maps.Select(Make);
        }

        private string MakePartyName(Map map)
        {
            if (map == null) return null;

            switch (map.GetTrimString("NamPartyType"))
            {
                case "Collaboration":
                    return new[]
                    {
                        map.GetTrimString("ColCollaborationName")
                    }.Concatenate(", ");
                case "Cutter Number":
                    return new[]
                    {
                        map.GetTrimString("NamBranch"),
                        map.GetTrimString("NamDepartment"),
                        map.GetTrimString("NamOrganisation"),
                        map.GetTrimString("AddPhysStreet"),
                        map.GetTrimString("AddPhysCity"),
                        map.GetTrimString("AddPhysState"),
                        map.GetTrimString("AddPhysCountry")
                    }.Concatenate(", ");
                case "Organisation":
                    return new[]
                    {
                        map.GetTrimString("NamBranch"),
                        map.GetTrimString("NamDepartment"),
                        map.GetTrimString("NamOrganisation")
                    }.Concatenate(", ");
                case "Person":
                    return new[]
                    {
                        map.GetTrimString("NamFullName"),
                        map.GetTrimString("NamOrganisation")
                    }.Concatenate(" - ");
                case "Position":
                    break;
                case "Transport":
                    var name = string.Empty;
                    var organisationOtherName = map.GetTrimStrings("NamOrganisationOtherNames_tab").FirstOrDefault();
                    var source = map.GetTrimString("NamSource");

                    if (string.IsNullOrWhiteSpace(organisationOtherName) && !string.IsNullOrWhiteSpace(source))
                    {
                        name = source;
                    }
                    else if (!string.IsNullOrWhiteSpace(organisationOtherName) && string.IsNullOrWhiteSpace(source))
                    {
                        name = organisationOtherName;
                    }
                    else if (!string.IsNullOrWhiteSpace(organisationOtherName) && !string.IsNullOrWhiteSpace(source))
                    {
                        name = $"{organisationOtherName} ({source})";
                    }

                    return new[]
                    {
                        name,
                        map.GetTrimString("NamFullName"),
                        map.GetTrimString("NamOrganisation")
                    }.Concatenate(", ");
            }

            return null;
        }

        private string MakeBasisOfRecord(Map map)
        {
            if (map.GetTrimString("ColTypeOfItem") == "Specimen")
            {
                return map.GetTrimString("ColRegPrefix") == "Z" ? "MaterialSample" : "PreservedSpecimen";
            }
            if (map.GetTrimString("ColTypeOfItem") == "Audiovisual" || map.GetTrimString("ColTypeOfItem") == "Image")
                return "HumanObservation";
            
            return null;
        }

        private string MakeOccurrenceId(Map map)
        {
            return new[]
            {
                "urn",
                "lsid",
                "ozcam.taxonomy.org.au",
                "NMV",
                map.GetTrimString("ColDiscipline"),
                MakeBasisOfRecord(map),
                MakeCatalogNumber(map),
            }.Concatenate(":");
        }

        private string MakeCatalogNumber(Map map)
        {
            return string.IsNullOrWhiteSpace(map.GetTrimString("ColRegPart"))
                ? $"{map.GetTrimString("ColRegPrefix")}{map.GetTrimString("ColRegNumber")}"
                : $"{map.GetTrimString("ColRegPrefix")}{map.GetTrimString("ColRegNumber")}-{map.GetTrimString("ColRegPart")}";
        }

        private string MakeAssociatedMedia(Map[] maps)
        {
            return maps.Select(map =>
            {
                if (map != null &&
                    string.Equals(map.GetTrimString("AdmPublishWebNoPassword"), "yes",
                        StringComparison.OrdinalIgnoreCase) &&
                    map.GetTrimStrings("MdaDataSets_tab").Contains(AtlasOfLivingAustraliaConstants.QueryString) &&
                    map.GetTrimStrings("MdaDataSets_tab").Contains("Collections Online: MMR") &&
                    string.Equals(map.GetTrimString("MulMimeType"), "image", StringComparison.OrdinalIgnoreCase))
                {
                    var irn = map.GetLong("irn");

                    return $"https://collections.museumvictoria.com.au/content/media/{(irn % 50)}/{irn}-large.jpg";
                }

                return null;
            }).Concatenate(" | ");
        }
        
        private string MakeDctermsModified(Map map)
        {
            var datesModified = new List<DateTime>();
            
            // Catalogue
            if (DateTime.TryParseExact(
                $"{map.GetTrimString("AdmDateModified")} {map.GetTrimString("AdmTimeModified")}",
                "dd/MM/yyyy HH:mm",
                new CultureInfo("en-AU"),
                DateTimeStyles.None,
                out var catalogueDateModified))
            {
                datesModified.Add(catalogueDateModified);
            }
            
            // Collection Event
            var colevent = map.GetMap("colevent");
            
            if (colevent != null &&
                DateTime.TryParseExact(
                    $"{colevent.GetTrimString("AdmDateModified")} {colevent.GetTrimString("AdmTimeModified")}",
                    "dd/MM/yyyy HH:mm",
                    new CultureInfo("en-AU"),
                    DateTimeStyles.None,
                    out var coleventDateModified))
            {
                datesModified.Add(coleventDateModified);
            }
            
            // Site
            var site = map.GetMap("site");
            if (site == null && colevent != null)
                site = colevent.GetMap("site");
            
            if (site != null &&
                DateTime.TryParseExact(
                    $"{site.GetTrimString("AdmDateModified")} {site.GetTrimString("AdmTimeModified")}",
                    "dd/MM/yyyy HH:mm",
                    new CultureInfo("en-AU"),
                    DateTimeStyles.None,
                    out var siteDateModified))
            {
                datesModified.Add(siteDateModified);
            }
            
            // Media
            var medias = map.GetMaps("media");
            
            foreach (var media in medias)
            {
                if (Assertions.IsMultimedia(media) &&
                    DateTime.TryParseExact(
                        $"{media.GetTrimString("AdmDateModified")} {media.GetTrimString("AdmTimeModified")}",
                        "dd/MM/yyyy HH:mm",
                        new CultureInfo("en-AU"),
                        DateTimeStyles.None,
                        out var mediaDateModified))
                {
                    datesModified.Add(mediaDateModified);
                }
            }

            // Taxonomy
            var identification = MapSearches.GetIdentification(map);
            var taxonomy = identification?.GetMap("taxa");
            
            if (taxonomy != null &&
                DateTime.TryParseExact(
                    $"{taxonomy.GetTrimString("AdmDateModified")} {taxonomy.GetTrimString("AdmTimeModified")}",
                    "dd/MM/yyyy HH:mm",
                    new CultureInfo("en-AU"),
                    DateTimeStyles.None,
                    out var taxonomyDateModified))
            {
                datesModified.Add(taxonomyDateModified);
            }

            return datesModified.Max().ToString("s");
        }
    }
}
