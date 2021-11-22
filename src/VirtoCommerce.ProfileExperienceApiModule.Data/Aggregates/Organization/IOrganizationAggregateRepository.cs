using System.Collections.Generic;
using System.Threading.Tasks;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization
{
    public interface IOrganizationAggregateRepository : IMemberAggregateRootRepository
    {
        Task<IEnumerable<OrganizationAggregate>> GetOrganizationsByIdsAsync(string[] ids);
    }
}
