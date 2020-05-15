using CsvHelper.Configuration;
using ImuExports.Tasks.AtlasOfLivingAustralia.Models;

namespace ImuExports.Tasks.AtlasOfLivingAustralia.ClassMaps
{
    public sealed class ResourceRelationshipClassMap : ClassMap<ResourceRelationship>
    {
        public ResourceRelationshipClassMap()
        {
            Map(m => m.CoreId).Name("coreID");
            Map(m => m.ResourceId).Name("resourceId");
            Map(m => m.RelatedResourceId).Name("relatedResourceId");
            Map(m => m.RelationshipOfResource).Name("relationshipOfResource");
        }
    }
}