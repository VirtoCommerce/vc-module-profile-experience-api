using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class GetOrganizationByIdQuery : IQuery<OrganizationAggregate>
    {
        public GetOrganizationByIdQuery(string organizationId)
        {
            OrganizationId = organizationId;
        }

        public string OrganizationId { get; set; }
    }
}
