namespace ImuExports.Tasks.AtlasOfLivingAustraliaTissueData.Models
{
    public class OccurrenceTissueData
    {
        public string DctermsType { get; set; }

        public string DctermsModified { get; set; }

        public string DctermsLanguage { get; } = "en";

        public string DctermsLicense { get; } = "https://creativecommons.org/publicdomain/zero/1.0/legalcode";

        public string DctermsRightsHolder { get; }  = "Museums Victoria";

        public string InstitutionId { get; } = "NMV";

        public string CollectionId { get; } = "urn:lsid:biocol.org:col:34978";

        public string DatasetId { get; set; }

        public string InstitutionCode { get; } = "NMV";

        public string CollectionCode { get; set; }

        public string DatasetName { get; set; }

        public string OwnerInstitutionCode { get; } = "NMV";

        public string BasisOfRecord { get; set; }

        public string OccurrenceID { get; set; }

        public string CatalogNumber { get; set; }

        public string RelatedResourceCollectionCode { get; set; }

        public string RelatedResourceInstitutionCode { get; set; }

        public string RelatedResourceCatalogNumber { get; set; }

        public string RecordedBy { get; set; }

        public string IndividualCount { get; set; }

        public string Sex { get; set; }

        public string LifeStage { get; set; }

        public string OccurrenceStatus { get; } = "present";

        public string Preparations { get; set; }

        public string Disposition { get; set; }

        public string EventID { get; set; }

        public string SamplingProtocol { get; set; }

        public string EventDate { get; set; }

        public string EventTime { get; set; }

        public string Year { get; set; }

        public string Month { get; set; }

        public string Day { get; set; }

        public string FieldNumber { get; set; }

        public string LocationID { get; set; }

        public string HigherGeography { get; set; }

        public string Continent { get; set; }

        public string WaterBody { get; set; }

        public string Country { get; set; }

        public string StateProvince { get; set; }

        public string County { get; set; }

        public string Municipality { get; set; }

        public string Locality { get; set; }

        public string VerbatimLocality { get; set; }

        public string MinimumElevationInMeters { get; set; }

        public string MaximumElevationInMeters { get; set; }

        public string MinimumDepthInMeters { get; set; }

        public string MaximumDepthInMeters { get; set; }

        public string DecimalLatitude { get; set; }

        public string DecimalLongitude { get; set; }

        public string GeodeticDatum { get; set; }

        public string CoordinateUncertaintyInMeters { get; set; }

        public string GeoreferencedBy { get; set; }

        public string GeoreferencedDate { get; set; }

        public string GeoreferenceProtocol { get; set; }

        public string GeoreferenceSources { get; set; }

        public string IdentifiedBy { get; set; }

        public string DateIdentified { get; set; }

        public string IdentificationQualifier { get; set; }

        public string TypeStatus { get; set; }

        public string ScientificName { get; set; }

        public string HigherClassification { get; set; }

        public string Kingdom { get; set; }

        public string Phylum { get; set; }

        public string Class { get; set; }

        public string Order { get; set; }

        public string Family { get; set; }

        public string Genus { get; set; }

        public string Subgenus { get; set; }

        public string SpecificEpithet { get; set; }

        public string InfraspecificEpithet { get; set; }

        public string TaxonRank { get; set; }

        public string ScientificNameAuthorship { get; set; }

        public string VernacularName { get; set; }

        public string NomenclaturalCode { get; set; }

        public string MaterialSampleType { get; set; }

        public string RelationshipOfResource { get; } = "same individual";

        public string Blocked { get; set; }

        public string PreservationType { get; set; }

        public string PreservationTemperature { get; set; }

        public string PreservationDateBegin { get; set; }

        public string PreparationType { get; set; }

        public string PreparationMaterials { get; set; }

        public string PreparedBy { get; set; }

        public string PreparationDate { get; set; }

        public string SampleSize { get; set; }
    }
}