using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization
{
    public class OrganizationAggregateRepository : MemberAggregateRootRepository, IOrganizationAggregateRepository
    {
        public OrganizationAggregateRepository(IMemberService memberService, IMemberAggregateFactory factory)
            : base(memberService, factory)
        {
        }

        public async Task<IEnumerable<OrganizationAggregate>> GetOrganizationsByIdsAsync(string[] ids)
        {
            var members = await _memberService.GetByIdsAsync(ids, null, new[] { nameof(Organization) });

            if (!members.IsNullOrEmpty())
            {
                // Copies of the member service's cached instances, as in GetMemberAggregateRootByIdAsync. Materialized,
                // so that every enumeration sees the same copies
                return members.OfType<CustomerModule.Core.Model.Organization>().Select(x => _memberAggregateFactory.Create<OrganizationAggregate>(x.CloneTyped())).ToList();
            }

            return Enumerable.Empty<OrganizationAggregate>();
        }
    }
}
