using CsvHelper.Configuration;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.ClassMaps
{
    public sealed class OccurrenceClassMap : ClassMap<Occurrence>
    {
        public OccurrenceClassMap()
        {
            Map(m => m.OccurrenceId).Name("occurrenceID");
            Map(m => m.DctermsType).Name("dcterms:type");
            Map(m => m.DctermsModified).Name("dcterms:modified");
            Map(m => m.DctermsLanguage).Name("dcterms:language");
            Map(m => m.DctermsLicense).Name("dcterms:license");
            Map(m => m.DctermsRightsHolder).Name("dcterms:rightsHolder");            
            Map(m => m.InstitutionId).Name("institutionID");
            Map(m => m.InstitutionCode).Name("institutionCode");
            Map(m => m.CollectionCode).Name("collectionCode");
            Map(m => m.DatasetName).Name("datasetName");
            Map(m => m.OwnerInstitutionCode).Name("ownerInstitutionCode");
            Map(m => m.BasisOfRecord).Name("basisOfRecord");
            Map(m => m.CatalogNumber).Name("catalogNumber");
            Map(m => m.RecordedBy).Name("recordedBy");
            Map(m => m.IndividualCount).Name("individualCount");
            Map(m => m.Sex).Name("sex");
            Map(m => m.LifeStage).Name("lifeStage");
            Map(m => m.OccurrenceStatus).Name("occurrenceStatus");
            Map(m => m.Preparations).Name("preparations");
            Map(m => m.AssociatedMedia).Name("associatedMedia");
            Map(m => m.EventId).Name("eventID");
            Map(m => m.SamplingProtocol).Name("samplingProtocol");
            Map(m => m.EventDate).Name("eventDate");
            Map(m => m.EventTime).Name("eventTime");
            Map(m => m.Year).Name("year");
            Map(m => m.Month).Name("month");
            Map(m => m.Day).Name("day");
            Map(m => m.FieldNumber).Name("fieldNumber");
            Map(m => m.LocationId).Name("locationID");
            Map(m => m.HigherGeography).Name("higherGeography");
            Map(m => m.Continent).Name("continent");
            Map(m => m.WaterBody).Name("waterBody");
            Map(m => m.Country).Name("country");
            Map(m => m.StateProvince).Name("stateProvince");
            Map(m => m.IslandGroup).Name("islandGroup");
            Map(m => m.Island).Name("island");
            Map(m => m.County).Name("county");
            Map(m => m.Municipality).Name("municipality");
            Map(m => m.Locality).Name("locality");
            Map(m => m.VerbatimLocality).Name("verbatimLocality");
            Map(m => m.MinimumElevationInMeters).Name("minimumElevationInMeters");
            Map(m => m.MaximumElevationInMeters).Name("maximumElevationInMeters");
            Map(m => m.MinimumDepthInMeters).Name("minimumDepthInMeters");
            Map(m => m.MaximumDepthInMeters).Name("maximumDepthInMeters");
            Map(m => m.DecimalLatitude).Name("decimalLatitude");
            Map(m => m.DecimalLongitude).Name("decimalLongitude");
            Map(m => m.GeodeticDatum).Name("geodeticDatum");
            Map(m => m.CoordinateUncertaintyInMeters).Name("coordinateUncertaintyInMeters");
            Map(m => m.GeoreferencedBy).Name("georeferencedBy");
            Map(m => m.GeoreferencedDate).Name("georeferencedDate");
            Map(m => m.GeoreferenceProtocol).Name("georeferenceProtocol");
            Map(m => m.GeoreferenceSources).Name("georeferenceSources");
            Map(m => m.IdentifiedBy).Name("identifiedBy");
            Map(m => m.DateIdentified).Name("dateIdentified");
            Map(m => m.IdentificationQualifier).Name("identificationQualifier");
            Map(m => m.TypeStatus).Name("typeStatus");
            Map(m => m.ScientificName).Name("scientificName");
            Map(m => m.HigherClassification).Name("higherClassification");
            Map(m => m.Kingdom).Name("kingdom");
            Map(m => m.Phylum).Name("phylum");
            Map(m => m.Class).Name("class");
            Map(m => m.Order).Name("order");
            Map(m => m.Family).Name("family");
            Map(m => m.Genus).Name("genus");
            Map(m => m.Subgenus).Name("subgenus");
            Map(m => m.SpecificEpithet).Name("specificEpithet");
            Map(m => m.InfraspecificEpithet).Name("infraspecificEpithet");
            Map(m => m.TaxonRank).Name("taxonRank");
            Map(m => m.ScientificNameAuthorship).Name("scientificNameAuthorship");
            Map(m => m.VernacularName).Name("vernacularName");
            Map(m => m.NomenclaturalCode).Name("nomenclaturalCode");
            Map(m => m.Blocked).Name("blocked");
            Map(m => m.Disposition).Name("disposition");
            Map(m => m.MaterialSampleType).Name("materialSampleType");
            Map(m => m.PreparationType).Name("preparationType");
            Map(m => m.PreparationMaterials).Name("preparationMaterials");
            Map(m => m.PreparedBy).Name("preparedBy");
            Map(m => m.PreparationDate).Name("preparationDate");
            Map(m => m.PreservationType).Name("preservationType");
            Map(m => m.PreservationTemperature).Name("preservationTemperature");
            Map(m => m.PreservationDateBegin).Name("preservationDateBegin");
            Map(m => m.RelatedResourceId).Name("relatedResourceId");
            Map(m => m.RelationshipOfResource).Name("relationshipOfResource");
        }
    }
}