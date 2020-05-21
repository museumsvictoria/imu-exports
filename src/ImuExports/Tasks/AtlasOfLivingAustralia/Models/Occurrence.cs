using System.Collections.Generic;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.Models
{
    public class Occurrence
    {
        public Occurrence()
        {
            this.Loan = new Loan();
            this.MaterialSample = new MaterialSample();
            this.Multimedia = new List<Multimedia>();
            this.Preparation = new Preparation();
            this.Preservation = new Preservation();
            this.ResourceRelationship = new ResourceRelationship();
        }

        public Loan Loan { get; set; }

        public MaterialSample MaterialSample { get; set; }

        public IList<Multimedia> Multimedia { get; set; }

        public Preparation Preparation { get; set; }

        public Preservation Preservation { get; set; }

        public ResourceRelationship ResourceRelationship { get; set; }

        public string DctermsType { get; set; }

        public string DctermsModified { get; set; }

        public string DctermsLanguage { get; set; }

        public string DctermsLicense { get; set; }

        public string DctermsRightsHolder { get; set; }

        public string InstitutionId { get; set; }

        public string InstitutionCode { get; set; }

        public string CollectionCode { get; set; }

        public string DatasetName { get; set; }

        public string OwnerInstitutionCode { get; set; }

        public string BasisOfRecord { get; set; }

        public string OccurrenceId { get; set; }

        public string CatalogNumber { get; set; }

        public string OccurrenceRemarks { get; set; }

        public string RecordedBy { get; set; }

        public string IndividualCount { get; set; }

        public string Sex { get; set; }

        public string LifeStage { get; set; }

        public string OccurrenceStatus { get; set; }

        public string Preparations { get; set; }

        public string AssociatedMedia { get; set; }

        public string EventId { get; set; }

        public string SamplingProtocol { get; set; }

        public string EventDate { get; set; }

        public string EventTime { get; set; }

        public string Year { get; set; }

        public string Month { get; set; }

        public string Day { get; set; }

        public string FieldNumber { get; set; }

        public string LocationId { get; set; }

        public string HigherGeography { get; set; }

        public string Continent { get; set; }

        public string WaterBody { get; set; }

        public string IslandGroup { get; set; }

        public string Island { get; set; }

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
    }
}