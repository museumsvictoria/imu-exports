using System.Collections.Generic;

namespace ImuExports.Tasks.AlaExport.Models
{
    public class Occurrence
    {
        public Occurrence()
        {
            this.Images = new List<Image>();
        }

        public IList<Image> Images { get; set; }

        public string DctermsType { get; set; }

        public string DctermsModified { get; set; }

        public string DctermsLanguage { get; set; }

        public string DctermsRights { get; set; }

        public string DctermsRightsHolder { get; set; }

        public string DctermsAccessRights { get; set; }

        public string DctermsBibliographicCitation { get; set; }

        public string DctermsReferences { get; set; }

        public string InstitutionId { get; set; }

        public string CollectionId { get; set; }

        public string DatasetId { get; set; }

        public string InstitutionCode { get; set; }

        public string CollectionCode { get; set; }

        public string DatasetName { get; set; }

        public string OwnerInstitutionCode { get; set; }

        public string BasisOfRecord { get; set; }

        public string InformationWithheld { get; set; }

        public string DataGeneralizations { get; set; }

        public string DynamicProperties { get; set; }

        public string OccurrenceID { get; set; }

        public string CatalogNumber { get; set; }

        public string OccurrenceRemarks { get; set; }

        public string RecordNumber { get; set; }

        public string RecordedBy { get; set; }

        public string IndividualID { get; set; }

        public string IndividualCount { get; set; }

        public string Sex { get; set; }

        public string LifeStage { get; set; }

        public string ReproductiveCondition { get; set; }

        public string Behavior { get; set; }

        public string EstablishmentMeans { get; set; }

        public string OccurrenceStatus { get; set; }

        public string Preparations { get; set; }

        public string Disposition { get; set; }

        public string OtherCatalogNumbers { get; set; }

        public string PreviousIdentifications { get; set; }

        public string AssociatedMedia { get; set; }

        public string AssociatedReferences { get; set; }

        public string AssociatedOccurrences { get; set; }

        public string AssociatedSequences { get; set; }

        public string AssociatedTaxa { get; set; }

        public string EventID { get; set; }

        public string SamplingProtocol { get; set; }

        public string EventDate { get; set; }

        public string EventTime { get; set; }

        public string StartDayOfYear { get; set; }

        public string EndDayOfYear { get; set; }

        public string Year { get; set; }

        public string Month { get; set; }

        public string Day { get; set; }

        public string VerbatimEventDate { get; set; }

        public string Habitat { get; set; }

        public string FieldNumber { get; set; }

        public string FieldNotes { get; set; }

        public string EventRemarks { get; set; }

        public string LocationID { get; set; }

        public string HigherGeographyID { get; set; }

        public string HigherGeography { get; set; }

        public string Continent { get; set; }

        public string WaterBody { get; set; }

        public string IslandGroup { get; set; }

        public string Island { get; set; }

        public string Country { get; set; }

        public string CountryCode { get; set; }

        public string StateProvince { get; set; }

        public string County { get; set; }

        public string Municipality { get; set; }

        public string Locality { get; set; }

        public string VerbatimLocality { get; set; }

        public string VerbatimElevation { get; set; }

        public string MinimumElevationInMeters { get; set; }

        public string MaximumElevationInMeters { get; set; }

        public string VerbatimDepth { get; set; }

        public string MinimumDepthInMeters { get; set; }

        public string MaximumDepthInMeters { get; set; }

        public string MinimumDistanceAboveSurfaceInMeters { get; set; }

        public string MaximumDistanceAboveSurfaceInMeters { get; set; }

        public string LocationAccordingTo { get; set; }

        public string LocationRemarks { get; set; }

        public string VerbatimCoordinates { get; set; }

        public string VerbatimLatitude { get; set; }

        public string VerbatimLongitude { get; set; }

        public string VerbatimCoordinateSystem { get; set; }

        public string VerbatimSRS { get; set; }

        public string DecimalLatitude { get; set; }

        public string DecimalLongitude { get; set; }

        public string GeodeticDatum { get; set; }

        public string CoordinateUncertaintyInMeters { get; set; }

        public string CoordinatePrecision { get; set; }

        public string PointRadiusSpatialFit { get; set; }

        public string FootprintWKT { get; set; }

        public string FootprintSRS { get; set; }

        public string FootprintSpatialFit { get; set; }

        public string GeoreferencedBy { get; set; }

        public string GeoreferencedDate { get; set; }

        public string GeoreferenceProtocol { get; set; }

        public string GeoreferenceSources { get; set; }

        public string GeoreferenceVerificationStatus { get; set; }

        public string GeoreferenceRemarks { get; set; }

        public string IdentificationID { get; set; }

        public string IdentifiedBy { get; set; }

        public string DateIdentified { get; set; }

        public string IdentificationReferences { get; set; }

        public string IdentificationVerificationStatus { get; set; }

        public string IdentificationRemarks { get; set; }

        public string IdentificationQualifier { get; set; }

        public string TypeStatus { get; set; }

        public string TaxonID { get; set; }

        public string ScientificNameID { get; set; }

        public string AcceptedNameUsageID { get; set; }

        public string ParentNameUsageID { get; set; }

        public string OriginalNameUsageID { get; set; }

        public string NameAccordingToID { get; set; }

        public string NamePublishedInID { get; set; }

        public string TaxonConceptID { get; set; }

        public string ScientificName { get; set; }

        public string AcceptedNameUsage { get; set; }

        public string ParentNameUsage { get; set; }

        public string OriginalNameUsage { get; set; }

        public string NameAccordingTo { get; set; }

        public string NamePublishedIn { get; set; }

        public string NamePublishedInYear { get; set; }

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

        public string VerbatimTaxonRank { get; set; }

        public string ScientificNameAuthorship { get; set; }

        public string VernacularName { get; set; }

        public string NomenclaturalCode { get; set; }

        public string TaxonomicStatus { get; set; }

        public string NomenclaturalStatus { get; set; }

        public string TaxonRemarks { get; set; }
    }
}